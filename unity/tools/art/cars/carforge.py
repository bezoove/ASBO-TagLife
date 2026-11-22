# Car modeller: builds UK 70s-90s cars in Blender from a few measurements, for ASBO: Life on Tag.
# Car space while building: x forward (front at +L/2), y to the car's left, z up, origin on the ground under the middle.
# Everything is rotated at the end so the front faces -Y in Blender (= +Z in Unity).
import bpy, bmesh, math, sys, os, json
from mathutils import Vector, Matrix

def lerp(a, b, t): return a + (b - a) * t
def clamp(v, a, b): return max(a, min(b, v))
def smooth(t): t = clamp(t, 0, 1); return t * t * (3 - 2 * t)

# ------------------------------------------------------------------ materials
MATS = {}
def mat(name, col, rough=0.5, metal=0.0, alpha=1.0, emit=None):
    if name in MATS: return MATS[name]
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes.get('Principled BSDF')
    b.inputs['Base Color'].default_value = (*col, 1)
    b.inputs['Roughness'].default_value = rough; b.inputs['Metallic'].default_value = metal
    if alpha < 1: b.inputs['Alpha'].default_value = alpha
    if emit: b.inputs['Emission Color'].default_value = (*emit, 1); b.inputs['Emission Strength'].default_value = 1.0
    MATS[name] = m; return m

def materials(paint):
    return {
        'paint': mat('paint', paint, 0.25, 0.0),
        'trim': mat('trim', (0.025, 0.025, 0.028), 0.6),
        'chrome': mat('chrome', (0.8, 0.8, 0.82), 0.12, 1.0),
        'glass': mat('glass', (0.05, 0.07, 0.08), 0.05, 0.0, 0.45),
        'lens': mat('lens', (0.85, 0.86, 0.88), 0.08),
        'red': mat('lamp_red', (0.55, 0.02, 0.02), 0.15),
        'amber': mat('lamp_amber', (0.9, 0.42, 0.02), 0.15),
        'inside': mat('interior', (0.045, 0.045, 0.05), 0.85),
        'tyre': mat('tyre', (0.03, 0.03, 0.032), 0.85),
        'rim': mat('rim', (0.6, 0.6, 0.62), 0.3, 0.9),
        'plate': mat('plate', (0.95, 0.95, 0.95), 0.4),
        'under': mat('under', (0.02, 0.02, 0.02), 0.9),
    }

# ------------------------------------------------------------------ mesh helper
class Kit:
    def __init__(self): self.bm = bmesh.new(); self.mats = []
    def mi(self, m):
        if m not in self.mats: self.mats.append(m)
        return self.mats.index(m)
    def face(self, pts, m, flip=False):
        vs = [self.bm.verts.new(p) for p in pts]
        if flip: vs.reverse()
        f = self.bm.faces.new(vs); f.material_index = self.mi(m); return f
    def box(self, c, s, m, rot=None):
        """Axis box centre c, size s (optionally rotated about z by rot radians)."""
        hx, hy, hz = s[0] / 2, s[1] / 2, s[2] / 2
        R = Matrix.Rotation(rot or 0, 3, 'Z')
        P = [Vector(c) + R @ Vector((sx * hx, sy * hy, sz * hz)) for sx in (-1, 1) for sy in (-1, 1) for sz in (-1, 1)]
        vs = [self.bm.verts.new(p) for p in P]
        q = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]
        k = self.mi(m)
        for a in q:
            f = self.bm.faces.new([vs[i] for i in a]); f.material_index = k
    def build(self, name, smooth_angle=None, recalc=True):
        me = bpy.data.meshes.new(name)
        if recalc: bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces[:])
        self.bm.to_mesh(me); self.bm.free()
        for m in self.mats: me.materials.append(m)
        ob = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(ob)
        if smooth_angle is not None:
            for p in me.polygons: p.use_smooth = True
            set_auto_smooth(ob, smooth_angle)
        return ob

def set_auto_smooth(ob, angle_deg):
    try:
        bpy.context.view_layer.objects.active = ob
        with bpy.context.temp_override(object=ob, active_object=ob, selected_objects=[ob], selected_editable_objects=[ob]):
            bpy.ops.object.shade_smooth_by_angle(angle=math.radians(angle_deg))
    except Exception as e:
        print('auto smooth failed', e)

# ------------------------------------------------------------------ the body
class Car:
    def __init__(self, P):
        self.P = P; L = P['L']
        self.L = L; self.hw0 = P['W'] / 2 - 0.02
        # axle positions measured from the front tip
        self.uF = P['fo']; self.uR = P['fo'] + P['wb']
        # top profile: (u from front tip, z)
        self.top = [(t * L if t <= 1.0 else t, z) for t, z in P['top']]
        self.top = [(u, z) for u, z in P['top']]

    def interp(self, pts, u):
        if u <= pts[0][0]: return pts[0][1]
        for (u0, z0), (u1, z1) in zip(pts, pts[1:]):
            if u <= u1: return lerp(z0, z1, (u - u0) / max(1e-6, u1 - u0))
        return pts[-1][1]

    def ztop(self, u): return self.interp(self.top, u)
    def zbot(self, u):
        P = self.P; zs = P['sill']
        zf = P.get('bumpF_bot', zs + 0.08); zr = P.get('bumpR_bot', zs + 0.1)
        if u < P['fo'] * 0.55: return lerp(zf, zs, smooth(u / (P['fo'] * 0.55)))
        ro = self.L - self.uR
        if u > self.L - ro * 0.6: return lerp(zs, zr, smooth((u - (self.L - ro * 0.6)) / (ro * 0.6)))
        return zs
    def zbelt(self, u):
        P = self.P; b0, b1 = P['belt'], P['belt'] + P.get('belt_rise', 0.03)
        return lerp(b0, b1, clamp(u / self.L, 0, 1))
    def half(self, u):
        """Half-width in plan: rounded corners front and rear."""
        rf, rr = self.P['cornerF'], self.P['cornerR']; hw = self.hw0
        if u < rf: d = rf - u; return hw - rf + math.sqrt(max(0, rf * rf - d * d))
        if u > self.L - rr: d = u - (self.L - rr); return hw - rr + math.sqrt(max(0, rr * rr - d * d))
        return hw

    def section(self, u):
        P = self.P
        hw = self.half(u); zb = self.zbot(u); zt = self.ztop(u); zbl = min(self.zbelt(u), zt - 0.02)
        g = smooth((zt - zbl - 0.06) / 0.22)            # how much greenhouse there is here
        hr = hw - P['tumble'] * g - (1 - g) * 0.07
        bulge = P.get('bulge', 0.012) * smooth((hw - (self.hw0 - 0.12)) / 0.12)
        crown = P.get('crown', 0.03) * (0.6 + 0.4 * g)
        zmid = lerp(zb, zbl, 0.5)
        pts = [(0.0, zb), (hw - 0.05, zb), (hw, zb + 0.05), (hw + bulge, zmid), (hw, zbl - 0.015), (hw - 0.012, zbl),
               (lerp(hw - 0.045, hr + 0.012, g), zt - lerp(0.006, 0.03, g)), (lerp(hw - 0.085, hr - 0.025, g), zt), (0.0, zt + crown)]
        return pts, g

    def stations(self):
        P = self.P; L = self.L
        us = set()
        n = int(L / 0.05)
        for i in range(n + 1): us.add(round(i * L / n, 4))
        for u, z in self.top:
            for d in (-0.02, -0.008, 0.0, 0.008, 0.02):
                if 0 < u + d < L: us.add(round(u + d, 4))
        # corners need more slices
        for i in range(1, 8):
            t = i / 8
            us.add(round(P['cornerF'] * (1 - math.cos(t * math.pi / 2)), 4)); us.add(round(L - P['cornerR'] * (1 - math.cos(t * math.pi / 2)), 4))
        return sorted(u for u in us if 0 <= u <= L)

    def body(self, M):
        P = self.P; L = self.L
        us = self.stations(); rings = []; gs = []
        k = Kit(); bm = k.bm
        for u in us:
            pts, g = self.section(u)
            x = L / 2 - u
            right = [Vector((x, -y, z)) for (y, z) in pts]
            left = [Vector((x, y, z)) for (y, z) in pts]
            # one ring: centre bottom -> right side up -> centre top -> left side down (shared centre points)
            ring_pts = right + list(reversed(left[1:-1]))
            rings.append([bm.verts.new(p) for p in ring_pts]); gs.append(g)
        npts = len(rings[0]); ns = 9
        # faces between rings: index j around the ring, material by region
        uws0, uws1, urw1, urw0 = P['ws'][0], P['ws'][1], P['rw'][0], P['rw'][1]
        pillars = P.get('pillars', [])
        def region(j, umid, gmid):
            jj = j if j < ns - 1 else npts - 1 - j  # the left side maps back to the matching right-side face (0..7)
            if jj == 5 and gmid > 0.7 and uws0 + 0.04 < umid < urw0 - 0.03:
                if not any(a <= umid <= b for a, b in pillars): return 'glass'
            if jj == 7:
                if uws0 + 0.035 < umid < uws1 - 0.035: return 'glass'
                if urw1 + 0.035 < umid < urw0 - 0.035: return 'glass'
            return 'paint'
        faces_by = {'glass': [], 'paint': []}
        for i in range(len(rings) - 1):
            a, b = rings[i], rings[i + 1]
            umid = (us[i] + us[i + 1]) / 2; gmid = (gs[i] + gs[i + 1]) / 2
            for j in range(npts):
                j2 = (j + 1) % npts
                q = [a[j], a[j2], b[j2], b[j]]
                if len(set(q)) < 4: continue
                try: f = bm.faces.new(q)
                except ValueError: continue
                r = region(j, umid, gmid); f.material_index = k.mi(M[r]); faces_by[r].append(f)
        # end caps
        for ring, flip in ((rings[0], False), (rings[-1], True)):
            try:
                f = bm.faces.new(list(reversed(ring)) if flip else ring); f.material_index = k.mi(M['paint'])
            except ValueError: pass
        bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=1e-5)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        # recess the glass: inset with a black rubber surround
        gl = [f for f in bm.faces if f.material_index == k.mi(M['glass'])]
        if gl:
            r = bmesh.ops.inset_region(bm, faces=gl, thickness=0.012, depth=-0.008, use_even_offset=True)
            for f in r['faces']: f.material_index = k.mi(M['trim'])
        ob = k.build('body', smooth_angle=None)
        self.body_ob = ob
        return ob

def cut_arches(car, ob, M):
    P = car.P; L = car.L
    for u in (car.uF, car.uR):
        x = L / 2 - u; ra = P['rad'] + P.get('arch_gap', 0.035)
        bpy.ops.mesh.primitive_cylinder_add(vertices=40, radius=ra, depth=P['W'] + 0.4, location=(x, 0, P['rad']), rotation=(math.pi / 2, 0, 0))
        cyl = bpy.context.active_object
        mod = ob.modifiers.new('arch', 'BOOLEAN'); mod.operation = 'DIFFERENCE'; mod.object = cyl; mod.solver = 'EXACT'
        bpy.context.view_layer.objects.active = ob
        with bpy.context.temp_override(object=ob, active_object=ob):
            bpy.ops.object.modifier_apply(modifier=mod.name)
        bpy.data.objects.remove(cyl)

def wheel_wells(car, M):
    """Dark liners inside the arches so you can't see into the body."""
    P = car.P; L = car.L; k = Kit()
    for u in (car.uF, car.uR):
        x = L / 2 - u; ra = P['rad'] + P.get('arch_gap', 0.035) - 0.005
        for side in (-1, 1):
            y0, y1 = side * (car.hw0 - 0.28), side * (car.hw0 + 0.02)
            seg = 20
            for i in range(seg):
                a0 = math.radians(-10 + 200 * i / seg); a1 = math.radians(-10 + 200 * (i + 1) / seg)
                p00 = (x + ra * math.cos(a0), y0, P['rad'] + ra * math.sin(a0)); p01 = (x + ra * math.cos(a1), y0, P['rad'] + ra * math.sin(a1))
                p10 = (x + ra * math.cos(a0), y1, P['rad'] + ra * math.sin(a0)); p11 = (x + ra * math.cos(a1), y1, P['rad'] + ra * math.sin(a1))
                k.face([p00, p01, p11, p10], M['under'], flip=side > 0)
            # inner wall
            pts = [(x + ra * math.cos(math.radians(-10 + 200 * i / seg)), y0, P['rad'] + ra * math.sin(math.radians(-10 + 200 * i / seg))) for i in range(seg + 1)]
            k.face(pts, M['under'], flip=side < 0)
    return k.build('wells')

# ------------------------------------------------------------------ wheels
def wheel(P, M, name):
    """Tyre and rim, centred on the origin, axle along y, outer face towards +y."""
    r = P['rad']; w = P.get('tyre_w', 0.185); rr = r * P.get('rim_frac', 0.62)
    k = Kit(); seg = 28
    prof = [(rr, -w / 2 + 0.01), (rr + 0.015, -w / 2), (r - 0.035, -w / 2 - 0.004), (r - 0.008, -w / 2 + 0.012), (r, -w / 2 + 0.03),
            (r, w / 2 - 0.03), (r - 0.008, w / 2 - 0.012), (r - 0.035, w / 2 + 0.004), (rr + 0.015, w / 2), (rr, w / 2 - 0.01)]
    rings = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        rings.append([k.bm.verts.new((rad * math.cos(a), y, rad * math.sin(a))) for rad, y in prof])
    ti = k.mi(M['tyre'])
    for i in range(seg):
        A, B = rings[i], rings[(i + 1) % seg]
        for j in range(len(prof) - 1):
            f = k.bm.faces.new([A[j], A[j + 1], B[j + 1], B[j]]); f.material_index = ti
    # rim face (outer, +y) with spokes or a steel disc
    style = P.get('wheel', 'steel')
    ri = k.mi(M['rim']); yf = w / 2 - 0.02
    hub = rr * 0.28
    if style in ('steel', 'hubcap'):
        # dished disc with a ring of holes look (hubcap: flatter, plastic silver)
        n = 24
        outer = [k.bm.verts.new((rr * math.cos(2 * math.pi * i / n), yf, rr * math.sin(2 * math.pi * i / n))) for i in range(n)]
        mid = [k.bm.verts.new((rr * 0.7 * math.cos(2 * math.pi * i / n), yf + 0.025, rr * 0.7 * math.sin(2 * math.pi * i / n))) for i in range(n)]
        inner = [k.bm.verts.new((hub * math.cos(2 * math.pi * i / n), yf + 0.04, hub * math.sin(2 * math.pi * i / n))) for i in range(n)]
        for i in range(n):
            i2 = (i + 1) % n
            f = k.bm.faces.new([outer[i], outer[i2], mid[i2], mid[i]]); f.material_index = ri
            f = k.bm.faces.new([mid[i], mid[i2], inner[i2], inner[i]]); f.material_index = ri
        f = k.bm.faces.new(inner); f.material_index = ri
        if style == 'hubcap':
            for i in range(10):
                a = 2 * math.pi * i / 10; c = (rr * 0.62 * math.cos(a), yf + 0.022, rr * 0.62 * math.sin(a))
                R = Matrix.Rotation(-a, 3, 'Y')
                hx, hy, hz = rr * 0.2, 0.006, 0.02
                Pp = [Vector(c) + R @ Vector((sx * hx, sy * hy, sz * hz)) for sx in (-1, 1) for sy in (-1, 1) for sz in (-1, 1)]
                vs = [k.bm.verts.new(p) for p in Pp]
                for q in [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]:
                    f = k.bm.faces.new([vs[j] for j in q]); f.material_index = k.mi(M['under'])
        if style == 'steel':
            for i in range(6):
                a = 2 * math.pi * i / 6; c = (rr * 0.5 * math.cos(a), yf + 0.03, rr * 0.5 * math.sin(a))
                k.box(c, (0.035, 0.012, 0.035), M['trim'])
    else:
        nsp = {'alloy5': 5, 'multi': 10, 'alloy_dark': 5, 'cross': 4}.get(style, 5)
        n = 30
        ring_o = [k.bm.verts.new((rr * math.cos(2 * math.pi * i / n), yf, rr * math.sin(2 * math.pi * i / n))) for i in range(n)]
        ring_i = [k.bm.verts.new((rr * 0.86 * math.cos(2 * math.pi * i / n), yf + 0.008, rr * 0.86 * math.sin(2 * math.pi * i / n))) for i in range(n)]
        for i in range(n):
            i2 = (i + 1) % n
            f = k.bm.faces.new([ring_o[i], ring_o[i2], ring_i[i2], ring_i[i]]); f.material_index = ri
        # dark barrel behind the spokes
        back = [k.bm.verts.new((rr * 0.86 * math.cos(2 * math.pi * i / n), yf - 0.07, rr * 0.86 * math.sin(2 * math.pi * i / n))) for i in range(n)]
        di = k.mi(M['under'])
        f = k.bm.faces.new(back); f.material_index = di
        for i in range(nsp):
            a = 2 * math.pi * i / nsp
            wsp = 0.05 if nsp <= 5 else 0.026
            length = rr * 0.86 - hub * 0.6
            c = ((hub * 0.6 + length / 2) * math.cos(a), yf + 0.012, (hub * 0.6 + length / 2) * math.sin(a))
            R = Matrix.Rotation(-a, 3, 'Y')
            hx, hy, hz = length / 2, 0.012, wsp / 2
            Pp = [Vector(c) + R @ Vector((sx * hx, sy * hy, sz * hz)) for sx in (-1, 1) for sy in (-1, 1) for sz in (-1, 1)]
            vs = [k.bm.verts.new(p) for p in Pp]
            for q in [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]:
                f = k.bm.faces.new([vs[j] for j in q]); f.material_index = ri
        hubv = [k.bm.verts.new((hub * math.cos(2 * math.pi * i / 12), yf + 0.02, hub * math.sin(2 * math.pi * i / 12))) for i in range(12)]
        f = k.bm.faces.new(hubv); f.material_index = ri
    ob = k.build(name, smooth_angle=40)
    return ob

# ------------------------------------------------------------------ details
def ribbon_on_side(car, k, u, z0, z1, m, w=0.006):
    """A thin dark line on both body sides at u, from z0 up to z1 (panel gaps)."""
    L = car.L; x = L / 2 - u
    for side in (-1, 1):
        pts, g = car.section(u)
        hw = car.half(u)
        # follow the side between the rocker and the shoulder
        prof = [(y, z) for (y, z) in pts[2:6] if z0 - 0.01 <= z <= z1 + 0.01] or [(hw, z0), (hw, z1)]
        prof = [(hw + 0.002, z0)] + [(y + 0.003, z) for y, z in prof] + [(hw - 0.01, z1)]
        for (y0, za), (y1, zb) in zip(prof, prof[1:]):
            k.face([(x - w / 2, side * y0, za), (x + w / 2, side * y0, za), (x + w / 2, side * y1, zb), (x - w / 2, side * y1, zb)], m, flip=side < 0)

def lamps_and_grille(car, M, k):
    P = car.P; L = car.L
    xf = L / 2 + 0.004; xr = -L / 2 - 0.004
    flat = car.hw0 - P['cornerF']
    zf_top = car.ztop(0.0)
    hl = P.get('head', dict(w=0.32, h=0.13, z=zf_top - 0.1, kind='rect'))
    gap = hl.get('inner', 0.28)  # half-width of grille between the lamps
    for s in (-1, 1):
        yc = s * (gap + hl['w'] / 2)
        if abs(yc) + hl['w'] / 2 > flat + 0.06: yc = s * (flat + 0.06 - hl['w'] / 2)
        if hl.get('kind') == 'twin_round':
            for d in (-1, 1):
                r = hl['h'] / 2
                cy = yc + d * hl['w'] / 4
                pts = [(xf + 0.006, cy + r * math.cos(2 * math.pi * i / 16), hl['z'] + r * math.sin(2 * math.pi * i / 16)) for i in range(16)]
                k.face(pts, M['lens'])
        else:
            k.box((xf - 0.012, yc, hl['z']), (0.035, hl['w'], hl['h']), M['lens'])
            k.box((xf - 0.016, yc, hl['z']), (0.034, hl['w'] + 0.02, hl['h'] + 0.02), M['trim'])
        # indicator
        ind = P.get('ind', 'outer')
        if ind == 'outer':
            k.box((xf - 0.014, s * (abs(yc) + hl['w'] / 2 + 0.035), hl['z']), (0.03, 0.06, hl['h'] * 0.85), M['amber'])
        elif ind == 'bumper':
            k.box((xf + P.get('bumpF_out', 0.04) + 0.01, s * (flat - 0.1), P['bumpF_top'] - 0.07), (0.03, 0.14, 0.05), M['amber'])
    # grille between the lamps
    gh = P.get('grille', dict(h=0.1, z=hl['z'], slats=4, chrome=False))
    gw = gap * 2 - 0.02
    k.box((xf - 0.02, 0, gh['z']), (0.03, gw, gh['h']), M['under'])
    for i in range(gh['slats']):
        z = gh['z'] - gh['h'] / 2 + (i + 0.5) * gh['h'] / gh['slats']
        k.box((xf - 0.004, 0, z), (0.012, gw, gh['h'] / gh['slats'] * 0.42), M['chrome'] if gh.get('chrome') else M['trim'])
    # tail lamps on the rear face
    tl = P.get('tail', dict(w=0.36, h=0.16, z=car.ztop(L) - 0.12))
    flatR = car.hw0 - P['cornerR']
    for s in (-1, 1):
        yc = s * (flatR + 0.05 - tl['w'] / 2)
        k.box((xr + 0.012, yc, tl['z']), (0.035, tl['w'], tl['h']), M['red'])
        k.box((xr + 0.006, yc - s * (tl['w'] / 2 - 0.05), tl['z'] - tl['h'] * 0.22), (0.035, 0.085, tl['h'] * 0.45), M['amber'])
        k.box((xr + 0.004, yc + s * 0.0 - s * (tl['w'] / 2 - 0.13), tl['z'] - tl['h'] * 0.22), (0.035, 0.07, tl['h'] * 0.45), M['lens'])

def bumper(car, M, k, front):
    """A bumper that wraps round the corners: a swept rounded section along the plan outline."""
    P = car.P; L = car.L
    style = P.get('bumper', 'body')
    m = M['chrome'] if style == 'chrome' else M['trim'] if style == 'black' else M['paint']
    z0 = P['bumpF_bot'] if front else P['bumpR_bot']; z1 = P['bumpF_top'] if front else P['bumpR_top']
    if style == 'chrome': z0, z1 = z1 - 0.1, z1
    out = P.get('bumpF_out' if front else 'bumpR_out', 0.045)
    rc = P['cornerF'] if front else P['cornerR']
    wrap = P.get('wrap', 0.32)
    # path in plan (u, y) round the end, then sample the body half-width there
    path = []
    us = [rc + wrap * (1 - i / 6) for i in range(7)] + [rc * (1 - math.sin(t * math.pi / 2 / 8 * 8 / 8)) for t in []]
    pts = []
    for i in range(7):  # along the left side towards the corner
        u = rc + wrap * (1 - i / 6); pts.append((u, car.half(u if front else L - u) + 0.0))
    for i in range(1, 9):  # round the corner
        a = math.pi / 2 * i / 8; u = rc - rc * math.sin(a); y = (car.hw0 - rc) + rc * math.cos(a); pts.append((u, y))
    left = [(u, y) for u, y in pts]
    full = left + [(u, -y) for u, y in reversed(left)]
    sec = [(0.0, z0), (out * 0.8, z0 + 0.01), (out, z0 + 0.03), (out, z1 - 0.03), (out * 0.8, z1), (-0.05, z1), (-0.05, z0)]
    rings = []
    for idx, (u, y) in enumerate(full):
        un, yn = full[min(idx + 1, len(full) - 1)]; up, yp = full[max(idx - 1, 0)]
        tu, ty = un - up, yn - yp; l = math.hypot(tu, ty) or 1
        nu, ny = -ty / l, tu / l  # outward normal in plan (u grows inward)
        if front: nu, ny = -nu, ny
        x = (L / 2 - u) if front else (-L / 2 + u)
        nx = -nu if front else nu
        # make sure the normal points away from the car
        if nx * (1 if front else -1) < 0 and abs(nx) > 0.5: nx, ny = -nx, -ny
        if y * ny < 0 and abs(ny) > 0.5: nx, ny = -nx, -ny
        rings.append([k.bm.verts.new((x + nx * o, y + ny * o, z)) for o, z in sec])
    mi = k.mi(m)
    for a, b in zip(rings, rings[1:]):
        for j in range(len(sec)):
            j2 = (j + 1) % len(sec)
            f = k.bm.faces.new([a[j], a[j2], b[j2], b[j]]); f.material_index = mi
    for ring in (rings[0], rings[-1]):
        f = k.bm.faces.new(ring); f.material_index = mi
    if style == 'chrome':  # black over-riders
        for s in (-1, 1):
            x = (L / 2 + out + 0.02) if front else (-L / 2 - out - 0.02)
            k.box((x, s * (car.hw0 - rc - 0.12), z0 - 0.0), (0.06, 0.07, 0.16), M['trim'])
    # number plate
    pz = (z0 + z1) / 2 if style != 'chrome' else z0 - 0.09
    xpl = (L / 2 + out + 0.006) if front else (-L / 2 - out - 0.006)
    if not front and P.get('plate_on_tail'):
        xpl = -L / 2 - 0.006; pz = P['plate_on_tail']
    pw, ph = 0.52, 0.11
    f = k.face([(xpl, -pw / 2 if front else pw / 2, pz - ph / 2), (xpl, pw / 2 if front else -pw / 2, pz - ph / 2), (xpl, pw / 2 if front else -pw / 2, pz + ph / 2), (xpl, -pw / 2 if front else pw / 2, pz + ph / 2)], M['plate'])

def details(car, M):
    P = car.P; L = car.L; k = Kit()
    lamps_and_grille(car, M, k)
    bumper(car, M, k, True); bumper(car, M, k, False)
    # panel gaps: doors, bonnet and boot lines
    for d in P.get('door_lines', []):
        u, z0 = (d, P['sill'] + 0.03) if not isinstance(d, (list, tuple)) else d
        ribbon_on_side(car, k, u, z0, car.zbelt(u) - 0.005, M['trim'])
    # mirrors on the doors by the windscreen base
    uws0 = P['ws'][0]
    for s in (-1, 1):
        u = uws0 + 0.14; x = L / 2 - u; hw = car.half(u); z = car.zbelt(u) + 0.07
        k.box((x, s * (hw + 0.09), z), (0.09, 0.15, 0.1), M['trim'])
        k.box((x - 0.01, s * (hw + 0.03), z - 0.03), (0.04, 0.06, 0.03), M['trim'])
        # door handles
        for uh in P.get('handles', []):
            xh = L / 2 - uh; hwh = car.half(uh)
            k.box((xh, s * (hwh + 0.006), car.zbelt(uh) - 0.07), (0.11, 0.02, 0.025), M['trim'] if P.get('bumper') != 'chrome' else M['chrome'])
    # rubber strip along the bottom of the windows
    for s in (-1, 1):
        u0, u1 = uws0 + 0.05, P['rw'][1] - 0.05
        n = 12
        for i in range(n):
            ua, ub = lerp(u0, u1, i / n), lerp(u0, u1, (i + 1) / n)
            pa, _ = car.section(ua); pb, _ = car.section(ub)
            ya, za = pa[5]; yb, zb_ = pb[5]
            xa, xb = L / 2 - ua, L / 2 - ub
            k.face([(xa, s * (ya + 0.004), za - 0.012), (xb, s * (yb + 0.004), zb_ - 0.012), (xb, s * (yb - 0.004), zb_ + 0.004), (xa, s * (ya - 0.004), za + 0.004)], M['trim'], flip=s < 0)
    # rubbing strip along the doors
    ms = P.get('moulding')
    if ms:
        zc, hgt = ms
        u0, u1 = car.uF + P['rad'] + 0.08, car.uR - P['rad'] - 0.06
        for s in (-1, 1):
            n = 10
            for i in range(n):
                ua, ub = lerp(u0, u1, i / n), lerp(u0, u1, (i + 1) / n)
                ya, yb = car.half(ua) + 0.016, car.half(ub) + 0.016
                pa, _ = car.section(ua); pb, _ = car.section(ub)
                ya = max(ya, pa[3][0] + 0.01); yb = max(yb, pb[3][0] + 0.01)
                xa, xb = L / 2 - ua, L / 2 - ub
                k.face([(xa, s * ya, zc - hgt / 2), (xb, s * yb, zc - hgt / 2), (xb, s * yb, zc + hgt / 2), (xa, s * ya, zc + hgt / 2)], M['trim'], flip=s < 0)
    # wipers
    for s in (-0.25, 0.15):
        u = uws0 + 0.06; x = L / 2 - u
        k.box((x, s, car.ztop(u) + 0.02), (0.02, 0.42, 0.012), M['trim'], rot=0.25)
    # exhaust
    k.box((-L / 2 + 0.05, -car.hw0 * 0.55, P['sill'] + 0.02), (0.18, 0.05, 0.05), M['chrome'] if P.get('rally') else M['under'])
    # extras by style
    if P.get('roof_rails'):
        for s in (-1, 1):
            u0, u1 = P['ws'][1] + 0.05, P['rw'][0] - 0.03
            x0, x1 = L / 2 - u0, L / 2 - u1
            zr = car.ztop((u0 + u1) / 2) + 0.05
            hwr = car.hw0 - P['tumble'] - 0.03
            k.box(((x0 + x1) / 2, s * hwr, zr), (abs(x0 - x1), 0.03, 0.025), M['trim'])
            for xx in (x0 - 0.03, x1 + 0.03):
                k.box((xx, s * hwr, zr - 0.03), (0.05, 0.035, 0.05), M['trim'])
    if P.get('wing'):
        u = L - 0.15; x = L / 2 - u; z = car.ztop(u) + 0.2
        k.box((x - 0.02, 0, z), (0.26, car.hw0 * 1.85, 0.03), M['paint'])
        for s in (-1, 1):
            k.box((x - 0.02, s * car.hw0 * 0.925, z - 0.03), (0.3, 0.012, 0.12), M['paint'])
            k.box((x + 0.02, s * car.hw0 * 0.6, z - 0.1), (0.07, 0.03, 0.2), M['paint'])
    if P.get('spare'):
        r = P['rad'] + 0.01; x = -L / 2 - 0.13; zc = P['sill'] + 0.55
        seg = 20
        rim = [(x - 0.0, r * math.cos(2 * math.pi * i / seg), zc + r * math.sin(2 * math.pi * i / seg)) for i in range(seg)]
        back = [(x + 0.11, r * math.cos(2 * math.pi * i / seg), zc + r * math.sin(2 * math.pi * i / seg)) for i in range(seg)]
        mi = k.mi(M['tyre'])
        vs_f = [k.bm.verts.new(p) for p in rim]; vs_b = [k.bm.verts.new(p) for p in back]
        for i in range(seg):
            i2 = (i + 1) % seg
            f = k.bm.faces.new([vs_f[i], vs_f[i2], vs_b[i2], vs_b[i]]); f.material_index = mi
        f = k.bm.faces.new(list(reversed(vs_f))); f.material_index = k.mi(M['trim'])
    if P.get('flares'):
        for u in (car.uF, car.uR):
            x = L / 2 - u; ra = P['rad'] + P.get('arch_gap', 0.035) + 0.02
            for s in (-1, 1):
                hw = car.half(u)
                seg = 14
                for i in range(seg):
                    a0 = math.radians(5 + 170 * i / seg); a1 = math.radians(5 + 170 * (i + 1) / seg)
                    p = lambda a, rr, yy: (x + rr * math.cos(a), s * yy, P['rad'] + rr * math.sin(a))
                    k.face([p(a0, ra, hw - 0.02), p(a1, ra, hw - 0.02), p(a1, ra + 0.05, hw + P['flares']), p(a0, ra + 0.05, hw + P['flares'])], M['paint' if P.get('rally') else 'trim'], flip=s > 0)
    return k.build('details', smooth_angle=35)

def interior(car, M):
    P = car.P; L = car.L; k = Kit()
    # the cabin box stays inside the glass: from the windscreen's middle to the rear window's middle, under the roof
    u0 = lerp(P['ws'][0], P['ws'][1], 0.55); u1 = lerp(P['rw'][0], P['rw'][1], 0.45)
    hw = car.hw0 - P['tumble'] - 0.03
    z0 = P['sill'] + 0.12; z1 = min(car.ztop(u0), car.ztop(u1), car.ztop((u0 + u1) / 2)) - 0.05
    x0, x1 = L / 2 - u0, L / 2 - u1
    # a dark cabin you see into through the glass (faces point inwards)
    c = [(x0, -hw, z0), (x0, hw, z0), (x1, hw, z0), (x1, -hw, z0), (x0, -hw, z1), (x0, hw, z1), (x1, hw, z1), (x1, -hw, z1)]
    cv = [k.bm.verts.new(p) for p in c]
    shell = []
    for q in [(0, 1, 2, 3), (4, 7, 6, 5), (0, 4, 5, 1), (2, 6, 7, 3), (1, 5, 6, 2), (0, 3, 7, 4)]:
        f = k.bm.faces.new([cv[i] for i in q]); f.material_index = k.mi(M['inside']); shell.append(f)
    bmesh.ops.recalc_face_normals(k.bm, faces=shell)
    bmesh.ops.reverse_faces(k.bm, faces=shell)  # the cabin walls face inwards: through the glass you see the far side, dark
    belt = car.zbelt((u0 + u1) / 2)
    uB = P.get('seat_u', (u0 + u1) / 2 - 0.1)
    for s in (-1, 1):
        k.box((L / 2 - uB, s * 0.36, belt + 0.05), (0.12, 0.46, 0.55), M['inside'])
        k.box((L / 2 - uB + 0.02, s * 0.36, belt + 0.38), (0.1, 0.24, 0.16), M['inside'])
    k.box((L / 2 - (uB + 0.8), 0, belt + 0.02), (0.12, hw * 1.8, 0.5), M['inside'])
    # dashboard under the windscreen and the wheel (UK: driver on the right)
    xd = L / 2 - (P['ws'][0] + 0.2)
    k.box((xd, 0, belt - 0.05), (0.32, (car.hw0 - 0.08) * 2, 0.16), M['inside'])
    r = 0.18; xc = xd - 0.3; yc = -0.36; zc = belt + 0.08
    seg = 14
    for i in range(seg):
        a0 = 2 * math.pi * i / seg; a1 = 2 * math.pi * (i + 1) / seg
        k.face([(xc + 0.02, yc + r * math.cos(a0), zc + r * math.sin(a0)), (xc + 0.02, yc + r * math.cos(a1), zc + r * math.sin(a1)),
                (xc - 0.02, yc + (r - 0.025) * math.cos(a1), zc + (r - 0.025) * math.sin(a1)), (xc - 0.02, yc + (r - 0.025) * math.cos(a0), zc + (r - 0.025) * math.sin(a0))], M['trim'])
    return k.build('interior', recalc=False)

def underside(car, M):
    P = car.P; L = car.L; k = Kit()
    hw = car.hw0 - 0.05; z = P['sill'] + 0.005
    k.face([(L / 2 - 0.2, -hw, z), (L / 2 - 0.2, hw, z), (-L / 2 + 0.2, hw, z), (-L / 2 + 0.2, -hw, z)], M['under'], flip=True)
    return k.build('under')

# ------------------------------------------------------------------ assemble
def build(P, paint=(0.6, 0.05, 0.05)):
    for o in list(bpy.data.objects): bpy.data.objects.remove(o)
    MATS.clear()
    for m in list(bpy.data.materials): bpy.data.materials.remove(m)
    M = materials(paint)
    car = Car(P)
    body = car.body(M)
    cut_arches(car, body, M)
    for p in body.data.polygons: p.use_smooth = True
    set_auto_smooth(body, 32)
    wells = wheel_wells(car, M)
    det = details(car, M)
    inner = interior(car, M)
    und = underside(car, M)
    wheels = []
    for name, u, s in (('wheel_FL', car.uF, 1), ('wheel_FR', car.uF, -1), ('wheel_RL', car.uR, 1), ('wheel_RR', car.uR, -1)):
        w = wheel(P, M, name)
        w.location = (car.L / 2 - u, s * (car.hw0 - P.get('tyre_w', 0.185) / 2 + 0.01 + P.get('track_out', 0.0)), P['rad'])
        if s < 0: w.rotation_euler = (0, 0, math.pi)
        wheels.append(w)
    return car, dict(body=body, wells=wells, details=det, interior=inner, under=und, wheels=wheels)

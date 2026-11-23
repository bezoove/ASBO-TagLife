# The UK box ambulance, rebuilt properly: a smooth modern van cab (lofted from real cross-sections: rounded nose,
# sloping bonnet, steeply raked curved windscreen in a rubber surround, swept headlamps, black bumper and grille, big
# mirrors on arms), with a tall rounded box body behind it (radiused edges, a peak over the cab carrying the light bar
# and the mirror-written AMBULANCE), fluorescent yellow with two rows of green Battenburg the full length, a chequer
# band along the top, AMBULANCE on the sides, a sliding side door with a frosted window, lockers, grab rails, scene
# lamps, blue LEDs, tall tail lamps, chevron rear doors opening on a white treatment room. Same parts and materials as
# before (the game drives door_L/R, bar_L/R, beacon_L/R, grille_L/R and the four wheels).
# Run: python3.13 ambforge2.py -- <out folder> [--preview]
import bpy, bmesh, sys, os, math, json
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import carforge
from carforge import Kit, mat
import ambforge as AF
from mathutils import Vector, Matrix
from PIL import Image, ImageDraw, ImageFont, ImageFilter

OUT = sys.argv[sys.argv.index('--') + 1]; PREVIEW = '--preview' in sys.argv
os.makedirs(OUT, exist_ok=True)
L, HW_CAB, HW_BOX, ROOF = 6.9, 1.0, 1.06, 2.96
UF, UR, RAD = 0.95, 4.985, 0.36          # wheel centres from the front, wheel radius
BOX_U0 = 1.82                             # where the box starts (from the front)
FONT = '/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf'
YEL = (0.86, 0.93, 0.06)
def X(u): return L / 2 - u
def lerp(a, b, t): return a + (b - a) * t
def sm(t): t = min(max(t, 0.0), 1.0); return t * t * (3 - 2 * t)
def interp(pts, u):
    """Smooth (Catmull-Rom) through the points."""
    if u <= pts[0][0]: return pts[0][1]
    if u >= pts[-1][0]: return pts[-1][1]
    for i in range(len(pts) - 1):
        if u <= pts[i + 1][0]:
            p0 = pts[max(0, i - 1)][1]; p1 = pts[i][1]; p2 = pts[i + 1][1]; p3 = pts[min(len(pts) - 1, i + 2)][1]
            t = (u - pts[i][0]) / (pts[i + 1][0] - pts[i][0])
            return 0.5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3)
    return pts[-1][1]

# ------------------------------------------------------------------ the cab surface
TOP = [(0.0, 0.9), (0.04, 0.99), (0.12, 1.06), (0.35, 1.14), (0.75, 1.23), (0.92, 1.3), (1.12, 1.6), (1.35, 1.92), (1.58, 2.16), (1.72, 2.27), (1.86, 2.33), (2.05, 2.35)]
def half_w(u): return lerp(0.8, HW_CAB, sm(u / 0.36)) - 0.012 * sm((u - 0.9) / 0.8)
def top_r(u): return lerp(0.13, 0.24, sm((u - 0.85) / 0.9))
def tumble(u): return lerp(0.03, 0.12, sm((u - 0.9) / 0.8))
ZB = 0.4
def section(u, s):
    """Point (y >= 0, z) on the cab's cross-section at u. s: 0 = bottom centre, 1 = top centre, round the side."""
    w = half_w(u); zt = interp(TOP, u); rr = min(top_r(u), (zt - ZB) * 0.4); rb = 0.06; tb = tumble(u)
    # lengths of the five runs: bottom, bottom corner, side, top corner, top
    side_h = (zt - rr) - (ZB + rb)
    runs = [w - rb, math.pi / 2 * rb, math.hypot(side_h, tb), math.pi / 2 * rr, max(0.0, w - tb - rr)]
    d = s * sum(runs)
    if d <= runs[0]: return (d, ZB)
    d -= runs[0]
    if d <= runs[1]: a = d / rb - math.pi / 2; return (w - rb + math.cos(a) * rb, ZB + rb + math.sin(a) * rb)
    d -= runs[1]
    if d <= runs[2]: t = d / runs[2]; return (w - tb * t * t, ZB + rb + side_h * t)
    d -= runs[2]
    if d <= runs[3]: a = d / rr; return (w - tb - rr + math.cos(a) * rr, zt - rr + math.sin(a) * rr)
    d -= runs[3]
    t = d / max(1e-6, runs[4]); return ((w - tb - rr) * (1 - t), zt + 0.02 * math.sin(t * math.pi / 2))
def S_side(u):
    """Where the side run starts and ends in s (for windows and the battenburg on the cab sides)."""
    w = half_w(u); zt = interp(TOP, u); rr = min(top_r(u), (zt - ZB) * 0.4); rb = 0.06; tb = tumble(u)
    runs = [w - rb, math.pi / 2 * rb, math.hypot((zt - rr) - (ZB + rb), tb), math.pi / 2 * rr, max(0.0, w - tb - rr)]
    T = sum(runs); return (runs[0] + runs[1]) / T, (runs[0] + runs[1] + runs[2]) / T, (runs[0] + runs[1] + runs[2] + runs[3]) / T

US = [0.0, 0.015, 0.04, 0.08, 0.13, 0.2, 0.3, 0.42, 0.56, 0.7, 0.8, 0.88, 0.94] + [0.98 + 0.06 * i for i in range(13)] + [1.8, 1.9, 2.05]
NS = 34
def cab_mesh(M):
    k = Kit(); rows = []
    for u in US:
        row = []
        for side in (1, -1):
            rng = range(NS + 1) if side == 1 else range(NS - 1, 0, -1)
            for j in rng:
                y, z = section(u, j / NS); row.append(k.bm.verts.new((X(u), side * y, z)))
        rows.append(row)
    pi = k.mi(M['paint']); n = len(rows[0])
    for a, b in zip(rows, rows[1:]):
        for j in range(n):
            j2 = (j + 1) % n
            f = k.bm.faces.new([a[j], b[j], b[j2], a[j2]]); f.material_index = pi; f.smooth = True
    # the nose: close the front with a fan
    c = k.bm.verts.new((X(0) + 0.002, 0, (ZB + TOP[0][1]) / 2))
    for j in range(n):
        f = k.bm.faces.new([rows[0][(j + 1) % n], rows[0][j], c]); f.material_index = pi; f.smooth = True
    ob = k.build('cab', recalc=True); return ob

def patch(k, m, u0, u1, s0f, s1f, off=0.006, nu=10, ns=10):
    """A piece of surface lying just proud of the cab (glass, a black surround): u from u0 to u1, s from s0f(u) to s1f(u) on both sides."""
    rows = []
    for i in range(nu + 1):
        u = lerp(u0, u1, i / nu); a, b = s0f(u), s1f(u); row = []
        for side in (1, -1):
            js = range(ns + 1) if side == 1 else range(ns - 1, -1, -1)
            for j in js:
                s = lerp(a, b, j / ns); y, z = section(u, s)
                # offset along the section's outward normal (approximate with the neighbour points)
                y2, z2 = section(u, min(1.0, s + 0.003)); ty, tz = y2 - y, z2 - z; ln = math.hypot(ty, tz) or 1.0; ny, nz = tz / ln, -ty / ln
                row.append(k.bm.verts.new((X(u), side * (y + ny * off), z + nz * off)))
        rows.append(row)
    mi = k.mi(m)
    for r0, r1 in zip(rows, rows[1:]):
        for j in range(len(r0) - 1):
            f = k.bm.faces.new([r0[j], r0[j + 1], r1[j + 1], r1[j]]); f.material_index = mi; f.smooth = True
    return rows

def cab_glass(M):
    k = Kit()
    def s_top(u): return S_side(u)[2] - 0.004       # where the roof corner ends on top
    # the windscreen: between the A-pillars, from the scuttle to the roof edge, in a black rubber band
    def s_wrap(u, f): a, b = S_side(u)[1], S_side(u)[2]; return a + (b - a) * f
    patch(k, M['trim'], 0.9, 1.74, lambda u: s_wrap(u, 0.45), lambda u: 1.0, 0.008, 28, 12)
    patch(k, M['glass'], 0.94, 1.7, lambda u: s_wrap(u, 0.62), lambda u: 1.0, 0.014, 28, 12)
    # wipers, parked along the bottom of the screen
    for y0 in (0.55, -0.05):
        p0 = Vector((X(0.97), y0, interp(TOP, 0.97) + 0.04)); p1 = Vector((X(0.95), y0 - 0.6, interp(TOP, 0.95) + 0.04))
        k.box(tuple((p0 + p1) / 2), (0.02, 0.6, 0.012), M['trim'])
    # side windows: raked front edge (quarter-light ahead of the divider), rounded corners; black frames
    for s in (1, -1):
        def yat(u, z):
            w = half_w(u); zt = interp(TOP, u); rr = min(top_r(u), (zt - ZB) * 0.4); tb = tumble(u)
            t = (z - (ZB + 0.06)) / ((zt - rr) - (ZB + 0.06)); return w - tb * t * t
        win = [(1.0, 1.42), (1.82, 1.42), (1.82, 2.16), (1.36, 2.16)]
        pts = AF_rounded(win, 0.07)
        k.face([(X(u), s * (yat(u, z) + 0.007), z) for (u, z) in pts], M['glass'], flip=s < 0)
        frame = AF_rounded([(0.97, 1.39), (1.85, 1.39), (1.85, 2.19), (1.33, 2.19)], 0.09)
        k.face([(X(u), s * (yat(u, z) + 0.004), z) for (u, z) in frame], M['trim'], flip=s < 0)
        k.box((X(1.27), s * (yat(1.27, 1.78) + 0.011), 1.78), (0.025, 0.008, 0.74), M['trim'])        # quarter-light divider
        # door shut lines and handle
        for (u, z0, z1) in ((0.98, 0.45, 1.36), (1.86, 0.45, 2.2)):
            k.box((X(u), s * (yat(u, 1.0) + 0.002), (z0 + z1) / 2), (0.008, 0.006, z1 - z0), M['trim'])
        k.box((X(1.7), s * (yat(1.7, 1.3) + 0.012), 1.3), (0.15, 0.02, 0.035), M['trim'])
        # the big van mirror on its arm
        x = X(0.98); yb = yat(0.98, 1.5)
        k.box((x, s * (yb + 0.13), 1.62), (0.04, 0.24, 0.03), M['trim']); k.box((x, s * (yb + 0.08), 1.36), (0.03, 0.14, 0.03), M['trim'])
        k.box((x - 0.03, s * (yb + 0.27), 1.62), (0.09, 0.2, 0.36), M['trim']); k.box((x - 0.078, s * (yb + 0.27), 1.62), (0.005, 0.17, 0.32), M['chrome'])
        k.box((x - 0.03, s * (yb + 0.27), 1.36), (0.08, 0.16, 0.12), M['trim'])
        # amber side repeater on the wing
        k.box((X(0.45), s * (half_w(0.45) - 0.01), 0.95), (0.08, 0.02, 0.03), M['amber'])
    return k.build('cab_glass', recalc=False)

def AF_rounded(pts, r, n=4):
    out = []
    for i in range(len(pts)):
        p0, p1, p2 = Vector(pts[i - 1]), Vector(pts[i]), Vector(pts[(i + 1) % len(pts)])
        a = (p0 - p1).normalized(); b = (p2 - p1).normalized(); rr = min(r, (p0 - p1).length * 0.45, (p2 - p1).length * 0.45)
        s0, s1 = p1 + a * rr, p1 + b * rr
        for j in range(n + 1): t = j / n; q = s0 * (1 - t) ** 2 + p1 * 2 * t * (1 - t) + s1 * t * t; out.append((q.x, q.y))
    return out

def front_end(M):
    """Bumper, grille, headlamps, fog lamps, plate, step and the lower cab."""
    k = Kit(); xf = X(0)
    # the bumper: deep, black, wrapping round the corners, with a lower intake and fog lamps
    for i in range(9):
        a0 = -math.pi / 2 + math.pi * i / 8 * 0.5; a1 = -math.pi / 2 + math.pi * (i + 1) / 8 * 0.5
    def bump(z0, z1, inset, mm):
        pts = []
        for i in range(13):
            t = i / 12; y = lerp(-1.0, 1.0, t) * (HW_CAB + 0.02); xx = xf + 0.06 - inset - 0.18 * (abs(y) / (HW_CAB + 0.02)) ** 4
            pts.append((xx, y))
        for a, b in zip(pts, pts[1:]):
            k.face([(a[0], a[1], z0), (b[0], b[1], z0), (b[0], b[1], z1), (a[0], a[1], z1)], mm, flip=True)
        k.face([(p[0], p[1], z1) for p in pts] + [(pts[-1][0] - 0.25, pts[-1][1], z1), (pts[0][0] - 0.25, pts[0][1], z1)], mm, flip=True)
        k.face([(p[0], p[1], z0) for p in pts] + [(pts[-1][0] - 0.25, pts[-1][1], z0), (pts[0][0] - 0.25, pts[0][1], z0)], mm, flip=False)
    bump(0.28, 0.62, 0.0, M['trim'])
    k.box((xf + 0.065, 0, 0.36), (0.01, 1.1, 0.12), M['under'])                                      # lower intake
    for s in (1, -1):
        k.box((xf + 0.062, s * 0.72, 0.4), (0.012, 0.16, 0.09), M['chrome']); k.box((xf + 0.068, s * 0.72, 0.4), (0.004, 0.13, 0.07), M['lens'])   # fog lamps
    k.box((xf + 0.066, 0, 0.5), (0.008, 0.52, 0.11), M['plate'])                                     # number plate
    # the grille: black, slatted, in a gloss surround, between the lamps
    k.box((xf + 0.03, 0, 0.78), (0.03, 1.0, 0.24), M['trim'])
    for z in (0.69, 0.74, 0.79, 0.84, 0.88): k.box((xf + 0.046, 0, z), (0.008, 0.94, 0.018), M['under'])
    k.box((xf + 0.048, 0, 0.91), (0.01, 1.04, 0.025), M['chrome'])
    # headlamps: swept back and up into the wings, a clear lens over a chrome bowl, indicator at the outer end
    for s in (1, -1):
        pts = []
        for i in range(9):
            t = i / 8; y = s * lerp(0.52, HW_CAB - 0.04, t); u = lerp(0.0, 0.34, t * t); z0 = lerp(0.74, 0.92, t); z1 = lerp(0.95, 1.06, t)
            pts.append((X(u) + 0.012, y, z0, z1))
        for a, b in zip(pts, pts[1:]):
            k.face([(a[0], a[1], a[2]), (b[0], b[1], b[2]), (b[0], b[1], b[3]), (a[0], a[1], a[3])], M['glass'], flip=s > 0)
            k.face([(a[0] - 0.015, a[1], a[2] + 0.01), (b[0] - 0.015, b[1], b[2] + 0.01), (b[0] - 0.015, b[1], b[3] - 0.01), (a[0] - 0.015, a[1], a[3] - 0.01)], M['chrome'], flip=s > 0)
        for t in (0.25, 0.6):   # projector lenses in the bowls
            y = s * lerp(0.52, HW_CAB - 0.04, t); u = lerp(0.0, 0.34, t * t); z = (lerp(0.74, 0.92, t) + lerp(0.95, 1.06, t)) / 2
            k.box((X(u) - 0.004, y, z), (0.02, 0.07, 0.07), M['lens'])
        k.box((X(0.3) + 0.01, s * (HW_CAB - 0.06), 0.98), (0.08, 0.05, 0.06), M['amber'])
        k.box((X(0.02) + 0.01, s * 0.57, 0.82), (0.01, 0.1, 0.06), M['lens'])
    # black lower panels on the cab sides and sills
    for s in (1, -1):
        k.box((X(1.4), s * (HW_CAB + 0.004), 0.47), (0.85, 0.012, 0.14), M['trim'])
        k.box((X(0.6), s * (half_w(0.6) + 0.004), 0.47), (0.3, 0.012, 0.14), M['trim'])
    return k.build('front', recalc=False)

# ------------------------------------------------------------------ the box
BOX_X0, BOX_X1, BOX_Z0, BOX_Z1 = X(L), X(BOX_U0), 0.52, ROOF
def box_body(M):
    """The box, radiused on every edge, with the peak over the cab."""
    tb = bmesh.new(); bmesh.ops.create_cube(tb, size=1.0)
    sx, sy, sz = BOX_X1 - BOX_X0, HW_BOX * 2, BOX_Z1 - BOX_Z0
    for v in tb.verts: v.co = Vector((v.co.x * sx + (BOX_X0 + BOX_X1) / 2, v.co.y * sy, v.co.z * sz + (BOX_Z0 + BOX_Z1) / 2))
    edges = [e for e in tb.edges if not (abs((e.verts[0].co - e.verts[1].co).z) < 1e-4 and min(e.verts[0].co.z, e.verts[1].co.z) < BOX_Z0 + 0.01)]
    bmesh.ops.bevel(tb, geom=edges, offset=0.11, segments=5, affect='EDGES', profile=0.5)
    bot = [e for e in tb.edges if all(v.co.z < BOX_Z0 + 0.001 for v in e.verts)]
    bmesh.ops.bevel(tb, geom=bot, offset=0.03, segments=2, affect='EDGES', profile=0.5)
    # subdivide the big flat sides a little so the arches cut cleanly
    me = bpy.data.meshes.new('box'); tb.to_mesh(me); tb.free()
    ob = bpy.data.objects.new('box', me); bpy.context.scene.collection.objects.link(ob)
    ob.data.materials.append(M['paint'])
    for p in me.polygons: p.use_smooth = True
    # the peak: a rounded wedge reaching forward over the cab roof, its face raked back
    k = Kit(); x0, x1, z0, z1 = BOX_X1 - 0.05, BOX_X1 + 0.28, 2.34, ROOF - 0.04
    for s_ in range(1):
        pass
    prof = [(x1 - 0.12, z0), (x1, z0 + 0.12), (x1 + 0.02, z1 - 0.12), (x1 - 0.06, z1), (x0, z1 + 0.02)]
    rows = []
    for j in range(13):
        y = lerp(-HW_BOX + 0.06, HW_BOX - 0.06, j / 12); rnd = 1 - 0.55 * (abs(y) / HW_BOX) ** 8
        rows.append([k.bm.verts.new((x0 + (px - x0) * rnd, y, pz)) for px, pz in prof] + [k.bm.verts.new((x0, y, z0))])
    pi = k.mi(M['paint'])
    for a, b in zip(rows, rows[1:]):
        for i in range(len(a)):
            i2 = (i + 1) % len(a); f = k.bm.faces.new([a[i], a[i2], b[i2], b[i]]); f.material_index = pi; f.smooth = True
    for r, flip in ((rows[0], False), (rows[-1], True)):
        f = k.bm.faces.new(r[::-1] if flip else r); f.material_index = pi
    peak = k.build('peak', recalc=True)
    return ob, peak

def cut(ob, shape, loc, scale, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, location=loc, rotation=rot) if shape == 'cyl' else bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    c = bpy.context.active_object; c.scale = scale
    md = ob.modifiers.new('cut', 'BOOLEAN'); md.operation = 'DIFFERENCE'; md.object = c; md.solver = 'EXACT'
    bpy.context.view_layer.objects.active = ob
    with bpy.context.temp_override(object=ob, active_object=ob): bpy.ops.object.modifier_apply(modifier=md.name)
    bpy.data.objects.remove(c)

def box_details(M):
    k = Kit(); xr = BOX_X0
    for s in (1, -1):
        y = s * (HW_BOX + 0.004)
        # blue LED pairs at the top corners, amber markers along the top, scene lamps, a rubbing strip
        for u in (BOX_U0 + 0.18, L - 0.16):
            k.box((X(u), y, 2.72), (0.18, 0.012, 0.07), M['blue'])
        for u in (2.6, 3.8, 5.0, 6.2): k.box((X(u), s * (HW_BOX + 0.003), 2.86), (0.06, 0.008, 0.03), M['amber'])
        for u in (2.4, 5.9): k.box((X(u), s * (HW_BOX + 0.008), 2.55), (0.34, 0.016, 0.09), M['lens']); k.box((X(u), s * (HW_BOX + 0.006), 2.55), (0.38, 0.012, 0.13), M['trim'])
        k.box((X(4.3), s * (HW_BOX + 0.006), 0.94), (4.6, 0.012, 0.06), M['trim'])
        if s > 0:   # nearside: the sliding door (outline, frosted window, handle, step, grab rail)
            u0, u1 = 2.05, 3.15
            for (u, z, du, dz) in ((u0, 1.6, 0.012, 2.08), (u1, 1.6, 0.012, 2.08), ((u0 + u1) / 2, 2.63, u1 - u0, 0.012), ((u0 + u1) / 2, 0.57, u1 - u0, 0.012)):
                k.box((X(u), s * (HW_BOX + 0.005), z), (du, 0.008, dz), M['trim'])
            k.face([(X(u), s * (HW_BOX + 0.007), z) for (u, z) in AF_rounded([(2.25, 1.9), (2.95, 1.9), (2.95, 2.42), (2.25, 2.42)], 0.06)], M['white'], flip=False)
            k.box((X(u1 - 0.08), s * (HW_BOX + 0.02), 1.32), (0.04, 0.03, 0.22), M['trim'])
            k.box((X(2.6), s * (HW_BOX + 0.13), 0.44), (1.0, 0.24, 0.04), M['chrome']); k.box((X(2.6), s * (HW_BOX + 0.02), 0.47), (1.0, 0.04, 0.06), M['trim'])
            k.box((X(u0 - 0.12), s * (HW_BOX + 0.04), 1.6), (0.03, 0.03, 0.9), M['chrome'])
        else:       # offside: two equipment lockers with roller shutters
            for (u0, u1) in ((2.1, 3.0), (3.15, 4.05)):
                for (u, z, du, dz) in ((u0, 1.45, 0.012, 1.7), (u1, 1.45, 0.012, 1.7), ((u0 + u1) / 2, 2.3, u1 - u0, 0.012), ((u0 + u1) / 2, 0.6, u1 - u0, 0.012)):
                    k.box((X(u), s * (HW_BOX + 0.005), z), (du, 0.008, dz), M['trim'])
                k.box((X((u0 + u1) / 2), s * (HW_BOX + 0.012), 0.72), (0.16, 0.02, 0.04), M['chrome'])
        # mud flaps behind the wheels, arch trims
        for u in (UF + 0.48, UR + 0.48): k.box((X(u), s * (HW_BOX - 0.1), 0.32), (0.02, 0.22, 0.34), M['trim'])
    # the back: tall tail lamps, the beacons' bases, high brake lamp, the rear step, hinges
    for s in (1, -1):
        k.box((xr - 0.012, s * (HW_BOX - 0.05), 1.15), (0.03, 0.1, 0.62), M['red']); k.box((xr - 0.014, s * (HW_BOX - 0.05), 0.78), (0.03, 0.1, 0.12), M['amber'])
        k.box((xr - 0.014, s * (HW_BOX - 0.05), 0.64), (0.03, 0.1, 0.1), M['lens'])
        for z in (0.8, 1.6, 2.4): k.box((xr - 0.03, s * (HW_BOX - 0.03), z), (0.04, 0.04, 0.12), M['trim'])
    k.box((xr - 0.012, 0, 2.86), (0.03, 0.4, 0.05), M['red'])
    k.box((xr - 0.14, 0, 0.42), (0.26, HW_BOX * 1.6, 0.05), M['chrome']); k.box((xr - 0.02, 0, 0.47), (0.06, HW_BOX * 1.9, 0.06), M['trim'])
    # the roof: air-con pod, aerials, GPS puck
    k.box((X(5.0), 0, ROOF + 0.06), (1.1, 0.8, 0.14), M['white']); k.box((X(5.0), 0, ROOF + 0.14), (0.9, 0.6, 0.03), M['trim'])
    for (u, y) in ((2.6, 0.5), (2.9, -0.5)): k.box((X(u), y, ROOF + 0.22), (0.015, 0.015, 0.45), M['trim'])
    k.box((X(3.3), 0, ROOF + 0.02), (0.12, 0.12, 0.04), M['trim'])
    # chassis between the wheels, fuel filler
    for s in (1, -1): k.box((X(3.0), s * (HW_BOX - 0.2), 0.42), (2.9, 0.1, 0.14), M['under'])
    k.box((X(3.4), -(HW_BOX - 0.15), 0.46), (0.14, 0.2, 0.12), M['under'])
    return k.build('box_det', recalc=False)

# ------------------------------------------------------------------ the livery: one picture, projected per face
TEX = 2048
def livery():
    """Nearside (bottom band: u from the front), offside (middle band: front at the right), front (top left), roof (top right)."""
    im = Image.new('RGB', (TEX, TEX), (219, 237, 16))
    Y, G = (219, 237, 16), (0, 107, 50)
    BAND = TEX * 0.375
    def band(offside):
        b_ = Image.new('RGB', (TEX, int(BAND)), Y); d = ImageDraw.Draw(b_)
        def px(u, z): x = u / L * TEX; return (TEX - 1 - x if offside else x), BAND - 1 - z / 3.0 * BAND
        def rect(u0, u1, z0, z1, col):
            (xa, ya), (xb, yb) = px(u0, z0), px(u1, z1); d.rectangle([min(xa, xb), min(ya, yb), max(xa, xb), max(ya, yb)], fill=col)
        bl, bh, z0 = 0.6, 0.42, 0.56; u = 0.35; k = 0
        while u < L - 0.02:
            for row in range(2): rect(u, min(L, u + bl), z0 + row * bh, z0 + (row + 1) * bh, G if (k + row) % 2 == 0 else Y)
            u += bl; k += 1
        u = BOX_U0; k = 0; q = 0.1
        while u < L - 0.02:
            for row in range(2): rect(u, u + q, 2.74 + row * q, 2.74 + (row + 1) * q, G if (k + row) % 2 == 0 else Y)
            u += q; k += 1
        def text(t, size, uc, zc, width):
            f = ImageFont.truetype(FONT, size); tl = Image.new('RGBA', (int(f.getlength(t)) + 20, int(size * 1.25)), (0, 0, 0, 0)); ImageDraw.Draw(tl).text((10, 0), t, font=f, fill=G + (255,))
            sc = (width / L * TEX) / tl.width; tl = tl.resize((max(1, int(tl.width * sc)), max(1, int(tl.height * sc * (BAND / (TEX / 2)) * 1.33))), Image.LANCZOS)
            cx, cy = px(uc, zc); b_.paste(tl, (int(cx - tl.width / 2), int(cy - tl.height / 2)), tl)
        text('AMBULANCE', 140, 5.0, 2.22, 2.5); text('EMERGENCY SERVICE', 60, 5.0, 2.56, 1.5)
        return b_
    im.paste(band(False), (0, int(TEX - BAND))); im.paste(band(True), (0, int(TEX - 2 * BAND)))
    # front (top-left quarter): u across the van, v = z; AMBULANCE mirror-written on the peak, chequer under it
    q4 = TEX // 4
    fr = Image.new('RGB', (TEX // 2, q4), Y); fd = ImageDraw.Draw(fr)
    t = 'AMBULANCE'; ft = ImageFont.truetype(FONT, 120); ftl = Image.new('RGBA', (int(ft.getlength(t)) + 20, 150), (0, 0, 0, 0)); ImageDraw.Draw(ftl).text((10, 0), t, font=ft, fill=G + (255,))
    ftl = ftl.transpose(Image.FLIP_LEFT_RIGHT); sc_ = (1.75 / 2.2 * fr.width) / ftl.width; ftl = ftl.resize((int(ftl.width * sc_), int(ftl.height * sc_ * 0.9)), Image.LANCZOS)
    def fz(z): return fr.height - 1 - z / 3.0 * fr.height
    fr.paste(ftl, (int(fr.width / 2 - ftl.width / 2), int(fz(2.66) - ftl.height / 2)), ftl)
    for kq in range(22):
        for row in range(2):
            col = G if (kq + row) % 2 == 0 else Y; x0 = kq * fr.width / 22; z0_ = 2.37 + row * 0.07
            fd.rectangle([x0, fz(z0_ + 0.07), x0 + fr.width / 22, fz(z0_)], fill=col)
    im.paste(fr, (0, 0))
    ImageDraw.Draw(im).rectangle([TEX // 2, 0, TEX, q4], fill=(236, 238, 236))
    im = im.filter(ImageFilter.GaussianBlur(0.6))
    a = np.asarray(im, np.float32) * (1 + np.random.default_rng(3).normal(0, 0.012, (TEX, TEX, 1))); im = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))
    im.save(os.path.join(OUT, 'amb_livery.png'))

def paint_uvs(ob):
    """Project the livery: each side from its own side, the front from the front, the cab's tops yellow, the box roof white."""
    me = ob.data
    if 'UVMap' not in me.uv_layers: me.uv_layers.new(name='UVMap')
    uv = me.uv_layers['UVMap'].data; names = [m.name if m else 'trim' for m in me.materials]
    c = lambda v, a, b: min(b, max(a, v))
    for p in me.polygons:
        if names[p.material_index] != 'paint': continue
        n = p.normal; cx = p.center.x
        for li in p.loop_indices:
            co = me.vertices[me.loops[li].vertex_index].co; u = (L / 2 - co.x) / L; z = co.z / 3.0
            if abs(n.y) >= 0.55 and abs(n.y) >= abs(n.z) * 0.8:
                if n.y > 0: uv[li].uv = (c(u, 0.001, 0.999), c(z * 0.375, 0.001, 0.374))
                else: uv[li].uv = (c(1 - u, 0.001, 0.999), 0.375 + c(z * 0.375, 0.001, 0.374))
            elif n.x > 0.35 and n.x >= abs(n.z) * 0.6: uv[li].uv = (c((co.y + 1.1) / 2.2 * 0.5, 0.001, 0.499), 0.75 + c(z * 0.25, 0.001, 0.249))
            elif cx > BOX_X1 - 0.05 and co.z < 2.4: uv[li].uv = (0.02, 0.76)      # bonnet and cab roof: plain yellow
            else: uv[li].uv = (0.75, 0.88)                                       # the box roof: white

# ------------------------------------------------------------------ assemble and export
def build():
    for o in list(bpy.data.objects): bpy.data.objects.remove(o)
    carforge.MATS.clear()
    for mm in list(bpy.data.materials): bpy.data.materials.remove(mm)
    M = carforge.materials(YEL)
    M['white'] = mat('white', (0.88, 0.89, 0.9), 0.4); M['floor'] = mat('floor', (0.42, 0.43, 0.45), 0.7)
    M['blue'] = mat('blue', (0.05, 0.12, 0.6), 0.1); M['seat'] = mat('seat', (0.04, 0.045, 0.06), 0.6)
    M['door'] = mat('door', (1, 1, 1), 0.4); M['doorin'] = mat('doorin', (0.55, 0.56, 0.58), 0.5)
    M['bag'] = mat('bag', (0.08, 0.22, 0.6), 0.4); M['bag2'] = mat('bag2', (0.06, 0.36, 0.2), 0.4)
    cab = cab_mesh(M); box, peak = box_body(M)
    # wheel arches through the cab (front) and box (rear), and the door opening at the back
    for u in (UF,): cut(cab, 'cyl', (X(u), 0, RAD), (RAD + 0.07, RAD + 0.07, 1.6), (math.pi / 2, 0, 0))
    cut(box, 'cyl', (X(UR), 0, RAD), (RAD + 0.07, RAD + 0.07, 1.6), (math.pi / 2, 0, 0))
    cut(box, 'cube', (BOX_X0, 0, 0.5 + (2.72 - 0.5) / 2), (0.14, (HW_BOX - 0.07) * 2, 2.72 - 0.5))
    bm = bmesh.new(); bm.from_mesh(box.data)
    back = [f for f in bm.faces if abs(f.calc_center_median().x - (BOX_X0 + 0.07)) < 0.015 and abs(f.calc_center_median().y) < HW_BOX - 0.06 and 0.5 < f.calc_center_median().z < 2.72]
    bmesh.ops.delete(bm, geom=back, context='FACES'); bm.to_mesh(box.data); bm.free()
    for o in (cab, box, peak):
        for p in o.data.polygons: p.use_smooth = True
        carforge.set_auto_smooth(o, 35)
    car = type('C', (), {})(); car.P = dict(rad=RAD, arch_gap=0.06, tyre_w=0.215); car.L = L; car.uF, car.uR = UF, UR; car.hw0 = HW_BOX
    wells = carforge.wheel_wells(car, M)
    glass = cab_glass(M); front = front_end(M); det = box_details(M)
    # the treatment room inside (as before: white walls, cabinets, bench, seat, bags, stretcher rails)
    k = Kit(); xr = BOX_X0 + 0.06; xf = BOX_X1 - 0.3; yi = HW_BOX - 0.07; zf, zc = 0.56, 2.74
    c = [(xr, -yi, zf), (xr, yi, zf), (xf, yi, zf), (xf, -yi, zf), (xr, -yi, zc), (xr, yi, zc), (xf, yi, zc), (xf, -yi, zc)]
    cv = [k.bm.verts.new(p) for p in c]
    for q, mname in [((0, 1, 2, 3), 'floor'), ((4, 7, 6, 5), 'white'), ((1, 5, 6, 2), 'white'), ((0, 3, 7, 4), 'white'), ((3, 2, 6, 7), 'white')]:
        f = k.bm.faces.new([cv[i] for i in q]); f.material_index = k.mi(M[mname])
        ctr = Vector((sum(c[i][0] for i in q) / 4, sum(c[i][1] for i in q) / 4, sum(c[i][2] for i in q) / 4))
        inset = [k.bm.verts.new(Vector(c[i]) + (Vector(((xr + xf) / 2, 0, (zf + zc) / 2)) - ctr).normalized() * 0.004) for i in reversed(q)]
        f2 = k.bm.faces.new(inset); f2.material_index = k.mi(M[mname])
    for i in range(4):
        x0 = xr + 0.5 + i * 0.75
        k.box((x0 + 0.33, -yi + 0.2, zf + 0.55), (0.62, 0.36, 1.1), M['white']); k.box((x0 + 0.33, -yi + 0.17, zc - 0.32), (0.62, 0.3, 0.5), M['white'])
        k.box((x0 + 0.33, -yi + 0.385, zf + 0.85), (0.5, 0.01, 0.02), M['chrome'])
    k.box(((xr + xf) / 2 + 0.3, yi - 0.22, zf + 0.42), (2.0, 0.42, 0.08), M['seat']); k.box(((xr + xf) / 2 + 0.3, yi - 0.04, zf + 0.85), (2.0, 0.06, 0.6), M['seat'])
    for i, mname in enumerate(['bag', 'bag2', 'bag']): k.box((xr + 0.9 + i * 0.55, yi - 0.24, zf + 0.62), (0.45, 0.3, 0.28), M[mname])
    k.box((xf + 0.1, 0, zf + 0.45), (0.5, 0.55, 0.1), M['seat']); k.box((xf - 0.12, 0, zf + 0.95), (0.1, 0.5, 0.75), M['seat'])
    for i in range(3): k.box((xr + 0.7 + i * 1.1, 0, zc - 0.01), (0.6, 0.12, 0.012), M['lens'])
    k.box((xr + 0.9, -0.2, zf + 0.05), (2.6, 0.08, 0.05), M['chrome']); k.box((xr + 0.9, 0.2, zf + 0.05), (2.6, 0.08, 0.05), M['chrome'])
    k.box((xr + 1.1, 0, zf + 0.45), (1.9, 0.56, 0.12), M['seat']); k.box((xr + 0.2, 0, zf + 0.62), (0.3, 0.56, 0.25), M['white'])   # the stretcher and its pillow
    # a dark cab interior (dash, seats, bulkhead) so the glass reads as glass
    xc0, xc1 = X(1.9), X(1.0)
    k.box(((xc0 + xc1) / 2, 0, 0.6), (xc1 - xc0, 1.9, 0.04), M['inside']); k.box((X(1.05), 0, 1.15), (0.3, 1.9, 0.25), M['inside'])
    for y in (0.5, -0.5): k.box((X(1.6), y, 0.85), (0.5, 0.5, 0.12), M['seat']); k.box((X(1.82), y, 1.3), (0.1, 0.5, 0.8), M['seat'])
    k.box((X(1.95), 0, 1.4), (0.04, 1.9, 1.8), M['inside'])
    inner = k.build('room', recalc=False)
    # lamps that flash, and the light bar on the peak
    parts = {}
    def lamp(name, c, s):
        kk = Kit(); kk.box(c, s, M['blue']); ob = kk.build(name); parts[name] = ob
    xb = BOX_X1 + 0.12; zb = ROOF + 0.02
    base = Kit(); base.box((xb, 0, zb), (0.3, 1.6, 0.05), M['white']); base.box((xb + 0.12, 0, zb + 0.06), (0.04, 0.3, 0.08), M['lens']); parts['bar_base'] = base.build('bar_base')
    lamp('bar_L', (xb, 0.45, zb + 0.07), (0.26, 0.66, 0.1)); lamp('bar_R', (xb, -0.45, zb + 0.07), (0.26, 0.66, 0.1))
    lamp('beacon_L', (BOX_X0 + 0.12, HW_BOX - 0.14, ROOF + 0.05), (0.18, 0.16, 0.12)); lamp('beacon_R', (BOX_X0 + 0.12, -(HW_BOX - 0.14), ROOF + 0.05), (0.18, 0.16, 0.12))
    lamp('grille_L', (X(0) + 0.055, 0.3, 0.84), (0.02, 0.14, 0.04)); lamp('grille_R', (X(0) + 0.055, -0.3, 0.84), (0.02, 0.14, 0.04))
    # rear doors, hinged at the outer edges, chevrons on the outside
    doors = {}
    for s, name in ((1, 'door_L'), (-1, 'door_R')):
        kk = Kit(); hw = HW_BOX; w = hw - 0.06; h0, h1 = 0.5, 2.72; t = 0.05
        x_out, x_in = BOX_X0 - 0.035, BOX_X0 - 0.035 + t
        yh = s * (hw - 0.04); yc = s * 0.005
        kk.face([(x_out, yh, h0), (x_out, yc, h0), (x_out, yc, h1), (x_out, yh, h1)], M['door'], flip=s < 0)
        kk.face([(x_in, yh, h0), (x_in, yc, h0), (x_in, yc, h1), (x_in, yh, h1)], M['doorin'], flip=s > 0)
        kk.box(((x_out + x_in) / 2, (yh + yc) / 2, h1), (t, abs(yh - yc), 0.02), M['trim']); kk.box(((x_out + x_in) / 2, (yh + yc) / 2, h0), (t, abs(yh - yc), 0.02), M['trim'])
        kk.box(((x_out + x_in) / 2, yc, (h0 + h1) / 2), (t, 0.02, h1 - h0), M['trim']); kk.box(((x_out + x_in) / 2, yh, (h0 + h1) / 2), (t, 0.02, h1 - h0), M['trim'])
        kk.face([(x_out - 0.004, yh - s * 0.12, 1.75), (x_out - 0.004, yc + s * 0.12, 1.75), (x_out - 0.004, yc + s * 0.12, 2.45), (x_out - 0.004, yh - s * 0.12, 2.45)], M['glass'], flip=s < 0)
        kk.box((x_out - 0.02, yc + s * 0.2, 1.35), (0.03, 0.12, 0.03), M['trim'])
        ob = kk.build(name, recalc=False); me = ob.data
        if 'UVMap' not in me.uv_layers: me.uv_layers.new(name='UVMap')
        uvd = me.uv_layers['UVMap'].data; names = [mm.name for mm in me.materials]
        for p in me.polygons:
            if names[p.material_index] != 'door': continue
            for li in p.loop_indices:
                co = me.vertices[me.loops[li].vertex_index].co; uu = (co.y - min(yh, yc)) / abs(yh - yc); vv = (co.z - h0) / (h1 - h0)
                uvd[li].uv = (uu if s < 0 else 1 - uu, vv)
        piv = Vector((x_out, yh, 0)); me.transform(Matrix.Translation(-piv)); ob.location = piv; doors[name] = ob
    wheels = []
    P = dict(rad=RAD, tyre_w=0.215, wheel='steel', rim_frac=0.6)
    for name, u, s in (('wheel_FL', UF, 1), ('wheel_FR', UF, -1), ('wheel_RL', UR, 1), ('wheel_RR', UR, -1)):
        w = carforge.wheel(P, M, name); w.location = (X(u), s * ((HW_CAB if u == UF else HW_BOX) - P['tyre_w'] / 2 - 0.02), RAD)
        if s < 0: w.rotation_euler = (0, 0, math.pi)
        wheels.append(w)
    return M, [cab, box, peak], [wells, glass, front, det, inner], parts, doors, wheels

if __name__ == '__main__':
    M, painted, extra, parts, doors, wheels = build()
    AF.make_images(OUT); livery()
    body = AF.join(painted, 'amb_paint')
    paint_uvs(body)
    allb = AF.join([body] + extra, 'amb_body')
    AF.remap(allb)
    for ob in list(parts.values()) + list(doors.values()): AF.remap(ob)
    for w in wheels: AF.wheel_swatch(w)
    R = Matrix.Rotation(math.radians(-90), 4, 'Z')
    allb.data.transform(R); allb.matrix_world = Matrix.Identity(4)
    for ob in list(parts.values()):
        ob.matrix_world = R @ ob.matrix_world
        for o in bpy.context.scene.objects: o.select_set(False)
        ob.select_set(True); bpy.context.view_layer.objects.active = ob
        with bpy.context.temp_override(active_object=ob, selected_editable_objects=[ob], selected_objects=[ob]): bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for ob in list(doors.values()) + wheels:
        ob.matrix_world = R @ ob.matrix_world
        for o in bpy.context.scene.objects: o.select_set(False)
        ob.select_set(True); bpy.context.view_layer.objects.active = ob
        with bpy.context.temp_override(active_object=ob, selected_editable_objects=[ob], selected_objects=[ob]): bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    keep = [allb] + list(parts.values()) + list(doors.values()) + wheels
    for o in bpy.context.scene.objects: o.select_set(o in keep)
    bpy.context.view_layer.objects.active = allb
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, 'ambulance.fbx'), use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y',
                             apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
                             mesh_smooth_type='FACE', use_tspace=False, path_mode='STRIP', embed_textures=False, add_leaf_bones=False)
    tri = lambda o: sum(len(p.vertices) - 2 for p in o.data.polygons)
    json.dump(dict(L=L, W=HW_BOX * 2, roof=ROOF, wb=UR - UF, fo=UF, rad=RAD, tris=tri(allb)), open(os.path.join(OUT, 'ambulance.json'), 'w'))
    print('AMB DONE tris', tri(allb), [o.name for o in keep])

"""
Every loose thing in the Silver Sixpence, modelled here in Blender (bpy) from scratch: the slot machines, tables,
chairs and stools, the bar (counter, back bar, beer fonts, optics, fridges, coffee machine, glass washer, till),
glasses and bottles and beer mats, chips and cards, and the back rooms (office, cash room, staff room, stock room).
All textured from one atlas (casino_atlas.py), two materials: CP_Opaque and CP_Glass. Each prop is its own mesh with
its pivot at the bottom centre, front facing -Y here (+Z in Unity). props.json says what each one is for in the game:
pick up, sit on, use, open, or just there.
Run: python3 propforge.py -- <atlas out folder> <out folder> [--preview]
"""
import bpy, bmesh, sys, os, math, json, random
from mathutils import Vector, Matrix
argv = sys.argv[sys.argv.index('--') + 1:]
ATL, OUT = argv[0], argv[1]; PREVIEW = '--preview' in argv
os.makedirs(OUT, exist_ok=True)
AT = json.load(open(os.path.join(ATL, 'casino_atlas.json')))
PAL, REG = AT['palette'], AT['regions']
TAU = math.pi * 2
bpy.ops.wm.read_factory_settings(use_empty=True)

# ===================================================================================== mesh building
def skin_uv(skin, u, v):
    if skin in PAL: return PAL[skin]
    r = REG[skin]; u = min(max(u, 0.0), 1.0); v = min(max(v, 0.0), 1.0)
    return (r[0] + (r[2] - r[0]) * u, r[1] + (r[3] - r[1]) * v)
def is_glass(skin): return skin.startswith('glass_')

class P:
    """One prop being built: a bmesh with UVs and per-face material (0 opaque, 1 glass)."""
    def __init__(s, name):
        s.name = name; s.bm = bmesh.new(); s.uv = s.bm.loops.layers.uv.new('UVMap'); s.M = Matrix.Identity(4); s.stack = []
    def push(s, pos=(0, 0, 0), rz=0.0, rx=0.0, ry=0.0, scale=1.0):
        s.stack.append(s.M.copy())
        s.M = s.M @ Matrix.Translation(Vector(pos)) @ Matrix.Rotation(rz, 4, 'Z') @ Matrix.Rotation(ry, 4, 'Y') @ Matrix.Rotation(rx, 4, 'X') @ Matrix.Scale(scale, 4)
    def pop(s): s.M = s.stack.pop()
    def face(s, pts, uvs, skin, smooth=False):
        vs = [s.bm.verts.new(s.M @ Vector(p)) for p in pts]
        try: f = s.bm.faces.new(vs)
        except ValueError: return None
        f.material_index = 1 if is_glass(skin) else 0; f.smooth = smooth
        for l, (u, v) in zip(f.loops, uvs): l[s.uv].uv = skin_uv(skin, u, v)
        return f
    def grid(s, rows, skin, smooth=True, closed_u=False):
        """rows: list of rows of (point, (u, v)); quads between consecutive rows/columns (verts shared so smoothing works)."""
        vr = [[s.bm.verts.new(s.M @ Vector(p)) for p, _ in row] for row in rows]
        n = len(rows[0])
        for i in range(len(rows) - 1):
            for j in range(n - 1 + (1 if closed_u else 0)):
                j1 = (j + 1) % n
                q = [vr[i][j], vr[i][j1], vr[i + 1][j1], vr[i + 1][j]]
                if len({id(x) for x in q}) < 3: continue
                try: f = s.bm.faces.new(q)
                except ValueError: continue
                f.material_index = 1 if is_glass(skin) else 0; f.smooth = smooth
                uvq = [rows[i][j][1], rows[i][j1][1] if j1 or not closed_u else (1.0, rows[i][j1][1][1]), rows[i + 1][j1][1] if j1 or not closed_u else (1.0, rows[i + 1][j1][1][1]), rows[i + 1][j][1]]
                for l, (u, v) in zip(f.loops, uvq): l[s.uv].uv = skin_uv(skin, u, v)
        return vr

    # ---------------------------------------------------------------- primitives
    def box(s, c, size, skin, top=None, front=None, skip=()):
        """Box centred at c (x, y, z centre). UVs: each face maps its own rectangle to [0,1]. skip: 'top','bottom','front','back','left','right'."""
        x, y, z = c; hx, hy, hz = size[0] / 2, size[1] / 2, size[2] / 2
        P0 = lambda a, b, cc: (x + a * hx, y + b * hy, z + cc * hz)
        F = {'top': [P0(-1, -1, 1), P0(1, -1, 1), P0(1, 1, 1), P0(-1, 1, 1)], 'bottom': [P0(-1, 1, -1), P0(1, 1, -1), P0(1, -1, -1), P0(-1, -1, -1)],
             'front': [P0(-1, -1, -1), P0(1, -1, -1), P0(1, -1, 1), P0(-1, -1, 1)], 'back': [P0(1, 1, -1), P0(-1, 1, -1), P0(-1, 1, 1), P0(1, 1, 1)],
             'left': [P0(-1, 1, -1), P0(-1, -1, -1), P0(-1, -1, 1), P0(-1, 1, 1)], 'right': [P0(1, -1, -1), P0(1, 1, -1), P0(1, 1, 1), P0(1, -1, 1)]}
        for k, pts in F.items():
            if k in skip: continue
            sk = top if (k == 'top' and top) else front if (k == 'front' and front) else skin
            s.face(pts, [(0, 0), (1, 0), (1, 1), (0, 1)], sk)
    def rbox(s, c, size, r, skin, segs=2, top=None, front=None):
        """Rounded box (bevelled on every edge), smooth shaded; UVs box-projected over the whole box."""
        tb = bmesh.new(); bmesh.ops.create_cube(tb, size=1.0)
        for v in tb.verts: v.co = Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]))
        r = min(r, min(size) * 0.49)
        if r > 0.0005: bmesh.ops.bevel(tb, geom=list(tb.edges) + list(tb.verts), offset=r, segments=segs, affect='EDGES', profile=0.5)
        tb.normal_update()
        for f in tb.faces:
            n = f.normal; ax = max(range(3), key=lambda i: abs(n[i]))
            sk = skin
            if top and ax == 2 and n.z > 0.9: sk = top
            if front and ax == 1 and n.y < -0.9: sk = front
            vs = [s.bm.verts.new(s.M @ (Vector(c) + v.co)) for v in f.verts]
            try: nf = s.bm.faces.new(vs)
            except ValueError: continue
            nf.material_index = 1 if is_glass(sk) else 0; nf.smooth = True
            for l, v in zip(nf.loops, f.verts):
                p = v.co; a = [(p.x / size[0] + 0.5), (p.y / size[1] + 0.5), (p.z / size[2] + 0.5)]
                uvp = (a[0], a[1]) if ax == 2 else (a[0], a[2]) if ax == 1 else (a[1], a[2])
                l[s.uv].uv = skin_uv(sk, *uvp)
        tb.free()
    def lathe(s, prof, skin, segs=16, a0=0.0, a1=TAU, cap_top=False, cap_bot=False, smooth=True, c=(0, 0, 0)):
        """Revolve profile [(r, z), ...] (bottom to top) about Z. u = around, v = along the profile."""
        L = [0.0]
        for i in range(1, len(prof)): L.append(L[-1] + math.hypot(prof[i][0] - prof[i - 1][0], prof[i][1] - prof[i - 1][1]))
        tot = L[-1] or 1.0; full = abs(a1 - a0 - TAU) < 1e-6; n = segs if full else segs + 1
        rows = []
        for i, (r, z) in enumerate(prof):
            row = []
            for j in range(n):
                a = a0 + (a1 - a0) * j / segs
                row.append(((c[0] + math.cos(a) * r, c[1] + math.sin(a) * r, c[2] + z), (j / segs, L[i] / tot)))
            rows.append(row)
        s.grid(rows, skin, smooth, closed_u=full)
        for cap, idx in ((cap_bot, 0), (cap_top, -1)):
            if not cap or prof[idx][0] < 1e-5: continue
            r, z = prof[idx]; pts = [(c[0] + math.cos(a0 + (a1 - a0) * j / segs) * r, c[1] + math.sin(a0 + (a1 - a0) * j / segs) * r, c[2] + z) for j in range(segs)]
            if idx == 0: pts = pts[::-1]
            uvs = [((p[0] - c[0]) / (2 * r) + 0.5, (p[1] - c[1]) / (2 * r) + 0.5) for p in pts]
            s.face(pts, uvs, cap if isinstance(cap, str) else skin)
    def cyl(s, c, r, h, skin, segs=12, top=True, bot=False, rtop=None, smooth=True):
        rt = r if rtop is None else rtop
        s.lathe([(r, 0), (rt, h)], skin, segs, cap_top=top, cap_bot=bot, smooth=smooth, c=c)
    def disc(s, c, r, skin, segs=16, up=True):
        pts = [(c[0] + math.cos(TAU * j / segs) * r, c[1] + math.sin(TAU * j / segs) * r, c[2]) for j in range(segs)]
        if not up: pts = pts[::-1]
        s.face(pts, [((p[0] - c[0]) / (2 * r) + 0.5, (p[1] - c[1]) / (2 * r) + 0.5) for p in pts], skin)
    def torus(s, c, R, r, skin, segs=16, rsegs=8, a0=0.0, a1=TAU):
        prof = [(R + math.cos(TAU * k / rsegs) * r, math.sin(TAU * k / rsegs) * r) for k in range(rsegs + 1)]
        s.lathe(prof, skin, segs, a0, a1, c=c)
    def tube(s, pts, r, skin, segs=8, caps=True):
        """A round bar along a polyline (rails, arms, handles)."""
        pts = [Vector(p) for p in pts]; rows = []; up = Vector((0, 0, 1))
        for i, p in enumerate(pts):
            t = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
            a = up if abs(t.dot(up)) < 0.9 else Vector((1, 0, 0))
            b1 = t.cross(a).normalized(); b2 = t.cross(b1).normalized()
            rows.append([(tuple(p + (b1 * math.cos(TAU * k / segs) + b2 * math.sin(TAU * k / segs)) * r), (k / segs, i / max(1, len(pts) - 1))) for k in range(segs)])
        s.grid(rows, skin, True, closed_u=True)
        if caps:
            for row in (rows[0][::-1], rows[-1]): s.face([q for q, _ in row], [(0.5, 0.5)] * len(row), skin)
    def quad(s, a, b, c, d, skin, uv=((0, 0), (1, 0), (1, 1), (0, 1))): s.face([a, b, c, d], list(uv), skin)
    def panel(s, x0, z0, x1, z1, y, skin, flip=False):
        """A flat picture facing -Y (or +Y if flip) at depth y."""
        if not flip: s.quad((x0, y, z0), (x1, y, z0), (x1, y, z1), (x0, y, z1), skin)
        else: s.quad((x1, y, z0), (x0, y, z0), (x0, y, z1), (x1, y, z1), skin)
    def tri_count(s): return sum(len(f.verts) - 2 for f in s.bm.faces)

PROPS = []; META = {}
def prop(name, kind='static', mass=1.0, label='', **kw):
    p = P(name); PROPS.append(p); META[name] = dict(kind=kind, mass=mass, label=label, **kw); return p

# ===================================================================================== glasses, drinks, bottles
def glass_shell(p, prof, skin='glass_clear', thick=0.003, segs=18):
    """A drinking glass: outer wall, rim, inner wall, thick base."""
    outer = prof; inner = [(max(0.0, r - thick), z) for r, z in prof]
    p.lathe(outer, skin, segs, smooth=True)
    p.lathe([(outer[-1][0], outer[-1][1]), (inner[-1][0], inner[-1][1])], skin, segs)
    p.lathe(inner[::-1][:-1] + [(0.0, inner[0][1] + 0.008)], skin, segs)
    p.disc((0, 0, 0.0005), prof[0][0], skin, segs, up=False)
def liquid(p, prof, level, skin, foam=None, foam_h=0.012, segs=18):
    """Drink inside a glass up to height `level`, profile = the glass's inner wall; optional head."""
    pts = []; base = prof[0][1] + 0.008
    for (r0, z0), (r1, z1) in zip(prof, prof[1:]):
        if z0 >= level: break
        pts.append((r0 - 0.004, max(z0, base)))
        if z1 > level: t = (level - z0) / max(1e-6, z1 - z0); pts.append((r0 + (r1 - r0) * t - 0.004, level)); break
    if not pts: return
    if pts[-1][1] < level - 1e-4: pts.append((prof[-1][0] - 0.004, level))
    p.lathe([(0.0, base)] + pts, skin, segs, cap_top=True)
    if foam:
        r = pts[-1][0]; p.lathe([(r, level), (r + 0.0005, level + foam_h * 0.6), (r * 0.9, level + foam_h), (0.0, level + foam_h * 1.05)], foam, segs)
PINT = [(0.033, 0.0), (0.034, 0.01), (0.036, 0.05), (0.040, 0.12), (0.043, 0.135), (0.041, 0.15)]           # nonic pint, 15 cm
HALF = [(0.028, 0.0), (0.03, 0.04), (0.034, 0.1), (0.036, 0.11), (0.035, 0.12)]
HIGH = [(0.030, 0.0), (0.031, 0.01), (0.032, 0.14)]
TUMB = [(0.036, 0.0), (0.037, 0.01), (0.040, 0.085)]
SHOT = [(0.018, 0.0), (0.019, 0.012), (0.023, 0.06)]
def stemware(p, bowl, stem_h, foot_r, skin='glass_clear'):
    p.lathe([(foot_r, 0.0), (foot_r, 0.002), (0.006, 0.006), (0.0035, 0.012), (0.0035, stem_h), (0.008, stem_h + 0.006)], skin, 16)
    p.disc((0, 0, 0.0004), foot_r, skin, 16, up=False)
    pr = [(r, z + stem_h) for r, z in bowl]; p.lathe(pr, skin, 18)
    inner = [(max(0.0, r - 0.002), z) for r, z in pr]; p.lathe(inner[::-1], skin, 18)
    return pr
WINE = [(0.006, 0.0), (0.03, 0.03), (0.038, 0.07), (0.035, 0.11), (0.03, 0.125)]
FLUTE = [(0.004, 0.0), (0.02, 0.03), (0.024, 0.11), (0.022, 0.15)]
def ice(p, n, r0, z0, seed):
    rr = random.Random(seed)
    for k in range(n):
        a = rr.random() * TAU; d = rr.random() * r0 * 0.5
        p.push((math.cos(a) * d, math.sin(a) * d, z0 + k * 0.012), rz=rr.random() * 3, rx=rr.random() * 0.6); p.rbox((0, 0, 0), (0.022, 0.022, 0.02), 0.004, 'ice', 1); p.pop()

for name, (lvl, sk, foam, label) in {'pint_empty': (0, None, None, 'Empty pint glass'), 'pint_lager': (0.135, 'lager', 'foam', 'Pint of lager'), 'pint_bitter': (0.135, 'bitter', 'foam', 'Pint of bitter'),
                                     'pint_stout': (0.128, 'stout', 'cream_liq', 'Pint of stout'), 'pint_cider': (0.135, 'cider', None, 'Pint of cider'), 'pint_half': (0.07, 'lager', 'foam', 'Half-drunk pint')}.items():
    p = prop(name, 'pickup', 0.5 if lvl else 0.25, label, drink=sk or '')
    glass_shell(p, PINT)
    if lvl: liquid(p, [(r - 0.003, z) for r, z in PINT], lvl, sk, foam, 0.018 if sk == 'stout' else 0.012)
p = prop('half_lager', 'pickup', 0.3, 'Half of lager', drink='lager'); glass_shell(p, HALF); liquid(p, [(r - 0.003, z) for r, z in HALF], 0.1, 'lager', 'foam')
p = prop('pint_inv', 'pickup', 0.25, 'Clean pint glass'); p.push((0, 0, 0.15), rx=math.pi); glass_shell(p, PINT); p.pop()
for name, (sk, lab) in {'wine_red': ('wine_red', 'Glass of red'), 'wine_white': ('wine_white', 'Glass of white'), 'wine_empty': (None, 'Wine glass')}.items():
    p = prop(name, 'pickup', 0.2, lab, drink=sk or ''); pr = stemware(p, WINE, 0.08, 0.035)
    if sk: liquid(p, [(r - 0.002, z) for r, z in pr], 0.08 + 0.055, sk)
p = prop('flute', 'pickup', 0.2, 'Glass of prosecco', drink='wine_white'); pr = stemware(p, FLUTE, 0.07, 0.03); liquid(p, [(r - 0.002, z) for r, z in pr], 0.07 + 0.12, 'wine_white')
p = prop('highball_cola', 'pickup', 0.35, 'Vodka and coke', drink='cola'); glass_shell(p, HIGH); liquid(p, [(r - 0.003, z) for r, z in HIGH], 0.12, 'cola'); ice(p, 4, 0.03, 0.06, 1)
p.push((0.012, 0, 0.12), rx=0.25); p.cyl((0, 0, -0.08), 0.0035, 0.16, 'plastic_black', 6); p.pop()
p.push((-0.026, 0, 0.13), ry=0.3); p.cyl((0, 0, -0.004), 0.022, 0.008, 'lemon', 12, True, True); p.pop()
p = prop('highball_gin', 'pickup', 0.35, 'Gin and tonic', drink='spirit_clear'); glass_shell(p, HIGH); liquid(p, [(r - 0.003, z) for r, z in HIGH], 0.12, 'spirit_clear'); ice(p, 4, 0.03, 0.05, 2)
p.push((-0.02, 0.01, 0.125), ry=0.5); p.cyl((0, 0, -0.004), 0.02, 0.008, 'lime', 12, True, True); p.pop()
p = prop('tumbler_whisky', 'pickup', 0.3, 'Whisky on the rocks', drink='whisky'); glass_shell(p, TUMB, thick=0.004); liquid(p, [(r - 0.004, z) for r, z in TUMB], 0.04, 'whisky'); ice(p, 2, 0.03, 0.025, 3)
p = prop('shot_full', 'pickup', 0.1, 'Shot of sambuca', drink='spirit_clear'); glass_shell(p, SHOT, thick=0.004); liquid(p, [(r - 0.004, z) for r, z in SHOT], 0.05, 'spirit_clear')
p = prop('shot_empty', 'pickup', 0.08, 'Shot glass'); glass_shell(p, SHOT, thick=0.004)
p = prop('cup_coffee', 'pickup', 0.25, 'Coffee', drink='coffee')
p.lathe([(0.06, 0.0), (0.065, 0.004), (0.07, 0.01), (0.072, 0.012), (0.04, 0.008), (0.0, 0.008)], 'white_gloss', 20)
p.lathe([(0.0, 0.01), (0.022, 0.01), (0.03, 0.02), (0.04, 0.06), (0.042, 0.07), (0.038, 0.07), (0.036, 0.062)], 'white_gloss', 18); p.disc((0, 0, 0.064), 0.036, 'coffee', 16)
p.tube([(0.04, 0, 0.055), (0.058, 0, 0.052), (0.06, 0, 0.035), (0.04, 0, 0.03)], 0.005, 'white_gloss', 6)
p = prop('mug_tea', 'pickup', 0.3, 'Mug of tea', drink='tea')
p.lathe([(0.0, 0.0), (0.04, 0.0), (0.042, 0.004), (0.042, 0.095), (0.038, 0.095), (0.038, 0.01)], 'white', 18); p.disc((0, 0, 0.08), 0.038, 'tea', 16)
p.tube([(0.041, 0, 0.08), (0.065, 0, 0.075), (0.066, 0, 0.03), (0.041, 0, 0.025)], 0.007, 'white', 6)
p.push(); p.lathe([(0.0425, 0.03), (0.0425, 0.07)], 'red', 18, -0.9, 0.9); p.pop()

def rat(prof, z):
    for (r0, z0), (r1, z1) in zip(prof, prof[1:]):
        if z0 <= z <= z1: return r0 + (r1 - r0) * (z - z0) / max(1e-6, z1 - z0)
    return prof[-1][0]
def rband(prof, band): return max(rat(prof, band[0] + (band[1] - band[0]) * t / 6) for t in range(7))
def bottle(name, label_idx, prof, glass, label_band, liq='whisky', cap='black', label='Bottle', kind='pickup', square=False, mass=1.0):
    p = prop(name, kind, mass, label, drink=liq or '')
    if square:
        z0, z1 = prof[0][1], label_band[1] + 0.03; w = prof[1][0] * 2
        p.rbox((0, 0, (z0 + z1) / 2 + 0.002), (w, w * 0.7, z1 - z0), 0.012, glass, 2)
        p.box((0, -w * 0.35 - 0.0015, (label_band[0] + label_band[1]) / 2), (w * 0.8, 0.002, label_band[1] - label_band[0]), f'label{label_idx}', skip=('back',))
        p.lathe([(w * 0.42, z1), (0.016, z1 + 0.03)] + [(r, z) for r, z in prof if z > z1 + 0.03], glass, 16)
    else:
        p.lathe(prof, glass, 18); p.disc((0, 0, 0.001), prof[0][0], glass, 18, up=False)
        rl = rband(prof, label_band) + 0.0012
        p.lathe([(rl, label_band[0]), (rl, label_band[1])], f'label{label_idx}', 18, -math.pi * 0.95, -math.pi * 0.05)
        if liq:
            lr = [(max(0.001, r - 0.003), z) for r, z in prof if z < label_band[1] + 0.02]
            p.lathe([(0.0, 0.004)] + lr[1:], liq, 14, cap_top=True)
    top = prof[-1]; p.cyl((0, 0, top[1]), top[0] + 0.002, 0.016, cap, 12, True)
    return p
WHISKY = [(0.0, 0.0), (0.042, 0.0), (0.042, 0.2), (0.016, 0.24), (0.014, 0.29)]
GIN = [(0.04, 0.0), (0.042, 0.01), (0.042, 0.18), (0.035, 0.21), (0.016, 0.24), (0.014, 0.28)]
VODKA = [(0.036, 0.0), (0.037, 0.01), (0.037, 0.2), (0.03, 0.24), (0.013, 0.27), (0.012, 0.31)]
RUM = [(0.042, 0.0), (0.044, 0.01), (0.044, 0.16), (0.03, 0.2), (0.015, 0.23), (0.014, 0.27)]
LIQ = [(0.038, 0.0), (0.045, 0.06), (0.045, 0.14), (0.02, 0.2), (0.014, 0.25)]
WINEB = [(0.037, 0.0), (0.038, 0.01), (0.038, 0.2), (0.03, 0.24), (0.014, 0.27), (0.014, 0.31)]
BEERB = [(0.031, 0.0), (0.032, 0.01), (0.032, 0.13), (0.026, 0.16), (0.013, 0.2), (0.0135, 0.22)]
MIXER = [(0.026, 0.0), (0.027, 0.01), (0.027, 0.1), (0.016, 0.13), (0.011, 0.15), (0.0115, 0.165)]
SMALL = [(0.025, 0.0), (0.026, 0.01), (0.026, 0.1), (0.012, 0.13), (0.01, 0.15)]
BLIST = [  # name, label, profile, glass, band, liquid, cap, square, game label
    ('btl_glenmorrach', 0, WHISKY, 'glass_clear', (0.06, 0.15), 'whisky', 'black', True, 'Glenmorrach malt'), ('btl_blackheron', 1, WHISKY, 'glass_clear', (0.05, 0.14), 'whisky', 'gold', True, 'Black Heron whisky'),
    ('btl_ardbrae', 2, GIN, 'glass_green', (0.05, 0.14), 'whisky', 'black', False, 'Ardbrae malt'), ('btl_oldharbour', 3, RUM, 'glass_clear', (0.04, 0.12), 'whisky', 'gold', False, 'Old Harbour whisky'),
    ('btl_kesterton', 4, GIN, 'glass_blue', (0.06, 0.15), 'spirit_clear', 'chrome', False, 'Kesterton gin'), ('btl_hawthorn', 5, GIN, 'glass_clear', (0.06, 0.15), 'wine_rose', 'rose_gold', False, 'Hawthorn pink gin'),
    ('btl_volkov', 6, VODKA, 'glass_clear', (0.06, 0.16), 'spirit_clear', 'red', False, 'Volkov vodka'), ('btl_arcticwolf', 7, VODKA, 'glass_frost', (0.06, 0.16), 'spirit_clear', 'chrome', False, 'Arctic Wolf vodka'),
    ('btl_rourke', 8, RUM, 'glass_amber', (0.04, 0.12), 'whisky', 'black', False, 'Spiced rum'), ('btl_bayisland', 9, RUM, 'glass_clear', (0.04, 0.12), 'spirit_clear', 'white', False, 'White rum'),
    ('btl_duvernay', 10, LIQ, 'glass_amber', (0.06, 0.13), 'whisky', 'gold', False, 'Cognac'), ('btl_cremadolce', 11, LIQ, 'glass_dark', (0.06, 0.13), 'cream_liq', 'gold', False, 'Cream liqueur'),
    ('btl_sambuca', 12, VODKA, 'glass_clear', (0.06, 0.16), 'spirit_clear', 'black', False, 'Sambuca'), ('btl_bluelagoon', 13, LIQ, 'glass_blue', (0.06, 0.13), 'blue_drink', 'white', False, 'Blue curacao'),
    ('btl_lavaux', 14, WINEB, 'glass_green', (0.05, 0.14), 'wine_red', 'burgundy', False, 'Red wine'), ('btl_montealto', 15, WINEB, 'glass_green', (0.05, 0.14), 'wine_white', 'gold', False, 'White wine'),
    ('btl_rosado', 16, WINEB, 'glass_clear', (0.05, 0.14), 'wine_rose', 'pink', False, 'Rose wine'), ('btl_prosecco', 17, WINEB, 'glass_green', (0.05, 0.14), 'wine_white', 'gold', False, 'Prosecco'),
    ('btl_sixpence', 18, BEERB, 'glass_clear', (0.035, 0.1), 'lager', 'gold', False, 'Sixpence Gold'), ('btl_abbots', 19, BEERB, 'glass_amber', (0.035, 0.1), 'bitter', 'red', False, 'Abbots Wend bitter'),
    ('btl_ironbridge', 20, BEERB, 'glass_dark', (0.035, 0.1), 'stout', 'black', False, 'Ironbridge stout'), ('btl_millane', 21, BEERB, 'glass_green', (0.035, 0.1), 'cider', 'green', False, 'Mill Lane cider'),
    ('btl_northfield', 22, BEERB, 'glass_green', (0.035, 0.1), 'lager', 'blue', False, 'Northfield lager'), ('btl_fizzo', 23, MIXER, 'glass_clear', (0.03, 0.08), 'cola', 'red', False, 'Fizzo cola'),
    ('btl_tonic', 24, MIXER, 'glass_clear', (0.03, 0.08), 'spirit_clear', 'teal', False, 'Tonic water'), ('btl_zest', 25, MIXER, 'glass_clear', (0.03, 0.08), 'lemon', 'green', False, 'Lemonade'),
    ('btl_orchard', 26, MIXER, 'glass_clear', (0.03, 0.08), 'orange_juice', 'orange', False, 'Orange juice'), ('btl_tropi', 27, MIXER, 'glass_clear', (0.03, 0.08), 'crisp', 'green', False, 'Pineapple juice'),
    ('btl_bitters', 28, SMALL, 'glass_amber', (0.03, 0.09), 'whisky', 'yellow', False, 'Bitters'), ('btl_grenadine', 29, SMALL, 'glass_clear', (0.03, 0.09), 'cherry', 'red', False, 'Grenadine'),
    ('btl_tequila', 30, GIN, 'glass_clear', (0.06, 0.15), 'spirit_clear', 'orange', False, 'Tequila'), ('btl_amaretto', 31, LIQ, 'glass_amber', (0.06, 0.13), 'whisky', 'gold', False, 'Amaretto'),
]
for nm, li, pr, gl, band, lq, cp, sq, lab in BLIST: bottle(nm, li, pr, gl, band, lq, cp, lab, square=sq, mass=1.2 if pr not in (BEERB, MIXER, SMALL) else 0.45)

# ===================================================================================== bar kit
for i in range(8):
    p = prop(f'beermat{i}', 'pickup', 0.01, 'Beer mat')
    if i % 2 == 0: p.cyl((0, 0, 0), 0.05, 0.0025, f'mat{i}', 20, True)
    else: p.box((0, 0, 0.00125), (0.095, 0.095, 0.0025), f'mat{i}', top=f'mat{i}')
p = prop('tray', 'pickup', 0.4, 'Drinks tray')
p.lathe([(0.0, 0.0), (0.17, 0.0), (0.178, 0.004), (0.18, 0.018), (0.172, 0.018), (0.17, 0.006), (0.0, 0.006)], 'steel', 28); p.disc((0, 0, 0.0065), 0.169, 'cork', 24)
p = prop('font3', 'use', 8, 'Pull a pint', action='pour')   # T-bar font: three taps with lit badges, on the bar top
p.cyl((0, 0, 0), 0.04, 0.02, 'chrome', 14); p.cyl((0, 0, 0.02), 0.03, 0.34, 'chrome', 14)
p.push((0, 0, 0.36)); p.tube([(-0.22, 0, 0), (0.22, 0, 0)], 0.03, 'chrome', 14); p.pop()
for k in range(3):
    x = -0.15 + k * 0.15
    p.cyl((x, -0.03, 0.33), 0.009, 0.03, 'chrome', 8, rtop=0.007); p.cyl((x, -0.03, 0.27), 0.007, 0.06, 'chrome', 8)    # spout
    p.push((x, -0.045, 0.405), rx=-0.15); p.cyl((0, 0, -0.04), 0.012, 0.12, 'black_gloss', 10, rtop=0.016); p.pop()       # handle
    p.push((x, -0.035, 0.47), rx=math.pi / 2); p.lathe([(0.045, 0.0), (0.045, 0.008)], 'brass', 18); p.disc((0, 0, 0.008), 0.045, f'badge{k}', 18); p.pop()
p = prop('font_python', 'use', 4, 'Pull a pint', action='pour')
p.cyl((0, 0, 0), 0.035, 0.02, 'chrome', 14); p.lathe([(0.03, 0.02), (0.03, 0.3), (0.05, 0.33), (0.05, 0.42), (0.0, 0.44)], 'chrome', 16)
p.push((0, -0.05, 0.38), rx=math.pi / 2); p.lathe([(0.0, 0.0), (0.045, 0.0), (0.045, 0.006)], 'brass', 18, cap_top='badge3'); p.pop()
p.cyl((0, -0.03, 0.22), 0.008, 0.06, 'chrome', 8); p.push((0, -0.04, 0.3), rx=-0.2); p.cyl((0, 0, 0), 0.012, 0.11, 'black_gloss', 10); p.pop()
p = prop('drip_tray', 'static', 1, '')
p.rbox((0, 0, 0.008), (0.5, 0.14, 0.016), 0.004, 'steel', 2); p.box((0, 0, 0.0165), (0.46, 0.11, 0.001), 'grey_dk')
for k in range(12): p.box((-0.22 + k * 0.04, 0, 0.017), (0.006, 0.11, 0.002), 'chrome')
p = prop('till', 'use', 6, 'Use the till', action='till')
p.rbox((0, 0.04, 0.05), (0.42, 0.42, 0.1), 0.01, 'charcoal', 2); p.box((0, -0.171, 0.05), (0.36, 0.01, 0.06), 'grey_dk')
p.cyl((0, 0.05, 0.1), 0.05, 0.02, 'charcoal', 12); p.cyl((0, 0.05, 0.12), 0.015, 0.14, 'charcoal', 8)
p.push((0, 0.04, 0.32), rx=-0.35); p.rbox((0, 0, 0), (0.34, 0.03, 0.26), 0.01, 'black_gloss', 2, front='till'); p.pop()
p = prop('cardmachine', 'pickup', 0.3, 'Card machine')
p.rbox((0, 0, 0.012), (0.075, 0.16, 0.024), 0.008, 'black', 2, top='cardterm'); p.box((0, 0.03, 0.0245), (0.06, 0.07, 0.001), 'grey_lt')
p = prop('icebucket', 'pickup', 1.2, 'Ice bucket'); p.lathe([(0.0, 0.0), (0.09, 0.0), (0.1, 0.02), (0.11, 0.2), (0.105, 0.2), (0.085, 0.02)], 'steel', 20)
ice(p, 9, 0.08, 0.13, 7)
p = prop('fruit_tray', 'static', 1, '')
p.rbox((0, 0, 0.02), (0.3, 0.1, 0.04), 0.005, 'plastic_black', 2)
for k in range(3):
    for q in range(5): p.push((-0.1 + k * 0.1 + (q % 2) * 0.01, -0.02 + q * 0.01, 0.035 + q * 0.004), rx=1.3, rz=q); p.cyl((0, 0, -0.003), 0.018, 0.006, ['lemon', 'lime', 'orange'][k], 10, True, True); p.pop()
p = prop('straws', 'pickup', 0.3, 'Straw holder'); p.cyl((0, 0, 0), 0.035, 0.12, 'glass_clear', 14)
for k in range(14): a = k * 2.4; p.push((math.cos(a) * 0.02, math.sin(a) * 0.02, 0.0), rx=math.sin(a) * 0.08, ry=math.cos(a) * 0.08); p.cyl((0, 0, 0.0), 0.003, 0.2, ['red', 'black', 'blue'][k % 3], 5); p.pop()
p = prop('napkins', 'pickup', 0.3, 'Napkins'); p.box((0, 0, 0.04), (0.14, 0.06, 0.08), 'paper'); p.rbox((0, 0, 0.03), (0.15, 0.07, 0.06), 0.004, 'chrome', 2)
p = prop('bell', 'use', 1, 'Ring the bell', action='bell')
p.box((0, 0.02, 0.15), (0.02, 0.02, 0.3), 'brass'); p.tube([(0, 0.02, 0.28), (0, -0.08, 0.28)], 0.008, 'brass', 8)
p.lathe([(0.0, 0.17), (0.02, 0.18), (0.045, 0.2), (0.05, 0.25), (0.03, 0.27), (0.0, 0.272)], 'brass', 16, c=(0, -0.07, 0))
p = prop('shaker', 'pickup', 0.4, 'Cocktail shaker'); p.lathe([(0.0, 0.0), (0.04, 0.0), (0.045, 0.12), (0.035, 0.15), (0.03, 0.19), (0.015, 0.21), (0.012, 0.23), (0.0, 0.235)], 'chrome', 18)
p = prop('peanuts_card', 'pickup', 0.3, 'Bag of peanuts', food='peanuts'); p.box((0, 0, 0.25), (0.25, 0.005, 0.5), 'peanuts', front='peanuts')
for k, nm in enumerate(['crisps_ready', 'crisps_salt', 'crisps_cheese', 'scratchings']):
    p = prop(nm, 'pickup', 0.05, ['Ready salted crisps', 'Salt & vinegar crisps', 'Cheese & onion crisps', 'Pork scratchings'][k], food='crisps')
    p.rbox((0, 0, 0.07), (0.12, 0.035, 0.15), 0.012, f'crisps{k}', 2, front=f'crisps{k}')
p = prop('crisp_box', 'static', 2, '')
p.box((0, 0, 0.1), (0.36, 0.26, 0.2), 'cardboard', front='box2', skip=('top',))
for k in range(9): p.push((-0.12 + (k % 3) * 0.12, -0.06 + (k // 3) * 0.06, 0.12), rx=-0.4); p.rbox((0, 0, 0.07), (0.11, 0.03, 0.14), 0.012, f'crisps{k % 4}', 1, front=f'crisps{k % 4}'); p.pop()
p = prop('bar_mat', 'static', 1, ''); p.rbox((0, 0, 0.005), (0.45, 0.25, 0.01), 0.004, 'rubber', 1)
for k in range(10): p.box((-0.2 + k * 0.045, 0, 0.0105), (0.02, 0.2, 0.002), 'rubber_grey')
p = prop('glass_rack', 'pickup', 4, 'Rack of glasses')
p.rbox((0, 0, 0.04), (0.5, 0.5, 0.08), 0.01, 'plastic_grey', 1)
for k in range(16): x, y = -0.18 + (k % 4) * 0.12, -0.18 + (k // 4) * 0.12; p.push((x, y, 0.23), rx=math.pi); glass_shell(p, PINT, segs=10); p.pop()

# coffee machine: two-group espresso, chrome, cups warming on top
p = prop('coffee_machine', 'use', 40, 'Make a coffee', action='coffee')
p.rbox((0, 0.02, 0.2), (0.7, 0.5, 0.4), 0.03, 'chrome', 2); p.box((0, -0.231, 0.27), (0.6, 0.01, 0.16), 'black_gloss')
p.rbox((0, 0.02, 0.44), (0.7, 0.5, 0.08), 0.02, 'black_gloss', 2)
for k in range(2):
    x = -0.15 + k * 0.3; p.cyl((x, -0.25, 0.27), 0.04, 0.05, 'chrome', 14); p.push((x, -0.3, 0.235)); p.tube([(0, 0, 0), (0, -0.12, -0.01)], 0.01, 'black', 8); p.pop()
    p.cyl((x, -0.25, 0.235), 0.035, 0.03, 'chrome', 14)
for k in range(4): p.box((-0.27 + k * 0.18, -0.23, 0.36), (0.05, 0.01, 0.03), 'led_amber' if k % 2 else 'led_green')
p.box((0, -0.15, 0.05), (0.6, 0.2, 0.02), 'steel'); p.tube([(0.3, -0.2, 0.32), (0.32, -0.24, 0.2), (0.32, -0.26, 0.12)], 0.006, 'chrome', 6)
for k in range(6):
    p.push((-0.25 + k * 0.1, 0.0, 0.48)); p.lathe([(0.0, 0.0), (0.022, 0.0), (0.03, 0.01), (0.04, 0.06)], 'white_gloss', 12); p.pop()
p = prop('glass_washer', 'use', 50, 'Open the glass washer', action='open')
p.box((0, 0, 0.42), (0.6, 0.6, 0.84), 'steel', front='brushed_steel'); p.box((0, -0.302, 0.78), (0.56, 0.005, 0.06), 'black'); p.box((-0.2, -0.306, 0.78), (0.06, 0.004, 0.02), 'led_green')
p.tube([(-0.24, -0.33, 0.66), (0.24, -0.33, 0.66)], 0.01, 'steel', 8)
p = prop('fridge_bottles', 'use', 60, 'Open the fridge', action='fridge')   # back-bar bottle cooler, two glass doors, lit, full of bottles
W, D_, H_ = 0.9, 0.55, 0.86
p.box((0, 0, H_ / 2), (W, D_, H_), 'black', skip=('front',)); p.box((0, 0.05, H_ / 2), (W - 0.06, D_ - 0.1, H_ - 0.08), 'fridge_glow', skip=('front', 'top', 'bottom'))
for zz in (0.05, 0.31, 0.57): p.box((0, 0.05, zz), (W - 0.08, D_ - 0.12, 0.01), 'chrome')
names = [b for b in BLIST if b[2] in (BEERB, MIXER)]
for row, zz in enumerate((0.06, 0.32, 0.58)):
    for k in range(9):
        nm, li, pr, gl, band, lq, cp, sq, lab = names[(row * 9 + k) % len(names)]
        p.push((-0.38 + k * 0.095, -0.12, zz)); p.lathe(pr, gl, 10); rl = rband(pr, band) + 0.001
        p.lathe([(rl, band[0]), (rl, band[1])], f'label{li}', 10, -math.pi, 0.0); p.cyl((0, 0, pr[-1][1]), pr[-1][0] + 0.002, 0.012, cp, 8); p.pop()
for k in range(2):
    x0 = -W / 2 + 0.01 + k * (W / 2); p.box((x0 + W / 4 - 0.005, -D_ / 2 - 0.015, H_ / 2), (W / 2 - 0.03, 0.006, H_ - 0.06), 'glass_clear')
    p.box((x0 + W / 4 - 0.005, -D_ / 2 - 0.02, 0.02), (W / 2 - 0.03, 0.02, 0.03), 'black'); p.box((x0 + W / 4 - 0.005, -D_ / 2 - 0.02, H_ - 0.025), (W / 2 - 0.03, 0.02, 0.04), 'black')
    p.box((x0 + 0.015, -D_ / 2 - 0.02, H_ / 2), (0.03, 0.02, H_), 'black'); p.box((x0 + W / 2 - 0.025, -D_ / 2 - 0.02, H_ / 2), (0.03, 0.02, H_), 'black')
    p.box((x0 + (W / 2 - 0.06 if k == 0 else 0.05), -D_ / 2 - 0.04, H_ / 2), (0.015, 0.02, 0.3), 'chrome')

# optics: a wall bracket with four bottles upside down on measures
p = prop('optics4', 'use', 6, 'Pour a shot', action='optic')
p.box((0, 0.06, 0.32), (0.62, 0.02, 0.12), 'chrome'); p.box((0, 0.06, 0.06), (0.62, 0.02, 0.06), 'chrome')
for k in range(4):
    nm, li, pr, gl, band, lq, cp, sq, lab = BLIST[[0, 4, 6, 8][k]]
    x = -0.225 + k * 0.15
    p.cyl((x, 0.0, 0.02), 0.015, 0.07, 'glass_clear', 10); p.cyl((x, 0.0, 0.0), 0.006, 0.03, 'chrome', 8); p.box((x, 0.03, 0.06), (0.04, 0.06, 0.03), 'chrome')
    p.push((x, 0.0, 0.42), rx=math.pi)
    if sq: p.rbox((0, 0, 0.12), (0.08, 0.056, 0.2), 0.01, gl, 1); p.box((0, -0.03, 0.12), (0.066, 0.002, 0.09), f'label{li}', skip=('back',)); p.cyl((0, 0, 0.22), 0.016, 0.07, gl, 10)
    else: p.lathe([(r, z) for r, z in pr], gl, 14); rl = rband(pr, band) + 0.001; p.lathe([(rl, band[0]), (rl, band[1])], f'label{li}', 14, 0.15, math.pi - 0.15)
    p.pop()

# ===================================================================================== bar furniture and seating
p = prop('bar_counter', 'static', 300, '')    # one metre of counter: panelled walnut front, brass kick and foot rail, marble top, back shelf
L = 1.0; p.box((0, 0.05, 0.53), (L, 0.5, 1.0), 'walnut', front='walnut', skip=('top', 'left', 'right'))
for k in range(2): x = -0.25 + k * 0.5; p.rbox((x, -0.205, 0.55), (0.4, 0.02, 0.62), 0.006, 'walnut', 1, front='walnut'); p.box((x, -0.216, 0.55), (0.36, 0.004, 0.58), 'mahogany')
p.box((0, -0.2, 0.06), (L, 0.04, 0.12), 'brass_dull'); p.rbox((0, -0.05, 1.05), (L + 0.001, 0.72, 0.05), 0.02, 'marble_bk', 2)
p.box((0, -0.41, 1.012), (L, 0.006, 0.02), 'brass')
p.tube([(-L / 2, -0.33, 0.24), (L / 2, -0.33, 0.24)], 0.025, 'brass', 12, caps=False); p.box((0, -0.27, 0.24), (0.04, 0.12, 0.03), 'brass')
p.box((0, 0.27, 0.85), (L, 0.04, 0.02), 'steel'); p.box((0, 0.2, 0.55), (L, 0.3, 0.02), 'formica'); p.box((0, 0.2, 0.2), (L, 0.3, 0.02), 'formica')
p.tube([(-L / 2, 0.32, 0.6), (L / 2, 0.32, 0.6)], 0.008, 'steel', 6, caps=False)
p = prop('bar_end', 'static', 150, '')        # the rounded end of the counter
p.lathe([(0.45, 0.0), (0.45, 1.0)], 'walnut', 16, -math.pi / 2, math.pi / 2, c=(0, 0.05, 0.03))
p.lathe([(0.0, 1.025), (0.5, 1.025), (0.52, 1.05), (0.5, 1.075), (0.0, 1.075)], 'marble_bk', 16, -math.pi / 2, math.pi / 2, c=(0, -0.05, 0))
p.torus((0, 0.05, 0.24), 0.57, 0.025, 'brass', 16, 8, -math.pi / 2, math.pi / 2)
def back_bar(name, cupboards=True, shelves=True):
    """One metre of back bar: cupboards (or open legs for a fridge underneath), worktop, mirrored back, lit glass shelves."""
    p = prop(name, 'static', 200, '')
    if cupboards:
        p.box((0, 0, 0.45), (1.0, 0.55, 0.9), 'walnut', front='walnut', skip=('left', 'right'))
        for k in range(2): p.rbox((-0.25 + k * 0.5, -0.28, 0.45), (0.46, 0.02, 0.78), 0.006, 'walnut', 1); p.box((-0.07 + k * 0.14, -0.3, 0.75), (0.012, 0.02, 0.1), 'brass')
    else:
        for x in (-0.47, 0.47): p.box((x, 0, 0.45), (0.06, 0.55, 0.9), 'walnut')
        p.box((0, 0.26, 0.45), (1.0, 0.03, 0.9), 'black_matt')
    p.rbox((0, 0, 0.92), (1.0, 0.58, 0.04), 0.01, 'marble_bk', 1)
    p.box((0, 0.22, 1.75), (1.0, 0.02, 1.6), 'glass_smoke'); p.box((0, 0.24, 1.75), (1.0, 0.02, 1.6), 'black_gloss')
    if shelves:
        for zz in (1.3, 1.7, 2.1): p.box((0, 0.1, zz), (1.0, 0.24, 0.012), 'glass_clear'); p.box((0, -0.02, zz - 0.008), (1.0, 0.006, 0.006), 'led_white')
    else: p.box((0, 0.2, 1.62), (1.0, 0.03, 0.03), 'chrome')
    p.box((-0.495, 0.1, 1.75), (0.01, 0.26, 1.6), 'walnut'); p.box((0, 0.1, 2.58), (1.0, 0.3, 0.06), 'walnut'); p.box((0, -0.03, 2.53), (1.0, 0.02, 0.03), 'brass')
back_bar('back_bar'); back_bar('back_bar_open', cupboards=False); back_bar('back_bar_optics', shelves=False)
p = prop('bar_stool', 'sit', 8, 'Sit at the bar', seat=0.78)
p.lathe([(0.0, 0.0), (0.22, 0.0), (0.22, 0.012), (0.2, 0.02), (0.05, 0.03), (0.0, 0.03)], 'chrome', 22)
p.cyl((0, 0, 0.03), 0.03, 0.7, 'chrome', 12); p.torus((0, 0, 0.32), 0.19, 0.012, 'chrome', 20, 6)
for k in range(4): a = k * math.pi / 2; p.tube([(0, 0, 0.32), (math.cos(a) * 0.19, math.sin(a) * 0.19, 0.32)], 0.008, 'chrome', 6)
p.lathe([(0.0, 0.7), (0.17, 0.7), (0.19, 0.72), (0.2, 0.75), (0.195, 0.78), (0.17, 0.795), (0.0, 0.8)], 'leather_quilt', 22, cap_top=False)
p.lathe([(0.16, 0.795), (0.0, 0.8)], 'leather_quilt', 22); p.disc((0, 0, 0.8), 0.001, 'leather_quilt', 6)
p.push((0, 0.17, 0.0)); p.lathe([(0.15, 0.8), (0.16, 0.9), (0.15, 0.98), (0.13, 0.99)], 'leather_quilt', 14, math.pi * 0.15, math.pi * 0.85); p.pop()
p = prop('slot_stool', 'sit', 6, 'Sit down', seat=0.62)
p.lathe([(0.0, 0.0), (0.2, 0.0), (0.2, 0.01), (0.04, 0.03)], 'charcoal', 18); p.cyl((0, 0, 0.02), 0.028, 0.55, 'chrome', 10)
p.torus((0, 0, 0.26), 0.16, 0.01, 'chrome', 18, 6)
p.lathe([(0.0, 0.55), (0.18, 0.55), (0.2, 0.58), (0.19, 0.62), (0.15, 0.635), (0.0, 0.64)], 'leather_red', 20)
p = prop('casino_chair', 'sit', 9, 'Sit down', seat=0.62)   # padded table chair, gold legs, studded back
for x in (-0.2, 0.2):
    p.tube([(x, -0.2, 0.0), (x * 1.02, -0.2, 0.5)], 0.016, 'gold', 8); p.tube([(x, 0.2, 0.0), (x, 0.21, 0.5), (x * 0.95, 0.24, 1.0)], 0.016, 'gold', 8)
p.rbox((0, 0, 0.56), (0.48, 0.48, 0.12), 0.04, 'upholstery', 3, top='velvet')
p.push((0, 0.22, 0.75), rx=0.12); p.rbox((0, 0, 0.12), (0.46, 0.08, 0.5), 0.035, 'velvet', 3); p.tube([(-0.23, -0.0, 0.38), (0.23, 0.0, 0.38)], 0.012, 'gold', 8); p.pop()
p = prop('tub_chair', 'sit', 12, 'Sit down', seat=0.45)
p.lathe([(0.0, 0.0), (0.3, 0.0), (0.32, 0.05), (0.32, 0.4), (0.0, 0.4)], 'leather_red', 20)
p.lathe([(0.32, 0.4), (0.34, 0.45), (0.35, 0.7), (0.33, 0.74), (0.28, 0.72), (0.27, 0.45)], 'leather_red', 20, math.pi * -0.1, math.pi * 1.1)
p.rbox((0, -0.02, 0.45), (0.5, 0.48, 0.1), 0.04, 'leather_red', 2)
p = prop('round_table', 'static', 25, '')
p.lathe([(0.0, 0.0), (0.26, 0.0), (0.26, 0.02), (0.05, 0.05)], 'brass_dull', 20); p.cyl((0, 0, 0.04), 0.035, 0.68, 'brass', 12)
p.lathe([(0.0, 0.7), (0.38, 0.7), (0.4, 0.72), (0.4, 0.74), (0.38, 0.76), (0.0, 0.76)], 'walnut', 26, cap_top='walnut')
p = prop('booth', 'sit', 80, 'Sit in the booth', seat=0.46)   # 1.8 m booth seat, button-back velvet
p.box((0, 0.05, 0.2), (1.8, 0.55, 0.4), 'walnut', front='walnut'); p.rbox((0, -0.03, 0.43), (1.78, 0.56, 0.12), 0.04, 'velvet', 3)
p.push((0, 0.3, 0.5), rx=0.12)
for k in range(6): p.rbox((-0.75 + k * 0.3, 0, 0.32), (0.3, 0.14, 0.62), 0.05, 'velvet', 3)
p.pop(); p.box((0, 0.42, 0.6), (1.8, 0.1, 1.2), 'walnut'); p.box((0, 0.4, 1.21), (1.82, 0.16, 0.04), 'brass')
p = prop('booth_table', 'static', 30, '')
p.box((0, 0, 0.36), (0.12, 0.12, 0.72), 'brass_dull'); p.box((0, 0, 0.01), (0.5, 0.4, 0.02), 'brass_dull'); p.rbox((0, 0, 0.74), (1.2, 0.6, 0.04), 0.015, 'walnut', 2, top='walnut')

# ===================================================================================== gaming floor
def slot_machine(i):
    p = prop(f'slot{i}', 'use', 120, 'Play the machine', action='slots', game=i)
    cab = ['black_gloss', 'charcoal', 'burgundy', 'navy'][i % 4]; edge = ['led_red', 'led_blue', 'led_purple', 'led_amber', 'led_green', 'led_white', 'led_amber', 'led_purple'][i]
    p.rbox((0, 0.02, 0.05), (0.72, 0.62, 0.1), 0.01, 'black', 1)                     # plinth
    p.rbox((0, 0.04, 0.48), (0.7, 0.56, 0.78), 0.03, cab, 2)                       # lower cabinet
    p.box((0, -0.241, 0.45), (0.5, 0.004, 0.4), 'black_matt'); p.box((0, -0.244, 0.6), (0.18, 0.004, 0.06), 'chrome')        # door, ticket mouth
    p.push((0, -0.24, 0.9), rx=-0.32); p.rbox((0, -0.08, 0), (0.7, 0.24, 0.07), 0.02, cab, 2, top=f'deck{i % 2}'); p.pop()    # button deck
    p.push((0, 0.06, 0.95), rx=0.1)                                                 # the screen, leaning back a touch, in a chrome bezel
    p.rbox((0, 0, 0.42), (0.66, 0.2, 0.84), 0.025, cab, 2); p.rbox((0, -0.098, 0.42), (0.6, 0.012, 0.78), 0.012, 'chrome', 2)
    p.panel(-0.27, 0.06, 0.27, 0.78, -0.106, f'slot{i}')
    p.pop()
    p.push((0, 0.06, 1.82)); p.rbox((0, 0, 0.14), (0.72, 0.32, 0.28), 0.05, cab, 3); p.panel(-0.33, 0.03, 0.33, 0.25, -0.161, f'topper{i}'); p.pop()   # topper
    for x in (-0.352, 0.352): p.box((x, -0.12, 1.1), (0.012, 0.03, 1.5), edge)       # LED edges
    for x in (-0.24, 0.24): p.push((x, -0.24, 0.24), rx=math.pi / 2); p.cyl((0, 0, 0), 0.05, 0.004, 'black_matt', 12, True); p.pop()   # speakers
    p.box((0.26, -0.33, 0.97), (0.1, 0.01, 0.04), 'black')                         # note acceptor
    return p
for i in range(8): slot_machine(i)

def chips(p, x, y, z, n, val, r=0.0195):
    for k in range(n): p.cyl((x, y, z + k * 0.0034), r, 0.0033, 'chip%d' % val if k == n - 1 else ['white', 'red', 'green', 'black_gloss', 'purple', 'yellow'][[1, 5, 25, 100, 500, 1000].index(val)], 12, True)
    p.disc((x, y, z + n * 0.0034 + 0.0002), r, f'chip{val}', 12)
p = prop('chip_stacks', 'pickup', 0.6, 'Pile of chips', value=150)
for k, v in enumerate([5, 25, 25, 100, 1]): chips(p, (k % 3) * 0.042 - 0.04, (k // 3) * 0.042, 0, 6 + k * 3, v)
p = prop('chip_float', 'static', 6, '')       # dealer's chip tray full of colour-sorted chips
p.rbox((0, 0, 0.02), (0.56, 0.18, 0.04), 0.008, 'glass_smoke', 1)
for k in range(10):
    v = [1, 5, 5, 25, 25, 25, 100, 100, 500, 1000][k]; x = -0.25 + k * 0.055
    p.push((x, 0, 0.032), rx=math.pi / 2)
    for q in range(16): p.cyl((0, 0.0, -0.06 + q * 0.0037), 0.0195, 0.0034, ['white', 'red', 'green', 'black_gloss', 'purple', 'yellow'][[1, 5, 25, 100, 500, 1000].index(v)], 10, False)
    p.pop()
p = prop('card_shoe', 'pickup', 1.5, 'Card shoe')
p.rbox((0, 0, 0.05), (0.13, 0.3, 0.1), 0.008, 'glass_smoke', 1); p.box((0, 0.02, 0.05), (0.09, 0.22, 0.08), 'cardback')
p.push((0, -0.15, 0.03), rx=-0.5); p.box((0, 0, 0), (0.12, 0.02, 0.06), 'black_gloss'); p.pop()
p = prop('cards', 'pickup', 0.1, 'Deck of cards'); p.box((0, 0, 0.0085), (0.063, 0.088, 0.017), 'white', top='cardback')
for k in range(4): p.push((-0.12 + k * 0.08, 0.15, 0.0), rz=(k - 1.5) * 0.08); p.box((0, 0, 0.0006), (0.063, 0.088, 0.0012), 'white', top=f'card{k}'); p.pop()
p = prop('limit_sign', 'static', 1, '')
p.cyl((0, 0, 0), 0.05, 0.01, 'chrome', 12); p.cyl((0, 0, 0.01), 0.008, 0.2, 'chrome', 8); p.rbox((0, 0, 0.27), (0.2, 0.04, 0.1), 0.006, 'black_gloss', 1, front='limit0')

def table_rail(p, pts2d, z, skin='leather_quilt', r=0.05):
    rows = []
    n = len(pts2d)
    for k in range(9):
        a = math.pi * k / 8; dx = -math.cos(a) * r; dz = math.sin(a) * r * 0.9
        row = []
        for j, (x, y, nx, ny) in enumerate(pts2d): row.append(((x + nx * (r + dx), y + ny * (r + dx), z + dz), (j / (n - 1), k / 8)))
        rows.append(row)
    p.grid(rows, skin, True)
p = prop('roulette_table', 'use', 300, 'Play roulette', action='roulette')
TL, TW = 3.0, 1.3
for x in (-1.0, 0.9): p.lathe([(0.0, 0.0), (0.28, 0.0), (0.28, 0.03), (0.1, 0.06), (0.09, 0.7)], 'walnut', 16, c=(x, 0, 0))
p.rbox((0, 0, 0.74), (TL, TW, 0.1), 0.02, 'walnut', 2)
p.box((0.45, 0, 0.792), (2.0, TW - 0.2, 0.004), 'felt_green', top='roulette_felt')
p.box((-1.0, 0, 0.792), (0.9, TW - 0.2, 0.004), 'felt_plain')
rail = []
for k in range(41):
    t = k / 40; x = -TL / 2 + 0.05 + t * (TL - 0.1); rail.append((x, -TW / 2 + 0.05, 0, -1))
table_rail(p, rail, 0.8)
rail2 = [(TL / 2 - 0.05, -TW / 2 + 0.05 + k / 20 * (TW - 0.1), 1, 0) for k in range(21)]; table_rail(p, rail2, 0.8)
p.lathe([(0.48, 0.79), (0.5, 0.82), (0.49, 0.9), (0.45, 0.92), (0.4, 0.86), (0.38, 0.84)], 'walnut', 32, c=(-1.0, 0, 0))     # the bowl
p.disc((-1.0, 0, 0.845), 0.385, 'wheel', 37)
p.lathe([(0.06, 0.845), (0.05, 0.88), (0.015, 0.9), (0.01, 0.98), (0.0, 1.0)], 'gold', 12, c=(-1.0, 0, 0))
for k in range(4): a = k * math.pi / 2; p.tube([(-1.0, 0, 0.93), (-1.0 + math.cos(a) * 0.1, math.sin(a) * 0.1, 0.92), (-1.0 + math.cos(a) * 0.13, math.sin(a) * 0.13, 0.9)], 0.006, 'gold', 6)
p.lathe([(0.0, 0.795), (0.012, 0.795), (0.012, 0.83), (0.018, 0.84), (0.0, 0.85)], 'brass', 10, c=(0.6, 0.1, 0))     # the dolly
for k, (x, y, n, v) in enumerate([(0.2, -0.2, 6, 5), (0.5, 0.3, 4, 25), (0.9, -0.1, 8, 1), (-0.1, 0.2, 5, 100), (1.1, 0.25, 3, 5)]): chips(p, x, y, 0.794, n, v)
p = prop('blackjack_table', 'use', 220, 'Play blackjack', action='blackjack')
R = 1.25
p.lathe([(0.0, 0.0), (0.4, 0.0), (0.4, 0.03), (0.12, 0.07), (0.1, 0.72)], 'walnut', 18, c=(0, 0.1, 0))
seg = 24; arc = [(math.cos(math.pi + math.pi * k / seg) * R, 0.35 + math.sin(math.pi + math.pi * k / seg) * R * 0.85) for k in range(seg + 1)]
rows = [[((x, y, 0.79), ((x / R + 1) / 2, (y - 0.35) / R + 1)) for x, y in arc], [((x * 0.0, 0.35, 0.79), (0.5, 1.0)) for x, y in arc]]
p.grid([[((x, y, 0.792), ((x / R + 1) / 2, 1 - (0.35 - y) / (R * 0.85))) for x, y in arc], [((0.0 + x * 0.02, 0.35, 0.792), (0.5 + x * 0.008, 1.0)) for x, y in arc]], 'blackjack_felt', False)
p.grid([[((x * 1.03, 0.35 + (y - 0.35) * 1.03, 0.79), (0, 0)) for x, y in arc], [((x * 1.03, 0.35 + (y - 0.35) * 1.03, 0.68), (0, 1)) for x, y in arc]], 'walnut', True)
p.grid([[((x * 1.03, 0.35 + (y - 0.35) * 1.03, 0.68), (0, 0)) for x, y in arc], [((x * 0.2, 0.35 + (y - 0.35) * 0.2, 0.68), (0, 1)) for x, y in arc]], 'walnut', False)
table_rail(p, [(x * 1.0, y, (x / R), (y - 0.35) / (R * 0.85)) for x, y in arc], 0.8)
p.box((0, 0.42, 0.75), (2.6, 0.16, 0.1), 'walnut')
p.box((0, 0.36, 0.806), (0.56, 0.18, 0.02), 'glass_smoke')
for k in range(10): v = [1, 5, 5, 25, 25, 25, 100, 100, 500, 1000][k]; chips(p, -0.25 + k * 0.055, 0.36, 0.8, 9, v, 0.0195)
p.rbox((0.55, 0.3, 0.86), (0.13, 0.3, 0.1), 0.008, 'glass_smoke', 1); p.box((0.55, 0.32, 0.86), (0.09, 0.22, 0.08), 'cardback')
for k in range(3): p.push((-0.4 + k * 0.4, -0.35, 0.795), rz=(k - 1) * 0.3); p.box((0, 0, 0.0006), (0.063, 0.088, 0.0012), 'white', top=f'card{k}'); p.box((0.02, 0.01, 0.0018), (0.063, 0.088, 0.0012), 'white', top=f'card{k + 1}'); p.pop()
p = prop('poker_table', 'use', 240, 'Sit at the poker table', action='poker')
RX, RY = 1.3, 0.65
p.lathe([(0.0, 0.0), (0.45, 0.0), (0.45, 0.03), (0.12, 0.07), (0.1, 0.72)], 'walnut', 18)
seg = 40; ov = [(math.cos(TAU * k / seg) * RX, math.sin(TAU * k / seg) * RY) for k in range(seg + 1)]
p.grid([[((x, y, 0.792), (x / (2 * RX) + 0.5, y / (2 * RY) + 0.5)) for x, y in ov], [((x * 0.01, y * 0.01, 0.792), (0.5, 0.5)) for x, y in ov]], 'poker_felt', False)
table_rail(p, [(x, y, math.cos(TAU * k / seg), math.sin(TAU * k / seg) * (RX / RY) / math.hypot(math.cos(TAU * k / seg), math.sin(TAU * k / seg) * (RX / RY))) for k, (x, y) in enumerate(ov)], 0.8)
p.grid([[((x * 1.04, y * 1.08, 0.79), (0, 0)) for x, y in ov], [((x * 1.04, y * 1.08, 0.66), (0, 1)) for x, y in ov]], 'walnut', True)
for k in range(6): chips(p, math.cos(k) * 0.7, math.sin(k) * 0.32, 0.794, 5 + k, [5, 25, 100, 1, 25, 5][k])
p = prop('etg', 'use', 400, 'Play electronic roulette', action='roulette')   # automated wheel under a dome with eight touch-screen seats
p.lathe([(0.0, 0.0), (0.7, 0.0), (0.72, 0.05), (0.7, 0.8), (0.75, 0.85), (0.0, 0.85)], 'black_gloss', 28)
p.lathe([(0.62, 0.85), (0.64, 0.88), (0.6, 0.92), (0.5, 0.94)], 'gold', 28); p.disc((0, 0, 0.9), 0.5, 'wheel', 32)
p.lathe([(0.66, 0.86), (0.66, 1.05), (0.6, 1.22), (0.45, 1.33), (0.25, 1.39), (0.0, 1.41)], 'glass_clear', 28)
p.lathe([(0.04, 0.9), (0.03, 0.97), (0.0, 1.0)], 'gold', 10)
for k in range(8):
    a = k * TAU / 8 + TAU / 16; c = (math.cos(a) * 1.35, math.sin(a) * 1.35)
    p.push((c[0], c[1], 0), rz=a + math.pi / 2)
    p.rbox((0, 0, 0.36), (0.6, 0.5, 0.72), 0.03, 'black_gloss', 2); p.push((0, 0.0, 0.78), rx=-0.5); p.rbox((0, 0, 0), (0.56, 0.46, 0.05), 0.02, 'black_gloss', 2, top='etg'); p.pop()
    p.box((0, -0.256, 0.5), (0.4, 0.01, 0.03), 'led_blue')
    p.pop()
p = prop('pit_podium', 'use', 60, 'Check the pit computer', action='pc')
p.rbox((0, 0, 0.55), (0.7, 0.5, 1.1), 0.02, 'walnut', 2); p.box((0, 0, 1.11), (0.74, 0.54, 0.02), 'brass')
p.push((0, 0.05, 1.12)); p.cyl((0, 0, 0), 0.06, 0.01, 'charcoal', 12); p.cyl((0, 0, 0.01), 0.015, 0.18, 'charcoal', 8)
p.rbox((0, 0, 0.36), (0.55, 0.03, 0.34), 0.01, 'black_gloss', 1, front='pc'); p.pop()
p = prop('stanchion', 'pickup', 6, 'Brass post')
p.lathe([(0.0, 0.0), (0.16, 0.0), (0.16, 0.015), (0.1, 0.04), (0.03, 0.06)], 'brass', 20); p.cyl((0, 0, 0.05), 0.025, 0.88, 'brass', 12)
p.lathe([(0.025, 0.93), (0.045, 0.95), (0.045, 0.98), (0.0, 1.0)], 'brass', 14)
p = prop('rope', 'static', 1, '')            # velvet rope hanging between two posts 1.2 m apart (along +X)
pts = [(-0.6 + 1.2 * k / 12, 0, 0.9 - 0.18 * math.sin(math.pi * k / 12)) for k in range(13)]; p.tube(pts, 0.018, 'velvet', 8)
for x in (-0.6, 0.6): p.cyl((x, 0, 0.88), 0.022, 0.05, 'brass', 8)
p = prop('chandelier', 'static', 40, '')     # three tiers of crystal drops on a brass frame, candle bulbs, hung from z=0 downwards
p.cyl((0, 0, -0.75), 0.012, 0.75, 'brass', 6); p.lathe([(0.0, -0.06), (0.15, -0.06), (0.15, 0.0), (0.0, 0.0)], 'brass', 16)
for tier, (rr, zz, n) in enumerate([(0.75, -1.3, 14), (0.52, -1.05, 10), (0.3, -0.85, 8)]):
    p.torus((0, 0, zz), rr, 0.015, 'brass', 24, 6)
    for k in range(n):
        a = TAU * k / n; x, y = math.cos(a) * rr, math.sin(a) * rr
        p.tube([(0, 0, zz + 0.3), (x * 0.5, y * 0.5, zz + 0.12), (x, y, zz)], 0.008, 'brass', 5, False)
        p.cyl((x, y, zz), 0.022, 0.03, 'brass', 8, True); p.cyl((x, y, zz + 0.03), 0.009, 0.07, 'wax', 6, False); p.lathe([(0.0, 0.0), (0.012, 0.015), (0.006, 0.04), (0.0, 0.05)], 'lamp_warm', 6, c=(x, y, zz + 0.1))
        for q in range(3): p.lathe([(0.0, 0.0), (0.012, -0.02), (0.0, -0.06)], 'crystal', 5, c=(x * (1 - q * 0.08), y * (1 - q * 0.08), zz - 0.03 - q * 0.07))
    for k in range(n * 2): a = TAU * (k + 0.5) / (n * 2); p.lathe([(0.0, 0.0), (0.008, -0.015), (0.0, -0.05)], 'crystal', 4, c=(math.cos(a) * rr, math.sin(a) * rr, zz - 0.02))
p.lathe([(0.0, -1.35), (0.06, -1.42), (0.03, -1.55), (0.0, -1.6)], 'crystal', 10)

# ===================================================================================== back rooms
p = prop('desk', 'static', 40, '')
p.rbox((0, 0, 0.735), (1.6, 0.8, 0.03), 0.004, 'laminate_oak', 1, top='oak')
p.box((-0.6, 0.0, 0.36), (0.4, 0.74, 0.72), 'laminate_oak', front='oak')
for k in range(3): p.box((-0.6, -0.372, 0.15 + k * 0.22), (0.36, 0.004, 0.18), 'oak'); p.box((-0.6, -0.38, 0.22 + k * 0.22), (0.12, 0.012, 0.012), 'steel')
p.box((0.78, 0, 0.36), (0.03, 0.74, 0.72), 'laminate_oak'); p.box((0.1, 0.35, 0.45), (1.3, 0.02, 0.4), 'laminate_oak')
p = prop('office_chair', 'sit', 12, 'Sit down', seat=0.5)
for k in range(5):
    a = TAU * k / 5; p.tube([(0, 0, 0.1), (math.cos(a) * 0.3, math.sin(a) * 0.3, 0.07)], 0.018, 'plastic_black', 6); p.cyl((math.cos(a) * 0.3, math.sin(a) * 0.3, 0.0), 0.03, 0.05, 'plastic_black', 8, True)
p.cyl((0, 0, 0.1), 0.025, 0.32, 'chrome', 10); p.rbox((0, 0, 0.47), (0.5, 0.48, 0.08), 0.03, 'fabric_grey', 2)
p.push((0, 0.23, 0.55), rx=0.15); p.rbox((0, 0, 0.32), (0.46, 0.06, 0.56), 0.04, 'fabric_grey', 2); p.pop()
for x in (-0.26, 0.26): p.tube([(x, 0.05, 0.47), (x, 0.0, 0.66), (x, -0.15, 0.66)], 0.015, 'plastic_black', 6)
for nm, scr, lab in [('monitor_pc', 'pc', 'Use the computer'), ('monitor_cctv', 'cctv', 'Watch the cameras')]:
    p = prop(nm, 'use', 5, lab, action=scr)
    p.rbox((0, 0.05, 0.01), (0.22, 0.18, 0.02), 0.006, 'charcoal', 1); p.cyl((0, 0.06, 0.02), 0.02, 0.2, 'charcoal', 8)
    p.rbox((0, 0.03, 0.3), (0.56, 0.04, 0.34), 0.008, 'black_gloss', 1); p.panel(-0.265, 0.145, 0.265, 0.455, 0.009, scr)
p = prop('cctv_wall', 'use', 30, 'Watch the cameras', action='cctv')   # six screens on a wall bracket (back at y=0, hung on the wall)
for k in range(6):
    x, z = -0.62 + (k % 3) * 0.62, 1.55 + (k // 3) * 0.38; p.rbox((x, -0.03, z), (0.6, 0.05, 0.36), 0.006, 'black_gloss', 1); p.panel(x - 0.28, z - 0.16, x + 0.28, z + 0.16, -0.056, 'cctv')
p.box((0, -0.005, 1.74), (1.8, 0.01, 0.06), 'steel_dark')
p = prop('pc_tower', 'static', 8, ''); p.rbox((0, 0, 0.22), (0.2, 0.45, 0.44), 0.01, 'black', 1); p.box((0, -0.226, 0.4), (0.14, 0.004, 0.04), 'grey_dk'); p.cyl((0.05, -0.228, 0.36), 0.008, 0.004, 'led_blue', 8)
p = prop('keyboard', 'pickup', 0.6, 'Keyboard'); p.rbox((0, 0, 0.012), (0.44, 0.14, 0.024), 0.006, 'black', 1)
for k in range(5): p.box((0, -0.05 + k * 0.025, 0.025), (0.4, 0.018, 0.004), 'charcoal')
p = prop('mouse', 'pickup', 0.1, 'Mouse'); p.rbox((0, 0, 0.015), (0.06, 0.1, 0.03), 0.014, 'black', 2)
p = prop('desk_phone', 'use', 1, 'Pick up the phone', action='phone'); p.rbox((0, 0, 0.03), (0.2, 0.22, 0.06), 0.02, 'black', 2, top='keypad'); p.push((0, 0.05, 0.07), rx=0.2); p.rbox((0, 0, 0), (0.2, 0.05, 0.04), 0.018, 'black', 2); p.pop()
p = prop('filing_cabinet', 'use', 40, 'Rifle through the files', action='search')
p.box((0, 0, 0.66), (0.47, 0.62, 1.32), 'locker_grey', skip=('front',))
for k in range(4): z = 0.17 + k * 0.33; p.rbox((0, -0.31, z), (0.45, 0.02, 0.31), 0.006, 'locker_grey', 1); p.box((0, -0.326, z + 0.08), (0.14, 0.014, 0.025), 'chrome'); p.box((0, -0.322, z + 0.12), (0.08, 0.004, 0.03), 'paper')
p = prop('safe', 'use', 400, 'Try the safe', action='safe')
p.rbox((0, 0, 0.45), (0.6, 0.6, 0.9), 0.02, 'safe_green', 2); p.rbox((0, -0.3, 0.45), (0.52, 0.03, 0.8), 0.01, 'safe_green', 2)
p.push((0.0, -0.32, 0.6), rx=math.pi / 2); p.lathe([(0.0, 0.0), (0.06, 0.0), (0.06, 0.025), (0.0, 0.03)], 'chrome', 20); p.pop()
p.push((0.15, -0.33, 0.45), rx=math.pi / 2)
for k in range(3): a = k * TAU / 3; p.tube([(0, 0, 0), (math.cos(a) * 0.09, math.sin(a) * 0.09, 0.03)], 0.01, 'chrome', 6)
p.pop(); p.box((0, -0.322, 0.3), (0.16, 0.005, 0.09), 'brass')
p = prop('vault_door', 'door', 2000, 'Open the vault', action='vault', code='1987', hinge=-0.88)   # round strongroom door standing up, face to -Y, hinged on its left
p.push((0, 0, 0.95), rx=math.pi / 2)            # local +Z now points out of the door's face (-Y)
p.lathe([(0.0, 0.0), (0.82, 0.0), (0.84, 0.04), (0.84, 0.22), (0.82, 0.25), (0.0, 0.25)], 'brushed_steel', 40)
p.lathe([(0.62, 0.25), (0.6, 0.275), (0.0, 0.275)], 'steel', 40)
for k in range(12): a = TAU * k / 12; p.cyl((math.cos(a) * 0.71, math.sin(a) * 0.71, 0.25), 0.035, 0.022, 'chrome', 10, True)
p.cyl((0, 0, 0.27), 0.06, 0.1, 'chrome', 12)
for k in range(4):
    a = TAU * k / 4 + 0.4; p.tube([(0, 0, 0.36), (math.cos(a) * 0.32, math.sin(a) * 0.32, 0.36)], 0.018, 'chrome', 8); p.cyl((math.cos(a) * 0.32, math.sin(a) * 0.32, 0.34), 0.03, 0.05, 'black', 8, True)
p.box((0.45, 0.4, 0.28), (0.16, 0.12, 0.03), 'grey_dk', top='keypad')
for z in (0.42, -0.42): p.rbox((-0.88, z, 0.12), (0.12, 0.2, 0.2), 0.02, 'steel_dark', 2)
p.pop()
p = prop('cash_bag', 'pickup', 2, 'Bag of cash', cash=2500)
p.rbox((0, 0, 0.14), (0.26, 0.16, 0.28), 0.06, 'cardboard', 3); p.lathe([(0.03, 0.27), (0.02, 0.32), (0.035, 0.34), (0.0, 0.36)], 'cardboard_dk', 10)
p.cyl((0, 0, 0.29), 0.034, 0.02, 'red', 10, True); p.box((0, -0.081, 0.12), (0.12, 0.002, 0.08), 'paper')
p = prop('cash_bundle', 'pickup', 1, 'Bundle of twenties', cash=1000)
p.box((0, 0, 0.03), (0.15, 0.075, 0.06), 'notes_edge', top='note20'); p.box((0, 0, 0.03), (0.03, 0.077, 0.062), 'white')
p = prop('cash_brick', 'pickup', 4, 'Brick of fifties', cash=5000)
for k in range(4): p.box(((k % 2) * 0.155 - 0.0775, 0, 0.03 + (k // 2) * 0.06), (0.15, 0.075, 0.06), 'notes_edge', top='note50')
p.box((0, 0, 0.06), (0.32, 0.08, 0.02), 'glass_frost')
p = prop('cash_box', 'pickup', 3, 'Cash box', cash=400)
p.rbox((0, 0, 0.045), (0.3, 0.24, 0.09), 0.006, 'red', 1); p.box((0, -0.121, 0.06), (0.06, 0.004, 0.03), 'chrome'); p.box((0, 0, 0.091), (0.12, 0.03, 0.006), 'black')
p = prop('note_counter', 'use', 6, 'Count the money', action='count')
p.rbox((0, 0, 0.09), (0.3, 0.26, 0.18), 0.02, 'plastic_white', 2); p.push((0, 0.05, 0.18), rx=-0.6); p.box((0, 0, 0.0), (0.18, 0.08, 0.02), 'note20'); p.pop()
p.box((0, -0.131, 0.11), (0.12, 0.004, 0.04), 'led_green'); p.box((0, -0.12, 0.03), (0.2, 0.06, 0.03), 'note20')
p = prop('cash_trolley', 'pickup', 30, 'Cash trolley')
for x in (-0.3, 0.3):
    for y in (-0.22, 0.22): p.cyl((x, y, 0.0), 0.04, 0.03, 'rubber', 8, True); p.cyl((x, y, 0.06), 0.015, 0.94, 'chrome', 6)
for z in (0.12, 0.55, 1.0): p.box((0, 0, z), (0.64, 0.48, 0.015), 'steel')
for k in range(5): p.push((-0.2 + (k % 3) * 0.2, -0.1 + (k // 3) * 0.18, 0.565)); p.rbox((0, 0, 0.14), (0.24, 0.15, 0.27), 0.06, 'cardboard', 2); p.pop()
p = prop('shelving', 'static', 30, '')       # steel racking 1.2 m wide, four shelves
for x in (-0.58, 0.58):
    for y in (-0.22, 0.22): p.box((x, y, 1.0), (0.035, 0.035, 2.0), 'steel_dark')
for z in (0.12, 0.6, 1.08, 1.56, 1.98): p.box((0, 0, z), (1.2, 0.48, 0.025), 'steel')
p = prop('lockers', 'use', 80, 'Open a locker', action='locker')
for k in range(4):
    x = -0.45 + k * 0.3; p.box((x, 0, 0.95), (0.3, 0.45, 1.9), 'locker_blue', skip=('front',))
    p.rbox((x, -0.225, 0.95), (0.28, 0.012, 1.86), 0.004, 'locker_blue', 1)
    for q in range(6): p.box((x, -0.232, 1.6 + q * 0.03), (0.16, 0.004, 0.012), 'charcoal')
    p.box((x + 0.1, -0.236, 1.0), (0.025, 0.01, 0.1), 'chrome'); p.box((x, -0.233, 1.75), (0.1, 0.004, 0.04), 'paper')
p.box((0, 0, 1.91), (1.2, 0.45, 0.02), 'locker_blue')
p = prop('sofa', 'sit', 40, 'Sit on the sofa', seat=0.45)
p.rbox((0, 0, 0.2), (1.8, 0.8, 0.3), 0.05, 'fabric_blue', 2)
for k in range(3): p.rbox((-0.55 + k * 0.55, -0.05, 0.4), (0.54, 0.66, 0.14), 0.06, 'fabric_blue', 3)
p.rbox((0, 0.33, 0.6), (1.8, 0.2, 0.55), 0.08, 'fabric_blue', 3)
for x in (-0.87, 0.87): p.rbox((x, 0, 0.45), (0.2, 0.8, 0.35), 0.08, 'fabric_blue', 3)
for x in (-0.8, 0.8):
    for y in (-0.33, 0.33): p.cyl((x, y, 0.0), 0.025, 0.05, 'brown_dk', 6, True)
p = prop('staff_table', 'static', 20, ''); p.rbox((0, 0, 0.72), (1.2, 0.8, 0.03), 0.01, 'formica', 1)
for x in (-0.55, 0.55):
    for y in (-0.35, 0.35): p.cyl((x, y, 0.0), 0.02, 0.71, 'chrome', 8)
p = prop('stack_chair', 'sit', 4, 'Sit down', seat=0.45)
for x in (-0.2, 0.2):
    p.tube([(x, -0.2, 0.0), (x, -0.2, 0.44), (x, 0.2, 0.44), (x, 0.22, 0.0)], 0.011, 'chrome', 6); p.tube([(x, 0.2, 0.44), (x, 0.24, 0.85)], 0.011, 'chrome', 6)
p.rbox((0, 0, 0.46), (0.44, 0.44, 0.03), 0.01, 'plastic_blue', 1); p.push((0, 0.23, 0.72), rx=0.12); p.rbox((0, 0, 0), (0.42, 0.03, 0.26), 0.012, 'plastic_blue', 1); p.pop()
p = prop('kettle', 'use', 1.4, 'Put the kettle on', action='kettle')
p.cyl((0, 0, 0), 0.085, 0.015, 'plastic_black', 14); p.lathe([(0.0, 0.015), (0.08, 0.015), (0.085, 0.05), (0.075, 0.2), (0.05, 0.23), (0.0, 0.235)], 'white_gloss', 18)
p.tube([(0.07, 0, 0.19), (0.12, 0, 0.2), (0.12, 0, 0.06), (0.08, 0, 0.05)], 0.012, 'white_gloss', 6); p.push((-0.09, 0, 0.18), ry=-0.6); p.cyl((0, 0, 0), 0.015, 0.06, 'white_gloss', 8, rtop=0.01); p.pop()
p = prop('microwave', 'use', 14, 'Use the microwave', action='microwave')
p.rbox((0, 0, 0.15), (0.5, 0.38, 0.3), 0.01, 'white', 1, front='micro')
p = prop('fridge_tall', 'use', 60, 'Open the fridge', action='fridge')
p.rbox((0, 0, 0.9), (0.6, 0.62, 1.8), 0.02, 'white', 2); p.box((0, -0.312, 1.15), (0.58, 0.004, 0.004), 'grey_lt'); p.box((0.25, -0.33, 1.4), (0.02, 0.03, 0.3), 'grey_lt'); p.box((0.25, -0.33, 0.85), (0.02, 0.03, 0.2), 'grey_lt')
p.box((-0.1, -0.312, 1.5), (0.12, 0.004, 0.16), 'notice1')
p = prop('kitchenette', 'use', 120, 'Wash up', action='sink')      # 2.4 m: cupboards, worktop, sink and tap, wall cupboards
p.box((0, 0, 0.44), (2.4, 0.6, 0.88), 'white', skip=('left', 'right'))
for k in range(4): x = -0.9 + k * 0.6; p.rbox((x, -0.302, 0.45), (0.58, 0.012, 0.72), 0.004, 'white_gloss', 1); p.box((x + 0.2, -0.312, 0.75), (0.1, 0.012, 0.012), 'chrome')
p.rbox((0, -0.01, 0.9), (2.42, 0.62, 0.04), 0.006, 'formica', 1); p.box((0.4, -0.02, 0.921), (0.6, 0.42, 0.002), 'steel'); p.box((0.4, -0.02, 0.9), (0.5, 0.36, 0.04), 'steel_dark')
p.cyl((0.4, 0.2, 0.92), 0.015, 0.25, 'chrome', 8); p.tube([(0.4, 0.2, 1.16), (0.4, 0.05, 1.18), (0.4, 0.0, 1.1)], 0.012, 'chrome', 6)
p.box((0, 0.1, 1.85), (2.4, 0.35, 0.7), 'white', front='white_gloss')
for k in range(4): p.box((-0.9 + k * 0.6 + 0.2, -0.08, 1.6), (0.1, 0.012, 0.012), 'chrome')
p.box((0, 0.29, 1.2), (2.4, 0.02, 0.56), 'tiles_grey' if 'tiles_grey' in REG else 'white_gloss')
p = prop('toaster', 'use', 2, 'Make toast', action='toast'); p.rbox((0, 0, 0.1), (0.28, 0.16, 0.2), 0.03, 'chrome', 2); p.box((-0.05, 0, 0.201), (0.17, 0.03, 0.004), 'black'); p.box((0.05, 0, 0.201), (0.17, 0.03, 0.004), 'black')
p = prop('vending', 'use', 200, 'Buy a snack', action='vend')
p.rbox((0, 0, 0.92), (0.9, 0.85, 1.84), 0.02, 'black', 2); p.box((-0.1, -0.426, 1.05), (0.6, 0.01, 1.3), 'glass_clear'); p.panel(-0.39, 0.42, 0.19, 1.68, -0.42, 'vend')
p.box((0.33, -0.43, 1.2), (0.16, 0.01, 0.5), 'grey_dk'); p.box((0.33, -0.436, 1.35), (0.1, 0.004, 0.06), 'led_green'); p.box((-0.1, -0.43, 0.2), (0.55, 0.02, 0.2), 'charcoal')
p.box((0, -0.43, 1.78), (0.86, 0.01, 0.1), 'led_red')
p = prop('wall_tv', 'use', 15, 'Change the channel', action='tv'); p.rbox((0, 0.03, 0.0), (1.1, 0.05, 0.64), 0.01, 'black_gloss', 1); p.panel(-0.53, -0.3, 0.53, 0.3, -0.0, 'tv')
p = prop('water_cooler', 'use', 25, 'Have a drink of water', action='water')
p.rbox((0, 0, 0.5), (0.32, 0.32, 1.0), 0.02, 'plastic_white', 2); p.lathe([(0.12, 1.0), (0.14, 1.05), (0.14, 1.35), (0.05, 1.42), (0.04, 1.48)], 'glass_blue', 16)
p.box((-0.05, -0.165, 0.85), (0.04, 0.03, 0.04), 'plastic_blue'); p.box((0.05, -0.165, 0.85), (0.04, 0.03, 0.04), 'plastic_red')
p = prop('noticeboard', 'use', 3, 'Read the notices', action='read')
p.box((0, 0.01, 0), (1.2, 0.02, 0.9), 'cork'); p.box((0, 0.012, 0), (1.24, 0.016, 0.94), 'oak')
for k in range(4): x, z = -0.38 + (k % 2) * 0.52, 0.18 - (k // 2) * 0.4; p.push((x, -0.001, z), ry=(k - 1.5) * 0.04); p.panel(-0.11, -0.15, 0.11, 0.15, 0.0, f'notice{k}'); p.pop()
p = prop('wall_clock', 'static', 1, ''); p.push((0, 0, 0), rx=math.pi / 2); p.lathe([(0.0, 0.0), (0.16, 0.0), (0.17, 0.02), (0.17, 0.05), (0.0, 0.05)], 'black', 24, cap_top='clock'); p.pop()
p = prop('pedal_bin', 'pickup', 3, 'Bin'); p.lathe([(0.0, 0.0), (0.14, 0.0), (0.15, 0.6), (0.155, 0.62), (0.12, 0.66), (0.0, 0.67)], 'steel', 18); p.box((0, -0.16, 0.03), (0.1, 0.06, 0.02), 'black')
p = prop('keg', 'pickup', 60, 'Keg of lager')
p.lathe([(0.0, 0.0), (0.18, 0.0), (0.19, 0.03), (0.2, 0.05), (0.2, 0.55), (0.19, 0.57), (0.18, 0.6), (0.0, 0.6)], 'brushed_steel', 24)
for z in (0.04, 0.56): p.torus((0, 0, z), 0.2, 0.012, 'steel', 24, 6)
p.cyl((0, 0, 0.6), 0.03, 0.03, 'steel_dark', 10, True); p.lathe([(0.205, 0.25), (0.205, 0.35)], 'label18', 24, -math.pi, 0.0)
p = prop('co2', 'pickup', 20, 'Gas bottle'); p.lathe([(0.0, 0.0), (0.09, 0.0), (0.1, 0.05), (0.1, 0.9), (0.06, 1.0), (0.025, 1.04), (0.0, 1.04)], 'black', 16); p.cyl((0, 0, 1.04), 0.02, 0.08, 'brass', 8, True)
p = prop('beer_crate', 'pickup', 12, 'Crate of beer')
p.box((0, 0, 0.15), (0.4, 0.3, 0.3), 'plastic_red', skip=('top',)); p.box((0, 0, 0.01), (0.39, 0.29, 0.02), 'plastic_red')
for k in range(24): x, y = -0.17 + (k % 6) * 0.068, -0.12 + (k // 6) * 0.08; p.push((x, y, 0.02)); p.lathe(BEERB, 'glass_amber', 8); p.cyl((0, 0, 0.22), 0.015, 0.012, 'gold', 6); p.pop()
for k in range(6):
    p = prop(f'box{k}', 'pickup', 6, 'Box of stock')
    w = [0.42, 0.4, 0.5, 0.45, 0.38, 0.35][k]; h = [0.3, 0.28, 0.35, 0.3, 0.3, 0.25][k]
    p.box((0, 0, h / 2), (w, w * 0.7, h), 'cardboard_ribbed', front=f'box{k}', top='cardboard'); p.box((0, 0, h + 0.0005), (0.06, w * 0.7, 0.001), 'glass_frost')
p = prop('mop_bucket', 'pickup', 6, 'Mop and bucket')
p.box((0, 0, 0.17), (0.45, 0.32, 0.3), 'plastic_yellow'); p.box((0, 0, 0.31), (0.44, 0.31, 0.01), 'water')
for x in (-0.2, 0.2):
    for y in (-0.13, 0.13): p.cyl((x, y, 0.0), 0.025, 0.03, 'black', 8, True)
p.push((0.08, 0.04, 0.2), rx=0.2); p.cyl((0, 0, 0), 0.012, 1.2, 'steel', 8); p.lathe([(0.0, -0.05), (0.07, -0.02), (0.06, 0.08), (0.0, 0.1)], 'cream', 10); p.pop()
p = prop('pallet_truck', 'pickup', 70, 'Pallet truck')
for x in (-0.2, 0.2): p.box((x, -0.4, 0.05), (0.16, 1.15, 0.07), 'red')
p.box((0, 0.22, 0.25), (0.5, 0.15, 0.4), 'red'); p.tube([(0, 0.22, 0.45), (0, 0.4, 1.15)], 0.02, 'red', 8); p.tube([(-0.15, 0.4, 1.15), (0.15, 0.4, 1.15)], 0.02, 'black', 8)
p.cyl((0, 0.3, 0.0), 0.08, 0.06, 'rubber', 10, True)
p = prop('chair_stack', 'static', 20, '')
for k in range(6):
    p.push((0, 0, k * 0.07))
    for x in (-0.2, 0.2): p.tube([(x, -0.2, 0.0), (x, -0.2, 0.44), (x, 0.2, 0.44), (x, 0.22, 0.0)], 0.011, 'chrome', 5)
    p.rbox((0, 0, 0.46), (0.44, 0.44, 0.03), 0.01, 'plastic_blue', 1); p.pop()
p.push((0, 0.23, 0.72 + 0.35), rx=0.12); p.rbox((0, 0, 0), (0.42, 0.03, 0.26), 0.012, 'plastic_blue', 1); p.pop()
p = prop('door', 'door', 30, 'Open the door', action='door', hinge=-0.45)   # single door leaf 0.9 x 2.05, hinge at x=-0.45, handle right; closer at the top
p.rbox((0, 0, 1.025), (0.9, 0.045, 2.05), 0.004, 'walnut', 1, front='walnut')
for z in (0.55, 1.45): p.box((0, -0.024, z), (0.62, 0.004, 0.7), 'mahogany')
p.box((0, -0.025, 0.12), (0.86, 0.004, 0.22), 'steel')
for s_ in (-1, 1): p.tube([(0.36, s_ * 0.025, 1.0), (0.36, s_ * 0.07, 1.0), (0.26, s_ * 0.07, 1.0)], 0.01, 'steel', 6); p.box((0.36, s_ * 0.026, 1.0), (0.05, 0.004, 0.14), 'steel')
p.box((-0.2, 0.04, 1.98), (0.3, 0.05, 0.05), 'steel_dark')
p = prop('door_steel', 'door', 50, 'Open the door', action='door', hinge=-0.45)
p.rbox((0, 0, 1.025), (0.9, 0.05, 2.05), 0.004, 'grey', 1); p.box((0.36, -0.03, 1.0), (0.06, 0.02, 0.22), 'black'); p.box((0.36, 0.03, 1.0), (0.06, 0.02, 0.22), 'black')
p.box((0, -0.028, 1.0), (0.84, 0.03, 0.04), 'steel')
p = prop('exit_sign', 'static', 1, ''); p.rbox((0, 0, 0), (0.4, 0.06, 0.16), 0.006, 'white', 1, front='sign_exit')
p = prop('cctv_dome', 'static', 1, ''); p.cyl((0, 0, -0.03), 0.07, 0.03, 'white', 14, False, True); p.lathe([(0.065, -0.03), (0.06, -0.07), (0.035, -0.1), (0.0, -0.11)], 'glass_dark', 14)
p = prop('extinguisher', 'pickup', 9, 'Fire extinguisher', action='spray')
p.lathe([(0.0, 0.0), (0.07, 0.0), (0.08, 0.03), (0.08, 0.45), (0.05, 0.52), (0.02, 0.55), (0.0, 0.55)], 'red', 16); p.lathe([(0.081, 0.3), (0.081, 0.4)], 'cream', 16, -math.pi, 0.0)
p.cyl((0, 0, 0.55), 0.015, 0.05, 'black', 8); p.tube([(0, 0, 0.6), (0.06, 0, 0.62), (0.08, -0.02, 0.5), (0.08, -0.05, 0.3)], 0.008, 'black', 6); p.box((0, 0, 0.62), (0.12, 0.02, 0.02), 'black')
p = prop('first_aid', 'pickup', 2, 'First aid kit', action='heal'); p.rbox((0, 0, 0.15), (0.35, 0.12, 0.3), 0.01, 'green', 1); p.box((0, -0.061, 0.15), (0.08, 0.004, 0.02), 'white'); p.box((0, -0.061, 0.15), (0.02, 0.004, 0.08), 'white')
p = prop('keypad', 'use', 1, 'Punch in a code', action='keypad'); p.rbox((0, 0.01, 0), (0.09, 0.02, 0.13), 0.006, 'grey_dk', 1, front='keypad'); p.box((0, -0.002, 0.055), (0.05, 0.004, 0.012), 'led_red')
p = prop('wet_floor', 'pickup', 1, 'Wet floor sign')
p.push((0, -0.08, 0), rx=0.18); p.box((0, 0, 0.3), (0.3, 0.012, 0.6), 'plastic_yellow', front='sign_wet'); p.pop(); p.push((0, 0.08, 0), rx=-0.18); p.box((0, 0, 0.3), (0.3, 0.012, 0.6), 'plastic_yellow'); p.pop()
p = prop('ashtray_bin', 'pickup', 5, 'Smokers\' bin'); p.lathe([(0.0, 0.0), (0.12, 0.0), (0.12, 0.85), (0.1, 0.9), (0.0, 0.9)], 'steel', 16)
p = prop('clipboard', 'pickup', 0.3, 'Clipboard', action='read'); p.box((0, 0, 0.004), (0.23, 0.32, 0.008), 'mdf'); p.box((0, -0.01, 0.009), (0.21, 0.27, 0.002), 'paper', top='clipboard'); p.box((0, 0.14, 0.012), (0.08, 0.03, 0.01), 'chrome')
p = prop('mug_dirty', 'pickup', 0.3, 'Dirty mug'); p.lathe([(0.0, 0.0), (0.04, 0.0), (0.042, 0.004), (0.042, 0.095), (0.038, 0.095), (0.038, 0.01)], 'cream', 16); p.disc((0, 0, 0.02), 0.038, 'coffee', 12)
p.tube([(0.041, 0, 0.08), (0.065, 0, 0.075), (0.066, 0, 0.03), (0.041, 0, 0.025)], 0.007, 'cream', 6)
p = prop('newspaper', 'pickup', 0.3, 'Newspaper', action='read'); p.box((0, 0, 0.006), (0.3, 0.4, 0.012), 'paper', top='notice2')
p = prop('potted_palm', 'static', 20, '')
p.lathe([(0.0, 0.0), (0.2, 0.0), (0.26, 0.45), (0.28, 0.5), (0.0, 0.5)], 'gold', 18); p.disc((0, 0, 0.47), 0.25, 'soil', 14)
rr = random.Random(4)
for k in range(9):
    a = k * TAU / 9 + rr.random() * 0.3; L_ = 0.9 + rr.random() * 0.4
    pts = [(math.cos(a) * L_ * t * 0.6, math.sin(a) * L_ * t * 0.6, 0.47 + L_ * (math.sin(t * 2.2) * 0.8)) for t in [q / 5 for q in range(6)]]
    p.tube(pts, 0.01, 'brown', 4, False)
    for q in range(1, 6):
        c0 = Vector(pts[q]); dirv = (Vector(pts[q]) - Vector(pts[q - 1])).normalized(); side = dirv.cross(Vector((0, 0, 1))).normalized()
        for sgn in (-1, 1):
            tip = c0 + side * sgn * 0.22 + Vector((0, 0, -0.12)); p.face([tuple(c0), tuple(c0 + dirv * 0.08), tuple(tip)], [(0, 0), (1, 0), (0.5, 1)], 'leaf')
            p.face([tuple(c0 + dirv * 0.08), tuple(c0), tuple(tip)], [(0, 0), (1, 0), (0.5, 1)], 'leaf')

# ===================================================================================== finish: objects, sharp edges, export
print('props:', len(PROPS))
mat_o = bpy.data.materials.new('CP_Opaque'); mat_g = bpy.data.materials.new('CP_Glass')
objs = []; tris = {}
for p in PROPS:
    bm = p.bm
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.00002)
    bmesh.ops.dissolve_degenerate(bm, dist=0.000005, edges=bm.edges)
    bm.normal_update()
    for e in bm.edges:
        if len(e.link_faces) == 2:
            a = e.link_faces[0].normal.angle(e.link_faces[1].normal, 0.0)
            e.smooth = a < math.radians(38) and e.link_faces[0].material_index == e.link_faces[1].material_index
    me = bpy.data.meshes.new(p.name); bm.to_mesh(me); bm.free()
    me.materials.append(mat_o); me.materials.append(mat_g)
    ob = bpy.data.objects.new(p.name, me); bpy.context.scene.collection.objects.link(ob); objs.append(ob)
    tris[p.name] = sum(len(f.vertices) - 2 for f in me.polygons)
    bb = [ob.matrix_world @ Vector(c) for c in ob.bound_box]
    mn = [min(v[i] for v in bb) for i in range(3)]; mx = [max(v[i] for v in bb) for i in range(3)]
    META[p.name].update(tris=tris[p.name], size=[round(mx[0] - mn[0], 4), round(mx[2] - mn[2], 4), round(mx[1] - mn[1], 4)], center=[round(-(mx[0] + mn[0]) / 2, 4), round((mx[2] + mn[2]) / 2, 4), round(-(mx[1] + mn[1]) / 2, 4)])
print('triangles total', sum(tris.values()), 'max', max(tris.items(), key=lambda kv: kv[1]))
bpy.ops.object.select_all(action='DESELECT')
for o in objs: o.select_set(True)
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, 'casino_props.fbx'), use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y',
                         apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, mesh_smooth_type='FACE',
                         use_tspace=False, path_mode='STRIP', embed_textures=False, add_leaf_bones=False)
json.dump(META, open(os.path.join(OUT, 'casino_props.json'), 'w'), indent=0)
print('exported', len(objs))

# ===================================================================================== preview sheets (Cycles)
if PREVIEW:
    def img(n, cs='sRGB'):
        im = bpy.data.images.load(os.path.join(ATL, n)); im.colorspace_settings.name = cs; return im
    nt = mat_o.node_tree if mat_o.use_nodes else None; mat_o.use_nodes = True; nt = mat_o.node_tree; N = nt.nodes; Lk = nt.links
    b = N.get('Principled BSDF'); ta = N.new('ShaderNodeTexImage'); ta.image = img('casino_albedo.png'); te = N.new('ShaderNodeTexImage'); te.image = img('casino_emit.png')
    tm = N.new('ShaderNodeTexImage'); tm.image = img('casino_mask.png', 'Non-Color'); sp = N.new('ShaderNodeSeparateColor'); inv = N.new('ShaderNodeMath'); inv.operation = 'SUBTRACT'; inv.inputs[0].default_value = 1
    Lk.new(ta.outputs['Color'], b.inputs['Base Color']); Lk.new(te.outputs['Color'], b.inputs['Emission Color']); b.inputs['Emission Strength'].default_value = 2.0
    Lk.new(tm.outputs['Color'], sp.inputs[0]); Lk.new(sp.outputs[0], b.inputs['Metallic']); Lk.new(tm.outputs['Alpha'], inv.inputs[1]); Lk.new(inv.outputs[0], b.inputs['Roughness'])
    tn = N.new('ShaderNodeTexImage'); tn.image = img('casino_normal.png', 'Non-Color'); nm = N.new('ShaderNodeNormalMap'); Lk.new(tn.outputs['Color'], nm.inputs['Color']); Lk.new(nm.outputs['Normal'], b.inputs['Normal'])
    mat_g.use_nodes = True; g = mat_g.node_tree.nodes.get('Principled BSDF'); tg = mat_g.node_tree.nodes.new('ShaderNodeTexImage'); tg.image = ta.image
    mat_g.node_tree.links.new(tg.outputs['Color'], g.inputs['Base Color']); g.inputs['Roughness'].default_value = 0.05; g.inputs['Transmission Weight'].default_value = 0.9; g.inputs['IOR'].default_value = 1.45
    sc = bpy.context.scene; sc.render.engine = 'CYCLES'; sc.cycles.samples = 20; sc.cycles.device = 'CPU'; sc.cycles.use_denoising = True
    w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True; w.node_tree.nodes['Background'].inputs[0].default_value = (0.5, 0.52, 0.56, 1); w.node_tree.nodes['Background'].inputs[1].default_value = 0.8
    bpy.ops.object.light_add(type='SUN', rotation=(math.radians(45), 0, math.radians(35))); bpy.context.active_object.data.energy = 3.0
    cam_d = bpy.data.cameras.new('cam'); cam = bpy.data.objects.new('cam', cam_d); sc.collection.objects.link(cam); sc.camera = cam
    groups = [[o for o in objs if o.name.startswith(('pint', 'half', 'wine', 'flute', 'highball', 'tumbler', 'shot', 'cup', 'mug', 'beermat', 'tray', 'crisps', 'cardm', 'shaker', 'straws', 'napkins', 'icebucket', 'chip_st', 'cards', 'cash_b', 'keyboard', 'mouse', 'kettle', 'toaster'))],
              [o for o in objs if o.name.startswith('btl_')],
              [o for o in objs if o.name.startswith(('slot', 'bar_', 'back_bar', 'font', 'drip', 'till', 'coffee', 'glass_', 'fridge_b', 'optics', 'bell', 'fruit', 'crisp_box', 'peanuts', 'limit'))],
              [o for o in objs if o.name.startswith(('roulette', 'blackjack', 'poker', 'etg', 'pit', 'casino_chair', 'tub', 'round', 'booth', 'stanchion', 'rope', 'chandelier', 'potted', 'card_shoe', 'chip_float'))],
              [o for o in objs if o not in sum([], []) and o.name.startswith(('desk', 'office', 'monitor', 'cctv', 'pc_', 'desk_phone', 'filing', 'safe', 'vault', 'note_', 'cash_t', 'shelving', 'lockers', 'sofa', 'staff', 'stack', 'micro', 'fridge_t', 'kitchen', 'vending', 'wall_', 'water', 'notice', 'pedal', 'keg', 'co2', 'beer_crate', 'box', 'mop', 'pallet', 'chair_stack', 'door', 'exit', 'extinguisher', 'first', 'keypad', 'wet', 'ashtray', 'clipboard', 'newspaper'))]]
    for gi, grp in enumerate(groups):
        for o in objs: o.hide_render = o not in grp
        x = 0.0; row_w = 4.0 if gi in (0, 1) else 14.0; y = 0.0; rowh = 0.0; cx = 0.0
        sizes = [(o, max(o.dimensions.x, 0.05), max(o.dimensions.y, 0.05)) for o in grp]
        gap = 0.05 if gi in (0, 1) else 0.4
        for o, wx, wy in sizes:
            if x + wx > row_w: x = 0.0; y += rowh + gap; rowh = 0.0
            o.location = (x + wx / 2 - (o.bound_box[0][0] + o.bound_box[6][0]) / 2, y + wy / 2, 0); x += wx + gap; rowh = max(rowh, wy)
        tot_y = y + rowh
        cam.location = (row_w / 2, -tot_y * 0.55 - row_w * 0.55, max(row_w, tot_y) * 0.62); cam.rotation_euler = (math.radians(55), 0, 0)
        cam_d.lens = 32; sc.render.resolution_x = 1600; sc.render.resolution_y = 1000
        sc.render.filepath = os.path.join(OUT, f'prev_{gi}.png'); bpy.ops.render.render(write_still=True)
        print('rendered', gi, len(grp))

"""Build game characters from CC0 MakeHuman data. Output: Unity-readable .bytes meshes + skin textures."""
import pickle, struct, os, sys, time
import numpy as np
from PIL import Image
from mhcore import *

OUT = '/tmp/claude-0/charpipe/out/chars'
os.makedirs(OUT, exist_ok=True)

V0, VT0, FACES = load_obj(MH + '/3dobjs/base.obj')
RIG = Rig()
DF, DT = pickle.load(open('/tmp/claude-0/charpipe/out/dec7000.pkl', 'rb'))
FULL, FULLT = [], []
for g, vs, ts in FACES:
    if g == 'body':
        for k in range(1, len(vs) - 1): FULL.append([vs[0], vs[k], vs[k + 1]]); FULLT.append([ts[0], ts[k], ts[k + 1]])
FULL = np.array(FULL)
_vts = {}
for f, t in zip(FULL, FULLT):
    for v, tt in zip(f, t): _vts.setdefault(int(v), set()).add(tt)
_edges = {}
for f in FULL:
    for a, b in ((f[0], f[1]), (f[1], f[2]), (f[2], f[0])):
        k = (min(a, b), max(a, b)); _edges[k] = _edges.get(k, 0) + 1
BASELOCK = np.zeros(len(V0), bool)
for v, st in _vts.items():
    if len(st) > 1: BASELOCK[v] = True
for (a, b), c in _edges.items():
    if c == 1: BASELOCK[a] = BASELOCK[b] = True

def helper_tris(prefix):
    F, T = [], []
    for g, vs, ts in FACES:
        if g is not None and g.startswith(prefix):
            for k in range(1, len(vs) - 1): F.append([vs[0], vs[k], vs[k + 1]]); T.append([ts[0], ts[k], ts[k + 1]])
    return F, T

SKIRT = helper_tris('helper-skirt')
LASH = helper_tris('helper-l-eyelashes'); _r = helper_tris('helper-r-eyelashes'); LASH = (LASH[0] + _r[0], LASH[1] + _r[1])
EYE_V, EYE_VT, EYE_F = load_obj(MH + '/eyes/low-poly/low-poly.obj')
EYE_MAP = [int(l) for l in open(MH + '/eyes/low-poly/low-poly.mhclo').read().split('verts 0')[1].split() if l.strip().isdigit()]

MIRROR = False   # FBX path: keep MakeHuman handedness (Blender/FBX convert it for Unity)
POSE_TARGET = 90.0  # arms horizontal: T-pose for Unity Humanoid
def to_game(P):
    Q = P * 0.1
    if MIRROR: Q[:, 0] = -Q[:, 0]
    return Q

def vnormals(P, F):
    n = np.zeros_like(P)
    tri = P[F]
    fn = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])
    for k in range(3): np.add.at(n, F[:, k], fn)
    l = np.linalg.norm(n, axis=1, keepdims=True); l[l == 0] = 1
    return n / l

BODIES = {
    'm_young': dict(gender=1.0, age=0.5, muscle=0.6, weight=0.5, height=0.55),
    'm_fit': dict(gender=1.0, age=0.55, muscle=0.85, weight=0.4, height=0.65),
    'm_heavy': dict(gender=1.0, age=0.68, muscle=0.35, weight=0.95, height=0.45),
    'm_old': dict(gender=1.0, age=0.95, muscle=0.3, weight=0.55, height=0.4),
    'f_young': dict(gender=0.0, age=0.5, muscle=0.45, weight=0.45, height=0.5),
    'f_slim': dict(gender=0.0, age=0.55, muscle=0.5, weight=0.25, height=0.62),
    'f_curvy': dict(gender=0.0, age=0.68, muscle=0.35, weight=0.85, height=0.42),
    'f_old': dict(gender=0.0, age=0.95, muscle=0.3, weight=0.6, height=0.35),
}
OUTFITS = {
    'tee': dict(sleeve='short', bottom='trousers', shoes='low', hair=None, top_off=0.005),
    'shirt': dict(sleeve='long', bottom='trousers', shoes='low', hair=None, top_off=0.006),
    'jacket': dict(sleeve='long', bottom='trousers', shoes='boot', hair=None, top_off=0.016, jacket=True),
    'skirt': dict(sleeve='long', bottom='skirt', shoes='low', hair=None, top_off=0.007),
    'dress': dict(sleeve='short', bottom='skirt', shoes='low', hair=None, top_off=0.005, dress=True),
}
COMBOS = [
    ('m_young', 'tee', 'short'), ('m_young', 'shirt', 'short'), ('m_fit', 'jacket', 'short'), ('m_fit', 'tee', 'short'),
    ('m_heavy', 'shirt', 'bald'), ('m_heavy', 'jacket', 'short'), ('m_old', 'jacket', 'bald'), ('m_old', 'shirt', 'bald'),
    ('f_young', 'tee', 'long'), ('f_young', 'skirt', 'bob'), ('f_slim', 'shirt', 'long'), ('f_slim', 'dress', 'bob'),
    ('f_curvy', 'skirt', 'bob'), ('f_curvy', 'jacket', 'bun'), ('f_old', 'skirt', 'bun'), ('f_old', 'jacket', 'bun'),
]
MATS = ['skin', 'hair', 'top', 'bottom', 'shoes', 'eyes', 'lashes']

def seg_t(p, a, b):
    d = b - a; return float(np.clip((p - a).dot(d) / (d.dot(d) + 1e-12), 0, 1))

def build(body, outfit, hair, target_tris=7600):
    O = OUTFITS[outfit]
    Pm = morph(V0, **BODIES[body])
    P = to_game(Pm)
    ground = P[np.unique(FULL)][:, 1].min()
    P[:, 1] -= ground
    N = vnormals(P, FULL)
    heads = {b: to_game(RIG.joint(Pm, HEADJ[b])[None])[0] - np.array([0, ground, 0]) for b in GAME}
    eyeL = to_game(RIG.joint(Pm, 'eye.L____head')[None])[0] - [0, ground, 0]
    eyeR = to_game(RIG.joint(Pm, 'eye.R____head')[None])[0] - [0, ground, 0]
    eyeY = (eyeL[1] + eyeR[1]) / 2
    headTop = to_game(RIG.joint(Pm, 'head____tail')[None])[0] - [0, ground, 0]
    bone_of = lambda v: max(RIG.W.get(v, {'Hips': 1}).items(), key=lambda kv: kv[1])[0]
    headVs = [v for v in np.unique(FULL) if bone_of(v) == 'Head']
    hc = P[headVs].mean(axis=0)
    waistY = heads['Hips'][1] + (heads['Spine'][1] - heads['Hips'][1]) * 0.55

    def region(f):
        c = P[f].mean(axis=0)
        bs = [bone_of(v) for v in f]
        b = max(set(bs), key=bs.count)
        side = b[-1] if b.endswith('.L') or b.endswith('.R') else None
        if b in ('Head', 'Neck'):
            hr = hair_region(c, b)
            return 'hair' if hr else 'skin'
        if b in ('Chest', 'Spine') or (side and b.startswith('Shoulder')):
            return 'top'
        if b == 'Hips':
            if c[1] > waistY: return 'top'
            return 'top' if O.get('jacket') and c[1] > heads['Hips'][1] - 0.12 else 'bottom'
        if side and b.startswith('UpperArm'):
            t = seg_t(c, heads[f'UpperArm.{side}'], heads[f'LowerArm.{side}'])
            return 'top' if O['sleeve'] == 'long' or t < 0.5 else 'skin'
        if side and b.startswith('LowerArm'):
            t = seg_t(c, heads[f'LowerArm.{side}'], heads[f'Hand.{side}'])
            return 'top' if O['sleeve'] == 'long' and t < 0.93 else 'skin'
        if side and b.startswith('Hand'):
            return 'skin'
        if side and b.startswith('UpperLeg'):
            if O['bottom'] == 'skirt':
                t = seg_t(c, heads[f'UpperLeg.{side}'], heads[f'LowerLeg.{side}'])
                return 'bottom' if t < 0.25 else 'legs'
            if O.get('jacket'):
                t = seg_t(c, heads[f'UpperLeg.{side}'], heads[f'LowerLeg.{side}'])
                if t < 0.12: return 'top'
            return 'bottom'
        if side and b.startswith('LowerLeg'):
            t = seg_t(c, heads[f'LowerLeg.{side}'], heads[f'Foot.{side}'])
            if t > (0.72 if O['shoes'] == 'boot' else 0.94): return 'shoes'
            return 'legs' if O['bottom'] == 'skirt' else 'bottom'
        if side and (b.startswith('Foot') or b.startswith('Toes')):
            return 'shoes'
        return 'skin'

    def hair_region(c, b):
        if hair is None: return False
        front = c[2] > hc[2] + 0.045
        face = c[2] > hc[2] + 0.012 and abs(c[0] - hc[0]) < 0.058 and c[1] < eyeY + 0.056
        if face: return False
        ear = abs(c[0] - hc[0]) > 0.062 and eyeY - 0.055 < c[1] < eyeY + 0.025 and abs(c[2] - hc[2]) < 0.035
        if b == 'Neck':
            if hair == 'long': return c[2] < hc[2] - 0.0 and c[1] > eyeY - 0.17
            if hair == 'bob': return c[2] < hc[2] - 0.01 and c[1] > eyeY - 0.12
            return False
        if hair == 'bald':
            return (not front) and (not ear) and eyeY - 0.07 < c[1] < eyeY + 0.03 and c[2] < hc[2] + 0.01
        if front and c[1] < eyeY + (0.058 if hair != 'bald' else 0.0): return False
        if hair in ('bob', 'long'):
            if (c[2] < hc[2] + 0.012 or abs(c[0] - hc[0]) > 0.058) and c[1] > eyeY - 0.1: return True
        if ear: return False
        if c[2] < hc[2] - 0.02 and c[1] > eyeY - 0.075: return True
        return c[1] > eyeY + 0.035

    OFF = {'skin': 0.0, 'legs': 0.0015, 'top': O['top_off'], 'bottom': 0.009, 'shoes': 0.011, 'hair': 0.011}

    def offset(reg, v):
        o = OFF[reg]
        p = P[v]
        if reg == 'hair':
            o += 0.014 * np.clip((p[1] - eyeY) / 0.1, 0, 1)
            if hair in ('bob', 'long'): o += 0.008 * np.clip((hc[2] - p[2]) / 0.06, 0, 1)
            if hair == 'bald': o = 0.004
        if reg == 'bottom' and O['bottom'] == 'trousers':
            o += 0.007 * np.clip((heads['LowerLeg.L'][1] - p[1]) / 0.35, 0, 1)
        if reg == 'top' and O.get('dress'): o += 0.0
        return o

    # ---------- assemble ----------
    verts = {}  # key -> index
    pos, nrm, uv, wts, keyv = [], [], [], [], []
    tris = {m: [] for m in MATS}
    regmat = {'skin': 'skin', 'legs': 'skin', 'hair': 'hair', 'top': 'top', 'bottom': 'bottom', 'shoes': 'shoes'}
    if O['bottom'] == 'skirt': regmat['legs'] = 'skin'

    def vid(reg, v, t, p=None, w=None, u=None, n=None):
        key = (reg, v, t)
        if key in verts: return verts[key]
        i = len(pos); verts[key] = i
        if p is None:
            p = P[v] + N[v] * offset(reg, v)
        pos.append(p); nrm.append(N[v] if n is None and v >= 0 else (n if n is not None else [0, 1, 0]))
        uv.append(VT0[t] if u is None else u)
        wts.append(RIG.weights4(v) if w is None else w)
        keyv.append((reg, v))
        return i

    full_reg = [region(list(f)) for f in FULL]
    lock = BASELOCK.copy()
    vreg = {}
    for f, r in zip(FULL, full_reg):
        for v in f: vreg.setdefault(int(v), set()).add(r)
    for v, st in vreg.items():
        if len(st) > 1: lock[v] = True
    global DF, DT
    DF, DT, keep = decimate(P, [list(map(int, f)) for f in FULL], [list(t) for t in FULLT], target_tris, lock)
    face_reg = [full_reg[i] for i in keep]
    for f, t, r in zip(DF, DT, face_reg):
        tris[regmat[r]].append([vid(r, f[k], t[k]) for k in range(3)])
    # hems: edges between faces of different regions with different offsets
    edge_faces = {}
    for fi, f in enumerate(DF):
        for k in range(3):
            a, b = f[k], f[(k + 1) % 3]
            edge_faces.setdefault((min(a, b), max(a, b)), []).append((fi, a, b, k))
    for (a, b), lst in edge_faces.items():
        if len(lst) != 2: continue
        (f1, a1, b1, k1), (f2, a2, b2, k2) = lst
        r1, r2 = face_reg[f1], face_reg[f2]
        if r1 == r2 or abs(OFF[r1] - OFF[r2]) < 1e-4 and r1 not in ('hair',) and r2 not in ('hair',): continue
        outer, inner, fo, fi_ = (r1, r2, f1, f2) if OFF[r1] >= OFF[r2] else (r2, r1, f2, f1)
        to = DT[fo]; ti = DT[fi_]
        ta_o = to[DF[fo].index(a)]; tb_o = to[DF[fo].index(b)]
        ta_i = ti[DF[fi_].index(a)]; tb_i = ti[DF[fi_].index(b)]
        A_o, B_o = vid(outer, a, ta_o), vid(outer, b, tb_o)
        A_i, B_i = vid(inner, a, ta_i), vid(inner, b, tb_i)
        # orient strip outward (along vertex normal)
        pa, pb, pc = np.array(pos[A_o]), np.array(pos[B_o]), np.array(pos[B_i])
        fn = np.cross(pb - pa, pc - pa)
        tris[regmat[outer]] += [[A_o, B_i, B_o], [A_o, A_i, B_i], [A_o, B_o, B_i], [A_o, B_i, A_i]]
    # skirt / dress lower
    if O['bottom'] == 'skirt':
        mat = 'top' if O.get('dress') else 'bottom'
        kneeY = (heads['LowerLeg.L'][1] + heads['LowerLeg.R'][1]) / 2 - 0.04
        for f, t in zip(*SKIRT):
            if min(P[f[k]][1] for k in range(3)) < kneeY: continue
            tris[mat].append([vid('skirt', f[k], t[k], p=P[f[k]] + N[f[k]] * 0.004) for k in range(3)])
    # eyelashes
    for f, t in zip(*LASH):
        tris['lashes'].append([vid('lash', f[k], t[k], p=P[f[k]], w=[(GAME.index('Head'), 1.0)]) for k in range(3)])
    # eyes (low-poly eye mesh bound 1:1 to helper eye vertices)
    for g, vs, ts in EYE_F:
        for k in range(1, len(vs) - 1):
            tri = []
            for j in (0, k, k + 1):
                bv = EYE_MAP[vs[j]]
                tri.append(vid('eye', -1 - vs[j], ts[j], p=P[bv], w=[(GAME.index('Head'), 1.0)], u=EYE_VT[ts[j]], n=(P[bv] - (eyeL if np.linalg.norm(P[bv] - eyeL) < np.linalg.norm(P[bv] - eyeR) else eyeR))))
            tris['eyes'].append(tri)
    # bun
    if hair == 'bun':
        c = np.array([hc[0], eyeY + 0.035, hc[2] - 0.105]); rad = np.array([0.045, 0.04, 0.038])
        rings, seg = 6, 10; idx = {}
        for i in range(rings + 1):
            ph = np.pi * i / rings
            for j in range(seg + 1):
                th = 2 * np.pi * j / seg
                nv = np.array([np.sin(ph) * np.cos(th), np.cos(ph), np.sin(ph) * np.sin(th)])
                idx[i, j] = vid('bun', -1000 - i * 100 - j, 0, p=c + nv * rad, w=[(GAME.index('Head'), 1.0)], u=[j / seg, i / rings], n=nv)
        for i in range(rings):
            for j in range(seg):
                tris['hair'] += [[idx[i, j], idx[i + 1, j + 1], idx[i + 1, j]], [idx[i, j], idx[i, j + 1], idx[i + 1, j + 1]]]

    pos = np.array(pos, np.float32)
    # smooth normals per (region, base vertex) over final geometry
    fn_acc = np.zeros_like(pos)
    for m in MATS:
        if not tris[m]: continue
        F = np.array(tris[m]); tri = pos[F]
        fn = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])
        for k in range(3): np.add.at(fn_acc, F[:, k], fn)
    group = {}
    for i, (reg, v) in enumerate(keyv):
        group.setdefault((reg if reg not in ('eye', 'bun', 'lash') else reg + str(i), v), []).append(i)
    nrm = np.array(nrm, np.float32)
    for k, ids in group.items():
        if k[0].startswith('eye') or k[0].startswith('bun'): continue
        s = fn_acc[ids].sum(axis=0); l = np.linalg.norm(s)
        if l > 0: nrm[ids] = s / l
    nrm = nrm / (np.linalg.norm(nrm, axis=1, keepdims=True) + 1e-9)
    tails = {b: to_game(RIG.joint(Pm, TAILJ[b])[None])[0] - np.array([0, ground, 0]) for b in TAILJ}
    heads.update({'_tail_' + b: v for b, v in tails.items()})
    pos, nrm, heads = pose_arms(pos, nrm, wts, heads, POSE_TARGET)
    return dict(pos=pos, nrm=nrm, uv=np.array(uv, np.float32), wts=wts, tris=tris, heads=heads, eyeY=eyeY, hc=hc)


def _rz(t):
    c, s = np.cos(t), np.sin(t)
    return np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]])


def pose_arms(pos, nrm, wts, heads, target_deg=7.0):
    """Bake a relaxed arms-down rest pose (linear blend skinning) so the game rig's arms hang at identity."""
    heads = {k: np.array(v, np.float64) for k, v in heads.items()}
    T = {b: (np.eye(3), np.zeros(3)) for b in GAME}  # v' = R v + t
    heads = dict(heads)
    for s in 'LR':
        h = heads[f'UpperArm.{s}']; d = heads[f'LowerArm.{s}'] - h
        ang = np.degrees(np.arctan2(abs(d[0]), -d[1]))
        th = np.radians(ang - target_deg) * (1.0 if h[0] < 0 else -1.0)
        hs = heads[f'Shoulder.{s}']
        Rs = _rz(th * 0.3); ts = hs - Rs @ hs
        Ra = _rz(th * 0.7)
        # upper arm: shoulder transform then own rotation about the (moved) arm head
        h2 = Rs @ h + ts
        Ru = Ra @ Rs; tu = Ra @ (ts - h2) + h2
        T[f'Shoulder.{s}'] = (Rs, ts)
        for b in (f'UpperArm.{s}', f'LowerArm.{s}', f'Hand.{s}'): T[b] = (Ru, tu)
        for b in (f'UpperArm.{s}', f'LowerArm.{s}', f'Hand.{s}', f'_tail_Hand.{s}'):
            R, t = T[f'Shoulder.{s}'] if b == f'UpperArm.{s}' else (Ru, tu)
            heads[b] = R @ heads[b] + t
    P2 = np.zeros_like(pos, dtype=np.float64); N2 = np.zeros_like(nrm, dtype=np.float64)
    for i, w in enumerate(wts):
        p = pos[i].astype(np.float64); n = nrm[i].astype(np.float64)
        for b, x in w:
            R, t = T[GAME[b]]
            P2[i] += x * (R @ p + t); N2[i] += x * (R @ n)
    N2 /= np.linalg.norm(N2, axis=1, keepdims=True) + 1e-9
    return P2.astype(np.float32), N2.astype(np.float32), heads

def write(path, M):
    with open(path, 'wb') as f:
        n = len(M['pos'])
        f.write(b'ASBC'); f.write(struct.pack('<iii', 1, n, len(GAME)))
        for b in GAME:
            nb = b.encode(); f.write(struct.pack('<B', len(nb))); f.write(nb)
            par = PARENT[b]; f.write(struct.pack('<i', GAME.index(par) if par else -1))
            f.write(struct.pack('<3f', *M['heads'][b]))
        f.write(M['pos'].astype('<f4').tobytes()); f.write(M['nrm'].astype('<f4').tobytes()); f.write(M['uv'].astype('<f4').tobytes())
        bi = np.zeros((n, 4), np.uint8); bw = np.zeros((n, 4), np.float32)
        for i, w in enumerate(M['wts']):
            for k, (b, x) in enumerate(w[:4]): bi[i, k] = b; bw[i, k] = x
        f.write(bi.tobytes()); f.write(bw.astype('<f4').tobytes())
        mats = [m for m in MATS if M['tris'][m]]
        f.write(struct.pack('<i', len(mats)))
        for m in mats:
            # flip winding for the handedness change
            F = np.array(M['tris'][m], np.int32)[:, [0, 2, 1]]
            nm = m.encode(); f.write(struct.pack('<B', len(nm))); f.write(nm)
            f.write(struct.pack('<i', F.size)); f.write(F.astype('<u2' if n < 65535 else '<u4').tobytes())

if __name__ == '__main__':
    only = sys.argv[1:]
    for body, outfit, hair in COMBOS:
        name = f'{body}_{outfit}_{hair}'
        if only and name not in only and body not in only: continue
        t = time.time()
        M = build(body, outfit, hair)
        write(f'{OUT}/{name}.bytes', M)
        pickle.dump(M, open(f'{OUT}/{name}.pkl', 'wb'))
        print(name, len(M['pos']), 'verts', sum(len(v) for v in M['tris'].values()), 'tris', round(time.time() - t, 1), 's')

"""MakeHuman (CC0 hm08 assets) -> game character core: mesh loading, macro morphs, skeleton, weights, decimation."""
import json, os, heapq, functools
import numpy as np

MH = '/tmp/claude-0/mh/makehuman/data'


def load_obj(path):
    V, VT, faces = [], [], []  # faces: (group, [v...], [vt...])
    grp = None
    for line in open(path):
        if line.startswith('v '):
            V.append([float(x) for x in line.split()[1:4]])
        elif line.startswith('vt '):
            VT.append([float(x) for x in line.split()[1:3]])
        elif line.startswith('g '):
            grp = line.split()[1]
        elif line.startswith('f '):
            vs, ts = [], []
            for c in line.split()[1:]:
                p = c.split('/')
                vs.append(int(p[0]) - 1)
                ts.append(int(p[1]) - 1 if len(p) > 1 and p[1] else -1)
            faces.append((grp, vs, ts))
    return np.array(V, np.float64), np.array(VT, np.float64), faces


@functools.lru_cache(maxsize=None)
def load_target(rel):
    p = os.path.join(MH, 'targets', rel)
    idx, d = [], []
    for line in open(p):
        if not line.strip() or line[0] == '#':
            continue
        a = line.split()
        idx.append(int(a[0])); d.append([float(a[1]), float(a[2]), float(a[3])])
    return np.array(idx, np.int64), np.array(d, np.float64)


def _tri(v):
    """value 0..1 (0.5 = average) -> weights for min/average/max."""
    lo = max(0.0, (0.5 - v) * 2); hi = max(0.0, (v - 0.5) * 2)
    return {'min': lo, 'average': 1 - lo - hi, 'max': hi}


def morph(base, gender=0.5, age=0.5, muscle=0.5, weight=0.5, height=0.5, race=(1/3, 1/3, 1/3)):
    """Approximation of MakeHuman's macro modifiers (gender/age/muscle/weight/height/race)."""
    V = base.copy()
    gw = {'female': 1 - gender, 'male': gender}
    # age: 0.5 = 25y (young), 1.0 = 90y (old). We only use young..old.
    old = max(0.0, (age - 0.5) * 2)
    aw = {'young': 1 - old, 'old': old}
    mw, ww = _tri(muscle), _tri(weight)
    def add(rel, w):
        if w < 1e-4:
            return
        fp = os.path.join(MH, 'targets', rel)
        if not os.path.exists(fp):
            return
        i, d = load_target(rel)
        if len(i):
            V[i] += d * w
    for g, gv in gw.items():
        for a, av in aw.items():
            for m, mv in mw.items():
                for w_, wv in ww.items():
                    add(f'macrodetails/universal-{g}-{a}-{m}muscle-{w_}weight.target', gv * av * mv * wv)
                    hmin = max(0.0, (0.5 - height) * 2); hmax = max(0.0, (height - 0.5) * 2)
                    add(f'macrodetails/height/{g}-{a}-{m}muscle-{w_}weight-minheight.target', gv * av * mv * wv * hmin)
                    add(f'macrodetails/height/{g}-{a}-{m}muscle-{w_}weight-maxheight.target', gv * av * mv * wv * hmax)
            for r, rv in zip(['african', 'asian', 'caucasian'], race):
                add(f'macrodetails/{r}-{g}-{a}.target', gv * av * rv)
    return V


# ---------------- skeleton ----------------
GAME = ['Hips', 'Spine', 'Chest', 'Neck', 'Head',
        'Shoulder.L', 'UpperArm.L', 'LowerArm.L', 'Hand.L',
        'Shoulder.R', 'UpperArm.R', 'LowerArm.R', 'Hand.R',
        'UpperLeg.L', 'LowerLeg.L', 'Foot.L', 'UpperLeg.R', 'LowerLeg.R', 'Foot.R', 'Toes.L', 'Toes.R']
PARENT = {'Hips': None, 'Spine': 'Hips', 'Chest': 'Spine', 'Neck': 'Chest', 'Head': 'Neck',
          'Shoulder.L': 'Chest', 'UpperArm.L': 'Shoulder.L', 'LowerArm.L': 'UpperArm.L', 'Hand.L': 'LowerArm.L',
          'Shoulder.R': 'Chest', 'UpperArm.R': 'Shoulder.R', 'LowerArm.R': 'UpperArm.R', 'Hand.R': 'LowerArm.R',
          'UpperLeg.L': 'Hips', 'LowerLeg.L': 'UpperLeg.L', 'Foot.L': 'LowerLeg.L',
          'UpperLeg.R': 'Hips', 'LowerLeg.R': 'UpperLeg.R', 'Foot.R': 'LowerLeg.R', 'Toes.L': 'Foot.L', 'Toes.R': 'Foot.R'}
HEADJ = {'Hips': 'root____head', 'Spine': 'spine04____head', 'Chest': 'spine02____head', 'Neck': 'neck01____head', 'Head': 'head____head'}
for s in 'LR':
    HEADJ.update({f'Shoulder.{s}': f'clavicle.{s}____head', f'UpperArm.{s}': f'upperarm01.{s}____head', f'LowerArm.{s}': f'lowerarm01.{s}____head',
                  f'Hand.{s}': f'wrist.{s}____head', f'UpperLeg.{s}': f'upperleg01.{s}____head', f'LowerLeg.{s}': f'lowerleg01.{s}____head', f'Foot.{s}': f'foot.{s}____head',
                  f'Toes.{s}': f'toe3-1.{s}____head'})
TAILJ = {'Head': 'head____tail'}
for s in 'LR':
    TAILJ.update({f'Hand.{s}': f'finger3-3.{s}____tail', f'Toes.{s}': f'toe3-3.{s}____tail'})
UNITY = {'Hips': 'Hips', 'Spine': 'Spine', 'Chest': 'Spine1', 'Neck': 'Neck', 'Head': 'Head'}
for s, w in (('L', 'Left'), ('R', 'Right')):
    UNITY.update({f'Shoulder.{s}': f'{w}Shoulder', f'UpperArm.{s}': f'{w}Arm', f'LowerArm.{s}': f'{w}ForeArm', f'Hand.{s}': f'{w}Hand',
                  f'UpperLeg.{s}': f'{w}UpLeg', f'LowerLeg.{s}': f'{w}Leg', f'Foot.{s}': f'{w}Foot', f'Toes.{s}': f'{w}ToeBase'})


def map_bone(n):
    side = n[-1] if n.endswith('.L') or n.endswith('.R') else None
    b = n[:-2] if side else n
    if b in ('root', 'pelvis', 'spine05'): return 'Hips'
    if b in ('spine04', 'spine03'): return 'Spine'
    if b in ('spine02', 'spine01', 'breast'): return 'Chest'
    if b.startswith('neck'): return 'Neck'
    if b in ('clavicle', 'shoulder01'): return f'Shoulder.{side}'
    if b.startswith('upperarm'): return f'UpperArm.{side}'
    if b.startswith('lowerarm'): return f'LowerArm.{side}'
    if b in ('wrist',) or b.startswith('metacarpal') or b.startswith('finger'): return f'Hand.{side}'
    if b.startswith('upperleg'): return f'UpperLeg.{side}'
    if b.startswith('lowerleg'): return f'LowerLeg.{side}'
    if b == 'foot': return f'Foot.{side}'
    if b.startswith('toe'): return f'Toes.{side}'
    return 'Head'


class Rig:
    def __init__(self):
        d = json.load(open(os.path.join(MH, 'rigs/default.mhskel')))
        self.joints = d['joints']
        w = json.load(open(os.path.join(MH, 'rigs/default_weights.mhw')))['weights']
        self.W = {}  # vertex -> {game bone: weight}
        for bone, lst in w.items():
            g = map_bone(bone)
            for v, wt in lst:
                dct = self.W.setdefault(v, {})
                dct[g] = dct.get(g, 0.0) + wt

    def joint(self, V, name):
        return V[self.joints[name]].mean(axis=0)

    def bone_heads(self, V):
        return {b: self.joint(V, HEADJ[b]) for b in GAME}

    def weights4(self, v):
        d = self.W.get(v, {'Hips': 1.0})
        it = sorted(d.items(), key=lambda kv: -kv[1])[:4]
        s = sum(w for _, w in it) or 1.0
        return [(GAME.index(b), w / s) for b, w in it]


# ---------------- decimation (half-edge collapse QEM, attributes preserved) ----------------
def decimate(V, faces_v, faces_t, target_tris, locked):
    """V: positions; faces_v/faces_t: lists of triangle corner indices (v, vt). Returns new (faces_v, faces_t)."""
    F = [list(f) for f in faces_v]; T = [list(t) for t in faces_t]
    alive = [True] * len(F)
    vf = {}  # vertex -> set of face ids
    for i, f in enumerate(F):
        for v in f:
            vf.setdefault(v, set()).add(i)
    def plane(fi):
        a, b, c = (V[F[fi][0]], V[F[fi][1]], V[F[fi][2]])
        n = np.cross(b - a, c - a); l = np.linalg.norm(n)
        if l < 1e-12: return None, 0
        n /= l; d = -n.dot(a)
        return np.append(n, d), l
    Q = {}
    for v in vf:
        q = np.zeros((4, 4))
        for fi in vf[v]:
            p, area = plane(fi)
            if p is not None: q += np.outer(p, p) * area
        Q[v] = q
    def neigh(v):
        s = set()
        for fi in vf[v]:
            if alive[fi]: s.update(F[fi])
        s.discard(v); return s
    def cost(u, v):
        p = np.append(V[v], 1.0); q = Q[u] + Q[v]
        return float(p @ q @ p)
    heap = []
    ver = {v: 0 for v in vf}
    def push(u):
        if locked[u]: return
        best = None
        for v in neigh(u):
            c = cost(u, v)
            if best is None or c < best[0]: best = (c, v)
        if best: heapq.heappush(heap, (best[0], u, best[1], ver[u]))
    for u in vf: push(u)
    ntri = sum(alive)
    while ntri > target_tris and heap:
        c, u, v, vv = heapq.heappop(heap)
        if vv != ver.get(u) or locked[u] or u not in vf: continue
        if v not in neigh(u): push(u); continue
        # faces around u: shared with v die; others get u->v. check flips and topology (link condition).
        nu, nv = neigh(u), neigh(v)
        common = nu & nv
        shared = [fi for fi in vf[u] if alive[fi] and v in F[fi]]
        if len(common) != len(shared):  # non-manifold link condition
            ver[u] += 1; continue
        ok = True
        for fi in vf[u]:
            if not alive[fi] or v in F[fi]: continue
            f = F[fi]; a, b, cc = [V[x] for x in f]
            n0 = np.cross(b - a, cc - a)
            nf = [v if x == u else x for x in f]
            a2, b2, c2 = [V[x] for x in nf]
            n1 = np.cross(b2 - a2, c2 - a2)
            l0, l1 = np.linalg.norm(n0), np.linalg.norm(n1)
            if l1 < 1e-12 or n0.dot(n1) < 0.25 * l0 * l1: ok = False; break
        if not ok:
            ver[u] += 1; continue
        # uv for v inside the island of u: from a shared face
        vt_v = None
        for fi in shared:
            vt_v = T[fi][F[fi].index(v)]; break
        for fi in shared:
            alive[fi] = False; ntri -= 1
            for x in F[fi]:
                if x != u: vf[x].discard(fi)
        for fi in list(vf[u]):
            if not alive[fi]: continue
            k = F[fi].index(u); F[fi][k] = v; T[fi][k] = vt_v
            vf[v].add(fi)
        del vf[u]
        Q[v] = Q[v] + Q[u]
        ver[v] = ver.get(v, 0) + 1
        for w in neigh(v) | {v}:
            if w in vf: ver[w] = ver.get(w, 0) + 1; push(w)
    keep = [i for i in range(len(F)) if alive[i]]
    return [F[i] for i in keep], [T[i] for i in keep], keep

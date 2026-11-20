"""Paint skin textures in the hm08 UV layout: pores, warmth, lips, brows, eye shadow, optional stubble. Tinted per person at runtime."""
import numpy as np
from PIL import Image
from scipy import ndimage
from mhcore import *

S = 2048
V0, VT0, FACES = load_obj(MH + '/3dobjs/base.obj')
FT, TT = [], []
for g, vs, ts in FACES:
    if g == 'body':
        for k in range(1, len(vs) - 1): FT.append([vs[0], vs[k], vs[k + 1]]); TT.append([ts[0], ts[k], ts[k + 1]])
FT = np.array(FT); TT = np.array(TT)

def rasterize(P, N):
    """Per texel: 3D position and normal (from the morphed body)."""
    pos = np.zeros((S, S, 3), np.float32); nrm = np.zeros((S, S, 3), np.float32); mask = np.zeros((S, S), bool)
    uv = VT0[TT] * (S - 1)
    uv[:, :, 1] = (S - 1) - uv[:, :, 1]
    for i in range(len(FT)):
        t = uv[i]; x0, y0 = np.floor(t.min(0)).astype(int); x1, y1 = np.ceil(t.max(0)).astype(int)
        if x1 < 0 or y1 < 0 or x0 >= S or y0 >= S: continue
        x0, y0 = max(x0, 0), max(y0, 0); x1, y1 = min(x1, S - 1), min(y1, S - 1)
        xs, ys = np.meshgrid(np.arange(x0, x1 + 1), np.arange(y0, y1 + 1))
        px = np.stack([xs.ravel() + 0.5, ys.ravel() + 0.5], 1)
        a, b, c = t
        m = np.array([[b[0] - a[0], c[0] - a[0]], [b[1] - a[1], c[1] - a[1]]])
        det = np.linalg.det(m)
        if abs(det) < 1e-9: continue
        l = np.linalg.solve(m, (px - a).T).T
        w = np.column_stack([1 - l.sum(1), l])
        ok = (w >= -0.02).all(1)
        if not ok.any(): continue
        w = w[ok]; X = xs.ravel()[ok]; Y = ys.ravel()[ok]
        f = FT[i]
        pos[Y, X] = w @ P[f]; nrm[Y, X] = w @ N[f]; mask[Y, X] = True
    return pos, nrm, mask

def smooth_noise(shape, sigma, seed):
    r = np.random.default_rng(seed).standard_normal(shape)
    n = ndimage.gaussian_filter(r, sigma)
    return n / (n.std() + 1e-9)

def paint(gender, stubble, seed=1):
    Pm = morph(V0, gender=gender)
    P = Pm * 0.1; P[:, 0] *= -1
    N = np.zeros_like(P)
    tri = P[FT]; fn = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])
    for k in range(3): np.add.at(N, FT[:, k], fn)
    N /= np.linalg.norm(N, axis=1, keepdims=True) + 1e-9
    rig = Rig()
    J = lambda n: (lambda q: np.array([-q[0], q[1], q[2]]))(rig.joint(Pm, n) * 0.1)
    eL, eR = J('eye.L____head'), J('eye.R____head'); eyeY = (eL[1] + eR[1]) / 2; ez = (eL[2] + eR[2]) / 2
    hv = np.unique(FT)
    hd = hv[(P[hv, 1] > eyeY - 0.12) & (P[hv, 1] < eyeY + 0.15)]
    near = hd[(np.abs(P[hd, 0]) < 0.012)]
    nose = near[(P[near, 1] < eyeY - 0.01) & (P[near, 1] > eyeY - 0.07)]
    noseTip = P[nose[np.argmax(P[nose, 2])]]
    chinC = near[(P[near, 1] < noseTip[1] - 0.04) & (P[near, 1] > noseTip[1] - 0.1)]
    chin = P[chinC[np.argmax(P[chinC, 2])]]
    mouthY = noseTip[1] + (chin[1] - noseTip[1]) * 0.44
    mz = near[np.abs(P[near, 1] - mouthY) < 0.004]
    mouth = np.array([0, mouthY, P[mz, 2].max() if len(mz) else noseTip[2] - 0.02])
    pos, nrm, mask = rasterize(P, N)
    X, Y, Z = pos[..., 0], pos[..., 1], pos[..., 2]
    base = np.array([0.92, 0.76, 0.66], np.float32)
    img = np.ones((S, S, 3), np.float32) * base
    # skin variation: blotchy warmth + fine pores
    lo = smooth_noise((S, S), 18, seed); mid = smooth_noise((S, S), 3, seed + 1); fine = smooth_noise((S, S), 0.7, seed + 2)
    img *= (1 + 0.018 * lo + 0.012 * mid + 0.02 * fine)[..., None]
    redd = np.array([0.0, -0.06, -0.05], np.float32)
    def blob(c, r, s=1.0):
        d = ((X - c[0]) ** 2 + (Y - c[1]) ** 2 + (Z - c[2]) ** 2) / (r * r)
        return np.exp(-d) * s
    face = (Z > ez - 0.05) & (Y > chin[1] - 0.03) & (Y < eyeY + 0.08)
    warm = blob(noseTip, 0.018, 0.9) + blob([eL[0] - 0.02, eyeY - 0.035, ez + 0.01], 0.025, 0.6) + blob([eR[0] + 0.02, eyeY - 0.035, ez + 0.01], 0.025, 0.6)
    ears = ((np.abs(X) > 0.065) & (np.abs(Y - (eyeY - 0.015)) < 0.035) & (Z < ez - 0.05) & (Z > ez - 0.11)).astype(np.float32) * 0.5
    img += (warm + ears)[..., None] * redd
    # eye sockets slightly darker, eyelid crease
    for e in (eL, eR):
        img *= (1 - 0.1 * blob(e + np.array([0, 0.002, 0.012]), 0.018))[..., None]
    # lips
    dx = (X - mouth[0]) / (0.024 if gender < 0.5 else 0.026)
    dy = (Y - mouth[1]) / (0.0085 if gender < 0.5 else 0.007)
    lip = np.clip(1.2 - (dx ** 2 + dy ** 2), 0, 1) * (Z > mouth[2] - 0.018) * (nrm[..., 2] > 0.2)
    lip = ndimage.gaussian_filter(lip, 1.2)
    lipc = np.array([0.72, 0.42, 0.42] if gender < 0.5 else [0.74, 0.5, 0.47], np.float32)
    img = img * (1 - lip[..., None] * 0.75) + lipc * lip[..., None] * 0.75
    img *= (1 - 0.25 * np.exp(-((X / 0.022) ** 2 + ((Y - mouth[1]) / 0.0012) ** 2)) * (Z > mouth[2] - 0.012))[..., None]  # lip line
    # eyebrows (dark, strand texture)
    strands = np.clip(0.5 + 0.5 * smooth_noise((S, S), 0.6, seed + 3), 0, 1)
    brow = np.zeros((S, S), np.float32)
    for e, sgn in ((eL, -1), (eR, 1)):
        u = (X - e[0]) * sgn  # + towards the outside
        within = (u > -0.022) & (u < 0.027) & (Z > ez - 0.025)
        yc = e[1] + 0.016 + 0.004 * (1 - ((u - 0.004) / 0.025) ** 2) - 0.004 * np.clip(u / 0.027, 0, 1)
        th = (0.0042 if gender < 0.5 else 0.0058) * (1 - 0.5 * np.clip((u + 0.022) / 0.05, 0, 1) ** 2)
        brow = np.maximum(brow, within * np.clip(1 - np.abs(Y - yc) / th, 0, 1))
    brow = ndimage.gaussian_filter(brow, 0.8) * (0.55 + 0.45 * strands)
    browc = np.array([0.18, 0.12, 0.08], np.float32)
    img = img * (1 - brow[..., None] * 0.9) + browc * brow[..., None] * 0.9
    # stubble / shadow
    if stubble:
        jaw = (Y < noseTip[1] - 0.012) & (Y > chin[1] - 0.04) & (Z > ez - 0.07) & ~((np.abs(dx) < 1.1) & (np.abs(dy) < 1.3))
        jaw = ndimage.gaussian_filter(jaw.astype(np.float32), 4) * face
        speck = (np.random.default_rng(seed + 5).random((S, S)) > 0.55).astype(np.float32)
        st = jaw * (0.55 + 0.45 * ndimage.gaussian_filter(speck, 0.5))
        img = img * (1 - st[..., None] * 0.35) + np.array([0.3, 0.27, 0.27]) * st[..., None] * 0.35
    # fingernails / toenails left skin-coloured; dilate into gutters
    img = np.clip(img, 0, 1)
    idx = ndimage.distance_transform_edt(~mask, return_distances=False, return_indices=True)
    img = img[idx[0], idx[1]]
    return Image.fromarray((img * 255).astype(np.uint8))

if __name__ == '__main__':
    import os
    os.makedirs('/tmp/claude-0/charpipe/out/chars', exist_ok=True)
    for name, g, st in [('skin_f', 0.0, False), ('skin_m', 1.0, False), ('skin_m_stubble', 1.0, True)]:
        im = paint(g, st)
        im.save(f'/tmp/claude-0/charpipe/out/chars/{name}.png', optimize=True)
        print(name, 'done')

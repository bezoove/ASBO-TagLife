# Poly Haven 4K blend zips -> 2K maps for Unity URP Lit: albedo (jpg), normal (png, OpenGL), mask (png: R metal, A smoothness)
import sys, os, zipfile, io, glob
import numpy as np, OpenEXR
from PIL import Image
src, out = sys.argv[1], sys.argv[2]
os.makedirs(out, exist_ok=True)
S = 2048
def exr(data):
    tmp = os.path.join(out, '_t.exr'); open(tmp, 'wb').write(data)
    f = OpenEXR.File(tmp); ch = f.channels(); k = list(ch.keys())[0]; a = np.asarray(ch[k].pixels, dtype=np.float32); os.remove(tmp)
    return a
for z in sorted(glob.glob(os.path.join(src, '*_4k_blend.zip'))):
    name = os.path.basename(z).replace('_4k_blend.zip', '')
    if os.path.exists(os.path.join(out, name + '_albedo.jpg')): continue
    zf = zipfile.ZipFile(z); names = zf.namelist()
    def find(tag): return next((n for n in names if tag in os.path.basename(n)), None)
    d = find('_diff_'); n = find('_nor_gl_'); r = find('_rough_'); m = find('_metal_'); arm = find('_arm_')
    if d is None or n is None: print('skip', name, names); continue
    Image.open(io.BytesIO(zf.read(d))).convert('RGB').resize((S, S), Image.LANCZOS).save(os.path.join(out, name + '_albedo.jpg'), quality=90)
    nd = zf.read(n); na = exr(nd) if n.endswith('.exr') else np.asarray(Image.open(io.BytesIO(nd)).convert('RGB'), np.float32) / 255.0
    Image.fromarray((np.clip(na[..., :3], 0, 1) * 255 + 0.5).astype(np.uint8)).resize((S, S), Image.LANCZOS).save(os.path.join(out, name + '_normal.png'), optimize=True)
    def gray(p):
        if p is None: return None
        b = zf.read(p); a = exr(b) if p.endswith('.exr') else np.asarray(Image.open(io.BytesIO(b)).convert('L'), np.float32) / 255.0
        return a[..., 0] if a.ndim == 3 else a
    rough = gray(r); metal = gray(m)
    if rough is None: rough = np.full((S, S), 0.8, np.float32)
    rough = np.asarray(Image.fromarray((np.clip(rough, 0, 1) * 255).astype(np.uint8)).resize((S, S), Image.LANCZOS), np.float32) / 255.0
    metal = np.zeros((S, S), np.float32) if metal is None else np.asarray(Image.fromarray((np.clip(metal, 0, 1) * 255).astype(np.uint8)).resize((S, S), Image.LANCZOS), np.float32) / 255.0
    mask = np.zeros((S, S, 4), np.uint8); mask[..., 0] = (metal * 255).astype(np.uint8); mask[..., 1] = 255; mask[..., 3] = ((1 - rough) * 255).astype(np.uint8)
    Image.fromarray(mask, 'RGBA').save(os.path.join(out, name + '_mask.png'), optimize=True)
    print(name, 'metal' if m else '', [os.path.basename(x) for x in names if '/' in x])

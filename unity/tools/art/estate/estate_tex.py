"""
Textures for the council estate (all generated here, own work):
  Surfaces/buff_brick/       yellow-buff stock brick in stretcher bond (albedo, normal, metal/smoothness mask), 1.8 m repeat
  Estate/estate_windows.png  white uPVC window bands: three upper-floor variants (nets, curtains, blinds) and a small
                             ground-floor window; alpha = smoothness (glass shiny, frames satin)
  Estate/estate_doors.png    four front doors (white uPVC half-glazed, red, blue, black with fanlight)
  Estate/garage_door.png     ribbed up-and-over steel garage door (grey, tinted per house by the material)
  Estate/sign_*.png          black-on-white street name plates (made-up names)
Run: python3 estate_tex.py <unity Assets folder>
"""
import sys, os, math
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ASSETS = sys.argv[1]
SURF = os.path.join(ASSETS, 'Textures', 'Surfaces', 'buff_brick'); EST = os.path.join(ASSETS, 'Art', 'Estate')
os.makedirs(SURF, exist_ok=True); os.makedirs(EST, exist_ok=True)
rng = np.random.default_rng(1966)
FONT = '/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf'

def blur(a, r):
    from scipy.ndimage import gaussian_filter
    return gaussian_filter(a.astype(np.float32), r, mode='wrap')

def tile_noise(n, scale, seed):
    """Tileable value noise via FFT-filtered white noise."""
    g = np.random.default_rng(seed).standard_normal((n, n))
    f = np.fft.fftfreq(n)[:, None] ** 2 + np.fft.fftfreq(n)[None, :] ** 2
    k = np.exp(-f * (n / scale) ** 2 * 6.0)
    out = np.real(np.fft.ifft2(np.fft.fft2(g) * k))
    out -= out.min(); out /= out.max() + 1e-9
    return out

def normal_from_height(h, strength):
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * strength
    dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * strength
    nz = np.ones_like(h)
    n = np.stack([-dx, dy, nz], -1)  # OpenGL (Y up) convention, as Unity expects
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return ((n * 0.5 + 0.5) * 255).astype(np.uint8)

# ------------------------------------------------------------------ buff brick
def brick():
    N = 1024; cols, rows = 8, 24              # 8 x 225 mm, 24 x 75 mm = 1.8 m square
    bw, bh = N / cols, N / rows; joint = 5.5
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    row = np.floor(yy / bh).astype(int)
    off = (row % 2) * (bw / 2)
    xs = (xx + off) % N
    col = np.floor(xs / bw).astype(int)
    lx = xs - col * bw; ly = yy - row * bh
    # distance inside the brick from its edges (mortar where small)
    d = np.minimum(np.minimum(lx, bw - lx), np.minimum(ly, bh - ly))
    jitter = tile_noise(N, 40, 3) * 2.2
    mortar = d < (joint / 2 + jitter * 0.5)
    bid = (row * 131 + col * 17) % 9973
    # per-brick colour: buff/yellow stock with a few darker, browner and greyer ones
    pal = np.array([[201, 176, 118], [190, 163, 104], [212, 189, 135], [176, 150, 98], [196, 160, 108],
                    [168, 141, 96], [205, 182, 128], [158, 132, 92], [214, 196, 150]], np.float32)
    pal = pal.mean(0) + (pal - pal.mean(0)) * 0.5   # real buff stock is fairly even: keep the spread gentle
    pick = (bid * 7919) % len(pal)
    base = pal[pick]
    br = np.random.default_rng(5).uniform(0.94, 1.05, 9973)[bid][..., None]
    col_img = base * br
    # surface grain, speckles (dark burnt bits typical of stock bricks), soft clouding
    grain = tile_noise(N, 3, 11)[..., None]; cloud = tile_noise(N, 60, 12)[..., None]
    col_img *= (0.9 + grain * 0.18) * (0.93 + cloud * 0.12)
    spk = np.random.default_rng(13).random((N, N)) > 0.9975
    spk = blur(spk.astype(np.float32) * 255, 0.8) / 255
    col_img *= (1 - np.clip(spk * 2.2, 0, 0.6))[..., None]
    # edges a touch darker and worn
    edge = np.clip(1 - (d - joint / 2) / 6, 0, 1)
    col_img *= (1 - edge * 0.12)[..., None]
    # mortar: light grey-cream with grit
    mcol = np.array([182, 176, 160], np.float32) * (0.88 + tile_noise(N, 4, 21)[..., None] * 0.2)
    img = np.where(mortar[..., None], mcol, col_img)
    # a little grime running down from the joints
    grime = blur((mortar * 1.0).astype(np.float32), 3) * tile_noise(N, 90, 31)
    img *= (1 - grime * 0.12)[..., None]
    Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)).save(os.path.join(SURF, 'buff_brick_albedo.jpg'), quality=92)
    # height: bricks proud, mortar recessed, pitted faces, rounded arrises
    h = np.where(mortar, 0.0, 1.0).astype(np.float32)
    h = blur(h * 255, 1.2) / 255
    h += (tile_noise(N, 2.5, 41) - 0.5) * 0.08 * (~mortar) + (tile_noise(N, 14, 42) - 0.5) * 0.1
    Image.fromarray(normal_from_height(h, 3.2)).save(os.path.join(SURF, 'buff_brick_normal.png'))
    # mask: R metallic 0, G 255, A smoothness (brick dull, mortar duller)
    sm = np.where(mortar, 16, 34).astype(np.uint8)
    mask = np.zeros((N, N, 4), np.uint8); mask[..., 1] = 255; mask[..., 3] = sm
    Image.fromarray(mask, 'RGBA').save(os.path.join(SURF, 'buff_brick_mask.png'))
    print('brick done')

# ------------------------------------------------------------------ windows
WHITE = (236, 236, 232)
def glass_bg(w, h, seed):
    """Dark glass with a sky reflection gradient and a soft diagonal sheen."""
    g = np.random.default_rng(seed)
    y = np.linspace(0, 1, h)[:, None]; x = np.linspace(0, 1, w)[None, :]
    sky = np.array([120, 138, 158], np.float32); dark = np.array([34, 40, 46], np.float32)
    t = np.clip(0.55 - y * 0.75 + (x - 0.5) * 0.05, 0, 1)[..., None]
    img = dark + (sky - dark) * t * 0.85
    sheen = np.exp(-((x * 1.2 + y * 0.8 - 0.55 - g.uniform(-0.2, 0.2)) ** 2) / 0.004)[..., None]
    img += sheen * 40
    return img

def nets(img, x0, y0, x1, y1, seed):
    g = np.random.default_rng(seed)
    w, h = x1 - x0, y1 - y0
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    folds = 0.9 + 0.1 * np.sin(xx / max(6, w / 7) * math.pi + g.uniform(0, 6))
    lace = ((np.sin(xx * 0.7) * np.sin(yy * 0.7)) > 0.75) * 0.06
    hem = np.clip((yy - (h - 10)) / 10, 0, 1) * 0.1
    a = np.clip(0.72 * folds - lace + hem, 0, 1)[..., None]
    col = np.array([226, 226, 220], np.float32)
    img[y0:y1, x0:x1] = img[y0:y1, x0:x1] * (1 - a) + col * a

def curtains(img, x0, y0, x1, y1, seed):
    g = np.random.default_rng(seed)
    palette = [(120, 40, 44), (60, 70, 110), (150, 130, 90), (80, 100, 70), (190, 170, 140), (110, 80, 120)]
    c = np.array(palette[g.integers(len(palette))], np.float32)
    w = x1 - x0; side = int(w * g.uniform(0.15, 0.28))
    for (a, b) in ((x0, x0 + side), (x1 - side, x1)):
        xx = np.arange(b - a, dtype=np.float32)
        fold = (0.7 + 0.3 * np.sin(xx / 4.0))[None, :, None]
        img[y0:y1, a:b] = c * fold
    # dim room in between, a lampshade or the back wall
    room = np.array([46, 42, 38], np.float32) * g.uniform(0.8, 1.4)
    img[y0 + 2:y1, x0 + side:x1 - side] = img[y0 + 2:y1, x0 + side:x1 - side] * 0.45 + room * 0.55

def blinds(img, x0, y0, x1, y1, seed):
    g = np.random.default_rng(seed)
    c = np.array([(222, 218, 205), (200, 205, 210), (170, 160, 140)][g.integers(3)], np.float32)
    w = x1 - x0; xx = np.arange(w, dtype=np.float32)
    slat = (0.85 + 0.15 * np.cos(xx / 5.0 * math.pi))[None, :, None]
    drop = int((y1 - y0) * g.uniform(0.55, 1.0))
    img[y0:y0 + drop, x0:x1] = c * slat

def frame_unit(img, x0, y0, x1, y1, lights, fill, seed):
    """A uPVC window: outer frame, mullions; each light gets glass plus a fill (nets / curtains / blinds / none)."""
    d = 7
    img[y0:y1, x0:x1] = WHITE
    xs = [x0 + d] + [int(x0 + (x1 - x0) * f) for f in lights] + [x1 - d]
    for i in range(len(xs) - 1):
        a, b = xs[i] + (0 if i == 0 else 4), xs[i + 1] - (0 if i == len(xs) - 2 else 4)
        top = y0 + d; bot = y1 - d
        img[top:bot, a:b] = glass_bg(b - a, bot - top, seed + i)
        if fill == 'nets': nets(img, a, top + 3, b, bot, seed + i)
        elif fill == 'blinds': blinds(img, a, top, b, bot, seed + i)
        # top-hung opener: a transom across a third of the way down with its own frame line
        if i % 2 == 0:
            t = top + int((bot - top) * 0.3)
            img[t - 3:t + 3, a:b] = WHITE
    if fill == 'curtains': curtains(img, x0 + d, y0 + d, x1 - d, y1 - d, seed)
    # frame shading: inner edge shadow and a sill
    img[y1 - 6:y1, x0:x1] = (200, 200, 196)

def windows():
    W, H = 1024, 1024
    img = np.zeros((H, W, 3), np.float32); img[:] = WHITE
    sm = np.full((H, W), 110, np.uint8)
    rows = [('nets', 101), ('curtains', 202), ('blinds', 303)]
    for r, (fill, seed) in enumerate(rows):
        y0 = r * 256
        frame_unit(img, 0, y0 + 4, W, y0 + 252, [0.18, 0.5, 0.82], fill, seed)
    # bottom row: small ground-floor window (left half), a two-light patio unit (right half)
    frame_unit(img, 0, 772, 512, 1020, [0.5], 'nets', 404)
    frame_unit(img, 512, 772, 1024, 1020, [0.5], 'curtains', 505)
    # smoothness: glass (dark-ish, not white frame) high
    lum = img.mean(-1)
    isframe = (np.abs(img - np.array(WHITE)).sum(-1) < 30)
    sm[~isframe] = 200
    out = np.dstack([np.clip(img, 0, 255).astype(np.uint8), sm])
    Image.fromarray(out, 'RGBA').save(os.path.join(EST, 'estate_windows.png'))
    print('windows done')

def doors():
    W, H = 1024, 512
    img = Image.new('RGB', (W, H), WHITE); d = ImageDraw.Draw(img)
    specs = [((236, 236, 232), True), ((132, 24, 26), False), ((28, 48, 96), False), ((24, 24, 26), True)]
    for i, (col, glazed) in enumerate(specs):
        x0 = i * 256; d.rectangle([x0, 0, x0 + 255, 511], fill=(225, 225, 220))  # frame
        d.rectangle([x0 + 12, 12, x0 + 243, 511], fill=col)
        edge = tuple(max(0, c - 22) for c in col)
        if glazed:
            d.rectangle([x0 + 40, 40, x0 + 215, 230], fill=(48, 56, 64)); d.rectangle([x0 + 40, 40, x0 + 215, 230], outline=edge, width=5)
            for k in range(3): d.line([x0 + 40, 40 + k * 64 + 32, x0 + 215, 40 + k * 64 + 32], fill=(70, 80, 88), width=2)
        else:
            d.rectangle([x0 + 40, 40, x0 + 215, 110], fill=(48, 56, 64))  # small vision panel
            d.rectangle([x0 + 40, 140, x0 + 215, 300], outline=edge, width=5)
        d.rectangle([x0 + 40, 330, x0 + 215, 480], outline=edge, width=5)
        d.rectangle([x0 + 95, 300, x0 + 160, 316], fill=(150, 140, 110))   # letterbox
        d.rectangle([x0 + 205, 250, x0 + 225, 262], fill=(170, 160, 120))  # handle
    img.save(os.path.join(EST, 'estate_doors.png')); print('doors done')

def garage():
    N = 512
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    v = 168 + 22 * np.cos(xx / (N / 22) * 2 * math.pi)          # vertical ribs
    v *= 0.94 + 0.06 * tile_noise(N, 30, 61)
    v = np.where((yy < 10) | (yy > N - 10) | (xx < 10) | (xx > N - 10), 120, v)
    v[int(N * 0.55):int(N * 0.58), int(N * 0.44):int(N * 0.56)] = 70   # handle
    dirt = np.clip((yy / N - 0.7) * 2, 0, 1) * tile_noise(N, 25, 62) * 50
    v -= dirt
    Image.fromarray(np.clip(v, 0, 255).astype(np.uint8)).convert('RGB').save(os.path.join(EST, 'garage_door.png')); print('garage done')

def sign(text, fname, sub=None):
    W, H = 1024, 256
    img = Image.new('RGB', (W, H), (245, 245, 242)); d = ImageDraw.Draw(img)
    d.rectangle([6, 6, W - 7, H - 7], outline=(20, 20, 20), width=8)
    f = ImageFont.truetype(FONT, 120 if sub is None else 104)
    tw = d.textlength(text, font=f)
    d.text(((W - tw) / 2, 50 if sub is None else 26), text, font=f, fill=(15, 15, 15))
    if sub:
        f2 = ImageFont.truetype(FONT, 44); tw2 = d.textlength(sub, font=f2)
        d.text(((W - tw2) / 2, 164), sub, font=f2, fill=(15, 15, 15))
    img.save(os.path.join(EST, fname)); print('sign', text)

brick(); windows(); doors(); garage()
sign('HAWTHORN WAY', 'sign_hawthorn_way.png')
sign('HAWTHORN WAY', 'sign_hawthorn_way_1_28.png', '1-28')

"""
Textures for the high street (all generated here, own work; every shop and business name is made up):
  Surfaces/red_brick_vic/   Victorian red brick in Flemish bond, 2.7 m repeat (albedo, normal, mask)
  Town/sash.png             painted timber sash windows, four variants (nets, curtains, blind, dark); alpha = smoothness
  Town/shopfronts.png       atlas of shopfronts, 4 x 5 cells of 512 x 384: fascia sign on top, window display and door below
  Town/pub_tiles.png        green glazed pub tiles with a darker plinth (1 m repeat)
  Town/pub_sign.png         The Eggy Duck hanging sign (painted board)
Run: python3 town_tex.py <unity Assets folder>
"""
import sys, os, math
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy.ndimage import gaussian_filter

A = sys.argv[1]
SURF = os.path.join(A, 'Textures', 'Surfaces', 'red_brick_vic'); OUT = os.path.join(A, 'Art', 'Town')
os.makedirs(SURF, exist_ok=True); os.makedirs(OUT, exist_ok=True)
F = '/usr/share/fonts/truetype/liberation/'
SANS, SANSR, SERIF, NARROW = F + 'LiberationSans-Bold.ttf', F + 'LiberationSans-Regular.ttf', F + 'LiberationSerif-Bold.ttf', F + 'LiberationSansNarrow-Bold.ttf'

def tile_noise(n, scale, seed):
    g = np.random.default_rng(seed).standard_normal((n, n))
    f = np.fft.fftfreq(n)[:, None] ** 2 + np.fft.fftfreq(n)[None, :] ** 2
    out = np.real(np.fft.ifft2(np.fft.fft2(g) * np.exp(-f * (n / scale) ** 2 * 6.0)))
    out -= out.min(); return out / (out.max() + 1e-9)

def normal_from_height(h, s):
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * s; dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * s
    n = np.stack([-dx, dy, np.ones_like(h)], -1); n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return ((n * 0.5 + 0.5) * 255).astype(np.uint8)

def red_brick():
    N = 1024; mod = N / 8.0; rows = 36; bh = N / rows; joint = 3.6
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    row = np.floor(yy / bh).astype(int); ly = yy - row * bh
    off = (row % 2) * (mod * 0.75)          # Flemish bond: stretcher + header, courses offset by a quarter brick
    xs = (xx + off) % N
    m = np.floor(xs / mod).astype(int); lm = xs - m * mod
    header = lm >= mod * (2.0 / 3.0)
    lx = np.where(header, lm - mod * (2.0 / 3.0), lm); bw = np.where(header, mod / 3.0, mod * (2.0 / 3.0))
    d = np.minimum(np.minimum(lx, bw - lx), np.minimum(ly, bh - ly))
    mortar = d < joint / 2 + tile_noise(N, 40, 3) * 1.2
    bid = (row * 211 + m * 37 + header * 7) % 9973
    pal = np.array([[150, 62, 42], [162, 72, 48], [138, 56, 40], [170, 84, 56], [128, 52, 38], [156, 76, 52], [146, 66, 50]], np.float32)
    base = pal[(bid * 7919) % len(pal)]
    base = np.where(header[..., None], base * np.array([0.72, 0.66, 0.68]), base)   # burnt headers, darker and bluer
    br = np.random.default_rng(5).uniform(0.9, 1.08, 9973)[bid][..., None]
    img = base * br * (0.9 + tile_noise(N, 3, 11)[..., None] * 0.18) * (0.92 + tile_noise(N, 70, 12)[..., None] * 0.14)
    soot = tile_noise(N, 120, 13)[..., None]; img *= 1 - np.clip(soot - 0.55, 0, 1) * 0.6   # sooty patches from coal-fire years
    mcol = np.array([176, 170, 156], np.float32) * (0.85 + tile_noise(N, 4, 21)[..., None] * 0.2)
    img = np.where(mortar[..., None], mcol, img)
    Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)).save(os.path.join(SURF, 'red_brick_vic_albedo.jpg'), quality=92)
    h = gaussian_filter(np.where(mortar, 0.0, 1.0).astype(np.float32), 1.0, mode='wrap')
    h += (tile_noise(N, 2.5, 41) - 0.5) * 0.1 * (~mortar)
    Image.fromarray(normal_from_height(h, 3.0)).save(os.path.join(SURF, 'red_brick_vic_normal.png'))
    mask = np.zeros((N, N, 4), np.uint8); mask[..., 1] = 255; mask[..., 3] = np.where(mortar, 14, 40)
    Image.fromarray(mask, 'RGBA').save(os.path.join(SURF, 'red_brick_vic_mask.png'))

def glass(w, h, seed):
    y = np.linspace(0, 1, h)[:, None]; x = np.linspace(0, 1, w)[None, :]
    t = np.clip(0.6 - y * 0.8 + (x - 0.5) * 0.06, 0, 1)[..., None]
    img = np.array([30, 34, 40], np.float32) + (np.array([120, 136, 155], np.float32) - [30, 34, 40]) * t * 0.8
    img += np.exp(-((x * 1.3 + y * 0.7 - 0.6) ** 2) / 0.006)[..., None] * 35
    return img

def sash():
    W, H = 1024, 512; img = np.full((H, W, 3), 236, np.float32); sm = np.full((H, W), 90, np.uint8)
    white = np.array([238, 238, 232], np.float32)
    for v in range(4):
        x0 = v * 256
        img[:, x0:x0 + 256] = white
        bars = 12
        for half in range(2):   # top sash, bottom sash
            y0, y1 = 14 + half * 246, 252 + half * 246 - (4 if half else 0)
            for pane in range(2):
                a = x0 + bars + pane * 116 + (6 if pane else 0); b = a + 110
                g = glass(b - a, y1 - y0, v * 10 + half * 3 + pane)
                if v == 0: g = g * 0.35 + np.array([224, 224, 218]) * 0.65 * (0.92 + 0.08 * np.sin(np.arange(b - a) / 4.0))[None, :, None]
                if v == 1 and pane in (0, 1):
                    side = 30
                    col = np.array([[110, 30, 36], [60, 74, 120], [150, 130, 80]][half % 3], np.float32)
                    if pane == 0: g[:, :side] = col
                    else: g[:, -side:] = col
                if v == 2 and half == 0: g[: int((y1 - y0) * 0.75)] = np.array([220, 214, 196], np.float32) * (0.9 + 0.1 * np.cos(np.arange(int((y1 - y0) * 0.75)) / 3.0))[:, None, None]
                if v == 3: g *= 0.55
                img[y0:y1, a:b] = g; sm[y0:y1, a:b] = 205
        img[250:262, x0:x0 + 256] = white * 0.95   # meeting rail
    Image.fromarray(np.dstack([np.clip(img, 0, 255).astype(np.uint8), sm]), 'RGBA').save(os.path.join(OUT, 'sash.png'))

SHOPS = [
    # name, sub, fascia colour, text colour, window kind
    ("RAJ'S MINI MARKET", 'Groceries  ·  Beers & Wines  ·  Open 7 till 11', (22, 110, 60), (255, 255, 255), 'posters'),
    ('GOLDEN DRAGON', 'Chinese Takeaway  ·  Collection & Delivery', (170, 22, 24), (250, 210, 70), 'lanterns'),
    ('THE CODFATHER', 'Traditional Fish & Chips', (24, 60, 130), (255, 255, 255), 'counter'),
    ('WINNERS CORNER', 'Bets  ·  Racing  ·  Football', (20, 70, 40), (250, 220, 60), 'frosted'),
    ('SPIN CITY', 'Launderette  ·  Service Washes', (40, 140, 190), (255, 255, 255), 'machines'),
    ('KUTZ', 'Barbers  ·  Walk-ins welcome', (20, 20, 22), (230, 230, 230), 'chairs'),
    ('TO LET', 'Retail unit available  ·  enquiries on 01632 960 000', (210, 210, 205), (40, 40, 40), 'whitewash'),
    ('WEND HOSPICE SHOP', 'Every penny helps', (110, 40, 120), (255, 255, 255), 'clothes'),
    ('POST & PARCELS', 'Parcels  ·  Bills  ·  Cash withdrawals', (30, 50, 110), (255, 255, 255), 'posters'),
    ('WEND PHARMACY', 'Prescriptions  ·  Advice  ·  Health', (245, 245, 242), (20, 120, 60), 'shelves'),
    ('POUND PALACE', 'Everything a pound  ·  and more', (250, 210, 30), (200, 20, 30), 'crammed'),
    ('CLOUD NINE VAPES', 'Liquids  ·  Kits  ·  Coils', (70, 30, 110), (240, 240, 255), 'shelves'),
    ('NAILED IT', 'Nails  ·  Brows  ·  Lashes', (230, 120, 170), (255, 255, 255), 'chairs'),
    ("CRUSTY'S BAKERY", 'Fresh bread  ·  Pasties  ·  Cakes', (120, 72, 36), (250, 236, 200), 'bread'),
    ('THE EGGY DUCK', 'Free House  ·  Real Ales  ·  Live Football', (22, 60, 40), (226, 190, 100), 'pub'),
    # empty units (to let) in different states, and the estate parade's two shops
    ('', '', (150, 120, 80), (40, 40, 40), 'boarded'),                                   # 15 boarded up
    ('', '', (90, 92, 96), (40, 40, 40), 'shutter'),                                     # 16 shutter down
    ('VIDEO WORLD', 'New releases  ·  Games  ·  Hire', (60, 40, 70), (150, 140, 120), 'empty'),  # 17 old fascia, gone bust
    ('BUTCHERS', 'Quality meats  ·  Family butchers', (120, 30, 30), (200, 170, 150), 'butchers'),  # 18 old fascia, tiled and empty
    ('', '', (220, 220, 214), (60, 60, 60), 'closed'),                                   # 19 closed down, whitewashed, notices
    ('HAWTHORN STORES', 'Groceries  ·  Off Licence  ·  Papers', (30, 90, 150), (255, 255, 255), 'crammed'),   # 20
    ('TASTY KEBAB', 'Kebabs  ·  Pizza  ·  Burgers  ·  Open late', (200, 60, 20), (255, 240, 200), 'kebab'),  # 21
    ('CARPETS & BEDS', 'Clearance  ·  Everything must go', (40, 70, 110), (170, 170, 160), 'empty'),     # 22 old fascia
    ('', '', (30, 30, 34), (220, 220, 220), 'available'),                                # 23 empty, AVAILABLE banner
]
FADED = {17, 18, 22}

def window_fill(d, img, x0, y0, x1, y1, kind, rng):
    w, h = x1 - x0, y1 - y0
    g = glass(w, h, int(rng.integers(1000)))
    img[y0:y1, x0:x1] = g
    pil = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)); dd = ImageDraw.Draw(pil)
    if kind == 'posters':
        for i in range(6):
            px = x0 + 10 + int(rng.integers(0, max(1, w - 90))); py = y0 + 10 + int(rng.integers(0, max(1, h - 110)))
            col = tuple(int(c) for c in rng.integers(120, 255, 3)); dd.rectangle([px, py, px + 70, py + 95], fill=col)
            dd.rectangle([px + 8, py + 10, px + 62, py + 30], fill=(250, 250, 250)); dd.rectangle([px + 8, py + 40, px + 50, py + 48], fill=(40, 40, 40))
        for i in range(4): dd.rectangle([x0 + 6, y0 + h - 40 - i * 34, x1 - 6, y0 + h - 36 - i * 34], fill=(150, 150, 150))
    elif kind == 'lanterns':
        for i in range(4):
            cx = x0 + 30 + i * (w - 60) // 3; dd.line([cx, y0, cx, y0 + 30], fill=(40, 20, 10), width=2)
            dd.ellipse([cx - 22, y0 + 30, cx + 22, y0 + 80], fill=(200, 30, 30)); dd.rectangle([cx - 10, y0 + 78, cx + 10, y0 + 84], fill=(240, 200, 60))
        dd.rectangle([x0 + 20, y0 + 110, x1 - 20, y0 + 190], fill=(30, 20, 18)); 
        for r in range(5): dd.rectangle([x0 + 30, y0 + 118 + r * 14, x1 - 60, y0 + 124 + r * 14], fill=(240, 220, 160))
    elif kind == 'counter':
        dd.rectangle([x0, y1 - 80, x1, y1], fill=(170, 175, 180)); dd.rectangle([x0, y1 - 84, x1, y1 - 78], fill=(220, 225, 230))
        dd.rectangle([x0 + 20, y0 + 20, x1 - 20, y0 + 90], fill=(20, 30, 70))
        for r in range(4): dd.rectangle([x0 + 30, y0 + 28 + r * 15, x1 - 80, y0 + 34 + r * 15], fill=(255, 255, 255))
    elif kind == 'frosted':
        img2 = np.asarray(pil).astype(np.float32); img2[y0:y1, x0:x1] = img2[y0:y1, x0:x1] * 0.3 + 160
        pil = Image.fromarray(np.clip(img2, 0, 255).astype(np.uint8)); dd = ImageDraw.Draw(pil)
        dd.rectangle([x0 + 20, y0 + 30, x1 - 20, y0 + 110], fill=(20, 70, 40)); dd.text((x0 + 34, y0 + 48), 'TODAY\'S RACING', font=ImageFont.truetype(SANS, 26), fill=(250, 220, 60))
    elif kind == 'machines':
        for c in range(max(2, w // 80)):
            for r in range(2):
                cx = x0 + 45 + c * 80; cy = y0 + 60 + r * 90
                if cx + 35 > x1: continue
                dd.rectangle([cx - 36, cy - 40, cx + 36, cy + 40], fill=(230, 232, 235)); dd.ellipse([cx - 26, cy - 26, cx + 26, cy + 26], fill=(60, 70, 90)); dd.ellipse([cx - 18, cy - 18, cx + 18, cy + 18], fill=(110, 130, 160))
    elif kind == 'chairs':
        for c in range(3):
            cx = x0 + 50 + c * (w - 100) // 2
            dd.rectangle([cx - 25, y1 - 110, cx + 25, y1 - 60], fill=(30, 30, 32)); dd.rectangle([cx - 25, y1 - 160, cx + 25, y1 - 110], fill=(140, 20, 20))
            dd.rectangle([cx - 30, y0 + 20, cx + 30, y0 + 90], fill=(200, 210, 220))
    elif kind == 'whitewash':
        img2 = np.asarray(pil).astype(np.float32); img2[y0:y1, x0:x1] = img2[y0:y1, x0:x1] * 0.15 + 205 + rng.normal(0, 12, (h, w, 1))
        pil = Image.fromarray(np.clip(img2, 0, 255).astype(np.uint8)); dd = ImageDraw.Draw(pil)
        dd.rectangle([x0 + w // 4, y0 + h // 4, x0 + 3 * w // 4, y0 + h // 4 + 70], fill=(220, 40, 30)); dd.text((x0 + w // 4 + 20, y0 + h // 4 + 14), 'TO LET', font=ImageFont.truetype(SANS, 36), fill=(255, 255, 255))
    elif kind == 'clothes':
        for c in range(3):
            cx = x0 + 50 + c * (w - 100) // 2; col = tuple(int(v) for v in rng.integers(60, 230, 3))
            dd.ellipse([cx - 12, y0 + 40, cx + 12, y0 + 64], fill=(210, 190, 170)); dd.polygon([(cx - 30, y0 + 70), (cx + 30, y0 + 70), (cx + 36, y0 + 190), (cx - 36, y0 + 190)], fill=col)
    elif kind == 'shelves':
        for r in range(4):
            yy = y0 + 30 + r * 50; dd.rectangle([x0 + 10, yy + 34, x1 - 10, yy + 38], fill=(200, 200, 200))
            for i in range(0, w - 30, 14): dd.rectangle([x0 + 14 + i, yy + 8 + (i % 3) * 3, x0 + 24 + i, yy + 34], fill=tuple(int(v) for v in rng.integers(60, 255, 3)))
    elif kind == 'crammed':
        for i in range(60):
            px = x0 + int(rng.integers(0, w - 30)); py = y0 + int(rng.integers(0, h - 30)); s = int(rng.integers(14, 40))
            dd.rectangle([px, py, px + s, py + s], fill=tuple(int(v) for v in rng.integers(40, 255, 3)))
        dd.rectangle([x0 + 10, y0 + 10, x0 + 150, y0 + 60], fill=(250, 220, 40)); dd.text((x0 + 18, y0 + 18), '£1', font=ImageFont.truetype(SANS, 34), fill=(200, 20, 30))
    elif kind == 'bread':
        for r in range(3):
            yy = y0 + 50 + r * 60; dd.rectangle([x0 + 10, yy + 40, x1 - 10, yy + 44], fill=(150, 110, 70))
            for i in range(0, w - 60, 46): dd.ellipse([x0 + 16 + i, yy + 10, x0 + 56 + i, yy + 40], fill=(200, 150, 80))
    elif kind in ('boarded',):
        img2 = np.asarray(pil).astype(np.float32); ply = np.array([170, 140, 96], np.float32)
        img2[y0:y1, x0:x1] = ply * (0.85 + 0.15 * rng.random((h, 1, 1))) * (0.95 + 0.05 * np.sin(np.arange(w) / 3.0))[None, :, None]
        pil = Image.fromarray(np.clip(img2, 0, 255).astype(np.uint8)); dd = ImageDraw.Draw(pil)
        for i in range(3): dd.line([x0 + 10, y0 + (i + 1) * h // 4, x1 - 10, y0 + (i + 1) * h // 4], fill=(120, 96, 64), width=2)
        tag(dd, x0 + 30, y0 + h // 2, rng); posters(dd, x0 + w - 120, y0 + 20, rng)
        agent(dd, x0 + 20, y0 + 20)
    elif kind == 'shutter':
        img2 = np.asarray(pil).astype(np.float32)
        for yy in range(y0, y1): img2[yy, x0:x1] = 130 + 25 * math.sin(yy / 3.0) + rng.normal(0, 3)
        pil = Image.fromarray(np.clip(img2, 0, 255).astype(np.uint8)); dd = ImageDraw.Draw(pil)
        tag(dd, x0 + 40, y0 + h // 3, rng); tag(dd, x0 + w // 2, y0 + 2 * h // 3, rng); agent(dd, x0 + w - 170, y0 + 20)
    elif kind in ('empty', 'butchers', 'available'):
        img2 = np.asarray(pil).astype(np.float32)
        img2[y0:y1, x0:x1] = img2[y0:y1, x0:x1] * 0.45 + (np.array([235, 235, 230]) * 0.25 if kind == 'butchers' else 8)
        pil = Image.fromarray(np.clip(img2, 0, 255).astype(np.uint8)); dd = ImageDraw.Draw(pil)
        if kind == 'butchers':
            for i in range(0, w, 24): dd.line([x0 + i, y0, x0 + i, y1], fill=(170, 170, 165), width=1)
            for i in range(0, h, 24): dd.line([x0, y0 + i, x1, y0 + i], fill=(170, 170, 165), width=1)
        if kind == 'available':
            dd.rectangle([x0 + 20, y0 + h // 3, x1 - 20, y0 + h // 3 + 60], fill=(250, 200, 20)); dd.text((x0 + 36, y0 + h // 3 + 12), 'AVAILABLE NOW', font=ImageFont.truetype(SANS, 30), fill=(20, 20, 20))
        if kind == 'available': agent(dd, x1 - 170, y1 - 110)
        else: agent(dd, x0 + 24, y0 + 24)
        for i in range(3): dd.rectangle([x0 + 40 + i * 70, y1 - 30, x0 + 90 + i * 70, y1 - 22], fill=(200, 200, 190))   # post piled up behind the door
    elif kind == 'closed':
        img2 = np.asarray(pil).astype(np.float32); img2[y0:y1, x0:x1] = img2[y0:y1, x0:x1] * 0.15 + 200 + rng.normal(0, 14, (h, w, 1))
        pil = Image.fromarray(np.clip(img2, 0, 255).astype(np.uint8)); dd = ImageDraw.Draw(pil)
        dd.text((x0 + 30, y0 + 30), 'CLOSED', font=ImageFont.truetype(SANS, 44), fill=(160, 30, 30))
        for i in range(3): dd.rectangle([x0 + 30 + i * 70, y0 + 110, x0 + 90 + i * 70, y0 + 190], fill=(250, 250, 245)); dd.rectangle([x0 + 36 + i * 70, y0 + 118, x0 + 84 + i * 70, y0 + 124], fill=(30, 30, 30))
        agent(dd, x1 - 170, y1 - 110)
    elif kind == 'kebab':
        dd.rectangle([x0, y1 - 70, x1, y1], fill=(180, 180, 185)); dd.rectangle([x0 + 30, y0 + 70, x0 + 70, y1 - 70], fill=(150, 90, 50))
        dd.rectangle([x0 + 20, y0 + 60, x0 + 80, y0 + 70], fill=(80, 80, 80)); dd.rectangle([x0 + 100, y0 + 20, x1 - 20, y0 + 90], fill=(20, 20, 20))
        for r in range(4): dd.rectangle([x0 + 110, y0 + 28 + r * 15, x1 - 60, y0 + 34 + r * 15], fill=(250, 200, 60))
    elif kind == 'pub':
        img2 = np.asarray(pil).astype(np.float32)
        img2[y0 + h // 2:y1, x0:x1] = img2[y0 + h // 2:y1, x0:x1] * 0.35 + np.array([170, 120, 60]) * 0.4   # warm light behind half curtains
        pil = Image.fromarray(np.clip(img2, 0, 255).astype(np.uint8)); dd = ImageDraw.Draw(pil)
        dd.rectangle([x0, y0 + h // 2 - 4, x1, y0 + h // 2], fill=(120, 80, 40))
        for i in range(0, w, 10): dd.line([x0 + i, y0 + h // 2, x0 + i + 4, y1], fill=(150, 30, 30), width=3)
    return np.asarray(pil).astype(np.float32)

def tag(dd, x, y, rng):
    """A spray-painted tag: a loopy scribble with a drip or two (no words)."""
    col = [(30, 30, 30), (200, 30, 40), (40, 90, 200), (30, 160, 80)][int(rng.integers(4))]
    pts = [(x + i * 9, y + int(18 * math.sin(i * 1.3 + rng.random() * 2))) for i in range(14)]
    dd.line(pts, fill=col, width=5); dd.line([(px, py + 6) for px, py in pts[::3]], fill=col, width=3)
    for px, py in pts[2::5]: dd.line([px, py, px, py + 18], fill=col, width=2)

def posters(dd, x, y, rng):
    for i in range(3):
        c = tuple(int(v) for v in rng.integers(100, 255, 3)); dd.rectangle([x, y + i * 70, x + 90, y + i * 70 + 64], fill=c)
        dd.rectangle([x + 8, y + i * 70 + 8, x + 82, y + i * 70 + 22], fill=(20, 20, 20))

def agent(dd, x, y):
    """The letting agent's board (made-up agent, an Ofcom drama-range number)."""
    dd.rectangle([x, y, x + 150, y + 96], fill=(250, 250, 248)); dd.rectangle([x, y, x + 150, y + 30], fill=(200, 30, 40))
    dd.text((x + 22, y + 3), 'TO LET', font=ImageFont.truetype(SANS, 24), fill=(255, 255, 255))
    dd.text((x + 8, y + 40), 'WEND PROPERTY', font=ImageFont.truetype(SANS, 15), fill=(30, 30, 30))
    dd.text((x + 8, y + 64), '01632 960 000', font=ImageFont.truetype(SANSR, 15), fill=(30, 30, 30))

def shopfronts():
    CW, CH, cols = 512, 340, 4
    W, H = CW * cols, 2048
    img = np.full((H, W, 3), 200, np.float32); rng = np.random.default_rng(77)
    for i, (name, sub, fcol, tcol, kind) in enumerate(SHOPS):
        cx, cy = (i % cols) * CW, (i // cols) * CH
        # fascia (top 72 px): name and a smaller line under it
        pil = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)); d = ImageDraw.Draw(pil)
        d.rectangle([cx, cy, cx + CW - 1, cy + 71], fill=fcol)
        if name:
            font = ImageFont.truetype(SERIF if kind in ('pub', 'bread') else SANS, 40)
            while d.textlength(name, font=font) > CW - 30: font = ImageFont.truetype(font.path, font.size - 2)
            d.text((cx + (CW - d.textlength(name, font=font)) / 2, cy + 6), name, font=font, fill=tcol)
            f2 = ImageFont.truetype(SANSR, 15); d.text((cx + (CW - d.textlength(sub, font=f2)) / 2, cy + 50), sub, font=f2, fill=tcol)
        img = np.asarray(pil).astype(np.float32)
        if i in FADED or not name:
            # sun-faded, peeling, grubby: the old business's board (or a bare one)
            band = img[cy:cy + 72, cx:cx + CW]
            band[:] = band * 0.55 + np.array([150, 145, 135]) * 0.45
            for _ in range(9):
                px, py = int(rng.integers(0, CW - 60)), int(rng.integers(0, 50)); band[py:py + int(rng.integers(8, 22)), px:px + int(rng.integers(20, 70))] = (120, 105, 85)
            band *= (0.8 + 0.2 * rng.random((72, 1, 1)))
        # below: stallriser, shop window on the left two thirds, glazed door on the right
        y0 = cy + 72; frame = np.array([60, 60, 64], np.float32) if kind != 'pub' else np.array([30, 60, 40], np.float32)
        img[y0:cy + CH, cx:cx + CW] = frame
        wx0, wx1, wy0, wy1 = cx + 12, cx + int(CW * 0.66), y0 + 10, cy + CH - 52
        img[cy + CH - 46:cy + CH, cx:cx + CW] = frame * 0.8 + 20   # stallriser
        img = window_fill(None, img, wx0, wy0, wx1, wy1, kind, rng)
        dx0, dx1 = cx + int(CW * 0.70), cx + CW - 14
        img[wy0:cy + CH, dx0:dx1] = frame * 1.3
        img[wy0 + 12:cy + CH - 140, dx0 + 12:dx1 - 12] = glass(dx1 - dx0 - 24, cy + CH - 140 - wy0 - 12, i)
        img[cy + CH - 120:cy + CH - 112, dx1 - 30:dx1 - 16] = (200, 190, 150)
    Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)).save(os.path.join(OUT, 'shopfronts.png'))

def pub_tiles():
    N = 512; img = Image.new('RGB', (N, N), (24, 80, 52)); d = ImageDraw.Draw(img)
    for y in range(0, N, 64):
        for x in range(0, N, 32):
            c = (24 + (x * 7 + y) % 12, 84 + (x + y * 3) % 18, 54 + (x * 3) % 10); d.rectangle([x + 1, y + 1, x + 30, y + 62], fill=c)
            d.line([x + 4, y + 4, x + 4, y + 58], fill=(70, 140, 100), width=2)
    d.rectangle([0, N - 70, N, N], fill=(12, 34, 24)); d.rectangle([0, N - 76, N, N - 70], fill=(180, 150, 80))
    img.save(os.path.join(OUT, 'pub_tiles.png'))

def pub_sign():
    W, H = 512, 640; img = Image.new('RGB', (W, H), (26, 50, 36)); d = ImageDraw.Draw(img)
    d.rectangle([12, 12, W - 13, H - 13], outline=(200, 160, 70), width=10)
    d.rectangle([40, 40, W - 41, 470], fill=(140, 180, 210))   # sky
    d.rectangle([40, 360, W - 41, 470], fill=(70, 120, 60))    # grass
    # the duck: body, neck, head, beak, eye, wing, and the egg it's very proud of
    d.ellipse([110, 250, 360, 400], fill=(250, 250, 245)); d.ellipse([290, 150, 390, 250], fill=(250, 250, 245)); d.rectangle([300, 210, 350, 300], fill=(250, 250, 245))
    d.polygon([(385, 195), (450, 205), (385, 222)], fill=(240, 150, 30)); d.ellipse([348, 178, 362, 192], fill=(20, 20, 20))
    d.chord([150, 270, 300, 370], 180, 360, fill=(225, 225, 220)); d.ellipse([90, 360, 160, 450], fill=(245, 236, 210)); d.ellipse([110, 380, 125, 395], fill=(255, 255, 250))
    f = ImageFont.truetype(SERIF, 64); t = 'THE EGGY'; d.text(((W - d.textlength(t, font=f)) / 2, 488), t, font=f, fill=(226, 190, 100))
    t = 'DUCK'; d.text(((W - d.textlength(t, font=f)) / 2, 556), t, font=f, fill=(226, 190, 100))
    img.save(os.path.join(OUT, 'pub_sign.png'))

red_brick(); sash(); shopfronts(); pub_tiles(); pub_sign(); print('town textures done')

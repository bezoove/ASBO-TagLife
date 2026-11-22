"""
Textures for the new parts of town (all generated here, own work). Run: python3 town_tex.py <unity Assets folder>
  Art/Town/signal_lamps.png    traffic light lenses (red, amber, green), red and green men, a Belisha globe (4x4 cells)
  Art/Town/town_signs.png      street name plates and signs (2x8 cells, made-up names)
  Art/Town/tower_windows.png   tower block window units (4x4): nets, curtains, blinds, dark, boarded, balcony doors,
                               stair glazing, frosted, lit, Crittall, tin foil; alpha = smoothness
  Art/Town/brown_brick*.png    70s brown brick, stretcher bond (albedo, normal)
  Art/Town/pebbledash*.png     beige pebble-dash render (albedo, normal)
  Art/Town/water_normal.png    tileable ripples for the river;  water_foam.png  scum and foam
  Art/Town/reeds.png           reed bed card (alpha cut-out)
  Art/Town/graffiti.png        spray-painted tags (4x4: a row per gang, a row of general scrawl), alpha
  Art/Town/chainlink.png       ball court mesh fence (alpha cut-out)
  Art/Town/notice.png          a lobby notice board
"""
import sys, os, math, random
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
from scipy.ndimage import gaussian_filter

ASSETS = sys.argv[1]
OUT = os.path.join(ASSETS, 'Art', 'Town'); os.makedirs(OUT, exist_ok=True)
BOLD = '/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf'
REG = '/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf'
rng = np.random.default_rng(2026)

def tile_noise(n, scale, seed):
    g = np.random.default_rng(seed).standard_normal((n, n))
    f = np.fft.fftfreq(n)[:, None] ** 2 + np.fft.fftfreq(n)[None, :] ** 2
    k = np.exp(-f * (n / scale) ** 2 * 6.0)
    out = np.real(np.fft.ifft2(np.fft.fft2(g) * k)); out -= out.min(); out /= out.max() + 1e-9
    return out

def normal_from_height(h, strength):
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * strength
    dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * strength
    n = np.stack([-dx, dy, np.ones_like(h)], -1); n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return ((n * 0.5 + 0.5) * 255).astype(np.uint8)

def save(img, name):
    p = os.path.join(OUT, name); img.save(p); print('wrote', name)

# ------------------------------------------------------------------ signal lamps
def signal_lamps():
    N = 512; C = N // 4; img = Image.new('RGB', (N, N), (8, 8, 8)); d = ImageDraw.Draw(img)
    def cell(i): return (i % 4) * C, (i // 4) * C
    def lens(i, col):
        x0, y0 = cell(i); cx, cy = x0 + C / 2, y0 + C / 2; R = C * 0.46
        for r in range(int(R), 0, -1):
            t = r / R; k = (1 - t) ** 0.6
            c = tuple(int(min(255, col[j] * (0.55 + 0.75 * k) + 255 * max(0, 0.35 - t) * 0.9)) for j in range(3))
            d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=c)
        # the fresnel rings of a real lens
        for r in np.linspace(R * 0.2, R * 0.95, 7):
            d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=tuple(int(c * 0.82) for c in col), width=2)
    lens(0, (255, 30, 20)); lens(1, (255, 150, 10)); lens(2, (40, 255, 140))
    def man(i, col, walking):
        x0, y0 = cell(i); s = C / 100.0
        d.rectangle([x0 + 4, y0 + 4, x0 + C - 4, y0 + C - 4], fill=(10, 10, 10))
        def P(x, y): return (x0 + x * s, y0 + y * s)
        d.ellipse([*P(42, 10), *P(58, 26)], fill=col)
        if walking:
            d.line([P(50, 28), P(46, 58)], fill=col, width=int(9 * s))
            d.line([P(48, 34), P(30, 50)], fill=col, width=int(7 * s)); d.line([P(48, 34), P(66, 46)], fill=col, width=int(7 * s))
            d.line([P(46, 58), P(32, 88)], fill=col, width=int(8 * s)); d.line([P(46, 58), P(64, 86)], fill=col, width=int(8 * s))
        else:
            d.rectangle([*P(42, 28), *P(58, 60)], fill=col)
            d.rectangle([*P(34, 30), *P(40, 56)], fill=col); d.rectangle([*P(60, 30), *P(66, 56)], fill=col)
            d.rectangle([*P(43, 60), *P(49, 90)], fill=col); d.rectangle([*P(51, 60), *P(57, 90)], fill=col)
    man(3, (255, 40, 30), False); man(4, (60, 255, 150), True)
    lens(5, (255, 175, 30))
    img = img.filter(ImageFilter.GaussianBlur(0.6))
    save(img, 'signal_lamps.png')

# ------------------------------------------------------------------ signs (2 x 8 cells of 512 x 128)
SIGNS = [('KESTREL ROAD', None, 'plate'), ('NORTHWAY', None, 'plate'), ('THORNBURY ROAD', None, 'plate'), ('MILL LANE', None, 'plate'),
         ('LONG ROAD', None, 'plate'), ('KESTREL FIELDS', 'ESTATE', 'estate'), ('THORNBURY', 'ESTATE', 'estate'), ('HAWTHORN HEIGHTS', 'FLATS 1-96', 'estate'),
         ('41', 'BUS STOP', 'bus'), ('TELEPHONE', None, 'phone'), ('ABBOTS WEND', 'PRECINCT', 'estate'), ('TOWPATH', 'RIVER WEND', 'green'),
         ('NO BALL GAMES', 'BY ORDER', 'red'), ('POLICE', None, 'police'), ('FIRE STATION', None, 'fire'), ('A&E', 'EMERGENCY', 'nhs')]
def signs():
    W, H = 512, 128; img = Image.new('RGB', (1024, 1024), (255, 255, 255)); d = ImageDraw.Draw(img)
    for i, (t, sub, kind) in enumerate(SIGNS):
        x0, y0 = (i % 2) * W, (i // 2) * H
        bg, fg, border = (250, 250, 248), (12, 12, 12), (12, 12, 12)
        if kind == 'estate': bg, fg, border = (20, 60, 40), (245, 245, 240), (245, 245, 240)
        if kind == 'green': bg, fg, border = (25, 85, 45), (255, 255, 255), (255, 255, 255)
        if kind == 'red': bg, fg, border = (250, 250, 248), (190, 20, 20), (190, 20, 20)
        if kind == 'police': bg, fg, border = (20, 40, 120), (255, 255, 255), (255, 255, 255)
        if kind == 'fire': bg, fg, border = (190, 20, 20), (255, 255, 255), (255, 255, 255)
        if kind == 'nhs': bg, fg, border = (0, 94, 184), (255, 255, 255), (255, 255, 255)
        if kind == 'phone': bg, fg, border = (30, 30, 34), (255, 255, 255), (30, 30, 34)
        d.rectangle([x0 + 2, y0 + 2, x0 + W - 3, y0 + H - 3], fill=bg)
        d.rectangle([x0 + 8, y0 + 8, x0 + W - 9, y0 + H - 9], outline=border, width=4)
        if kind == 'bus':
            d.rectangle([x0 + 2, y0 + 2, x0 + W - 3, y0 + H - 3], fill=(250, 250, 248))
            # a plain bus pictogram (not any operator's symbol)
            d.rounded_rectangle([x0 + 18, y0 + 22, x0 + 122, y0 + 96], radius=10, fill=(20, 110, 60))
            for wx in range(x0 + 26, x0 + 116, 24): d.rectangle([wx, y0 + 32, wx + 18, y0 + 56], fill=(225, 235, 240))
            d.ellipse([x0 + 30, y0 + 88, x0 + 50, y0 + 108], fill=(20, 20, 20)); d.ellipse([x0 + 92, y0 + 88, x0 + 112, y0 + 108], fill=(20, 20, 20))
            f = ImageFont.truetype(BOLD, 64); d.text((x0 + 300, y0 + 64), t, font=f, fill=(12, 12, 12), anchor='mm')
            f2 = ImageFont.truetype(BOLD, 24); d.text((x0 + 300, y0 + 24), sub, font=f2, fill=(12, 12, 12), anchor='mm')
            continue
        size = 70 if sub is None else 56
        f = ImageFont.truetype(BOLD, size)
        while d.textlength(t, font=f) > W - 50 and size > 20: size -= 2; f = ImageFont.truetype(BOLD, size)
        d.text((x0 + W / 2, y0 + (H / 2 if sub is None else H * 0.42)), t, font=f, fill=fg, anchor='mm')
        if sub: d.text((x0 + W / 2, y0 + H * 0.8), sub, font=ImageFont.truetype(BOLD, 22), fill=fg, anchor='mm')
    save(img, 'town_signs.png')

# ------------------------------------------------------------------ tower windows (4 x 4 cells of 256)
def tower_windows():
    N = 1024; C = 256; rgb = np.zeros((N, N, 3), np.float32); smooth = np.zeros((N, N), np.float32)
    def glass(x0, y0, x1, y1, seed, tint=(0.2, 0.25, 0.3)):
        # darkish glass with the sky in it (brighter towards the top) and a soft diagonal sheen
        h, w = y1 - y0, x1 - x0
        yy = np.linspace(0, 1, h)[:, None]; xx = np.linspace(0, 1, w)[None, :]
        sheen = np.clip(1 - np.abs((xx * 0.8 + yy * 0.6) - (0.55 + (seed % 5) * 0.06)) * 6, 0, 1) * 0.12
        sky = np.array(tint) * (0.6 + 0.6 * (1 - yy))[..., None] * np.ones((1, w, 1)) + sheen[..., None]
        n = tile_noise(256, 18, seed)[:h % 256 if h <= 256 else 256, :w] if h <= 256 and w <= 256 else 0
        rgb[y0:y1, x0:x1] = sky + (n[..., None] * 0.06 if isinstance(n, np.ndarray) and n.shape[:2] == (h, w) else 0)
        smooth[y0:y1, x0:x1] = 0.92
    def fill(x0, y0, x1, y1, col, sm=0.25, noise=0.04, seed=1):
        h, w = y1 - y0, x1 - x0
        n = np.random.default_rng(seed).standard_normal((h, w)) * noise
        rgb[y0:y1, x0:x1] = np.array(col)[None, None, :] + n[..., None]; smooth[y0:y1, x0:x1] = sm
    def frame(x0, y0, x1, y1, col=(0.92, 0.92, 0.9), t=10, mullions=(), transom=None):
        fill(x0, y0, x1, y0 + t, col, 0.45, 0.01); fill(x0, y1 - t, x1, y1, col, 0.45, 0.01)
        fill(x0, y0, x0 + t, y1, col, 0.45, 0.01); fill(x1 - t, y0, x1, y1, col, 0.45, 0.01)
        for m in mullions: mx = int(x0 + (x1 - x0) * m); fill(mx - t // 2, y0, mx + t // 2, y1, col, 0.45, 0.01)
        if transom: ty = int(y0 + (y1 - y0) * transom); fill(x0, ty - t // 2, x1, ty + t // 2, col, 0.45, 0.01)
    for c in range(16):
        x0, y0 = (c % 4) * C + 4, (c // 4) * C + 4; x1, y1 = x0 + C - 8, y0 + C - 8
        glass(x0, y0, x1, y1, c)
        mx = (x0 + x1) // 2
        if c == 0:  # nets
            fill(x0 + 10, y0 + 70, x1 - 10, y1 - 10, (0.86, 0.86, 0.84), 0.1, 0.03, c)
            for x in range(x0 + 10, x1 - 10, 6): rgb[y0 + 70:y1 - 10, x:x + 2] *= 0.92
        elif c in (1, 2, 14, 5):  # curtains, mostly drawn back to the sides (muted, faded colours), nets behind on some
            col = {1: (0.45, 0.24, 0.22), 2: (0.27, 0.3, 0.38), 14: (0.36, 0.4, 0.32), 5: (0.58, 0.5, 0.4)}[c]
            w = x1 - x0; side = int(w * (0.2 if c != 5 else 0.42))
            # the room behind: a dim interior, a lamp glow on one
            rgb[y0 + 10:y1 - 10, x0 + 10:x1 - 10] = rgb[y0 + 10:y1 - 10, x0 + 10:x1 - 10] * 0.7 + 0.03
            if c in (1, 14): fill(x0 + side, y0 + 110, x1 - side, y1 - 10, (0.84, 0.84, 0.8), 0.1, 0.03, c + 3)  # nets on the bottom half
            for (a, b, sd) in ((x0 + 10, x0 + side, c), (x1 - side, x1 - 10, c + 7)):
                fill(a, y0 + 10, b, y1 - 10, col, 0.08, 0.03, sd)
                for x in range(a, b, 12): rgb[y0 + 10:y1 - 10, x:x + 4] *= 0.8   # folds
                for x in range(a + 6, b, 12): rgb[y0 + 10:y1 - 10, x:x + 2] *= 1.12
        elif c == 3:  # blinds
            for y in range(y0 + 10, y1 - 10, 12): fill(x0 + 10, y, x1 - 10, y + 8, (0.82, 0.8, 0.74), 0.15, 0.02, y)
        elif c == 4: rgb[y0:y1, x0:x1] *= 0.45
        elif c == 6:  # boarded up: council grey steel or ply
            fill(x0, y0, x1, y1, (0.62, 0.52, 0.36), 0.05, 0.08, c)
            for y in range(y0, y1, 40): rgb[y:y + 2, x0:x1] *= 0.7
        elif c == 7:
            fill(x0 + 10, y0 + 70, x1 - 10, y1 - 10, (0.86, 0.86, 0.84), 0.1, 0.03, c)
            fill(x0 + 40, y1 - 70, x0 + 90, y1 - 12, (0.18, 0.4, 0.15), 0.1, 0.1, c)
        elif c in (8, 9):  # balcony door: tall glazed door and side light
            if c == 9: fill(x0 + 10, y0 + 10, mx, y1 - 10, (0.45, 0.2, 0.3), 0.08, 0.05, c)
            frame(x0, y0, x1, y1, mullions=(0.5,), transom=None)
            fill(x0 + 12, y1 - 60, mx - 4, y1 - 12, (0.92, 0.92, 0.9), 0.45, 0.01)
            continue
        elif c == 10:  # wired stair glass
            rgb[y0:y1, x0:x1] = rgb[y0:y1, x0:x1] * 0.5 + 0.35
            for k in range(y0, y1, 16): rgb[k, x0:x1] *= 0.6
            for k in range(x0, x1, 16): rgb[y0:y1, k] *= 0.6
            frame(x0, y0, x1, y1, (0.3, 0.3, 0.3), 8); continue
        elif c == 11:  # frosted
            fill(x0, y0, x1, y1, (0.72, 0.76, 0.78), 0.5, 0.05, c)
        elif c == 12:  # lit warm room, curtains open
            fill(x0 + 10, y0 + 10, x1 - 10, y1 - 10, (0.86, 0.7, 0.45), 0.6, 0.04, c)
            w = x1 - x0; side = int(w * 0.18)
            fill(x0 + 10, y0 + 10, x0 + side, y1 - 10, (0.5, 0.36, 0.26), 0.1, 0.03, c + 1); fill(x1 - side, y0 + 10, x1 - 10, y1 - 10, (0.5, 0.36, 0.26), 0.1, 0.03, c + 2)
        elif c == 13:  # Crittall steel windows
            frame(x0, y0, x1, y1, (0.15, 0.17, 0.16), 6, mullions=(0.25, 0.5, 0.75), transom=0.3); continue
        elif c == 15:  # tin foil
            fill(x0 + 10, y0 + 10, x1 - 10, y1 - 10, (0.78, 0.78, 0.8), 0.95, 0.15, c)
        frame(x0, y0, x1, y1, mullions=(0.5,) if c in (0, 1, 2, 3, 4, 5, 7, 12, 14, 15) else (), transom=0.28 if c in (0, 1, 2, 3, 4, 5, 7, 12, 14) else None)
    out = np.dstack([np.clip(rgb, 0, 1) * 255, np.clip(smooth, 0, 1) * 255]).astype(np.uint8)
    save(Image.fromarray(out, 'RGBA'), 'tower_windows.png')

# ------------------------------------------------------------------ brown brick (2.7 m repeat: 12 x 36 bricks)
def brick(name, pal, joint_col):
    N = 1024; cols, rows = 12, 36; bw, bh = N / cols, N / rows; joint = 4.5
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    row = np.floor(yy / bh).astype(int); off = (row % 2) * (bw / 2); xs = (xx + off) % N
    col = np.floor(xs / bw).astype(int); lx = xs - col * bw; ly = yy - row * bh
    d = np.minimum(np.minimum(lx, bw - lx), np.minimum(ly, bh - ly))
    mortar = d < joint / 2 + tile_noise(N, 50, 4) * 1.2
    bid = (row * 131 + col * 17) % 9973
    pal = np.array(pal, np.float32); pick = pal[bid % len(pal)]
    vary = (np.random.default_rng(5).random(9973)[bid] - 0.5)[..., None] * 18
    tex = tile_noise(N, 160, 6)[..., None] * 26 - 13 + tile_noise(N, 20, 7)[..., None] * 14
    rgb = pick + vary + tex
    rgb[mortar] = np.array(joint_col, np.float32) + tile_noise(N, 40, 8)[mortar][:, None] * 18
    grime = tile_noise(N, 6, 9)[..., None]; rgb = rgb * (0.88 + 0.12 * grime)
    save(Image.fromarray(np.clip(rgb, 0, 255).astype(np.uint8)), name + '.png')
    h = np.where(mortar, 0.0, 1.0) * 0.8 + tile_noise(N, 30, 10) * 0.2
    save(Image.fromarray(normal_from_height(gaussian_filter(h, 1.0, mode='wrap'), 2.6)), name + '_normal.png')

def pebbledash():
    N = 1024; base = np.array([186, 172, 146], np.float32)
    pebbles = np.random.default_rng(11).random((N, N)); h = gaussian_filter((pebbles > 0.7).astype(np.float32), 1.6, mode='wrap')
    h += gaussian_filter(np.random.default_rng(12).random((N, N)), 0.8, mode='wrap') * 0.5
    stain = tile_noise(N, 5, 13)[..., None]; streak = gaussian_filter(np.random.default_rng(14).random((N, 1)), 3, mode='wrap') * np.ones((1, N))
    rgb = base * (0.82 + 0.28 * h[..., None] / h.max()) * (0.9 + 0.1 * stain) * (0.93 + 0.12 * streak[..., None])
    save(Image.fromarray(np.clip(rgb, 0, 255).astype(np.uint8)), 'pebbledash.png')
    save(Image.fromarray(normal_from_height(h, 3.0)), 'pebbledash_normal.png')

# ------------------------------------------------------------------ water
def water():
    N = 512
    h = sum(tile_noise(N, s, 20 + i) * w for i, (s, w) in enumerate([(4, 1.0), (9, 0.55), (19, 0.3), (40, 0.14)]))
    # stretch along the flow (x) a little: ripples are longer than they are wide
    h = gaussian_filter(h, (0.6, 2.2), mode='wrap')
    save(Image.fromarray(normal_from_height(h / h.max(), 7.0)), 'water_normal.png')
    f = tile_noise(N, 26, 30); f2 = tile_noise(N, 70, 31)
    foam = np.clip((f * 0.7 + f2 * 0.5 - 0.62) * 4.0, 0, 1) * 255
    save(Image.fromarray(foam.astype(np.uint8)).convert('RGB'), 'water_foam.png')

def reeds():
    W, H = 256, 512; img = Image.new('RGBA', (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    r = random.Random(3)
    for i in range(70):
        x = r.uniform(10, W - 10); top = r.uniform(30, 260); lean = r.uniform(-25, 25)
        g = r.uniform(0.5, 1.0); col = (int(70 * g + 30), int(100 * g + 40), int(40 * g + 10), 255)
        d.line([(x, H), (x + lean * 0.5, (H + top) / 2), (x + lean, top)], fill=col, width=r.choice([2, 3, 3, 4]))
        if r.random() < 0.18:  # a bulrush head
            d.ellipse([x + lean - 4, top - 6, x + lean + 4, top + 34], fill=(84, 52, 28, 255))
    save(img, 'reeds.png')

# ------------------------------------------------------------------ graffiti (4 x 4 cells of 256)
def graffiti():
    N = 1024; C = 256; img = Image.new('RGBA', (N, N), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    rows = [('HWY', [(220, 30, 30), (250, 250, 250), (30, 30, 30)]), ('KCR', [(40, 110, 230), (250, 250, 250), (20, 20, 20)]), ('TBL', [(240, 190, 20), (30, 30, 30), (250, 250, 250)])]
    scrawl = ['DAZ 4 SHAZ', 'GRASSES', '5-0', 'BOMBS']
    r = random.Random(9)
    def tag(cx, cy, text, fill, outline, style):
        size = 92 if len(text) <= 3 else 44
        f = ImageFont.truetype(BOLD, size)
        if style == 0:  # bubble letters with a fat outline and drop shadow
            for dx, dy in [(6, 6)]: d.text((cx + dx, cy + dy), text, font=f, fill=(0, 0, 0, 160), anchor='mm', stroke_width=10, stroke_fill=(0, 0, 0, 160))
            d.text((cx, cy), text, font=f, fill=fill + (255,), anchor='mm', stroke_width=9, stroke_fill=outline + (255,))
        elif style == 1:  # quick tag: thin, skewed
            t = Image.new('RGBA', (C, C), (0, 0, 0, 0)); td = ImageDraw.Draw(t)
            td.text((C / 2, C / 2), text, font=ImageFont.truetype(BOLD, size // 2 + 10), fill=fill + (255,), anchor='mm')
            t = t.transform((C, C), Image.AFFINE, (1, 0.35, -40, 0, 1, 0)); img.alpha_composite(t, (int(cx - C / 2), int(cy - C / 2)))
            d.line([(cx - 80, cy + 40), (cx + 90, cy + 34)], fill=fill + (255,), width=4)
        elif style == 2:  # drippy throw-up
            d.text((cx, cy), text, font=f, fill=fill + (230,), anchor='mm')
            for k in range(8):
                x = cx + r.uniform(-80, 80); y = cy + r.uniform(10, 30); d.line([(x, y), (x, y + r.uniform(15, 55))], fill=fill + (230,), width=3)
        else:  # crown and letters
            d.polygon([(cx - 50, cy - 30), (cx - 30, cy - 70), (cx - 10, cy - 40), (cx + 10, cy - 75), (cx + 30, cy - 40), (cx + 50, cy - 70), (cx + 60, cy - 30)], outline=fill + (255,), width=5)
            d.text((cx, cy + 25), text, font=ImageFont.truetype(BOLD, 64), fill=fill + (255,), anchor='mm', stroke_width=4, stroke_fill=outline + (255,))
    for ri, (txt, (a, b, c)) in enumerate(rows):
        for k in range(4): tag(k * C + C / 2, ri * C + C / 2, txt, a if k != 2 else c, b if k != 2 else a, k)
    for k, s in enumerate(scrawl): tag(k * C + C / 2, 3 * C + C / 2, s, [(30, 30, 30), (200, 30, 160), (250, 250, 250), (30, 160, 60)][k], (0, 0, 0), 1 if k % 2 else 2)
    save(img, 'graffiti.png')

def chainlink():
    N = 256; img = Image.new('RGBA', (N, N), (0, 0, 0, 0)); d = ImageDraw.Draw(img); s = 32
    for k in range(-N, 2 * N, s):
        d.line([(k, 0), (k + N, N)], fill=(70, 90, 70, 255), width=3); d.line([(k, N), (k + N, 0)], fill=(70, 90, 70, 255), width=3)
    save(img, 'chainlink.png')

def notice():
    W, H = 512, 340; img = Image.new('RGB', (W, H), (150, 110, 70)); d = ImageDraw.Draw(img)
    r = random.Random(4)
    papers = [('RESIDENTS MEETING', 'TUES 7PM COMMUNITY ROOM'), ('LIFTS OUT OF ORDER', 'ENGINEER BOOKED'), ('NO BALL GAMES', ''), ('BINS WEDS', 'NOT IN THE LOBBY'), ('LOST CAT', 'ANSWERS TO DAVE'), ('FOOD BANK', 'ST MARY\'S HALL SAT')]
    for i, (a, b) in enumerate(papers):
        x, y = 20 + (i % 3) * 165, 20 + (i // 3) * 160
        col = r.choice([(250, 250, 245), (255, 245, 170), (200, 230, 255), (255, 210, 210)])
        d.rectangle([x, y, x + 150, y + 140], fill=col); d.ellipse([x + 70, y + 4, x + 80, y + 14], fill=(200, 30, 30))
        d.text((x + 75, y + 50), a, font=ImageFont.truetype(BOLD, 15), fill=(20, 20, 20), anchor='mm')
        d.text((x + 75, y + 85), b, font=ImageFont.truetype(REG, 12), fill=(40, 40, 40), anchor='mm')
    save(img, 'notice.png')

signal_lamps(); signs(); tower_windows()
brick('brown_brick', [[112, 62, 44], [98, 54, 38], [124, 70, 48], [88, 50, 36], [132, 80, 56], [104, 60, 46], [80, 46, 34]], [150, 140, 125])
pebbledash(); water(); reeds(); graffiti(); chainlink(); notice()

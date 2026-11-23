"""
One texture atlas for every prop in the Silver Sixpence (bar, gaming floor and back rooms), so the whole interior's
small stuff draws as a handful of meshes on a phone. All artwork generated here (own work); every brand is made up.
Maps, same layout: albedo 4096 (RGB), normal 2048 (OpenGL), mask 2048 (R metal, G 1, A smoothness), emission 2048.
  palette     32x32 flat swatches (paints, metals, plastics, liquids, glass tints, lamps) with their own metal,
              smoothness and glow
  materials   leather, velvet, upholstery (from the CC BY 4.0 packs when given), walnut, oak, brushed steel
  print       bottle labels, beer mats, tap badges, slot screens and toppers, felts, screens, signs, notes, chips,
              cards, packets, boxes, notices
Run: python3 casino_atlas.py <out folder> [Assets/Textures/Surfaces folder]
Writes casino_albedo.png, casino_normal.png, casino_mask.png, casino_emit.png and casino_atlas.json (UV rects,
origin bottom left, and the palette swatch UVs by name).
"""
import sys, os, json, math, random
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageChops

OUT = sys.argv[1]; SURF = sys.argv[2] if len(sys.argv) > 2 else None
os.makedirs(OUT, exist_ok=True)
S = 4096; H2 = 2048
G = '/usr/share/fonts/truetype/google-fonts/ofl/'
FONTS = dict(anton=G + 'anton/Anton-Regular.ttf', archivo=G + 'archivoblack/ArchivoBlack-Regular.ttf', cardo=G + 'cardo/Cardo-Bold.ttf',
             cardoi=G + 'cardo/Cardo-Italic.ttf', crimson=G + 'crimsontext/CrimsonText-Bold.ttf', crimsoni=G + 'crimsontext/CrimsonText-BoldItalic.ttf',
             fira=G + 'firasans/FiraSans-Bold.ttf', firab=G + 'firasans/FiraSans-Black.ttf', firar=G + 'firasans/FiraSans-Regular.ttf',
             firam=G + 'firasans/FiraSans-Medium.ttf', mono=G + 'anonymouspro/AnonymousPro-Bold.ttf', comic=G + 'comicrelief/ComicRelief-Bold.ttf',
             dejavu='/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf', dejavur='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf')
_fc = {}
def F(n, s):
    k = (n, int(s))
    if k not in _fc: _fc[k] = ImageFont.truetype(FONTS[n], max(6, int(s)))
    return _fc[k]
rng = random.Random(7)

albedo = Image.new('RGB', (S, S), (128, 128, 128))
emit = Image.new('RGB', (S, S), (0, 0, 0))            # drawn at full size, halved at the end
normal = Image.new('RGB', (S, S), (128, 128, 255))
mask = Image.new('RGBA', (S, S), (0, 255, 0, 90))
regions = {}

# ------------------------------------------------------------------ packing
class Shelf:
    """Best-fit shelves: each request goes on the lowest existing shelf it fits (no more than 1.6x its height), else a new one."""
    def __init__(s): s.rows = []; s.y = 0; s.h = 0
    def put(s, w, h):
        best = None
        for r in s.rows:
            if r[1] >= h and r[1] <= h * 1.6 + 1 and r[2] + w <= S and (best is None or r[1] < best[1]): best = r
        if best is None:
            if s.y + h > S: raise RuntimeError(f'atlas full at {w}x{h}')
            best = [s.y, h, 0]; s.rows.append(best); s.y += h
        p = (best[2], best[0]); best[2] += w; return p
shelf = Shelf()
def region(name, w, h, smooth=0.3, metal=0, glow=False):
    """Reserve w x h pixels; returns (x, y, w, h) and an RGB canvas to draw on (pasted by commit())."""
    x, y = shelf.put(w, h)
    regions[name] = dict(px=[x, y, w, h], uv=[(x + 0.5) / S, 1 - (y + h - 0.5) / S, (x + w - 0.5) / S, 1 - (y + 0.5) / S])
    ImageDraw.Draw(mask).rectangle([x, y, x + w - 1, y + h - 1], fill=(int(metal * 255), 255, 0, int(smooth * 255)))
    return x, y
def commit(name, im, glow=None, nrm=None):
    x, y, w, h = regions[name]['px']
    albedo.paste(im.convert('RGB').resize((w, h), Image.LANCZOS), (x, y))
    if glow is not None: emit.paste((im if glow is True else glow).convert('RGB').resize((w, h), Image.LANCZOS), (x, y))
    if nrm is not None: normal.paste(nrm.convert('RGB').resize((w, h), Image.LANCZOS), (x, y))

def C(im): return ImageDraw.Draw(im)
def centred(d, xy, t, f, fill, **k):
    b = d.textbbox((0, 0), t, font=f); d.text((xy[0] - (b[2] - b[0]) / 2 - b[0], xy[1] - (b[3] - b[1]) / 2 - b[1]), t, font=f, fill=fill, **k)
def fit(t, n, w, s0):
    s = s0
    while s > 8 and F(n, s).getlength(t) > w: s -= 2
    return F(n, s)
def noise(w, h, amt, blur=1.0, seed=0):
    a = np.random.default_rng(seed).normal(0, 1, (h, w)).astype(np.float32)
    im = Image.fromarray(np.clip(128 + a * 40, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(blur))
    return (np.asarray(im, np.float32) - 128) / 40 * amt
def grad(w, h, top, bot):
    t = np.linspace(0, 1, h)[:, None, None]
    a = np.array(top, np.float32) * (1 - t) + np.array(bot, np.float32) * t
    return Image.fromarray(np.repeat(a, w, 1).astype(np.uint8))
def radial(w, h, inner, outer, cx=0.5, cy=0.5, r=0.7):
    yy, xx = np.mgrid[0:h, 0:w]; d = np.sqrt(((xx / w - cx) / r) ** 2 + ((yy / h - cy) / r) ** 2); d = np.clip(d, 0, 1)[..., None]
    return Image.fromarray((np.array(inner) * (1 - d) + np.array(outer) * d).astype(np.uint8))
def grain_tex(w, h, base, dark, seed, ring=14.0, straight=True):
    """Wood: long flowing grain with darker late-wood bands, a little figure and pores."""
    r = np.random.default_rng(seed); yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    warp = noise(w, h, 1.0, 18, seed) * 22 + noise(w, h, 1.0, 60, seed + 1) * 60
    v = (yy + warp) / h * ring * (1 if straight else 1.6)
    band = (np.sin(v * 2 * np.pi) * 0.5 + 0.5) ** 3
    fine = noise(w, h, 1.0, 0.6, seed + 2) * 0.25 + noise(w, h, 1.0, 2.5, seed + 3) * 0.2
    t = np.clip(band * 0.65 + fine, 0, 1)[..., None]
    a = np.array(base, np.float32) * (1 - t) + np.array(dark, np.float32) * t
    a *= (1 + noise(w, h, 0.08, 40, seed + 4))[..., None]
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).transpose(Image.ROTATE_90)
def height_to_normal(hm, strength=2.0):
    a = np.asarray(hm.convert('L'), np.float32) / 255.0
    dx = np.roll(a, -1, 1) - np.roll(a, 1, 1); dy = np.roll(a, -1, 0) - np.roll(a, 1, 0)
    n = np.dstack([-dx * strength, dy * strength, np.ones_like(a)]); n /= np.linalg.norm(n, axis=2, keepdims=True)
    return Image.fromarray(((n * 0.5 + 0.5) * 255).astype(np.uint8))
def surf(name):
    if SURF is None: return None
    d = os.path.join(SURF, name)
    for ext in ('jpg', 'png'):
        p = os.path.join(d, f'{name}_albedo.{ext}')
        if os.path.exists(p): return Image.open(p).convert('RGB'), Image.open(os.path.join(d, f'{name}_normal.png')).convert('RGB'), Image.open(os.path.join(d, f'{name}_mask.png'))
    return None

# ------------------------------------------------------------------ palette: 32 x 32 swatches of 32 px
PAL = {}
PW = 32; pal_x, pal_y = region('palette', 1024, 192)
def sw(name, rgb, smooth=0.35, metal=0.0, glow=None):
    i = len(PAL); cx, cy = i % 32, i // 32
    x, y = pal_x + cx * PW, pal_y + cy * PW
    a = np.zeros((PW, PW, 3), np.float32) + np.array(rgb, np.float32)
    a *= (1 + noise(PW, PW, 0.03, 0.8, i))[..., None]
    albedo.paste(Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)), (x, y))
    ImageDraw.Draw(mask).rectangle([x, y, x + PW - 1, y + PW - 1], fill=(int(metal * 255), 255, 0, int(smooth * 255)))
    if glow: ImageDraw.Draw(emit).rectangle([x, y, x + PW - 1, y + PW - 1], fill=tuple(int(c) for c in glow))
    PAL[name] = [(x + PW / 2) / S, 1 - (y + PW / 2) / S]
# metals
sw('brass', (205, 160, 70), 0.82, 1); sw('brass_dull', (170, 135, 70), 0.55, 1); sw('gold', (230, 185, 80), 0.9, 1)
sw('chrome', (225, 228, 232), 0.94, 1); sw('steel', (175, 178, 182), 0.6, 1); sw('steel_dark', (95, 98, 104), 0.5, 1)
sw('aluminium', (200, 202, 206), 0.45, 1); sw('copper', (190, 110, 70), 0.7, 1); sw('iron', (60, 62, 66), 0.35, 0.9)
sw('gunmetal', (55, 58, 64), 0.7, 1); sw('rose_gold', (210, 150, 130), 0.85, 1)
# paints and plastics
for n, c, s in [('black', (16, 16, 18), 0.4), ('black_gloss', (10, 10, 12), 0.9), ('black_matt', (22, 22, 24), 0.12), ('white', (232, 232, 228), 0.35),
                ('white_gloss', (240, 240, 238), 0.85), ('cream', (232, 222, 196), 0.3), ('grey_lt', (180, 182, 184), 0.3), ('grey', (120, 122, 126), 0.3),
                ('grey_dk', (62, 64, 68), 0.3), ('charcoal', (38, 40, 44), 0.35), ('red', (178, 20, 26), 0.45), ('red_dk', (110, 10, 18), 0.4),
                ('burgundy', (96, 16, 30), 0.35), ('navy', (20, 28, 66), 0.4), ('blue', (30, 70, 170), 0.45), ('blue_lt', (110, 160, 220), 0.4),
                ('green', (20, 110, 50), 0.4), ('green_dk', (14, 60, 34), 0.35), ('felt_green', (18, 92, 52), 0.08), ('felt_blue', (20, 54, 110), 0.08),
                ('yellow', (240, 200, 30), 0.45), ('orange', (230, 110, 20), 0.45), ('purple', (90, 30, 120), 0.4), ('pink', (230, 120, 160), 0.4),
                ('beige', (200, 180, 150), 0.25), ('brown', (96, 62, 38), 0.3), ('brown_dk', (52, 34, 22), 0.35), ('tan', (170, 120, 70), 0.3),
                ('cardboard', (176, 136, 90), 0.08), ('cardboard_dk', (140, 102, 64), 0.08), ('paper', (244, 242, 236), 0.15), ('cork', (170, 120, 76), 0.1),
                ('rubber', (28, 28, 30), 0.25), ('rubber_grey', (70, 70, 74), 0.2), ('plastic_blue', (30, 90, 180), 0.6), ('plastic_red', (200, 30, 30), 0.6),
                ('plastic_yellow', (245, 200, 20), 0.6), ('plastic_green', (40, 150, 60), 0.6), ('plastic_white', (236, 236, 232), 0.65),
                ('plastic_black', (24, 24, 26), 0.6), ('plastic_grey', (140, 142, 146), 0.55), ('formica', (210, 204, 190), 0.55), ('laminate_oak', (190, 150, 100), 0.5),
                ('mdf', (150, 120, 90), 0.15), ('locker_blue', (60, 90, 140), 0.55), ('locker_grey', (150, 154, 158), 0.5), ('safe_green', (50, 80, 60), 0.55),
                ('marble_wh', (232, 230, 226), 0.85), ('marble_bk', (26, 24, 26), 0.9), ('onyx', (240, 200, 140), 0.85), ('teal', (20, 120, 120), 0.4),
                ('leather_bk', (24, 20, 20), 0.55), ('leather_red', (120, 18, 22), 0.55), ('leather_brn', (80, 46, 28), 0.5), ('velvet_red', (110, 10, 20), 0.1),
                ('velvet_navy', (20, 24, 60), 0.1), ('fabric_grey', (90, 92, 98), 0.1), ('fabric_blue', (40, 60, 110), 0.1), ('carpet_red', (120, 20, 26), 0.05),
                ('foam', (246, 240, 226), 0.2), ('ice', (220, 236, 245), 0.9), ('lemon', (240, 220, 60), 0.5), ('lime', (120, 190, 40), 0.5),
                ('olive', (110, 120, 40), 0.6), ('cherry', (180, 10, 30), 0.8), ('peanut', (190, 140, 80), 0.3), ('crisp', (230, 200, 120), 0.3),
                ('skin_note', (190, 160, 120), 0.2), ('wax', (240, 236, 220), 0.4), ('soil', (60, 42, 30), 0.05), ('leaf', (40, 90, 40), 0.35),
                ('water', (180, 200, 210), 0.95)]:
    sw(n, c, s)
# drinks (opaque liquid inside glass)
for n, c, s in [('lager', (220, 160, 30), 0.85), ('bitter', (150, 74, 20), 0.85), ('stout', (18, 10, 8), 0.85), ('cider', (230, 190, 70), 0.85),
                ('wine_red', (90, 6, 20), 0.9), ('wine_white', (230, 220, 150), 0.9), ('wine_rose', (235, 140, 140), 0.9), ('whisky', (190, 100, 30), 0.9),
                ('cola', (40, 16, 10), 0.9), ('spirit_clear', (210, 220, 225), 0.95), ('coffee', (60, 34, 20), 0.6), ('tea', (140, 80, 40), 0.7),
                ('orange_juice', (245, 150, 30), 0.6), ('blue_drink', (30, 120, 220), 0.9), ('cream_liq', (225, 205, 170), 0.7), ('milk', (245, 245, 240), 0.5)]:
    sw(n, c, s)
# glass tints (used with the glass material: its colour and alpha come from here)
for n, c, s in [('glass_clear', (210, 225, 230), 0.97), ('glass_green', (30, 90, 40), 0.95), ('glass_amber', (110, 60, 15), 0.95), ('glass_blue', (40, 80, 150), 0.95),
                ('glass_frost', (230, 236, 240), 0.4), ('glass_dark', (20, 24, 28), 0.97), ('glass_smoke', (60, 60, 64), 0.95), ('glass_screen', (12, 14, 16), 0.98)]:
    sw(n, c, s)
# lamps and LEDs (glow)
for n, c, g in [('lamp_warm', (255, 230, 180), (255, 206, 140)), ('lamp_white', (250, 250, 245), (235, 235, 225)), ('lamp_soft', (255, 220, 170), (150, 110, 60)),
                ('led_red', (255, 40, 40), (255, 20, 20)), ('led_green', (40, 255, 80), (20, 230, 60)), ('led_blue', (40, 120, 255), (30, 100, 255)),
                ('led_amber', (255, 170, 30), (255, 150, 20)), ('led_white', (240, 245, 255), (220, 230, 255)), ('led_purple', (180, 60, 255), (150, 40, 230)),
                ('crystal', (250, 248, 240), (190, 170, 130)), ('neon_pink', (255, 80, 180), (255, 60, 160)), ('screen_off', (8, 10, 12), None),
                ('exit_green', (20, 170, 70), (10, 150, 60)), ('fridge_glow', (220, 235, 255), (150, 170, 200)), ('candle', (255, 200, 120), (255, 160, 60))]:
    sw(n, c, 0.6, 0, g)

# ------------------------------------------------------------------ materials
def matregion(name, size, im, nrm=None, msk=None, smooth=0.3, metal=0):
    region(name, size, size, smooth, metal)
    commit(name, im, nrm=nrm)
    if msk is not None:
        x, y, w, h = regions[name]['px']; mask.paste(msk.convert('RGBA').resize((w, h), Image.LANCZOS), (x, y))
for nm, src, fb, sm in [('leather_quilt', 'leather_quilt', (26, 22, 22), 0.5), ('leather_red', 'leather_worn_red', (120, 20, 24), 0.45), ('velvet', 'velvet_red', (110, 10, 20), 0.1),
                        ('upholstery', 'fabric_gold', (150, 110, 50), 0.15), ('carpet_office', 'carpet_office', (60, 70, 90), 0.05)]:
    s = surf(src)
    if s: matregion(nm, 1024 if nm == 'leather_quilt' else 512, s[0], s[1], s[2])
    else:
        im = Image.new('RGB', (512, 512), fb); a = np.asarray(im, np.float32) * (1 + noise(512, 512, 0.08, 1.2, len(regions)))[..., None]
        matregion(nm, 512, Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)), smooth=sm)
wal = grain_tex(1024, 1024, (98, 52, 28), (46, 22, 12), 11); matregion('walnut', 1024, wal, height_to_normal(wal.filter(ImageFilter.GaussianBlur(1)), 1.2), smooth=0.72)
oak = grain_tex(512, 512, (190, 146, 96), (140, 96, 56), 12, 10); matregion('oak', 512, oak, smooth=0.45)
mah = grain_tex(512, 512, (120, 40, 26), (64, 18, 12), 13, 16); matregion('mahogany', 512, mah, smooth=0.78)
st = Image.fromarray(np.clip(np.full((512, 512, 3), 170, np.float32) + noise(512, 512, 1, 0.5, 14)[..., None] * np.array([[[1, 1, 1]]]) * 6 + np.repeat(noise(512, 1, 1, 0.3, 15), 512, 0)[..., None] * 14, 0, 255).astype(np.uint8))
matregion('brushed_steel', 512, st, smooth=0.55, metal=1)
cb = Image.new('RGB', (512, 512), (176, 136, 90)); d = C(cb)
for i in range(0, 512, 9): d.line([(0, i), (512, i)], fill=(166, 126, 82), width=2)
matregion('cardboard_ribbed', 512, cb, smooth=0.05)

# ------------------------------------------------------------------ bottle labels (256 x 128 each; wraps the front of a bottle)
LABELS = [
    ('Glenmorrach', 'SINGLE MALT SCOTCH WHISKY', '12', (232, 214, 170), (90, 30, 20), 'cardo'), ('Black Heron', 'BLENDED SCOTCH', '', (20, 20, 22), (210, 170, 80), 'crimson'),
    ('Ardbrae', 'ISLAY MALT  10 YEARS', '10', (240, 236, 226), (20, 70, 40), 'cardo'), ('Old Harbour', 'RESERVE WHISKY', '', (120, 30, 24), (240, 210, 140), 'crimsoni'),
    ('KESTERTON', 'LONDON DRY GIN', '', (232, 240, 236), (20, 60, 90), 'archivo'), ('Hawthorn', 'PINK GIN', '', (246, 210, 220), (150, 30, 70), 'cardoi'),
    ('VOLKOV', 'PREMIUM VODKA', '', (240, 240, 242), (170, 20, 30), 'anton'), ('ARCTIC WOLF', 'TRIPLE DISTILLED VODKA', '', (210, 230, 245), (20, 40, 90), 'archivo'),
    ('Capt. Rourke', 'SPICED GOLDEN RUM', '', (40, 26, 16), (230, 180, 70), 'crimsoni'), ('BAY ISLAND', 'WHITE RUM', '', (250, 250, 240), (30, 120, 120), 'anton'),
    ('Duvernay', 'V.S.O.P. COGNAC', '', (24, 18, 14), (220, 180, 90), 'cardoi'), ('Crema Dolce', 'CREAM LIQUEUR', '', (60, 30, 20), (240, 220, 180), 'cardoi'),
    ('SAMBUCA NERA', 'LIQUORE', '', (12, 12, 14), (230, 230, 230), 'anton'), ('Blue Lagoon', 'CURACAO', '', (30, 80, 170), (250, 250, 250), 'cardoi'),
    ('Ch. Lavaux', 'ROUGE  2019', '', (238, 232, 220), (100, 10, 30), 'cardo'), ('Monte Alto', 'PINOT GRIGIO', '', (240, 238, 228), (60, 90, 50), 'crimsoni'),
    ('Rosado', 'VINO ROSADO', '', (250, 236, 236), (190, 80, 100), 'cardoi'), ("Vigna d'Oro", 'PROSECCO', '', (30, 30, 34), (220, 180, 80), 'cardoi'),
    ('SIXPENCE GOLD', 'PREMIUM LAGER', '', (230, 190, 60), (30, 30, 70), 'anton'), ('Abbots Wend', 'BEST BITTER', '', (130, 40, 20), (250, 230, 190), 'crimson'),
    ('IRONBRIDGE', 'IRISH-STYLE STOUT', '', (16, 14, 12), (230, 210, 160), 'archivo'), ('MILL LANE', 'CLOUDY CIDER', '', (90, 140, 40), (250, 240, 200), 'anton'),
    ('NORTHFIELD', 'LAGER  4.0%', '', (240, 240, 240), (30, 80, 160), 'archivo'), ('FIZZO', 'COLA', '', (180, 20, 20), (250, 250, 250), 'anton'),
    ('Tonic', 'INDIAN TONIC WATER', '', (240, 245, 240), (40, 100, 120), 'cardoi'), ('ZEST', 'LEMONADE', '', (250, 240, 140), (40, 130, 60), 'anton'),
    ('Orchard', 'ORANGE JUICE', '', (250, 170, 40), (250, 250, 250), 'archivo'), ('Tropi', 'PINEAPPLE', '', (250, 220, 80), (30, 120, 40), 'anton'),
    ('Bitters', 'AROMATIC', '', (230, 200, 160), (120, 30, 20), 'cardoi'), ('Grenadine', 'SYRUP', '', (180, 10, 40), (250, 230, 230), 'cardoi'),
    ('Tequila Sol', 'BLANCO', '', (240, 230, 200), (180, 60, 20), 'anton'), ('Amaretto', 'DI SARONNO-STYLE', '', (120, 50, 20), (240, 210, 160), 'cardoi'),
]
for i, (big, small, num, bg, fg, font) in enumerate(LABELS):
    w, h = 512, 256; im = Image.new('RGB', (w, h), bg); d = C(im)
    a = np.asarray(im, np.float32) * (1 + noise(w, h, 0.05, 2, 100 + i))[..., None]; im = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)); d = C(im)
    d.rectangle([10, 10, w - 11, h - 11], outline=fg, width=4); d.rectangle([20, 20, w - 21, h - 21], outline=fg, width=1)
    centred(d, (w / 2, h * 0.42), big, fit(big, font, w - 70, 96), fg)
    centred(d, (w / 2, h * 0.74), small, fit(small, 'fira', w - 90, 30), fg)
    if num: d.ellipse([w - 92, 26, w - 32, 86], outline=fg, width=3); centred(d, (w - 62, 56), num, F('cardo', 34), fg)
    d.line([(60, h * 0.6), (w - 60, h * 0.6)], fill=fg, width=2)
    region(f'label{i}', 256, 128, 0.25); commit(f'label{i}', im)

# ------------------------------------------------------------------ beer mats (round and square), tap badges
MATS = [('SIXPENCE GOLD', 'Premium Lager', (30, 30, 80), (240, 200, 70)), ('ABBOTS WEND', 'Best Bitter', (130, 40, 20), (250, 230, 190)),
        ('IRONBRIDGE', 'Stout', (16, 14, 12), (230, 210, 160)), ('NORTHFIELD', 'Lager', (240, 240, 240), (30, 80, 160)),
        ('MILL LANE', 'Cider', (90, 140, 40), (250, 240, 200)), ('SILVER SIXPENCE', 'Casino & Bar', (110, 10, 20), (230, 190, 90)),
        ('HOP & ANCHOR', 'Pale Ale', (20, 70, 90), (240, 230, 200)), ('FIZZO', 'Ice Cold', (180, 20, 20), (250, 250, 250))]
for i, (a1, a2, bg, fg) in enumerate(MATS):
    im = Image.new('RGB', (256, 256), (236, 230, 216)); d = C(im)
    if i % 2 == 0: d.ellipse([4, 4, 251, 251], fill=bg); d.ellipse([16, 16, 239, 239], outline=fg, width=4)
    else: d.rounded_rectangle([6, 6, 249, 249], 24, fill=bg); d.rounded_rectangle([18, 18, 237, 237], 18, outline=fg, width=4)
    centred(d, (128, 112), a1, fit(a1, 'anton', 200, 54), fg); centred(d, (128, 162), a2, fit(a2, 'cardoi', 180, 30), fg)
    region(f'mat{i}', 128, 128, 0.1); commit(f'mat{i}', im)
for i, (a1, a2, bg, fg) in enumerate(MATS):
    im = radial(256, 256, tuple(min(255, c + 50) for c in bg), bg); d = C(im)
    d.ellipse([6, 6, 249, 249], outline=(212, 170, 80), width=10)
    centred(d, (128, 118), a1.split()[0], fit(a1.split()[0], 'anton', 190, 60), fg); centred(d, (128, 170), a2, fit(a2, 'cardoi', 170, 30), fg)
    region(f'badge{i}', 128, 128, 0.7); commit(f'badge{i}', im, glow=Image.eval(im, lambda v: v // 3))

# ------------------------------------------------------------------ slot machines: screens (portrait), toppers, button decks
GAMES = [('FRUIT FEVER', (200, 20, 60), (255, 210, 40)), ('LUCKY 7s', (20, 40, 160), (255, 60, 60)), ('DIAMOND DELUXE', (20, 20, 30), (120, 220, 255)),
         ('CASH COW', (30, 140, 50), (255, 240, 120)), ('TREASURE ISLE', (10, 90, 130), (255, 200, 60)), ('WILD BANDIT', (120, 60, 20), (255, 180, 60)),
         ('POTS O\' PENNIES', (20, 110, 40), (250, 200, 60)), ('MEGA JACKPOT', (90, 10, 120), (255, 220, 60))]
SYM = ['7', 'BAR', '$', '★', '♦', '♣', '♥', 'X']
for i, (name, c1, c2) in enumerate(GAMES):
    w, h = 512, 1024; im = grad(w, h, tuple(min(255, int(c * 1.3)) for c in c1), tuple(c // 3 for c in c1)); d = C(im)
    # title banner and jackpot meter
    d.rounded_rectangle([20, 24, w - 20, 170], 26, fill=tuple(c // 2 for c in c1), outline=c2, width=6)
    centred(d, (w / 2, 98), name, fit(name, 'anton', w - 80, 92), c2, stroke_width=3, stroke_fill=(0, 0, 0))
    d.rounded_rectangle([60, 190, w - 60, 270], 18, fill=(10, 10, 10), outline=(255, 220, 90), width=4)
    centred(d, (w / 2, 230), f'JACKPOT £{500 + i * 250:,}', F('fira', 40), (255, 80, 60))
    # the reels: 5 x 3 in a gold frame
    d.rounded_rectangle([20, 300, w - 20, 700], 16, fill=(250, 248, 240), outline=(220, 180, 70), width=8)
    r = random.Random(i)
    for cx in range(5):
        for cy in range(3):
            x0 = 30 + cx * ((w - 60) / 5); y0 = 312 + cy * 128; s = r.choice(SYM)
            col = r.choice([(200, 20, 30), (30, 60, 180), (20, 140, 50), (220, 150, 20), (120, 30, 150)])
            centred(d, (x0 + (w - 60) / 10, y0 + 60), s, F('dejavu', 70 if len(s) == 1 else 40), col)
        if cx: d.line([(30 + cx * ((w - 60) / 5), 306), (30 + cx * ((w - 60) / 5), 694)], fill=(200, 196, 186), width=2)
    d.line([(24, 504), (w - 24, 504)], fill=(255, 40, 40), width=4)
    # credits / bet / win bar and touch buttons
    d.rectangle([20, 720, w - 20, 800], fill=(0, 0, 0))
    for k, (t, v) in enumerate([('CREDIT', '£0.00'), ('BET', '£1.00'), ('WIN', '£0.00')]):
        centred(d, (90 + k * 166, 742), t, F('fira', 22), (255, 210, 60)); centred(d, (90 + k * 166, 778), v, F('mono', 32), (255, 255, 255))
    for k, t in enumerate(['HELP', 'STAKE', 'AUTO', 'SPIN']):
        x0 = 24 + k * 118; d.rounded_rectangle([x0, 830, x0 + 108, 900], 14, fill=(30, 30, 30) if t != 'SPIN' else (20, 160, 50), outline=c2, width=3)
        centred(d, (x0 + 54, 865), t, F('fira', 28), (255, 255, 255))
    centred(d, (w / 2, 960), 'PLEASE PLAY RESPONSIBLY  18+', F('firar', 22), (230, 230, 230))
    region(f'slot{i}', 256, 512, 0.9); commit(f'slot{i}', im, glow=Image.eval(im, lambda v: int(v * 0.85)))
    # topper: a lit sign with the name
    tw, th = 512, 256; t = radial(tw, th, tuple(min(255, int(c * 1.6) + 40) for c in c1), tuple(c // 2 for c in c1)); d = C(t)
    for k in range(18): a = k / 18 * 2 * math.pi; d.ellipse([tw / 2 + math.cos(a) * 230 - 8, th / 2 + math.sin(a) * 110 - 8, tw / 2 + math.cos(a) * 230 + 8, th / 2 + math.sin(a) * 110 + 8], fill=(255, 240, 180))
    centred(d, (tw / 2, th / 2), name, fit(name, 'anton', tw - 90, 110), c2, stroke_width=5, stroke_fill=(20, 0, 0))
    region(f'topper{i}', 256, 128, 0.8); commit(f'topper{i}', t, glow=t)
for i in range(2):
    w, h = 512, 192; im = Image.new('RGB', (w, h), (18, 18, 20)); d = C(im)
    cols = [(220, 30, 30), (240, 200, 30), (30, 170, 60), (30, 110, 220), (240, 240, 240)]
    for k in range(5): x0 = 24 + k * 96; d.rounded_rectangle([x0, 40, x0 + 80, 120], 14, fill=cols[(k + i) % 5], outline=(255, 255, 255), width=3)
    centred(d, (w / 2, 160), 'COLLECT   HOLD   NUDGE   STAKE   START', F('fira', 22), (220, 220, 220))
    region(f'deck{i}', 256, 96, 0.8); commit(f'deck{i}', im, glow=Image.eval(im, lambda v: v // 2))

# ------------------------------------------------------------------ felts: roulette layout, wheel, blackjack, poker
RED = {1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36}
def felt(w, h, col=(18, 92, 52)):
    a = np.zeros((h, w, 3), np.float32) + np.array(col, np.float32); a *= (1 + noise(w, h, 0.05, 0.7, w + h))[..., None]
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))
im = felt(2048, 1024); d = C(im); gold = (236, 214, 150)
x0, y0, cw, ch = 300, 120, 128, 200
d.polygon([(x0, y0), (x0, y0 + ch * 3), (x0 - 160, y0 + ch * 1.5)], outline=gold, width=6); centred(d, (x0 - 60, y0 + ch * 1.5), '0', F('cardo', 90), gold)
for c in range(12):
    for r_ in range(3):
        n = c * 3 + (3 - r_); x, y = x0 + c * cw, y0 + r_ * ch
        d.rectangle([x, y, x + cw, y + ch], outline=gold, width=5)
        d.ellipse([x + 22, y + 58, x + cw - 22, y + ch - 58], fill=(190, 20, 28) if n in RED else (14, 14, 14))
        centred(d, (x + cw / 2, y + ch / 2), str(n), F('cardo', 62), gold)
for r_ in range(3): x, y = x0 + 12 * cw, y0 + r_ * ch; d.rectangle([x, y, x + 150, y + ch], outline=gold, width=5); centred(d, (x + 75, y + ch / 2), '2 to 1', F('cardoi', 40), gold)
for k, t in enumerate(['1st 12', '2nd 12', '3rd 12']): x = x0 + k * 4 * cw; d.rectangle([x, y0 + 3 * ch, x + 4 * cw, y0 + 3 * ch + 130], outline=gold, width=5); centred(d, (x + 2 * cw, y0 + 3 * ch + 65), t, F('cardo', 58), gold)
for k, t in enumerate(['1-18', 'EVEN', '◆', '◆', 'ODD', '19-36']):
    x = x0 + k * 2 * cw; y = y0 + 3 * ch + 130; d.rectangle([x, y, x + 2 * cw, y + 120], outline=gold, width=5)
    if t == '◆': d.polygon([(x + cw, y + 18), (x + 2 * cw - 30, y + 60), (x + cw, y + 102), (x + 30, y + 60)], fill=(190, 20, 28) if k == 2 else (14, 14, 14), outline=gold)
    else: centred(d, (x + cw, y + 60), t, F('cardo', 48), gold)
region('roulette_felt', 1024, 512, 0.08); commit('roulette_felt', im)
w = 1024; im = Image.new('RGB', (w, w), (60, 30, 16)); d = C(im)
ORDER = [0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10, 5, 24, 16, 33, 1, 20, 14, 31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26]
cx = cy = w / 2
for k, n in enumerate(ORDER):
    a0 = (k - 0.5) / 37 * 2 * math.pi - math.pi / 2; a1 = (k + 0.5) / 37 * 2 * math.pi - math.pi / 2
    col = (20, 120, 50) if n == 0 else (190, 20, 28) if n in RED else (16, 16, 16)
    pts = [(cx + math.cos(a0) * 500, cy + math.sin(a0) * 500), (cx + math.cos(a1) * 500, cy + math.sin(a1) * 500), (cx + math.cos(a1) * 380, cy + math.sin(a1) * 380), (cx + math.cos(a0) * 380, cy + math.sin(a0) * 380)]
    d.polygon(pts, fill=col, outline=(230, 200, 120))
    am = (a0 + a1) / 2; t = Image.new('RGBA', (90, 60), (0, 0, 0, 0)); centred(C(t), (45, 30), str(n), F('cardo', 40), (245, 230, 190))
    t = t.rotate(-math.degrees(am) - 90, expand=True); im.paste(t, (int(cx + math.cos(am) * 440 - t.width / 2), int(cy + math.sin(am) * 440 - t.height / 2)), t)
    pts2 = [(cx + math.cos(a0) * 378, cy + math.sin(a0) * 378), (cx + math.cos(a1) * 378, cy + math.sin(a1) * 378), (cx + math.cos(a1) * 300, cy + math.sin(a1) * 300), (cx + math.cos(a0) * 300, cy + math.sin(a0) * 300)]
    d.polygon(pts2, fill=(225, 225, 225), outline=(150, 150, 150))
d.ellipse([cx - 298, cy - 298, cx + 298, cy + 298], fill=(110, 56, 26)); d.ellipse([cx - 120, cy - 120, cx + 120, cy + 120], fill=(200, 160, 70))
for k in range(4): a = k * math.pi / 2; d.line([(cx, cy), (cx + math.cos(a) * 280, cy + math.sin(a) * 280)], fill=(210, 170, 80), width=18)
region('wheel', 512, 512, 0.75); commit('wheel', im)
im = felt(2048, 1024); d = C(im)
for k in range(7):
    a = math.radians(152 - k * 20.7); x, y = 1024 + math.cos(a) * 860, -60 + math.sin(a) * 860
    d.ellipse([x - 66, y - 66, x + 66, y + 66], outline=gold, width=6)
d.arc([1024 - 600, -60 - 600, 1024 + 600, -60 + 600], 22, 158, fill=gold, width=5)
t = Image.new('RGBA', (2048, 1024), (0, 0, 0, 0)); centred(C(t), (1024, 250), 'BLACKJACK PAYS 3 TO 2', F('cardo', 84), gold + (255,))
centred(C(t), (1024, 345), 'Dealer must draw to 16 and stand on all 17s', F('cardoi', 50), gold + (255,)); centred(C(t), (1024, 470), 'INSURANCE PAYS 2 TO 1', F('cardo', 44), gold + (255,))
im.paste(t, (0, 0), t); region('blackjack_felt', 1024, 512, 0.08); commit('blackjack_felt', im)
im = felt(1024, 1024, (20, 60, 120)); d = C(im)
d.ellipse([140, 300, 884, 724], outline=gold, width=8); centred(d, (512, 512), 'SILVER SIXPENCE', F('cardo', 70), gold); centred(d, (512, 590), "TEXAS HOLD'EM", F('cardoi', 44), gold)
region('poker_felt', 512, 512, 0.08); commit('poker_felt', im)
region('felt_plain', 256, 256, 0.08); commit('felt_plain', felt(256, 256))

# ------------------------------------------------------------------ screens and signs
def screen(name, w, h, draw, glow=0.9, rw=None, rh=None):
    im = Image.new('RGB', (w, h), (6, 8, 10)); draw(im, C(im)); region(name, rw or w // 2, rh or h // 2, 0.92); commit(name, im, glow=Image.eval(im, lambda v: int(v * glow)))
def cctv(im, d):
    w, h = im.size; r = random.Random(3)
    for k in range(9):
        x0, y0 = (k % 3) * w // 3, (k // 3) * h // 3; ww, hh = w // 3, h // 3
        a = np.zeros((hh, ww, 3), np.float32) + r.randint(40, 90); a += noise(ww, hh, 18, 0.6, k)[..., None]
        sub = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)); sd = C(sub)
        for q in range(6): x = r.randint(5, ww - 30); y = r.randint(hh // 3, hh - 20); sd.rectangle([x, y, x + r.randint(10, 40), y + r.randint(6, 20)], fill=(r.randint(90, 150),) * 3)
        for q in range(3): x = r.randint(10, ww - 10); y = r.randint(hh // 2, hh - 12); sd.ellipse([x - 4, y - 16, x + 4, y - 8], fill=(160, 160, 160)); sd.rectangle([x - 5, y - 8, x + 5, y + 8], fill=(120, 120, 130))
        sd.text((6, 4), f'CAM {k + 1:02d}', font=F('mono', 18), fill=(240, 240, 240)); sd.text((ww - 110, hh - 22), '23:14:%02d' % (k * 6), font=F('mono', 16), fill=(240, 240, 240))
        im.paste(sub.convert('L').convert('RGB'), (x0, y0)); d.rectangle([x0, y0, x0 + ww - 1, y0 + hh - 1], outline=(30, 30, 30), width=3)
screen('cctv', 1024, 640, cctv)
def pc(im, d):
    w, h = im.size; d.rectangle([0, 0, w, h], fill=(236, 238, 242)); d.rectangle([0, 0, w, 40], fill=(30, 60, 110)); d.text((14, 8), 'Sixpence Back Office  -  Takings', font=F('fira', 24), fill=(255, 255, 255))
    for r_ in range(14):
        y = 60 + r_ * 38; d.line([(10, y), (w - 10, y)], fill=(200, 204, 210))
        for c, t in enumerate(['Fri', 'Sat', 'Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Total', 'Slots', 'Tables', 'Bar', 'Cage', 'Float', 'Safe'][r_:r_ + 1] + [f'{random.randint(100, 9999):,}' for _ in range(5)]):
            d.text((20 + c * 190, y + 6), t, font=F('firar', 22), fill=(30, 30, 40))
    d.rectangle([w - 300, h - 120, w - 20, h - 20], fill=(255, 250, 160)); d.text((w - 290, h - 112), 'SAFE: 1987', font=F('comic', 40), fill=(20, 20, 120)); d.text((w - 290, h - 62), "don't tell Gaz!", font=F('comic', 28), fill=(20, 20, 120))
screen('pc', 1024, 640, pc, 0.8)
def tv(im, d):
    w, h = im.size; im.paste(grad(w, h // 2, (110, 160, 220), (190, 210, 230)), (0, 0)); im.paste(grad(w, h // 2, (60, 130, 50), (40, 100, 40)), (0, h // 2)); d = C(im)
    for k in range(6):
        x = 120 + k * 140 + (k % 2) * 30; y = h // 2 + 40 + k * 18
        d.ellipse([x - 60, y - 30, x + 40, y + 10], fill=(70 + k * 15, 40, 20)); d.rectangle([x - 10, y - 60, x + 10, y - 20], fill=[(200, 20, 20), (20, 60, 200), (240, 200, 20), (20, 150, 60), (240, 240, 240), (120, 30, 150)][k])
    d.rectangle([0, h - 70, w, h], fill=(20, 20, 70)); d.text((20, h - 58), '3:45 ABBOTS WEND  -  1st LUCKY LAD 5/1   2nd DOUBLE OR NOWT 11/4', font=F('fira', 28), fill=(255, 255, 255))
    d.rectangle([w - 160, 20, w - 20, 70], fill=(200, 20, 20)); d.text((w - 140, 26), 'LIVE', font=F('anton', 36), fill=(255, 255, 255))
screen('tv', 1024, 576, tv)
def till(im, d):
    w, h = im.size; d.rectangle([0, 0, w, h], fill=(230, 232, 236)); cols = [(200, 60, 60), (60, 140, 60), (50, 90, 180), (220, 160, 40), (120, 60, 150), (40, 150, 150)]
    for k in range(18): x, y = 10 + (k % 6) * 82, 60 + (k // 6) * 80; d.rounded_rectangle([x, y, x + 76, y + 72], 8, fill=cols[k % 6])
    d.rectangle([0, 0, w, 48], fill=(30, 30, 40)); d.text((10, 10), 'TABLE 4   £12.40', font=F('fira', 26), fill=(255, 255, 255)); d.rounded_rectangle([10, h - 70, w - 10, h - 10], 10, fill=(30, 160, 60)); centred(d, (w / 2, h - 40), 'PAY', F('anton', 40), (255, 255, 255))
screen('till', 512, 384, till)
def etg(im, d):
    w, h = im.size; im.paste(felt(w, h), (0, 0)); d = C(im)
    for c in range(12):
        for r_ in range(3):
            n = c * 3 + (3 - r_); x, y = 40 + c * 38, 120 + r_ * 70; d.rectangle([x, y, x + 38, y + 70], outline=(236, 214, 150), width=2); centred(d, (x + 19, y + 35), str(n), F('fira', 20), (230, 30, 30) if n in RED else (255, 255, 255))
    d.rectangle([0, 0, w, 90], fill=(10, 10, 10)); d.text((20, 20), 'LAST: 17  32  5  0  21', font=F('fira', 34), fill=(255, 220, 80)); d.rectangle([0, h - 90, w, h], fill=(10, 10, 10)); d.text((20, h - 66), 'CREDIT £20.00   BET £2.00   SPIN IN 0:12', font=F('fira', 30), fill=(255, 255, 255))
screen('etg', 512, 512, etg, rw=256, rh=256)
def cardterm(im, d):
    w, h = im.size; d.rectangle([0, 0, w, h], fill=(180, 220, 200)); centred(d, (w / 2, h / 3), '£12.40', F('fira', 44), (10, 10, 10)); centred(d, (w / 2, h * 0.7), 'TAP CARD', F('fira', 30), (10, 10, 10))
screen('cardterm', 256, 256, cardterm, rw=64, rh=64)
def vend(im, d):
    w, h = im.size; d.rectangle([0, 0, w, h], fill=(20, 20, 24)); r = random.Random(9)
    for row in range(6):
        y = 30 + row * 150; d.rectangle([20, y + 120, w - 20, y + 128], fill=(160, 160, 165))
        for c in range(5):
            x = 30 + c * 92; col = r.choice([(200, 30, 30), (30, 90, 200), (240, 200, 30), (40, 160, 60), (230, 110, 20), (120, 40, 150)])
            if row < 3: d.rounded_rectangle([x, y + 20, x + 70, y + 118], 6, fill=col); d.text((x + 6, y + 50), r.choice(['CRISP', 'CHOC', 'NUTS', 'BAR']), font=F('fira', 18), fill=(255, 255, 255))
            else: d.rounded_rectangle([x + 14, y + 10, x + 56, y + 118], 12, fill=col); d.rectangle([x + 14, y + 50, x + 56, y + 80], fill=(240, 240, 240))
            d.text((x + 20, y + 128), f'{row}{c}', font=F('mono', 16), fill=(255, 220, 80))
screen('vend', 512, 960, vend, 0.75, rw=256, rh=480)
def micro(im, d):
    w, h = im.size; d.rectangle([0, 0, w, h], fill=(20, 20, 20)); d.rectangle([20, 20, w - 140, h - 20], fill=(40, 42, 44)); d.rectangle([w - 120, 30, w - 20, 70], fill=(30, 60, 40)); d.text((w - 110, 36), '0:00', font=F('mono', 28), fill=(80, 255, 120))
    for k in range(9): x, y = w - 115 + (k % 3) * 32, 90 + (k // 3) * 32; d.rectangle([x, y, x + 26, y + 26], fill=(70, 70, 74))
screen('micro', 512, 320, micro, 0.3, rw=256, rh=160)

def sign(name, txt, bg, fg, w=512, h=160, font='archivo', sub=None, glow=False, pict=None):
    im = Image.new('RGB', (w, h), bg); d = C(im); d.rectangle([6, 6, w - 7, h - 7], outline=fg, width=4)
    tx = w / 2 if pict is None else w / 2 + h * 0.35
    if pict: pict(d, h)
    centred(d, (tx, h / 2 - (18 if sub else 0)), txt, fit(txt, font, w - 60 - (h if pict else 0), h * 0.5), fg)
    if sub: centred(d, (tx, h * 0.78), sub, fit(sub, 'fira', w - 60, h * 0.18), fg)
    region(name, w // 2, h // 2, 0.5); commit(name, im, glow=im if glow else None)
def runner(d, h):
    d.rectangle([14, 14, h - 14, h - 14], fill=(255, 255, 255)); c = (10, 150, 60); s = (h - 28) / 100.0; o = 14
    d.ellipse([o + 52 * s, o + 8 * s, o + 66 * s, o + 22 * s], fill=c); d.line([(o + 56 * s, o + 26 * s), (o + 44 * s, o + 58 * s)], fill=c, width=int(9 * s))
    d.line([(o + 44 * s, o + 58 * s), (o + 60 * s, o + 76 * s), (o + 56 * s, o + 94 * s)], fill=c, width=int(8 * s)); d.line([(o + 44 * s, o + 58 * s), (o + 30 * s, o + 72 * s), (o + 14 * s, o + 70 * s)], fill=c, width=int(8 * s))
    d.line([(o + 54 * s, o + 34 * s), (o + 72 * s, o + 46 * s)], fill=c, width=int(7 * s)); d.line([(o + 54 * s, o + 34 * s), (o + 36 * s, o + 40 * s)], fill=c, width=int(7 * s))
sign('sign_staff', 'STAFF ONLY', (20, 30, 80), (255, 255, 255)); sign('sign_private', 'PRIVATE', (140, 10, 20), (255, 255, 255), sub='AUTHORISED PERSONNEL ONLY')
sign('sign_cash', 'CASH OFFICE', (20, 20, 22), (230, 190, 90), sub='NO ENTRY  -  TIME DELAY LOCK'); sign('sign_office', 'MANAGER', (60, 40, 24), (240, 220, 180), font='cardo')
sign('sign_stock', 'STOCK ROOM', (20, 30, 80), (255, 255, 255)); sign('sign_staffroom', 'STAFF ROOM', (20, 30, 80), (255, 255, 255))
sign('sign_exit', 'FIRE EXIT', (10, 150, 60), (255, 255, 255), glow=True, pict=runner); sign('sign_cctv', 'CCTV', (250, 210, 20), (10, 10, 10), sub='IN OPERATION 24 HOURS')
sign('sign_18', 'OVER 18s ONLY', (20, 20, 22), (255, 255, 255), sub='ID MAY BE REQUESTED'); sign('sign_resp', 'Play responsibly', (110, 10, 20), (230, 190, 90), font='cardoi', sub='Set a limit. Take a break.')
sign('sign_cashier', 'CASHIER', (12, 12, 14), (236, 196, 96), w=1024, h=200, font='cardo', glow=True); sign('sign_bar', 'THE SIXPENCE BAR', (12, 12, 14), (236, 196, 96), w=1024, h=200, font='cardo', glow=True)
sign('sign_slots', 'SLOTS', (12, 12, 14), (255, 70, 90), w=512, h=160, font='anton', glow=True); sign('sign_tables', 'TABLE GAMES', (12, 12, 14), (236, 196, 96), w=768, h=160, font='cardo', glow=True)
sign('sign_toilets', 'TOILETS', (20, 30, 80), (255, 255, 255), sub='←  GENTS   LADIES  →'); sign('sign_wet', 'CAUTION', (250, 210, 20), (10, 10, 10), sub='WET FLOOR')
for k, lim in enumerate(['£5 - £500', '£2 - £200', '£10 - £1000', '£1 - £100']):
    im = Image.new('RGB', (256, 128), (6, 6, 8)); d = C(im); centred(d, (128, 40), 'MIN - MAX', F('fira', 24), (255, 200, 60)); centred(d, (128, 88), lim, F('mono', 34), (255, 80, 60))
    region(f'limit{k}', 128, 64, 0.9); commit(f'limit{k}', im, glow=im)

# ------------------------------------------------------------------ paper, money, chips, cards, packets, boxes
im = Image.new('RGB', (512, 768), (40, 30, 20)); d = C(im); d.rectangle([12, 12, 499, 755], outline=(200, 170, 90), width=6)
centred(d, (256, 70), 'DRINKS', F('cardo', 64), (236, 214, 150))
for k, (n, p) in enumerate([('Sixpence Gold', '4.60'), ('Northfield Lager', '4.20'), ('Abbots Wend Bitter', '4.10'), ('Ironbridge Stout', '4.80'), ('Mill Lane Cider', '4.50'),
                            ('House Wine 175ml', '5.20'), ('Prosecco', '6.50'), ('Spirit & Mixer', '5.40'), ('Double', '7.90'), ('Soft Drinks', '2.40'), ('Coffee / Tea', '2.20'), ('Crisps / Nuts', '1.30')]):
    y = 150 + k * 50; d.text((40, y), n, font=F('crimson', 32), fill=(240, 230, 210)); d.text((400, y), p, font=F('crimson', 32), fill=(240, 230, 210))
region('pricelist', 256, 384, 0.3); commit('pricelist', im)
for k, (title, lines) in enumerate([('STAFF ROTA - WK 47', ['MON  Gaz / Shaz', 'TUE  Dev / Kel', 'WED  Gaz / Mo', 'THU  Kel / Shaz', 'FRI  ALL HANDS', 'SAT  ALL HANDS', 'SUN  Dev / Mo']),
                                    ('MEMO', ['Till 2 short again.', 'Whoever it is -', 'we WILL find out.', '', 'Do NOT prop the', 'fire door open.', '- Management']),
                                    ('FIRE ASSEMBLY', ['Meet in the car', 'park by the sign.', '', 'Do not use the', 'lift.  (We have', 'no lift.)', '']),
                                    ('UNION NOTICE', ['Staff meeting', 'Tuesday 10am.', 'Biscuits provided.', '', 'Gaz - stop eating', 'the biscuits.', ''])]):
    im = Image.new('RGB', (512, 640), (248, 246, 238)); d = C(im); d.text((30, 30), title, font=F('fira', 40), fill=(20, 20, 30)); d.line([(30, 86), (480, 86)], fill=(20, 20, 30), width=3)
    for q, l in enumerate(lines): d.text((30, 120 + q * 66), l, font=F('comic' if k else 'firar', 36), fill=(30, 30, 60))
    region(f'notice{k}', 256, 320, 0.15); commit(f'notice{k}', im)
for k, (v, col) in enumerate([(20, (150, 60, 120)), (10, (200, 110, 40)), (50, (190, 40, 40))]):
    im = Image.new('RGB', (512, 256), col); a = np.asarray(im, np.float32); yy, xx = np.mgrid[0:256, 0:512]
    a *= (0.85 + 0.15 * np.sin(xx / 7.0 + np.sin(yy / 13.0) * 3))[..., None]; im = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)); d = C(im)
    d.rectangle([8, 8, 503, 247], outline=(250, 240, 220), width=4); d.text((24, 18), 'BANK OF ALBION', font=F('cardo', 34), fill=(250, 240, 220))
    d.ellipse([330, 60, 480, 210], fill=tuple(min(255, c + 60) for c in col)); centred(d, (405, 135), f'£{v}', F('cardo', 60), (40, 20, 20)); d.text((24, 190), f'{v} POUNDS  A{random.randint(10, 99)} {random.randint(100000, 999999)}', font=F('mono', 22), fill=(250, 240, 220))
    region(f'note{v}', 128, 64, 0.2); commit(f'note{v}', im)
im = Image.new('RGB', (256, 128), (150, 60, 120)); d = C(im)
for k in range(30): d.line([(0, 4 + k * 4), (256, 4 + k * 4)], fill=(170 + (k % 3) * 20, 80, 140), width=2)
d.rectangle([110, 0, 146, 128], fill=(240, 240, 230)); region('notes_edge', 128, 64, 0.2); commit('notes_edge', im)
for k, (v, col, ink) in enumerate([(1, (240, 240, 236), (30, 30, 30)), (5, (190, 30, 30), (255, 255, 255)), (25, (30, 130, 60), (255, 255, 255)), (100, (20, 20, 22), (255, 255, 255)), (500, (110, 40, 150), (255, 255, 255)), (1000, (230, 170, 30), (20, 20, 20))]):
    im = Image.new('RGB', (256, 256), (0, 0, 0)); d = C(im); d.ellipse([2, 2, 253, 253], fill=col)
    for q in range(8): a = q * math.pi / 4; d.pieslice([2, 2, 253, 253], math.degrees(a) - 8, math.degrees(a) + 8, fill=(250, 250, 250) if col[0] < 200 or col[1] < 200 else (30, 60, 150))
    d.ellipse([40, 40, 216, 216], fill=col); d.ellipse([52, 52, 204, 204], outline=ink, width=3); centred(d, (128, 128), str(v), F('anton', 76 if v < 100 else 60), ink)
    region(f'chip{v}', 64, 64, 0.55); commit(f'chip{v}', im)
im = Image.new('RGB', (256, 384), (150, 20, 30)); d = C(im); d.rounded_rectangle([8, 8, 247, 375], 18, outline=(255, 255, 255), width=8)
for q in range(0, 384, 16): d.line([(16, q), (240, q + 120)], fill=(170, 40, 50), width=3)
region('cardback', 64, 96, 0.4); commit('cardback', im)
for k, (r_, s, c) in enumerate([('A', '♠', (10, 10, 10)), ('K', '♥', (200, 10, 20)), ('7', '♣', (10, 10, 10)), ('Q', '♦', (200, 10, 20))]):
    im = Image.new('RGB', (256, 384), (250, 250, 246)); d = C(im); d.rounded_rectangle([4, 4, 251, 379], 18, outline=(200, 200, 200), width=4)
    d.text((18, 12), r_, font=F('cardo', 70), fill=c); d.text((22, 86), s, font=F('dejavu', 50), fill=c); centred(d, (128, 200), s, F('dejavu', 150), c)
    region(f'card{k}', 64, 96, 0.4); commit(f'card{k}', im)
for k, (n, f, col) in enumerate([('CRUNCHERS', 'Ready Salted', (30, 80, 200)), ('CRUNCHERS', 'Salt & Vinegar', (30, 150, 60)), ('CRUNCHERS', 'Cheese & Onion', (60, 140, 200)), ('PORKY BITES', 'Scratchings', (200, 60, 30))]):
    im = grad(256, 384, tuple(min(255, c + 60) for c in col), col); d = C(im); centred(d, (128, 110), n, fit(n, 'anton', 220, 60), (255, 230, 60), stroke_width=3, stroke_fill=(20, 20, 20))
    centred(d, (128, 190), f, fit(f, 'cardoi', 220, 36), (255, 255, 255)); d.ellipse([60, 230, 196, 340], fill=(230, 200, 120))
    region(f'crisps{k}', 128, 192, 0.6); commit(f'crisps{k}', im)
im = Image.new('RGB', (256, 512), (200, 30, 30)); d = C(im); centred(d, (128, 60), 'DRY ROASTED', F('anton', 40), (255, 230, 60)); centred(d, (128, 110), 'PEANUTS', F('anton', 52), (255, 255, 255))
for q in range(12): x, y = 30 + (q % 3) * 70, 160 + (q // 3) * 84; d.rounded_rectangle([x, y, x + 56, y + 76], 6, fill=(250, 220, 120))
region('peanuts', 128, 256, 0.4); commit('peanuts', im)
for k, (t1, t2) in enumerate([('KESTERTON', 'LONDON DRY GIN  6 x 70cl'), ('SIXPENCE GOLD', '24 x 330ml BOTTLES'), ('CRUNCHERS', '48 x 32.5g ASSORTED'), ('GLASSWARE', 'FRAGILE  -  THIS WAY UP'), ('FIZZO', 'COLA 24 x 330ml'), ('BAR ROLL', 'BLUE  x 6')]):
    im = Image.new('RGB', (512, 384), (176, 136, 90)); d = C(im)
    for q in range(0, 384, 9): d.line([(0, q), (512, q)], fill=(168, 128, 84), width=1)
    d.rectangle([0, 170, 512, 210], fill=(196, 160, 110)); centred(d, (256, 100), t1, fit(t1, 'anton', 440, 90), (40, 30, 30)); centred(d, (256, 290), t2, fit(t2, 'fira', 440, 34), (40, 30, 30))
    if k == 3: d.text((30, 320), '↑↑', font=F('dejavu', 50), fill=(200, 20, 20))
    region(f'box{k}', 256, 192, 0.05); commit(f'box{k}', im)
im = Image.new('RGB', (512, 512), (250, 250, 250)); d = C(im); d.ellipse([10, 10, 501, 501], fill=(250, 250, 248), outline=(20, 20, 20), width=10)
for q in range(12): a = q * math.pi / 6; centred(d, (256 + math.sin(a) * 200, 256 - math.cos(a) * 200), str(q or 12), F('fira', 50), (20, 20, 20))
d.line([(256, 256), (256 + 120, 256 - 70)], fill=(20, 20, 20), width=12); d.line([(256, 256), (256 - 40, 256 - 170)], fill=(20, 20, 20), width=8)
region('clock', 128, 128, 0.6); commit('clock', im)
im = Image.new('RGB', (256, 256), (230, 230, 232)); d = C(im)
for k in range(12): x, y = 20 + (k % 3) * 75, 20 + (k // 3) * 56; d.rounded_rectangle([x, y, x + 64, y + 46], 8, fill=(250, 250, 250), outline=(140, 140, 150), width=2); centred(d, (x + 32, y + 23), '123456789*0#'[k], F('fira', 28), (30, 30, 40))
region('keypad', 64, 64, 0.5); commit('keypad', im)
im = Image.new('RGB', (512, 384), (240, 240, 236)); d = C(im)
for q, (t, c) in enumerate([('FLOAT CHECK', (20, 20, 20)), ('Cage 1   £2,000', (40, 40, 120)), ('Cage 2   £2,000', (40, 40, 120)), ('Bar       £  300', (40, 40, 120)), ('Safe    £48,250', (160, 20, 20))]):
    d.text((30, 30 + q * 66), t, font=F('comic', 40), fill=c)
region('clipboard', 128, 96, 0.2); commit('clipboard', im)
im = Image.new('RGB', (512, 256), (10, 10, 12)); d = C(im); centred(d, (256, 128), 'WINNER!', F('anton', 150), (255, 210, 40), stroke_width=4, stroke_fill=(200, 20, 20))
region('winner', 256, 128, 0.9); commit('winner', im, glow=im)
im = Image.new('RGB', (512, 512), (24, 24, 28)); d = C(im)
for k in range(5): d.text((30, 40 + k * 90), ['CHIPS', 'CASH', 'TICKETS', 'ID', 'CARDS'][k], font=F('fira', 60), fill=(200, 200, 210))
region('panel_dark', 128, 128, 0.6); commit('panel_dark', im)

# ------------------------------------------------------------------ write out
print('atlas used to y =', shelf.y, 'of', S, '|', len(regions), 'regions,', len(PAL), 'swatches')
albedo.save(os.path.join(OUT, 'casino_albedo.png'), optimize=True)
emit.resize((H2, H2), Image.LANCZOS).save(os.path.join(OUT, 'casino_emit.png'), optimize=True)
normal.resize((H2, H2), Image.LANCZOS).save(os.path.join(OUT, 'casino_normal.png'), optimize=True)
mask.resize((H2, H2), Image.NEAREST).save(os.path.join(OUT, 'casino_mask.png'), optimize=True)
json.dump(dict(size=S, palette=PAL, regions={k: v['uv'] for k, v in regions.items()}, labels=len(LABELS), mats=len(MATS), games=len(GAMES)), open(os.path.join(OUT, 'casino_atlas.json'), 'w'), indent=0)

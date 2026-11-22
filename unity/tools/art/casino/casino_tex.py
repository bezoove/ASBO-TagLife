"""
Textures for the Silver Sixpence casino on the edge of town (all generated here, own work; the name is made up):
  Casino/carpet.png          the loud casino carpet: deep red with gold and navy medallions (2 m repeat, tiles)
  Casino/wallpaper.png       burgundy flock wallpaper (1.5 m repeat)
  Casino/sign.png            the illuminated fascia: SILVER SIXPENCE over CASINO (emissive)
  Casino/pylon.png           the tall roadside sign: CASINO, the name and the hours (emissive)
  Casino/slots.png           a fruit machine's front: lit header, three reels, buttons (emissive)
  Casino/roulette_felt.png   roulette table felt with the layout printed on it
  Casino/blackjack_felt.png  blackjack table felt: the betting spots and the rules round the arc
Run: python3 casino_tex.py <unity Assets folder>
"""
import sys, os, math
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

A = sys.argv[1]; OUT = os.path.join(A, 'Art', 'Casino'); os.makedirs(OUT, exist_ok=True)
F = '/usr/share/fonts/truetype/liberation/'
SERIF, SANS = F + 'LiberationSerif-Bold.ttf', F + 'LiberationSans-Bold.ttf'
ANTON = '/usr/share/fonts/truetype/google-fonts/ofl/anton/Anton-Regular.ttf'
if not os.path.exists(ANTON): ANTON = SANS
GOLD, CREAM, RED, NAVY = (232, 190, 80), (255, 244, 220), (150, 14, 22), (18, 26, 70)

def font(p, s): return ImageFont.truetype(p, s)
def centred(d, xy, text, f, fill, **kw):
    b = d.textbbox((0, 0), text, font=f); d.text((xy[0] - (b[2] - b[0]) / 2 - b[0], xy[1] - (b[3] - b[1]) / 2 - b[1]), text, font=f, fill=fill, **kw)

# ---------------------------------------------------------------- carpet: tiles in both directions
def carpet():
    n = 512; im = Image.new('RGB', (n, n), (120, 10, 18)); d = ImageDraw.Draw(im)
    step = 128
    for gy in range(-1, 5):
        for gx in range(-1, 5):
            cx, cy = gx * step + (step // 2 if gy % 2 else 0), gy * step
            for ox, oy in ((0, 0), (n, 0), (-n, 0), (0, n), (0, -n)):
                x, y = cx + ox, cy + oy
                d.ellipse((x - 46, y - 46, x + 46, y + 46), outline=GOLD, width=5)
                d.ellipse((x - 30, y - 30, x + 30, y + 30), fill=NAVY)
                for a in range(8):
                    t = a * math.pi / 4; px, py = x + math.cos(t) * 38, y + math.sin(t) * 38
                    d.ellipse((px - 7, py - 7, px + 7, py + 7), fill=GOLD)
                d.regular_polygon((x, y, 18), 4, rotation=45, fill=(200, 40, 30))
                d.ellipse((x - 6, y - 6, x + 6, y + 6), fill=GOLD)
                # little diamonds between the medallions
                for dx, dy in ((step // 2, step // 2), (-step // 2, step // 2)):
                    d.regular_polygon((x + dx, y + dy - (step // 2 if gy % 2 else 0) * 0, 10), 4, fill=(30, 110, 60))
    a = np.asarray(im).astype(np.float32)
    rng = np.random.default_rng(3); a *= (0.92 + 0.12 * rng.random((n, n, 1)))  # pile
    Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).save(os.path.join(OUT, 'carpet.png'))

def wallpaper():
    n = 512; im = Image.new('RGB', (n, n), (92, 12, 22)); d = ImageDraw.Draw(im)
    for gy in range(0, 3):
        for gx in range(0, 3):
            for ox in (-n, 0, n):
                x, y = gx * 256 + (128 if gy % 2 else 0) + ox, gy * 256
                for k in range(6):
                    t = k * math.pi / 3
                    d.ellipse((x + math.cos(t) * 52 - 26, y + math.sin(t) * 70 - 34, x + math.cos(t) * 52 + 26, y + math.sin(t) * 70 + 34), fill=(110, 18, 30))
                d.ellipse((x - 22, y - 30, x + 22, y + 30), fill=(130, 26, 38))
    for x in range(0, n, 128): d.line((x, 0, x, n), fill=(80, 10, 18), width=2)
    im.filter(ImageFilter.GaussianBlur(1.2)).save(os.path.join(OUT, 'wallpaper.png'))

# ---------------------------------------------------------------- the fascia sign (lit at night)
def sign():
    W, H = 2048, 512; im = Image.new('RGBA', (W, H), (8, 10, 24, 255)); d = ImageDraw.Draw(im)
    d.rounded_rectangle((14, 14, W - 14, H - 14), 26, outline=GOLD, width=10)
    d.rounded_rectangle((34, 34, W - 34, H - 34), 18, outline=(120, 95, 40), width=3)
    # bulbs round the edge
    for x in range(70, W - 50, 56):
        for y in (58, H - 58): d.ellipse((x - 9, y - 9, x + 9, y + 9), fill=(255, 236, 170))
    txt = Image.new('RGBA', (W, H), (0, 0, 0, 0)); t = ImageDraw.Draw(txt)
    centred(t, (W / 2, 200), 'SILVER SIXPENCE', font(SERIF, 158), (236, 240, 248, 255))
    centred(t, (W / 2, 385), 'C  A  S  I  N  O', font(ANTON, 120), GOLD + (255,))
    halo = txt.filter(ImageFilter.GaussianBlur(14)); im = Image.alpha_composite(im, halo); im = Image.alpha_composite(im, txt)
    # a sixpence coin either end
    d = ImageDraw.Draw(im)
    for cx in (150, W - 150):
        d.ellipse((cx - 80, 256 - 80, cx + 80, 256 + 80), fill=(200, 205, 214), outline=(120, 124, 132), width=7)
        d.ellipse((cx - 60, 256 - 60, cx + 60, 256 + 60), outline=(150, 154, 162), width=4)
        centred(d, (cx, 250), '6d', font(SERIF, 72), (110, 114, 124))
    im.convert('RGB').save(os.path.join(OUT, 'sign.png'))

def pylon():
    W, H = 512, 1024; im = Image.new('RGBA', (W, H), (10, 12, 30, 255)); d = ImageDraw.Draw(im)
    d.rectangle((10, 10, W - 10, H - 10), outline=GOLD, width=12)
    txt = Image.new('RGBA', (W, H), (0, 0, 0, 0)); t = ImageDraw.Draw(txt)
    for i, ch in enumerate('CASINO'):
        centred(t, (W / 2, 120 + i * 118), ch, font(ANTON, 128), (255, 70, 60, 255))
    halo = txt.filter(ImageFilter.GaussianBlur(10)); im = Image.alpha_composite(im, halo); im = Image.alpha_composite(im, txt)
    d = ImageDraw.Draw(im)
    d.rectangle((40, 820, W - 40, 990), fill=(236, 240, 248))
    centred(d, (W / 2, 862), 'Silver Sixpence', font(SERIF, 58), NAVY)
    centred(d, (W / 2, 940), 'OPEN TILL 4AM', font(SANS, 44), RED)
    im.convert('RGB').save(os.path.join(OUT, 'pylon.png'))

# ---------------------------------------------------------------- a fruit machine front
def slots():
    W, H = 512, 1024; im = Image.new('RGB', (W, H), (40, 6, 10)); d = ImageDraw.Draw(im)
    d.rectangle((0, 0, W, 230), fill=(20, 24, 90))
    for x in range(20, W, 40): d.ellipse((x - 8, 14, x + 8, 30), fill=(255, 230, 140)); d.ellipse((x - 8, 200, x + 8, 216), fill=(255, 230, 140))
    centred(d, (W / 2, 95), 'FRUIT', font(ANTON, 100), GOLD); centred(d, (W / 2, 165), 'FRENZY', font(ANTON, 64), (255, 90, 70))
    d.rectangle((30, 270, W - 30, 560), fill=(230, 226, 214), outline=GOLD, width=8)
    for i, (sym, col) in enumerate((('7', (210, 20, 30)), ('BAR', (20, 20, 24)), ('7', (210, 20, 30)))):
        x = 30 + (W - 60) * (i + 0.5) / 3
        if sym == 'BAR':
            d.rounded_rectangle((x - 62, 380, x + 62, 450), 10, fill=col); centred(d, (x, 414), 'BAR', font(SANS, 42), (255, 255, 255))
        else: centred(d, (x, 412), sym, font(ANTON, 170), col)
        if i: d.line((30 + (W - 60) * i / 3, 270, 30 + (W - 60) * i / 3, 560), fill=(150, 140, 120), width=4)
    d.line((30, 415, W - 30, 415), fill=(255, 200, 40), width=4)
    d.rectangle((30, 600, W - 30, 700), fill=(10, 10, 12)); centred(d, (W / 2, 650), 'CREDIT  0042', font(SANS, 46), (255, 70, 40))
    for i, c in enumerate(((220, 30, 30), (240, 200, 30), (40, 160, 60))): x = 110 + i * 146; d.ellipse((x - 46, 760, x + 46, 852), fill=c, outline=(255, 255, 255), width=4)
    centred(d, (W / 2, 930), 'PAYS 94%', font(SANS, 40), CREAM)
    im.save(os.path.join(OUT, 'slots.png'))

# ---------------------------------------------------------------- table felts
REDS = {1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36}
def felt_base(W, H, seed):
    rng = np.random.default_rng(seed); a = np.zeros((H, W, 3), np.float32); a[:] = (16, 92, 48)
    a *= (0.94 + 0.08 * rng.random((H, W, 1))); return Image.fromarray(a.astype(np.uint8))

def roulette_felt():
    W, H = 1024, 512; im = felt_base(W, H, 5); d = ImageDraw.Draw(im)
    cw, ch, x0, y0 = 62, 80, 150, 60; f = font(SANS, 30)
    d.rectangle((x0 - cw, y0, x0, y0 + 3 * ch), fill=(10, 120, 50), outline=CREAM, width=3); centred(d, (x0 - cw / 2, y0 + 1.5 * ch), '0', f, CREAM)
    for c in range(12):
        for r in range(3):
            n = c * 3 + (2 - r) + 1; x, y = x0 + c * cw, y0 + r * ch
            d.rectangle((x, y, x + cw, y + ch), fill=(170, 18, 22) if n in REDS else (16, 16, 18), outline=CREAM, width=3)
            centred(d, (x + cw / 2, y + ch / 2), str(n), f, CREAM)
    for r in range(3): x = x0 + 12 * cw; d.rectangle((x, y0 + r * ch, x + cw, y0 + (r + 1) * ch), outline=CREAM, width=3); centred(d, (x + cw / 2, y0 + r * ch + ch / 2), '2:1', font(SANS, 22), CREAM)
    for k, lab in enumerate(('1st 12', '2nd 12', '3rd 12')): x = x0 + k * 4 * cw; d.rectangle((x, y0 + 3 * ch, x + 4 * cw, y0 + 3 * ch + 70), outline=CREAM, width=3); centred(d, (x + 2 * cw, y0 + 3 * ch + 35), lab, f, CREAM)
    for k, lab in enumerate(('1-18', 'EVEN', '', '', 'ODD', '19-36')):
        x = x0 + k * 2 * cw; y = y0 + 3 * ch + 70; d.rectangle((x, y, x + 2 * cw, y + 70), outline=CREAM, width=3)
        if k == 2: d.regular_polygon((x + cw, y + 35, 26), 4, fill=(170, 18, 22))
        elif k == 3: d.regular_polygon((x + cw, y + 35, 26), 4, fill=(16, 16, 18))
        else: centred(d, (x + cw, y + 35), lab, f, CREAM)
    im.save(os.path.join(OUT, 'roulette_felt.png'))

def blackjack_felt():
    W, H = 1024, 512; im = felt_base(W, H, 9); d = ImageDraw.Draw(im)
    cx, cy = W / 2, -120
    for r, txt, fs in ((430, 'BLACKJACK PAYS 3 TO 2', 34), (370, 'DEALER MUST STAND ON ALL 17s', 26), (320, 'INSURANCE PAYS 2 TO 1', 24)):
        d.arc((cx - r, cy - r, cx + r, cy + r), 20, 160, fill=GOLD if fs > 30 else CREAM, width=3)
        f = font(SERIF, fs); total = sum(d.textlength(c, font=f) for c in txt); ang = math.pi / 2 + total / 2 / (r - 22)
        for c in txt:
            w = d.textlength(c, font=f); a = ang - w / 2 / (r - 22)
            px, py = cx + math.cos(a) * (r - 22), cy + math.sin(a) * (r - 22)
            g = Image.new('RGBA', (60, 60), (0, 0, 0, 0)); ImageDraw.Draw(g).text((30, 30), c, font=f, fill=GOLD if fs > 30 else CREAM, anchor='mm')
            g = g.rotate(90 - math.degrees(a), resample=Image.BICUBIC)  # letters lean with the arc, tops to the centre
            im.paste(g, (int(px - 30), int(py - 30)), g)
            ang -= w / (r - 22)
    for k in range(5):
        a = math.radians(30 + k * 30); x, y = cx + math.cos(a) * 520, cy + math.sin(a) * 520
        d.ellipse((x - 44, y - 44, x + 44, y + 44), outline=CREAM, width=4)
    im.save(os.path.join(OUT, 'blackjack_felt.png'))

carpet(); wallpaper(); sign(); pylon(); slots(); roulette_felt(); blackjack_felt()
print('casino textures written to', OUT)

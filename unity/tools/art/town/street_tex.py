"""
Textures for the streets that fill the gaps in town (all generated here, own work, every name made up).
Run: python3 street_tex.py <unity Assets folder>
  Art/Town/street_shopfronts.png  24 more shopfronts, the same layout as the high street's (4 columns of 512 x 340
                                  cells in a 2048 square: a 72 px fascia board over the window and door)
  Art/Town/street_shopfronts2.png 12 more in the same cells (4 columns x 3 rows, 2048 x 1024)
  Art/Town/street_signs.png       name boards for the bigger buildings (2 columns of 1024 x 170 cells, like
                                  service_signs.png): the mill, the office block, the car park, the chambers, the hall
"""
import sys, os, random
from PIL import Image, ImageDraw, ImageFont

ASSETS = sys.argv[1]
OUT = os.path.join(ASSETS, 'Art', 'Town'); os.makedirs(OUT, exist_ok=True)
BOLD = '/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf'
REG = '/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf'
SERIF = '/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf'


def fit(d, text, path, size, maxw):
    while size > 8:
        f = ImageFont.truetype(path, size)
        if d.textlength(text, font=f) <= maxw: return f
        size -= 2
    return ImageFont.truetype(path, size)


SHOPS = [  # (name, sub line, fascia colour, text colour, what's in the window)
    ("STAR KEBAB & PIZZA", "OPEN TIL 3AM  FREE DELIVERY", (180, 25, 20), (255, 225, 70), "menu"),
    ("LUCKY GARDEN", "CHINESE TAKEAWAY", (150, 15, 20), (240, 200, 90), "menu"),
    ("THE OFFY", "BEERS  WINES  SPIRITS  CIGS", (25, 70, 35), (250, 220, 60), "bottles"),
    ("SPIN CYCLE", "LAUNDERETTE  SERVICE WASHES", (30, 110, 190), (255, 255, 255), "laundry"),
    ("QUICK CASH", "PAWNBROKERS  CHEQUES CASHED", (110, 20, 40), (255, 210, 80), "stock"),
    ("PAUL'S BUTCHERS", "QUALITY MEATS SINCE 1974", (120, 20, 25), (255, 255, 255), "butcher"),
    ("ISTANBUL BARBERS", "HOT TOWEL SHAVES  NO APPOINTMENT", (20, 20, 22), (225, 190, 90), "salon"),
    ("POLSKI SKLEP", "EUROPEAN FOOD", (240, 240, 238), (200, 20, 35), "shelves"),
    ("INK WELL TATTOO", "PIERCING  WALK INS WELCOME", (15, 15, 15), (200, 30, 30), "frosted"),
    ("WEND MOBILITY", "SCOOTERS  CHAIRS  DAILY AIDS", (20, 80, 150), (255, 255, 255), "stock"),
    ("ANGLERS CORNER", "TACKLE  BAIT  PERMITS", (30, 80, 50), (240, 240, 230), "stock"),
    ("BETWISE", "BOOKMAKERS", (15, 90, 60), (255, 220, 50), "frosted"),
    ("CURRY MAHAL", "INDIAN RESTAURANT & TAKEAWAY", (110, 20, 50), (240, 200, 90), "cafe"),
    ("BAKERS DOZEN", "BAKERY  HOT PIES  SANDWICHES", (120, 70, 35), (250, 235, 200), "bakery"),
    ("IRON PUMP GYM", "OPEN 24/7  NO CONTRACT", (25, 25, 25), (250, 210, 0), "frosted"),
    ("SMILE DENTAL", "NEW PATIENTS WELCOME", (240, 245, 245), (0, 120, 130), "office"),
    ("CUT ABOVE", "UNISEX HAIR SALON", (235, 160, 190), (40, 20, 30), "salon"),
    ("SPICE BAZAAR", "WORLD FOODS  HALAL MEAT", (230, 120, 20), (255, 255, 255), "shelves"),
    ("GAME ZONE", "BUY  SELL  TRADE", (80, 30, 130), (120, 250, 220), "stock"),
    ("HEEL & KEY", "SHOE REPAIRS  KEYS CUT  ENGRAVING", (240, 200, 30), (25, 25, 25), "office"),
    ("DEBBIE'S FLOWERS", "WEDDINGS  FUNERALS  BOUQUETS", (40, 100, 60), (255, 255, 255), "flowers"),
    ("TO LET", "01632 960 115", (235, 235, 230), (200, 30, 30), "boarded"),
    ("", "", (60, 60, 62), (0, 0, 0), "shutter"),
    ("CLOSED", "", (200, 200, 195), (120, 120, 118), "whitewash"),
]


SHOPS2 = [
    ("WEND PHARMACY", "PRESCRIPTIONS  TRAVEL CLINIC", (20, 120, 80), (255, 255, 255), "shelves"),
    ("HARGREAVES & CO", "SOLICITORS  NO WIN NO FEE", (30, 40, 70), (230, 210, 150), "office"),
    ("THE COFFEE POT", "BREAKFAST  LUNCH  CAKES", (240, 232, 215), (90, 50, 30), "cafe"),
    ("MR FIXIT", "PHONES  LAPTOPS  CONSOLES", (240, 120, 20), (20, 20, 20), "stock"),
    ("WEND ANIMAL RESCUE", "CHARITY SHOP  DONATIONS WELCOME", (90, 50, 130), (255, 255, 255), "stock"),
    ("STAR CARS", "MINICABS 24/7  01632 960 777", (20, 20, 20), (250, 210, 0), "office"),
    ("PIZZA PALACE", "PIZZA  WINGS  DONER", (200, 30, 30), (255, 255, 255), "menu"),
    ("VALUE MART", "CONVENIENCE STORE  OPEN 7 TIL 11", (230, 40, 30), (255, 230, 60), "shelves"),
    ("WEND HOMES", "SALES  LETTINGS  VALUATIONS", (20, 90, 110), (255, 255, 255), "office"),
    ("CASH PLUS", "LOANS  FOREIGN EXCHANGE", (230, 200, 30), (30, 30, 30), "frosted"),
    ("UNIT AVAILABLE", "01632 960 116", (235, 235, 230), (200, 30, 30), "boarded"),
    ("", "", (60, 60, 62), (0, 0, 0), "shutter"),
]


def shopfronts(shops, rows, fname, seed):
    W, CH = 512, 340; img = Image.new('RGB', (W * 4, rows), (40, 40, 40)); d = ImageDraw.Draw(img); r = random.Random(seed)
    for i, (name, sub, bg, fg, kind) in enumerate(shops):
        x, y = (i % 4) * W, (i // 4) * CH
        d.rectangle([x, y, x + W - 1, y + 71], fill=bg)
        d.rectangle([x, y + 66, x + W - 1, y + 71], fill=tuple(int(c * 0.6) for c in bg))
        if name and kind != "whitewash":
            f = fit(d, name, BOLD, 40, W - 40); d.text((x + W // 2, y + 26), name, font=f, fill=fg, anchor='mm')
            f2 = fit(d, sub, REG, 16, W - 60); d.text((x + W // 2, y + 55), sub, font=f2, fill=fg, anchor='mm')
        fy0, fy1 = y + 72, y + CH - 1
        d.rectangle([x, fy0, x + W - 1, fy1], fill=(70, 72, 74))
        wx0, wx1, dx0 = x + 10, x + W - 150, x + W - 130

        def glass(a, b, c, e, dark=0.0):
            for yy in range(b, e):
                t = (yy - b) / max(1, e - b); g = int((90 - 50 * t) * (1 - dark)); d.line([a, yy, c, yy], fill=(g // 2 + 10, g // 2 + 18, g // 2 + 26))
        glass(wx0, fy0 + 10, wx1, fy1 - 30); glass(dx0, fy0 + 10, x + W - 12, fy1 - 4, 0.2)
        d.rectangle([wx0, fy1 - 30, wx1, fy1 - 4], fill=(110, 110, 112))
        d.rectangle([dx0 + 8, fy0 + 120, dx0 + 12, fy0 + 170], fill=(200, 200, 200))
        ix0, iy0, ix1, iy1 = wx0 + 6, fy0 + 16, wx1 - 6, fy1 - 36
        if kind == "bottles":
            for row in range(4):
                yy = iy0 + 20 + row * 52; d.rectangle([ix0, yy + 40, ix1, yy + 44], fill=(150, 120, 80))
                for bx in range(ix0 + 4, ix1 - 10, 14):
                    c = r.choice([(40, 90, 40), (120, 60, 20), (200, 200, 190), (90, 20, 30)]); d.rectangle([bx, yy + 8, bx + 8, yy + 40], fill=c); d.rectangle([bx + 2, yy, bx + 6, yy + 8], fill=c)
            d.rectangle([ix0 + 40, iy0 + 4, ix0 + 220, iy0 + 30], fill=(250, 230, 40)); d.text((ix0 + 130, iy0 + 17), "4 CANS £5", font=ImageFont.truetype(BOLD, 18), fill=(200, 20, 20), anchor='mm')
        elif kind == "menu":
            d.rectangle([ix0 + 20, iy0 + 10, ix0 + 170, iy0 + 210], fill=(250, 240, 200))
            for k in range(9): d.rectangle([ix0 + 30, iy0 + 30 + k * 19, ix0 + 30 + r.randint(60, 120), iy0 + 36 + k * 19], fill=(60, 30, 30))
            d.rectangle([ix0 + 190, iy0 + 120, ix1 - 10, iy1], fill=(160, 160, 160)); d.rectangle([ix0 + 200, iy0 + 96, ix0 + 250, iy0 + 120], fill=(220, 120, 40))
        elif kind == "stock":
            for row in range(5):
                yy = iy0 + 6 + row * 42
                for bx in range(ix0, ix1 - 20, r.randint(22, 34)): d.rectangle([bx, yy, bx + r.randint(14, 26), yy + r.randint(20, 34)], fill=(r.randint(60, 240), r.randint(60, 240), r.randint(60, 240)))
            d.rectangle([ix0 + 60, iy0 + 60, ix0 + 230, iy0 + 100], fill=(255, 255, 255)); tag = "DONATE" if "CHARITY" in sub else "WE BUY" if any(w in sub for w in ("PAWN", "TRADE", "PHONES")) else "SALE"
            d.text((ix0 + 145, iy0 + 80), tag, font=ImageFont.truetype(BOLD, 26), fill=(220, 20, 20), anchor='mm')
        elif kind == "shelves":
            for row in range(5):
                yy = iy0 + 10 + row * 42; d.rectangle([ix0, yy + 32, ix1, yy + 35], fill=(200, 200, 200))
                for bx in range(ix0 + 2, ix1 - 8, 10): d.rectangle([bx, yy + 12 + r.randint(0, 10), bx + 7, yy + 32], fill=(r.randint(80, 250), r.randint(80, 250), r.randint(80, 250)))
        elif kind == "frosted":
            d.rectangle([ix0, iy0, ix1, iy1], fill=(185, 195, 200)); d.text(((ix0 + ix1) // 2, (iy0 + iy1) // 2), name.split()[0] if name else "", font=ImageFont.truetype(BOLD, 34), fill=(140, 150, 160), anchor='mm')
        elif kind == "salon":
            for k in range(3): cx = ix0 + 40 + k * 100; d.ellipse([cx, iy0 + 60, cx + 50, iy0 + 110], fill=(30, 30, 30)); d.rectangle([cx - 5, iy0 + 30, cx + 55, iy0 + 60], fill=(220, 220, 225))
            d.rectangle([ix0, iy1 - 20, ix1, iy1], fill=(240, 240, 240))
            # the barber's pole by the door
            for k in range(8): d.rectangle([dx0 - 18, fy0 + 20 + k * 22, dx0 - 6, fy0 + 31 + k * 22], fill=(220, 30, 30) if k % 2 else (240, 240, 240))
        elif kind == "cafe":
            for k in range(3): cx = ix0 + 40 + k * 110; d.rectangle([cx, iy0 + 140, cx + 70, iy0 + 146], fill=(150, 90, 50)); d.rectangle([cx + 30, iy0 + 146, cx + 36, iy1], fill=(80, 60, 40))
            d.rectangle([ix0 + 10, iy0 + 10, ix0 + 140, iy0 + 40], fill=(255, 255, 255)); d.text((ix0 + 75, iy0 + 25), "EAT IN", font=ImageFont.truetype(BOLD, 20), fill=(200, 30, 30), anchor='mm')
        elif kind == "office":
            d.rectangle([ix0, iy0 + 100, ix1, iy0 + 108], fill=(200, 200, 200))
            for k in range(4): cx = ix0 + 20 + k * 80; d.rectangle([cx, iy0 + 20, cx + 60, iy0 + 90], fill=(230, 230, 230)); d.rectangle([cx + 5, iy0 + 25, cx + 55, iy0 + 40], fill=bg)
        elif kind == "laundry":
            for k in range(4):
                cx = ix0 + 14 + k * 82; d.rectangle([cx, iy0 + 60, cx + 70, iy1], fill=(235, 235, 235))
                d.ellipse([cx + 10, iy0 + 80, cx + 60, iy0 + 130], fill=(60, 80, 100), outline=(150, 150, 150), width=4)
            d.rectangle([ix0, iy0 + 10, ix1, iy0 + 40], fill=(255, 255, 255)); d.text(((ix0 + ix1) // 2, iy0 + 25), "WASH £4.50  DRY 20p", font=ImageFont.truetype(BOLD, 18), fill=(30, 110, 190), anchor='mm')
        elif kind == "butcher":
            d.rectangle([ix0, iy0 + 120, ix1, iy1], fill=(235, 235, 235))
            for k in range(7):
                cx = ix0 + 8 + k * 46; d.rectangle([cx, iy0 + 130, cx + 40, iy0 + 175], fill=(r.randint(150, 190), r.randint(30, 50), r.randint(35, 55)))
                d.rectangle([cx + 12, iy0 + 120, cx + 30, iy0 + 132], fill=(40, 120, 50))
            for k in range(5): cx = ix0 + 30 + k * 60; d.line([cx, iy0, cx, iy0 + 40], fill=(180, 180, 180), width=2); d.ellipse([cx - 12, iy0 + 40, cx + 12, iy0 + 90], fill=(170, 60, 50))
        elif kind == "bakery":
            for row in range(3):
                yy = iy0 + 30 + row * 60; d.rectangle([ix0, yy + 40, ix1, yy + 44], fill=(190, 190, 190))
                for bx in range(ix0 + 6, ix1 - 30, 38): d.ellipse([bx, yy + 14, bx + 32, yy + 42], fill=(r.randint(170, 215), r.randint(110, 150), r.randint(50, 80)))
            d.rectangle([ix0 + 10, iy0 + 2, ix0 + 230, iy0 + 26], fill=(255, 255, 255)); d.text((ix0 + 120, iy0 + 14), "SAUSAGE ROLLS 3 FOR £2", font=ImageFont.truetype(BOLD, 14), fill=(120, 70, 35), anchor='mm')
        elif kind == "flowers":
            for k in range(14):
                cx, cy = ix0 + r.randint(10, ix1 - ix0 - 40), iy0 + r.randint(60, iy1 - iy0 - 40)
                d.rectangle([cx + 12, cy + 18, cx + 26, cy + 46], fill=(60, 110, 60))
                for p in range(5): px, py = cx + r.randint(0, 30), cy + r.randint(0, 22); d.ellipse([px, py, px + 12, py + 12], fill=r.choice([(230, 60, 80), (250, 220, 60), (250, 250, 250), (180, 80, 200), (250, 140, 40)]))
            d.rectangle([ix0, iy1 - 24, ix1, iy1], fill=(70, 50, 30))
        elif kind == "boarded":
            d.rectangle([wx0, fy0 + 10, x + W - 12, fy1 - 4], fill=(160, 130, 90))
            for yy in range(fy0 + 10, fy1, 30): d.line([wx0, yy, x + W - 12, yy], fill=(120, 95, 60), width=2)
            d.rectangle([wx0 + 60, fy0 + 60, wx0 + 300, fy0 + 140], fill=(240, 240, 235)); d.text((wx0 + 180, fy0 + 90), "TO LET", font=ImageFont.truetype(BOLD, 34), fill=(200, 30, 30), anchor='mm'); d.text((wx0 + 180, fy0 + 124), "01632 960 115", font=ImageFont.truetype(REG, 16), fill=(30, 30, 30), anchor='mm')
        elif kind == "shutter":
            d.rectangle([x, y, x + W - 1, fy1], fill=(95, 98, 100))
            for yy in range(y, fy1, 9): d.line([x, yy, x + W, yy], fill=(70, 72, 74), width=2)
            for k in range(3):
                tx, ty = x + r.randint(20, 300), y + r.randint(40, 220); col = r.choice([(30, 120, 220), (230, 200, 30), (220, 40, 40), (250, 250, 250)])
                d.text((tx, ty), r.choice(["KCR", "TBL", "HWY", "WEND", "RIP"]), font=ImageFont.truetype(BOLD, r.randint(40, 70)), fill=col)
        elif kind == "whitewash":
            # the glass smeared white from inside, a faded board with the old name just showing
            for yy in range(fy0 + 10, fy1 - 4):
                for xx in range(wx0, x + W - 12, 6):
                    v = 200 + r.randint(-18, 18); d.line([xx, yy, xx + 5, yy], fill=(v, v, v - 4))
            f = fit(d, "SUNRISE CAFE", BOLD, 34, W - 60); d.text((x + W // 2, y + 34), "SUNRISE CAFE", font=f, fill=(170, 168, 160), anchor='mm')
            d.rectangle([wx0 + 70, fy0 + 70, wx0 + 270, fy0 + 120], fill=(250, 250, 250)); d.text((wx0 + 170, fy0 + 95), "CLOSED DOWN", font=ImageFont.truetype(BOLD, 22), fill=(200, 30, 30), anchor='mm')
        if kind not in ("boarded", "shutter", "whitewash"):
            d.rectangle([dx0 + 30, fy0 + 40, dx0 + 100, fy0 + 70], fill=(255, 255, 255)); d.text((dx0 + 65, fy0 + 55), "OPEN", font=ImageFont.truetype(BOLD, 16), fill=(20, 140, 40), anchor='mm')
    p = os.path.join(OUT, fname); img.save(p); print('wrote', p)


SIGNS = [  # (text, small text, background, text colour, font)
    ("WEND MILL", "1887", (196, 186, 160), (70, 60, 50), SERIF),
    ("WEND MILL APARTMENTS", "LUXURY CITY LIVING  1 & 2 BED  SALES OFFICE OPEN", (25, 25, 30), (230, 200, 120), BOLD),
    ("NORTHWAY HOUSE", "", (170, 170, 165), (35, 35, 38), BOLD),
    ("WEND BOROUGH COUNCIL", "CIVIC OFFICES  COUNCIL TAX  HOUSING BENEFIT", (240, 240, 235), (20, 60, 120), BOLD),
    ("MULTI STOREY CAR PARK", "OPEN 24 HOURS  PAY ON FOOT", (30, 90, 180), (255, 255, 255), BOLD),
    ("ENTRANCE", "MAX HEADROOM 2.1M", (250, 210, 0), (20, 20, 20), BOLD),
    ("KESTREL CHAMBERS", "1889", (200, 190, 170), (60, 50, 40), SERIF),
    ("WEND GOSPEL HALL", "ALL WELCOME  SUNDAY 10.30AM", (240, 238, 228), (30, 40, 90), SERIF),
    ("WEND SNOOKER CLUB", "MEMBERS & GUESTS  18 TABLES  BAR", (20, 70, 40), (250, 230, 160), BOLD),
    ("MILL STREET", "", (250, 250, 248), (20, 20, 20), BOLD),
    ("RESIDENTS PARKING ONLY", "CLAMPING IN OPERATION", (250, 250, 248), (200, 30, 30), BOLD),
    ("OFFICES TO LET", "01632 960 333", (230, 230, 225), (200, 30, 30), BOLD),
    ("EXIT", "", (30, 130, 60), (255, 255, 255), BOLD),
    ("NO BALL GAMES", "NO LOITERING", (250, 250, 248), (200, 30, 30), BOLD),
    ("THE WEND LOFTS", "PHASE 2 COMING SOON", (240, 236, 225), (40, 40, 40), SERIF),
    ("LEVEL 1  2  3  ROOF", "STAIRS  LIFT OUT OF ORDER", (30, 90, 180), (255, 255, 255), BOLD),
]


def signs():
    W, H = 1024, 170; img = Image.new('RGB', (W * 2, 2048), (230, 230, 230)); d = ImageDraw.Draw(img)
    for i, (t, sub, bg, fg, font) in enumerate(SIGNS):
        x, y = (i % 2) * W, (i // 2) * H
        d.rectangle([x, y, x + W - 1, y + H - 1], fill=bg)
        d.rectangle([x + 6, y + 6, x + W - 7, y + H - 7], outline=tuple(int(c * 0.75) for c in bg), width=4)
        if sub:
            f = fit(d, t, font, 84, W - 80); d.text((x + W // 2, y + 66), t, font=f, fill=fg, anchor='mm')
            f2 = fit(d, sub, REG, 30, W - 100); d.text((x + W // 2, y + 132), sub, font=f2, fill=fg, anchor='mm')
        else:
            f = fit(d, t, font, 100, W - 80); d.text((x + W // 2, y + H // 2), t, font=f, fill=fg, anchor='mm')
    p = os.path.join(OUT, 'street_signs.png'); img.save(p); print('wrote', p)


shopfronts(SHOPS, 2048, 'street_shopfronts.png', 11); shopfronts(SHOPS2, 1024, 'street_shopfronts2.png', 12); signs()

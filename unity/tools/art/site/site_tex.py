"""
Textures for the construction site (all generated here, own work, made-up names only):
  Site/heras.png        temporary mesh fence panel (3.5 m x 2 m), alpha cut-out
  Site/lattice.png      tower-crane lattice face (yellow tubes and diagonals), alpha cut-out, 2 m repeat
  Site/hoarding.png     developer hoarding board (made-up developer and scheme name)
  Site/safety.png       site safety board (keep out, hard hats, report to the office)
  Site/boards.png       scaffold boards (timber with banded ends)
Run: python3 site_tex.py <unity Assets folder>
"""
import sys, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont

OUT = os.path.join(sys.argv[1], 'Art', 'Site'); os.makedirs(OUT, exist_ok=True)
BOLD = '/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf'
REG = '/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf'

def heras():
    W, H = 896, 512  # 3.5 m x 2 m
    img = Image.new('RGBA', (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    steel = (178, 182, 186, 255)
    for x in range(14, W - 14, 13): d.line([x, 14, x, H - 14], fill=steel, width=2)
    for y in range(14, H - 14, 38): d.line([14, y, W - 14, y], fill=steel, width=2)
    d.rectangle([4, 4, W - 5, H - 5], outline=(190, 194, 198, 255), width=10)
    img.save(os.path.join(OUT, 'heras.png'))

def lattice():
    N = 256
    img = Image.new('RGBA', (N, N), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    y = (232, 186, 20, 255); dk = (190, 150, 15, 255)
    d.rectangle([0, 0, 14, N], fill=y); d.rectangle([N - 15, 0, N, N], fill=y)
    d.rectangle([0, 0, N, 8], fill=dk); d.rectangle([0, N // 2 - 4, N, N // 2 + 4], fill=dk)
    d.line([8, 8, N - 8, N // 2], fill=y, width=9); d.line([N - 8, N // 2, 8, N - 4], fill=y, width=9)
    img.save(os.path.join(OUT, 'lattice.png'))

def hoarding():
    W, H = 2048, 512   # one board 9.6 m x 2.4 m
    img = Image.new('RGB', (W, H), (24, 52, 78)); d = ImageDraw.Draw(img)
    d.rectangle([0, H - 70, W, H], fill=(240, 196, 40))
    f1 = ImageFont.truetype(BOLD, 150); f2 = ImageFont.truetype(REG, 70); f3 = ImageFont.truetype(BOLD, 46)
    d.text((90, 70), 'WEND VALLEY HOMES', font=f1, fill=(255, 255, 255))
    d.text((96, 255), 'Coming soon: 48 new homes at Hawthorn Rise', font=f2, fill=(220, 230, 238))
    d.text((96, 352), 'One, two and three bedroom apartments', font=f2, fill=(220, 230, 238))
    d.text((90, H - 60), 'SITE ENTRANCE   ->   ALL DELIVERIES REPORT TO THE SITE OFFICE', font=f3, fill=(24, 30, 36))
    # a simple house-roof mark
    d.polygon([(1760, 300), (1880, 170), (2000, 300)], fill=(240, 196, 40)); d.rectangle([1790, 300, 1970, 420], fill=(240, 196, 40))
    img.save(os.path.join(OUT, 'hoarding.png'))

def safety():
    W, H = 512, 768
    img = Image.new('RGB', (W, H), (250, 250, 248)); d = ImageDraw.Draw(img)
    fb = ImageFont.truetype(BOLD, 44); fs = ImageFont.truetype(BOLD, 34)
    rows = [((200, 20, 24), 'DANGER', 'CONSTRUCTION SITE\nKEEP OUT'), ((20, 90, 170), 'HARD HATS', 'MUST BE WORN'),
            ((20, 90, 170), 'HI-VIS', 'MUST BE WORN'), ((30, 130, 60), 'VISITORS', 'REPORT TO\nTHE SITE OFFICE')]
    y = 10
    for col, head, sub in rows:
        d.rectangle([10, y, W - 10, y + 60], fill=col); tw = d.textlength(head, font=fb); d.text(((W - tw) / 2, y + 6), head, font=fb, fill=(255, 255, 255))
        d.multiline_text((W / 2, y + 70), sub, font=fs, fill=(20, 20, 20), anchor='ma', align='center')
        y += 188
    d.rectangle([2, 2, W - 3, H - 3], outline=(30, 30, 30), width=6)
    img.save(os.path.join(OUT, 'safety.png'))

def boards():
    W, H = 512, 128   # 3.9 m board x 4 boards wide
    rng = np.random.default_rng(3)
    img = np.zeros((H, W, 3), np.float32)
    for b in range(4):
        y0, y1 = b * 32, b * 32 + 30
        base = np.array([150, 118, 78], np.float32) * rng.uniform(0.85, 1.1)
        grain = np.sin(np.linspace(0, 40, W)[None, :] + rng.uniform(0, 6) + np.linspace(0, 3, y1 - y0)[:, None]) * 8
        img[y0:y1] = base + grain[..., None] + rng.normal(0, 5, (y1 - y0, W, 1))
        img[y0:y1, :14] = (60, 60, 64); img[y0:y1, -14:] = (60, 60, 64)   # steel banded ends
    img = np.clip(img * 0.9, 0, 255).astype(np.uint8)
    Image.fromarray(img).save(os.path.join(OUT, 'boards.png'))

heras(); lattice(); hoarding(); safety(); boards(); print('site textures done')

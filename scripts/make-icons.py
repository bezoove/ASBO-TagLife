#!/usr/bin/env python3
"""Generate the original duck-on-dark launcher icons (needs Pillow). Run from repo root."""
from PIL import Image, ImageDraw
import os
BG = (18, 13, 9, 255)
RES = 'android/app/src/main/res'
S = 1024  # master canvas

def duck(size=S, with_bg=False, circle=False):
    im = Image.new('RGBA', (size, size), BG if with_bg else (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    k = size / 1024.0
    def E(x0, y0, x1, y1, **kw): d.ellipse([x0*k, y0*k, x1*k, y1*k], **kw)
    # ground glow ring
    E(250, 760, 774, 840, fill=(51, 34, 26, 255))
    # tail + body
    d.polygon([(250*k, 560*k), (170*k, 470*k), (330*k, 520*k)], fill=(240, 207, 122, 255))
    E(240, 440, 760, 790, fill=(244, 206, 70, 255))
    # wing
    E(330, 560, 590, 730, fill=(212, 164, 60, 255))
    # head
    E(520, 250, 780, 510, fill=(244, 206, 70, 255))
    # beak
    d.polygon([(760*k, 360*k), (900*k, 395*k), (760*k, 440*k)], fill=(232, 120, 40, 255))
    # eye
    E(655, 330, 695, 370, fill=(18, 13, 9, 255))
    E(667, 337, 679, 349, fill=(255, 255, 255, 255))
    # tag-style brass band on neck
    d.rounded_rectangle([540*k, 470*k, 720*k, 505*k], radius=14*k, fill=(200, 67, 47, 255))
    return im

def write(im, path, px):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    im.resize((px, px), Image.LANCZOS).save(path)

dens = {'mdpi': 1, 'hdpi': 1.5, 'xhdpi': 2, 'xxhdpi': 3, 'xxxhdpi': 4}
full = duck(with_bg=True)
# legacy square icon: duck fills most of the canvas, rounded corners
sq = Image.new('RGBA', (S, S), (0, 0, 0, 0))
m = Image.new('L', (S, S), 0); ImageDraw.Draw(m).rounded_rectangle([0, 0, S-1, S-1], radius=180, fill=255)
sq.paste(full, (0, 0), m)
rd = Image.new('RGBA', (S, S), (0, 0, 0, 0))
m2 = Image.new('L', (S, S), 0); ImageDraw.Draw(m2).ellipse([0, 0, S-1, S-1], fill=255)
rd.paste(full, (0, 0), m2)
# adaptive foreground: 108dp canvas, safe zone is the central 66dp -> scale duck to 62%
fg = Image.new('RGBA', (S, S), (0, 0, 0, 0))
small = duck().resize((int(S*0.62), int(S*0.62)), Image.LANCZOS)
fg.paste(small, ((S-small.width)//2, (S-small.height)//2), small)
for name, f in dens.items():
    write(sq, f'{RES}/mipmap-{name}/ic_launcher.png', round(48*f))
    write(rd, f'{RES}/mipmap-{name}/ic_launcher_round.png', round(48*f))
    write(fg, f'{RES}/mipmap-{name}/ic_launcher_foreground.png', round(108*f))
with open(f'{RES}/values/ic_launcher_background.xml', 'w') as fh:
    fh.write('<?xml version="1.0" encoding="utf-8"?>\n<resources>\n    <color name="ic_launcher_background">#120D09</color>\n</resources>\n')
full.resize((512, 512), Image.LANCZOS).save('icon-preview.png')
print('icons written')

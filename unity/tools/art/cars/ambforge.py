# The UK box ambulance (from the user's photos): a high-roof van cab with a box body over it, fluorescent yellow,
# two rows of green/yellow Battenburg squares, a thin chequer band at the top, EMERGENCY AMBULANCE along the box,
# AMBULANCE mirror-written on the front, blue light bar and beacons, rear doors that open onto a white treatment room
# with cabinets, a seat and kit. Exported for Unity with the doors, lamps and wheels as separate parts.
import bpy, bmesh, sys, os, math, json
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import carforge
from carforge import Kit, Car, mat
from mathutils import Vector, Matrix
from PIL import Image, ImageDraw, ImageFont, ImageFilter

OUT = sys.argv[sys.argv.index('--') + 1]
PREVIEW = '--preview' in sys.argv
os.makedirs(OUT, exist_ok=True)
FONT = '/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf'
YEL = (0.86, 0.93, 0.06); GRN = (0.0, 0.42, 0.19)

P = dict(L=6.9, W=2.1, wb=4.035, fo=0.95, rad=0.36, tyre_w=0.225, sill=0.44, belt=1.38, belt_rise=0.0, arch_gap=0.05,
         top=[(0.0, 0.98), (0.07, 1.06), (0.85, 1.25), (1.8, 2.27), (1.92, 2.42), (2.12, 2.93), (2.3, 2.96), (6.84, 2.96), (6.9, 2.9)],
         ws=(0.85, 1.8), rw=(99.0, 99.5), rubber_end=2.32, pillars=[(0.0, 99.0)], tumble=0.03, cornerF=0.3, cornerR=0.06, crown=0.015, bulge=0.0,
         bumpF_bot=0.32, bumpF_top=0.66, bumpR_bot=0.44, bumpR_top=0.6, bumper='black', bumpF_out=0.05, bumpR_out=0.06, wrap=0.4,
         head=dict(w=0.36, h=0.17, z=0.9, inner=0.42), grille=dict(h=0.2, z=0.84, slats=4), ind='outer',
         tail=dict(w=0.06, h=0.3, z=0.9), door_lines=[], handles=[], wheel='alloy5', seat_u=1.45, no_mirrors=True)

def text_image(txt, h_px, colour, mirror=False, pad=10):
    f = ImageFont.truetype(FONT, h_px)
    w = int(f.getlength(txt)) + pad * 2
    im = Image.new('RGBA', (w, h_px + pad * 2), (0, 0, 0, 0))
    ImageDraw.Draw(im).text((pad, pad - int(h_px * 0.12)), txt, font=f, fill=colour + (255,))
    if mirror: im = im.transpose(Image.FLIP_LEFT_RIGHT)
    return im

def make_images(folder):
    side = text_image('EMERGENCY AMBULANCE', 120, (0, 105, 48)); side.save(f'{folder}/amb_text_side.png')
    front = text_image('AMBULANCE', 120, (0, 105, 48), mirror=True); front.save(f'{folder}/amb_text_front.png')
    # rear doors: red and yellow chevrons on the bottom half, yellow above (the window is its own glass)
    W, H = 256, 512; px = np.zeros((H, W, 3), np.uint8); px[:] = (219, 237, 16)
    for y in range(H // 2, H):
        for x in range(W):
            u = x / W; k = ((H - y) / W + abs(u - 0.5) * 1.0) * 3.4
            px[y, x] = (205, 24, 20) if int(math.floor(k)) % 2 == 0 else (250, 214, 10)
    Image.fromarray(px).save(f'{folder}/amb_door.png')
    return side.size, front.size

def livery_bake(ob, car, folder, side_size, front_size):
    """Bake the livery into the body's own texture: base yellow, Battenburg, chequer band, white roof, lettering, shading."""
    sc = bpy.context.scene; sc.render.engine = 'CYCLES'; sc.cycles.samples = 32; sc.cycles.device = 'CPU'
    L = car.L
    m = bpy.data.materials.new('livbake'); m.use_nodes = True; nt = m.node_tree; N = nt.nodes; Lk = nt.links
    for n in list(N): N.remove(n)
    out = N.new('ShaderNodeOutputMaterial'); em = N.new('ShaderNodeEmission'); Lk.new(em.outputs[0], out.inputs['Surface'])
    tc = N.new('ShaderNodeTexCoord'); geo = N.new('ShaderNodeNewGeometry')
    sep = N.new('ShaderNodeSeparateXYZ'); Lk.new(tc.outputs['Object'], sep.inputs[0])
    nsep = N.new('ShaderNodeSeparateXYZ'); Lk.new(geo.outputs['Normal'], nsep.inputs[0])
    def math_(op, a, b=None, clamp=False):
        n = N.new('ShaderNodeMath'); n.operation = op; n.use_clamp = clamp
        for i, v in enumerate([a, b]):
            if v is None: continue
            if isinstance(v, (int, float)): n.inputs[i].default_value = v
            else: Lk.new(v, n.inputs[i])
        return n.outputs[0]
    def mix(fac, a, b):
        n = N.new('ShaderNodeMix'); n.data_type = 'RGBA'
        Lk.new(fac, n.inputs['Factor']) if not isinstance(fac, float) else None
        for sock, v in ((n.inputs[6], a), (n.inputs[7], b)):
            if isinstance(v, tuple): sock.default_value = v + (1,) if len(v) == 3 else v
            else: Lk.new(v, sock)
        return n.outputs[2]
    X, Y, Z = sep.outputs[0], sep.outputs[1], sep.outputs[2]
    NY, NZ, NX = nsep.outputs[1], nsep.outputs[2], nsep.outputs[0]
    u = math_('SUBTRACT', L / 2, X)                     # distance back from the front
    side = math_('GREATER_THAN', math_('ABSOLUTE', NY), 0.6)
    def between(v, a, b): return math_('MULTIPLY', math_('GREATER_THAN', v, a), math_('LESS_THAN', v, b))
    # Battenburg: two rows of squares behind the front wheel
    sq = 0.335; z0 = 0.62
    row = math_('FLOOR', math_('DIVIDE', math_('SUBTRACT', Z, z0), sq))
    colm = math_('FLOOR', math_('DIVIDE', u, sq))
    chk = math_('MODULO', math_('ADD', math_('ADD', row, colm), 100.0), 2.0)
    bat = math_('MULTIPLY', math_('MULTIPLY', between(Z, z0, z0 + 2 * sq), between(u, car.uF + P['rad'] + 0.1, L - 0.04)), side)
    green_b = math_('MULTIPLY', bat, math_('LESS_THAN', chk, 0.5))
    # thin chequer band near the top of the box
    sq2 = 0.15; zb = 2.6
    chk2 = math_('MODULO', math_('ADD', math_('ADD', math_('FLOOR', math_('DIVIDE', math_('SUBTRACT', Z, zb), sq2)), math_('FLOOR', math_('DIVIDE', u, sq2))), 100.0), 2.0)
    band = math_('MULTIPLY', math_('MULTIPLY', between(Z, zb, zb + sq2), between(u, 2.25, L - 0.04)), side)
    green_t = math_('MULTIPLY', band, math_('LESS_THAN', chk2, 0.5))
    col = mix(math_('MAXIMUM', green_b, green_t), YEL, GRN)
    # white roof
    roof = math_('GREATER_THAN', NZ, 0.6); roofz = math_('GREATER_THAN', Z, 2.85)
    col = mix(math_('MULTIPLY', roof, roofz), col, (0.9, 0.9, 0.88))
    # EMERGENCY AMBULANCE on both sides of the box (reads front-to-back on the left, back-to-front on the right)
    def text_layer(col, img_path, size, ua, za, height, mirror_side, front=False):
        img = bpy.data.images.load(img_path); img.alpha_mode = 'STRAIGHT'
        tex = N.new('ShaderNodeTexImage'); tex.image = img; tex.extension = 'CLIP'
        w = height * size[0] / size[1]
        if not front:
            # u coordinate along the text: left side runs with u, right side against it
            left = math_('GREATER_THAN', NY, 0.0)
            tu_l = math_('DIVIDE', math_('SUBTRACT', u, ua), w)
            tu_r = math_('SUBTRACT', 1.0, tu_l)
            tu = math_('ADD', math_('MULTIPLY', left, tu_l), math_('MULTIPLY', math_('SUBTRACT', 1.0, left), tu_r))
            gate = side
        else:
            tu = math_('DIVIDE', math_('SUBTRACT', Y, -w / 2), w)  # the image is already mirror-written
            gate = math_('GREATER_THAN', NX, 0.3)
        tv = math_('DIVIDE', math_('SUBTRACT', Z, za), height)
        cmb = N.new('ShaderNodeCombineXYZ'); Lk.new(tu, cmb.inputs[0]); Lk.new(tv, cmb.inputs[1]); Lk.new(cmb.outputs[0], tex.inputs['Vector'])
        a = math_('MULTIPLY', tex.outputs['Alpha'], gate)
        return mix(a, col, tex.outputs['Color'])
    text_h = 0.3; text_w = text_h * side_size[0] / side_size[1]; ua = 2.35 + (L - 2.35 - text_w) / 2
    col = text_layer(col, f'{folder}/amb_text_side.png', side_size, ua, 2.12, text_h, True)
    col = text_layer(col, f'{folder}/amb_text_front.png', front_size, 0, 2.5, 0.22, False, front=True)
    # ambient occlusion for shading
    ao = N.new('ShaderNodeAmbientOcclusion'); ao.inputs['Distance'].default_value = 0.5; ao.samples = 16
    shade = math_('ADD', 0.45, math_('MULTIPLY', ao.outputs['AO'], 0.55))
    fin = N.new('ShaderNodeMix'); fin.data_type = 'RGBA'; fin.blend_type = 'MULTIPLY'; fin.inputs['Factor'].default_value = 1.0
    Lk.new(col, fin.inputs[6]); sh_rgb = N.new('ShaderNodeCombineColor'); Lk.new(shade, sh_rgb.inputs[0]); Lk.new(shade, sh_rgb.inputs[1]); Lk.new(shade, sh_rgb.inputs[2])
    Lk.new(sh_rgb.outputs[0], fin.inputs[7]); Lk.new(fin.outputs[2], em.inputs['Color'])
    TEX = 1024
    img = bpy.data.images.new('amb_livery', TEX, TEX, alpha=False)
    tn = N.new('ShaderNodeTexImage'); tn.image = img; N.active = tn
    dup = ob.copy(); dup.data = ob.data.copy(); sc.collection.objects.link(dup)
    bm = bmesh.new(); bm.from_mesh(dup.data)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.material_index != 0], context='FACES'); bm.to_mesh(dup.data); bm.free()
    dup.data.materials.clear(); dup.data.materials.append(m)
    for o in sc.objects: o.select_set(False)
    dup.select_set(True); bpy.context.view_layer.objects.active = dup
    with bpy.context.temp_override(active_object=dup, object=dup, selected_objects=[dup], selected_editable_objects=[dup]):
        bpy.ops.object.bake(type='EMIT', margin=8)
    px = np.array(img.pixels[:], np.float32).reshape(TEX, TEX, 4)[..., :3]
    Image.fromarray((np.flipud(np.clip(px, 0, 1)) * 255 + 0.5).astype(np.uint8)).save(f'{folder}/amb_livery.png')
    bpy.data.objects.remove(dup)

def build():
    for o in list(bpy.data.objects): bpy.data.objects.remove(o)
    carforge.MATS.clear()
    for mm in list(bpy.data.materials): bpy.data.materials.remove(mm)
    M = carforge.materials(YEL)
    M['white'] = mat('white', (0.88, 0.89, 0.9), 0.4); M['floor'] = mat('floor', (0.42, 0.43, 0.45), 0.7)
    M['blue'] = mat('blue', (0.05, 0.12, 0.6), 0.1); M['seat'] = mat('seat', (0.04, 0.045, 0.06), 0.6)
    M['door'] = mat('door', (1, 1, 1), 0.4); M['doorin'] = mat('doorin', (0.55, 0.56, 0.58), 0.5)
    M['bag'] = mat('bag', (0.08, 0.22, 0.6), 0.4); M['bag2'] = mat('bag2', (0.06, 0.36, 0.2), 0.4)
    car = Car(P); L = car.L; hw = car.hw0
    body = car.body(M)
    carforge.cut_arches(car, body, M)
    # the door opening at the back
    bpy.ops.mesh.primitive_cube_add(size=1, location=(-L / 2, 0, 0.5 + (2.72 - 0.5) / 2))
    cut = bpy.context.active_object; cut.scale = (0.14, (hw - 0.07) * 2, 2.72 - 0.5)
    mod = body.modifiers.new('rear', 'BOOLEAN'); mod.operation = 'DIFFERENCE'; mod.object = cut; mod.solver = 'EXACT'
    bpy.context.view_layer.objects.active = body
    with bpy.context.temp_override(object=body, active_object=body): bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cut)
    # the cut leaves a shallow recess: take out its back wall so the opening goes through into the room
    bm = bmesh.new(); bm.from_mesh(body.data)
    back = [f for f in bm.faces if abs(f.calc_center_median().x - (-L / 2 + 0.07)) < 0.012 and abs(f.calc_center_median().y) < hw - 0.06 and 0.5 < f.calc_center_median().z < 2.72]
    bmesh.ops.delete(bm, geom=back, context='FACES'); bm.to_mesh(body.data); bm.free()
    for p in body.data.polygons: p.use_smooth = True
    carforge.set_auto_smooth(body, 32)
    wells = carforge.wheel_wells(car, M)
    det = carforge.details(car, M)
    k = Kit()
    xr = -L / 2 + 0.06; xf = L / 2 - 2.32  # treatment room from the back doors to the bulkhead behind the cab
    yi = hw - 0.07; zf, zc = 0.56, 2.74
    # room shell facing inwards
    c = [(xr, -yi, zf), (xr, yi, zf), (xf, yi, zf), (xf, -yi, zf), (xr, -yi, zc), (xr, yi, zc), (xf, yi, zc), (xf, -yi, zc)]
    cv = [k.bm.verts.new(p) for p in c]; shell = []
    for q, mname in [((0, 1, 2, 3), 'floor'), ((4, 7, 6, 5), 'white'), ((1, 5, 6, 2), 'white'), ((0, 3, 7, 4), 'white'), ((3, 2, 6, 7), 'white')]:
        f = k.bm.faces.new([cv[i] for i in q]); f.material_index = k.mi(M[mname]); shell.append(f)
        # and the same face turned round, set in a hair: the room is solid from inside whichever way it was wound
        # (one side of the room was see-through when you looked in through the open back doors)
        ctr = Vector((sum(c[i][0] for i in q) / 4, sum(c[i][1] for i in q) / 4, sum(c[i][2] for i in q) / 4))
        inset = [k.bm.verts.new(Vector(c[i]) + (Vector(((xr + xf) / 2, 0, (zf + zc) / 2)) - ctr).normalized() * 0.004) for i in reversed(q)]
        f2 = k.bm.faces.new(inset); f2.material_index = k.mi(M[mname]); shell.append(f2)
    # (the back of the room stays open: it's the door opening)
    # cabinets down the right-hand wall, a bench on the left, the attendant's seat against the bulkhead, lights in the ceiling
    for i in range(4):
        x0 = xr + 0.5 + i * 0.75
        k.box((x0 + 0.33, -yi + 0.2, zf + 0.55), (0.62, 0.36, 1.1), M['white'])
        k.box((x0 + 0.33, -yi + 0.17, zc - 0.32), (0.62, 0.3, 0.5), M['white'])
        k.box((x0 + 0.33, -yi + 0.385, zf + 0.85), (0.5, 0.01, 0.02), M['chrome'])
    k.box(((xr + xf) / 2 + 0.3, yi - 0.22, zf + 0.42), (2.0, 0.42, 0.08), M['seat'])
    k.box(((xr + xf) / 2 + 0.3, yi - 0.04, zf + 0.85), (2.0, 0.06, 0.6), M['seat'])
    for i, mname in enumerate(['bag', 'bag2', 'bag']):
        k.box((xr + 0.9 + i * 0.55, yi - 0.24, zf + 0.62), (0.45, 0.3, 0.28), M[mname])
    k.box((xf + 0.3, 0, zf + 0.45), (0.5, 0.55, 0.1), M['seat']); k.box((xf + 0.08, 0, zf + 0.95), (0.1, 0.5, 0.75), M['seat'])
    k.box((xf + 0.08, 0, zf + 1.42), (0.1, 0.3, 0.2), M['seat'])
    for i in range(3):
        k.box((xr + 0.7 + i * 1.1, 0, zc - 0.01), (0.6, 0.12, 0.012), M['lens'])
    k.box((xr + 0.9, -0.2, zf + 0.05), (2.6, 0.08, 0.05), M['chrome']); k.box((xr + 0.9, 0.2, zf + 0.05), (2.6, 0.08, 0.05), M['chrome'])  # stretcher rails
    # the frame round the door opening and the rear step
    k.box((-L / 2 + 0.02, 0, 0.48), (0.06, hw * 1.9, 0.04), M['chrome'])
    k.box((-L / 2 - 0.12, 0, 0.42), (0.24, hw * 1.5, 0.05), M['chrome'])
    # side windows: the cab door glass follows the screen pillar (its front edge raked back, the corners rounded, a
    # quarter-light at the front); the box has no big square windows, just a small frosted one in the nearside door
    def rounded(pts, r, n=4):
        out = []
        for i in range(len(pts)):
            p0, p1, p2 = Vector(pts[i - 1]), Vector(pts[i]), Vector(pts[(i + 1) % len(pts)])
            a = (p0 - p1).normalized(); b = (p2 - p1).normalized(); rr = min(r, (p0 - p1).length * 0.45, (p2 - p1).length * 0.45)
            s0, s1 = p1 + a * rr, p1 + b * rr
            for j in range(n + 1):
                t = j / n; q = s0 * (1 - t) ** 2 + p1 * 2 * t * (1 - t) + s1 * t * t; out.append(tuple(q))
        return out
    for s in (-1, 1):
        hwx = car.half(1.5) + 0.004
        # (u along from the front, z up): bottom front, bottom back, top back, top front (raked)
        win = [(1.08, 1.4), (1.84, 1.4), (1.84, 2.14), (1.42, 2.14)]
        pts = rounded([(L / 2 - u, s * hwx, z) for (u, z) in win], 0.07)
        k.face(pts, M['glass'], flip=s < 0)
        k.face([(x + (0.0), y - s * 0.001, z) for (x, y, z) in rounded([(L / 2 - u, s * hwx, z) for (u, z) in [(1.05, 1.37), (1.87, 1.37), (1.87, 2.17), (1.4, 2.17)]], 0.09)], M['trim'], flip=s < 0)
        k.box((L / 2 - 1.3, s * (hwx + 0.002), 1.77), (0.02, 0.01, 0.74), M['trim'])  # the quarter-light divider
        if s > 0:  # nearside sliding door: its outline, a frosted window, the handle, a step and a grab rail
            hb = car.half(3.0) + 0.006; ud0, ud1 = 2.55, 3.55
            for (u, z, du, dz) in ((ud0, 1.55, 0.012, 2.05), (ud1, 1.55, 0.012, 2.05), ((ud0 + ud1) / 2, 2.58, ud1 - ud0, 0.012), ((ud0 + ud1) / 2, 0.52, ud1 - ud0, 0.012)):
                k.box((L / 2 - u, s * hb, z), (du, 0.01, dz), M['trim'])
            k.face(rounded([(L / 2 - u, s * (hb + 0.002), z) for (u, z) in [(2.75, 1.85), (3.35, 1.85), (3.35, 2.35), (2.75, 2.35)]], 0.06), M['white'], flip=s < 0)
            k.box((L / 2 - 3.42, s * (hb + 0.02), 1.3), (0.04, 0.03, 0.2), M['trim'])
            k.box((L / 2 - 3.05, s * (hb + 0.12), 0.42), (1.0, 0.24, 0.04), M['chrome'])  # the step
            k.box((L / 2 - 2.5, s * (hb + 0.05), 1.6), (0.03, 0.03, 0.9), M['chrome'])   # a grab rail by the door
        # a black rubbing strip along the box, amber marker lamps along the top edge, mud flaps behind the wheels
        hb2 = car.half(4.5) + 0.008
        k.box((-0.3, s * hb2, 0.98), (L - 2.9, 0.02, 0.07), M['trim'])
        for u in (2.6, 3.8, 5.0, 6.2): k.box((L / 2 - u, s * (hb2 + 0.004), 2.9), (0.07, 0.01, 0.035), M['amber'])
        for u in (car.uF + 0.5, car.uR + 0.5): k.box((L / 2 - u, s * (car.half(u) - 0.12), 0.3), (0.02, 0.24, 0.32), M['trim'])
        # big van mirrors on arms
        u = 0.95; x = L / 2 - u
        k.box((x - 0.05, s * (hw + 0.22), 1.75), (0.08, 0.2, 0.32), M['trim'])
        k.box((x - 0.02, s * (hw + 0.08), 1.6), (0.03, 0.18, 0.03), M['trim'])
        # door handles and the side door's outline on the box
        k.box((L / 2 - 1.75, s * (hw + 0.008), 1.3), (0.14, 0.02, 0.03), M['trim'])
        k.box((L / 2 - 2.5, s * (hw + 0.004), 1.5), (0.01, 0.01, 2.0), M['trim']) if s < 0 else None
        # scene lamps high on the box sides
        k.box((-L / 2 + 1.2, s * (hw + 0.012), 2.82), (0.32, 0.02, 0.08), M['lens'])
        k.box((L / 2 - 2.6, s * (hw + 0.012), 2.82), (0.32, 0.02, 0.08), M['lens'])
        # tall red tail lamps on the back corners
        k.box((-L / 2 + 0.01, s * (hw - 0.03), 1.25), (0.04, 0.1, 0.6), M['red'])
    # on the roof: the air-con pod over the treatment room, two aerials and the GPS puck
    k.box((-L / 2 + 1.9, 0, 3.02), (1.1, 0.8, 0.16), M['white']); k.box((-L / 2 + 1.9, 0, 3.11), (0.9, 0.6, 0.03), M['trim'])
    for (u, y) in ((2.6, 0.5), (2.9, -0.5)): k.box((L / 2 - u, y, 3.2), (0.015, 0.015, 0.45), M['trim'])
    k.box((L / 2 - 3.3, 0, 2.99), (0.12, 0.12, 0.04), M['trim'])
    # black lower panels on the cab
    for s in (-1, 1):
        k.box((L / 2 - 0.55, s * (car.half(0.55) + 0.002), 0.55), (0.9, 0.012, 0.22), M['trim'])
    inner = k.build('room', recalc=False)
    # lamps that flash: bar halves at the top front of the box, beacons at the back corners, blues in the grille
    parts = {}
    def lamp(name, c, s):
        kk = Kit(); kk.box(c, s, M['blue']); ob = kk.build(name); parts[name] = ob
    zb = car.ztop(2.3) + 0.06; xb = L / 2 - 2.05
    base = Kit(); base.box((xb, 0, zb - 0.03), (0.3, 1.5, 0.05), M['white']); parts['bar_base'] = base.build('bar_base')
    lamp('bar_L', (xb, 0.4, zb + 0.04), (0.26, 0.62, 0.1)); lamp('bar_R', (xb, -0.4, zb + 0.04), (0.26, 0.62, 0.1))
    lamp('beacon_L', (-L / 2 + 0.12, hw - 0.12, 2.98), (0.18, 0.16, 0.12)); lamp('beacon_R', (-L / 2 + 0.12, -(hw - 0.12), 2.98), (0.18, 0.16, 0.12))
    lamp('grille_L', (L / 2 + 0.005, 0.32, 0.8), (0.03, 0.16, 0.05)); lamp('grille_R', (L / 2 + 0.005, -0.32, 0.8), (0.03, 0.16, 0.05))
    # rear doors, hinged at the outside edges
    doors = {}
    for s, name in ((1, 'door_L'), (-1, 'door_R')):
        kk = Kit(); w = hw - 0.06; h0, h1 = 0.5, 2.72; t = 0.05
        # outer face with the door texture (UV 0..1), inner face grey, edges dark
        x_out, x_in = -L / 2 - 0.035, -L / 2 - 0.035 + t
        yh = s * (hw - 0.04); yc = s * 0.005
        f = kk.face([(x_out, yh, h0), (x_out, yc, h0), (x_out, yc, h1), (x_out, yh, h1)], M['door'], flip=s < 0)
        f2 = kk.face([(x_in, yh, h0), (x_in, yc, h0), (x_in, yc, h1), (x_in, yh, h1)], M['doorin'], flip=s > 0)
        for (a, b) in (((x_out, yh, h0), (x_in, yh, h1)),):
            pass
        kk.box(((x_out + x_in) / 2, (yh + yc) / 2, h1), (t, abs(yh - yc), 0.02), M['trim'])
        kk.box(((x_out + x_in) / 2, (yh + yc) / 2, h0), (t, abs(yh - yc), 0.02), M['trim'])
        kk.box(((x_out + x_in) / 2, yc, (h0 + h1) / 2), (t, 0.02, h1 - h0), M['trim'])
        # window in the top half
        kk.face([(x_out - 0.004, yh - s * 0.12, 1.75), (x_out - 0.004, yc + s * 0.12, 1.75), (x_out - 0.004, yc + s * 0.12, 2.45), (x_out - 0.004, yh - s * 0.12, 2.45)], M['glass'], flip=s < 0)
        kk.box((x_out - 0.02, yc + s * 0.2, 1.35), (0.03, 0.12, 0.03), M['trim'])  # handle
        ob = kk.build(name, recalc=False)
        me = ob.data
        if 'UVMap' not in me.uv_layers: me.uv_layers.new(name='UVMap')
        uv = me.uv_layers['UVMap'].data
        names = [mm.name for mm in me.materials]
        for p in me.polygons:
            if names[p.material_index] != 'door': continue
            for li in p.loop_indices:
                co = me.vertices[me.loops[li].vertex_index].co
                uu = (co.y - min(yh, yc)) / abs(yh - yc); vv = (co.z - h0) / (h1 - h0)
                uv[li].uv = (uu if s < 0 else 1 - uu, vv)
        # pivot on the hinge
        piv = Vector((x_out, yh, 0))
        me.transform(Matrix.Translation(-piv)); ob.location = piv
        doors[name] = ob
    wheels = []
    for name, u, s in (('wheel_FL', car.uF, 1), ('wheel_FR', car.uF, -1), ('wheel_RL', car.uR, 1), ('wheel_RR', car.uR, -1)):
        w = carforge.wheel(P, M, name)
        w.location = (L / 2 - u, s * (hw - P['tyre_w'] / 2 + 0.01), P['rad'])
        if s < 0: w.rotation_euler = (0, 0, math.pi)
        wheels.append(w)
    return car, M, body, [wells, det, inner], parts, doors, wheels

MAP = {'paint': 'X_livery', 'trim': 'X_dark', 'under': 'X_dark', 'interior': 'X_dark', 'seat': 'X_dark', 'tyre': 'X_dark',
       'chrome': 'X_chrome', 'glass': 'X_glass', 'lens': 'X_lamps', 'lamp_red': 'X_lamps', 'lamp_amber': 'X_lamps', 'plate': 'X_plate',
       'white': 'X_white', 'floor': 'X_floor', 'bag': 'X_bag', 'bag2': 'X_bag2', 'blue': 'X_blue', 'door': 'X_door', 'doorin': 'X_doorin', 'rim': 'X_dark'}
SW = {'lens': 0.125, 'lamp_red': 0.375, 'lamp_amber': 0.625}

def remap(ob):
    me = ob.data; names = [m.name if m else 'trim' for m in me.materials]
    idx = np.zeros(len(me.polygons), np.int32); me.polygons.foreach_get('material_index', idx)
    src = [names[i] for i in idx]
    if 'UVMap' not in me.uv_layers: me.uv_layers.new(name='UVMap')
    uv = me.uv_layers['UVMap'].data
    for p in me.polygons:
        n = src[p.index]
        if n in SW:
            for li in p.loop_indices: uv[li].uv = (SW[n], 0.5)
        elif n == 'plate':
            co = [me.vertices[me.loops[li].vertex_index].co for li in p.loop_indices]
            ys = [c.y for c in co]; zs = [c.z for c in co]; front = co[0].x > 0
            for li, c in zip(p.loop_indices, co):
                uu = (max(ys) - c.y) / max(1e-4, max(ys) - min(ys)) if front else (c.y - min(ys)) / max(1e-4, max(ys) - min(ys))
                uv[li].uv = (uu * 0.5 + (0 if front else 0.5), (c.z - min(zs)) / max(1e-4, max(zs) - min(zs)))
    order = []
    for n in src:
        t = MAP.get(n, 'X_dark')
        if t not in order: order.append(t)
    new = np.array([order.index(MAP.get(n, 'X_dark')) for n in src], np.int32)
    me.materials.clear()
    for t in order: me.materials.append(mat(t, (0.5, 0.5, 0.5)))
    me.polygons.foreach_set('material_index', new)

def uv_paint(ob):
    me = ob.data
    if 'UVMap' not in me.uv_layers: me.uv_layers.new(name='UVMap')
    for o in bpy.context.scene.objects: o.select_set(False)
    ob.select_set(True); bpy.context.view_layer.objects.active = ob
    with bpy.context.temp_override(active_object=ob, object=ob, selected_editable_objects=[ob], selected_objects=[ob]):
        bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='DESELECT')
        ob.active_material_index = 0; bpy.ops.object.material_slot_select()
        bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.01, area_weight=0.0, scale_to_bounds=True)
        bpy.ops.object.mode_set(mode='OBJECT')

def join(obs, name):
    for o in bpy.context.scene.objects: o.select_set(False)
    for o in obs: o.select_set(True)
    bpy.context.view_layer.objects.active = obs[0]
    with bpy.context.temp_override(active_object=obs[0], selected_editable_objects=obs, selected_objects=obs): bpy.ops.object.join()
    obs[0].name = name; obs[0].data.name = name; return obs[0]

def wheel_swatch(ob):
    me = ob.data
    if 'UVMap' not in me.uv_layers: me.uv_layers.new(name='UVMap')
    uv = me.uv_layers['UVMap'].data; names = [m.name for m in me.materials]
    for p in me.polygons:
        n = names[p.material_index]; u = 0.125 if n in ('tyre', 'interior', 'under', 'trim') else 0.375
        for li in p.loop_indices: uv[li].uv = (u, 0.5)
    me.materials.clear(); me.materials.append(mat('X_wheel', (0.5, 0.5, 0.5)))
    for p in me.polygons: p.material_index = 0

if __name__ == '__main__':
    car, M, body, extra, parts, doors, wheels = build()
    if PREVIEW:
        bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, 'amb_preview.blend'))
        print('PREVIEW SAVED'); sys.exit(0)
    side_size, front_size = make_images(OUT)
    uv_paint(body)
    livery_bake(body, car, OUT, side_size, front_size)
    allb = join([body] + extra, 'amb_body')
    remap(allb)
    for ob in list(parts.values()) + list(doors.values()): remap(ob)
    for w in wheels: wheel_swatch(w)
    R = Matrix.Rotation(math.radians(-90), 4, 'Z')
    allb.data.transform(R); allb.matrix_world = Matrix.Identity(4)
    for ob in list(parts.values()):
        ob.matrix_world = R @ ob.matrix_world
        for o in bpy.context.scene.objects: o.select_set(False)
        ob.select_set(True); bpy.context.view_layer.objects.active = ob
        with bpy.context.temp_override(active_object=ob, selected_editable_objects=[ob], selected_objects=[ob]):
            bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for ob in list(doors.values()) + wheels:
        ob.matrix_world = R @ ob.matrix_world
        for o in bpy.context.scene.objects: o.select_set(False)
        ob.select_set(True); bpy.context.view_layer.objects.active = ob
        with bpy.context.temp_override(active_object=ob, selected_editable_objects=[ob], selected_objects=[ob]):
            bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    keep = [allb] + list(parts.values()) + list(doors.values()) + wheels
    for o in bpy.context.scene.objects: o.select_set(o in keep)
    bpy.context.view_layer.objects.active = allb
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, 'ambulance.fbx'), use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y',
                             apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
                             mesh_smooth_type='FACE', use_tspace=False, path_mode='STRIP', embed_textures=False, add_leaf_bones=False)
    tri = lambda o: sum(len(p.vertices) - 2 for p in o.data.polygons)
    json.dump(dict(L=P['L'], W=P['W'], roof=2.96, wb=P['wb'], fo=P['fo'], rad=P['rad'], tris=tri(allb)), open(os.path.join(OUT, 'ambulance.json'), 'w'))
    print('AMB DONE tris', tri(allb), [o.name for o in keep])

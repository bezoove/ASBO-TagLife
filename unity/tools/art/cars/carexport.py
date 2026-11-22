# Export each car for Unity: one merged mesh per level of detail (6 materials), a battered version with dents,
# four wheels, and a wear mask texture (R dirt, G rust, B ambient occlusion, A panel id) for paint at runtime.
import bpy, bmesh, sys, os, math, json, random
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import carforge, models
from mathutils import Vector, Matrix, kdtree
from PIL import Image

OUT = sys.argv[sys.argv.index('--') + 1]
ONLY = sys.argv[sys.argv.index('--') + 2].split(',') if len(sys.argv) > sys.argv.index('--') + 2 else None
os.makedirs(OUT, exist_ok=True)
TEX = 512
SW = {'lens': 0.125, 'red': 0.375, 'amber': 0.625, 'dark': 0.875}           # lamp atlas swatches (u)
WS = {'tyre': 0.125, 'silver': 0.375, 'white': 0.625, 'gun': 0.875}         # wheel atlas swatches (u)

def objs_join(obs, name):
    for o in bpy.context.scene.objects: o.select_set(False)
    for o in obs: o.select_set(True)
    bpy.context.view_layer.objects.active = obs[0]
    with bpy.context.temp_override(active_object=obs[0], selected_editable_objects=obs, selected_objects=obs):
        bpy.ops.object.join()
    obs[0].name = name; obs[0].data.name = name
    return obs[0]

def apply_xf(ob):
    for o in bpy.context.scene.objects: o.select_set(False)
    ob.select_set(True); bpy.context.view_layer.objects.active = ob
    with bpy.context.temp_override(active_object=ob, selected_editable_objects=[ob], selected_objects=[ob]):
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

def remap_materials(ob, M):
    """Collapse the modelling materials to the six the game uses: paint, dark, chrome, glass, lamps, plate."""
    final = {n: carforge.mat('X_' + n, (0.5, 0.5, 0.5)) for n in ('paint', 'dark', 'chrome', 'glass', 'lamps', 'plate')}
    names = [m.name if m else 'trim' for m in ob.data.materials]
    to = {}
    for i, n in enumerate(names):
        key = {'paint': 'paint', 'trim': 'dark', 'under': 'dark', 'interior': 'dark', 'chrome': 'chrome', 'glass': 'glass',
               'lens': 'lamps', 'lamp_red': 'lamps', 'lamp_amber': 'lamps', 'plate': 'plate'}.get(n, 'dark')
        to[i] = key
    me = ob.data
    order = ['paint', 'dark', 'chrome', 'glass', 'lamps', 'plate']
    idx = np.zeros(len(me.polygons), np.int32); me.polygons.foreach_get('material_index', idx)
    srcname = [names[i] for i in idx]
    new = np.array([order.index(to[i]) for i in idx], np.int32)
    me.materials.clear()
    for n in order: me.materials.append(final[n])
    me.polygons.foreach_set('material_index', new)
    return srcname

def uv_paint(ob):
    """Unwrap just the painted faces into the wear texture; everything else gets its own UVs later."""
    me = ob.data
    if 'UVMap' not in me.uv_layers: me.uv_layers.new(name='UVMap')
    bpy.context.view_layer.objects.active = ob
    for o in bpy.context.scene.objects: o.select_set(False)
    ob.select_set(True)
    with bpy.context.temp_override(active_object=ob, object=ob, selected_editable_objects=[ob], selected_objects=[ob]):
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.select_all(action='DESELECT')
        ob.active_material_index = 0
        bpy.ops.object.material_slot_select()
        bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.012, area_weight=0.0, scale_to_bounds=True)
        bpy.ops.object.mode_set(mode='OBJECT')

def set_swatch_uvs(ob, srcname):
    me = ob.data; uv = me.uv_layers['UVMap'].data
    for p in me.polygons:
        n = srcname[p.index]
        if n in ('lens', 'lamp_red', 'lamp_amber'):
            u = SW['lens' if n == 'lens' else 'red' if n == 'lamp_red' else 'amber']
            for li in p.loop_indices: uv[li].uv = (u, 0.5)
        elif n == 'plate':
            co = [me.vertices[me.loops[li].vertex_index].co for li in p.loop_indices]
            ys = [c.y for c in co]; zs = [c.z for c in co]; front = co[0].x > 0
            y0, y1, z0, z1 = min(ys), max(ys), min(zs), max(zs)
            for li, c in zip(p.loop_indices, co):
                u = (y1 - c.y) / max(1e-4, y1 - y0) if front else (c.y - y0) / max(1e-4, y1 - y0)
                uv[li].uv = (u * 0.5 + (0.0 if front else 0.5), (c.z - z0) / max(1e-4, z1 - z0))  # front plate left half, rear right
        elif n != 'paint':
            for li in p.loop_indices: uv[li].uv = (0.0, 0.0)

def wear_fields(ob, car, P):
    """Smooth per-corner fields the bake multiplies with noise: R dirt, G rust; and a panel id per face."""
    me = ob.data; L = car.L
    if 'wear' in me.color_attributes: me.color_attributes.remove(me.color_attributes['wear'])
    if 'panel' in me.color_attributes: me.color_attributes.remove(me.color_attributes['panel'])
    wear = me.color_attributes.new('wear', 'FLOAT_COLOR', 'CORNER')
    pan = me.color_attributes.new('panel', 'FLOAT_COLOR', 'CORNER')
    axles = [L / 2 - car.uF, L / 2 - car.uR]; ra = P['rad'] + P.get('arch_gap', 0.035)
    dl = [d if not isinstance(d, (list, tuple)) else d[0] for d in P.get('door_lines', [])]
    ws0, ws1 = P['ws']; rw1, rw0 = P['rw']
    for p in me.polygons:
        c = p.center; u = L / 2 - c.x; nz = p.normal.z
        # panel id
        if nz > 0.55: pid = 1 if u < ws0 else 2 if u < rw1 else 3
        elif abs(c.x) > L / 2 - 0.06 or c.z < P['sill'] + 0.06: pid = 12
        else:
            k = 0
            for d in sorted(dl):
                if u > d: k += 1
            side = 0 if c.y > 0 else 4
            pid = 4 + side + min(k, 3)
        for li in p.loop_indices:
            v = me.vertices[me.loops[li].vertex_index].co
            z = v.z
            low = 1 - carforge.smooth((z - (P['sill'] + 0.04)) / (P['belt'] - P['sill'] - 0.1))
            d = min(math.hypot(v.x - ax, v.z - P['rad']) for ax in axles)
            arch = 1 - carforge.smooth((d - ra) / 0.2)
            sill = 1 - carforge.smooth((z - P['sill']) / 0.12)
            edge = 0.0
            dirt = min(1, low * 0.85 + arch * 0.4)
            rust = min(1, arch * 0.95 + sill * 0.8 + 0.12)
            wear.data[li].color = (dirt, rust, 0, 1)
            pan.data[li].color = ((pid + 0.5) / 16.0, 0, 0, 1)

def bake_wear(ob, name):
    """Bake (dirt x noise, rust x noise, AO) and the panel ids into one RGBA texture."""
    sc = bpy.context.scene; sc.render.engine = 'CYCLES'; sc.cycles.samples = 48; sc.cycles.device = 'CPU'
    dup = ob.copy(); dup.data = ob.data.copy(); sc.collection.objects.link(dup)
    bm = bmesh.new(); bm.from_mesh(dup.data)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.material_index != 0], context='FACES')
    bm.to_mesh(dup.data); bm.free()
    bm_mat = bpy.data.materials.new('bakemat'); bm_mat.use_nodes = True
    nt = bm_mat.node_tree; nodes = nt.nodes; links = nt.links
    for n in list(nodes): nodes.remove(n)
    out = nodes.new('ShaderNodeOutputMaterial'); em = nodes.new('ShaderNodeEmission')
    attr = nodes.new('ShaderNodeVertexColor'); attr.layer_name = 'wear'
    tc = nodes.new('ShaderNodeTexCoord')
    n1 = nodes.new('ShaderNodeTexNoise'); n1.inputs['Scale'].default_value = 3.5; n1.inputs['Detail'].default_value = 3.0
    n2 = nodes.new('ShaderNodeTexNoise'); n2.inputs['Scale'].default_value = 13.0; n2.inputs['Detail'].default_value = 8.0; n2.inputs['Roughness'].default_value = 0.7
    links.new(tc.outputs['Object'], n1.inputs['Vector']); links.new(tc.outputs['Object'], n2.inputs['Vector'])
    sep = nodes.new('ShaderNodeSeparateColor'); links.new(attr.outputs['Color'], sep.inputs['Color'])
    # dirt = field * (0.4 + 0.9 * noise)
    m1 = nodes.new('ShaderNodeMath'); m1.operation = 'MULTIPLY_ADD'; m1.inputs[1].default_value = 0.9; m1.inputs[2].default_value = 0.05
    links.new(n1.outputs['Fac'], m1.inputs[0])
    d = nodes.new('ShaderNodeMath'); d.operation = 'MULTIPLY'; d.use_clamp = True
    links.new(sep.outputs['Red'], d.inputs[0]); links.new(m1.outputs[0], d.inputs[1])
    # rust = field * patchy noise (contrast stretched)
    cr = nodes.new('ShaderNodeMapRange'); cr.inputs['From Min'].default_value = 0.42; cr.inputs['From Max'].default_value = 0.68
    links.new(n2.outputs['Fac'], cr.inputs['Value'])
    r = nodes.new('ShaderNodeMath'); r.operation = 'MULTIPLY'; r.use_clamp = True
    links.new(sep.outputs['Green'], r.inputs[0]); links.new(cr.outputs['Result'], r.inputs[1])
    ao = nodes.new('ShaderNodeAmbientOcclusion'); ao.inputs['Distance'].default_value = 0.35; ao.samples = 32
    comb = nodes.new('ShaderNodeCombineColor')
    links.new(d.outputs[0], comb.inputs['Red']); links.new(r.outputs[0], comb.inputs['Green']); links.new(ao.outputs['AO'], comb.inputs['Blue'])
    links.new(comb.outputs['Color'], em.inputs['Color']); links.new(em.outputs['Emission'], out.inputs['Surface'])
    img = bpy.data.images.new(name + '_wear', TEX, TEX, alpha=False, float_buffer=False)
    tn = nodes.new('ShaderNodeTexImage'); tn.image = img; nodes.active = tn
    dup.data.materials.clear(); dup.data.materials.append(bm_mat)
    for o in sc.objects: o.select_set(False)
    dup.select_set(True); bpy.context.view_layer.objects.active = dup
    sc.render.bake.margin = 6
    with bpy.context.temp_override(active_object=dup, object=dup, selected_objects=[dup], selected_editable_objects=[dup]):
        bpy.ops.object.bake(type='EMIT', margin=6)
    rgb = np.array(img.pixels[:], np.float32).reshape(TEX, TEX, 4)[..., :3]
    # panel ids
    attr.layer_name = 'panel'
    for n in list(nodes):
        if n not in (out, em, attr, tn): nodes.remove(n)
    links.new(attr.outputs['Color'], em.inputs['Color'])
    img2 = bpy.data.images.new(name + '_panel', TEX, TEX, alpha=False, float_buffer=True); tn.image = img2
    with bpy.context.temp_override(active_object=dup, object=dup, selected_objects=[dup], selected_editable_objects=[dup]):
        bpy.ops.object.bake(type='EMIT', margin=6)
    pid = np.array(img2.pixels[:], np.float32).reshape(TEX, TEX, 4)[..., 0]
    bpy.data.objects.remove(dup)
    out_px = np.zeros((TEX, TEX, 4), np.uint8)
    out_px[..., :3] = (np.clip(rgb, 0, 1) * 255 + 0.5).astype(np.uint8)
    # soften the sampling grain in the shading and dirt (rust keeps its crisp patches)
    from PIL import ImageFilter
    for ch in (0, 2):
        out_px[..., ch] = np.asarray(Image.fromarray(out_px[..., ch]).filter(ImageFilter.GaussianBlur(1.6)))
    out_px[..., 3] = (np.clip(pid, 0, 1) * 255 + 0.5).astype(np.uint8)
    Image.fromarray(np.flipud(out_px), 'RGBA').save(os.path.join(OUT, name + '_wear.png'))

def dents(ob, seed, n=9):
    """Push the panels in at a few places (battered version)."""
    rnd = random.Random(seed); me = ob.data
    idx = np.zeros(len(me.polygons), np.int32); me.polygons.foreach_get('material_index', idx)
    paintv = set()
    for p in me.polygons:
        if p.material_index == 0: paintv.update(p.vertices)
    vs = [me.vertices[i] for i in paintv]
    if not vs: return
    for _ in range(n):
        c = rnd.choice(vs).co.copy(); r = rnd.uniform(0.12, 0.28); depth = rnd.uniform(0.015, 0.045)
        for v in vs:
            dd = (v.co - c).length / r
            if dd < 1: v.co -= v.normal * depth * (1 - dd * dd) ** 2

def decimate(ob, ratio):
    mod = ob.modifiers.new('dec', 'DECIMATE'); mod.ratio = ratio; mod.use_collapse_triangulate = True
    bpy.context.view_layer.objects.active = ob
    with bpy.context.temp_override(object=ob, active_object=ob):
        bpy.ops.object.modifier_apply(modifier=mod.name)

def tris(ob): return sum(len(p.vertices) - 2 for p in ob.data.polygons)

def wheel_swatch(ob, rimkey):
    me = ob.data
    if 'UVMap' not in me.uv_layers: me.uv_layers.new(name='UVMap')
    uv = me.uv_layers['UVMap'].data
    names = [m.name if m else 'trim' for m in me.materials]
    for p in me.polygons:
        n = names[p.material_index]
        u = WS['tyre'] if n in ('tyre', 'interior', 'under', 'trim') else WS[rimkey]
        for li in p.loop_indices: uv[li].uv = (u, 0.5)
    wm = carforge.mat('X_wheel', (0.5, 0.5, 0.5))
    me.materials.clear(); me.materials.append(wm)
    for p in me.polygons: p.material_index = 0

manifest = {}
mf = os.path.join(OUT, 'cars.json')
if os.path.exists(mf): manifest = json.load(open(mf))
for name, P in models.MODELS.items():
    if ONLY and name not in ONLY: continue
    car, parts = carforge.build(P)
    M = carforge.MATS
    # LOD1 parts first (no cabin), from copies
    low_src = [parts['body'], parts['details'], parts['wells'], parts['under']]
    low = []
    for o in low_src:
        c = o.copy(); c.data = o.data.copy(); bpy.context.scene.collection.objects.link(c); low.append(c)
    hi = objs_join([parts['body'], parts['details'], parts['interior'], parts['wells'], parts['under']], 'car_LOD0')
    lo = objs_join(low, 'car_LOD1')
    src_hi = remap_materials(hi, M); src_lo = remap_materials(lo, M)
    uv_paint(hi)
    wear_fields(hi, car, P)
    bake_wear(hi, name)
    set_swatch_uvs(hi, src_hi)
    # the low version shares the painted UV layout: transfer by nearest face centre
    uv_paint(lo); set_swatch_uvs(lo, src_lo)
    # transfer paint UVs from hi to lo so the same wear texture fits
    kd = kdtree.KDTree(len(hi.data.loops))
    hv = hi.data.vertices; huv = hi.data.uv_layers['UVMap'].data
    hmat = np.zeros(len(hi.data.polygons), np.int32); hi.data.polygons.foreach_get('material_index', hmat)
    loopmat = np.zeros(len(hi.data.loops), np.int32)
    for p in hi.data.polygons:
        for li in p.loop_indices: loopmat[li] = p.material_index
    n_ins = 0
    for li, l in enumerate(hi.data.loops):
        if loopmat[li] == 0: kd.insert(hv[l.vertex_index].co, li); n_ins += 1
    kd.balance()
    luv = lo.data.uv_layers['UVMap'].data
    for p in lo.data.polygons:
        if p.material_index != 0: continue
        for li in p.loop_indices:
            co = lo.data.vertices[lo.data.loops[li].vertex_index].co
            _, j, _ = kd.find(co)
            if j is not None: luv[li].uv = huv[j].uv
    decimate(lo, 0.45)
    # battered versions
    dh = hi.copy(); dh.data = hi.data.copy(); bpy.context.scene.collection.objects.link(dh); dh.name = 'dent_LOD0'
    dl = lo.copy(); dl.data = lo.data.copy(); bpy.context.scene.collection.objects.link(dl); dl.name = 'dent_LOD1'
    dents(dh, hash(name) & 0xffff); dents(dl, hash(name) & 0xffff)
    # wheels
    rim = {'multi': 'white', 'alloy_dark': 'gun', 'steel': 'tyre'}.get(P.get('wheel', 'steel'), 'silver')
    if P.get('wheel') == 'steel': rim = 'silver' if name != 'jeep' else 'tyre'
    for w in parts['wheels']: wheel_swatch(w, rim)
    # turn everything so the front faces -Y (Unity +Z), then export
    R = Matrix.Rotation(math.radians(-90), 4, 'Z')
    for o in [hi, lo, dh, dl]:
        o.data.transform(R); o.matrix_world = Matrix.Identity(4)
    for w in parts['wheels']:
        w.matrix_world = R @ w.matrix_world
        apply_xf(w)  # bake the wheel's place into its mesh, then put its origin back at the wheel centre
        c = sum((v.co for v in w.data.vertices), Vector()) / len(w.data.vertices)
        bb = [min(v.co.x for v in w.data.vertices), max(v.co.x for v in w.data.vertices)]
        cen = Vector(((bb[0] + bb[1]) / 2, c.y, P['rad']))
        w.data.transform(Matrix.Translation(-cen)); w.location = cen
    for o in bpy.context.scene.objects: o.select_set(False)
    keep = [hi, lo, dh, dl] + parts['wheels']
    for o in keep: o.select_set(True)
    bpy.context.view_layer.objects.active = hi
    path = os.path.join(OUT, name + '.fbx')
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y',
                             apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
                             mesh_smooth_type='FACE', use_tspace=False, path_mode='STRIP', embed_textures=False, add_leaf_bones=False)
    manifest[name] = dict(L=P['L'], W=P['W'], wb=P['wb'], fo=P['fo'], rad=P['rad'], tris=[tris(hi), tris(lo)], wheel=P.get('wheel'), rim=rim,
                          height=max(z for _, z in P['top']), van=False)
    print('CAR', name, manifest[name]['tris'], 'wheel', tris(parts['wheels'][0]))
    json.dump(manifest, open(mf, 'w'), indent=1)

# shared atlases
la = np.zeros((16, 64, 4), np.uint8)
for k, (c, u) in {'lens': ((235, 238, 242), 0.125), 'red': ((150, 8, 8), 0.375), 'amber': ((235, 120, 10), 0.625), 'dark': ((10, 10, 12), 0.875)}.items():
    x0 = int((u - 0.125) * 64); la[:, x0:x0 + 16, :3] = c; la[:, x0:x0 + 16, 3] = 255
Image.fromarray(la, 'RGBA').save(os.path.join(OUT, 'car_lamps.png'))
wa = np.zeros((16, 64, 4), np.uint8)
for k, (c, u) in {'tyre': ((16, 16, 17), 0.125), 'silver': ((165, 167, 172), 0.375), 'white': ((228, 228, 226), 0.625), 'gun': ((48, 50, 54), 0.875)}.items():
    x0 = int((u - 0.125) * 64); wa[:, x0:x0 + 16, :3] = c; wa[:, x0:x0 + 16, 3] = 255
Image.fromarray(wa, 'RGBA').save(os.path.join(OUT, 'car_wheels.png'))
print('DONE')

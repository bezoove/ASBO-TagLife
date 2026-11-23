# Poly Haven .blend models -> phone-ready FBX (two LODs, one mesh per LOD, pivot at bottom centre, Y up) + textures + manifest
import bpy, bmesh, sys, os, re, json, glob
import numpy as np
from mathutils import Vector
from PIL import Image

SRC, OUT = sys.argv[1], sys.argv[2]
ONLY = sys.argv[3].split(',') if len(sys.argv) > 3 and sys.argv[3] else None
os.makedirs(OUT, exist_ok=True)

# kind, texture size, and how to choose / reduce geometry
CFG = {
    'garden_gnome': dict(kind='prop', tex=1024, mode='prop', caps=[('', 3000)]),
    'planter_pot_clay': dict(kind='prop', tex=1024, mode='prop', caps=[('', 1500)]),
    'potted_plant_04': dict(kind='prop', tex=1024, mode='prop', caps=[('', 1500)]),
    'potted_plant_01': dict(kind='prop', tex=1024, mode='prop', caps=[('dirt|pebbles|ground', 300), ('leaves', 6000), ('', 1500)]),
    'potted_plant_02': dict(kind='prop', tex=1024, mode='prop', caps=[('dirt|pebbles|ground', 300), ('leaves', 5000), ('', 1500)]),
    'metal_trash_can': dict(kind='prop', tex=1024, mode='prop', caps=[('', 1800)],
                            variants={'metal_trash_can': r'^metal_trash_can(_lid|_handle_left|_handle_right)?$', 'metal_trash_can_rust': r'^metal_trash_can_rust'}),
    'garden_hose_wall_mounted_01': dict(kind='prop', tex=1024, mode='prop', caps=[('', 1600)], pivot='keep'),
    'fern_02': dict(kind='foliage', tex=1024, mode='each', caps=[('', 1200)]),
    'rock_moss_set_01': dict(kind='rock', tex=2048, mode='each', caps=[('', 2000)]),
    'dandelion_01': dict(kind='foliage', tex=1024, mode='lod', lods=(2, 3)),
    'celandine_01': dict(kind='foliage', tex=1024, mode='lod', lods=(1, 2), skip=r'geonodes|geometry'),
    'periwinkle_plant': dict(kind='foliage', tex=1024, mode='lod', lods=(4, 6)),
    'weed_plant_02': dict(kind='foliage', tex=1024, mode='lod', lods=(1, 3)),
    'grass_medium_01': dict(kind='foliage', tex=1024, mode='lod', lods=(2, None), skip=r'geonodes|geometry|tiny'),
    'rock_07': dict(kind='rock', tex=512, mode='lod', lods=(3, 3), caps=[('', 500)], caps1=150),
    'rock_09': dict(kind='rock', tex=512, mode='lod', lods=(3, 3), caps=[('', 500)], caps1=150),
    'stone_01': dict(kind='rock', tex=512, mode='lod', lods=(3, 3), caps=[('', 400)], caps1=150),
    'namaqualand_boulder_02': dict(kind='rock', tex=1024, mode='lod', lods=(3, 3), caps=[('', 2500)], caps1=800),
    'namaqualand_boulder_03': dict(kind='rock', tex=1024, mode='lod', lods=(3, 3), caps=[('', 2500)], caps1=800),
    'namaqualand_boulder_04': dict(kind='rock', tex=1024, mode='lod', lods=(3, 3), caps=[('', 2500)], caps1=800),
    'coastal_cliff_02': dict(kind='rock', tex=2048, mode='lod', lods=(3, 3), caps=[('', 12000)], caps1=4000),
    'mountainside': dict(kind='rock', tex=2048, mode='lod', lods=(3, 3), caps=[('', 8000)], caps1=2500),
    'fir_sapling_medium': dict(kind='tree', tex=1024, mode='lod', lods=(2, None), skip=r'twigs|needles|dead_branches|geometry'),
    # from the Drive (Poly Haven, CC0): street furniture and junk for the town. kind: prop = knock it about,
    # fixture = solid but stays put, wall = on a wall (no collider), flat = in the ground (no collider)
    'barrel_stove': dict(kind='fixture', tex=1024, mode='prop', caps=[('', 2500)], variants={'barrel_stove': r'^barrel_stove$'}),
    'concrete_road_barrier': dict(kind='fixture', tex=1024, mode='lod', lods=(3, 5)),
    'concrete_road_barrier_02': dict(kind='fixture', tex=1024, mode='lod', lods=(3, 4)),
    'covered_car': dict(kind='fixture', tex=1024, mode='prop', caps=[('wheel', 500), ('', 4500)]),
    'electric_stove': dict(kind='fixture', tex=1024, mode='prop', caps=[('stovetop', 4000), ('door_top|handle', 1200), ('', 200)], variants={'electric_stove': r'^(?!wdg_)'}),
    'football': dict(kind='prop', tex=512, mode='prop', caps=[('', 1200)], variants={'football': r'^football_inflated$'}),
    'industrial_wall_lamp': dict(kind='wall', tex=512, mode='prop', caps=[('', 1500)]),
    'large_iron_gate': dict(kind='fixture', tex=1024, mode='prop', caps=[('door', 3000), ('', 400)]),
    'modular_electricity_poles': dict(kind='fixture', tex=1024, mode='prop', caps=[('connection_(large|small)', 1500), ('fuse|transformer', 600), ('', 150)],
                                      variants={'electricity_pole_01': r'^preset_01_', 'electricity_pole_02': r'^preset_02_', 'electricity_pole_03': r'^preset_03_'}),
    'old_tyre': dict(kind='prop', tex=512, mode='prop', caps=[('', 1200)]),
    'rollershutter_window_02': dict(kind='wall', tex=1024, mode='prop', caps=[('', 600)],
                                    variants={'rollershutter_window_02': r'^rollershutter_window_02$', 'rollershutter_window_02_graffiti': r'_graffiti$'}),
    'rusted_wheel_rim_01': dict(kind='prop', tex=512, mode='prop', caps=[('', 1500)]),
    'security_camera_01': dict(kind='wall', tex=512, mode='prop', caps=[('', 2000)], variants={'security_camera_01': r'^security_camera_01$'}),
    'security_camera_02': dict(kind='wall', tex=512, mode='prop', caps=[('', 1500)], variants={'security_camera_02': r'^security_camera_02'}),
    'utility_box_02': dict(kind='fixture', tex=1024, mode='prop', caps=[('', 1500)]),
    'water_manhole_cover': dict(kind='flat', tex=1024, mode='prop', caps=[('frame', 300), ('', 800)]),
}

def tris(me): return sum(len(p.vertices) - 2 for p in me.polygons)

def cap_for(name, caps):
    for pat, c in caps:
        if pat == '' or re.search(pat, name): return c
    return 1e9

def fix_uv(me):
    """Geometry-node trees keep their UVs as a 3D corner attribute, which the FBX writer ignores: turn it into a real UV map."""
    if len(me.uv_layers) > 0: return
    a = next((x for x in me.attributes if x.domain == 'CORNER' and x.data_type in ('FLOAT_VECTOR', 'FLOAT2') and not x.name.startswith('.')), None)
    if a is None: return
    n = len(me.loops); w = 3 if a.data_type == 'FLOAT_VECTOR' else 2
    d = np.zeros(n * w, np.float32); a.data.foreach_get('vector', d); d = d.reshape(-1, w)[:, :2].copy()
    nm = a.name; a.name = nm + '_src'
    uv = me.uv_layers.new(name=nm); uv.data.foreach_set('uv', d.ravel())

def eval_tris(ob):
    dg = bpy.context.evaluated_depsgraph_get(); ev = ob.evaluated_get(dg); me = ev.to_mesh(); t = tris(me); ev.to_mesh_clear(); return t

def baked_mesh(ob, target=None):
    """The object's evaluated mesh in world space, welded and collapsed to about `target` triangles if it has more."""
    mods = []
    if target is not None:
        t = eval_tris(ob)
        if t > target:
            w = ob.modifiers.new('weld', 'WELD'); w.merge_threshold = 1e-5; mods.append(w)
            d = ob.modifiers.new('dec', 'DECIMATE'); d.decimate_type = 'COLLAPSE'; d.use_collapse_triangulate = True; mods.append(d)
            d.ratio = max(0.002, target / t)
            dg = bpy.context.evaluated_depsgraph_get(); ev = ob.evaluated_get(dg); me = ev.to_mesh(); got = tris(me); ev.to_mesh_clear()
            if got > target * 1.15: d.ratio = max(0.002, d.ratio * target / got)  # welding changes the count; correct once
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg), preserve_all_data_layers=True, depsgraph=dg)
    for m in mods: ob.modifiers.remove(m)
    me.transform(ob.matrix_world)
    fix_uv(me)
    return me, [m for m in ob.data.materials]

def merge(parts, name):
    """One mesh from several (mesh, materials) parts; materials become submeshes."""
    mats = []; bm = bmesh.new()
    for me, ml in parts:
        remap = []
        for m in ml:
            if m not in mats: mats.append(m)
            remap.append(mats.index(m))
        if remap:
            idx = np.zeros(len(me.polygons), np.int32); me.polygons.foreach_get('material_index', idx)
            idx = np.array([remap[i] if i < len(remap) else 0 for i in idx], np.int32); me.polygons.foreach_set('material_index', idx)
        bm.from_mesh(me)
    out = bpy.data.meshes.new(name); bm.to_mesh(out); bm.free()
    for m in mats: out.materials.append(m)
    return out

def tex_files(mat):
    got = {}
    if mat is None or not mat.use_nodes: return got
    for n in mat.node_tree.nodes:
        if n.type != 'TEX_IMAGE' or n.image is None: continue
        f = bpy.path.abspath(n.image.filepath); b = os.path.basename(f).lower()
        for key, tags in [('diff', ['_diff_']), ('nor', ['_nor_gl_']), ('rough', ['_rough_']), ('metal', ['_metal_']), ('alpha', ['_alpha_', '_opacity_'])]:
            if any(t in b for t in tags) and key not in got: got[key] = f
    return got

def load_gray(p, size):
    if p is None: return None
    if p.endswith('.exr'):
        import OpenEXR
        f = OpenEXR.File(p); ch = f.channels(); k = list(ch.keys())[0]; a = np.asarray(ch[k].pixels, np.float32)
        a = a[..., 0] if a.ndim == 3 else a
        im = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
    else:
        im = Image.open(p).convert('L')
    return np.asarray(im.resize((size, size), Image.LANCZOS), np.float32) / 255.0

def write_textures(mat, folder, size):
    t = tex_files(mat); name = re.sub(r'[^A-Za-z0-9_]', '_', mat.name); info = {'name': name}
    if 'diff' not in t: return info
    dif = Image.open(t['diff']).convert('RGB').resize((size, size), Image.LANCZOS)
    if 'alpha' in t:
        a = Image.open(t['alpha']).convert('L').resize((size, size), Image.LANCZOS)
        dif = dif.convert('RGBA'); dif.putalpha(a); dif.save(f'{folder}/{name}_albedo.png', optimize=True); info['albedo'] = f'{name}_albedo.png'; info['alpha'] = True
    else:
        dif.save(f'{folder}/{name}_albedo.jpg', quality=90); info['albedo'] = f'{name}_albedo.jpg'
    if 'nor' in t:
        p = t['nor']
        if p.endswith('.exr'):
            import OpenEXR
            f = OpenEXR.File(p); ch = f.channels(); k = list(ch.keys())[0]; a = np.asarray(ch[k].pixels, np.float32)
            im = Image.fromarray((np.clip(a[..., :3], 0, 1) * 255 + 0.5).astype(np.uint8))
        else:
            im = Image.open(p).convert('RGB')
        im.resize((size, size), Image.LANCZOS).save(f'{folder}/{name}_normal.png', optimize=True); info['normal'] = f'{name}_normal.png'
    ms = max(256, size // 2)
    rough = load_gray(t.get('rough'), ms); metal = load_gray(t.get('metal'), ms)
    if rough is None: rough = np.full((ms, ms), 0.8, np.float32)
    if metal is None: metal = np.zeros((ms, ms), np.float32)
    mk = np.zeros((ms, ms, 4), np.uint8); mk[..., 0] = (metal * 255).astype(np.uint8); mk[..., 1] = 255; mk[..., 3] = ((1 - rough) * 255).astype(np.uint8)
    Image.fromarray(mk, 'RGBA').save(f'{folder}/{name}_mask.png', optimize=True); info['mask'] = f'{name}_mask.png'
    return info

def export_variant(asset, vname, lod0, lod1, folder, pivot):
    # pivot: bottom centre of LOD0
    if pivot != 'keep':
        vs = np.zeros(len(lod0.vertices) * 3, np.float32); lod0.vertices.foreach_get('co', vs); vs = vs.reshape(-1, 3)
        mn, mx = vs.min(0), vs.max(0); off = Vector((-(mn[0] + mx[0]) / 2, -(mn[1] + mx[1]) / 2, -mn[2]))
        from mathutils import Matrix
        for me in [lod0, lod1]:
            if me is not None: me.transform(Matrix.Translation(off))
    for o in list(bpy.context.scene.objects): o.select_set(False)
    obs = []
    for i, me in enumerate([lod0, lod1]):
        if me is None: continue
        nm = f'{vname}_LOD{i}'
        if nm in bpy.data.objects: bpy.data.objects[nm].name = nm + '_src'  # keep the exported name exact (Unity reads _LODn)
        ob = bpy.data.objects.new(nm, me); bpy.context.scene.collection.objects.link(ob); ob.select_set(True); obs.append(ob)
    bpy.context.view_layer.objects.active = obs[0]
    path = f'{folder}/{vname}.fbx'
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y',
                             apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
                             mesh_smooth_type='FACE', use_tspace=False, path_mode='STRIP', embed_textures=False, add_leaf_bones=False,
                             use_mesh_modifiers=False)
    for ob in obs: bpy.data.objects.remove(ob)
    return {'name': vname, 'fbx': f'{vname}.fbx', 'tris': [tris(lod0), tris(lod1) if lod1 else 0]}

manifest = {}
mf = f'{OUT}/manifest.json'
if os.path.exists(mf): manifest = json.load(open(mf))
for asset, cfg in CFG.items():
    if ONLY and asset not in ONLY: continue
    d = os.path.join(SRC, asset)
    blends = glob.glob(d + '/*.blend')
    if not blends: continue
    bpy.ops.wm.open_mainfile(filepath=blends[0], load_ui=False, use_scripts=False)
    def include(lc):  # every LOD collection must be in the view layer, or its modifiers never evaluate
        lc.exclude = False; lc.hide_viewport = False
        for c in lc.children: include(c)
    include(bpy.context.view_layer.layer_collection)
    for o in bpy.data.objects: o.hide_viewport = False; o.hide_set(False) if o.name in bpy.context.view_layer.objects else None
    folder = f'{OUT}/{asset}'; os.makedirs(folder, exist_ok=True)
    meshes = [o for o in bpy.data.objects if o.type == 'MESH']
    groups = {}  # vname -> (lod0 objs, lod1 objs, caps0, cap1)
    mode = cfg['mode']; caps = cfg.get('caps', [('', 1e9)])
    if mode == 'prop':
        vs = cfg.get('variants', {asset: '.*'})
        for vn, pat in vs.items(): groups[vn] = [o for o in meshes if re.search(pat, o.name)]
    elif mode == 'each':
        for o in meshes: groups[o.name] = [o]
    variants = []
    if mode in ('prop', 'each'):
        for vn, obs in groups.items():
            if not obs: continue
            p0 = []; p1 = []
            for o in obs:
                t = eval_tris(o); c = min(t, cap_for(o.name, caps))
                p0.append(baked_mesh(o, c)); p1.append(baked_mesh(o, max(40, int(c * 0.35))))
            variants.append(export_variant(asset, vn, merge(p0, vn + '_0'), merge(p1, vn + '_1'), folder, cfg.get('pivot')))
    else:
        l0, l1 = cfg['lods']; skip = cfg.get('skip')
        byv = {}
        for o in meshes:
            m = re.match(r'^(.*)_LOD(\d)$', o.name)
            if not m or (skip and re.search(skip, o.name)): continue
            byv.setdefault(m.group(1), {})[int(m.group(2))] = o
        for vn, lods in sorted(byv.items()):
            if l0 not in lods: continue
            o0 = lods[l0]; c0 = cap_for(vn, caps)
            m0 = merge([baked_mesh(o0, c0 if c0 < 1e8 else None)], vn + '_0')
            m1 = None
            if l1 is not None and l1 in lods:
                o1 = lods[l1]; c1 = cfg.get('caps1')
                m1 = merge([baked_mesh(o1, c1)], vn + '_1')
            variants.append(export_variant(asset, vn, m0, m1, folder, cfg.get('pivot')))
    # textures for every material used
    mats = {}
    for o in meshes:
        for m in o.data.materials:
            if m and m.name not in mats: mats[m.name] = m
    texinfo = {}
    for mn, m in mats.items():
        if not tex_files(m): continue
        texinfo[mn] = write_textures(m, folder, cfg['tex'])
    manifest[asset] = {'kind': cfg['kind'], 'variants': variants, 'materials': texinfo}
    print('DONE', asset, [(v['name'], v['tris']) for v in variants], list(texinfo))
    json.dump(manifest, open(mf, 'w'), indent=1)

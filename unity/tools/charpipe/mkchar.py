"""Build rigged character FBX files (T-pose, Unity Humanoid bone names, LOD0-2) from the MakeHuman pipeline. Run with system python (bpy module)."""
import sys, os, json, pickle, time
import numpy as np
import bpy
sys.path.insert(0, '/tmp/claude-0/charpipe')
import build as B
from mhcore import GAME, PARENT, UNITY

OUT = '/tmp/claude-0/charpipe/out/fbx'
os.makedirs(OUT, exist_ok=True)
LODS = [16000, 6500, 2600]
CHILD = {'Hips': 'Spine', 'Spine': 'Chest', 'Chest': 'Neck', 'Neck': 'Head', 'Head': '_tail_Head'}
for s in 'LR':
    CHILD.update({f'Shoulder.{s}': f'UpperArm.{s}', f'UpperArm.{s}': f'LowerArm.{s}', f'LowerArm.{s}': f'Hand.{s}', f'Hand.{s}': f'_tail_Hand.{s}',
                  f'UpperLeg.{s}': f'LowerLeg.{s}', f'LowerLeg.{s}': f'Foot.{s}', f'Foot.{s}': f'Toes.{s}', f'Toes.{s}': f'_tail_Toes.{s}'})

def bl(p):  # MakeHuman/metres (y up, z forward) -> Blender (z up, -y forward)
    return (float(p[0]), float(-p[2]), float(p[1]))

def make(name, lods):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    heads = lods[0]['heads']
    arm_data = bpy.data.armatures.new('Armature')
    arm = bpy.data.objects.new('Armature', arm_data)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    eb = {}
    for b in GAME:
        e = arm_data.edit_bones.new(UNITY[b])
        e.head = bl(heads[b]); e.tail = bl(heads[CHILD[b]])
        if (np.array(e.tail) - np.array(e.head)).dot(np.array(e.tail) - np.array(e.head)) < 1e-6:
            e.tail = (e.head[0], e.head[1], e.head[2] + 0.05)
        eb[b] = e
    for b in GAME:
        if PARENT[b]: eb[b].parent = eb[PARENT[b]]
    # roll: keep bones' local X pointing to world X for a clean humanoid setup
    for e in arm_data.edit_bones: e.roll = 0.0
    bpy.ops.object.mode_set(mode='OBJECT')
    mats = {}
    for m in B.MATS:
        mt = bpy.data.materials.new(m); mats[m] = mt
    for li, M in enumerate(lods):
        verts = [bl(p) for p in M['pos']]
        faces, fmat = [], []
        mlist = [m for m in B.MATS if M['tris'][m]]
        for mi, m in enumerate(mlist):
            for t in M['tris'][m]: faces.append(tuple(int(i) for i in t)); fmat.append(mi)
        me = bpy.data.meshes.new(f'Body_LOD{li}')
        me.from_pydata(verts, [], faces)
        for m in mlist: me.materials.append(mats[m])
        me.polygons.foreach_set('material_index', fmat)
        uvl = me.uv_layers.new(name='UVMap')
        uv = M['uv']
        loops_v = np.zeros(len(me.loops), np.int32); me.loops.foreach_get('vertex_index', loops_v)
        uvl.data.foreach_set('uv', uv[loops_v].astype(np.float32).ravel())
        me.polygons.foreach_set('use_smooth', [True] * len(me.polygons))
        n = M['nrm']; me.normals_split_custom_set_from_vertices([bl(v) for v in n])
        ob = bpy.data.objects.new(f'Body_LOD{li}', me)
        bpy.context.scene.collection.objects.link(ob)
        groups = {b: ob.vertex_groups.new(name=UNITY[b]) for b in GAME}
        for vi, w in enumerate(M['wts']):
            for bi, x in w:
                if x > 1e-4: groups[GAME[bi]].add([vi], float(x), 'REPLACE')
        ob.parent = arm
        mod = ob.modifiers.new('Armature', 'ARMATURE'); mod.object = arm
    for o in bpy.context.scene.objects: o.select_set(True)
    out = f'{OUT}/{name}.fbx'
    bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False,
                             bake_anim=False, mesh_smooth_type='OFF', use_mesh_modifiers=False, axis_forward='-Z', axis_up='Y',
                             apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS', armature_nodetype='NULL',
                             primary_bone_axis='Y', secondary_bone_axis='X', use_armature_deform_only=True)
    return out

if __name__ == '__main__':
    only = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    for body, outfit, hair in B.COMBOS:
        name = f'{body}_{outfit}_{hair}'
        if only and name not in only: continue
        t = time.time()
        lods = [B.build(body, outfit, hair, tt) for tt in LODS]
        out = make(name, lods)
        print('OK', name, [sum(len(v) for v in L['tris'].values()) for L in lods], round(time.time() - t, 1), 's', flush=True)

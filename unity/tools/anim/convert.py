"""CMU BVH (cgspeed MotionBuilder-friendly) -> FBX for Unity Humanoid import.
Rest pose is re-baked to the frame-1 T-pose (straight legs) so Unity's avatar auto-config sees a proper T-pose."""
import bpy, sys, os, json

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.render.fps = 120

def convert(bvh, out, scale=0.0615):
    reset()
    bpy.ops.import_anim.bvh(filepath=bvh, global_scale=scale, frame_start=1, use_fps_scale=False,
                            update_scene_fps=False, update_scene_duration=True, rotate_mode='NATIVE',
                            axis_forward='-Z', axis_up='Y')
    A = bpy.context.selected_objects[0]
    sc = bpy.context.scene
    f0, f1 = int(sc.frame_start), int(sc.frame_end)
    sc.frame_set(f0)
    # B: copy of the skeleton, rest = frame-1 T-pose
    B = A.copy(); B.data = A.data.copy(); B.animation_data_clear(); B.name = 'Armature'; B.data.name = 'Armature'
    sc.collection.objects.link(B)
    bpy.context.view_layer.update()
    for o in sc.objects: o.select_set(False)
    bpy.context.view_layer.objects.active = B; B.select_set(True)
    bpy.ops.object.mode_set(mode='POSE')
    for pb in B.pose.bones:
        pb.matrix = A.pose.bones[pb.name].matrix.copy()
        bpy.context.view_layer.update()
    bpy.ops.pose.select_all(action='SELECT')
    bpy.ops.pose.armature_apply(selected=False)
    for pb in B.pose.bones:
        c = pb.constraints.new('COPY_TRANSFORMS'); c.target = A; c.subtarget = pb.name
    bpy.ops.nla.bake(frame_start=f0 + 1, frame_end=f1, only_selected=False, visual_keying=True,
                     clear_constraints=True, use_current_action=True, bake_types={'POSE'})
    # frame f0 = exact rest (T-pose) so the exported default pose is the T-pose
    for pb in B.pose.bones:
        pb.location = (0, 0, 0); pb.rotation_mode = pb.rotation_mode
        pb.rotation_quaternion = (1, 0, 0, 0); pb.rotation_euler = (0, 0, 0); pb.scale = (1, 1, 1)
        for path in ('location', 'rotation_quaternion' if pb.rotation_mode == 'QUATERNION' else 'rotation_euler'):
            pb.keyframe_insert(path, frame=f0)
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.data.objects.remove(A, do_unlink=True)
    for o in sc.objects: o.select_set(False)
    B.select_set(True)
    sc.frame_start = f0
    sc.frame_set(f0)
    bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={'ARMATURE'}, add_leaf_bones=False,
                             bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
                             bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0,
                             axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
                             armature_nodetype='NULL', primary_bone_axis='Y', secondary_bone_axis='X')
    return f1 - f0

if __name__ == '__main__':
    jobs = json.loads(sys.argv[-1])
    for src, dst in jobs:
        n = convert(src, dst)
        print('OK', os.path.basename(dst), n, 'frames', flush=True)

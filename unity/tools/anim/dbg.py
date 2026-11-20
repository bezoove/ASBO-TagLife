import bpy
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.context.scene.render.fps=120
bpy.ops.import_anim.bvh(filepath='/tmp/claude-0/cmu/data/007/07_01.bvh', global_scale=0.0615, frame_start=1, use_fps_scale=False, update_scene_fps=False, update_scene_duration=True, rotate_mode='NATIVE', axis_forward='-Z', axis_up='Y')
A=bpy.context.selected_objects[0]; sc=bpy.context.scene
print('frames', sc.frame_start, sc.frame_end, A.animation_data.action.frame_range[:])
for f in (1,2):
    sc.frame_set(f)
    pb=A.pose.bones['LeftArm']; print(f, 'LeftArm head',tuple(round(x,3) for x in (A.matrix_world@pb.head)), 'tail', tuple(round(x,3) for x in (A.matrix_world@pb.tail)))
    pb=A.pose.bones['LeftUpLeg']; print(f, 'LeftUpLeg', tuple(round(x,3) for x in (A.matrix_world@pb.head)), tuple(round(x,3) for x in (A.matrix_world@pb.tail)))
pb=A.data.bones['LeftArm']; print('rest LeftArm', tuple(round(x,3) for x in pb.head_local), tuple(round(x,3) for x in pb.tail_local))
sc.frame_set(1)
B = A.copy(); B.data = A.data.copy(); B.animation_data_clear(); sc.collection.objects.link(B)
bpy.context.view_layer.update()
for o in sc.objects: o.select_set(False)
bpy.context.view_layer.objects.active = B; B.select_set(True)
print('mode set', bpy.ops.object.mode_set(mode='POSE'), B.mode)
for pb in B.pose.bones:
    pb.matrix = A.pose.bones[pb.name].matrix.copy(); bpy.context.view_layer.update()
pb=B.pose.bones['LeftArm']; print('B posed LeftArm', tuple(round(x,3) for x in (B.matrix_world@pb.head)), tuple(round(x,3) for x in (B.matrix_world@pb.tail)))
print('A obj', A.matrix_world.translation, 'B obj', B.matrix_world.translation)
bpy.ops.pose.select_all(action='SELECT')
print(bpy.ops.pose.armature_apply(selected=False))
pb=B.data.bones['LeftArm']; print('B rest LeftArm', tuple(round(x,3) for x in pb.head_local), tuple(round(x,3) for x in pb.tail_local))

import bpy, sys, json
import matplotlib; matplotlib.use('Agg'); import matplotlib.pyplot as plt
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=sys.argv[-1])
arm=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'][0]
act=arm.animation_data.action
print('range',act.frame_range[:], 'fps', bpy.context.scene.render.fps)
fs=[int(act.frame_range[0]), int(act.frame_range[0])+30, int(act.frame_range[0])+60, int(act.frame_range[0])+90]
fig,axs=plt.subplots(1,5,figsize=(15,4))
def draw(ax, rest=False):
    for b in arm.pose.bones:
        h=arm.matrix_world@(b.bone.head_local if rest else b.head); t=arm.matrix_world@(b.bone.tail_local if rest else b.tail)
        ax.plot([h.x,t.x],[h.z,t.z],'b-' if '.L' in b.name or 'Left' in b.name else 'r-' if 'Right' in b.name else 'k-')
    ax.set_aspect('equal')
draw(axs[0],True); axs[0].set_title('rest')
for a,f in zip(axs[1:],fs):
    bpy.context.scene.frame_set(f); draw(a); a.set_title(str(f))
plt.savefig('/tmp/claude-0/anim/check.png',dpi=60)
hips=arm.pose.bones['Hips']; bpy.context.scene.frame_set(fs[0]); print('hips z', (arm.matrix_world@hips.head).z)

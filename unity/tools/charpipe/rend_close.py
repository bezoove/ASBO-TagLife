import bpy, sys, math
f=sys.argv[sys.argv.index('--')+1]; out=sys.argv[sys.argv.index('--')+2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=f)
sc=bpy.context.scene
for o in sc.objects:
    if o.type=='MESH' and not o.name.endswith('LOD0'): o.hide_render=True
cols={'skin':(0.9,0.7,0.6,1),'hair':(0.25,0.16,0.1,1),'top':(0.2,0.35,0.7,1),'bottom':(0.15,0.15,0.2,1),'shoes':(0.08,0.06,0.05,1),'eyes':(0.95,0.95,0.95,1),'lashes':(0.02,0.02,0.02,1)}
for m in bpy.data.materials:
    m.diffuse_color=cols.get(m.name.split('.')[0],(0.5,0.5,0.5,1))
arm=[o for o in sc.objects if o.type=='ARMATURE'][0]
# pose: arms down & a step, to test skinning
bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='POSE')
pb=arm.pose.bones
for n,ax,a in []:
    if n in pb: pb[n].rotation_mode='XYZ'; setattr(pb[n],'rotation_euler',(math.radians(a) if ax=='X' else 0, math.radians(a) if ax=='Y' else 0, math.radians(a) if ax=='Z' else 0))
bpy.ops.object.mode_set(mode='OBJECT')
sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='MATERIAL'
sc.render.resolution_x=900; sc.render.resolution_y=700
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam
cam.data.type='ORTHO'; cam.data.ortho_scale=1.1
import mathutils
shots=[]
for i,(yaw) in enumerate([0,90]):
    r=math.radians(yaw); cam.location=(3.5*math.sin(r), -3.5*math.cos(r), 1.25); cam.rotation_euler=(math.radians(90),0,r)
    sc.render.filepath=out.replace('.png',f'_{i}.png'); bpy.ops.render.render(write_still=True)

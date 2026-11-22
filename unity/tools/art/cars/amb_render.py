import bpy, sys, math, os
from mathutils import Vector
D = sys.argv[sys.argv.index('--') + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.join(D, 'ambulance.fbx'))
def tex_mat(m, path=None, col=(0.5, 0.5, 0.5), rough=0.5, metal=0.0, emit=0.0, alpha=1.0):
    m.use_nodes = True; b = m.node_tree.nodes.get('Principled BSDF')
    b.inputs['Base Color'].default_value = (*col, 1); b.inputs['Roughness'].default_value = rough; b.inputs['Metallic'].default_value = metal
    if alpha < 1: b.inputs['Alpha'].default_value = alpha
    if path:
        t = m.node_tree.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(path); m.node_tree.links.new(t.outputs['Color'], b.inputs['Base Color'])
    if emit: b.inputs['Emission Color'].default_value = (*col, 1); b.inputs['Emission Strength'].default_value = emit
for m in bpy.data.materials:
    n = m.name
    if n.startswith('X_livery'): tex_mat(m, os.path.join(D, 'amb_livery.png'), rough=0.35)
    elif n.startswith('X_door'): tex_mat(m, os.path.join(D, 'amb_door.png'), rough=0.4) if not n.startswith('X_doorin') else tex_mat(m, col=(0.55, 0.56, 0.58))
    elif n.startswith('X_dark'): tex_mat(m, col=(0.03, 0.03, 0.035), rough=0.6)
    elif n.startswith('X_chrome'): tex_mat(m, col=(0.8, 0.8, 0.82), rough=0.15, metal=1)
    elif n.startswith('X_glass'): tex_mat(m, col=(0.03, 0.04, 0.05), rough=0.05)
    elif n.startswith('X_lamps'): tex_mat(m, col=(0.9, 0.9, 0.92), rough=0.1)
    elif n.startswith('X_white'): tex_mat(m, col=(0.88, 0.89, 0.9))
    elif n.startswith('X_floor'): tex_mat(m, col=(0.42, 0.43, 0.45))
    elif n.startswith('X_blue'): tex_mat(m, col=(0.1, 0.25, 1.0), emit=3.0)
    elif n.startswith('X_bag2'): tex_mat(m, col=(0.06, 0.36, 0.2))
    elif n.startswith('X_bag'): tex_mat(m, col=(0.08, 0.22, 0.6))
    elif n.startswith('X_wheel'): tex_mat(m, col=(0.25, 0.25, 0.27), rough=0.5, metal=0.5)
    elif n.startswith('X_plate'): tex_mat(m, col=(0.95, 0.9, 0.3))
# open the doors
for o in bpy.data.objects:
    if o.name.startswith('door_L'): o.rotation_euler.z += math.radians(-105) if '--open' in sys.argv else 0
    if o.name.startswith('door_R'): o.rotation_euler.z += math.radians(105) if '--open' in sys.argv else 0
sc = bpy.context.scene; sc.render.engine = 'CYCLES'; sc.cycles.samples = 24; sc.cycles.device = 'CPU'
sc.render.resolution_x = 760; sc.render.resolution_y = 430
w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True; w.node_tree.nodes['Background'].inputs[0].default_value = (0.75, 0.8, 0.88, 1)
bpy.ops.object.light_add(type='SUN', rotation=(math.radians(50), 0, math.radians(30))); bpy.context.active_object.data.energy = 3.5
bpy.ops.mesh.primitive_plane_add(size=60); g = bpy.context.active_object
cam_data = bpy.data.cameras.new('cam'); cam = bpy.data.objects.new('cam', cam_data); sc.collection.objects.link(cam); sc.camera = cam; cam_data.lens = 40
# front faces -Y here (exported orientation inverse) - FBX import brings it back to Blender convention: front -Y
views = {'q': (Vector((5.5, -8.5, 2.4)), Vector((0, 0, 1.3))), 's': (Vector((-13, 0, 1.6)), Vector((0, 0, 1.4))), 'r': (Vector((-3.5, 8.0, 1.8)), Vector((0, 1, 1.3)))}
for k, (pos, tgt) in views.items():
    cam.location = pos; d = tgt - pos; cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    sc.render.filepath = os.path.join(D, f'r_{k}.png'); bpy.ops.render.render(write_still=True)

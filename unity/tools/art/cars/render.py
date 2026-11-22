import bpy, sys, math, importlib, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import carforge, models
importlib.reload(carforge)
from mathutils import Vector
name = sys.argv[sys.argv.index('--') + 1] if '--' in sys.argv else 'hatch90'
out = sys.argv[sys.argv.index('--') + 2] if '--' in sys.argv and len(sys.argv) > sys.argv.index('--') + 2 else '/tmp/car.png'
P = models.MODELS[name]
car, parts = carforge.build(P, (0.55, 0.04, 0.05))
sc = bpy.context.scene
sc.render.engine = 'CYCLES'; sc.cycles.samples = 24; sc.cycles.use_denoising = False; sc.cycles.device = 'CPU'
sc.render.resolution_x = 760; sc.render.resolution_y = 340; sc.cycles.samples = 16
w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True
bg = w.node_tree.nodes['Background']; bg.inputs[0].default_value = (0.75, 0.8, 0.88, 1); bg.inputs[1].default_value = 0.9
bpy.ops.object.light_add(type='SUN', rotation=(math.radians(50), 0, math.radians(30))); bpy.context.active_object.data.energy = 3.5
bpy.ops.mesh.primitive_plane_add(size=40); g = bpy.context.active_object
gm = bpy.data.materials.new('ground'); gm.use_nodes = True; gm.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (0.25, 0.25, 0.25, 1); g.data.materials.append(gm)
cam_data = bpy.data.cameras.new('cam'); cam = bpy.data.objects.new('cam', cam_data); sc.collection.objects.link(cam); sc.camera = cam
cam_data.lens = 50
views = {'q': (Vector((6.2, -4.6, 1.6)), Vector((0, 0, 0.6))), 's': (Vector((0, -9.5, 0.75)), Vector((0, 0, 0.62))), 'r': (Vector((-6.0, -4.2, 1.7)), Vector((0, 0, 0.6)))}
which = sys.argv[sys.argv.index('--') + 3] if '--' in sys.argv and len(sys.argv) > sys.argv.index('--') + 3 else 'qsr'
paint = {'hatch90': (0.5, 0.04, 0.04), 'saloon90': (0.55, 0.56, 0.58), 'jeep': (0.05, 0.15, 0.35), 'boxy80': (0.62, 0.55, 0.42), 'supermini': (0.5, 0.3, 0.1), 'hatch70': (0.85, 0.85, 0.82), 'rally': (0.85, 0.86, 0.87), 'coupe80': (0.85, 0.85, 0.83), 'estate90': (0.02, 0.02, 0.025)}[name]
for o in bpy.data.materials:
    if o.name == 'paint': o.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (*paint, 1)
for k, (pos, tgt) in views.items():
    if k not in which: continue
    cam.location = pos; d = tgt - pos; cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    sc.render.filepath = out.replace('.png', f'_{k}.png'); bpy.ops.render.render(write_still=True)

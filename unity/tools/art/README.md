# Art pipeline (scanned CC0 assets)

All the scanned surfaces and models come from Poly Haven (CC0, free for commercial use, no credit needed).
The big binaries live only in the Unity project on the laptop; they can be re-made from the original
Poly Haven `.blend` zips with these scripts.

## Surfaces (tarmac, slabs, walls, roofs, bark, planks...)
`python3 -I surface_maps.py <folder of *_4k_blend.zip> <out>` makes, per pack:
`<name>_albedo.jpg` (2048), `<name>_normal.png` (OpenGL, as Unity wants), `<name>_mask.png`
(R metal, G 1, A smoothness = 1 - roughness). Copy each set to `Assets/Textures/Surfaces/<name>/`, then run
**ASBO/Art/Apply Surface Textures** in Unity: it sets phone import options, makes `Resources/Surfaces/<name>.mat`,
re-skins the baked Abbots Wend materials and gives the road and pavements metre UVs and concrete kerbs.
Real-world sizes are in `Surfaces.Size` (taken from each scan's own reference plane).

## Models (gnomes, bins, pots, plants, weeds, grass, rocks, boulders, cliffs, firs)
`pip install bpy` (Blender as a Python module), then
`python3 -I blend_to_fbx.py <folder of unzipped packs> <out> [asset,asset]` makes phone-sized FBX files:
two levels of detail per variant (reduced to the triangle budgets in `CFG`), one mesh per level, pivot at the
bottom centre, Y up, plus textures (leaves get colour + alpha in one PNG) and `props.json`.
Copy `<out>` to `Assets/Art/Props/`, then run **ASBO/Art/Build Props**: materials (cut-out, two-sided leaves),
LOD distances by kind, colliders, and rigid bodies + `Knockable` for things you can knock over.
`Dressing.cs` places them round the estate at start-up.

Street furniture from the Drive (Poly Haven, CC0): `barrel_stove`, `concrete_road_barrier(_02)`, `covered_car`,
`electric_stove`, `football`, `industrial_wall_lamp`, `large_iron_gate`, `modular_electricity_poles` (three made-up
poles), `old_tyre`, `rollershutter_window_02` (graffiti), `rusted_wheel_rim_01`, `security_camera_01/02`,
`utility_box_02`, `water_manhole_cover`. Kinds in `props.json`: `prop` (knock it about), `fixture` (solid, stays put),
`wall` (on a wall, no collider), `flat` (in the ground). `Dressing.Street()` places them round town.
Not converted: `modular_fort_01` (647 MB, not the setting), `modular_pipes`, `modular_fire_escape`,
`modular_chainlink_fence`, `large_castle_door` (modular kits that need assembling).

Not converted yet: `island_tree_01` (half a million triangles even at its lowest level: needs a billboard
version for phones) and `moss_01` (tiny geometry strands; better as a texture on rocks).

## Cars (modelled from the reference photos)
`tools/art/cars/`: `models.py` holds each car's measurements (length, width, wheelbase, overhang, wheel size, the
roof/bonnet line, windscreen and rear window positions, pillars, door lines, lamps, bumpers, extras). `carforge.py`
builds the car in Blender: lofted body with tumblehome and rounded corners, recessed glass with rubber surrounds,
cut wheel arches with dark liners, bumpers (body colour, black or chrome with over-riders), lamps and grille,
mirrors, handles, rubbing strips, panel gaps, wipers, roof rails, rally wing and flares, spare wheel, a cabin with
seats and a right-hand-drive dash, and proper wheels (hubcaps, steels, five-spoke, multi-spoke).
`render.py -- <model> <out.png> [qsr]` renders quick previews; `carexport.py -- <out> [model,...]` exports each car:
one merged mesh per level of detail with six materials, a dented version, four wheels, and a baked wear map
(R dirt, G rust, B shading, A panel id). Copy `Art/Cars/*` and `Resources/Cars/*_wear.png` into the project and run
**ASBO/Art/Build Cars**. `CarKit.cs` paints each car at runtime: clean, tired, filthy, rusty with a mismatched
panel, smashed-up wreck, or burnt-out shell.

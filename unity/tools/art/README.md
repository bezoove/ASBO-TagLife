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

Not converted yet: `island_tree_01` (half a million triangles even at its lowest level: needs a billboard
version for phones) and `moss_01` (tiny geometry strands; better as a texture on rocks).

# Council estate art (Hawthorn Way)

`estate_tex.py` makes every texture the estate uses (all generated, own work):

- `Assets/Textures/Surfaces/buff_brick/` yellow-buff stock brick, stretcher bond, 1.8 m repeat (albedo, normal, mask)
- `Assets/Art/Estate/estate_windows.png` uPVC window bands (nets, curtains, blinds), small and patio units; alpha = smoothness
- `Assets/Art/Estate/estate_doors.png` four front doors
- `Assets/Art/Estate/garage_door.png` ribbed up-and-over door (tinted per house)
- `Assets/Art/Estate/sign_*.png` street name plates (made-up names)

```
python3 estate_tex.py <path to unity/Assets>
```

Then in Unity: **ASBO/Art/Build Council Estate** (ASBOEstate.cs). It makes the materials in
Resources/Estate, levels the ground, builds the terraces, roads, lamps and sign from CouncilEstate.cs into the
test scene, plants trees on the green and rebakes the NavMesh. Wheelie bins and parked cars are added at Play.

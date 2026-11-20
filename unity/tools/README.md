# Asset pipeline (offline, Python + Blender-as-module `pip install bpy`)
- charpipe/: CC0 MakeHuman base mesh (makehumancommunity/makehuman data) -> body morphs, outfits/hair from the body surface,
  19+2 bone Humanoid rig (Unity names), LOD0-2 decimation, 2K painted skin -> FBX (`python3 mkchar.py`).
- anim/: CMU mocap BVH (cgspeed MotionBuilder-friendly release, free for commercial use) -> FBX with frame-1 T-pose rest
  (`convert.py`), loop analysis (`loops.py`) -> Assets/Art/Anim/clips.json. Unity's ASBOImport.cs cuts/loops clips and builds the Animator.

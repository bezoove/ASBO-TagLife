# PBR texture packs (Julio Sillet, CC BY 4.0, from the user's Drive) -> maps for Unity URP Lit, the same layout as
# surface_maps.py: <out>/<name>/<name>_albedo.jpg (2048, ambient occlusion multiplied in; .png with alpha when the
# set has an opacity map), <name>_normal.png (OpenGL, 1024), <name>_mask.png (R metal, G 1, A smoothness, 1024).
# Run: python3 pack_maps.py <folder of Materials_*.zip> <Assets/Textures/Surfaces> [name,name,...]
import sys, os, zipfile, io
import numpy as np
from PIL import Image
Image.MAX_IMAGE_PIXELS = None
src, out = sys.argv[1], sys.argv[2]
only = set(sys.argv[3].split(',')) if len(sys.argv) > 3 else None

# name: (zip, folder in the zip, has see-through parts)
SETS = {
    # casino, bars, back rooms
    'carpet_hex': ('Materials_Fabric.zip', 'Fabric_carpet_02', False),
    'carpet_office': ('Materials_Fabric.zip', 'Fabric_carpet_01', False),
    'velvet_red': ('Materials_Fabric.zip', 'Fabric_valvet_01', False),
    'fabric_gold': ('Materials_Fabric.zip', 'Fabric_Craft_01', False),
    'fabric_wool': ('Materials_Fabric.zip', 'Fabric_wool_01', False),
    'leather_quilt': ('Materials_Leather_03.zip', 'Leather_Pattern_01', False),
    'leather_quilt2': ('Materials_Leather_03.zip', 'Leather_Pattern_04', False),
    'leather_worn_red': ('Materials_Leather_03.zip', 'Leather_Worn_01', False),
    'grille_clover': ('Materials_MetalPack_02.zip', 'Metal_Grid_Clover', True),
    'grille_square': ('Materials_MetalPack_02.zip', 'Metal_Grid_Square', True),
    'shutter_roller': ('Materials_MetalPack_02.zip', 'Metal_Shutter_Perfored_Roller', False),
    'shutter_bars': ('Materials_MetalPack_02.zip', 'Metal_Shutter_Cross_Bars', True),
    'parquet_diamond': ('Materials_WoodenFloor_01.zip', 'WoodenFloor_01', False),
    'parquet_cherry': ('Materials_WoodenFloor_01.zip', 'WoodenFloor_03', False),
    'parquet_herring': ('Materials_WoodenFloor_01.zip', 'WoodenFloor_06', False),
    'wood_slats': ('Materials_Wood_VariationsV2.zip', 'Wood Wall Slits Var01', False),
    'wood_slats_dark': ('Materials_Wood_VariationsV2.zip', 'Wood Wall Slits Var03', False),
    'wood_weathered': ('Materials_Wood_VariationsV2.zip', 'Wood 02 Var01', False),
    'wood_painted': ('Materials_Wood_VariationsV2.zip', 'Wood Planks Painted Var01', False),
    'tiles_metro_black': ('Materials_Tiles.zip', 'Tiles_04', False),
    'tiles_metro_green': ('Materials_Tiles.zip', 'Tiles_05', False),
    'tiles_herring_teal': ('Materials_Tiles.zip', 'Tiles_02', False),
    'tiles_victorian': ('Materials_Tiles.zip', 'Tiles_Floor_02', False),
    'tiles_chevron': ('Materials_Tiles.zip', 'Tiles_Floor_01', False),
    'tiles_red': ('Materials_Tiles.zip', 'Tiles_01', False),
    'tiles_grey_wall': ('Materials_Tiles.zip', 'Tiles_Wall_01', False),
    'concrete_floor': ('Materials_Concrete.zip', 'Concrete_Base_01', False),
    'concrete_panels': ('Materials_Concrete.zip', 'Concrete_Panels_01', False),
    'concrete_dark': ('Materials_Concrete.zip', 'Concrete_Dark_01', False),
    'rubber_mat': ('Materials_Tech.zip', 'Plastic_RubberGrid_01', False),
    'rug_brown': ('Materials_MixPack_05.zip', 'Old_Rug_Brown', False),
    'dark_wood_tiles': ('Materials_MixPack_05.zip', 'Dark_Wood_Floor_Tiles', False),
    'tiles_black_deco': ('Materials_MixPack_05.zip', 'Decorative_Black_Tiles', False),
    # the town: infill building fronts, walls, crossings, shop shutters
    'facade_01': ('Materials_Facadebu.zip', 'Facades_01', False), 'facade_02': ('Materials_Facadebu.zip', 'Facades_02', False),
    'facade_03': ('Materials_Facadebu.zip', 'Facades_03', False), 'facade_04': ('Materials_Facadebu.zip', 'Facades_04', False),
    'facade_05': ('Materials_Facadebu.zip', 'Facades_05', False), 'facade_06': ('Materials_Facadebu.zip', 'Facades_06', False),
    'facade_07': ('Materials_Facadebu.zip', 'Facades_07', False), 'facade_08': ('Materials_Facadebu.zip', 'Facades_08', False),
    'facade_09': ('Materials_Facadebu.zip', 'Facades_09', False),
    'brickfront_01': ('Materials_BricksAndWalls.zip', 'BricksWall_01', False), 'brickfront_02': ('Materials_BricksAndWalls.zip', 'BricksWall_02', False),
    'brickfront_03': ('Materials_BricksAndWalls.zip', 'BricksWall_03', False), 'brickfront_04': ('Materials_BricksAndWalls.zip', 'BricksWall_04', False),
    'brickfront_05': ('Materials_BricksAndWalls.zip', 'BricksWall_05', False),
    'red_bricks_01': ('Materials_BricksAndWalls.zip', 'RedBricks_01', False), 'red_bricks_02': ('Materials_BricksAndWalls.zip', 'RedBricks_02', False),
    'concrete_damaged': ('Materials_BricksAndWalls.zip', 'Concrete_DamagedCracked', False),
    'tactile_red': ('Materials_Tactile.zip', 'Tactile Blocks Bricks Dots', False),
    'tactile_buff': ('Materials_Tactile.zip', 'Tactile Blocks Rough Dots', False),
    'tactile_lines': ('Materials_Tactile.zip', 'Tactile Blocks Rough Lines', False),
    'security_gate': ('Materials_MixPack_05.zip', 'Retractable_Security_Door', True),
    'window_grill': ('Materials_MixPack_05.zip', 'Window_Grill', True),
    'hivis_mesh': ('Materials_Tech.zip', 'Tech_Mesh_01', True),
    'sidewalk_01': ('Materisl_PS_01.zip', 'Ground Sidewalk 01', False), 'sidewalk_02': ('Materisl_PS_01.zip', 'Ground Sidewalk 02', False),
    'ground_stones': ('Materisl_PS_01.zip', 'Ground Stones', False),
}
A, N, M = 2048, 1024, 1024
zips = {}
def Z(z):
    if z not in zips: zips[z] = zipfile.ZipFile(os.path.join(src, z))
    return zips[z]
def key(n):
    b = os.path.basename(n).lower().replace(' ', '').replace('_', '')
    return b
def pick(files, *tags, avoid=()):
    for t in tags:
        for f in files:
            k = key(f)
            if t in k and not any(a in k for a in avoid): return f
    return None
def load(zf, f, mode, size):
    im = Image.open(io.BytesIO(zf.read(f))); im.draft(mode, (size, size))
    return im.convert(mode).resize((size, size), Image.LANCZOS)
for name, (z, folder, cut) in SETS.items():
    if only and name not in only: continue
    d = os.path.join(out, name)
    if os.path.exists(os.path.join(d, name + '_mask.png')): continue
    zf = Z(z); files = [f for f in zf.namelist() if f.rstrip('/').split('/')[-2:-1] == [folder] and f.lower().endswith(('.jpg', '.jpeg', '.png'))]
    col = pick(files, 'basecolor', 'albedo', 'diffuse', 'color')
    nor = pick(files, 'normalogl', 'normalopengl', 'normal', avoid=('normaldx',))
    rough = pick(files, 'roughness'); gloss = pick(files, 'glossiness'); metal = pick(files, 'metallic', 'metalness')
    ao = pick(files, 'ambientocclusion', 'ambient', 'occlusion'); op = pick(files, 'opacity') if cut else None
    if col is None or nor is None: print('SKIP', name, files[:8]); continue
    os.makedirs(d, exist_ok=True)
    a = np.asarray(load(zf, col, 'RGB', A), np.float32)
    if ao: a *= (0.45 + 0.55 * np.asarray(load(zf, ao, 'L', A), np.float32) / 255.0)[..., None]
    a = np.clip(a, 0, 255).astype(np.uint8)
    if op:
        alpha = np.asarray(load(zf, op, 'L', A))
        Image.fromarray(np.dstack([a, alpha]), 'RGBA').save(os.path.join(d, name + '_albedo.png'), optimize=True)
    else:
        Image.fromarray(a).save(os.path.join(d, name + '_albedo.jpg'), quality=90)
    load(zf, nor, 'RGB', N).save(os.path.join(d, name + '_normal.png'), optimize=True)
    if rough: sm = 1.0 - np.asarray(load(zf, rough, 'L', M), np.float32) / 255.0
    elif gloss: sm = np.asarray(load(zf, gloss, 'L', M), np.float32) / 255.0
    else: sm = np.full((M, M), 0.25, np.float32)
    mt = np.asarray(load(zf, metal, 'L', M), np.float32) / 255.0 if metal else np.zeros((M, M), np.float32)
    mask = np.zeros((M, M, 4), np.uint8); mask[..., 0] = (mt * 255).astype(np.uint8); mask[..., 1] = 255; mask[..., 3] = (np.clip(sm, 0, 1) * 255).astype(np.uint8)
    Image.fromarray(mask, 'RGBA').save(os.path.join(d, name + '_mask.png'), optimize=True)
    print('OK', name, os.path.basename(col), os.path.basename(nor), 'cut' if op else '')

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using ASBO.Slice;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace ASBO.EditorTools
{
    /// <summary>
    /// The editor side of the new parts of town, called from ASBO/Art/Build Council Estate: imports the generated
    /// textures (tools/art/town), makes the Resources/Town materials, deepens the terrain so the river can be cut into
    /// it (once), levels the ground for the new roads and areas, carves the river channel and banks, clears trees and
    /// long grass off the roads and out of the water, and keeps people out of the river on the NavMesh.
    /// </summary>
    public static class ASBOTown
    {
        const string Tex = "Assets/Art/Town", MatDir = "Assets/Resources/Town";
        public const float TerrainBase = -6.02f, TerrainHeight = 30f;

        public static void Materials()
        {
            Directory.CreateDirectory(MatDir);
            if (Directory.Exists(Tex))
                foreach (var f in Directory.GetFiles(Tex, "*.png"))
                {
                    var path = f.Replace('\\', '/'); var imp = AssetImporter.GetAtPath(path) as TextureImporter; if (imp == null) continue;
                    string n = Path.GetFileNameWithoutExtension(path);
                    bool normal = n.EndsWith("_normal"), alpha = n == "reeds" || n == "graffiti" || n == "chainlink" || n == "tower_windows", lamps = n == "signal_lamps";
                    imp.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    imp.sRGBTexture = !normal; imp.mipmapEnabled = true; imp.filterMode = FilterMode.Trilinear; imp.anisoLevel = n.Contains("brick") || n.Contains("pebble") ? 4 : 2;
                    imp.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None; imp.alphaIsTransparency = alpha && n != "tower_windows";
                    imp.wrapMode = n == "town_signs" || n == "signal_lamps" || n == "tower_windows" || n == "graffiti" || n == "notice" ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                    int max = n == "town_signs" || n == "tower_windows" || n == "graffiti" ? 1024 : n.Contains("water") ? 512 : 1024;
                    imp.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = max, format = TextureImporterFormat.ASTC_6x6 });
                    imp.SaveAndReimport();
                }
            Texture2D T(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{Tex}/{n}.png");
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            Material Mat(string name, System.Action<Material> set, Shader sh = null, Material from = null)
            {
                string p = $"{MatDir}/{name}.mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (m == null) { m = from != null ? new Material(from) : new Material(sh ?? lit); AssetDatabase.CreateAsset(m, p); }
                else if (from != null) m.CopyPropertiesFromMaterial(from);
                if (sh != null) m.shader = sh; else if (from == null) m.shader = lit;
                set?.Invoke(m); EditorUtility.SetDirty(m); return m;
            }
            void Plain(string n, Color c, float smooth, float metal = 0f, Texture2D tex = null) => Mat(n, m => { m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal); if (tex != null) { m.SetTexture("_BaseMap", tex); m.mainTexture = tex; } });
            void Surf(string n, string surface, Color tint) { var src = Surfaces.Source(surface); if (src != null) Mat(n, m => m.SetColor("_BaseColor", tint), null, src); else Plain(n, tint * 0.6f, 0.2f); }
            void Cut(string n, Texture2D tex, Color c, float smooth, bool twoSided)
            {
                Mat(n, m =>
                {
                    m.SetTexture("_BaseMap", tex); m.mainTexture = tex; m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", 0f);
                    m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.45f); m.EnableKeyword("_ALPHATEST_ON"); m.SetFloat("_Cull", twoSided ? 0f : 2f); m.renderQueue = 2450; m.SetOverrideTag("RenderType", "TransparentCutout");
                });
            }
            void Textured(string n, string tex, string nrm, Color tint, float smooth, float bump = 1f)
            {
                Mat(n, m =>
                {
                    var t = T(tex); m.SetTexture("_BaseMap", t); m.mainTexture = t; m.SetColor("_BaseColor", tint); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", 0f);
                    var nm = nrm != null ? T(nrm) : null; if (nm != null) { m.SetTexture("_BumpMap", nm); m.SetFloat("_BumpScale", bump); m.EnableKeyword("_NORMALMAP"); }
                });
            }
            Mat("TownSignal", m => { var t = T("signal_lamps"); m.SetTexture("_MainTex", t); m.SetFloat("_Glow", 2.2f); m.SetFloat("_Off", 0.1f); }, Shader.Find("ASBO/Signal"));
            Plain("TownSignalHead", new Color(0.035f, 0.035f, 0.035f), 0.45f);
            Plain("TownPole", new Color(0.52f, 0.54f, 0.55f), 0.45f, 0.6f);
            Plain("TownYellow", new Color(0.95f, 0.74f, 0.08f), 0.15f);
            Plain("TownSigns", Color.white, 0.3f, 0f, T("town_signs"));
            Mat("TownBeacon", m => { m.SetColor("_BaseColor", new Color(1f, 0.6f, 0.15f)); m.SetColor("_EmissionColor", new Color(1f, 0.55f, 0.1f) * 1.5f); m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; });
            Plain("TownSteel", new Color(0.62f, 0.64f, 0.66f), 0.55f, 0.7f);
            Mat("TownGlass", m =>
            {
                m.SetColor("_BaseColor", new Color(0.62f, 0.72f, 0.76f, 0.3f)); m.SetFloat("_Smoothness", 0.92f); m.SetFloat("_Metallic", 0f);
                m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_ZWrite", 0f); m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000; m.SetOverrideTag("RenderType", "Transparent");
            });
            Plain("TownGlassDark", new Color(0.07f, 0.09f, 0.11f), 0.92f);
            Surf("TownStone", "old_stone_wall", new Color(1.0f, 0.98f, 0.94f));
            Surf("TownGravel", "rocky_trail", new Color(0.95f, 0.9f, 0.82f));
            Surf("TownMud", "dry_riverbed_rock", new Color(0.7f, 0.66f, 0.58f));
            Mat("TownWater", m =>
            {
                m.SetTexture("_Ripples", T("water_normal")); m.SetTexture("_Foam", T("water_foam"));
                m.SetColor("_Deep", new Color(0.06f, 0.1f, 0.085f, 0.94f)); m.SetColor("_Shallow", new Color(0.22f, 0.27f, 0.19f, 0.72f));
                m.SetFloat("_Flow", 0.4f); m.SetFloat("_Scale", 0.16f); m.SetFloat("_Bump", 0.22f); m.SetFloat("_Glint", 0.9f); m.SetFloat("_Reflect", 0.55f);
            }, Shader.Find("ASBO/Water"));
            Cut("TownReeds", T("reeds"), new Color(0.85f, 0.9f, 0.8f), 0.1f, true);
            Textured("TownBrownBrick", "brown_brick", "brown_brick_normal", Color.white, 0.12f);
            Surf("TownRedBrick", "red_brick_vic", Color.white);
            Textured("TownPebble", "pebbledash", "pebbledash_normal", Color.white, 0.05f, 1.2f);
            Surf("TownPanel", "concrete_layers", new Color(1.25f, 1.24f, 1.2f));
            Surf("TownPanelDark", "concrete_layers", new Color(0.82f, 0.82f, 0.8f));
            Mat("TownTowerWindows", m =>
            {
                var t = T("tower_windows"); m.SetTexture("_BaseMap", t); m.mainTexture = t; m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Metallic", 0f); m.SetFloat("_Smoothness", 1f); m.SetFloat("_SmoothnessTextureChannel", 1f); m.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            });
            Textured("TownDeckBrick0", "brown_brick", "brown_brick_normal", new Color(1.35f, 0.82f, 0.72f), 0.12f);
            Textured("TownDeckBrick1", "brown_brick", "brown_brick_normal", new Color(1.45f, 1.0f, 0.74f), 0.12f);
            Textured("TownDeckBrick2", "brown_brick", "brown_brick_normal", new Color(1.5f, 1.2f, 0.9f), 0.12f);
            Textured("TownDeckBrick3", "brown_brick", "brown_brick_normal", new Color(1.65f, 1.45f, 1.05f), 0.12f);
            Surf("TownRender", "plaster_smooth", new Color(0.9f, 0.87f, 0.8f));
            Surf("TownRoofTile", "roof_slates_03", new Color(0.62f, 0.56f, 0.52f));
            Cut("TownGraffiti", T("graffiti"), Color.white, 0.25f, false);
            Cut("TownChainlink", T("chainlink"), Color.white, 0.4f, true);
            Plain("TownNotice", Color.white, 0.2f, 0f, T("notice"));
            Surf("TownMetalSheet", "metal_plate", new Color(0.85f, 0.86f, 0.88f));
            Plain("TownRust", new Color(0.42f, 0.2f, 0.09f), 0.25f, 0.3f);
            Plain("TownTarmacRed", new Color(0.6f, 0.18f, 0.14f), 0.2f);
            Surf("TownLobbyFloor", "hangar_concrete_floor", new Color(0.8f, 0.8f, 0.78f));
            Plain("TownLobbyWall", new Color(0.78f, 0.8f, 0.7f), 0.35f);
            Plain("TownLiftDoor", new Color(0.72f, 0.74f, 0.76f), 0.55f, 0.85f);
            Plain("TownLino", new Color(0.36f, 0.4f, 0.37f), 0.35f);
            Plain("TownCourtGreen", new Color(0.18f, 0.34f, 0.22f), 0.12f);
            Plain("TownPlaySoft", new Color(0.62f, 0.24f, 0.17f), 0.05f);
            Plain("TownBench", new Color(0.1f, 0.1f, 0.11f), 0.4f, 0.4f);
            Mat("TownHiVis", m => { m.SetColor("_BaseColor", new Color(0.86f, 0.96f, 0.12f)); m.SetColor("_EmissionColor", new Color(0.25f, 0.3f, 0.02f)); m.EnableKeyword("_EMISSION"); m.SetFloat("_Smoothness", 0.3f); });
            Plain("TownRedPaint", new Color(0.72f, 0.06f, 0.05f), 0.55f, 0.1f);
            Plain("TownNhsBlue", new Color(0.0f, 0.37f, 0.72f), 0.45f);
            Mat("TownBlueLamp", m => { m.SetColor("_BaseColor", new Color(0.25f, 0.4f, 1f)); m.SetColor("_EmissionColor", new Color(0.2f, 0.35f, 1f) * 1.5f); m.EnableKeyword("_EMISSION"); });
            Plain("TownTyre", new Color(0.05f, 0.05f, 0.05f), 0.25f);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Once: lower the terrain's base to 6 m under the town (and give it a little more height) so the river can be cut into it. Heights are kept where they were.</summary>
        public static void Deepen(Terrain t)
        {
            var td = t.terrainData; var tp = t.transform.position;
            if (tp.y < -1f) return;
            float oldBase = tp.y, oldH = td.size.y; int res = td.heightmapResolution; var h = td.GetHeights(0, 0, res, res);
            for (int y = 0; y < res; y++) for (int x = 0; x < res; x++) h[y, x] = Mathf.Clamp01((oldBase + h[y, x] * oldH - TerrainBase) / TerrainHeight);
            td.size = new Vector3(td.size.x, TerrainHeight, td.size.z);
            t.transform.position = new Vector3(tp.x, TerrainBase, tp.z);
            td.SetHeights(0, 0, h);
            EditorUtility.SetDirty(td); EditorUtility.SetDirty(t);
            Debug.Log("[ASBO] town: terrain deepened (base " + TerrainBase + " m) so the river can be cut in");
        }

        static float RectDist(Rect r, float x, float z) { float dx = Mathf.Max(0f, Mathf.Max(r.xMin - x, x - r.xMax)), dz = Mathf.Max(0f, Mathf.Max(r.yMin - z, z - r.yMax)); return Mathf.Sqrt(dx * dx + dz * dz); }

        /// <summary>Level the ground under the new roads and areas, then cut the river, then clear trees and long grass.</summary>
        public static void Ground(Terrain t)
        {
            var td = t.terrainData; Vector3 tp = t.transform.position, size = td.size;
            int res = td.heightmapResolution; var h = td.GetHeights(0, 0, res, res);
            float zero = -tp.y / size.y;
            int carved = 0;
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float wx = tp.x + x / (float)(res - 1) * size.x, wz = tp.z + y / (float)(res - 1) * size.z; var p = new Vector3(wx, 0, wz);
                    // roads: a level corridor, eased back into the fields
                    float rd = TownPlan.RoadDist(p, out _) - (TownPlan.Half + TownPlan.KerbW + TownPlan.Pave + 1.5f);
                    float k = rd <= 0f ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, rd / 14f));
                    // the new areas: flat, with a falloff
                    float ad = float.MaxValue; foreach (var r in TownPlan.Levelled) ad = Mathf.Min(ad, RectDist(r, wx, wz));
                    k = Mathf.Min(k, ad <= 0f ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, ad / 22f)));
                    // the river: level fields either side, then the channel and banks cut into them
                    if (wz > 150f)
                    {
                        float dRiver = TownPlan.RiverDist(p, out float s, out _); float hw = TownPlan.RiverHalfWidth(s);
                        float lv = dRiver - (hw + 10f); k = Mathf.Min(k, lv <= 0f ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, lv / 20f)));
                        h[y, x] = Mathf.Lerp(zero, h[y, x], k);
                        float g = TownPlan.RiverGround(p);
                        if (g < 0f) { h[y, x] = Mathf.Min(h[y, x], Mathf.Clamp01((g - tp.y) / size.y)); carved++; }
                        continue;
                    }
                    h[y, x] = Mathf.Lerp(zero, h[y, x], k);
                }
            td.SetHeights(0, 0, h);
            // trees: out of the roads, the new areas and the river
            var keep = new List<TreeInstance>();
            foreach (var ti in td.treeInstances)
            {
                float wx = tp.x + ti.position.x * size.x, wz = tp.z + ti.position.z * size.z; var p = new Vector3(wx, 0, wz);
                if (TownPlan.RoadDist(p, out _) < TownPlan.Half + TownPlan.KerbW + TownPlan.Pave + 3f) continue;
                bool inArea = false; foreach (var r in TownPlan.Levelled) if (RectDist(r, wx, wz) < 3f) inArea = true; if (inArea) continue;
                if (wz > 150f) { float dr = TownPlan.RiverDist(p, out float s, out _); if (dr < TownPlan.RiverHalfWidth(s) + 11f) continue; }
                keep.Add(ti);
            }
            td.treeInstances = keep.ToArray();
            // trees sit on the ground as it now is
            var trees = td.treeInstances;
            for (int i = 0; i < trees.Length; i++) { var p = trees[i].position; p.y = td.GetInterpolatedHeight(p.x, p.z) / size.y; trees[i].position = p; }
            td.treeInstances = trees;
            // long grass off the roads and the towpath, and out of the river
            int dr2 = td.detailResolution;
            for (int layer = 0; layer < td.detailPrototypes.Length; layer++)
            {
                var map = td.GetDetailLayer(0, 0, dr2, dr2, layer);
                for (int y = 0; y < dr2; y++)
                    for (int x = 0; x < dr2; x++)
                    {
                        if (map[y, x] == 0) continue;
                        float wx = tp.x + (x + 0.5f) / dr2 * size.x, wz = tp.z + (y + 0.5f) / dr2 * size.z; var p = new Vector3(wx, 0, wz);
                        if (TownPlan.RoadDist(p, out _) < TownPlan.Half + TownPlan.KerbW + TownPlan.Pave + 0.6f) { map[y, x] = 0; continue; }
                        if (wz > 150f) { float dr = TownPlan.RiverDist(p, out float s, out float side); float hw = TownPlan.RiverHalfWidth(s); if (dr < hw + 0.5f || side < 0 && Mathf.Abs(dr - (hw + 8.2f)) < 1.6f) map[y, x] = 0; }
                    }
                td.SetDetailLayer(0, 0, layer, map);
            }
            EditorUtility.SetDirty(td);
            Debug.Log($"[ASBO] town: ground levelled for {TownPlan.Roads.Length} roads and {TownPlan.Levelled.Length} areas, river cut ({carved} terrain samples), {keep.Count} trees left");
        }

        /// <summary>The river is no place to walk: NavMesh modifier volumes over the water along its length.</summary>
        public static void RiverNav(Transform riverRoot)
        {
            if (riverRoot == null) return;
            var r = TownPlan.River; int area = NavMesh.GetAreaFromName("Not Walkable"); float acc = 0f, s = 0f;
            for (int i = 1; i < r.Count; i++)
            {
                float seg = (r[i] - r[i - 1]).magnitude; acc += seg; s += seg;
                if (acc < 10f && i < r.Count - 1) continue;
                Vector3 c = r[i], d = (r[i] - r[Mathf.Max(0, i - 4)]).WithY(0).normalized; float hw = TownPlan.RiverHalfWidth(s);
                var g = new GameObject("NoWalk"); g.transform.SetParent(riverRoot, false); g.transform.position = new Vector3(c.x, -2f, c.z) - d * (acc * 0.5f); g.transform.rotation = Quaternion.LookRotation(d);
                var v = g.AddComponent<NavMeshModifierVolume>(); v.size = new Vector3(2f * (hw - 0.6f), 6f, acc + 1f); v.center = Vector3.zero; v.area = area;
                acc = 0f;
            }
        }
    }
}
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ASBO.Slice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ASBO.EditorTools
{
    /// <summary>
    /// Puts the scanned surface textures (Assets/Textures/Surfaces/&lt;name&gt;/) to work: sets their import options
    /// for phones, makes a URP Lit material for each in Resources/Surfaces, re-skins the baked Abbots Wend materials
    /// (road, pavements, stone and limewashed walls, plaster, brick, slate, flagstones, gravel) and gives the road and
    /// pavements metre UVs plus proper concrete kerbs. Run from ASBO/Art/Apply Surface Textures (or the job runner).
    /// </summary>
    public static class ASBOSurfaces
    {
        const string TexRoot = "Assets/Textures/Surfaces", MatRoot = "Assets/Resources/Surfaces", Gen = "Assets/Art/Generated";
        const string TestScene = "Assets/Scenes/AbbotsWend_Test.unity";

        [MenuItem("ASBO/Art/Apply Surface Textures")]
        public static void Apply()
        {
            int n = ImportAll();
            var mats = MakeMaterials();
            Debug.Log($"[ASBO] surfaces: {n} textures set up, {mats.Count} materials in {MatRoot}");
            Reskin();
            Tangents();
            DressScene(TestScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[ASBO] surfaces: done");
        }

        /// <summary>Just the texture import settings and the Resources/Surfaces materials (no scene changes).</summary>
        public static void ImportAndMake() { ImportAll(); MakeMaterials(); AssetDatabase.SaveAssets(); }

        // ------------------------------------------------------------ import settings
        static int ImportAll()
        {
            int n = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var dir in Directory.GetDirectories(TexRoot))
                    foreach (var f in Directory.GetFiles(dir))
                    {
                        if (f.EndsWith(".meta")) continue;
                        var path = f.Replace('\\', '/');
                        if (!(AssetImporter.GetAtPath(path) is TextureImporter imp)) continue;
                        bool normal = path.Contains("_normal."), mask = path.Contains("_mask.");
                        imp.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                        imp.sRGBTexture = !normal && !mask;
                        bool cut = path.EndsWith("_albedo.png");   // see-through sets (grilles, gates, mesh) keep their alpha
                        imp.alphaSource = mask || cut ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None; imp.alphaIsTransparency = cut;
                        imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Repeat; imp.anisoLevel = 4;
                        imp.filterMode = FilterMode.Trilinear;
                        imp.maxTextureSize = normal || mask ? 1024 : 2048;
                        imp.textureCompression = TextureImporterCompression.Compressed;
                        imp.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                        {
                            name = "Android", overridden = true, maxTextureSize = normal || mask ? 512 : 1024,
                            format = TextureImporterFormat.ASTC_6x6, compressionQuality = 50
                        });
                        imp.SaveAndReimport(); n++;
                    }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            return n;
        }

        // ------------------------------------------------------------ materials
        static Dictionary<string, Material> MakeMaterials()
        {
            Directory.CreateDirectory(MatRoot);
            var made = new Dictionary<string, Material>();
            foreach (var dir in Directory.GetDirectories(TexRoot))
            {
                string name = Path.GetFileName(dir), path = $"{MatRoot}/{name}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(Gfx.Lit) { name = name }; AssetDatabase.CreateAsset(m, path); }
                Skin(m, name, 1f, Color.white, 1f);
                EditorUtility.SetDirty(m); made[name] = m;
            }
            return made;
        }

        static Texture2D T(string name, string kind) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexRoot}/{name}/{name}_{kind}.{(kind == "albedo" ? "jpg" : "png")}") ?? (kind == "albedo" ? AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexRoot}/{name}/{name}_albedo.png") : null);

        /// <summary>Turns any material into the named surface: colour, normal and metal/smoothness maps, real-size tiling.</summary>
        static void Skin(Material m, string surf, float scale, Color tint, float bump)
        {
            var a = T(surf, "albedo"); var nm = T(surf, "normal"); var mk = T(surf, "mask");
            if (a == null) { Debug.LogWarning("[ASBO] surfaces: missing " + surf); return; }
            if (m.shader != Gfx.Lit) m.shader = Gfx.Lit;
            m.SetTexture("_BaseMap", a); m.SetTexture("_MainTex", a);
            m.SetTextureScale("_BaseMap", Vector2.one * scale); m.SetTextureScale("_MainTex", Vector2.one * scale);
            m.SetColor("_BaseColor", tint); m.SetColor("_Color", tint);
            if (nm != null) { m.SetTexture("_BumpMap", nm); m.SetFloat("_BumpScale", bump); m.EnableKeyword("_NORMALMAP"); }
            if (mk != null)
            {
                m.SetTexture("_MetallicGlossMap", mk); m.EnableKeyword("_METALLICSPECGLOSSMAP");
                m.SetFloat("_SmoothnessTextureChannel", 0f); m.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                m.SetFloat("_Smoothness", 1f); m.SetFloat("_Metallic", 1f);
            }
            // large soft blotches over the top (a second, much bigger tiling) so you don't see the same patch repeat
            var macro = Macro();
            if (macro != null && Surfaces.Size.TryGetValue(surf, out var size))
            {
                m.SetTexture("_DetailAlbedoMap", macro); m.SetTextureScale("_DetailAlbedoMap", Vector2.one * (scale * size / Surfaces.MacroMetres));
                m.SetFloat("_DetailAlbedoMapScale", 1f); m.EnableKeyword("_DETAIL_MULX2");
            }
            // see-through sets: cut out by the albedo's alpha, both sides drawn
            bool cut = AssetDatabase.GetAssetPath(a).EndsWith(".png");
            m.SetFloat("_AlphaClip", cut ? 1f : 0f); m.SetFloat("_Cutoff", 0.5f); m.SetFloat("_Cull", cut ? 0f : 2f);
            if (cut) { m.EnableKeyword("_ALPHATEST_ON"); m.renderQueue = 2450; m.SetOverrideTag("RenderType", "TransparentCutout"); }
            else { m.DisableKeyword("_ALPHATEST_ON"); if (m.renderQueue == 2450) m.renderQueue = -1; }
            m.enableInstancing = true;
        }

        const string MacroPath = "Assets/Textures/surface_macro.png";
        static Texture2D Macro()
        {
            if (!(AssetImporter.GetAtPath(MacroPath) is TextureImporter imp)) return null;
            if (!imp.sRGBTexture || imp.wrapMode != TextureWrapMode.Repeat || imp.maxTextureSize != 512)
            {
                imp.sRGBTexture = true; imp.wrapMode = TextureWrapMode.Repeat; imp.mipmapEnabled = true; imp.maxTextureSize = 512;
                imp.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = 256, format = TextureImporterFormat.ASTC_6x6 });
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(MacroPath);
        }

        // ------------------------------------------------------------ baked Abbots Wend materials
        struct Rule { public Texture2D was; public string surf; public float tile; public bool keepTint; public Color tint; public float bump; public string what; }

        /// <summary>
        /// Finds each baked material by comparing its texture with the one the cottage code makes (the bake saved copies
        /// of those), then swaps in the real surface at the same real-world size.
        /// </summary>
        static void Reskin()
        {
            // the bake saved copies of the cottage code's own procedural textures: make those again to match against
            Surfaces.Off = true; CotMats.Reset(); CotMats.Init();
            var lime = CotMats.Lime(Color.white);
            Texture2D Of(Material m) => m.GetTexture("_BaseMap") as Texture2D;
            var rules = new List<Rule>
            {
                new Rule { was = Of(CotMats.stone), surf = "rustic_stone_wall_02", tile = 2.2f, tint = new Color(0.95f, 0.93f, 0.9f), bump = 1f, what = "stone walls" },
                new Rule { was = Of(lime), surf = "limewash_stone", tile = 2.2f, keepTint = true, bump = 0.7f, what = "limewashed walls" },
                new Rule { was = Of(CotMats.plaster), surf = "plaster_smooth", tile = 1.5f, keepTint = true, bump = 0.8f, what = "plaster" },
                new Rule { was = Of(CotMats.flag), surf = "large_grey_tiles", tile = 2.4f, tint = new Color(0.96f, 0.95f, 0.93f), bump = 1f, what = "flagstones" },
                new Rule { was = Of(CotMats.brick), surf = "red_brick_plaster_patch_02", tile = 1.2f, tint = Color.white, bump = 1f, what = "brick" },
                new Rule { was = Of(CotMats.slate), surf = "roof_slates_03", tile = 1.6f, tint = new Color(0.9f, 0.92f, 0.96f), bump = 1f, what = "slate roofs" },
                new Rule { was = Of(CotMats.gravel), surf = "rocky_trail", tile = 2f, tint = Color.white, bump = 1f, what = "gravel" },
                new Rule { was = Of(CotMats.soil), surf = "rocky_trail", tile = 1f, tint = new Color(0.5f, 0.38f, 0.28f), bump = 0.8f, what = "flower-bed soil" },
            };
            var sigs = rules.Select(r => Sig(r.was)).ToArray();
            Surfaces.Off = false; CotMats.Reset();
            int done = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { Gen + "/Materials" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                var tex = m.GetTexture("_BaseMap") as Texture2D; if (tex == null) continue;
                var tp = AssetDatabase.GetAssetPath(tex);
                // already re-skinned on an earlier run: re-apply the same rule (sizes may have changed)
                string tag = m.GetTag("ASBOSurface", false, "");
                if (tag == "" && tp.StartsWith(TexRoot + "/"))
                {
                    string surf = Path.GetFileName(Path.GetDirectoryName(tp));
                    var c = m.GetColor("_BaseColor");
                    tag = surf == "plaster_smooth" && c.grayscale < 0.4f ? "flower-bed soil" : rules.FirstOrDefault(q => q.surf == surf).what ?? "";
                }
                if (tag != "")
                {
                    int ri = rules.FindIndex(q => q.what == tag); if (ri < 0) continue;
                    var rr = rules[ri];
                    var keep = m.GetColor("_BaseColor");
                    Skin(m, rr.surf, rr.tile / Surfaces.Size[rr.surf], rr.keepTint ? keep : rr.tint, rr.bump);
                    m.SetOverrideTag("ASBOSurface", rr.what); EditorUtility.SetDirty(m); done++;
                    Debug.Log($"[ASBO] surfaces: {Path.GetFileName(path)} -> {rr.surf} ({rr.what}, again)");
                    continue;
                }
                if (!tp.StartsWith(Gen + "/Textures/")) continue;
                var baked = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                if (!baked.LoadImage(File.ReadAllBytes(tp))) continue;
                var s = Sig(baked); Object.DestroyImmediate(baked);
                int best = -1; float bd = 1e9f;
                for (int i = 0; i < rules.Count; i++) { if (sigs[i] == null || sigs[i].Length != s.Length) continue; float d = Dist(s, sigs[i]); if (d < bd) { bd = d; best = i; } }
                if (best < 0 || bd > 0.004f) continue;
                var r = rules[best];
                var tint = r.keepTint ? m.GetColor("_BaseColor") : r.tint;
                Skin(m, r.surf, r.tile / Surfaces.Size[r.surf], tint, r.bump);
                m.SetOverrideTag("ASBOSurface", r.what);
                EditorUtility.SetDirty(m); done++;
                Debug.Log($"[ASBO] surfaces: {Path.GetFileName(path)} -> {r.surf} ({r.what}, match {bd:0.0000})");
            }
            Debug.Log($"[ASBO] surfaces: re-skinned {done} baked materials");
        }

        /// <summary>A small colour fingerprint of a texture (its 8x8 mip), for matching a baked copy to its source.</summary>
        static Color[] Sig(Texture2D t)
        {
            if (t == null) return null;
            Color32[] px; try { px = t.GetPixels32(0); } catch { return null; }
            var o = new Color[64]; int w = t.width, h = t.height;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) { var c = px[y * w + x]; int i = (y * 8 / h) * 8 + x * 8 / w; o[i].r += c.r; o[i].g += c.g; o[i].b += c.b; o[i].a += 1; }
            for (int i = 0; i < 64; i++) { float k = 1f / (255f * Mathf.Max(1f, o[i].a)); o[i] = new Color(o[i].r * k, o[i].g * k, o[i].b * k, 1f); }
            return o;
        }
        static float Dist(Color[] a, Color[] b) { float d = 0; for (int i = 0; i < a.Length; i++) d += Mathf.Abs(a[i].r - b[i].r) + Mathf.Abs(a[i].g - b[i].g) + Mathf.Abs(a[i].b - b[i].b); return d / (a.Length * 3); }

        /// <summary>Normal maps need tangents; the baked meshes were saved without them.</summary>
        static void Tangents()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Mesh", new[] { Gen + "/Meshes" }))
            {
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));
                if (mesh == null || mesh.tangents.Length == mesh.vertexCount || mesh.uv.Length == 0) continue;
                mesh.RecalculateTangents(); EditorUtility.SetDirty(mesh); n++;
            }
            Debug.Log($"[ASBO] surfaces: tangents added to {n} meshes");
        }

        // ------------------------------------------------------------ road, pavements, kerbs
        static void DressScene(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(Gen + "/Meshes/Surf");
            var meshes = new Dictionary<string, Mesh>();
            Mesh MeshFor(Vector3 s)
            {
                string key = $"{s.x:0.###}x{s.y:0.###}x{s.z:0.###}";
                if (meshes.TryGetValue(key, out var have)) return have;
                string p = $"{Gen}/Meshes/Surf/MetreCube_{key}.asset";
                var old = AssetDatabase.LoadAssetAtPath<Mesh>(p); if (old != null) AssetDatabase.DeleteAsset(p);
                var m = Surfaces.MetreCube(s); AssetDatabase.CreateAsset(m, p); meshes[key] = m; return m;
            }
            var all = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Material road = null, pave = null;
            foreach (var r in all)
            {
                if (r.name != "LongRoad" && r.name != "Approach" && r.name != "Pavement") continue;
                r.GetComponent<MeshFilter>().sharedMesh = MeshFor(r.transform.lossyScale);
                if (r.name == "Pavement") pave = r.sharedMaterial; else road = r.sharedMaterial;
            }
            if (road != null) { Skin(road, "asphalt_02", 1f / Surfaces.Size["asphalt_02"], new Color(0.78f, 0.78f, 0.8f), 1f); EditorUtility.SetDirty(road); }
            if (pave != null) { Skin(pave, "large_grey_tiles", 1f / Surfaces.Size["large_grey_tiles"], new Color(0.9f, 0.9f, 0.88f), 1f); EditorUtility.SetDirty(pave); }

            // concrete kerb stones along the road edge of each pavement
            var world = GameObject.Find("World");
            foreach (var old in all) if (old != null && old.name == "Kerb") Object.DestroyImmediate(old.gameObject);
            string kp = MatRoot + "/kerb.mat";
            var kerb = AssetDatabase.LoadAssetAtPath<Material>(kp);
            if (kerb == null) { kerb = new Material(Gfx.Lit) { name = "kerb" }; AssetDatabase.CreateAsset(kerb, kp); }
            Skin(kerb, "concrete_layers", 1f / Surfaces.Size["concrete_layers"], new Color(1.25f, 1.24f, 1.22f), 0.8f);
            EditorUtility.SetDirty(kerb);
            foreach (var r in all)
            {
                if (r == null || r.name != "Pavement") continue;
                var t = r.transform; Vector3 s = t.lossyScale, c = t.position;
                float side = Mathf.Sign(c.z - World.RoadZ);
                var ks = new Vector3(s.x, s.y + 0.01f, 0.16f);
                var kc = new Vector3(c.x, (s.y + 0.01f) * 0.5f + (c.y - s.y * 0.5f), c.z - side * (s.z * 0.5f - 0.08f));
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = "Kerb";
                Object.DestroyImmediate(g.GetComponent<Collider>());
                g.transform.SetParent(world != null ? world.transform : null, true);
                g.transform.position = kc; g.transform.localScale = ks;
                g.GetComponent<MeshFilter>().sharedMesh = MeshFor(ks);
                var mr = g.GetComponent<MeshRenderer>(); mr.sharedMaterial = kerb;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveGI = ReceiveGI.LightProbes;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ASBO] surfaces: {Path.GetFileName(scenePath)} road, pavements and kerbs done");
        }
    }
}
#endif

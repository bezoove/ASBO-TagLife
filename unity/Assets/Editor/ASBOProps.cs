#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using ASBO.Slice;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.EditorTools
{
    /// <summary>
    /// Turns the scanned CC0 models in Assets/Art/Props (converted from the .blend packs into phone-sized FBX with two
    /// levels of detail, plus their textures and props.json) into ready prefabs in Resources/Props: URP Lit materials
    /// (cut-out and two-sided for leaves), LOD distances by kind, colliders, and rigid bodies for things you can knock
    /// over (bins, gnomes, pots). Run from ASBO/Art/Build Props.
    /// </summary>
    public static class ASBOProps
    {
        const string Root = "Assets/Art/Props", Out = "Assets/Resources/Props";

        [Serializable] class PropsFile { public PAsset[] assets; }
        [Serializable] class PAsset { public string name, kind; public PVar[] variants; public PMat[] materials; }
        [Serializable] class PVar { public string name, fbx; public int tris0, tris1; }
        [Serializable] class PMat { public string key, name, albedo, normal, mask; public bool alpha; }

        [MenuItem("ASBO/Art/Build Props")]
        public static void Build()
        {
            var pf = JsonUtility.FromJson<PropsFile>(File.ReadAllText(Root + "/props.json"));
            Directory.CreateDirectory(Out);
            // 1) textures
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var a in pf.assets)
                {
                    bool big = a.name.Contains("cliff") || a.name.Contains("mountainside") || a.name.Contains("rock_moss");
                    foreach (var m in a.materials)
                    {
                        Tex($"{Root}/{a.name}/{m.albedo}", false, false, m.alpha, big ? 1024 : 512);
                        Tex($"{Root}/{a.name}/{m.normal}", true, false, false, big ? 1024 : 512);
                        Tex($"{Root}/{a.name}/{m.mask}", false, true, false, 256);
                    }
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }

            int made = 0;
            foreach (var a in pf.assets)
            {
                // 2) materials
                var mats = a.materials.ToDictionary(m => m.key, m => Mat(a, m));
                foreach (var v in a.variants)
                {
                    // 3) model import with our materials
                    string fbx = $"{Root}/{a.name}/{v.fbx}";
                    if (!(AssetImporter.GetAtPath(fbx) is ModelImporter mi)) { Debug.LogWarning("[ASBO] props: no model " + fbx); continue; }
                    mi.importCameras = false; mi.importLights = false; mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None;
                    mi.importBlendShapes = false; mi.importVisibility = false;
                    mi.importNormals = ModelImporterNormals.Import; mi.importTangents = ModelImporterTangents.CalculateMikk;
                    mi.meshCompression = ModelImporterMeshCompression.Low; mi.isReadable = false;
                    mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                    foreach (var kv in mats) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
                    mi.SaveAndReimport();
                    // 4) prefab
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
                    var go = UnityEngine.Object.Instantiate(model); go.name = v.name;
                    Dress(go, a, v);
                    PrefabUtility.SaveAsPrefabAsset(go, $"{Out}/{v.name}.prefab");
                    UnityEngine.Object.DestroyImmediate(go); made++;
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[ASBO] props: {made} prefabs in {Out}");
        }

        static void Tex(string path, bool normal, bool mask, bool alpha, int android)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter imp)) return;
            imp.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            imp.sRGBTexture = !normal && !mask;
            imp.alphaSource = mask || alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            imp.alphaIsTransparency = alpha;
            imp.mipmapEnabled = true; imp.anisoLevel = 2; imp.wrapMode = TextureWrapMode.Repeat;
            if (alpha) { imp.mipMapsPreserveCoverage = true; imp.alphaTestReferenceValue = 0.45f; }
            imp.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = android, format = TextureImporterFormat.ASTC_6x6, compressionQuality = 50 });
            imp.SaveAndReimport();
        }

        static Material Mat(PAsset a, PMat pm)
        {
            string path = $"{Root}/{a.name}/{pm.name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Gfx.Lit) { name = pm.name }; AssetDatabase.CreateAsset(m, path); }
            var alb = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/{a.name}/{pm.albedo}");
            var nor = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/{a.name}/{pm.normal}");
            var msk = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/{a.name}/{pm.mask}");
            m.SetTexture("_BaseMap", alb); m.SetTexture("_MainTex", alb); m.SetColor("_BaseColor", Color.white);
            if (nor != null) { m.SetTexture("_BumpMap", nor); m.SetFloat("_BumpScale", 1f); m.EnableKeyword("_NORMALMAP"); }
            if (msk != null)
            {
                m.SetTexture("_MetallicGlossMap", msk); m.EnableKeyword("_METALLICSPECGLOSSMAP");
                m.SetFloat("_SmoothnessTextureChannel", 0f); m.SetFloat("_Smoothness", pm.alpha ? 0.6f : 1f); m.SetFloat("_Metallic", 1f);
            }
            if (pm.alpha)
            {
                // leaves: cut-out, both sides drawn
                m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.45f); m.EnableKeyword("_ALPHATEST_ON");
                m.SetOverrideTag("RenderType", "TransparentCutout"); m.renderQueue = (int)RenderQueue.AlphaTest;
                m.SetFloat("_Cull", 0f); m.doubleSidedGI = true;
            }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>LOD distances, shadows, colliders and (for things you can knock over) a rigid body.</summary>
        static void Dress(GameObject go, PAsset a, PVar v)
        {
            var rs = go.GetComponentsInChildren<MeshRenderer>(true);
            var r0 = rs.FirstOrDefault(r => r.name.EndsWith("_LOD0")); if (r0 == null) r0 = rs.FirstOrDefault();
            var r1 = rs.FirstOrDefault(r => r.name.EndsWith("_LOD1"));
            if (r0 == null) return;
            var lg = go.GetComponent<LODGroup>(); if (lg == null) lg = go.AddComponent<LODGroup>(); // (not ??: Unity's fake null)
            var b = r0.bounds; float size = Mathf.Max(b.size.x, b.size.y, b.size.z);
            float h0, h1;
            switch (a.kind)
            {
                case "foliage": h0 = 0.05f; h1 = 0.015f; break;
                case "tree": h0 = 0.025f; h1 = 0.025f; break;
                case "rock": h0 = size > 6f ? 0.3f : size > 1f ? 0.18f : 0.06f; h1 = size > 6f ? 0.003f : size > 1f ? 0.015f : 0.012f; break;
                default: h0 = 0.12f; h1 = 0.02f; break;
            }
            if (r1 != null) lg.SetLODs(new[] { new LOD(h0, new Renderer[] { r0 }), new LOD(h1, new Renderer[] { r1 }) });
            else lg.SetLODs(new[] { new LOD(h1, new Renderer[] { r0 }) });
            lg.RecalculateBounds();
            bool smallLeaf = a.kind == "foliage" || a.kind == "flat";
            foreach (var r in rs)
            {
                r.shadowCastingMode = smallLeaf || r == r1 && a.kind != "rock" ? ShadowCastingMode.Off : ShadowCastingMode.On;
                r.receiveGI = ReceiveGI.LightProbes;
                r.lightProbeUsage = LightProbeUsage.BlendProbes;
            }
            // colliders
            string n = a.name;
            Vector3 lc = go.transform.InverseTransformPoint(b.center), ls = b.size;
            if (a.kind == "prop")
            {
                if (n.StartsWith("garden_hose")) return; // a wall fixture: nothing to bump into
                var bc = go.AddComponent<BoxCollider>(); bc.center = lc; bc.size = ls;
                var rb = go.AddComponent<Rigidbody>();
                rb.mass = n.Contains("trash") ? 7f : n.Contains("gnome") ? 3f : n.Contains("potted_plant_01") ? 9f : n.Contains("potted_plant_02") ? 6f
                    : n.Contains("football") ? 0.43f : n.Contains("tyre") ? 8f : n.Contains("rim") ? 6.5f : 2.5f;
                rb.linearDamping = 0.2f; rb.angularDamping = 0.6f; rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                rb.sleepThreshold = 0.05f;
                go.AddComponent<Knockable>();
            }
            else if (a.kind == "fixture")
            {
                // solid, but it stays put (cabinets, barriers, poles, gates, the covered car, the burning barrel)
                var bc = go.AddComponent<BoxCollider>(); bc.center = lc; bc.size = ls;
                if (n.Contains("electricity_pole")) { bc.center = new Vector3(0f, ls.y * 0.5f, 0f); bc.size = new Vector3(0.3f, ls.y, 0.3f); } // just the pole, not the cross-arms
            }
            else if (a.kind == "rock" && size > 0.6f)
            {
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = (r1 != null ? r1 : r0).GetComponent<MeshFilter>().sharedMesh;
            }
            else if (a.kind == "tree")
            {
                var cc = go.AddComponent<CapsuleCollider>(); cc.radius = 0.22f; cc.height = 3.5f; cc.center = new Vector3(0, 1.75f, 0);
            }
        }
    }
}
#endif

#if UNITY_EDITOR
using System.IO;
using ASBO.Slice;
using UnityEditor;
using UnityEngine;

namespace ASBO.EditorTools
{
    /// <summary>
    /// Sets up the modelled cars (Assets/Art/Cars/&lt;id&gt;.fbx from tools/art/cars): their wear maps (readable, for
    /// painting at runtime, kept in Resources/Cars), the shared car materials in Resources/CarMats, and a prefab per car
    /// in Resources/Cars. Run from ASBO/Art/Build Cars.
    /// </summary>
    public static class ASBOCars
    {
        const string Src = "Assets/Art/Cars", Res = "Assets/Resources/Cars", Mats = "Assets/Resources/CarMats";

        [MenuItem("ASBO/Art/Build Cars")]
        public static void Build()
        {
            Directory.CreateDirectory(Res);
            // wear maps: exact bytes, readable, no compression
            foreach (var f in Directory.GetFiles(Res, "*_wear.png"))
            {
                var imp = (TextureImporter)AssetImporter.GetAtPath(f.Replace('\\', '/'));
                imp.textureType = TextureImporterType.Default; imp.sRGBTexture = false; imp.isReadable = true; imp.mipmapEnabled = false;
                imp.alphaSource = TextureImporterAlphaSource.FromInput; imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.npotScale = TextureImporterNPOTScale.None; imp.filterMode = FilterMode.Point;
                imp.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = 512, format = TextureImporterFormat.RGBA32 });
                imp.SaveAndReimport();
            }
            foreach (var n in new[] { "car_lamps", "car_wheels" })
            {
                var imp = (TextureImporter)AssetImporter.GetAtPath($"{Src}/{n}.png");
                if (imp == null) continue;
                imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.mipmapEnabled = true; imp.filterMode = FilterMode.Bilinear;
                imp.textureCompression = TextureImporterCompression.Uncompressed; imp.wrapMode = TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }
            var paint = Mat("KitPaint", m =>
            {
                m.SetColor("_BaseColor", Color.white); m.SetFloat("_Metallic", 0.12f); m.SetFloat("_Smoothness", 1f);
                m.SetFloat("_SmoothnessTextureChannel", 1f); m.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            });
            var dark = Mat("KitDark", m => { m.SetColor("_BaseColor", new Color(0.035f, 0.035f, 0.04f)); m.SetFloat("_Smoothness", 0.35f); m.SetFloat("_Metallic", 0f); });
            var chrome = Mat("KitChrome", m => { m.SetColor("_BaseColor", new Color(0.82f, 0.83f, 0.85f)); m.SetFloat("_Smoothness", 0.88f); m.SetFloat("_Metallic", 1f); });
            var lampsTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Src}/car_lamps.png");
            var lamps = Mat("KitLamps", m =>
            {
                m.SetTexture("_BaseMap", lampsTex); m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.85f);
                m.SetTexture("_EmissionMap", lampsTex); m.SetColor("_EmissionColor", Color.white * 0.35f); m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            });
            var plate = Mat("KitPlate", m => { m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.4f); });
            var wheelTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Src}/car_wheels.png");
            var wheel = Mat("KitWheel", m => { m.SetTexture("_BaseMap", wheelTex); m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.4f); m.SetFloat("_Metallic", 0.3f); });
            var glassSrc = AssetDatabase.LoadAssetAtPath<Material>($"{Mats}/Glass.mat");
            var glass = AssetDatabase.LoadAssetAtPath<Material>($"{Mats}/KitGlass.mat");
            if (glass == null && glassSrc != null) { glass = new Material(glassSrc) { name = "KitGlass" }; AssetDatabase.CreateAsset(glass, $"{Mats}/KitGlass.mat"); }
            if (glass != null) { glass.SetColor("_BaseColor", new Color(0.02f, 0.03f, 0.035f, 0.82f)); glass.SetFloat("_Smoothness", 0.92f); EditorUtility.SetDirty(glass); }

            int made = 0;
            foreach (var fbx in Directory.GetFiles(Src, "*.fbx"))
            {
                string path = fbx.Replace('\\', '/'), id = Path.GetFileNameWithoutExtension(path);
                var mi = (ModelImporter)AssetImporter.GetAtPath(path);
                mi.importCameras = false; mi.importLights = false; mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None;
                mi.importBlendShapes = false; mi.importNormals = ModelImporterNormals.Import; mi.importTangents = ModelImporterTangents.None;
                mi.meshCompression = ModelImporterMeshCompression.Off; mi.isReadable = true; // readable: the bodywork dents in crashes
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                void Map(string n, Material m) { if (m != null) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), m); }
                Map("X_paint", paint); Map("X_dark", dark); Map("X_chrome", chrome); Map("X_glass", glass); Map("X_lamps", lamps); Map("X_plate", plate); Map("X_wheel", wheel);
                mi.SaveAndReimport();
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var go = Object.Instantiate(model); go.name = id;
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                {
                    r.receiveGI = ReceiveGI.LightProbes; r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
                    if (r.name.StartsWith("dent_")) r.gameObject.SetActive(false);
                }
                PrefabUtility.SaveAsPrefabAsset(go, $"{Res}/{id}.prefab");
                Object.DestroyImmediate(go); made++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[ASBO] cars: {made} car prefabs in {Res}");
            BuildAmbulance(dark, chrome, glass, lamps, plate, wheel);
        }

        const string AmbSrc = "Assets/Art/Ambulance";

        /// <summary>
        /// The box ambulance (Assets/Art/Ambulance/ambulance.fbx from tools/art/cars/ambforge.py): baked livery
        /// (yellow, green Battenburg, EMERGENCY AMBULANCE), chevron rear doors, white treatment room with kit inside.
        /// Parts the call-out drives (doors, flashing lamps, wheels) stay separate objects in the prefab.
        /// </summary>
        static void BuildAmbulance(Material dark, Material chrome, Material glass, Material lamps, Material plate, Material wheel)
        {
            string fbxPath = $"{AmbSrc}/ambulance.fbx";
            if (AssetImporter.GetAtPath(fbxPath) == null) return;
            foreach (var n in new[] { "amb_livery", "amb_door" })
            {
                var imp = (TextureImporter)AssetImporter.GetAtPath($"{AmbSrc}/{n}.png"); if (imp == null) continue;
                imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.mipmapEnabled = true; imp.filterMode = FilterMode.Trilinear;
                imp.anisoLevel = 4; imp.wrapMode = TextureWrapMode.Clamp;
                imp.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = 1024, format = TextureImporterFormat.ASTC_6x6 });
                imp.SaveAndReimport();
            }
            var liveryTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{AmbSrc}/amb_livery.png");
            var doorTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{AmbSrc}/amb_door.png");
            var livery = Mat("AmbLivery", m => { m.SetTexture("_BaseMap", liveryTex); m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.62f); m.SetFloat("_Metallic", 0.05f); });
            var door = Mat("AmbDoor", m => { m.SetTexture("_BaseMap", doorTex); m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", 0.6f); m.SetFloat("_Metallic", 0.05f); });
            var doorIn = Mat("AmbDoorIn", m => { m.SetColor("_BaseColor", new Color(0.78f, 0.79f, 0.8f)); m.SetFloat("_Smoothness", 0.35f); m.SetFloat("_Metallic", 0f); });
            var white = Mat("AmbWhite", m => { m.SetColor("_BaseColor", new Color(0.9f, 0.91f, 0.92f)); m.SetFloat("_Smoothness", 0.45f); m.SetFloat("_Metallic", 0f); });
            var floor = Mat("AmbFloor", m => { m.SetColor("_BaseColor", new Color(0.4f, 0.42f, 0.45f)); m.SetFloat("_Smoothness", 0.6f); m.SetFloat("_Metallic", 0f); });
            var bag = Mat("AmbBag", m => { m.SetColor("_BaseColor", new Color(0.09f, 0.27f, 0.62f)); m.SetFloat("_Smoothness", 0.2f); m.SetFloat("_Metallic", 0f); });
            var bag2 = Mat("AmbBag2", m => { m.SetColor("_BaseColor", new Color(0.06f, 0.4f, 0.2f)); m.SetFloat("_Smoothness", 0.2f); m.SetFloat("_Metallic", 0f); });
            var blue = Mat("AmbBlue", m => { m.SetColor("_BaseColor", new Color(0.06f, 0.12f, 0.36f)); m.SetFloat("_Smoothness", 0.85f); m.SetFloat("_Metallic", 0f); });
            var mi = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
            mi.importCameras = false; mi.importLights = false; mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None;
            mi.importBlendShapes = false; mi.importNormals = ModelImporterNormals.Import; mi.importTangents = ModelImporterTangents.None;
            mi.meshCompression = ModelImporterMeshCompression.Off; mi.isReadable = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            void Map(string n, Material m) { if (m != null) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), m); }
            Map("X_livery", livery); Map("X_door", door); Map("X_doorin", doorIn); Map("X_white", white); Map("X_floor", floor);
            Map("X_bag", bag); Map("X_bag2", bag2); Map("X_blue", blue);
            Map("X_dark", dark); Map("X_chrome", chrome); Map("X_glass", glass); Map("X_lamps", lamps); Map("X_plate", plate); Map("X_wheel", wheel);
            mi.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var go = Object.Instantiate(model); go.name = "ambulance";
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true)) { r.receiveGI = ReceiveGI.LightProbes; r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes; }
            PrefabUtility.SaveAsPrefabAsset(go, $"{Res}/ambulance.prefab");
            Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            Debug.Log("[ASBO] cars: ambulance prefab built");
        }

        /// <summary>Log where each part of a car prefab sits (for checking the import).</summary>
        public static void Inspect(string id)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>($"{Res}/{id}.prefab");
            foreach (var mf in pf.GetComponentsInChildren<MeshFilter>(true))
            {
                var t = mf.transform; var b = mf.sharedMesh.bounds;
                Debug.Log($"[ASBO] {id}/{t.name} pos {t.localPosition} rot {t.localEulerAngles} scale {t.localScale} mesh min {b.min} max {b.max}");
            }
        }

        static Material Mat(string name, System.Action<Material> set)
        {
            string path = $"{Mats}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Gfx.Lit) { name = name }; AssetDatabase.CreateAsset(m, path); }
            set(m); m.enableInstancing = true; EditorUtility.SetDirty(m);
            return m;
        }
    }
}
#endif

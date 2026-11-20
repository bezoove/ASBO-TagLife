#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ASBO.Slice;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ASBO.EditorTools
{
    /// <summary>
    /// ASBO > Build Abbots Wend Scene: turns the procedural static world into a proper Unity scene —
    /// saved meshes/materials/textures, one prefab per cottage, Unity Terrain (painted layers, grass, trees),
    /// static flags + lightmap UVs, light probes, reflection probe, baked NavMesh (NavMeshSurface) and baked lightmaps.
    /// </summary>
    public static class ASBOSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/AbbotsWend.unity";
        const string Gen = "Assets/Art/Generated";

        [MenuItem("ASBO/Build Abbots Wend Scene")]
        public static void Build()
        {
            foreach (var d in new[] { "Assets/Scenes", Gen, Gen + "/Meshes", Gen + "/Materials", Gen + "/Textures", Gen + "/Prefabs", Gen + "/Terrain" })
                Directory.CreateDirectory(d);
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // camera + sun
            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera"; var cam = camGo.AddComponent<Camera>(); camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(-17, 3, -30); cam.farClipPlane = 600f;
            var sunGo = new GameObject("Sun"); var sun = sunGo.AddComponent<Light>(); sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(42f, -35f, 0); sun.color = new Color(1f, 0.95f, 0.86f); sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.82f; sun.lightmapBakeType = LightmapBakeType.Mixed;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.78f) * 1.05f;
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.58f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.36f, 0.32f, 0.24f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.74f, 0.8f, 0.86f); RenderSettings.fogStartDistance = 90f; RenderSettings.fogEndDistance = 420f;

            // post-processing volume (saved profile)
            var volGo = new GameObject("PostFX"); var vol = volGo.AddComponent<Volume>(); vol.isGlobal = true;
            var prof = Look.MakeProfile(); AssetDatabase.CreateAsset(prof, Gen + "/PostFX.asset");
            foreach (var c in prof.components) { c.name = c.GetType().Name; AssetDatabase.AddObjectToAsset(c, prof); }
            vol.sharedProfile = prof;

            // static world from the procedural builders
            var world = new GameObject("World"); world.AddComponent<BakedWorld>();
            World.BuildStatic(world.transform);
            var terrain = BuildTerrain(world.transform);

            // persist generated assets, set static flags / lightmap UVs, prefab each cottage
            var texSaved = new Dictionary<Texture, Texture>(); var matSaved = new Dictionary<Material, Material>(); var meshSaved = new Dictionary<Mesh, Mesh>();
            int n = 0;
            foreach (var r in world.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                var mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mf.sharedMesh)))
                {
                    if (!meshSaved.TryGetValue(mf.sharedMesh, out var sm))
                    {
                        sm = mf.sharedMesh;
                        bool big = sm.vertexCount > 60;
                        if (big) Unwrapping.GenerateSecondaryUVSet(sm);
                        AssetDatabase.CreateAsset(sm, $"{Gen}/Meshes/{Safe(r.name)}_{n++}.asset");
                        meshSaved[mf.sharedMesh] = sm;
                    }
                    var mc = r.GetComponent<MeshCollider>(); if (mc != null && mc.sharedMesh == mf.sharedMesh) mc.sharedMesh = sm;
                    mf.sharedMesh = sm;
                }
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) if (mats[i] != null) mats[i] = SaveMat(mats[i], matSaved, texSaved);
                r.sharedMaterials = mats;
                var flags = StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic;
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags);
                if (r is MeshRenderer mr)
                {
                    bool small = r.name == "Interior" || r.name == "Furniture" || r.name.StartsWith("Estate") && r.name != "EstateWalls" && r.name != "EstateGround" || r.bounds.size.magnitude < 1.2f;
                    mr.receiveGI = small ? ReceiveGI.LightProbes : ReceiveGI.Lightmaps;
                    if (r.name == "Roof") mr.scaleInLightmap = 0.5f;
                }
            }
            foreach (var t in world.GetComponentsInChildren<TextMesh>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);

            // NavMesh tuned for cottage doorways, baked with NavMeshSurface
            TuneAgent();
            var surf = world.AddComponent<NavMeshSurface>();
            surf.collectObjects = CollectObjects.Children; surf.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surf.overrideVoxelSize = true; surf.voxelSize = 0.08f; surf.overrideTileSize = true; surf.tileSize = 160;
            surf.BuildNavMesh();
            var navPath = Gen + "/NavMesh.asset"; if (File.Exists(navPath)) AssetDatabase.DeleteAsset(navPath);
            AssetDatabase.CreateAsset(surf.navMeshData, navPath);

            // light probes over the estate and road, plus one per room inside each cottage
            var probes = new List<Vector3>();
            for (float x = -52; x <= 52; x += 5f) for (float z = -38; z <= 58; z += 5f) { probes.Add(new Vector3(x, 0.6f, z)); probes.Add(new Vector3(x, 2.4f, z)); }
            foreach (var h in Estate.Houses)
                foreach (var rm in h.P.rooms)
                {
                    if (rm.lv > 1 || rm.lv < 0) continue;
                    foreach (var q in rm.r) { var c = h.root.TransformPoint(new Vector3(q.Cx, rm.y + 0.6f, q.Cd)); probes.Add(c); probes.Add(c + Vector3.up * 1.2f); }
                }
            var lpg = new GameObject("LightProbes").AddComponent<LightProbeGroup>(); lpg.transform.SetParent(world.transform, false);
            lpg.probePositions = probes.ToArray();
            var rp = new GameObject("ReflectionProbe").AddComponent<ReflectionProbe>(); rp.transform.SetParent(world.transform, false);
            rp.transform.position = new Vector3(0, 4, 10); rp.size = new Vector3(140, 30, 140); rp.mode = ReflectionProbeMode.Baked; rp.resolution = 128;

            // prefabs (one per cottage), connected back into the scene
            foreach (var cref in world.GetComponentsInChildren<CottageRef>(true))
                PrefabUtility.SaveAsPrefabAssetAndConnect(cref.gameObject, $"{Gen}/Prefabs/{Safe(cref.gameObject.name)}.prefab", InteractionMode.AutomatedAction);

            // lighting settings + bake
            var ls = new LightingSettings
            {
                name = "AbbotsWendLighting", bakedGI = true, realtimeGI = false, lightmapper = LightingSettings.Lightmapper.ProgressiveGPU,
                lightmapResolution = 6f, lightmapMaxSize = 2048, directSampleCount = 32, indirectSampleCount = 256, environmentSampleCount = 128,
                maxBounces = 2, ao = true, aoMaxDistance = 1.2f, mixedBakeMode = MixedLightingMode.Shadowmask, lightmapCompression = LightmapCompression.NormalQuality,
                filteringMode = LightingSettings.FilterMode.Auto, lightmapPadding = 2
            };
            AssetDatabase.CreateAsset(ls, Gen + "/AbbotsWendLighting.lighting");
            Lightmapping.lightingSettings = ls;

            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log($"[ASBO] Scene built: {meshSaved.Count} meshes, {matSaved.Count} materials, {texSaved.Count} textures, {probes.Count} light probes. Baking lighting...");
            Lightmapping.BakeAsync();
            Lightmapping.bakeCompleted -= OnBaked; Lightmapping.bakeCompleted += OnBaked;
        }

        static void OnBaked()
        {
            Lightmapping.bakeCompleted -= OnBaked;
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("[ASBO] Lighting bake complete and scene saved.");
        }

        [MenuItem("ASBO/Bake Lighting Only")]
        public static void BakeOnly() { Lightmapping.BakeAsync(); Lightmapping.bakeCompleted -= OnBaked; Lightmapping.bakeCompleted += OnBaked; }

        static string Safe(string s) { foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_'); return s.Replace(' ', '_'); }

        static Texture SaveTex(Texture t, Dictionary<Texture, Texture> saved)
        {
            if (t == null || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(t))) return t;
            if (saved.TryGetValue(t, out var s)) return s;
            if (!(t is Texture2D t2)) return t;
            var path = $"{Gen}/Textures/tex_{saved.Count}.png";
            File.WriteAllBytes(path, t2.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.wrapMode = t2.wrapMode; imp.anisoLevel = 4; imp.mipmapEnabled = true; imp.maxTextureSize = 512;
            imp.SaveAndReimport();
            s = AssetDatabase.LoadAssetAtPath<Texture2D>(path); saved[t] = s; return s;
        }

        static Material SaveMat(Material m, Dictionary<Material, Material> saved, Dictionary<Texture, Texture> tex)
        {
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(m))) return m;
            if (saved.TryGetValue(m, out var s)) return s;
            foreach (var p in m.GetTexturePropertyNames()) { var t = m.GetTexture(p); if (t != null) m.SetTexture(p, SaveTex(t, tex)); }
            if (m.IsKeywordEnabled("_EMISSION")) m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            AssetDatabase.CreateAsset(m, $"{Gen}/Materials/mat_{saved.Count}.mat");
            saved[m] = m; return m;
        }

        static void TuneAgent()
        {
            var so = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/NavMeshAreas.asset")[0]);
            var settings = so.FindProperty("m_Settings");
            if (settings != null && settings.arraySize > 0)
            {
                var a = settings.GetArrayElementAtIndex(0);
                a.FindPropertyRelative("agentRadius").floatValue = 0.24f;
                a.FindPropertyRelative("agentHeight").floatValue = 1.75f;
                a.FindPropertyRelative("agentClimb").floatValue = 0.3f;
                a.FindPropertyRelative("agentSlope").floatValue = 40f;
                so.ApplyModifiedProperties();
            }
        }

        // ---------------------------------------------------------------- terrain
        static Terrain BuildTerrain(Transform parent)
        {
            const float size = 600f, height = 24f; const int res = 257;
            var td = new TerrainData { heightmapResolution = res, size = new Vector3(size, height, size) };
            var h = new float[res, res];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float wx = -size / 2 + x * size / (res - 1), wz = -size / 2 + y * size / (res - 1);
                    // flat where the village is (estate + road strip), rolling fields beyond
                    float dx = Mathf.Max(0, Mathf.Abs(wx) - 70f), dz = Mathf.Max(0, Mathf.Max(-46f - wz, wz - 70f));
                    float road = Mathf.Abs(wz - World.RoadZ) < 14f ? 0f : 1f;
                    float t = Mathf.Clamp01(Mathf.Max(dx, dz) / 60f) * Mathf.Max(road, Mathf.Clamp01(dx / 40f));
                    float hill = Mathf.PerlinNoise(wx * 0.006f + 3.1f, wz * 0.006f + 7.7f) * 9f + Mathf.PerlinNoise(wx * 0.02f, wz * 0.02f) * 1.5f;
                    h[y, x] = (hill * t * t * (3 - 2 * t)) / height;
                }
            td.SetHeights(0, 0, h);
            // layers: grass, darker meadow, earth
            TerrainLayer L(string name, Texture2D tex, float tile, Color tint)
            {
                var path = $"{Gen}/Terrain/{name}.png"; File.WriteAllBytes(path, tex.EncodeToPNG()); AssetDatabase.ImportAsset(path);
                var layer = new TerrainLayer { diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path), tileSize = new Vector2(tile, tile), diffuseRemapMax = tint };
                AssetDatabase.CreateAsset(layer, $"{Gen}/Terrain/{name}.terrainlayer"); return layer;
            }
            td.terrainLayers = new[]
            {
                L("grass", Gfx.GrassTex(1), 4f, Color.white),
                L("meadow", Gfx.GrassTex(7), 7f, new Color(0.85f, 0.95f, 0.75f, 1f)),
                L("earth", Gfx.GravelTex(9), 3f, new Color(0.7f, 0.55f, 0.4f, 1f)),
            };
            int ar = td.alphamapResolution; var amap = new float[ar, ar, 3];
            for (int y = 0; y < ar; y++)
                for (int x = 0; x < ar; x++)
                {
                    float wx = -size / 2 + x * size / (ar - 1), wz = -size / 2 + y * size / (ar - 1);
                    float m = Mathf.Clamp01(Mathf.PerlinNoise(wx * 0.02f + 11f, wz * 0.02f) * 1.6f - 0.5f);
                    float e = Mathf.Clamp01(Mathf.PerlinNoise(wx * 0.05f, wz * 0.05f + 5f) * 2.2f - 1.6f);
                    if (Mathf.Abs(wz - World.RoadZ) < 7.6f && Mathf.Abs(wx) < 250f) e = Mathf.Max(e, 0.6f); // worn verges by the road
                    float g = Mathf.Max(0, 1 - m - e); float sum = g + m + e;
                    amap[y, x, 0] = g / sum; amap[y, x, 1] = m / sum; amap[y, x, 2] = e / sum;
                }
            td.SetAlphamaps(0, 0, amap);
            // grass detail (billboards), kept light for phones
            var blade = GrassBlade(); var bp = $"{Gen}/Terrain/grassblade.png"; File.WriteAllBytes(bp, blade.EncodeToPNG()); AssetDatabase.ImportAsset(bp);
            var bimp = (TextureImporter)AssetImporter.GetAtPath(bp); bimp.alphaIsTransparency = true; bimp.wrapMode = TextureWrapMode.Clamp; bimp.SaveAndReimport();
            td.detailPrototypes = new[] { new DetailPrototype { prototypeTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(bp), renderMode = DetailRenderMode.GrassBillboard, healthyColor = new Color(0.45f, 0.62f, 0.3f), dryColor = new Color(0.62f, 0.6f, 0.35f), minHeight = 0.25f, maxHeight = 0.55f, minWidth = 0.3f, maxWidth = 0.6f, noiseSpread = 0.2f } };
            td.SetDetailResolution(512, 32);
            var det = new int[512, 512];
            for (int y = 0; y < 512; y++)
                for (int x = 0; x < 512; x++)
                {
                    float wx = -size / 2 + x * size / 511f, wz = -size / 2 + y * size / 511f;
                    bool village = Mathf.Abs(wx) < 50f && wz > -24f && wz < 56f || Mathf.Abs(wz - World.RoadZ) < 6.5f;
                    det[y, x] = village ? 0 : (Mathf.PerlinNoise(wx * 0.08f, wz * 0.08f) > 0.45f ? 3 : 1);
                }
            td.SetDetailLayer(0, 0, 0, det);
            // trees: a saved low-poly tree prefab scattered over the fields
            var treePrefab = TreePrefab();
            td.treePrototypes = new[] { new TreePrototype { prefab = treePrefab } };
            var rng = new System.Random(4); var trees = new List<TreeInstance>();
            for (int i = 0; i < 420; i++)
            {
                float wx = (float)(rng.NextDouble() * size - size / 2), wz = (float)(rng.NextDouble() * size - size / 2);
                if (Mathf.Abs(wx) < 60f && wz > -42f && wz < 66f) continue;
                if (Mathf.Abs(wz - World.RoadZ) < 12f) continue;
                float s = 0.8f + (float)rng.NextDouble() * 0.7f;
                trees.Add(new TreeInstance { position = new Vector3((wx + size / 2) / size, 0, (wz + size / 2) / size), prototypeIndex = 0, widthScale = s, heightScale = s, color = Color.white, lightmapColor = Color.white, rotation = (float)(rng.NextDouble() * Mathf.PI * 2) });
            }
            td.SetTreeInstances(trees.ToArray(), true);
            AssetDatabase.CreateAsset(td, $"{Gen}/Terrain/AbbotsWendTerrain.asset");
            var tgo = Terrain.CreateTerrainGameObject(td); tgo.name = "Terrain"; tgo.transform.SetParent(parent, false);
            tgo.transform.position = new Vector3(-size / 2, -0.02f, -size / 2);
            var terr = tgo.GetComponent<Terrain>();
            terr.detailObjectDistance = 45f; terr.detailObjectDensity = 0.6f; terr.treeBillboardDistance = 90f; terr.heightmapPixelError = 8f; terr.basemapDistance = 120f;
            terr.materialTemplate = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
            AssetDatabase.CreateAsset(terr.materialTemplate, $"{Gen}/Terrain/TerrainLit.mat");
            GameObjectUtility.SetStaticEditorFlags(tgo, StaticEditorFlags.ContributeGI | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic);
            return terr;
        }

        static Texture2D GrassBlade()
        {
            const int S = 64; var t = new Texture2D(S, S, TextureFormat.RGBA32, true); var px = new Color32[S * S]; var r = new System.Random(2);
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);
            for (int b = 0; b < 14; b++)
            {
                int x0 = r.Next(6, S - 6); float lean = (float)(r.NextDouble() - 0.5) * 0.5f; int hgt = r.Next(S / 2, S - 2);
                for (int y = 0; y < hgt; y++)
                {
                    int x = Mathf.RoundToInt(x0 + lean * y); float w = 1.6f * (1 - y / (float)hgt) + 0.4f;
                    for (int dx = -2; dx <= 2; dx++) { int xx = x + dx; if (xx < 0 || xx >= S || Mathf.Abs(dx) > w) continue; byte g = (byte)(150 + 60 * y / hgt); px[y * S + xx] = new Color32((byte)(g * 0.55f), g, (byte)(g * 0.35f), 255); }
                }
            }
            t.SetPixels32(px); t.Apply(); return t;
        }

        static GameObject TreePrefab()
        {
            var k = new MeshKit(); var r = new System.Random(3);
            var bark = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.32f, 0.24f, 0.16f) };
            var leaf = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.22f, 0.4f, 0.16f) };
            AssetDatabase.CreateAsset(bark, $"{Gen}/Terrain/bark.mat"); AssetDatabase.CreateAsset(leaf, $"{Gen}/Terrain/leaf.mat");
            k.Cyl(bark, Vector3.zero, 0.22f, 4.2f, 8, false, 0.14f);
            k.Sphere(leaf, new Vector3(0, 5.0f, 0), new Vector3(2.3f, 2.0f, 2.3f), 10, 7);
            for (int q = 0; q < 4; q++) { float a = q * 1.6f; k.Sphere(leaf, new Vector3(Mathf.Cos(a) * 1.3f, 4.3f + q * 0.25f, Mathf.Sin(a) * 1.3f), new Vector3(1.5f, 1.3f, 1.5f), 8, 6); }
            var go = k.Build("Tree", null, false);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh; Unwrapping.GenerateSecondaryUVSet(mesh);
            AssetDatabase.CreateAsset(mesh, $"{Gen}/Terrain/tree_mesh.asset");
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{Gen}/Terrain/Tree.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }
    }
}
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.EditorTools
{
    /// <summary>What the open scene costs to draw: renderers by size, materials, meshes, shadow casters, lights, sounds, textures.</summary>
    public static class ASBOPerfAudit
    {
        [MenuItem("ASBO/Perf/Audit Scene")]
        public static void Scene()
        {
            var rs = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(r => r.enabled).ToArray();
            long tris = 0; int casters = 0, statics = 0, small = 0, mid = 0, big = 0, smallCasters = 0, subs = 0;
            var byMat = new Dictionary<Material, int>(); var byMesh = new Dictionary<Mesh, (int n, long t)>(); var byRoot = new Dictionary<string, (int n, long t)>();
            foreach (var r in rs)
            {
                var mf = r.GetComponent<MeshFilter>(); var m = mf != null ? mf.sharedMesh : null; long t = 0;
                if (m != null) { for (int i = 0; i < m.subMeshCount; i++) t += m.GetIndexCount(i) / 3; var e = byMesh.TryGetValue(m, out var x) ? x : (0, 0); byMesh[m] = (e.Item1 + 1, t); }
                tris += t; subs += r.sharedMaterials.Length;
                float s = r.bounds.size.magnitude; if (s < 1.5f) small++; else if (s < 5f) mid++; else big++;
                if (r.shadowCastingMode != ShadowCastingMode.Off) { casters++; if (s < 1.5f) smallCasters++; }
                if (GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.BatchingStatic)) statics++;
                foreach (var mat in r.sharedMaterials) if (mat != null) byMat[mat] = byMat.TryGetValue(mat, out var c) ? c + 1 : 1;
                var root = r.transform; while (root.parent != null && root.parent.parent != null && root.parent.name != "World") root = root.parent;
                string key = root.name; var er = byRoot.TryGetValue(key, out var y) ? y : (0, 0); byRoot[key] = (er.Item1 + 1, er.Item2 + t);
            }
            Debug.Log($"[ASBO] audit renderers {rs.Length} (submeshes {subs}), tris {tris / 1000}k, small<1.5m {small} / 1.5-5m {mid} / big {big}, shadow casters {casters} (small {smallCasters}), batching-static {statics}, materials {byMat.Count}, meshes {byMesh.Count}");
            Debug.Log("[ASBO] audit by area: " + string.Join(" | ", byRoot.OrderByDescending(k => k.Value.n).Take(25).Select(k => $"{k.Key} {k.Value.n}r {k.Value.t / 1000}k")));
            Debug.Log("[ASBO] audit top materials: " + string.Join(" | ", byMat.OrderByDescending(k => k.Value).Take(20).Select(k => $"{k.Key.name}({k.Key.shader.name.Replace("Universal Render Pipeline/", "")}) x{k.Value}")));
            Debug.Log("[ASBO] audit top meshes by total tris: " + string.Join(" | ", byMesh.OrderByDescending(k => k.Value.n * k.Value.t).Take(15).Select(k => $"{k.Key.name} {k.Value.t}t x{k.Value.n}")));
            var shaders = byMat.Keys.GroupBy(m => m.shader.name).Select(g => $"{g.Key} {g.Count()}"); Debug.Log("[ASBO] audit shaders: " + string.Join(" | ", shaders));
            var ls = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"[ASBO] audit lights {ls.Length}: active {ls.Count(l => l.isActiveAndEnabled)}, realtime {ls.Count(l => l.lightmapBakeType == LightmapBakeType.Realtime)}, mixed {ls.Count(l => l.lightmapBakeType == LightmapBakeType.Mixed)}, shadowed {ls.Count(l => l.shadows != LightShadows.None)}; types {string.Join(",", ls.GroupBy(l => l.type).Select(g => g.Key + " " + g.Count()))}");
            var aus = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"[ASBO] audit audio sources {aus.Length}: playOnAwake {aus.Count(a => a.playOnAwake)}, loop {aus.Count(a => a.loop)}; real voices {AudioSettings.GetConfiguration().numRealVoices} virtual {AudioSettings.GetConfiguration().numVirtualVoices}");
            var cols = Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Debug.Log($"[ASBO] audit colliders {cols.Length}: mesh {cols.Count(c => c is MeshCollider)} (convex {cols.OfType<MeshCollider>().Count(c => c.convex)}), box {cols.Count(c => c is BoxCollider)}, triggers {cols.Count(c => c.isTrigger)}; rigidbodies {Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Length}");
            foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
            {
                var td = t.terrainData;
                Debug.Log($"[ASBO] audit terrain {t.name}: heightmap {td.heightmapResolution}, detail res {td.detailResolution} layers {td.detailPrototypes.Length}, trees {td.treeInstanceCount} protos {td.treePrototypes.Length}, alphamap {td.alphamapResolution} x{td.alphamapLayers}, pixelError {t.heightmapPixelError}, detailDist {t.detailObjectDistance} density {t.detailObjectDensity}, treeDist {t.treeDistance} billboard {t.treeBillboardDistance}, drawInstanced {t.drawInstanced}");
            }
            // textures used by materials in the scene, biggest first (Android settings)
            var texs = new Dictionary<string, string>();
            foreach (var mat in byMat.Keys)
                foreach (var n in mat.GetTexturePropertyNames())
                {
                    var tx = mat.GetTexture(n) as Texture2D; if (tx == null) continue; string p = AssetDatabase.GetAssetPath(tx); if (string.IsNullOrEmpty(p) || texs.ContainsKey(p)) continue;
                    var imp = AssetImporter.GetAtPath(p) as TextureImporter; string a = "-";
                    if (imp != null) { var ps = imp.GetPlatformTextureSettings("Android"); a = ps.overridden ? $"{ps.maxTextureSize} {ps.format}" : $"default {imp.maxTextureSize} {imp.textureCompression}"; }
                    texs[p] = $"{System.IO.Path.GetFileName(p)} {tx.width}x{tx.height} [{a}]";
                }
            Debug.Log($"[ASBO] audit textures {texs.Count}: " + string.Join(" | ", texs.Values.OrderByDescending(v => v.Contains("4096") ? 3 : v.Contains("2048") ? 2 : v.Contains("default") ? 1 : 0).Take(40)));
            Debug.Log($"[ASBO] audit occlusion data {(StaticOcclusionCulling.umbraDataSize > 0 ? StaticOcclusionCulling.umbraDataSize / 1024 + " KB" : "none")}; android APIs {string.Join(",", PlayerSettings.GetGraphicsAPIs(BuildTarget.Android))}; static batching {GetStaticBatching()}; gpu skinning {PlayerSettings.gpuSkinning}");
        }
        /// <summary>The public static methods of an editor or engine type (for checking an API in this Unity version).</summary>
        public static void Api(string typeName)
        {
            System.Type t = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies()) { t = asm.GetType(typeName); if (t != null) break; }
            if (t == null) { Debug.Log("[ASBO] api: no type " + typeName); return; }
            Debug.Log($"[ASBO] api {typeName}: " + string.Join(" | ", t.GetMembers(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly).Select(m => m.ToString())));
            foreach (var nt in t.GetNestedTypes()) Debug.Log($"[ASBO] api {typeName}.{nt.Name}: " + (nt.IsEnum ? string.Join(",", System.Enum.GetNames(nt)) : string.Join(" | ", nt.GetMembers().Select(m => m.ToString()))));
        }
        public static void Lights()
        {
            var ls = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log("[ASBO] lights: " + string.Join(" | ", ls.GroupBy(l => (l.transform.parent != null ? l.transform.parent.name : "") + "/" + l.name).Select(g => $"{g.Key} x{g.Count()} {g.First().type} r{g.First().range:F0} i{g.First().intensity:F1}")));
        }
        static string GetStaticBatching()
        {
            var m = typeof(PlayerSettings).GetMethod("GetBatchingForPlatform", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (m == null) return "?"; var args = new object[] { BuildTarget.Android, 0, 0 }; m.Invoke(null, args); return $"static={args[1]} dynamic={args[2]}";
        }
    }
}
#endif

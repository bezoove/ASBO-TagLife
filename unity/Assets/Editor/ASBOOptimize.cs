#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using ASBO.Slice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.EditorTools
{
    /// <summary>
    /// The finishing pass on the town scene, run after every rebuild (ASBO/Art/Build Council Estate calls it) and on its
    /// own from ASBO/Perf/Optimise Scene. Each step makes the frame cheaper without changing how anything looks close up:
    ///  1. Shadows only from things that throw a shadow worth seeing: not flat ground, kerbs, paving and road paint, not
    ///     small bits, and not interiors (they're inside walls that already throw the shadow).
    ///  2. Everything static stays in static batching (fewer draw calls).
    ///  3. The terrain drawn instanced (fewer, cheaper draw calls).
    ///  4. Occlusion culling baked: anything hidden behind a building or a hill isn't drawn at all, people and cars too.
    /// </summary>
    public static class ASBOOptimize
    {
        [MenuItem("ASBO/Perf/Optimise Scene")]
        public static void Menu() { Run(true); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); }

        public static void Run(bool bakeOcclusion)
        {
            var world = Object.FindFirstObjectByType<BakedWorld>();
            if (world == null) { Debug.LogWarning("[ASBO] optimise: no baked world in the open scene"); return; }
            int noShadow = 0, lodded = 0; long trisBefore = 0;
            var rs = world.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in rs)
            {
                if (r.shadowCastingMode == ShadowCastingMode.Off) continue;
                var b = r.bounds; string n = r.name.ToLowerInvariant();
                bool inside = false; for (var t = r.transform; t != null && t != world.transform; t = t.parent) { string tn = t.name.ToLowerInvariant(); if (tn == "interior" || tn == "furniture" || tn.StartsWith("deco") || tn.StartsWith("furn") || tn.Contains("interior")) { inside = true; break; } }
                bool flat = b.size.y < 0.35f, small = b.size.magnitude < 1.3f, paint = n.Contains("dash") || n.Contains("line") || n.Contains("zebra") || n.Contains("kerb") || n.Contains("pavement") || n.Contains("road") || n.Contains("ground") || n.Contains("drive");
                if (flat || small || inside || paint) { r.shadowCastingMode = ShadowCastingMode.Off; noShadow++; EditorUtility.SetDirty(r); }
            }
            // static batching on everything static (measured: taking heavy meshes out of it for Mesh LOD cost more draw calls and
            // shadow casters than the triangles it saved; the town's own meshes are a small share of the triangles, people are most)
            foreach (var r in rs)
            {
                var f = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
                if (f != 0 && (f & StaticEditorFlags.BatchingStatic) == 0) { GameObjectUtility.SetStaticEditorFlags(r.gameObject, f | StaticEditorFlags.BatchingStatic); lodded++; }
            }
            AssetDatabase.SaveAssets();
            foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
            {
                t.drawInstanced = true; t.treeDistance = Mathf.Min(t.treeDistance, 420f); EditorUtility.SetDirty(t);
                var f = GameObjectUtility.GetStaticEditorFlags(t.gameObject); GameObjectUtility.SetStaticEditorFlags(t.gameObject, f | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            }
            // everything static in the world can hide things and be hidden (moving parts are flagged 0 by their builders)
            int occ = 0;
            foreach (var r in world.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is SkinnedMeshRenderer) continue;
                var f = GameObjectUtility.GetStaticEditorFlags(r.gameObject); if (f == 0) continue;
                bool big = r.bounds.size.magnitude > 3f && !r.name.ToLowerInvariant().Contains("glass") && !r.name.ToLowerInvariant().Contains("railing") && !r.name.ToLowerInvariant().Contains("fence") && !r.name.ToLowerInvariant().Contains("leaves") && !r.name.ToLowerInvariant().Contains("green");
                var want = f | StaticEditorFlags.OccludeeStatic; if (big) want |= StaticEditorFlags.OccluderStatic; else want &= ~StaticEditorFlags.OccluderStatic;
                if (want != f) { GameObjectUtility.SetStaticEditorFlags(r.gameObject, want); occ++; }
            }
            PlayerSettings.gcIncremental = true;
            Debug.Log($"[ASBO] optimise: shadows off on {noShadow} of {rs.Length} renderers, static batching restored on {lodded}, terrain instanced, occlusion flags changed on {occ}");
            if (bakeOcclusion)
            {
                StaticOcclusionCulling.smallestOccluder = 4f; StaticOcclusionCulling.smallestHole = 0.3f; StaticOcclusionCulling.backfaceThreshold = 100f;
                var t0 = System.DateTime.Now;
                bool ok = StaticOcclusionCulling.Compute();
                Debug.Log($"[ASBO] optimise: occlusion culling baked ({ok}) in {(System.DateTime.Now - t0).TotalSeconds:F0}s, {StaticOcclusionCulling.umbraDataSize / 1024} KB");
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
#endif

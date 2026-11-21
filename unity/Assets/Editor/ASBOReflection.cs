#if UNITY_EDITOR
using ASBO.Slice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.EditorTools
{
    /// <summary>
    /// One baked reflection probe over the road and houses, so car paint, glass and chrome reflect the street (houses,
    /// hedges, trees, road) instead of only the sky gradient. Baked once: free at runtime on phones.
    /// </summary>
    public static class ASBOReflection
    {
        [MenuItem("ASBO/Art/Bake Street Reflections")]
        public static void Bake()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var go = GameObject.Find("ASBO_StreetReflections"); if (go == null) go = new GameObject("ASBO_StreetReflections");
            go.transform.position = new Vector3(0f, 1.3f, World.RoadZ);
            var p = go.GetComponent<ReflectionProbe>(); if (p == null) p = go.AddComponent<ReflectionProbe>();
            p.mode = ReflectionProbeMode.Custom;
            p.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            p.size = new Vector3(260f, 40f, 140f);
            p.resolution = 128; p.hdr = true; p.boxProjection = false; p.importance = 1; p.intensity = 1f;
            p.nearClipPlane = 0.3f; p.farClipPlane = 600f; p.blendDistance = 4f;
            string dir = System.IO.Path.GetDirectoryName(scene.path).Replace('\\', '/');
            string path = dir + "/" + scene.name + "_StreetReflections.exr";
            if (!Lightmapping.BakeReflectionProbe(p, path)) { Debug.LogWarning("[ASBO] reflection bake failed"); return; }
            AssetDatabase.ImportAsset(path);
            p.customBakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(path);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("[ASBO] street reflections baked -> " + path + " (scene " + scene.name + ")");
        }
    }
}
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.EditorTools
{
    /// <summary>
    /// Phone render settings, applied in one go: one shadow cascade over a shorter distance, extra lights lit per
    /// vertex (siren lights, fires), no LOD cross-fade dithering, GPU skinning for the characters.
    /// </summary>
    public static class ASBOPerf
    {
        [MenuItem("ASBO/Perf/Apply Mobile Settings")]
        public static void Apply()
        {
            var a = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            if (a != null)
            {
                var so = new SerializedObject(a);
                void Set(string n, float v) { var p = so.FindProperty(n); if (p == null) { Debug.LogWarning("[ASBO] no " + n); return; } if (p.propertyType == SerializedPropertyType.Float) p.floatValue = v; else if (p.propertyType == SerializedPropertyType.Boolean) p.boolValue = v != 0f; else p.intValue = (int)v; }
                Set("m_ShadowDistance", 35f); Set("m_ShadowCascadeCount", 1f); Set("m_AdditionalLightsRenderingMode", 2f);
                Set("m_EnableLODCrossFade", 0f); Set("m_AdditionalLightsPerObjectLimit", 2f); Set("m_RenderScale", 0.85f);
                so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(a);
            }
            PlayerSettings.gpuSkinning = true;
            PlayerSettings.stripUnusedMeshComponents = true;
            PlayerSettings.SetMobileMTRendering(UnityEditor.Build.NamedBuildTarget.Android, true);
            AssetDatabase.SaveAssets();
            Debug.Log("[ASBO] mobile settings applied: 1 cascade 35 m, per-vertex extra lights, no LOD cross-fade, GPU skinning, multithreaded rendering");
        }
    }
}
#endif

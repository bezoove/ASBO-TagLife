#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ASBO.Slice
{
    /// Applies mobile-first project settings once (re-run via menu ASBO > Apply Mobile Settings).
    [InitializeOnLoad]
    public static class MobileSetup
    {
        const string Key = "ASBO_MobileSetup_v1";
        static MobileSetup()
        {
            if (SessionState.GetBool(Key, false) || EditorPrefs.GetBool(Key, false)) return;
            EditorApplication.delayCall += () => { Apply(); EditorPrefs.SetBool(Key, true); };
        }

        [MenuItem("ASBO/Apply Mobile Settings")]
        public static void Apply()
        {
            try
            {
                PlayerSettings.companyName = "ASBO";
                PlayerSettings.productName = "ASBO Life on Tag";
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.bezoove.asbo.lifeontag");
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26; // Android 8: covers 5+ year old phones
                PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Medium);
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
                PlayerSettings.gcIncremental = true;
                EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
                
                PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

                // Quality tiers: keep three (Low / Medium / High), default Medium on mobile.
                var names = QualitySettings.names;
                for (int i = 0; i < names.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.vSyncCount = 0;
                    QualitySettings.antiAliasing = 0;
                    QualitySettings.shadowDistance = i == 0 ? 25f : i == 1 ? 45f : 70f;
                    QualitySettings.shadowCascades = i == 0 ? 1 : 2;
                    QualitySettings.lodBias = i == 0 ? 0.8f : 1f;
                    QualitySettings.skinWeights = SkinWeights.TwoBones;
                    QualitySettings.softParticles = false;
                    QualitySettings.billboardsFaceCameraPosition = false;
                }
                Application.targetFrameRate = 60;

                // URP assets: SRP batcher on, HDR off, no depth/opaque textures, MSAA 2x, soft shadows only on top tier.
                var guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
                foreach (var g in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    var a = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                    if (a == null) continue;
                    var so = new SerializedObject(a);
                    void SetB(string n, bool v) { var p = so.FindProperty(n); if (p != null) p.boolValue = v; }
                    void SetI(string n, int v) { var p = so.FindProperty(n); if (p != null) p.intValue = v; }
                    void SetF(string n, float v) { var p = so.FindProperty(n); if (p != null) p.floatValue = v; }
                    bool mobile = path.Contains("Mobile");
                    SetB("m_UseSRPBatcher", true);
                    SetB("m_SupportsHDR", !mobile);
                    SetB("m_RequireDepthTexture", false);
                    SetB("m_RequireOpaqueTexture", false);
                    SetI("m_MSAA", mobile ? 2 : 4);
                    SetF("m_ShadowDistance", mobile ? 45f : 70f);
                    SetI("m_ShadowCascadeCount", mobile ? 2 : 3);
                    SetB("m_SoftShadowsSupported", !mobile);
                    SetF("m_RenderScale", mobile ? 0.85f : 1f);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(a);
                }
                AssetDatabase.SaveAssets();
                Debug.Log("[ASBO] Mobile settings applied (IL2CPP, ARM64, ASTC, Vulkan+GLES3, URP mobile tuning).");
            }
            catch (System.Exception e) { Debug.LogWarning("[ASBO] MobileSetup: " + e.Message); }
        }
    }
}
#endif

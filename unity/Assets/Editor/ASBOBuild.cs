#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ASBO.EditorTools
{
    /// <summary>Android test builds (APK for side-loading), driven from the menu or the job runner.</summary>
    public static class ASBOBuild
    {
        const string Scene = "Assets/Scenes/AbbotsWend_Test.unity", Out = "Builds/ASBO_test.apk";

        [MenuItem("ASBO/Build/Check Android Setup")]
        public static void Check()
        {
            bool ok = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android);
            string sdk = "?", ndk = "?", jdk = "?";
            var t = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.Android.AndroidExternalToolsSettings")).FirstOrDefault(x => x != null);
            if (t != null)
            {
                string P(string n) { var p = t.GetProperty(n, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public); return p != null ? (string)p.GetValue(null) : "n/a"; }
                sdk = P("sdkRootPath"); ndk = P("ndkRootPath"); jdk = P("jdkRootPath");
            }
            Debug.Log($"[ASBO] android supported={ok} active={EditorUserBuildSettings.activeBuildTarget} sdk='{sdk}' ndk='{ndk}' jdk='{jdk}'");
        }

        [MenuItem("ASBO/Build/Android APK (test)")]
        public static void BuildApk()
        {
            var nt = NamedBuildTarget.Android;
            PlayerSettings.SetScriptingBackend(nt, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false; PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true; PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.overrideTextureCompression = OverrideTextureCompression.ForceFastCompressor; // quicker test builds
            EditorUserBuildSettings.overrideMaxTextureSize = 1024; // phones: the character system ships 4K maps; 1K is plenty at this view distance
            Directory.CreateDirectory("Builds");
            var opts = new BuildPlayerOptions { scenes = new[] { Scene }, locationPathName = Out, target = BuildTarget.Android, targetGroup = BuildTargetGroup.Android, options = BuildOptions.None };
            Debug.Log("[ASBO] build starting");
            var rep = BuildPipeline.BuildPlayer(opts);
            var s = rep.summary;
            Debug.Log($"[ASBO] build {s.result}: {s.totalSize / 1048576f:F1} MB in {s.totalTime.TotalMinutes:F1} min, {s.totalErrors} errors, {s.totalWarnings} warnings -> {Out}");
            foreach (var st in rep.steps) foreach (var m in st.messages) if (m.type == LogType.Error || m.type == LogType.Exception) Debug.Log("[ASBO] build error: " + m.content);
            File.WriteAllText("Builds/last_build.txt", $"{s.result} {s.totalSize} {s.totalTime.TotalMinutes:F1} {s.totalErrors}\n");
        }
    }
}
#endif

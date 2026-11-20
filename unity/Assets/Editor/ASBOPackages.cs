#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace ASBO.EditorTools
{
    public static class ASBOPackages
    {
        [MenuItem("ASBO/Open Package Manager (My Assets)")]
        public static void Open() { UnityEditor.PackageManager.UI.Window.Open("UMA 2"); }

        /// <summary>Imports downloaded Asset Store packages (from the local Asset Store cache) without the interactive dialog.</summary>
        [MenuItem("ASBO/Import Downloaded Packs")]
        public static void ImportAll()
        {
            var roots = new[] {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity") };
            var want = new[] { "Basic Motions", "UMA 3" };
            foreach (var root in roots)
            {
                if (!Directory.Exists(root)) continue;
                foreach (var f in Directory.GetFiles(root, "*.unitypackage", SearchOption.AllDirectories))
                {
                    Debug.Log("[ASBO] cached package: " + f + " (" + new FileInfo(f).Length / 1048576 + " MB)");
                    var name = Path.GetFileNameWithoutExtension(f);
                    if (!want.Any(w => name.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0 || f.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0)) continue;
                    if (SessionState.GetBool("asbo_imported_" + f, false)) continue;
                    SessionState.SetBool("asbo_imported_" + f, true);
                    Debug.Log("[ASBO] importing " + f);
                    AssetDatabase.ImportPackage(f, false);
                }
            }
        }
    }
}
#endif

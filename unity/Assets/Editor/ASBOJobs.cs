#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
namespace ASBO.EditorTools
{
    /// <summary>Runs editor menu commands written to Library/asbo_job.txt (one per line), so tooling can drive the editor without clicking menus.</summary>
    [InitializeOnLoad]
    public static class ASBOJobs
    {
        const string JobFile = "Library/asbo_job.txt";
        static double next;
        static ASBOJobs() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < next || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            next = EditorApplication.timeSinceStartup + 1.5;
            if (!File.Exists(JobFile)) return;
            var lines = File.ReadAllLines(JobFile); File.Delete(JobFile);
            foreach (var l in lines)
            {
                var cmd = l.Trim(); if (cmd.Length == 0) continue;
                if (cmd == "play") { EditorApplication.isPlaying = true; continue; }
                if (cmd == "stop") { EditorApplication.isPlaying = false; continue; }
                if (cmd.StartsWith("call "))
                {
                    // call Type.Method arg1 arg2 ...  (static, args parsed as float/int/bool/string)
                    var parts = cmd.Substring(5).Split(' ');
                    int dot = parts[0].LastIndexOf('.'); string tn = parts[0].Substring(0, dot), mn = parts[0].Substring(dot + 1);
                    System.Type t = null;
                    foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies()) { t = asm.GetType(tn); if (t != null) break; }
                    var m = t?.GetMethod(mn);
                    if (m == null) { Debug.LogWarning("[ASBO] job: no method " + parts[0]); continue; }
                    var ps = m.GetParameters(); var args = new object[ps.Length];
                    for (int i = 0; i < ps.Length; i++)
                    {
                        string v = i + 1 < parts.Length ? parts[i + 1] : "";
                        var pt = ps[i].ParameterType;
                        args[i] = pt == typeof(float) ? float.Parse(v, System.Globalization.CultureInfo.InvariantCulture) : pt == typeof(int) ? int.Parse(v) : pt == typeof(bool) ? (object)(v == "true" || v == "1") : v;
                    }
                    m.Invoke(null, args); continue;
                }
                if (cmd.StartsWith("open ")) { UnityEditor.SceneManagement.EditorSceneManager.OpenScene(cmd.Substring(5)); continue; }
                bool ok = EditorApplication.ExecuteMenuItem(cmd);
                Debug.Log("[ASBO] job '" + cmd + "' " + (ok ? "ran" : "NOT FOUND"));
            }
        }
    }
}
#endif

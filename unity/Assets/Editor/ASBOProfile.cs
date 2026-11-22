#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace ASBO.EditorTools
{
    /// <summary>
    /// A real profile of the running game, written to the log: records some frames with Unity's profiler, then lists
    /// where main-thread time goes (by marker, self time) and which scripts allocate garbage. Call while playing:
    /// ASBO.EditorTools.ASBOProfile.Capture 120
    /// </summary>
    public static class ASBOProfile
    {
        static int want, startFrame; static string label;
        public static void Capture(int frames) => CaptureAs(frames, "");
        public static void CaptureAs(int frames, string name)
        {
            if (!EditorApplication.isPlaying) { Debug.Log("[ASBO] profile: not playing"); return; }
            want = Mathf.Clamp(frames, 10, 300); label = name;
            ProfilerDriver.ClearAllFrames(); ProfilerDriver.profileEditor = false; ProfilerDriver.enabled = true;
            startFrame = Time.frameCount;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
        }
        static void Tick()
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
            if (Time.frameCount - startFrame < want + 5) return;
            EditorApplication.update -= Tick;
            ProfilerDriver.enabled = false;
            Report();
        }
        static void Report()
        {
            int first = ProfilerDriver.firstFrameIndex, last = ProfilerDriver.lastFrameIndex;
            if (last < 0) { Debug.Log("[ASBO] profile: no frames"); return; }
            first = Mathf.Max(first, last - want + 1);
            var self = new Dictionary<string, double>(); var gc = new Dictionary<string, double>(); var calls = new Dictionary<string, double>();
            int n = 0; double frameMs = 0;
            for (int f = first; f <= last; f++)
            {
                using (var v = ProfilerDriver.GetHierarchyFrameDataView(f, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName, HierarchyFrameDataView.columnSelfTime, false))
                {
                    if (v == null || !v.valid) continue;
                    n++; frameMs += v.frameTimeMs;
                    var stack = new Stack<int>(); stack.Push(v.GetRootItemID()); var kids = new List<int>();
                    while (stack.Count > 0)
                    {
                        int id = stack.Pop(); kids.Clear(); v.GetItemChildren(id, kids);
                        foreach (var k in kids) stack.Push(k);
                        if (id == v.GetRootItemID()) continue;
                        string name = v.GetItemName(id);
                        double s = v.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnSelfTime);
                        double g = v.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnGcMemory);
                        double c = v.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnCalls);
                        self[name] = (self.TryGetValue(name, out var a) ? a : 0) + s;
                        calls[name] = (calls.TryGetValue(name, out var cc) ? cc : 0) + c;
                        // garbage counted at the script call that made it (its own GC.Alloc children roll up into it)
                        if (g > 0 && kids.Count >= 0) gc[name] = (gc.TryGetValue(name, out var b) ? b : 0) + g;
                    }
                }
            }
            if (n == 0) { Debug.Log("[ASBO] profile: no readable frames"); return; }
            Debug.Log($"[ASBO] profile {label}: {n} frames, {frameMs / n:F1} ms a frame. Self time per frame: " + string.Join(" | ", self.OrderByDescending(k => k.Value).Take(40).Select(k => $"{k.Key} {k.Value / n:F2}ms x{calls[k.Key] / n:F0}")));
            var scripts = gc.Where(k => k.Key.EndsWith("()") || k.Key.Contains(".") && !k.Key.StartsWith("GC.")).OrderByDescending(k => k.Value).Take(25);
            Debug.Log($"[ASBO] profile {label} garbage per frame by call: " + string.Join(" | ", scripts.Select(k => $"{k.Key} {k.Value / n / 1024f:F1}KB")));
        }
    }
}
#endif

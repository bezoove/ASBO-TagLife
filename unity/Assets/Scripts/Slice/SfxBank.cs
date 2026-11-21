using System;
using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Recorded sound effects by key (footsteps per surface, engine, crashes, pain, ambience, dogs...), built by
    /// ASBO/Audio/Build Sound Bank from the imported sound packs. Anything missing falls back to the synthesised sounds.
    /// </summary>
    public class SfxBank : ScriptableObject
    {
        [Serializable] public class Entry { public string key; public AudioClip[] clips; }
        public List<Entry> entries = new List<Entry>();

        static Dictionary<string, AudioClip[]> map;
        static readonly Dictionary<string, int> last = new Dictionary<string, int>();

        static void Load()
        {
            if (map != null) return;
            map = new Dictionary<string, AudioClip[]>();
            var b = Resources.Load<SfxBank>("SfxBank");
            if (b == null) return;
            foreach (var e in b.entries)
            {
                if (e == null || e.clips == null) continue;
                var l = new List<AudioClip>(); foreach (var c in e.clips) if (c != null) l.Add(c);
                if (l.Count > 0) map[e.key] = l.ToArray();
            }
        }

        public static bool Has(string key) { Load(); return map.ContainsKey(key); }

        /// <summary>A random clip for the key, never the same one twice running; null if the bank has none.</summary>
        public static AudioClip Pick(string key)
        {
            Load();
            if (!map.TryGetValue(key, out var a)) return null;
            if (a.Length == 1) return a[0];
            last.TryGetValue(key, out int prev);
            int i = UnityEngine.Random.Range(0, a.Length - 1); if (i >= prev) i++;
            last[key] = i;
            return a[i];
        }
    }
}

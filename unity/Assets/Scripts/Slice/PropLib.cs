using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// The library of modelled props (Assets/Art/CasinoProps, from tools/art/casino/propforge.py): each one's mesh and
    /// what it is in the game: something to pick up, sit on, use or open, or just there. Built by ASBO/Art/Build Casino
    /// Props into Resources/CasinoProps/PropLib.asset, with the two shared materials (opaque atlas, glass).
    /// </summary>
    public class PropLib : ScriptableObject
    {
        public enum Kind : byte { Static, Pickup, Sit, Use, Door }
        [System.Serializable]
        public class Entry
        {
            public string id; public Mesh mesh; public Kind kind; public float mass = 1f; public string label, action, drink, food, code;
            public float seat, cash, hinge; public int game = -1;
            public Vector3 size, center; public int tris;
        }
        public List<Entry> entries = new List<Entry>();
        public Material opaque, glass;

        static PropLib lib; static Dictionary<string, Entry> byId;
        public static PropLib I { get { if (lib == null) lib = Resources.Load<PropLib>("CasinoProps/PropLib"); return lib; } }
        public static Entry Get(string id)
        {
            if (byId == null || byId.Count == 0)
            {
                byId = new Dictionary<string, Entry>();
                if (I != null) foreach (var e in I.entries) byId[e.id] = e;
            }
            return id != null && byId.TryGetValue(id, out var x) ? x : null;
        }
        public static void Reset() { byId = null; lib = null; }

        /// <summary>The atlas rectangles (UV min, max) by name, for signs and pictures laid straight onto walls.</summary>
        public static Dictionary<string, Rect> Regions
        {
            get
            {
                if (regions != null) return regions;
                regions = new Dictionary<string, Rect>();
                var ta = Resources.Load<TextAsset>("CasinoProps/casino_regions");
                if (ta == null) return regions;
                foreach (var line in ta.text.Split('\n'))
                {
                    var f = line.Trim().Split('\t'); if (f.Length < 5) continue;
                    float P(string s) => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
                    regions[f[0]] = Rect.MinMaxRect(P(f[1]), P(f[2]), P(f[3]), P(f[4]));
                }
                return regions;
            }
        }
        static Dictionary<string, Rect> regions;
    }
}

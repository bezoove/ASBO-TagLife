using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// The different ways each fight move can be thrown, and each hit taken, so a scrap never looks like the same
    /// punch twice. Every time someone swings, one of that move's versions is picked at random.
    ///
    /// Takes lists the Mixamo versions (Assets/Mixamo/XBot, the X Bot downloads): which seconds of the take to cut,
    /// when the blow lands in the take, how fast to play it, and how close the two bodies must be for it to land
    /// (measured from the takes: how far in front of the body the fist, foot, knee or head gets, plus the depth of the
    /// other person's face or belly). The editor builds them into the controllers and writes what it built, with the
    /// timings at play speed, to Resources/Anim/FightVariants.json, which the game reads.
    /// </summary>
    public static class FightSet
    {
        public struct Take
        {
            public string trig, file; public float a, b, hit, speed, contact, w; public bool big;
        }

        public static readonly Take[] Takes =
        {
            // jab (lead hand): fast, snaps out and back
            new Take { trig = "Punch", file = "Lead Jab", a = 2.96f, b = 3.62f, hit = 3.20f, speed = 1.15f, contact = 0.85f, w = 1f },
            // cross / straight right
            new Take { trig = "Cross", file = "Cross Punch", a = 1.85f, b = 2.75f, hit = 2.13f, speed = 1.2f, contact = 0.83f, w = 1f },
            new Take { trig = "Cross", file = "Punching", a = 0.47f, b = 1.25f, hit = 0.77f, speed = 1.1f, contact = 0.82f, w = 1f },
            // hooks, both hands
            new Take { trig = "Hook", file = "Hook (1)", a = 1.15f, b = 2.0f, hit = 1.47f, speed = 1.2f, contact = 0.85f, w = 1f },
            new Take { trig = "Hook", file = "Right Hook", a = 0.55f, b = 1.5f, hit = 0.87f, speed = 1.2f, contact = 0.80f, w = 1f },
            // to the body: a knee in close
            new Take { trig = "Body", file = "Illegal Knee", a = 1.50f, b = 2.6f, hit = 2.00f, speed = 1.35f, contact = 0.68f, w = 0.8f },
            // the big one (held punch): a swinging haymaker, an uppercut, or the nut (the old overhand read as taking a punch, so it's gone)
            new Take { trig = "Hay", file = "Hook Punch", a = 1.25f, b = 2.6f, hit = 1.93f, speed = 1.25f, contact = 0.86f, w = 1.4f },
            new Take { trig = "Hay", file = "Uppercut", a = 0.10f, b = 1.33f, hit = 0.47f, speed = 1.2f, contact = 0.82f, w = 1f },
            new Take { trig = "Hay", file = "Headbutt", a = 1.30f, b = 2.5f, hit = 1.83f, speed = 1.25f, contact = 0.57f, w = 0.8f },
            // kicks: front kick, and now and then a flying one
            new Take { trig = "Kick", file = "Kicking", a = 1.40f, b = 2.6f, hit = 1.87f, speed = 1.3f, contact = 1.05f, w = 1f },
            new Take { trig = "Kick", file = "Flying Kick", a = 0f, b = 1.5f, hit = 0.53f, speed = 1.1f, contact = 1.10f, w = 0.25f },
            // hit in the head (starts on the blow)
            new Take { trig = "Struck", file = "Head Hit", a = 0.88f, b = 2.1f, speed = 1.25f, w = 1f },
            new Take { trig = "Struck", file = "Big Hit To Head", a = 0.92f, b = 2.45f, speed = 1.3f, w = 1f, big = true },
            new Take { trig = "Struck", file = "Receive Uppercut To The Face", a = 0f, b = 1.23f, speed = 1.15f, w = 1f, big = true },
            new Take { trig = "Struck", file = "Receive Uppercut To The Face (1)", a = 0.62f, b = 1.9f, speed = 1.2f, w = 1f },
            // hit in the body
            new Take { trig = "StruckBody", file = "Hit To Body", a = 0.62f, b = 1.8f, speed = 1.2f, w = 1f },
            new Take { trig = "StruckBody", file = "Rib Hit", a = 0.52f, b = 1.6f, speed = 1.2f, w = 1f },
            new Take { trig = "StruckBody", file = "Big Stomach Hit", a = 0.72f, b = 2.2f, speed = 1.2f, w = 1f, big = true },
        };

        /// <summary>Recovery after the blow lands before you can throw again (the animation carries on and blends out).</summary>
        public static float Recovery(string trig) => trig switch { "Punch" => 0.12f, "Cross" => 0.2f, "Hook" => 0.24f, "Body" => 0.25f, "Hay" => 0.5f, "Kick" => 0.22f, _ => 0.2f };

        [System.Serializable] public class Var { public string trig; public int idx; public float imp, dur, contact, w; public bool big; public string name; }
        [System.Serializable] public class Table { public Var[] v; }

        static Dictionary<string, List<Var>> byTrig;
        static void Load()
        {
            byTrig = new Dictionary<string, List<Var>>();
            var ta = Resources.Load<TextAsset>("Anim/FightVariants"); if (ta == null) return;
            Table t = null; try { t = JsonUtility.FromJson<Table>(ta.text); } catch { }
            if (t?.v == null) return;
            foreach (var v in t.v) { if (!byTrig.TryGetValue(v.trig, out var l)) byTrig[v.trig] = l = new List<Var>(); l.Add(v); }
        }

        /// <summary>A random version of this move (big: a heavy blow, prefer the big reactions). Null if none were built.</summary>
        public static Var Pick(string trig, bool big = false)
        {
            if (byTrig == null) Load();
            if (!byTrig.TryGetValue(trig, out var l) || l.Count == 0) return null;
            float tot = 0f; foreach (var v in l) tot += W(v, big);
            float r = Random.value * tot;
            foreach (var v in l) { r -= W(v, big); if (r <= 0f) return v; }
            return l[l.Count - 1];
        }
        static float W(Var v, bool big) => v.w * (v.big ? (big ? 3f : 0.35f) : 1f);
    }
}

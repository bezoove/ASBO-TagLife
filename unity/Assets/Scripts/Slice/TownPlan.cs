using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// The town's layout in one place, shared by the builders (roads, river, estates, precinct, services), the ground
    /// levelling in the editor, and the traffic and gangs at runtime.
    ///
    ///   z  290 ┌──────────────── fields and woods north of the river ────────────────┐
    ///      240 │~~~~~~~~~~~~~~~~~~~~~~~~~~ the river (flows east) ~~~~~~~~~~~~~~~~~~~~~│  bridges on Kestrel Rd and Thornbury Rd, a footbridge
    ///      190 │  Kestrel Fields   │   hospital                │   Thornbury (north)    │
    ///      125 ├═══════════════════╬════════ Northway ═════════╬════════════════════════┤  crossroads with lights at both ends, a pelican crossing
    ///      113 │  Kestrel Fields   │  precinct / community hub │   Thornbury estate     │
    ///       25 │  police station   │  Abbots Wend · manor      │ casino · fire station  │
    ///      -30 ├═══════════════════╩════════ Long Road ════════╩═══════╦════════════════┤  T-junctions with lights
    ///     -150 │  castle hill      │  Hawthorn Way · site      │       ║ industrial est.│
    ///          x -290            -160                         128     200            290
    /// </summary>
    public static class TownPlan
    {
        // ---------------------------------------------------------------- roads
        public const float Half = 4.5f, LaneOff = 2.05f, KerbW = 0.16f, Pave = 3f, RoadTop = 0.04f, KerbTop = 0.14f, PaveTop = 0.13f;
        /// <summary>Kerb radius at junction corners.</summary>
        public const float CornerR = 5f;
        /// <summary>From a junction's centre to its stop lines: past the rounded corners and the pedestrian crossing.</summary>
        public const float BoxHalf = 13.8f;
        /// <summary>The pedestrian crossing across each arm, from and to (distance from the junction centre).</summary>
        public const float CrossFrom = 10f, CrossTo = 12.8f;

        public struct Road { public string name; public Vector3 a, b; public bool exists; }
        public static readonly Road[] Roads =
        {
            new Road { name = "Long Road", a = new Vector3(-250f, 0, -30f), b = new Vector3(250f, 0, -30f), exists = true },
            new Road { name = "Kestrel Road", a = new Vector3(-160f, 0, -30f), b = new Vector3(-160f, 0, 292f) },
            new Road { name = "Thornbury Road", a = new Vector3(128f, 0, -30f), b = new Vector3(128f, 0, 292f) },
            new Road { name = "Northway", a = new Vector3(-292f, 0, 125f), b = new Vector3(292f, 0, 125f) },
            new Road { name = "Mill Lane", a = new Vector3(200f, 0, -30f), b = new Vector3(200f, 0, -150f) },
        };

        /// <summary>
        /// Side roads and entrances off a main road: the pavement stops and the tarmac runs through (a dropped kerb).
        /// side: +1 is the left of the road's a→b direction (west of a road running north, north of one running east);
        /// at: the x (east-west road) or z (north-south road) of its middle; w: its width.
        /// </summary>
        public struct Access { public string road, name; public int side; public float at, w; }
        public static readonly Access[] Accesses =
        {
            new Access { road = "Kestrel Road", name = "Kestrel Close", side = 1, at = 68f, w = 7.2f },
            new Access { road = "Thornbury Road", name = "Thornbury Way", side = -1, at = 30f, w = 7.2f },
            new Access { road = "Long Road", name = "Police yard", side = 1, at = -181f, w = 7.2f },
            new Access { road = "Long Road", name = "Fire station apron", side = 1, at = 239f, w = 22f },
            new Access { road = "Northway", name = "Hospital car park", side = 1, at = -52f, w = 7.2f },
            new Access { road = "Northway", name = "A&E", side = 1, at = 52f, w = 7.2f },
            new Access { road = "Mill Lane", name = "Wend Motors", side = -1, at = -66f, w = 14f },
            new Access { road = "Mill Lane", name = "A1 Taxis", side = -1, at = -89f, w = 12f },
            new Access { road = "Mill Lane", name = "Lock-ups", side = -1, at = -104f, w = 8f },
            new Access { road = "Mill Lane", name = "Scrapyard gate", side = -1, at = -134f, w = 8f },
            new Access { road = "Mill Lane", name = "Swap Shop", side = 1, at = -55f, w = 12f },
            new Access { road = "Mill Lane", name = "Mill Lane East", side = 1, at = -90f, w = 7.2f },
            new Access { road = "Kestrel Road", name = "Mill Street", side = -1, at = 57f, w = 6f },
            new Access { road = "Northway", name = "Car park entrance", side = 1, at = -85f, w = 7.2f },
        };

        public enum JKind { Cross, Tee, Pelican }
        public class Junction { public string name; public Vector3 p; public JKind kind; }
        public static readonly Junction[] Junctions =
        {
            new Junction { name = "Kestrel Corner", p = new Vector3(-160f, 0, -30f), kind = JKind.Tee },
            new Junction { name = "Thornbury Corner", p = new Vector3(128f, 0, -30f), kind = JKind.Tee },
            new Junction { name = "Mill Lane End", p = new Vector3(200f, 0, -30f), kind = JKind.Tee },
            new Junction { name = "Kestrel Cross", p = new Vector3(-160f, 0, 125f), kind = JKind.Cross },
            new Junction { name = "Thornbury Cross", p = new Vector3(128f, 0, 125f), kind = JKind.Cross },
            new Junction { name = "Precinct Crossing", p = new Vector3(20f, 0, 125f), kind = JKind.Pelican },
        };
        /// <summary>The old zebra on the Long Road (x).</summary>
        public const float ZebraX = 18f;
        /// <summary>Mill Lane's turning head (traffic turns round here).</summary>
        public static readonly Vector3 MillTurn = new Vector3(200f, 0, -150f);

        public static Junction JunctionAt(Vector3 p, float within = 1f)
        {
            foreach (var j in Junctions) if ((j.p - p).WithY(0).sqrMagnitude < within * within) return j;
            return null;
        }

        /// <summary>Distance from p to the nearest road centreline (and that road's index).</summary>
        public static float RoadDist(Vector3 p, out int road)
        {
            float best = float.MaxValue; road = -1;
            for (int i = 0; i < Roads.Length; i++)
            {
                Vector3 a = Roads[i].a, b = Roads[i].b, ab = b - a; float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
                float d = ((a + ab * t) - p).WithY(0).magnitude; if (d < best) { best = d; road = i; }
            }
            return best;
        }
        public static bool OnCarriageway(Vector3 p) => RoadDist(p, out _) < Half + 0.2f;
        public static bool OnRoadOrPavement(Vector3 p) => RoadDist(p, out _) < Half + KerbW + Pave + 0.3f;

        // ---------------------------------------------------------------- the river
        /// <summary>Water surface height; the banks run from 0 down to it.</summary>
        public const float WaterY = -1.35f;
        static readonly Vector3[] RiverCtl =
        {
            new Vector3(-320f, 0, 236f), new Vector3(-270f, 0, 229f), new Vector3(-215f, 0, 218f), new Vector3(-160f, 0, 213f),
            new Vector3(-112f, 0, 220f), new Vector3(-66f, 0, 236f), new Vector3(-18f, 0, 243f), new Vector3(30f, 0, 238f),
            new Vector3(80f, 0, 224f), new Vector3(128f, 0, 214f), new Vector3(172f, 0, 214f), new Vector3(222f, 0, 225f),
            new Vector3(262f, 0, 238f), new Vector3(320f, 0, 246f),
        };
        static List<Vector3> river; static List<float> riverS;

        /// <summary>The river's centreline, sampled every couple of metres (west to east, the way it flows).</summary>
        public static List<Vector3> River
        {
            get
            {
                if (river != null) return river;
                river = new List<Vector3>(); riverS = new List<float>();
                for (int i = 0; i < RiverCtl.Length - 1; i++)
                {
                    Vector3 p0 = RiverCtl[Mathf.Max(0, i - 1)], p1 = RiverCtl[i], p2 = RiverCtl[i + 1], p3 = RiverCtl[Mathf.Min(RiverCtl.Length - 1, i + 2)];
                    int n = Mathf.CeilToInt((p2 - p1).magnitude / 2f);
                    for (int k = 0; k < n; k++)
                    {
                        float t = k / (float)n, t2 = t * t, t3 = t2 * t;
                        river.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                    }
                }
                river.Add(RiverCtl[RiverCtl.Length - 1]);
                float s = 0f; riverS.Add(0f);
                for (int i = 1; i < river.Count; i++) { s += (river[i] - river[i - 1]).magnitude; riverS.Add(s); }
                return river;
            }
        }
        public static float RiverLength { get { var _ = River; return riverS[riverS.Count - 1]; } }

        /// <summary>Half the water's width at a distance s along the river: wider on the bends, narrower at the bridges.</summary>
        public static float RiverHalfWidth(float s) => 9.5f + 2.2f * Mathf.Sin(s * 0.017f + 1.3f) + 1.2f * Mathf.Sin(s * 0.051f);

        /// <summary>Distance from p to the river's centreline; s = how far along it, side = +1 north bank / -1 south.</summary>
        public static float RiverDist(Vector3 p, out float s, out float side)
        {
            var r = River; float best = float.MaxValue; s = 0f; side = 1f;
            for (int i = 0; i < r.Count - 1; i++)
            {
                Vector3 a = r[i], b = r[i + 1], ab = b - a; float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
                Vector3 c = a + ab * t; float d = (c - p).WithY(0).sqrMagnitude;
                if (d < best) { best = d; s = riverS[i] + (riverS[i + 1] - riverS[i]) * t; side = Vector3.Cross(ab, p - a).y < 0f ? 1f : -1f; }
            }
            return Mathf.Sqrt(best);
        }

        /// <summary>
        /// The ground height the river cuts to at (x, z), or 0 away from it: a deep channel, a shelf just under the
        /// water at the edge, then grassy banks rising to the fields over a few metres.
        /// </summary>
        public static float RiverGround(Vector3 p)
        {
            float d = RiverDist(p, out float s, out _); float hw = RiverHalfWidth(s);
            float bankTop = hw + 6.5f; if (d >= bankTop) return 0f;
            if (d <= hw - 2.5f) return Mathf.Lerp(-3.6f, -2.9f, Mathf.Clamp01(d / Mathf.Max(1f, hw - 2.5f))) ; // the channel
            if (d <= hw + 0.4f) return Mathf.Lerp(-2.9f, WaterY - 0.35f, Mathf.SmoothStep(0f, 1f, (d - (hw - 2.5f)) / 2.9f)); // shelving up to the water's edge
            return Mathf.Lerp(WaterY - 0.35f, 0f, Mathf.SmoothStep(0f, 1f, (d - hw - 0.4f) / (bankTop - hw - 0.4f))); // the bank
        }

        /// <summary>Where a straight road (x = const, running north) crosses the river: z of the crossing and the water's half width there.</summary>
        public static float RiverCrossingZ(float x, out float halfW)
        {
            var r = River; halfW = 10f;
            for (int i = 0; i < r.Count - 1; i++)
                if ((r[i].x - x) * (r[i + 1].x - x) <= 0f)
                {
                    float t = Mathf.InverseLerp(r[i].x, r[i + 1].x, x); float z = Mathf.Lerp(r[i].z, r[i + 1].z, t);
                    halfW = RiverHalfWidth(Mathf.Lerp(riverS[i], riverS[i + 1], t)); return z;
                }
            return float.NaN;
        }

        // ---------------------------------------------------------------- areas
        public static readonly Rect KestrelFields = new Rect(-292f, 24f, 122f, 90f);
        public static readonly Rect KestrelNorth = new Rect(-292f, 134f, 122f, 56f);
        public static readonly Rect Thornbury = new Rect(138f, 30f, 152f, 84f);
        public static readonly Rect ThornburyNorth = new Rect(138f, 134f, 152f, 56f);
        public static readonly Rect HawthornHeights = new Rect(-62f, -196f, 124f, 48f);
        public static readonly Rect Precinct = new Rect(-44f, 70f, 156f, 46f);
        public static readonly Rect PrecinctYard = new Rect(50f, 24f, 64f, 44f);
        public static readonly Rect Hospital = new Rect(-64f, 134f, 136f, 58f);
        public static readonly Rect Police = new Rect(-236f, -22f, 64f, 42f);
        public static readonly Rect FireStation = new Rect(226f, -22f, 46f, 42f);
        public static readonly Rect Industrial = new Rect(160f, -152f, 132f, 108f);

        // the streets that fill the gaps (Infill): what was bare hill and field between the districts
        /// <summary>Between Kestrel Road, Northway, the high street and Abbots Wend: shops on both main roads, a terrace, the mill and Mill Street.</summary>
        public static readonly Rect InfillKestrel = new Rect(-152.5f, 8f, 104.5f, 109.5f);
        /// <summary>North of Northway from Kestrel Road to the hospital: Kestrel Chambers, the multi-storey car park, Kestrel House flats.</summary>
        public static readonly Rect InfillNorthwayWest = new Rect(-152.5f, 132.5f, 85f, 55f);
        /// <summary>North of Northway from the hospital to Thornbury Road: a parade, the gospel hall, a terrace.</summary>
        public static readonly Rect InfillNorthwayEast = new Rect(74f, 132.5f, 46.5f, 61.5f);
        /// <summary>The Long Road / Thornbury Road corner by the manor: a parade and a few houses.</summary>
        public static readonly Rect InfillThornburyCorner = new Rect(97.5f, -22.5f, 23f, 44f);
        /// <summary>Thornbury Road's east side before the casino: a parade.</summary>
        public static readonly Rect InfillThornburyEast = new Rect(135.5f, -21f, 20f, 45f);
        public static readonly Rect[] Infill = { InfillKestrel, InfillNorthwayWest, InfillNorthwayEast, InfillThornburyCorner, InfillThornburyEast };

        /// <summary>Every new area the ground is levelled for (with the road corridors on top).</summary>
        public static readonly Rect[] Levelled = { KestrelFields, KestrelNorth, Thornbury, ThornburyNorth, HawthornHeights, Precinct, PrecinctYard, Hospital, Police, FireStation, Industrial,
            InfillKestrel, InfillNorthwayWest, InfillNorthwayEast, InfillThornburyCorner, InfillThornburyEast };

        // ---------------------------------------------------------------- gangs
        public struct Turf { public string gang, estate, tag; public Rect[] area; public Color colour; }
        public static readonly Turf[] Turfs =
        {
            new Turf { gang = "the Hawthorn lot", estate = "Hawthorn Way", tag = "HWY", area = new[] { new Rect(-82f, -196f, 164f, 158f) }, colour = new Color(0.85f, 0.15f, 0.12f) },
            new Turf { gang = "the Kestrel Crew", estate = "Kestrel Fields", tag = "KCR", area = new[] { KestrelFields, KestrelNorth }, colour = new Color(0.15f, 0.45f, 0.9f) },
            new Turf { gang = "T-Bone's lot", estate = "Thornbury", tag = "TBL", area = new[] { Thornbury, ThornburyNorth }, colour = new Color(0.95f, 0.75f, 0.1f) },
        };
        /// <summary>Which gang's ends you're standing in (-1 = nobody's).</summary>
        public static int TurfAt(Vector3 p)
        {
            for (int i = 0; i < Turfs.Length; i++) foreach (var r in Turfs[i].area) if (r.Contains(new Vector2(p.x, p.z))) return i;
            return -1;
        }
    }
}

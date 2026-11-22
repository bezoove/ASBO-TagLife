using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>
    /// The town's roads (TownPlan.Roads), built into the scene by the editor: tarmac with its markings, kerbs and
    /// pavements; junctions with rounded kerbs, stop lines, crossings with studs and red tactile paving, and UK traffic
    /// lights on poles (primary at the stop line, secondary across the junction, red and green men for the crossings);
    /// a pelican crossing with zigzags; the old zebra with Belisha beacons; lamp columns; bus shelters for the 41; phone
    /// kiosks; street name plates; and the two road bridges over the river. The Long Road is rebuilt with the rest so its
    /// junctions match. All the lamps of one junction's lights are one mesh drawn with the signal shader (one draw call),
    /// lit by <see cref="JunctionSignal"/> at runtime.
    /// </summary>
    public static class Roads
    {
        public const string RootName = "TownRoads";
        const float H = TownPlan.Half, KW = TownPlan.KerbW, PV = TownPlan.Pave, RT = TownPlan.RoadTop, KT = TownPlan.KerbTop, PT = TownPlan.PaveTop, CR = TownPlan.CornerR;
        static CouncilEstate.Mats E;

        // street name plates in the TownSigns atlas (2 x 8 cells)
        public enum Sign { KestrelRoad, Northway, ThornburyRoad, MillLane, LongRoad, KestrelFields, Thornbury, HawthornHeights, BusStop, Telephone, Precinct, Towpath, NoBallGames, Police, FireStation, AandE }
        public static Rect SignUV(Sign s) { int i = (int)s; return new Rect((i % 2) * 0.5f + 0.004f, 1f - (i / 2 + 1) * 0.125f + 0.004f, 0.492f, 0.117f); }

        public static Transform Build(Transform parent)
        {
            if (!TownMats.Load()) return null;
            E = CouncilEstate.M;
            // the old Long Road (one long box each for tarmac, kerbs and pavements) is replaced so its junctions can be cut in
            foreach (var n in new[] { "LongRoad", "Pavement", "Kerb", "EdgeLine", "Dash", "Zebra" })
                for (int guard = 0; guard < 400; guard++) { var t = parent.Find(n); if (t == null) break; Object.DestroyImmediate(t.gameObject); }
            var root = new GameObject(RootName).transform; root.SetParent(parent, false);
            foreach (var r in TownPlan.Roads) Road(root, r);
            foreach (var j in TownPlan.Junctions) JunctionAt(root, j);
            Zebra(root);
            TurningHead(root, TownPlan.MillTurn);
            foreach (var r in TownPlan.Roads) if (!r.exists && Mathf.Abs(r.a.x - r.b.x) < 0.1f) { float z = TownPlan.RiverCrossingZ(r.a.x, out float hw); if (!float.IsNaN(z)) Bridge(root, r.a.x, z, hw); }
            Furniture(root);
            Debug.Log($"[ASBO] roads: {TownPlan.Roads.Length} roads, {TownPlan.Junctions.Length} junctions with lights");
            return root;
        }

        // ------------------------------------------------------------------ straight roads
        /// <summary>Where along a road (distance from a) a junction box interrupts it, with how far each side is cut.</summary>
        static List<(float s, float cut, TownPlan.Junction j)> Cuts(TownPlan.Road r)
        {
            var list = new List<(float, float, TownPlan.Junction)>();
            Vector3 d = (r.b - r.a); float len = d.magnitude; d /= len;
            foreach (var j in TownPlan.Junctions)
            {
                Vector3 v = j.p - r.a; float s = Vector3.Dot(v, d); if (s < -0.5f || s > len + 0.5f) continue;
                if ((r.a + d * s - j.p).WithY(0).magnitude > 0.5f) continue;
                list.Add((s, j.kind == TownPlan.JKind.Pelican ? 0f : H, j));
            }
            list.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            return list;
        }

        static void Road(Transform root, TownPlan.Road r)
        {
            var g = new GameObject(r.name).transform; g.SetParent(root, false);
            var tar = new MeshKit(); var kerb = new MeshKit(); var marks = new MeshKit();
            Vector3 d = (r.b - r.a).normalized; float len = (r.b - r.a).magnitude;
            var cuts = Cuts(r);
            // carriageway: one strip between junction squares; kerbs and pavements stop at the rounded corners
            float s0 = 0f;
            foreach (var c in cuts) { if (c.cut > 0f) { Carriageway(tar, r.a + d * s0, r.a + d * (c.s - c.cut)); s0 = c.s + c.cut; } }
            Carriageway(tar, r.a + d * s0, r.b);
            foreach (int side in new[] { 1, -1 })
            {
                // breaks in this side's pavement: junction corners, and (on the Long Road) the driveways in
                var breaks = new List<(float a, float b, bool drive)>();
                foreach (var c in cuts) if (c.cut > 0f && HasArmOnSide(c.j, r, side)) breaks.Add((c.s - H - CR, c.s + H + CR, false)); // a T's straight side runs on past it
                if (r.name == "Long Road")
                {
                    // the Hawthorn Way loop's two mouths and the building site gate (south side), Abbots Wend's gate (north side)
                    var drives = side < 0 ? new[] { (-43.6f, -36.4f), (36.4f, 43.6f), (102f, 111f) } : new[] { (-3.2f, 3.2f) };
                    foreach (var (x0, x1) in drives) breaks.Add((x0 - r.a.x, x1 - r.a.x, true));
                }
                foreach (var ac in TownPlan.Accesses)
                {
                    if (ac.road != r.name || ac.side != side) continue;
                    float c = Mathf.Abs(d.x) > 0.5f ? ac.at - r.a.x : ac.at - r.a.z; c *= Mathf.Abs(d.x) > 0.5f ? Mathf.Sign(d.x) : Mathf.Sign(d.z);
                    breaks.Add((c - ac.w * 0.5f, c + ac.w * 0.5f, true));
                }
                breaks.Sort((x, y) => x.a.CompareTo(y.a));
                float p0 = 0f;
                foreach (var b in breaks)
                {
                    Footway(kerb, r.a + d * p0, r.a + d * b.a, side);
                    if (b.drive) { Vector3 l2 = new Vector3(-d.z, 0, d.x) * side; Vector3 a0 = r.a + d * b.a, a1 = r.a + d * b.b; tar.Quad(E.asphalt, a0 + l2 * H + Vector3.up * RT, a1 + l2 * H + Vector3.up * RT, a1 + l2 * (H + KW + PV) + Vector3.up * RT, a0 + l2 * (H + KW + PV) + Vector3.up * RT, Vector3.up); }
                    p0 = Mathf.Max(p0, b.b);
                }
                Footway(kerb, r.a + d * p0, r.b, side);
            }
            // markings: centre line (longer warning lines near junctions), edge lines, broken at junctions and crossings
            float last = 0f; var stops = new List<(float a, float b)>();
            foreach (var c in cuts) stops.Add((c.s - (c.j.kind == TownPlan.JKind.Pelican ? 4f : TownPlan.BoxHalf + 0.2f), c.s + (c.j.kind == TownPlan.JKind.Pelican ? 4f : TownPlan.BoxHalf + 0.2f)));
            if (r.name == "Long Road") stops.Add((TownPlan.ZebraX + 250f - 4.8f, TownPlan.ZebraX + 250f + 4.8f));
            bool Blocked(float s) { foreach (var b in stops) if (s > b.a && s < b.b) return true; return false; }
            float NearJ(float s) { float m = 999f; foreach (var b in stops) m = Mathf.Min(m, Mathf.Min(Mathf.Abs(s - b.a), Mathf.Abs(s - b.b))); return m; }
            for (float s = 2f; s < len - 2f;)
            {
                bool warn = NearJ(s) < 30f; float dash = warn ? 6f : 3f, gap = warn ? 3f : 6f;
                if (!Blocked(s) && !Blocked(s + dash)) Line(marks, r.a + d * s, r.a + d * (s + dash), 0f, 0.12f, E.line);
                s += dash + gap;
            }
            stops.Sort((x, y) => x.a.CompareTo(y.a));
            foreach (int side in new[] { 1, -1 })
            {
                float from = 0f;
                foreach (var b in stops) { if (b.a - from > 1f) Line(marks, r.a + d * from, r.a + d * b.a, side * (H - 0.3f), 0.1f, E.line); from = Mathf.Max(from, b.b); }
                if (len - from > 1f) Line(marks, r.a + d * from, r.b, side * (H - 0.3f), 0.1f, E.line);
            }
            // double yellows along the kerb near junctions (no parking at the lights)
            foreach (var c in cuts)
            {
                if (c.cut <= 0f) continue;
                foreach (int side in new[] { 1, -1 })
                    foreach (int dir in new[] { -1, 1 })
                    {
                        float a = c.s + dir * (TownPlan.BoxHalf + 0.6f), b = c.s + dir * (TownPlan.BoxHalf + 22f);
                        if (Mathf.Min(a, b) < 0f || Mathf.Max(a, b) > len) continue;
                        Line(marks, r.a + d * Mathf.Min(a, b), r.a + d * Mathf.Max(a, b), side * (H - 0.12f), 0.07f, TownMats.yellow);
                        Line(marks, r.a + d * Mathf.Min(a, b), r.a + d * Mathf.Max(a, b), side * (H - 0.27f), 0.07f, TownMats.yellow);
                    }
            }
            var t = tar.Build("Tarmac", g, true, ShadowCastingMode.Off); if (t != null) t.layer = 0;
            kerb.Build("Footways", g, true, ShadowCastingMode.Off);
            marks.Build("Markings", g, false, ShadowCastingMode.Off);
        }

        static float NextAfter(List<(float a, float b)> stops, float e)
        {
            float best = e; foreach (var b in stops) if (b.a <= e + 0.01f && b.b > best) best = b.b;
            return best;
        }

        /// <summary>Does junction j have an arm leaving road r on the given side (+1 = the left of r's direction)?</summary>
        static bool HasArmOnSide(TownPlan.Junction j, TownPlan.Road r, int side)
        {
            if (j.kind == TownPlan.JKind.Cross) return true;
            if (j.kind == TownPlan.JKind.Pelican) return false;
            Vector3 d = (r.b - r.a).normalized, left = new Vector3(-d.z, 0, d.x);
            foreach (var arm in Arms(j)) if (Mathf.Abs(Vector3.Dot(arm, d)) < 0.5f && Vector3.Dot(arm, left) * side > 0f) return true;
            return false;
        }

        /// <summary>The directions roads leave a junction in (unit vectors).</summary>
        public static List<Vector3> Arms(TownPlan.Junction j)
        {
            var arms = new List<Vector3>();
            foreach (var r in TownPlan.Roads)
            {
                Vector3 d = (r.b - r.a); float len = d.magnitude; d /= len; float s = Vector3.Dot(j.p - r.a, d);
                if (s < -0.5f || s > len + 0.5f || (r.a + d * s - j.p).WithY(0).magnitude > 0.5f) continue;
                if (s > 1f) arms.Add(-d); if (s < len - 1f) arms.Add(d);
            }
            return arms;
        }

        static void Carriageway(MeshKit k, Vector3 a, Vector3 b)
        {
            Vector3 d = (b - a); if (d.magnitude < 0.05f) return; d.Normalize(); Vector3 l = new Vector3(-d.z, 0, d.x);
            k.Quad(E.asphalt, a + l * H + Vector3.up * RT, b + l * H + Vector3.up * RT, b - l * H + Vector3.up * RT, a - l * H + Vector3.up * RT, Vector3.up);
        }

        /// <summary>Kerb and pavement along one side (+1 left of a→b, -1 right).</summary>
        static void Footway(MeshKit k, Vector3 a, Vector3 b, int side)
        {
            Vector3 d = (b - a); if (Vector3.Dot(d, d) < 0.25f) return; d.Normalize(); Vector3 l = new Vector3(-d.z, 0, d.x) * side;
            Vector3 k0 = l * H, k1 = l * (H + KW), p1 = l * (H + KW + PV);
            // kerb: face to the road, top
            k.Quad(E.kerb, a + k0, b + k0, b + k0 + Vector3.up * KT, a + k0 + Vector3.up * KT, -l);
            k.Quad(E.kerb, a + k0 + Vector3.up * KT, b + k0 + Vector3.up * KT, b + k1 + Vector3.up * KT, a + k1 + Vector3.up * KT, Vector3.up);
            k.Quad(E.kerb, a + k1 + Vector3.up * PT, b + k1 + Vector3.up * PT, b + k1 + Vector3.up * KT, a + k1 + Vector3.up * KT, l);
            // pavement: top and its back edge down to the ground
            k.Quad(E.pave, a + k1 + Vector3.up * PT, b + k1 + Vector3.up * PT, b + p1 + Vector3.up * PT, a + p1 + Vector3.up * PT, Vector3.up);
            k.Quad(E.kerb, a + p1, b + p1, b + p1 + Vector3.up * PT, a + p1 + Vector3.up * PT, l);
        }

        static void Line(MeshKit k, Vector3 a, Vector3 b, float offset, float w, Material m)
        {
            Vector3 d = (b - a); if (d.magnitude < 0.05f) return; d.Normalize(); Vector3 l = new Vector3(-d.z, 0, d.x);
            Vector3 o = l * offset + Vector3.up * (RT + 0.004f);
            k.Quad(m, a + o + l * w * 0.5f, b + o + l * w * 0.5f, b + o - l * w * 0.5f, a + o - l * w * 0.5f, Vector3.up);
        }

        // ------------------------------------------------------------------ junctions
        static void JunctionAt(Transform root, TownPlan.Junction j)
        {
            var g = new GameObject(j.name).transform; g.SetParent(root, false);
            var k = new MeshKit(); var marks = new MeshKit(); var heads = new MeshKit(); var lamps = new MeshKit();
            var arms = Arms(j);
            Vector3 c = j.p;
            if (j.kind != TownPlan.JKind.Pelican)
            {
                // the square where the carriageways cross
                k.Quad(E.asphalt, c + new Vector3(-H, RT, -H), c + new Vector3(H, RT, -H), c + new Vector3(H, RT, H), c + new Vector3(-H, RT, H), Vector3.up);
                // a rounded corner between each pair of neighbouring arms
                for (int i = 0; i < arms.Count; i++)
                    for (int q = i + 1; q < arms.Count; q++)
                        if (Mathf.Abs(Vector3.Dot(arms[i], arms[q])) < 0.1f) Corner(k, c, arms[i], arms[q]);
                // (a T's straight side: the main road's own kerb and pavement run straight past)
            }
            int sigGroup(Vector3 arm) => j.kind == TownPlan.JKind.Pelican ? 0 : j.kind == TownPlan.JKind.Cross ? (Mathf.Abs(arm.x) > 0.5f ? 0 : 1) : IsMainArm(j, arm) ? 0 : 1;
            float stopAt = j.kind == TownPlan.JKind.Pelican ? 4.2f : TownPlan.BoxHalf;
            foreach (var arm in arms)
            {
                Vector3 l = new Vector3(-arm.z, 0, arm.x); // left of the arm's outward direction
                Vector3 inTravel = -arm, inLeft = new Vector3(-inTravel.z, 0, inTravel.x); // a driver coming in: their left
                // stop line across the incoming lane (the lane on the incoming driver's left)
                Vector3 sp = c + arm * stopAt;
                StripAcross(marks, sp, arm, inLeft * 0.2f, inLeft * H, 0.3f, E.line);
                // crossing studs (two rows of squares) and red tactile paving at each end
                float c0 = j.kind == TownPlan.JKind.Pelican ? -1.5f : TownPlan.CrossFrom, c1 = j.kind == TownPlan.JKind.Pelican ? 1.5f : TownPlan.CrossTo;
                if (j.kind != TownPlan.JKind.Pelican || Vector3.Dot(arm, Vector3.right) > 0f)
                {
                    foreach (float cs in new[] { c0, c1 })
                        for (float o = -H + 0.3f; o < H - 0.2f; o += 0.6f)
                        {
                            Vector3 sq = c + arm * cs + l * o + Vector3.up * (RT + 0.005f);
                            marks.Quad(E.line, sq - arm * 0.12f - l * 0.12f, sq + arm * 0.12f - l * 0.12f, sq + arm * 0.12f + l * 0.12f, sq - arm * 0.12f + l * 0.12f, Vector3.up);
                        }
                    foreach (int s in new[] { -1, 1 })
                    {
                        Vector3 tp = c + arm * ((c0 + c1) * 0.5f) + l * s * (H + KW + 0.6f) + Vector3.up * (PT + 0.006f);
                        marks.Quad(TownMats.tarmacRed, tp - arm * 1.3f - l * 0.45f, tp + arm * 1.3f - l * 0.45f, tp + arm * 1.3f + l * 0.45f, tp - arm * 1.3f + l * 0.45f, Vector3.up);
                    }
                }
                int grp = sigGroup(arm);
                // primary signal on the near-side kerb at the stop line, facing the traffic coming in
                Vector3 prim = c + arm * (stopAt + 0.6f) + inLeft * (H + KW + 0.45f);
                SignalPole(k, heads, lamps, prim, arm, grp, true);
                // pedestrians' red and green men either side of the crossing, facing across it
                if (j.kind != TownPlan.JKind.Pelican || Vector3.Dot(arm, Vector3.right) > 0f)
                {
                    Vector3 mid = c + arm * ((c0 + c1) * 0.5f);
                    PedPole(k, heads, lamps, mid + inLeft * (H + KW + 0.45f) + arm * 1.9f, -inLeft);
                    PedPole(k, heads, lamps, mid - inLeft * (H + KW + 0.45f) - arm * 1.9f, inLeft);
                }
                // secondary signal across the junction on the far side, for the same traffic
                if (j.kind != TownPlan.JKind.Pelican)
                {
                    Vector3 sec = c - arm * (H + CR * 0.5f) - inLeft * (H + KW + 0.6f);
                    SignalPole(k, heads, lamps, sec, arm, grp, false);
                }
                // pelican: zigzags along the kerbs and the centre on the approach
                if (j.kind == TownPlan.JKind.Pelican) Zigzags(marks, c, arm, 4.6f, 16f);
            }
            k.Build("Junction", g, true, ShadowCastingMode.Off);
            marks.Build("Markings", g, false, ShadowCastingMode.Off);
            heads.Build("SignalHeads", g, false, ShadowCastingMode.On);
            var lg = lamps.Build("LiveSignalLamps", g, false, ShadowCastingMode.Off);
            if (lg != null)
            {
                var js = lg.AddComponent<JunctionSignal>(); js.kind = j.kind; js.centre = c;
            }
        }

        static bool IsMainArm(TownPlan.Junction j, Vector3 arm)
        {
            // on a T, the two arms in a straight line are the main road
            foreach (var other in Arms(j)) if (other != arm && Vector3.Dot(other, arm) < -0.9f) return true;
            return false;
        }

        static void StripAcross(MeshKit k, Vector3 at, Vector3 along, Vector3 from, Vector3 to, float w, Material m)
        {
            Vector3 y = Vector3.up * (RT + 0.005f);
            k.Quad(m, at + from - along * w * 0.5f + y, at + to - along * w * 0.5f + y, at + to + along * w * 0.5f + y, at + from + along * w * 0.5f + y, Vector3.up);
        }

        static void Zigzags(MeshKit k, Vector3 c, Vector3 arm, float from, float to)
        {
            Vector3 l = new Vector3(-arm.z, 0, arm.x);
            foreach (float off in new[] { H - 0.35f, -H + 0.35f, 0f })
            {
                float s = from; bool up = true; Vector3 prev = c + arm * s + l * off;
                while (s < to)
                {
                    s += 1.0f; Vector3 next = c + arm * s + l * (off + (up ? 0.25f : -0.25f)); up = !up;
                    Line(k, prev.WithY(0), next.WithY(0), 0f, 0.1f, E.line); prev = next;
                }
            }
        }

        /// <summary>A rounded kerb corner between two arms at right angles: tarmac fillet, curved kerb, a paved quarter.</summary>
        static void Corner(MeshKit k, Vector3 c, Vector3 a1, Vector3 a2)
        {
            Vector3 corner = c + (a1 + a2) * H, cc = c + (a1 + a2) * (H + CR);
            float ang1 = Mathf.Atan2(-a1.z, -a1.x), ang2 = Mathf.Atan2(-a2.z, -a2.x);
            float dA = Mathf.DeltaAngle(ang1 * Mathf.Rad2Deg, ang2 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            const int N = 8;
            Vector3 P(float r, int i) { float a = ang1 + dA * i / N; return cc + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r; }
            for (int i = 0; i < N; i++)
            {
                // tarmac between the carriageway corner and the kerb's curve
                k.TriN(E.asphalt, corner + Vector3.up * RT, P(CR, i) + Vector3.up * RT, P(CR, i + 1) + Vector3.up * RT, Vector3.up);
                // kerb: its face to the road and its top
                Vector3 o0 = P(CR, i), o1 = P(CR, i + 1), i0 = P(CR - KW, i), i1 = P(CR - KW, i + 1);
                Vector3 n = ((o0 + o1) * 0.5f - cc).normalized;
                k.Quad(E.kerb, o0, o1, o1 + Vector3.up * KT, o0 + Vector3.up * KT, n);
                k.Quad(E.kerb, o0 + Vector3.up * KT, o1 + Vector3.up * KT, i1 + Vector3.up * KT, i0 + Vector3.up * KT, Vector3.up);
                // the paved quarter behind it
                k.TriN(E.pave, cc + Vector3.up * PT, i0 + Vector3.up * PT, i1 + Vector3.up * PT, Vector3.up);
            }
        }

        // ------------------------------------------------------------------ signals
        /// <summary>Lamp quad in the signal atlas: group and aspect ride in the UVs' whole numbers (the shader reads them).</summary>
        static void Lamp(MeshKit lamps, Vector3 centre, Vector3 face, float size, int group, int aspect, int cell)
        {
            Vector3 r = Vector3.Cross(Vector3.up, face).normalized, u = Vector3.up; float h = size * 0.5f;
            float cu = (cell % 4) * 0.25f, cv = 0.75f - (cell / 4) * 0.25f; // 4x4 cells
            Vector2 o = new Vector2(group, aspect);
            Vector2 Uv(float x, float y) => o + new Vector2(cu + 0.01f + x * 0.23f, cv + 0.01f + y * 0.23f);
            Vector3 p = centre + face * 0.02f;
            lamps.QuadUV(TownMats.signal, p - r * h - u * h, p + r * h - u * h, p + r * h + u * h, p - r * h + u * h, face, Uv(0, 0), Uv(1, 0), Uv(1, 1), Uv(0, 1));
        }

        static void SignalPole(MeshKit k, MeshKit heads, MeshKit lamps, Vector3 at, Vector3 face, int group, bool primary)
        {
            k.Cyl(TownMats.pole, at + Vector3.up * PT, 0.065f, 3.3f, 8, true);
            Vector3 r = Vector3.Cross(Vector3.up, face).normalized;
            Vector3 hc = at + Vector3.up * 2.75f + face * 0.16f; // head centre
            // backboard (black with a white border, as UK signals have) and the head with its three hoods
            heads.Push(Matrix4x4.TRS(hc, Quaternion.LookRotation(face), Vector3.one));
            heads.Box(E.white, new Vector3(-0.33f, -0.62f, -0.06f), new Vector3(0.33f, 0.62f, -0.04f));
            heads.Box(TownMats.signalHead, new Vector3(-0.3f, -0.59f, -0.04f), new Vector3(0.3f, 0.59f, -0.03f));
            heads.Box(TownMats.signalHead, new Vector3(-0.15f, -0.52f, -0.03f), new Vector3(0.15f, 0.52f, 0.12f));
            for (int i = 0; i < 3; i++) { float y = 0.33f - i * 0.33f; heads.Box(TownMats.signalHead, new Vector3(-0.13f, y + 0.12f, 0.12f), new Vector3(0.13f, y + 0.14f, 0.26f)); }
            heads.Pop();
            for (int i = 0; i < 3; i++) Lamp(lamps, hc + face * 0.12f + Vector3.up * (0.33f - i * 0.33f), face, 0.22f, group, i, i);
            // a push-button box on the primary pole
            if (primary) k.Box(TownMats.hiVis, at + new Vector3(-0.07f, 1.05f, -0.07f) - face * 0.12f, at + new Vector3(0.07f, 1.3f, 0.07f) - face * 0.12f);
        }

        static void PedPole(MeshKit k, MeshKit heads, MeshKit lamps, Vector3 at, Vector3 face)
        {
            k.Cyl(TownMats.pole, at + Vector3.up * PT, 0.055f, 2.7f, 8, true);
            Vector3 hc = at + Vector3.up * 2.35f + face * 0.12f;
            heads.Push(Matrix4x4.TRS(hc, Quaternion.LookRotation(face), Vector3.one));
            heads.Box(TownMats.signalHead, new Vector3(-0.17f, -0.36f, -0.05f), new Vector3(0.17f, 0.36f, 0.1f));
            heads.Pop();
            Lamp(lamps, hc + face * 0.1f + Vector3.up * 0.17f, face, 0.26f, 2, 3, 3);
            Lamp(lamps, hc + face * 0.1f - Vector3.up * 0.17f, face, 0.26f, 2, 4, 4);
            k.Box(TownMats.hiVis, at + new Vector3(-0.07f, 1.0f, -0.07f) - face * 0.1f, at + new Vector3(0.07f, 1.25f, 0.07f) - face * 0.1f);
        }

        // ------------------------------------------------------------------ the zebra, Mill Lane's turning head
        static void Zebra(Transform root)
        {
            var g = new GameObject("Zebra").transform; g.SetParent(root, false);
            var marks = new MeshKit(); var k = new MeshKit(); var lamps = new MeshKit();
            float x = TownPlan.ZebraX, z = World.RoadZ;
            for (int i = 0; i < 9; i++) { float zz = z - 3.6f + i * 0.9f; marks.Quad(E.line, new Vector3(x - 1.5f, RT + 0.005f, zz - 0.225f), new Vector3(x + 1.5f, RT + 0.005f, zz - 0.225f), new Vector3(x + 1.5f, RT + 0.005f, zz + 0.225f), new Vector3(x - 1.5f, RT + 0.005f, zz + 0.225f), Vector3.up); }
            foreach (int dir in new[] { -1, 1 }) Zigzags(marks, new Vector3(x, 0, z), new Vector3(dir, 0, 0), 2.4f, 14f);
            // give-way lines (broken) a metre back from the stripes
            foreach (int dir in new[] { -1, 1 }) for (float o = -H + 0.2f; o < H; o += 1.2f) StripAcross(marks, new Vector3(x + dir * 2.6f, 0, z), Vector3.right, Vector3.forward * o, Vector3.forward * (o + 0.6f), 0.2f, E.line);
            // Belisha beacons: black and white poles, flashing amber globes
            foreach (int s in new[] { -1, 1 })
            {
                Vector3 p = new Vector3(x - 2f, PT, z + s * (H + KW + 0.5f));
                for (int b = 0; b < 8; b++) k.Cyl(b % 2 == 0 ? E.black : E.white, p + Vector3.up * (b * 0.33f), 0.06f, 0.33f, 8, false);
                // the globe is a lamp in the signal shader (group 3: the zebra's flasher)
                for (int f = 0; f < 4; f++) { Vector3 face = Quaternion.Euler(0, f * 90f, 0) * Vector3.forward; Lamp(lamps, p + Vector3.up * 2.85f + face * 0.16f, face, 0.32f, 3, 1, 5); }
                k.Sphere(TownMats.beacon, p + Vector3.up * 2.85f, Vector3.one * 0.15f, 10, 6);
            }
            marks.Build("Markings", g, false, ShadowCastingMode.Off); k.Build("Beacons", g, true, ShadowCastingMode.Off);
            var lg = lamps.Build("LiveSignalLamps", g, false, ShadowCastingMode.Off); if (lg != null) { var js = lg.AddComponent<JunctionSignal>(); js.zebra = true; js.centre = new Vector3(x, 0, z); }
        }

        static void TurningHead(Transform root, Vector3 c)
        {
            var g = new GameObject("Mill Lane Turning Head").transform; g.SetParent(root, false);
            var k = new MeshKit(); const int N = 20; float R = 10f;
            for (int i = 0; i < N; i++)
            {
                float a0 = i * Mathf.PI * 2f / N, a1 = (i + 1) * Mathf.PI * 2f / N;
                Vector3 p0 = c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * R, p1 = c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * R;
                k.TriN(E.asphalt, c + Vector3.up * RT, p0 + Vector3.up * RT, p1 + Vector3.up * RT, Vector3.up);
                Vector3 n = ((p0 + p1) * 0.5f - c).normalized;
                if (Vector3.Dot(n, Vector3.forward) > 0.75f) continue; // where Mill Lane comes in
                k.Quad(E.kerb, p0 + Vector3.up * RT, p1 + Vector3.up * RT, p1 + Vector3.up * KT, p0 + Vector3.up * KT, -n);
                k.Quad(E.kerb, p0 + Vector3.up * KT, p1 + Vector3.up * KT, p1 + n * KW + Vector3.up * KT, p0 + n * KW + Vector3.up * KT, Vector3.up);
                k.Quad(E.pave, p0 + n * KW + Vector3.up * PT, p1 + n * KW + Vector3.up * PT, p1 + n * (KW + PV) + Vector3.up * PT, p0 + n * (KW + PV) + Vector3.up * PT, Vector3.up);
            }
            k.Build("TurningHead", g, true, ShadowCastingMode.Off);
        }

        // ------------------------------------------------------------------ bridges
        /// <summary>A road bridge where a north-running road crosses the river: deck, stone parapets, abutments, a pier mid-stream.</summary>
        public static void Bridge(Transform root, float x, float zc, float hw)
        {
            var g = new GameObject("Bridge " + x).transform; g.SetParent(root, false);
            var k = new MeshKit();
            float span = hw + 7.5f, w = H + KW + PV + 0.35f, z0 = zc - span, z1 = zc + span;
            // deck: the slab under the road and its fascia
            k.Box(TownMats.panel, new Vector3(x - w - 0.3f, -1.05f, z0), new Vector3(x + w + 0.3f, 0.0f, z1));
            // parapets: coursed stone with a coping, a little pilaster at each end
            foreach (int s in new[] { -1, 1 })
            {
                float px = x + s * (w + 0.05f);
                k.Box(TownMats.stone, new Vector3(px - 0.25f, PT, z0 - 1.5f), new Vector3(px + 0.25f, 1.05f, z1 + 1.5f));
                k.Box(TownMats.panel, new Vector3(px - 0.32f, 1.05f, z0 - 1.5f), new Vector3(px + 0.32f, 1.17f, z1 + 1.5f));
                foreach (float pz in new[] { z0 - 1.5f, z1 + 1.5f }) k.Box(TownMats.stone, new Vector3(px - 0.4f, 0f, pz - 0.45f), new Vector3(px + 0.4f, 1.45f, pz + 0.45f));
                // the bridge's string course along the fascia
                k.Box(TownMats.stone, new Vector3(x + s * (w + 0.3f) - 0.15f, -0.5f, z0), new Vector3(x + s * (w + 0.3f) + 0.15f, -0.25f, z1));
            }
            // abutments down into each bank, and a pier with pointed cutwaters in the middle
            foreach (float az in new[] { z0, z1 }) k.Box(TownMats.stone, new Vector3(x - w - 0.6f, -4f, az - 1.2f), new Vector3(x + w + 0.6f, -1.05f, az + 1.2f));
            k.Box(TownMats.stone, new Vector3(x - w + 0.5f, -4.2f, zc - 1.1f), new Vector3(x + w - 0.5f, -1.05f, zc + 1.1f));
            foreach (int s in new[] { -1, 1 })
            {
                Vector3 tip = new Vector3(x + s * (w + 0.9f), 0, zc); Vector3 a = new Vector3(x + s * (w - 0.5f), 0, zc - 1.1f), b = new Vector3(x + s * (w - 0.5f), 0, zc + 1.1f);
                k.Quad(TownMats.stone, a + Vector3.up * -4.2f, tip + Vector3.up * -4.2f, tip + Vector3.up * -1.05f, a + Vector3.up * -1.05f, (tip - a).normalized + Vector3.back);
                k.Quad(TownMats.stone, tip + Vector3.up * -4.2f, b + Vector3.up * -4.2f, b + Vector3.up * -1.05f, tip + Vector3.up * -1.05f, (tip - b).normalized + Vector3.forward);
            }
            var body = k.Build("Bridge", g, true, ShadowCastingMode.On);
            // lamp columns on the parapets
            var lk = new MeshKit();
            foreach (int s in new[] { -1, 1 }) foreach (float lz in new[] { zc - span * 0.5f, zc + span * 0.5f }) HeritageLamp(lk, new Vector3(x + s * (w + 0.05f), 1.17f, lz), 3.6f);
            lk.Build("BridgeLamps", g, false, ShadowCastingMode.Off);
        }

        // ------------------------------------------------------------------ lamps, shelters, kiosks, signs
        /// <summary>A modern lamp column with a short outreach and a flat lantern, standing at p (on the pavement), reaching towards dir.</summary>
        public static void LampColumn(MeshKit k, Vector3 p, Vector3 dir, float h = 6f)
        {
            k.Cyl(TownMats.pole, p, 0.085f, h, 8, true, 0.06f);
            Vector3 top = p + Vector3.up * h;
            k.Box(TownMats.pole, Vector3.Min(top, top + dir * 0.9f) + new Vector3(-0.04f, -0.04f, -0.04f), Vector3.Max(top, top + dir * 0.9f) + new Vector3(0.04f, 0.04f, 0.04f));
            Vector3 lc = top + dir * 1.05f;
            k.Push(Matrix4x4.TRS(lc, Quaternion.LookRotation(dir), Vector3.one));
            k.Box(TownMats.pole, new Vector3(-0.17f, -0.07f, -0.35f), new Vector3(0.17f, 0.05f, 0.3f));
            k.Box(CouncilEstate.M.glow, new Vector3(-0.13f, -0.09f, -0.3f), new Vector3(0.13f, -0.07f, 0.25f));
            k.Pop();
        }

        /// <summary>A Victorian-style lamp post in dark green (the precinct, the bridges).</summary>
        public static void HeritageLamp(MeshKit k, Vector3 p, float h)
        {
            var green = CouncilEstate.M.railGreen;
            k.Cyl(green, p, 0.14f, 0.45f, 10, true, 0.1f);
            k.Cyl(green, p + Vector3.up * 0.45f, 0.07f, h - 0.45f, 8, false, 0.05f);
            Vector3 t = p + Vector3.up * h;
            k.Cyl(green, t, 0.05f, 0.15f, 8, true, 0.17f);
            k.Cyl(CouncilEstate.M.glow, t + Vector3.up * 0.15f, 0.16f, 0.42f, 8, false, 0.21f);
            k.Cyl(green, t + Vector3.up * 0.57f, 0.23f, 0.18f, 8, true, 0.02f);
        }

        static void BusShelter(MeshKit k, MeshKit glass, Vector3 p, Vector3 roadDir, Vector3 toRoad)
        {
            k.Push(Matrix4x4.TRS(p, Quaternion.LookRotation(roadDir), Vector3.one)); // local x = towards the back (away from the road) if toRoad is -x
            float side = Vector3.Dot(Vector3.Cross(Vector3.up, roadDir), -toRoad) > 0f ? 1f : -1f; // +1: local +x points away from the road
            float bx = side * 0.75f; // the back wall
            foreach (float z in new[] { -1.8f, 1.8f }) foreach (float x in new[] { -bx, bx }) k.Box(TownMats.steel, new Vector3(x - 0.04f, 0f, z - 0.04f), new Vector3(x + 0.04f, 2.45f, z + 0.04f));
            k.Box(TownMats.steel, new Vector3(-0.95f, 2.45f, -1.95f), new Vector3(0.95f, 2.55f, 1.95f));
            k.Box(TownMats.bench, new Vector3(bx - side * 0.3f - 0.2f, 0.45f, -1.2f), new Vector3(bx - side * 0.3f + 0.2f, 0.5f, 1.2f));
            k.Pop();
            glass.Push(Matrix4x4.TRS(p, Quaternion.LookRotation(roadDir), Vector3.one));
            glass.Box(TownMats.glass, new Vector3(bx - 0.02f, 0.1f, -1.8f), new Vector3(bx + 0.02f, 2.4f, 1.8f));
            foreach (float z in new[] { -1.8f, 1.8f }) glass.Box(TownMats.glass, new Vector3(-0.75f, 0.1f, z - 0.02f), new Vector3(0.75f, 2.4f, z + 0.02f));
            glass.Pop();
        }

        static void SignPlate(MeshKit k, MeshKit plates, Vector3 p, Vector3 face, Sign s, float w = 1.2f, float h = 0.3f, float y = 0.95f, bool posts = true)
        {
            Vector3 r = Vector3.Cross(Vector3.up, face).normalized;
            if (posts) foreach (float o in new[] { -w * 0.42f, w * 0.42f }) k.Box(E.black, p + r * o + new Vector3(-0.025f, 0f, -0.025f), p + r * o + new Vector3(0.025f, y + h, 0.025f));
            var uv = SignUV(s); Vector3 c = p + Vector3.up * (y + h * 0.5f);
            plates.QuadUV(TownMats.signs, c - r * w * 0.5f - Vector3.up * h * 0.5f + face * 0.03f, c + r * w * 0.5f - Vector3.up * h * 0.5f + face * 0.03f, c + r * w * 0.5f + Vector3.up * h * 0.5f + face * 0.03f, c - r * w * 0.5f + Vector3.up * h * 0.5f + face * 0.03f, face,
                new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMin, uv.yMax));
            plates.QuadUV(E.white, c - r * w * 0.5f - Vector3.up * h * 0.5f - face * 0.0f, c - r * w * 0.5f + Vector3.up * h * 0.5f, c + r * w * 0.5f + Vector3.up * h * 0.5f, c + r * w * 0.5f - Vector3.up * h * 0.5f, -face, Vector2.zero, Vector2.up, Vector2.one, Vector2.right);
        }

        /// <summary>A modern phone kiosk: steel frame, glass sides, the phone on the back panel, TELEPHONE across the top.</summary>
        static void Kiosk(MeshKit k, MeshKit glass, MeshKit plates, Vector3 p, Vector3 face)
        {
            k.Push(Matrix4x4.TRS(p, Quaternion.LookRotation(face), Vector3.one));
            foreach (float x in new[] { -0.5f, 0.5f }) foreach (float z in new[] { -0.5f, 0.5f }) k.Box(TownMats.steel, new Vector3(x - 0.04f, 0f, z - 0.04f), new Vector3(x + 0.04f, 2.3f, z + 0.04f));
            k.Box(TownMats.steel, new Vector3(-0.56f, 2.3f, -0.56f), new Vector3(0.56f, 2.55f, 0.56f));
            k.Box(TownMats.steel, new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 2.3f, -0.46f)); // back panel
            k.Box(E.black, new Vector3(-0.15f, 1.15f, -0.46f), new Vector3(0.15f, 1.6f, -0.38f)); // the phone
            k.Pop();
            glass.Push(Matrix4x4.TRS(p, Quaternion.LookRotation(face), Vector3.one));
            foreach (float x in new[] { -0.5f, 0.5f }) glass.Box(TownMats.glass, new Vector3(x - 0.01f, 0.15f, -0.46f), new Vector3(x + 0.01f, 2.3f, 0.5f));
            glass.Pop();
            SignPlate(k, plates, p + face * 0.57f + Vector3.up * 0f, face, Sign.Telephone, 0.9f, 0.18f, 2.34f, false);
        }

        static void Furniture(Transform root)
        {
            var g = new GameObject("Street Furniture").transform; g.SetParent(root, false);
            var lamps = new MeshKit(); var k = new MeshKit(); var glass = new MeshKit(); var plates = new MeshKit();
            // lamp columns along every road, alternate sides every 30 m, clear of junctions and the bridges
            foreach (var r in TownPlan.Roads)
            {
                Vector3 d = (r.b - r.a); float len = d.magnitude; d /= len; Vector3 left = new Vector3(-d.z, 0, d.x);
                int n = 0;
                for (float s = 12f; s < len - 6f; s += 15f, n++)
                {
                    Vector3 p = r.a + d * s; bool near = false;
                    foreach (var j in TownPlan.Junctions) if ((j.p - p).WithY(0).magnitude < TownPlan.BoxHalf + 4f) near = true;
                    if (Mathf.Abs(p.z - World.RoadZ) < 1f && Mathf.Abs(p.x - TownPlan.ZebraX) < 6f) near = true;
                    if (TownPlan.RiverDist(p, out float rs, out _) < TownPlan.RiverHalfWidth(rs) + 9f) near = true;
                    if (near) continue;
                    int side = (n % 2 == 0) ? 1 : -1;
                    if (r.name == "Long Road" && side == 1 && p.x > -140f && p.x < -40f) side = -1; // the shops' side of the high street keeps its own
                    if (r.name == "Long Road" && p.x > -46f && p.x < 46f && side == 1) continue; // Abbots Wend's wall
                    LampColumn(lamps, p + left * side * (H + KW + PV - 0.4f) + Vector3.up * PT, -left * side);
                }
            }
            lamps.Build("LampColumns", g, false, ShadowCastingMode.Off);
            // bus stops for the 41
            var stops = new (Vector3 p, Vector3 roadDir, Vector3 toRoad)[]
            {
                (new Vector3(40f, PT, 125f - H - KW - PV + 0.9f), Vector3.right, Vector3.forward),
                (new Vector3(4f, PT, 125f + H + KW + PV - 0.9f), Vector3.left, Vector3.back),
                (new Vector3(-100f, PT, World.RoadZ + H + KW + PV - 0.9f), Vector3.left, Vector3.back),
                (new Vector3(-12f, PT, World.RoadZ - H - KW - PV + 0.9f), Vector3.right, Vector3.forward),
                (new Vector3(-160f - H - KW - PV + 0.9f, PT, 62f), Vector3.back, Vector3.right),
                (new Vector3(128f + H + KW + PV - 0.9f, PT, 72f), Vector3.forward, Vector3.left),
            };
            foreach (var s in stops)
            {
                BusShelter(k, glass, s.p, s.roadDir, s.toRoad);
                Vector3 flag = s.p + s.toRoad * 1.9f - s.roadDir * 2.6f;
                k.Cyl(TownMats.pole, flag, 0.045f, 2.9f, 8, true);
                SignPlate(k, plates, flag + Vector3.up * 0f, -s.roadDir, Sign.BusStop, 0.5f, 0.5f, 2.35f, false);
                SignPlate(k, plates, flag, s.roadDir, Sign.BusStop, 0.5f, 0.5f, 2.35f, false);
            }
            // phone kiosks
            Kiosk(k, glass, plates, new Vector3(-6f, PT, 125f - H - KW - 2.2f), Vector3.forward);
            Kiosk(k, glass, plates, new Vector3(-170f, PT, 40f), Vector3.right);
            Kiosk(k, glass, plates, new Vector3(140f, PT, 40f), Vector3.left);
            Kiosk(k, glass, plates, new Vector3(-70f, PT, World.RoadZ + H + KW + PV - 0.7f), Vector3.back);
            // street name plates at the corners
            void Plate(Vector3 at, Vector3 face, Sign s) => SignPlate(k, plates, at + Vector3.up * PT, face, s);
            Plate(new Vector3(-160f - 9f, 0, -30f + 9.2f), Vector3.right, Sign.KestrelRoad);
            Plate(new Vector3(-160f + 9f, 0, -30f + 9.2f), Vector3.back, Sign.LongRoad);
            Plate(new Vector3(128f + 9f, 0, -30f + 9.2f), Vector3.left, Sign.ThornburyRoad);
            Plate(new Vector3(128f - 9f, 0, -30f + 9.2f), Vector3.back, Sign.LongRoad);
            Plate(new Vector3(200f + 9f, 0, -30f - 9.2f), Vector3.left, Sign.MillLane);
            Plate(new Vector3(-160f + 9.3f, 0, 125f + 9.3f), Vector3.back, Sign.Northway);
            Plate(new Vector3(-160f - 9.3f, 0, 125f - 9.3f), Vector3.right, Sign.KestrelRoad);
            Plate(new Vector3(128f - 9.3f, 0, 125f + 9.3f), Vector3.back, Sign.Northway);
            Plate(new Vector3(128f + 9.3f, 0, 125f - 9.3f), Vector3.left, Sign.ThornburyRoad);
            k.Build("Furniture", g, true, ShadowCastingMode.On);
            var gl = glass.Build("Glazing", g, false, ShadowCastingMode.Off);
            plates.Build("Plates", g, false, ShadowCastingMode.Off);
        }
    }
}

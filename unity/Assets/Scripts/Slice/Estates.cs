using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>
    /// The three gangs' estates, laid out on TownPlan's ground:
    ///  - Kestrel Fields (the Kestrel Crew), west of Kestrel Road either side of Northway: 70s brown-brick and grey
    ///    panel tower blocks round a cul-de-sac and its car park, walk-up flats, lock-ups, a caged ball court and a
    ///    playground; another tower and a row of walk-ups north of Northway.
    ///  - Thornbury (T-Bone's lot), east of Thornbury Road: two Park Hill deck-access slabs in an L, two pebble-dash
    ///    towers, a car park and lock-ups off Thornbury Way, a ball court and a playground; a tower and walk-ups north.
    ///  - Hawthorn Heights (the Hawthorn lot), south of the old Hawthorn Way estate: two towers over a ball court.
    /// Each has its access road (asphalt, kerbs, pavements, a dropped kerb into the main road), paved paths from the
    /// pavements to every front door, lamp columns, bins, benches, bollards and railings. Graffiti on each estate is
    /// its own gang's tag (seed % 3 picks the gang's row in the graffiti atlas: 0 Hawthorn, 1 Kestrel, 2 Thornbury).
    /// Built in the editor into the baked world; parked cars are put in the car parks at runtime (Dress).
    /// </summary>
    public static class Estates
    {
        public const string RootName = "Estates";
        static CouncilEstate.Mats E;
        const float RT = TownPlan.RoadTop, KT = TownPlan.KerbTop, PT = TownPlan.PaveTop;

        /// <summary>Car park bays (centre, facing) filled with parked cars at runtime.</summary>
        public static readonly List<(Vector3 p, float yaw, int gang)> Bays = new List<(Vector3, float, int)>();

        public static Transform Build(Transform parent)
        {
            if (!TownMats.Load()) return null; E = CouncilEstate.M; Bays.Clear();
            var root = new GameObject(RootName).transform; root.SetParent(parent, false);
            Kestrel(root); Thornbury(root); Hawthorn(root);
            Debug.Log($"[ASBO] estates: Kestrel Fields, Thornbury and Hawthorn Heights built ({Bays.Count} parking bays)");
            return root;
        }

        // ------------------------------------------------------------------ helpers
        /// <summary>A tower on a footprint (world rect) with its front to one side ('S' faces -z, 'N' +z, 'W' -x, 'E' +x).</summary>
        static Transform Tower(Transform p, string name, Rect fp, char front, int storeys, Towers.Style st, int gang, int n)
        {
            Place(fp, front, out var o, out float yaw, out float w, out float d);
            return Towers.Point(p, new Towers.Spec { name = name, plate = name, origin = o, yaw = yaw, w = w, d = d, storeys = storeys, seed = n * 3 + gang, style = st });
        }
        static void Place(Rect fp, char front, out Vector3 o, out float yaw, out float w, out float d)
        {
            switch (front)
            {
                case 'N': o = new Vector3(fp.xMax, 0, fp.yMax); yaw = 180f; w = fp.width; d = fp.height; break;
                case 'W': o = new Vector3(fp.xMin, 0, fp.yMax); yaw = 90f; w = fp.height; d = fp.width; break;
                case 'E': o = new Vector3(fp.xMax, 0, fp.yMin); yaw = -90f; w = fp.height; d = fp.width; break;
                default: o = new Vector3(fp.xMin, 0, fp.yMin); yaw = 0f; w = fp.width; d = fp.height; break;
            }
        }
        /// <summary>Just outside the middle of a footprint's front (where its path arrives).</summary>
        static Vector3 Door(Rect fp, char front, float outBy = 2.2f) => front switch
        {
            'N' => new Vector3(fp.center.x, 0, fp.yMax + outBy),
            'W' => new Vector3(fp.xMin - outBy, 0, fp.center.y),
            'E' => new Vector3(fp.xMax + outBy, 0, fp.center.y),
            _ => new Vector3(fp.center.x, 0, fp.yMin - outBy),
        };
        static Rect R(float x0, float z0, float x1, float z1) => new Rect(x0, z0, x1 - x0, z1 - z0);

        /// <summary>A paved footpath along a polyline: concrete flags just above the grass with a little edge.</summary>
        static void Path(MeshKit k, float w, params Vector3[] pts)
        {
            const float y = 0.06f;
            for (int i = 0; i < pts.Length - 1; i++)
            {
                Vector3 a = pts[i].WithY(0), b = pts[i + 1].WithY(0), dd = b - a; if (dd.sqrMagnitude < 0.01f) continue; dd.Normalize();
                Vector3 l = new Vector3(-dd.z, 0, dd.x) * (w * 0.5f);
                // run each piece on a little past its ends so the corners close up
                a -= dd * (w * 0.5f); b += dd * (w * 0.5f);
                k.Quad(E.slabs, a + l + Vector3.up * y, b + l + Vector3.up * y, b - l + Vector3.up * y, a - l + Vector3.up * y, Vector3.up);
                k.Quad(E.kerb, a + l, b + l, b + l + Vector3.up * y, a + l + Vector3.up * y, l.normalized);
                k.Quad(E.kerb, b - l, a - l, a - l + Vector3.up * y, b - l + Vector3.up * y, -l.normalized);
            }
        }

        /// <summary>An estate road from a to b: asphalt with kerbs and a pavement each side (none where pave = 0).</summary>
        static void Lane(MeshKit tar, MeshKit kerb, Vector3 a, Vector3 b, float half, float paveL, float paveR)
        {
            Vector3 d = (b - a).WithY(0).normalized, l = new Vector3(-d.z, 0, d.x);
            a = a.WithY(0); b = b.WithY(0);
            tar.Quad(E.asphalt, a + l * half + Vector3.up * RT, b + l * half + Vector3.up * RT, b - l * half + Vector3.up * RT, a - l * half + Vector3.up * RT, Vector3.up);
            foreach (var (side, pv) in new[] { (1f, paveL), (-1f, paveR) })
            {
                Vector3 s = l * side, k0 = s * half, k1 = s * (half + 0.15f), p1 = s * (half + 0.15f + pv);
                kerb.Quad(E.kerb, a + k0, b + k0, b + k0 + Vector3.up * KT, a + k0 + Vector3.up * KT, -s);
                kerb.Quad(E.kerb, a + k0 + Vector3.up * KT, b + k0 + Vector3.up * KT, b + k1 + Vector3.up * KT, a + k1 + Vector3.up * KT, Vector3.up);
                if (pv > 0f) kerb.Quad(E.pave, a + k1 + Vector3.up * PT, b + k1 + Vector3.up * PT, b + p1 + Vector3.up * PT, a + p1 + Vector3.up * PT, Vector3.up);
                Vector3 back = pv > 0f ? p1 : k1; float top = pv > 0f ? PT : KT;
                kerb.Quad(E.kerb, a + back, b + back, b + back + Vector3.up * top, a + back + Vector3.up * top, s);
            }
        }
        /// <summary>A flat tarmac area (a car park, a turning circle's middle): just the surface, kerbed round.</summary>
        static void Apron(MeshKit tar, MeshKit kerb, Rect r)
        {
            Vector3 a = new Vector3(r.xMin, RT, r.yMin), b = new Vector3(r.xMax, RT, r.yMin), c = new Vector3(r.xMax, RT, r.yMax), d = new Vector3(r.xMin, RT, r.yMax);
            tar.Quad(E.asphalt, a, b, c, d, Vector3.up);
        }
        /// <summary>A kerb along one edge of an apron (a to b, the kerb's face towards 'into').</summary>
        static void KerbRun(MeshKit kerb, Vector3 a, Vector3 b, Vector3 into)
        {
            Vector3 d = (b - a).WithY(0).normalized; Vector3 o = -into.normalized * 0.15f;
            a = a.WithY(0); b = b.WithY(0);
            kerb.Quad(E.kerb, a, b, b + Vector3.up * KT, a + Vector3.up * KT, into.normalized);
            kerb.Quad(E.kerb, a + Vector3.up * KT, b + Vector3.up * KT, b + o + Vector3.up * KT, a + o + Vector3.up * KT, Vector3.up);
            kerb.Quad(E.kerb, b + o, a + o, a + o + Vector3.up * KT, b + o + Vector3.up * KT, -into.normalized);
        }
        /// <summary>A row of parking bays: n bays side by side from 'start' along 'along', each 'depth' deep towards 'inward' (the bay's back).</summary>
        static void BayRow(MeshKit marks, Vector3 start, Vector3 along, Vector3 inward, int n, int gang)
        {
            const float bw = 2.5f, depth = 4.8f, lw = 0.1f, y = RT + 0.005f;
            along = along.normalized; inward = inward.normalized;
            for (int i = 0; i <= n; i++)
            {
                Vector3 p = start + along * (i * bw);
                marks.Quad(E.line, (p - along * lw * 0.5f).WithY(y), (p + along * lw * 0.5f).WithY(y), (p + along * lw * 0.5f + inward * depth).WithY(y), (p - along * lw * 0.5f + inward * depth).WithY(y), Vector3.up);
                if (i < n) Bays.Add((start + along * ((i + 0.5f) * bw) + inward * (depth * 0.5f), Quaternion.LookRotation(inward).eulerAngles.y, gang));
            }
        }

        static void Bin(MeshKit k, Vector3 p, float yaw) // a council litter bin on a post
        {
            k.Push(p, yaw);
            k.Cyl(E.black, Vector3.zero, 0.04f, 0.6f, 6);
            k.Cyl(E.railGreen != null ? E.railGreen : E.black, new Vector3(0, 0.45f, 0), 0.22f, 0.55f, 10, true);
            k.Pop();
        }
        static void Eurobin(MeshKit k, Vector3 p, float yaw) // the big communal wheelie bins outside the flats
        {
            k.Push(p, yaw);
            k.Box(TownMats.steel, new Vector3(-0.65f, 0.15f, -0.5f), new Vector3(0.65f, 1.25f, 0.5f));
            k.Box(E.black, new Vector3(-0.7f, 1.25f, -0.55f), new Vector3(0.7f, 1.35f, 0.55f));
            foreach (float x in new[] { -0.5f, 0.5f }) foreach (float z in new[] { -0.38f, 0.38f }) k.Cyl(E.black, new Vector3(x, 0f, z), 0.08f, 0.15f, 6);
            k.Pop();
        }
        static void Bench(MeshKit k, Vector3 c, float yaw)
        {
            k.Push(c, yaw);
            k.Box(E.fence, new Vector3(-0.9f, 0.42f, -0.22f), new Vector3(0.9f, 0.47f, 0.22f));
            k.Box(E.fence, new Vector3(-0.9f, 0.5f, 0.18f), new Vector3(0.9f, 0.85f, 0.22f));
            foreach (float x in new[] { -0.75f, 0.75f }) k.Box(E.black, new Vector3(x - 0.04f, 0f, -0.2f), new Vector3(x + 0.04f, 0.45f, 0.22f));
            k.Pop();
        }
        static void Bollards(MeshKit k, Vector3 a, Vector3 b, float every = 1.6f)
        {
            float len = (b - a).magnitude; int n = Mathf.Max(1, Mathf.RoundToInt(len / every));
            for (int i = 0; i <= n; i++) { Vector3 p = Vector3.Lerp(a, b, i / (float)n).WithY(0); k.Cyl(E.black, p, 0.09f, 0.9f, 8, true); k.Cyl(E.white, p + Vector3.up * 0.7f, 0.095f, 0.08f, 8, false); }
        }
        /// <summary>Knee-high steel railings (the "keep off the grass" kind) from a to b.</summary>
        static void Railing(MeshKit k, Vector3 a, Vector3 b)
        {
            a = a.WithY(0); b = b.WithY(0); Vector3 d = (b - a); float len = d.magnitude; if (len < 0.5f) return; d /= len;
            int n = Mathf.Max(1, Mathf.RoundToInt(len / 2f));
            for (int i = 0; i <= n; i++) { Vector3 p = a + d * (len * i / n); k.Box(E.railGreen, p + new Vector3(-0.03f, 0f, -0.03f), p + new Vector3(0.03f, 0.75f, 0.03f)); }
            Vector3 s = new Vector3(-d.z, 0, d.x) * 0.02f;
            foreach (float y in new[] { 0.3f, 0.7f }) k.Box(E.railGreen, Vector3.Min(a, b) - new Vector3(0.02f, 0, 0.02f) + Vector3.up * y + Vector3.Min(s, -s), Vector3.Max(a, b) + new Vector3(0.02f, 0, 0.02f) + Vector3.up * (y + 0.04f) + Vector3.Max(s, -s));
        }
        static void Lamp(MeshKit k, Vector3 p, Vector3 toward) => Roads.LampColumn(k, p.WithY(0), toward.WithY(0).normalized, 6f);

        /// <summary>The estate's sign by its entrance: a white board on two posts with the estate's name in the sign atlas.</summary>
        static void Sign(MeshKit k, Vector3 p, float yaw)
        {
            k.Push(p, yaw);
            foreach (float x in new[] { -0.9f, 0.9f }) k.Box(E.post != null ? E.post : E.black, new Vector3(x - 0.04f, 0f, -0.04f), new Vector3(x + 0.04f, 1.6f, 0.04f));
            k.Box(E.white, new Vector3(-1.1f, 1.0f, -0.03f), new Vector3(1.1f, 1.6f, 0.03f));
            k.Box(TownMats.nhsBlue != null ? TownMats.nhsBlue : E.black, new Vector3(-1.1f, 1.48f, -0.035f), new Vector3(1.1f, 1.6f, 0.035f));
            k.Pop();
        }

        // ------------------------------------------------------------------ Kestrel Fields
        static void Kestrel(Transform root)
        {
            const int G = 1; var g = new GameObject("Kestrel Fields").transform; g.SetParent(root, false);
            var tar = new MeshKit(); var kerb = new MeshKit(); var marks = new MeshKit(); var paths = new MeshKit(); var bits = new MeshKit();
            const float edge = -160f - 7.66f; // the back of Kestrel Road's west pavement
            // Kestrel Close: in off Kestrel Road at z 68, west to a turning head
            const float cz = 68f;
            Lane(tar, kerb, new Vector3(edge, 0, cz), new Vector3(-258f, 0, cz), 3.2f, 2f, 2f);
            Apron(tar, kerb, R(-270f, cz - 8f, -258f, cz + 8f)); // the turning head
            KerbRun(kerb, new Vector3(-270f, 0, cz - 8f), new Vector3(-270f, 0, cz + 8f), Vector3.right);
            KerbRun(kerb, new Vector3(-258f, 0, cz + 8f), new Vector3(-270f, 0, cz + 8f), Vector3.back);
            KerbRun(kerb, new Vector3(-270f, 0, cz - 8f), new Vector3(-258f, 0, cz - 8f), Vector3.forward);
            // the car park on the north side: two rows of bays off an aisle
            var park = R(-236f, cz + 5.4f, -192f, cz + 22f);
            Apron(tar, kerb, park);
            BayRow(marks, new Vector3(-235f, 0, cz + 5.4f + 0.2f), Vector3.right, Vector3.forward, 17, G);
            BayRow(marks, new Vector3(-235f, 0, park.yMax - 0.2f), Vector3.right, Vector3.back, 17, G);
            KerbRun(kerb, new Vector3(park.xMin, 0, park.yMax), new Vector3(park.xMax, 0, park.yMax), Vector3.back);
            KerbRun(kerb, new Vector3(park.xMin, 0, park.yMin), new Vector3(park.xMin, 0, park.yMax), Vector3.right);
            KerbRun(kerb, new Vector3(park.xMax, 0, park.yMax), new Vector3(park.xMax, 0, park.yMin), Vector3.left);

            // the towers: Kestrel House (brown brick) and Merlin House (grey panels) north of the close, Harrier House south
            var kh = R(-232f, 96f, -214f, 114f); Tower(g, "Kestrel House", kh, 'S', 16, Towers.Style.BrownBrick, G, 1);
            var mh = R(-286f, 88f, -266f, 104f); Tower(g, "Merlin House", mh, 'E', 13, Towers.Style.GreyPanel, G, 2);
            var hh = R(-252f, 34f, -234f, 52f); Tower(g, "Harrier House", hh, 'N', 16, Towers.Style.BrownBrick, G, 3);
            Path(paths, 2.4f, Door(kh, 'S'), new Vector3(kh.center.x, 0, park.yMax + 0.5f));
            Path(paths, 2.4f, Door(mh, 'E'), new Vector3(-258f, 0, mh.center.y), new Vector3(-258f, 0, cz + 3.4f));
            Path(paths, 2.4f, Door(hh, 'N'), new Vector3(hh.center.x, 0, cz - 5.4f));
            // walk-ups on the south side, lock-ups behind them, a ball court and a playground
            Towers.WalkUp(g, "Peregrine Walk", new Vector3(-186f, 0, 58f), 180f, 3, 4 * 3 + G, false); // x -222..-186, z 48..58, faces the close
            Path(paths, 2f, new Vector3(-222f, 0, 59.6f), new Vector3(-186f, 0, 59.6f));
            Towers.Garages(g, "Kestrel Lock-ups", new Vector3(-262f, 0, 56f), 180f, 8, 5 * 3 + G); // x -285..-262, faces north onto a yard
            Apron(tar, kerb, R(-286f, 56f, -262f, cz - 3.35f));
            Towers.BallCourt(g, "Kestrel Cage", new Vector3(-205f, 0, 33f), 0f);
            Path(paths, 2f, new Vector3(-205f, 0, 42.5f), new Vector3(-205f, 0, 46.6f));
            Towers.Playground(g, "Kestrel Swings", new Vector3(-180f, 0, 92f), 90f);
            Path(paths, 2f, new Vector3(edge, 0, 92f), new Vector3(-173.5f, 0, 92f)); // to the playground gate
            Path(paths, 2f, new Vector3(edge, 0, 104f), new Vector3(-214f, 0, 104f));
            // a cut-through from the close to Northway (worn into the grass)
            Path(paths, 1.6f, new Vector3(-245f, 0, cz + 3.4f), new Vector3(-245f, 0, 92f), new Vector3(-240f, 0, 117.3f));

            // north of Northway: Kestrel Point, a row of walk-ups, another playground
            var kp = R(-216f, 150f, -198f, 168f); Tower(g, "Kestrel Point", kp, 'S', 18, Towers.Style.BrownBrick, G, 6);
            Path(paths, 2.4f, new Vector3(kp.center.x, 0, 132.7f), Door(kp, 'S'));
            Towers.WalkUp(g, "Falcon Row", new Vector3(-286f, 0, 142f), 0f, 4, 7 * 3 + G, true); // x -286..-238, z 142..152
            Path(paths, 2f, new Vector3(-286f, 0, 140f), new Vector3(-238f, 0, 140f));
            for (int e = 0; e < 4; e++) Path(paths, 1.6f, new Vector3(-280f + e * 12f, 0, 132.7f), new Vector3(-280f + e * 12f, 0, 140f));
            Towers.Playground(g, "Falcon Play", new Vector3(-258f, 0, 172f), 0f);
            Towers.Garages(g, "Kestrel North Lock-ups", new Vector3(-190f, 0, 182f), 180f, 6, 8 * 3 + G);

            // furniture: lamps along the close and the paths, bins, benches, bollards, communal bins, the estate sign
            for (float x = edge - 6f; x > -256f; x -= 16f) { Lamp(bits, new Vector3(x, 0, cz + 5.2f), Vector3.back); Lamp(bits, new Vector3(x - 8f, 0, cz - 5.2f), Vector3.forward); }
            foreach (var p in new[] { new Vector3(-223f, 0, 88f), new Vector3(-262f, 0, 80f), new Vector3(-243f, 0, 56f), new Vector3(-207f, 0, 150f), new Vector3(-250f, 0, 138f) }) Lamp(bits, p, Vector3.forward);
            Bin(bits, new Vector3(-226f, 0, 91f), 0f); Bin(bits, new Vector3(-238f, 0, 56f), 0f); Bin(bits, new Vector3(-200f, 0, 145f), 0f); Bin(bits, new Vector3(-190f, 0, 61f), 0f);
            Bench(bits, new Vector3(-196f, 0, 44f), 180f); Bench(bits, new Vector3(-170.5f, 0, 92f), -90f); Bench(bits, new Vector3(-262f, 0, 176f), 0f);
            for (int i = 0; i < 3; i++) Eurobin(bits, new Vector3(-236f + i * 1.6f, 0, 92f), 180f);
            for (int i = 0; i < 2; i++) Eurobin(bits, new Vector3(-238f + i * 1.6f, 0, 53f), 0f);
            for (int i = 0; i < 2; i++) Eurobin(bits, new Vector3(-196f - i * 1.6f, 0, 146f), 180f);
            Bollards(bits, new Vector3(-246.6f, 0, 74f), new Vector3(-243.4f, 0, 74f)); // stops cars up the cut-through
            Railing(bits, new Vector3(edge - 1f, 0, 75f), new Vector3(edge - 1f, 0, 88f));
            Sign(bits, new Vector3(edge - 1.2f, 0, cz + 6.5f), 90f);

            tar.Build("Tarmac", g, true, ShadowCastingMode.Off); kerb.Build("Kerbs", g, true, ShadowCastingMode.Off);
            marks.Build("Markings", g, false, ShadowCastingMode.Off); paths.Build("Paths", g, true, ShadowCastingMode.Off);
            bits.Build("Furniture", g, true, ShadowCastingMode.On);
        }

        // ------------------------------------------------------------------ Thornbury
        static void Thornbury(Transform root)
        {
            const int G = 2; var g = new GameObject("Thornbury").transform; g.SetParent(root, false);
            var tar = new MeshKit(); var kerb = new MeshKit(); var marks = new MeshKit(); var paths = new MeshKit(); var bits = new MeshKit();
            const float edge = 128f + 7.66f; // the back of Thornbury Road's east pavement
            // Thornbury Way: in off Thornbury Road at z 30, east along the estate's south edge (behind the casino)
            const float wz = 30f;
            Lane(tar, kerb, new Vector3(edge, 0, wz), new Vector3(282f, 0, wz), 3.2f, 2f, 1.2f);
            Apron(tar, kerb, R(276f, wz - 8f, 288f, wz + 8f));
            KerbRun(kerb, new Vector3(288f, 0, wz + 8f), new Vector3(288f, 0, wz - 8f), Vector3.left);
            // the car park north of the Way
            var park = R(170f, wz + 5.4f, 234f, wz + 22f);
            Apron(tar, kerb, park);
            BayRow(marks, new Vector3(171f, 0, park.yMin + 0.2f), Vector3.right, Vector3.forward, 25, G);
            BayRow(marks, new Vector3(171f, 0, park.yMax - 0.2f), Vector3.right, Vector3.back, 25, G);
            KerbRun(kerb, new Vector3(park.xMin, 0, park.yMax), new Vector3(park.xMax, 0, park.yMax), Vector3.back);
            KerbRun(kerb, new Vector3(park.xMin, 0, park.yMin), new Vector3(park.xMin, 0, park.yMax), Vector3.right);
            KerbRun(kerb, new Vector3(park.xMax, 0, park.yMax), new Vector3(park.xMax, 0, park.yMin), Vector3.left);

            // the decks: Thornbury Deck along the road (front to the west), Bone Deck across the top
            Towers.Slab(g, "Thornbury Deck", new Vector3(148f, 0, 110f), 90f, 66f, 8, 1 * 3 + G);   // x 148..159, z 44..110
            Towers.Slab(g, "Bone Deck", new Vector3(174f, 0, 98f), 0f, 60f, 7, 2 * 3 + G);         // x 174..234, z 98..109
            Path(paths, 2.4f, new Vector3(edge, 0, 52f), new Vector3(146f, 0, 52f), new Vector3(146f, 0, 108f));
            Path(paths, 2.4f, new Vector3(edge, 0, 90f), new Vector3(146f, 0, 90f));
            Path(paths, 2.4f, new Vector3(160.5f, 0, 96f), new Vector3(236f, 0, 96f));
            Path(paths, 2.4f, new Vector3(160.5f, 0, 44f), new Vector3(160.5f, 0, 96f));
            // the pebble-dash towers
            var bp = R(180f, 64f, 198f, 82f); Tower(g, "Bramble Point", bp, 'S', 14, Towers.Style.Pebble, G, 3);
            var tt = R(250f, 66f, 268f, 84f); Tower(g, "Thornbury Tower", tt, 'W', 16, Towers.Style.Pebble, G, 4);
            Path(paths, 2.4f, Door(bp, 'S'), new Vector3(bp.center.x, 0, park.yMax + 0.5f));
            Path(paths, 2.4f, Door(tt, 'W'), new Vector3(242f, 0, tt.center.y), new Vector3(242f, 0, wz + 3.4f));
            Path(paths, 2f, new Vector3(160.5f, 0, 73f), new Vector3(bp.xMin, 0, 73f));
            Towers.Garages(g, "Thornbury Lock-ups", new Vector3(244f, 0, 37f), 0f, 10, 5 * 3 + G); // x 244..273, faces the Way
            Towers.BallCourt(g, "Thornbury Cage", new Vector3(223f, 0, 75f), 90f);
            Towers.Playground(g, "Thornbury Swings", new Vector3(275f, 0, 100f), 0f);
            Path(paths, 2f, new Vector3(236f, 0, 96f), new Vector3(275f, 0, 96f), new Vector3(275f, 0, 94f));

            // north of Northway
            var tn = R(160f, 146f, 178f, 164f); Tower(g, "Thorn House", tn, 'S', 13, Towers.Style.Pebble, G, 6);
            Path(paths, 2.4f, new Vector3(tn.center.x, 0, 132.7f), Door(tn, 'S'));
            Towers.WalkUp(g, "Bramble Row", new Vector3(200f, 0, 142f), 0f, 4, 7 * 3 + G, true); // x 200..248, z 142..152
            Path(paths, 2f, new Vector3(200f, 0, 140f), new Vector3(248f, 0, 140f));
            for (int e = 0; e < 4; e++) Path(paths, 1.6f, new Vector3(206f + e * 12f, 0, 132.7f), new Vector3(206f + e * 12f, 0, 140f));
            Towers.Garages(g, "Thornbury North Lock-ups", new Vector3(260f, 0, 142f), 0f, 6, 8 * 3 + G);

            for (float x = edge + 6f; x < 276f; x += 16f) { Lamp(bits, new Vector3(x, 0, wz + 5.2f), Vector3.back); }
            foreach (var p in new[] { new Vector3(144.2f, 0, 70f), new Vector3(144.2f, 0, 100f), new Vector3(200f, 0, 94.5f), new Vector3(240f, 0, 60f), new Vector3(169f, 0, 140f), new Vector3(224f, 0, 138f) }) Lamp(bits, p, Vector3.right);
            Bin(bits, new Vector3(144f, 0, 54.5f), 0f); Bin(bits, new Vector3(196f, 0, 62f), 0f); Bin(bits, new Vector3(240f, 0, 96f), 0f); Bin(bits, new Vector3(166f, 0, 140f), 0f);
            Bench(bits, new Vector3(223f, 0, 62f), 180f); Bench(bits, new Vector3(268f, 0, 95f), 0f);
            for (int i = 0; i < 3; i++) Eurobin(bits, new Vector3(163.2f, 0, 60f + i * 1.6f), 90f);
            for (int i = 0; i < 3; i++) Eurobin(bits, new Vector3(200f + i * 1.6f, 0, 93.5f), 180f);
            for (int i = 0; i < 2; i++) Eurobin(bits, new Vector3(182f + i * 1.6f, 0, 143.5f), 180f);
            Railing(bits, new Vector3(edge + 1f, 0, 36f), new Vector3(edge + 1f, 0, 50f));
            Railing(bits, new Vector3(edge + 1f, 0, 54f), new Vector3(edge + 1f, 0, 88f));
            Sign(bits, new Vector3(edge + 1.2f, 0, wz + 6.5f), -90f);

            tar.Build("Tarmac", g, true, ShadowCastingMode.Off); kerb.Build("Kerbs", g, true, ShadowCastingMode.Off);
            marks.Build("Markings", g, false, ShadowCastingMode.Off); paths.Build("Paths", g, true, ShadowCastingMode.Off);
            bits.Build("Furniture", g, true, ShadowCastingMode.On);
        }

        // ------------------------------------------------------------------ Hawthorn Heights
        static void Hawthorn(Transform root)
        {
            const int G = 0; var g = new GameObject("Hawthorn Heights").transform; g.SetParent(root, false);
            var tar = new MeshKit(); var kerb = new MeshKit(); var marks = new MeshKit(); var paths = new MeshKit(); var bits = new MeshKit();
            var hh = R(-50f, -186f, -32f, -168f); Tower(g, "Hawthorn Heights", hh, 'N', 17, Towers.Style.BrownBrick, G, 1);
            var rc = R(20f, -186f, 40f, -170f); Tower(g, "Rowan Court", rc, 'N', 12, Towers.Style.GreyPanel, G, 2);
            Towers.BallCourt(g, "Hawthorn Cage", new Vector3(-5f, 0, -170f), 0f);
            Towers.Garages(g, "Hawthorn Lock-ups", new Vector3(-12f, 0, -188f), 0f, 8, 3 * 3 + G); // faces south onto a yard
            // a paved square between the towers (where the lot hang about), paths to the doors and up to the old estate
            for (float x = -28f; x < 16f; x += 4f) Path(paths, 10f, new Vector3(x, 0, -155f), new Vector3(Mathf.Min(16f, x + 4f), 0, -155f));
            Path(paths, 2.4f, Door(hh, 'N'), new Vector3(hh.center.x, 0, -155f), new Vector3(-28f, 0, -155f));
            Path(paths, 2.4f, Door(rc, 'N'), new Vector3(rc.center.x, 0, -155f), new Vector3(16f, 0, -155f));
            Path(paths, 2.4f, new Vector3(0f, 0, -150f), new Vector3(0f, 0, -104f)); // the old estate's path, on south
            foreach (var p in new[] { new Vector3(-41f, 0, -158f), new Vector3(30f, 0, -158f), new Vector3(-30f, 0, -150.5f), new Vector3(18f, 0, -150.5f), new Vector3(1.6f, 0, -125f) }) Lamp(bits, p, Vector3.forward);
            Bin(bits, new Vector3(-36f, 0, -163.5f), 0f); Bin(bits, new Vector3(26f, 0, -163.5f), 0f); Bin(bits, new Vector3(-2f, 0, -151f), 0f);
            Bench(bits, new Vector3(-10f, 0, -151f), 0f); Bench(bits, new Vector3(6f, 0, -151f), 0f); Bench(bits, new Vector3(-23f, 0, -164f), 90f);
            for (int i = 0; i < 3; i++) Eurobin(bits, new Vector3(-30f + i * 1.6f, 0, -165f), 0f);
            tar.Build("Tarmac", g, true, ShadowCastingMode.Off); kerb.Build("Kerbs", g, true, ShadowCastingMode.Off);
            marks.Build("Markings", g, false, ShadowCastingMode.Off); paths.Build("Paths", g, true, ShadowCastingMode.Off);
            bits.Build("Furniture", g, true, ShadowCastingMode.On);
        }

        // ------------------------------------------------------------------ runtime
        /// <summary>Parked cars in the estate car parks (about two in three bays taken: tired, dirty, the odd wreck).</summary>
        public static void Dress(Transform parent)
        {
            if (Bays.Count == 0) RebuildBays();
            var root = new GameObject("Estate Cars").transform; root.SetParent(parent, false);
            var r = new System.Random(777); int n = 0;
            string[] kinds = { "hatch90", "hatch90", "hatch90", "saloon90", "saloon90", "saloon90", "boxy80", "supermini", "hatch70", "estate90", "coupe80", "jeep" }; // mostly the detailed hatch and saloon
            foreach (var (p, yaw, gang) in Bays)
            {
                if (r.NextDouble() > 0.62) continue;
                var id = kinds[r.Next(kinds.Length)]; double t = r.NextDouble();
                var wear = t < 0.3 ? CarKit.Wear.Tired : t < 0.6 ? CarKit.Wear.Clean : t < 0.82 ? CarKit.Wear.Dirty : t < 0.95 ? CarKit.Wear.Rusty : CarKit.Wear.Wreck;
                if (CarKit.Scenery(root, id, wear, p + Vector3.up * 0.05f, yaw + (r.NextDouble() < 0.5 ? 180f : 0f) + (float)(r.NextDouble() - 0.5) * 6f, r.Next()) != null) n++;
            }
            Debug.Log($"[ASBO] estates: {n} cars parked");
        }
        /// <summary>The bays again without building anything (at runtime the estates are already in the baked world).</summary>
        static void RebuildBays()
        {
            void Row(Vector3 s, Vector3 along, Vector3 inward, int n, int gang) { along = along.normalized; inward = inward.normalized; for (int i = 0; i < n; i++) Bays.Add((s + along * ((i + 0.5f) * 2.5f) + inward * 2.4f, Quaternion.LookRotation(inward).eulerAngles.y, gang)); }
            Row(new Vector3(-235f, 0, 68f + 5.6f), Vector3.right, Vector3.forward, 17, 1); Row(new Vector3(-235f, 0, 68f + 21.8f), Vector3.right, Vector3.back, 17, 1);
            Row(new Vector3(171f, 0, 30f + 5.6f), Vector3.right, Vector3.forward, 25, 2); Row(new Vector3(171f, 0, 30f + 21.8f), Vector3.right, Vector3.back, 25, 2);
            for (int i = 0; i < 20; i++) { float z = 137f + i * 2.5f + 1.25f; Bays.Add((new Vector3(-59.6f, 0, z), 90f, -1)); Bays.Add((new Vector3(-44.4f, 0, z), -90f, -1)); } // the hospital car park
        }
    }
}

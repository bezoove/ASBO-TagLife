using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Hawthorn Rise, the building site along the main road east of the estate: a half-built four-storey block of flats
    /// (concrete frame, blockwork going up, rebar sticking out of the top), scaffolding on two faces with boards and
    /// ladders, a tower crane that slews about, two stacked site cabins and a portaloo, an excavator, a dumper, a skip,
    /// a sand heap, pallets of bricks and blocks and a cement mixer, on mud and hardcore behind developer hoardings and
    /// mesh fencing. Built into the scene by the editor (with the estate); <see cref="CraneSlew"/> turns the crane at Play.
    /// </summary>
    public static class ConstructionSite
    {
        public const string RootName = "HawthornRise";
        /// <summary>The site inside its fence: x min/max, z min/max.</summary>
        public static readonly Rect Area = new Rect(96f, -96f, 54f, 55f);
        public static readonly Rect Block = new Rect(118f, -74f, 24f, 14f);
        public const float GateX0 = 103f, GateX1 = 110f, FrontZ = -41f;
        public static readonly Vector3 CraneBase = new Vector3(112f, 0f, -80f);
        public const int Storeys = 4; public const float StoreyH = 3f;

        class Mats { public Material mud, hardcore, concrete, blocks, brick, steel, rebar, boards, heras, lattice, hoarding, safety, yellow, cabin, cabinTrim, windows, black, blue, glass, sand, timber, rubber, red, puddle; }
        static Mats M;
        static Material L(string n) => Resources.Load<Material>("Site/" + n);
        static Material E(string n) => Resources.Load<Material>("Estate/" + n);

        static bool LoadMats()
        {
            M = new Mats
            {
                mud = L("SiteMud"), hardcore = L("SiteHardcore"), concrete = E("EstateConcrete"), blocks = L("SiteBlocks"), brick = E("EstateBrick"), steel = E("EstateLampColumn"),
                rebar = L("SiteRebar"), boards = L("SiteBoards"), heras = L("SiteHeras"), lattice = L("SiteLattice"), hoarding = L("SiteHoarding"), safety = L("SiteSafety"),
                yellow = L("SiteYellow"), cabin = L("SiteCabin"), cabinTrim = L("SiteCabinTrim"), windows = E("EstateWindows"), black = E("EstateBlack"), blue = L("SiteBlue"),
                sand = L("SiteSand"), timber = E("EstateFence"), rubber = E("EstateFelt"), red = E("EstatePlayRed"), puddle = L("SitePuddle"),
            };
            if (M.mud == null || M.lattice == null || M.concrete == null) { Debug.LogWarning("[ASBO] site: materials missing (run ASBO/Art/Build Council Estate)"); return false; }
            void T(Material m, float metres) { if (m != null) MeshKit.Tile[m] = metres; }
            T(M.mud, 2f); T(M.hardcore, 2f); T(M.blocks, 2f); T(M.concrete, 1.55f); T(M.brick, 1.8f); T(M.timber, 1.89f);
            return true;
        }

        public static Transform Build(Transform parent)
        {
            if (!LoadMats()) return null;
            var root = new GameObject(RootName).transform; root.SetParent(parent, false);
            Ground(root); Perimeter(root); Flats(root); Scaffold(root); Crane(root); Cabins(root); Plant(root);
            Debug.Log("[ASBO] site: Hawthorn Rise built");
            return root;
        }

        static GameObject Done(MeshKit k, string name, Transform parent, bool collider = false, bool shadows = true)
        {
            var g = k.Build(name, parent, collider, shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off);
            return g;
        }
        static void Col(GameObject g, Vector3 centre, Vector3 size) { var bc = g.AddComponent<BoxCollider>(); bc.center = centre; bc.size = size; }

        // ------------------------------------------------------------------------------------------ ground
        static void Ground(Transform root)
        {
            var k = new MeshKit();
            k.Box(M.mud, new Vector3(Area.xMin, 0f, Area.yMin), new Vector3(Area.xMax, 0.04f, FrontZ - 0.3f), 16 | 32 | 1 | 2);
            // hardcore haul road from the gate round to the block and the crane
            k.Box(M.hardcore, new Vector3(GateX0, 0f, -66f), new Vector3(GateX1, 0.07f, FrontZ - 0.3f), 32);
            k.Box(M.hardcore, new Vector3(GateX0, 0f, -88f), new Vector3(Block.xMax + 4f, 0.07f, -76f));
            k.Box(M.hardcore, new Vector3(GateX0, 0f, -76f), new Vector3(GateX1, 0.07f, -66f), 16 | 32);
            // a dropped crossing over the main road's pavement into the gate
            k.Box(M.hardcore, new Vector3(GateX0, 0f, -37.75f), new Vector3(GateX1, 0.14f, -34.45f));
            k.Quad(M.hardcore, new Vector3(GateX0, 0.04f, -34.2f), new Vector3(GateX1, 0.04f, -34.2f), new Vector3(GateX1, 0.14f, -34.45f), new Vector3(GateX0, 0.14f, -34.45f), new Vector3(0, 1, -0.4f));
            k.Box(M.hardcore, new Vector3(GateX0, 0f, FrontZ - 0.3f), new Vector3(GateX1, 0.1f, -37.75f));
            // puddles
            var rng = new System.Random(5);
            for (int i = 0; i < 7; i++)
            {
                var c = new Vector3(Area.xMin + 2f + (float)rng.NextDouble() * 16f, 0.075f, -48f - (float)rng.NextDouble() * 38f);
                k.Cyl(M.puddle != null ? M.puddle : M.black, c, 0.6f + (float)rng.NextDouble() * 1.2f, 0.005f, 12);
            }
            var g = Done(k, "SiteGround", root, true, false);
        }

        // ------------------------------------------------------------------------------------------ hoardings and fences
        static void Perimeter(Transform root)
        {
            var k = new MeshKit(); var fence = new MeshKit();
            float x0 = Area.xMin, x1 = Area.xMax, z0 = Area.yMin, z1 = FrontZ;
            // developer hoardings along the road, either side of the gate
            void Hoard(float a, float b)
            {
                float len = b - a; if (len < 0.5f) return;
                k.QuadUV(M.hoarding, new Vector3(a, 0f, z1 + 0.03f), new Vector3(b, 0f, z1 + 0.03f), new Vector3(b, 2.4f, z1 + 0.03f), new Vector3(a, 2.4f, z1 + 0.03f), Vector3.forward,
                    new Vector2(len / 9.6f, 0), new Vector2(0, 0), new Vector2(0, 1), new Vector2(len / 9.6f, 1)); // read from the road (looking -z)
                k.Box(M.timber, new Vector3(a, 0f, z1 - 0.02f), new Vector3(b, 2.4f, z1 + 0.02f), 32);
                for (float x = a; x <= b + 0.01f; x += 2.4f) k.Box(M.timber, new Vector3(x - 0.05f, 0f, z1 - 0.3f), new Vector3(x + 0.05f, 2.2f, z1 - 0.02f));
            }
            Hoard(x0, GateX0); Hoard(GateX1, x1);
            // safety board by the gate
            k.QuadUV(M.safety, new Vector3(GateX1 + 0.3f, 0.6f, z1 + 0.05f), new Vector3(GateX1 + 1.3f, 0.6f, z1 + 0.05f), new Vector3(GateX1 + 1.3f, 2.1f, z1 + 0.05f), new Vector3(GateX1 + 0.3f, 2.1f, z1 + 0.05f), Vector3.forward,
                new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1));
            // mesh fence panels down the sides and the back, on concrete feet
            void Panels(Vector3 a, Vector3 b)
            {
                float len = Vector3.Distance(a, b); int n = Mathf.Max(1, Mathf.RoundToInt(len / 3.5f));
                var dir = (b - a) / len; var nrm = Vector3.Cross(Vector3.up, dir);
                for (int i = 0; i < n; i++)
                {
                    var p = a + dir * (i * len / n); var q = a + dir * ((i + 1) * len / n);
                    fence.QuadUV(M.heras, p + Vector3.up * 0.12f, q + Vector3.up * 0.12f, q + Vector3.up * 2.12f, p + Vector3.up * 2.12f, nrm, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
                    k.Box(M.concrete, p - dir * 0.3f - nrm * 0.1f + Vector3.zero, p + dir * 0.3f + nrm * 0.1f + Vector3.up * 0.13f);
                }
            }
            Panels(new Vector3(x0, 0, z1), new Vector3(x0, 0, z0)); Panels(new Vector3(x0, 0, z0), new Vector3(x1, 0, z0)); Panels(new Vector3(x1, 0, z0), new Vector3(x1, 0, z1));
            // the gate: two mesh panels standing open against the hoarding
            fence.QuadUV(M.heras, new Vector3(GateX0, 0.12f, z1 - 0.3f), new Vector3(GateX0, 0.12f, z1 - 3.8f), new Vector3(GateX0, 2.12f, z1 - 3.8f), new Vector3(GateX0, 2.12f, z1 - 0.3f), Vector3.right,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
            var g = Done(k, "SiteHoardings", root);
            Col(g, new Vector3((x0 + GateX0) * 0.5f, 1.2f, z1 - 0.1f), new Vector3(GateX0 - x0, 2.4f, 0.3f));
            Col(g, new Vector3((GateX1 + x1) * 0.5f, 1.2f, z1 - 0.1f), new Vector3(x1 - GateX1, 2.4f, 0.3f));
            Col(g, new Vector3(x0, 1.1f, (z0 + z1) * 0.5f), new Vector3(0.2f, 2.2f, z1 - z0));
            Col(g, new Vector3(x1, 1.1f, (z0 + z1) * 0.5f), new Vector3(0.2f, 2.2f, z1 - z0));
            Col(g, new Vector3((x0 + x1) * 0.5f, 1.1f, z0), new Vector3(x1 - x0, 2.2f, 0.2f));
            Done(fence, "SiteFence", root);
        }

        // ------------------------------------------------------------------------------------------ the half-built flats
        static void Flats(Transform root)
        {
            var k = new MeshKit(); var rb = new MeshKit();
            float x0 = Block.xMin, x1 = Block.xMax, z0 = Block.yMin, z1 = Block.yMax;
            int nx = 5, nz = 3; float bx = (x1 - x0) / nx, bz = (z1 - z0) / nz;
            const float col = 0.4f, slab = 0.25f;
            // slabs on every floor bar the top, which is still just columns
            for (int f = 0; f < Storeys; f++)
            {
                float y = f * StoreyH;
                if (f == 0) k.Box(M.concrete, new Vector3(x0 - 0.2f, 0f, z0 - 0.2f), new Vector3(x1 + 0.2f, 0.3f, z1 + 0.2f));
                else k.Box(M.concrete, new Vector3(x0, y - slab, z0), new Vector3(f == Storeys - 1 ? x0 + bx * 3 : x1, y, z1));
            }
            // columns (top storey columns only where the slab below is finished), with rebar starter bars above the top
            for (int f = 0; f < Storeys; f++)
                for (int i = 0; i <= nx; i++)
                    for (int j = 0; j <= nz; j++)
                    {
                        if (f == Storeys - 1 && i > 3) continue;
                        float x = x0 + i * bx, z = z0 + j * bz, y0 = f == 0 ? 0.3f : f * StoreyH, y1 = (f + 1) * StoreyH - (f == Storeys - 1 ? 1.2f : slab);
                        if (f == Storeys - 1 && (i + j) % 2 == 1) y1 = y0 + 1.6f;
                        k.Box(M.concrete, new Vector3(x - col / 2, y0, z - col / 2), new Vector3(x + col / 2, y1, z + col / 2));
                        if (f == Storeys - 1)
                            foreach (var o in new[] { new Vector2(-0.12f, -0.12f), new Vector2(0.12f, -0.12f), new Vector2(-0.12f, 0.12f), new Vector2(0.12f, 0.12f) })
                                rb.Box(M.rebar, new Vector3(x + o.x - 0.012f, y1, z + o.y - 0.012f), new Vector3(x + o.x + 0.012f, y1 + 0.9f, z + o.y + 0.012f));
                    }
            // blockwork walls going up: ground floor nearly done, first floor half, with gaps for windows and doors
            void BlockWall(Vector3 a, Vector3 b, float y0, float h, bool holes)
            {
                var d = b - a; float len = d.magnitude; var dir = d / len; var n = Vector3.Cross(Vector3.up, dir);
                float t = 0.1f;
                float s = 0f;
                while (s < len - 0.05f)
                {
                    float seg = holes ? Mathf.Min(1.6f, len - s) : len - s;
                    var p = a + dir * s; var q = a + dir * (s + seg);
                    Vector3 mn = Vector3.Min(p - n * t, q + n * t), mx = Vector3.Max(p - n * t, q + n * t);
                    mn.y = y0; mx.y = y0 + h;
                    k.Box(M.blocks, mn, mx);
                    s += seg + (holes ? 1.2f : 0f);
                    if (holes && s < len) { var w0 = a + dir * (s - 1.2f); var w1 = a + dir * s; Vector3 a2 = Vector3.Min(w0 - n * t, w1 + n * t), b2 = Vector3.Max(w0 - n * t, w1 + n * t); a2.y = y0; b2.y = y0 + 0.9f; k.Box(M.blocks, a2, b2); }
                }
            }
            BlockWall(new Vector3(x0, 0, z1), new Vector3(x1, 0, z1), 0.3f, StoreyH - 0.55f, true);
            BlockWall(new Vector3(x0, 0, z0), new Vector3(x1, 0, z0), 0.3f, StoreyH - 0.55f, true);
            BlockWall(new Vector3(x0, 0, z0), new Vector3(x0, 0, z1), 0.3f, StoreyH - 0.55f, false);
            BlockWall(new Vector3(x0, 0, z1), new Vector3(x0 + bx * 2.5f, 0, z1), StoreyH, 1.4f, true);
            BlockWall(new Vector3(x0, 0, z0), new Vector3(x0, 0, z1), StoreyH, 2.2f, false);
            // facing brick started on the ground floor front
            k.Box(M.brick, new Vector3(x0, 0.3f, z1 + 0.12f), new Vector3(x0 + bx * 3f, 1.1f, z1 + 0.22f));
            // stair core at the west end
            k.Box(M.concrete, new Vector3(x0 - 3.2f, 0f, z0 + 4f), new Vector3(x0, Storeys * StoreyH + 0.5f, z0 + 9.5f), 0);
            var g = Done(k, "SiteFlats", root, true);
            Done(rb, "SiteRebar", root, false, false);
        }

        // ------------------------------------------------------------------------------------------ scaffolding
        static void Scaffold(Transform root)
        {
            var k = new MeshKit(); var boards = new MeshKit();
            float x0 = Block.xMin, x1 = Block.xMax, z1 = Block.yMax;
            const float out1 = 0.3f, out2 = 1.55f, lift = 2.0f; int lifts = 5;
            float h = lifts * lift + 1f;
            void Tube(Vector3 a, Vector3 b) { var lo = Vector3.Min(a, b) - Vector3.one * 0.024f; var hi = Vector3.Max(a, b) + Vector3.one * 0.024f; k.Box(M.steel, lo, hi); }
            // north face (the road side) and east face
            void Face(Vector3 o, Vector3 along, Vector3 outw, float len)
            {
                int bays = Mathf.CeilToInt(len / 2.4f); float bay = len / bays;
                for (int i = 0; i <= bays; i++)
                {
                    var p = o + along * (i * bay);
                    Tube(p + outw * out1, p + outw * out1 + Vector3.up * h);
                    Tube(p + outw * out2, p + outw * out2 + Vector3.up * h);
                }
                for (int l = 1; l <= lifts; l++)
                {
                    float y = l * lift;
                    foreach (float d in new[] { out1, out2 }) Tube(o + outw * d + Vector3.up * y, o + along * len + outw * d + Vector3.up * y);
                    Tube(o + outw * out2 + Vector3.up * (y + 1.0f), o + along * len + outw * out2 + Vector3.up * (y + 1.0f)); // guard rail
                    for (int i = 0; i <= bays; i++) { var p = o + along * (i * bay) + Vector3.up * y; Tube(p + outw * out1, p + outw * out2); }
                    // boards and toe boards
                    var a = o + outw * (out1 + 0.05f) + Vector3.up * (y + 0.05f); var b = o + along * len + outw * (out2 - 0.05f) + Vector3.up * (y + 0.1f);
                    var lo = Vector3.Min(a, b); var hi = Vector3.Max(a, b);
                    float t = MeshKit.TileOf(M.boards);
                    boards.Box(M.boards, lo, hi);
                    var ta = o + outw * (out2 - 0.03f) + Vector3.up * (y + 0.1f); var tb = o + along * len + outw * (out2) + Vector3.up * (y + 0.25f);
                    boards.Box(M.timber, Vector3.Min(ta, tb), Vector3.Max(ta, tb));
                }
                // diagonal bracing on the outer face
                for (int i = 0; i < bays; i += 3)
                {
                    var p = o + along * (i * bay) + outw * (out2 + 0.06f);
                    var q = p + along * (bay * Mathf.Min(3, bays - i)) + Vector3.up * h * 0.6f;
                    var mid = (p + q) * 0.5f; var dv = q - p;
                    k.Push(Matrix4x4.TRS(mid, Quaternion.LookRotation(dv.normalized, outw), Vector3.one));
                    k.Box(M.steel, new Vector3(-0.024f, -0.024f, -dv.magnitude / 2), new Vector3(0.024f, 0.024f, dv.magnitude / 2));
                    k.Pop();
                }
                // ladders between lifts at one end
                var lp = o + along * 0.8f + outw * ((out1 + out2) * 0.5f);
                for (int l = 0; l < lifts; l++)
                {
                    float y = l * lift;
                    foreach (float s in new[] { -0.22f, 0.22f }) Tube(lp + along * s + Vector3.up * y, lp + along * s + Vector3.up * (y + lift + 1f));
                    for (float r = 0.3f; r < lift; r += 0.3f) Tube(lp + along * -0.22f + Vector3.up * (y + r), lp + along * 0.22f + Vector3.up * (y + r));
                }
            }
            Face(new Vector3(x0, 0f, z1), Vector3.right, Vector3.forward, x1 - x0);
            Face(new Vector3(x1, 0f, z1), Vector3.back, Vector3.right, Block.height);
            Done(k, "SiteScaffold", root);
            var bg = Done(boards, "SiteScaffoldBoards", root, true);
            ScaffoldSpots.Clear();
            for (int l = 1; l <= lifts; l++) ScaffoldSpots.Add(new Vector3(x0 + 0.8f, l * lift + 0.1f, z1 + (out1 + out2) * 0.5f));
        }
        /// <summary>Where the ladders reach each lift of the scaffold (for climbing later).</summary>
        public static readonly List<Vector3> ScaffoldSpots = new List<Vector3>();

        // ------------------------------------------------------------------------------------------ tower crane
        static void Crane(Transform root)
        {
            var mast = new MeshKit(); var top = new MeshKit();
            const float H = 34f, w = 1.6f, jib = 38f, counter = 11f;
            Vector3 b = CraneBase;
            // concrete base with the mast's foot, then the lattice mast (four alpha-cut faces)
            mast.Box(M.concrete, b + new Vector3(-2.5f, 0f, -2.5f), b + new Vector3(2.5f, 0.6f, 2.5f));
            Lattice(mast, b + new Vector3(-w / 2, 0.6f, -w / 2), b + new Vector3(w / 2, H, w / 2), true);
            var mg = Done(mast, "CraneMast", root);
            Col(mg, b + new Vector3(0, H * 0.5f, 0), new Vector3(w, H, w));
            // the slewing top as its own object, so it can turn: cab, jib, counter-jib with weights, trolley, hook
            var tg = new GameObject("CraneTop"); tg.transform.SetParent(root, false); tg.transform.localPosition = b + Vector3.up * H;
            top.Box(M.yellow, new Vector3(-1.2f, 0f, -1.2f), new Vector3(1.2f, 0.8f, 1.2f));
            top.Box(M.cabin, new Vector3(1.0f, -0.4f, -1.4f), new Vector3(2.6f, 1.8f, 0.2f));
            top.QuadUV(M.windows, new Vector3(2.62f, 0.2f, -1.3f), new Vector3(2.62f, 0.2f, 0.1f), new Vector3(2.62f, 1.6f, 0.1f), new Vector3(2.62f, 1.6f, -1.3f), Vector3.right,
                new Vector2(0.5f, 0.004f), new Vector2(1f, 0.004f), new Vector2(1f, 0.246f), new Vector2(0.5f, 0.246f));
            Lattice(top, new Vector3(1.2f, 0.8f, -0.6f), new Vector3(jib, 2.0f, 0.6f), false);
            Lattice(top, new Vector3(-counter, 0.8f, -0.8f), new Vector3(-1.2f, 1.8f, 0.8f), false);
            top.Box(M.concrete, new Vector3(-counter + 0.2f, -1.2f, -1.0f), new Vector3(-counter + 3.4f, 0.8f, 1.0f));
            // apex and tie bars
            Lattice(top, new Vector3(-0.6f, 0.8f, -0.6f), new Vector3(0.6f, 7f, 0.6f), true);
            Bar(top, new Vector3(0, 7f, 0), new Vector3(jib * 0.55f, 2.0f, 0)); Bar(top, new Vector3(0, 7f, 0), new Vector3(-counter + 1f, 1.8f, 0));
            // trolley part way out, cable down to the hook block
            float tx = jib * 0.55f, hookY = -H + 9f;
            top.Box(M.yellow, new Vector3(tx - 0.6f, 0.4f, -0.7f), new Vector3(tx + 0.6f, 0.8f, 0.7f));
            foreach (float dz in new[] { -0.15f, 0.15f }) top.Box(M.black, new Vector3(tx - 0.015f, hookY + 0.6f, dz - 0.015f), new Vector3(tx + 0.015f, 0.4f, dz + 0.015f));
            top.Box(M.red, new Vector3(tx - 0.3f, hookY, -0.2f), new Vector3(tx + 0.3f, hookY + 0.6f, 0.2f));
            top.Box(M.black, new Vector3(tx - 0.05f, hookY - 0.35f, -0.05f), new Vector3(tx + 0.05f, hookY, 0.05f));
            var tb = top.Build("CraneTopMesh", tg.transform, false);
            tg.transform.localRotation = Quaternion.Euler(0, -30f, 0);
        }

        /// <summary>A lattice box (crane mast or jib) from min to max: four alpha-cut faces, UVs repeating every 2 m along its length.</summary>
        static void Lattice(MeshKit k, Vector3 mn, Vector3 mx, bool vertical)
        {
            Vector3 a = mn, b = mx;
            float L = vertical ? (b.y - a.y) : (b.x - a.x), r = L / 2f;
            void F(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 n) => k.QuadUV(M.lattice, p0, p1, p2, p3, n, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, r), new Vector2(0, r));
            if (vertical)
            {
                F(new Vector3(a.x, a.y, a.z), new Vector3(b.x, a.y, a.z), new Vector3(b.x, b.y, a.z), new Vector3(a.x, b.y, a.z), Vector3.back);
                F(new Vector3(b.x, a.y, b.z), new Vector3(a.x, a.y, b.z), new Vector3(a.x, b.y, b.z), new Vector3(b.x, b.y, b.z), Vector3.forward);
                F(new Vector3(a.x, a.y, b.z), new Vector3(a.x, a.y, a.z), new Vector3(a.x, b.y, a.z), new Vector3(a.x, b.y, b.z), Vector3.left);
                F(new Vector3(b.x, a.y, a.z), new Vector3(b.x, a.y, b.z), new Vector3(b.x, b.y, b.z), new Vector3(b.x, b.y, a.z), Vector3.right);
            }
            else
            {
                // along x: sides (z faces), top and bottom; texture's long axis (v) runs along x
                void G(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 n) => k.QuadUV(M.lattice, p0, p1, p2, p3, n, new Vector2(0, 0), new Vector2(0, r), new Vector2(1, r), new Vector2(1, 0));
                G(new Vector3(a.x, a.y, a.z), new Vector3(b.x, a.y, a.z), new Vector3(b.x, b.y, a.z), new Vector3(a.x, b.y, a.z), Vector3.back);
                G(new Vector3(a.x, a.y, b.z), new Vector3(b.x, a.y, b.z), new Vector3(b.x, b.y, b.z), new Vector3(a.x, b.y, b.z), Vector3.forward);
                G(new Vector3(a.x, b.y, a.z), new Vector3(b.x, b.y, a.z), new Vector3(b.x, b.y, b.z), new Vector3(a.x, b.y, b.z), Vector3.up);
                G(new Vector3(a.x, a.y, a.z), new Vector3(b.x, a.y, a.z), new Vector3(b.x, a.y, b.z), new Vector3(a.x, a.y, b.z), Vector3.down);
            }
        }

        static void Bar(MeshKit k, Vector3 a, Vector3 b)
        {
            var d = b - a; k.Push(Matrix4x4.TRS((a + b) * 0.5f, Quaternion.LookRotation(d.normalized), Vector3.one));
            k.Box(M.steel, new Vector3(-0.03f, -0.03f, -d.magnitude / 2), new Vector3(0.03f, 0.03f, d.magnitude / 2)); k.Pop();
        }

        // ------------------------------------------------------------------------------------------ cabins
        static void Cabins(Transform root)
        {
            var k = new MeshKit();
            Vector3 o = new Vector3(98.5f, 0f, -55f); const float L = 7f, W = 2.7f, H = 2.6f;
            for (int s = 0; s < 2; s++)
            {
                float y = s * (H + 0.05f) + 0.15f;
                k.Box(M.cabin, o + new Vector3(0, y, 0), o + new Vector3(W, y + H, L));
                k.Box(M.cabinTrim, o + new Vector3(-0.05f, y - 0.1f, -0.05f), o + new Vector3(W + 0.05f, y + 0.05f, L + 0.05f));
                k.Box(M.cabinTrim, o + new Vector3(-0.05f, y + H - 0.05f, -0.05f), o + new Vector3(W + 0.05f, y + H + 0.08f, L + 0.05f));
                // windows and a door on the side facing the haul road (+x)
                for (int wv = 0; wv < 2; wv++)
                {
                    float z = 1.0f + wv * 3.6f;
                    k.QuadUV(M.windows, o + new Vector3(W + 0.01f, y + 1.0f, z), o + new Vector3(W + 0.01f, y + 1.0f, z + 1.6f), o + new Vector3(W + 0.01f, y + 2.0f, z + 1.6f), o + new Vector3(W + 0.01f, y + 2.0f, z), Vector3.right,
                        new Vector2(0, 0.004f), new Vector2(0.5f, 0.004f), new Vector2(0.5f, 0.246f), new Vector2(0, 0.246f));
                }
                k.Box(M.cabinTrim, o + new Vector3(W, y + 0.05f, 3.0f), o + new Vector3(W + 0.03f, y + 2.1f, 3.95f));
            }
            // steel stair up to the top cabin's door, and a landing
            Vector3 st = o + new Vector3(W, 0f, 3.0f);
            k.Box(M.steel, st + new Vector3(0.05f, H + 0.1f, -0.2f), st + new Vector3(1.4f, H + 0.2f, 1.2f));
            for (int i = 0; i < 13; i++) k.Box(M.steel, st + new Vector3(0.1f, 0.2f * i, -0.2f - (13 - i) * 0.26f), st + new Vector3(1.3f, 0.2f * i + 0.05f, -0.2f - (12 - i) * 0.26f));
            Vector3 lo = o + new Vector3(-2.2f, 0f, 1.5f);
            k.Box(M.blue, lo, lo + new Vector3(1.2f, 2.3f, 1.2f)); k.Box(M.cabinTrim, lo + new Vector3(-0.05f, 2.3f, -0.05f), lo + new Vector3(1.25f, 2.4f, 1.25f));
            var g = Done(k, "SiteCabins", root);
            Col(g, o + new Vector3(W / 2, H + 0.1f, L / 2), new Vector3(W, H * 2f + 0.2f, L));
            Col(g, lo + new Vector3(0.6f, 1.15f, 0.6f), new Vector3(1.2f, 2.3f, 1.2f));
        }

        // ------------------------------------------------------------------------------------------ plant and materials
        static void Plant(Transform root)
        {
            var k = new MeshKit();
            var cols = new List<(Vector3, Vector3)>();
            // tracked excavator, arm up and bucket curled, by the block
            Vector3 ex = new Vector3(130f, 0f, -84f);
            k.Push(ex, 20f);
            foreach (float x in new[] { -1.15f, 1.15f }) k.Box(M.rubber, new Vector3(x - 0.3f, 0f, -1.9f), new Vector3(x + 0.3f, 0.75f, 1.9f));
            k.Box(M.black, new Vector3(-0.8f, 0.75f, -0.8f), new Vector3(0.8f, 0.95f, 0.8f));
            k.Box(M.yellow, new Vector3(-1.25f, 0.95f, -1.9f), new Vector3(1.25f, 2.0f, 1.0f));
            k.Box(M.yellow, new Vector3(-1.25f, 0.95f, -2.3f), new Vector3(1.25f, 1.9f, -1.9f));
            k.Box(M.cabin, new Vector3(-1.2f, 2.0f, -0.2f), new Vector3(-0.1f, 3.1f, 1.0f));
            k.QuadUV(M.windows, new Vector3(-1.2f, 2.2f, 1.01f), new Vector3(-0.1f, 2.2f, 1.01f), new Vector3(-0.1f, 3.0f, 1.01f), new Vector3(-1.2f, 3.0f, 1.01f), Vector3.forward,
                new Vector2(0, 0.004f), new Vector2(0.25f, 0.004f), new Vector2(0.25f, 0.246f), new Vector2(0, 0.246f));
            Arm(k, new Vector3(0.4f, 1.8f, 0.9f), new Vector3(0.4f, 4.2f, 3.3f)); Arm(k, new Vector3(0.4f, 4.2f, 3.3f), new Vector3(0.4f, 1.4f, 5.2f));
            k.Box(M.black, new Vector3(-0.05f, 0.6f, 4.8f), new Vector3(0.85f, 1.4f, 5.6f));
            k.Pop();
            cols.Add((ex + new Vector3(0, 1.4f, 0), new Vector3(3.2f, 2.8f, 4.6f)));
            // site dumper, skip at the front
            Vector3 dp = new Vector3(106.5f, 0f, -71f);
            k.Push(dp, 0f);
            foreach (var w in new[] { new Vector3(-0.85f, 0.5f, -0.9f), new Vector3(0.85f, 0.5f, -0.9f), new Vector3(-0.85f, 0.5f, 0.9f), new Vector3(0.85f, 0.5f, 0.9f) }) k.CylX(M.rubber, w, 0.5f, 0.4f, 12);
            k.Box(M.yellow, new Vector3(-0.7f, 0.5f, -1.4f), new Vector3(0.7f, 1.2f, 0.4f));
            k.Box(M.yellow, new Vector3(-0.9f, 0.9f, 0.3f), new Vector3(0.9f, 1.8f, 1.6f));
            k.Box(M.black, new Vector3(-0.3f, 1.2f, -0.9f), new Vector3(0.3f, 1.6f, -0.4f));
            k.Box(M.steel, new Vector3(-0.6f, 1.2f, -1.3f), new Vector3(0.6f, 2.4f, -1.25f));
            k.Pop();
            cols.Add((dp + new Vector3(0, 0.9f, 0), new Vector3(2.1f, 1.8f, 3.2f)));
            // skip of rubble, sand heap, brick and block pallets, cement mixer
            Vector3 sk = new Vector3(143f, 0f, -86f);
            k.Push(sk, 90f);
            k.Box(M.yellow, new Vector3(-0.9f, 0f, -1.6f), new Vector3(0.9f, 0.2f, 1.6f));
            k.Quad(M.yellow, new Vector3(-0.9f, 0.2f, -1.6f), new Vector3(0.9f, 0.2f, -1.6f), new Vector3(0.95f, 1.2f, -2.1f), new Vector3(-0.95f, 1.2f, -2.1f), new Vector3(0, 0.45f, -1));
            k.Quad(M.yellow, new Vector3(-0.9f, 0.2f, 1.6f), new Vector3(0.9f, 0.2f, 1.6f), new Vector3(0.95f, 1.2f, 2.1f), new Vector3(-0.95f, 1.2f, 2.1f), new Vector3(0, 0.45f, 1));
            foreach (float x in new[] { -0.92f, 0.92f }) k.Box(M.yellow, new Vector3(x - 0.03f, 0.2f, -2.1f), new Vector3(x + 0.03f, 1.2f, 2.1f));
            k.Sphere(M.hardcore, new Vector3(0, 0.9f, 0), new Vector3(0.85f, 0.5f, 1.8f), 10, 6, true);
            k.Pop();
            cols.Add((sk + new Vector3(0, 0.6f, 0), new Vector3(4.2f, 1.2f, 1.9f)));
            k.Sphere(M.sand, new Vector3(136f, -0.2f, -90f), new Vector3(2.6f, 1.6f, 2.2f), 12, 8, true);
            var rng = new System.Random(9);
            for (int i = 0; i < 6; i++)
            {
                var p = new Vector3(113f + (i % 3) * 1.6f, 0f, -54f - (i / 3) * 1.6f);
                k.Box(M.timber, p, p + new Vector3(1.2f, 0.14f, 1.0f));
                k.Box(i < 3 ? M.brick : M.blocks, p + new Vector3(0.05f, 0.14f, 0.05f), p + new Vector3(1.15f, 0.14f + 0.7f + (float)rng.NextDouble() * 0.3f, 0.95f));
            }
            cols.Add((new Vector3(115.4f, 0.5f, -55.3f), new Vector3(4.6f, 1.1f, 3.0f)));
            Vector3 mx = new Vector3(110.5f, 0f, -61f);
            k.Push(mx, 35f);
            k.Box(M.steel, new Vector3(-0.5f, 0f, -0.05f), new Vector3(0.5f, 0.7f, 0.05f));
            k.CylX(M.rubber, new Vector3(0, 0.25f, 0.6f), 0.25f, 1.1f, 10);
            k.Push(Matrix4x4.TRS(new Vector3(0, 1.0f, 0.2f), Quaternion.Euler(-35f, 0, 0), Vector3.one));
            k.Cyl(M.red, new Vector3(0, -0.4f, 0), 0.45f, 0.8f, 12, true, 0.25f);
            k.Pop(); k.Pop();
            var g = Done(k, "SitePlant", root);
            foreach (var (c, s) in cols) Col(g, c, s);
        }

        /// <summary>At Play: builders in hi-vis and hard hats working about the site, if the scene has it.</summary>
        public static void Builders()
        {
            if (GameObject.Find(RootName) == null) return;
            var routes = new[]
            {
                new[] { new Vector3(105.5f, 0, -57f), new Vector3(119f, 0, -58.5f), new Vector3(126f, 0, -58.5f) },
                new[] { new Vector3(112f, 0, -62.5f), new Vector3(114.5f, 0, -57.8f), new Vector3(117.5f, 0, -63f) },
                new[] { new Vector3(126f, 0, -79f), new Vector3(140f, 0, -79.5f), new Vector3(138f, 0, -88f) },
            };
            var hats = new[] { Color.white, new Color(0.95f, 0.8f, 0.1f), new Color(0.2f, 0.4f, 0.85f) };
            for (int i = 0; i < routes.Length; i++)
            {
                var p = World.MakePerson(routes[i][0], new Color(1f, 0.5f, 0.1f), new Color(0.25f, 0.25f, 0.28f), "Builder");
                for (int j = 1; j < routes[i].Length; j++) p.route.Add(routes[i][j]);
                p.route.Add(routes[i][0]); p.speed = 0.9f + i * 0.1f;
                p.gameObject.AddComponent<HardHat>().colour = hats[i];
            }
        }

        static void Arm(MeshKit k, Vector3 a, Vector3 b)
        {
            var d = b - a; k.Push(Matrix4x4.TRS((a + b) * 0.5f, Quaternion.LookRotation(d.normalized, Vector3.up), Vector3.one));
            k.Box(M.yellow, new Vector3(-0.22f, -0.28f, -d.magnitude / 2), new Vector3(0.22f, 0.28f, d.magnitude / 2)); k.Pop();
        }
    }

    /// <summary>A hard hat that goes on once the person's body has loaded, riding on the head bone.</summary>
    public class HardHat : MonoBehaviour
    {
        public Color colour = Color.white;
        System.Collections.IEnumerator Start()
        {
            var p = GetComponent<Person>(); float t0 = Time.time;
            while (p != null && p.anim == null && Time.time - t0 < 8f) yield return null;
            if (p == null || p.anim == null || !p.anim.isHuman) yield break;
            var head = p.anim.GetBoneTransform(HumanBodyBones.Head); if (head == null) yield break;
            var m = Gfx.Mat(colour, null, 0.6f);
            var hat = new GameObject("HardHat").transform; hat.SetParent(head, false);
            hat.position = head.position + p.anim.transform.up * 0.13f + p.anim.transform.forward * 0.01f;
            hat.rotation = p.anim.transform.rotation;
            var dome = Gfx.Prim(PrimitiveType.Sphere, hat, Vector3.zero, new Vector3(0.235f, 0.17f, 0.27f), m, "Dome", false);
            var brim = Gfx.Prim(PrimitiveType.Cylinder, hat, new Vector3(0, -0.02f, 0.02f), new Vector3(0.27f, 0.006f, 0.31f), m, "Brim", false);
            foreach (var r in hat.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    /// <summary>The tower crane's top slewing round now and then, pausing as if lowering a load.</summary>
    public class CraneSlew : MonoBehaviour
    {
        float target, wait;
        void Start() { target = transform.localEulerAngles.y; }
        void Update()
        {
            if (wait > 0f) { wait -= Time.deltaTime; return; }
            float y = Mathf.MoveTowardsAngle(transform.localEulerAngles.y, target, 4f * Time.deltaTime);
            transform.localRotation = Quaternion.Euler(0, y, 0);
            if (Mathf.Abs(Mathf.DeltaAngle(y, target)) < 0.1f) { target = Random.Range(-120f, 60f); wait = Random.Range(6f, 18f); }
        }

        /// <summary>At Play: start the crane turning if the scene has the site.</summary>
        public static void Attach()
        {
            var t = GameObject.Find("CraneTop"); if (t != null && t.GetComponent<CraneSlew>() == null) t.AddComponent<CraneSlew>();
        }
    }
}

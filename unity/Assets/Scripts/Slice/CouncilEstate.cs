using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Hawthorn Way: a 1960s council estate across the main road from Abbots Wend, after the user's photos. Long terraces
    /// of three-storey yellow-brick town houses with shallow pitched roofs, white uPVC window bands over cream panels,
    /// integral garages with up-and-over doors (blue, red, white, brown) and recessed porches; concrete drives, slab
    /// paths, low walls and timber fences in front, fenced back gardens behind. A loop road with kerbs, pavements and
    /// street lamps runs round an open green with a footpath across it and mature trees.
    ///
    /// The static parts are built once into the scene by the editor (ASBO/Art/Build Council Estate) so they are batched,
    /// in the NavMesh and solid; at Play, <see cref="Dress"/> adds the loose things: wheelie bins you can kick over and
    /// cars parked on the drives.
    /// </summary>
    public static partial class CouncilEstate
    {
        public const string RootName = "CouncilEstate";
        // house: width, depth, floor-to-floor, eaves height, roof pitch, forecourt and back-garden depth
        public const float W = 5.4f, D = 8.6f, FF = 2.6f, Eave = 7.85f, Forecourt = 6.8f, Garden = 9f;
        static readonly float Pitch = Mathf.Tan(17f * Mathf.Deg2Rad);
        public static float RidgeY => Eave + D * 0.5f * Pitch;

        // the loop road: in from the main road at x=-40, round the green, back out at x=40
        public const float RoadHalf = 3.2f, KerbW = 0.16f, PaveW = 2.0f, RoadTop = 0.04f, PaveTop = 0.16f;
        public static readonly Vector3[] Loop = { new Vector3(-40f, 0, -37.7f), new Vector3(-40f, 0, -110f), new Vector3(40f, 0, -110f), new Vector3(40f, 0, -37.7f) };
        public const float CornerR = 12f;
        /// <summary>Everything the estate covers (for flattening the ground and clearing trees): x min/max, z min/max.</summary>
        public static readonly Rect Area = new Rect(-82f, -142f, 164f, 104f);

        public struct Block { public string name; public Vector3 origin; public float yaw; public int n, seed; }
        /// <summary>
        /// The terraces. Origin is the front corner of house 1 on the ground; houses run along local +x, fronts face local
        /// +z (towards the road), backs go to local -z. Yaw 90 faces east, -90 west, 0 north.
        /// </summary>
        public static readonly Block[] Blocks =
        {
            new Block { name = "Hawthorn West 1", origin = new Vector3(-52f, 0, -44.5f), yaw = 90f, n = 6, seed = 11 },
            new Block { name = "Hawthorn West 2", origin = new Vector3(-52f, 0, -81.4f), yaw = 90f, n = 3, seed = 12 },
            new Block { name = "Hawthorn South 1", origin = new Vector3(-28f, 0, -122f), yaw = 0f, n = 6, seed = 13 },
            new Block { name = "Hawthorn South 2", origin = new Vector3(8.6f, 0, -122f), yaw = 0f, n = 4, seed = 14 },
            new Block { name = "Hawthorn East 1", origin = new Vector3(52f, 0, -97.6f), yaw = -90f, n = 6, seed = 15 },
            new Block { name = "Hawthorn East 2", origin = new Vector3(52f, 0, -60.2f), yaw = -90f, n = 3, seed = 16 },
        };

        // ------------------------------------------------------------------------------------------ materials
        public class Mats
        {
            public Material brick, roof, ridge, cream, white, black, windows, doors, slabs, drive, asphalt, kerb, pave, fence, sign, post, lampCol, glow, line, step;
            public Material[] garage;
            public Material concrete, towerPanel, felt, rubber, railing, railGreen, playRed, playYellow, playBlue, courtSign;
        }
        public static Mats M;
        static Material L(string n) => Resources.Load<Material>("Estate/" + n);
        public static bool LoadMats()
        {
            M = new Mats
            {
                brick = L("EstateBrick") != null ? L("EstateBrick") : Surfaces.Source("buff_brick"), roof = L("EstateRoof"), ridge = L("EstateRidge"), cream = L("EstateCream"), white = L("EstateWhite"),
                black = L("EstateBlack"), windows = L("EstateWindows"), doors = L("EstateDoors"), slabs = Surfaces.Source("large_grey_tiles"),
                drive = L("EstateDrive"), asphalt = Surfaces.Source("asphalt_02"), kerb = L("EstateKerb"), pave = L("EstatePave"), fence = L("EstateFence"),
                sign = L("EstateSign"), post = L("EstatePost"), lampCol = L("EstateLampColumn"), glow = L("EstateLampGlow"), line = L("EstateLine"), step = L("EstateStep"),
                garage = new[] { L("EstateGarageBlue"), L("EstateGarageRed"), L("EstateGarageWhite"), L("EstateGarageBrown") },
                concrete = L("EstateConcrete"), towerPanel = L("EstateTowerPanel"), felt = L("EstateFelt"), rubber = L("EstateRubber"), railing = L("EstateRailing"), railGreen = L("EstateRailGreen"),
                playRed = L("EstatePlayRed"), playYellow = L("EstatePlayYellow"), playBlue = L("EstatePlayBlue"), courtSign = L("EstateCourtSign"),
            };
            if (M.brick == null || M.roof == null || M.windows == null) { Debug.LogWarning("[ASBO] estate: materials missing (run ASBO/Art/Build Council Estate)"); return false; }
            // metres per texture repeat, for MeshKit's planar UVs
            void T(Material m, float metres) { if (m != null) MeshKit.Tile[m] = metres; }
            T(M.brick, Surfaces.Size["buff_brick"]); T(M.roof, 3f); T(M.ridge, 3f); T(M.cream, 2f); T(M.slabs, Surfaces.Size["large_grey_tiles"]);
            T(M.concrete, 1.55f); T(M.towerPanel, 2f); T(M.railing, 2f);
            T(M.drive, 2f); T(M.asphalt, Surfaces.Size["asphalt_02"]); T(M.kerb, 1.55f); T(M.pave, 2.4f); T(M.fence, 1.89f); T(M.step, 1.55f);
            return true;
        }

        // ------------------------------------------------------------------------------------------ build
        public static Transform Build(Transform parent)
        {
            if (!LoadMats()) return null;
            var root = new GameObject(RootName).transform; root.SetParent(parent, false);
            foreach (var b in Blocks) Terrace(root, b);
            Roads(root);
            Court(root);
            Debug.Log($"[ASBO] estate: {Blocks.Length} terraces, {Houses().Count} houses");
            return root;
        }

        /// <summary>One terrace: walls with their openings, windows, porches, garages, roof, gutters, plus its plots in front and behind.</summary>
        static void Terrace(Transform root, Block b)
        {
            var rng = new System.Random(b.seed);
            var g = new GameObject(b.name).transform; g.SetParent(root, false);
            g.localPosition = b.origin; g.localRotation = Quaternion.Euler(0, b.yaw, 0);
            var k = new MeshKit(); var plots = new MeshKit(); var fences = new MeshKit();
            float len = b.n * W;
            var frontHoles = new List<Rect>(); var backHoles = new List<Rect>();
            for (int i = 0; i < b.n; i++)
            {
                float x0 = i * W, x1 = x0 + W;
                int garageCol = rng.Next(M.garage.Length), doorV = rng.Next(4), w1 = rng.Next(3), w2 = rng.Next(3), wg = rng.Next(2);
                bool mirror = (i % 2) == 1; // neighbours are handed: garages pair up
                float gx0 = mirror ? x1 - 2.7f : x0 + 0.3f, gx1 = gx0 + 2.4f;
                float px0 = mirror ? x1 - 4.3f : x0 + 3.0f, px1 = px0 + 1.3f;
                float sx0 = mirror ? x0 + 0.25f : x0 + 4.5f, sx1 = mirror ? x0 + 0.9f : x1 - 0.25f;
                // ground floor: garage, porch, little window
                frontHoles.Add(new Rect(gx0, 0, 2.4f, 2.1f)); Recess(k, gx0, gx1, 0f, 2.1f, 0.12f, M.garage[garageCol], new Rect(0, 0, 1, 1), M.white, true);
                frontHoles.Add(new Rect(px0, 0, 1.3f, 2.3f)); Porch(k, px0, px1, doorV);
                frontHoles.Add(new Rect(sx0, 0.95f, sx1 - sx0, 1.15f)); Window(k, sx0, sx1, 0.95f, 2.1f, 0f, AtlasGround(wg), true);
                // upper floors: cream panel under a full-width window band
                for (int f = 1; f <= 2; f++)
                {
                    float y0 = f * FF;
                    var panel = new Rect(x0 + 0.25f, y0 + 0.05f, W - 0.5f, 0.62f); frontHoles.Add(panel); Panel(k, panel, 0f, true);
                    var win = new Rect(x0 + 0.25f, y0 + 0.67f, W - 0.5f, 1.43f); frontHoles.Add(win);
                    Window(k, win.xMin, win.xMax, win.yMin, win.yMax, 0f, AtlasUpper(f == 1 ? w1 : w2), true);
                    var pb = new Rect(x0 + 0.25f, y0 + 0.05f, W - 0.5f, 0.62f); backHoles.Add(pb); Panel(k, pb, -D, false);
                    var wb = new Rect(x0 + 0.25f, y0 + 0.67f, W - 0.5f, 1.43f); backHoles.Add(wb);
                    Window(k, wb.xMin, wb.xMax, wb.yMin, wb.yMax, -D, AtlasUpper((w1 + f) % 3), false);
                }
                // back: kitchen window, back door, patio window
                float kx0 = mirror ? x1 - 1.8f : x0 + 0.4f, dx0 = mirror ? x1 - 3.1f : x0 + 2.2f, qx0 = mirror ? x0 + 0.3f : x0 + 3.4f, qx1 = mirror ? x1 - 3.4f : x1 - 0.3f;
                backHoles.Add(new Rect(kx0, 0.95f, 1.4f, 1.15f)); Window(k, kx0, kx0 + 1.4f, 0.95f, 2.1f, -D, AtlasGround(0), false);
                backHoles.Add(new Rect(dx0, 0, 0.9f, 2.05f)); Recess(k, dx0, dx0 + 0.9f, 0f, 2.05f, 0.06f, M.doors, DoorUV(0), M.white, false, -D);
                backHoles.Add(new Rect(qx0, 0.3f, qx1 - qx0, 1.8f)); Window(k, qx0, qx1, 0.3f, 2.1f, -D, AtlasPatio(), false);
                // downpipes on every other party wall, front and back
                if (i % 2 == 0) foreach (float z in new[] { 0.06f, -D - 0.06f }) k.Box(M.black, new Vector3(x0 - 0.04f, 0f, z - 0.04f), new Vector3(x0 + 0.04f, Eave - 0.2f, z + 0.04f));
                Plot(plots, fences, x0, x1, gx0, gx1, px0, px1, mirror, i == b.n - 1);
            }
            k.Box(M.black, new Vector3(len - 0.04f, 0f, 0.02f), new Vector3(len + 0.04f, Eave - 0.2f, 0.1f));
            Wall(k, new Rect(0, 0, len, Eave), 0f, frontHoles, true);
            Wall(k, new Rect(0, 0, len, Eave), -D, backHoles, false);
            Gable(k, 0f, -1, rng); Gable(k, len, 1, rng);
            Roof(k, len);
            var body = k.Build("Terrace", g, false);
            var col = body.AddComponent<BoxCollider>(); col.center = new Vector3(len * 0.5f, Eave * 0.5f, -D * 0.5f); col.size = new Vector3(len, Eave, D);
            var pg = plots.Build("EstateGround", g, true); if (pg != null) pg.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fences.Build("EstateFences", g, true);
        }

        // ---- atlas rectangles (estate_windows.png: three upper rows, then small window | patio unit)
        internal static Rect AtlasUpper(int v) => new Rect(0f, 1f - (v + 1) * 0.25f + 0.004f, 1f, 0.242f);
        internal static Rect AtlasGround(int v) => v == 0 ? new Rect(0f, 0.004f, 0.5f, 0.242f) : new Rect(0f, 0.004f, 0.5f, 0.242f);
        internal static Rect AtlasPatio() => new Rect(0.5f, 0.004f, 0.5f, 0.242f);
        static Rect DoorUV(int v) => new Rect(v * 0.25f + 0.008f, 0f, 0.234f, 1f);

        /// <summary>The wall plane at depth z (front faces +z, back -z) with rectangular holes cut out, as brick.</summary>
        static void Wall(MeshKit k, Rect r, float z, List<Rect> holes, bool front)
        {
            var xs = new SortedSet<float> { r.xMin, r.xMax }; var ys = new SortedSet<float> { r.yMin, r.yMax };
            foreach (var h in holes) { xs.Add(h.xMin); xs.Add(h.xMax); ys.Add(h.yMin); ys.Add(h.yMax); }
            var X = new List<float>(xs); var Y = new List<float>(ys);
            Vector3 n = front ? Vector3.forward : Vector3.back;
            for (int j = 0; j < Y.Count - 1; j++)
            {
                float ya = Y[j], yb = Y[j + 1]; if (yb - ya < 1e-4f) continue;
                int runStart = -1;
                for (int i = 0; i <= X.Count - 1; i++)
                {
                    bool solid = false;
                    if (i < X.Count - 1)
                    {
                        float cx = (X[i] + X[i + 1]) * 0.5f, cy = (ya + yb) * 0.5f; solid = X[i + 1] - X[i] > 1e-4f;
                        foreach (var h in holes) if (h.Contains(new Vector2(cx, cy))) { solid = false; break; }
                    }
                    if (solid && runStart < 0) runStart = i;
                    if (!solid && runStart >= 0)
                    {
                        float xa = X[runStart], xb = X[i];
                        k.Quad(M.brick, new Vector3(xa, ya, z), new Vector3(xb, ya, z), new Vector3(xb, yb, z), new Vector3(xa, yb, z), n);
                        runStart = -1;
                    }
                }
            }
        }

        /// <summary>A set-back panel (garage door, back door) in an opening, with white reveals and head.</summary>
        static void Recess(MeshKit k, float x0, float x1, float y0, float y1, float depth, Material face, Rect uv, Material reveal, bool front, float zWall = 0f)
        {
            float s = front ? 1f : -1f, zf = zWall, zb = zWall - s * depth;
            Vector3 n = front ? Vector3.forward : Vector3.back;
            var a = new Vector3(x0, y0, zb); var b = new Vector3(x1, y0, zb); var c = new Vector3(x1, y1, zb); var d = new Vector3(x0, y1, zb);
            if (front) k.QuadUV(face, a, b, c, d, n, new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMin, uv.yMax));
            else k.QuadUV(face, a, b, c, d, n, new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMin, uv.yMax), new Vector2(uv.xMax, uv.yMax));
            k.Quad(reveal, new Vector3(x0, y0, zf), new Vector3(x0, y1, zf), new Vector3(x0, y1, zb), new Vector3(x0, y0, zb), Vector3.right);
            k.Quad(reveal, new Vector3(x1, y0, zf), new Vector3(x1, y1, zf), new Vector3(x1, y1, zb), new Vector3(x1, y0, zb), Vector3.left);
            k.Quad(reveal, new Vector3(x0, y1, zf), new Vector3(x1, y1, zf), new Vector3(x1, y1, zb), new Vector3(x0, y1, zb), Vector3.down);
        }

        /// <summary>A uPVC window from the atlas set 8 cm into the wall, white reveals, a sill below.</summary>
        static void Window(MeshKit k, float x0, float x1, float y0, float y1, float zWall, Rect uv, bool front)
        {
            Recess(k, x0, x1, y0, y1, 0.08f, M.windows, uv, M.white, front, zWall);
            float s = front ? 1f : -1f;
            k.Box(M.white, new Vector3(x0 - 0.03f, y0 - 0.05f, Mathf.Min(zWall, zWall + s * 0.06f)), new Vector3(x1 + 0.03f, y0, Mathf.Max(zWall, zWall + s * 0.06f)));
        }

        static void Panel(MeshKit k, Rect r, float zWall, bool front)
        {
            float z = zWall + (front ? 0.004f : -0.004f);
            k.Quad(M.cream, new Vector3(r.xMin, r.yMin, z), new Vector3(r.xMax, r.yMin, z), new Vector3(r.xMax, r.yMax, z), new Vector3(r.xMin, r.yMax, z), front ? Vector3.forward : Vector3.back);
        }

        /// <summary>The recessed porch: white boarded sides and soffit, the front door in the back wall, a concrete step.</summary>
        static void Porch(MeshKit k, float x0, float x1, int doorV)
        {
            const float depth = 1.0f, h = 2.3f;
            // back wall with the door in the middle
            float dc = (x0 + x1) * 0.5f, dx0 = dc - 0.46f, dx1 = dc + 0.46f, z = -depth;
            k.Quad(M.white, new Vector3(x0, 0, z), new Vector3(dx0, 0, z), new Vector3(dx0, h, z), new Vector3(x0, h, z), Vector3.forward);
            k.Quad(M.white, new Vector3(dx1, 0, z), new Vector3(x1, 0, z), new Vector3(x1, h, z), new Vector3(dx1, h, z), Vector3.forward);
            k.Quad(M.white, new Vector3(dx0, 2.05f, z), new Vector3(dx1, 2.05f, z), new Vector3(dx1, h, z), new Vector3(dx0, h, z), Vector3.forward);
            var uv = DoorUV(doorV);
            k.QuadUV(M.doors, new Vector3(dx0, 0.15f, z + 0.01f), new Vector3(dx1, 0.15f, z + 0.01f), new Vector3(dx1, 2.05f, z + 0.01f), new Vector3(dx0, 2.05f, z + 0.01f), Vector3.forward,
                new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMin, uv.yMax));
            k.Quad(M.white, new Vector3(x0, 0, 0), new Vector3(x0, h, 0), new Vector3(x0, h, z), new Vector3(x0, 0, z), Vector3.right);
            k.Quad(M.white, new Vector3(x1, 0, 0), new Vector3(x1, h, 0), new Vector3(x1, h, z), new Vector3(x1, 0, z), Vector3.left);
            k.Quad(M.white, new Vector3(x0, h, 0), new Vector3(x1, h, 0), new Vector3(x1, h, z), new Vector3(x0, h, z), Vector3.down);
            k.Box(M.step, new Vector3(x0, 0f, z), new Vector3(x1, 0.15f, 0.12f), 16);
        }

        /// <summary>The end wall: brick up to the eaves and the gable, a small window on each upper floor, white barge boards.</summary>
        static void Gable(MeshKit k, float x, int side, System.Random rng)
        {
            Vector3 n = side < 0 ? Vector3.left : Vector3.right;
            var holes = new List<Rect>();
            float zc = -D * 0.5f;
            for (int f = 1; f <= 2; f++) holes.Add(new Rect(zc - 0.6f, f * FF + 0.8f, 1.2f, 1.15f));
            // wall in (z, y) with holes: reuse the grid splitter by working in a rotated frame
            var zs = new SortedSet<float> { -D, 0f }; var ys = new SortedSet<float> { 0f, Eave };
            foreach (var h in holes) { zs.Add(h.xMin); zs.Add(h.xMax); ys.Add(h.yMin); ys.Add(h.yMax); }
            var Z = new List<float>(zs); var Y = new List<float>(ys);
            for (int j = 0; j < Y.Count - 1; j++)
                for (int i = 0; i < Z.Count - 1; i++)
                {
                    float cz = (Z[i] + Z[i + 1]) * 0.5f, cy = (Y[j] + Y[j + 1]) * 0.5f; bool hole = false;
                    foreach (var h in holes) if (h.Contains(new Vector2(cz, cy))) hole = true;
                    if (hole) continue;
                    k.Quad(M.brick, new Vector3(x, Y[j], Z[i]), new Vector3(x, Y[j], Z[i + 1]), new Vector3(x, Y[j + 1], Z[i + 1]), new Vector3(x, Y[j + 1], Z[i]), n);
                }
            k.TriN(M.brick, new Vector3(x, Eave, 0f), new Vector3(x, Eave, -D), new Vector3(x, RidgeY, -D * 0.5f), n);
            foreach (var h in holes)
            {
                // window in the end wall: glass set in, white reveals (built in the house frame by swapping axes)
                float xin = x - side * 0.08f;
                var uv = AtlasGround(0);
                Vector3 a = new Vector3(xin, h.yMin, side < 0 ? h.xMax : h.xMin), b = new Vector3(xin, h.yMin, side < 0 ? h.xMin : h.xMax);
                Vector3 c = new Vector3(xin, h.yMax, b.z), d = new Vector3(xin, h.yMax, a.z);
                k.QuadUV(M.windows, a, b, c, d, n, new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMin, uv.yMax));
                k.Quad(M.white, new Vector3(x, h.yMin, h.xMin), new Vector3(x, h.yMax, h.xMin), new Vector3(xin, h.yMax, h.xMin), new Vector3(xin, h.yMin, h.xMin), Vector3.forward);
                k.Quad(M.white, new Vector3(x, h.yMin, h.xMax), new Vector3(x, h.yMax, h.xMax), new Vector3(xin, h.yMax, h.xMax), new Vector3(xin, h.yMin, h.xMax), Vector3.back);
                k.Quad(M.white, new Vector3(x, h.yMax, h.xMin), new Vector3(x, h.yMax, h.xMax), new Vector3(xin, h.yMax, h.xMax), new Vector3(xin, h.yMax, h.xMin), Vector3.down);
                k.Quad(M.white, new Vector3(x, h.yMin, h.xMin), new Vector3(x, h.yMin, h.xMax), new Vector3(x + side * 0.05f, h.yMin - 0.05f, h.xMax), new Vector3(x + side * 0.05f, h.yMin - 0.05f, h.xMin), Vector3.up);
            }
        }

        /// <summary>Shallow pitched roof in grey concrete tiles: overhangs, white fascia and soffits, black gutters, ridge tiles, barge boards.</summary>
        static void Roof(MeshKit k, float len)
        {
            const float oh = 0.35f, go = 0.18f; // eaves overhang, gable overhang
            float x0 = -go, x1 = len + go, zr = -D * 0.5f, ry = RidgeY;
            float yf = Eave - oh * Pitch;
            foreach (int s in new[] { 1, -1 })
            {
                // s=1 front slope from z=+oh up to the ridge; s=-1 back slope from z=-D-oh
                float ze = s > 0 ? oh : -D - oh;
                Vector3 n = new Vector3(0, 1f, s * Pitch).normalized;
                float slope = Mathf.Sqrt((ze - zr) * (ze - zr) + (ry - yf) * (ry - yf));
                var a = new Vector3(x0, yf, ze); var b = new Vector3(x1, yf, ze); var c = new Vector3(x1, ry, zr); var d = new Vector3(x0, ry, zr);
                float t = MeshKit.TileOf(M.roof);
                k.QuadUV(M.roof, a, b, c, d, n, new Vector2(x0 / t, 0), new Vector2(x1 / t, 0), new Vector2(x1 / t, slope / t), new Vector2(x0 / t, slope / t));
                // underside of the overhang (soffit) and fascia board, then the gutter
                float zw = s > 0 ? 0f : -D;
                k.Quad(M.white, new Vector3(x0, yf - 0.02f, zw), new Vector3(x1, yf - 0.02f, zw), new Vector3(x1, yf - 0.02f, ze), new Vector3(x0, yf - 0.02f, ze), Vector3.down);
                k.Quad(M.white, new Vector3(x0, yf - 0.22f, ze), new Vector3(x1, yf - 0.22f, ze), new Vector3(x1, yf + 0.02f, ze), new Vector3(x0, yf + 0.02f, ze), s > 0 ? Vector3.forward : Vector3.back);
                float zg0 = s > 0 ? ze : ze - 0.12f, zg1 = s > 0 ? ze + 0.12f : ze;
                k.Box(M.black, new Vector3(x0, yf - 0.2f, zg0), new Vector3(x1, yf - 0.08f, zg1));
            }
            // ridge tiles
            k.Box(M.ridge, new Vector3(x0, ry - 0.04f, zr - 0.16f), new Vector3(x1, ry + 0.09f, zr + 0.16f));
            // barge boards along the gable slopes (both ends, both slopes)
            foreach (float bx in new[] { x0, x1 })
                foreach (int s in new[] { 1, -1 })
                {
                    float ze = s > 0 ? oh : -D - oh;
                    var p0 = new Vector3(bx, yf, ze); var p1 = new Vector3(bx, ry, zr);
                    var dn = Vector3.down * 0.22f; var nn = bx < 0 ? Vector3.left : Vector3.right;
                    k.Quad(M.white, p0 + dn, p1 + dn, p1, p0, nn);
                }
            // the gable overhang's underside, so you can't see into the roof from below
            foreach (int s in new[] { 1, -1 })
            {
                float ze = s > 0 ? oh : -D - oh;
                foreach (var (xa, xb) in new[] { (x0, 0f), (len, x1) })
                    k.Quad(M.white, new Vector3(xa, yf - 0.02f, ze), new Vector3(xb, yf - 0.02f, ze), new Vector3(xb, ry - 0.02f, zr), new Vector3(xa, ry - 0.02f, zr), new Vector3(0, -1f, -s * Pitch));
            }
        }

        /// <summary>The house's ground in front (drive, path, low wall, fence) and behind (patio, garden fences).</summary>
        static void Plot(MeshKit k, MeshKit f, float x0, float x1, float gx0, float gx1, float px0, float px1, bool mirror, bool last)
        {
            const float top = 0.05f;
            k.Box(M.drive, new Vector3(gx0 - 0.1f, 0f, 0f), new Vector3(gx1 + 0.1f, top, Forecourt), 16);
            k.Box(M.slabs, new Vector3(px0, 0f, 0.12f), new Vector3(px1, top, Forecourt), 16);
            k.Box(M.slabs, new Vector3(x0 + 0.1f, 0f, -D - 2.6f), new Vector3(x1 - 0.1f, top, -D), 32);
            // the bit of front garden that isn't drive or path gets a low brick wall along the pavement
            float wa = mirror ? x0 : px1, wb = mirror ? px0 : x1;
            if (wb - wa > 0.4f) k.Box(M.brick, new Vector3(wa + 0.02f, 0f, Forecourt - 0.32f), new Vector3(wb - 0.02f, 0.5f, Forecourt - 0.1f));
            // timber fences: low ones between front gardens, tall panels round the back
            FenceRun(f, new Vector3(x1, 0, 0.3f), new Vector3(x1, 0, Forecourt - 0.4f), 0.9f);
            if (x0 < 0.01f) FenceRun(f, new Vector3(x0, 0, -D), new Vector3(x0, 0, -D - Garden), 1.8f);
            FenceRun(f, new Vector3(x1, 0, -D), new Vector3(x1, 0, -D - Garden), 1.8f);
            FenceRun(f, new Vector3(x0, 0, -D - Garden), new Vector3(x1, 0, -D - Garden), 1.8f);
        }

        /// <summary>Close-boarded timber fence between two points: posts every 1.8 m, boards and a top rail.</summary>
        static void FenceRun(MeshKit f, Vector3 a, Vector3 b, float h)
        {
            Vector3 d = b - a; float len = d.magnitude; if (len < 0.1f) return;
            bool alongX = Mathf.Abs(d.x) > Mathf.Abs(d.z);
            float t = 0.04f;
            Vector3 mn = Vector3.Min(a, b), mx = Vector3.Max(a, b);
            if (alongX) f.Box(M.fence, new Vector3(mn.x, 0.02f, a.z - t), new Vector3(mx.x, h, a.z + t));
            else f.Box(M.fence, new Vector3(a.x - t, 0.02f, mn.z), new Vector3(a.x + t, h, mx.z));
            int posts = Mathf.Max(1, Mathf.CeilToInt(len / 1.8f));
            for (int i = 0; i <= posts; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)posts);
                f.Box(M.fence, new Vector3(p.x - 0.05f, 0f, p.z - 0.05f), new Vector3(p.x + 0.05f, h + 0.05f, p.z + 0.05f));
            }
        }

        // ------------------------------------------------------------------------------------------ roads
        /// <summary>The loop road's centreline, with its two corners rounded, sampled about every metre and a half.</summary>
        public static List<Vector3> Centreline()
        {
            var pts = new List<Vector3> { Loop[0] };
            for (int i = 1; i < Loop.Length - 1; i++)
            {
                Vector3 p = Loop[i], din = (p - Loop[i - 1]).normalized, dout = (Loop[i + 1] - p).normalized;
                Vector3 a = p - din * CornerR, b = p + dout * CornerR;
                pts.Add(a);
                // quarter circle from a to b: centre is a + (turn normal) * R
                Vector3 nrm = Vector3.Cross(Vector3.up, din); if (Vector3.Dot(nrm, dout) < 0) nrm = -nrm;
                Vector3 c = a + nrm * CornerR;
                const int seg = 12;
                for (int s = 1; s < seg; s++)
                {
                    float ang = s / (float)seg * 90f * Mathf.Deg2Rad;
                    pts.Add(c + (-nrm) * Mathf.Cos(ang) * CornerR + din * Mathf.Sin(ang) * CornerR);
                }
                pts.Add(b);
            }
            pts.Add(Loop[Loop.Length - 1]);
            // subdivide long straights so strips follow the ground
            var outp = new List<Vector3> { pts[0] };
            for (int i = 1; i < pts.Count; i++)
            {
                float l = Vector3.Distance(pts[i - 1], pts[i]); int n = Mathf.Max(1, Mathf.CeilToInt(l / 6f));
                for (int s = 1; s <= n; s++) outp.Add(Vector3.Lerp(pts[i - 1], pts[i], s / (float)n));
            }
            return outp;
        }

        static void Roads(Transform root)
        {
            var g = new GameObject("HawthornWay").transform; g.SetParent(root, false);
            var road = new MeshKit(); var paths = new MeshKit(); var lamps = new MeshKit(); var marks = new MeshKit();
            var c = Centreline();
            // carriageway, kerbs (with their road-side face) and pavements on both sides
            Strip(road, c, -RoadHalf, RoadHalf, RoadTop, M.asphalt);
            foreach (int s in new[] { -1, 1 })
            {
                float a = s * RoadHalf, b = s * (RoadHalf + KerbW), p = s * (RoadHalf + KerbW + PaveW);
                Strip(road, c, Mathf.Min(a, b), Mathf.Max(a, b), PaveTop + 0.01f, M.kerb);
                Edge(road, c, a, RoadTop, PaveTop + 0.01f, M.kerb, s);
                Strip(road, c, Mathf.Min(b, p), Mathf.Max(b, p), PaveTop, M.pave);
                Edge(road, c, p, 0f, PaveTop, M.kerb, -s);
            }
            // where it meets the main road: a dropped crossing over the main road's pavement, ramped down each side
            foreach (var at in new[] { Loop[0], Loop[Loop.Length - 1] })
            {
                float x0 = at.x - RoadHalf - KerbW - PaveW, x1 = at.x + RoadHalf + KerbW + PaveW;
                road.Box(M.asphalt, new Vector3(at.x - RoadHalf, 0f, -37.75f), new Vector3(at.x + RoadHalf, 0.14f, -34.45f));
                road.Quad(M.asphalt, new Vector3(at.x - RoadHalf, RoadTop, -34.2f), new Vector3(at.x + RoadHalf, RoadTop, -34.2f), new Vector3(at.x + RoadHalf, 0.14f, -34.45f), new Vector3(at.x - RoadHalf, 0.14f, -34.45f), new Vector3(0, 1f, -0.4f));
                road.Quad(M.asphalt, new Vector3(at.x - RoadHalf, 0.14f, -37.75f), new Vector3(at.x + RoadHalf, 0.14f, -37.75f), new Vector3(at.x + RoadHalf, RoadTop, -38.6f), new Vector3(at.x - RoadHalf, RoadTop, -38.6f), new Vector3(0, 1f, 0.1f));
                // give way: two dashed lines across the mouth
                for (float x = at.x - RoadHalf + 0.2f; x < at.x + RoadHalf - 0.3f; x += 0.9f)
                    foreach (float z in new[] { -38.9f, -39.3f }) marks.Box(M.line, new Vector3(x, RoadTop, z - 0.1f), new Vector3(x + 0.55f, RoadTop + 0.012f, z + 0.1f), 4);
            }
            // footpath straight across the green, main road to the south pavement
            paths.Box(M.asphalt, new Vector3(-1f, 0f, -104.5f + 0.0f), new Vector3(1f, 0.06f, -37.7f), 16 | 32);
            // street lamps along the outer pavements
            float acc = 0f;
            for (int i = 1; i < c.Count; i++)
            {
                acc += Vector3.Distance(c[i - 1], c[i]);
                if (acc < 28f) continue;
                acc = 0f;
                Vector3 t = (c[i] - c[i - 1]).normalized, nrm = Vector3.Cross(Vector3.up, t); // nrm points to the left of travel
                // outer side is the left when travelling P0 -> P3 (anticlockwise seen from above)... pick the side away from the green
                Vector3 toGreen = (new Vector3(0, 0, -72f) - c[i]); toGreen.y = 0;
                Vector3 outer = Vector3.Dot(nrm, toGreen) > 0 ? -nrm : nrm;
                Lamp(lamps, c[i] + outer * (RoadHalf + KerbW + PaveW - 0.35f), -outer);
            }
            var rg = road.Build("EstateRoad", g, true); if (rg != null) rg.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var pg = paths.Build("EstateGround", g, true); if (pg != null) pg.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mg = marks.Build("EstateMarkings", g, false); if (mg != null) mg.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var lg = lamps.Build("EstateLamps", g, false);
            if (lg != null)
                foreach (var p in lampSpots) { var bc = lg.AddComponent<BoxCollider>(); bc.center = p + Vector3.up * 3f; bc.size = new Vector3(0.22f, 6f, 0.22f); }
            lampSpots.Clear();
            // street sign on the corner of the green, facing traffic turning in
            Sign(g, new Vector3(Loop[0].x + RoadHalf + KerbW + PaveW + 1.2f, 0f, -40.2f), 0f);
        }

        static readonly List<Vector3> lampSpots = new List<Vector3>();

        /// <summary>A band beside the centreline from offset a to b (left positive), at height y, metre UVs.</summary>
        static void Strip(MeshKit k, List<Vector3> c, float a, float b, float y, Material m)
        {
            float t = MeshKit.TileOf(m), along = 0f;
            for (int i = 0; i < c.Count - 1; i++)
            {
                Vector3 n0 = Nrm(c, i), n1 = Nrm(c, i + 1);
                float l = Vector3.Distance(c[i], c[i + 1]);
                Vector3 p0 = c[i] + n0 * a, p1 = c[i + 1] + n1 * a, p2 = c[i + 1] + n1 * b, p3 = c[i] + n0 * b;
                p0.y = p1.y = p2.y = p3.y = y;
                k.QuadUV(m, p0, p1, p2, p3, Vector3.up, new Vector2(a / t, along / t), new Vector2(a / t, (along + l) / t), new Vector2(b / t, (along + l) / t), new Vector2(b / t, along / t));
                along += l;
            }
        }

        /// <summary>A vertical face along offset o from y0 to y1, facing towards side s (left positive).</summary>
        static void Edge(MeshKit k, List<Vector3> c, float o, float y0, float y1, Material m, int s)
        {
            float t = MeshKit.TileOf(m), along = 0f;
            for (int i = 0; i < c.Count - 1; i++)
            {
                Vector3 n0 = Nrm(c, i), n1 = Nrm(c, i + 1); float l = Vector3.Distance(c[i], c[i + 1]);
                Vector3 a = c[i] + n0 * o, b = c[i + 1] + n1 * o;
                Vector3 nn = ((n0 + n1) * 0.5f).normalized * -s;
                k.QuadUV(m, new Vector3(a.x, y0, a.z), new Vector3(b.x, y0, b.z), new Vector3(b.x, y1, b.z), new Vector3(a.x, y1, a.z), nn,
                    new Vector2(along / t, y0 / t), new Vector2((along + l) / t, y0 / t), new Vector2((along + l) / t, y1 / t), new Vector2(along / t, y1 / t));
                along += l;
            }
        }

        static Vector3 Nrm(List<Vector3> c, int i)
        {
            Vector3 t = (i == 0 ? c[1] - c[0] : i == c.Count - 1 ? c[i] - c[i - 1] : c[i + 1] - c[i - 1]); t.y = 0; t.Normalize();
            return Vector3.Cross(Vector3.up, t) * -1f; // left of travel
        }

        /// <summary>A grey steel column lamp: base, tapered shaft, an arm out over the road and a lantern that glows.</summary>
        static void Lamp(MeshKit k, Vector3 p, Vector3 toRoad)
        {
            lampSpots.Add(p);
            k.Cyl(M.lampCol, p, 0.11f, 0.9f, 8);
            k.Cyl(M.lampCol, p + Vector3.up * 0.9f, 0.075f, 5.2f, 8, true, 0.05f);
            var top = p + Vector3.up * 6.0f; var end = top + toRoad * 1.1f + Vector3.up * 0.15f;
            var mid = (top + end) * 0.5f;
            k.Push(Matrix4x4.TRS(mid, Quaternion.LookRotation(end - top), Vector3.one));
            k.Box(M.lampCol, new Vector3(-0.04f, -0.04f, -0.6f), new Vector3(0.04f, 0.04f, 0.6f));
            k.Pop();
            k.Push(Matrix4x4.TRS(end, Quaternion.LookRotation(toRoad), Vector3.one));
            k.Box(M.lampCol, new Vector3(-0.16f, -0.05f, -0.3f), new Vector3(0.16f, 0.08f, 0.3f));
            k.Box(M.glow, new Vector3(-0.13f, -0.08f, -0.26f), new Vector3(0.13f, -0.05f, 0.26f));
            k.Pop();
        }

        /// <summary>The street name plate on two black posts, readable from both faces.</summary>
        static void Sign(Transform parent, Vector3 at, float yaw)
        {
            var k = new MeshKit();
            k.Push(at, yaw);
            foreach (float x in new[] { -0.62f, 0.62f }) k.Box(M.post, new Vector3(x - 0.035f, 0f, -0.035f), new Vector3(x + 0.035f, 1.05f, 0.035f));
            Vector3 a = new Vector3(-0.68f, 0.62f, 0.04f), b = new Vector3(0.68f, 0.62f, 0.04f), c = new Vector3(0.68f, 0.96f, 0.04f), d = new Vector3(-0.68f, 0.96f, 0.04f);
            // seen from the front (looking -z) the left end is +x; from the back the left end is -x
            k.QuadUV(M.sign, a, b, c, d, Vector3.forward, new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1));
            Vector3 o = new Vector3(0, 0, -0.08f);
            k.QuadUV(M.sign, b + o, a + o, d + o, c + o, Vector3.back, new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1));
            k.Box(M.post, new Vector3(-0.7f, 0.6f, -0.04f), new Vector3(0.7f, 0.62f, 0.04f));
            k.Box(M.post, new Vector3(-0.7f, 0.96f, -0.04f), new Vector3(0.7f, 0.98f, 0.04f));
            k.Pop();
            var g = k.Build("EstateSign", parent, false);
            var bc = g.AddComponent<BoxCollider>(); bc.center = at + new Vector3(0, 0.5f, 0); bc.size = new Vector3(1.4f, 1f, 0.12f);
        }

        // ------------------------------------------------------------------------------------------ houses (runtime)
        public struct House { public Vector3 front, garage, porch, binSpot, driveEnd; public Quaternion facing; public int block, index; }

        /// <summary>Every house's key spots in world space (front door, garage, where the bins live, end of the drive).</summary>
        public static List<House> Houses(Transform estateRoot = null)
        {
            var list = new List<House>();
            Matrix4x4 R = estateRoot != null ? estateRoot.localToWorldMatrix : Matrix4x4.identity;
            for (int bi = 0; bi < Blocks.Length; bi++)
            {
                var b = Blocks[bi]; var m = R * Matrix4x4.TRS(b.origin, Quaternion.Euler(0, b.yaw, 0), Vector3.one);
                var rot = Quaternion.Euler(0, b.yaw, 0) * (estateRoot != null ? estateRoot.rotation : Quaternion.identity);
                for (int i = 0; i < b.n; i++)
                {
                    float x0 = i * W, x1 = x0 + W; bool mirror = (i % 2) == 1;
                    float gc = mirror ? x1 - 1.5f : x0 + 1.5f, pc = mirror ? x1 - 3.65f : x0 + 3.65f, lawn = mirror ? x0 + 0.6f : x1 - 0.55f;
                    list.Add(new House
                    {
                        block = bi, index = i, facing = rot,
                        front = m.MultiplyPoint3x4(new Vector3(pc, 0f, 0.3f)),
                        porch = m.MultiplyPoint3x4(new Vector3(pc, 0f, -0.6f)),
                        garage = m.MultiplyPoint3x4(new Vector3(gc, 0f, 0.4f)),
                        driveEnd = m.MultiplyPoint3x4(new Vector3(gc, 0f, Forecourt - 2.3f)),
                        binSpot = m.MultiplyPoint3x4(new Vector3(lawn, 0f, 1.2f)),
                    });
                }
            }
            return list;
        }

        /// <summary>
        /// At Play: wheelie bins by the houses (knock them over) and cars on some of the drives. Does nothing when the
        /// scene has no estate.
        /// </summary>
        public static void Dress(Transform parent)
        {
            var est = GameObject.Find(RootName); if (est == null) return;
            var root = new GameObject("EstateLoose").transform; root.SetParent(parent, false);
            var rng = new System.Random(4242);
            int bins = 0, cars = 0;
            string[] ids = { "hatch90", "hatch90", "hatch90", "saloon90", "saloon90" }; // only the detailed models
            foreach (var h in Houses(est.transform))
            {
                Vector3 fwd = h.facing * Vector3.forward, right = h.facing * Vector3.right;
                int n = rng.NextDouble() < 0.7 ? 2 : 1;
                for (int j = 0; j < n; j++)
                {
                    var p = h.binSpot + right * ((j - 0.5f) * 0.7f * (n - 1)) + fwd * (float)(rng.NextDouble() * 0.2);
                    bool fallen = rng.NextDouble() < 0.06;
                    WheelieBin(root, p, h.facing * Quaternion.Euler(0, 180f + (float)(rng.NextDouble() * 24 - 12), fallen ? 90f : 0f), j == 0 ? 0 : 1 + rng.Next(3));
                    bins++;
                }
                if (rng.NextDouble() < 0.42)
                {
                    string id = ids[rng.Next(ids.Length)];
                    var wear = CarKit.PickWear(rng, true);
                    if (wear >= CarKit.Wear.Wreck) wear = CarKit.Wear.Dirty;
                    var car = CarKit.Scenery(root, id, wear, h.driveEnd + Vector3.up * 0.05f, (h.facing * Quaternion.Euler(0, rng.NextDouble() < 0.7 ? 180f : 0f, 0)).eulerAngles.y, rng.Next());
                    if (car != null) { car.name = "Parked " + id; cars++; }
                }
            }
            int gardens = Gardens(root, rng);
            Desire(root);
            Debug.Log($"[ASBO] estate dressed: {bins} wheelie bins, {cars} cars on drives, {gardens} back gardens kitted out");
        }

        /// <summary>
        /// Back gardens that look lived in: a shed in the bottom corner of some, a rotary washing line with the odd bit of
        /// washing on it, plastic chairs on the patio, a hedge grown up along the back fence here and there, a kids' slide
        /// or a paddling pool. One mesh for the lot.
        /// </summary>
        static int Gardens(Transform root, System.Random rng)
        {
            if (M == null && !LoadMats()) return 0; // baked scene: the materials weren't loaded at start
            CotMats.Init();
            var k = new MeshKit(); int n = 0;
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            var hedge = CotMats.hedge != null ? CotMats.hedge : M.fence;
            var washing = new[] { M.white, M.playBlue, M.playRed, M.cream, M.playYellow };
            foreach (var b in Blocks)
            {
                var m = Matrix4x4.TRS(b.origin, Quaternion.Euler(0, b.yaw, 0), Vector3.one);
                for (int i = 0; i < b.n; i++)
                {
                    float x0 = i * W, x1 = x0 + W; bool mirror = (i % 2) == 1;
                    float zBack = -D - Garden, zPatio = -D - 2.6f;
                    k.Push(m); n++;
                    // shed in a back corner (felt pent roof, a door, a little window)
                    if (rng.NextDouble() < 0.5)
                    {
                        float sx0 = mirror ? x1 - 2.25f : x0 + 0.25f, sx1 = sx0 + 2.0f, sz0 = zBack + 0.25f, sz1 = sz0 + 1.5f;
                        k.Box(M.fence, new Vector3(sx0, 0f, sz0), new Vector3(sx1, 1.95f, sz1));
                        k.Push(Matrix4x4.TRS(new Vector3((sx0 + sx1) * 0.5f, 2.0f, (sz0 + sz1) * 0.5f), Quaternion.Euler(-6f, 0f, 0f), Vector3.one));
                        k.Box(M.felt, new Vector3(-1.12f, -0.04f, -0.88f), new Vector3(1.12f, 0.04f, 0.88f)); k.Pop();
                        k.Box(M.black, new Vector3(sx0 + 0.35f, 0f, sz1), new Vector3(sx0 + 1.05f, 1.75f, sz1 + 0.02f));
                        k.Box(M.windows, new Vector3(sx1 - 0.75f, 1.0f, sz1), new Vector3(sx1 - 0.25f, 1.45f, sz1 + 0.02f));
                    }
                    // rotary washing line out on the grass, sometimes with washing on
                    if (rng.NextDouble() < 0.45)
                    {
                        Vector3 c = new Vector3((x0 + x1) * 0.5f + R(-0.6f, 0.6f), 0f, zPatio - R(2.4f, 3.4f));
                        k.Box(M.post, c + new Vector3(-0.025f, 0f, -0.025f), c + new Vector3(0.025f, 1.85f, 0.025f));
                        float rot = R(0f, 90f);
                        for (int a = 0; a < 4; a++)
                        {
                            k.Push(Matrix4x4.TRS(c + Vector3.up * 1.8f, Quaternion.Euler(0f, rot + a * 90f, 0f), Vector3.one));
                            k.Box(M.post, new Vector3(-0.012f, -0.012f, 0f), new Vector3(0.012f, 0.012f, 1.25f)); k.Pop();
                        }
                        if (rng.NextDouble() < 0.6)
                            for (int w = 0; w < 3 + rng.Next(3); w++)
                            {
                                var mat = washing[rng.Next(washing.Length)]; float a = rot + rng.Next(4) * 90f + 45f, r0 = R(0.45f, 0.95f);
                                Vector3 at = c + Quaternion.Euler(0f, a, 0f) * Vector3.forward * r0 + Vector3.up * 1.78f;
                                k.Push(Matrix4x4.TRS(at, Quaternion.Euler(0f, a + 90f, 0f), Vector3.one));
                                k.Box(mat, new Vector3(-0.3f, -0.55f, -0.005f), new Vector3(0.3f, 0f, 0.005f)); k.Pop();
                            }
                    }
                    // a couple of white plastic chairs on the patio
                    if (rng.NextDouble() < 0.35)
                        for (int ch = 0; ch < 2; ch++)
                        {
                            Vector3 c = new Vector3(x0 + 1.2f + ch * 1.1f + R(-0.2f, 0.2f), 0.05f, -D - R(1.0f, 2.0f));
                            k.Push(Matrix4x4.TRS(c, Quaternion.Euler(0f, R(150f, 210f), 0f), Vector3.one));
                            k.Box(M.white, new Vector3(-0.24f, 0.4f, -0.24f), new Vector3(0.24f, 0.45f, 0.24f));
                            k.Box(M.white, new Vector3(-0.24f, 0.45f, 0.2f), new Vector3(0.24f, 0.88f, 0.24f));
                            foreach (var (lx, lz) in new[] { (-0.2f, -0.2f), (0.2f, -0.2f), (-0.2f, 0.2f), (0.2f, 0.2f) }) k.Box(M.white, new Vector3(lx - 0.02f, 0f, lz - 0.02f), new Vector3(lx + 0.02f, 0.4f, lz + 0.02f));
                            k.Pop();
                        }
                    // hedge grown up along the back fence
                    if (rng.NextDouble() < 0.35) k.Box(hedge, new Vector3(x0 + 0.15f, 0f, zBack + 0.1f), new Vector3(x1 - 0.15f, R(1.6f, 2.3f), zBack + R(0.6f, 0.9f)));
                    // kids' things: a little slide or a paddling pool
                    double t = rng.NextDouble();
                    if (t < 0.12) { Vector3 c = new Vector3((x0 + x1) * 0.5f + R(-1f, 1f), 0f, zBack + R(2.5f, 3.5f)); k.Push(Matrix4x4.TRS(c, Quaternion.Euler(0, R(0, 360), 0), Vector3.one)); k.Box(M.playYellow, new Vector3(-0.25f, 0f, -0.25f), new Vector3(0.25f, 0.9f, 0.25f)); k.Push(Matrix4x4.TRS(new Vector3(0, 0.45f, 0.75f), Quaternion.Euler(35f, 0, 0), Vector3.one)); k.Box(M.playRed, new Vector3(-0.22f, -0.02f, -0.6f), new Vector3(0.22f, 0.02f, 0.6f)); k.Pop(); k.Pop(); }
                    else if (t < 0.2) { Vector3 c = new Vector3((x0 + x1) * 0.5f + R(-1f, 1f), 0.02f, zBack + R(2.5f, 3.5f)); k.Cyl(M.playBlue, c, 0.8f, 0.22f, 16); }
                    k.Pop();
                }
            }
            var g = k.Build("BackGardens", root, false);
            if (g != null) g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return n;
        }

        /// <summary>
        /// Paths worn into the green where people actually walk, not where the council put the tarmac: the parade's corner
        /// to the footpath, the footpath to the tower door and on to the playground, and a short cut to the lock-ups.
        /// </summary>
        static void Desire(Transform root)
        {
            var m = CotMats.gravel != null ? CotMats.gravel : M.slabs; var k = new MeshKit();
            void Worn(params Vector3[] pts)
            {
                for (int i = 0; i < pts.Length - 1; i++)
                {
                    Vector3 a = pts[i], b = pts[i + 1], d = b - a; float len = d.magnitude; if (len < 0.1f) continue;
                    Vector3 sd = Vector3.Cross(Vector3.up, d / len) * 0.55f;
                    k.QuadUV(m, a - sd + Vector3.up * 0.03f, b - sd + Vector3.up * 0.03f, b + sd + Vector3.up * 0.03f, a + sd + Vector3.up * 0.03f, Vector3.up,
                        new Vector2(0, 0), new Vector2(len / 2f, 0), new Vector2(len / 2f, 0.55f), new Vector2(0, 0.55f));
                }
            }
            Worn(new Vector3(-23f, 0, -60f), new Vector3(-16f, 0, -63.5f), new Vector3(-8f, 0, -65f), new Vector3(-1.2f, 0, -67f));
            Worn(new Vector3(1.2f, 0, -60f), new Vector3(6f, 0, -57.5f), new Vector3(10f, 0, -55f), new Vector3(14f, 0, -53.5f));
            Worn(new Vector3(-1.2f, 0, -80f), new Vector3(-6f, 0, -83f), new Vector3(-10f, 0, -86.5f));
            Worn(new Vector3(1.2f, 0, -88f), new Vector3(8f, 0, -85f), new Vector3(14f, 0, -82.5f));
            var g = k.Build("WornPaths", root, false);
            if (g != null) g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>A few locals out and about on the estate: walking the pavements, crossing the green, two lads by the sign.</summary>
        public static void Locals()
        {
            if (GameObject.Find(RootName) == null) return;
            var rng = new System.Random(55);
            Color C() => Color.HSVToRGB((float)rng.NextDouble(), 0.2f + (float)rng.NextDouble() * 0.5f, 0.25f + (float)rng.NextDouble() * 0.55f);
            void Walk(float speed, params Vector3[] pts)
            {
                var p = World.MakePerson(pts[0], C(), C() * 0.6f, "Pedestrian");
                for (int i = 1; i < pts.Length; i++) p.route.Add(pts[i]);
                p.route.Add(pts[0]); p.speed = speed;
            }
            float o = RoadHalf + KerbW + PaveW * 0.5f;
            Walk(1.25f, new Vector3(-40f - o, 0, -40f), new Vector3(-40f - o, 0, -100f), new Vector3(-30f, 0, -110f - o), new Vector3(30f, 0, -110f - o), new Vector3(40f + o, 0, -100f), new Vector3(40f + o, 0, -40f));
            Walk(1.1f, new Vector3(40f - o, 0, -42f), new Vector3(40f - o, 0, -98f), new Vector3(26f, 0, -110f + o), new Vector3(-26f, 0, -110f + o), new Vector3(-40f + o, 0, -98f), new Vector3(-40f + o, 0, -42f));
            Walk(1.3f, new Vector3(0f, 0, -38.5f), new Vector3(0f, 0, -103.5f));
            Walk(0.55f, new Vector3(-32.6f, 0, -41.6f), new Vector3(-31.4f, 0, -43.2f));
            Walk(0.6f, new Vector3(-31.8f, 0, -43.6f), new Vector3(-33.2f, 0, -42f));
            Walk(1.15f, new Vector3(-46.5f, 0, -60f), new Vector3(-36f, 0, -75f), new Vector3(-12f, 0, -75f)); // round the end of Hawthorn Parade
        }

        static Material[] binMats;
        /// <summary>
        /// A 240-litre wheelie bin: body, hinged lid, two wheels and the handle bar. Lid colours: 0 black (general),
        /// 1 green (garden), 2 blue (recycling), 3 brown (food). Loose: kick it, drive into it.
        /// </summary>
        public static GameObject WheelieBin(Transform parent, Vector3 pos, Quaternion rot, int kind)
        {
            if (binMats == null)
                binMats = new[]
                {
                    Gfx.Mat(new Color(0.07f, 0.07f, 0.075f), null, 0.3f), Gfx.Mat(new Color(0.09f, 0.26f, 0.13f), null, 0.3f),
                    Gfx.Mat(new Color(0.1f, 0.22f, 0.5f), null, 0.3f), Gfx.Mat(new Color(0.3f, 0.19f, 0.1f), null, 0.3f),
                };
            var body = kind == 1 ? binMats[1] : binMats[0];
            var k = new MeshKit();
            // slightly tapered body, origin on the ground at its middle
            Vector3 b0 = new Vector3(-0.27f, 0.06f, -0.34f), b1 = new Vector3(0.27f, 0.06f, 0.34f), t0 = new Vector3(-0.3f, 0.98f, -0.38f), t1 = new Vector3(0.3f, 0.98f, 0.38f);
            Vector3[] q = { new Vector3(b0.x, b0.y, b0.z), new Vector3(b1.x, b0.y, b0.z), new Vector3(b1.x, b0.y, b1.z), new Vector3(b0.x, b0.y, b1.z),
                            new Vector3(t0.x, t0.y, t0.z), new Vector3(t1.x, t0.y, t0.z), new Vector3(t1.x, t0.y, t1.z), new Vector3(t0.x, t0.y, t1.z) };
            k.Quad(body, q[0], q[1], q[5], q[4], Vector3.back); k.Quad(body, q[1], q[2], q[6], q[5], Vector3.right);
            k.Quad(body, q[2], q[3], q[7], q[6], Vector3.forward); k.Quad(body, q[3], q[0], q[4], q[7], Vector3.left);
            k.Quad(body, q[0], q[3], q[2], q[1], Vector3.down);
            var lid = binMats[Mathf.Clamp(kind, 0, 3)];
            k.Box(lid, new Vector3(-0.32f, 0.98f, -0.4f), new Vector3(0.32f, 1.04f, 0.43f));
            k.Box(body, new Vector3(-0.28f, 0.92f, 0.38f), new Vector3(0.28f, 0.99f, 0.46f)); // handle bar at the back
            foreach (float x in new[] { -0.26f, 0.26f }) k.CylX(binMats[0], new Vector3(x, 0.1f, 0.33f), 0.1f, 0.06f, 10);
            var g = k.Build("wheelie_bin", parent, false);
            g.transform.SetPositionAndRotation(pos, rot);
            var bc = g.AddComponent<BoxCollider>(); bc.center = new Vector3(0, 0.53f, 0.02f); bc.size = new Vector3(0.6f, 1.02f, 0.82f);
            var rb = g.AddComponent<Rigidbody>(); rb.mass = 14f; rb.linearDamping = 0.3f; rb.angularDamping = 0.6f; rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete; rb.Sleep();
            g.AddComponent<Knockable>();
            return g;
        }
    }
}

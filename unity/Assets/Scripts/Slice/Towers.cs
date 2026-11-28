using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>
    /// Council housing from the user's photos, as builders the estates use:
    ///  - point blocks (tower blocks) in three styles: 70s brown brick with a white-framed window strip up the middle,
    ///    grey precast panels with horizontal bands and corner balconies, beige pebble-dash with a grid of windows,
    ///    balconies and satellite dishes; flat roofs with a lift motor room; a glazed entrance with a canopy and a lobby
    ///    inside (lifts, stairs, post boxes, notice board, strip light);
    ///  - deck-access slabs like Park Hill: concrete frame, brick infill fading from red to yellow up the block, a deck
    ///    "street in the sky" every third floor, stair towers at the ends;
    ///  - three-storey walk-up flats with hipped tiled roofs and common stair doors;
    ///  - lock-up garages, a caged ball court with goals and hoops, a playground.
    /// Every building is one mesh (a handful of materials) plus colliders, so a tower is a few draw calls.
    /// Local frame: origin at the front-left corner on the ground, x along the front, the front faces -z, depth runs +z.
    /// </summary>
    public static class Towers
    {
        public enum Style { BrownBrick, GreyPanel, Pebble }
        public struct Spec { public string name, plate; public Vector3 origin; public float yaw, w, d; public int storeys, seed; public Style style; }
        const float FH = 2.75f, GF = 3.3f; // floor height, ground floor (lobby) height
        static CouncilEstate.Mats E;

        // window cells in the TownTowerWindows atlas (4 x 4)
        public const int Nets = 0, CurtRed = 1, CurtBlue = 2, Blinds = 3, Dark = 4, CurtHalf = 5, Boarded = 6, NetsPlant = 7, BalcDoor = 8, BalcDoorCurt = 9, Stair = 10, Frosted = 11, Lit = 12, Crittall = 13, CurtGreen = 14, Foil = 15;
        static Rect Cell(int c) => new Rect((c % 4) * 0.25f + 0.004f, 1f - (c / 4 + 1) * 0.25f + 0.004f, 0.242f, 0.242f);
        static int RandomWindow(System.Random r)
        {
            double t = r.NextDouble();
            return t < 0.22 ? Nets : t < 0.36 ? CurtRed : t < 0.48 ? CurtBlue : t < 0.62 ? Blinds : t < 0.72 ? Dark : t < 0.82 ? CurtHalf : t < 0.88 ? NetsPlant : t < 0.93 ? CurtGreen : t < 0.965 ? Boarded : t < 0.985 ? Lit : Foil;
        }

        static bool Ready() { if (!TownMats.Load()) return false; E = CouncilEstate.M; return true; }

        static Transform Root(Transform parent, string name, Vector3 origin, float yaw)
        {
            var g = new GameObject(name).transform; g.SetParent(parent, false); g.localPosition = origin; g.localRotation = Quaternion.Euler(0, yaw, 0); return g;
        }

        /// <summary>A window on a wall: the glazed unit from the atlas, a concrete sill under it and a reveal head over it.</summary>
        /// <summary>While a point block is being built, every window it gets is noted here for the interior to cut through.</summary>
        static List<TowerInterior.WinRec> rec;
        static void Win(MeshKit k, Vector3 o, Vector3 r, Vector3 n, float x0, float x1, float yb, float yt, int cell, Material sill = null)
        {
            if (rec != null)
            {
                Vector3 pa = o + r * x0, pb = o + r * x1; char sd = n.z < -0.5f ? 'F' : n.x > 0.5f ? 'R' : n.z > 0.5f ? 'B' : 'L';
                bool fb = sd == 'F' || sd == 'B';
                rec.Add(new TowerInterior.WinRec { side = sd, a0 = fb ? Mathf.Min(pa.x, pb.x) : Mathf.Min(pa.z, pb.z), a1 = fb ? Mathf.Max(pa.x, pb.x) : Mathf.Max(pa.z, pb.z), y0 = yb, y1 = yt });
            }
            var uv = Cell(cell); Vector3 off = n * 0.025f;
            Vector3 a = o + r * x0 + Vector3.up * yb + off, b = o + r * x1 + Vector3.up * yb + off, c = o + r * x1 + Vector3.up * yt + off, d = o + r * x0 + Vector3.up * yt + off;
            k.QuadUV(TownMats.towerWin, a, b, c, d, n, new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMin, uv.yMax));
            if (sill != null)
            {
                // sill: a little ledge proud of the wall
                Vector3 s0 = o + r * (x0 - 0.06f) + Vector3.up * (yb - 0.08f), s1 = o + r * (x1 + 0.06f) + Vector3.up * yb;
                k.Quad(sill, s0 + n * 0.12f, s0.WithY(s0.y) + r * (x1 - x0 + 0.12f) + n * 0.12f, s0 + r * (x1 - x0 + 0.12f) + n * 0.12f + Vector3.up * 0.08f, s0 + n * 0.12f + Vector3.up * 0.08f, n);
                k.Quad(sill, s0 + n * 0.12f + Vector3.up * 0.08f, s0 + r * (x1 - x0 + 0.12f) + n * 0.12f + Vector3.up * 0.08f, s0 + r * (x1 - x0 + 0.12f) + Vector3.up * 0.08f, s0 + Vector3.up * 0.08f, Vector3.up);
            }
        }

        /// <summary>A box from any two opposite corners (the faces' corners come in either order on the back and side walls).</summary>
        static void B(MeshKit k, Material m, Vector3 a, Vector3 b) => k.Box(m, Vector3.Min(a, b), Vector3.Max(a, b));

        static void Wall(MeshKit k, Material m, Vector3 o, Vector3 r, Vector3 n, float len, float y0, float y1)
        {
            k.Quad(m, o + Vector3.up * y0, o + r * len + Vector3.up * y0, o + r * len + Vector3.up * y1, o + Vector3.up * y1, n);
        }

        /// <summary>A horizontal band of concrete proud of the wall (floor edges, sill courses).</summary>
        static void Band(MeshKit k, Material m, Vector3 o, Vector3 r, Vector3 n, float len, float y, float h, float proud)
        {
            Vector3 a = o + Vector3.up * y + n * proud, b = o + r * len + Vector3.up * y + n * proud;
            k.Quad(m, a, b, b + Vector3.up * h, a + Vector3.up * h, n);
            k.Quad(m, a + Vector3.up * h, b + Vector3.up * h, b + Vector3.up * h - n * proud, a + Vector3.up * h - n * proud, Vector3.up);
            k.Quad(m, a, b, b - n * proud, a - n * proud, Vector3.down);
        }

        static void Dish(MeshKit k, Vector3 at, Vector3 n)
        {
            k.Push(Matrix4x4.TRS(at + n * 0.25f, Quaternion.LookRotation(n + Vector3.up * 0.35f), Vector3.one));
            k.Sphere(E.white, new Vector3(0, 0, -0.06f), new Vector3(0.32f, 0.32f, 0.1f), 8, 4, true);
            k.Box(E.black, new Vector3(-0.02f, -0.02f, -0.25f), new Vector3(0.02f, 0.02f, 0.05f));
            k.Pop();
        }

        // ================================================================== point blocks
        public static Transform Point(Transform parent, Spec s)
        {
            if (!Ready()) return null;
            var g = Root(parent, s.name, s.origin, s.yaw);
            var rng = new System.Random(s.seed);
            var k = new MeshKit(); var inside = new MeshKit();
            Material wall = s.style == Style.BrownBrick ? TownMats.brownBrick : s.style == Style.GreyPanel ? TownMats.panel : TownMats.pebble;
            Material band = s.style == Style.BrownBrick ? TownMats.panel : s.style == Style.GreyPanel ? TownMats.panelDark : TownMats.panel;
            float W = s.w, D = s.d, top = GF + (s.storeys - 1) * FH;
            var wrec = new List<TowerInterior.WinRec>(); rec = wrec;
            // the four faces: origin, along, outward normal, length
            var faces = new (Vector3 o, Vector3 r, Vector3 n, float len)[]
            {
                (new Vector3(0, 0, 0), Vector3.right, Vector3.back, W), (new Vector3(W, 0, 0), Vector3.forward, Vector3.right, D),
                (new Vector3(W, 0, D), Vector3.left, Vector3.forward, W), (new Vector3(0, 0, D), Vector3.back, Vector3.left, D),
            };
            for (int f = 0; f < 4; f++)
            {
                var (o, r, n, len) = faces[f];
                // ground floor: darker plinth, the entrance on the front
                if (f == 0)
                {
                    float dx0 = W * 0.5f - 1.6f, dx1 = W * 0.5f + 1.6f;
                    Wall(k, TownMats.panelDark, o, r, n, dx0, 0f, GF); Wall(k, TownMats.panelDark, o + r * dx1, r, n, len - dx1, 0f, GF);
                    Wall(k, TownMats.panelDark, o + r * dx0, r, n, dx1 - dx0, 2.6f, GF);
                    // glazed entrance screen set back, a real door in the middle of it (the interior hangs the leaf), a concrete canopy over
                    k.Box(TownMats.glassDark, new Vector3(dx0, 0f, 1.2f), new Vector3(W * 0.5f - 0.55f, 2.6f, 1.3f));
                    k.Box(TownMats.glassDark, new Vector3(W * 0.5f + 0.55f, 0f, 1.2f), new Vector3(dx1, 2.6f, 1.3f));
                    k.Box(TownMats.glassDark, new Vector3(W * 0.5f - 0.55f, 2.15f, 1.2f), new Vector3(W * 0.5f + 0.55f, 2.6f, 1.3f));
                    foreach (float px in new[] { W * 0.5f - 0.55f, W * 0.5f + 0.5f }) k.Box(TownMats.steel, new Vector3(px, 0f, 1.17f), new Vector3(px + 0.05f, 2.6f, 1.31f));
                    foreach (float x in new[] { dx0, dx1 }) k.Box(TownMats.panelDark, new Vector3(x - 0.15f, 0f, 0f), new Vector3(x + 0.15f, 2.6f, 1.25f));
                    k.Box(TownMats.panel, new Vector3(dx0 - 1.2f, 2.65f, -1.8f), new Vector3(dx1 + 1.2f, 2.9f, 0.05f));
                    k.Box(TownMats.steel, new Vector3(dx1 + 0.3f, 1.2f, -0.06f), new Vector3(dx1 + 0.6f, 1.6f, 0f)); // the door entry panel
                    if (!string.IsNullOrEmpty(s.plate)) Plate(k, new Vector3(W * 0.5f, 3.05f, -0.03f), s.plate);
                }
                else Wall(k, TownMats.panelDark, o, r, n, len, 0f, GF);
                Band(k, band, o, r, n, len, GF - 0.25f, 0.3f, 0.15f);
                // upper floors
                Wall(k, wall, o, r, n, len, GF, top);
                for (int fl = 1; fl < s.storeys; fl++)
                {
                    float y = GF + (fl - 1) * FH;
                    if (s.style == Style.GreyPanel) Band(k, band, o, r, n, len, y + FH - 0.32f, 0.32f, 0.12f);
                    if (s.style == Style.BrownBrick)
                    {
                        // the central window strip in white frames (a tall bay up the middle), small windows near the corners
                        float bay0 = len * 0.32f, bay1 = len * 0.68f; int nb = Mathf.Max(2, Mathf.RoundToInt((bay1 - bay0) / 1.9f));
                        for (int i = 0; i < nb; i++) { float x0 = bay0 + i * (bay1 - bay0) / nb + 0.1f, x1 = bay0 + (i + 1) * (bay1 - bay0) / nb - 0.1f; Win(k, o, r, n, x0, x1, y + 0.75f, y + 2.25f, RandomWindow(rng), band); }
                        foreach (float x in new[] { len * 0.1f, len * 0.86f }) Win(k, o, r, n, x, x + 1.1f, y + 1.0f, y + 2.1f, f % 2 == 0 ? RandomWindow(rng) : Frosted);
                        Band(k, band, o + r * bay0, r, n, bay1 - bay0, y + 0.55f, 0.2f, 0.1f); // white spandrel under the strip
                    }
                    else if (s.style == Style.GreyPanel)
                    {
                        // ribbon windows between the bands, corner balconies set into the far corners
                        int nw = Mathf.Max(2, Mathf.RoundToInt((len - 5f) / 2.1f));
                        for (int i = 0; i < nw; i++) { float x0 = 2.6f + i * (len - 5.2f) / nw + 0.08f, x1 = 2.6f + (i + 1) * (len - 5.2f) / nw - 0.08f; Win(k, o, r, n, x0, x1, y + 0.85f, y + 2.25f, RandomWindow(rng)); }
                        Win(k, o, r, n, len - 2.3f, len - 0.5f, y + 0.05f, y + 2.25f, rng.NextDouble() < 0.5 ? BalcDoor : BalcDoorCurt);
                        // the balcony: a slab and a solid panel front
                        B(k, band, o + r * (len - 2.5f) + n * 0.0f + Vector3.up * (y - 0.05f) + Vector3.Cross(Vector3.up, r) * 0f, o + r * (len - 0.2f) + n * 1.25f + Vector3.up * (y + 0.1f));
                        B(k, band, o + r * (len - 2.5f) + n * 1.15f + Vector3.up * (y + 0.1f), o + r * (len - 0.2f) + n * 1.25f + Vector3.up * (y + 1.05f));
                    }
                    else
                    {
                        // pebble-dash: a regular grid, every other flat with a little balcony on the front and back
                        int nw = Mathf.Max(3, Mathf.RoundToInt((len - 1.5f) / 2.3f));
                        for (int i = 0; i < nw; i++)
                        {
                            float x0 = 0.75f + i * (len - 1.5f) / nw + 0.25f, x1 = 0.75f + (i + 1) * (len - 1.5f) / nw - 0.25f;
                            bool balc = (f == 0 || f == 2) && i % 3 == 1;
                            Win(k, o, r, n, x0, x1, y + (balc ? 0.05f : 0.8f), y + 2.2f, balc ? (rng.NextDouble() < 0.5 ? BalcDoor : BalcDoorCurt) : RandomWindow(rng), balc ? null : band);
                            if (balc)
                            {
                                B(k, band, o + r * (x0 - 0.3f) + Vector3.up * (y - 0.08f), o + r * (x1 + 0.3f) + n * 1.1f + Vector3.up * (y + 0.06f));
                                for (float bx = x0 - 0.25f; bx <= x1 + 0.26f; bx += 0.14f) B(k, TownMats.steel, o + r * bx + n * 1.05f + Vector3.up * y, o + r * (bx + 0.02f) + n * 1.08f + Vector3.up * (y + 1.0f));
                                B(k, TownMats.steel, o + r * (x0 - 0.3f) + n * 1.03f + Vector3.up * (y + 1.0f), o + r * (x1 + 0.3f) + n * 1.1f + Vector3.up * (y + 1.05f));
                            }
                            if (rng.NextDouble() < 0.07) Dish(k, o + r * (x1 + 0.35f) + Vector3.up * (y + 1.7f), n);
                        }
                    }
                    if (s.style != Style.Pebble && rng.NextDouble() < 0.12) Dish(k, o + r * (len * (0.25f + (float)rng.NextDouble() * 0.5f)) + Vector3.up * (y + 1.9f), n);
                }
                // a stair window column on one side face
                if (f == 1) for (int fl = 1; fl < s.storeys; fl++) { float y = GF + (fl - 1) * FH + 1.2f; Win(k, o, r, n, len * 0.5f - 0.45f, len * 0.5f + 0.45f, y, y + 1.2f, Stair); }
                // parapet at the top
                Band(k, band, o, r, n, len, top, 0.75f, 0.12f);
            }
            // roof: felt, parapet inner faces, the lift motor room and a water tank, aerials
            k.Quad(E.felt, new Vector3(0, top + 0.02f, 0), new Vector3(W, top + 0.02f, 0), new Vector3(W, top + 0.02f, D), new Vector3(0, top + 0.02f, D), Vector3.up);
            k.Box(band, new Vector3(W * 0.5f - 2.2f, top, D * 0.5f - 2.6f), new Vector3(W * 0.5f + 2.2f, top + 3f, D * 0.5f + 2.6f));
            k.Box(TownMats.metalSheet, new Vector3(W * 0.5f + 2.5f, top, D * 0.5f - 1.2f), new Vector3(W * 0.5f + 5f, top + 2f, D * 0.5f + 1.2f));
            for (int a = 0; a < 3; a++) { float ax = 1.5f + a * 1.6f; k.Box(TownMats.steel, new Vector3(ax, top + 0.75f, D - 1.5f), new Vector3(ax + 0.04f, top + 3.5f, D - 1.46f)); k.Box(TownMats.steel, new Vector3(ax - 0.6f, top + 3.1f, D - 1.5f), new Vector3(ax + 0.6f, top + 3.14f, D - 1.46f)); }
            // graffiti round the bottom: the local gang's tags
            Graffiti(k, faces, rng, s.seed % 3);
            rec = null;
            var body = k.Build("Tower", g, false, ShadowCastingMode.On);
            // colliders while nobody's near: the whole block solid above the ground floor, solid round the entrance; the interior swaps them out
            var baked = new List<Collider>
            {
                Coll(g, new Vector3(0, GF, 0), new Vector3(W, top + 0.8f, D)),
                Coll(g, new Vector3(0, 0, 0), new Vector3(W * 0.5f - 1.6f, GF, D)), Coll(g, new Vector3(W * 0.5f + 1.6f, 0, 0), new Vector3(W, GF, D)),
                Coll(g, new Vector3(W * 0.5f - 1.6f, 0, 1.3f), new Vector3(W * 0.5f + 1.6f, GF, D)),
            };
            TowerInterior.Bake(g, Vector3.zero, 0f, s.name, W, D, GF, FH, s.storeys, W * 0.5f, 1.3f, W * 0.5f - 1.6f, W * 0.5f + 1.6f, 2.6f, 0f, 0f, s.seed * 97 + 11, wrec, baked);
            return g;
        }

        static BoxCollider Coll(Transform g, Vector3 mn, Vector3 mx)
        {
            var c = new GameObject("Solid").AddComponent<BoxCollider>(); c.transform.SetParent(g, false);
            c.center = (mn + mx) * 0.5f; c.size = mx - mn; return c;
        }

        static void Plate(MeshKit k, Vector3 at, string text)
        {
            // block names are lettered on a white plate (the letters themselves come from the sign atlas in the estate builder)
            k.Box(E.white, at + new Vector3(-1.4f, -0.2f, -0.02f), at + new Vector3(1.4f, 0.2f, 0.02f));
        }

        /// <summary>Behind the entrance: lino floor, painted walls, two lifts, the stair door and a flight up, post boxes, a notice board.</summary>
        static void Lobby(MeshKit k, float W, string name)
        {
            float x0 = W * 0.5f - 3.2f, x1 = W * 0.5f + 3.2f, z0 = 1.3f, z1 = 7.2f, h = GF - 0.3f;
            k.Quad(TownMats.lino, new Vector3(x0, 0.02f, z0 - 1.3f), new Vector3(x1, 0.02f, z0 - 1.3f), new Vector3(x1, 0.02f, z1), new Vector3(x0, 0.02f, z1), Vector3.up);
            k.Quad(TownMats.lobbyWall, new Vector3(x0, h, z0), new Vector3(x1, h, z0), new Vector3(x1, h, z1), new Vector3(x0, h, z1), Vector3.down);
            k.Quad(TownMats.lobbyWall, new Vector3(x0, 0, z1), new Vector3(x1, 0, z1), new Vector3(x1, h, z1), new Vector3(x0, h, z1), Vector3.back);
            k.Quad(TownMats.lobbyWall, new Vector3(x0, 0, z0), new Vector3(x0, 0, z1), new Vector3(x0, h, z1), new Vector3(x0, h, z0), Vector3.right);
            k.Quad(TownMats.lobbyWall, new Vector3(x1, 0, z1), new Vector3(x1, 0, z0), new Vector3(x1, h, z0), new Vector3(x1, h, z1), Vector3.left);
            // two lifts on the back wall, steel doors in steel frames, call buttons
            foreach (float lx in new[] { W * 0.5f - 1.6f, W * 0.5f + 0.4f })
            {
                k.Box(TownMats.steel, new Vector3(lx - 0.1f, 0f, z1 - 0.08f), new Vector3(lx + 1.3f, 2.25f, z1 - 0.02f));
                k.Box(TownMats.liftDoor, new Vector3(lx, 0f, z1 - 0.1f), new Vector3(lx + 1.2f, 2.1f, z1 - 0.08f));
                k.Box(TownMats.liftDoor, new Vector3(lx + 0.6f - 0.005f, 0f, z1 - 0.11f), new Vector3(lx + 0.6f + 0.005f, 2.1f, z1 - 0.1f));
            }
            k.Box(TownMats.hiVis, new Vector3(W * 0.5f - 0.2f, 1.1f, z1 - 0.06f), new Vector3(W * 0.5f - 0.1f, 1.3f, z1 - 0.02f));
            // the stair door (heavy, wired glass) and a flight of stairs up behind the right-hand wall
            k.Box(E.black, new Vector3(x1 - 0.06f, 0f, z0 + 0.8f), new Vector3(x1 - 0.02f, 2.1f, z0 + 1.8f));
            for (int i = 0; i < 9; i++) k.Box(TownMats.panel, new Vector3(x1 - 1.4f, 0f, z0 + 2.2f + i * 0.28f), new Vector3(x1 - 0.1f, 0.18f * (i + 1), z0 + 2.5f + i * 0.28f));
            k.Box(E.black, new Vector3(x1 - 1.5f, 0.9f, z0 + 2.2f), new Vector3(x1 - 1.45f, 2.6f, z0 + 4.8f)); // the stair rail
            // post boxes and the notice board on the left wall
            k.Box(TownMats.steel, new Vector3(x0 + 0.02f, 0.9f, z0 + 0.6f), new Vector3(x0 + 0.32f, 1.9f, z0 + 2.8f));
            for (int r = 0; r < 4; r++) for (int c = 0; c < 6; c++) k.Box(E.black, new Vector3(x0 + 0.32f, 1.0f + r * 0.23f, z0 + 0.7f + c * 0.35f), new Vector3(x0 + 0.33f, 1.05f + r * 0.23f, z0 + 0.95f + c * 0.35f));
            k.Box(TownMats.notice, new Vector3(x0 + 0.02f, 1.2f, z0 + 3.3f), new Vector3(x0 + 0.06f, 2.0f, z0 + 4.5f));
            // strip light
            k.Box(E.glow, new Vector3(W * 0.5f - 0.7f, h - 0.06f, (z0 + z1) * 0.5f - 0.08f), new Vector3(W * 0.5f + 0.7f, h - 0.02f, (z0 + z1) * 0.5f + 0.08f));
        }

        /// <summary>Spray-painted tags on the ground floor walls (cells of the TownGraffiti atlas: one row per gang).</summary>
        static void Graffiti(MeshKit k, (Vector3 o, Vector3 r, Vector3 n, float len)[] faces, System.Random rng, int gang)
        {
            if (TownMats.graffiti == null) return;
            int n = 2 + rng.Next(4);
            for (int i = 0; i < n; i++)
            {
                var (o, r, nn, len) = faces[rng.Next(faces.Length)];
                float w = 1.6f + (float)rng.NextDouble() * 2f, h = w * 0.45f, x = 0.5f + (float)rng.NextDouble() * Mathf.Max(0.1f, len - w - 1f), y = 0.4f + (float)rng.NextDouble() * 0.9f;
                if (nn == Vector3.back && Mathf.Abs(x + w * 0.5f - len * 0.5f) < 2.5f) continue; // not over the doors
                int cell = gang * 4 + rng.Next(4); // 4 x 4 atlas: rows are gangs, the last row general scrawl
                if (rng.NextDouble() < 0.3) cell = 12 + rng.Next(4);
                float cu = (cell % 4) * 0.25f, cv = 0.75f - (cell / 4) * 0.25f;
                Vector3 off = nn * 0.03f;
                k.QuadUV(TownMats.graffiti, o + r * x + Vector3.up * y + off, o + r * (x + w) + Vector3.up * y + off, o + r * (x + w) + Vector3.up * (y + h) + off, o + r * x + Vector3.up * (y + h) + off, nn,
                    new Vector2(cu, cv), new Vector2(cu + 0.25f, cv), new Vector2(cu + 0.25f, cv + 0.25f), new Vector2(cu, cv + 0.25f));
            }
        }

        // ================================================================== deck-access slab (Park Hill)
        public static Transform Slab(Transform parent, string name, Vector3 origin, float yaw, float L, int storeys, int seed)
        {
            if (!Ready()) return null;
            var g = Root(parent, name, origin, yaw); var rng = new System.Random(seed);
            var k = new MeshKit(); const float D = 11f, F = 2.85f, bay = 6f;
            float top = storeys * F; int bays = Mathf.Max(3, Mathf.RoundToInt(L / bay)); L = bays * bay;
            var wrec = new List<TowerInterior.WinRec>(); rec = wrec;
            Material[] infill = { TownMats.deckBrick0, TownMats.deckBrick1, TownMats.deckBrick2, TownMats.deckBrick3 };
            foreach (var (o, r, n) in new[] { (Vector3.zero, Vector3.right, Vector3.back), (new Vector3(L, 0, D), Vector3.left, Vector3.forward) })
            {
                bool front = n == Vector3.back;
                for (int fl = 0; fl < storeys; fl++)
                {
                    float y = fl * F; var brick = infill[Mathf.Min(3, fl * 4 / Mathf.Max(1, storeys))];
                    bool deck = front && fl > 0 && fl % 3 == 1; // streets in the sky on the front
                    for (int b = 0; b < bays; b++)
                    {
                        float x0 = b * bay, x1 = x0 + bay;
                        if (front && fl == 0 && b % 2 == 0 && b + 1 < bays)
                        {
                            // a stair door in the first bay of each pair (the interior hangs the door), a window beside it
                            // window x0+0.35..4.1, door x0+4.3..5.4
                            Wall(k, brick, o + r * x0, r, n, 4.3f, y, y + 0.95f); Wall(k, brick, o + r * x0, r, n, 4.3f, y + 2.35f, y + F);
                            Wall(k, brick, o + r * x0, r, n, 0.35f, y + 0.95f, y + 2.35f); Wall(k, brick, o + r * (x0 + 4.1f), r, n, 0.2f, y + 0.95f, y + 2.35f);
                            Win(k, o, r, n, x0 + 0.35f, x0 + 4.1f, y + 0.95f, y + 2.35f, Crittall);
                            Wall(k, brick, o + r * (x0 + 4.3f), r, n, 1.1f, y + 2.2f, y + F); Wall(k, brick, o + r * (x0 + 5.4f), r, n, bay - 5.4f, y, y + F);
                            B(k, TownMats.panel, o + r * (x0 + 4.2f) + Vector3.up * 2.2f, o + r * (x0 + 5.5f) + n * 0.6f + Vector3.up * 2.35f);
                            continue;
                        }
                        // brick infill below the windows, the windows (or a front door and a window on the deck levels)
                        Wall(k, brick, o + r * x0, r, n, bay, y, y + 0.95f);
                        Wall(k, brick, o + r * x0, r, n, bay, y + 2.35f, y + F);
                        if (deck)
                        {
                            Wall(k, brick, o + r * x0, r, n, 0.4f, y + 0.95f, y + 2.35f);
                            Win(k, o, r, n, x0 + 0.4f, x0 + 1.4f, y + 0.02f, y + 2.2f, BalcDoorCurt); // front door
                            Win(k, o, r, n, x0 + 1.6f, x1 - 0.4f, y + 0.95f, y + 2.2f, RandomWindow(rng));
                            Wall(k, brick, o + r * (x1 - 0.4f), r, n, 0.4f, y + 0.95f, y + 2.35f);
                        }
                        else
                        {
                            Win(k, o, r, n, x0 + 0.35f, x1 - 0.35f, y + 0.95f, y + 2.35f, fl == 0 ? Crittall : RandomWindow(rng));
                            Wall(k, brick, o + r * x0, r, n, 0.35f, y + 0.95f, y + 2.35f); Wall(k, brick, o + r * (x1 - 0.35f), r, n, 0.35f, y + 0.95f, y + 2.35f);
                        }
                    }
                    // the frame: floor slab edges
                    Band(k, TownMats.panel, o, r, n, L, y + F - 0.22f, 0.3f, 0.22f);
                    if (deck)
                    {
                        // the deck: 2.4 m wide, solid concrete parapet
                        Vector3 a = o + Vector3.up * (y - 0.25f), b = o + r * L + Vector3.up * (y - 0.25f);
                        k.Quad(TownMats.panel, a + n * 2.4f, b + n * 2.4f, b + n * 2.4f + Vector3.up * 0.25f, a + n * 2.4f + Vector3.up * 0.25f, n);
                        k.Quad(TownMats.lobbyFloor, a + Vector3.up * 0.25f, b + Vector3.up * 0.25f, b + n * 2.4f + Vector3.up * 0.25f, a + n * 2.4f + Vector3.up * 0.25f, Vector3.up);
                        k.Quad(TownMats.panel, a, b, b + n * 2.4f, a + n * 2.4f, Vector3.down);
                        k.Box(TownMats.panel, Vector3.Min(a + n * 2.25f, b + n * 2.4f) + Vector3.up * 0.25f, Vector3.Max(a + n * 2.25f, b + n * 2.4f) + Vector3.up * 1.35f);
                    }
                }
                // the frame: columns every bay, full height
                for (int b = 0; b <= bays; b++) B(k, TownMats.panel, o + r * (b * bay - 0.22f) + n * 0.0f, o + r * (b * bay + 0.22f) + n * 0.3f + Vector3.up * top);
            }
            // ends: blank brick gables with the stair towers
            foreach (float ex in new[] { 0f, L })
            {
                Vector3 n = ex == 0f ? Vector3.left : Vector3.right;
                k.Quad(TownMats.deckBrick3, new Vector3(ex, 0, 0), new Vector3(ex, 0, D), new Vector3(ex, top, D), new Vector3(ex, top, 0), n);
                float tx = ex == 0f ? -5f : L; // the stair tower
                k.Box(TownMats.panel, new Vector3(tx, 0f, 2f), new Vector3(tx + 5f, top + 2.2f, 8f));
                for (int fl = 1; fl < storeys; fl++) Win(k, new Vector3(tx + (ex == 0f ? 0f : 5f), 0, 2f), Vector3.forward, n, 2f, 4f, fl * F + 0.9f, fl * F + 2.0f, Stair);
                k.Box(TownMats.glassDark, new Vector3(tx + 1.5f, 0f, 1.95f), new Vector3(tx + 3.5f, 2.4f, 2.0f));
            }
            k.Quad(E.felt, new Vector3(0, top + 0.02f, 0), new Vector3(L, top + 0.02f, 0), new Vector3(L, top + 0.02f, D), new Vector3(0, top + 0.02f, D), Vector3.up);
            Band(k, TownMats.panel, Vector3.zero, Vector3.right, Vector3.back, L, top, 0.6f, 0.1f);
            Band(k, TownMats.panel, new Vector3(L, 0, D), Vector3.left, Vector3.forward, L, top, 0.6f, 0.1f);
            var faces = new (Vector3, Vector3, Vector3, float)[] { (Vector3.zero, Vector3.right, Vector3.back, L), (new Vector3(L, 0, D), Vector3.left, Vector3.forward, L), (new Vector3(0, 0, D), Vector3.back, Vector3.left, D), (new Vector3(L, 0, 0), Vector3.forward, Vector3.right, D) };
            Graffiti(k, faces, rng, 2);
            rec = null;
            k.Build("Slab", g, false, ShadowCastingMode.On);
            var col = Coll(g, new Vector3(0, 0, 0), new Vector3(L, top, D));
            for (int u = 0; u * 12f + 11.9f <= L; u++) Close(g, name + " " + (u + 1), u * 12f, 12f, D, F, storeys, 4.85f, seed * 17 + u, wrec, col);
            foreach (float ex in new[] { -5f, L }) Coll(g, new Vector3(ex, 0, 2f), new Vector3(ex + 5f, top + 2.2f, 8f));
            return g;
        }

        // ================================================================== walk-up flats
        public static Transform WalkUp(Transform parent, string name, Vector3 origin, float yaw, int entrances, int seed, bool grey)
        {
            if (!Ready()) return null;
            var g = Root(parent, name, origin, yaw); var rng = new System.Random(seed);
            var k = new MeshKit(); const float D = 10f, F = 2.8f, per = 12f; float L = entrances * per, top = 3 * F;
            Material wall = TownMats.render;
            var wrec = new List<TowerInterior.WinRec>(); rec = wrec;
            foreach (var (o, r, n, len, front) in new[] { (Vector3.zero, Vector3.right, Vector3.back, L, true), (new Vector3(L, 0, D), Vector3.left, Vector3.forward, L, false) })
            {
                if (!front) Wall(k, wall, o, r, n, len, 0f, top);
                else
                {
                    // the front with a real doorway at each close (the interior hangs the door)
                    float x = 0f;
                    for (int e = 0; e < entrances; e++) { float c = e * per + per * 0.5f - 1.15f; Wall(k, wall, o + r * x, r, n, c - 0.55f - x, 0f, top); Wall(k, wall, o + r * (c - 0.55f), r, n, 1.1f, 2.2f, top); x = c + 0.55f; }
                    Wall(k, wall, o + r * x, r, n, len - x, 0f, top);
                }
                for (int e = 0; e < entrances; e++)
                {
                    float c = e * per + per * 0.5f;
                    for (int fl = 0; fl < 3; fl++)
                    {
                        float y = fl * F;
                        foreach (float wx in new[] { c - 4.6f, c - 2.9f, c + 1.9f, c + 3.6f }) Win(k, o, r, n, wx, wx + 1.2f, y + 0.9f, y + 2.2f, RandomWindow(rng), TownMats.panel);
                        if (front && fl > 0) Win(k, o, r, n, c - 0.5f, c + 0.5f, y + 1.2f, y + 2.3f, Stair, TownMats.panel); // the close's stair windows
                    }
                    if (front)
                    {
                        B(k, TownMats.panel, o + r * (c - 1.77f) + Vector3.up * 2.2f, o + r * (c - 0.53f) + n * 0.12f + Vector3.up * 2.3f);   // the door head
                        B(k, TownMats.panel, o + r * (c - 2.05f) + n * 0.0f + Vector3.up * 2.35f, o + r * (c - 0.25f) + n * 0.9f + Vector3.up * 2.5f);
                    }
                }
            }
            foreach (float ex in new[] { 0f, L }) { Vector3 n = ex == 0f ? Vector3.left : Vector3.right; k.Quad(wall, new Vector3(ex, 0, 0), new Vector3(ex, 0, D), new Vector3(ex, top, D), new Vector3(ex, top, 0), n); }
            // the hipped roof: two long slopes and two hip ends to a ridge
            float rise = 2.6f, inset = D * 0.5f; Vector3 r0 = new Vector3(inset, top + rise, D * 0.5f), r1 = new Vector3(L - inset, top + rise, D * 0.5f);
            float ov = 0.35f; Vector3 a = new Vector3(-ov, top, -ov), b = new Vector3(L + ov, top, -ov), c2 = new Vector3(L + ov, top, D + ov), d = new Vector3(-ov, top, D + ov);
            k.Quad(TownMats.roofTile, a, b, r1, r0, (Vector3.back + Vector3.up).normalized);
            k.Quad(TownMats.roofTile, c2, d, r0, r1, (Vector3.forward + Vector3.up).normalized);
            k.TriN(TownMats.roofTile, d, a, r0, (Vector3.left + Vector3.up).normalized);
            k.TriN(TownMats.roofTile, b, c2, r1, (Vector3.right + Vector3.up).normalized);
            k.Box(E.ridge, r0 + new Vector3(0, -0.05f, -0.1f), r1 + new Vector3(0, 0.08f, 0.1f));
            k.Box(E.white, new Vector3(-ov, top - 0.25f, -ov - 0.02f), new Vector3(L + ov, top, -ov + 0.1f)); // fascia
            for (int e = 0; e <= entrances; e++) { float cx = e * per + (e == 0 ? 2f : e == entrances ? -2f : 0f); k.Box(TownMats.panel, new Vector3(cx - 0.5f, top + 1f, D * 0.5f - 0.4f), new Vector3(cx + 0.5f, top + rise + 0.8f, D * 0.5f + 0.4f)); } // chimney stacks
            var faces = new (Vector3, Vector3, Vector3, float)[] { (Vector3.zero, Vector3.right, Vector3.back, L), (new Vector3(L, 0, D), Vector3.left, Vector3.forward, L) };
            Graffiti(k, faces, rng, seed % 3);
            rec = null;
            k.Build("Flats", g, false, ShadowCastingMode.On);
            var col = Coll(g, Vector3.zero, new Vector3(L, top, D));
            for (int e = 0; e < entrances; e++) Close(g, name + (entrances > 1 ? " " + (char)('A' + e) : ""), e * per, per, D, F, 3, per * 0.5f - 1.15f, seed * 13 + e, wrec, col);
            return g;
        }

        /// <summary>The inside of one stair (close) of a walk-up or a deck block: its own frame along the block, its windows shifted into it.</summary>
        static void Close(Transform g, string name, float x0, float per, float D, float F, int storeys, float cx, int seed, List<TowerInterior.WinRec> all, Collider col)
        {
            var mine = new List<TowerInterior.WinRec>();
            foreach (var w in all)
            {
                if (w.side != 'F' && w.side != 'B') continue;
                if (w.a1 <= x0 || w.a0 >= x0 + per) continue;
                var m = w; m.a0 = Mathf.Max(w.a0, x0) - x0; m.a1 = Mathf.Min(w.a1, x0 + per) - x0; mine.Add(m);
            }
            TowerInterior.Bake(g, new Vector3(x0, 0f, 0f), 0f, name, per, D, F, F, storeys, cx, 0.15f, cx - 0.55f, cx + 0.55f, 2.2f, 0f, 0f, seed, mine, new List<Collider> { col }, false, true);
        }

        // ================================================================== lock-ups, ball court, playground
        public static Transform Garages(Transform parent, string name, Vector3 origin, float yaw, int n, int seed)
        {
            if (!Ready()) return null;
            var g = Root(parent, name, origin, yaw); var rng = new System.Random(seed); var k = new MeshKit();
            const float w = 2.9f, D = 5.6f, h = 2.4f; float L = n * w;
            var mats = new Material[n]; for (int i = 0; i < n; i++) mats[i] = rng.NextDouble() < 0.12 ? TownMats.rust : E.garage[rng.Next(E.garage.Length)];
            var open = new HashSet<int>(); if (rng.NextDouble() < 0.7) open.Add(rng.Next(n));
            Lockup.Build(g, name, n, w, D, h, 2.05f, 0.25f, TownMats.panel, E.felt, i => mats[i], seed * 7 + 3, open);
            var faces = new (Vector3, Vector3, Vector3, float)[] { (new Vector3(L + 0.15f, 0, D), Vector3.left, Vector3.forward, L + 0.3f), (new Vector3(-0.15f, 0, D), Vector3.back, Vector3.left, D) };
            Graffiti(k, faces, rng, seed % 3);
            k.Build("Garages", g, false, ShadowCastingMode.On);
            return g;
        }

        /// <summary>A caged ball court: tarmac with lines, three-metre mesh fence on posts, goals with hoops above them, a gap to get in.</summary>
        public static Transform BallCourt(Transform parent, string name, Vector3 centre, float yaw)
        {
            if (!Ready()) return null;
            var g = Root(parent, name, centre, yaw); var k = new MeshKit(); var cage = new MeshKit();
            const float hx = 15f, hz = 9f, fh = 3.2f;
            k.Quad(TownMats.courtGreen, new Vector3(-hx, 0.04f, -hz), new Vector3(hx, 0.04f, -hz), new Vector3(hx, 0.04f, hz), new Vector3(-hx, 0.04f, hz), Vector3.up);
            void L(Vector3 a, Vector3 b, float w) { Vector3 d = (b - a).normalized, l = new Vector3(-d.z, 0, d.x) * w * 0.5f; k.Quad(E.line, a + l + Vector3.up * 0.045f, b + l + Vector3.up * 0.045f, b - l + Vector3.up * 0.045f, a - l + Vector3.up * 0.045f, Vector3.up); }
            L(new Vector3(-hx + 0.5f, 0, -hz + 0.5f), new Vector3(hx - 0.5f, 0, -hz + 0.5f), 0.08f); L(new Vector3(-hx + 0.5f, 0, hz - 0.5f), new Vector3(hx - 0.5f, 0, hz - 0.5f), 0.08f);
            L(new Vector3(-hx + 0.5f, 0, -hz + 0.5f), new Vector3(-hx + 0.5f, 0, hz - 0.5f), 0.08f); L(new Vector3(hx - 0.5f, 0, -hz + 0.5f), new Vector3(hx - 0.5f, 0, hz - 0.5f), 0.08f);
            L(new Vector3(0, 0, -hz + 0.5f), new Vector3(0, 0, hz - 0.5f), 0.08f);
            for (int i = 0; i < 24; i++) { float a0 = i * Mathf.PI * 2f / 24f, a1 = (i + 1) * Mathf.PI * 2f / 24f; L(new Vector3(Mathf.Cos(a0) * 2.5f, 0, Mathf.Sin(a0) * 2.5f), new Vector3(Mathf.Cos(a1) * 2.5f, 0, Mathf.Sin(a1) * 2.5f), 0.08f); }
            // the cage: posts, top rail, mesh panels (cut-out texture), a gap on one long side
            for (float x = -hx; x <= hx + 0.01f; x += 3f) foreach (float z in new[] { -hz, hz }) k.Box(E.railGreen, new Vector3(x - 0.05f, 0f, z - 0.05f), new Vector3(x + 0.05f, fh, z + 0.05f));
            for (float z = -hz; z <= hz + 0.01f; z += 3f) foreach (float x in new[] { -hx, hx }) k.Box(E.railGreen, new Vector3(x - 0.05f, 0f, z - 0.05f), new Vector3(x + 0.05f, fh, z + 0.05f));
            void Panel(Vector3 a, Vector3 b) { cage.QuadUV(TownMats.chainlink, a, b, b + Vector3.up * fh, a + Vector3.up * fh, Vector3.Cross(b - a, Vector3.up), new Vector2(0, 0), new Vector2((b - a).magnitude / 2f, 0), new Vector2((b - a).magnitude / 2f, fh / 2f), new Vector2(0, fh / 2f)); }
            Panel(new Vector3(-hx, 0, hz), new Vector3(hx, 0, hz)); Panel(new Vector3(-hx, 0, -hz), new Vector3(-2f, 0, -hz)); Panel(new Vector3(2f, 0, -hz), new Vector3(hx, 0, -hz));
            Panel(new Vector3(-hx, 0, -hz), new Vector3(-hx, 0, hz)); Panel(new Vector3(hx, 0, -hz), new Vector3(hx, 0, hz));
            // goals at each end with a hoop and backboard above
            foreach (int s in new[] { -1, 1 })
            {
                float x = s * (hx - 0.1f);
                k.Box(E.white, new Vector3(x - 0.05f, 0f, -1.6f), new Vector3(x + 0.05f, 2.1f, -1.5f)); k.Box(E.white, new Vector3(x - 0.05f, 0f, 1.5f), new Vector3(x + 0.05f, 2.1f, 1.6f));
                k.Box(E.white, new Vector3(x - 0.05f, 2.0f, -1.6f), new Vector3(x + 0.05f, 2.1f, 1.6f));
                k.Box(E.white, new Vector3(x - 0.04f, 2.6f, -0.9f), new Vector3(x + 0.04f, 3.5f, 0.9f));
                for (int i = 0; i < 12; i++) { float a0 = i * Mathf.PI / 6f; Vector3 rc = new Vector3(x - s * 0.45f, 3.05f, 0f); k.Box(E.playRed, rc + new Vector3(Mathf.Cos(a0) * 0.23f - 0.02f, -0.01f, Mathf.Sin(a0) * 0.23f - 0.02f), rc + new Vector3(Mathf.Cos(a0) * 0.23f + 0.02f, 0.01f, Mathf.Sin(a0) * 0.23f + 0.02f)); }
            }
            k.Build("Court", g, true, ShadowCastingMode.Off);
            cage.Build("Cage", g, false, ShadowCastingMode.Off);
            foreach (var (mn, mx) in new[] { (new Vector3(-hx, 0, hz - 0.05f), new Vector3(hx, fh, hz + 0.05f)), (new Vector3(-hx, 0, -hz - 0.05f), new Vector3(-2f, fh, -hz + 0.05f)), (new Vector3(2f, 0, -hz - 0.05f), new Vector3(hx, fh, -hz + 0.05f)), (new Vector3(-hx - 0.05f, 0, -hz), new Vector3(-hx + 0.05f, fh, hz)), (new Vector3(hx - 0.05f, 0, -hz), new Vector3(hx + 0.05f, fh, hz)) })
                Coll(g, mn, mx);
            return g;
        }

        public static Transform Playground(Transform parent, string name, Vector3 centre, float yaw)
        {
            if (!Ready()) return null;
            var g = Root(parent, name, centre, yaw); var k = new MeshKit();
            const float hx = 8f, hz = 6f;
            k.Quad(TownMats.playSoft, new Vector3(-hx, 0.04f, -hz), new Vector3(hx, 0.04f, -hz), new Vector3(hx, 0.04f, hz), new Vector3(-hx, 0.04f, hz), Vector3.up);
            // swings
            foreach (float x in new[] { -6.5f, -3.5f }) { k.Box(E.playRed, new Vector3(x - 0.06f, 0f, -1.2f), new Vector3(x + 0.06f, 2.5f, -1.1f)); k.Box(E.playRed, new Vector3(x - 0.06f, 0f, 1.1f), new Vector3(x + 0.06f, 2.5f, 1.2f)); }
            k.Box(E.playRed, new Vector3(-6.6f, 2.45f, -1.2f), new Vector3(-3.4f, 2.55f, 1.2f));
            foreach (float x in new[] { -5.8f, -4.2f }) { k.Box(TownMats.steel, new Vector3(x - 0.01f, 0.5f, -0.25f), new Vector3(x + 0.01f, 2.45f, -0.23f)); k.Box(TownMats.steel, new Vector3(x - 0.01f, 0.5f, 0.23f), new Vector3(x + 0.01f, 2.45f, 0.25f)); k.Box(E.black, new Vector3(x - 0.22f, 0.45f, -0.27f), new Vector3(x + 0.22f, 0.5f, 0.27f)); }
            // slide: steps, platform, chute
            k.Box(E.playBlue, new Vector3(1f, 0f, -0.6f), new Vector3(2.2f, 1.6f, 0.6f));
            for (int i = 0; i < 5; i++) k.Box(E.playYellow, new Vector3(-0.2f + i * 0.24f, i * 0.32f, -0.4f), new Vector3(0.05f + i * 0.24f, i * 0.32f + 0.05f, 0.4f));
            k.Quad(TownMats.steel, new Vector3(2.2f, 1.6f, -0.35f), new Vector3(2.2f, 1.6f, 0.35f), new Vector3(5f, 0.35f, 0.35f), new Vector3(5f, 0.35f, -0.35f), (Vector3.up + Vector3.right * 0.45f).normalized);
            // roundabout and a springy rider
            k.Cyl(E.playYellow, new Vector3(4.5f, 0f, 3.5f), 1.3f, 0.25f, 16, true); k.Cyl(E.playRed, new Vector3(4.5f, 0.25f, 3.5f), 0.08f, 0.8f, 8, true);
            k.Cyl(E.black, new Vector3(-2f, 0f, 3.5f), 0.12f, 0.45f, 8, false); k.Box(E.playBlue, new Vector3(-2.3f, 0.45f, 3.2f), new Vector3(-1.7f, 0.9f, 3.8f));
            // fence and gate
            var rail = new MeshKit();
            void Rail(Vector3 a, Vector3 b) { float l = (b - a).magnitude; rail.QuadUV(E.railing, a, b, b + Vector3.up * 1.0f, a + Vector3.up * 1.0f, Vector3.Cross(b - a, Vector3.up), new Vector2(0, 0), new Vector2(l / 2f, 0), new Vector2(l / 2f, 1f), new Vector2(0, 1f)); }
            Rail(new Vector3(-hx, 0, hz), new Vector3(hx, 0, hz)); Rail(new Vector3(-hx, 0, -hz), new Vector3(-0.7f, 0, -hz)); Rail(new Vector3(0.7f, 0, -hz), new Vector3(hx, 0, -hz));
            Rail(new Vector3(-hx, 0, -hz), new Vector3(-hx, 0, hz)); Rail(new Vector3(hx, 0, -hz), new Vector3(hx, 0, hz));
            for (float x = -hx; x <= hx + 0.01f; x += 2f) foreach (float z in new[] { -hz, hz }) k.Box(E.railGreen, new Vector3(x - 0.04f, 0f, z - 0.04f), new Vector3(x + 0.04f, 1.05f, z + 0.04f));
            k.Build("Playground", g, true, ShadowCastingMode.Off);
            rail.Build("Railings", g, false, ShadowCastingMode.Off);
            return g;
        }
    }
}

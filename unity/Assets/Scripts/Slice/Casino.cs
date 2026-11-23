using System.Collections.Generic;
using UnityEngine;
using ASBO.Casino;

namespace ASBO.Slice
{
    /// <summary>
    /// The Silver Sixpence, a working out-of-town casino where the main road heads east. Outside: grey cladding on a
    /// black plinth, the lit sign, a deep canopy on white columns, red carpet and brass posts, the car park and the
    /// tall sign by the road. Inside, packed: a hall of slot machines in rows and islands with stools, the cashier's
    /// cage, a table-games pit under crystal chandeliers (two roulette wheels, three blackjack tables, a poker table and
    /// an electronic roulette with eight seats), and a proper working bar: marble counter on walnut, brass foot rail,
    /// stools, beer fonts, optics, a mirrored back bar with lit shelves of bottles, fridges, a coffee machine, glass
    /// washer, tills, beer mats, trays and drinks on the go, plus booths and tub chairs in the lounge. Behind the staff
    /// door: the cash cage, the secure cash room behind a round vault door, the manager's office (CCTV wall, the PC with
    /// the safe code stuck to it), the staff room (lockers, kitchenette, sofa, vending machine) and the stock room off
    /// the bar (kegs, gas, crates, shelving). Nearly everything can be picked up, sat on, opened or used (PropWorld).
    /// All the props are modelled (tools/art/casino) and baked into a few meshes per zone. The name is made up.
    /// </summary>
    public static class CasinoHall
    {
        public const string RootName = "Casino";
        /// <summary>The building's front-left (south-west) corner; it runs W along +x and D back along +z, front to the road.</summary>
        public static readonly Vector3 Origin = new Vector3(162f, 0f, -2f);
        public const float W = 52f, D = 22f, H = 6.6f, CeilY = 4.2f, BohY = 2.8f, DoorX0 = 24f, DoorX1 = 28f, DoorH = 3f;
        /// <summary>The ground levelled for it (x, z): building and car park.</summary>
        public static readonly Rect Plot = new Rect(156f, -24.5f, 64f, 47f);
        public static readonly Vector3 PylonAt = new Vector3(159.5f, 0f, -21.6f);

        public struct Station { public CasinoUI.Game game; public Vector3 stand; public float yaw; public int seed; public string label; }
        /// <summary>The cashier's windows: where you stand to buy or cash in chips (everything else is used through PropWorld).</summary>
        public static List<Station> Stations()
        {
            var l = new List<Station>();
            foreach (float x in new[] { 2.5f, 4.8f, 7.1f }) l.Add(new Station { game = CasinoUI.Game.Cage, stand = Origin + new Vector3(x, 0f, 11.35f), yaw = 0f, label = "Cashier: buy or cash chips" });
            return l;
        }

        class Mats { public Material carpet, wall, sign, pylon, felt, wood, gold, clad, navy, glass, ceiling, light, cab, chrome, black, white, line, asphalt, kerb, felt2, plaster, grille, shutter; }
        static Mats M;
        static Material C(string n) => Resources.Load<Material>("Casino/" + n);
        static Material E(string n) => Resources.Load<Material>("Estate/" + n);
        static readonly Dictionary<Material, Material> lit = new Dictionary<Material, Material>();
        /// <summary>
        /// The indoor copy of a material: it also glows a little with its own colour, a stand-in for light bouncing round
        /// a lit room (phones light each mesh with only a couple of lamps, and at night the outdoor ambient is dark).
        /// Saved as an asset so the built scene keeps it.
        /// </summary>
        static Material In(Material m, float glow = 0.3f)
        {
            if (m == null || m.IsKeywordEnabled("_EMISSION")) return m;
            if (lit.TryGetValue(m, out var c) && c != null) return c;
#if UNITY_EDITOR
            const string dir = "Assets/Art/Generated/CasinoMats"; System.IO.Directory.CreateDirectory(dir);
            string path = $"{dir}/{m.name}_in.mat";
            c = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            if (c == null) { c = new Material(m) { name = m.name + "_in" }; UnityEditor.AssetDatabase.CreateAsset(c, path); }
            else { c.shader = m.shader; c.CopyPropertiesFromMaterial(m); }
            if (m.HasProperty("_BaseMap")) c.SetTexture("_EmissionMap", m.GetTexture("_BaseMap"));
            var bc = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
            c.SetColor("_EmissionColor", new Color(bc.r * glow, bc.g * glow, bc.b * glow, 1f)); c.EnableKeyword("_EMISSION");
            c.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; UnityEditor.EditorUtility.SetDirty(c);
#else
            c = m;
#endif
            lit[m] = c; MeshKit.Tile[c] = MeshKit.TileOf(m); return c;
        }
        static Material S(string n, Material fallback, float size)
        {
            var m = Surfaces.Source(n); if (m == null) return fallback;
            MeshKit.Tile[m] = size; return m;
        }

        public static Transform Build(Transform parent)
        {
            M = new Mats
            {
                carpet = C("CasinoCarpet"), wall = C("CasinoWall"), sign = C("CasinoSign"), pylon = C("CasinoPylon"), felt = C("CasinoFelt"), wood = C("CasinoWood"), gold = C("CasinoGold"),
                clad = C("CasinoClad"), navy = C("CasinoNavy"), glass = C("CasinoGlass"), ceiling = C("CasinoCeiling"), light = C("CasinoLight"), cab = C("CasinoCabinet"), chrome = C("CasinoChrome"),
                black = E("EstateBlack"), white = E("EstateWhite"), line = E("EstateLine"), kerb = E("EstateKerb"), felt2 = E("EstateFelt"), asphalt = Surfaces.Source("asphalt_02"),
            };
            if (M.carpet == null || M.clad == null) { Debug.LogWarning("[ASBO] casino: materials missing"); return null; }
            MeshKit.Tile[M.carpet] = 2f; MeshKit.Tile[M.wall] = 1.5f; MeshKit.Tile[M.clad] = 2f;
            M.plaster = M.white; M.grille = Surfaces.Source("grille_clover"); M.shutter = S("shutter_roller", M.chrome, 1.5f);
            PropLib.Flush();
            var root = new GameObject(RootName).transform; root.SetParent(parent, false); root.position = Origin;
            Shell(root); CarPark(root);
            var outdoor = M; M = Indoors(M); Interior(root); M = outdoor;
            if (PropLib.I != null) Dress(root); else Debug.LogWarning("[ASBO] casino: no prop library (run ASBO/Art/Build Casino Props)");
            Lights(root);
            return root;
        }

        // ------------------------------------------------------------------------------------------ the building
        static void Shell(Transform root)
        {
            var k = new MeshKit(); const float t = 0.3f, plinth = 1.0f;
            void Outer(Vector3 a, Vector3 b, Vector3 n, float y0, float y1, Material m)
            { k.Quad(m, new Vector3(a.x, y0, a.z), new Vector3(b.x, y0, b.z), new Vector3(b.x, y1, b.z), new Vector3(a.x, y1, a.z), n); }
            void Wall(Vector3 a, Vector3 b, Vector3 n, bool front)
            {
                Outer(a, b, n, 0f, plinth, M.black); Outer(a, b, n, plinth, front ? 4.4f : H, front ? M.glass : M.clad);
                if (front)
                {
                    k.Quad(M.black, new Vector3(a.x, plinth, a.z + 0.04f), new Vector3(b.x, plinth, b.z + 0.04f), new Vector3(b.x, 4.4f, b.z + 0.04f), new Vector3(a.x, 4.4f, a.z + 0.04f), n);
                    for (float fx = Mathf.Ceil(Mathf.Min(a.x, b.x) / 2.5f) * 2.5f; fx < Mathf.Max(a.x, b.x); fx += 2.5f) k.Box(M.gold, new Vector3(fx - 0.06f, plinth, a.z - 0.16f), new Vector3(fx + 0.06f, 4.35f, a.z));
                    Outer(a, b, n, 4.4f, H, M.navy);
                }
            }
            Wall(new Vector3(-t, 0, -t), new Vector3(DoorX0, 0, -t), Vector3.back, true);
            Wall(new Vector3(DoorX1, 0, -t), new Vector3(W + t, 0, -t), Vector3.back, true);
            Outer(new Vector3(DoorX0, 0, -t), new Vector3(DoorX1, 0, -t), Vector3.back, DoorH, 4.4f, M.clad); Outer(new Vector3(DoorX0, 0, -t), new Vector3(DoorX1, 0, -t), Vector3.back, 4.4f, H, M.navy);
            Wall(new Vector3(-t, 0, D + t), new Vector3(W + t, 0, D + t), Vector3.forward, false);
            Wall(new Vector3(-t, 0, -t), new Vector3(-t, 0, D + t), Vector3.left, false);
            Wall(new Vector3(W + t, 0, -t), new Vector3(W + t, 0, D + t), Vector3.right, false);
            for (float z = 2f; z < D; z += 2f) { k.Box(M.chrome, new Vector3(-t - 0.08f, plinth, z - 0.05f), new Vector3(-t, H, z + 0.05f)); k.Box(M.chrome, new Vector3(W + t, plinth, z - 0.05f), new Vector3(W + t + 0.08f, H, z + 0.05f)); }
            for (float x = 2f; x < W; x += 2f) if (x < 43f || x > 47f) k.Box(M.chrome, new Vector3(x - 0.05f, plinth, D + t), new Vector3(x + 0.05f, H, D + t + 0.08f));
            // the delivery shutter at the back of the stock room, and the fire exit
            k.Quad(M.shutter, new Vector3(43.5f, 0f, D + t + 0.02f), new Vector3(46.5f, 0f, D + t + 0.02f), new Vector3(46.5f, 3f, D + t + 0.02f), new Vector3(43.5f, 3f, D + t + 0.02f), Vector3.forward);
            k.Box(M.chrome, new Vector3(43.3f, 3f, D + t), new Vector3(46.7f, 3.4f, D + t + 0.35f));
            k.Quad(M.black, new Vector3(9.5f, 0f, D + t + 0.02f), new Vector3(10.5f, 0f, D + t + 0.02f), new Vector3(10.5f, 2.1f, D + t + 0.02f), new Vector3(9.5f, 2.1f, D + t + 0.02f), Vector3.forward);
            k.QuadUV(M.sign, new Vector3(W * 0.5f - 13f, 4.55f, -t - 0.06f), new Vector3(W * 0.5f + 13f, 4.55f, -t - 0.06f), new Vector3(W * 0.5f + 13f, 6.45f, -t - 0.06f), new Vector3(W * 0.5f - 13f, 6.45f, -t - 0.06f), Vector3.back,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
            k.Box(M.gold, new Vector3(-t - 0.1f, 4.35f, -t - 0.12f), new Vector3(W + t + 0.1f, 4.45f, -t));
            k.Box(M.gold, new Vector3(-t - 0.1f, H - 0.02f, -t - 0.12f), new Vector3(W + t + 0.1f, H + 0.1f, -t));
            k.Box(M.clad, new Vector3(-t, H, -t), new Vector3(W + t, H + 0.5f, 0f)); k.Box(M.clad, new Vector3(-t, H, D), new Vector3(W + t, H + 0.5f, D + t));
            k.Box(M.clad, new Vector3(-t, H, 0f), new Vector3(0f, H + 0.5f, D)); k.Box(M.clad, new Vector3(W, H, 0f), new Vector3(W + t, H + 0.5f, D));
            k.Quad(M.felt2, new Vector3(0, H, 0), new Vector3(W, H, 0), new Vector3(W, H, D), new Vector3(0, H, D), Vector3.up);
            k.Box(M.chrome, new Vector3(8f, H, 12f), new Vector3(11f, H + 1.2f, 14f)); k.Box(M.chrome, new Vector3(30f, H, 6f), new Vector3(32f, H + 0.9f, 8f)); k.Box(M.chrome, new Vector3(44f, H, 16f), new Vector3(47f, H + 1.1f, 18f));
            k.Quad(M.gold, new Vector3(DoorX0, 0, -t), new Vector3(DoorX0, DoorH, -t), new Vector3(DoorX0, DoorH, 0), new Vector3(DoorX0, 0, 0), Vector3.right);
            k.Quad(M.gold, new Vector3(DoorX1, 0, -t), new Vector3(DoorX1, DoorH, -t), new Vector3(DoorX1, DoorH, 0), new Vector3(DoorX1, 0, 0), Vector3.left);
            k.Quad(M.gold, new Vector3(DoorX0, DoorH, -t), new Vector3(DoorX1, DoorH, -t), new Vector3(DoorX1, DoorH, 0), new Vector3(DoorX0, DoorH, 0), Vector3.down);
            foreach (float gx in new[] { DoorX0 - 1.0f, DoorX1 + 0.05f }) { k.Box(M.glass, new Vector3(gx, 0.02f, -t - 0.06f), new Vector3(gx + 0.95f, DoorH - 0.05f, -t - 0.02f)); k.Box(M.gold, new Vector3(gx, 0f, -t - 0.08f), new Vector3(gx + 0.95f, 0.08f, -t)); }
            Grand(k);
            var g = k.Build("Shell", root, false);
            void Solid(Vector3 mn, Vector3 mx) { var bc = g.AddComponent<BoxCollider>(); bc.center = (mn + mx) * 0.5f; bc.size = mx - mn; }
            Solid(new Vector3(-t, 0, -t), new Vector3(DoorX0, H, 0)); Solid(new Vector3(DoorX1, 0, -t), new Vector3(W + t, H, 0)); Solid(new Vector3(DoorX0, DoorH, -t), new Vector3(DoorX1, H, 0));
            Solid(new Vector3(-t, 0, D), new Vector3(9.5f, H, D + t)); Solid(new Vector3(10.5f, 0, D), new Vector3(W + t, H, D + t)); Solid(new Vector3(9.5f, 2.1f, D), new Vector3(10.5f, H, D + t));
            Solid(new Vector3(-t, 0, 0), new Vector3(0, H, D)); Solid(new Vector3(W, 0, 0), new Vector3(W + t, H, D));
            Solid(new Vector3(-t, H, -t), new Vector3(W + t, H + 0.5f, D + t));
            foreach (float cx in new[] { CanX0 + 0.5f, CanX1 - 0.5f }) Solid(new Vector3(cx - 0.4f, 0, CanZ0 + 0.1f), new Vector3(cx + 0.4f, CanY, CanZ0 + 0.9f));
            foreach (float px in new[] { DoorX0 - 1.7f, DoorX1 + 1.7f }) Solid(new Vector3(px - 0.4f, 0f, -1.6f), new Vector3(px + 0.4f, 1.2f, -0.8f));
            GameAudio.RegisterZone(root, new List<CRect> { new CRect(0f, W, 0f, D) }, CeilY + 0.3f);
        }

        const float CanX0 = DoorX0 - 5f, CanX1 = DoorX1 + 5f, CanZ0 = -9f, CanY = 4.6f;
        static void Grand(MeshKit k)
        {
            const float t = 0.3f;
            k.Box(M.navy, new Vector3(CanX0, CanY, CanZ0), new Vector3(CanX1, CanY + 0.75f, -t));
            k.Box(M.gold, new Vector3(CanX0 - 0.05f, CanY - 0.06f, CanZ0 - 0.05f), new Vector3(CanX1 + 0.05f, CanY + 0.02f, -t));
            k.Box(M.gold, new Vector3(CanX0 - 0.06f, CanY + 0.75f, CanZ0 - 0.06f), new Vector3(CanX1 + 0.06f, CanY + 0.85f, -t));
            k.Box(M.light, new Vector3(CanX0 + 0.2f, CanY - 0.09f, CanZ0 + 0.2f), new Vector3(CanX1 - 0.2f, CanY - 0.06f, CanZ0 + 0.35f));
            for (float x = CanX0 + 1.2f; x < CanX1 - 0.5f; x += 1.6f) for (float z = CanZ0 + 1.2f; z < -1f; z += 1.8f) k.Cyl(M.light, new Vector3(x, CanY - 0.07f, z), 0.12f, 0.02f, 10);
            k.QuadUV(M.sign, new Vector3(CanX0 + 1f, CanY + 0.08f, CanZ0 - 0.07f), new Vector3(CanX1 - 1f, CanY + 0.08f, CanZ0 - 0.07f), new Vector3(CanX1 - 1f, CanY + 0.72f, CanZ0 - 0.07f), new Vector3(CanX0 + 1f, CanY + 0.72f, CanZ0 - 0.07f), Vector3.back,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
            foreach (float cx in new[] { CanX0 + 0.5f, CanX1 - 0.5f })
            {
                var c = new Vector3(cx, 0f, CanZ0 + 0.5f);
                k.Box(M.gold, c + new Vector3(-0.38f, 0f, -0.38f), c + new Vector3(0.38f, 0.3f, 0.38f)); k.Cyl(M.white, c + Vector3.up * 0.3f, 0.26f, CanY - 0.75f, 16);
                for (int f = 0; f < 12; f++) { float a = f * Mathf.PI / 6f; var fp = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.265f; k.Box(M.chrome, fp + new Vector3(-0.012f, 0.4f, -0.012f), fp + new Vector3(0.012f, CanY - 0.55f, 0.012f)); }
                k.Cyl(M.gold, c + Vector3.up * (CanY - 0.45f), 0.3f, 0.12f, 16, true, 0.36f); k.Box(M.gold, c + new Vector3(-0.4f, CanY - 0.33f, -0.4f), c + new Vector3(0.4f, CanY, 0.4f));
            }
            k.Box(M.white, new Vector3(DoorX0 - 4f, 0f, -4.2f), new Vector3(DoorX1 + 4f, 0.07f, -t));
            k.Box(M.cab, new Vector3(DoorX0 + 0.4f, 0.07f, -8.6f), new Vector3(DoorX1 - 0.4f, 0.09f, 1.0f));
            for (int i = 0; i < 6; i++)
                foreach (float x in new[] { DoorX0 + 0.1f, DoorX1 - 0.1f })
                {
                    var p = new Vector3(x, 0.07f, -7.8f + i * 1.25f);
                    k.Cyl(M.gold, p, 0.16f, 0.05f, 10); k.Cyl(M.gold, p, 0.03f, 0.95f, 8); k.Sphere(M.gold, p + Vector3.up * 1f, Vector3.one * 0.06f, 8, 4);
                    if (i < 5) { var q = p + new Vector3(0, 0.85f, 0.62f); k.Box(M.cab, q + new Vector3(-0.025f, -0.07f, -0.58f), q + new Vector3(0.025f, 0.0f, 0.58f)); }
                }
            foreach (float px in new[] { DoorX0 - 1.7f, DoorX1 + 1.7f })
            {
                var p = new Vector3(px, 0.07f, -1.2f);
                k.Cyl(M.gold, p, 0.38f, 0.6f, 12, true, 0.3f); k.Cyl(M.wood, p + Vector3.up * 0.6f, 0.04f, 1.2f, 6);
                k.Sphere(M.felt, p + Vector3.up * 1.9f, Vector3.one * 0.45f, 10, 6); k.Sphere(M.felt, p + Vector3.up * 1.2f, Vector3.one * 0.3f, 10, 6);
            }
            for (float x = 1.25f; x < W; x += 2.5f) if (x < CanX0 || x > CanX1) k.Box(M.light, new Vector3(x - 0.15f, 0f, -t - 0.35f), new Vector3(x + 0.15f, 0.08f, -t - 0.05f));
        }

        // ------------------------------------------------------------------------------------------ outside
        static void CarPark(Transform root)
        {
            var k = new MeshKit(); Vector3 L(float wx, float wz) => new Vector3(wx, 0f, wz) - Origin;
            var a = L(Plot.xMin, -24.45f); var b = L(Plot.xMax, Origin.z - 0.31f);
            k.Quad(M.asphalt, new Vector3(a.x, 0.03f, a.z), new Vector3(b.x, 0.03f, a.z), new Vector3(b.x, 0.03f, b.z), new Vector3(a.x, 0.03f, b.z), Vector3.up);
            float bx0 = a.x + 1.5f, bx1 = b.x - 1.5f;
            for (int row = 0; row < 2; row++)
            {
                float z0 = row == 0 ? -6.2f : -18f, z1 = z0 + 5f;
                for (float x = bx0; x <= bx1; x += 2.6f)
                {
                    if (row == 0 && x > DoorX0 - 5.5f && x < DoorX1 + 5.5f) continue;
                    k.Box(M.line, new Vector3(x - 0.05f, 0.03f, z0), new Vector3(x + 0.05f, 0.04f, z1));
                }
                k.Box(M.line, new Vector3(bx0, 0.03f, row == 0 ? z0 - 0.05f : z1 - 0.05f), new Vector3(bx1, 0.04f, row == 0 ? z0 + 0.05f : z1 + 0.05f));
            }
            k.Box(M.kerb, new Vector3(a.x + 1f, 0f, -12.4f), new Vector3(a.x + 8f, 0.15f, -11.6f));
            foreach (float lx in new[] { a.x + 4.5f, W * 0.5f, b.x - 4f })
            {
                var p = new Vector3(lx, 0f, -12f); k.Cyl(M.chrome, p, 0.1f, 6.5f, 8);
                k.Box(M.chrome, new Vector3(lx - 0.3f, 6.4f, -12.3f), new Vector3(lx + 0.3f, 6.6f, -11.7f)); k.Box(M.light, new Vector3(lx - 0.26f, 6.36f, -12.26f), new Vector3(lx + 0.26f, 6.4f, -11.74f));
            }
            var s = PylonAt - Origin;
            k.Box(M.chrome, new Vector3(s.x - 0.2f, 0f, s.z - 0.2f), new Vector3(s.x + 0.2f, 3f, s.z + 0.2f));
            k.Box(M.navy, new Vector3(s.x - 1.1f, 3f, s.z - 0.3f), new Vector3(s.x + 1.1f, 7.6f, s.z + 0.3f));
            foreach (int side in new[] { -1, 1 })
            {
                float z = s.z + side * 0.31f;
                var p0 = new Vector3(s.x - 1f, 3.1f, z); var p1 = new Vector3(s.x + 1f, 3.1f, z); var p2 = new Vector3(s.x + 1f, 7.5f, z); var p3 = new Vector3(s.x - 1f, 7.5f, z);
                bool south = side < 0;
                k.QuadUV(M.pylon, p0, p1, p2, p3, side < 0 ? Vector3.back : Vector3.forward, south ? new Vector2(0, 0) : new Vector2(1, 0), south ? new Vector2(1, 0) : new Vector2(0, 0), south ? new Vector2(1, 1) : new Vector2(0, 1), south ? new Vector2(0, 1) : new Vector2(1, 1));
            }
            var g = k.Build("CarPark", root, false);
            var col = g.AddComponent<BoxCollider>(); col.center = new Vector3(s.x, 5.3f, s.z); col.size = new Vector3(2.2f, 4.6f, 0.6f);
            var c2 = g.AddComponent<BoxCollider>(); c2.center = new Vector3(s.x, 1.5f, s.z); c2.size = new Vector3(0.4f, 3f, 0.4f);
        }

        static Mats Indoors(Mats o) => new Mats
        {
            carpet = In(o.carpet), wall = In(o.wall), sign = o.sign, pylon = o.pylon, felt = In(o.felt), wood = In(o.wood), gold = In(o.gold, 0.12f), clad = o.clad, navy = In(o.navy),
            glass = o.glass, ceiling = In(o.ceiling), light = o.light, cab = In(o.cab), chrome = In(o.chrome, 0.12f), black = In(o.black), white = In(o.white, 0.1f), line = o.line,
            asphalt = o.asphalt, kerb = o.kerb, felt2 = o.felt2, plaster = In(o.plaster, 0.1f), grille = o.grille, shutter = In(o.shutter),
        };

        // ------------------------------------------------------------------------------------------ inside: floors, walls, ceilings, rooms
        static void Interior(Transform root)
        {
            var k = new MeshKit(); var cols = new List<(Vector3 mn, Vector3 mx)>();
            Material carpet = In(S("carpet_hex", M.carpet, 1.6f)), parquet = In(S("parquet_diamond", M.wood, 2f)), rubber = In(S("rubber_mat", M.black, 0.6f));
            Material conc = In(S("concrete_floor", M.white, 3f)), concD = In(S("concrete_panels", M.black, 3f)), staffFloor = In(S("parquet_herring", M.wood, 1.5f)), office = In(S("carpet_office", M.carpet, 1f));
            Material slats = In(S("wood_slats", M.wood, 2f)), tilesW = In(S("tiles_metro_black", M.black, 1f)), panel = In(S("parquet_cherry", M.wood, 2f));
            void Floor(float x0, float z0, float x1, float z1, Material m, float y = 0.01f) => k.Quad(m, new Vector3(x0, y, z0), new Vector3(x1, y, z0), new Vector3(x1, y, z1), new Vector3(x0, y, z1), Vector3.up);
            void Ceil(float x0, float z0, float x1, float z1, float y, Material m) => k.Quad(m, new Vector3(x0, y, z0), new Vector3(x1, y, z0), new Vector3(x1, y, z1), new Vector3(x0, y, z1), Vector3.down);
            // floors: carpet on the gaming floor, parquet in the bar, rubber behind it; concrete, tiles and office carpet out back
            Floor(0f, 0f, 37.5f, 12f, carpet); Floor(20f, 12f, 37.5f, 15f, carpet); Floor(20f, 15f, 39f, 22f, carpet);
            Floor(DoorX0, -0.3f, DoorX1, 0f, carpet);
            Floor(37.5f, 0f, 52f, 15f, parquet); Floor(50.15f, 0.3f, 51.4f, 14.9f, rubber, 0.015f);
            Floor(0f, 12f, 9f, 22f, concD); Floor(9f, 12f, 11f, 22f, conc); Floor(11f, 12f, 20f, 17f, staffFloor); Floor(11f, 17f, 20f, 22f, office); Floor(39f, 15f, 52f, 22f, conc);
            // the hall ceiling: black with gold coffers and lit panels; the back rooms: low white tiles
            Ceil(0f, 0f, 20f, 12f, CeilY, M.ceiling); Ceil(20f, 0f, 39f, 22f, CeilY, M.ceiling); Ceil(39f, 0f, 52f, 15f, CeilY, M.ceiling);
            for (float x = 3f; x < 38f; x += 5f) for (float z = 3f; z < 21.5f; z += 4.5f)
            {
                if (x < 20f && z > 11.5f) continue;
                k.Box(M.gold, new Vector3(x - 1.25f, CeilY - 0.08f, z - 1.25f), new Vector3(x + 1.25f, CeilY, z + 1.25f));
                k.Box(M.ceiling, new Vector3(x - 1.1f, CeilY - 0.081f, z - 1.1f), new Vector3(x + 1.1f, CeilY - 0.079f, z + 1.1f), 8);
                for (int q = 0; q < 4; q++) k.Cyl(M.light, new Vector3(x + (q % 2 == 0 ? -0.6f : 0.6f), CeilY - 0.1f, z + (q < 2 ? -0.6f : 0.6f)), 0.07f, 0.02f, 8);
            }
            for (float z = 2f; z < 14f; z += 2.2f) k.Cyl(M.light, new Vector3(45f, CeilY - 0.03f, z), 0.08f, 0.02f, 8);
            Ceil(0f, 12f, 20f, 22f, BohY, M.plaster); Ceil(39f, 15f, 52f, 22f, BohY, M.plaster);
            for (float x = 0.6f; x < 20f; x += 0.6f) k.Box(M.chrome, new Vector3(x - 0.01f, BohY - 0.012f, 12f), new Vector3(x + 0.01f, BohY - 0.002f, 22f));
            for (float z = 12.6f; z < 22f; z += 1.2f) k.Box(M.chrome, new Vector3(0f, BohY - 0.012f, z - 0.01f), new Vector3(20f, BohY - 0.002f, z + 0.01f));
            for (float x = 39.6f; x < 52f; x += 0.6f) k.Box(M.chrome, new Vector3(x - 0.01f, BohY - 0.012f, 15f), new Vector3(x + 0.01f, BohY - 0.002f, 22f));
            foreach (var (x, z) in new[] { (4.5f, 13.5f), (4.5f, 18.5f), (10f, 15f), (10f, 20f), (15.5f, 14.5f), (15.5f, 19.5f), (43f, 18.5f), (48f, 18.5f) })
                k.Box(M.light, new Vector3(x - 0.55f, BohY - 0.03f, z - 0.25f), new Vector3(x + 0.55f, BohY - 0.005f, z + 0.25f));

            // the hall's own walls (inside faces of the shell): walnut-look panelling to 1.2 m under a brass rail, flock above
            void Hall(Vector3 a, Vector3 b, Vector3 n)
            {
                Vector3 o = n * 0.04f;
                k.Quad(panel, a + o, b + o, b + o + Vector3.up * 1.2f, a + o + Vector3.up * 1.2f, n);
                k.Box(M.gold, Vector3.Min(a, b + o * 1.6f) + Vector3.up * 1.18f, Vector3.Max(a, b + o * 1.6f) + Vector3.up * 1.24f);
                k.Box(M.wood, Vector3.Min(a, b + o * 1.5f), Vector3.Max(a, b + o * 1.5f) + Vector3.up * 0.14f);
                k.Quad(M.wall, a + Vector3.up * 1.2f, b + Vector3.up * 1.2f, b + Vector3.up * CeilY, a + Vector3.up * CeilY, n);
                k.Box(M.gold, Vector3.Min(a, b + o * 3f) + Vector3.up * (CeilY - 0.2f), Vector3.Max(a, b + o * 3f) + Vector3.up * CeilY);
            }
            void Plain(Vector3 a, Vector3 b, Vector3 n, float h, Material m) { k.Quad(m, a, b, b + Vector3.up * h, a + Vector3.up * h, n); k.Box(M.wood, Vector3.Min(a, b + n * 0.03f), Vector3.Max(a, b + n * 0.03f) + Vector3.up * 0.1f); }
            Hall(new Vector3(0, 0, 0.001f), new Vector3(DoorX0, 0, 0.001f), Vector3.forward); Hall(new Vector3(DoorX1, 0, 0.001f), new Vector3(W, 0, 0.001f), Vector3.forward);
            k.Quad(M.wall, new Vector3(DoorX0, DoorH, 0.001f), new Vector3(DoorX1, DoorH, 0.001f), new Vector3(DoorX1, CeilY, 0.001f), new Vector3(DoorX0, CeilY, 0.001f), Vector3.forward);
            Hall(new Vector3(0.001f, 0, 0), new Vector3(0.001f, 0, 12f), Vector3.right);
            Hall(new Vector3(20f, 0, D - 0.001f), new Vector3(39f, 0, D - 0.001f), Vector3.back);
            // the bar's back wall: dark metro tiles to the top of the back bar, then wood slats to the ceiling
            k.Quad(tilesW, new Vector3(W - 0.001f, 0, 0), new Vector3(W - 0.001f, 0, 15f), new Vector3(W - 0.001f, 2.6f, 15f), new Vector3(W - 0.001f, 2.6f, 0), Vector3.left);
            k.Quad(slats, new Vector3(W - 0.001f, 2.6f, 0), new Vector3(W - 0.001f, 2.6f, 15f), new Vector3(W - 0.001f, CeilY, 15f), new Vector3(W - 0.001f, CeilY, 0), Vector3.left);
            // back rooms: painted plaster on the outside walls
            Plain(new Vector3(0.001f, 0, 12f), new Vector3(0.001f, 0, D), Vector3.right, BohY, M.plaster);
            Plain(new Vector3(0, 0, D - 0.001f), new Vector3(9.5f, 0, D - 0.001f), Vector3.back, BohY, M.plaster); Plain(new Vector3(10.5f, 0, D - 0.001f), new Vector3(20f, 0, D - 0.001f), Vector3.back, BohY, M.plaster);
            k.Quad(M.plaster, new Vector3(9.5f, 2.1f, D - 0.001f), new Vector3(10.5f, 2.1f, D - 0.001f), new Vector3(10.5f, BohY, D - 0.001f), new Vector3(9.5f, BohY, D - 0.001f), Vector3.back);
            Plain(new Vector3(39f, 0, D - 0.001f), new Vector3(W, 0, D - 0.001f), Vector3.back, BohY, M.plaster);
            Plain(new Vector3(W - 0.001f, 0, 15f), new Vector3(W - 0.001f, 0, D), Vector3.left, BohY, M.plaster);
            k.Quad(M.shutter, new Vector3(43.5f, 0f, D - 0.01f), new Vector3(46.5f, 0f, D - 0.01f), new Vector3(46.5f, 2.75f, D - 0.01f), new Vector3(43.5f, 2.75f, D - 0.01f), Vector3.back);

            // partitions (12 cm), with doorways; each side its own finish. Along x (z fixed) or along z (x fixed).
            void PartX(float z, float x0, float x1, float h, Material south, Material north, params (float a, float b, float top)[] gaps)
            {
                var cuts = new List<(float a, float b, float top)>(gaps); cuts.Sort((p, q) => p.a.CompareTo(q.a));
                float x = x0; const float ht = 0.06f;
                void Seg(float a, float b, float y0, float y1)
                {
                    if (b - a < 0.01f || y1 - y0 < 0.01f) return;
                    k.Quad(south, new Vector3(a, y0, z - ht), new Vector3(b, y0, z - ht), new Vector3(b, y1, z - ht), new Vector3(a, y1, z - ht), Vector3.back);
                    k.Quad(north, new Vector3(a, y0, z + ht), new Vector3(b, y0, z + ht), new Vector3(b, y1, z + ht), new Vector3(a, y1, z + ht), Vector3.forward);
                    cols.Add((new Vector3(a, y0, z - ht), new Vector3(b, y1, z + ht)));
                }
                foreach (var (a, b, top) in cuts) { Seg(x, a, 0f, h); Seg(a, b, top, h); k.Box(M.wood, new Vector3(a - 0.05f, 0f, z - ht - 0.01f), new Vector3(a, top, z + ht + 0.01f)); k.Box(M.wood, new Vector3(b, 0f, z - ht - 0.01f), new Vector3(b + 0.05f, top, z + ht + 0.01f)); k.Box(M.wood, new Vector3(a - 0.05f, top, z - ht - 0.01f), new Vector3(b + 0.05f, top + 0.05f, z + ht + 0.01f)); x = b; }
                Seg(x, x1, 0f, h);
            }
            void PartZ(float x, float z0, float z1, float h, Material west, Material east, params (float a, float b, float top)[] gaps)
            {
                var cuts = new List<(float a, float b, float top)>(gaps); cuts.Sort((p, q) => p.a.CompareTo(q.a));
                float z = z0; const float ht = 0.06f;
                void Seg(float a, float b, float y0, float y1)
                {
                    if (b - a < 0.01f || y1 - y0 < 0.01f) return;
                    k.Quad(west, new Vector3(x - ht, y0, a), new Vector3(x - ht, y0, b), new Vector3(x - ht, y1, b), new Vector3(x - ht, y1, a), Vector3.left);
                    k.Quad(east, new Vector3(x + ht, y0, a), new Vector3(x + ht, y0, b), new Vector3(x + ht, y1, b), new Vector3(x + ht, y1, a), Vector3.right);
                    cols.Add((new Vector3(x - ht, y0, a), new Vector3(x + ht, y1, b)));
                }
                foreach (var (a, b, top) in cuts) { Seg(z, a, 0f, h); Seg(a, b, top, h); k.Box(M.wood, new Vector3(x - ht - 0.01f, 0f, a - 0.05f), new Vector3(x + ht + 0.01f, top, a)); k.Box(M.wood, new Vector3(x - ht - 0.01f, 0f, b), new Vector3(x + ht + 0.01f, top, b + 0.05f)); k.Box(M.wood, new Vector3(x - ht - 0.01f, top, a - 0.05f), new Vector3(x + ht + 0.01f, top + 0.05f, b + 0.05f)); z = b; }
                Seg(z, z1, 0f, h);
            }
            // the back-of-house front wall (cage windows, staff door), its bulkhead up to the hall ceiling, and its east wall
            PartX(12f, 0f, 20f, BohY, panel, M.plaster, (1.6f, 3.4f, 2.25f), (3.9f, 5.7f, 2.25f), (6.2f, 8.0f, 2.25f), (9.55f, 10.45f, 2.1f));
            foreach (var (a, b) in new[] { (1.6f, 3.4f), (3.9f, 5.7f), (6.2f, 8.0f) })
            {
                k.Box(M.wood, new Vector3(a, 0f, 11.94f), new Vector3(b, 1.0f, 12.06f));                                      // the wall under the window
                k.Box(M.black, new Vector3(a - 0.05f, 1.0f, 11.6f), new Vector3(b + 0.05f, 1.05f, 12.4f));                       // the marble ledge, both sides
                k.Box(M.gold, new Vector3(a - 0.05f, 1.05f, 11.58f), new Vector3(b + 0.05f, 1.07f, 11.62f));
                cols.Add((new Vector3(a, 0f, 11.94f), new Vector3(b, 2.25f, 12.06f)));
                if (M.grille != null) { var gk = Grille(); gk.Quad(M.grille, new Vector3(a, 1.35f, 12f), new Vector3(b, 1.35f, 12f), new Vector3(b, 2.25f, 12f), new Vector3(a, 2.25f, 12f), Vector3.back); gk.Quad(M.grille, new Vector3(a, 1.35f, 12f), new Vector3(b, 1.35f, 12f), new Vector3(b, 2.25f, 12f), new Vector3(a, 2.25f, 12f), Vector3.forward); }
                k.Box(M.glass, new Vector3(a, 1.07f, 11.99f), new Vector3(b, 1.35f, 12.01f));                                  // the glass speech screen, with a slot under it
            }
            void Bulk(Vector3 a, Vector3 b, Vector3 n) { k.Quad(M.wall, a + Vector3.up * BohY, b + Vector3.up * BohY, b + Vector3.up * CeilY, a + Vector3.up * CeilY, n); k.Box(M.gold, Vector3.Min(a, b) + Vector3.up * (BohY - 0.06f) - new Vector3(n.x == 0 ? 0 : 0.05f, 0, n.z == 0 ? 0 : 0.05f), Vector3.Max(a, b) + Vector3.up * BohY + new Vector3(n.x == 0 ? 0 : 0.05f, 0, n.z == 0 ? 0 : 0.05f)); cols.Add((Vector3.Min(a, b) + Vector3.up * BohY - new Vector3(0.06f, 0, 0.06f), Vector3.Max(a, b) + Vector3.up * CeilY + new Vector3(0.06f, 0, 0.06f))); }
            Bulk(new Vector3(0f, 0, 11.94f), new Vector3(20f, 0, 11.94f), Vector3.back);
            PartZ(20f, 12f, D, BohY, M.plaster, panel); Bulk(new Vector3(20.06f, 0, 12f), new Vector3(20.06f, 0, D), Vector3.right);
            PartZ(9f, 12f, 15f, BohY, concD == null ? M.plaster : M.plaster, M.plaster, (13.15f, 14.05f, 2.1f));                 // cage | corridor
            PartX(15f, 0f, 9f, BohY, M.plaster, M.plaster, (3.15f, 4.95f, 1.95f));                                             // cage | cash room (vault opening)
            PartZ(9f, 15f, D, BohY, M.plaster, M.plaster);                                                                       // cash room | corridor
            PartZ(11f, 12f, D, BohY, M.plaster, M.plaster, (12.3f, 13.2f, 2.1f), (19.05f, 19.95f, 2.1f));                       // corridor | staff room, office
            PartX(17f, 11f, 20f, BohY, M.plaster, M.plaster);                                                                    // staff room | office
            // the stock room, off the back of the bar
            PartX(15f, 39f, W, BohY, M.wall, M.plaster, (49.9f, 50.8f, 2.1f)); Bulk(new Vector3(39f, 0, 14.94f), new Vector3(W, 0, 14.94f), Vector3.back);
            PartZ(39f, 15f, D, BohY, panel, M.plaster); Bulk(new Vector3(38.94f, 0, 15f), new Vector3(38.94f, 0, D), Vector3.left);
            // the vault's steel frame, and a steel lining to the cash room
            k.Box(M.chrome, new Vector3(3.0f, 0f, 14.9f), new Vector3(3.15f, 2.1f, 15.1f)); k.Box(M.chrome, new Vector3(4.95f, 0f, 14.9f), new Vector3(5.1f, 2.1f, 15.1f)); k.Box(M.chrome, new Vector3(3.0f, 1.95f, 14.9f), new Vector3(5.1f, 2.1f, 15.1f));
            var g = k.Build("Interior", root, false); g.layer = 0;
            foreach (var (mn, mx) in cols) { var bc = g.AddComponent<BoxCollider>(); bc.center = (mn + mx) * 0.5f; bc.size = mx - mn; }
            if (grille != null) { var gg = grille.Build("Grilles", root, false, UnityEngine.Rendering.ShadowCastingMode.Off); grille = null; }
        }
        static MeshKit grille; static MeshKit Grille() => grille ??= new MeshKit();

        // ------------------------------------------------------------------------------------------ dressing: every prop in its place
        const int Slots = 0, Pit = 1, Bar = 2, Lobby = 3, Cage = 4, Cash = 5, Staff = 6, Office = 7, Stock = 8, Corr = 9;
        static System.Random R;
        static string Pick(params string[] s) => s[R.Next(s.Length)];

        static void Dress(Transform root)
        {
            var kit = new PropKit(); R = new System.Random(66);
            Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
            void Put(string id, float x, float y, float z, float yaw, int zone, bool solid = false, string tag = null) => kit.Put(id, V(x, y, z), yaw, zone, solid, tag);

            // ---------------------------------------------------------------- the slots hall
            int game = 0;
            void Machine(float x, float z, float yaw, bool stool)
            {
                kit.Put($"slot{game % 8}", V(x, 0f, z), yaw, Slots, true); game += 1 + (game % 3 == 0 ? 2 : 0);
                if (stool) { var f = Quaternion.Euler(0, yaw, 0) * Vector3.forward; kit.Put("slot_stool", V(x, 0f, z) + f * 0.78f, yaw + 180f, Slots); }
            }
            for (int i = 0; i < 13; i++) Machine(0.4f, 1.1f + i * 0.76f, 90f, i % 5 != 3);                       // down the left wall
            for (int i = 0; i < 24; i++) Machine(2.1f + i * 0.76f, 0.4f, 0f, i % 6 != 4);                         // along the front
            foreach (float zc in new[] { 4.2f, 8.2f })                                                              // two islands, back to back, two runs each
                foreach (float x0 in new[] { 2.6f, 11.6f })
                    for (int i = 0; i < 8; i++)
                    {
                        float x = x0 + i * 0.76f;
                        Machine(x, zc - 0.31f, 180f, (i + (int)zc) % 4 != 1); Machine(x, zc + 0.31f, 0f, (i + (int)zc) % 5 != 2);
                    }
            foreach (float zc in new[] { 4.2f, 8.2f }) foreach (float x in new[] { 1.85f, 9.25f, 10.85f, 18.25f }) Put("potted_palm", x, 0f, zc, 0f, Slots);
            Put("ashtray_bin", 20.4f, 0f, 11.4f, 0f, Slots); Put("wet_floor", 10.1f, 0f, 6.3f, 30f, Slots);
            kit.Put("chandelier", V(10.3f, CeilY, 6.2f), 0f, Slots);
            // ---------------------------------------------------------------- the lobby: reception, security, rope line
            for (int i = 0; i < 3; i++) Put("bar_counter", 22.6f, 0f, 1.5f + i, 90f, Lobby, true);
            Put("monitor_pc", 22.5f, 1.075f, 2.2f, -90f, Lobby); Put("cardmachine", 22.4f, 1.075f, 3.1f, -90f, Lobby); Put("clipboard", 22.65f, 1.075f, 1.4f, -80f, Lobby);
            Put("pit_podium", 29.8f, 0f, 2.6f, 90f, Lobby, true); Put("cctv_dome", 26f, CeilY, 3.5f, 0f, Lobby);
            foreach (float x in new[] { 24.25f, 27.75f }) { for (int i = 0; i < 3; i++) Put("stanchion", x, 0f, 0.7f + i * 1.2f, 0f, Lobby); for (int i = 0; i < 2; i++) Put("rope", x, 0f, 1.3f + i * 1.2f, 90f, Lobby); }
            Put("potted_palm", 21.4f, 0f, 0.7f, 0f, Lobby); Put("potted_palm", 30.6f, 0f, 0.7f, 0f, Lobby);
            // ---------------------------------------------------------------- the pit
            Put("etg", 26.2f, 0f, 7.8f, 0f, Pit, true);
            foreach (float z in new[] { 12.6f, 18.4f })
            {
                Put("roulette_table", 24.0f, 0f, z, -90f, Pit, true);
                for (int i = 0; i < 6; i++) Put("casino_chair", 22.85f, 0f, z - 1.25f + i * 0.5f, 90f, Pit);
                Put("limit_sign", 24.6f, 0.8f, z - 1.3f, -90f, Pit);
            }
            foreach (float z in new[] { 11.4f, 15.2f, 19.0f })
            {
                Put("blackjack_table", 35.0f, 0f, z, 90f, Pit, true);
                for (int i = 0; i < 5; i++)
                {
                    float a = Mathf.PI * (i + 0.5f) / 5f; Vector3 lp = new Vector3(Mathf.Cos(a) * 1.55f, 0f, 0.35f - 0.35f + Mathf.Sin(a) * 1.25f);   // round the arc, in front
                    Vector3 wp = V(35.0f, 0f, z) + Quaternion.Euler(0, 90f, 0) * lp;
                    float yaw = Quaternion.LookRotation(V(35.0f, 0f, z) - wp).eulerAngles.y;
                    kit.Put("casino_chair", wp, yaw, Pit);
                }
                Put("limit_sign", 34.5f, 0.8f, z + 1.0f, 90f, Pit);
            }
            Put("poker_table", 30.0f, 0f, 20.3f, 180f, Pit, true);
            for (int i = 0; i < 8; i++)
            {
                float a = Mathf.PI * 2f * (i + 0.5f) / 9f + Mathf.PI * 0.5f + Mathf.PI * 2f / 18f; Vector3 lp = new Vector3(Mathf.Cos(a) * 1.75f, 0f, Mathf.Sin(a) * 1.05f);
                Vector3 wp = V(30f, 0f, 20.3f) + lp; if (wp.z > 21.6f) continue;
                kit.Put("casino_chair", wp, Quaternion.LookRotation(V(30f, 0f, 20.3f) - wp).eulerAngles.y, Pit);
            }
            Put("pit_podium", 30.0f, 0f, 15.0f, 0f, Pit, true);
            foreach (var z in new[] { 9.2f, 13.8f, 18.2f }) kit.Put("chandelier", V(30f, CeilY, z), 0f, Pit);
            foreach (var (x, z) in new[] { (21.5f, 21.4f), (38.4f, 21.4f), (38.4f, 5.6f) }) Put("potted_palm", x, 0f, z, 0f, Pit);
            // ---------------------------------------------------------------- the bar
            string[] spirits = { "btl_glenmorrach", "btl_blackheron", "btl_ardbrae", "btl_oldharbour", "btl_kesterton", "btl_hawthorn", "btl_volkov", "btl_arcticwolf", "btl_rourke", "btl_bayisland", "btl_duvernay", "btl_cremadolce", "btl_sambuca", "btl_bluelagoon", "btl_lavaux", "btl_montealto", "btl_rosado", "btl_prosecco", "btl_tequila", "btl_amaretto", "btl_bitters", "btl_grenadine" };
            const float BBX = 51.64f, BCX = 49.93f, Top = 1.075f, Work = 0.94f;
            string[] modules = { "back_bar_open", "back_bar", "back_bar_open", "back_bar_optics", "back_bar", "back_bar", "back_bar_open", "back_bar_optics", "back_bar", "back_bar", "back_bar_open", "back_bar", "back_bar" };
            int sp = 0;
            for (int i = 0; i < modules.Length; i++)
            {
                float z = 1.0f + i; Put(modules[i], BBX, 0f, z, -90f, Bar);
                if (modules[i] == "back_bar_open") Put(i == 0 ? "glass_washer" : "fridge_bottles", BBX + 0.02f, 0f, z, -90f, Bar);
                if (modules[i] == "back_bar_optics") { Put("optics4", BBX + 0.08f, Work + 0.02f, z, -90f, Bar); }
                if (modules[i] == "back_bar" || modules[i] == "back_bar_open")
                    foreach (float sy in new[] { 1.306f, 1.706f, 2.106f })
                        for (int j = 0; j < 6; j++) Put(spirits[(sp++) % spirits.Length], BBX + 0.08f, sy, z - 0.4f + j * 0.16f, -90f + (j % 3 - 1) * 6f, Bar);
            }
            Put("coffee_machine", BBX - 0.02f, Work, 2.0f, -90f, Bar);
            for (int i = 0; i < 4; i++) for (int j = 0; j < 6; j++) Put("pint_inv", BBX - 0.12f + i * 0.1f, Work, 4.6f + j * 0.1f, 0f, Bar);
            for (int i = 0; i < 2; i++) for (int j = 0; j < 6; j++) Put("wine_empty", BBX - 0.08f + i * 0.12f, Work, 5.6f + j * 0.12f, 0f, Bar);
            for (int i = 0; i < 3; i++) for (int j = 0; j < 5; j++) Put("shot_empty", BBX - 0.1f + i * 0.08f, Work, 8.6f + j * 0.08f, 0f, Bar);
            Put("icebucket", BBX - 0.1f, Work, 9.3f, 0f, Bar); Put("fruit_tray", BBX - 0.12f, Work, 9.8f, -90f, Bar); Put("shaker", BBX - 0.05f, Work, 10.2f, 0f, Bar);
            Put("straws", BBX - 0.1f, Work, 10.45f, 0f, Bar); Put("napkins", BBX - 0.1f, Work, 10.75f, -90f, Bar); Put("crisp_box", BBX - 0.05f, Work, 11.6f, -90f, Bar);
            Put("peanuts_card", BBX + 0.2f, Work + 0.7f, 12.3f, -90f, Bar); Put("peanuts_card", BBX + 0.2f, Work + 0.7f, 12.6f, -90f, Bar);
            Put("glass_rack", BBX - 0.1f, Work, 11.0f, 0f, Bar);
            for (int i = 0; i < 12; i++) Put("bar_counter", BCX, 0f, 1.0f + i, -90f, Bar, true);
            Put("bar_end", BCX, 0f, 12.5f, 90f, Bar, true);
            kit.Solid(V(BBX - 0.3f, 0f, 0.5f), V(W, 0.95f, 13.5f));
            foreach (float z in new[] { 3.5f, 7.5f, 10.5f }) { Put("font3", BCX + 0.05f, Top, z, -90f, Bar); Put("drip_tray", BCX - 0.12f, Top, z, -90f, Bar); }
            Put("font_python", BCX + 0.05f, Top, 5.5f, -90f, Bar); Put("drip_tray", BCX - 0.12f, Top, 5.5f, -90f, Bar);
            foreach (float z in new[] { 2.0f, 9.0f }) { Put("till", BCX + 0.12f, Top, z, 90f, Bar); Put("cardmachine", BCX + 0.2f, Top, z + 0.35f, 90f, Bar); }
            Put("bell", BCX + 0.2f, Top, 12.0f, -90f, Bar); Put("tray", BCX - 0.05f, Top, 6.6f, 0f, Bar); Put("bar_mat", BCX - 0.05f, Top, 8.8f, -90f, Bar);
            string[] drinks = { "pint_lager", "pint_bitter", "pint_stout", "pint_half", "pint_cider", "wine_red", "wine_white", "highball_cola", "highball_gin", "tumbler_whisky", "flute", "half_lager", "pint_empty", "shot_full" };
            for (int i = 0; i < 11; i++)
            {
                float z = 1.0f + i * 1.05f; Put("bar_stool", 49.02f, 0f, z, 90f, Bar);
                float mz = z + (float)(R.NextDouble() - 0.5) * 0.12f; Put($"beermat{R.Next(8)}", BCX - 0.25f, Top, mz, R.Next(360), Bar);
                if (i % 3 != 2) Put(drinks[R.Next(drinks.Length)], BCX - 0.25f, Top + 0.0025f, mz, R.Next(360), Bar);
                if (i % 4 == 1) Put($"beermat{R.Next(8)}", BCX - 0.18f, Top, z + 0.4f, R.Next(360), Bar);
            }
            Put("pint_empty", BCX + 0.02f, Top, 6.5f, 0f, Bar); Put("pint_empty", BCX + 0.0f, Top, 6.7f, 0f, Bar); Put("wine_empty", BCX - 0.1f, Top, 6.75f, 0f, Bar);
            // the lounge: booths along the front, tub chairs round tables, tellies on the wall
            foreach (float x in new[] { 39.4f, 41.4f, 43.4f, 45.4f })
            {
                Put("booth", x, 0f, 0.52f, 0f, Bar, true); Put("booth_table", x, 0f, 1.45f, 0f, Bar, true);
                for (int q = 0; q < 2; q++) if (R.NextDouble() < 0.7) { float dx = q == 0 ? -0.3f : 0.3f; Put($"beermat{R.Next(8)}", x + dx, 0.76f, 1.5f, R.Next(360), Bar); Put(drinks[R.Next(drinks.Length)], x + dx, 0.7625f, 1.5f, R.Next(360), Bar); }
            }
            foreach (var (x, z) in new[] { (40.0f, 5.6f), (43.7f, 5.6f), (40.0f, 9.4f), (43.7f, 9.4f), (41.8f, 12.9f), (46.0f, 7.5f) })
            {
                Put("round_table", x, 0f, z, 0f, Bar, true);
                for (int q = 0; q < 3; q++) { float a = q * 2.094f + x; Vector3 cp = V(x + Mathf.Cos(a) * 0.82f, 0f, z + Mathf.Sin(a) * 0.82f); kit.Put("tub_chair", cp, Quaternion.LookRotation(V(x, 0f, z) - cp).eulerAngles.y, Bar); }
                for (int q = 0; q < 2; q++) if (R.NextDouble() < 0.75) { float a = q * 3.1f + z; float dx = Mathf.Cos(a) * 0.2f, dz = Mathf.Sin(a) * 0.2f; Put($"beermat{R.Next(8)}", x + dx, 0.76f, z + dz, R.Next(360), Bar); Put(drinks[R.Next(drinks.Length)], x + dx, 0.7625f, z + dz, R.Next(360), Bar); }
            }
            Put("crisps_salt", 43.6f, 0.76f, 9.5f, 40f, Bar); Put("tray", 40.2f, 0.76f, 5.5f, 0f, Bar);
            foreach (float x in new[] { 42f, 46f }) Put("wall_tv", x, 2.4f, 14.92f, 180f, Bar);
            kit.Put("chandelier", V(43f, CeilY, 7.5f), 0f, Bar, false, null, 0.8f);
            // ---------------------------------------------------------------- the cashier's cage
            for (int i = 0; i < 3; i++) { float x = 2.5f + i * 2.3f; Put("note_counter", x - 0.3f, 1.05f, 12.25f, 180f, Cage); Put("cardmachine", x + 0.45f, 1.05f, 12.15f, 0f, Cage); Put("chip_stacks", x + 0.15f, 1.07f, 11.75f, R.Next(360), Cage); }
            Put("desk", 4.5f, 0f, 14.3f, 180f, Cage, true); Put("monitor_pc", 4.2f, 0.75f, 14.5f, 180f, Cage); Put("keyboard", 4.2f, 0.75f, 14.05f, 180f, Cage); Put("chip_float", 5.0f, 0.75f, 14.2f, 180f, Cage);
            Put("cash_box", 3.4f, 0.75f, 14.2f, 170f, Cage); Put("cash_bundle", 5.6f, 0.75f, 14.0f, 10f, Cage); Put("cash_bundle", 5.65f, 0.81f, 14.02f, 14f, Cage);
            Put("slot_stool", 2.5f, 0f, 12.9f, 180f, Cage); Put("slot_stool", 7.1f, 0f, 12.9f, 180f, Cage); Put("office_chair", 4.5f, 0f, 13.6f, 180f, Cage);
            Put("keypad", 3.05f, 1.35f, 14.85f, 180f, Cage); Put("cctv_dome", 4.5f, BohY, 13.5f, 0f, Cage); Put("extinguisher", 0.4f, 0f, 14.6f, 90f, Cage);
            // ---------------------------------------------------------------- the secure cash room
            Put("vault_door", 4.05f, 0f, 15.0f, 180f, Cash, false, "vault");
            float[] lv = { 0.135f, 0.615f, 1.095f, 1.575f };
            foreach (var (x, z, yaw) in new[] { (0.6f, 16.6f, 90f), (0.6f, 17.9f, 90f), (0.6f, 19.2f, 90f), (2.6f, 21.6f, 180f), (3.9f, 21.6f, 180f) })
            {
                Put("shelving", x, 0f, z, yaw, Cash, true);
                Vector3 f = Quaternion.Euler(0, yaw, 0) * Vector3.right;
                for (int l = 0; l < 4; l++) for (int q = 0; q < 3; q++)
                {
                    Vector3 p = V(x, lv[l], z) + f * (-0.38f + q * 0.38f);
                    string id = l == 3 ? Pick("box3", "cash_box", "cash_box") : Pick("cash_bag", "cash_bag", "cash_bundle", "cash_brick", "cash_box");
                    kit.Put(id, p, yaw + R.Next(-10, 10), Cash);
                }
            }
            Put("safe", 8.3f, 0f, 21.35f, 180f, Cash, true); Put("staff_table", 5.6f, 0f, 18.6f, 0f, Cash, true);
            Put("note_counter", 5.3f, 0.735f, 18.6f, 0f, Cash); Put("cash_brick", 5.95f, 0.735f, 18.4f, 20f, Cash); Put("cash_bundle", 5.9f, 0.735f, 18.85f, 80f, Cash); Put("cash_bundle", 6.15f, 0.735f, 18.7f, 40f, Cash);
            Put("stack_chair", 5.6f, 0f, 17.8f, 0f, Cash); Put("cash_trolley", 7.6f, 0f, 16.3f, 20f, Cash); Put("cctv_dome", 4.5f, BohY, 18.5f, 0f, Cash);
            for (int q = 0; q < 4; q++) Put("cash_bag", 7.4f + (q % 2) * 0.3f, 0f, 19.6f + (q / 2) * 0.25f, R.Next(360), Cash);
            // ---------------------------------------------------------------- the corridor and its doors
            Put("door", 10.0f, 0f, 12.0f, 0f, Corr, false, "staff"); Put("door_steel", 9.0f, 0f, 13.6f, 90f, Corr, false, "cage");
            Put("door", 11.0f, 0f, 12.75f, 90f, Corr, false, "staffroom"); Put("door", 11.0f, 0f, 19.5f, 90f, Corr, false, "office"); Put("door_steel", 10.0f, 0f, D, 180f, Corr, false, "exit");
            Put("door", 50.35f, 0f, 15.0f, 0f, Stock, false, "stock");
            Put("exit_sign", 10.0f, 2.35f, D - 0.05f, 180f, Corr); Put("extinguisher", 9.25f, 0f, 20.6f, 90f, Corr); Put("first_aid", 10.85f, 1.4f, 16.4f, -90f, Corr);
            Put("keypad", 10.65f, 1.35f, 11.92f, 180f, Corr); Put("wet_floor", 10.2f, 0f, 17.2f, 20f, Corr); Put("noticeboard", 9.07f, 1.55f, 17.5f, 90f, Corr);
            // ---------------------------------------------------------------- the staff room
            Put("lockers", 14.6f, 0f, 16.74f, 180f, Staff, true); Put("lockers", 15.85f, 0f, 16.74f, 180f, Staff, true);
            Put("kitchenette", 19.68f, 0f, 14.0f, -90f, Staff, true); Put("fridge_tall", 19.64f, 0f, 16.35f, -90f, Staff, true);
            Put("kettle", 19.72f, 0.92f, 13.15f, -90f, Staff); Put("toaster", 19.72f, 0.92f, 13.65f, -90f, Staff); Put("microwave", 19.7f, 0.92f, 14.95f, -90f, Staff);
            Put("mug_dirty", 19.5f, 0.92f, 12.95f, 30f, Staff); Put("mug_tea", 19.45f, 0.92f, 14.3f, 200f, Staff); Put("mug_dirty", 19.5f, 0.92f, 15.6f, 100f, Staff);
            Put("staff_table", 15.2f, 0f, 14.2f, 0f, Staff, true);
            Put("stack_chair", 14.8f, 0f, 13.55f, 0f, Staff); Put("stack_chair", 15.6f, 0f, 13.55f, 0f, Staff); Put("stack_chair", 14.8f, 0f, 14.85f, 180f, Staff); Put("stack_chair", 15.6f, 0f, 14.85f, 175f, Staff);
            Put("mug_dirty", 15.0f, 0.735f, 14.1f, 10f, Staff); Put("newspaper", 15.5f, 0.735f, 14.3f, 20f, Staff); Put("crisps_cheese", 14.7f, 0.735f, 14.4f, 70f, Staff); Put("cards", 15.3f, 0.735f, 13.95f, 15f, Staff);
            Put("sofa", 11.62f, 0f, 14.7f, 90f, Staff, true); Put("vending", 12.25f, 0f, 16.5f, 180f, Staff, true);
            Put("wall_tv", 15.2f, 1.9f, 12.08f, 0f, Staff); Put("noticeboard", 17.8f, 1.55f, 16.96f, 180f, Staff); Put("wall_clock", 13.0f, 2.25f, 16.96f, 180f, Staff);
            Put("chair_stack", 18.0f, 0f, 12.45f, 0f, Staff); Put("pedal_bin", 19.6f, 0f, 12.4f, 0f, Staff);
            // ---------------------------------------------------------------- the office
            Put("desk", 15.8f, 0f, 20.0f, 0f, Office, true); Put("office_chair", 15.8f, 0f, 20.8f, 180f, Office);
            Put("monitor_pc", 15.8f, 0.75f, 19.8f, 0f, Office); Put("keyboard", 15.8f, 0.75f, 20.2f, 180f, Office); Put("mouse", 16.2f, 0.75f, 20.25f, 180f, Office);
            Put("desk_phone", 15.0f, 0.75f, 19.95f, 160f, Office); Put("pc_tower", 16.45f, 0f, 19.9f, 0f, Office); Put("clipboard", 16.5f, 0.75f, 20.0f, 200f, Office); Put("mug_dirty", 15.3f, 0.75f, 20.25f, 0f, Office);
            Put("cctv_wall", 19.96f, 0f, 19.5f, -90f, Office); Put("filing_cabinet", 12.0f, 0f, 17.36f, 0f, Office, true); Put("filing_cabinet", 12.5f, 0f, 17.36f, 0f, Office, true);
            Put("safe", 19.35f, 0f, 21.45f, -90f, Office, true); Put("water_cooler", 11.4f, 0f, 21.55f, 90f, Office, true);
            Put("stack_chair", 15.3f, 0f, 19.05f, 0f, Office); Put("stack_chair", 16.3f, 0f, 19.05f, 5f, Office);
            Put("noticeboard", 11.07f, 1.55f, 18.4f, 90f, Office); Put("wall_clock", 16.0f, 2.25f, D - 0.04f, 180f, Office); Put("potted_palm", 19.4f, 0f, 17.5f, 0f, Office);
            // ---------------------------------------------------------------- the stock room
            foreach (var (x, z, yaw) in new[] { (40.2f, 21.65f, 180f), (41.5f, 21.65f, 180f), (42.8f, 21.65f, 180f), (39.35f, 16.4f, 90f), (39.35f, 17.7f, 90f), (39.35f, 19.0f, 90f) })
            {
                Put("shelving", x, 0f, z, yaw, Stock, true); Vector3 f = Quaternion.Euler(0, yaw, 0) * Vector3.right;
                for (int l = 0; l < 4; l++) for (int q = 0; q < 2; q++) { if (R.NextDouble() < 0.15) continue; kit.Put(l == 0 ? Pick("beer_crate", "box1", "box4") : Pick("box0", "box1", "box2", "box3", "box4", "box5"), V(x, lv[l], z) + f * (-0.29f + q * 0.58f), yaw + R.Next(-6, 6), Stock); }
            }
            for (int i = 0; i < 8; i++) Put("keg", 48.6f + (i % 4) * 0.45f, 0f, 19.6f + (i / 4) * 0.45f, R.Next(360), Stock);
            for (int i = 0; i < 3; i++) Put("co2", 51.6f, 0f, 20.2f + i * 0.28f, 0f, Stock);
            for (int i = 0; i < 3; i++) for (int j = 0; j < 2; j++) Put("beer_crate", 45.6f + j * 0.42f, i * 0.3f, 16.0f, 0f, Stock);
            for (int i = 0; i < 3; i++) Put("glass_rack", 47.4f, i * 0.25f, 21.4f, 0f, Stock);
            Put("pallet_truck", 44.2f, 0f, 18.6f, 35f, Stock); Put("mop_bucket", 39.9f, 0f, 15.6f, 0f, Stock); Put("chair_stack", 46.6f, 0f, 21.4f, 0f, Stock);
            Put("box2", 42.0f, 0f, 17.0f, 12f, Stock); Put("box3", 42.1f, 0.35f, 17.05f, 5f, Stock); Put("box0", 42.7f, 0f, 16.8f, 40f, Stock); Put("extinguisher", 51.7f, 0f, 15.4f, -90f, Stock);
            Put("ashtray_bin", 51.5f, 0f, 21.6f, 0f, Stock);

            // ---------------------------------------------------------------- signs and pictures
            kit.Picture("sign_cashier", V(4.8f, 2.55f, 11.93f), Vector3.back, 2.6f, 0.5f, Cage);
            kit.Picture("sign_slots", V(15.0f, 3.5f, 11.93f), Vector3.back, 2.4f, 0.75f, Slots);
            kit.Picture("sign_tables", V(38.93f, 3.5f, 18.5f), Vector3.left, 3.2f, 0.66f, Pit);
            kit.Picture("sign_bar", V(W - 0.02f, 3.2f, 7.5f), Vector3.left, 4.0f, 0.78f, Bar);
            kit.Picture("sign_staff", V(10.0f, 2.32f, 11.92f), Vector3.back, 0.7f, 0.22f, Corr);
            kit.Picture("sign_private", V(11.0f, 1.6f, 11.92f), Vector3.back, 0.5f, 0.16f, Corr);
            kit.Picture("sign_cash", V(8.93f, 2.3f, 13.6f), Vector3.right, 0.7f, 0.22f, Corr);
            kit.Picture("sign_staffroom", V(11.07f, 2.3f, 12.75f), Vector3.left, 0.7f, 0.22f, Corr);
            kit.Picture("sign_office", V(11.07f, 2.3f, 19.5f), Vector3.left, 0.7f, 0.22f, Corr);
            kit.Picture("sign_stock", V(50.35f, 2.3f, 14.93f), Vector3.back, 0.7f, 0.22f, Bar);
            kit.Picture("sign_18", V(23.03f, 0.75f, 2.5f), Vector3.right, 0.7f, 0.22f, Lobby);
            kit.Picture("sign_cctv", V(21.5f, 2.2f, 0.05f), Vector3.forward, 0.5f, 0.16f, Lobby);
            kit.Picture("sign_resp", V(30.5f, 2.2f, 0.05f), Vector3.forward, 0.6f, 0.19f, Lobby);
            kit.Picture("pricelist", V(W - 0.02f, 1.9f, 13.6f), Vector3.left, 0.5f, 0.75f, Bar);
            kit.Picture("pricelist", V(W - 0.02f, 1.9f, 0.45f), Vector3.left, 0.5f, 0.75f, Bar);
            kit.Picture("sign_resp", V(0.02f, 2.2f, 11.0f), Vector3.right, 0.6f, 0.19f, Slots);
            kit.Picture("sign_exit", V(DoorX0 + 2f, 3.2f, 0.05f), Vector3.forward, 0.5f, 0.16f, Lobby);
            kit.Picture("notice1", V(19.98f, 1.5f, 18.4f), Vector3.left, 0.3f, 0.38f, Office);
            kit.Picture("notice0", V(14.0f, 1.6f, 12.06f), Vector3.forward, 0.3f, 0.38f, Staff);
            kit.Picture("clipboard", V(0.02f, 1.5f, 13.2f), Vector3.right, 0.4f, 0.3f, Cage);
            kit.Picture("sign_wet", V(39.03f, 1.4f, 15.6f), Vector3.right, 0.4f, 0.12f, Stock);
            kit.Build(root, "Props");
        }

        static void Lights(Transform root)
        {
            var spots = new List<(Vector3 p, float range, float inten, bool cool)>();
            foreach (var x in new[] { 4f, 10.5f, 17f }) foreach (var z in new[] { 3f, 9f }) spots.Add((new Vector3(x, 3.4f, z), 9f, 3.2f, false));      // the slots hall
            foreach (var x in new[] { 24f, 30f, 36f }) foreach (var z in new[] { 8f, 13.5f, 19f }) spots.Add((new Vector3(x, 3.0f, z), 8.5f, 3.4f, false));  // the pit, warm under the chandeliers
            foreach (var x in new[] { 41f, 46.5f }) foreach (var z in new[] { 3f, 8f, 12.5f }) spots.Add((new Vector3(x, 3.2f, z), 8f, 3.0f, false));   // the lounge
            foreach (var z in new[] { 3f, 7.5f, 12f }) spots.Add((new Vector3(50.8f, 2.6f, z), 6f, 2.6f, false));                                         // behind the bar
            spots.Add((new Vector3(26f, 3.4f, 2.5f), 8.5f, 3.2f, false));                                                                                       // the lobby
            foreach (var p in new[] { new Vector3(4.5f, 2.5f, 13.5f), new Vector3(4.5f, 2.5f, 18.5f), new Vector3(10f, 2.5f, 15f), new Vector3(10f, 2.5f, 20f), new Vector3(15.5f, 2.5f, 14.5f), new Vector3(15.5f, 2.5f, 19.5f), new Vector3(43f, 2.6f, 18.5f), new Vector3(48.5f, 2.6f, 18.5f) })
                spots.Add((p, 6.5f, 2.4f, true));                                                                                                               // the back rooms
            spots.Add((new Vector3(16f, 4.2f, -4.5f), 8f, 2.2f, false)); spots.Add((new Vector3(36f, 4.2f, -4.5f), 8f, 2.2f, false));                         // under the canopy
            foreach (var (p, range, inten, cool) in spots)
            {
                var g = new GameObject("Light"); g.transform.SetParent(root, false); g.transform.localPosition = p;
                var l = g.AddComponent<Light>(); l.type = LightType.Point; l.range = range; l.intensity = inten; l.color = cool ? new Color(0.95f, 0.97f, 1f) : new Color(1f, 0.86f, 0.66f);
                l.shadows = LightShadows.None; l.renderMode = LightRenderMode.Auto;
#if UNITY_EDITOR
                l.lightmapBakeType = LightmapBakeType.Realtime;
#endif
            }
        }

        // ================================================================================================ at play: staff, regulars
        static readonly List<GameObject> inside = new List<GameObject>();
        /// <summary>The staff where they work and a few regulars, plus the room's sound. People indoors only exist while you're near.</summary>
        public static void People()
        {
            if (GameObject.Find(RootName) == null) return;
            PropWorld.Ensure(); inside.Clear();
            void Make(Vector3 at, float yaw, string role, bool sit = false, bool outdoor = false)
            {
                var p = World.MakePerson(Origin + at, role == "Staff" ? Color.black : new Color(0.3f, 0.3f, 0.35f), role == "Staff" ? Color.black : new Color(0.2f, 0.2f, 0.22f), role == "Staff" ? "Staff" : "Punter");
                p.transform.rotation = Quaternion.Euler(0, yaw, 0);
                if (sit) p.gameObject.AddComponent<SitPose>();
                if (!outdoor) inside.Add(p.gameObject);
            }
            Make(new Vector3(DoorX0 - 1.2f, 0f, -2.4f), 180f, "Staff", false, true); Make(new Vector3(DoorX1 + 1.2f, 0f, -2.4f), 180f, "Staff", false, true);
            Make(new Vector3(21.7f, 0f, 2.5f), 90f, "Staff"); Make(new Vector3(30.5f, 0f, 2.6f), -90f, "Staff");
            Make(new Vector3(4.8f, 0f, 12.8f), 180f, "Staff");                                              // the cashier
            Make(new Vector3(25.1f, 0f, 13.4f), -90f, "Staff"); Make(new Vector3(25.1f, 0f, 19.2f), -90f, "Staff");   // croupiers
            Make(new Vector3(34.2f, 0f, 11.4f), 90f, "Staff"); Make(new Vector3(34.2f, 0f, 15.2f), 90f, "Staff");     // dealers
            Make(new Vector3(30f, 0f, 15.8f), 180f, "Staff");                                                // the pit boss
            Make(new Vector3(50.8f, 0f, 4.2f), -90f, "Staff"); Make(new Vector3(50.8f, 0f, 9.2f), -90f, "Staff");     // behind the bar
            // punters (and two staff) sat on real seats: picked from the props, so they're exactly on them
            PropSet props = null; foreach (var ps in PropSet.All) if (ps != null && ps.transform.parent != null && ps.transform.parent.name == RootName) props = ps;
            if (props != null)
            {
                var want = new Dictionary<string, int[]> { ["slot_stool"] = new[] { 3, 17, 29, 41, 55, 66 }, ["bar_stool"] = new[] { 1, 4, 8 }, ["casino_chair"] = new[] { 2, 7, 13, 18, 22, 27 }, ["tub_chair"] = new[] { 1, 9 }, ["booth"] = new[] { 2 }, ["office_chair"] = new[] { 1 }, ["sofa"] = new[] { 0 } };
                var seen = new Dictionary<string, int>();
                for (int i = 0; i < props.recs.Length; i++)
                {
                    string id = props.recs[i].id; if (!want.TryGetValue(id, out var picks)) continue;
                    int n = seen.TryGetValue(id, out var c) ? c : 0; seen[id] = n + 1;
                    if (System.Array.IndexOf(picks, n) < 0) continue;
                    var e = PropLib.Get(id); if (e == null) continue;
                    Quaternion q = props.Rot(i); Vector3 at = props.World(i) + q * new Vector3(0f, Mathf.Max(0f, e.seat - 0.46f), -0.06f);
                    bool staff = id == "office_chair" || id == "sofa";
                    var p = World.MakePerson(at, staff ? Color.black : new Color(0.3f, 0.3f, 0.35f), staff ? Color.black : new Color(0.2f, 0.2f, 0.22f), staff ? "Staff" : "Punter");
                    p.transform.rotation = Quaternion.Euler(0, q.eulerAngles.y, 0); p.gameObject.AddComponent<SitPose>().lean = id == "slot_stool" || id == "bar_stool"; inside.Add(p.gameObject);
                }
            }
            Make(new Vector3(22.3f, 0f, 16.0f), 60f, "Punter"); Make(new Vector3(27.6f, 0f, 10.2f), 200f, "Punter");
            new GameObject("CasinoFloor").AddComponent<CasinoFloor>();
        }
        /// <summary>Show or hide the people inside (they cost a lot to draw; nobody sees them from the road).</summary>
        public static void Inside(bool on) { foreach (var g in inside) if (g != null && g.activeSelf != on) g.SetActive(on); }
    }

    /// <summary>Keeps a person sat down (the seated pose) wherever they were put.</summary>
    public class SitPose : MonoBehaviour
    {
        /// <summary>Leaning forward with the hands out (at the bar, at a machine) more often than sat back.</summary>
        public bool lean;
        Person p; Vector3 at; Quaternion rot; float sitVar = -1f;
        void Start()
        {
            p = GetComponent<Person>(); at = transform.position; rot = transform.rotation;
            sitVar = lean ? (Random.value < 0.6f ? 2f : Random.Range(0, 2)) : Random.Range(0, 2); // each their own way of sitting
            if (GetComponent<Fidget>() == null) gameObject.AddComponent<Fidget>();
            StartCoroutine(Apply());
        }
        void OnEnable() { if (p != null) StartCoroutine(Apply()); }
        /// <summary>Sat down, in their own way of sitting and at their own point in it, so neighbours never move in step.</summary>
        System.Collections.IEnumerator Apply()
        {
            yield return null;
            if (p == null || p.anim == null || !p.anim.isActiveAndEnabled) yield break;
            bool hasSit = false, hasVar = false;
            foreach (var q in p.anim.parameters) { if (q.name == "Sit") hasSit = true; if (q.name == "SitVar") hasVar = true; }
            if (!hasSit) yield break;
            if (hasVar) p.anim.SetFloat("SitVar", sitVar);
            p.anim.SetBool("Sit", true);
            p.anim.Play("Sit", 0, Random.value);
        }
        void LateUpdate() { if (p == null || p.state != Person.State.Walking) return; transform.SetPositionAndRotation(at, rot); }
    }

    /// <summary>
    /// The room itself: buying chips at the cage windows (TALK), the murmur and the machines paying out that you only
    /// hear inside, and switching the indoor people on as you come near.
    /// </summary>
    public class CasinoFloor : MonoBehaviour
    {
        public static CasinoFloor I;
        static List<CasinoHall.Station> stations;
        public static CasinoHall.Station? Near;
        PlayerCtl pc; float jingleT; AudioSource murmur; bool peopleOn = true; readonly List<Vector3> machines = new List<Vector3>();
        static GUIStyle style;

        System.Collections.IEnumerator Start()
        {
            // the room counts as indoors (warm room light, the murmur, the camera pulled in): the zone set when the town was
            // built doesn't survive into play, so it's registered again here, once everything else is up
            yield return null;
            var root = GameObject.Find(CasinoHall.RootName);
            if (root != null) GameAudio.RegisterZone(root.transform, new List<CRect> { new CRect(0f, CasinoHall.W, 0f, CasinoHall.D) }, CasinoHall.CeilY + 0.3f);
        }

        void Awake()
        {
            I = this; stations = CasinoHall.Stations();
            foreach (var s in PropSet.All) if (s != null && s.recs != null) for (int i = 0; i < s.recs.Length; i++) if (s.recs[i].id.StartsWith("slot") && !s.recs[i].id.StartsWith("slot_")) machines.Add(s.World(i));
        }

        void Update()
        {
            if (pc == null) pc = FindFirstObjectByType<PlayerCtl>();
            Near = null; if (pc == null) return;
            Vector3 me = pc.transform.position; Vector3 centre = CasinoHall.Origin + new Vector3(CasinoHall.W * 0.5f, 0, CasinoHall.D * 0.5f);
            float d2 = (me - centre).WithY(0).sqrMagnitude;
            bool want = d2 < (peopleOn ? 46f * 46f : 40f * 40f); if (want != peopleOn) { peopleOn = want; CasinoHall.Inside(want); }
            if (pc.InCar || CasinoUI.Open) return;
            if (d2 > 900f) { Ambience(false); return; }
            float best = 1.0f;
            foreach (var s in stations) { float d = (s.stand - me).WithY(0).magnitude; if (d < best) { best = d; Near = s; } }
            Ambience(GameAudio.I != null && GameAudio.I.InsideAny(me + Vector3.up));
        }

        /// <summary>TALK pressed at a cage window: buy or cash chips. True if it did.</summary>
        public static bool TryUse()
        {
            if (Near == null || CasinoUI.Open) return false;
            var s = Near.Value; var pc = I != null ? I.pc : null;
            if (pc != null) pc.transform.rotation = Quaternion.Euler(0, s.yaw, 0);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            CasinoUI.Show(s.game, s.seed); return true;
        }

        void Ambience(bool inside)
        {
            if (murmur == null)
            {
                murmur = gameObject.AddComponent<AudioSource>(); murmur.loop = true; murmur.spatialBlend = 0f; murmur.volume = 0f;
                const int sr = 22050, n = sr * 4; var d = new float[n]; var r = new System.Random(5); float lp = 0f, lp2 = 0f;
                for (int i = 0; i < n; i++) { lp += ((float)(r.NextDouble() * 2 - 1) - lp) * 0.05f; lp2 += (lp - lp2) * 0.08f; d[i] = lp2 * 2.2f * (0.8f + 0.2f * Mathf.Sin(i / (float)sr * 1.7f)); }
                murmur.clip = AudioClip.Create("casino_murmur", n, 1, sr, false); murmur.clip.SetData(d, 0); murmur.Play();
            }
            murmur.volume = Mathf.MoveTowards(murmur.volume, inside ? 0.35f : 0f, Time.deltaTime * 0.8f);
            if (!inside || GameAudio.I == null || machines.Count == 0) return;
            jingleT -= Time.deltaTime; if (jingleT > 0f) return;
            jingleT = Random.Range(2f, 5f);
            GameAudio.I.Play3D(Jingle(), machines[Random.Range(0, machines.Count)] + Vector3.up * 1.4f, 0.25f, Random.Range(0.9f, 1.15f), 22f, 2f);
        }

        static AudioClip jingle;
        static AudioClip Jingle()
        {
            if (jingle != null) return jingle;
            const int sr = 22050; int n = (int)(sr * 0.9f); var d = new float[n]; float[] notes = { 784f, 988f, 1175f, 1568f, 1175f, 1568f };
            for (int i = 0; i < n; i++) { float t = i / (float)sr; int k = Mathf.Min(notes.Length - 1, (int)(t / 0.12f)); float nt = t - k * 0.12f; d[i] = (Mathf.Sign(Mathf.Sin(nt * 2 * Mathf.PI * notes[k])) * 0.25f + Mathf.Sin(nt * 2 * Mathf.PI * notes[k]) * 0.3f) * Mathf.Exp(-nt * 9f) * Mathf.Min(1f, (n - i) / 400f); }
            jingle = AudioClip.Create("casino_jingle", n, 1, sr, false); jingle.SetData(d, 0); return jingle;
        }

        void OnGUI()
        {
            if (Near == null || CasinoUI.Open || World.HideHud) return;
            style ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(18, Screen.height / 30), fontStyle = FontStyle.Bold };
            string t = (TouchControls.Wanted ? "TALK: " : "T: ") + Near.Value.label;
            var r = new Rect(0, Screen.height * 0.78f, Screen.width, style.fontSize * 1.6f);
            style.normal.textColor = new Color(0, 0, 0, 0.75f); GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), t, style);
            style.normal.textColor = new Color(1f, 0.85f, 0.4f); GUI.Label(r, t, style);
        }
    }
}

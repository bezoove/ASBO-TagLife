using System.Collections.Generic;
using UnityEngine;
using ASBO.Casino;

namespace ASBO.Slice
{
    /// <summary>
    /// The Silver Sixpence, a 1990s out-of-town casino on the edge of town where the main road heads out east: a big
    /// flat-roofed box in grey cladding on a black tiled plinth, an illuminated sign right across the front, a canopy and
    /// red carpet over the door, a car park, and a tall lit sign by the road. Inside: loud carpet, flock wallpaper and
    /// gold, the cashier's cage, a bar, a roulette wheel, two blackjack tables, a poker table and eighteen fruit
    /// machines, with a doorman, a cashier, croupiers, a barman and a few regulars. Walk up to anything and press TALK
    /// to play. The name is made up. Built into the scene by the editor with the rest of the town.
    /// </summary>
    public static class CasinoHall
    {
        public const string RootName = "Casino";
        /// <summary>The building's front-left (south-west) corner; it runs W along +x and D back along +z, front to the road.</summary>
        public static readonly Vector3 Origin = new Vector3(168f, 0f, -2f);
        public const float W = 40f, D = 22f, H = 6.6f, CeilY = 4.2f, DoorX0 = 18f, DoorX1 = 22f, DoorH = 3f;
        /// <summary>The ground levelled for it (x, z): building and car park.</summary>
        public static readonly Rect Plot = new Rect(156f, -24.5f, 64f, 47f);
        public static readonly Vector3 PylonAt = new Vector3(162.5f, 0f, -21.6f);

        public struct Station { public CasinoUI.Game game; public Vector3 stand; public float yaw; public int seed; public string label; }
        /// <summary>Where you stand to play each thing (world), which way you face, and what it is.</summary>
        public static List<Station> Stations()
        {
            var l = new List<Station>();
            void S(CasinoUI.Game g, float x, float z, float yaw, string label, int seed = 0) => l.Add(new Station { game = g, stand = Origin + new Vector3(x, 0f, z), yaw = yaw, seed = seed, label = label });
            S(CasinoUI.Game.Cage, 4.4f, 4.5f, 270f, "Cashier: buy or cash chips");
            S(CasinoUI.Game.Roulette, 16.5f, 10.4f, 0f, "Play roulette");
            S(CasinoUI.Game.Blackjack, 24f, 8.2f, 0f, "Play blackjack");
            S(CasinoUI.Game.Blackjack, 24f, 13.7f, 0f, "Play blackjack");
            S(CasinoUI.Game.Poker, 33f, 13.4f, 0f, "Sit at the poker table");
            int n = 0;
            foreach (var p in SlotSpots()) S(CasinoUI.Game.Slots, p.x + (p.w == 90f ? 1.0f : 0f), p.z - (p.w == 180f ? 1.0f : 0f), p.w == 90f ? 270f : 0f, "Play the fruit machine", n++);
            return l;
        }
        /// <summary>The fruit machines: (x, z, facing) inside. Eight down the left wall facing in, ten along the back.</summary>
        static IEnumerable<Vector4> SlotSpots()
        {
            for (int i = 0; i < 8; i++) yield return new Vector4(0.75f, 0f, 9f + i * 1.25f, 90f);
            for (int i = 0; i < 10; i++) yield return new Vector4(3f + i * 1.15f, 0f, D - 0.75f, 180f);
        }

        class Mats { public Material carpet, wall, sign, pylon, slots, roulette, blackjack, felt, wood, gold, clad, navy, glass, ceiling, light, cab, black, white, line, asphalt, kerb, felt2, chrome, bottles; }
        static Mats M;
        static Material C(string n) => Resources.Load<Material>("Casino/" + n);
        static Material E(string n) => Resources.Load<Material>("Estate/" + n);

        public static Transform Build(Transform parent)
        {
            M = new Mats
            {
                carpet = C("CasinoCarpet"), wall = C("CasinoWall"), sign = C("CasinoSign"), pylon = C("CasinoPylon"), slots = C("CasinoSlots"), roulette = C("CasinoRoulette"), blackjack = C("CasinoBlackjack"),
                felt = C("CasinoFelt"), wood = C("CasinoWood"), gold = C("CasinoGold"), clad = C("CasinoClad"), navy = C("CasinoNavy"), glass = C("CasinoGlass"), ceiling = C("CasinoCeiling"),
                light = C("CasinoLight"), cab = C("CasinoCabinet"), chrome = C("CasinoChrome"), bottles = C("CasinoBottles"),
                black = E("EstateBlack"), white = E("EstateWhite"), line = E("EstateLine"), kerb = E("EstateKerb"), felt2 = E("EstateFelt"), asphalt = Surfaces.Source("asphalt_02"),
            };
            if (M.carpet == null || M.clad == null) { Debug.LogWarning("[ASBO] casino: materials missing"); return null; }
            MeshKit.Tile[M.carpet] = 2f; MeshKit.Tile[M.wall] = 1.5f; MeshKit.Tile[M.clad] = 2f;
            var root = new GameObject(RootName).transform; root.SetParent(parent, false); root.position = Origin;
            Shell(root); CarPark(root); Inside(root); Lights(root);
            Debug.Log($"[ASBO] casino: the Silver Sixpence, {Stations().Count} places to play");
            return root;
        }

        // ------------------------------------------------------------------------------------------ the building
        static void Shell(Transform root)
        {
            var k = new MeshKit(); const float t = 0.3f, plinth = 1.0f;
            // outside faces: black tiled plinth, grey cladding, a navy fascia band with the sign right across the front
            void Outer(Vector3 a, Vector3 b, Vector3 n, float y0, float y1, Material m)
            { k.Quad(m, new Vector3(a.x, y0, a.z), new Vector3(b.x, y0, b.z), new Vector3(b.x, y1, b.z), new Vector3(a.x, y1, a.z), n); }
            void Wall(Vector3 a, Vector3 b, Vector3 n, bool front)
            {
                Outer(a, b, n, 0f, plinth, M.black); Outer(a, b, n, plinth, front ? 4.4f : H, M.clad);
                if (front) Outer(a, b, n, 4.4f, H, M.navy);
            }
            // front, in two runs either side of the doors, and over the door
            Wall(new Vector3(-t, 0, -t), new Vector3(DoorX0, 0, -t), Vector3.back, true);
            Wall(new Vector3(DoorX1, 0, -t), new Vector3(W + t, 0, -t), Vector3.back, true);
            Outer(new Vector3(DoorX0, 0, -t), new Vector3(DoorX1, 0, -t), Vector3.back, DoorH, 4.4f, M.clad); Outer(new Vector3(DoorX0, 0, -t), new Vector3(DoorX1, 0, -t), Vector3.back, 4.4f, H, M.navy);
            Wall(new Vector3(-t, 0, D + t), new Vector3(W + t, 0, D + t), Vector3.forward, false);
            Wall(new Vector3(-t, 0, -t), new Vector3(-t, 0, D + t), Vector3.left, false);
            Wall(new Vector3(W + t, 0, -t), new Vector3(W + t, 0, D + t), Vector3.right, false);
            // cladding fins every two metres down the sides and back
            for (float z = 2f; z < D; z += 2f) { k.Box(M.chrome, new Vector3(-t - 0.08f, plinth, z - 0.05f), new Vector3(-t, H, z + 0.05f)); k.Box(M.chrome, new Vector3(W + t, plinth, z - 0.05f), new Vector3(W + t + 0.08f, H, z + 0.05f)); }
            for (float x = 2f; x < W; x += 2f) k.Box(M.chrome, new Vector3(x - 0.05f, plinth, D + t), new Vector3(x + 0.05f, H, D + t + 0.08f));
            // the sign, lit, across the middle of the fascia; gold trim above and below the band
            k.QuadUV(M.sign, new Vector3(W * 0.5f - 11f, 4.55f, -t - 0.06f), new Vector3(W * 0.5f + 11f, 4.55f, -t - 0.06f), new Vector3(W * 0.5f + 11f, 6.45f, -t - 0.06f), new Vector3(W * 0.5f - 11f, 6.45f, -t - 0.06f), Vector3.back,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)); // seen from the road (looking north) the left end is -x
            k.Box(M.gold, new Vector3(-t - 0.1f, 4.35f, -t - 0.12f), new Vector3(W + t + 0.1f, 4.45f, -t));
            k.Box(M.gold, new Vector3(-t - 0.1f, H - 0.02f, -t - 0.12f), new Vector3(W + t + 0.1f, H + 0.1f, -t));
            // parapet and flat roof
            k.Box(M.clad, new Vector3(-t, H, -t), new Vector3(W + t, H + 0.5f, 0f)); k.Box(M.clad, new Vector3(-t, H, D), new Vector3(W + t, H + 0.5f, D + t));
            k.Box(M.clad, new Vector3(-t, H, 0f), new Vector3(0f, H + 0.5f, D)); k.Box(M.clad, new Vector3(W, H, 0f), new Vector3(W + t, H + 0.5f, D));
            k.Quad(M.felt2, new Vector3(0, H, 0), new Vector3(W, H, 0), new Vector3(W, H, D), new Vector3(0, H, D), Vector3.up);
            k.Box(M.chrome, new Vector3(8f, H, 12f), new Vector3(11f, H + 1.2f, 14f)); k.Box(M.chrome, new Vector3(28f, H, 6f), new Vector3(30f, H + 0.9f, 8f)); // air-con units
            // the door reveals and head, glass doors slid open, the canopy on two gold columns, the red carpet
            k.Quad(M.gold, new Vector3(DoorX0, 0, -t), new Vector3(DoorX0, DoorH, -t), new Vector3(DoorX0, DoorH, 0), new Vector3(DoorX0, 0, 0), Vector3.right);
            k.Quad(M.gold, new Vector3(DoorX1, 0, -t), new Vector3(DoorX1, DoorH, -t), new Vector3(DoorX1, DoorH, 0), new Vector3(DoorX1, 0, 0), Vector3.left);
            k.Quad(M.gold, new Vector3(DoorX0, DoorH, -t), new Vector3(DoorX1, DoorH, -t), new Vector3(DoorX1, DoorH, 0), new Vector3(DoorX0, DoorH, 0), Vector3.down);
            foreach (float gx in new[] { DoorX0 - 1.0f, DoorX1 + 0.05f }) { k.Box(M.glass, new Vector3(gx, 0.02f, -t - 0.06f), new Vector3(gx + 0.95f, DoorH - 0.05f, -t - 0.02f)); k.Box(M.gold, new Vector3(gx, 0f, -t - 0.08f), new Vector3(gx + 0.95f, 0.08f, -t)); }
            k.Box(M.navy, new Vector3(DoorX0 - 3f, 3.25f, -3.6f), new Vector3(DoorX1 + 3f, 3.6f, -t));
            k.Box(M.gold, new Vector3(DoorX0 - 3.05f, 3.2f, -3.65f), new Vector3(DoorX1 + 3.05f, 3.3f, -t));
            foreach (float cx in new[] { DoorX0 - 2.7f, DoorX1 + 2.7f }) k.Cyl(M.gold, new Vector3(cx, 0f, -3.3f), 0.14f, 3.25f, 12);
            for (float x = DoorX0 - 2f; x <= DoorX1 + 2.01f; x += 2f) k.Cyl(M.light, new Vector3(x, 3.19f, -2f), 0.15f, 0.02f, 10);
            k.Box(M.cab, new Vector3(DoorX0 + 0.6f, 0.0f, -6f), new Vector3(DoorX1 - 0.6f, 0.025f, 1.2f)); // red carpet runner, out to the car park and into the lobby
            foreach (float px in new[] { DoorX0 - 1.6f, DoorX1 + 1.6f }) { k.Box(M.gold, new Vector3(px - 0.35f, 0f, -1.5f), new Vector3(px + 0.35f, 0.6f, -0.8f)); k.Cyl(M.felt, new Vector3(px, 0.6f, -1.15f), 0.3f, 1.3f, 8, true, 0.05f); } // planters with clipped conifers
            var g = k.Build("Shell", root, false);
            // solid walls (boxes: the player's capsule never slips through a one-sided face), with the doorway open
            void Solid(Vector3 mn, Vector3 mx) { var bc = g.AddComponent<BoxCollider>(); bc.center = (mn + mx) * 0.5f; bc.size = mx - mn; }
            Solid(new Vector3(-t, 0, -t), new Vector3(DoorX0, H, 0)); Solid(new Vector3(DoorX1, 0, -t), new Vector3(W + t, H, 0)); Solid(new Vector3(DoorX0, DoorH, -t), new Vector3(DoorX1, H, 0));
            Solid(new Vector3(-t, 0, D), new Vector3(W + t, H, D + t)); Solid(new Vector3(-t, 0, 0), new Vector3(0, H, D)); Solid(new Vector3(W, 0, 0), new Vector3(W + t, H, D));
            Solid(new Vector3(-t, H, -t), new Vector3(W + t, H + 0.5f, D + t)); // the roof
            foreach (float cx in new[] { DoorX0 - 2.7f, DoorX1 + 2.7f }) Solid(new Vector3(cx - 0.15f, 0, -3.45f), new Vector3(cx + 0.15f, 3.25f, -3.15f));
            // inside faces: wallpaper to the ceiling, the ceiling, the carpet
            var ki = new MeshKit();
            void Inner(Vector3 a, Vector3 b, Vector3 n) { ki.Quad(M.wall, new Vector3(a.x, 0, a.z), new Vector3(b.x, 0, b.z), new Vector3(b.x, CeilY, b.z), new Vector3(a.x, CeilY, a.z), n); ki.Box(M.wood, new Vector3(Mathf.Min(a.x, b.x) - (n.x > 0 ? 0 : 0), 0f, Mathf.Min(a.z, b.z)), new Vector3(Mathf.Max(a.x, b.x), 0.15f, Mathf.Max(a.z, b.z)) + n * 0.03f); }
            Inner(new Vector3(0, 0, 0), new Vector3(DoorX0, 0, 0), Vector3.forward); Inner(new Vector3(DoorX1, 0, 0), new Vector3(W, 0, 0), Vector3.forward);
            ki.Quad(M.wall, new Vector3(DoorX0, DoorH, 0), new Vector3(DoorX1, DoorH, 0), new Vector3(DoorX1, CeilY, 0), new Vector3(DoorX0, CeilY, 0), Vector3.forward);
            Inner(new Vector3(0, 0, D), new Vector3(W, 0, D), Vector3.back); Inner(new Vector3(0, 0, 0), new Vector3(0, 0, D), Vector3.right); Inner(new Vector3(W, 0, 0), new Vector3(W, 0, D), Vector3.left);
            ki.Quad(M.ceiling, new Vector3(0, CeilY, 0), new Vector3(W, CeilY, 0), new Vector3(W, CeilY, D), new Vector3(0, CeilY, D), Vector3.down);
            ki.Quad(M.carpet, new Vector3(0, 0.01f, 0), new Vector3(W, 0.01f, 0), new Vector3(W, 0.01f, D), new Vector3(0, 0.01f, D), Vector3.up);
            ki.Quad(M.carpet, new Vector3(DoorX0, 0.01f, -t), new Vector3(DoorX1, 0.01f, -t), new Vector3(DoorX1, 0.01f, 0), new Vector3(DoorX0, 0.01f, 0), Vector3.up);
            // gold coving, lit ceiling panels in a grid
            ki.Box(M.gold, new Vector3(0, CeilY - 0.18f, 0), new Vector3(W, CeilY, 0.12f)); ki.Box(M.gold, new Vector3(0, CeilY - 0.18f, D - 0.12f), new Vector3(W, CeilY, D));
            ki.Box(M.gold, new Vector3(0, CeilY - 0.18f, 0), new Vector3(0.12f, CeilY, D)); ki.Box(M.gold, new Vector3(W - 0.12f, CeilY - 0.18f, 0), new Vector3(W, CeilY, D));
            for (float x = 4f; x < W - 1f; x += 6f) for (float z = 4f; z < D - 1f; z += 5f) { ki.Box(M.gold, new Vector3(x - 1.05f, CeilY - 0.06f, z - 1.05f), new Vector3(x + 1.05f, CeilY, z + 1.05f)); ki.Box(M.light, new Vector3(x - 0.9f, CeilY - 0.08f, z - 0.9f), new Vector3(x + 0.9f, CeilY - 0.05f, z + 0.9f)); }
            ki.Build("Interior", root, false);
            GameAudio.RegisterZone(root, new List<CRect> { new CRect(0f, W, 0f, D) }, CeilY + 0.3f);
        }

        // ------------------------------------------------------------------------------------------ outside
        static void CarPark(Transform root)
        {
            var k = new MeshKit(); Vector3 L(float wx, float wz) => new Vector3(wx, 0f, wz) - Origin;
            // tarmac from the pavement to the building, white bays, a kerbed island with a lamp, and the sign by the road
            var a = L(Plot.xMin, -24.45f); var b = L(Plot.xMax, Origin.z - 0.31f); // up to the pavement, and to the foot of the front wall
            k.Quad(M.asphalt, new Vector3(a.x, 0.03f, a.z), new Vector3(b.x, 0.03f, a.z), new Vector3(b.x, 0.03f, b.z), new Vector3(a.x, 0.03f, b.z), Vector3.up);
            for (int row = 0; row < 2; row++)
            {
                float z0 = row == 0 ? -6.2f : -18f, z1 = z0 + 5f; // bays against the building, and a row facing them across the aisle
                for (float x = -9.5f; x <= W + 9.6f; x += 2.6f)
                {
                    if (row == 0 && x > DoorX0 - 4f && x < DoorX1 + 4f) continue; // the entrance and its canopy
                    k.Box(M.line, new Vector3(x - 0.05f, 0.03f, z0), new Vector3(x + 0.05f, 0.04f, z1));
                }
                k.Box(M.line, new Vector3(-9.5f, 0.03f, row == 0 ? z0 - 0.05f : z1 - 0.05f), new Vector3(W + 9.6f, 0.04f, row == 0 ? z0 + 0.05f : z1 + 0.05f));
            }
            // the island at the end with a lamp
            k.Box(M.kerb, new Vector3(-9f, 0f, -12.4f), new Vector3(-2f, 0.15f, -11.6f));
            foreach (float lx in new[] { -5.5f, W * 0.5f, W + 5f })
            {
                var p = new Vector3(lx, 0f, -12f); k.Cyl(M.chrome, p, 0.1f, 6.5f, 8);
                k.Box(M.chrome, new Vector3(lx - 0.3f, 6.4f, -12.3f), new Vector3(lx + 0.3f, 6.6f, -11.7f)); k.Box(M.light, new Vector3(lx - 0.26f, 6.36f, -12.26f), new Vector3(lx + 0.26f, 6.4f, -11.74f));
            }
            // the tall sign by the road, lit both sides
            var s = PylonAt - Origin;
            k.Box(M.chrome, new Vector3(s.x - 0.2f, 0f, s.z - 0.2f), new Vector3(s.x + 0.2f, 3f, s.z + 0.2f));
            k.Box(M.navy, new Vector3(s.x - 1.1f, 3f, s.z - 0.3f), new Vector3(s.x + 1.1f, 7.6f, s.z + 0.3f));
            foreach (int side in new[] { -1, 1 })
            {
                float z = s.z + side * 0.31f;
                var p0 = new Vector3(s.x - 1f, 3.1f, z); var p1 = new Vector3(s.x + 1f, 3.1f, z); var p2 = new Vector3(s.x + 1f, 7.5f, z); var p3 = new Vector3(s.x - 1f, 7.5f, z);
                bool south = side < 0; // seen from the south, left is -x
                k.QuadUV(M.pylon, p0, p1, p2, p3, side < 0 ? Vector3.back : Vector3.forward, south ? new Vector2(0, 0) : new Vector2(1, 0), south ? new Vector2(1, 0) : new Vector2(0, 0), south ? new Vector2(1, 1) : new Vector2(0, 1), south ? new Vector2(0, 1) : new Vector2(1, 1));
            }
            var g = k.Build("CarPark", root, false);
            var col = g.AddComponent<BoxCollider>(); col.center = new Vector3(s.x, 5.3f, s.z); col.size = new Vector3(2.2f, 4.6f, 0.6f);
            var c2 = g.AddComponent<BoxCollider>(); c2.center = new Vector3(s.x, 1.5f, s.z); c2.size = new Vector3(0.4f, 3f, 0.4f);
        }

        // ------------------------------------------------------------------------------------------ inside
        static void Inside(Transform root)
        {
            var k = new MeshKit(); var g = new GameObject("Furniture").transform; g.SetParent(root, false);
            var cols = new List<(Vector3 c, Vector3 s)>();
            void Solid(Vector3 mn, Vector3 mx) => cols.Add(((mn + mx) * 0.5f, mx - mn));
            // the cashier's cage, front left: a counter with brass bars over it and a window
            k.Box(M.wood, new Vector3(3.2f, 0f, 1.5f), new Vector3(3.8f, 1.1f, 7.5f)); k.Box(M.gold, new Vector3(3.15f, 1.1f, 1.45f), new Vector3(3.9f, 1.16f, 7.55f));
            for (float z = 1.6f; z < 7.5f; z += 0.16f) if (z < 4f || z > 5f) k.Cyl(M.gold, new Vector3(3.5f, 1.16f, z), 0.015f, 1.5f, 6);
            k.Box(M.gold, new Vector3(3.4f, 2.66f, 1.5f), new Vector3(3.6f, 2.76f, 7.5f));
            k.Box(M.wood, new Vector3(0f, 0f, 7.5f), new Vector3(3.8f, 2.76f, 7.7f)); // the cage's back
            k.QuadUV(M.sign, new Vector3(3.82f, 2.85f, 3f), new Vector3(3.82f, 2.85f, 6f), new Vector3(3.82f, 3.6f, 6f), new Vector3(3.82f, 3.6f, 3f), Vector3.right, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
            Solid(new Vector3(3.2f, 0f, 1.5f), new Vector3(3.8f, 2.7f, 7.5f)); Solid(new Vector3(0f, 0f, 7.5f), new Vector3(3.8f, 2.7f, 7.7f));
            // the bar, front right: counter, brass rail, stools, shelves of bottles
            k.Box(M.wood, new Vector3(31f, 0f, 4f), new Vector3(W - 0.3f, 1.1f, 4.8f)); k.Box(M.gold, new Vector3(30.95f, 1.1f, 3.95f), new Vector3(W - 0.3f, 1.15f, 4.85f));
            k.CylX(M.gold, new Vector3((31f + W) * 0.5f, 0.25f, 4.95f), 0.03f, W - 31.3f);
            for (float x = 32f; x < W - 0.5f; x += 1.3f) { k.Cyl(M.chrome, new Vector3(x, 0f, 5.6f), 0.04f, 0.72f, 6); k.Cyl(M.cab, new Vector3(x, 0.72f, 5.6f), 0.2f, 0.08f, 10); }
            k.Box(M.wood, new Vector3(31f, 0f, 0f), new Vector3(W, 2.6f, 0.45f));
            k.QuadUV(M.bottles, new Vector3(31.1f, 0.9f, 0.46f), new Vector3(W - 0.1f, 0.9f, 0.46f), new Vector3(W - 0.1f, 2.5f, 0.46f), new Vector3(31.1f, 2.5f, 0.46f), Vector3.forward, new Vector2(0, 0), new Vector2(4, 0), new Vector2(4, 1), new Vector2(0, 1));
            Solid(new Vector3(31f, 0f, 4f), new Vector3(W, 1.15f, 4.8f));
            // the roulette table: wooden rail, printed felt, the wheel in its bowl at the croupier's end
            RouletteTable(k, new Vector3(16.5f, 0f, 12f)); Solid(new Vector3(14.1f, 0f, 11.2f), new Vector3(18.9f, 0.95f, 12.8f));
            // two blackjack tables: half-moon, dealer on the straight side to the north
            foreach (float z in new[] { 9.6f, 15.1f }) { HalfMoon(k, new Vector3(24f, 0f, z)); Solid(new Vector3(22.6f, 0f, z - 0.8f), new Vector3(25.4f, 0.95f, z + 0.5f)); }
            // the poker table: an oval with a padded rail
            Oval(k, new Vector3(33f, 0f, 15f), 1.55f, 0.85f); Solid(new Vector3(31.4f, 0f, 14.2f), new Vector3(34.6f, 0.95f, 15.8f));
            // the fruit machines
            foreach (var s in SlotSpots()) { Slot(k, new Vector3(s.x, 0f, s.z), s.w); Solid(new Vector3(s.x - 0.4f, 0f, s.z - 0.4f), new Vector3(s.x + 0.4f, 1.9f, s.z + 0.4f)); }
            // mirrored gold pillars
            foreach (var p in new[] { new Vector3(11f, 0, 6.5f), new Vector3(29f, 0, 6.5f), new Vector3(11f, 0, 17.5f), new Vector3(29f, 0, 17.5f) }) { k.Box(M.gold, p + new Vector3(-0.35f, 0, -0.35f), p + new Vector3(0.35f, CeilY, 0.35f)); Solid(p + new Vector3(-0.35f, 0, -0.35f), p + new Vector3(0.35f, CeilY, 0.35f)); }
            var body = k.Build("Tables", g, false);
            foreach (var (c, s) in cols) { var bc = body.AddComponent<BoxCollider>(); bc.center = c; bc.size = s; }
        }

        static void Legs(MeshKit k, Vector3 c, float hx, float hz)
        { foreach (var (dx, dz) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) }) k.Box(M.wood, c + new Vector3(dx * hx - 0.06f, 0f, dz * hz - 0.06f), c + new Vector3(dx * hx + 0.06f, 0.8f, dz * hz + 0.06f)); }

        static void RouletteTable(MeshKit k, Vector3 c)
        {
            Legs(k, c, 2.2f, 0.65f);
            k.Box(M.wood, c + new Vector3(-2.4f, 0.8f, -0.8f), c + new Vector3(2.4f, 0.9f, 0.8f));
            k.QuadUV(M.roulette, c + new Vector3(-1.2f, 0.905f, -0.72f), c + new Vector3(2.32f, 0.905f, -0.72f), c + new Vector3(2.32f, 0.905f, 0.72f), c + new Vector3(-1.2f, 0.905f, 0.72f), Vector3.up, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
            k.Box(M.felt, c + new Vector3(-2.32f, 0.9f, -0.72f), c + new Vector3(-1.2f, 0.902f, 0.72f));
            k.Box(M.wood, c + new Vector3(-2.4f, 0.9f, -0.8f), c + new Vector3(2.4f, 0.96f, -0.72f)); k.Box(M.wood, c + new Vector3(-2.4f, 0.9f, 0.72f), c + new Vector3(2.4f, 0.96f, 0.8f)); k.Box(M.wood, c + new Vector3(2.32f, 0.9f, -0.72f), c + new Vector3(2.4f, 0.96f, 0.72f));
            var w = c + new Vector3(-1.75f, 0.9f, 0f);
            k.Cyl(M.wood, w, 0.48f, 0.12f, 24); k.Cyl(M.cab, w + Vector3.up * 0.12f, 0.4f, 0.01f, 24); k.Cyl(M.gold, w + Vector3.up * 0.12f, 0.12f, 0.1f, 12, true, 0.05f);
            k.Cyl(M.gold, w + Vector3.up * 0.22f, 0.012f, 0.12f, 4);
        }

        static void HalfMoon(MeshKit k, Vector3 c)
        {
            Legs(k, c + new Vector3(0, 0, -0.1f), 1.0f, 0.4f);
            // the top: a half disc (straight edge north) as a fan of quads, printed felt
            const int seg = 16; float R = 1.4f;
            for (int i = 0; i < seg; i++)
            {
                float a0 = Mathf.PI * i / seg, a1 = Mathf.PI * (i + 1) / seg;
                Vector3 p0 = c + new Vector3(Mathf.Cos(a0) * R, 0.9f, -Mathf.Sin(a0) * R + 0.45f), p1 = c + new Vector3(Mathf.Cos(a1) * R, 0.9f, -Mathf.Sin(a1) * R + 0.45f);
                Vector2 Uv(Vector3 p) => new Vector2((p.x - c.x) / (2f * R) + 0.5f, 1f - (c.z + 0.45f - p.z) / R); // the felt texture: straight edge at the top
                k.QuadUV(M.blackjack, c + new Vector3(0, 0.905f, 0.45f), p0 + Vector3.up * 0.005f, p1 + Vector3.up * 0.005f, c + new Vector3(0, 0.905f, 0.45f), Vector3.up, Uv(c + new Vector3(0, 0, 0.45f)), Uv(p0), Uv(p1), Uv(c + new Vector3(0, 0, 0.45f)));
                k.Quad(M.wood, p0, p1, p1 + Vector3.down * 0.12f, p0 + Vector3.down * 0.12f, (p0 + p1 - 2f * (c + new Vector3(0, 0.9f, 0.45f))).normalized);
                k.Quad(M.cab, p0 + new Vector3(0, 0.05f, 0), p1 + new Vector3(0, 0.05f, 0), p1, p0, Vector3.up); // padded rail
            }
            k.Box(M.wood, c + new Vector3(-R, 0.78f, 0.4f), c + new Vector3(R, 0.95f, 0.5f));
            k.Box(M.chrome, c + new Vector3(0.6f, 0.95f, 0.25f), c + new Vector3(0.95f, 1.02f, 0.45f)); // the shoe
        }

        static void Oval(MeshKit k, Vector3 c, float rx, float rz)
        {
            Legs(k, c, rx * 0.6f, rz * 0.5f);
            const int seg = 28;
            for (int i = 0; i < seg; i++)
            {
                float a0 = Mathf.PI * 2f * i / seg, a1 = Mathf.PI * 2f * (i + 1) / seg;
                Vector3 p0 = c + new Vector3(Mathf.Cos(a0) * rx, 0.9f, Mathf.Sin(a0) * rz), p1 = c + new Vector3(Mathf.Cos(a1) * rx, 0.9f, Mathf.Sin(a1) * rz);
                Vector3 q0 = c + new Vector3(Mathf.Cos(a0) * (rx - 0.18f), 0.9f, Mathf.Sin(a0) * (rz - 0.18f)), q1 = c + new Vector3(Mathf.Cos(a1) * (rx - 0.18f), 0.9f, Mathf.Sin(a1) * (rz - 0.18f));
                k.TriN(M.felt, c + Vector3.up * 0.9f, q1, q0, Vector3.up);
                k.Quad(M.cab, q0 + Vector3.up * 0.06f, q1 + Vector3.up * 0.06f, p1 + Vector3.up * 0.06f, p0 + Vector3.up * 0.06f, Vector3.up);
                k.Quad(M.cab, q0, q1, q1 + Vector3.up * 0.06f, q0 + Vector3.up * 0.06f, -(q0 + q1 - 2f * c).WithY(0).normalized);
                k.Quad(M.wood, p0 + Vector3.up * 0.06f, p1 + Vector3.up * 0.06f, p1 + Vector3.down * 0.1f, p0 + Vector3.down * 0.1f, (p0 + p1 - 2f * c).WithY(0).normalized);
            }
        }

        /// <summary>A fruit machine: red cabinet, lit front (header, reels, buttons), a little stool in front.</summary>
        static void Slot(MeshKit k, Vector3 p, float yaw)
        {
            k.Push(p, yaw);
            k.Box(M.cab, new Vector3(-0.36f, 0f, -0.32f), new Vector3(0.36f, 1.85f, 0.32f), 32);
            // the front faces local +z
            k.QuadUV(M.slots, new Vector3(0.33f, 0.15f, 0.33f), new Vector3(-0.33f, 0.15f, 0.33f), new Vector3(-0.33f, 1.8f, 0.33f), new Vector3(0.33f, 1.8f, 0.33f), Vector3.forward,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
            k.Box(M.cab, new Vector3(-0.36f, 0.85f, 0.32f), new Vector3(0.36f, 0.9f, 0.5f)); // the button deck
            k.Box(M.chrome, new Vector3(-0.38f, 1.85f, -0.34f), new Vector3(0.38f, 1.9f, 0.34f));
            k.Pop();
        }

        static void Lights(Transform root)
        {
            // four warm lights over the floor (no shadows: cheap), and one over the door outside
            foreach (var p in new[] { new Vector3(9f, 3.7f, 11f), new Vector3(20f, 3.7f, 11f), new Vector3(31f, 3.7f, 11f), new Vector3(20f, 3.7f, 18.5f), new Vector3(20f, 3f, -2f) })
            {
                var g = new GameObject("Light"); g.transform.SetParent(root, false); g.transform.localPosition = p;
                var l = g.AddComponent<Light>(); l.type = LightType.Point; l.range = p.z < 0 ? 8f : 13f; l.intensity = p.z < 0 ? 2.2f : 1.6f; l.color = new Color(1f, 0.86f, 0.66f); l.shadows = LightShadows.None;
                l.renderMode = LightRenderMode.Auto;
#if UNITY_EDITOR
                l.lightmapBakeType = LightmapBakeType.Realtime; // (an editor-only setting; it's baked into the scene)
#endif
            }
        }

        // ================================================================================================ at play: staff, regulars, playing
        /// <summary>The doorman, cashier, croupiers, barman and a handful of regulars, standing where they work and play.</summary>
        public static void People()
        {
            if (GameObject.Find(RootName) == null) return;
            var staff = new (Vector3 at, float yaw)[] { (new Vector3(DoorX0 - 1.2f, 0f, -2.4f), 180f), (new Vector3(1.8f, 0f, 4.5f), 90f), (new Vector3(13.6f, 0f, 12f), 90f), (new Vector3(24f, 0f, 10.6f), 180f), (new Vector3(24f, 0f, 16.1f), 180f), (new Vector3(33f, 0f, 16.2f), 180f), (new Vector3(35f, 0f, 2.4f), 0f) };
            foreach (var (at, yaw) in staff) { var p = World.MakePerson(Origin + at, Color.black, Color.black, "Staff"); p.transform.rotation = Quaternion.Euler(0, yaw, 0); }
            var regs = new (Vector3 at, float yaw)[] { (new Vector3(5.3f, 0f, 21.0f - 1f), 0f), (new Vector3(1.75f, 0f, 12.75f), 270f), (new Vector3(31.6f, 0f, 14.2f), 45f), (new Vector3(34.4f, 0f, 14.2f), -45f), (new Vector3(17.6f, 0f, 10.6f), 0f), (new Vector3(33.3f, 0f, 5.6f), 180f) };
            foreach (var (at, yaw) in regs) { var p = World.MakePerson(Origin + at, new Color(0.3f, 0.3f, 0.35f), new Color(0.2f, 0.2f, 0.22f), "Punter"); p.transform.rotation = Quaternion.Euler(0, yaw, 0); }
            new GameObject("CasinoFloor").AddComponent<CasinoFloor>();
        }
    }

    /// <summary>
    /// Walking round the casino: shows what you can do where you're standing ("TALK: Play blackjack"), opens the game
    /// when you press TALK, keeps the player still while a game's up, and the room's own sound (the murmur and the
    /// machines going off) that you only hear when you're in there.
    /// </summary>
    public class CasinoFloor : MonoBehaviour
    {
        public static CasinoFloor I;
        static List<CasinoHall.Station> stations;
        public static CasinoHall.Station? Near;
        PlayerCtl pc; float jingleT; AudioSource murmur;
        static GUIStyle style;

        void Awake() { I = this; stations = CasinoHall.Stations(); }

        void Update()
        {
            if (pc == null) pc = FindFirstObjectByType<PlayerCtl>();
            Near = null; if (pc == null || pc.InCar || CasinoUI.Open) return;
            Vector3 me = pc.transform.position; float best = 1.35f;
            if ((me - (CasinoHall.Origin + new Vector3(CasinoHall.W * 0.5f, 0, CasinoHall.D * 0.5f))).sqrMagnitude > 900f) { Ambience(false); return; }
            foreach (var s in stations) { float d = (s.stand - me).WithY(0).magnitude; if (d < best) { best = d; Near = s; } }
            Ambience(GameAudio.I != null && GameAudio.I.InsideAny(me + Vector3.up));
        }

        /// <summary>TALK pressed: if you're at a table or machine, sit down to it. True if it did.</summary>
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
            if (!inside || GameAudio.I == null) return;
            jingleT -= Time.deltaTime; if (jingleT > 0f) return;
            jingleT = Random.Range(2.5f, 7f);
            // a machine somewhere pays out
            var s = stations[Random.Range(5, stations.Count)];
            GameAudio.I.Play3D(Jingle(), s.stand + Vector3.up * 1.4f, 0.25f, Random.Range(0.9f, 1.15f), 22f, 2f);
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
            var r = new Rect(0, Screen.height * 0.72f, Screen.width, style.fontSize * 1.6f);
            style.normal.textColor = new Color(0, 0, 0, 0.75f); GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), t, style);
            style.normal.textColor = new Color(1f, 0.85f, 0.4f); GUI.Label(r, t, style);
        }
    }
}

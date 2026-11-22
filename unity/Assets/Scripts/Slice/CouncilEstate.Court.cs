using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// The rest of Hawthorn Way on the green: Hawthorn Court (a twelve-storey 1960s concrete point block with balconies,
    /// a lift-motor room on the roof and a canopied entrance), a row of eight lock-up garages with a tarmac forecourt,
    /// and a fenced playground (swings, slide, roundabout, see-saw, springer, benches and a bin).
    /// </summary>
    public static partial class CouncilEstate
    {
        // Hawthorn Court footprint (x0, z0, width, depth), storeys and storey height
        public static readonly Rect Tower = new Rect(6f, -80f, 20f, 15f);
        public const int TowerFloors = 12; public const float TowerFH = 2.8f;
        public static float TowerTop => TowerFloors * TowerFH;
        public static Vector3 TowerDoor => new Vector3(Tower.xMin - 1.2f, 0f, Tower.center.y);
        // lock-up garages: fronts face north along z = GaragesZ, from x = GaragesX
        public const float GaragesX = -26f, GaragesZ = -93.5f, GarageW = 2.75f, GarageD = 5.5f; public const int GarageCount = 8;
        public static readonly Rect Playground = new Rect(-22f, -62f, 14f, 12f);

        static void Court(Transform root)
        {
            var g = new GameObject("HawthornCourt").transform; g.SetParent(root, false);
            TowerBlock(g); GarageRow(g); PlayArea(g);
        }

        // ------------------------------------------------------------------------------------------ the tower
        static void TowerBlock(Transform parent)
        {
            var k = new MeshKit(); var ground = new MeshKit();
            float x0 = Tower.xMin, x1 = Tower.xMax, z0 = Tower.yMin, z1 = Tower.yMax, top = TowerTop;
            var faces = new (Vector3 o, float yaw, float len, bool entrance)[]
            {
                (new Vector3(x0, 0, z1), 0f, Tower.width, false),     // north
                (new Vector3(x1, 0, z1), 90f, Tower.height, false),   // east
                (new Vector3(x1, 0, z0), 180f, Tower.width, false),   // south
                (new Vector3(x0, 0, z0), -90f, Tower.height, true),   // west: the way in, off the footpath
            };
            var rng = new System.Random(1964);
            foreach (var f in faces)
            {
                k.Push(Matrix4x4.TRS(f.o, Quaternion.Euler(0, f.yaw, 0), Vector3.one));
                TowerFace(k, f.len, f.entrance, f.len > 16f, rng);
                k.Pop();
            }
            // flat roof, parapet coping, lift-motor room, tank housing and aerials
            k.Quad(M.felt, new Vector3(x0, top, z0), new Vector3(x1, top, z0), new Vector3(x1, top, z1), new Vector3(x0, top, z1), Vector3.up);
            k.Box(M.concrete, new Vector3(x0 - 0.15f, top + 0.85f, z0 - 0.15f), new Vector3(x1 + 0.15f, top + 1.0f, z1 + 0.15f), 4);
            foreach (var (a, b) in new[] { (new Vector3(x0, top, z0), new Vector3(x1, top + 0.85f, z0 + 0.2f)), (new Vector3(x0, top, z1 - 0.2f), new Vector3(x1, top + 0.85f, z1)),
                                          (new Vector3(x0, top, z0), new Vector3(x0 + 0.2f, top + 0.85f, z1)), (new Vector3(x1 - 0.2f, top, z0), new Vector3(x1, top + 0.85f, z1)) })
                k.Box(M.concrete, a, b, 4);
            Vector3 c = new Vector3(Tower.center.x, top, Tower.center.y);
            k.Box(M.concrete, c + new Vector3(-3.2f, 0, -2.4f), c + new Vector3(3.2f, 3.6f, 2.4f), 4);
            k.Box(M.felt, c + new Vector3(-3.35f, 3.6f, -2.55f), c + new Vector3(3.35f, 3.75f, 2.55f), 4);
            k.Box(M.towerPanel, c + new Vector3(3.6f, 0, -1.5f), c + new Vector3(6.2f, 2.2f, 1.5f), 4);
            k.Box(M.black, c + new Vector3(-0.6f, 3.6f, 1.2f), c + new Vector3(-0.2f, 3.7f, 1.6f), 4);
            foreach (var p in new[] { new Vector3(x0 + 1.5f, top, z0 + 1.5f), new Vector3(x1 - 2f, top, z1 - 1.2f), new Vector3(x0 + 3f, top, z1 - 2f) })
            {
                k.Box(M.lampCol, p + new Vector3(-0.03f, 0, -0.03f), p + new Vector3(0.03f, 3.2f, 0.03f), 4);
                k.Box(M.lampCol, p + new Vector3(-0.7f, 2.6f, -0.02f), p + new Vector3(0.7f, 2.64f, 0.02f));
                k.Box(M.lampCol, p + new Vector3(-0.5f, 3.0f, -0.02f), p + new Vector3(0.5f, 3.04f, 0.02f));
            }
            // entrance canopy with the name on its fascia, and a slab path to the footpath
            float ez = Tower.center.y;
            k.Box(M.concrete, new Vector3(x0 - 2.4f, 2.6f, ez - 2.2f), new Vector3(x0, 3.1f, ez + 2.2f));
            k.QuadUV(M.courtSign, new Vector3(x0 - 2.41f, 2.62f, ez + 1.0f), new Vector3(x0 - 2.41f, 2.62f, ez - 1.0f), new Vector3(x0 - 2.41f, 3.08f, ez - 1.0f), new Vector3(x0 - 2.41f, 3.08f, ez + 1.0f), Vector3.left,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
            foreach (float zz in new[] { ez - 2.0f, ez + 2.0f }) k.Box(M.lampCol, new Vector3(x0 - 2.25f, 0f, zz - 0.06f), new Vector3(x0 - 2.13f, 2.6f, zz + 0.06f));
            ground.Box(M.slabs, new Vector3(1f, 0f, ez - 1.2f), new Vector3(x0, 0.05f, ez + 1.2f), 1 | 2);
            ground.Box(M.slabs, new Vector3(x0 - 3f, 0f, ez - 2.6f), new Vector3(x0, 0.06f, ez + 2.6f), 2);
            var body = k.Build("Tower", parent, false);
            var col = body.AddComponent<BoxCollider>(); col.center = new Vector3(Tower.center.x, top * 0.5f, Tower.center.y); col.size = new Vector3(Tower.width, top, Tower.height);
            var gg = ground.Build("EstateGround", parent, true); if (gg != null) gg.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>
        /// One face of the tower in its own frame (along +x, out of the building +z): a concrete grid of columns and floor
        /// bands, each bay with a coloured spandrel panel under a window, recessed balconies with railings on the long
        /// faces, brick and the entrance at the bottom.
        /// </summary>
        static void TowerFace(MeshKit k, float len, bool entrance, bool balconies, System.Random rng)
        {
            int nb = Mathf.Max(3, Mathf.RoundToInt(len / 3.4f)); float bw = len / nb, top = TowerTop, fh = TowerFH;
            const float colW = 0.32f, band = 0.28f, proud = 0.1f;
            // columns and floor bands, standing proud of the infill
            for (int j = 0; j <= nb; j++)
            {
                float x = j * bw; float a = Mathf.Max(0f, x - colW * 0.5f), b = Mathf.Min(len, x + colW * 0.5f);
                k.Box(M.concrete, new Vector3(a, 0f, 0f), new Vector3(b, top + 0.85f, proud), 16);
            }
            for (int f = 1; f <= TowerFloors; f++)
            {
                float y = f * fh - band * 0.5f;
                k.Box(M.concrete, new Vector3(0f, y, 0f), new Vector3(len, y + band, proud * 0.8f), 16);
            }
            k.Box(M.concrete, new Vector3(0f, top - 0.1f, 0f), new Vector3(len, top + 0.85f, proud * 0.6f), 16); // parapet face
            int mid = nb / 2;
            for (int f = 0; f < TowerFloors; f++)
                for (int j = 0; j < nb; j++)
                {
                    float xa = j * bw + colW * 0.5f, xb = (j + 1) * bw - colW * 0.5f;
                    float ya = f * fh + (f == 0 ? 0f : band * 0.5f), yb = (f + 1) * fh - band * 0.5f;
                    if (f == 0)
                    {
                        if (entrance && j == mid) { TowerEntrance(k, xa, xb, ya, yb); continue; }
                        // ground floor: brick with a small high window
                        var hole = new Rect(xa + 0.5f, 1.3f, xb - xa - 1.0f, 1.0f);
                        WallCell(k, xa, xb, ya, yb, M.brick, hole);
                        Window(k, hole.xMin, hole.xMax, hole.yMin, hole.yMax, 0f, AtlasGround(0), true);
                        continue;
                    }
                    if (balconies && j % 3 == 1) { Balcony(k, xa, xb, ya, yb, rng); continue; }
                    // spandrel panel, window above it
                    float sill = ya + 0.85f, head = yb - 0.15f;
                    k.Quad(M.towerPanel, new Vector3(xa, ya, 0f), new Vector3(xb, ya, 0f), new Vector3(xb, sill, 0f), new Vector3(xa, sill, 0f), Vector3.forward);
                    k.Quad(M.concrete, new Vector3(xa, head, 0f), new Vector3(xb, head, 0f), new Vector3(xb, yb, 0f), new Vector3(xa, yb, 0f), Vector3.forward);
                    var uv = AtlasUpper(rng.Next(3)); uv.width = 0.5f; uv.x = rng.Next(2) * 0.5f;
                    Window(k, xa, xb, sill, head, 0f, uv, true);
                }
        }

        /// <summary>A wall cell of one material with one rectangular hole in it.</summary>
        static void WallCell(MeshKit k, float xa, float xb, float ya, float yb, Material m, Rect hole)
        {
            void Q(float a, float b, float c, float d) { if (b - a > 1e-3f && d - c > 1e-3f) k.Quad(m, new Vector3(a, c, 0f), new Vector3(b, c, 0f), new Vector3(b, d, 0f), new Vector3(a, d, 0f), Vector3.forward); }
            Q(xa, xb, ya, hole.yMin); Q(xa, xb, hole.yMax, yb); Q(xa, hole.xMin, hole.yMin, hole.yMax); Q(hole.xMax, xb, hole.yMin, hole.yMax);
        }

        /// <summary>A recessed balcony: concrete floor and sides, the flat's glazed doors at the back, a steel railing at the front.</summary>
        static void Balcony(MeshKit k, float xa, float xb, float ya, float yb, System.Random rng)
        {
            const float d = 1.2f;
            k.Quad(M.concrete, new Vector3(xa, ya, 0f), new Vector3(xb, ya, 0f), new Vector3(xb, ya, -d), new Vector3(xa, ya, -d), Vector3.up);
            k.Quad(M.concrete, new Vector3(xa, yb, 0f), new Vector3(xb, yb, 0f), new Vector3(xb, yb, -d), new Vector3(xa, yb, -d), Vector3.down);
            k.Quad(M.concrete, new Vector3(xa, ya, 0f), new Vector3(xa, yb, 0f), new Vector3(xa, yb, -d), new Vector3(xa, ya, -d), Vector3.right);
            k.Quad(M.concrete, new Vector3(xb, ya, 0f), new Vector3(xb, yb, 0f), new Vector3(xb, yb, -d), new Vector3(xb, ya, -d), Vector3.left);
            // back wall: panel, glazed doors
            float dh = ya + 2.1f;
            var uv = AtlasPatio();
            k.QuadUV(M.windows, new Vector3(xa + 0.2f, ya, -d), new Vector3(xb - 0.2f, ya, -d), new Vector3(xb - 0.2f, dh, -d), new Vector3(xa + 0.2f, dh, -d), Vector3.forward,
                new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMin, uv.yMax));
            k.Quad(M.towerPanel, new Vector3(xa, dh, -d), new Vector3(xb, dh, -d), new Vector3(xb, yb, -d), new Vector3(xa, yb, -d), Vector3.forward);
            k.Quad(M.towerPanel, new Vector3(xa, ya, -d), new Vector3(xa + 0.2f, ya, -d), new Vector3(xa + 0.2f, dh, -d), new Vector3(xa, dh, -d), Vector3.forward);
            k.Quad(M.towerPanel, new Vector3(xb - 0.2f, ya, -d), new Vector3(xb, ya, -d), new Vector3(xb, dh, -d), new Vector3(xb - 0.2f, dh, -d), Vector3.forward);
            // railing: solid lower panel (some with washing or a satellite dish behind), top rail
            k.Box(M.towerPanel, new Vector3(xa, ya, -0.08f), new Vector3(xb, ya + 0.55f, -0.02f));
            k.Box(M.lampCol, new Vector3(xa, ya + 1.0f, -0.09f), new Vector3(xb, ya + 1.05f, -0.01f));
            for (float x = xa + 0.25f; x < xb - 0.1f; x += 0.5f) k.Box(M.lampCol, new Vector3(x - 0.015f, ya + 0.55f, -0.07f), new Vector3(x + 0.015f, ya + 1.0f, -0.03f));
        }

        /// <summary>The way in: a deep recess with glazed double doors and side lights, lit inside.</summary>
        static void TowerEntrance(MeshKit k, float xa, float xb, float ya, float yb)
        {
            const float d = 1.6f;
            k.Quad(M.concrete, new Vector3(xa, yb, 0f), new Vector3(xb, yb, 0f), new Vector3(xb, yb, -d), new Vector3(xa, yb, -d), Vector3.down);
            k.Quad(M.concrete, new Vector3(xa, ya, 0f), new Vector3(xa, yb, 0f), new Vector3(xa, yb, -d), new Vector3(xa, ya, -d), Vector3.right);
            k.Quad(M.concrete, new Vector3(xb, ya, 0f), new Vector3(xb, yb, 0f), new Vector3(xb, yb, -d), new Vector3(xb, ya, -d), Vector3.left);
            k.Quad(M.step, new Vector3(xa, 0.06f, 0f), new Vector3(xb, 0.06f, 0f), new Vector3(xb, 0.06f, -d), new Vector3(xa, 0.06f, -d), Vector3.up);
            var uv = AtlasPatio();
            float dh = 2.3f;
            k.QuadUV(M.windows, new Vector3(xa, 0.06f, -d), new Vector3(xb, 0.06f, -d), new Vector3(xb, dh, -d), new Vector3(xa, dh, -d), Vector3.forward,
                new Vector2(uv.xMin + 0.06f, uv.yMin), new Vector2(uv.xMax - 0.06f, uv.yMin), new Vector2(uv.xMax - 0.06f, uv.yMax), new Vector2(uv.xMin + 0.06f, uv.yMax));
            k.Quad(M.concrete, new Vector3(xa, dh, -d), new Vector3(xb, dh, -d), new Vector3(xb, yb, -d), new Vector3(xa, yb, -d), Vector3.forward);
            k.Box(M.glow, new Vector3((xa + xb) * 0.5f - 0.4f, yb - 0.05f, -d * 0.6f), new Vector3((xa + xb) * 0.5f + 0.4f, yb, -d * 0.4f));
        }

        // ------------------------------------------------------------------------------------------ lock-up garages
        static void GarageRow(Transform parent)
        {
            var k = new MeshKit(); var ground = new MeshKit();
            var rng = new System.Random(808);
            float x0 = GaragesX, x1 = GaragesX + GarageCount * GarageW, zf = GaragesZ, zb = GaragesZ - GarageD;
            const float h = 2.45f, doorH = 2.05f, pier = 0.22f;
            // front: brick piers and lintel round a run of up-and-over doors
            var holes = new List<Rect>();
            for (int i = 0; i < GarageCount; i++)
            {
                float a = x0 + i * GarageW + pier * 0.5f, b = x0 + (i + 1) * GarageW - pier * 0.5f;
                holes.Add(new Rect(a, 0f, b - a, doorH));
                bool open = i == 5; // one left up, black inside
                var door = open ? M.black : M.garage[rng.Next(M.garage.Length)];
                Recess(k, a, b, 0f, doorH, open ? 4.8f : 0.1f, door, new Rect(0, 0, 1, 1), M.brick, true, zf);
                if (open) k.Quad(M.black, new Vector3(a, 0.01f, zf), new Vector3(b, 0.01f, zf), new Vector3(b, 0.01f, zf - 4.8f), new Vector3(a, 0.01f, zf - 4.8f), Vector3.up);
            }
            k.Push(new Vector3(0, 0, zf)); WallLocal(k, new Rect(x0, 0f, x1 - x0, h), holes); k.Pop();
            // back and ends in brick, felt roof with a fascia
            k.Quad(M.brick, new Vector3(x0, 0, zb), new Vector3(x1, 0, zb), new Vector3(x1, h, zb), new Vector3(x0, h, zb), Vector3.back);
            k.Quad(M.brick, new Vector3(x0, 0, zb), new Vector3(x0, h, zb), new Vector3(x0, h, zf), new Vector3(x0, 0, zf), Vector3.left);
            k.Quad(M.brick, new Vector3(x1, 0, zb), new Vector3(x1, h, zb), new Vector3(x1, h, zf), new Vector3(x1, 0, zf), Vector3.right);
            k.Box(M.felt, new Vector3(x0 - 0.12f, h, zb - 0.12f), new Vector3(x1 + 0.12f, h + 0.12f, zf + 0.2f));
            k.Box(M.white, new Vector3(x0 - 0.13f, h - 0.1f, zf + 0.2f), new Vector3(x1 + 0.13f, h + 0.13f, zf + 0.24f));
            var body = k.Build("LockUps", parent, false);
            var col = body.AddComponent<BoxCollider>(); col.center = new Vector3((x0 + x1) * 0.5f, h * 0.5f, (zf + zb) * 0.5f); col.size = new Vector3(x1 - x0, h, GarageD);
            ground.Box(M.asphalt, new Vector3(x0 - 0.5f, 0f, zf), new Vector3(x1 + 0.5f, 0.05f, zf + 7f), 16);
            var gg = ground.Build("EstateGround", parent, true); if (gg != null) gg.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>A brick wall in the current frame's z=0 plane with holes, merging runs (as <see cref="Wall"/>, front-facing).</summary>
        static void WallLocal(MeshKit k, Rect r, List<Rect> holes) => Wall(k, r, 0f, holes, true);

        // ------------------------------------------------------------------------------------------ playground
        static void PlayArea(Transform parent)
        {
            var k = new MeshKit(); var rail = new MeshKit();
            float x0 = Playground.xMin, x1 = Playground.xMax, z0 = Playground.yMin, z1 = Playground.yMax;
            // rubber safety surface
            k.Box(M.rubber, new Vector3(x0, 0f, z0), new Vector3(x1, 0.05f, z1));
            // bow-top railings (alpha-cut texture), with the gate gap facing the footpath
            const float rh = 1.0f; float gate0 = -57.2f, gate1 = -55.8f;
            void Run(Vector3 a, Vector3 b)
            {
                float len = Vector3.Distance(a, b); float t = MeshKit.TileOf(M.railing);
                var n = Vector3.Cross(Vector3.up, (b - a).normalized);
                rail.QuadUV(M.railing, a, b, b + Vector3.up * rh, a + Vector3.up * rh, n, new Vector2(0, 0), new Vector2(len / t, 0), new Vector2(len / t, 1), new Vector2(0, 1));
                int posts = Mathf.CeilToInt(len / 2f);
                for (int i = 0; i <= posts; i++) { var p = Vector3.Lerp(a, b, i / (float)posts); k.Box(RailPost(), p + new Vector3(-0.03f, 0, -0.03f), p + new Vector3(0.03f, rh + 0.05f, 0.03f)); }
            }
            Run(new Vector3(x0, 0, z0), new Vector3(x1, 0, z0)); Run(new Vector3(x0, 0, z1), new Vector3(x1, 0, z1)); Run(new Vector3(x0, 0, z0), new Vector3(x0, 0, z1));
            Run(new Vector3(x1, 0, z0), new Vector3(x1, 0, gate0)); Run(new Vector3(x1, 0, gate1), new Vector3(x1, 0, z1));
            // swings: A-frames, top bar, two seats on chains
            Vector3 s0 = new Vector3(x0 + 3.5f, 0.05f, z1 - 2.6f);
            foreach (float dx in new[] { -1.8f, 1.8f })
                foreach (float dz in new[] { -0.7f, 0.7f })
                    Leg(k, s0 + new Vector3(dx, 0, dz), s0 + new Vector3(dx, 2.35f, 0), M.playRed);
            k.CylX(M.playYellow, s0 + new Vector3(0, 2.38f, 0), 0.06f, 3.8f, 8);
            foreach (float dx in new[] { -0.8f, 0.8f })
            {
                foreach (float dz in new[] { -0.18f, 0.18f }) k.Box(M.black, s0 + new Vector3(dx - 0.008f, 0.48f, dz - 0.008f), s0 + new Vector3(dx + 0.008f, 2.35f, dz + 0.008f));
                k.Box(M.black, s0 + new Vector3(dx - 0.22f, 0.42f, -0.2f), s0 + new Vector3(dx + 0.22f, 0.48f, 0.2f));
            }
            // slide: ladder to a platform, chute down the far side
            Vector3 sl = new Vector3(x1 - 3.2f, 0.05f, z1 - 3.0f);
            foreach (float dx in new[] { -0.3f, 0.3f }) foreach (float dz in new[] { -0.3f, 0.3f }) k.Box(M.playBlue, sl + new Vector3(dx - 0.04f, 0, dz - 0.04f), sl + new Vector3(dx + 0.04f, 1.5f, dz + 0.04f));
            k.Box(M.playYellow, sl + new Vector3(-0.38f, 1.45f, -0.38f), sl + new Vector3(0.38f, 1.52f, 0.38f));
            for (int r = 0; r < 5; r++) k.Box(M.playBlue, sl + new Vector3(-0.28f, 0.3f + r * 0.28f, 0.42f + r * 0.06f), sl + new Vector3(0.28f, 0.33f + r * 0.28f, 0.46f + r * 0.06f));
            foreach (float dx in new[] { -0.3f, 0.3f }) Leg(k, sl + new Vector3(dx, 0, 0.95f), sl + new Vector3(dx, 1.5f, 0.35f), M.playBlue);
            k.Push(Matrix4x4.TRS(sl + new Vector3(0, 0.78f, -1.75f), Quaternion.Euler(-28f, 0, 0), Vector3.one));
            k.Box(M.lampCol, new Vector3(-0.3f, -0.02f, -1.6f), new Vector3(0.3f, 0.02f, 1.6f));
            foreach (float dx in new[] { -0.32f, 0.32f }) k.Box(M.playRed, new Vector3(dx - 0.02f, 0f, -1.6f), new Vector3(dx + 0.02f, 0.18f, 1.6f));
            k.Pop();
            // roundabout, see-saw, springer
            Vector3 rb = new Vector3(x0 + 4f, 0.05f, z0 + 3.5f);
            k.Cyl(M.lampCol, rb, 0.08f, 0.35f, 8); k.Cyl(M.playRed, rb + Vector3.up * 0.3f, 1.1f, 0.06f, 16);
            for (int i = 0; i < 4; i++) { var dir = Quaternion.Euler(0, i * 90f + 45f, 0) * Vector3.forward; Leg(k, rb + Vector3.up * 0.36f + dir * 0.85f, rb + Vector3.up * 0.95f + dir * 0.25f, M.playYellow); }
            Vector3 ss = new Vector3(x1 - 4f, 0.05f, z0 + 3f);
            k.Box(M.playRed, ss + new Vector3(-0.15f, 0, -0.12f), ss + new Vector3(0.15f, 0.45f, 0.12f));
            k.Push(Matrix4x4.TRS(ss + Vector3.up * 0.5f, Quaternion.Euler(0, 0, 8f), Vector3.one));
            k.Box(M.playYellow, new Vector3(-1.8f, -0.03f, -0.13f), new Vector3(1.8f, 0.03f, 0.13f));
            foreach (float x in new[] { -1.4f, 1.4f }) k.Box(M.playBlue, new Vector3(x - 0.02f, 0.03f, -0.15f), new Vector3(x + 0.02f, 0.3f, 0.15f));
            k.Pop();
            Vector3 sp = new Vector3((x0 + x1) * 0.5f + 1f, 0.05f, z0 + 6.5f);
            k.Cyl(M.black, sp, 0.1f, 0.45f, 10); k.Sphere(M.playYellow, sp + Vector3.up * 0.7f, new Vector3(0.22f, 0.25f, 0.4f), 10, 6);
            k.Sphere(M.playYellow, sp + new Vector3(0, 0.98f, 0.32f), new Vector3(0.14f, 0.15f, 0.16f), 8, 5);
            // benches and a bin just outside the railings, by the gate
            foreach (float z in new[] { z1 - 1.5f, z0 + 1.5f })
            {
                Vector3 b = new Vector3(x1 + 1.6f, 0f, z);
                foreach (float dz in new[] { -0.7f, 0.7f }) k.Box(M.concrete, b + new Vector3(-0.25f, 0, dz - 0.05f), b + new Vector3(0.25f, 0.42f, dz + 0.05f));
                for (int sl2 = 0; sl2 < 3; sl2++) k.Box(M.fence, b + new Vector3(-0.24f + sl2 * 0.17f, 0.42f, -0.9f), b + new Vector3(-0.1f + sl2 * 0.17f, 0.46f, 0.9f));
                k.Box(M.fence, b + new Vector3(0.22f, 0.55f, -0.9f), b + new Vector3(0.26f, 0.85f, 0.9f));
            }
            Vector3 bin = new Vector3(x1 + 0.9f, 0f, gate1 + 1.2f);
            k.Box(M.black, bin + new Vector3(-0.03f, 0, -0.03f), bin + new Vector3(0.03f, 0.9f, 0.03f));
            k.Cyl(M.playRed, bin + new Vector3(0, 0.45f, 0.2f), 0.18f, 0.5f, 10);
            var body = k.Build("Playground", parent, false);
            foreach (var (c, s) in new[] { (s0 + new Vector3(-1.8f, 1.2f, 0), new Vector3(0.2f, 2.4f, 1.6f)), (s0 + new Vector3(1.8f, 1.2f, 0), new Vector3(0.2f, 2.4f, 1.6f)),
                                           (sl + new Vector3(0, 0.75f, 0), new Vector3(0.8f, 1.5f, 0.8f)), (rb + Vector3.up * 0.2f, new Vector3(2.2f, 0.4f, 2.2f)) })
            { var bc = body.AddComponent<BoxCollider>(); bc.center = c; bc.size = s; }
            var rg = rail.Build("PlaygroundRailings", parent, false);
            if (rg != null)
            {
                rg.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                void Side(Vector3 a, Vector3 b) { var bc = rg.AddComponent<BoxCollider>(); bc.center = (a + b) * 0.5f + Vector3.up * 0.5f; var d = b - a; bc.size = new Vector3(Mathf.Abs(d.x) + 0.08f, 1f, Mathf.Abs(d.z) + 0.08f); }
                Side(new Vector3(x0, 0, z0), new Vector3(x1, 0, z0)); Side(new Vector3(x0, 0, z1), new Vector3(x1, 0, z1)); Side(new Vector3(x0, 0, z0), new Vector3(x0, 0, z1));
                Side(new Vector3(x1, 0, z0), new Vector3(x1, 0, gate0)); Side(new Vector3(x1, 0, gate1), new Vector3(x1, 0, z1));
            }
        }

        static Material RailPost() => M.railGreen != null ? M.railGreen : M.post;

        /// <summary>A round steel tube between two points (swing frames, slide legs, roundabout handles).</summary>
        static void Leg(MeshKit k, Vector3 a, Vector3 b, Material m)
        {
            var d = b - a; float len = d.magnitude; if (len < 0.01f) return;
            k.Push(Matrix4x4.TRS(a, Quaternion.FromToRotation(Vector3.up, d / len), Vector3.one));
            k.Cyl(m, Vector3.zero, 0.045f, len, 8);
            k.Pop();
        }
    }
}

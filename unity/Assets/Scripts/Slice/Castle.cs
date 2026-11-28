using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Wend Castle: a crumbling ruin on its own hill south-west of the town, up a winding lane off the main road.
    /// An octagonal curtain wall with broken battlements and a breach on the far side, three round towers (the fourth
    /// fallen to a stump), a gatehouse with a raised portcullis, a tall square keep with arrow slits, corner turrets and
    /// stone steps up to its door, and a crypt entrance going down into the dark. Crows circle the keep. Built into the
    /// scene by the editor with the estate (the hill and the lane too); <see cref="Crows"/> start at Play.
    /// </summary>
    public static class Castle
    {
        public const string RootName = "WendCastle";
        public static readonly Vector3 Centre = new Vector3(-175f, 0f, -118f);
        public const float HillTop = 18f, Plateau = 27f, HillFoot = 72f, WallR = 20f, WallH = 7f, WallT = 1.8f;
        /// <summary>The lane up from the main road, as waypoints on the ground (heights come from the terrain).</summary>
        public static readonly Vector2[] Lane = { new Vector2(-152.5f, -37.8f), new Vector2(-149.5f, -56f), new Vector2(-153f, -70f), new Vector2(-139f, -84f), new Vector2(-151f, -95f), new Vector2(-160.5f, -101.5f) };

        class Mats { public Material stone, stone2, dark, iron, gravel, grass, timber; }
        static Mats M;
        static Material L(string n) => Resources.Load<Material>("Castle/" + n);

        /// <summary>Height of the hill above the surrounding ground at (x, z): a flat top, smooth flanks.</summary>
        public static float HillHeight(float x, float z)
        {
            float d = Vector2.Distance(new Vector2(x, z), new Vector2(Centre.x, Centre.z));
            if (d <= Plateau) return HillTop;
            if (d >= HillFoot) return 0f;
            float t = 1f - (d - Plateau) / (HillFoot - Plateau);
            return HillTop * t * t * (3f - 2f * t);
        }

        public static Transform Build(Transform parent, Terrain terrain)
        {
            M = new Mats { stone = L("CastleStone"), stone2 = L("CastleStone2"), dark = L("CastleDark"), iron = L("CastleIron"), gravel = L("CastleGravel"), grass = L("CastleGrass"), timber = Resources.Load<Material>("Estate/EstateFence") };
            if (M.stone == null) { Debug.LogWarning("[ASBO] castle: materials missing"); return null; }
            MeshKit.Tile[M.stone] = 2f; MeshKit.Tile[M.stone2] = 1.5f; if (M.gravel != null) MeshKit.Tile[M.gravel] = 2f;
            var root = new GameObject(RootName).transform; root.SetParent(parent, false);
            float y = HillTop + (terrain != null ? terrain.transform.position.y : 0f);
            var c = new Vector3(Centre.x, y, Centre.z);
            var rng = new System.Random(1066);
            Walls(root, c, rng); Keep(root, c + new Vector3(-2f, 0, -5f)); Crypt(root, c + new Vector3(9f, 0, -3f), terrain);
            if (terrain != null) LaneMesh(root, terrain);
            Debug.Log("[ASBO] castle: built");
            return root;
        }

        static Vector3 Corner(Vector3 c, int i, float r) { float a = (i * 45f + 22.5f) * Mathf.Deg2Rad; return c + new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r); }

        // ------------------------------------------------------------------------------------------ curtain wall, towers, gatehouse
        static void Walls(Transform root, Vector3 c, System.Random rng)
        {
            var k = new MeshKit();
            // eight sides: side 0 runs from corner 0 to corner 1 (north-east, the gate side); side 4 (south-west) is breached
            for (int i = 0; i < 8; i++)
            {
                Vector3 a = Corner(c, i, WallR), b = Corner(c, i + 1, WallR);
                if (i == 0) { Gatehouse(k, a, b, c); continue; }
                bool breach = i == 4, low = i == 5 || i == 3;
                WallRun(k, a, b, c, breach, low ? 0.6f : 1f, rng);
            }
            // round towers on four corners away from the gate (the one by the breach fallen to a stump)
            foreach (int i in new[] { 2, 4, 5, 7 })
            {
                var p = Corner(c, i, WallR);
                bool ruined = i == 5;
                Tower(k, p, 3.4f, ruined ? 4.5f : 11f, !ruined, rng);
            }
            // rubble in the breach and round the stump
            var br = (Corner(c, 4, WallR) + Corner(c, 5, WallR)) * 0.5f;
            for (int i = 0; i < 9; i++)
            {
                var p = br + new Vector3((float)rng.NextDouble() * 8f - 4f, 0f, (float)rng.NextDouble() * 6f - 3f);
                float s = 0.4f + (float)rng.NextDouble() * 0.8f;
                k.Push(Matrix4x4.TRS(p, Quaternion.Euler((float)rng.NextDouble() * 30f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 30f), Vector3.one));
                k.Box(M.stone2, new Vector3(-s, -s * 0.4f, -s * 0.7f), new Vector3(s, s * 0.5f, s * 0.7f));
                k.Pop();
            }
            var g = k.Build("CastleWalls", root, true);
        }

        /// <summary>A length of curtain wall from a to b: thick stone, a wall-walk, battlements with gaps where merlons have fallen.</summary>
        static void WallRun(MeshKit k, Vector3 a, Vector3 b, Vector3 c, bool breach, float heightK, System.Random rng)
        {
            Vector3 d = b - a; float len = d.magnitude; var dir = d / len; var outw = Vector3.Cross(Vector3.up, dir); if (Vector3.Dot(outw, (a + b) * 0.5f - c) < 0) outw = -outw;
            float h = WallH * heightK;
            // break the run into chunks so the top can be ragged (and the breach can drop to rubble)
            int n = Mathf.CeilToInt(len / 2.4f);
            for (int s = 0; s < n; s++)
            {
                float t0 = s / (float)n, t1 = (s + 1) / (float)n;
                float tm = (t0 + t1) * 0.5f;
                float hh = h;
                if (breach && tm > 0.3f && tm < 0.75f) hh = 0.6f + (float)rng.NextDouble() * 1.2f;
                else if (rng.NextDouble() < 0.18) hh = h - 0.8f - (float)rng.NextDouble() * 1.5f;
                var p0 = a + d * t0; var p1 = a + d * t1;
                Slab(k, p0, p1, outw, 0f, hh, WallT, M.stone);
                // merlons on the outer edge of the wall-walk
                if (hh >= h - 0.01f)
                    for (float m = 0.2f; m < (t1 - t0) * len - 0.6f; m += 1.4f)
                    {
                        if (rng.NextDouble() < 0.22) continue;
                        var q0 = p0 + dir * m; var q1 = q0 + dir * 0.8f;
                        Slab(k, q0 + outw * (WallT * 0.5f - 0.3f), q1 + outw * (WallT * 0.5f - 0.3f), outw, hh, hh + 0.9f, 0.6f, M.stone);
                    }
            }
        }

        /// <summary>A straight block from p0 to p1 along the ground, centred on the line, offset by nothing, from y0 to y1, thickness t.</summary>
        static void Slab(MeshKit k, Vector3 p0, Vector3 p1, Vector3 outw, float y0, float y1, float t, Material m)
        {
            Vector3 dir = (p1 - p0).normalized; float len = Vector3.Distance(p0, p1);
            var mid = (p0 + p1) * 0.5f;
            k.Push(Matrix4x4.TRS(mid, Quaternion.LookRotation(outw, Vector3.up), Vector3.one));
            // local: x along the wall (dir is local -x or +x; use extents), z outwards
            k.Box(m, new Vector3(-len / 2 - 0.02f, y0, -t / 2), new Vector3(len / 2 + 0.02f, y1, t / 2));
            k.Pop();
        }

        /// <summary>A round tower: a stone drum with battlements (or a broken top), arrow slits.</summary>
        static void Tower(MeshKit k, Vector3 p, float r, float h, bool battlements, System.Random rng)
        {
            k.Cyl(M.stone2, p, r, h, 14, !battlements);
            if (battlements)
            {
                k.Cyl(M.stone2, p + Vector3.up * h, r + 0.25f, 0.4f, 14, true);
                for (int i = 0; i < 10; i++)
                {
                    if (rng.NextDouble() < 0.2) continue;
                    float a = i * 36f * Mathf.Deg2Rad; var q = p + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * (r - 0.05f) + Vector3.up * (h + 0.4f);
                    k.Push(Matrix4x4.TRS(q, Quaternion.Euler(0, i * 36f, 0), Vector3.one));
                    k.Box(M.stone2, new Vector3(-0.45f, 0f, -0.3f), new Vector3(0.45f, 0.95f, 0.3f));
                    k.Pop();
                }
            }
            else
            {
                // jagged broken top
                for (int i = 0; i < 7; i++)
                {
                    float a = (i * 51f + 10f) * Mathf.Deg2Rad; var q = p + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * (r - 0.4f) + Vector3.up * h;
                    k.Push(Matrix4x4.TRS(q, Quaternion.Euler(0, i * 51f, 0), Vector3.one));
                    k.Box(M.stone2, new Vector3(-0.7f, 0f, -0.4f), new Vector3(0.7f, 0.4f + (float)rng.NextDouble() * 2.2f, 0.4f));
                    k.Pop();
                }
            }
            for (int s = 0; s < 4; s++)
            {
                float a = (s * 90f + 45f) * Mathf.Deg2Rad; var q = p + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * (r + 0.01f);
                if (h < 6f) continue;
                k.Push(Matrix4x4.TRS(q + Vector3.up * (h * 0.6f), Quaternion.LookRotation(new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a))), Vector3.one));
                k.Box(M.dark, new Vector3(-0.08f, -0.6f, -0.02f), new Vector3(0.08f, 0.6f, 0.02f));
                k.Pop();
            }
        }

        /// <summary>The gatehouse across side 0: two flanking drum towers, a pointed archway with a raised portcullis, battlements over.</summary>
        static void Gatehouse(MeshKit k, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 d = b - a; float len = d.magnitude; var dir = d / len; var outw = Vector3.Cross(Vector3.up, dir); if (Vector3.Dot(outw, (a + b) * 0.5f - c) < 0) outw = -outw;
            var mid = (a + b) * 0.5f; const float gw = 3.6f, gh = 4.2f;
            // wall either side of the gate passage
            Slab(k, a, mid - dir * (gw / 2 + 2.6f), outw, 0f, WallH, WallT, M.stone);
            Slab(k, mid + dir * (gw / 2 + 2.6f), b, outw, 0f, WallH, WallT, M.stone);
            // the gate block: thicker, taller, with the passage through it
            k.Push(Matrix4x4.TRS(mid, Quaternion.LookRotation(outw, Vector3.up), Vector3.one));
            float bw = gw / 2 + 2.6f, depth = 5f, H = 9f;
            k.Box(M.stone, new Vector3(-bw, 0f, -depth / 2), new Vector3(-gw / 2, H, depth / 2));
            k.Box(M.stone, new Vector3(gw / 2, 0f, -depth / 2), new Vector3(bw, H, depth / 2));
            k.Box(M.stone, new Vector3(-gw / 2, gh + 0.9f, -depth / 2), new Vector3(gw / 2, H, depth / 2));
            // pointed arch head inside the opening
            for (int s = 0; s < 6; s++)
            {
                float t = s / 6f, x = Mathf.Lerp(-gw / 2, 0f, t), yy = gh + Mathf.Sin(t * Mathf.PI * 0.5f) * 0.9f;
                k.Box(M.stone, new Vector3(x, yy, -depth / 2), new Vector3(x + gw / 12f + 0.02f, gh + 0.9f, depth / 2));
                k.Box(M.stone, new Vector3(-x - gw / 12f - 0.02f, yy, -depth / 2), new Vector3(-x, gh + 0.9f, depth / 2));
            }
            // portcullis raised into the slot, its spikes just showing
            for (float x = -gw / 2 + 0.2f; x < gw / 2; x += 0.35f) k.Box(M.iron, new Vector3(x - 0.04f, gh - 0.5f, 0.8f), new Vector3(x + 0.04f, gh + 0.9f, 0.88f));
            k.Box(M.iron, new Vector3(-gw / 2, gh - 0.3f, 0.78f), new Vector3(gw / 2, gh - 0.22f, 0.9f));
            // battlements along the front of the gate block
            for (float x = -bw + 0.3f; x < bw - 0.6f; x += 1.3f) k.Box(M.stone, new Vector3(x, H, depth / 2 - 0.6f), new Vector3(x + 0.8f, H + 1f, depth / 2));
            k.Pop();
            foreach (int s in new[] { -1, 1 }) Tower(k, mid + dir * s * (bw + 0.6f) + outw * 1.6f, 2.6f, 10f, true, new System.Random(7 + s));
            // floor of the passage
            k.Push(Matrix4x4.TRS(mid, Quaternion.LookRotation(outw, Vector3.up), Vector3.one));
            k.Box(M.gravel != null ? M.gravel : M.stone2, new Vector3(-gw / 2, -0.2f, -depth / 2 - 1f), new Vector3(gw / 2, 0.05f, depth / 2 + 3f), 0);
            k.Pop();
        }

        // ------------------------------------------------------------------------------------------ keep
        static void Keep(Transform root, Vector3 p)
        {
            var k = new MeshKit();
            const float w = 12f, h = 17f;
            Vector3 a = p + new Vector3(-w / 2, 0, -w / 2), b = p + new Vector3(w / 2, h, w / 2);
            k.Box(M.stone, a, b, 4 | 32);
            // the north face in pieces round the first-floor doorway
            {
                float zf = w / 2, dx0 = -w / 2 + 5.6f, dx1 = -w / 2 + 7.0f;
                void Fq(float xa, float xb, float ya, float yb) { if (xb - xa > 0.01f && yb - ya > 0.01f) k.Quad(M.stone, p + new Vector3(xa, ya, zf), p + new Vector3(xb, ya, zf), p + new Vector3(xb, yb, zf), p + new Vector3(xa, yb, zf), Vector3.forward); }
                Fq(-w / 2, dx0, 0f, h); Fq(dx1, w / 2, 0f, h); Fq(dx0, dx1, 0f, 4.0f); Fq(dx0, dx1, 6.4f, h);
            }
            // a plinth that splays out at the bottom, string courses
            k.Box(M.stone2, p + new Vector3(-w / 2 - 0.5f, 0, -w / 2 - 0.5f), p + new Vector3(w / 2 + 0.5f, 1.2f, w / 2 + 0.5f), 4);
            foreach (float yy in new[] { 6f, 11.5f }) k.Box(M.stone2, p + new Vector3(-w / 2 - 0.12f, yy, -w / 2 - 0.12f), p + new Vector3(w / 2 + 0.12f, yy + 0.3f, w / 2 + 0.12f), 4);
            // corner turrets rising above the parapet
            foreach (var s in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
            {
                var q = p + new Vector3(s.x * (w / 2 - 0.6f), 0, s.y * (w / 2 - 0.6f));
                k.Box(M.stone, q + new Vector3(-1.4f, h - 2f, -1.4f), q + new Vector3(1.4f, h + 3.2f, 1.4f), 4);
                for (int m = 0; m < 4; m++) { var o = new Vector3(m % 2 == 0 ? -1.1f : 0.5f, 0, m < 2 ? -1.1f : 0.5f); k.Box(M.stone, q + o + new Vector3(0, h + 3.2f, 0), q + o + new Vector3(0.6f, h + 4f, 0.6f)); }
            }
            // battlements round the top
            for (float x = -w / 2 + 2f; x < w / 2 - 2f; x += 1.3f)
                foreach (int s in new[] { -1, 1 })
                {
                    k.Box(M.stone, p + new Vector3(x, h, s * (w / 2) - 0.3f), p + new Vector3(x + 0.7f, h + 1f, s * (w / 2) + 0.3f));
                    k.Box(M.stone, p + new Vector3(s * (w / 2) - 0.3f, h, x), p + new Vector3(s * (w / 2) + 0.3f, h + 1f, x + 0.7f));
                }
            // arrow slits and a couple of larger upper windows, all dark
            for (int f = 0; f < 3; f++)
                for (int i = -1; i <= 1; i++)
                {
                    float yy = 3f + f * 5f, xx = i * 3.2f; bool big = f == 2 && i == 0;
                    Vector3 hs = big ? new Vector3(0.5f, 1.1f, 0.03f) : new Vector3(0.09f, 0.75f, 0.03f);
                    k.Box(M.dark, p + new Vector3(xx - hs.x, yy - hs.y, w / 2), p + new Vector3(xx + hs.x, yy + hs.y, w / 2 + hs.z));
                    k.Box(M.dark, p + new Vector3(xx - hs.x, yy - hs.y, -w / 2 - hs.z), p + new Vector3(xx + hs.x, yy + hs.y, -w / 2));
                    k.Box(M.dark, p + new Vector3(w / 2, yy - hs.y, xx - hs.x), p + new Vector3(w / 2 + hs.z, yy + hs.y, xx + hs.x));
                    k.Box(M.dark, p + new Vector3(-w / 2 - hs.z, yy - hs.y, xx - hs.x), p + new Vector3(-w / 2, yy + hs.y, xx + hs.x));
                }
            // stone steps up the north side to the first-floor door (the door itself dark, a heavy timber door ajar)
            for (int s = 0; s < 14; s++)
                k.Box(M.stone2, p + new Vector3(-w / 2 + 1f + s * 0.32f, 0f, w / 2 + 0.6f), p + new Vector3(-w / 2 + 1.32f + s * 0.32f, 0.3f + s * 0.27f, w / 2 + 2.2f));
            k.Box(M.stone2, p + new Vector3(-w / 2 + 5.4f, 0f, w / 2 + 0.6f), p + new Vector3(-w / 2 + 7.2f, 4.0f, w / 2 + 2.2f));
            // (the door itself is the modelled iron-bound pair, hung by Dressing.Kits)
            var g = k.Build("CastleKeep", root, false);
            // the steps: one collider per step so you can walk up them
            for (int s = 0; s < 14; s++) { var sc = g.AddComponent<BoxCollider>(); sc.center = p + new Vector3(-w / 2 + 1.16f + s * 0.32f, (0.3f + s * 0.27f) * 0.5f, w / 2 + 1.4f); sc.size = new Vector3(0.32f, 0.3f + s * 0.27f, 1.6f); }
            var tp = g.AddComponent<BoxCollider>(); tp.center = p + new Vector3(-w / 2 + 6.3f, 2.0f, w / 2 + 1.4f); tp.size = new Vector3(1.8f, 4.0f, 1.6f);
            KeepInside(root, p, w, h);
        }

        /// <summary>
        /// Inside the keep: walls a metre and a half thick, the undercroft filled with rubble, the hall on the first floor
        /// (through the door at the top of the steps) with its fireplace, rotten table and somebody's camp, a timber stair up
        /// the west wall to the solar on the second floor, half its floor fallen through, and the sky showing through the roof.
        /// </summary>
        static void KeepInside(Transform root, Vector3 p, float w, float h)
        {
            TownMats.Load(); CotMats.Init();
            var b = new Bld(root, "Keep Inside"); const float t = 1.5f; float i0 = -w / 2 + t, i1 = w / 2 - t;
            Vector3 P(float x, float y, float z) => p + new Vector3(x, y, z);
            var stone = M.stone; var wood = M.timber ?? M.stone2; var dark = M.dark;
            // the thick walls: inner faces (the outside box is one-sided), solid through
            float dx0 = -w / 2 + 5.6f, dx1 = -w / 2 + 7.0f;
            void Face(Vector3 a0, Vector3 a1, Vector3 a2, Vector3 a3, Vector3 n) => b.inK.Quad(stone, a0, a1, a2, a3, n);
            Face(P(i0, 4f, i1), P(i1, 4f, i1), P(i1, h, i1), P(i0, h, i1), Vector3.back);   // north (the door cut below)
            Face(P(i0, 4f, i0), P(i1, 4f, i0), P(i1, h, i0), P(i0, h, i0), Vector3.forward);
            Face(P(i0, 4f, i0), P(i0, 4f, i1), P(i0, h, i1), P(i0, h, i0), Vector3.right);
            Face(P(i1, 4f, i0), P(i1, 4f, i1), P(i1, h, i1), P(i1, h, i0), Vector3.left);
            b.Solid(P(-w / 2, 0f, i1), P(dx0, h, w / 2)); b.Solid(P(dx1, 0f, i1), P(w / 2, h, w / 2)); b.Solid(P(dx0, 0f, i1), P(dx1, 4.0f, w / 2)); b.Solid(P(dx0, 6.4f, i1), P(dx1, h, w / 2));
            b.Solid(P(-w / 2, 0f, -w / 2), P(w / 2, h, i0)); b.Solid(P(-w / 2, 0f, i0), P(i0, h, i1)); b.Solid(P(i1, 0f, i0), P(w / 2, h, i1));
            // the doorway through the wall: reveals and a flagged threshold
            b.inK.Quad(stone, P(dx0, 4f, w / 2), P(dx0, 4f, i1), P(dx0, 6.4f, i1), P(dx0, 6.4f, w / 2), Vector3.right);
            b.inK.Quad(stone, P(dx1, 4f, i1), P(dx1, 4f, w / 2), P(dx1, 6.4f, w / 2), P(dx1, 6.4f, i1), Vector3.left);
            b.inK.Quad(stone, P(dx0, 6.4f, w / 2), P(dx1, 6.4f, w / 2), P(dx1, 6.4f, i1), P(dx0, 6.4f, i1), Vector3.down);
            b.inK.Quad(M.stone2, P(dx0, 4.0f, i1), P(dx1, 4.0f, i1), P(dx1, 4.0f, w / 2), P(dx0, 4.0f, w / 2), Vector3.up);
            // the undercroft is rubble: the hall's floor is flags on top of it
            b.Box(M.stone2, P(i0, 0f, i0), P(i1, 4.0f, i1), true);
            // arrow slits through the walls: dark slots on the inner faces
            for (int f = 0; f < 2; f++) foreach (float xx in new[] { -3.2f, 0f, 3.2f })
                {
                    float yy = 8f + f * 5f; if (yy > h - 1f) continue;
                    b.furnK.Box(dark, P(xx - 0.12f, yy - 0.7f, i0 + 0.01f), P(xx + 0.12f, yy + 0.7f, i0 + 0.02f));
                    b.furnK.Box(dark, P(i1 - 0.02f, yy - 0.7f, xx - 0.12f), P(i1 - 0.01f, yy + 0.7f, xx + 0.12f));
                }
            // the solar floor at 9.5 m: timber on joists, the middle fallen through, the stair coming up the west wall
            const float y2 = 9.5f;
            b.Box(wood, P(i0, y2 - 0.2f, 0.6f), P(i1, y2, i1), true);                    // north part
            b.Box(wood, P(i0 + 1.1f, y2 - 0.2f, i0), P(i0 + 3.2f, y2, 0.6f), true);       // a strip along the stair's top
            b.Box(wood, P(i1 - 2.2f, y2 - 0.2f, i0), P(i1, y2, 0.6f), true);
            for (int j = 0; j < 6; j++) b.furnK.Box(wood, P(i0, y2 - 0.45f, i0 + 0.4f + j * 1.5f), P(i1, y2 - 0.2f, i0 + 0.6f + j * 1.5f));
            b.Box(wood, P(i0 + 3.2f, y2, 0.55f), P(i1 - 2.2f, y2 + 1.0f, 0.6f), true);   // a rail along the broken edge
            // the stair: timber treads up the west wall from the door end (north) to the south, then onto the solar
            int n = 21; float rise = (y2 - 4f) / n;
            for (int s = 1; s <= n; s++)
            {
                float z1 = i1 - 0.8f - (s - 1) * 0.3f, z0 = z1 - 0.3f, ty = 4f + s * rise;
                b.Box(M.stone2, P(i0, 4f, z0), P(i0 + 1.0f, ty, z1), true);                                       // solid masonry steps
                b.Box(wood, P(i0 + 0.97f, ty, z0), P(i0 + 1.03f, ty + 0.95f, z1), true);                         // the hand rail, stepping up with them
            }
            // the roof: a timber ceiling with daylight showing through the holes
            b.inK.Quad(wood, P(i0, h - 0.6f, i0), P(i1, h - 0.6f, i0), P(i1, h - 0.6f, i1), P(i0, h - 0.6f, i1), Vector3.down);
            // the hall: a fireplace in the east wall, a rotten table, squatters' camp
            b.Box(M.stone2, P(i1 - 0.4f, 4f, -1.2f), P(i1, 6.2f, 1.2f), true); b.furnK.Box(dark, P(i1 - 0.41f, 4f, -0.8f), P(i1 - 0.4f, 5.4f, 0.8f));
            for (int a = 0; a < 5; a++) b.furnK.Box(HomeBuilder.Flat(new Color(0.25f, 0.2f, 0.18f), 0.1f), P(i1 - 1.2f + a * 0.1f, 4.01f, -0.3f + a * 0.12f), P(i1 - 1.0f + a * 0.1f, 4.03f, -0.2f + a * 0.12f));
            b.Table(P(-0.5f, 4f, -1.5f), 2.4f, 0.9f, 0.76f, wood);
            b.furnK.Box(HomeBuilder.Flat(new Color(0.2f, 0.35f, 0.25f), 0.1f), P(1.0f, 4f, -3.5f), P(2.9f, 4.12f, -2.7f));   // a sleeping bag
            for (int c = 0; c < 6; c++) b.furnK.Cyl(HomeBuilder.Flat(Color.HSVToRGB(c * 0.17f, 0.6f, 0.7f), 0.7f), P(0.4f + c * 0.25f, 4f, -3.9f + (c % 2) * 0.2f), 0.033f, 0.12f, 8);
            b.Box(wood, P(-3.5f, 4f, 2.6f), P(-2.4f, 4.7f, 3.3f), true);   // an old chest
            b.Light(P(0f, 7f, 0f), new Color(0.9f, 0.85f, 0.75f), 10f); b.Light(P(2f, 5.5f, -2f), new Color(1f, 0.7f, 0.45f), 6f); b.Light(P(0f, 12f, 0f), new Color(0.85f, 0.88f, 1f), 8f);
            b.Done();
            var home = b.Root.gameObject.AddComponent<Home>();
            var hh = Household.Make(77123, new System.Random(77)); hh.kind = Household.Kind.Hoarder; hh.wealth = 0.2f; hh.stuff = 0.8f; hh.family = "Wend Castle";
            home.H = hh; home.label = "Wend Castle keep"; home.variant = "hall";
            void Sp(string kind, string label, string tag, Vector3 at, bool hidden = false, float seat = 0f) => home.spots.Add(new Home.SpotRec { kind = kind, label = label, tag = tag, pos = at, yaw = 0f, room = 0, lv = 0, hidden = hidden, h = seat });
            Sp("rummage", "Open the old chest", "cabinet", P(-2.95f, 4f, 1.9f));
            Sp("rummage", "Go through the sleeping bag", "bag", P(1.9f, 4f, -2.2f), true);
            Sp("say", "Warm your hands at the fireplace", "Cold ash, a burnt shopping trolley wheel and a lot of history.", P(i1 - 1.3f, 4f, 0f));
            Sp("say", "Look through the arrow slit", "The whole town laid out below. You can see your house from here. Probably.", P(0f, y2, i0 + 0.6f));
            Sp("sit", "Sit at the old table", "chair", P(-0.5f, 4f, -0.7f), false, 0.45f);
            home.rooms.Add(new Home.RoomRec { type = "hall", use = "keep", lv = 0, centre = P(0f, 4f, 0f), u0 = i0, u1 = i1, d0 = i0, d1 = i1 });
            home.zoneRects.Add(Rect.MinMaxRect(p.x + i0, p.z + i0, p.x + i1, p.z + i1)); home.centre = P(0f, 5.5f, 0f); home.radius = 16f; home.midZ = p.z; home.halfD = w / 2 - t;
        }

        /// <summary>
        /// The crypt: a low stone hood over a steep flight down into a vaulted chamber under the courtyard (the ground is dug out
        /// beneath and flagged over). Two tombs, a squatter's corner, inscriptions; the tomb lids heave aside.
        /// Local frame at p: the hood opens north (+z), the steps go down southward, the chamber lies east of the foot of them.
        /// </summary>
        static void Crypt(Transform root, Vector3 p, Terrain terrain)
        {
            var k = new MeshKit(); var dk = new MeshKit(); var g = new GameObject("Castle Crypt").transform; g.SetParent(root, false); g.position = p;
            var cols = new List<(Vector3 mn, Vector3 mx)>();
            void B(Material m, Vector3 a, Vector3 b, bool col = true) { k.Box(m, a, b); if (col) cols.Add((Vector3.Min(a, b), Vector3.Max(a, b))); }
            const float fl = -3.0f, ceil = -0.3f, sw = 1.3f, st = 0.6f, zTop = 1.2f, zLand = -3.0f, zBack = -3.6f;
            float cx0 = sw + st, cx1 = 4.9f, cz0 = zBack, cz1 = 3.6f;      // the chamber's inside
            // dig the ground out under it all (a cell's margin so no slope pokes through), then flag the courtyard over the top
            if (terrain != null)
            {
                var td = terrain.terrainData; Vector3 tp = terrain.transform.position; int res = td.heightmapResolution; float cell = td.size.x / (res - 1);
                float rx0 = -sw - st - cell - 0.1f, rx1 = cx1 + st + cell + 0.1f, rz0 = zBack - 0.4f - cell - 0.1f, rz1 = cz1 + st + cell + 0.1f;
                int x0 = Mathf.CeilToInt((p.x + rx0 - tp.x) / cell), x1 = Mathf.FloorToInt((p.x + rx1 - tp.x) / cell), z0 = Mathf.CeilToInt((p.z + rz0 - tp.z) / cell), z1 = Mathf.FloorToInt((p.z + rz1 - tp.z) / cell);
                x0 = Mathf.Clamp(x0, 0, res - 1); x1 = Mathf.Clamp(x1, 0, res - 1); z0 = Mathf.Clamp(z0, 0, res - 1); z1 = Mathf.Clamp(z1, 0, res - 1);
                if (x1 >= x0 && z1 >= z0)
                {
                    var h = td.GetHeights(x0, z0, x1 - x0 + 1, z1 - z0 + 1); float v = Mathf.Clamp01((p.y + fl - 0.8f - tp.y) / td.size.y);
                    for (int j = 0; j < h.GetLength(0); j++) for (int i = 0; i < h.GetLength(1); i++) h[j, i] = Mathf.Min(h[j, i], v);
                    td.SetHeights(x0, z0, h);
                }
                // the flags: a cell beyond the dug-out ground all round, the stairwell left open
                float px0 = rx0 - cell - 0.2f, px1 = rx1 + cell + 0.2f, pz0 = rz0 - cell - 0.2f, pz1 = rz1 + cell + 0.2f;
                void Pave(float a0, float a1, float b0, float b1) { if (a1 - a0 < 0.05f || b1 - b0 < 0.05f) return; B(M.stone2, new Vector3(a0, ceil, b0), new Vector3(a1, 0.02f, b1)); }
                Pave(px0, px1, pz0, zBack - 0.4f); Pave(px0, px1, zTop, pz1); Pave(px0, -sw, zBack - 0.4f, zTop); Pave(sw, px1, zBack - 0.4f, zTop);
            }
            // the hood over the stairwell
            B(M.stone2, new Vector3(-sw - st, 0f, zBack - 0.4f), new Vector3(-sw, 2.4f, zTop));
            B(M.stone2, new Vector3(sw, 0f, zBack - 0.4f), new Vector3(sw + st, 2.4f, zTop));
            B(M.stone2, new Vector3(-sw - st, 2.4f, zBack - 0.4f), new Vector3(sw + st, 3.0f, zTop));
            B(M.stone2, new Vector3(-sw, 0f, zBack - 0.4f), new Vector3(sw, 2.4f, zBack));
            k.Box(M.iron, new Vector3(-0.9f, 0f, zTop), new Vector3(0.9f, 0.04f, zTop + 0.04f));
            // twelve steep steps down, solid masonry under each
            for (int i = 0; i < 12; i++) { float zt = zTop - i * 0.35f, y = -(i + 1) * 0.25f; B(M.stone2, new Vector3(-sw, fl, zt - 0.35f), new Vector3(sw, y, zt)); }
            // the stairwell's walls below ground, the landing, the doorway east into the chamber
            B(M.stone, new Vector3(-sw - st, fl, zBack - 0.4f), new Vector3(-sw, 0f, zTop));
            B(M.stone, new Vector3(-sw, fl, zBack - 0.4f), new Vector3(sw, 0f, zBack));
            const float dz0 = zBack, dz1 = zBack + 1.25f, dh = fl + 2.1f;
            B(M.stone, new Vector3(sw, fl, dz1), new Vector3(sw + st, 0f, zTop)); B(M.stone, new Vector3(sw, dh, dz0), new Vector3(sw + st, 0f, dz1));
            B(M.stone, new Vector3(sw, fl, zBack - 0.4f), new Vector3(cx0, 0f, zBack));                         // the corner by the doorway
            B(M.stone2, new Vector3(-sw - st, fl - 0.3f, zBack - 0.4f), new Vector3(cx1 + st, fl, cz1 + st)); // one floor under the lot
            // the chamber: walls, the flags over it as its ceiling, ribs across like a vault
            B(M.stone, new Vector3(sw, fl, zTop), new Vector3(cx0, ceil, cz1 + st));
            B(M.stone, new Vector3(cx1, fl, zBack - 0.4f), new Vector3(cx1 + st, ceil, cz1 + st));
            B(M.stone, new Vector3(sw, fl, cz1), new Vector3(cx1 + st, ceil, cz1 + st));
            B(M.stone, new Vector3(cx0, fl, zBack - 0.4f), new Vector3(cx1, ceil, zBack));
            k.Box(M.stone2, new Vector3(cx0, ceil - 0.02f, cz0), new Vector3(cx1, ceil, cz1));
            for (float z = cz0 + 1.2f; z < cz1 - 0.5f; z += 1.8f) { k.Box(M.stone2, new Vector3(cx0, ceil - 0.35f, z), new Vector3(cx1, ceil - 0.02f, z + 0.3f)); k.Box(M.stone2, new Vector3(cx0, fl, z), new Vector3(cx0 + 0.2f, ceil - 0.35f, z + 0.3f)); k.Box(M.stone2, new Vector3(cx1 - 0.2f, fl, z), new Vector3(cx1, ceil - 0.35f, z + 0.3f)); }
            // two tombs along the east wall, a broken one with its lid askew
            foreach (var (tz, askew) in new[] { (-1.4f, false), (1.4f, true) })
            {
                B(M.stone2, new Vector3(cx1 - 1.15f, fl, tz - 1.0f), new Vector3(cx1 - 0.25f, fl + 0.8f, tz + 1.0f));
                if (askew) { k.Push(Matrix4x4.TRS(new Vector3(cx1 - 0.7f, fl + 0.88f, tz + 0.15f), Quaternion.Euler(0f, 14f, 6f), Vector3.one)); k.Box(M.stone, new Vector3(-0.5f, -0.08f, -1.05f), new Vector3(0.5f, 0.08f, 1.05f)); k.Pop(); }
                else k.Box(M.stone, new Vector3(cx1 - 1.2f, fl + 0.8f, tz - 1.05f), new Vector3(cx1 - 0.2f, fl + 0.96f, tz + 1.05f));
            }
            // somebody's been dossing down here: a sleeping bag, candles, cans, a holdall
            var bagM = M.dark != null ? M.dark : M.iron; var waxM = M.gravel != null ? M.gravel : M.stone;
            dk.Box(bagM, new Vector3(cx0 + 0.1f, fl, cz1 - 2.2f), new Vector3(cx0 + 0.9f, fl + 0.12f, cz1 - 0.2f));
            dk.Box(bagM, new Vector3(cx0 + 0.15f, fl, zBack + 0.2f), new Vector3(cx0 + 0.7f, fl + 0.3f, zBack + 0.55f));
            for (int i = 0; i < 7; i++) { var cp = new Vector3(cx0 + 1.2f + (i % 3) * 0.25f, fl, cz1 - 0.4f - (i / 3) * 0.3f); dk.Cyl(waxM, cp, 0.03f, 0.12f + (i % 2) * 0.08f, 6); }
            var glowM = CouncilEstate.M != null ? CouncilEstate.M.glow : null;
            if (glowM != null) for (int i = 0; i < 3; i++) { var cp = new Vector3(cx0 + 1.2f + i * 0.25f, fl + 0.2f + (i % 2) * 0.08f, cz1 - 0.4f); dk.Box(glowM, cp + new Vector3(-0.012f, 0f, -0.012f), cp + new Vector3(0.012f, 0.04f, 0.012f)); }
            for (int i = 0; i < 5; i++) dk.Cyl(M.iron, new Vector3(cx0 + 0.3f + i * 0.31f, fl, zBack + 1.1f + (i % 2) * 0.2f), 0.033f, 0.12f, 6);
            // a home for its spots
            var home = g.gameObject.AddComponent<Home>(); var rng = new System.Random(1067);
            var hh = Household.Make(10670, rng); hh.kind = Household.Kind.Couple; hh.wealth = 0.6f; hh.stuff = 0.6f; hh.family = "the crypt";
            home.H = hh; home.label = "The crypt"; home.variant = "crypt"; home.centre = new Vector3(2f, fl + 1.2f, 0f); home.radius = 8f; home.midZ = 0f; home.halfD = 5f;
            home.zoneRects.Add(Rect.MinMaxRect(-sw, zBack, cx1, cz1));
            void Sp(string kind, string label, string tag, Vector3 at, bool hidden = false) => home.spots.Add(new Home.SpotRec { kind = kind, label = label, tag = tag, pos = at, room = 0, lv = 0, hidden = hidden });
            Sp("rummage", "Heave the tomb lid aside", "tomb", new Vector3(cx1 - 1.75f, fl, -1.4f), true);
            Sp("rummage", "Feel inside the broken tomb", "tomb", new Vector3(cx1 - 1.75f, fl, 1.4f));
            Sp("rummage", "Go through the holdall", "bag", new Vector3(cx0 + 0.45f, fl, zBack + 1.05f));
            Sp("rummage", "Feel inside the sleeping bag", "underbed", new Vector3(cx0 + 1.2f, fl, cz1 - 1.3f));
            Sp("say", "Read the inscription", "HERE LYETH WALTER OF THE WEND, WHO DID KEEP THIS CASTLE. Somebody has added 'BADLY' in biro.", new Vector3(cx1 - 1.75f, fl, -0.2f));
            Sp("say", "Look at the candles", "Still warm. Whoever lit these isn't far away.", new Vector3(cx0 + 1.5f, fl, cz1 - 1.1f));
            var go = k.Build("Crypt", g, false, UnityEngine.Rendering.ShadowCastingMode.On); var gd = dk.Build("Crypt clutter", g, false, UnityEngine.Rendering.ShadowCastingMode.Off);
            var cg = new GameObject("Solid").transform; cg.SetParent(g, false);
            foreach (var (mn, mx) in cols) { var bc = new GameObject("c").AddComponent<BoxCollider>(); bc.transform.SetParent(cg, false); bc.center = (mn + mx) * 0.5f; bc.size = mx - mn; }
        }

        // ------------------------------------------------------------------------------------------ lane
        /// <summary>The gravel lane winding up the hill, draped on the terrain, with grass verges worn at the edges.</summary>
        static void LaneMesh(Transform root, Terrain t)
        {
            var k = new MeshKit(); const float half = 1.6f;
            var pts = new List<Vector3>();
            for (int i = 0; i < Lane.Length - 1; i++)
            {
                Vector2 a = Lane[i], b = Lane[i + 1]; int n = Mathf.CeilToInt(Vector2.Distance(a, b) / 1.5f);
                for (int s = 0; s < n; s++) { var q = Vector2.Lerp(a, b, s / (float)n); pts.Add(new Vector3(q.x, 0, q.y)); }
            }
            pts.Add(new Vector3(Lane[Lane.Length - 1].x, 0, Lane[Lane.Length - 1].y));
            float along = 0f, tl = MeshKit.TileOf(M.gravel);
            Vector3 H(Vector3 p) { p.y = t.SampleHeight(p) + t.transform.position.y + 0.06f; return p; }
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector3 d0 = (i > 0 ? pts[i + 1] - pts[i - 1] : pts[i + 1] - pts[i]); d0.y = 0; d0.Normalize();
                Vector3 d1 = (i + 2 < pts.Count ? pts[i + 2] - pts[i] : pts[i + 1] - pts[i]); d1.y = 0; d1.Normalize();
                Vector3 n0 = Vector3.Cross(Vector3.up, d0) * half, n1 = Vector3.Cross(Vector3.up, d1) * half;
                Vector3 a = H(pts[i] - n0), b = H(pts[i + 1] - n1), c = H(pts[i + 1] + n1), d = H(pts[i] + n0);
                float l = Vector3.Distance(pts[i], pts[i + 1]);
                k.QuadUV(M.gravel, a, b, c, d, Vector3.up, new Vector2(0, along / tl), new Vector2(0, (along + l) / tl), new Vector2(2 * half / tl, (along + l) / tl), new Vector2(2 * half / tl, along / tl));
                along += l;
            }
            var g = k.Build("CastleLane", root, true, UnityEngine.Rendering.ShadowCastingMode.Off);
        }
    }

    /// <summary>Crows wheeling over the castle keep: black birds on slow overlapping circles, wings beating, the odd caw.</summary>
    public class Crows : MonoBehaviour
    {
        class Bird { public Transform t, wl, wr; public float r, speed, phase, h; }
        readonly List<Bird> birds = new List<Bird>();
        Vector3 centre;

        public static void Attach()
        {
            var keep = GameObject.Find("CastleKeep"); if (keep == null) return;
            var go = new GameObject("Crows"); go.transform.SetParent(keep.transform.parent, false);
            var c = go.AddComponent<Crows>(); c.centre = keep.GetComponent<BoxCollider>() != null ? keep.GetComponent<BoxCollider>().center + Vector3.up * 14f : keep.transform.position;
        }

        void Start()
        {
            var black = Gfx.Mat(new Color(0.03f, 0.03f, 0.035f), null, 0.3f);
            var rng = new System.Random(13);
            for (int i = 0; i < 7; i++)
            {
                var b = new Bird { r = 6f + (float)rng.NextDouble() * 9f, speed = 0.25f + (float)rng.NextDouble() * 0.2f, phase = (float)rng.NextDouble() * 6.28f, h = (float)rng.NextDouble() * 8f };
                b.t = new GameObject("Crow").transform; b.t.SetParent(transform, false);
                Gfx.Prim(PrimitiveType.Sphere, b.t, Vector3.zero, new Vector3(0.14f, 0.12f, 0.4f), black, "Body", false);
                b.wl = new GameObject("WingL").transform; b.wl.SetParent(b.t, false);
                b.wr = new GameObject("WingR").transform; b.wr.SetParent(b.t, false);
                Gfx.Prim(PrimitiveType.Cube, b.wl, new Vector3(-0.28f, 0, 0), new Vector3(0.55f, 0.015f, 0.22f), black, "W", false);
                Gfx.Prim(PrimitiveType.Cube, b.wr, new Vector3(0.28f, 0, 0), new Vector3(0.55f, 0.015f, 0.22f), black, "W", false);
                foreach (var r in b.t.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                birds.Add(b);
            }
        }

        void Update()
        {
            float t = Time.time;
            foreach (var b in birds)
            {
                float a = b.phase + t * b.speed; float r = b.r + Mathf.Sin(t * 0.3f + b.phase) * 2f;
                var p = centre + new Vector3(Mathf.Cos(a) * r, b.h + Mathf.Sin(t * 0.7f + b.phase) * 1.5f, Mathf.Sin(a) * r);
                var tangent = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a));
                b.t.position = p; b.t.rotation = Quaternion.LookRotation(tangent) * Quaternion.Euler(0, 0, -18f);
                float flap = Mathf.Sin(t * 9f + b.phase * 3f) * 35f;
                b.wl.localRotation = Quaternion.Euler(0, 0, flap); b.wr.localRotation = Quaternion.Euler(0, 0, -flap);
            }
        }
    }
}

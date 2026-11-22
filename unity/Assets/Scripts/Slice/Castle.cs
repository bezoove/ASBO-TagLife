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
            Walls(root, c, rng); Keep(root, c + new Vector3(-2f, 0, -5f)); Crypt(root, c + new Vector3(8f, 0, -9f));
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
            k.Box(M.stone, a, b, 4);
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
            k.Box(M.dark, p + new Vector3(-w / 2 + 5.6f, 4.0f, w / 2), p + new Vector3(-w / 2 + 7.0f, 6.4f, w / 2 + 0.03f));
            k.Box(M.timber, p + new Vector3(-w / 2 + 5.65f, 4.0f, w / 2 + 0.05f), p + new Vector3(-w / 2 + 6.1f, 6.3f, w / 2 + 0.75f));
            var g = k.Build("CastleKeep", root, false);
            var bc = g.AddComponent<BoxCollider>(); bc.center = p + new Vector3(0, h / 2, 0); bc.size = new Vector3(w, h, w);
            var st = g.AddComponent<BoxCollider>(); st.center = p + new Vector3(-w / 2 + 4f, 1.6f, w / 2 + 1.4f); st.size = new Vector3(6.5f, 3.2f, 1.6f);
        }

        /// <summary>The crypt: a low stone hood over steps that go down into the dark.</summary>
        static void Crypt(Transform root, Vector3 p)
        {
            var k = new MeshKit();
            k.Box(M.stone2, p + new Vector3(-1.9f, 0f, -2.5f), p + new Vector3(-1.3f, 2.4f, 1.2f));
            k.Box(M.stone2, p + new Vector3(1.3f, 0f, -2.5f), p + new Vector3(1.9f, 2.4f, 1.2f));
            k.Box(M.stone2, p + new Vector3(-1.9f, 2.4f, -2.5f), p + new Vector3(1.9f, 3.0f, 1.2f));
            k.Box(M.stone2, p + new Vector3(-1.3f, 0f, -2.5f), p + new Vector3(1.3f, 2.4f, -2.0f));
            // steps descending to a black doorway
            for (int s = 0; s < 6; s++) k.Box(M.stone2, p + new Vector3(-1.3f, -0.2f - s * 0.25f, 1.2f - s * 0.5f), p + new Vector3(1.3f, 0.05f - s * 0.25f, 0.7f - s * 0.5f));
            k.Box(M.dark, p + new Vector3(-1.3f, -1.8f, -2.0f), p + new Vector3(1.3f, 2.4f, -1.98f));
            k.Box(M.iron, p + new Vector3(-0.9f, 0f, 1.2f), p + new Vector3(0.9f, 0.04f, 1.24f));
            var g = k.Build("CastleCrypt", root, false);
            var bc = g.AddComponent<BoxCollider>(); bc.center = p + new Vector3(0, 1.5f, -0.65f); bc.size = new Vector3(3.8f, 3f, 3.7f);
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

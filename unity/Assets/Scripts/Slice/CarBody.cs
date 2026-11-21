using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>Anything that can run people over.</summary>
    public interface IVehicle { float Speed { get; } Vector3 Velocity { get; } }

    /// <summary>
    /// Procedural car bodies (hatchback, saloon, van). The body is a smooth loft: the control profile is resampled with
    /// Catmull-Rom splines into ~60 stations, each a rounded cross-section (rocker, waist, belt line, tumblehome,
    /// roof crown), with smooth normals and real wheel-arch cut-outs. Glass is see-through (premultiplied, reflective)
    /// with an interior behind it — seats, dash, steering wheel on the right. Details: door seams and handles,
    /// headlamp and tail-lamp clusters, grille, two-tone bumpers, mirrors, UK plates with generated registrations,
    /// five-spoke alloys with tyre sidewalls, and a soft contact shadow.
    /// Materials come from templates in Resources/CarMats (so their shader variants survive Android builds).
    /// </summary>
    public static class CarBody
    {
        public class Spec { public float L, yb = 0.3f, tumble = 0.8f; public float[] u, belt, top, W; public bool van; public float wheelR = 0.31f, wheelZ0 = 0.17f, wheelZ1 = 0.83f; }

        public static readonly Spec Hatch = new Spec
        {
            L = 3.9f,
            u = new[] { 0.00f, 0.03f, 0.10f, 0.17f, 0.30f, 0.62f, 0.76f, 0.90f, 0.97f, 1.00f },
            belt = new[] { 0.80f, 0.90f, 0.94f, 0.95f, 0.95f, 0.95f, 0.94f, 0.88f, 0.80f, 0.70f },
            top = new[] { 0.84f, 0.98f, 1.20f, 1.38f, 1.46f, 1.46f, 1.00f, 0.92f, 0.84f, 0.72f },
            W = new[] { 0.74f, 0.80f, 0.83f, 0.84f, 0.85f, 0.85f, 0.85f, 0.83f, 0.79f, 0.72f },
            wheelZ0 = 0.16f, wheelZ1 = 0.84f
        };
        public static readonly Spec Saloon = new Spec
        {
            L = 4.4f,
            u = new[] { 0.00f, 0.03f, 0.12f, 0.24f, 0.37f, 0.63f, 0.76f, 0.91f, 0.97f, 1.00f },
            belt = new[] { 0.82f, 0.90f, 0.94f, 0.95f, 0.95f, 0.95f, 0.94f, 0.88f, 0.80f, 0.70f },
            top = new[] { 0.86f, 0.96f, 1.00f, 1.05f, 1.42f, 1.42f, 1.00f, 0.92f, 0.84f, 0.72f },
            W = new[] { 0.76f, 0.83f, 0.86f, 0.87f, 0.87f, 0.87f, 0.87f, 0.85f, 0.80f, 0.73f },
            wheelZ0 = 0.17f, wheelZ1 = 0.82f
        };
        public static readonly Spec Van = new Spec
        {
            L = 4.8f, van = true, tumble = 0.93f,
            u = new[] { 0.00f, 0.02f, 0.60f, 0.70f, 0.79f, 0.91f, 0.97f, 1.00f },
            belt = new[] { 0.98f, 1.02f, 1.05f, 1.05f, 1.00f, 0.92f, 0.85f, 0.75f },
            top = new[] { 1.95f, 2.02f, 2.02f, 1.97f, 1.32f, 1.02f, 0.92f, 0.78f },
            W = new[] { 0.94f, 0.97f, 0.97f, 0.97f, 0.95f, 0.92f, 0.88f, 0.80f },
            wheelR = 0.34f, wheelZ0 = 0.15f, wheelZ1 = 0.83f
        };

        // ------------------------------------------------------------------ materials
        static Material paintTpl, plateTpl, glass, trim, under, chrome, tyre, alloy, lampF, lampR, amber, interior, grille, shadowM;
        static Mesh wheelMesh;

        static Material Tpl(string n, System.Func<Material> fallback)
        {
            var m = Resources.Load<Material>("CarMats/" + n);
            return m != null ? m : fallback();
        }

        static void Init()
        {
            if (glass != null) return;
            paintTpl = Tpl("Paint", () => Gfx.Mat(Color.white, null, 0.82f, 0.35f));
            plateTpl = Tpl("Plate", () => Gfx.Mat(Color.white, null, 0.35f));
            glass = Tpl("Glass", () => Gfx.Mat(new Color(0.05f, 0.07f, 0.09f), null, 0.95f, 0.2f));
            trim = Tpl("Trim", () => Gfx.Mat(new Color(0.03f, 0.03f, 0.035f), null, 0.55f));
            under = Tpl("Under", () => Gfx.Mat(new Color(0.035f, 0.035f, 0.035f), null, 0.08f));
            chrome = Tpl("Chrome", () => Gfx.Mat(new Color(0.8f, 0.81f, 0.83f), null, 0.9f, 1f));
            tyre = Tpl("Tyre", () => Gfx.Mat(new Color(0.045f, 0.045f, 0.05f), null, 0.28f));
            alloy = Tpl("Alloy", () => Gfx.Mat(new Color(0.62f, 0.63f, 0.65f), null, 0.72f, 0.9f));
            lampF = Tpl("LampFront", () => Gfx.Emissive(new Color(1f, 0.96f, 0.85f), 1.6f));
            lampR = Tpl("LampRear", () => Gfx.Emissive(new Color(0.85f, 0.05f, 0.04f), 1.1f));
            amber = Tpl("LampAmber", () => Gfx.Emissive(new Color(1f, 0.5f, 0.05f), 0.6f));
            interior = Tpl("Interior", () => Gfx.Mat(new Color(0.07f, 0.07f, 0.075f), null, 0.15f));
            grille = Tpl("Grille", () => Gfx.Mat(new Color(0.02f, 0.02f, 0.025f), null, 0.35f, 0.2f));
            shadowM = Resources.Load<Material>("CarMats/Shadow"); // no fallback: an opaque quad would be worse than none
            wheelMesh = BuildWheel();
        }

        // ------------------------------------------------------------------ body
        struct St { public float u, z, belt, top, W, yb, cab; }

        static float CR(float p0, float p1, float p2, float p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        static float Sample(float[] a, float f)
        {
            int n = a.Length; int i = Mathf.Clamp(Mathf.FloorToInt(f), 0, n - 2); float t = f - i;
            float p0 = a[Mathf.Max(i - 1, 0)], p1 = a[i], p2 = a[i + 1], p3 = a[Mathf.Min(i + 2, n - 1)];
            return CR(p0, p1, p2, p3, t);
        }

        const int R = 11; // points per half cross-section, floor centre (0) to roof centre (10)

        static Vector3[] Section(Spec s, St st)
        {
            float W = st.W, yb = st.yb, belt = st.belt, top = st.top, cab = st.cab;
            float Wt = W * Mathf.Lerp(0.95f, s.tumble, cab);
            float edge = top - Mathf.Lerp(0.015f, 0.06f, cab);
            float crown = Mathf.Lerp(0.008f, 0.018f, cab);
            float waist = Mathf.Lerp(yb + 0.06f, belt, 0.5f);
            return new[]
            {
                new Vector3(0f, yb, st.z),
                new Vector3(W - 0.08f, yb, st.z),
                new Vector3(W - 0.012f, yb + 0.055f, st.z),
                new Vector3(W + 0.012f, waist, st.z),
                new Vector3(W + 0.002f, belt - 0.05f, st.z),
                new Vector3(W - 0.022f, belt, st.z),
                new Vector3(Mathf.Lerp(W - 0.022f, Wt, 0.55f), Mathf.Lerp(belt, edge, 0.55f), st.z),
                new Vector3(Wt, edge, st.z),
                new Vector3(Wt - Mathf.Lerp(0.03f, 0.08f, cab), top - 0.004f, st.z),
                new Vector3(Wt * 0.45f, top + crown * 0.8f, st.z),
                new Vector3(0f, top + crown, st.z),
            };
        }

        /// <summary>Builds the body under parent (local origin at ground centre, front = +z). Returns 4 wheel visuals FL, FR, RL, RR.</summary>
        public static Transform[] Build(Transform parent, Spec s, Color paintCol, bool castShadow = true, int seed = -1)
        {
            Init();
            var rng = new System.Random(seed >= 0 ? seed : UnityEngine.Random.Range(0, int.MaxValue));
            var paint = new Material(paintTpl) { name = "CarPaint" };
            paint.SetColor("_BaseColor", paintCol);
            float lum = paintCol.r * 0.3f + paintCol.g * 0.59f + paintCol.b * 0.11f;
            bool metallic = rng.NextDouble() < 0.6;
            paint.SetFloat("_Metallic", metallic ? Mathf.Lerp(0.55f, 0.35f, lum) : 0.05f);
            paint.SetFloat("_Smoothness", metallic ? 0.86f : 0.8f);

            int n = s.u.Length, S = 64;
            var st = new St[S];
            float wr = s.wheelR, rA = s.wheelR + 0.075f;
            float zw0 = (s.wheelZ0 - 0.5f) * s.L, zw1 = (s.wheelZ1 - 0.5f) * s.L;
            for (int j = 0; j < S; j++)
            {
                float f = j / (S - 1f) * (n - 1);
                var x = new St { u = Mathf.Clamp01(Sample(s.u, f)), belt = Sample(s.belt, f), top = Sample(s.top, f), W = Sample(s.W, f) };
                x.u = j == 0 ? 0f : j == S - 1 ? 1f : x.u;
                x.z = (x.u - 0.5f) * s.L;
                x.top = Mathf.Max(x.top, x.belt + 0.015f);
                x.cab = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 0.34f, x.top - x.belt));
                // wheel arches: the bottom edge of the body follows the arch
                float yb = s.yb;
                foreach (float wz in new[] { zw0, zw1 })
                {
                    float dz = x.z - wz;
                    if (Mathf.Abs(dz) < rA) yb = Mathf.Max(yb, wr + Mathf.Sqrt(rA * rA - dz * dz));
                }
                x.yb = Mathf.Min(yb, x.belt - 0.14f);
                st[j] = x;
            }
            var P = new Vector3[S][];
            for (int j = 0; j < S; j++) P[j] = Section(s, st[j]);
            // smooth normals (side +1), outward
            var Nn = new Vector3[S][];
            for (int j = 0; j < S; j++)
            {
                Nn[j] = new Vector3[R];
                for (int r = 0; r < R; r++)
                {
                    Vector3 ta = P[Mathf.Min(j + 1, S - 1)][r] - P[Mathf.Max(j - 1, 0)][r];
                    Vector3 tb = P[j][Mathf.Min(r + 1, R - 1)] - P[j][Mathf.Max(r - 1, 0)];
                    Vector3 nn = Vector3.Cross(ta, tb);
                    Vector3 c = new Vector3(0f, (st[j].yb + st[j].top) * 0.5f, st[j].z);
                    if (Vector3.Dot(nn, P[j][r] - c) < 0f) nn = -nn;
                    if (nn.sqrMagnitude < 1e-10f) nn = (P[j][r] - c);
                    if (r == 0 || r == R - 1) nn.x = 0f;
                    Nn[j][r] = nn.normalized;
                }
            }

            // where the glass goes
            float slopeAt(int j) { int a = Mathf.Max(j, 0), b = Mathf.Min(j + 1, S - 1); return Mathf.Abs(st[b].top - st[a].top) / Mathf.Max(Mathf.Abs(st[b].z - st[a].z), 1e-4f); }
            int flatA = -1, flatB = -1; // first and last column of the flat roof
            for (int j = 0; j < S - 1; j++) if (st[j].cab > 0.8f && st[j + 1].cab > 0.8f && slopeAt(j) < 0.4f) { if (flatA < 0) flatA = j; flatB = j; }
            int bPillar = flatA >= 0 ? Mathf.RoundToInt(Mathf.Lerp(flatA, flatB, s.van ? 1f : 0.56f)) : -1;
            float glassStartU = s.van ? 0.62f : 0f;

            var k = new MeshKit();
            var idx = new int[2, S, R];
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? 1f : -1f;
                for (int j = 0; j < S; j++)
                    for (int r = 0; r < R; r++)
                    {
                        var p = P[j][r]; var q = Nn[j][r];
                        idx[side, j, r] = k.Vert(new Vector3(p.x * sx, p.y, p.z), new Vector3(q.x * sx, q.y, q.z), Vector2.zero);
                    }
            }
            Material SideGlass(int j)
            {
                bool cabinCols = st[j].cab > 0.55f && st[j + 1].cab > 0.55f;
                bool steep = slopeAt(j) > 0.42f && (st[j].cab > 0.35f || st[j + 1].cab > 0.35f);
                if (cabinCols && st[j].u >= glassStartU && j >= flatA && j <= flatB)
                {
                    if (j == bPillar || j == bPillar - 1) return trim;
                    if (j == flatA || j == flatB) return paint;
                    return glass;
                }
                if (cabinCols && !steep && st[j].u >= glassStartU) return glass; // quarter-lights where the roof slopes gently
                return null;
            }
            Material QuadMat(int j, int r)
            {
                if (r == 0) return under;
                if (r == 1) return trim;
                bool steep = slopeAt(j) > 0.42f && (st[j].cab > 0.35f || st[j + 1].cab > 0.35f);
                if (r >= 5 && steep && r <= 9) return glass; // windscreen / rear screen
                var sg = SideGlass(j);
                if (r == 5 || r == 6) return sg ?? paint;
                if ((r == 4 || r == 7) && sg == glass) return trim; // black window surround: belt moulding and drip rail
                return paint;
            }
            for (int side = 0; side < 2; side++)
                for (int j = 0; j < S - 1; j++)
                    for (int r = 0; r < R - 1; r++)
                    {
                        int a = idx[side, j, r], b = idx[side, j + 1, r], c = idx[side, j + 1, r + 1], d = idx[side, j, r + 1];
                        Vector3 nAvg = k.N[a] + k.N[b] + k.N[c] + k.N[d];
                        var m = QuadMat(j, r);
                        TriOut(k, m, a, b, c, nAvg); TriOut(k, m, a, c, d, nAvg);
                    }
            // end caps
            Cap(k, P[0], paint, Vector3.back); Cap(k, P[S - 1], paint, Vector3.forward);

            // inner shell: dark interior behind the glass (floor, door panels, headlining)
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? 1f : -1f;
                var inner = new int[S, R];
                for (int j = 0; j < S; j++)
                    for (int r = 0; r < R; r++)
                    {
                        var p = P[j][r]; var q = Nn[j][r];
                        Vector3 pi = new Vector3(p.x * sx, p.y, p.z) - new Vector3(q.x * sx, q.y, q.z) * 0.02f;
                        inner[j, r] = k.Vert(pi, new Vector3(-q.x * sx, -q.y, -q.z), Vector2.zero);
                    }
                for (int j = 0; j < S - 1; j++)
                {
                    if (st[j].cab < 0.3f && st[j + 1].cab < 0.3f) continue;
                    for (int r = 0; r < R - 1; r++)
                    {
                        if (r == 5 || r == 6) continue; // that's where the glass is
                        int a = inner[j, r], b = inner[j + 1, r], c = inner[j + 1, r + 1], d = inner[j, r + 1];
                        Vector3 nAvg = k.N[a] + k.N[b] + k.N[c] + k.N[d];
                        TriOut(k, interior, a, b, c, nAvg); TriOut(k, interior, a, c, d, nAvg);
                    }
                }
            }

            float zf = s.L / 2f, zr = -s.L / 2f;
            float wf = st[S - 1].W, wb = st[0].W, beltF = st[S - 1].belt, beltR = st[0].belt;
            // ---- front: bumper (paint over black), grille, lamps, plate
            k.Box(paint, new Vector3(-wf - 0.03f, 0.38f, zf - 0.1f), new Vector3(wf + 0.03f, 0.5f, zf + 0.065f));
            k.Box(trim, new Vector3(-wf - 0.02f, 0.26f, zf - 0.12f), new Vector3(wf + 0.02f, 0.38f, zf + 0.05f));
            k.Box(grille, new Vector3(-0.42f, 0.29f, zf + 0.05f), new Vector3(0.42f, 0.36f, zf + 0.056f));
            float gy0 = Mathf.Min(0.53f, beltF - 0.22f), gy1 = Mathf.Min(gy0 + 0.13f, beltF - 0.05f);
            k.Box(grille, new Vector3(-0.36f, gy0, zf - 0.02f), new Vector3(0.36f, gy1, zf + 0.012f));
            for (int q = 0; q < 3; q++) { float y = Mathf.Lerp(gy0 + 0.025f, gy1 - 0.025f, q / 2f); k.Box(chrome, new Vector3(-0.35f, y - 0.006f, zf + 0.012f), new Vector3(0.35f, y + 0.006f, zf + 0.02f)); }
            var reg = Registration(rng);
            var plateF = new Material(plateTpl) { name = "PlateF" }; plateF.SetTexture("_BaseMap", PlateTex(reg, false)); plateF.SetColor("_BaseColor", Color.white);
            var plateR = new Material(plateTpl) { name = "PlateR" }; plateR.SetTexture("_BaseMap", PlateTex(reg, true)); plateR.SetColor("_BaseColor", Color.white);
            PlateQuad(k, plateF, new Vector3(0, 0.425f, zf + 0.068f), 0.52f, 0.11f, 1f);
            foreach (int sd in new[] { -1, 1 })
            {
                float x = sd * (wf - 0.2f), ly = Mathf.Min(beltF - 0.11f, 0.68f);
                k.Box(chrome, new Vector3(x - 0.17f, ly - 0.07f, zf - 0.05f), new Vector3(x + 0.17f, ly + 0.065f, zf + 0.008f));
                k.Box(lampF, new Vector3(x - 0.14f, ly - 0.045f, zf + 0.008f), new Vector3(x + 0.1f, ly + 0.04f, zf + 0.018f));
                k.Box(amber, new Vector3(sd > 0 ? x + 0.1f : x - 0.15f, ly - 0.045f, zf + 0.008f), new Vector3(sd > 0 ? x + 0.15f : x - 0.1f, ly + 0.04f, zf + 0.016f));
                // fog lamp in the lower bumper
                k.Box(lampF, new Vector3(x - 0.05f, 0.3f, zf + 0.05f), new Vector3(x + 0.05f, 0.34f, zf + 0.058f));
            }
            // ---- rear: bumper, lamp clusters wrapping round the corners, plate
            k.Box(paint, new Vector3(-wb - 0.03f, 0.38f, zr - 0.065f), new Vector3(wb + 0.03f, 0.52f, zr + 0.1f));
            k.Box(trim, new Vector3(-wb - 0.02f, 0.26f, zr - 0.05f), new Vector3(wb + 0.02f, 0.38f, zr + 0.12f));
            float rly = s.van ? 0.95f : Mathf.Min(beltR - 0.08f, 0.78f);
            PlateQuad(k, plateR, new Vector3(0, s.van ? 0.62f : Mathf.Min(0.62f, beltR - 0.1f), zr - 0.012f), 0.52f, 0.11f, -1f);
            foreach (int sd in new[] { -1, 1 })
            {
                float x = sd * (wb - 0.16f), h = s.van ? 0.22f : 0.09f;
                k.Box(lampR, new Vector3(x - 0.15f, rly - h, zr - 0.018f), new Vector3(x + 0.15f, rly + h * 0.6f, zr + 0.02f));
                k.Box(amber, new Vector3(x - 0.05f, rly - h - 0.05f, zr - 0.016f), new Vector3(x + 0.05f, rly - h, zr + 0.02f));
                // wrap-round on the flank
                float fx = sd * (wb + 0.004f);
                k.Box(lampR, new Vector3(Mathf.Min(fx, fx + sd * 0.012f), rly - h * 0.8f, zr + 0.02f), new Vector3(Mathf.Max(fx, fx + sd * 0.012f), rly + h * 0.5f, zr + 0.2f));
            }

            // ---- doors: seams and handles, mirrors
            int Col(float u) { for (int j = 0; j < S - 1; j++) if (st[j + 1].u >= u) return j; return S - 2; }
            var seams = new List<int>();
            // station index runs rear (0) to front (S-1): flatB is the front of the cabin roof, flatA the back
            if (!s.van && flatA >= 0)
            {
                int frontDoorFront = Mathf.Min(flatB + Mathf.RoundToInt((flatB - flatA) * 0.12f) + 1, S - 2);
                seams.Add(frontDoorFront); seams.Add(bPillar);
                if (s == Saloon || rng.NextDouble() < 0.6) seams.Add(Mathf.Max(flatA - 1, 1)); // 5-door / saloon rear door
            }
            else if (s.van) { seams.Add(Col(0.78f)); seams.Add(Col(0.64f)); seams.Add(Col(0.3f)); }
            seams.Sort((a, b) => b.CompareTo(a)); // front to back
            foreach (int sideSign in new[] { 1, -1 })
            {
                foreach (int j in seams)
                {
                    if (j <= 0 || j >= S - 1) continue;
                    for (int r = 2; r < 5; r++) SeamQuad(k, P[j][r], P[j][r + 1], Nn[j][r], Nn[j][r + 1], sideSign);
                }
                for (int i = 0; i + 1 < seams.Count; i++)
                {
                    int jm = Mathf.Clamp(seams[i + 1] + 4, seams[i + 1] + 1, seams[i] - 1); // near the back edge of each door
                    if (s.van && i > 0) break;
                    var hp = P[jm][4]; var hn = Nn[jm][4];
                    Vector3 c = new Vector3(hp.x * sideSign, hp.y - 0.06f, hp.z) + new Vector3(hn.x * sideSign, 0, 0) * 0.01f;
                    k.Box(trim, c + new Vector3(-0.012f, -0.012f, -0.07f), c + new Vector3(0.012f, 0.012f, 0.07f));
                }
                // mirror at the A-pillar
                int mj = flatB >= 0 ? Mathf.Min(flatB + 2, S - 2) : Col(0.66f);
                if (s.van) mj = Col(0.73f);
                var mp = P[Mathf.Clamp(mj, 0, S - 1)][5];
                float mx = mp.x * sideSign;
                k.Box(paint, new Vector3(Mathf.Min(mx, mx + sideSign * 0.2f), mp.y + 0.02f, mp.z - 0.07f), new Vector3(Mathf.Max(mx, mx + sideSign * 0.2f), mp.y + 0.13f, mp.z + 0.04f));
                k.Box(chrome, new Vector3(Mathf.Min(mx + sideSign * 0.02f, mx + sideSign * 0.18f), mp.y + 0.035f, mp.z - 0.075f), new Vector3(Mathf.Max(mx + sideSign * 0.02f, mx + sideSign * 0.18f), mp.y + 0.115f, mp.z - 0.07f));
            }

            // ---- interior: dash, seats with headrests, steering wheel on the right (UK)
            if (flatA >= 0)
            {
                int ws = flatB; while (ws < S - 1 && st[ws + 1].cab > 0.3f) ws++; // windscreen base (front of the cabin)
                float zDash = st[ws].z - (s.van ? 0.15f : 0.05f);
                float wIn = st[flatA].W - 0.1f, bl = st[flatA].belt;
                k.Box(interior, new Vector3(-wIn, bl - 0.25f, zDash - 0.45f), new Vector3(wIn, bl + 0.04f, zDash));
                // rear bench just inside the back of the flat roof, front seats ahead of it (so nothing pokes through the rear screen)
                float zRear = st[flatA].z + 0.32f;
                float zSeat = s.van ? zDash - 0.75f : Mathf.Min(zDash - 0.95f, zRear + 0.85f);
                float seatW = 0.46f;
                foreach (float sx in new[] { -0.36f, 0.36f }) Seat(k, new Vector3(sx * wIn, s.yb + 0.05f, zSeat), seatW, bl);
                if (!s.van && zSeat - zRear > 0.6f) Seat(k, new Vector3(0f, s.yb + 0.05f, zRear), wIn * 1.7f, bl);
                if (s.van) k.Box(interior, new Vector3(-wIn, s.yb, zSeat - 0.25f), new Vector3(wIn, st[flatA].top - 0.05f, zSeat - 0.2f)); // bulkhead
                // steering wheel
                var swc = new Vector3(0.36f * wIn, bl + 0.06f, zDash - 0.5f);
                k.Push(Matrix4x4.TRS(swc, Quaternion.Euler(-62f, 0f, 0f), Vector3.one));
                k.Cyl(trim, new Vector3(0, -0.015f, 0), 0.18f, 0.03f, 14, true);
                k.Pop();
            }

            var body = k.Build("Body", parent, false, castShadow ? ShadowCastingMode.On : ShadowCastingMode.Off);
            var bmr = body.GetComponent<MeshRenderer>(); bmr.lightProbeUsage = LightProbeUsage.BlendProbes; bmr.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;

            // contact shadow
            if (shadowM != null) {
            var sh = new GameObject("ContactShadow"); sh.transform.SetParent(parent, false);
            sh.transform.localPosition = new Vector3(0, 0.025f, 0);
            var shm = new Mesh { name = "CarShadow" };
            float hw = Mathf.Max(s.W) + 0.22f, hl = s.L / 2f + 0.28f;
            shm.vertices = new[] { new Vector3(-hw, 0, -hl), new Vector3(hw, 0, -hl), new Vector3(hw, 0, hl), new Vector3(-hw, 0, hl) };
            shm.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            shm.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            shm.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            sh.AddComponent<MeshFilter>().sharedMesh = shm;
            var shr = sh.AddComponent<MeshRenderer>(); shr.sharedMaterial = shadowM; shr.shadowCastingMode = ShadowCastingMode.Off; shr.receiveShadows = false;
            shr.lightProbeUsage = LightProbeUsage.Off; shr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }

            // wheels
            var wheels = new Transform[4];
            float[] zs = { zw1, zw1, zw0, zw0 };
            for (int i = 0; i < 4; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                var g = new GameObject("Wheel" + i); g.transform.SetParent(parent, false);
                float wz = zs[i]; int jj = Col(Mathf.InverseLerp(-s.L / 2f, s.L / 2f, wz));
                g.transform.localPosition = new Vector3(side * (st[jj].W - 0.1f), s.wheelR, wz);
                g.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                g.transform.localScale = new Vector3(s.wheelR, 1f, s.wheelR);
                g.AddComponent<MeshFilter>().sharedMesh = wheelMesh;
                var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterials = new[] { tyre, alloy, under };
                mr.shadowCastingMode = castShadow ? ShadowCastingMode.On : ShadowCastingMode.Off;
                mr.lightProbeUsage = LightProbeUsage.BlendProbes;
                wheels[i] = g.transform;
            }
            return wheels;
        }

        static void TriOut(MeshKit k, Material m, int a, int b, int c, Vector3 outward)
        {
            Vector3 pa = k.V[a], pb = k.V[b], pc = k.V[c];
            if (Vector3.Dot(Vector3.Cross(pb - pa, pc - pa), outward) >= 0f) k.Tri(m, a, b, c); else k.Tri(m, a, c, b);
        }

        static void Cap(MeshKit k, Vector3[] sec, Material m, Vector3 n)
        {
            Vector3 c = new Vector3(0, (sec[0].y + sec[R - 1].y) / 2f, sec[0].z);
            for (int side = -1; side <= 1; side += 2)
                for (int e = 0; e < R - 1; e++) k.TriN(m, c, new Vector3(sec[e].x * side, sec[e].y, sec[e].z), new Vector3(sec[e + 1].x * side, sec[e + 1].y, sec[e + 1].z), n);
        }

        /// <summary>A narrow dark strip lying on the body between two section points: a panel gap.</summary>
        static void SeamQuad(MeshKit k, Vector3 a, Vector3 b, Vector3 na, Vector3 nb, int side)
        {
            const float hw = 0.0045f, off = 0.0025f;
            Vector3 A = new Vector3(a.x * side, a.y, a.z) + new Vector3(na.x * side, na.y, na.z) * off;
            Vector3 B = new Vector3(b.x * side, b.y, b.z) + new Vector3(nb.x * side, nb.y, nb.z) * off;
            Vector3 n = new Vector3((na.x + nb.x) * side, na.y + nb.y, na.z + nb.z).normalized;
            Vector3 dz = Vector3.forward * hw;
            k.Quad(under, A - dz, A + dz, B + dz, B - dz, n);
        }

        static void PlateQuad(MeshKit k, Material m, Vector3 c, float w, float h, float facing)
        {
            Vector3 x = Vector3.right * (w / 2f) * -facing, y = Vector3.up * (h / 2f); // reads left-to-right for someone facing the plate
            Vector3 n = Vector3.forward * facing;
            k.QuadUV(m, c - x - y, c + x - y, c + x + y, c - x + y, n, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
            k.Box(trim, c - x * 1.04f - y * 1.12f - n * 0.01f, c + x * 1.04f + y * 1.12f - n * 0.002f);
        }

        static void Seat(MeshKit k, Vector3 basePos, float w, float belt)
        {
            float back = belt + 0.16f - basePos.y;
            k.Box(interior, basePos + new Vector3(-w / 2f, 0.18f, -0.05f), basePos + new Vector3(w / 2f, 0.3f, 0.45f)); // cushion
            k.Push(Matrix4x4.TRS(basePos + new Vector3(0, 0.28f, -0.02f), Quaternion.Euler(-14f, 0f, 0f), Vector3.one));
            k.Box(interior, new Vector3(-w / 2f, 0f, -0.06f), new Vector3(w / 2f, back - 0.25f, 0.06f));
            int heads = w > 0.8f ? 3 : 1;
            for (int h = 0; h < heads; h++)
            {
                float hx = heads == 1 ? 0f : Mathf.Lerp(-w * 0.36f, w * 0.36f, h / (heads - 1f));
                k.Box(interior, new Vector3(hx - 0.12f, back - 0.2f, -0.05f), new Vector3(hx + 0.12f, back - 0.02f, 0.05f));
            }
            k.Pop();
        }

        // ------------------------------------------------------------------ wheels
        static Mesh BuildWheel()
        {
            var k = new MeshKit();
            const int seg = 28;
            // tyre: tread with shoulders, bulging sidewalls (radius 1 = tyre outer; axle along Y, ±0.1 m wide)
            var tyreProf = new[] { new Vector2(0.64f, -0.088f), new Vector2(0.8f, -0.104f), new Vector2(0.93f, -0.1f), new Vector2(0.985f, -0.085f), new Vector2(1f, -0.06f), new Vector2(1f, 0.06f), new Vector2(0.985f, 0.085f), new Vector2(0.93f, 0.1f), new Vector2(0.8f, 0.104f), new Vector2(0.64f, 0.088f) };
            Lathe(k, tyre, tyreProf, seg, new Vector2(0.82f, 0f));
            // rim lip and dark barrel behind the spokes
            Lathe(k, alloy, new[] { new Vector2(0.58f, 0.07f), new Vector2(0.645f, 0.09f), new Vector2(0.66f, 0.083f) }, seg, new Vector2(0.62f, 0f));
            Lathe(k, alloy, new[] { new Vector2(0.66f, -0.083f), new Vector2(0.645f, -0.09f), new Vector2(0.58f, -0.07f) }, seg, new Vector2(0.62f, 0f));
            foreach (float sy in new[] { 1f, -1f })
            {
                Disc(k, under, 0.6f, 0.02f * sy, sy, seg);
                Disc(k, alloy, 0.17f, 0.078f * sy, sy, 14); // hub
                // five spokes
                for (int sp = 0; sp < 5; sp++)
                {
                    float a = sp * Mathf.PI * 2f / 5f + (sy > 0 ? 0f : 0.31f);
                    Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)), perp = new Vector3(-dir.z, 0, dir.x);
                    Vector3 i0 = dir * 0.15f - perp * 0.075f, i1 = dir * 0.15f + perp * 0.075f, o0 = dir * 0.6f - perp * 0.045f, o1 = dir * 0.6f + perp * 0.045f;
                    float yf = 0.072f * sy, yb = 0.035f * sy;
                    Vector3 up = Vector3.up * sy;
                    k.Quad(alloy, i0 + Vector3.up * yf, i1 + Vector3.up * yf, o1 + Vector3.up * (yf - 0.01f * sy), o0 + Vector3.up * (yf - 0.01f * sy), up);
                    k.Quad(alloy, i0 + Vector3.up * yf, o0 + Vector3.up * (yf - 0.01f * sy), o0 + Vector3.up * yb, i0 + Vector3.up * yb, -perp);
                    k.Quad(alloy, i1 + Vector3.up * yf, o1 + Vector3.up * (yf - 0.01f * sy), o1 + Vector3.up * yb, i1 + Vector3.up * yb, perp);
                }
                // brake disc seen between the spokes
                Disc(k, alloy, 0.42f, 0.028f * sy, sy, 20);
            }
            var g = k.Build("WheelProto", null, false);
            var mesh = g.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(g);
            // the order of submeshes follows first use: tyre, alloy, under
            return mesh;
        }

        static void Lathe(MeshKit k, Material m, Vector2[] prof, int seg, Vector2 centre)
        {
            int np = prof.Length; var id = new int[seg + 1, np];
            for (int s = 0; s <= seg; s++)
            {
                float a = s * Mathf.PI * 2f / seg; float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                for (int p = 0; p < np; p++)
                {
                    Vector2 t = prof[Mathf.Min(p + 1, np - 1)] - prof[Mathf.Max(p - 1, 0)];
                    Vector2 n2 = new Vector2(t.y, -t.x).normalized;
                    if (Vector2.Dot(n2, prof[p] - centre) < 0f) n2 = -n2; // away from the profile's middle
                    id[s, p] = k.Vert(new Vector3(prof[p].x * ca, prof[p].y, prof[p].x * sa), new Vector3(n2.x * ca, n2.y, n2.x * sa), new Vector2(s / (float)seg, p / (float)(np - 1)));
                }
            }
            for (int s = 0; s < seg; s++)
                for (int p = 0; p < np - 1; p++)
                {
                    int a = id[s, p], b = id[s + 1, p], c = id[s + 1, p + 1], d = id[s, p + 1];
                    Vector3 o = k.N[a] + k.N[b] + k.N[c] + k.N[d];
                    TriOut(k, m, a, b, c, o); TriOut(k, m, a, c, d, o);
                }
        }

        static void Disc(MeshKit k, Material m, float r, float y, float face, int seg)
        {
            Vector3 n = Vector3.up * face;
            int c = k.Vert(new Vector3(0, y, 0), n, new Vector2(0.5f, 0.5f));
            int first = -1, prev = -1;
            for (int s = 0; s <= seg; s++)
            {
                float a = s * Mathf.PI * 2f / seg;
                int v = k.Vert(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r), n, new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
                if (prev >= 0) TriOut(k, m, c, prev, v, n);
                if (first < 0) first = v; prev = v;
            }
        }

        // ------------------------------------------------------------------ number plates
        static readonly Dictionary<char, string[]> Glyph = new Dictionary<char, string[]>
        {
            ['0'] = new[] { " ### ", "#   #", "#  ##", "# # #", "##  #", "#   #", " ### " },
            ['1'] = new[] { "  #  ", " ##  ", "  #  ", "  #  ", "  #  ", "  #  ", " ### " },
            ['2'] = new[] { " ### ", "#   #", "    #", "   # ", "  #  ", " #   ", "#####" },
            ['3'] = new[] { "#####", "   # ", "  #  ", "   # ", "    #", "#   #", " ### " },
            ['4'] = new[] { "   # ", "  ## ", " # # ", "#  # ", "#####", "   # ", "   # " },
            ['5'] = new[] { "#####", "#    ", "#### ", "    #", "    #", "#   #", " ### " },
            ['6'] = new[] { "  ## ", " #   ", "#    ", "#### ", "#   #", "#   #", " ### " },
            ['7'] = new[] { "#####", "    #", "   # ", "  #  ", " #   ", " #   ", " #   " },
            ['8'] = new[] { " ### ", "#   #", "#   #", " ### ", "#   #", "#   #", " ### " },
            ['9'] = new[] { " ### ", "#   #", "#   #", " ####", "    #", "   # ", " ##  " },
            ['A'] = new[] { " ### ", "#   #", "#   #", "#####", "#   #", "#   #", "#   #" },
            ['B'] = new[] { "#### ", "#   #", "#   #", "#### ", "#   #", "#   #", "#### " },
            ['C'] = new[] { " ### ", "#   #", "#    ", "#    ", "#    ", "#   #", " ### " },
            ['D'] = new[] { "#### ", "#   #", "#   #", "#   #", "#   #", "#   #", "#### " },
            ['E'] = new[] { "#####", "#    ", "#    ", "#### ", "#    ", "#    ", "#####" },
            ['F'] = new[] { "#####", "#    ", "#    ", "#### ", "#    ", "#    ", "#    " },
            ['G'] = new[] { " ### ", "#   #", "#    ", "# ###", "#   #", "#   #", " ####" },
            ['H'] = new[] { "#   #", "#   #", "#   #", "#####", "#   #", "#   #", "#   #" },
            ['J'] = new[] { "  ###", "   # ", "   # ", "   # ", "   # ", "#  # ", " ##  " },
            ['K'] = new[] { "#   #", "#  # ", "# #  ", "##   ", "# #  ", "#  # ", "#   #" },
            ['L'] = new[] { "#    ", "#    ", "#    ", "#    ", "#    ", "#    ", "#####" },
            ['M'] = new[] { "#   #", "## ##", "# # #", "# # #", "#   #", "#   #", "#   #" },
            ['N'] = new[] { "#   #", "#   #", "##  #", "# # #", "#  ##", "#   #", "#   #" },
            ['O'] = new[] { " ### ", "#   #", "#   #", "#   #", "#   #", "#   #", " ### " },
            ['P'] = new[] { "#### ", "#   #", "#   #", "#### ", "#    ", "#    ", "#    " },
            ['R'] = new[] { "#### ", "#   #", "#   #", "#### ", "# #  ", "#  # ", "#   #" },
            ['S'] = new[] { " ####", "#    ", "#    ", " ### ", "    #", "    #", "#### " },
            ['T'] = new[] { "#####", "  #  ", "  #  ", "  #  ", "  #  ", "  #  ", "  #  " },
            ['U'] = new[] { "#   #", "#   #", "#   #", "#   #", "#   #", "#   #", " ### " },
            ['V'] = new[] { "#   #", "#   #", "#   #", "#   #", "#   #", " # # ", "  #  " },
            ['W'] = new[] { "#   #", "#   #", "#   #", "# # #", "# # #", "# # #", " # # " },
            ['X'] = new[] { "#   #", "#   #", " # # ", "  #  ", " # # ", "#   #", "#   #" },
            ['Y'] = new[] { "#   #", "#   #", " # # ", "  #  ", "  #  ", "  #  ", "  #  " },
        };

        static string Registration(System.Random r)
        {
            const string L = "ABCDEFGHJKLMNOPRSTUVWXY";
            char C() => L[r.Next(L.Length)];
            int age = new[] { 15, 65, 16, 66, 17, 67, 18, 68, 19, 69, 20, 70, 21, 71, 22, 72, 23, 73, 24, 74 }[r.Next(20)];
            return "" + C() + C() + age.ToString("00") + " " + C() + C() + C();
        }

        static Texture2D PlateTex(string reg, bool rear)
        {
            const int W = 208, H = 48, sx = 3, sy = 4;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, anisoLevel = 2, name = "Plate " + reg };
            var bg = rear ? new Color32(250, 205, 20, 255) : new Color32(242, 242, 236, 255);
            var ink = new Color32(18, 18, 20, 255);
            var px = new Color32[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = bg;
            // thin border
            for (int x = 0; x < W; x++) { px[x] = px[(H - 1) * W + x] = new Color32(40, 40, 40, 255); }
            for (int y = 0; y < H; y++) { px[y * W] = px[y * W + W - 1] = new Color32(40, 40, 40, 255); }
            int charW = 5 * sx, gap = 4, space = 12;
            int total = 0; foreach (char ch in reg) total += ch == ' ' ? space : charW + gap; total -= gap;
            int cx = (W - total) / 2, cy = (H - 7 * sy) / 2;
            foreach (char ch in reg)
            {
                if (ch == ' ') { cx += space; continue; }
                if (Glyph.TryGetValue(ch, out var g))
                    for (int gy = 0; gy < 7; gy++)
                        for (int gx = 0; gx < 5; gx++)
                            if (g[gy][gx] == '#')
                                for (int yy = 0; yy < sy; yy++)
                                    for (int xx = 0; xx < sx; xx++)
                                    {
                                        int X = cx + gx * sx + xx, Y = cy + (6 - gy) * sy + yy;
                                        if (X >= 0 && X < W && Y >= 0 && Y < H) px[Y * W + X] = ink;
                                    }
                cx += charW + gap;
            }
            tex.SetPixels32(px); tex.Apply(true, true);
            return tex;
        }

        public static Spec Random(System.Random r) { int v = r.Next(10); return v < 5 ? Hatch : v < 8 ? Saloon : Van; }
        public static Color RandomPaint(System.Random r)
        {
            Color[] c = { new Color(0.72f, 0.73f, 0.75f), new Color(0.06f, 0.06f, 0.07f), new Color(0.9f, 0.9f, 0.88f), new Color(0.1f, 0.2f, 0.42f), new Color(0.48f, 0.05f, 0.06f), new Color(0.13f, 0.27f, 0.18f), new Color(0.32f, 0.33f, 0.35f), new Color(0.55f, 0.46f, 0.3f), new Color(0.2f, 0.32f, 0.55f), new Color(0.62f, 0.62f, 0.58f) };
            return c[r.Next(c.Length)];
        }
    }
}

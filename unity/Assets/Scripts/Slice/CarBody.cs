using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>Anything that can run people over.</summary>
    public interface IVehicle { float Speed { get; } Vector3 Velocity { get; } }

    // ---------------------------------------------------------------- car table (ported from the original game)
    [Serializable] public class CarGH { public float rb, rrx, fr, fb, roof, crown, rR, rF, curR, curF, beltIn, tumble, pA, pTop, pC, pBelt, bw, qw; public float[] xB, xQ; public bool van; }
    [Serializable] public class CarBump { public float y0, y1, @out, plateY; public string col, stripe; public float[] intake; public bool step; }
    [Serializable] public class CarLamp { public float z0, z1; public float[] yt, yb; public int oval, wrap, twin, band, tall; }
    [Serializable] public class CarGrille { public float z, y0, y1; public int bars, chrome, oval; }
    [Serializable] public class CarStrip { public float y, h, y0, y1; public string hex, stripe; public bool on; }
    [Serializable] public class CarModel
    {
        public string id; public float L, W, wb, fo, rad, tw, arch, sill, endY, rcF, rcR, floor, crease, seatF, seatR, plateRy;
        public float[] top, crown, rakeF, rakeR, seamFdF, seamFdR, seamRdR, handles;
        public CarGH gh; public int doors; public bool van, boot, handleBody, plateROnBumper, indBumper, mirrorBody;
        public CarBump bumpF, bumpR; public CarLamp head, tail; public CarGrille grille; public CarStrip clad, moulding;
        public string aerial, wheel; public int fuel;
        [NonSerialized] public float Le, hw, wxF, wxR, archR, profB1, profC2;
        [NonSerialized] public CarBody.Mono topC, crownC;
        [NonSerialized] public List<Vector2> prof;
    }
    [Serializable] class CarModelList { public CarModel[] models; }

    /// <summary>
    /// UK cars of the original game, built procedurally from its car table (Resources/CarModels.json): nine models
    /// (three-, four- and five-door hatches, superminis, a booted saloon and a panel van) with each one's plan shape,
    /// shoulder line, tumblehome, raked ends, greenhouse and pillars, bumpers (body colour or grey plastic with a stripe),
    /// lamp and grille shapes, cladding, mouldings, mirrors, aerial and steel / hubcap / alloy wheels.
    /// Every car is a bit rough, council-estate style: faded or mismatched panels, dents, dirt and rust, missing hubcaps,
    /// tax discs, fluffy dice and air fresheners, roof racks and ladders on vans, old and new style plates.
    /// </summary>
    public static class CarBody
    {
        public class Spec
        {
            public CarModel m;
            public float L => m.L; public bool van => m.van; public float wheelR => m.rad;
            public float wheelZ0 => (m.wxR + m.Le) / m.L; public float wheelZ1 => (m.wxF + m.Le) / m.L;
        }

        static Dictionary<string, CarModel> models;
        public static Dictionary<string, CarModel> Models { get { Load(); return models; } }
        public static Spec Get(string id) { Load(); return new Spec { m = models.TryGetValue(id, out var m) ? m : models["hatch_a"] }; }
        public static Spec Hatch => Get("hatch_a");
        public static Spec Saloon => Get("saloon_b");
        public static Spec Van => Get("van_a");

        static void Load()
        {
            if (models != null) return;
            models = new Dictionary<string, CarModel>();
            var ta = Resources.Load<TextAsset>("CarModels");
            if (ta == null) { Debug.LogError("[ASBO] Resources/CarModels.json missing"); return; }
            foreach (var m in JsonUtility.FromJson<CarModelList>(ta.text).models) { Prepare(m); models[m.id] = m; }
        }

        // ---------------------------------------------------------------- shape functions (as the original)
        public class Mono
        {
            readonly float[] x, y, m; readonly int n;
            public Mono(float[] flat)
            {
                n = flat.Length / 2; x = new float[n]; y = new float[n]; m = new float[n];
                for (int i = 0; i < n; i++) { x[i] = flat[2 * i]; y[i] = flat[2 * i + 1]; }
                if (n < 2) return;
                var d = new float[n - 1];
                for (int i = 0; i < n - 1; i++) d[i] = (y[i + 1] - y[i]) / Mathf.Max(1e-5f, x[i + 1] - x[i]);
                m[0] = d[0]; m[n - 1] = d[n - 2];
                for (int i = 1; i < n - 1; i++) m[i] = d[i - 1] * d[i] <= 0f ? 0f : (d[i - 1] + d[i]) * 0.5f;
                for (int i = 0; i < n - 1; i++)
                {
                    if (Mathf.Abs(d[i]) < 1e-7f) { m[i] = m[i + 1] = 0f; continue; }
                    float a = m[i] / d[i], b = m[i + 1] / d[i], s = a * a + b * b;
                    if (s > 9f) { float t = 3f / Mathf.Sqrt(s); m[i] = t * a * d[i]; m[i + 1] = t * b * d[i]; }
                }
            }
            public float Eval(float t)
            {
                if (n == 1) return y[0];
                if (t <= x[0]) return y[0];
                if (t >= x[n - 1]) return y[n - 1];
                int k = 0; while (k < n - 2 && t > x[k + 1]) k++;
                float h = x[k + 1] - x[k], s = (t - x[k]) / h, s2 = s * s, s3 = s2 * s;
                return (2 * s3 - 3 * s2 + 1) * y[k] + (s3 - 2 * s2 + s) * h * m[k] + (-2 * s3 + 3 * s2) * y[k + 1] + (s3 - s2) * h * m[k + 1];
            }
        }

        static readonly Mono lowIn = new Mono(new[] { 0.15f, 0.075f, 0.26f, 0.035f, 0.36f, 0.01f, 0.48f, 0f });
        static float SStep(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        static void Prepare(CarModel m)
        {
            m.Le = m.L / 2f; m.hw = m.W / 2f; m.wxF = m.Le - m.fo; m.wxR = m.wxF - m.wb; m.archR = m.rad + m.arch;
            m.topC = new Mono(m.top); m.crownC = new Mono(m.crown);
            var G = m.gh;
            Vector2 A = new Vector2(G.rb, Top(m, G.rb)), B = new Vector2(G.rrx, G.roof), C = new Vector2(G.fr, G.roof), D = new Vector2(G.fb, Top(m, G.fb));
            Vector2 Toward(Vector2 p, Vector2 q, float d) { float l = Mathf.Max(1e-4f, (q - p).magnitude); float k = Mathf.Min(d / l, 0.45f); return p + (q - p) * k; }
            Vector2 Bulge(Vector2 p, Vector2 q, float k) { Vector2 mid = (p + q) * 0.5f, dd = q - p; float l = Mathf.Max(1e-4f, dd.magnitude); return new Vector2(mid.x + dd.y / l * k, mid.y - dd.x / l * k); }
            Vector2 B1 = Toward(B, A, G.rR), B2 = Toward(B, C, G.rR), C1 = Toward(C, B, G.rF), C2 = Toward(C, D, G.rF);
            var P = new List<Vector2>();
            void Bez(Vector2 p, Vector2 c, Vector2 q, int nn, bool skip0) { for (int i = skip0 ? 1 : 0; i <= nn; i++) { float t = i / (float)nn, u = 1 - t; P.Add(u * u * p + 2 * u * t * c + t * t * q); } }
            Bez(A, Bulge(A, B1, G.curR), B1, 6, false); Bez(B1, B, B2, 4, true); Bez(B2, Bulge(B2, C1, 0.015f), C1, 4, true); Bez(C1, C, C2, 4, true); Bez(C2, Bulge(C2, D, G.curF), D, 7, true);
            for (int i = 1; i < P.Count; i++) if (P[i].x <= P[i - 1].x) P[i] = new Vector2(P[i - 1].x + 1e-4f, P[i].y); // keep x increasing
            m.prof = P; m.profB1 = B1.x; m.profC2 = C2.x;
        }

        static float Top(CarModel m, float x) => m.topC.Eval(x);
        static float Crown(CarModel m, float x) => m.crownC.Eval(x);
        static float Hp(CarModel m, float x)
        {
            float w = m.hw * (1f - 0.025f * Mathf.Pow(x / m.Le, 2));
            if (x > m.Le - m.rcF) { float d = Mathf.Min(m.rcF, x - (m.Le - m.rcF)); w -= m.rcF - Mathf.Sqrt(Mathf.Max(0, m.rcF * m.rcF - d * d)); }
            if (x < -m.Le + m.rcR) { float d = Mathf.Min(m.rcR, -m.Le + m.rcR - x); w -= m.rcR - Mathf.Sqrt(Mathf.Max(0, m.rcR * m.rcR - d * d)); }
            return w;
        }
        static float Yb(CarModel m, float x)
        {
            float y = x > m.wxR && x < m.wxF ? m.sill : m.endY;
            foreach (float wx in new[] { m.wxF, m.wxR }) { float d = Mathf.Abs(x - wx); if (d < m.archR) y = Mathf.Max(y, m.rad + Mathf.Sqrt(m.archR * m.archR - d * d)); }
            return y;
        }
        static float Inset(CarModel m, float y, float top)
        {
            float cr = top - m.crease - 0.04f;
            if (y < cr) return lowIn.Eval(y) + (m.van ? 0f : 0.004f * SStep(0.48f, cr, y));
            float t = Mathf.Clamp01((y - cr) / Mathf.Max(1e-4f, top - cr));
            return 0.004f + 0.032f * t * t;
        }
        static float Warp(CarModel m, float x, float y)
        {
            float dx = 0f;
            if (m.rakeF[1] != 0f) dx -= m.rakeF[1] * Mathf.Max(0f, y - m.rakeF[0]) * SStep(m.Le - 0.7f, m.Le, x);
            if (m.rakeR[1] != 0f) dx += m.rakeR[1] * Mathf.Max(0f, y - m.rakeR[0]) * SStep(-m.Le + 0.6f, -m.Le, x);
            return x + dx;
        }
        static float HwG(CarModel m, float x, float y) => Hp(m, x) - m.gh.beltIn - m.gh.tumble * Mathf.Max(0f, y - Top(m, x));
        static float GhY(CarModel m, float x)
        {
            var P = m.prof; if (x < P[0].x || x > P[P.Count - 1].x) return float.NaN;
            for (int i = 0; i < P.Count - 1; i++) if (x <= P[i + 1].x) return Mathf.Lerp(P[i].y, P[i + 1].y, (x - P[i].x) / Mathf.Max(1e-5f, P[i + 1].x - P[i].x));
            return P[P.Count - 1].y;
        }
        /// <summary>Front or rear face position (length axis) at a lateral offset and height: plan rounding and rake.</summary>
        static float FaceX(CarModel m, bool front, float lat, float y)
        {
            float rc = front ? m.rcF : m.rcR, topE = Top(m, front ? m.Le : -m.Le), hwE = m.hw * 0.975f;
            float zz = Mathf.Min(Mathf.Abs(lat) + Inset(m, y, topE), hwE - 0.004f), e = Mathf.Max(0f, zz - (hwE - rc));
            float x = (m.Le - rc + Mathf.Sqrt(Mathf.Max(0f, rc * rc - e * e))) * (front ? 1f : -1f);
            return Warp(m, x, y);
        }
        /// <summary>Point on the lower body side at length x and height y (side = +1 right, -1 left), pushed out by off.</summary>
        static Vector3 Side(CarModel m, float x, float y, int side, float off)
        {
            float lat = Hp(m, x) - Inset(m, y, Top(m, x)) + off;
            return new Vector3(lat * side, y, Warp(m, x, y));
        }

        // ---------------------------------------------------------------- materials
        static Material paintTpl, plateTpl, glass, glassHaze, trim, under, chrome, tyre, alloy, lampF, lampFRound, lampR, lampClear, amber, interior, grilleM, shadowM, steelM, discM;
        static readonly Dictionary<string, Material> plastic = new Dictionary<string, Material>();
        static Mesh wheelSteel, wheelCap, wheelAlloy, wheelBare;
        static Texture2D[] grime;

        static Material Tpl(string n, Func<Material> fallback) { var m = Resources.Load<Material>("CarMats/" + n); return m != null ? m : fallback(); }
        static void Init()
        {
            Load();
            if (glass != null) return;
            paintTpl = Tpl("Paint", () => Gfx.Mat(Color.white, null, 0.82f, 0.35f));
            plateTpl = Tpl("Plate", () => Gfx.Mat(Color.white, null, 0.35f));
            glass = Tpl("Glass", () => Gfx.Mat(new Color(0.05f, 0.07f, 0.09f), null, 0.95f, 0.2f));
            glassHaze = new Material(glass) { name = "GlassHaze" }; glassHaze.SetColor("_BaseColor", new Color(0.16f, 0.17f, 0.16f, 0.68f)); glassHaze.SetFloat("_Smoothness", 0.72f);
            trim = Tpl("Trim", () => Gfx.Mat(new Color(0.03f, 0.03f, 0.035f), null, 0.55f));
            under = Tpl("Under", () => Gfx.Mat(new Color(0.035f, 0.035f, 0.035f), null, 0.08f));
            chrome = Tpl("Chrome", () => Gfx.Mat(new Color(0.8f, 0.81f, 0.83f), null, 0.9f, 1f));
            tyre = Tpl("Tyre", () => Gfx.Mat(new Color(0.045f, 0.045f, 0.05f), null, 0.28f));
            alloy = Tpl("Alloy", () => Gfx.Mat(new Color(0.62f, 0.63f, 0.65f), null, 0.72f, 0.9f));
            lampF = Tpl("LampFront", () => Gfx.Emissive(new Color(1f, 0.96f, 0.85f), 1.6f));
            lampR = Tpl("LampRear", () => Gfx.Emissive(new Color(0.85f, 0.05f, 0.04f), 1.1f));
            // lamps with something inside them: reflector bowls behind the head lamp glass, ribbed lenses at the back
            lampFRound = LampMat(lampF, "LampFrontRound", 1, Color.white);
            lampF = LampMat(lampF, "LampFrontRect", 0, Color.white);
            lampClear = LampMat(lampF, "LampReverse", 3, new Color(0.9f, 0.9f, 0.88f)); lampClear.SetColor("_EmissionColor", new Color(0.25f, 0.25f, 0.24f));
            lampR = LampMat(lampR, "LampRearRibbed", 2, Color.white);
            amber = Tpl("LampAmber", () => Gfx.Emissive(new Color(1f, 0.5f, 0.05f), 0.6f));
            interior = Tpl("Interior", () => Gfx.Mat(new Color(0.07f, 0.07f, 0.075f), null, 0.15f));
            grilleM = Tpl("Grille", () => Gfx.Mat(new Color(0.02f, 0.02f, 0.025f), null, 0.35f, 0.2f));
            shadowM = Resources.Load<Material>("CarMats/Shadow");
            steelM = new Material(grilleM) { name = "SteelWheel" }; steelM.SetColor("_BaseColor", new Color(0.13f, 0.135f, 0.14f)); steelM.SetFloat("_Smoothness", 0.45f);
            discM = new Material(plateTpl) { name = "TaxDisc" }; discM.SetColor("_BaseColor", new Color(0.35f, 0.65f, 0.45f));
            wheelSteel = BuildWheel("steel"); wheelCap = BuildWheel("cap"); wheelAlloy = BuildWheel("alloy"); wheelBare = BuildWheel("bare");
            grime = new[] { GrimeTex(1, 0.35f, false), GrimeTex(2, 0.6f, false), GrimeTex(3, 0.85f, false), GrimeTex(4, 0.7f, true), GrimeTex(5, 0.9f, true) };
        }

        static Material LampMat(Material tpl, string name, int kind, Color tint)
        {
            var mm = new Material(tpl) { name = name };
            mm.SetTexture("_BaseMap", LampTex(kind, false)); mm.SetColor("_BaseColor", tint * (tpl.HasProperty("_BaseColor") ? 1f : 1f));
            if (kind != 3) { mm.SetTexture("_EmissionMap", LampTex(kind, true)); mm.EnableKeyword("_EMISSION"); }
            mm.SetFloat("_Smoothness", 0.92f);
            return mm;
        }

        /// <summary>0 rectangular head lamp (two reflector bowls), 1 round head lamp, 2 red ribbed tail lens, 3 clear ribbed reversing lens.</summary>
        static Texture2D LampTex(int kind, bool emit)
        {
            int W = kind == 1 ? 128 : 256, H = 128;
            var px = new Color32[W * H];
            float Bowl(float u, float v, float cu, float cv, float r)
            {
                float d = new Vector2((u - cu) * W, (v - cv) * H).magnitude / (r * H);
                return d; // 0 centre .. 1 rim
            }
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W, v = (y + 0.5f) / H;
                    Color c;
                    float edge = Mathf.Min(Mathf.Min(u * W, (1 - u) * W), Mathf.Min(v * H, (1 - v) * H));
                    if (kind <= 1)
                    {
                        // housing: brushed silver, darker low down
                        c = Color.Lerp(new Color(0.42f, 0.43f, 0.45f), new Color(0.7f, 0.71f, 0.72f), v);
                        float glow = 0f;
                        var bowls = kind == 1 ? new[] { (0.5f, 0.5f, 0.46f) } : new[] { (0.7f, 0.52f, 0.4f), (0.3f, 0.5f, 0.32f) };
                        foreach (var (cu, cv, r) in bowls)
                        {
                            float d = Bowl(u, v, cu, cv, r);
                            if (d >= 1.06f) continue;
                            if (d > 0.97f) { c = new Color(0.12f, 0.12f, 0.13f); continue; } // bowl rim
                            float ring = 0.5f + 0.5f * Mathf.Cos(d * 18f);
                            c = Color.Lerp(new Color(0.95f, 0.95f, 0.96f), new Color(0.55f, 0.56f, 0.58f), d * 0.8f) * (0.92f + 0.08f * ring);
                            if (d < 0.18f) { c = new Color(1f, 0.98f, 0.92f); glow = Mathf.Max(glow, 1f); }
                            else glow = Mathf.Max(glow, Mathf.Clamp01(1f - d) * 0.35f);
                        }
                        // diagonal sheen on the glass
                        float sheen = Mathf.Clamp01(1f - Mathf.Abs((u - v * 0.6f) - 0.25f) * 14f) * 0.18f;
                        c = Color.Lerp(c, Color.white, sheen);
                        if (edge < 3f) c = new Color(0.05f, 0.05f, 0.055f); // shut line round the lamp
                        if (emit) c = new Color(glow, glow, glow);
                    }
                    else
                    {
                        // ribbed lens: horizontal prisms, a darker frame, brighter where the bulb sits
                        float rib = 0.75f + 0.25f * Mathf.Abs(Mathf.Sin(v * Mathf.PI * 7f));
                        float bulb = Mathf.Clamp01(1f - new Vector2((u - 0.5f) * 1.6f, v - 0.5f).magnitude * 1.8f);
                        float b = rib * (0.75f + 0.25f * bulb);
                        c = kind == 2 ? new Color(0.62f * b, 0.06f * b, 0.05f * b) : new Color(0.85f * b, 0.85f * b, 0.84f * b);
                        if (edge < 4f) c *= 0.35f;
                        if (emit) { float e = edge < 4f ? 0f : 0.35f + 0.65f * bulb * rib; c = new Color(e, e, e); }
                    }
                    px[y * W + x] = c;
                }
            var t = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "Lamp" + kind + (emit ? "E" : "") };
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        static Material Plastic(string hex)
        {
            if (!plastic.TryGetValue(hex, out var m))
            {
                ColorUtility.TryParseHtmlString(hex, out var c);
                m = new Material(trim) { name = "Plastic " + hex }; m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", 0.3f);
                plastic[hex] = m;
            }
            return m;
        }

        /// <summary>Multiply texture for paint: road grime up the sills, speckle, and (rusty) brown corrosion round the arches and edges.</summary>
        static Texture2D GrimeTex(int seed, float amount, bool rust)
        {
            // u runs the length of the car (0 rear .. 1 front, + 0.5 for the other side), v = height / 1.55 m
            const int W = 256, H = 128;
            var rng = new System.Random(seed); float R() => (float)rng.NextDouble();
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float v = y / (float)H, u = x / (float)W;
                    float sill = Mathf.Clamp01(1f - v / 0.33f); sill = sill * sill * (3f - 2f * sill);
                    float streak = Mathf.PerlinNoise(u * 40f + seed, v * 3f) * 0.5f + Mathf.PerlinNoise(u * 7f, v * 2f + seed) * 0.5f;
                    float film = Mathf.PerlinNoise(u * 5f + seed * 1.7f, v * 4f) * 0.12f;
                    float dirt = amount * (sill * (0.45f + 0.35f * streak) + film * (1f - v));
                    px[y * W + x] = Color.Lerp(Color.white, new Color(0.62f, 0.57f, 0.49f), Mathf.Clamp01(dirt));
                }
            if (rust)
            {
                // rot along the bottom edge and the arch lips, broken up with noise
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float v = y / (float)H, u = (x / (float)W) % 0.5f * 2f;
                        float edge = Mathf.Clamp01(1f - v / 0.12f);
                        float arch = 0f;
                        foreach (float cu in new[] { 0.21f, 0.79f }) { float d = Mathf.Abs(u - cu); if (d < 0.09f) arch = Mathf.Max(arch, Mathf.Clamp01(1f - Mathf.Abs(v - (0.27f - d * d * 18f)) / 0.035f)); }
                        float n = Mathf.PerlinNoise(x * 0.18f + seed * 5f, y * 0.18f);
                        float a = Mathf.Clamp01((Mathf.Max(edge * 0.9f, arch) - 0.35f + n * 0.6f - 0.3f) * 2.2f);
                        if (a > 0f) px[y * W + x] = Color.Lerp(px[y * W + x], new Color(0.45f, 0.23f, 0.11f), a * 0.85f);
                    }
                for (int k = 0; k < 5; k++)
                {
                    int cx = rng.Next(W), cy = (int)(R() * H * 0.4f); float rad = 1.5f + R() * 2.5f;
                    for (int yy = -4; yy <= 4; yy++) for (int xx = -4; xx <= 4; xx++)
                    {
                        float d = Mathf.Sqrt(xx * xx + yy * yy) / rad; if (d > 1f) continue;
                        int X = (cx + xx + W) % W, Y = Mathf.Clamp(cy + yy, 0, H - 1);
                        px[Y * W + X] = Color.Lerp(px[Y * W + X], new Color(0.42f, 0.22f, 0.12f), (1f - d) * 0.8f);
                    }
                }
            }
            var t = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "CarGrime" + seed };
            t.SetPixels(px); t.Apply(true, true);
            return t;
        }

        // ---------------------------------------------------------------- per-car roughness
        class Rough
        {
            public bool rust, haze, dice, fresh, tax, tow, rack, ladder, rails, capMiss, panel, clutter, metallic;
            public string wheel; public float fade; public int grime, panelDoor; public Color panelCol, freshCol;
            public List<Vector4> dents = new List<Vector4>();
        }

        static Rough MakeRough(CarModel m, System.Random r, bool rough)
        {
            float R() => (float)r.NextDouble();
            var o = new Rough();
            o.metallic = R() < 0.3f;
            o.wheel = m.wheel;
            if (!rough) { o.grime = 0; o.fade = 0.03f; o.tax = true; return o; }
            o.fade = R() * R() * 0.32f;
            o.rust = R() < 0.3f;
            o.grime = o.rust ? 3 + r.Next(2) : r.Next(3);
            o.haze = R() < 0.4f;
            o.dice = R() < 0.18f; o.fresh = R() < 0.55f; o.tax = R() < 0.55f;
            o.freshCol = new[] { new Color(0.18f, 0.6f, 0.29f), new Color(0.75f, 0.2f, 0.18f), new Color(0.9f, 0.82f, 0.13f), new Color(0.63f, 0.25f, 0.75f), new Color(0.48f, 0.82f, 0.94f) }[r.Next(5)];
            o.tow = R() < (m.van ? 0.5f : 0.15f);
            o.rack = m.van && R() < 0.55f; o.ladder = o.rack && R() < 0.6f;
            o.rails = !m.van && m.L > 4f && R() < 0.12f;
            o.clutter = R() < 0.3f;
            if (o.wheel == "alloy" && R() < 0.35f) o.wheel = "cap";
            else if (o.wheel == "cap" && R() < 0.25f) o.wheel = "steel";
            o.capMiss = o.wheel == "cap" && R() < 0.3f;
            o.panel = R() < 0.22f;
            if (o.panel) { o.panelDoor = r.Next(m.doors >= 4 ? 4 : 2); o.panelCol = R() < 0.4f ? new Color(0.48f, 0.5f, 0.5f) : RandomPaint(r); }
            int dn = R() < 0.4f ? 1 + r.Next(3) : 0;
            for (int i = 0; i < dn; i++) o.dents.Add(new Vector4(Mathf.Lerp(m.wxR - 0.3f, m.wxF + 0.4f, R()), Mathf.Lerp(m.sill + 0.12f, Top(m, 0) - 0.12f, R()), r.Next(2) == 0 ? -1 : 1, 0.1f + R() * 0.12f));
            return o;
        }

        // ---------------------------------------------------------------- build
        const int R_ = 15; // points per half section: floor centre .. roof centre

        /// <summary>Builds the car under parent (local origin at ground centre, front = +z). Returns 4 wheel visuals FL, FR, RL, RR.</summary>
        public static Transform[] Build(Transform parent, Spec s, Color paintCol, bool castShadow = true, int seed = -1, bool rough = true)
        {
            if (CarKit.Has(s.m.id)) return CarKit.Build(parent, s, paintCol, castShadow, seed, rough);
            Init();
            var m = s.m; var G = m.gh;
            var rng = new System.Random(seed >= 0 ? seed : UnityEngine.Random.Range(0, int.MaxValue));
            var ro = MakeRough(m, rng, rough);
            Color faded = Color.Lerp(paintCol, Color.Lerp(paintCol, new Color(0.78f, 0.78f, 0.76f), 0.6f), ro.fade);
            var paint = new Material(paintTpl) { name = "CarPaint" };
            paint.SetColor("_BaseColor", faded);
            paint.SetTexture("_BaseMap", grime[Mathf.Clamp(ro.grime, 0, grime.Length - 1)]);
            paint.SetFloat("_Metallic", ro.metallic ? 0.45f : 0.04f);
            paint.SetFloat("_Smoothness", Mathf.Lerp(ro.metallic ? 0.82f : 0.74f, 0.42f, ro.fade * 2.5f + (ro.rust ? 0.15f : 0f)));
            Material panelM = paint;
            if (ro.panel) { panelM = new Material(paint) { name = "CarPanel" }; panelM.SetColor("_BaseColor", ro.panelCol); panelM.SetFloat("_Smoothness", 0.55f); }
            var glassM = ro.haze ? glassHaze : glass;

            int S = Mathf.Clamp(Mathf.RoundToInt(m.L / 0.05f), 60, 110);
            var xs = new float[S];
            for (int j = 0; j < S; j++) xs[j] = Mathf.Lerp(-m.Le, m.Le, j / (S - 1f));
            float vScale = m.van ? 2.15f : 1.55f;

            // ---- sections
            var P = new Vector3[S][]; var gv = new float[S];
            for (int j = 0; j < S; j++)
            {
                float x = xs[j], top = Top(m, x), yb = Mathf.Min(Yb(m, x), top - 0.1f), hp = Hp(m, x), cr = Crown(m, x);
                float gy = GhY(m, x); bool has = !float.IsNaN(gy) && gy > top + 0.005f;
                float g = has ? SStep(0f, 0.14f, gy - top) : 0f; gv[j] = g;
                var row = new Vector3[R_];
                row[0] = new Vector3(0f, yb, x);
                row[1] = new Vector3(Mathf.Max(0f, hp - Inset(m, yb, top) - 0.06f), yb, x);
                float[] f = { 0.03f, 0.2f, 0.42f, 0.62f, 0.8f, 0.93f };
                for (int k = 0; k < 6; k++) { float y = Mathf.Lerp(yb, top, f[k]); row[2 + k] = new Vector3(hp - Inset(m, y, top), y, x); }
                float shoulder = hp - Inset(m, top, top);
                row[8] = new Vector3(shoulder, top, x);
                // bonnet / boot deck: shoulder to centre with the crown
                float[] fd = { 0.1f, 0.3f, 0.55f, 0.75f, 0.9f, 1f };
                var deck = new Vector3[6];
                for (int k = 0; k < 6; k++) { float t = fd[k]; deck[k] = new Vector3(Mathf.Lerp(shoulder, 0f, t), top + cr * (1f - (1f - t) * (1f - t)) + 0.004f * (1 - t), x); }
                if (g > 0f)
                {
                    float yL = top + G.pBelt, yU = Mathf.Max(gy - G.pTop, yL + 0.002f), yM = (yL + yU) * 0.5f, rw = Mathf.Max(0.05f, HwG(m, x, gy));
                    var gh = new[]
                    {
                        new Vector3(HwG(m, x, yL), yL, x), new Vector3(HwG(m, x, yM), yM, x), new Vector3(HwG(m, x, yU), yU, x),
                        new Vector3(rw - Mathf.Min(0.05f, rw * 0.3f), gy - 0.004f, x), new Vector3(rw * 0.5f, gy + G.crown * 0.8f, x), new Vector3(0f, gy + G.crown, x)
                    };
                    for (int k = 0; k < 6; k++) row[9 + k] = Vector3.Lerp(deck[k], gh[k], g);
                }
                else for (int k = 0; k < 6; k++) row[9 + k] = deck[k];
                P[j] = row;
            }
            // dents: push the side in round a few points
            foreach (var d in ro.dents)
                for (int j = 0; j < S; j++)
                    for (int r = 2; r <= 8; r++)
                    {
                        var p = P[j][r]; float dd = new Vector2(p.z - d.x, p.y - d.y).magnitude / d.w;
                        if (dd < 1f) P[j][r].x -= 0.02f * (1f - dd * dd) * (1f - dd * dd);
                    }
            // rake the ends
            for (int j = 0; j < S; j++) for (int r = 0; r < R_; r++) { var p = P[j][r]; P[j][r] = new Vector3(p.x, p.y, Warp(m, p.z, p.y)); }

            // normals (right side), outward
            var N = new Vector3[S][];
            for (int j = 0; j < S; j++)
            {
                N[j] = new Vector3[R_];
                for (int r = 0; r < R_; r++)
                {
                    Vector3 ta = P[Mathf.Min(j + 1, S - 1)][r] - P[Mathf.Max(j - 1, 0)][r];
                    Vector3 tb = P[j][Mathf.Min(r + 1, R_ - 1)] - P[j][Mathf.Max(r - 1, 0)];
                    Vector3 nn = Vector3.Cross(ta, tb);
                    Vector3 c = new Vector3(0f, (P[j][0].y + P[j][R_ - 1].y) * 0.5f, P[j][r].z);
                    if (Vector3.Dot(nn, P[j][r] - c) < 0f) nn = -nn;
                    if (nn.sqrMagnitude < 1e-10f) nn = P[j][r] - c;
                    if (r == 0 || r == R_ - 1) nn.x = 0f;
                    N[j][r] = nn.normalized;
                }
            }

            // ---- glass and panels
            float xBm = G.xB != null && G.xB.Length == 2 ? (G.xB[0] + G.xB[1]) * 0.5f : 99f;
            float xQm = G.xQ != null && G.xQ.Length == 2 ? (G.xQ[0] + G.xQ[1]) * 0.5f : 99f;
            float gx0 = G.van ? xBm + G.bw * 0.5f : G.rrx + G.pC * 0.6f, gx1 = G.fb - G.pA;
            bool SideGlass(float xm, float gm) => gm > 0.85f && xm > gx0 && xm < gx1 && Mathf.Abs(xm - xBm) > G.bw * 0.5f && Mathf.Abs(xm - xQm) > G.qw * 0.5f && GhY(m, xm) - Top(m, xm) > 0.12f;
            bool Pillar(float xm, float gm) => gm > 0.85f && (Mathf.Abs(xm - xBm) <= G.bw * 0.5f || Mathf.Abs(xm - xQm) <= G.qw * 0.5f) && xm > G.rb && xm < G.fb;
            // door panels between seams (for a mismatched door)
            float fdF = m.seamFdF != null && m.seamFdF.Length > 0 ? m.seamFdF[0] : G.fb, fdR = m.seamFdR != null && m.seamFdR.Length > 0 ? m.seamFdR[0] : 0f, rdR = m.seamRdR != null && m.seamRdR.Length > 0 ? m.seamRdR[0] : fdR;
            bool InDoor(float xm, int side)
            {
                if (!ro.panel) return false;
                int door = ro.panelDoor; int ds = door % 2 == 0 ? 1 : -1; bool front = door < 2;
                if (ds != side) return false;
                return front ? xm < fdF && xm > fdR : (m.doors >= 4 && xm < fdR && xm > rdR);
            }
            Material QuadMat(int j, int r, int side)
            {
                float xm = (xs[j] + xs[j + 1]) * 0.5f, gm = Mathf.Min(gv[j], gv[j + 1]);
                if (r == 0) return under;
                if (r == 1) return trim;
                bool sg = SideGlass(xm, gm);
                if (r <= 7) return InDoor(xm, side) ? panelM : paint;
                if (r == 8) return sg ? trim : (InDoor(xm, side) ? panelM : paint);
                if (r == 9 || r == 10) { if (sg) return glassM; if (Pillar(xm, gm)) return trim; return paint; }
                if (r == 11) return paint;
                if (gm > 0.3f && (xm > m.profC2 + 0.02f || (xm < m.profB1 - 0.02f && xm > G.rb + 0.04f && !G.van))) return glassM; // windscreen / rear screen
                return paint;
            }

            var k2 = new MeshKit();
            var idx = new int[2, S, R_];
            for (int sd = 0; sd < 2; sd++)
            {
                float sx = sd == 0 ? 1f : -1f;
                for (int j = 0; j < S; j++)
                    for (int r = 0; r < R_; r++)
                    {
                        var p = P[j][r]; var q = N[j][r];
                        idx[sd, j, r] = k2.Vert(new Vector3(p.x * sx, p.y, p.z), new Vector3(q.x * sx, q.y, q.z), new Vector2((p.z + m.Le) / m.L + (sd == 0 ? 0f : 0.5f), p.y / vScale));
                    }
            }
            for (int sd = 0; sd < 2; sd++)
                for (int j = 0; j < S - 1; j++)
                    for (int r = 0; r < R_ - 1; r++)
                    {
                        int a = idx[sd, j, r], b = idx[sd, j + 1, r], c = idx[sd, j + 1, r + 1], d = idx[sd, j, r + 1];
                        Vector3 o = k2.N[a] + k2.N[b] + k2.N[c] + k2.N[d];
                        var mat = QuadMat(j, r, sd == 0 ? 1 : -1);
                        TriOut(k2, mat, a, b, c, o); TriOut(k2, mat, a, c, d, o);
                    }
            Cap(k2, P[0], paint, Vector3.back); Cap(k2, P[S - 1], paint, Vector3.forward);

            // ---- inner shell behind the glass
            for (int sd = 0; sd < 2; sd++)
            {
                float sx = sd == 0 ? 1f : -1f;
                var inner = new int[S, R_];
                for (int j = 0; j < S; j++)
                    for (int r = 0; r < R_; r++)
                    {
                        var p = P[j][r]; var q = N[j][r];
                        inner[j, r] = k2.Vert(new Vector3(p.x * sx, p.y, p.z) - new Vector3(q.x * sx, q.y, q.z) * 0.025f, new Vector3(-q.x * sx, -q.y, -q.z), Vector2.zero);
                    }
                for (int j = 0; j < S - 1; j++)
                {
                    if (gv[j] < 0.3f && gv[j + 1] < 0.3f) continue;
                    for (int r = 0; r < R_ - 1; r++)
                    {
                        if (r == 9 || r == 10) continue;
                        float xm = (xs[j] + xs[j + 1]) * 0.5f;
                        if (r >= 11 && (xm > m.profC2 || xm < m.profB1)) continue; // see-through screens
                        int a = inner[j, r], b = inner[j + 1, r], c = inner[j + 1, r + 1], d = inner[j, r + 1];
                        Vector3 o = k2.N[a] + k2.N[b] + k2.N[c] + k2.N[d];
                        TriOut(k2, interior, a, b, c, o); TriOut(k2, interior, a, c, d, o);
                    }
                }
            }

            // ---- ends: bumpers, lamps, grille, plates
            var reg = Registration(rng);
            Bumper(k2, m, true, m.bumpF, paint, rng);
            Bumper(k2, m, false, m.bumpR, paint, rng);
            var plateF = new Material(plateTpl) { name = "PlateF" }; plateF.SetTexture("_BaseMap", PlateTex(reg, false)); plateF.SetColor("_BaseColor", Color.Lerp(Color.white, new Color(0.8f, 0.76f, 0.66f), ro.grime * 0.15f));
            var plateR = new Material(plateTpl) { name = "PlateR" }; plateR.SetTexture("_BaseMap", PlateTex(reg, true)); plateR.SetColor("_BaseColor", plateF.GetColor("_BaseColor"));
            float pfY = m.bumpF.plateY > 0 ? m.bumpF.plateY : (m.bumpF.y0 + m.bumpF.y1) * 0.5f;
            PlateQuad(k2, plateF, new Vector3(0, pfY, FaceX(m, true, 0, pfY) + m.bumpF.@out + 0.008f), 0.52f, 0.11f, 1f);
            float prY = m.plateRy; float prZ = m.plateROnBumper ? FaceX(m, false, 0, prY) - m.bumpR.@out - 0.008f : FaceX(m, false, 0, prY) - 0.012f;
            PlateQuad(k2, plateR, new Vector3(0, prY, prZ), 0.52f, 0.11f, -1f);
            // grille
            var gr = m.grille;
            if (gr != null && gr.z > 0f)
            {
                FaceGrid(k2, grilleM, m, true, -gr.z, gr.z, gr.y0, gr.y0, gr.y1, gr.y1, 0.006f, 6, 2, false);
                for (int b = 0; b < gr.bars; b++)
                {
                    float y = Mathf.Lerp(gr.y0, gr.y1, (b + 1f) / (gr.bars + 1f));
                    FaceGrid(k2, gr.chrome > 0 ? chrome : trim, m, true, -gr.z * 0.96f, gr.z * 0.96f, y - 0.006f, y - 0.006f, y + 0.006f, y + 0.006f, 0.01f, 6, 1, false);
                }
                if (gr.chrome > 0) { FaceGrid(k2, chrome, m, true, -gr.z - 0.01f, gr.z + 0.01f, gr.y1, gr.y1, gr.y1 + 0.012f, gr.y1 + 0.012f, 0.008f, 6, 1, false); }
            }
            // head lamps
            var hd = m.head;
            foreach (int sd in new[] { 1, -1 })
            {
                if (hd.twin > 0)
                {
                    float w = (hd.z1 - hd.z0) * 0.42f, yc = (hd.yt[0] + hd.yb[0]) * 0.5f, hh = (hd.yt[0] - hd.yb[0]) * 0.46f;
                    foreach (float c in new[] { hd.z0 + w * 0.5f + 0.01f, hd.z1 - w * 0.5f - 0.01f })
                    {
                        FaceGrid(k2, chrome, m, true, sd * (c - w * 0.55f), sd * (c + w * 0.55f), yc - hh * 1.1f, yc - hh * 1.1f, yc + hh * 1.1f, yc + hh * 1.1f, 0.005f, 3, 2, false);
                        FaceGrid(k2, lampFRound, m, true, sd * (c - w * 0.45f), sd * (c + w * 0.45f), yc - hh, yc - hh, yc + hh, yc + hh, 0.009f, 3, 2, false);
                    }
                }
                else
                {
                    FaceGrid(k2, chrome, m, true, sd * (hd.z0 - 0.008f), sd * (hd.z1 + 0.004f), hd.yb[0] - 0.008f, hd.yb[1] - 0.008f, hd.yt[0] + 0.008f, hd.yt[1] + 0.008f, 0.004f, 6, 2, false);
                    FaceGrid(k2, lampF, m, true, sd * hd.z0, sd * (m.indBumper ? hd.z1 : hd.z1 - 0.07f), hd.yb[0], hd.yb[1], hd.yt[0], hd.yt[1], 0.008f, 6, 2, false);
                    if (!m.indBumper) FaceGrid(k2, amber, m, true, sd * (hd.z1 - 0.065f), sd * hd.z1, hd.yb[1], hd.yb[1], hd.yt[1], hd.yt[1], 0.008f, 2, 2, false);
                }
                if (m.indBumper) { float y = m.bumpF.y1 - 0.07f; FaceGrid(k2, amber, m, true, sd * (m.hw * 0.975f - 0.2f), sd * (m.hw * 0.975f - 0.06f), y - 0.03f, y - 0.03f, y + 0.03f, y + 0.03f, m.bumpF.@out + 0.006f, 2, 1, false); }
                // tail lamps (+ reversing light inboard)
                var tl = m.tail;
                float zi = tl.z0, zo = tl.z1;
                FaceGrid(k2, lampR, m, false, sd * (zi + (zo - zi) * 0.22f), sd * zo, tl.yb[0], tl.yb[1], tl.yt[0], tl.yt[1], 0.008f, 4, 2, false);
                FaceGrid(k2, lampClear, m, false, sd * zi, sd * (zi + (zo - zi) * 0.22f), tl.yb[0], tl.yb[0], tl.yt[0], tl.yt[0], 0.008f, 2, 2, false);
                FaceGrid(k2, amber, m, false, sd * (zi + (zo - zi) * 0.22f), sd * (zi + (zo - zi) * 0.45f), tl.yb[0], tl.yb[0], tl.yb[0] + (tl.yt[0] - tl.yb[0]) * 0.35f, tl.yb[0] + (tl.yt[0] - tl.yb[0]) * 0.35f, 0.01f, 2, 1, false);
            }
            if (m.tail.band > 0) FaceGrid(k2, Plastic("#3a0c0c"), m, false, -m.tail.z0, m.tail.z0, m.tail.yb[0] + 0.02f, m.tail.yb[0] + 0.02f, m.tail.yt[0] - 0.02f, m.tail.yt[0] - 0.02f, 0.007f, 6, 1, false);

            // ---- sides: cladding, moulding, seams, handles, mirrors, fuel flap, aerial
            float xa = m.wxR + m.archR + 0.03f, xb = m.wxF - m.archR - 0.03f;
            foreach (int sd in new[] { 1, -1 })
            {
                if (m.clad != null && m.clad.on) SideStrip(k2, m, xa, xb, m.clad.y0, m.clad.y1, sd, 0.008f, Plastic(m.clad.hex));
                if (m.moulding != null && m.moulding.on)
                {
                    var mm = m.moulding.hex == "body" ? paint : Plastic(m.moulding.hex);
                    SideStrip(k2, m, xa, xb, m.moulding.y - m.moulding.h * 0.5f, m.moulding.y + m.moulding.h * 0.5f, sd, 0.01f, mm);
                    if (!string.IsNullOrEmpty(m.moulding.stripe)) SideStrip(k2, m, xa, xb, m.moulding.y - 0.006f, m.moulding.y + 0.006f, sd, 0.013f, Plastic(m.moulding.stripe));
                }
                Seam(k2, m, m.seamFdF, sd); Seam(k2, m, m.seamFdR, sd); Seam(k2, m, m.seamRdR, sd);
                if (m.handles != null)
                    foreach (float hx in m.handles)
                    {
                        float y = Top(m, hx) - 0.09f; var c = Side(m, hx, y, sd, 0.008f);
                        k2.Box(m.handleBody ? paint : trim, c + new Vector3(-0.012f, -0.014f, -0.07f), c + new Vector3(0.012f, 0.014f, 0.07f));
                    }
                // mirror at the A-pillar foot
                {
                    float mx = G.fb - 0.08f, my = Top(m, mx) + 0.04f; float lat = HwG(m, mx, my);
                    var mmat = m.mirrorBody ? paint : trim;
                    // short stalk off the door, then a rounded housing with the glass facing back
                    k2.Box(trim, new Vector3(sd > 0 ? lat - 0.01f : -lat - 0.05f, my + 0.005f, mx - 0.03f), new Vector3(sd > 0 ? lat + 0.05f : -lat + 0.01f, my + 0.035f, mx + 0.03f));
                    var hc = new Vector3(sd * (lat + 0.115f), my + 0.055f, mx - 0.015f);
                    k2.Sphere(mmat, hc, new Vector3(0.085f, 0.052f, 0.042f), 14, 8);
                    k2.Push(Matrix4x4.TRS(hc + new Vector3(0f, 0f, -0.03f), Quaternion.identity, Vector3.one));
                    k2.Sphere(chrome, Vector3.zero, new Vector3(0.072f, 0.042f, 0.014f), 14, 6);
                    k2.Pop();
                }
                if (m.fuel == sd)
                {
                    float fx = m.wxR + m.archR * 0.2f + 0.12f, fy = Top(m, fx) - 0.15f;
                    var a0 = Side(m, fx - 0.08f, fy - 0.06f, sd, 0.004f); var a1 = Side(m, fx + 0.08f, fy + 0.06f, sd, 0.004f);
                    SeamLine(k2, Side(m, fx - 0.08f, fy - 0.06f, sd, 0.004f), Side(m, fx + 0.08f, fy - 0.06f, sd, 0.004f), sd);
                    SeamLine(k2, Side(m, fx - 0.08f, fy + 0.06f, sd, 0.004f), Side(m, fx + 0.08f, fy + 0.06f, sd, 0.004f), sd);
                    SeamLine(k2, Side(m, fx - 0.08f, fy - 0.06f, sd, 0.004f), Side(m, fx - 0.08f, fy + 0.06f, sd, 0.004f), sd);
                    SeamLine(k2, Side(m, fx + 0.08f, fy - 0.06f, sd, 0.004f), Side(m, fx + 0.08f, fy + 0.06f, sd, 0.004f), sd);
                }
            }
            if (!string.IsNullOrEmpty(m.aerial))
            {
                Vector3 b; float lean = -0.35f;
                if (m.aerial == "wing") { float ax = G.fb + 0.12f; b = new Vector3(-(Hp(m, ax) - 0.12f), Top(m, ax), ax); }
                else { float ax = m.aerial == "roofF" ? G.fr - 0.12f : G.rrx + 0.12f; b = new Vector3(0f, G.roof + G.crown, ax); }
                k2.Push(Matrix4x4.TRS(b, Quaternion.Euler(lean * Mathf.Rad2Deg, 0f, 0f), Vector3.one));
                k2.Box(trim, new Vector3(-0.004f, 0f, -0.004f), new Vector3(0.004f, m.aerial == "wing" ? 0.75f : 0.42f, 0.004f));
                k2.Pop();
            }

            // ---- roof extras
            float roofY = G.roof + G.crown;
            if (ro.rack)
            {
                float rx0 = G.rrx + 0.05f, rx1 = G.fr - 0.05f, rw = HwG(m, (rx0 + rx1) * 0.5f, G.roof) - 0.05f;
                for (int b = 0; b < 4; b++) { float bx = Mathf.Lerp(rx0, rx1, b / 3f); k2.Box(steelM, new Vector3(-rw, roofY + 0.06f, bx - 0.02f), new Vector3(rw, roofY + 0.1f, bx + 0.02f)); }
                foreach (int sd in new[] { -1, 1 }) k2.Box(steelM, new Vector3(sd * rw - 0.02f, roofY + 0.1f, rx0), new Vector3(sd * rw + 0.02f, roofY + 0.14f, rx1));
                if (ro.ladder)
                {
                    var wood = Plastic("#9a8a6a");
                    foreach (float lx in new[] { -0.22f, 0.22f }) k2.Box(wood, new Vector3(lx - 0.025f, roofY + 0.14f, rx0 - 0.3f), new Vector3(lx + 0.025f, roofY + 0.2f, rx1 + 0.15f));
                    for (float z = rx0 - 0.2f; z < rx1 + 0.1f; z += 0.3f) k2.Box(wood, new Vector3(-0.22f, roofY + 0.16f, z - 0.015f), new Vector3(0.22f, roofY + 0.18f, z + 0.015f));
                }
            }
            if (ro.rails)
                foreach (int sd in new[] { -1, 1 })
                {
                    float rx0 = G.rrx + 0.02f, rx1 = G.fr - 0.05f, rw = HwG(m, (rx0 + rx1) * 0.5f, G.roof) - 0.12f;
                    k2.Box(trim, new Vector3(sd * rw - 0.02f, roofY + 0.02f, rx0), new Vector3(sd * rw + 0.02f, roofY + 0.07f, rx1));
                }
            if (ro.tow) { float tz = -m.Le - m.bumpR.@out - 0.12f; k2.Box(steelM, new Vector3(-0.03f, 0.3f, tz), new Vector3(0.03f, 0.36f, -m.Le + 0.1f)); k2.Box(chrome, new Vector3(-0.03f, 0.36f, tz - 0.03f), new Vector3(0.03f, 0.42f, tz + 0.03f)); }

            // ---- interior: seats, dash, steering wheel on the right, dice / air freshener, tax disc
            {
                float waist = Top(m, (G.fb + G.rb) * 0.5f);
                float cabH = G.roof - waist;
                float fsx = G.fb - (m.van ? 1.05f : 1.2f);
                float wIn = Hp(m, fsx) - 0.12f;
                float RoomAt(float x) { float gy = GhY(m, x); return (float.IsNaN(gy) ? G.roof : gy) - 0.06f - waist; } // headroom so headrests stay inside
                foreach (float zc in new[] { -0.4f, 0.4f }) SeatBack(k2, new Vector3(zc * wIn / 0.75f, waist, fsx), 0.44f, Mathf.Min(cabH, RoomAt(fsx - 0.1f) / 0.85f));
                if (!m.van && m.seatR > -50f) { float rsx = Mathf.Max(G.rb + (m.L > 4.4f ? 0.55f : 0.4f), G.rrx + 0.12f); if (rsx < fsx - 0.6f) { foreach (float zc in new[] { -0.5f, 0f, 0.5f }) SeatBack(k2, new Vector3(zc * wIn / 0.75f, waist, rsx), zc == 0f ? 0.4f : 0.48f, Mathf.Min(cabH, RoomAt(rsx - 0.1f) / 0.85f)); } }
                if (m.van) k2.Box(interior, new Vector3(-wIn, m.floor, fsx - 0.42f), new Vector3(wIn, G.roof - 0.05f, fsx - 0.38f));
                float dashX = G.fb - 0.06f;
                k2.Box(interior, new Vector3(-wIn, waist - 0.22f, dashX - 0.45f), new Vector3(wIn, waist + 0.03f, dashX));
                var swc = new Vector3(0.4f * wIn / 0.75f, waist + 0.06f, dashX - 0.5f);
                k2.Push(Matrix4x4.TRS(swc, Quaternion.Euler(-62f, 0f, 0f), Vector3.one)); k2.Cyl(trim, new Vector3(0, -0.015f, 0), 0.18f, 0.03f, 14, true); k2.Pop();
                // rear-view mirror with whatever's hanging off it
                float rvx = G.fr - 0.06f, rvy = G.roof - 0.06f;
                k2.Box(trim, new Vector3(-0.11f, rvy - 0.025f, rvx - 0.02f), new Vector3(0.11f, rvy + 0.025f, rvx + 0.02f));
                if (ro.dice) { k2.Box(Plastic("#f4f4f0"), new Vector3(-0.045f, rvy - 0.16f, rvx - 0.025f), new Vector3(0.005f, rvy - 0.11f, rvx + 0.025f)); k2.Box(Plastic("#c62828"), new Vector3(0.01f, rvy - 0.19f, rvx - 0.025f), new Vector3(0.06f, rvy - 0.14f, rvx + 0.025f)); }
                if (ro.fresh) { var fm = Plastic("#" + ColorUtility.ToHtmlStringRGB(ro.freshCol)); k2.Box(fm, new Vector3(-0.004f, rvy - 0.2f, rvx - 0.045f), new Vector3(0.004f, rvy - 0.08f, rvx + 0.045f)); }
                if (ro.tax)
                {
                    float ty = Top(m, G.fb) + 0.08f; float tx = G.fb - 0.05f;
                    k2.Push(Matrix4x4.TRS(new Vector3(-0.5f * Hp(m, tx) / 0.85f, ty, tx), Quaternion.Euler(-35f, 0f, 0f), Vector3.one));
                    k2.Box(discM, new Vector3(-0.035f, -0.035f, -0.003f), new Vector3(0.035f, 0.035f, 0.003f));
                    k2.Pop();
                }
                if (ro.clutter)
                {
                    var cl = new[] { Plastic("#b08850"), Plastic("#2a5aa0"), Plastic("#d8d0c0") };
                    for (int c = 0; c < 3; c++) { float cx = Mathf.Lerp(-0.4f, 0.4f, (float)rng.NextDouble()), cz = G.rb + 0.2f + (float)rng.NextDouble() * 0.4f; k2.Box(cl[c], new Vector3(cx - 0.12f, waist - 0.05f, cz - 0.1f), new Vector3(cx + 0.12f, waist + 0.1f, cz + 0.1f)); }
                }
            }

            var body = k2.Build("Body", parent, false, castShadow ? ShadowCastingMode.On : ShadowCastingMode.Off);
            var bmr = body.GetComponent<MeshRenderer>(); bmr.lightProbeUsage = LightProbeUsage.BlendProbes; bmr.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;

            if (shadowM != null)
            {
                var sh = new GameObject("ContactShadow"); sh.transform.SetParent(parent, false); sh.transform.localPosition = new Vector3(0, 0.025f, 0);
                var shm = new Mesh { name = "CarShadow" }; float hw2 = m.hw + 0.2f, hl = m.Le + 0.25f;
                shm.vertices = new[] { new Vector3(-hw2, 0, -hl), new Vector3(hw2, 0, -hl), new Vector3(hw2, 0, hl), new Vector3(-hw2, 0, hl) };
                shm.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
                shm.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up }; shm.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                sh.AddComponent<MeshFilter>().sharedMesh = shm;
                var shr = sh.AddComponent<MeshRenderer>(); shr.sharedMaterial = shadowM; shr.shadowCastingMode = ShadowCastingMode.Off; shr.receiveShadows = false; shr.lightProbeUsage = LightProbeUsage.Off; shr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }

            // ---- wheels
            var wheels = new Transform[4];
            float[] wz = { m.wxF, m.wxF, m.wxR, m.wxR };
            for (int i = 0; i < 4; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                var g = new GameObject("Wheel" + i); g.transform.SetParent(parent, false);
                g.transform.localPosition = new Vector3(side * (Hp(m, wz[i]) - m.tw * 0.5f - 0.025f), m.rad, wz[i]);
                g.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                g.transform.localScale = new Vector3(m.rad, m.tw / 0.2f, m.rad);
                string wt = ro.wheel; if (ro.capMiss && i == 2) wt = "bare";
                var mesh = wt == "alloy" ? wheelAlloy : wt == "cap" ? wheelCap : wt == "bare" ? wheelBare : wheelSteel;
                g.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = g.AddComponent<MeshRenderer>();
                mr.sharedMaterials = new[] { tyre, wt == "alloy" || wt == "cap" ? alloy : steelM, under };
                mr.shadowCastingMode = castShadow ? ShadowCastingMode.On : ShadowCastingMode.Off; mr.lightProbeUsage = LightProbeUsage.BlendProbes;
                wheels[i] = g.transform;
            }
            return wheels;
        }

        // ---------------------------------------------------------------- detail helpers
        static void TriOut(MeshKit k, Material m, int a, int b, int c, Vector3 outward)
        {
            Vector3 pa = k.V[a], pb = k.V[b], pc = k.V[c];
            if (Vector3.Dot(Vector3.Cross(pb - pa, pc - pa), outward) >= 0f) k.Tri(m, a, b, c); else k.Tri(m, a, c, b);
        }

        static void Cap(MeshKit k, Vector3[] sec, Material m, Vector3 n)
        {
            Vector3 c = new Vector3(0, (sec[0].y + sec[R_ - 1].y) / 2f, (sec[0].z + sec[R_ - 1].z) * 0.5f);
            for (int side = -1; side <= 1; side += 2)
                for (int e = 0; e < R_ - 1; e++) k.TriN(m, c, new Vector3(sec[e].x * side, sec[e].y, sec[e].z), new Vector3(sec[e + 1].x * side, sec[e + 1].y, sec[e + 1].z), n);
        }

        /// <summary>A patch lying on the front/rear face: lateral from l0 to l1, bottom yb0→yb1, top yt0→yt1 (both along lateral), pushed out by off.</summary>
        static void FaceGrid(MeshKit k, Material mat, CarModel m, bool front, float l0, float l1, float yb0, float yb1, float yt0, float yt1, float off, int nu, int nv, bool unused)
        {
            float fs = front ? 1f : -1f;
            var id = new int[nu + 1, nv + 1];
            for (int i = 0; i <= nu; i++)
                for (int j = 0; j <= nv; j++)
                {
                    float u = i / (float)nu, v = j / (float)nv;
                    float lat = Mathf.Lerp(l0, l1, u), y = Mathf.Lerp(Mathf.Lerp(yb0, yb1, u), Mathf.Lerp(yt0, yt1, u), v);
                    float z = FaceX(m, front, lat, y);
                    float latOut = Mathf.Clamp01((Mathf.Abs(lat) - (m.hw * 0.975f - (front ? m.rcF : m.rcR))) / Mathf.Max(0.05f, front ? m.rcF : m.rcR));
                    Vector3 n = new Vector3(Mathf.Sign(lat) * latOut, 0f, fs * (1f - latOut * 0.6f)).normalized;
                    id[i, j] = k.Vert(new Vector3(lat, y, z) + n * off, n, new Vector2(u, v));
                }
            for (int i = 0; i < nu; i++)
                for (int j = 0; j < nv; j++)
                {
                    int a = id[i, j], b = id[i + 1, j], c = id[i + 1, j + 1], d = id[i, j + 1];
                    Vector3 o = k.N[a] + k.N[b] + k.N[c] + k.N[d];
                    TriOut(k, mat, a, b, c, o); TriOut(k, mat, a, c, d, o);
                }
        }

        /// <summary>
        /// Bumper moulded round the end of the car: a rounded section (soft top and bottom edges, slightly bowed face)
        /// swept across the face and round the corners, then along the sides, thinning until it melts into the wings.
        /// </summary>
        static void Bumper(MeshKit k, CarModel m, bool front, CarBump b, Material paint, System.Random rng)
        {
            if (b == null) return;
            var mat = string.IsNullOrEmpty(b.col) || b.col == "body" ? paint : Plastic(b.col);
            float fs = front ? 1f : -1f, outw = Mathf.Max(0.02f, b.@out);
            float hwE = m.hw * 0.975f, rc = front ? m.rcF : m.rcR;
            float h = b.y1 - b.y0, yMid = (b.y0 + b.y1) * 0.5f;
            float rb = Mathf.Min(0.04f, h * 0.32f), rt = Mathf.Min(0.045f, h * 0.36f);
            // section (outward offset, height), bottom-inside to top-inside
            var prof = new List<Vector2> { new Vector2(-0.02f, b.y0 + rb * 0.3f) };
            for (int i = 0; i <= 3; i++) { float a = Mathf.PI * 0.5f * i / 3f; prof.Add(new Vector2(outw - rb + Mathf.Sin(a) * rb, b.y0 + rb - Mathf.Cos(a) * rb)); }
            prof.Add(new Vector2(outw + Mathf.Min(0.008f, outw * 0.15f), yMid));
            for (int i = 0; i <= 3; i++) { float a = Mathf.PI * 0.5f * i / 3f; prof.Add(new Vector2(outw - rt + Mathf.Cos(a) * rt, b.y1 - rt + Mathf.Sin(a) * rt)); }
            prof.Add(new Vector2(-0.015f, b.y1 - 0.004f));
            int np = prof.Count;
            // sweep: along the right side from the wing to the corner, across the face, back along the left side
            var stations = new List<(Func<float, float, Vector3> pos, Vector3 n, float scale)>();
            float xEnd = front ? m.Le - rc - 0.32f : -m.Le + rc + 0.32f, xCorner = front ? m.Le - rc * 0.6f : -m.Le + rc * 0.6f;
            // never run the side return into the wheel arch
            float archEdge = front ? m.wxF + m.archR + 0.03f : m.wxR - m.archR - 0.03f;
            xEnd = front ? Mathf.Max(xEnd, archEdge) : Mathf.Min(xEnd, archEdge);
            bool sides = front ? xEnd < xCorner - 0.04f : xEnd > xCorner + 0.04f;
            void SideStations(int sd, bool toCorner)
            {
                if (!sides) return;
                for (int i = 0; i <= 4; i++)
                {
                    float t = toCorner ? i / 4f : 1f - i / 4f;
                    float x = Mathf.Lerp(xEnd, xCorner, t), sc = Mathf.Lerp(0.15f, 0.8f, t * t * (3f - 2f * t));
                    stations.Add(((o, y) => Side(m, x, y, sd, o), new Vector3(sd, 0f, 0f), sc));
                }
            }
            SideStations(-1, true);
            int nf = 22;
            for (int i = 0; i <= nf; i++)
            {
                float lat = Mathf.Lerp(-hwE + 0.01f, hwE - 0.01f, i / (float)nf);
                float t = Mathf.Clamp01((Mathf.Abs(lat) - (hwE - rc)) / Mathf.Max(0.05f, rc));
                Vector3 nn = new Vector3(Mathf.Sign(lat) * t, 0f, fs * (1f - t * 0.7f)).normalized;
                float l = lat;
                stations.Add(((o, y) => new Vector3(l, y, FaceX(m, front, l, y)) + nn * o, nn, Mathf.Lerp(1f, 0.8f, t)));
            }
            SideStations(1, false);
            int ns = stations.Count;
            var id = new int[ns, np];
            for (int si = 0; si < ns; si++)
            {
                var st = stations[si];
                for (int pi = 0; pi < np; pi++)
                {
                    var pr = prof[pi];
                    float o = pr.x > 0f ? pr.x * st.scale : pr.x;
                    // section tangent -> normal (blend of the station's outward direction and up/down)
                    Vector2 tg = prof[Mathf.Min(pi + 1, np - 1)] - prof[Mathf.Max(pi - 1, 0)];
                    Vector2 n2 = new Vector2(tg.y, -tg.x).normalized; // outward in (o, y)
                    Vector3 nrm = (st.n * n2.x + Vector3.up * n2.y).normalized;
                    id[si, pi] = k.Vert(st.pos(o, pr.y), nrm, new Vector2(si / (float)(ns - 1), pi / (float)(np - 1)));
                }
            }
            for (int si = 0; si < ns - 1; si++)
                for (int pi = 0; pi < np - 1; pi++)
                {
                    int a = id[si, pi], bb = id[si + 1, pi], c = id[si + 1, pi + 1], d = id[si, pi + 1];
                    Vector3 o = k.N[a] + k.N[bb] + k.N[c] + k.N[d];
                    var mm = pi == 0 ? under : mat;
                    TriOut(k, mm, a, bb, c, o); TriOut(k, mm, a, c, d, o);
                }
            // rubbing strip across the face
            if (!string.IsNullOrEmpty(b.stripe))
            {
                float sy = b.y0 + h * 0.6f; var sm = Plastic(b.stripe);
                for (int si = 5; si < ns - 6; si++)
                {
                    var A = stations[si]; var B = stations[si + 1];
                    float oa = (outw + 0.006f) * A.scale, ob = (outw + 0.006f) * B.scale;
                    k.Quad(sm, A.pos(oa, sy - 0.014f), B.pos(ob, sy - 0.014f), B.pos(ob, sy + 0.014f), A.pos(oa, sy + 0.014f), (A.n + B.n).normalized);
                }
            }
            // lower intake / air dam
            if (b.intake != null && b.intake.Length >= 3)
            {
                float y0 = b.intake[0], y1 = b.intake[1], w = b.intake[2];
                for (int i = 0; i < 6; i++)
                {
                    float l0 = Mathf.Lerp(-w, w, i / 6f), l1 = Mathf.Lerp(-w, w, (i + 1) / 6f);
                    float x0 = FaceX(m, front, l0, (y0 + y1) * 0.5f) + fs * (outw + 0.01f), x1 = FaceX(m, front, l1, (y0 + y1) * 0.5f) + fs * (outw + 0.01f);
                    k.Quad(grilleM, new Vector3(l0, y0, x0), new Vector3(l1, y0, x1), new Vector3(l1, y1, x1), new Vector3(l0, y1, x0), Vector3.forward * fs);
                }
            }
            if (b.step) { float x = FaceX(m, false, 0, b.y0) - outw - 0.12f; k.Box(steelM, new Vector3(-0.45f, b.y0 - 0.06f, x), new Vector3(0.45f, b.y0, x + 0.15f)); }
        }

        static void SideStrip(MeshKit k, CarModel m, float x0, float x1, float y0, float y1, int sd, float off, Material mat)
        {
            int n = 10;
            for (int i = 0; i < n; i++)
            {
                float xa = Mathf.Lerp(x0, x1, i / (float)n), xb = Mathf.Lerp(x0, x1, (i + 1) / (float)n);
                Vector3 a0 = Side(m, xa, y0, sd, off), b0 = Side(m, xb, y0, sd, off), b1 = Side(m, xb, y1, sd, off), a1 = Side(m, xa, y1, sd, off);
                k.Quad(mat, a0, b0, b1, a1, new Vector3(sd, 0, 0));
                // little top and bottom faces so it stands proud
                Vector3 a0i = Side(m, xa, y0, sd, off - 0.012f), b0i = Side(m, xb, y0, sd, off - 0.012f), a1i = Side(m, xa, y1, sd, off - 0.012f), b1i = Side(m, xb, y1, sd, off - 0.012f);
                k.Quad(mat, a1, b1, b1i, a1i, Vector3.up); k.Quad(mat, a0, b0, b0i, a0i, Vector3.down);
            }
        }

        /// <summary>Panel gap along a polyline of (length x, t) where t runs sill (0) to shoulder (1).</summary>
        static void Seam(MeshKit k, CarModel m, float[] poly, int sd)
        {
            if (poly == null || poly.Length < 4) return;
            var pts = new List<Vector3>();
            for (int i = 0; i + 1 < poly.Length; i += 2)
            {
                float x = poly[i], t = poly[i + 1];
                float yb = Yb(m, x), top = Top(m, x), y = Mathf.Lerp(yb + 0.02f, top - 0.01f, t);
                pts.Add(Side(m, x, y, sd, 0.003f));
            }
            for (int i = 0; i < pts.Count - 1; i++)
            {
                // subdivide so the line hugs the curve
                for (int s = 0; s < 4; s++)
                {
                    Vector3 a = Vector3.Lerp(pts[i], pts[i + 1], s / 4f), b = Vector3.Lerp(pts[i], pts[i + 1], (s + 1) / 4f);
                    float ax = a.z, ay = a.y, bx = b.z, by = b.y;
                    SeamLine(k, Side(m, ax, ay, sd, 0.003f), Side(m, bx, by, sd, 0.003f), sd);
                }
            }
        }

        static void SeamLine(MeshKit k, Vector3 a, Vector3 b, int sd)
        {
            Vector3 dir = (b - a); if (dir.sqrMagnitude < 1e-8f) return; dir.Normalize();
            Vector3 n = new Vector3(sd, 0, 0);
            Vector3 w = Vector3.Cross(n, dir).normalized * 0.0045f;
            k.Quad(under, a - w, a + w, b + w, b - w, n);
        }

        static void PlateQuad(MeshKit k, Material m, Vector3 c, float w, float h, float facing)
        {
            Vector3 x = Vector3.right * (w / 2f) * -facing, y = Vector3.up * (h / 2f);
            Vector3 n = Vector3.forward * facing;
            k.QuadUV(m, c - x - y, c + x - y, c + x + y, c - x + y, n, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
            k.Box(trim, c - new Vector3(w * 0.52f, h * 0.56f, 0f) - n * 0.012f, c + new Vector3(w * 0.52f, h * 0.56f, 0f) - n * 0.002f);
        }

        static void SeatBack(MeshKit k, Vector3 pos, float wd, float cabH)
        {
            float bh = Mathf.Clamp(Mathf.Min(0.36f, cabH * 0.55f), 0.08f, Mathf.Max(0.08f, cabH * 0.85f - 0.2f));
            k.Push(Matrix4x4.TRS(pos, Quaternion.Euler(-12f, 0f, 0f), Vector3.one));
            k.Box(interior, new Vector3(-wd / 2f, 0.03f, -0.05f), new Vector3(wd / 2f, 0.03f + bh, 0.05f));
            k.Box(interior, new Vector3(-wd * 0.23f, 0.03f + bh + 0.01f, -0.035f), new Vector3(wd * 0.23f, 0.03f + bh + 0.15f, 0.035f));
            k.Pop();
            k.Box(interior, new Vector3(pos.x - wd / 2f, pos.y - 0.02f, pos.z + 0.02f), new Vector3(pos.x + wd / 2f, pos.y + 0.07f, pos.z + 0.42f));
        }

        // ---------------------------------------------------------------- wheels
        static Mesh BuildWheel(string type)
        {
            var k = new MeshKit();
            const int seg = 28;
            var tyreProf = new[] { new Vector2(0.64f, -0.088f), new Vector2(0.8f, -0.104f), new Vector2(0.93f, -0.1f), new Vector2(0.985f, -0.085f), new Vector2(1f, -0.06f), new Vector2(1f, 0.06f), new Vector2(0.985f, 0.085f), new Vector2(0.93f, 0.1f), new Vector2(0.8f, 0.104f), new Vector2(0.64f, 0.088f) };
            Lathe(k, tyre, tyreProf, seg, new Vector2(0.82f, 0f));
            var dish = type == "alloy" || type == "cap" ? alloy : steelM;
            foreach (float sy in new[] { 1f, -1f })
            {
                Lathe(k, dish, new[] { new Vector2(0.58f, 0.07f * sy), new Vector2(0.645f, 0.09f * sy), new Vector2(0.66f, 0.083f * sy) }, seg, new Vector2(0.62f, 0f));
                if (type == "alloy")
                {
                    Disc(k, under, 0.6f, 0.02f * sy, sy, seg);
                    Disc(k, dish, 0.17f, 0.078f * sy, sy, 14);
                    for (int sp = 0; sp < 5; sp++)
                    {
                        float a = sp * Mathf.PI * 2f / 5f + (sy > 0 ? 0f : 0.31f);
                        Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)), perp = new Vector3(-dir.z, 0, dir.x);
                        Vector3 i0 = dir * 0.15f - perp * 0.075f, i1 = dir * 0.15f + perp * 0.075f, o0 = dir * 0.6f - perp * 0.045f, o1 = dir * 0.6f + perp * 0.045f;
                        float yf = 0.072f * sy, yb = 0.035f * sy;
                        k.Quad(dish, i0 + Vector3.up * yf, i1 + Vector3.up * yf, o1 + Vector3.up * (yf - 0.01f * sy), o0 + Vector3.up * (yf - 0.01f * sy), Vector3.up * sy);
                        k.Quad(dish, i0 + Vector3.up * yf, o0 + Vector3.up * (yf - 0.01f * sy), o0 + Vector3.up * yb, i0 + Vector3.up * yb, -perp);
                        k.Quad(dish, i1 + Vector3.up * yf, o1 + Vector3.up * (yf - 0.01f * sy), o1 + Vector3.up * yb, i1 + Vector3.up * yb, perp);
                    }
                    Disc(k, dish, 0.42f, 0.028f * sy, sy, 20);
                }
                else if (type == "cap")
                {
                    // plastic wheel trim: domed disc with a ring of slots
                    Lathe(k, dish, new[] { new Vector2(0.001f, 0.1f * sy), new Vector2(0.2f, 0.098f * sy), new Vector2(0.45f, 0.088f * sy), new Vector2(0.6f, 0.078f * sy) }, seg, new Vector2(0f, -0.5f * sy));
                    for (int s = 0; s < 10; s++)
                    {
                        float a = s * Mathf.PI * 2f / 10f; Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)), perp = new Vector3(-dir.z, 0, dir.x);
                        Vector3 c0 = dir * 0.3f, c1 = dir * 0.5f; float y = 0.095f * sy;
                        k.Quad(under, c0 - perp * 0.035f + Vector3.up * y, c1 - perp * 0.05f + Vector3.up * (y - 0.006f * sy), c1 + perp * 0.05f + Vector3.up * (y - 0.006f * sy), c0 + perp * 0.035f + Vector3.up * y, Vector3.up * sy);
                    }
                }
                else
                {
                    // pressed steel wheel (bare = its hubcap's gone)
                    Lathe(k, dish, new[] { new Vector2(0.001f, 0.04f * sy), new Vector2(0.16f, 0.045f * sy), new Vector2(0.3f, 0.05f * sy), new Vector2(0.45f, 0.035f * sy), new Vector2(0.58f, 0.06f * sy) }, seg, new Vector2(0.3f, -0.5f * sy));
                    for (int s = 0; s < 6; s++)
                    {
                        float a = s * Mathf.PI * 2f / 6f; Vector3 c = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.38f;
                        k.Quad(under, c + new Vector3(-0.04f, 0.052f * sy, -0.04f), c + new Vector3(0.04f, 0.052f * sy, -0.04f), c + new Vector3(0.04f, 0.052f * sy, 0.04f), c + new Vector3(-0.04f, 0.052f * sy, 0.04f), Vector3.up * sy);
                    }
                    if (type != "bare") Disc(k, chrome, 0.12f, 0.058f * sy, sy, 12);
                }
            }
            var g = k.Build("Wheel_" + type, null, false);
            var mesh = g.GetComponent<MeshFilter>().sharedMesh; UnityEngine.Object.Destroy(g);
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
                    if (Vector2.Dot(n2, prof[p] - centre) < 0f) n2 = -n2;
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
            int prev = -1;
            for (int s = 0; s <= seg; s++)
            {
                float a = s * Mathf.PI * 2f / seg;
                int v = k.Vert(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r), n, new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
                if (prev >= 0) TriOut(k, m, c, prev, v, n);
                prev = v;
            }
        }

        // ---------------------------------------------------------------- number plates
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

        /// <summary>Old prefix plates (P716 FVN) on older cars, current style (KX09 DUK) on the rest.</summary>
        static string Registration(System.Random r)
        {
            const string L = "ABCDEFGHJKLMNOPRSTUVWXY";
            char C() => L[r.Next(L.Length)];
            if (r.NextDouble() < 0.35)
            {
                const string pre = "FGHJKLMNPRSTVWXY";
                return "" + pre[r.Next(pre.Length)] + r.Next(10, 999) + " " + C() + C() + C();
            }
            int age = new[] { 51, 2, 52, 3, 53, 4, 54, 5, 55, 6, 56, 7, 57, 8, 58, 9, 59, 10, 60, 11, 61, 12, 62, 13, 63, 14, 64, 15, 65, 16, 66, 17, 67, 18, 68, 19 }[r.Next(36)];
            return "" + C() + C() + age.ToString("00") + " " + C() + C() + C();
        }

        static Texture2D PlateTex(string reg, bool rear)
        {
            var tex = new Texture2D(208, 48, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, anisoLevel = 2, name = "Plate " + reg };
            tex.SetPixels32(PlatePx(reg, rear)); tex.Apply(true, true);
            return tex;
        }

        /// <summary>Front (white) and rear (yellow) plates side by side in one texture, for the modelled cars.</summary>
        internal static Texture2D PlateAtlas(System.Random r)
        {
            string reg = Registration(r);
            var f = PlatePx(reg, false); var b = PlatePx(reg, true);
            const int W = 208, H = 48;
            var px = new Color32[W * 2 * H];
            for (int y = 0; y < H; y++) { System.Array.Copy(f, y * W, px, y * W * 2, W); System.Array.Copy(b, y * W, px, y * W * 2 + W, W); }
            var tex = new Texture2D(W * 2, H, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, anisoLevel = 2, name = "Plates " + reg };
            tex.SetPixels32(px); tex.Apply(true, true);
            return tex;
        }

        static Color32[] PlatePx(string reg, bool rear)
        {
            const int W = 208, H = 48, sx = 3, sy = 4;
            var bg = rear ? new Color32(250, 205, 20, 255) : new Color32(242, 242, 236, 255);
            var ink = new Color32(18, 18, 20, 255);
            var px = new Color32[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = bg;
            for (int x = 0; x < W; x++) { px[x] = px[(H - 1) * W + x] = new Color32(40, 40, 40, 255); }
            for (int y = 0; y < H; y++) { px[y * W] = px[y * W + W - 1] = new Color32(40, 40, 40, 255); }
            bool modern = reg.Length > 1 && char.IsLetter(reg[1]);
            int x0 = 0;
            if (modern)
            {
                // blue band with GB
                for (int y = 1; y < H - 1; y++) for (int x = 1; x < 22; x++) px[y * W + x] = new Color32(18, 60, 148, 255);
                DrawText(px, W, H, "GB", 3, 6, 2, 2, new Color32(255, 212, 0, 255));
                x0 = 20;
            }
            int charW = 5 * sx, gap = 4, space = 12;
            int total = 0; foreach (char ch in reg) total += ch == ' ' ? space : charW + gap; total -= gap;
            int cx = x0 + (W - x0 - total) / 2, cy = (H - 7 * sy) / 2;
            foreach (char ch in reg)
            {
                if (ch == ' ') { cx += space; continue; }
                DrawChar(px, W, H, ch, cx, cy, sx, sy, ink);
                cx += charW + gap;
            }
            return px;
        }

        static void DrawText(Color32[] px, int W, int H, string s, int x, int y, int sx, int sy, Color32 c) { foreach (char ch in s) { DrawChar(px, W, H, ch, x, y, sx, sy, c); x += 6 * sx; } }
        static void DrawChar(Color32[] px, int W, int H, char ch, int cx, int cy, int sx, int sy, Color32 ink)
        {
            if (!Glyph.TryGetValue(ch, out var g)) return;
            for (int gy = 0; gy < 7; gy++)
                for (int gx = 0; gx < 5; gx++)
                    if (g[gy][gx] == '#')
                        for (int yy = 0; yy < sy; yy++)
                            for (int xx = 0; xx < sx; xx++)
                            {
                                int X = cx + gx * sx + xx, Y = cy + (6 - gy) * sy + yy;
                                if (X >= 0 && X < W && Y >= 0 && Y < H) px[Y * W + X] = ink;
                            }
        }

        // ---------------------------------------------------------------- what's on the estate
        static readonly (string id, int w)[] Mix = { ("hatch_a", 16), ("hatch_b", 12), ("saloon_b", 9), ("supermini_a", 14), ("supermini_b", 13), ("micro_a", 10), ("hatch_c", 9), ("hatch_d", 7), ("van_a", 13) };
        // the modelled UK cars from the reference photos (used when their prefabs are in the build)
        // only the detailed models drive about (the hatch and the saloon, in every colour, condition and plate); our simpler
        // generated bodies (boxy80, supermini, hatch70, rally, coupe80, estate90, jeep, van) stay off the road until they're
        // remade to the same standard or more detailed models are added
        static readonly (string id, int w)[] Mix2 = { ("hatch90", 55), ("saloon90", 45) };
        public static Spec Random(System.Random r)
        {
            var Mix = CarKit.Has("hatch90") ? Mix2 : CarBody.Mix;
            int tot = 0; foreach (var e in Mix) tot += e.w;
            int k = r.Next(tot);
            foreach (var e in Mix) { if (k < e.w) return Get(e.id); k -= e.w; }
            return Get("hatch_a");
        }
        /// <summary>Paint from the original game's estate cars: tired greys, navy, maroon, bottle green, beige, white van white.</summary>
        public static Color RandomPaint(System.Random r)
        {
            string[] hex = { "#7a7f86", "#2a3f6a", "#b8b2a2", "#8a1f22", "#2f3a2e", "#d0d4d8", "#2f4a84", "#d9d8cf", "#aab0b6", "#4a6a45", "#d8c038", "#c9c4bc", "#6c2434", "#5a6e86", "#8a8f86", "#e8e8e8", "#1f3657", "#6e2530", "#3a5a8a", "#5e7a52", "#6e6a72", "#9a2a1e" };
            ColorUtility.TryParseHtmlString(hex[r.Next(hex.Length)], out var c);
            return c;
        }
        public static Color VanPaint(System.Random r) { ColorUtility.TryParseHtmlString(r.NextDouble() < 0.7 ? "#e8e6dc" : r.NextDouble() < 0.5 ? "#c8c4b6" : "#2a3f6a", out var c); return c; }
    }
}

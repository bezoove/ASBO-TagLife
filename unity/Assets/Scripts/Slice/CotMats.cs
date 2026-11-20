using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>Shared cottage materials and procedural textures (one set for the estate; per-cottage tints cached by colour).</summary>
    public static class CotMats
    {
        public static Material plaster, oak, boards, flag, stone, brick, terracotta, tiles, slate, glassLead, glassFrost, glassClear,
            iron, brass, ceramic, copper, linen, soot, fire, white, rug, rug2, books, leaf, soil, gravel, hedge, lead, cushion, velvet, check, quilt, pewter, canvas;
        public static Material[] fabrics, flowers;
        static readonly Dictionary<Color, Material> lime = new Dictionary<Color, Material>(), paint = new Dictionary<Color, Material>(), straw = new Dictionary<Color, Material>();
        static Texture2D stoneTex, strawTex, limeTex;

        static Material T(Material m, float tile) { MeshKit.Tile[m] = tile; return m; }

        public static void Init()
        {
            if (plaster != null) return;
            stoneTex = Gfx.StoneTex(21); strawTex = Gfx.StrawTex(5);
            plaster = T(Gfx.Mat(new Color(0.95f, 0.92f, 0.86f), Noise(64, 0.06f, 3), 0.08f), 1.5f);
            oak = T(Gfx.Mat(new Color(0.62f, 0.5f, 0.4f), Gfx.PlankTex(7), 0.25f), 1.2f);
            boards = T(Gfx.Mat(new Color(1.15f, 1.0f, 0.86f), Gfx.PlankTex(3), 0.3f), 2f);
            flag = T(Gfx.Mat(new Color(0.95f, 0.93f, 0.9f), FlagTex(9), 0.18f), 2.4f);
            stone = T(Gfx.Mat(Color.white, stoneTex, 0.06f), 2.2f);
            brick = T(Gfx.Mat(Color.white, BrickTex(4), 0.08f), 1.2f);
            terracotta = T(Gfx.Mat(new Color(0.72f, 0.36f, 0.22f), null, 0.15f), 1f);
            tiles = T(Gfx.Mat(Color.white, TileTex(6), 0.12f), 1.6f);
            slate = T(Gfx.Mat(new Color(0.75f, 0.8f, 0.88f), TileTex(12, true), 0.35f), 1.6f);
            glassLead = T(Transparent(new Color(0.85f, 0.92f, 0.95f, 1f), LeadTex(), 0.92f), 0.6f);
            glassFrost = T(Transparent(new Color(0.92f, 0.94f, 0.95f, 0.85f), null, 0.5f), 1f);
            glassClear = T(Transparent(new Color(0.8f, 0.9f, 0.95f, 0.22f), null, 0.95f), 1f);
            iron = Gfx.Mat(new Color(0.08f, 0.08f, 0.09f), null, 0.45f, 0.6f);
            brass = Gfx.Mat(new Color(0.9f, 0.7f, 0.3f), null, 0.8f, 1f);
            pewter = Gfx.Mat(new Color(0.62f, 0.64f, 0.66f), null, 0.65f, 0.9f);
            copper = Gfx.Mat(new Color(0.85f, 0.45f, 0.25f), null, 0.7f, 1f);
            ceramic = Gfx.Mat(new Color(0.96f, 0.96f, 0.94f), null, 0.85f);
            linen = Gfx.Mat(new Color(0.95f, 0.94f, 0.9f), null, 0.1f);
            soot = Gfx.Mat(new Color(0.06f, 0.05f, 0.05f), null, 0.05f);
            fire = Gfx.Emissive(new Color(1f, 0.45f, 0.1f), 3.5f);
            white = Gfx.Mat(new Color(0.94f, 0.93f, 0.9f), null, 0.35f);
            lead = Gfx.Mat(new Color(0.35f, 0.37f, 0.4f), null, 0.4f, 0.6f);
            rug = T(Gfx.Mat(Color.white, RugTex(1, new Color(0.55f, 0.12f, 0.12f), new Color(0.15f, 0.2f, 0.4f)), 0.05f), 2.4f);
            rug2 = T(Gfx.Mat(Color.white, RugTex(2, new Color(0.2f, 0.35f, 0.25f), new Color(0.75f, 0.6f, 0.3f)), 0.05f), 2.4f);
            books = T(Gfx.Mat(Color.white, BookTex(3), 0.2f), 0.9f);
            leaf = Gfx.Mat(new Color(0.18f, 0.38f, 0.14f), Noise(64, 0.25f, 8), 0.1f);
            soil = Gfx.Mat(new Color(0.24f, 0.15f, 0.08f), Noise(64, 0.2f, 2), 0.05f);
            gravel = T(Gfx.Mat(Color.white, Gfx.GravelTex(4), 0.05f), 2f);
            hedge = T(Gfx.Mat(new Color(0.12f, 0.3f, 0.1f), Noise(64, 0.35f, 6), 0.05f), 1.2f);
            cushion = Gfx.Mat(new Color(0.55f, 0.18f, 0.16f), null, 0.15f);
            velvet = Gfx.Mat(new Color(0.18f, 0.28f, 0.2f), null, 0.3f);
            check = T(Gfx.Mat(Color.white, CheckTex(), 0.1f), 0.5f);
            quilt = T(Gfx.Mat(Color.white, QuiltTex(), 0.1f), 1.2f);
            canvas = Gfx.Mat(new Color(0.9f, 0.88f, 0.8f), null, 0.1f);
            fabrics = new[] { cushion, velvet, Gfx.Mat(new Color(0.2f, 0.25f, 0.45f), null, 0.15f), Gfx.Mat(new Color(0.75f, 0.68f, 0.5f), null, 0.15f), Gfx.Mat(new Color(0.45f, 0.3f, 0.45f), null, 0.15f) };
            flowers = new[] { Gfx.Mat(new Color(0.9f, 0.3f, 0.45f)), Gfx.Mat(new Color(0.95f, 0.85f, 0.3f)), Gfx.Mat(new Color(0.55f, 0.4f, 0.85f)), Gfx.Mat(new Color(0.98f, 0.97f, 0.95f)), Gfx.Mat(new Color(0.95f, 0.55f, 0.2f)) };
        }

        /// <summary>Limewashed stone wall tinted to the cottage's wall colour.</summary>
        public static Material Lime(Color c)
        {
            if (lime.TryGetValue(c, out var m)) return m;
            if (limeTex == null)
            {
                var src = stoneTex.GetPixels32(); var dst = new Color32[src.Length];
                for (int i = 0; i < src.Length; i++) { var q = (Color)src[i]; float v = (q.r + q.g + q.b) / 3f; float l = Mathf.Lerp(0.78f, 1.0f, Mathf.Clamp01(v * 1.6f)); dst[i] = new Color(l, l * 0.99f, l * 0.97f, 1f); }
                limeTex = new Texture2D(stoneTex.width, stoneTex.height, TextureFormat.RGBA32, true); limeTex.SetPixels32(dst); limeTex.wrapMode = TextureWrapMode.Repeat; limeTex.anisoLevel = 4; limeTex.Apply(true);
            }
            m = T(Gfx.Mat(c, limeTex, 0.04f), 2.2f); lime[c] = m; return m;
        }
        public static Material Paint(Color c)
        {
            if (paint.TryGetValue(c, out var m)) return m;
            m = T(Gfx.Mat(c, null, 0.45f), 1f); paint[c] = m; return m;
        }
        public static Material Straw(Color c)
        {
            if (straw.TryGetValue(c, out var m)) return m;
            m = T(Gfx.Mat(Color.Lerp(c, new Color(0.85f, 0.72f, 0.5f), 0.35f) * 1.05f, strawTex, 0.04f), 1.4f); straw[c] = m; return m;
        }

        public static Material Transparent(Color c, Texture2D tex, float smooth)
        {
            var m = Gfx.Mat(c, tex, smooth);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_Cull", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            return m;
        }

        static Texture2D Tex(int s, Color32[] px, TextureWrapMode wrap = TextureWrapMode.Repeat)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, true);
            t.SetPixels32(px); t.wrapMode = wrap; t.anisoLevel = 4; t.Apply(true);
            return t;
        }

        static Texture2D Noise(int s, float amt, int seed)
        {
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                {
                    float n = 1f - amt + amt * (Mathf.PerlinNoise(x * 0.2f + seed, y * 0.2f) * 0.6f + Mathf.PerlinNoise(x * 0.7f, y * 0.7f + seed) * 0.4f) * 2f;
                    byte v = (byte)Mathf.Clamp(n * 235f, 0, 255); px[y * s + x] = new Color32(v, v, v, 255);
                }
            return Tex(s, px);
        }

        static Texture2D FlagTex(int seed)
        {
            const int S = 256; var px = new Color32[S * S]; var rng = new System.Random(seed);
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(70, 66, 60, 255);
            int y = 0;
            while (y < S)
            {
                int h = rng.Next(40, 80); int x = -rng.Next(0, 60);
                while (x < S)
                {
                    int w = rng.Next(50, 110); float t = 0.62f + (float)rng.NextDouble() * 0.25f;
                    var bc = Color.Lerp(new Color(0.55f, 0.53f, 0.5f), new Color(0.68f, 0.62f, 0.52f), (float)rng.NextDouble()) * t;
                    for (int yy = y + 2; yy < Mathf.Min(S, y + h - 2); yy++)
                        for (int xx = Mathf.Max(0, x + 2); xx < Mathf.Min(S, x + w - 2); xx++)
                        { float n = 0.85f + Mathf.PerlinNoise(xx * 0.08f + seed, yy * 0.08f) * 0.3f; var c = bc * n; c.a = 1; px[yy * S + xx] = c; }
                    x += w;
                }
                y += h;
            }
            return Tex(S, px);
        }

        static Texture2D BrickTex(int seed)
        {
            const int S = 256; var px = new Color32[S * S]; var rng = new System.Random(seed);
            for (int y = 0; y < S; y++)
            {
                int row = y / 16; bool mortarY = y % 16 < 2;
                for (int x = 0; x < S; x++)
                {
                    int xo = x + (row % 2) * 32; bool mortarX = xo % 64 < 2;
                    if (mortarX || mortarY) { px[y * S + x] = new Color32(150, 140, 125, 255); continue; }
                    float t = Mathf.PerlinNoise((xo / 64) * 3.1f + row * 7.7f, seed) * 0.35f + 0.65f;
                    float n = 0.9f + Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.2f;
                    px[y * S + x] = new Color(0.6f * t * n, 0.27f * t * n, 0.18f * t * n, 1);
                }
            }
            return Tex(S, px);
        }

        static Texture2D TileTex(int seed, bool slateLook = false)
        {
            const int S = 256; var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
            {
                int row = y / 32; float shadow = (y % 32) < 5 ? 0.55f : 1f;
                for (int x = 0; x < S; x++)
                {
                    int xo = x + (row % 2) * 16; bool gap = xo % 32 < 2;
                    float t = 0.75f + Mathf.PerlinNoise((xo / 32) * 4.3f + row * 2.7f, seed) * 0.35f;
                    float n = 0.92f + Mathf.PerlinNoise(x * 0.25f + seed, y * 0.25f) * 0.16f;
                    Color c = slateLook ? new Color(0.36f, 0.38f, 0.42f) : new Color(0.66f, 0.42f, 0.3f);
                    c *= t * n * shadow * (gap ? 0.5f : 1f); c.a = 1;
                    px[y * S + x] = c;
                }
            }
            return Tex(S, px);
        }

        static Texture2D LeadTex()
        {
            const int S = 128; var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    // diamond lattice: lines where (x+y) or (x-y) is near a multiple of 32
                    int a = (x + y) % 32, b = ((x - y) % 32 + 32) % 32;
                    bool line = a < 2 || b < 2;
                    px[y * S + x] = line ? new Color32(55, 58, 60, 255) : new Color32(215, 230, 235, 70);
                }
            return Tex(S, px);
        }

        static Texture2D RugTex(int seed, Color a, Color b)
        {
            const int S = 128; var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    int bx = Mathf.Min(Mathf.Min(x, S - 1 - x), Mathf.Min(y, S - 1 - y));
                    bool border = bx < 12 && bx > 4, fringe = bx <= 4;
                    float m = Mathf.Abs(Mathf.Sin(x * 0.2f)) * Mathf.Abs(Mathf.Sin(y * 0.2f));
                    Color c = fringe ? new Color(0.85f, 0.8f, 0.7f) : border ? b : Color.Lerp(a, b * 0.8f, m > 0.6f ? 0.6f : 0f);
                    c *= 0.9f + Mathf.PerlinNoise(x * 0.5f + seed, y * 0.5f) * 0.15f; c.a = 1; px[y * S + x] = c;
                }
            return Tex(S, px, TextureWrapMode.Clamp);
        }

        static Texture2D BookTex(int seed)
        {
            const int S = 128; var px = new Color32[S * S]; var rng = new System.Random(seed);
            Color[] cols = { new Color(0.45f, 0.1f, 0.1f), new Color(0.12f, 0.25f, 0.15f), new Color(0.12f, 0.15f, 0.35f), new Color(0.5f, 0.38f, 0.2f), new Color(0.2f, 0.12f, 0.08f), new Color(0.6f, 0.55f, 0.45f) };
            for (int shelf = 0; shelf < 4; shelf++)
            {
                int y0 = shelf * 32, x = 0;
                while (x < S)
                {
                    int w = rng.Next(4, 9), h = rng.Next(20, 30); var c = cols[rng.Next(cols.Length)];
                    for (int xx = x; xx < Mathf.Min(S, x + w); xx++)
                        for (int yy = y0; yy < y0 + 32; yy++)
                        {
                            bool book = yy - y0 < h; float band = (yy - y0 == h - 6 || yy - y0 == 5) ? 1.6f : 1f;
                            px[yy * S + xx] = book ? (Color32)(c * band * (xx == x ? 0.6f : 1f)) : new Color32(30, 20, 12, 255);
                        }
                    x += w;
                }
            }
            return Tex(S, px);
        }

        static Texture2D CheckTex()
        {
            const int S = 64; var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    bool a = (x / 8) % 2 == 0, b = (y / 8) % 2 == 0;
                    px[y * S + x] = a && b ? new Color32(170, 40, 40, 255) : a || b ? new Color32(210, 140, 140, 255) : new Color32(245, 240, 230, 255);
                }
            return Tex(S, px);
        }

        static Texture2D QuiltTex()
        {
            const int S = 128; var px = new Color32[S * S]; var rng = new System.Random(11);
            Color[] cols = { new Color(0.8f, 0.7f, 0.55f), new Color(0.55f, 0.65f, 0.75f), new Color(0.75f, 0.5f, 0.5f), new Color(0.6f, 0.7f, 0.55f), new Color(0.9f, 0.88f, 0.8f) };
            var grid = new Color[8, 8];
            for (int i = 0; i < 8; i++) for (int j = 0; j < 8; j++) grid[i, j] = cols[rng.Next(cols.Length)];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    bool seam = x % 16 == 0 || y % 16 == 0;
                    var c = seam ? new Color(0.95f, 0.93f, 0.88f) : grid[x / 16, y / 16]; c.a = 1; px[y * S + x] = c;
                }
            return Tex(S, px);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>Material, texture and primitive helpers. Everything in the slice is generated in code.</summary>
    public static class Gfx
    {
        static Shader _lit, _part;
        public static Shader Lit => _lit != null ? _lit : (_lit = Shader.Find("Universal Render Pipeline/Lit"));
        public static Shader ParticleUnlit => _part != null ? _part : (_part = Shader.Find("Universal Render Pipeline/Particles/Unlit"));

        public static Material Mat(Color tint, Texture2D tex = null, float smooth = 0.12f, float metal = 0f)
        {
            var m = new Material(Lit);
            m.SetColor("_BaseColor", tint);
            if (tex != null) m.SetTexture("_BaseMap", tex);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            return m;
        }

        public static Material Emissive(Color c, float intensity = 2f)
        {
            var m = Mat(c);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * intensity);
            return m;
        }

        static Texture2D MakeTex(int s, Color32[] px)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, true);
            t.SetPixels32(px);
            t.wrapMode = TextureWrapMode.Repeat;
            t.anisoLevel = 4;
            t.Apply(true);
            return t;
        }

        public static Texture2D StoneTex(int seed)
        {
            const int S = 256; int rows = 8, rh = S / rows;
            var px = new Color32[S * S];
            var mortar = new Color32(112, 106, 94, 255);
            for (int i = 0; i < px.Length; i++) px[i] = mortar;
            var rng = new System.Random(seed);
            for (int r = 0; r < rows; r++)
            {
                int x = -rng.Next(0, 50);
                while (x < S)
                {
                    int w = rng.Next(38, 72);
                    float shade = 0.72f + (float)rng.NextDouble() * 0.28f;
                    float warm = (float)rng.NextDouble();
                    Color bc = Color.Lerp(new Color(0.66f, 0.58f, 0.45f), new Color(0.78f, 0.68f, 0.5f), warm) * shade;
                    for (int yy = 2; yy < rh - 1; yy++)
                        for (int xx = 2; xx < w; xx++)
                        {
                            int X = x + xx; if (X < 0 || X >= S) continue;
                            int Y = r * rh + yy;
                            float n = Mathf.PerlinNoise(X * 0.11f + seed, Y * 0.11f) * 0.3f;
                            Color c = bc * (0.85f + n); c.a = 1f;
                            px[Y * S + X] = c;
                        }
                    x += w;
                }
            }
            return MakeTex(S, px);
        }

        public static Texture2D StrawTex(int seed)
        {
            const int S = 256;
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float streak = Mathf.PerlinNoise(x * 0.09f + seed, y * 0.015f);
                    float fine = Mathf.PerlinNoise(x * 0.6f, y * 0.05f + seed);
                    float band = (y % 64) < 4 ? 0.8f : 1f;
                    float v = (0.55f + streak * 0.35f + fine * 0.2f) * band;
                    Color c = Color.Lerp(new Color(0.38f, 0.28f, 0.14f), new Color(0.82f, 0.68f, 0.38f), v);
                    c.a = 1f; px[y * S + x] = c;
                }
            return MakeTex(S, px);
        }

        public static Texture2D PlankTex(int seed)
        {
            const int S = 256;
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int plank = x / 32;
                    float tone = 0.7f + Mathf.PerlinNoise(plank * 7.3f + seed, 3f) * 0.3f;
                    float grain = Mathf.PerlinNoise(x * 0.2f + plank * 11f, y * 0.02f + seed) * 0.25f;
                    float edge = (x % 32) < 2 ? 0.55f : 1f;
                    Color c = new Color(0.42f, 0.27f, 0.14f) * ((tone + grain) * edge); c.a = 1f;
                    px[y * S + x] = c;
                }
            return MakeTex(S, px);
        }

        public static Texture2D GrassTex(int seed)
        {
            const int S = 128;
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float n = Mathf.PerlinNoise(x * 0.15f + seed, y * 0.15f) * 0.5f + Mathf.PerlinNoise(x * 0.7f, y * 0.7f) * 0.5f;
                    Color c = Color.Lerp(new Color(0.16f, 0.32f, 0.1f), new Color(0.3f, 0.5f, 0.18f), n); c.a = 1f;
                    px[y * S + x] = c;
                }
            return MakeTex(S, px);
        }

        public static Texture2D GravelTex(int seed)
        {
            const int S = 128;
            var px = new Color32[S * S];
            var rng = new System.Random(seed);
            for (int i = 0; i < px.Length; i++)
            {
                float v = 0.55f + (float)rng.NextDouble() * 0.35f;
                px[i] = new Color(v * 0.95f, v * 0.9f, v * 0.8f, 1f);
            }
            return MakeTex(S, px);
        }

        /// <summary>Cube primitive under parent. If tileMeters>0 the texture repeats every tileMeters.</summary>
        public static GameObject Box(Transform parent, Vector3 center, Vector3 size, Material mat, float tileMeters = 0f, string name = "Box", bool collider = true)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = center;
            g.transform.localScale = size;
            var r = g.GetComponent<Renderer>();
            if (tileMeters > 0f)
            {
                var m = new Material(mat);
                m.SetTextureScale("_BaseMap", new Vector2(Mathf.Max(size.x, size.z) / tileMeters, size.y / tileMeters));
                r.sharedMaterial = m;
            }
            else r.sharedMaterial = mat;
            if (!collider) Object.Destroy(g.GetComponent<Collider>());
            return g;
        }

        public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Material mat, string name = "Prim", bool collider = true)
        {
            var g = GameObject.CreatePrimitive(t);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.Destroy(g.GetComponent<Collider>());
            return g;
        }

        /// <summary>Triangle gable prism: triangle in XY (base width, height), extruded along Z by thick.</summary>
        public static Mesh GablePrism(float width, float height, float thick)
        {
            float hw = width / 2f, ht = thick / 2f;
            Vector3 a = new Vector3(-hw, 0, -ht), b = new Vector3(hw, 0, -ht), c = new Vector3(0, height, -ht);
            Vector3 a2 = new Vector3(-hw, 0, ht), b2 = new Vector3(hw, 0, ht), c2 = new Vector3(0, height, ht);
            var v = new List<Vector3>(); var tri = new List<int>();
            void Face(Vector3[] pts, Vector3 outward)
            {
                int s = v.Count;
                for (int i = 0; i < pts.Length; i++) v.Add(pts[i]);
                Vector3 n = Vector3.Cross(pts[1] - pts[0], pts[2] - pts[0]);
                bool flip = Vector3.Dot(n, outward) < 0;
                for (int i = 1; i < pts.Length - 1; i++)
                {
                    if (!flip) { tri.Add(s); tri.Add(s + i); tri.Add(s + i + 1); }
                    else { tri.Add(s); tri.Add(s + i + 1); tri.Add(s + i); }
                }
            }
            Face(new[] { a, b, c }, Vector3.back);
            Face(new[] { a2, b2, c2 }, Vector3.forward);
            Face(new[] { a, b, b2, a2 }, Vector3.down);
            Face(new[] { a, a2, c2, c }, new Vector3(-height, hw, 0));
            Face(new[] { b, b2, c2, c }, new Vector3(height, hw, 0));
            var m = new Mesh { name = "GablePrism" };
            m.SetVertices(v); m.SetTriangles(tri, 0);
            var uv = new Vector2[v.Count];
            for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(v[i].x * 0.4f, v[i].y * 0.4f);
            m.uv = uv;
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
    }
}

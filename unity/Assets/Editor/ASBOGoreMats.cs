#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.EditorTools
{
    /// <summary>
    /// Blood material templates saved under Resources so their shader variants survive into phone builds: cut-out
    /// splat decals, soft round droplets (alpha-blended particles) and a glossy pool.
    /// </summary>
    public static class ASBOGoreMats
    {
        const string Dir = "Assets/Resources/GoreMats/";

        [MenuItem("ASBO/Art/Build Gore Materials")]
        public static void Build()
        {
            Directory.CreateDirectory(Dir);
            var drop = Png("Drop", 64, (u, v) => { float d = new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f; float a = Mathf.Clamp01(1f - d); a = a * a * (3f - 2f * a); return new Color(1f, 1f, 1f, a); });
            var splat = Png("Splat", 128, (u, v) =>
            {
                float dx = u * 2f - 1f, dy = v * 2f - 1f, r = Mathf.Sqrt(dx * dx + dy * dy), ang = Mathf.Atan2(dy, dx);
                float edge = 0.42f + 0.3f * Mathf.PerlinNoise(Mathf.Cos(ang) * 1.6f + 3f, Mathf.Sin(ang) * 1.6f + 3f) + 0.1f * Mathf.Sin(ang * 6f);
                bool inside = r < edge || (Mathf.PerlinNoise(u * 22f, v * 22f) > 0.74f && r < 0.92f);
                float shade = 0.65f + 0.35f * Mathf.PerlinNoise(u * 12f, v * 12f);
                return inside ? new Color(0.36f * shade, 0.015f, 0.025f, 1f) : new Color(0.3f, 0.01f, 0.02f, 0f);
            });
            var lit = Shader.Find("Universal Render Pipeline/Lit"); var part = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var s = Get("BloodSplat", lit);
            s.SetTexture("_BaseMap", splat); s.SetColor("_BaseColor", Color.white); s.SetFloat("_Smoothness", 0.62f); s.SetFloat("_Metallic", 0f);
            s.SetFloat("_AlphaClip", 1f); s.SetFloat("_Cutoff", 0.5f); s.EnableKeyword("_ALPHATEST_ON"); s.SetOverrideTag("RenderType", "TransparentCutout"); s.renderQueue = 2450; s.enableInstancing = true;
            var d = Get("BloodDrop", part);
            d.SetTexture("_BaseMap", drop); d.SetColor("_BaseColor", new Color(0.6f, 0.03f, 0.04f, 1f));
            d.SetFloat("_Surface", 1f); d.SetFloat("_Blend", 0f);
            d.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); d.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); d.SetFloat("_ZWrite", 0f);
            d.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); d.SetOverrideTag("RenderType", "Transparent"); d.renderQueue = (int)RenderQueue.Transparent;
            var pl = Get("BloodPool", lit);
            pl.SetColor("_BaseColor", new Color(0.26f, 0.008f, 0.018f)); pl.SetFloat("_Smoothness", 0.93f); pl.SetFloat("_Metallic", 0f); pl.enableInstancing = true;
            foreach (var m in new[] { s, d, pl }) EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
            Debug.Log("[ASBO] gore materials written to " + Dir);
        }

        static Texture2D Png(string name, int N, System.Func<float, float, Color> f)
        {
            string p = Dir + name + ".png"; var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) t.SetPixel(x, y, f((x + 0.5f) / N, (y + 0.5f) / N));
            t.Apply(); File.WriteAllBytes(p, t.EncodeToPNG()); Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(p);
            var ti = (TextureImporter)AssetImporter.GetAtPath(p); ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.mipmapEnabled = true; ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }

        static Material Get(string name, Shader sh)
        {
            string p = Dir + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(sh) { name = name }; AssetDatabase.CreateAsset(m, p); } else if (m.shader != sh) m.shader = sh;
            return m;
        }
    }
}
#endif

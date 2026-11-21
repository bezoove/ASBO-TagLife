#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.EditorTools
{
    /// <summary>
    /// Material templates for the procedural cars, saved under Resources so every shader variant they use
    /// (transparent premultiplied glass, emissive lamps, unlit alpha shadow) is kept in Android builds.
    /// </summary>
    public static class ASBOCarMats
    {
        const string Dir = "Assets/Resources/CarMats/";

        [MenuItem("ASBO/Art/Build Car Materials")]
        public static void Build()
        {
            Directory.CreateDirectory(Dir);
            var lit = Shader.Find("Universal Render Pipeline/Lit"); var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            Lit("Paint", lit, Color.white, 0.84f, 0.4f);
            Lit("Plate", lit, Color.white, 0.4f, 0f);
            Lit("Trim", lit, new Color(0.03f, 0.03f, 0.035f), 0.62f, 0f);
            Lit("Under", lit, new Color(0.035f, 0.035f, 0.035f), 0.08f, 0f);
            Lit("Chrome", lit, new Color(0.86f, 0.87f, 0.89f), 0.93f, 1f);
            Lit("Tyre", lit, new Color(0.045f, 0.045f, 0.05f), 0.3f, 0f);
            Lit("Alloy", lit, new Color(0.64f, 0.65f, 0.67f), 0.76f, 0.9f);
            Lit("Interior", lit, new Color(0.075f, 0.075f, 0.08f), 0.15f, 0f);
            Lit("Grille", lit, new Color(0.02f, 0.02f, 0.025f), 0.35f, 0.2f);
            Emit("LampFront", lit, new Color(0.92f, 0.92f, 0.88f), new Color(1f, 0.96f, 0.85f) * 1.4f);
            Emit("LampRear", lit, new Color(0.45f, 0.02f, 0.02f), new Color(0.85f, 0.05f, 0.04f) * 0.9f);
            Emit("LampAmber", lit, new Color(0.6f, 0.3f, 0.02f), new Color(1f, 0.45f, 0.05f) * 0.45f);

            // glass: premultiplied transparency keeps the reflections bright while you can see in
            var g = Lit("Glass", lit, new Color(0.05f, 0.07f, 0.085f, 0.5f), 0.96f, 0f);
            g.SetFloat("_Surface", 1f); g.SetFloat("_Blend", 1f);
            g.SetFloat("_SrcBlend", (float)BlendMode.One); g.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            g.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); g.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            g.SetFloat("_ZWrite", 0f); g.SetFloat("_Cull", 0f);
            g.SetOverrideTag("RenderType", "Transparent");
            g.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); g.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            g.renderQueue = (int)RenderQueue.Transparent;
            g.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(g);

            // soft contact shadow under each car
            string tp = Dir + "CarShadow.png";
            const int W = 64, H = 128; var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = Mathf.Abs((x + 0.5f) / W - 0.5f) * 2f, dy = Mathf.Abs((y + 0.5f) / H - 0.5f) * 2f;
                    float ex = Mathf.Max(0f, dx - 0.55f) / 0.45f, ey = Mathf.Max(0f, dy - 0.75f) / 0.25f; // rounded rectangle
                    float d = Mathf.Sqrt(ex * ex + ey * ey);
                    float a = Mathf.Clamp01(1f - d); a = a * a * (3f - 2f * a);
                    tex.SetPixel(x, y, new Color(0, 0, 0, a));
                }
            tex.Apply(); File.WriteAllBytes(tp, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(tp);
            var ti = (TextureImporter)AssetImporter.GetAtPath(tp); ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.sRGBTexture = false; ti.SaveAndReimport();
            var sh = Get("Shadow", unlit);
            sh.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tp)); sh.SetColor("_BaseColor", new Color(0, 0, 0, 0.62f));
            sh.SetFloat("_Surface", 1f); sh.SetFloat("_Blend", 0f);
            sh.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); sh.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            sh.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); sh.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            sh.SetFloat("_ZWrite", 0f); sh.SetOverrideTag("RenderType", "Transparent");
            sh.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); sh.renderQueue = 2501;
            EditorUtility.SetDirty(sh);
            AssetDatabase.SaveAssets();
            Debug.Log("[ASBO] car material templates written to " + Dir);
        }

        static Material Get(string name, Shader s)
        {
            string p = Dir + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) { m = new Material(s) { name = name }; AssetDatabase.CreateAsset(m, p); }
            else if (m.shader != s) m.shader = s;
            return m;
        }

        static Material Lit(string name, Shader s, Color c, float smooth, float metal)
        {
            var m = Get(name, s);
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
            m.enableInstancing = false;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Emit(string name, Shader s, Color c, Color e)
        {
            var m = Lit(name, s, c, 0.9f, 0f);
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", e); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
#endif

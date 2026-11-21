using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Vehicle lettering and signs painted onto the bodywork: text drawn into a texture with a built-in block font
    /// (bold, upright, like real livery), laid on the panel as a thin decal so it follows the surface instead of
    /// floating flat in front of it. Mirror it for the front of an ambulance.
    /// </summary>
    public static class Lettering
    {
        // 5x7 block capitals: each glyph is 7 rows of 5 bits, top row first
        static readonly Dictionary<char, string[]> G = new Dictionary<char, string[]>
        {
            ['A'] = new[] { "01110", "10001", "10001", "11111", "10001", "10001", "10001" },
            ['B'] = new[] { "11110", "10001", "10001", "11110", "10001", "10001", "11110" },
            ['C'] = new[] { "01111", "10000", "10000", "10000", "10000", "10000", "01111" },
            ['D'] = new[] { "11110", "10001", "10001", "10001", "10001", "10001", "11110" },
            ['E'] = new[] { "11111", "10000", "10000", "11110", "10000", "10000", "11111" },
            ['F'] = new[] { "11111", "10000", "10000", "11110", "10000", "10000", "10000" },
            ['H'] = new[] { "10001", "10001", "10001", "11111", "10001", "10001", "10001" },
            ['I'] = new[] { "11111", "00100", "00100", "00100", "00100", "00100", "11111" },
            ['L'] = new[] { "10000", "10000", "10000", "10000", "10000", "10000", "11111" },
            ['M'] = new[] { "10001", "11011", "10101", "10101", "10001", "10001", "10001" },
            ['N'] = new[] { "10001", "11001", "10101", "10011", "10001", "10001", "10001" },
            ['O'] = new[] { "01110", "10001", "10001", "10001", "10001", "10001", "01110" },
            ['P'] = new[] { "11110", "10001", "10001", "11110", "10000", "10000", "10000" },
            ['R'] = new[] { "11110", "10001", "10001", "11110", "10100", "10010", "10001" },
            ['S'] = new[] { "01111", "10000", "10000", "01110", "00001", "00001", "11110" },
            ['T'] = new[] { "11111", "00100", "00100", "00100", "00100", "00100", "00100" },
            ['U'] = new[] { "10001", "10001", "10001", "10001", "10001", "10001", "01110" },
            ['V'] = new[] { "10001", "10001", "10001", "10001", "10001", "01010", "00100" },
            ['W'] = new[] { "10001", "10001", "10001", "10101", "10101", "10101", "01010" },
            ['Y'] = new[] { "10001", "10001", "01010", "00100", "00100", "00100", "00100" },
            ['9'] = new[] { "01110", "10001", "10001", "01111", "00001", "00001", "01110" },
            [' '] = new[] { "00000", "00000", "00000", "00000", "00000", "00000", "00000" },
        };
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        /// <summary>The text as a texture: ink colour on transparent, crisp edges, optional mirror (reads right in a mirror).</summary>
        public static Texture2D Texture(string text, Color ink, bool mirror = false)
        {
            string key = text + ink + mirror;
            if (cache.TryGetValue(key, out var have) && have != null) return have;
            const int cell = 12, gap = 3, pad = 6; // pixels per font dot, between letters, round the edge
            int w = pad * 2 + text.Length * (5 * cell + gap * cell) - gap * cell, h = pad * 2 + 7 * cell;
            var px = new Color32[w * h]; var clear = new Color32(0, 0, 0, 0); var c = (Color32)ink;
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            for (int ci = 0; ci < text.Length; ci++)
            {
                if (!G.TryGetValue(char.ToUpperInvariant(text[ci]), out var gl)) continue;
                int ox = pad + ci * (5 + gap) * cell;
                for (int row = 0; row < 7; row++)
                    for (int col = 0; col < 5; col++)
                    {
                        if (gl[row][col] != '1') continue;
                        for (int yy = 0; yy < cell; yy++)
                            for (int xx = 0; xx < cell; xx++)
                            {
                                int x = ox + col * cell + xx, y = h - 1 - (pad + row * cell + yy);
                                if (mirror) x = w - 1 - x;
                                px[y * w + x] = c;
                            }
                    }
            }
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4, name = "Letters " + text };
            t.SetPixels32(px); t.Apply(true, true);
            cache[key] = t; return t;
        }

        /// <summary>A decal of the text on a panel: centre, outward normal, which way is up, and the letter height in metres.</summary>
        public static GameObject Decal(Transform parent, string text, Color ink, Vector3 centre, Vector3 normal, Vector3 up, float height, bool mirror = false)
        {
            var tex = Texture(text, ink, mirror);
            float width = height * tex.width / (float)tex.height;
            var g = GameObject.CreatePrimitive(PrimitiveType.Quad); g.name = "Lettering " + text;
            Object.Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = centre + normal.normalized * 0.004f;
            g.transform.localRotation = Quaternion.LookRotation(-normal, up);
            g.transform.localScale = new Vector3(width, height, 1f);
            // the cut-out material is copied from a template kept in Resources (a runtime-made one loses its alpha-clip variant in builds)
            var tpl = Resources.Load<Material>("GoreMats/BloodSplat");
            var m = tpl != null ? new Material(tpl) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.name = "Lettering"; m.SetTexture("_BaseMap", tex); m.mainTexture = tex; m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", 0.5f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.55f);
            m.enableInstancing = false;
            g.GetComponent<MeshRenderer>().sharedMaterial = m;
            g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }
    }
}

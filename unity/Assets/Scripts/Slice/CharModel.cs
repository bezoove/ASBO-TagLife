using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Skinned character mesh built offline from CC0 MakeHuman data (Resources/Chars/*.bytes):
    /// 19-bone game skeleton, arms-down rest pose, sub-meshes skin/hair/top/bottom/shoes/eyes/lashes.
    /// </summary>
    public class CharModel
    {
        public string name;
        public string[] bones; public int[] parent; public Vector3[] heads;
        public Mesh mesh; public string[] mats;
        public byte[] domBone; // dominant bone per vertex (for severing limbs)
        static readonly Dictionary<string, CharModel> cache = new Dictionary<string, CharModel>();

        public int Bone(string n) { for (int i = 0; i < bones.Length; i++) if (bones[i] == n) return i; return -1; }
        public Vector3 Head(string n) { int i = Bone(n); return i >= 0 ? heads[i] : Vector3.zero; }

        public static CharModel Load(string name)
        {
            if (cache.TryGetValue(name, out var m)) return m;
            var ta = Resources.Load<TextAsset>("Chars/" + name);
            if (ta == null) { Debug.LogWarning("[ASBO] missing character " + name); return null; }
            m = new CharModel { name = name };
            using (var br = new BinaryReader(new MemoryStream(ta.bytes)))
            {
                br.ReadBytes(4); br.ReadInt32();
                int n = br.ReadInt32(), nb = br.ReadInt32();
                m.bones = new string[nb]; m.parent = new int[nb]; m.heads = new Vector3[nb];
                for (int i = 0; i < nb; i++)
                {
                    m.bones[i] = new string(br.ReadChars(br.ReadByte()));
                    m.parent[i] = br.ReadInt32();
                    m.heads[i] = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                }
                var pos = new Vector3[n]; var nrm = new Vector3[n]; var uv = new Vector2[n];
                for (int i = 0; i < n; i++) pos[i] = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                for (int i = 0; i < n; i++) nrm[i] = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                for (int i = 0; i < n; i++) uv[i] = new Vector2(br.ReadSingle(), br.ReadSingle());
                var bi = br.ReadBytes(n * 4);
                var bw = new BoneWeight[n]; m.domBone = new byte[n];
                for (int i = 0; i < n; i++)
                {
                    float w0 = br.ReadSingle(), w1 = br.ReadSingle(), w2 = br.ReadSingle(), w3 = br.ReadSingle();
                    bw[i] = new BoneWeight { boneIndex0 = bi[i * 4], boneIndex1 = bi[i * 4 + 1], boneIndex2 = bi[i * 4 + 2], boneIndex3 = bi[i * 4 + 3], weight0 = w0, weight1 = w1, weight2 = w2, weight3 = w3 };
                    m.domBone[i] = bi[i * 4];
                }
                int nm = br.ReadInt32();
                m.mats = new string[nm];
                var mesh = new Mesh { name = name };
                if (n > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.vertices = pos; mesh.normals = nrm; mesh.uv = uv; mesh.boneWeights = bw;
                mesh.subMeshCount = nm;
                for (int s = 0; s < nm; s++)
                {
                    m.mats[s] = new string(br.ReadChars(br.ReadByte()));
                    int cnt = br.ReadInt32();
                    var idx = new int[cnt];
                    if (n < 65535) for (int i = 0; i < cnt; i++) idx[i] = br.ReadUInt16();
                    else for (int i = 0; i < cnt; i++) idx[i] = br.ReadInt32();
                    mesh.SetTriangles(idx, s, false);
                }
                var bind = new Matrix4x4[nb];
                for (int i = 0; i < nb; i++) bind[i] = Matrix4x4.Translate(-m.heads[i]);
                mesh.bindposes = bind;
                mesh.RecalculateBounds();
                m.mesh = mesh;
            }
            cache[name] = m;
            return m;
        }

        // ---------------- materials ----------------
        static Texture2D fabricTex, hairTex, eyeTex;
        static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();
        static Material Cached(string key, System.Func<Material> make) { if (!matCache.TryGetValue(key, out var m)) { m = make(); matCache[key] = m; } return m; }
        static string K(Color c) => ColorUtility.ToHtmlStringRGB(c);

        public static Material Skin(bool male, bool stubble, Color tone) => Cached("skin" + male + stubble + K(tone), () =>
        {
            var tex = Resources.Load<Texture2D>(male ? (stubble ? "Chars/skin_m_stubble" : "Chars/skin_m") : "Chars/skin_f");
            var m = Gfx.Mat(tone, tex, 0.32f); return m;
        });
        public static Material Hair(Color c) => Cached("hair" + K(c), () => { hairTex ??= HairTex(); var m = Gfx.Mat(c, hairTex, 0.42f); m.SetTextureScale("_BaseMap", new Vector2(6, 6)); return m; });
        public static Material Cloth(Color c, float smooth = 0.12f) => Cached("cloth" + K(c) + smooth, () => { fabricTex ??= FabricTex(); var m = Gfx.Mat(c, fabricTex, smooth); m.SetTextureScale("_BaseMap", new Vector2(28, 28)); return m; });
        /// <summary>Alpha-cut, double-sided strand cards for hair.</summary>
        public static Material HairCards(Color c) => Cached("hcard" + K(c), () =>
        {
            cardTex ??= CardTex();
            var m = Gfx.Mat(c * 1.1f, cardTex, 0.45f);
            m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.4f); m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", 0f); m.renderQueue = 2450;
            return m;
        });
        static Texture2D cardTex;
        static Texture2D CardTex()
        {
            const int W = 128, H = 128; var px = new Color32[W * H]; var r = new System.Random(5);
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);
            for (int s = 0; s < 70; s++)
            {
                float x0 = (float)r.NextDouble() * W, wob = (float)r.NextDouble() * 3f, len = H * (0.55f + 0.45f * (float)r.NextDouble());
                byte v = (byte)(150 + r.Next(105));
                for (int y = 0; y < len; y++)
                {
                    int x = Mathf.RoundToInt(x0 + Mathf.Sin(y * 0.06f + s) * wob) % W; if (x < 0) x += W;
                    float a = 1f - Mathf.Pow(y / len, 3f);
                    px[y * W + x] = new Color32(v, v, v, (byte)(a * 255));
                    int x2 = (x + 1) % W; if (px[y * W + x2].a < a * 120) px[y * W + x2] = new Color32(v, v, v, (byte)(a * 120));
                }
            }
            var t = new Texture2D(W, H, TextureFormat.RGBA32, true); t.SetPixels32(px); t.wrapMode = TextureWrapMode.Repeat; t.Apply(true); return t;
        }

        public static Material Shoes(Color c) => Cached("shoe" + K(c), () => Gfx.Mat(c, null, 0.55f));
        public static Material Eyes() => Cached("eyes", () => { var t = Resources.Load<Texture2D>("Chars/eye_brown"); return Gfx.Mat(Color.white, t, 0.85f); });
        public static Material Lashes() => Cached("lashes", () => { var m = Gfx.Mat(new Color(0.05f, 0.04f, 0.03f), null, 0.2f); m.SetFloat("_Cull", 0f); return m; });

        static Texture2D FabricTex()
        {
            const int S = 64; var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    float wx = Mathf.Abs(Mathf.Sin(x * Mathf.PI / 2f)), wy = Mathf.Abs(Mathf.Sin(y * Mathf.PI / 2f));
                    float v = 0.86f + 0.08f * ((x + y) % 2 == 0 ? wx : wy) + 0.06f * Mathf.PerlinNoise(x * 0.3f, y * 0.3f);
                    byte b = (byte)(Mathf.Clamp01(v) * 255); px[y * S + x] = new Color32(b, b, b, 255);
                }
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true); t.SetPixels32(px); t.wrapMode = TextureWrapMode.Repeat; t.Apply(true); return t;
        }
        static Texture2D HairTex()
        {
            const int S = 128; var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    float v = 0.65f + 0.35f * Mathf.PerlinNoise(x * 0.9f, y * 0.04f) - 0.15f * Mathf.PerlinNoise(x * 3.1f, y * 0.2f);
                    byte b = (byte)(Mathf.Clamp01(v) * 255); px[y * S + x] = new Color32(b, b, b, 255);
                }
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true); t.SetPixels32(px); t.wrapMode = TextureWrapMode.Repeat; t.Apply(true); return t;
        }

        public static readonly string[] Male = { "m_young_tee_short", "m_young_shirt_short", "m_fit_jacket_short", "m_fit_tee_short", "m_heavy_shirt_bald", "m_heavy_jacket_short", "m_old_jacket_bald", "m_old_shirt_bald" };
        public static readonly string[] Female = { "f_young_tee_long", "f_young_skirt_bob", "f_slim_shirt_long", "f_slim_dress_bob", "f_curvy_skirt_bob", "f_curvy_jacket_bun", "f_old_skirt_bun", "f_old_jacket_bun" };
    }
}

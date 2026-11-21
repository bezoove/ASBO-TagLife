#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.EditorTools
{
    /// <summary>
    /// Hedge overhaul for the baked scene, without re-baking lighting: the hedge material gets a dense leaf texture with
    /// a normal map, and every hedge gets a shell of alpha-cut leaf cards scattered over its faces so the outline is
    /// leafy instead of a box. Cards are lit by light probes, don't cast shadows, and fade out with a LODGroup.
    /// </summary>
    public static class ASBOHedges
    {
        const string TexDir = "Assets/Art/Generated/Textures/", MatDir = "Assets/Art/Generated/Materials/", MeshDir = "Assets/Art/Generated/Meshes/";
        static readonly Color HedgeTint = new Color(0.12f, 0.3f, 0.1f);

        [MenuItem("ASBO/Art/Upgrade Hedges")]
        public static void Upgrade()
        {
            Directory.CreateDirectory(TexDir);
            var surf = Leaves("HedgeSurface", 512, true, 11); var surfN = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "HedgeSurface_N.png");
            var card = Leaves("HedgeCard", 512, false, 23); var cardN = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "HedgeCard_N.png");

            // 1. the hedge body material (found by its tint) gets the leaf texture
            var hedgeMats = new HashSet<Material>();
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.HasProperty("_BaseColor") && (Near(m.GetColor("_BaseColor"), HedgeTint) || (m.GetTexture("_BaseMap") != null && m.GetTexture("_BaseMap").name == "HedgeSurface"))) hedgeMats.Add(m);
            if (hedgeMats.Count == 0) { Debug.LogError("[ASBO] no hedge material found"); return; }
            foreach (var m in hedgeMats)
            {
                m.SetTexture("_BaseMap", surf); m.SetColor("_BaseColor", new Color(0.92f, 0.98f, 0.9f));
                m.SetTexture("_BumpMap", surfN); m.SetFloat("_BumpScale", 1.2f); m.EnableKeyword("_NORMALMAP");
                m.SetFloat("_Smoothness", 0.22f);
                EditorUtility.SetDirty(m);
            }

            // 2. leaf-card material
            string cardMatPath = MatDir + "HedgeLeaves.mat";
            var cm = AssetDatabase.LoadAssetAtPath<Material>(cardMatPath);
            if (cm == null) { cm = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(cm, cardMatPath); }
            cm.SetTexture("_BaseMap", card); cm.SetColor("_BaseColor", Color.white);
            cm.SetTexture("_BumpMap", cardN); cm.EnableKeyword("_NORMALMAP");
            cm.SetFloat("_AlphaClip", 1f); cm.SetFloat("_Cutoff", 0.45f); cm.EnableKeyword("_ALPHATEST_ON");
            cm.SetFloat("_Cull", 0f); cm.SetFloat("_Smoothness", 0.25f); cm.renderQueue = 2450; cm.enableInstancing = true;
            cm.SetOverrideTag("RenderType", "TransparentCutout");
            EditorUtility.SetDirty(cm);

            // 3. shells
            foreach (var old in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t != null && t.name == "HedgeLeaves").ToList())
                Object.DestroyImmediate(old.gameObject);
            int n = 0, cards = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                var mats = r.sharedMaterials;
                for (int sub = 0; sub < mats.Length && sub < mf.sharedMesh.subMeshCount; sub++)
                {
                    if (!hedgeMats.Contains(mats[sub])) continue;
                    var mesh = Shell(mf.sharedMesh, sub, r.transform, 1000 + n * 17, out int c);
                    if (mesh == null) continue;
                    string mp = MeshDir + "HedgeLeaves_" + n + ".asset";
                    if (File.Exists(mp)) AssetDatabase.DeleteAsset(mp);
                    AssetDatabase.CreateAsset(mesh, mp);
                    var go = new GameObject("HedgeLeaves"); go.transform.SetParent(r.transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = cm;
                    mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = true;
                    mr.lightProbeUsage = LightProbeUsage.BlendProbes; mr.receiveGI = ReceiveGI.LightProbes;
                    GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
                    var lg = go.AddComponent<LODGroup>();
                    lg.SetLODs(new[] { new LOD(0.035f, new Renderer[] { mr }) }); lg.fadeMode = LODFadeMode.None;
                    n++; cards += c;
                }
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log($"[ASBO] hedges upgraded: {hedgeMats.Count} material(s), {n} leaf shells, {cards} cards");
        }

        static bool Near(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.04f;

        /// <summary>Leaf cards scattered over the faces of one submesh (in the renderer's local space), area-weighted.</summary>
        static Mesh Shell(Mesh src, int sub, Transform t, int seed, out int count)
        {
            count = 0;
            var v = src.vertices; var nm = src.normals; var tri = src.GetTriangles(sub);
            if (tri.Length == 0) return null;
            Vector3 sc = t.lossyScale;
            var areas = new List<float>(); float total = 0f;
            for (int i = 0; i < tri.Length; i += 3)
            {
                Vector3 a = Vector3.Scale(v[tri[i]], sc), b = Vector3.Scale(v[tri[i + 1]], sc), c = Vector3.Scale(v[tri[i + 2]], sc);
                Vector3 fn = Vector3.Cross(b - a, c - a); float ar = fn.magnitude * 0.5f;
                if (fn.normalized.y < -0.5f) ar = 0f; // no cards underneath
                areas.Add(ar); total += ar;
            }
            if (total < 0.05f) return null;
            var rng = new System.Random(seed);
            float R() => (float)rng.NextDouble();
            int want = Mathf.Clamp(Mathf.RoundToInt(total * 9f), 4, 60000 / 4);
            var cum = new float[areas.Count]; float acc = 0f; for (int i = 0; i < areas.Count; i++) { acc += areas[i]; cum[i] = acc; }
            var ov = new List<Vector3>(); var on = new List<Vector3>(); var ou = new List<Vector2>(); var ot = new List<int>(); var oc = new List<Vector4>();
            for (int k = 0; k < want; k++)
            {
                float pick = R() * total; int ti = System.Array.BinarySearch(cum, pick); if (ti < 0) ti = ~ti; ti = Mathf.Min(ti, areas.Count - 1);
                int i0 = tri[ti * 3], i1 = tri[ti * 3 + 1], i2 = tri[ti * 3 + 2];
                float u = R(), w = R(); if (u + w > 1f) { u = 1f - u; w = 1f - w; }
                Vector3 p = v[i0] + (v[i1] - v[i0]) * u + (v[i2] - v[i0]) * w;
                Vector3 fnl = Vector3.Cross(v[i1] - v[i0], v[i2] - v[i0]).normalized;
                // local scale correction for the normal and offset
                Vector3 n = fnl;
                Vector3 rnd = new Vector3(R() * 2f - 1f, R() * 2f - 1f, R() * 2f - 1f);
                Vector3 cn = (n + rnd * 0.75f).normalized; if (Vector3.Dot(cn, n) < 0.35f) cn = (cn + n).normalized;
                Vector3 tA = Vector3.Cross(cn, Mathf.Abs(cn.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
                Vector3 tB = Vector3.Cross(cn, tA);
                float ang = R() * Mathf.PI * 2f; Vector3 ax = tA * Mathf.Cos(ang) + tB * Mathf.Sin(ang), ay = Vector3.Cross(cn, ax);
                float size = Mathf.Lerp(0.26f, 0.48f, R());
                Vector3 c0 = p + n * Mathf.Lerp(-0.02f, 0.07f, R());
                Vector3 hx = new Vector3(ax.x / sc.x, ax.y / sc.y, ax.z / sc.z) * size * 0.5f, hy = new Vector3(ay.x / sc.x, ay.y / sc.y, ay.z / sc.z) * size * 0.5f;
                int b0 = ov.Count;
                ov.Add(c0 - hx - hy); ov.Add(c0 + hx - hy); ov.Add(c0 + hx + hy); ov.Add(c0 - hx + hy);
                Vector3 ln = Vector3.Slerp(n, cn, 0.35f); // light like the hedge surface, a little varied
                for (int q = 0; q < 4; q++) on.Add(ln);
                ou.Add(new Vector2(0, 0)); ou.Add(new Vector2(1, 0)); ou.Add(new Vector2(1, 1)); ou.Add(new Vector2(0, 1));
                Vector4 tg = new Vector4(ax.x, ax.y, ax.z, 1f); for (int q = 0; q < 4; q++) oc.Add(tg);
                ot.Add(b0); ot.Add(b0 + 2); ot.Add(b0 + 1); ot.Add(b0); ot.Add(b0 + 3); ot.Add(b0 + 2);
                count++;
            }
            var m = new Mesh { name = "HedgeLeaves" };
            if (ov.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(ov); m.SetNormals(on); m.SetUVs(0, ou); m.SetTangents(oc); m.SetTriangles(ot, 0); m.RecalculateBounds();
            return m;
        }

        /// <summary>Procedural privet leaves: small glossy ovals with a midrib, layered dark-to-light. opaque = tileable surface, else a clump card with alpha.</summary>
        static Texture2D Leaves(string name, int N, bool opaque, int seed)
        {
            var rng = new System.Random(seed); float R() => (float)rng.NextDouble();
            var col = new Color[N * N]; var nrm = new Vector3[N * N];
            for (int i = 0; i < col.Length; i++) { col[i] = opaque ? new Color(0.035f, 0.075f, 0.03f, 1f) : new Color(0.08f, 0.16f, 0.06f, 0f); nrm[i] = Vector3.forward; }
            if (opaque) // twiggy dark interior first
                for (int k = 0; k < 140; k++)
                {
                    float x = R() * N, y = R() * N, a = R() * Mathf.PI, L = N * (0.05f + R() * 0.12f);
                    for (float s = 0; s < L; s += 0.7f) { int px = ((int)(x + Mathf.Cos(a) * s) % N + N) % N, py = ((int)(y + Mathf.Sin(a) * s) % N + N) % N; col[py * N + px] = new Color(0.07f, 0.06f, 0.035f, 1f); }
                }
            int count = opaque ? 1100 : 300;
            for (int k = 0; k < count; k++)
            {
                float depth = (k + 1f) / count;
                float cx, cy;
                if (opaque) { cx = R() * N; cy = R() * N; }
                else { float rr = N * 0.42f * Mathf.Sqrt(R()), aa = R() * Mathf.PI * 2f; cx = N * 0.5f + Mathf.Cos(aa) * rr; cy = N * 0.5f + Mathf.Sin(aa) * rr; }
                float len = N * (opaque ? 0.034f : 0.062f) * Mathf.Lerp(0.75f, 1.3f, R()), wid = len * Mathf.Lerp(0.42f, 0.55f, R());
                float ang = R() * Mathf.PI * 2f, ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
                Vector2 tilt = new Vector2(R() * 1.2f - 0.6f, R() * 1.2f - 0.6f);
                Color baseC = Color.Lerp(new Color(0.09f, 0.22f, 0.05f), new Color(0.27f, 0.46f, 0.12f), R() * R() + depth * 0.35f);
                float lightK = Mathf.Lerp(0.45f, 1.05f, depth) * Mathf.Lerp(0.85f, 1.1f, R());
                int r = Mathf.CeilToInt(len) + 1;
                for (int oy = -r; oy <= r; oy++)
                    for (int ox = -r; ox <= r; ox++)
                    {
                        float u = ox * ca + oy * sa, vv = -ox * sa + oy * ca;
                        float taper = 1f - 0.45f * Mathf.Abs(u) / len;
                        float e = (u / len) * (u / len) + (vv / (wid * taper)) * (vv / (wid * taper));
                        if (e >= 1f) continue;
                        int px = Mathf.RoundToInt(cx) + ox, py = Mathf.RoundToInt(cy) + oy;
                        if (opaque) { px = (px % N + N) % N; py = (py % N + N) % N; } else if (px < 0 || py < 0 || px >= N || py >= N) continue;
                        float shade = (0.78f + 0.22f * (1f - e)) * lightK;
                        if (Mathf.Abs(vv) < wid * 0.07f && Mathf.Abs(u) < len * 0.85f) shade *= 0.82f; // midrib
                        float spec = Mathf.Pow(Mathf.Max(0f, 1f - e), 3f) * 0.08f;
                        var c = baseC * shade; c.r += spec; c.g += spec; c.b += spec * 0.6f; c.a = 1f;
                        col[py * N + px] = c;
                        // dome + tilt, rotated back to texture space
                        float lx = u / len * 0.7f, ly = vv / wid * 0.7f;
                        Vector3 ln = new Vector3(lx * ca - ly * sa + tilt.x, lx * sa + ly * ca + tilt.y, 1f).normalized;
                        nrm[py * N + px] = ln;
                    }
            }
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.SetPixels(col); tex.Apply();
            var nt = new Texture2D(N, N, TextureFormat.RGBA32, false);
            var nc = new Color[N * N]; for (int i = 0; i < nc.Length; i++) { var q = nrm[i]; nc[i] = new Color(q.x * 0.5f + 0.5f, q.y * 0.5f + 0.5f, q.z * 0.5f + 0.5f, 1f); }
            nt.SetPixels(nc); nt.Apply();
            string p = TexDir + name + ".png", pn = TexDir + name + "_N.png";
            File.WriteAllBytes(p, tex.EncodeToPNG()); File.WriteAllBytes(pn, nt.EncodeToPNG());
            Object.DestroyImmediate(tex); Object.DestroyImmediate(nt);
            AssetDatabase.ImportAsset(p); AssetDatabase.ImportAsset(pn);
            var ti = (TextureImporter)AssetImporter.GetAtPath(p);
            ti.sRGBTexture = true; ti.alphaIsTransparency = !opaque; ti.mipmapEnabled = true; ti.wrapMode = opaque ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            if (!opaque) { ti.mipMapsPreserveCoverage = true; ti.alphaTestReferenceValue = 0.45f; }
            ti.SaveAndReimport();
            var tn = (TextureImporter)AssetImporter.GetAtPath(pn);
            tn.textureType = TextureImporterType.NormalMap; tn.wrapMode = ti.wrapMode; tn.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }
    }
}
#endif

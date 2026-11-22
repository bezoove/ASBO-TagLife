using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ASBO.Slice;

namespace ASBO.EditorTools
{
    /// <summary>
    /// The Hatchback and Sedan pack (your Asset Store download) as our 90s hatch and saloon: each model turned to face
    /// +z, sat on the ground and scaled to our car's length; URP materials made from its texture (the paint panels get
    /// their own material so each car can be any colour); the wheels wrapped so the driving code can spin and steer
    /// them; and the driver's door (offside, right-hand drive) cut out of the shell and hung on a hinge so it swings open
    /// like the rest. Saved as Resources/CarsPack/hatch90 and saloon90 (out of git: they're made from the pack).
    /// CarKit uses them for clean, tired and dirty cars of those two kinds; rusty, wrecked and burnt ones stay ours.
    /// </summary>
    public static class ASBOPackCars
    {
        const string Out = "Assets/Resources/CarsPack";
        static readonly (string id, string prefab, float door)[] Map =
        {
            ("hatch90", "Assets/Hatchback and Sedan/prefabs/HATCHBACK_1988.prefab", 1.22f),
            ("saloon90", "Assets/Hatchback and Sedan/prefabs/SEDAN.prefab", 1.05f),
        };

        [MenuItem("ASBO/Cars/Use Pack Cars")]
        public static void Make()
        {
            Directory.CreateDirectory(Out + "/Mats"); Directory.CreateDirectory(Out + "/Meshes");
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var glass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/CarMats/KitGlass.mat");
            foreach (var (id, path, doorLen) in Map)
            {
                var src = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (src == null) { Debug.LogWarning("[ASBO] pack cars: missing " + path); continue; }
                var inst = (GameObject)Object.Instantiate(src);
                foreach (var c in inst.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                foreach (var mb in inst.GetComponentsInChildren<MonoBehaviour>(true)) if (mb != null) Object.DestroyImmediate(mb);
                inst.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var root = new GameObject(id); var holder = new GameObject("Model").transform; holder.SetParent(root.transform, false);
                inst.transform.SetParent(holder, true);
                var rends = inst.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.GetComponent<MeshFilter>()?.sharedMesh != null).ToList();
                // face +z: the long way round, front lamps forward
                var b = Bounds(rends);
                Vector3 fwd = b.size.x > b.size.z ? Vector3.right : Vector3.forward;
                var lamp = rends.FirstOrDefault(r => r.name.ToLowerInvariant().Contains("front_lamp") || r.name.ToUpperInvariant().StartsWith("F_LAMP"));
                if (lamp != null && Vector3.Dot(lamp.bounds.center - b.center, fwd) < 0) fwd = -fwd;
                holder.rotation = Quaternion.FromToRotation(fwd, Vector3.forward);
                b = Bounds(rends);
                // our car's length, on the ground, centred
                var spec = CarBody.Get(id).m; float s = spec.L / b.size.z;
                holder.localScale = Vector3.one * s; b = Bounds(rends);
                holder.position -= new Vector3(b.center.x, b.min.y, b.center.z);
                b = Bounds(rends);
                // wheels: the low, round pieces in each corner
                float midZ = b.center.z;
                var wheelBits = rends.Where(r => r.bounds.center.y < 0.5f && r.bounds.size.y > 0.12f && r.bounds.size.y < 0.9f && Mathf.Max(r.bounds.size.x, r.bounds.size.z) < 0.9f && Mathf.Abs(r.bounds.center.x) > b.extents.x * 0.45f).ToList();
                string[] names = { "wheel_FL", "wheel_FR", "wheel_RL", "wheel_RR" };
                var wheelR = new List<Renderer>();
                for (int q = 0; q < 4; q++)
                {
                    bool front = q < 2, left = q % 2 == 0;
                    var bits = wheelBits.Where(r => (r.bounds.center.z > midZ) == front && (r.bounds.center.x < 0f) == left).ToList();
                    if (bits.Count == 0) { Debug.LogWarning($"[ASBO] pack cars: {id} has no {names[q]}"); continue; }
                    var tyre = bits.OrderByDescending(r => r.bounds.size.y).First();
                    var wrap = new GameObject(names[q]).transform; wrap.SetParent(root.transform, false); wrap.position = tyre.bounds.center;
                    var spin = new GameObject("spin").transform; spin.SetParent(wrap, false);
                    foreach (var r in bits) { r.transform.SetParent(spin, true); wheelR.Add(r); }
                    spin.localRotation = Quaternion.Euler(0f, 0f, -90f); wrap.localRotation = Quaternion.Euler(0f, 0f, 90f); // the driving code sets Euler(steer) * Euler(spin, 0, 90)
                }
                // materials from the pack's texture: one for the paint panels (tinted per car), one for the rest, our glass
                var srcMat = rends.Select(r => r.sharedMaterial).FirstOrDefault(m => m != null && m.mainTexture != null);
                var tex = srcMat != null ? srcMat.mainTexture : null;
                Material Mat(string n, float smooth, float metal)
                {
                    string p = $"{Out}/Mats/{id}_{n}.mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(p);
                    if (m == null) { m = new Material(lit); AssetDatabase.CreateAsset(m, p); }
                    m.shader = lit; m.SetTexture("_BaseMap", tex); m.mainTexture = tex; m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
                    EditorUtility.SetDirty(m); return m;
                }
                var paintM = Mat("Paint", 0.62f, 0.18f); var bodyM = Mat("Body", 0.35f, 0.05f);
                foreach (var r in rends)
                {
                    string n = r.name.ToLowerInvariant();
                    bool paintPart = n.Contains("paint") || r.gameObject == inst; // the model's root mesh is its painted shell
                    var m = paintPart ? paintM : (n.Contains("glass") && !n.Contains("lamp")) || n == "windows" ? (glass != null ? glass : bodyM) : bodyM;
                    r.sharedMaterials = Enumerable.Repeat(m, r.sharedMaterials.Length).ToArray();
                    r.receiveGI = ReceiveGI.LightProbes; r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
                }
                // the driver's door: cut out of the right-hand side along its shut lines. Each triangle is clipped exactly at
                // the lines (front edge behind the front wheel, rear edge at the pillar, the sill below, the bottom of the
                // window above), so nothing beyond them swings open with it: the long low-poly triangles of the pack's body
                // used to drag half the side of the car round with the door. The side window goes with the door (a
                // frameless door: the frame stays on the body); everything inside the car stays put.
                Transform fw = root.transform.Find("wheel_FR");
                float zF = fw != null ? fw.position.z : spec.wxF, rad = fw != null ? Mathf.Max(0.2f, wheelR.Where(r => r.transform.IsChildOf(fw)).Select(r => r.bounds.extents.y).DefaultIfEmpty(0.3f).Max()) : spec.rad;
                float z1 = zF - rad - 0.14f, z0 = z1 - doorLen, xIn = b.max.x - 0.3f, y0 = rad + 0.04f;
                // the bottom of the side window: the door skin runs up to it
                float glassLo = float.MaxValue;
                foreach (var r in rends)
                {
                    if (!IsGlass(r)) continue; var mesh = r.GetComponent<MeshFilter>().sharedMesh; var v = mesh.vertices; var t = mesh.triangles;
                    for (int i = 0; i < t.Length; i += 3)
                    {
                        Vector3 a = r.transform.TransformPoint(v[t[i]]), bb = r.transform.TransformPoint(v[t[i + 1]]), c = r.transform.TransformPoint(v[t[i + 2]]);
                        var n = Vector3.Cross(bb - a, c - a).normalized; var ctr = (a + bb + c) / 3f;
                        if (Mathf.Abs(n.x) > 0.55f && ctr.x > xIn && ctr.z > z0 + 0.1f && ctr.z < z1 - 0.1f) glassLo = Mathf.Min(glassLo, Mathf.Min(a.y, Mathf.Min(bb.y, c.y)));
                    }
                }
                float belt = Mathf.Clamp(glassLo == float.MaxValue ? b.max.y * 0.66f : glassLo - 0.005f, b.max.y * 0.58f, b.max.y * 0.76f);
                var hinge = new GameObject("DoorHinge|" + doorLen.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)).transform;
                hinge.SetParent(root.transform, false); hinge.position = new Vector3(b.max.x - 0.06f, (y0 + belt) * 0.5f, z1);
                var cardM = Mat("DoorCard", 0.18f, 0f); cardM.SetTexture("_BaseMap", null); cardM.mainTexture = null; cardM.SetColor("_BaseColor", new Color(0.075f, 0.072f, 0.07f));
                int cutTris = 0;
                foreach (var r in rends.ToList())
                {
                    if (wheelR.Contains(r)) continue;
                    string n = r.name.ToLowerInvariant();
                    var mf = r.GetComponent<MeshFilter>(); var mesh = mf.sharedMesh;
                    bool glassPart = IsGlass(r), skip = n.Contains("mirror") || n.Contains("lamp") || n.Contains("bulb") || n.Contains("reflector");
                    var planes = glassPart ? new[] { new Vector4(0, 0, 1, -z0), new Vector4(0, 0, -1, z1) }
                                           : new[] { new Vector4(0, 0, 1, -z0), new Vector4(0, 0, -1, z1), new Vector4(0, 1, 0, -y0), new Vector4(0, -1, 0, belt) };
                    var cl = new Clipper(mesh, r.transform);
                    var keep = new List<int>[mesh.subMeshCount]; var cut = new List<int>[mesh.subMeshCount]; int nc = 0;
                    for (int sm = 0; sm < mesh.subMeshCount; sm++)
                    {
                        keep[sm] = new List<int>(); cut[sm] = new List<int>(); var t = mesh.GetTriangles(sm);
                        for (int i = 0; i < t.Length; i += 3)
                        {
                            Vector3 a = cl.W(t[i]), bb = cl.W(t[i + 1]), c = cl.W(t[i + 2]);
                            var fn = Vector3.Cross(bb - a, c - a); float area = fn.magnitude; fn = area > 1e-9f ? fn / area : Vector3.up;
                            var ctr = (a + bb + c) / 3f;
                            // only the car's side skin (or the side window) can be door: not the floor, roof, seats or dashboard
                            bool cand = !skip && Mathf.Max(a.x, Mathf.Max(bb.x, c.x)) > xIn && ctr.x > xIn - 0.05f && Mathf.Abs(fn.x) > (glassPart ? 0.55f : 0.3f);
                            if (!cand) { keep[sm].Add(t[i]); keep[sm].Add(t[i + 1]); keep[sm].Add(t[i + 2]); continue; }
                            int before = cut[sm].Count;
                            cl.Split(new List<int> { t[i], t[i + 1], t[i + 2] }, planes, 0, keep[sm], cut[sm]);
                            if (cut[sm].Count > before) nc++;
                        }
                    }
                    if (nc == 0) { var copy = Copy(mesh, keep); Save(copy, $"{id}_{Clean(r.name)}"); mf.sharedMesh = copy; Rename(r, false); continue; } // a readable copy, so it can dent
                    cutTris += nc;
                    var shell = cl.Build(keep, null, 0f); Save(shell, $"{id}_{Clean(r.name)}_shell"); mf.sharedMesh = shell; Rename(r, false);
                    // the door piece, with a dark inner skin set in a few centimetres so it isn't see-through when it's open
                    var dm = cl.Build(cut, glassPart ? null : r.transform.InverseTransformDirection(Vector3.left).normalized, 0.03f / Mathf.Max(0.01f, r.transform.lossyScale.x));
                    Save(dm, $"{id}_{Clean(r.name)}_door");
                    var doorPart = Object.Instantiate(r.gameObject, r.transform.parent);
                    // just the door piece: the copy's children (lamps, mirrors, all the black plastic trim) stay on the car. (Destroying
                    // them while walking the list skipped every other one, and those swung open with the door.)
                    foreach (var ch in doorPart.transform.Cast<Transform>().ToList()) Object.DestroyImmediate(ch.gameObject);
                    doorPart.GetComponent<MeshFilter>().sharedMesh = dm;
                    if (!glassPart) { var dmats = r.sharedMaterials.ToList(); while (dmats.Count < mesh.subMeshCount) dmats.Add(dmats[dmats.Count - 1]); dmats.Add(cardM); doorPart.GetComponent<MeshRenderer>().sharedMaterials = dmats.ToArray(); }
                    doorPart.name = "door_" + Clean(r.name); doorPart.transform.SetParent(hinge, true);
                }
                // number plates: the pack's say "SEDAN" and "1988"; ours go over them (white front, yellow back, a made-up reg per car)
                int plates = Plates(root.transform, rends, b, id);
                // fewer draws: the shell's parts into one mesh (a submesh per material), the door's into one, each wheel's into one
                int partsBefore = root.GetComponentsInChildren<MeshRenderer>(true).Length;
                Merge(root.transform, root.transform, id + "_body", t => !t.GetComponentsInParent<Transform>(true).Any(q => q.name.StartsWith("wheel_") || q.name.StartsWith("DoorHinge")));
                Merge(hinge, hinge, id + "_door", t => true);
                foreach (var wn in new[] { "wheel_FL", "wheel_FR", "wheel_RL", "wheel_RR" }) { var w = root.transform.Find(wn + "/spin"); if (w != null) Merge(w, w, id + "_" + wn, t => true); }
                Debug.Log($"[ASBO] pack cars: {id} {partsBefore} parts merged to {root.GetComponentsInChildren<MeshRenderer>(true).Length}");
                Debug.Log($"[ASBO] pack cars: {id} door z {z0:F2}..{z1:F2}, sill {y0:F2}, belt {belt:F2}; {plates} plates");
                var prefabPath = $"{Out}/{id}.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Object.DestroyImmediate(root);
                Debug.Log($"[ASBO] pack cars: {id} made from {Path.GetFileName(path)}: scale {s:F2}, {wheelR.Count} wheel pieces, door {doorLen} m cut ({cutTris} triangles), {rends.Count} parts");
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Every mesh under 'from' that 'take' accepts, combined into one mesh under 'into' (one submesh per material, in
        /// the space of 'into'), the originals removed. Saved as an asset so the prefab keeps it (and it stays readable for dents).
        /// </summary>
        static void Merge(Transform from, Transform into, string name, System.Func<Transform, bool> take)
        {
            var rs = from.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.GetComponent<MeshFilter>()?.sharedMesh != null && take(r.transform)).ToList();
            if (rs.Count < 2) return;
            var byMat = new Dictionary<Material, List<CombineInstance>>(); var order = new List<Material>();
            var toLocal = into.worldToLocalMatrix;
            foreach (var r in rs)
            {
                var mesh = r.GetComponent<MeshFilter>().sharedMesh; var mats = r.sharedMaterials;
                for (int sm = 0; sm < mesh.subMeshCount; sm++)
                {
                    var m = sm < mats.Length ? mats[sm] : mats[mats.Length - 1]; if (m == null) continue;
                    if (!byMat.TryGetValue(m, out var l)) { l = new List<CombineInstance>(); byMat[m] = l; order.Add(m); }
                    l.Add(new CombineInstance { mesh = mesh, subMeshIndex = sm, transform = toLocal * r.transform.localToWorldMatrix });
                }
            }
            var parts = new List<CombineInstance>();
            foreach (var m in order)
            {
                var pm = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; pm.CombineMeshes(byMat[m].ToArray(), true, true);
                parts.Add(new CombineInstance { mesh = pm, transform = Matrix4x4.identity });
            }
            var all = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; all.CombineMeshes(parts.ToArray(), false, false);
            all.RecalculateBounds(); Save(all, name);
            var go = new GameObject(name.EndsWith("_body") ? "body" : name.Contains("_door") ? "door_merged" : "tyre"); go.transform.SetParent(into, false);
            go.AddComponent<MeshFilter>().sharedMesh = all; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterials = order.ToArray();
            mr.receiveGI = ReceiveGI.LightProbes; mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
            foreach (var r in rs) { if (r == null) continue; var t = r.transform; Object.DestroyImmediate(r.GetComponent<MeshFilter>()); Object.DestroyImmediate(r); }
            foreach (var p in parts) Object.DestroyImmediate(p.mesh);
        }

        static bool IsGlass(Renderer r) { string n = r.name.ToLowerInvariant(); return (n.Contains("glass") && !n.Contains("lamp")) || n == "windows"; }

        /// <summary>
        /// Splits triangles of one mesh against planes (in the car's space: n.xyz·p + w ≥ 0 is inside), making new
        /// vertices where an edge crosses a plane (position, normal, tangent and UV carried across), so a cut follows
        /// the line exactly rather than taking whole triangles.
        /// </summary>
        class Clipper
        {
            readonly List<Vector3> pos, nor, wpos; readonly List<Vector4> tan; readonly List<Vector2> uv; readonly Transform tr; readonly bool hasTan;
            public Clipper(Mesh m, Transform t)
            {
                tr = t; pos = m.vertices.ToList(); nor = m.normals.ToList(); uv = m.uv.ToList(); var tg = m.tangents; hasTan = tg.Length == pos.Count; tan = hasTan ? tg.ToList() : new List<Vector4>();
                if (nor.Count != pos.Count) { m.RecalculateNormals(); nor = m.normals.ToList(); }
                if (uv.Count != pos.Count) uv = Enumerable.Repeat(Vector2.zero, pos.Count).ToList();
                wpos = pos.Select(p => t.TransformPoint(p)).ToList();
            }
            public Vector3 W(int i) => wpos[i];
            int Mid(int a, int b, float f)
            {
                pos.Add(Vector3.Lerp(pos[a], pos[b], f)); wpos.Add(Vector3.Lerp(wpos[a], wpos[b], f));
                nor.Add(Vector3.Lerp(nor[a], nor[b], f).normalized); uv.Add(Vector2.Lerp(uv[a], uv[b], f));
                if (hasTan) { var t = Vector4.Lerp(tan[a], tan[b], f); var t3 = new Vector3(t.x, t.y, t.z).normalized; tan.Add(new Vector4(t3.x, t3.y, t3.z, tan[a].w)); }
                return pos.Count - 1;
            }
            /// <summary>Whatever of the polygon is outside plane k goes to outList; the inside goes on to the next plane, and what's inside them all to inList.</summary>
            public void Split(List<int> poly, Vector4[] planes, int k, List<int> outList, List<int> inList)
            {
                if (poly.Count < 3) return;
                if (k == planes.Length) { Fan(poly, inList); return; }
                var pl = planes[k]; var inside = new List<int>(); var outside = new List<int>();
                for (int i = 0; i < poly.Count; i++)
                {
                    int a = poly[i], b = poly[(i + 1) % poly.Count];
                    float da = Vector3.Dot(new Vector3(pl.x, pl.y, pl.z), wpos[a]) + pl.w, db = Vector3.Dot(new Vector3(pl.x, pl.y, pl.z), wpos[b]) + pl.w;
                    if (da >= 0f) inside.Add(a); else outside.Add(a);
                    if ((da >= 0f) != (db >= 0f)) { int m = Mid(a, b, da / (da - db)); inside.Add(m); outside.Add(m); }
                }
                if (outside.Count >= 3) Fan(outside, outList);
                Split(inside, planes, k + 1, outList, inList);
            }
            static void Fan(List<int> p, List<int> tris) { for (int i = 1; i + 1 < p.Count; i++) { tris.Add(p[0]); tris.Add(p[i]); tris.Add(p[i + 1]); } }
            /// <summary>A mesh of these triangles (only the vertices they use). With an inward direction, a turned-round copy set in that far is added as one more submesh (the door's inside).</summary>
            public Mesh Build(List<int>[] subs, Vector3? inward, float depth)
            {
                var map = new Dictionary<int, int>(); var P = new List<Vector3>(); var N = new List<Vector3>(); var U = new List<Vector2>(); var T = new List<Vector4>();
                int Use(int i) { if (map.TryGetValue(i, out var j)) return j; j = P.Count; map[i] = j; P.Add(pos[i]); N.Add(nor[i]); U.Add(uv[i]); if (hasTan) T.Add(tan[i]); return j; }
                var outSubs = subs.Select(l => l.Select(Use).ToList()).ToList();
                if (inward.HasValue)
                {
                    var inner = new List<int>(); int n0 = P.Count; var inMap = new Dictionary<int, int>();
                    for (int s = 0; s < outSubs.Count; s++)
                        for (int i = 0; i < outSubs[s].Count; i += 3)
                        {
                            for (int k = 2; k >= 0; k--)
                            {
                                int v = outSubs[s][i + k];
                                if (!inMap.TryGetValue(v, out var w)) { w = P.Count; inMap[v] = w; P.Add(P[v] + inward.Value * depth); N.Add(-N[v]); U.Add(U[v]); if (hasTan) T.Add(-T[v]); }
                                inner.Add(w);
                            }
                        }
                    outSubs.Add(inner);
                }
                var m = new Mesh { name = "cut", indexFormat = P.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
                m.SetVertices(P); m.SetNormals(N); m.SetUVs(0, U); if (hasTan) m.SetTangents(T);
                m.subMeshCount = outSubs.Count; for (int i = 0; i < outSubs.Count; i++) m.SetTriangles(outSubs[i], i);
                m.RecalculateBounds(); return m;
            }
        }

        /// <summary>
        /// Our plates over the pack's: finds the flat, bright, upright patch at the middle of each end (by its colour in
        /// the pack's texture) and puts a plate quad just proud of it, mapped into the plate atlas (front half white, back
        /// half yellow). CarKit gives each car its own registration.
        /// </summary>
        static int Plates(Transform root, List<MeshRenderer> rends, Bounds b, string id)
        {
            var body = rends.Where(r => !IsGlass(r)).ToList(); if (body.Count == 0) return 0;
            string mp = $"{Out}/Mats/{id}_Plates.mat"; var pm = AssetDatabase.LoadAssetAtPath<Material>(mp);
            if (pm == null) { pm = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(pm, mp); }
            pm.SetColor("_BaseColor", Color.white); pm.SetFloat("_Smoothness", 0.4f); pm.SetFloat("_Metallic", 0f); EditorUtility.SetDirty(pm);
            int made = 0;
            foreach (int end in new[] { 1, -1 })
            {
                // upright flat triangles facing out of this end, near the middle: joined up into patches by shared corners,
                // the plate is the patch the size and shape of a plate
                var tris = new List<Vector3[]>();
                foreach (var r in body)
                {
                    var mesh = r.GetComponent<MeshFilter>().sharedMesh; var v = mesh.vertices; var t = mesh.triangles;
                    for (int i = 0; i < t.Length; i += 3)
                    {
                        Vector3 a = r.transform.TransformPoint(v[t[i]]), bb = r.transform.TransformPoint(v[t[i + 1]]), c = r.transform.TransformPoint(v[t[i + 2]]);
                        var n = Vector3.Cross(bb - a, c - a).normalized; var ctr = (a + bb + c) / 3f;
                        if (n.z * end < 0.9f || Mathf.Abs(ctr.x) > 0.4f || ctr.y < 0.12f || ctr.y > 1.25f) continue;
                        if (end > 0 ? ctr.z < b.max.z - 0.45f : ctr.z > b.min.z + 0.45f) continue;
                        tris.Add(new[] { a, bb, c });
                    }
                }
                var parent = Enumerable.Range(0, tris.Count).ToArray();
                int Find(int x) { while (parent[x] != x) x = parent[x] = parent[parent[x]]; return x; }
                var owner = new Dictionary<Vector3Int, int>();
                for (int i = 0; i < tris.Count; i++)
                    foreach (var p in tris[i])
                    {
                        var key = Vector3Int.RoundToInt(p * 500f);
                        if (owner.TryGetValue(key, out var j)) parent[Find(i)] = Find(j); else owner[key] = i;
                    }
                var groups = Enumerable.Range(0, tris.Count).GroupBy(Find).ToList();
                Vector3 mn = Vector3.zero, mx = Vector3.zero; float bestZ = float.MinValue; int hits = 0;
                foreach (var g in groups)
                {
                    Vector3 gmn = Vector3.one * 1e9f, gmx = -Vector3.one * 1e9f;
                    foreach (int i in g) foreach (var p in tris[i]) { gmn = Vector3.Min(gmn, p); gmx = Vector3.Max(gmx, p); }
                    var sz = gmx - gmn; float cx = (gmn.x + gmx.x) * 0.5f;
                    if (sz.x < 0.36f || sz.x > 0.64f || sz.y < 0.07f || sz.y > 0.18f || sz.z > 0.06f || Mathf.Abs(cx) > 0.1f) continue;
                    float out_ = end > 0 ? gmx.z : -gmn.z; hits++;
                    if (out_ > bestZ) { bestZ = out_; mn = gmn; mx = gmx; }
                }
                Vector3 size = mx - mn;
                if (hits == 0) { Debug.LogWarning($"[ASBO] pack cars: {id} {(end > 0 ? "front" : "back")} plate not found ({tris.Count} upright bits in {groups.Count} patches)"); continue; }
                float w = Mathf.Min(size.x, 0.52f), h = w * 0.215f; Vector3 c0 = (mn + mx) * 0.5f; c0.z = end > 0 ? mx.z + 0.006f : mn.z - 0.006f;
                var q = new GameObject(end > 0 ? "plate_F" : "plate_R"); q.transform.SetParent(root, false);
                var m = new Mesh { name = id + (end > 0 ? "_plateF" : "_plateR") };
                float u0 = end > 0 ? 0f : 0.5f, u1 = u0 + 0.5f;
                // seen from the front (+z) the plate's right is -x; from the back it's +x
                float sx = end > 0 ? -1f : 1f;
                m.vertices = new[] { c0 + new Vector3(-w / 2 * sx, -h / 2, 0), c0 + new Vector3(w / 2 * sx, -h / 2, 0), c0 + new Vector3(w / 2 * sx, h / 2, 0), c0 + new Vector3(-w / 2 * sx, h / 2, 0) };
                m.uv = new[] { new Vector2(u0, 0), new Vector2(u1, 0), new Vector2(u1, 1), new Vector2(u0, 1) };
                var nn = new Vector3(0, 0, end); m.normals = new[] { nn, nn, nn, nn };
                m.triangles = end > 0 ? new[] { 0, 2, 1, 0, 3, 2 } : new[] { 0, 2, 1, 0, 3, 2 };
                // make sure the face points out of the car
                var tri = m.triangles; var vv = m.vertices; if (Vector3.Dot(Vector3.Cross(vv[tri[1]] - vv[tri[0]], vv[tri[2]] - vv[tri[0]]), nn) < 0) m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                m.RecalculateBounds(); m.RecalculateTangents(); Save(m, m.name);
                q.AddComponent<MeshFilter>().sharedMesh = m; var mr = q.AddComponent<MeshRenderer>(); mr.sharedMaterial = pm;
                made++;
            }
            return made;
        }

        static string Clean(string n) => new string(n.Where(ch => char.IsLetterOrDigit(ch) || ch == '_').ToArray());
        static void Rename(Renderer r, bool door) { string n = r.name.ToLowerInvariant(); if (!n.StartsWith("body")) r.name = "body_" + Clean(r.name); }

        static Mesh Copy(Mesh src, List<int>[] tris)
        {
            var m = new Mesh { name = src.name, indexFormat = src.vertexCount > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            m.vertices = src.vertices; m.normals = src.normals; m.uv = src.uv; if (src.tangents.Length == src.vertexCount) m.tangents = src.tangents;
            m.subMeshCount = tris.Length; for (int i = 0; i < tris.Length; i++) m.SetTriangles(tris[i], i);
            m.RecalculateBounds(); return m;
        }
        static void Save(Mesh m, string name)
        {
            string p = $"{Out}/Meshes/{name}.asset"; var old = AssetDatabase.LoadAssetAtPath<Mesh>(p); if (old != null) AssetDatabase.DeleteAsset(p);
            AssetDatabase.CreateAsset(m, p);
        }
        static Bounds Bounds(List<MeshRenderer> rs) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b; }
    }
}

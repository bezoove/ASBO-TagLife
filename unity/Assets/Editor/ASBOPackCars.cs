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
                // the driver's door: cut out of the right-hand side between the front wheel and the pillar, up to the roof line
                Transform fw = root.transform.Find("wheel_FR");
                float zF = fw != null ? fw.position.z : spec.wxF, rad = fw != null ? Mathf.Max(0.2f, wheelR.Where(r => r.transform.IsChildOf(fw)).Select(r => r.bounds.extents.y).DefaultIfEmpty(0.3f).Max()) : spec.rad;
                float z1 = zF - rad - 0.14f, z0 = z1 - doorLen, xIn = b.max.x - 0.32f, y0 = 0.22f, y1 = b.max.y - 0.16f;
                var hinge = new GameObject("DoorHinge|" + doorLen.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)).transform;
                hinge.SetParent(root.transform, false); hinge.position = new Vector3(b.max.x - 0.06f, (y0 + y1) * 0.5f, z1);
                int cutTris = 0;
                foreach (var r in rends.ToList())
                {
                    if (wheelR.Contains(r)) continue;
                    string n = r.name.ToLowerInvariant(); if (n.Contains("mirror")) continue;
                    var mf = r.GetComponent<MeshFilter>(); var mesh = mf.sharedMesh;
                    var v = mesh.vertices; var keep = new List<int>[mesh.subMeshCount]; var cut = new List<int>[mesh.subMeshCount]; int nc = 0;
                    for (int sm = 0; sm < mesh.subMeshCount; sm++)
                    {
                        keep[sm] = new List<int>(); cut[sm] = new List<int>(); var t = mesh.GetTriangles(sm);
                        for (int i = 0; i < t.Length; i += 3)
                        {
                            Vector3 c = r.transform.TransformPoint((v[t[i]] + v[t[i + 1]] + v[t[i + 2]]) / 3f);
                            bool inDoor = c.x > xIn && c.z > z0 && c.z < z1 && c.y > y0 && c.y < y1;
                            (inDoor ? cut[sm] : keep[sm]).AddRange(new[] { t[i], t[i + 1], t[i + 2] }); if (inDoor) nc++;
                        }
                    }
                    if (nc == 0) { var copy = Copy(mesh, keep); Save(copy, $"{id}_{Clean(r.name)}"); mf.sharedMesh = copy; Rename(r, false); continue; } // a readable copy, so it can dent
                    cutTris += nc;
                    var shell = Copy(mesh, keep); Save(shell, $"{id}_{Clean(r.name)}_shell"); mf.sharedMesh = shell; Rename(r, false);
                    var doorPart = Object.Instantiate(r.gameObject, r.transform.parent); foreach (Transform ch in doorPart.transform) Object.DestroyImmediate(ch.gameObject);
                    var dm = Copy(mesh, cut); Save(dm, $"{id}_{Clean(r.name)}_door"); doorPart.GetComponent<MeshFilter>().sharedMesh = dm;
                    doorPart.name = "door_" + Clean(r.name); doorPart.transform.SetParent(hinge, true);
                }
                var prefabPath = $"{Out}/{id}.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Object.DestroyImmediate(root);
                Debug.Log($"[ASBO] pack cars: {id} made from {Path.GetFileName(path)}: scale {s:F2}, {wheelR.Count} wheel pieces, door {doorLen} m cut ({cutTris} triangles), {rends.Count} parts");
            }
            AssetDatabase.SaveAssets();
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

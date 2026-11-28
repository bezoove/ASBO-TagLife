using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Props made from the user's own photos. Drop a photo into Assets/Resources/PhotoProps named name_WxHxD (size in cm),
    /// e.g. kettle_22x24x16.jpg. The photo goes on the front and back of a box of that real size, the sides take its average
    /// colour, and it's an ordinary pick-up-and-throw prop (pickable, throwable, noisy). Anything fancier is modelled by hand
    /// from the photo and added to the prop library; this is the quick route for boxes, packets, signs, tins and the like.
    /// </summary>
    public static class PhotoProps
    {
        class P { public string id; public Mesh mesh; public Material mat, sideMat; public Vector3 size; public PropLib.Entry e; }
        static List<P> list; static Transform root;

        static void Init()
        {
            if (list != null) return;
            list = new List<P>();
            foreach (var t in Resources.LoadAll<Texture2D>("PhotoProps"))
            {
                try
                {
                    Vector3 s = Vector3.one * 0.2f; string id = t.name; int u = id.LastIndexOf('_');
                    if (u > 0)
                    {
                        var parts = id.Substring(u + 1).ToLowerInvariant().Split('x');
                        if (parts.Length == 3 && float.TryParse(parts[0], out var w) && float.TryParse(parts[1], out var h) && float.TryParse(parts[2], out var d)) { s = new Vector3(w, h, d) * 0.01f; id = id.Substring(0, u); }
                    }
                    Color avg = Average(t);
                    var m = BoxMesh(s); var mat = Gfx.Mat(Color.white, t, 0.25f);
                    float mass = Mathf.Clamp(s.x * s.y * s.z * 300f, 0.1f, 15f);
                    var e = new PropLib.Entry { id = "photo_" + id, mesh = m, kind = PropLib.Kind.Pickup, mass = mass, label = Nice(id), size = m.bounds.size, center = m.bounds.center, tris = 12 };
                    var sideMat = Gfx.Mat(avg, null, 0.2f);
                    list.Add(new P { id = id, mesh = m, mat = mat, size = s, e = e, sideMat = sideMat });
                }
                catch (System.Exception ex) { Debug.LogWarning("[ASBO] photo prop " + t.name + ": " + ex.Message); }
            }
            Debug.Log("[ASBO] photo props: " + list.Count);
        }
        static string Nice(string id) { id = id.Replace('-', ' ').Replace('_', ' '); return char.ToUpper(id[0]) + id.Substring(1); }
        public static int Count { get { Init(); return list.Count; } }

        static Color Average(Texture2D t)
        {
            try
            {
                var px = t.GetPixels(Mathf.Min(3, t.mipmapCount - 1)); float r = 0, g = 0, b = 0; foreach (var c in px) { r += c.r; g += c.g; b += c.b; }
                float n = Mathf.Max(1, px.Length); return new Color(r / n, g / n, b / n) * 0.85f;
            }
            catch { return new Color(0.5f, 0.5f, 0.5f); }
        }

        /// <summary>A box, pivot at the bottom centre. Submesh 0 = front and back (the photo), submesh 1 = the other four faces.</summary>
        static Mesh BoxMesh(Vector3 s)
        {
            float x = s.x * 0.5f, z = s.z * 0.5f, y = s.y;
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var front = new List<int>(); var side = new List<int>();
            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 nn, bool photo, bool mirror)
            {
                int i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d); for (int k = 0; k < 4; k++) n.Add(nn);
                if (mirror) { uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(0, 1)); uv.Add(new Vector2(1, 1)); }
                else { uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1)); }
                var t = photo ? front : side; t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2);
            }
            Face(new Vector3(-x, 0, -z), new Vector3(x, 0, -z), new Vector3(x, y, -z), new Vector3(-x, y, -z), -Vector3.forward, true, false);
            Face(new Vector3(x, 0, z), new Vector3(-x, 0, z), new Vector3(-x, y, z), new Vector3(x, y, z), Vector3.forward, true, false);
            Face(new Vector3(x, 0, -z), new Vector3(x, 0, z), new Vector3(x, y, z), new Vector3(x, y, -z), Vector3.right, false, false);
            Face(new Vector3(-x, 0, z), new Vector3(-x, 0, -z), new Vector3(-x, y, -z), new Vector3(-x, y, z), -Vector3.right, false, false);
            Face(new Vector3(-x, y, -z), new Vector3(x, y, -z), new Vector3(x, y, z), new Vector3(-x, y, z), Vector3.up, false, false);
            Face(new Vector3(-x, 0, z), new Vector3(x, 0, z), new Vector3(x, 0, -z), new Vector3(-x, 0, -z), -Vector3.up, false, false);
            var m = new Mesh { name = "photoprop" }; m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv);
            m.subMeshCount = 2; m.SetTriangles(front, 0); m.SetTriangles(side, 1); m.RecalculateBounds(); m.UploadMeshData(true);
            return m;
        }

        public static GameObject Spawn(string id, Vector3 pos, Quaternion rot)
        {
            Init(); P k = null; foreach (var x in list) if (x.id == id) k = x; if (k == null) return null;
            if (root == null) root = new GameObject("PhotoProps").transform;
            var g = new GameObject("photo_" + k.id); g.transform.SetParent(root, true); g.transform.SetPositionAndRotation(pos, rot);
            g.AddComponent<MeshFilter>().sharedMesh = k.mesh;
            var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterials = new[] { k.mat, k.sideMat };
            var bc = g.AddComponent<BoxCollider>(); bc.center = k.e.center; bc.size = k.e.size;
            var rb = g.AddComponent<Rigidbody>(); rb.mass = k.e.mass; rb.interpolation = RigidbodyInterpolation.Interpolate; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var kn = g.AddComponent<Knockable>(); kn.loudBase = 12f;
            var t = g.AddComponent<Throwable>(); t.entry = k.e; t.rb = rb;
            return g;
        }

        /// <summary>A few of each lying about near where people walk (only if any photos have been added).</summary>
        public static void Scatter(int perKind)
        {
            Init(); if (list.Count == 0) return;
            foreach (var k in list)
                for (int tries = 0, made = 0; made < perKind && tries < perKind * 8 && Person.All.Count > 0; tries++)
                {
                    var p = Person.All[Random.Range(0, Person.All.Count)]; if (p == null || p.isPlayer) continue;
                    Vector2 o = Random.insideUnitCircle.normalized * Random.Range(2f, 8f);
                    Vector3 from = p.transform.position + new Vector3(o.x, 2.5f, o.y);
                    if (!Physics.Raycast(from, Vector3.down, out var hit, 4f, 1 << 0, QueryTriggerInteraction.Ignore) || hit.normal.y < 0.92f) continue;
                    Spawn(k.id, hit.point + Vector3.up * 0.02f, Quaternion.Euler(0f, Random.value * 360f, 0f)); made++;
                }
        }
    }
}

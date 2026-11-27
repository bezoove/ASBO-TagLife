using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Things lying about the estate that are made for throwing: bricks, stones, lager cans, bottles. Pick one up (TALK),
    /// punch to throw it. Each one is a tiny shared mesh and a shared material, so hundreds cost next to nothing.
    /// Bottles shatter on a hard hit. Where they land, they make a noise (see NoiseBus).
    /// </summary>
    public class Throwable : MonoBehaviour
    {
        public static readonly List<Throwable> All = new List<Throwable>();
        public PropLib.Entry entry; public Rigidbody rb;
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        /// <summary>Lying on the floor and free to pick up (not in a hand).</summary>
        public bool Free => rb != null && !rb.isKinematic;
    }

    public static class Throwables
    {
        class Kind { public string id, label; public PropLib.Entry e; public Material mat; public float loud, weight; public bool glass; }
        static Kind[] kinds; static Transform root;

        static void Init()
        {
            if (kinds != null) return;
            Mesh cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx"), cyl = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx"), sph = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            var brick = Bake(new[] { (cube, new Vector3(0.215f, 0.065f, 0.1025f), Vector3.zero) }, 0.01f, 1);
            var can = Bake(new[] { (cyl, new Vector3(0.066f, 0.0575f, 0.066f), Vector3.zero) }, 0.003f, 2);
            var stone = Bake(new[] { (sph, new Vector3(0.11f, 0.08f, 0.1f), Vector3.zero) }, 0.014f, 3);
            var bottle = Bake(new[] { (cyl, new Vector3(0.07f, 0.09f, 0.07f), new Vector3(0, 0.09f, 0)), (cyl, new Vector3(0.026f, 0.05f, 0.026f), new Vector3(0, 0.23f, 0)) }, 0f, 4);
            kinds = new[]
            {
                Make("brick", "Brick", brick, 0.5f, 0.32f, new Color(0.55f, 0.24f, 0.18f), 0.05f, false, 16f, 20),
                Make("stone", "Stone", stone, 0.45f, 0.18f, new Color(0.46f, 0.45f, 0.43f), 0.08f, false, 14f, 30),
                Make("can", "Lager can", can, 0.04f, 0.045f, new Color(0.62f, 0.66f, 0.7f), 0.6f, false, 11f, 30),
                Make("bottle", "Bottle", bottle, 0.35f, 0.3f, new Color(0.1f, 0.34f, 0.16f), 0.85f, true, 26f, 20),
            };
        }

        static Kind Make(string id, string label, Mesh m, float mass, float tall, Color tint, float smooth, bool glass, float loud, float weight)
        {
            var b = m.bounds;
            var k = new Kind
            {
                id = id, label = label, loud = loud, weight = weight, glass = glass,
                mat = Gfx.Mat(tint, null, smooth, id == "can" ? 0.8f : 0f),
                e = new PropLib.Entry { id = id, mesh = m, kind = PropLib.Kind.Pickup, mass = mass, label = label, size = b.size, center = b.center, tris = m.triangles.Length / 3 }
            };
            return k;
        }

        /// <summary>Combine primitive meshes, scaled and shifted, with a little lumpiness so no two stones look the same.</summary>
        static Mesh Bake((Mesh m, Vector3 scale, Vector3 at)[] parts, float jitter, int seed)
        {
            var ci = new CombineInstance[parts.Length];
            for (int i = 0; i < parts.Length; i++) { ci[i].mesh = parts[i].m; ci[i].transform = Matrix4x4.TRS(parts[i].at, Quaternion.identity, parts[i].scale * 2f); }
            var mesh = new Mesh(); mesh.CombineMeshes(ci, true, true); mesh.name = "throwable" + seed;
            if (jitter > 0f)
            {
                var v = mesh.vertices; var rnd = new System.Random(seed);
                for (int i = 0; i < v.Length; i++) v[i] += new Vector3((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f) * jitter;
                mesh.vertices = v; mesh.RecalculateNormals();
            }
            mesh.RecalculateBounds(); mesh.UploadMeshData(false);
            return mesh;
        }

        public static GameObject Spawn(string id, Vector3 pos, Quaternion rot)
        {
            Init(); Kind k = null; foreach (var x in kinds) if (x.id == id) k = x; if (k == null) return null;
            if (root == null) root = new GameObject("Litter").transform;
            var g = new GameObject(k.id); g.transform.SetParent(root, true); g.transform.SetPositionAndRotation(pos, rot);
            g.AddComponent<MeshFilter>().sharedMesh = k.e.mesh;
            var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = k.mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var bc = g.AddComponent<BoxCollider>(); bc.center = k.e.center; bc.size = Vector3.Max(k.e.size * 0.9f, Vector3.one * 0.03f);
            var rb = g.AddComponent<Rigidbody>(); rb.mass = k.e.mass; rb.interpolation = RigidbodyInterpolation.Interpolate; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var kn = g.AddComponent<Knockable>(); kn.glass = k.glass; kn.loudBase = k.loud;
            var t = g.AddComponent<Throwable>(); t.entry = k.e; t.rb = rb;
            return g;
        }

        /// <summary>Scatter litter on pavements and verges: near where people walk, so it's always within reach.</summary>
        public static void Scatter(int count)
        {
            Init(); float total = 0f; foreach (var k in kinds) total += k.weight;
            for (int tries = 0, made = 0; made < count && tries < count * 6 && Person.All.Count > 0; tries++)
            {
                var p = Person.All[Random.Range(0, Person.All.Count)]; if (p == null || p.isPlayer) continue;
                Vector2 o = Random.insideUnitCircle.normalized * Random.Range(2f, 7f);
                Vector3 from = p.transform.position + new Vector3(o.x, 2.5f, o.y);
                if (!Physics.Raycast(from, Vector3.down, out var hit, 4f, 1 << 0, QueryTriggerInteraction.Ignore) || hit.normal.y < 0.92f) continue;
                float r = Random.value * total; Kind pick = kinds[0]; foreach (var k in kinds) { r -= k.weight; if (r <= 0f) { pick = k; break; } }
                Spawn(pick.id, hit.point + Vector3.up * 0.06f, Quaternion.Euler(0f, Random.value * 360f, 0f)); made++;
            }
        }
    }
}

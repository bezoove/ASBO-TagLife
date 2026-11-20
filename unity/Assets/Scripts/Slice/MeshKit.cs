using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>
    /// Builds one mesh with a sub-mesh per material from boxes, quads, cylinders and spheres.
    /// Everything for a building goes into a few of these so a whole cottage costs a handful of draw calls.
    /// </summary>
    public class MeshKit
    {
        public readonly List<Vector3> V = new List<Vector3>();
        public readonly List<Vector3> N = new List<Vector3>();
        public readonly List<Vector2> U = new List<Vector2>();
        readonly Dictionary<Material, List<int>> subs = new Dictionary<Material, List<int>>();
        readonly List<Material> order = new List<Material>();
        readonly Stack<Matrix4x4> stack = new Stack<Matrix4x4>();
        public Matrix4x4 M = Matrix4x4.identity;
        /// <summary>Texture repeat size in metres per material (default 1 m).</summary>
        public static readonly Dictionary<Material, float> Tile = new Dictionary<Material, float>();
        public int Count => V.Count;

        public void Push(Vector3 pos, float yaw = 0f, float scale = 1f) { stack.Push(M); M = M * Matrix4x4.TRS(pos, Quaternion.Euler(0, yaw, 0), Vector3.one * scale); }
        public void Push(Matrix4x4 m) { stack.Push(M); M = M * m; }
        public void Pop() { M = stack.Pop(); }

        List<int> T(Material m) { if (!subs.TryGetValue(m, out var l)) { l = new List<int>(); subs[m] = l; order.Add(m); } return l; }
        public static float TileOf(Material m) => m != null && Tile.TryGetValue(m, out var t) ? t : 1f;

        public int Vert(Vector3 p, Vector3 n, Vector2 uv) { V.Add(M.MultiplyPoint3x4(p)); N.Add(M.MultiplyVector(n).normalized); U.Add(uv); return V.Count - 1; }
        public void Tri(Material m, int a, int b, int c) { var t = T(m); t.Add(a); t.Add(b); t.Add(c); }

        Vector2 Planar(Vector3 p, Vector3 n, float s)
        {
            float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
            if (ay >= ax && ay >= az) return new Vector2(p.x, p.z) * s;
            if (ax >= az) return new Vector2(p.z, p.y) * s;
            return new Vector2(p.x, p.y) * s;
        }

        /// <summary>Quad a-b-c-d (any winding, flipped to face n). Planar UVs from the material's tile size.</summary>
        public void Quad(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n)
        {
            float s = 1f / TileOf(m);
            QuadUV(m, a, b, c, d, n, Planar(a, n, s), Planar(b, n, s), Planar(c, n, s), Planar(d, n, s));
        }

        public void QuadUV(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            n = n.normalized;
            int i0 = Vert(a, n, ua), i1 = Vert(b, n, ub), i2 = Vert(c, n, uc), i3 = Vert(d, n, ud);
            var t = T(m);
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), n) >= 0f) { t.Add(i0); t.Add(i1); t.Add(i2); t.Add(i0); t.Add(i2); t.Add(i3); }
            else { t.Add(i0); t.Add(i2); t.Add(i1); t.Add(i0); t.Add(i3); t.Add(i2); }
        }

        public void TriN(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 n)
        {
            float s = 1f / TileOf(m);
            int i0 = Vert(a, n, Planar(a, n, s)), i1 = Vert(b, n, Planar(b, n, s)), i2 = Vert(c, n, Planar(c, n, s));
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), n) >= 0f) Tri(m, i0, i1, i2); else Tri(m, i0, i2, i1);
        }

        /// <summary>Axis-aligned box from mn to mx. skip bitmask: 1=-x 2=+x 4=-y 8=+y 16=-z 32=+z.</summary>
        public void Box(Material m, Vector3 mn, Vector3 mx, int skip = 0)
        {
            Vector3 a = mn, b = mx;
            if ((skip & 1) == 0) Quad(m, new Vector3(a.x, a.y, a.z), new Vector3(a.x, b.y, a.z), new Vector3(a.x, b.y, b.z), new Vector3(a.x, a.y, b.z), Vector3.left);
            if ((skip & 2) == 0) Quad(m, new Vector3(b.x, a.y, a.z), new Vector3(b.x, b.y, a.z), new Vector3(b.x, b.y, b.z), new Vector3(b.x, a.y, b.z), Vector3.right);
            if ((skip & 4) == 0) Quad(m, new Vector3(a.x, a.y, a.z), new Vector3(b.x, a.y, a.z), new Vector3(b.x, a.y, b.z), new Vector3(a.x, a.y, b.z), Vector3.down);
            if ((skip & 8) == 0) Quad(m, new Vector3(a.x, b.y, a.z), new Vector3(b.x, b.y, a.z), new Vector3(b.x, b.y, b.z), new Vector3(a.x, b.y, b.z), Vector3.up);
            if ((skip & 16) == 0) Quad(m, new Vector3(a.x, a.y, a.z), new Vector3(b.x, a.y, a.z), new Vector3(b.x, b.y, a.z), new Vector3(a.x, b.y, a.z), Vector3.back);
            if ((skip & 32) == 0) Quad(m, new Vector3(a.x, a.y, b.z), new Vector3(b.x, a.y, b.z), new Vector3(b.x, b.y, b.z), new Vector3(a.x, b.y, b.z), Vector3.forward);
        }

        /// <summary>Box by centre (x,z) with base at y0: size.x wide, size.y tall, size.z deep.</summary>
        public void BoxB(Material m, Vector3 baseCenter, Vector3 size, int skip = 0)
        {
            Box(m, new Vector3(baseCenter.x - size.x / 2, baseCenter.y, baseCenter.z - size.z / 2), new Vector3(baseCenter.x + size.x / 2, baseCenter.y + size.y, baseCenter.z + size.z / 2), skip);
        }

        /// <summary>Vertical cylinder/cone frustum standing on b. Smooth sides.</summary>
        public void Cyl(Material m, Vector3 b, float r, float h, int seg = 10, bool caps = true, float rTop = -1f)
        {
            if (rTop < 0f) rTop = r;
            float s = 1f / TileOf(m), slope = (r - rTop) / Mathf.Max(h, 0.001f);
            int[] lo = new int[seg + 1], hi = new int[seg + 1];
            for (int k = 0; k <= seg; k++)
            {
                float a = k * Mathf.PI * 2f / seg, c = Mathf.Cos(a), sn = Mathf.Sin(a);
                var n = new Vector3(c, slope, sn);
                lo[k] = Vert(b + new Vector3(c * r, 0, sn * r), n, new Vector2(a * r * s, 0));
                hi[k] = Vert(b + new Vector3(c * rTop, h, sn * rTop), n, new Vector2(a * r * s, h * s));
            }
            for (int k = 0; k < seg; k++) { Tri(m, lo[k], hi[k], hi[k + 1]); Tri(m, lo[k], hi[k + 1], lo[k + 1]); }
            if (!caps) return;
            int ct = Vert(b + Vector3.up * h, Vector3.up, Vector2.zero), cb = Vert(b, Vector3.down, Vector2.zero);
            int[] t2 = new int[seg + 1], b2 = new int[seg + 1];
            for (int k = 0; k <= seg; k++)
            {
                float a = k * Mathf.PI * 2f / seg, c = Mathf.Cos(a), sn = Mathf.Sin(a);
                t2[k] = Vert(b + new Vector3(c * rTop, h, sn * rTop), Vector3.up, new Vector2(c * rTop, sn * rTop) * s);
                b2[k] = Vert(b + new Vector3(c * r, 0, sn * r), Vector3.down, new Vector2(c * r, sn * r) * s);
            }
            for (int k = 0; k < seg; k++) { Tri(m, ct, t2[k + 1], t2[k]); Tri(m, cb, b2[k], b2[k + 1]); }
        }

        /// <summary>Cylinder lying along X (for beams, rolled eaves, logs).</summary>
        public void CylX(Material m, Vector3 c, float r, float len, int seg = 8)
        {
            Push(Matrix4x4.TRS(c + new Vector3(-len / 2, 0, 0), Quaternion.Euler(0, 0, -90f), Vector3.one));
            Cyl(m, Vector3.zero, r, len, seg, true);
            Pop();
        }

        /// <summary>Ellipsoid with radii rad, smooth normals.</summary>
        public void Sphere(Material m, Vector3 c, Vector3 rad, int seg = 10, int rings = 6, bool hemi = false)
        {
            float s = 1f / TileOf(m);
            int r0 = hemi ? rings / 2 : 0;
            var idx = new int[rings + 1, seg + 1];
            for (int i = r0; i <= rings; i++)
            {
                float v = i / (float)rings, phi = Mathf.PI * (1f - v); // from top (i=rings) to bottom (i=0)
                for (int k = 0; k <= seg; k++)
                {
                    float th = k * Mathf.PI * 2f / seg;
                    var n = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                    var p = c + Vector3.Scale(n, rad);
                    idx[i, k] = Vert(p, new Vector3(n.x / rad.x, n.y / rad.y, n.z / rad.z), new Vector2(th / (2 * Mathf.PI), v) * (rad.x * 6.28f * s));
                }
            }
            for (int i = r0; i < rings; i++)
                for (int k = 0; k < seg; k++) { Tri(m, idx[i, k], idx[i + 1, k], idx[i + 1, k + 1]); Tri(m, idx[i, k], idx[i + 1, k + 1], idx[i, k + 1]); }
        }

        public GameObject Build(string name, Transform parent, bool collider, ShadowCastingMode shadows = ShadowCastingMode.On, bool recalcNormals = false)
        {
            if (V.Count == 0) return null;
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name };
            if (V.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(V); mesh.SetUVs(0, U);
            if (!recalcNormals) mesh.SetNormals(N);
            mesh.subMeshCount = order.Count;
            for (int i = 0; i < order.Count; i++) mesh.SetTriangles(subs[order[i]], i, false);
            if (recalcNormals) mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>();
            r.sharedMaterials = order.ToArray();
            r.shadowCastingMode = shadows;
            if (collider) g.AddComponent<MeshCollider>().sharedMesh = mesh;
            return g;
        }
    }
}

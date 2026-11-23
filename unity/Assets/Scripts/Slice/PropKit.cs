using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>
    /// Lays out library props for an interior and bakes them into a few big meshes (one per zone, two materials), so
    /// hundreds of glasses, bottles, chairs and machines cost a handful of draws on a phone. Every prop is remembered
    /// (where it is, which vertices are its own) in a PropSet, so at play it can still be picked up, knocked flying,
    /// sat on or used: the baked copy is hidden and a real one takes its place. Doors are built as their own objects.
    /// Used by the editor when the town is built.
    /// </summary>
    public class PropKit
    {
        class Chunk { public readonly List<Vector3> v = new List<Vector3>(); public readonly List<Vector3> n = new List<Vector3>(); public readonly List<Vector4> t = new List<Vector4>(); public readonly List<Vector2> u = new List<Vector2>(); public readonly List<int> t0 = new List<int>(), t1 = new List<int>(); }
        class Src { public Vector3[] v, n; public Vector4[] t; public Vector2[] u; public int[] s0, s1; }
        readonly Dictionary<int, Chunk> chunks = new Dictionary<int, Chunk>();
        readonly List<PropSet.Rec> recs = new List<PropSet.Rec>();
        readonly List<(Vector3 c, Vector3 s, Quaternion r)> solids = new List<(Vector3, Vector3, Quaternion)>();
        readonly List<(string id, Vector3 p, float yaw, string tag)> doors = new List<(string, Vector3, float, string)>();
        static readonly Dictionary<Mesh, Src> cache = new Dictionary<Mesh, Src>();
        public int Count => recs.Count;
        public int Missing;

        static Src Of(Mesh m)
        {
            if (cache.TryGetValue(m, out var s) && s != null) return s;
            s = new Src { v = m.vertices, n = m.normals, t = m.tangents, u = m.uv, s0 = m.subMeshCount > 0 ? m.GetTriangles(0) : new int[0], s1 = m.subMeshCount > 1 ? m.GetTriangles(1) : new int[0] };
            if (s.t == null || s.t.Length != s.v.Length) s.t = new Vector4[s.v.Length];
            if (s.n == null || s.n.Length != s.v.Length) s.n = new Vector3[s.v.Length];
            cache[m] = s; return s;
        }

        /// <summary>
        /// Puts prop <paramref name="id"/> at <paramref name="pos"/> (local to the interior, its base), turned
        /// <paramref name="yaw"/> degrees (0 = its front facing +Z), into zone chunk <paramref name="zone"/>.
        /// solid: give it a box collider (big furniture). Returns the record index, or -1 if the prop isn't in the library.
        /// </summary>
        public int Put(string id, Vector3 pos, float yaw, int zone, bool solid = false, string tag = null, float scale = 1f, float tilt = 0f)
        {
            var e = PropLib.Get(id); if (e == null || e.mesh == null) { Missing++; return -1; }
            if (e.kind == PropLib.Kind.Door) { doors.Add((id, pos, yaw, tag)); return -1; }
            var rot = Quaternion.Euler(tilt, yaw, 0f); var src = Of(e.mesh); zone = Cell(pos);
            if (!chunks.TryGetValue(zone, out var c)) chunks[zone] = c = new Chunk();
            int v0 = c.v.Count;
            for (int i = 0; i < src.v.Length; i++)
            {
                c.v.Add(pos + rot * (src.v[i] * scale)); c.n.Add(rot * src.n[i]);
                var tt = src.t[i]; Vector3 td = rot * new Vector3(tt.x, tt.y, tt.z); c.t.Add(new Vector4(td.x, td.y, td.z, tt.w == 0 ? 1 : tt.w)); c.u.Add(src.u[i]);
            }
            foreach (var k in src.s0) c.t0.Add(v0 + k);
            foreach (var k in src.s1) c.t1.Add(v0 + k);
            if (solid) solids.Add((pos + rot * (e.center * scale), e.size * scale, rot));
            recs.Add(new PropSet.Rec { id = id, pos = pos, yaw = yaw, chunk = zone, v0 = v0, vn = src.v.Length, tag = tag ?? "" });
            return recs.Count - 1;
        }

        /// <summary>A flat picture from the atlas (a sign, a poster, a screen) centred at c, facing <paramref name="facing"/>.</summary>
        public void Picture(string region, Vector3 c, Vector3 facing, float w, float h, int zone)
        {
            if (!PropLib.Regions.TryGetValue(region, out var r)) { Missing++; return; }
            zone = Cell(c);
            if (!chunks.TryGetValue(zone, out var k)) chunks[zone] = k = new Chunk();
            facing.y = 0f; facing.Normalize(); Vector3 right = Vector3.Cross(Vector3.up, facing).normalized * (w * 0.5f), up = Vector3.up * (h * 0.5f); c += facing * 0.012f;
            int v0 = k.v.Count;
            // seen from the front (looking along -facing), +right runs to the viewer's left, so the left edge is c + right
            k.v.Add(c + right - up); k.v.Add(c - right - up); k.v.Add(c - right + up); k.v.Add(c + right + up);
            k.u.Add(new Vector2(r.xMin, r.yMin)); k.u.Add(new Vector2(r.xMax, r.yMin)); k.u.Add(new Vector2(r.xMax, r.yMax)); k.u.Add(new Vector2(r.xMin, r.yMax));
            Vector3 td = -right.normalized; for (int i = 0; i < 4; i++) { k.n.Add(facing); k.t.Add(new Vector4(td.x, td.y, td.z, 1f)); }
            k.t0.Add(v0); k.t0.Add(v0 + 2); k.t0.Add(v0 + 1); k.t0.Add(v0); k.t0.Add(v0 + 3); k.t0.Add(v0 + 2);
        }

        /// <summary>Meshes are cut into 8 m squares, so each is lit by its own nearest lamps (phones light a mesh with only a couple).</summary>
        public static int Cell(Vector3 p) => Mathf.FloorToInt(p.x / 8f) * 64 + Mathf.FloorToInt(p.z / 8f) + 2080;

        public void Solid(Vector3 mn, Vector3 mx) => solids.Add(((mn + mx) * 0.5f, mx - mn, Quaternion.identity));

        /// <summary>Bakes the chunks into meshes under parent (layer: the culled-at-distance detail layer), the doors, colliders and the PropSet.</summary>
        public PropSet Build(Transform parent, string name, string meshFolder = null)
        {
            var lib = PropLib.I;
            var root = new GameObject(name).transform; root.SetParent(parent, false);
            var set = root.gameObject.AddComponent<PropSet>();
            var mfs = new List<MeshFilter>(); var keyToIndex = new Dictionary<int, int>();
            foreach (var kv in chunks)
            {
                var c = kv.Value; if (c.v.Count == 0) continue;
                var m = new Mesh { name = $"{name}_{kv.Key}" };
                if (c.v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
                m.SetVertices(c.v); m.SetNormals(c.n); m.SetTangents(c.t); m.SetUVs(0, c.u);
                m.subMeshCount = 2; m.SetTriangles(c.t0, 0); m.SetTriangles(c.t1, 1); m.RecalculateBounds();
                var g = new GameObject(m.name); g.transform.SetParent(root, false); g.layer = Bld.DetailLayer;
                var mf = g.AddComponent<MeshFilter>(); mf.sharedMesh = m;
                var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterials = new[] { lib.opaque, lib.glass };
                mr.shadowCastingMode = ShadowCastingMode.Off; mr.lightProbeUsage = LightProbeUsage.Off; mr.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
                keyToIndex[kv.Key] = mfs.Count; mfs.Add(mf);
            }
            var rs = recs.ToArray(); for (int i = 0; i < rs.Length; i++) rs[i].chunk = keyToIndex.TryGetValue(rs[i].chunk, out var ci) ? ci : -1;
            set.chunks = mfs.ToArray(); set.recs = rs;
            var col = new GameObject("Colliders"); col.transform.SetParent(root, false);
            foreach (var (c, s, r) in solids)
            {
                var g = new GameObject("c"); g.transform.SetParent(col.transform, false); g.transform.localPosition = c; g.transform.localRotation = r;
                var bc = g.AddComponent<BoxCollider>(); bc.size = new Vector3(Mathf.Max(0.05f, s.x), Mathf.Max(0.05f, s.y), Mathf.Max(0.05f, s.z));
            }
            foreach (var (id, p, yaw, tag) in doors) PropDoor.Make(root, id, p, yaw, tag);
            Debug.Log($"[ASBO] props: {name}: {rs.Length} props in {mfs.Count} meshes, {solids.Count} colliders, {doors.Count} doors" + (Missing > 0 ? $", {Missing} missing" : ""));
            return set;
        }
    }
}

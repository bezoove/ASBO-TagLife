using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>Where every baked prop in an interior is, so it can be found, hidden and swapped for a real one at play.</summary>
    public class PropSet : MonoBehaviour
    {
        [System.Serializable] public struct Rec { public string id; public Vector3 pos; public float yaw; public int chunk, v0, vn; public string tag; }
        public MeshFilter[] chunks; public Rec[] recs;
        [System.NonSerialized] public bool[] gone;
        [System.NonSerialized] Vector3[][] verts;
        public static readonly List<PropSet> All = new List<PropSet>();
        [System.NonSerialized] BoxCollider[] solid; [System.NonSerialized] bool built;
        void OnEnable() { All.Add(this); gone ??= new bool[recs?.Length ?? 0]; if (!built) BuildSolids(); }

        /// <summary>The baked props were only pictures: nobody could bump into them. Each solid one (machines, tables, chairs, counters, cabinets,
        /// beds, big plants) gets a box its own size, so people, the player and cars stop at it. Small things you can pick up, and
        /// things hanging up out of reach, stay without.</summary>
        void BuildSolids()
        {
            built = true; if (recs == null || PropLib.I == null) return;
            var holder = new GameObject("PropSolids").transform; holder.SetParent(transform, false);
            solid = new BoxCollider[recs.Length];
            for (int i = 0; i < recs.Length; i++)
            {
                var e = PropLib.Get(recs[i].id); if (e == null || e.kind == PropLib.Kind.Pickup || e.kind == PropLib.Kind.Door) continue;
                Vector3 sz = e.size; if (sz.y < 0.4f || Mathf.Max(sz.x, sz.z) < 0.15f) continue;           // trinkets, ornaments, flat things
                if (recs[i].pos.y + e.center.y - sz.y * 0.5f > 1.6f) continue;                             // hung up high: lights, signs
                var g = new GameObject("s"); g.transform.SetParent(holder, false);
                g.transform.localPosition = recs[i].pos; g.transform.localRotation = Quaternion.Euler(0f, recs[i].yaw, 0f);
                var bc = g.AddComponent<BoxCollider>(); bc.center = e.center; bc.size = new Vector3(Mathf.Max(0.1f, sz.x * 0.92f), sz.y, Mathf.Max(0.1f, sz.z * 0.92f));
                solid[i] = bc;
            }
        }
        void OnDisable() { All.Remove(this); }

        public Vector3 World(int i) => transform.TransformPoint(recs[i].pos);
        public Quaternion Rot(int i) => transform.rotation * Quaternion.Euler(0f, recs[i].yaw, 0f);

        /// <summary>Takes prop i out of its baked mesh (squashes its vertices to a point).</summary>
        public void Hide(int i)
        {
            if (gone[i]) return; gone[i] = true;
            if (solid != null && i < solid.Length && solid[i] != null) solid[i].enabled = false;
            var r = recs[i]; if (r.chunk < 0 || r.chunk >= chunks.Length) return;
            verts ??= new Vector3[chunks.Length][];
            var mf = chunks[r.chunk];
            if (verts[r.chunk] == null) { var m = mf.mesh; verts[r.chunk] = m.vertices; }   // .mesh: our own copy, the asset stays whole
            var v = verts[r.chunk]; var p = v[r.v0];
            for (int k = r.v0; k < r.v0 + r.vn && k < v.Length; k++) v[k] = p;
            mf.mesh.SetVertices(v);
        }
    }
}

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
        void OnEnable() { All.Add(this); gone ??= new bool[recs?.Length ?? 0]; }
        void OnDisable() { All.Remove(this); }

        public Vector3 World(int i) => transform.TransformPoint(recs[i].pos);
        public Quaternion Rot(int i) => transform.rotation * Quaternion.Euler(0f, recs[i].yaw, 0f);

        /// <summary>Takes prop i out of its baked mesh (squashes its vertices to a point).</summary>
        public void Hide(int i)
        {
            if (gone[i]) return; gone[i] = true;
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

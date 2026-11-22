using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Crash dents: the bodywork pushes in where it's hit, harder hits deeper (Realistic Mesh Deformation). Only the
    /// shell and the door dent, never the wheels or the glass-free bits under the car. The bigger the car, the less a
    /// knock shows.
    /// </summary>
    public static class Dents
    {
        /// <summary>Make the car under host dent on impact. body is our model root ("Body"); host is whatever gets the collisions.</summary>
        public static void Add(GameObject host, Transform body, float mass = 1100f)
        {
            if (host == null || body == null || host.GetComponent<RMD_Deformation>() != null) return;
            var mfs = new List<MeshFilter>();
            foreach (var mf in body.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                string n = mf.name.ToLowerInvariant();
                bool shell = n.StartsWith("car_lod") || n.StartsWith("dent_lod") || n.StartsWith("door") || n.StartsWith("dentdoor_lod") || n.StartsWith("body") || n.StartsWith("shell");
                if (!shell || !mf.sharedMesh.isReadable) continue;
                mfs.Add(mf);
            }
            if (mfs.Count == 0) return;
            var d = host.AddComponent<RMD_Deformation>();
            d.meshFilters = mfs.ToArray(); // just the shell (it would otherwise gather the wheels too); its vertex copies are made on the first hit
            d.deformationMode = RMD_Deformation.DeformationMode.Fast;
            d.deformResolution = 100;
            d.deformRadius = 0.55f;
            d.deformMultiplier = 1.25f * Mathf.Clamp(1100f / Mathf.Max(600f, mass), 0.6f, 1.4f);
            d.maximumDeformation = 0.22f;
            d.minimumDeformationImpulse = 0.18f;
            d.randomizeVertices = 0.6f;
            d.deformFilter = ~((1 << Person.RagdollLayer) | (1 << 2)); // bodies and triggers don't dent cars
            d.recalculateNormals = true; d.recalculateBounds = true;
        }

        /// <summary>Knock all the dents out (a respray at the garage, say).</summary>
        public static void Repair(GameObject host)
        {
            var d = host != null ? host.GetComponent<RMD_Deformation>() : null;
            if (d != null) d.Repair();
        }
    }
}

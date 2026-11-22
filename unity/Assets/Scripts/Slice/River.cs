using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>
    /// The river through the north of town (TownPlan.River), flowing east. The editor carves its channel and banks into
    /// the terrain; this builds the rest: the water (the ASBO/Water shader: rippling, flowing, reflecting the sky with a
    /// sun glint, darker in the deep middle, paler and foamy at the edges), stone embankment walls where it passes the
    /// bridges, reed beds and the odd shopping trolley, a gravel towpath along the south bank with benches and a
    /// litter bin, and a steel footbridge from the hospital side over to the fields. At Play, <see cref="RiverWater"/>
    /// fishes you out if you wander in.
    /// </summary>
    public static class River
    {
        public const string RootName = "River";
        static CouncilEstate.Mats E;

        public static Transform Build(Transform parent)
        {
            if (!TownMats.Load()) return null;
            E = CouncilEstate.M;
            var root = new GameObject(RootName).transform; root.SetParent(parent, false);
            Water(root);
            Banks(root);
            Footbridge(root, 30f);
            root.gameObject.AddComponent<RiverWater>();
            Debug.Log($"[ASBO] river: {TownPlan.RiverLength:F0} m");
            return root;
        }

        /// <summary>
        /// The water surface in chunks of ~110 m (so the out-of-sight ones aren't drawn). UV0 = metres along and across
        /// (for the ripples to flow downstream), UV1 = how far to the edge (0 middle, 1 bank) and the half width.
        /// </summary>
        static void Water(Transform root)
        {
            var r = TownPlan.River; int start = 0; float chunkLen = 0f; float sAcc = 0f;
            var sList = new List<float> { 0f }; for (int i = 1; i < r.Count; i++) sList.Add(sList[i - 1] + (r[i] - r[i - 1]).magnitude);
            for (int i = 1; i < r.Count; i++)
            {
                chunkLen += (r[i] - r[i - 1]).magnitude;
                if (chunkLen > 110f || i == r.Count - 1) { Chunk(root, r, sList, start, i); start = i; chunkLen = 0f; }
            }
        }

        static void Chunk(Transform root, List<Vector3> r, List<float> s, int i0, int i1)
        {
            const int Across = 6; // vertices across: enough to shade the deep middle and the shallows
            var v = new List<Vector3>(); var uv0 = new List<Vector2>(); var uv1 = new List<Vector2>(); var n = new List<Vector3>(); var tg = new List<Vector4>(); var tri = new List<int>();
            for (int i = i0; i <= i1; i++)
            {
                Vector3 d = (r[Mathf.Min(i + 1, r.Count - 1)] - r[Mathf.Max(i - 1, 0)]).WithY(0).normalized; Vector3 side = new Vector3(-d.z, 0, d.x); // north bank on +side
                float hw = TownPlan.RiverHalfWidth(s[i]) + 1.4f; // a little under the banks so the edge never shows
                for (int k = 0; k <= Across; k++)
                {
                    float a = k / (float)Across * 2f - 1f;
                    v.Add(new Vector3(r[i].x, TownPlan.WaterY, r[i].z) + side * (a * hw));
                    uv0.Add(new Vector2(s[i], a * hw)); uv1.Add(new Vector2(Mathf.Abs(a), hw));
                    n.Add(Vector3.up); tg.Add(new Vector4(d.x, 0f, d.z, 1f));
                }
                if (i > i0)
                {
                    int b = (i - i0) * (Across + 1), p = b - (Across + 1);
                    for (int k = 0; k < Across; k++) { tri.Add(p + k); tri.Add(b + k); tri.Add(b + k + 1); tri.Add(p + k); tri.Add(b + k + 1); tri.Add(p + k + 1); }
                }
            }
            // wind the triangles to face up
            if (tri.Count >= 3 && Vector3.Cross(v[tri[1]] - v[tri[0]], v[tri[2]] - v[tri[0]]).y < 0f) for (int q = 0; q < tri.Count; q += 3) { int t = tri[q + 1]; tri[q + 1] = tri[q + 2]; tri[q + 2] = t; }
            var mesh = new Mesh { name = "RiverWater" };
            mesh.SetVertices(v); mesh.SetUVs(0, uv0); mesh.SetUVs(1, uv1); mesh.SetNormals(n); mesh.SetTangents(tg); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
            var g = new GameObject("Water"); g.transform.SetParent(root, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = TownMats.water != null ? TownMats.water : E.windows;
            mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false; mr.lightProbeUsage = LightProbeUsage.Off; mr.reflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox;
        }

        static void Banks(Transform root)
        {
            var r = TownPlan.River; var walls = new MeshKit(); var path = new MeshKit(); var reeds = new MeshKit(); var bits = new MeshKit();
            var rng = new System.Random(77); float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            float s = 0f; float nextReed = 3f, nextBench = 60f;
            var bridgeX = new List<float>(); foreach (var rd in TownPlan.Roads) if (!rd.exists && Mathf.Abs(rd.a.x - rd.b.x) < 0.1f) bridgeX.Add(rd.a.x);
            for (int i = 0; i < r.Count - 1; i++)
            {
                Vector3 a = r[i], b = r[i + 1]; float segL = (b - a).magnitude; Vector3 d = (b - a) / segL, side = new Vector3(-d.z, 0, d.x);
                float hw0 = TownPlan.RiverHalfWidth(s), hw1 = TownPlan.RiverHalfWidth(s + segL);
                float nearBridge = 999f; foreach (float bx in bridgeX) nearBridge = Mathf.Min(nearBridge, Mathf.Abs(a.x - bx));
                bool footbridge = Mathf.Abs(a.x - 30f) < 14f;
                // stone embankment walls for 30 m either side of each road bridge (the banks are built up there)
                if (nearBridge < 30f)
                    foreach (int sd in new[] { -1, 1 })
                    {
                        Vector3 p0 = a + side * sd * (hw0 + 0.2f), p1 = b + side * sd * (hw1 + 0.2f);
                        walls.Quad(TownMats.stone, p0 + Vector3.up * -3.6f, p1 + Vector3.up * -3.6f, p1 + Vector3.up * 0.15f, p0 + Vector3.up * 0.15f, -side * sd);
                        walls.Quad(TownMats.panel, p0 + Vector3.up * 0.15f, p1 + Vector3.up * 0.15f, p1 + side * sd * 0.5f + Vector3.up * 0.15f, p0 + side * sd * 0.5f + Vector3.up * 0.15f, Vector3.up);
                        walls.Quad(TownMats.panel, p0 + side * sd * 0.5f + Vector3.up * 0.15f, p1 + side * sd * 0.5f + Vector3.up * 0.15f, p1 + side * sd * 0.5f + Vector3.up * -0.5f, p0 + side * sd * 0.5f + Vector3.up * -0.5f, side * sd);
                        // railings along the top of the wall
                        if (i % 2 == 0) walls.Box(E.railGreen, p0 + side * sd * 0.25f + new Vector3(-0.03f, 0.15f, -0.03f), p0 + side * sd * 0.25f + new Vector3(0.03f, 1.1f, 0.03f));
                        Vector3 r0 = p0 + side * sd * 0.25f + Vector3.up * 1.05f, r1 = p1 + side * sd * 0.25f + Vector3.up * 1.05f;
                        walls.Quad(E.railGreen, r0, r1, r1 + Vector3.up * 0.06f, r0 + Vector3.up * 0.06f, side * sd);
                    }
                // the towpath: gravel along the south bank's top, stopping at the roads
                bool atRoad = false; foreach (float bx in bridgeX) if (Mathf.Abs(a.x - bx) < 9f) atRoad = true;
                if (!atRoad)
                {
                    float po = -1f; Vector3 q0 = a + side * po * (hw0 + 8.2f), q1 = b + side * po * (hw1 + 8.2f);
                    Vector3 w0 = side * 1.25f, y = Vector3.up * 0.03f;
                    path.Quad(TownMats.gravel, q0 - w0 + y, q1 - w0 + y, q1 + w0 + y, q0 + w0 + y, Vector3.up);
                }
                // reeds along the water's edge (not in the walled stretches), as crossed cards
                while (nextReed < s + segL)
                {
                    float t = (nextReed - s) / segL; Vector3 c = Vector3.Lerp(a, b, t);
                    if (nearBridge > 32f && !footbridge && TownMats.reeds != null)
                    {
                        int sd = rng.NextDouble() < 0.5 ? -1 : 1; float hw = Mathf.Lerp(hw0, hw1, t);
                        Vector3 at = c + side * sd * (hw + R(-0.6f, 0.4f)); at.y = TownPlan.WaterY - 0.1f;
                        float h = R(1.2f, 2.0f), w = R(0.9f, 1.7f);
                        for (int q = 0; q < 2; q++)
                        {
                            float ang = R(0f, 180f) + q * 90f; Vector3 ax = Quaternion.Euler(0, ang, 0) * Vector3.right * (w * 0.5f);
                            reeds.QuadUV(TownMats.reeds, at - ax, at + ax, at + ax + Vector3.up * h, at - ax + Vector3.up * h, Vector3.Cross(ax, Vector3.up), new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
                        }
                    }
                    nextReed += R(2.5f, 7f);
                }
                // benches and a bin along the towpath
                if (s > nextBench && !atRoad && nearBridge > 20f)
                {
                    Vector3 c = a - side * (hw0 + 10.2f); Bench(bits, c, side);
                    bits.Cyl(E.black, c + d * 1.6f, 0.22f, 0.8f, 10, true);
                    nextBench = s + R(55f, 90f);
                }
                s += segL;
            }
            walls.Build("Embankments", root, true, ShadowCastingMode.On);
            path.Build("Towpath", root, true, ShadowCastingMode.Off);
            var rg = reeds.Build("Reeds", root, false, ShadowCastingMode.Off);
            bits.Build("TowpathBits", root, true, ShadowCastingMode.On);
            // a couple of shopping trolleys and a traffic cone dumped in the shallows, this being where it is
            var junk = new MeshKit();
            foreach (var (x, sd) in new[] { (-120f, 1), (95f, -1) })
            {
                float zc = TownPlan.RiverCrossingZ(x, out float hw); if (float.IsNaN(zc)) continue;
                Vector3 c = new Vector3(x, TownPlan.WaterY - 0.45f, zc + sd * (hw - 1.2f));
                Trolley(junk, c, x * 7f);
            }
            junk.Build("Junk", root, false, ShadowCastingMode.Off);
        }

        static void Bench(MeshKit k, Vector3 c, Vector3 faceToRiver)
        {
            k.Push(Matrix4x4.TRS(c + Vector3.up * 0.03f, Quaternion.LookRotation(faceToRiver), Vector3.one));
            k.Box(E.fence, new Vector3(-0.9f, 0.42f, -0.22f), new Vector3(0.9f, 0.47f, 0.22f));
            k.Box(E.fence, new Vector3(-0.9f, 0.5f, -0.26f), new Vector3(0.9f, 0.85f, -0.22f));
            foreach (float x in new[] { -0.75f, 0.75f }) k.Box(E.black, new Vector3(x - 0.04f, 0f, -0.24f), new Vector3(x + 0.04f, 0.45f, 0.2f));
            k.Pop();
        }

        static void Trolley(MeshKit k, Vector3 c, float yaw)
        {
            k.Push(Matrix4x4.TRS(c, Quaternion.Euler(12f, yaw, 70f), Vector3.one)); // on its side in the mud
            for (float x = -0.3f; x <= 0.31f; x += 0.1f) k.Box(TownMats.steel, new Vector3(x - 0.008f, 0.2f, -0.45f), new Vector3(x + 0.008f, 0.9f, 0.45f));
            k.Box(TownMats.steel, new Vector3(-0.32f, 0.2f, -0.46f), new Vector3(0.32f, 0.22f, 0.46f));
            k.Box(TownMats.steel, new Vector3(-0.32f, 0.88f, -0.46f), new Vector3(0.32f, 0.9f, 0.46f));
            k.Pop();
        }

        /// <summary>A steel bowstring footbridge across the river at x: deck, two arches, hangers, railings, ramps.</summary>
        static void Footbridge(Transform root, float x)
        {
            float zc = TownPlan.RiverCrossingZ(x, out float hw); if (float.IsNaN(zc)) return;
            var k = new MeshKit(); float span = hw + 7.5f, z0 = zc - span, z1 = zc + span, w = 1.6f, deck = 0.55f;
            // ramps up from each bank to the deck, and the deck
            k.Quad(E.pave, new Vector3(x - w, 0.03f, z0 - 6f), new Vector3(x + w, 0.03f, z0 - 6f), new Vector3(x + w, deck, z0), new Vector3(x - w, deck, z0), Vector3.up + Vector3.back * 0.1f);
            k.Quad(E.pave, new Vector3(x - w, deck, z1), new Vector3(x + w, deck, z1), new Vector3(x + w, 0.03f, z1 + 6f), new Vector3(x - w, 0.03f, z1 + 6f), Vector3.up + Vector3.forward * 0.1f);
            k.Box(TownMats.steel, new Vector3(x - w, deck - 0.35f, z0), new Vector3(x + w, deck, z1));
            k.Box(E.pave, new Vector3(x - w + 0.05f, deck, z0), new Vector3(x + w - 0.05f, deck + 0.02f, z1));
            foreach (int s in new[] { -1, 1 })
            {
                float px = x + s * (w + 0.05f);
                // the arch: a curved box tube from bank to bank, hangers down to the deck edge
                int N = 14; float rise = 5.5f; Vector3 prev = new Vector3(px, deck, z0);
                for (int i = 1; i <= N; i++)
                {
                    float t = i / (float)N; Vector3 p = new Vector3(px, deck + Mathf.Sin(t * Mathf.PI) * rise, Mathf.Lerp(z0, z1, t));
                    Vector3 mid = (prev + p) * 0.5f, dir = (p - prev);
                    k.Push(Matrix4x4.TRS(mid, Quaternion.LookRotation(dir), Vector3.one)); k.Box(E.white, new Vector3(-0.12f, -0.12f, -dir.magnitude * 0.5f - 0.05f), new Vector3(0.12f, 0.12f, dir.magnitude * 0.5f + 0.05f)); k.Pop();
                    if (i < N) k.Box(E.white, new Vector3(px - 0.02f, deck, p.z - 0.02f), new Vector3(px + 0.02f, p.y, p.z + 0.02f));
                    prev = p;
                }
                // railings
                k.Box(TownMats.steel, new Vector3(px - 0.03f, deck + 1.05f, z0), new Vector3(px + 0.03f, deck + 1.12f, z1));
                for (float z = z0; z <= z1; z += 1.2f) k.Box(TownMats.steel, new Vector3(px - 0.02f, deck, z - 0.02f), new Vector3(px + 0.02f, deck + 1.1f, z + 0.02f));
            }
            var g = k.Build("Footbridge", root, true, ShadowCastingMode.On);
        }
    }

    /// <summary>Anyone who ends up in the deep part of the river (the player, mostly) is hauled out onto the nearest bank, soaked.</summary>
    public class RiverWater : MonoBehaviour
    {
        float next; float inSince = -1f;
        void Update()
        {
            if (Time.time < next) return; next = Time.time + 0.25f;
            var pc = FindPlayer(); if (pc == null || pc.InCar) return;
            Vector3 p = pc.transform.position;
            if (p.z < 180f || p.z > 270f) { inSince = -1f; return; }
            float d = TownPlan.RiverDist(p, out float s, out float side); float hw = TownPlan.RiverHalfWidth(s);
            if (d < hw - 0.5f && p.y < TownPlan.WaterY + 0.4f)
            {
                if (inSince < 0f) { inSince = Time.time; World.Toast("You're in the river. It's freezing.", 1); if (GameAudio.I != null) GameAudio.I.Play3D(AudioSynth.Thud(), p, 0.3f); }
                else if (Time.time - inSince > 2.5f)
                {
                    // out onto the bank on the side they went in
                    var r = TownPlan.River; int best = 0; float bd = float.MaxValue;
                    for (int i = 0; i < r.Count; i++) { float dd = (r[i] - p).WithY(0).sqrMagnitude; if (dd < bd) { bd = dd; best = i; } }
                    Vector3 dir = (r[Mathf.Min(best + 1, r.Count - 1)] - r[Mathf.Max(best - 1, 0)]).WithY(0).normalized; Vector3 nrm = new Vector3(-dir.z, 0, dir.x);
                    Vector3 to = r[best] + nrm * side * (hw + 8f);
                    pc.Teleport(to + Vector3.up * 0.2f, pc.transform.eulerAngles.y);
                    World.Toast("You drag yourself out onto the bank, soaked through and stinking of river.", 0);
                    inSince = -1f;
                }
            }
            else inSince = -1f;
        }
        static PlayerCtl pc; static PlayerCtl FindPlayer() { if (pc == null) pc = FindFirstObjectByType<PlayerCtl>(); return pc; }
    }
}

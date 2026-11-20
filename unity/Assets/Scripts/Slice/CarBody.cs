using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>Anything that can run people over.</summary>
    public interface IVehicle { float Speed { get; } Vector3 Velocity { get; } }

    /// <summary>
    /// Procedural car bodies lofted from cross-sections (hatchback, saloon, van): painted shell with tinted glass,
    /// wheel-arch shadows, bumpers, lamps and UK plates. One mesh per car plus four shared wheel meshes.
    /// </summary>
    public static class CarBody
    {
        public class Spec { public float L, yb = 0.3f; public float[] u, belt, top, W; public bool van; public float wheelR = 0.31f, wheelZ0 = 0.17f, wheelZ1 = 0.83f; }

        public static readonly Spec Hatch = new Spec
        {
            L = 3.9f,
            u = new[] { 0.00f, 0.03f, 0.10f, 0.17f, 0.30f, 0.62f, 0.76f, 0.90f, 0.97f, 1.00f },
            belt = new[] { 0.80f, 0.90f, 0.94f, 0.95f, 0.95f, 0.95f, 0.94f, 0.88f, 0.80f, 0.70f },
            top = new[] { 0.84f, 0.98f, 1.20f, 1.38f, 1.46f, 1.46f, 1.00f, 0.92f, 0.84f, 0.72f },
            W = new[] { 0.74f, 0.80f, 0.83f, 0.84f, 0.85f, 0.85f, 0.85f, 0.83f, 0.79f, 0.72f },
            wheelZ0 = 0.16f, wheelZ1 = 0.84f
        };
        public static readonly Spec Saloon = new Spec
        {
            L = 4.4f,
            u = new[] { 0.00f, 0.03f, 0.12f, 0.24f, 0.37f, 0.63f, 0.76f, 0.91f, 0.97f, 1.00f },
            belt = new[] { 0.82f, 0.90f, 0.94f, 0.95f, 0.95f, 0.95f, 0.94f, 0.88f, 0.80f, 0.70f },
            top = new[] { 0.86f, 0.96f, 1.00f, 1.05f, 1.42f, 1.42f, 1.00f, 0.92f, 0.84f, 0.72f },
            W = new[] { 0.76f, 0.83f, 0.86f, 0.87f, 0.87f, 0.87f, 0.87f, 0.85f, 0.80f, 0.73f },
            wheelZ0 = 0.17f, wheelZ1 = 0.82f
        };
        public static readonly Spec Van = new Spec
        {
            L = 4.8f, van = true,
            u = new[] { 0.00f, 0.02f, 0.60f, 0.70f, 0.79f, 0.91f, 0.97f, 1.00f },
            belt = new[] { 0.98f, 1.02f, 1.05f, 1.05f, 1.00f, 0.92f, 0.85f, 0.75f },
            top = new[] { 1.95f, 2.02f, 2.02f, 1.97f, 1.32f, 1.02f, 0.92f, 0.78f },
            W = new[] { 0.94f, 0.97f, 0.97f, 0.97f, 0.95f, 0.92f, 0.88f, 0.80f },
            wheelR = 0.34f, wheelZ0 = 0.15f, wheelZ1 = 0.83f
        };

        static Material glass, under, chrome, tyre, lampF, lampR, plateF, plateR, grille;
        static Mesh wheelMesh;
        static void Init()
        {
            if (glass != null) return;
            glass = Gfx.Mat(new Color(0.05f, 0.07f, 0.09f), null, 0.95f, 0.2f);
            under = Gfx.Mat(new Color(0.05f, 0.05f, 0.05f), null, 0.1f);
            chrome = Gfx.Mat(new Color(0.75f, 0.76f, 0.78f), null, 0.85f, 1f);
            tyre = Gfx.Mat(new Color(0.06f, 0.06f, 0.06f), null, 0.25f);
            grille = Gfx.Mat(new Color(0.09f, 0.09f, 0.1f), null, 0.4f, 0.3f);
            lampF = Gfx.Emissive(new Color(1f, 0.96f, 0.85f), 1.6f);
            lampR = Gfx.Emissive(new Color(0.85f, 0.05f, 0.04f), 1.1f);
            plateF = Gfx.Mat(new Color(0.95f, 0.95f, 0.93f), null, 0.4f);
            plateR = Gfx.Mat(new Color(0.98f, 0.82f, 0.1f), null, 0.4f);
            var k = new MeshKit();
            k.Cyl(tyre, new Vector3(0, -0.1f, 0), 1f, 0.2f, 18, true);
            k.Cyl(chrome, new Vector3(0, 0.1f, 0), 0.55f, 0.012f, 14, true);
            k.Cyl(chrome, new Vector3(0, -0.112f, 0), 0.55f, 0.012f, 14, true);
            var g = k.Build("WheelProto", null, false); wheelMesh = g.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(g);
        }

        /// <summary>Builds the body under parent (local origin at ground centre, front = +z). Returns 4 wheel visuals FL, FR, RL, RR.</summary>
        public static Transform[] Build(Transform parent, Spec s, Color paintCol, bool castShadow = true)
        {
            Init();
            var paint = Gfx.Mat(paintCol, null, 0.82f, 0.35f);
            var k = new MeshKit();
            int n = s.u.Length;
            Vector3[][] sec = new Vector3[n][];
            for (int i = 0; i < n; i++)
            {
                float z = (s.u[i] - 0.5f) * s.L, W = s.W[i], yb = s.yb, yB = s.belt[i], yT = Mathf.Max(s.top[i], yB + 0.02f);
                bool cabin = yT - yB > 0.2f;
                float Wt = cabin ? W * 0.8f : W - 0.04f;
                sec[i] = new[]
                {
                    new Vector3(0, yb, z), new Vector3(W - 0.06f, yb, z), new Vector3(W, yb + 0.1f, z), new Vector3(W, yB - 0.04f, z),
                    new Vector3(W - 0.02f, yB, z), new Vector3(Wt, yT - 0.05f, z), new Vector3(Wt - 0.05f, yT, z), new Vector3(0, yT, z)
                };
            }
            for (int i = 0; i < n - 1; i++)
            {
                var a = sec[i]; var b = sec[i + 1];
                bool cabA = s.top[i] - s.belt[i] > 0.2f, cabB = s.top[i + 1] - s.belt[i + 1] > 0.2f;
                bool screen = Mathf.Abs(s.top[i] - s.top[i + 1]) > 0.22f && (cabA || cabB);
                for (int e = 0; e < 7; e++)
                {
                    Material m = e == 0 ? under : paint;
                    if (e == 4 && cabA && cabB && (!s.van || s.u[i] > 0.55f)) m = glass;
                    if (e == 6 && screen) m = glass;
                    foreach (int side in new[] { 1, -1 })
                    {
                        Vector3 p0 = M(a[e], side), p1 = M(a[e + 1], side), p2 = M(b[e + 1], side), p3 = M(b[e], side);
                        Vector3 mid = (p0 + p1 + p2 + p3) / 4f;
                        Vector3 nrm = Vector3.Cross(p1 - p0, p3 - p0); // orientation fixed below
                        Vector3 outward = new Vector3(mid.x, mid.y - 0.65f, 0f);
                        if (Vector3.Dot(nrm, outward) < 0) nrm = -nrm;
                        k.Quad(m, p0, p1, p2, p3, nrm);
                    }
                }
            }
            // end caps
            Cap(k, sec[0], paint, Vector3.back); Cap(k, sec[n - 1], paint, Vector3.forward);
            float zr = -s.L / 2f, zf = s.L / 2f;
            // bumpers, grille, lamps, plates
            k.Box(under, new Vector3(-s.W[n - 1] - 0.03f, 0.3f, zf - 0.08f), new Vector3(s.W[n - 1] + 0.03f, 0.5f, zf + 0.06f));
            k.Box(under, new Vector3(-s.W[0] - 0.03f, 0.3f, zr - 0.06f), new Vector3(s.W[0] + 0.03f, 0.5f, zr + 0.08f));
            k.Box(grille, new Vector3(-0.35f, 0.52f, zf - 0.02f), new Vector3(0.35f, 0.66f, zf + 0.012f));
            k.Box(plateF, new Vector3(-0.26f, 0.36f, zf + 0.06f), new Vector3(0.26f, 0.47f, zf + 0.075f));
            k.Box(plateR, new Vector3(-0.26f, 0.55f, zr - 0.02f), new Vector3(0.26f, 0.66f, zr - 0.005f));
            foreach (int side in new[] { -1, 1 })
            {
                float wf = s.W[n - 1], wr = s.W[0];
                k.Box(lampF, new Vector3(side * (wf - 0.1f) - 0.14f, 0.6f, zf - 0.02f), new Vector3(side * (wf - 0.1f) + 0.14f, 0.7f, zf + 0.015f));
                k.Box(lampR, new Vector3(side * (wr - 0.08f) - 0.12f, s.van ? 0.8f : 0.7f, zr - 0.015f), new Vector3(side * (wr - 0.08f) + 0.12f, s.van ? 1.1f : 0.82f, zr + 0.02f));
                // door mirror
                int mi = 0; for (int i = 0; i < n; i++) if (s.u[i] <= 0.7f) mi = i;
                float zm = (0.68f - 0.5f) * s.L, wm = s.W[mi];
                k.Box(paint, new Vector3(side > 0 ? wm : -wm - 0.16f, s.belt[mi] + 0.02f, zm - 0.05f), new Vector3(side > 0 ? wm + 0.16f : -wm, s.belt[mi] + 0.14f, zm + 0.06f));
                // wheel-arch shadows
                foreach (float uz in new[] { s.wheelZ0, s.wheelZ1 })
                {
                    float wz = (uz - 0.5f) * s.L, x = side * (Wat(s, uz) + 0.004f), r = s.wheelR + 0.08f;
                    Vector3 c = new Vector3(x, s.wheelR, wz);
                    for (int q = 0; q < 10; q++)
                    {
                        float a0 = Mathf.PI * q / 10f, a1 = Mathf.PI * (q + 1) / 10f;
                        Vector3 p0 = c + new Vector3(0, Mathf.Max(Mathf.Sin(a0) * r, s.yb - s.wheelR), Mathf.Cos(a0) * r), p1 = c + new Vector3(0, Mathf.Max(Mathf.Sin(a1) * r, s.yb - s.wheelR), Mathf.Cos(a1) * r);
                        k.TriN(under, c, p0, p1, new Vector3(side, 0, 0));
                    }
                }
            }
            var body = k.Build("Body", parent, false, castShadow ? ShadowCastingMode.On : ShadowCastingMode.Off);
            // wheels
            var wheels = new Transform[4];
            float[] zs = { (s.wheelZ1 - 0.5f) * s.L, (s.wheelZ1 - 0.5f) * s.L, (s.wheelZ0 - 0.5f) * s.L, (s.wheelZ0 - 0.5f) * s.L };
            for (int i = 0; i < 4; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                var g = new GameObject("Wheel" + i); g.transform.SetParent(parent, false);
                g.transform.localPosition = new Vector3(side * (Wat(s, i < 2 ? s.wheelZ1 : s.wheelZ0) - 0.08f), s.wheelR, zs[i]);
                g.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                g.transform.localScale = new Vector3(s.wheelR, 1f, s.wheelR);
                g.AddComponent<MeshFilter>().sharedMesh = wheelMesh;
                var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterials = new[] { tyre, chrome }; mr.shadowCastingMode = castShadow ? ShadowCastingMode.On : ShadowCastingMode.Off;
                wheels[i] = g.transform;
            }
            return wheels;
        }

        static float Wat(Spec s, float u)
        {
            for (int i = 0; i < s.u.Length - 1; i++) if (u >= s.u[i] && u <= s.u[i + 1]) return Mathf.Lerp(s.W[i], s.W[i + 1], (u - s.u[i]) / (s.u[i + 1] - s.u[i]));
            return s.W[0];
        }

        static Vector3 M(Vector3 p, int side) => new Vector3(p.x * side, p.y, p.z);

        static void Cap(MeshKit k, Vector3[] sec, Material m, Vector3 n)
        {
            Vector3 c = new Vector3(0, (sec[0].y + sec[7].y) / 2f, sec[0].z);
            for (int side = -1; side <= 1; side += 2)
                for (int e = 0; e < 7; e++) k.TriN(m, c, M(sec[e], side), M(sec[e + 1], side), n);
        }

        public static Spec Random(System.Random r) { int v = r.Next(10); return v < 5 ? Hatch : v < 8 ? Saloon : Van; }
        public static Color RandomPaint(System.Random r)
        {
            Color[] c = { new Color(0.75f, 0.75f, 0.77f), new Color(0.1f, 0.1f, 0.12f), new Color(0.92f, 0.92f, 0.9f), new Color(0.12f, 0.22f, 0.45f), new Color(0.5f, 0.06f, 0.07f), new Color(0.15f, 0.3f, 0.2f), new Color(0.35f, 0.36f, 0.38f), new Color(0.6f, 0.5f, 0.3f) };
            return c[r.Next(c.Length)];
        }
    }
}

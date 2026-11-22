using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASBO.Slice
{
    /// <summary>
    /// Someone at the wheel of a passing car: a small seated figure (head, hair or hat or hood, neck, torso, arms to the
    /// wheel, hands, thighs) cheap enough for every car on the road. One shared material and a palette texture of skin,
    /// hair and clothes colours; a couple of dozen figures made once and shared out. Seen through the glass it reads as a
    /// person driving; right up close it's simple, which is why the player's own car seats the player's real body instead.
    /// UK cars: the driver sits on the right.
    /// </summary>
    public static class Driver
    {
        const int Kinds = 24, PalW = 16, PalH = 4, Cell = 4;
        static Mesh[] kinds; static Material mat;

        // palette: row 0 skin (0-7) and hair (8-15), row 1 tops, row 2 trousers (0-7) and hats (8-15), row 3 face bits
        static readonly Color[] Skin = { C(0.94f, 0.78f, 0.67f), C(0.9f, 0.7f, 0.58f), C(0.96f, 0.82f, 0.72f), C(0.86f, 0.62f, 0.48f), C(0.62f, 0.42f, 0.3f), C(0.45f, 0.3f, 0.21f), C(0.3f, 0.2f, 0.14f), C(0.76f, 0.56f, 0.4f) };
        static readonly Color[] Hair = { C(0.08f, 0.06f, 0.05f), C(0.22f, 0.14f, 0.08f), C(0.38f, 0.25f, 0.14f), C(0.62f, 0.48f, 0.3f), C(0.78f, 0.66f, 0.45f), C(0.5f, 0.22f, 0.1f), C(0.55f, 0.55f, 0.55f), C(0.85f, 0.85f, 0.82f) };
        static readonly Color[] Tops = { C(0.12f, 0.12f, 0.13f), C(0.35f, 0.35f, 0.37f), C(0.6f, 0.6f, 0.62f), C(0.1f, 0.13f, 0.28f), C(0.55f, 0.08f, 0.08f), C(0.9f, 0.9f, 0.88f), C(0.15f, 0.3f, 0.15f), C(0.85f, 0.85f, 0.15f),
                                         C(0.25f, 0.18f, 0.12f), C(0.08f, 0.2f, 0.45f), C(0.45f, 0.4f, 0.32f), C(0.62f, 0.3f, 0.45f), C(0.06f, 0.06f, 0.07f), C(0.75f, 0.42f, 0.12f), C(0.3f, 0.32f, 0.22f), C(0.18f, 0.4f, 0.5f) };
        static readonly Color[] Legs = { C(0.12f, 0.15f, 0.25f), C(0.08f, 0.08f, 0.09f), C(0.3f, 0.3f, 0.32f), C(0.22f, 0.28f, 0.42f), C(0.35f, 0.3f, 0.22f), C(0.15f, 0.15f, 0.16f), C(0.45f, 0.45f, 0.47f), C(0.2f, 0.22f, 0.18f) };
        static readonly Color[] Hats = { C(0.07f, 0.07f, 0.08f), C(0.12f, 0.14f, 0.3f), C(0.5f, 0.5f, 0.52f), C(0.55f, 0.1f, 0.08f), C(0.85f, 0.85f, 0.8f), C(0.1f, 0.25f, 0.12f), C(0.3f, 0.22f, 0.15f), C(0.8f, 0.7f, 0.15f) };
        static Color C(float r, float g, float b) => new Color(r, g, b);

        static Vector2 UV(int col, int row) => new Vector2((col + 0.5f) / PalW, (row + 0.5f) / PalH);

        static Material Mat()
        {
            if (mat != null) return mat;
            var px = new Color32[PalW * Cell * PalH * Cell];
            void Put(int col, int row, Color c) { for (int y = 0; y < Cell; y++) for (int x = 0; x < Cell; x++) px[(row * Cell + y) * PalW * Cell + col * Cell + x] = c; }
            for (int i = 0; i < 8; i++) { Put(i, 0, Skin[i]); Put(8 + i, 0, Hair[i]); Put(i, 2, Legs[i]); Put(8 + i, 2, Hats[i]); }
            for (int i = 0; i < 16; i++) Put(i, 1, Tops[i]);
            Put(0, 3, C(0.05f, 0.04f, 0.04f)); Put(1, 3, C(0.12f, 0.12f, 0.13f)); Put(2, 3, C(0.95f, 0.95f, 0.92f)); Put(3, 3, C(0.9f, 0.82f, 0.15f));
            var tex = new Texture2D(PalW * Cell, PalH * Cell, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "DriverPalette" };
            tex.SetPixels32(px); tex.Apply(false, true);
            mat = Gfx.Mat(Color.white, tex, 0.18f); mat.name = "Driver";
            return mat;
        }

        // where the front seat backs are in our modelled cars (from the Blender build: car length / 2 - seat_u)
        static readonly Dictionary<string, float> SeatBack = new Dictionary<string, float>
        { { "hatch90", -0.035f }, { "saloon90", 0.025f }, { "jeep", 0.06f }, { "boxy80", 0.03f }, { "supermini", 0.15f }, { "hatch70", -0.1f }, { "rally", -0.025f }, { "coupe80", -0.075f }, { "estate90", 0.055f } };

        /// <summary>The driver's hips in the car (its ground-level middle as origin, front +z; right-hand drive). A car's seat is low, a van's high and upright.</summary>
        public static Vector3 HipLocal(CarModel m)
        {
            float floor = m.floor > 0.2f ? m.floor : 0.36f;
            float back = SeatBack.TryGetValue(m.id, out var sb) ? sb : m.gh != null && m.gh.fb != 0f ? m.gh.fb - (m.van ? 1.05f : 1.2f) : m.seatF + 0.3f;
            return new Vector3(m.W * 0.215f, floor + (m.van ? 0.2f : 0.13f), back + 0.17f);
        }
        /// <summary>The middle of the steering wheel, from the driver's hips.</summary>
        public static readonly Vector3 WheelFromHip = new Vector3(0f, 0.37f, 0.45f);

        /// <summary>Puts a driver in this car (under body, which has the car's ground-level middle as its origin, front +z).</summary>
        public static GameObject Seat(Transform body, CarModel m, int seed)
        {
            if (kinds == null) { kinds = new Mesh[Kinds]; for (int i = 0; i < Kinds; i++) kinds[i] = Make(i); }
            var g = new GameObject("Driver"); g.transform.SetParent(body, false);
            g.transform.localPosition = HipLocal(m);
            g.layer = body.gameObject.layer;
            g.AddComponent<MeshFilter>().sharedMesh = kinds[(seed & 0x7fffffff) % Kinds];
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = Mat();
            r.shadowCastingMode = ShadowCastingMode.Off; r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            var lg = g.AddComponent<LODGroup>(); lg.SetLODs(new[] { new LOD(0.035f, new Renderer[] { r }) }); lg.RecalculateBounds(); // gone when the car's small on screen
            return g;
        }

        // ------------------------------------------------------------------------------------------- the figures
        class B
        {
            public readonly List<Vector3> v = new List<Vector3>(); public readonly List<Vector3> n = new List<Vector3>(); public readonly List<Vector2> uv = new List<Vector2>(); public readonly List<int> t = new List<int>();
            /// <summary>An ellipsoid; keep(dir) says which bits of its surface to make (for hair, hoods and hats).</summary>
            public void Blob(Vector3 c, Vector3 r, Quaternion q, Vector2 col, int seg = 10, int rings = 7, System.Func<Vector3, bool> keep = null)
            {
                int b0 = v.Count;
                for (int i = 0; i <= rings; i++)
                {
                    float th = Mathf.PI * i / rings;
                    for (int j = 0; j <= seg; j++)
                    {
                        float ph = 2f * Mathf.PI * j / seg;
                        var d = new Vector3(Mathf.Sin(th) * Mathf.Sin(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Cos(ph));
                        v.Add(c + q * Vector3.Scale(d, r)); n.Add((q * new Vector3(d.x / r.x, d.y / r.y, d.z / r.z)).normalized); uv.Add(col);
                    }
                }
                for (int i = 0; i < rings; i++)
                    for (int j = 0; j < seg; j++)
                    {
                        int a = b0 + i * (seg + 1) + j, b = a + seg + 1;
                        if (keep != null)
                        {
                            float th = Mathf.PI * (i + 0.5f) / rings, ph = 2f * Mathf.PI * (j + 0.5f) / seg;
                            if (!keep(new Vector3(Mathf.Sin(th) * Mathf.Sin(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Cos(ph)))) continue;
                        }
                        t.Add(a); t.Add(b + 1); t.Add(b); t.Add(a); t.Add(a + 1); t.Add(b + 1);
                    }
            }
            /// <summary>A tapering tube from a to b (elliptical: rx across, rz the other way), capped.</summary>
            public void Tube(Vector3 a, Vector3 b, float ra, float rb, Vector2 col, int seg = 7, float squash = 1f)
            {
                var ax = (b - a).normalized; var side = Vector3.Cross(ax, Mathf.Abs(ax.y) > 0.9f ? Vector3.right : Vector3.up).normalized; var up = Vector3.Cross(side, ax);
                int b0 = v.Count;
                for (int e = 0; e < 2; e++)
                    for (int j = 0; j <= seg; j++)
                    {
                        float ph = 2f * Mathf.PI * j / seg; var d = side * Mathf.Cos(ph) + up * Mathf.Sin(ph) * squash;
                        v.Add((e == 0 ? a : b) + d * (e == 0 ? ra : rb)); n.Add(d.normalized); uv.Add(col);
                    }
                for (int j = 0; j < seg; j++) { int p = b0 + j, q2 = p + seg + 1; t.Add(p); t.Add(q2); t.Add(p + 1); t.Add(p + 1); t.Add(q2); t.Add(q2 + 1); }
                // caps
                for (int e = 0; e < 2; e++)
                {
                    int ci = v.Count; v.Add(e == 0 ? a : b); n.Add(e == 0 ? -ax : ax); uv.Add(col);
                    for (int j = 0; j < seg; j++)
                    {
                        int p = b0 + e * (seg + 1) + j;
                        if (e == 0) { t.Add(ci); t.Add(p + 1); t.Add(p); } else { t.Add(ci); t.Add(p); t.Add(p + 1); }
                    }
                }
            }
            public void Box(Vector3 c, Vector3 s, Quaternion q, Vector2 col)
            {
                Vector3[] f = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
                foreach (var nn in f)
                {
                    var u = Vector3.Cross(nn, Mathf.Abs(nn.y) > 0.5f ? Vector3.forward : Vector3.up); var w = Vector3.Cross(nn, u);
                    int b0 = v.Count;
                    foreach (var (su, sw) in new[] { (-1, -1), (1, -1), (1, 1), (-1, 1) })
                    {
                        var p = nn * 0.5f + u * 0.5f * su + w * 0.5f * sw;
                        v.Add(c + q * Vector3.Scale(p, s)); n.Add(q * nn); uv.Add(col);
                    }
                    t.Add(b0); t.Add(b0 + 2); t.Add(b0 + 1); t.Add(b0); t.Add(b0 + 3); t.Add(b0 + 2);
                }
            }
            public Mesh Done(string name)
            {
                var m = new Mesh { name = name }; m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
                // make every triangle face the way its normals say (the builders above don't all wind the same way)
                var tri = m.triangles;
                for (int i = 0; i < tri.Length; i += 3)
                {
                    Vector3 a = v[tri[i]], b = v[tri[i + 1]], c = v[tri[i + 2]];
                    if (Vector3.Dot(Vector3.Cross(b - a, c - a), n[tri[i]] + n[tri[i + 1]] + n[tri[i + 2]]) < 0f) { int k = tri[i + 1]; tri[i + 1] = tri[i + 2]; tri[i + 2] = k; }
                }
                m.triangles = tri; m.RecalculateBounds(); return m;
            }
        }

        static Mesh Make(int k)
        {
            var r = new System.Random(k * 7919 + 13);
            int Pick(params float[] w) { float s = 0; foreach (var x in w) s += x; double p = r.NextDouble() * s; for (int i = 0; i < w.Length; i++) { p -= w[i]; if (p <= 0) return i; } return w.Length - 1; }
            bool woman = r.NextDouble() < 0.38;
            int skin = Pick(5, 4, 3, 2, 2, 2, 1.5f, 1.5f);
            int hairC = skin >= 4 ? (r.NextDouble() < 0.85 ? 0 : 1) : Pick(2, 3, 2, 1.5f, 1.5f, 0.8f, 1f, 0.6f);
            // what's on their head: 0 short hair, 1 shaved, 2 long hair, 3 beanie, 4 baseball cap, 5 hood up
            int head = woman ? Pick(1, 0, 6, 1, 0.6f, 0.8f) : Pick(5, 2.5f, 0.4f, 1.5f, 1.8f, 1.5f);
            int top = Pick(3, 2, 1, 2, 1, 1, 0.6f, 0.5f, 1, 1, 0.8f, 0.6f, 1.2f, 0.4f, 0.6f, 0.6f);
            int legs = r.Next(8), hat = r.Next(8);
            Vector2 cSkin = UV(skin, 0), cHair = UV(8 + hairC, 0), cTop = UV(top, 1), cLeg = UV(legs, 2), cHat = UV(8 + hat, 2), cDark = UV(0, 3);

            var b = new B();
            float sw = woman ? 0.18f : 0.205f, hw = woman ? 0.16f : 0.155f; // half widths: shoulders, hips
            var lean = Quaternion.Euler(-13f, 0f, 0f); // sat back in the seat
            Vector3 L(float x, float y, float z) => lean * new Vector3(x, y, z);
            // torso: hips, waist, chest and shoulders as one stack of tubes
            Vector3 hip = L(0, 0.02f, 0), waist = L(0, 0.2f, 0.005f), chest = L(0, 0.38f, 0.01f), shoulder = L(0, 0.5f, 0f);
            b.Tube(hip, waist, hw, hw * 0.92f, cTop, 8, 0.66f);
            b.Tube(waist, chest, hw * 0.92f, sw * 0.93f, cTop, 8, 0.6f);
            b.Tube(chest, shoulder, sw * 0.93f, sw * 0.82f, cTop, 8, 0.56f);
            b.Blob(L(0, 0.5f, -0.005f), new Vector3(sw * 0.86f, 0.06f, 0.1f), lean, cTop, 8, 4); // round the shoulders off
            if (top == 7) b.Tube(waist + L(0, 0.02f, 0), shoulder - L(0, 0.04f, 0), hw * 0.96f, sw * 0.88f, UV(3, 3), 8, 0.62f); // hi-vis vest
            // neck and head
            Vector3 neckA = L(0, 0.52f, 0.0f), neckB = neckA + new Vector3(0, 0.085f, 0.012f);
            b.Tube(neckA, neckB, 0.05f, 0.046f, cSkin, 6);
            Vector3 hc = neckB + new Vector3(0, 0.1f, 0.018f); Vector3 hr = new Vector3(woman ? 0.074f : 0.078f, 0.105f, 0.095f);
            b.Blob(hc, hr, Quaternion.identity, cSkin, 12, 8);
            // eyes, brows and a nose so it isn't a blank egg close up
            foreach (float sx in new[] { -1f, 1f })
            {
                b.Box(hc + new Vector3(sx * 0.03f, 0.012f, hr.z * 0.93f), new Vector3(0.018f, 0.009f, 0.006f), Quaternion.identity, cDark);
                if (head != 5) b.Box(hc + new Vector3(sx * 0.031f, 0.032f, hr.z * 0.9f), new Vector3(0.026f, 0.007f, 0.008f), Quaternion.identity, head == 1 || hairC >= 6 ? UV(1, 3) : cHair);
            }
            b.Box(hc + new Vector3(0, -0.012f, hr.z * 0.98f), new Vector3(0.018f, 0.03f, 0.022f), Quaternion.Euler(-12f, 0, 0), cSkin);
            b.Box(hc + new Vector3(0, -0.05f, hr.z * 0.86f), new Vector3(0.03f, 0.006f, 0.008f), Quaternion.identity, UV(1, 3));
            // what's on top
            Vector3 hairR = hr * 1.07f;
            switch (head)
            {
                case 0: b.Blob(hc + new Vector3(0, 0.008f, -0.006f), hairR, Quaternion.identity, cHair, 12, 8, d => d.y > 0.25f - Mathf.Max(0f, -d.z) * 0.75f && !(d.z > 0.55f && d.y < 0.55f)); break;
                case 1: b.Blob(hc + new Vector3(0, 0.004f, -0.004f), hr * 1.015f, Quaternion.identity, cHair, 12, 8, d => d.y > 0.35f - Mathf.Max(0f, -d.z) * 0.6f && d.z < 0.6f); break;
                case 2:
                    b.Blob(hc + new Vector3(0, 0.01f, -0.008f), hairR, Quaternion.identity, cHair, 12, 8, d => d.y > 0.15f - Mathf.Max(0f, -d.z) * 1.2f && !(d.z > 0.5f && d.y < 0.6f));
                    b.Box(hc + new Vector3(0, -0.1f, -0.07f), new Vector3(0.15f, 0.2f, 0.04f), Quaternion.Euler(10f, 0, 0), cHair); // hair down the back
                    break;
                case 3: b.Blob(hc + new Vector3(0, 0.022f, -0.004f), new Vector3(hairR.x * 1.02f, hairR.y * 1.05f, hairR.z * 1.02f), Quaternion.identity, cHat, 12, 8, d => d.y > 0.18f); break;
                case 4:
                    b.Blob(hc + new Vector3(0, 0.014f, -0.004f), hairR * 1.01f, Quaternion.identity, cHat, 12, 8, d => d.y > 0.3f);
                    b.Box(hc + new Vector3(0, 0.045f, hr.z + 0.035f), new Vector3(0.13f, 0.008f, 0.08f), Quaternion.Euler(-8f, 0, 0), cHat); // the peak
                    break;
                case 5: b.Blob(hc + new Vector3(0, 0.01f, -0.018f), new Vector3(0.105f, 0.13f, 0.12f), Quaternion.identity, cTop, 12, 8, d => !(d.z > 0.45f && d.y < 0.75f && d.y > -0.5f)); break;
            }
            if (head != 5 && (top == 0 || top == 1 || top == 3 || top == 12)) b.Blob(L(0, 0.53f, -0.06f), new Vector3(0.12f, 0.05f, 0.07f), lean, cTop, 8, 4); // a hoodie's hood down round the neck
            // arms: shoulder, elbow down by the side, hands on the wheel at ten to two
            foreach (float sx in new[] { -1f, 1f })
            {
                Vector3 sh = L(sx * (sw - 0.025f), 0.47f, 0f), hand = new Vector3(sx * 0.16f, 0.37f, 0.44f);
                Vector3 elbow = (sh + hand) * 0.5f + new Vector3(sx * 0.07f, -0.13f, -0.04f);
                b.Tube(sh, elbow, woman ? 0.042f : 0.05f, 0.042f, cTop, 6);
                b.Tube(elbow, hand - (hand - elbow).normalized * 0.04f, 0.042f, 0.034f, cTop, 6);
                b.Blob(hand, new Vector3(0.036f, 0.045f, 0.04f), Quaternion.identity, cSkin, 6, 4);
                // thighs out along the seat, shins down to the pedals (mostly hidden by the doors and dash)
                Vector3 h0 = new Vector3(sx * 0.09f, 0.03f, 0.04f), knee = new Vector3(sx * 0.11f, 0.13f, 0.44f), ankle = new Vector3(sx * 0.12f, -0.22f, 0.66f);
                b.Tube(h0, knee, 0.078f, 0.058f, cLeg, 6);
                b.Tube(knee, ankle, 0.055f, 0.045f, cLeg, 6);
            }
            return b.Done("Driver" + k);
        }
    }
}

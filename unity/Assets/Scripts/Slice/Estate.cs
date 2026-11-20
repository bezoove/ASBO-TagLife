using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Abbots Wend: the gated estate of ten distinct thatched cottages, laid out exactly as in the browser build.
    /// Browser coordinates (x, z) map to Unity as (x - 45, 214 - z): the gate sits on the long road's north pavement.
    /// </summary>
    public static class Estate
    {
        public static readonly List<CotHouse> Houses = new List<CotHouse>();
        public static Vector3 W(float bx, float bz, float y = 0f) => new Vector3(bx - 45f, y, 214f - bz);
        public const float LaneZ = 26f; // centre of the internal lane in Unity z

        public static void Build(Transform parent)
        {
            CotMats.Init();
            Houses.Clear();
            var root = new GameObject("AbbotsWend").transform; root.SetParent(parent, false);
            if (!CotData.Load()) return;
            var walls = new MeshKit(); var ground = new MeshKit(); var green = new MeshKit(); var lamps = new MeshKit();
            var rng = new System.Random(77);

            // ---- perimeter wall with coping, gap for the gate ----
            void WallRun(Vector3 a, Vector3 b)
            {
                Vector3 mn = Vector3.Min(a, b), mx = Vector3.Max(a, b);
                bool alongX = mx.x - mn.x > mx.z - mn.z;
                Vector3 th = alongX ? new Vector3(0, 0, 0.25f) : new Vector3(0.25f, 0, 0);
                walls.Box(CotMats.stone, mn - th, mx + th + Vector3.up * 1.6f);
                walls.Box(CotMats.stone, mn - th * 1.3f + Vector3.up * 1.6f, mx + th * 1.3f + Vector3.up * 1.75f);
            }
            WallRun(W(0, 236), W(40.3f, 236)); WallRun(W(49.7f, 236), W(90, 236));
            WallRun(W(0, 160), W(90, 160)); WallRun(W(0, 160), W(0, 236)); WallRun(W(90, 160), W(90, 236));

            // ---- gate pillars, lamps and open iron gates ----
            var iron = CotMats.iron; var glow = Gfx.Emissive(new Color(1f, 0.88f, 0.6f), 1.6f);
            foreach (int s in new[] { -1, 1 })
            {
                var pc = new Vector3(s * 4.9f, 0, -22f);
                walls.BoxB(CotMats.stone, pc, new Vector3(0.9f, 2.6f, 0.9f));
                walls.BoxB(CotMats.stone, pc + Vector3.up * 2.6f, new Vector3(1.1f, 0.15f, 1.1f));
                lamps.Sphere(glow, pc + Vector3.up * 3.0f, Vector3.one * 0.2f, 10, 6);
                lamps.Cyl(iron, pc + Vector3.up * 2.75f, 0.06f, 0.1f, 8);
                lamps.Push(new Vector3(s * 4.4f, 0, -22f), s * -80f);
                for (int i = 0; i < 12; i++) lamps.BoxB(iron, new Vector3(-s * (0.15f + i * 0.2f), 0.1f, 0), new Vector3(0.035f, 2.0f, 0.035f));
                lamps.BoxB(iron, new Vector3(-s * 1.3f, 0.28f, 0), new Vector3(2.6f, 0.06f, 0.05f));
                lamps.BoxB(iron, new Vector3(-s * 1.3f, 1.92f, 0), new Vector3(2.6f, 0.06f, 0.05f));
                lamps.Sphere(CotMats.brass, new Vector3(-s * 1.3f, 1.15f, 0), Vector3.one * 0.1f, 8, 5);
                lamps.Pop();
            }
            var nameBoard = new GameObject("EstateName"); nameBoard.transform.SetParent(root, false);
            nameBoard.transform.localPosition = new Vector3(-4.9f, 1.36f, -22.49f); nameBoard.transform.localRotation = Quaternion.identity;
            var tm = nameBoard.AddComponent<TextMesh>(); tm.text = "ABBOTS WEND\nPRIVATE"; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
            tm.characterSize = 0.035f; tm.fontSize = 48; tm.color = new Color(0.15f, 0.12f, 0.1f);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); tm.font = font; nameBoard.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            walls.BoxB(CotMats.Paint(new Color(0.9f, 0.87f, 0.78f)), new Vector3(-4.9f, 1.35f, -22.46f), new Vector3(0.75f, 0.5f, 0.02f));

            // ---- drive and lane (gravel) ----
            ground.Box(CotMats.gravel, W(42.5f, 236, 0f), W(47.5f, 186, 0.05f));
            ground.Box(CotMats.gravel, W(3, 190, 0f), W(87, 186, 0.05f));
            // drive lamps
            for (float bz = 230; bz > 192; bz -= 9f)
                foreach (int s in new[] { -1, 1 })
                {
                    var p = W(45 + s * 3.2f, bz);
                    lamps.Cyl(iron, p, 0.05f, 2.6f, 8);
                    lamps.Sphere(glow, p + Vector3.up * 2.75f, Vector3.one * 0.16f, 8, 5);
                }

            // ---- cottages ----
            var sw = System.Diagnostics.Stopwatch.StartNew();
            foreach (var pl in CotData.Plots)
            {
                if (!CotData.Plans.TryGetValue(pl.id, out var plan)) continue;
                float th = pl.side == "+z" ? 0f : pl.side == "-z" ? Mathf.PI : pl.side == "+x" ? Mathf.PI / 2f : -Mathf.PI / 2f;
                float cx = pl.side == "+z" || pl.side == "-x" ? pl.r[0] : pl.r[1];
                float cz = pl.side == "+z" || pl.side == "+x" ? pl.r[3] : pl.r[2];
                float c = Mathf.Cos(th), s = Mathf.Sin(th);
                float ox = cx + plan.ox * c - plan.fg * s, oz = cz - plan.ox * s - plan.fg * c;
                var h = new CotHouse(pl, plan);
                try { h.Build(root, W(ox, oz), -th * Mathf.Rad2Deg); }
                catch (System.Exception e) { Debug.LogError("[ASBO] cottage " + pl.id + " failed: " + e); continue; }
                Houses.Add(h);
                Garden(h, pl, green, ground, rng);
            }
            Debug.Log("[ASBO] Abbots Wend: " + Houses.Count + " cottages built in " + sw.ElapsedMilliseconds + " ms");

            // ---- border trees inside the wall ----
            for (int i = 0; i < 22; i++)
            {
                float bx = i < 11 ? 2.5f + (float)rng.NextDouble() * 1.5f : 86f + (float)rng.NextDouble() * 1.5f;
                float bz = 165f + (i % 11) * 6.6f + (float)rng.NextDouble() * 2f;
                if (bz > 228f) continue;
                Tree(green, W(bx, bz), 3.5f + (float)rng.NextDouble() * 2.5f, rng);
            }

            walls.Build("EstateWalls", root, true);
            ground.Build("EstateGround", root, true, UnityEngine.Rendering.ShadowCastingMode.Off);
            green.Build("EstateGreenery", root, false);
            lamps.Build("EstateIron", root, false);
        }

        /// <summary>Hedged plot with a gap and flagstone path to the front door, flower beds, and back-garden trees.</summary>
        static void Garden(CotHouse h, CPlot pl, MeshKit green, MeshKit ground, System.Random rng)
        {
            float x0 = pl.r[0] - 45f, x1 = pl.r[1] - 45f, z0 = 214f - pl.r[3], z1 = 214f - pl.r[2];
            Vector3 door = h.root.TransformPoint(h.FrontDoorLocal);
            const float ins = 0.35f, hh = 1.05f, ht = 0.55f;
            // which edge is the front?
            string front = pl.side == "+z" ? "zmin" : pl.side == "-z" ? "zmax" : pl.side == "+x" ? "xmax" : "xmin";
            void HedgeX(float xa, float xb, float z) { if (xb - xa > 0.2f) green.Box(CotMats.hedge, new Vector3(xa, 0, z - ht / 2), new Vector3(xb, hh, z + ht / 2)); }
            void HedgeZ(float za, float zb, float x) { if (zb - za > 0.2f) green.Box(CotMats.hedge, new Vector3(x - ht / 2, 0, za), new Vector3(x + ht / 2, hh, zb)); }
            const float gap = 0.8f;
            if (front == "zmin") { HedgeX(x0 + ins, door.x - gap, z0 + ins); HedgeX(door.x + gap, x1 - ins, z0 + ins); } else HedgeX(x0 + ins, x1 - ins, z0 + ins);
            if (front == "zmax") { HedgeX(x0 + ins, door.x - gap, z1 - ins); HedgeX(door.x + gap, x1 - ins, z1 - ins); } else HedgeX(x0 + ins, x1 - ins, z1 - ins);
            if (front == "xmin") { HedgeZ(z0 + ins, door.z - gap, x0 + ins); HedgeZ(door.z + gap, z1 - ins, x0 + ins); } else HedgeZ(z0 + ins, z1 - ins, x0 + ins);
            if (front == "xmax") { HedgeZ(z0 + ins, door.z - gap, x1 - ins); HedgeZ(door.z + gap, z1 - ins, x1 - ins); } else HedgeZ(z0 + ins, z1 - ins, x1 - ins);

            // path from the gap to the door step
            Vector3 gapP = front == "zmin" ? new Vector3(door.x, 0, z0) : front == "zmax" ? new Vector3(door.x, 0, z1) : front == "xmin" ? new Vector3(x0, 0, door.z) : new Vector3(x1, 0, door.z);
            Vector3 a = Vector3.Min(gapP, door), b = Vector3.Max(gapP, door);
            bool alongZ = front == "zmin" || front == "zmax";
            Vector3 wv = alongZ ? new Vector3(0.6f, 0, 0) : new Vector3(0, 0, 0.6f);
            ground.Box(CotMats.flag, a - wv, b + wv + Vector3.up * 0.06f);

            // flower beds along the front wall either side of the path, and roses by the door
            Vector3 fwd = alongZ ? new Vector3(0, 0, front == "zmin" ? -1f : 1f) : new Vector3(front == "xmin" ? -1f : 1f, 0, 0);
            Vector3 along = alongZ ? Vector3.right : Vector3.forward;
            Vector3 bedC = door + fwd * 0.3f;
            foreach (int sgn in new[] { -1, 1 })
            {
                Vector3 c0 = bedC + along * sgn * 0.9f, c1 = bedC + along * sgn * 4.2f;
                Vector3 mn = Vector3.Min(c0, c1) - (alongZ ? new Vector3(0, 0, 0.45f) : new Vector3(0.45f, 0, 0)), mx = Vector3.Max(c0, c1) + (alongZ ? new Vector3(0, 0, 0.45f) : new Vector3(0.45f, 0, 0));
                mn.y = 0; mx.y = 0.08f;
                ground.Box(CotMats.soil, mn, mx);
                for (int q = 0; q < 16; q++)
                {
                    float t = (float)rng.NextDouble(); Vector3 p = Vector3.Lerp(c0, c1, t) + (alongZ ? new Vector3(0, 0, ((float)rng.NextDouble() - 0.5f) * 0.6f) : new Vector3(((float)rng.NextDouble() - 0.5f) * 0.6f, 0, 0));
                    float fh = 0.25f + (float)rng.NextDouble() * 0.35f;
                    green.Sphere(CotMats.leaf, p + Vector3.up * (fh * 0.5f), new Vector3(0.16f, fh * 0.5f, 0.16f), 6, 4);
                    green.Sphere(CotMats.flowers[rng.Next(CotMats.flowers.Length)], p + Vector3.up * fh, new Vector3(0.11f, 0.08f, 0.11f), 6, 4);
                }
            }
            // back-garden trees (away from the house)
            Vector3 back = -fwd;
            for (int q = 0; q < 3; q++)
            {
                Vector3 p = door + back * (11f + (float)rng.NextDouble() * 6f) + along * (((float)rng.NextDouble() - 0.5f) * (alongZ ? x1 - x0 - 4f : z1 - z0 - 4f));
                p.x = Mathf.Clamp(p.x, x0 + 2f, x1 - 2f); p.z = Mathf.Clamp(p.z, z0 + 2f, z1 - 2f); p.y = 0;
                bool clash = false; foreach (var r in h.zones) { Vector3 l = h.root.InverseTransformPoint(p); if (r.In(l.x, l.z, 2.5f)) clash = true; }
                if (!clash) Tree(green, p, 3.2f + (float)rng.NextDouble() * 2.5f, rng);
            }
        }

        static void Tree(MeshKit k, Vector3 p, float h, System.Random rng)
        {
            k.Cyl(CotMats.oak, p, 0.18f, h, 8, false, 0.12f);
            var leaf = CotMats.leaf;
            k.Sphere(leaf, p + Vector3.up * (h + 0.8f), new Vector3(2.0f, 1.7f, 2.0f), 10, 7);
            for (int q = 0; q < 3; q++)
            {
                float a = (float)rng.NextDouble() * 6.28f;
                k.Sphere(leaf, p + new Vector3(Mathf.Cos(a) * 1.1f, h + 0.2f + (float)rng.NextDouble() * 0.8f, Mathf.Sin(a) * 1.1f), new Vector3(1.3f, 1.1f, 1.3f), 8, 6);
            }
        }
    }
}

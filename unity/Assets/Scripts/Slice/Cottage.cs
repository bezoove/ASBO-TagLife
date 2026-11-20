using System.Collections.Generic;
using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>Rose Cottage: thatched stone cottage with eyebrow dormers, inglenook, beams and vintage contents.</summary>
    public static class Cottage
    {
        struct Op { public float c, w, sill, head; public Op(float c, float w, float sill, float head) { this.c = c; this.w = w; this.sill = sill; this.head = head; } }

        static Material stone, straw, strawDark, oak, plank, plaster, dark, glass, gold, brass, rug, brick, terracotta, paint, cloth, black, white;

        static void Mats()
        {
            stone = Gfx.Mat(Color.white, Gfx.StoneTex(3), 0.05f);
            straw = Gfx.Mat(Color.white, Gfx.StrawTex(5), 0.02f);
            strawDark = Gfx.Mat(new Color(0.55f, 0.45f, 0.25f), Gfx.StrawTex(9), 0.02f);
            oak = Gfx.Mat(new Color(0.24f, 0.15f, 0.08f), null, 0.25f);
            plank = Gfx.Mat(Color.white, Gfx.PlankTex(2), 0.3f);
            plaster = Gfx.Mat(new Color(0.9f, 0.86f, 0.76f), null, 0.05f);
            dark = Gfx.Mat(new Color(0.03f, 0.03f, 0.04f), null, 0.6f);
            glass = Gfx.Mat(new Color(0.12f, 0.16f, 0.2f), null, 0.9f);
            gold = Gfx.Mat(new Color(0.9f, 0.7f, 0.2f), null, 0.8f, 1f);
            brass = Gfx.Mat(new Color(0.7f, 0.5f, 0.2f), null, 0.7f, 1f);
            rug = Gfx.Mat(new Color(0.45f, 0.1f, 0.1f), null, 0.05f);
            brick = Gfx.Mat(new Color(0.5f, 0.22f, 0.14f), null, 0.05f);
            terracotta = Gfx.Mat(new Color(0.7f, 0.35f, 0.2f), null, 0.1f);
            paint = Gfx.Mat(new Color(0.18f, 0.3f, 0.24f), null, 0.4f);
            cloth = Gfx.Mat(new Color(0.3f, 0.22f, 0.16f), null, 0.05f);
            black = Gfx.Mat(new Color(0.05f, 0.05f, 0.05f), null, 0.5f, 0.6f);
            white = Gfx.Mat(new Color(0.92f, 0.92f, 0.9f), null, 0.4f);
        }

        // Wall segment along X (alongX=true, fixed z) or along Z (fixed x) with openings.
        static void Wall(Transform p, bool alongX, float u0, float u1, float fixedC, float h, float t, Material m, params Op[] ops)
        {
            System.Array.Sort(ops, (a, b) => a.c.CompareTo(b.c));
            float cur = u0;
            void Seg(float a, float b, float y0, float y1)
            {
                if (b - a < 0.001f || y1 - y0 < 0.001f) return;
                float uc = (a + b) / 2f, yc = (y0 + y1) / 2f;
                Vector3 c = alongX ? new Vector3(uc, yc, fixedC) : new Vector3(fixedC, yc, uc);
                Vector3 s = alongX ? new Vector3(b - a, y1 - y0, t) : new Vector3(t, y1 - y0, b - a);
                Gfx.Box(p, c, s, m, 2.5f, "Wall");
            }
            foreach (var o in ops)
            {
                float l = o.c - o.w / 2f, r = o.c + o.w / 2f;
                Seg(cur, l, 0, h);
                Seg(l, r, 0, o.sill);
                Seg(l, r, o.head, h);
                cur = r;
            }
            Seg(cur, u1, 0, h);
        }

        static void Window(Transform p, Vector3 center, bool alongX, float w, float h)
        {
            // leaded window: glass pane, oak frame, lead mullions, deep stone sill
            Vector3 gs = alongX ? new Vector3(w, h, 0.05f) : new Vector3(0.05f, h, w);
            Gfx.Box(p, center, gs, glass, 0, "Glass", false);
            float ft = 0.07f;
            Vector3 hs = alongX ? new Vector3(w + 0.1f, ft, 0.14f) : new Vector3(0.14f, ft, w + 0.1f);
            Gfx.Box(p, center + Vector3.up * (h / 2f), hs, oak, 0, "FrameTop", false);
            Gfx.Box(p, center - Vector3.up * (h / 2f), hs, oak, 0, "FrameBot", false);
            for (int i = -1; i <= 1; i++)
            {
                Vector3 vs = alongX ? new Vector3(0.04f, h, 0.07f) : new Vector3(0.07f, h, 0.04f);
                Vector3 off = alongX ? new Vector3(i * w / 3f, 0, 0) : new Vector3(0, 0, i * w / 3f);
                Gfx.Box(p, center + off, vs, black, 0, "Mullion", false);
            }
            for (int j = -1; j <= 1; j += 2)
            {
                Vector3 hs2 = alongX ? new Vector3(w, 0.03f, 0.07f) : new Vector3(0.07f, 0.03f, w);
                Gfx.Box(p, center + Vector3.up * (j * h / 6f), hs2, black, 0, "Transom", false);
            }
        }

        public static GameObject Build(Transform parent, Vector3 pos, float yaw)
        {
            Mats();
            var root = new GameObject("RoseCottage").transform;
            root.SetParent(parent, false); root.position = pos; root.rotation = Quaternion.Euler(0, yaw, 0);

            GameAudio.Interiors.Add(root);
            var rz = new GameObject("Reverb"); rz.transform.SetParent(root, false); rz.transform.localPosition = new Vector3(0, 1.2f, 0);
            var rev = rz.AddComponent<AudioReverbZone>(); rev.reverbPreset = AudioReverbPreset.Livingroom; rev.minDistance = 4.2f; rev.maxDistance = 6.5f;
            const float W = 9f, D = 5.5f, H = 3.3f, T = 0.5f;
            float hw = W / 2f;

            // --- ground floor slab, floorboards inside, step at door
            Gfx.Box(root, new Vector3(0, -0.15f, 0), new Vector3(W + 0.6f, 0.3f, D + 0.6f), stone, 2.5f, "Plinth");
            Gfx.Box(root, new Vector3(0, 0.03f, 0), new Vector3(W - 1f, 0.06f, D - 1f), plank, 2f, "Floor");

            // --- outer walls
            Wall(root, true, -hw, hw, -D / 2f + T / 2f, H, T, stone,
                new Op(1.5f, 1.1f, 0, 2.05f), new Op(-2.5f, 1.2f, 0.85f, 1.95f), new Op(3.6f, 1.2f, 0.85f, 1.95f));
            Wall(root, true, -hw, hw, D / 2f - T / 2f, H, T, stone,
                new Op(3.0f, 1.0f, 0, 2.0f), new Op(-1.8f, 1.2f, 0.85f, 1.95f), new Op(0.4f, 1.2f, 0.85f, 1.95f));
            Wall(root, false, -D / 2f, D / 2f, -hw + T / 2f, H, T, stone, new Op(0f, 1.8f, 0f, 1.45f));
            Wall(root, false, -D / 2f, D / 2f, hw - T / 2f, H, T, stone, new Op(0f, 1.2f, 0.85f, 1.95f));
            // partition between parlour and kitchen with doorway
            Wall(root, false, -D / 2f + T, D / 2f - T, -0.4f, 2.5f, 0.25f, plaster, new Op(0.2f, 1.0f, 0, 2.0f));

            // --- windows
            Window(root, new Vector3(-2.5f, 1.4f, -D / 2f + T / 2f), true, 1.2f, 1.1f);
            Window(root, new Vector3(3.6f, 1.4f, -D / 2f + T / 2f), true, 1.2f, 1.1f);
            Window(root, new Vector3(-1.8f, 1.4f, D / 2f - T / 2f), true, 1.2f, 1.1f);
            Window(root, new Vector3(0.4f, 1.4f, D / 2f - T / 2f), true, 1.2f, 1.1f);
            Window(root, new Vector3(hw - T / 2f, 1.4f, 0f), false, 1.2f, 1.1f);
            for (int i = 0; i < 4; i++)
            {
                float x = new[] { -2.5f, 3.6f }[i % 2]; // deep stone sills on the front
                if (i < 2) Gfx.Box(root, new Vector3(x, 0.83f, -D / 2f - 0.05f), new Vector3(1.4f, 0.08f, 0.3f), stone, 1.5f, "Sill");
            }

            // --- front door (oak, half-open) with porch
            var hinge = new GameObject("DoorHinge").transform; hinge.SetParent(root, false);
            hinge.localPosition = new Vector3(1.5f - 0.5f, 0, -D / 2f + 0.1f); hinge.localRotation = Quaternion.Euler(0, -100f, 0);
            Gfx.Box(hinge, new Vector3(0.5f, 1.0f, 0f), new Vector3(1.0f, 2.0f, 0.07f), oak, 0, "DoorLeaf");
            Gfx.Prim(PrimitiveType.Sphere, hinge, new Vector3(0.88f, 1.0f, -0.06f), Vector3.one * 0.07f, brass, "Knob", false);
            Gfx.Box(root, new Vector3(0.85f, 1.1f, -D / 2f - 0.75f), new Vector3(0.12f, 2.2f, 0.12f), oak, 0, "PorchPostL");
            Gfx.Box(root, new Vector3(2.15f, 1.1f, -D / 2f - 0.75f), new Vector3(0.12f, 2.2f, 0.12f), oak, 0, "PorchPostR");
            Gfx.Box(root, new Vector3(1.5f, 2.25f, -D / 2f - 0.75f), new Vector3(1.6f, 0.12f, 0.12f), oak, 0, "PorchBeam");
            var porch = Gfx.Box(root, new Vector3(1.5f, 2.55f, -D / 2f - 0.5f), new Vector3(1.9f, 0.1f, 1.5f), strawDark, 1.5f, "PorchRoof", false);
            porch.transform.localRotation = Quaternion.Euler(-12f, 0, 0);
            Gfx.Box(root, new Vector3(1.5f, 0.06f, -D / 2f - 0.6f), new Vector3(1.6f, 0.12f, 1.0f), stone, 1.5f, "DoorStep");

            // stable door at rear (split)
            Gfx.Box(root, new Vector3(3.0f, 0.55f, D / 2f - 0.15f), new Vector3(0.95f, 1.1f, 0.07f), oak, 0, "StableLower");
            Gfx.Box(root, new Vector3(3.0f, 1.55f, D / 2f - 0.05f), new Vector3(0.95f, 0.9f, 0.07f), oak, 0, "StableUpper", false);

            // --- gables, roof of thatch with rolled eaves + ridge, chimney
            float a = 36f * Mathf.Deg2Rad;
            float run = 3.3f, slabLen = run / Mathf.Cos(a), Hr = H + 2.3f;
            foreach (int sgn in new[] { -1, 1 })
            {
                var slab = Gfx.Box(root, new Vector3(0, Hr - run * Mathf.Tan(a) / 2f, sgn * run / 2f), new Vector3(W + 0.9f, 0.5f, slabLen), straw, 3f, "Thatch", false);
                slab.transform.localRotation = Quaternion.Euler(sgn * 36f, 0, 0);
                var roll = Gfx.Prim(PrimitiveType.Cylinder, root, new Vector3(0, H - 0.1f - 0.1f, sgn * run), new Vector3(0.5f, (W + 0.9f) / 2f, 0.5f), straw, "EaveRoll", false);
                roll.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            }
            var ridge = Gfx.Prim(PrimitiveType.Cylinder, root, new Vector3(0, Hr + 0.22f, 0), new Vector3(0.65f, (W + 1.0f) / 2f, 0.65f), strawDark, "Ridge", false);
            ridge.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            var gm = Gfx.GablePrism(D, 2.0f, T);
            foreach (int sgn in new[] { -1, 1 })
            {
                var g = new GameObject("Gable"); g.transform.SetParent(root, false);
                g.transform.localPosition = new Vector3(sgn * (hw - T / 2f), H, 0);
                g.transform.localRotation = Quaternion.Euler(0, 90f, 0);
                g.AddComponent<MeshFilter>().sharedMesh = gm;
                g.AddComponent<MeshRenderer>().sharedMaterial = stone;
                g.AddComponent<MeshCollider>();
            }

            // eyebrow dormers on front slope (thatch bulging over leaded windows)
            foreach (float x in new[] { -2.5f, 3.0f })
            {
                float s = 1.35f;
                Vector3 n = new Vector3(0, Mathf.Cos(a), -Mathf.Sin(a));
                Vector3 along = new Vector3(0, Mathf.Sin(a), Mathf.Cos(a)); // up-slope direction pointing +z/up? slope rises toward ridge (+z)
                Vector3 eave = new Vector3(x, H - 0.1f, -run);
                Vector3 onSlope = eave + along * s + n * 0.25f;
                var brow = Gfx.Prim(PrimitiveType.Sphere, root, onSlope + n * 0.1f, new Vector3(1.7f, 0.75f, 1.1f), straw, "Eyebrow", false);
                brow.transform.localRotation = Quaternion.Euler(-36f, 0, 0);
                var win = Gfx.Box(root, eave + along * (s - 0.35f) + n * 0.31f, new Vector3(0.9f, 0.6f, 0.06f), glass, 0, "DormerGlass", false);
                win.transform.localRotation = Quaternion.Euler(-36f, 0, 0);
                var fr = Gfx.Box(root, eave + along * (s - 0.35f) + n * 0.3f, new Vector3(1.0f, 0.7f, 0.04f), oak, 0, "DormerFrame", false);
                fr.transform.localRotation = Quaternion.Euler(-36f, 0, 0);
            }

            // chimney breast + stack with pots, hollow inglenook niche
            float cx = -hw - 0.65f;
            Gfx.Box(root, new Vector3(cx, 0.75f, -1.2f), new Vector3(1.3f, 1.5f, 0.4f), stone, 2.5f, "NicheSideS");
            Gfx.Box(root, new Vector3(cx, 0.75f, 1.2f), new Vector3(1.3f, 1.5f, 0.4f), stone, 2.5f, "NicheSideN");
            Gfx.Box(root, new Vector3(cx - 0.65f - 0.15f + 0.3f, 0.75f, 0f), new Vector3(0.3f, 1.5f, 2.4f), stone, 2.5f, "NicheBack");
            Gfx.Box(root, new Vector3(cx, 0.03f, 0f), new Vector3(1.3f, 0.06f, 2.0f), brick, 1.5f, "HearthFloor");
            Gfx.Box(root, new Vector3(cx, 1.5f + (H + 3.3f - 1.5f) / 2f, 0f), new Vector3(1.3f, H + 3.3f - 1.5f, 2.4f), stone, 2.5f, "ChimneyStack");
            Gfx.Box(root, new Vector3(-hw + 0.1f, 1.45f, 0f), new Vector3(0.35f, 0.25f, 2.2f), oak, 0, "InglenookBeam");
            Gfx.Prim(PrimitiveType.Cylinder, root, new Vector3(cx, H + 3.55f, -0.4f), new Vector3(0.4f, 0.3f, 0.4f), terracotta, "Pot", false);
            Gfx.Prim(PrimitiveType.Cylinder, root, new Vector3(cx, H + 3.55f, 0.4f), new Vector3(0.4f, 0.3f, 0.4f), terracotta, "Pot", false);
            // fire
            var fire = Gfx.Box(root, new Vector3(cx + 0.1f, 0.35f, 0f), new Vector3(0.6f, 0.5f, 0.9f), Gfx.Emissive(new Color(1f, 0.4f, 0.1f), 2.5f), 0, "Fire", false);
            for (int i = -1; i <= 1; i++)
            {
                var lg = Gfx.Prim(PrimitiveType.Cylinder, root, new Vector3(cx + 0.1f, 0.14f, i * 0.28f), new Vector3(0.14f, 0.45f, 0.14f), oak, "Log", false);
                lg.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            }
            var fl = new GameObject("FireLight"); fl.transform.SetParent(root, false); fl.transform.localPosition = new Vector3(cx + 0.7f, 0.9f, 0f);
            var lt = fl.AddComponent<Light>(); lt.type = LightType.Point; lt.color = new Color(1f, 0.55f, 0.25f); lt.range = 7f; lt.intensity = 3f;
            fl.AddComponent<FireFlicker>();
            var fsrc = fl.AddComponent<AudioSource>(); fsrc.clip = AudioSynth.Fire(); fsrc.loop = true; fsrc.spatialBlend = 1f; fsrc.minDistance = 1.5f; fsrc.maxDistance = 14f; fsrc.volume = 0.5f; fsrc.Play();
            Gfx.Box(root, new Vector3(cx + 0.1f, 1.0f, 0f), new Vector3(0.05f, 0.08f, 1.3f), black, 0, "FireCrane", false);

            // --- ceiling with oak beams (joists)
            Gfx.Box(root, new Vector3(0, 2.52f, 0), new Vector3(W - 1f, 0.06f, D - 1f), plaster, 0, "Ceiling");
            Gfx.Box(root, new Vector3(0, 2.4f, 0), new Vector3(W - 1f, 0.22f, 0.26f), oak, 0, "SpineBeam");
            for (float x = -3.6f; x <= 3.7f; x += 1.2f)
                Gfx.Box(root, new Vector3(x, 2.4f, 0), new Vector3(0.16f, 0.2f, D - 1f), oak, 0, "Joist", false);

            // --- parlour (x<-0.4): vintage furnishings
            Gfx.Box(root, new Vector3(-2.2f, 0.07f, 0.2f), new Vector3(2.6f, 0.02f, 1.8f), rug, 0, "Rug", false);
            Armchair(root, new Vector3(-2.3f, 0f, 1.6f), 180f);
            Armchair(root, new Vector3(-2.3f, 0f, -1.3f), 0f);
            GrandfatherClock(root, new Vector3(-0.8f, 0f, 2.1f));
            Bookcase(root, new Vector3(-3.2f, 0f, 2.15f), 2.0f);
            Picture(root, new Vector3(-hw + 0.28f, 1.7f, 1.8f), 90f);
            Gfx.Prim(PrimitiveType.Cylinder, root, new Vector3(-3.1f, 0.78f, -1.7f), new Vector3(0.1f, 0.1f, 0.1f), gold, "Candlestick", false);
            Gfx.Box(root, new Vector3(-3.1f, 0.35f, -1.7f), new Vector3(0.6f, 0.7f, 0.5f), oak, 0, "SideTable");
            Gfx.Box(root, new Vector3(-3.1f, 0.74f, -1.45f), new Vector3(0.3f, 0.12f, 0.2f), gold, 0, "JewelleryBox", false);

            // --- kitchen (x>-0.4)
            Gfx.Box(root, new Vector3(2.0f, 0.4f, 0.8f), new Vector3(1.8f, 0.8f, 0.9f), oak, 0, "ScrubbedTable");
            for (int i = -1; i <= 1; i += 2) Gfx.Box(root, new Vector3(2.0f + i * 0.5f, 0.25f, 1.55f), new Vector3(0.4f, 0.5f, 0.4f), oak, 0, "Chair");
            Dresser(root, new Vector3(3.5f, 0f, 2.1f));
            Gfx.Box(root, new Vector3(0.8f, 0.55f, 2.1f), new Vector3(1.1f, 1.1f, 0.7f), black, 0, "Range");
            Gfx.Prim(PrimitiveType.Cylinder, root, new Vector3(0.8f, 2.0f, 2.3f), new Vector3(0.2f, 0.9f, 0.2f), black, "RangeFlue", false);
            Gfx.Box(root, new Vector3(3.9f, 0.45f, -1.2f), new Vector3(0.5f, 0.9f, 1.2f), white, 0, "BelfastSink");
            Gfx.Box(root, new Vector3(3.55f, 1.2f, -1.2f), new Vector3(0.03f, 0.1f, 0.1f), brass, 0, "Tap", false);

            return root.gameObject;
        }

        static void Armchair(Transform p, Vector3 pos, float yaw)
        {
            var g = new GameObject("Armchair").transform; g.SetParent(p, false); g.localPosition = pos; g.localRotation = Quaternion.Euler(0, yaw, 0);
            Gfx.Box(g, new Vector3(0, 0.25f, 0), new Vector3(0.8f, 0.4f, 0.8f), cloth, 0, "Seat");
            Gfx.Box(g, new Vector3(0, 0.65f, -0.35f), new Vector3(0.8f, 0.7f, 0.15f), cloth, 0, "Back");
            Gfx.Box(g, new Vector3(-0.4f, 0.45f, 0), new Vector3(0.12f, 0.4f, 0.8f), cloth, 0, "ArmL");
            Gfx.Box(g, new Vector3(0.4f, 0.45f, 0), new Vector3(0.12f, 0.4f, 0.8f), cloth, 0, "ArmR");
        }

        static void GrandfatherClock(Transform p, Vector3 pos)
        {
            var g = new GameObject("GrandfatherClock").transform; g.SetParent(p, false); g.localPosition = pos;
            Gfx.Box(g, new Vector3(0, 0.95f, 0), new Vector3(0.45f, 1.9f, 0.3f), oak, 0, "Case");
            Gfx.Prim(PrimitiveType.Cylinder, g, new Vector3(0, 1.55f, -0.16f), new Vector3(0.32f, 0.01f, 0.32f), white, "Face", false).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            Gfx.Box(g, new Vector3(0, 0.8f, -0.16f), new Vector3(0.12f, 0.6f, 0.02f), brass, 0, "Pendulum", false);
        }

        static void Bookcase(Transform p, Vector3 pos, float w)
        {
            var g = new GameObject("Bookcase").transform; g.SetParent(p, false); g.localPosition = pos;
            Gfx.Box(g, new Vector3(0, 1.0f, 0), new Vector3(w, 2.0f, 0.35f), oak, 0, "Frame");
            var rng = new System.Random(7);
            for (int s = 0; s < 4; s++)
            {
                float x = -w / 2f + 0.1f;
                while (x < w / 2f - 0.15f)
                {
                    float bw = 0.05f + (float)rng.NextDouble() * 0.05f, bh = 0.22f + (float)rng.NextDouble() * 0.12f;
                    var c = Color.HSVToRGB((float)rng.NextDouble(), 0.5f, 0.35f + (float)rng.NextDouble() * 0.3f);
                    Gfx.Box(g, new Vector3(x + bw / 2f, 0.28f + s * 0.45f + bh / 2f, -0.04f), new Vector3(bw, bh, 0.22f), Gfx.Mat(c), 0, "Book", false);
                    x += bw + 0.005f;
                }
            }
        }

        static void Picture(Transform p, Vector3 pos, float yaw)
        {
            var g = new GameObject("OilPainting").transform; g.SetParent(p, false); g.localPosition = pos; g.localRotation = Quaternion.Euler(0, yaw, 0);
            Gfx.Box(g, Vector3.zero, new Vector3(0.9f, 0.65f, 0.06f), gold, 0, "GiltFrame", false);
            Gfx.Box(g, new Vector3(0, 0, -0.01f), new Vector3(0.78f, 0.53f, 0.06f), paint, 0, "Canvas", false);
        }

        static void Dresser(Transform p, Vector3 pos)
        {
            var g = new GameObject("WelshDresser").transform; g.SetParent(p, false); g.localPosition = pos;
            Gfx.Box(g, new Vector3(0, 0.5f, 0), new Vector3(1.6f, 1.0f, 0.5f), oak, 0, "Base");
            Gfx.Box(g, new Vector3(0, 1.7f, 0.1f), new Vector3(1.6f, 1.4f, 0.3f), oak, 0, "Hutch");
            for (int s = 0; s < 2; s++)
                for (int i = -2; i <= 2; i++)
                {
                    var pl = Gfx.Prim(PrimitiveType.Cylinder, g, new Vector3(i * 0.3f, 1.45f + s * 0.5f, -0.07f), new Vector3(0.24f, 0.01f, 0.24f), white, "Plate", false);
                    pl.transform.localRotation = Quaternion.Euler(80f, 0, 0);
                }
        }
    }

    public class FireFlicker : MonoBehaviour
    {
        Light l; float seed;
        void Start() { l = GetComponent<Light>(); seed = Random.value * 100f; }
        void Update() { l.intensity = 2.6f + Mathf.PerlinNoise(Time.time * 6f, seed) * 1.6f; }
    }
}

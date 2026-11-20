using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ASBO.Slice
{
    /// <summary>Builds the Abbots Wend vertical slice at Play: long road, gated estate, cottages, car, people.</summary>
    public class World : MonoBehaviour
    {
        public static Transform Root;
        public const float RoadZ = -30f;
        public const float LaneZ = -27.8f; // lane the ambulance uses
        static World inst;
        Car car;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (inst != null) return;
            var g = new GameObject("ASBO_Slice"); inst = g.AddComponent<World>();
            DontDestroyOnLoad(g);
        }

        void Start() { Rebuild(); }

        void Rebuild()
        {
            if (Root != null) Destroy(Root.gameObject);
            Gore.Reset(); Ambulance.ResetAll(); Person.All.Clear();
            Root = new GameObject("SliceWorld").transform;
            var sun = FindFirstObjectByType<Light>();
            if (sun != null && sun.type == LightType.Directional) { sun.transform.rotation = Quaternion.Euler(48f, -35f, 0); sun.shadows = LightShadows.Soft; sun.intensity = 1.25f; }
            GameAudio.Create(Root);
            BuildGround(); BuildRoad(); BuildEstate();
            BuildCars(); BuildPeople();
        }

        void BuildGround()
        {
            var grass = Gfx.Mat(Color.white, Gfx.GrassTex(1), 0.02f);
            var g = Gfx.Box(Root, new Vector3(0, -0.5f, 0), new Vector3(500, 1, 500), grass, 0, "Ground");
            g.GetComponent<Renderer>().sharedMaterial.SetTextureScale("_BaseMap", new Vector2(250, 250));
        }

        void BuildRoad()
        {
            var asphalt = Gfx.Mat(new Color(0.13f, 0.13f, 0.14f), null, 0.15f);
            var line = Gfx.Mat(new Color(0.9f, 0.9f, 0.85f), null, 0.1f);
            var kerb = Gfx.Mat(new Color(0.55f, 0.55f, 0.55f), null, 0.1f);
            Gfx.Box(Root, new Vector3(0, 0.02f, RoadZ), new Vector3(500, 0.04f, 9f), asphalt, 0, "LongRoad");
            for (float x = -240; x < 240; x += 6f) Gfx.Box(Root, new Vector3(x, 0.05f, RoadZ), new Vector3(2.5f, 0.01f, 0.15f), line, 0, "Dash", false);
            foreach (int s in new[] { -1, 1 })
            {
                Gfx.Box(Root, new Vector3(0, 0.05f, RoadZ + s * 4.3f), new Vector3(500, 0.01f, 0.12f), line, 0, "EdgeLine", false);
                Gfx.Box(Root, new Vector3(0, 0.06f, RoadZ + s * 6.1f), new Vector3(500, 0.12f, 3f), kerb, 0, "Pavement");
            }
            // zebra crossing at x=16..20
            for (int i = 0; i < 9; i++) Gfx.Box(Root, new Vector3(18f, 0.05f, RoadZ - 3.6f + i * 0.9f), new Vector3(3f, 0.01f, 0.45f), line, 0, "Zebra", false);
            // approach road to the estate gate
            Gfx.Box(Root, new Vector3(0, 0.025f, -22f), new Vector3(6f, 0.05f, 6.5f), asphalt, 0, "Approach");
        }

        void BuildEstate()
        {
            var stone = Gfx.Mat(Color.white, Gfx.StoneTex(11), 0.05f);
            var gravel = Gfx.Mat(Color.white, Gfx.GravelTex(4), 0.05f);
            var hedge = Gfx.Mat(new Color(0.1f, 0.3f, 0.1f), null, 0.02f);
            var iron = Gfx.Mat(new Color(0.06f, 0.06f, 0.07f), null, 0.5f, 0.7f);
            var gold = Gfx.Mat(new Color(0.95f, 0.75f, 0.3f), null, 0.8f, 1f);
            var estate = new GameObject("AbbotsWend").transform; estate.SetParent(Root, false);
            float x0 = -34, x1 = 34, z0 = -19, z1 = 46;
            // perimeter wall (gap at gate)
            Gfx.Box(estate, new Vector3((x0 - 3.5f) / 2f - 0.0f, 0.9f, z0), new Vector3(-3.5f - x0, 1.8f, 0.5f), stone, 2.5f, "WallSW");
            Gfx.Box(estate, new Vector3((x1 + 3.5f) / 2f, 0.9f, z0), new Vector3(x1 - 3.5f, 1.8f, 0.5f), stone, 2.5f, "WallSE");
            Gfx.Box(estate, new Vector3((x0 + x1) / 2f, 0.9f, z1), new Vector3(x1 - x0, 1.8f, 0.5f), stone, 2.5f, "WallN");
            Gfx.Box(estate, new Vector3(x0, 0.9f, (z0 + z1) / 2f), new Vector3(0.5f, 1.8f, z1 - z0), stone, 2.5f, "WallW");
            Gfx.Box(estate, new Vector3(x1, 0.9f, (z0 + z1) / 2f), new Vector3(0.5f, 1.8f, z1 - z0), stone, 2.5f, "WallE");
            // gate: pillars with lamps and open iron gates
            foreach (int s in new[] { -1, 1 })
            {
                Gfx.Box(estate, new Vector3(s * 3.7f, 1.3f, z0), new Vector3(0.9f, 2.6f, 0.9f), stone, 2f, "Pillar");
                Gfx.Box(estate, new Vector3(s * 3.7f, 2.7f, z0), new Vector3(1.1f, 0.15f, 1.1f), stone, 2f, "PillarCap", false);
                Gfx.Prim(PrimitiveType.Sphere, estate, new Vector3(s * 3.7f, 3.0f, z0), Vector3.one * 0.4f, Gfx.Emissive(new Color(1f, 0.9f, 0.6f), 1.5f), "PillarLamp", false);
                var gate = new GameObject("GateLeaf").transform; gate.SetParent(estate, false);
                gate.localPosition = new Vector3(s * 3.2f, 0, z0); gate.localRotation = Quaternion.Euler(0, s * -80f, 0);
                for (int i = 0; i < 12; i++) Gfx.Box(gate, new Vector3(-s * (0.15f + i * 0.2f), 1.1f, 0), new Vector3(0.035f, 2.0f, 0.035f), iron, 0, "Bar", false);
                Gfx.Box(gate, new Vector3(-s * 1.3f, 0.3f, 0), new Vector3(2.6f, 0.06f, 0.05f), iron, 0, "RailLow", false);
                Gfx.Box(gate, new Vector3(-s * 1.3f, 1.95f, 0), new Vector3(2.6f, 0.06f, 0.05f), iron, 0, "RailHigh", false);
                Gfx.Prim(PrimitiveType.Sphere, gate, new Vector3(-s * 1.3f, 1.15f, 0), Vector3.one * 0.2f, gold, "Crest", false);
            }
            // gravel drive to the front door of Rose Cottage and a loop
            Gfx.Box(estate, new Vector3(0.7f, 0.04f, -4f), new Vector3(3.2f, 0.07f, 31f), gravel, 3f, "Drive", false);
            Gfx.Box(estate, new Vector3(0, 0.04f, -7f), new Vector3(14f, 0.07f, 3f), gravel, 3f, "DriveLoop", false);
            // lawns/hedges around the main garden
            foreach (int s in new[] { -1, 1 })
            {
                Gfx.Box(estate, new Vector3(s * 11f, 0.6f, 3f), new Vector3(0.9f, 1.2f, 22f), hedge, 0, "HedgeSide");
            }
            Gfx.Box(estate, new Vector3(-6.5f, 0.6f, -8f), new Vector3(9f, 1.2f, 0.9f), hedge, 0, "HedgeFrontL");
            Gfx.Box(estate, new Vector3(8.5f, 0.6f, -8f), new Vector3(5f, 1.2f, 0.9f), hedge, 0, "HedgeFrontR");
            Gfx.Box(estate, new Vector3(0, 0.6f, 14f), new Vector3(22f, 1.2f, 0.9f), hedge, 0, "HedgeBack");
            // flower beds
            var soil = Gfx.Mat(new Color(0.22f, 0.13f, 0.07f), null, 0.05f);
            var rng = new System.Random(5);
            foreach (var bx in new[] { -4.5f, 5.5f })
            {
                Gfx.Box(estate, new Vector3(bx, 0.1f, -3.2f), new Vector3(3.2f, 0.2f, 1.0f), soil, 0, "Bed", false);
                for (int i = 0; i < 14; i++)
                    Gfx.Prim(PrimitiveType.Sphere, estate, new Vector3(bx - 1.5f + i * 0.23f, 0.35f, -3.2f + (float)(rng.NextDouble() - 0.5) * 0.6f), Vector3.one * 0.2f,
                        Gfx.Mat(Color.HSVToRGB((float)rng.NextDouble(), 0.7f, 0.9f)), "Flower", false);
            }
            // cottages
            Cottage.Build(estate, new Vector3(0, 0, 6f), 0f);
            Cottage.Build(estate, new Vector3(-24f, 0, 22f), -90f);
            Cottage.Build(estate, new Vector3(24f, 0, 24f), 90f);
            // pond + trees + lamps
            var water = Gfx.Mat(new Color(0.1f, 0.25f, 0.3f), null, 0.95f);
            Gfx.Prim(PrimitiveType.Cylinder, estate, new Vector3(-14f, 0.03f, -2f), new Vector3(5f, 0.03f, 5f), water, "Pond", false);
            var trunk = Gfx.Mat(new Color(0.25f, 0.17f, 0.1f), null, 0.05f);
            var leaf = new[] { Gfx.Mat(new Color(0.15f, 0.4f, 0.12f)), Gfx.Mat(new Color(0.2f, 0.48f, 0.15f)), Gfx.Mat(new Color(0.55f, 0.4f, 0.1f)) };
            for (int i = 0; i < 26; i++)
            {
                float x = (float)(rng.NextDouble() * 60 - 30), z = (float)(rng.NextDouble() * 60 - 14);
                if (Mathf.Abs(x) < 6f && z < 16f && z > -20f) continue;
                if (Vector2.Distance(new Vector2(x, z), new Vector2(0, 6)) < 8f) continue;
                if (Vector2.Distance(new Vector2(x, z), new Vector2(-24, 22)) < 8f || Vector2.Distance(new Vector2(x, z), new Vector2(24, 24)) < 8f) continue;
                if (Vector2.Distance(new Vector2(x, z), new Vector2(-14, -2)) < 4f) continue;
                float h = 3f + (float)rng.NextDouble() * 2f;
                Gfx.Prim(PrimitiveType.Cylinder, estate, new Vector3(x, h / 2f, z), new Vector3(0.4f, h / 2f, 0.4f), trunk, "Trunk");
                var m = leaf[rng.Next(leaf.Length)];
                Gfx.Prim(PrimitiveType.Sphere, estate, new Vector3(x, h + 1.0f, z), new Vector3(3.6f, 3f, 3.6f), m, "Crown", false);
                Gfx.Prim(PrimitiveType.Sphere, estate, new Vector3(x + 0.8f, h + 0.5f, z + 0.5f), new Vector3(2.4f, 2f, 2.4f), m, "Crown2", false);
            }
            foreach (float z in new[] { -17f, -9f, -1f })
                foreach (int s in new[] { -1, 1 })
                {
                    Gfx.Box(estate, new Vector3(s * 2.8f, 1.2f, z), new Vector3(0.08f, 2.4f, 0.08f), iron, 0, "LampPost");
                    Gfx.Prim(PrimitiveType.Sphere, estate, new Vector3(s * 2.8f, 2.5f, z), Vector3.one * 0.3f, Gfx.Emissive(new Color(1f, 0.9f, 0.6f), 1.2f), "Lamp", false);
                }
        }

        void BuildCars()
        {
            car = Car.Create(Root, new Vector3(-14f, 0.7f, -26.9f), 90f, new Color(0.55f, 0.08f, 0.1f));
            var pl = MakePerson(new Vector3(-17f, 0, -23.9f), new Color(0.2f, 0.4f, 0.8f), new Color(0.12f, 0.12f, 0.2f), "Player", true);
            pl.transform.rotation = Quaternion.Euler(0, 90f, 0);
            pl.gameObject.AddComponent<PlayerCtl>().Setup(car);
        }

        void BuildPeople()
        {
            // pedestrians crossing the long road (some at the zebra, some jaywalking)
            Walker(new Vector3(18f, 0, RoadZ - 7f), new Vector3(18f, 0, RoadZ + 7f), new Color(0.8f, 0.2f, 0.2f), false);
            Walker(new Vector3(30f, 0, RoadZ + 7f), new Vector3(30f, 0, RoadZ - 7f), new Color(0.2f, 0.6f, 0.3f), false);
            Walker(new Vector3(44f, 0, RoadZ - 7f), new Vector3(44f, 0, RoadZ + 7f), new Color(0.9f, 0.8f, 0.2f), true);  // important: survives
            Walker(new Vector3(58f, 0, RoadZ + 7f), new Vector3(58f, 0, RoadZ - 7f), new Color(0.6f, 0.3f, 0.7f), false);
            // two people standing in the car's lane for testing: an ordinary passer-by and an important one
            var s1 = MakePerson(new Vector3(24f, 0, -26.9f), new Color(0.75f, 0.15f, 0.15f), new Color(0.15f, 0.15f, 0.25f), "Pedestrian");
            s1.transform.rotation = Quaternion.Euler(0, 180f, 0);
            var s2 = MakePerson(new Vector3(52f, 0, -26.9f), new Color(0.95f, 0.8f, 0.15f), new Color(0.15f, 0.15f, 0.25f), "Important");
            s2.important = true; s2.transform.rotation = Quaternion.Euler(0, 180f, 0);
            // estate residents strolling the garden
            var r = MakePerson(new Vector3(-6f, 0, -8f), new Color(0.35f, 0.45f, 0.3f), new Color(0.3f, 0.25f, 0.2f), "Resident");
            r.route.AddRange(new[] { new Vector3(-6f, 0, -12f), new Vector3(6f, 0, -12f), new Vector3(6f, 0, -6f), new Vector3(-6f, 0, -6f) });
            r.speed = 1.0f;
        }

        void Walker(Vector3 a, Vector3 b, Color shirt, bool important)
        {
            var p = MakePerson(a, shirt, new Color(0.15f, 0.15f, 0.25f), important ? "Important" : "Pedestrian");
            p.important = important; p.route.Add(b); p.route.Add(a); p.speed = Random.Range(1.1f, 1.5f);
        }

        public static Person MakePerson(Vector3 pos, Color shirt, Color trousers, string label, bool player = false)
        {
            var g = new GameObject(label); g.transform.SetParent(Root, false); g.transform.position = pos;
            var p = g.AddComponent<Person>();
            p.shirt = shirt; p.trousers = trousers; p.label = label; p.isPlayer = player;
            p.Init();
            return p;
        }

        void Update()
        {
            var kb = Keyboard.current; if (kb == null) return;
            if (kb.rKey.wasPressedThisFrame) Rebuild();
            if (kb.gKey.wasPressedThisFrame) Gore.Level = (Gore.Level + 1) % 3;
        }

        void OnGUI()
        {
            string lvl = Gore.Level == 2 ? "Full" : Gore.Level == 1 ? "Reduced" : "Off";
            GUI.Label(new Rect(12, 8, 700, 120), "ASBO — Abbots Wend slice   WASD move/drive • Shift run • E enter/exit car • Space jump/brake • R reset • G gore: " + lvl);
            if (!string.IsNullOrEmpty(Ambulance.Status)) GUI.Label(new Rect(12, 28, 700, 30), Ambulance.Status);
            GUI.Label(new Rect(12, 46, 700, 30), Mathf.RoundToInt(1f / Time.smoothDeltaTime) + " fps");
        }
    }
}

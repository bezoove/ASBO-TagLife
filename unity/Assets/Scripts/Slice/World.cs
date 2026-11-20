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
        public const int CamBlockLayer = 31;   // collides with nothing; only the camera deoccluder sees it
        static World inst;
        Car car;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            for (int i = 0; i < 32; i++) Physics.IgnoreLayerCollision(CamBlockLayer, i, true);
            if (inst != null || FindFirstObjectByType<World>() != null) return;
            var g = new GameObject("ASBO_Slice"); inst = g.AddComponent<World>();
            DontDestroyOnLoad(g);
        }

        void Start() { Rebuild(); }

        void Rebuild()
        {
            foreach (var go in GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None)) if (go != null && go.parent == null && go.name == "SliceWorld") DestroyImmediate(go.gameObject);
            Root = null;
            Gore.Reset(); Ambulance.ResetAll(); Person.All.Clear(); FreePackTest.Reset();
            Root = new GameObject("SliceWorld").transform;
            bool baked = FindFirstObjectByType<BakedWorld>() != null;
            if (!baked)
            {
                // no saved scene: build the static world procedurally (fallback / sandbox)
                Look.Apply(Root);
                GameAudio.Create(Root);
                BuildGround(Root); BuildStatic(Root);
                Physics.SyncTransforms();
                Nav.Build(new Bounds(new Vector3(0, 4f, 10f), new Vector3(240f, 12f, 96f)));
            }
            else
            {
                // the Abbots Wend scene holds the static world (prefabs, terrain, baked lighting, baked NavMesh)
                Look.ApplyPost(Root);
                GameAudio.Create(Root);
                Estate.Attach();
                Nav.UseBaked();
            }
            Traffic.Spawn(Root);
            BuildCars(); BuildPeople();
        }

        public static void BuildStatic(Transform root)
        {
            BuildRoad(root); Estate.Build(root);
        }

        static void BuildGround(Transform Root)
        {
            var grass = Gfx.Mat(Color.white, Gfx.GrassTex(1), 0.02f);
            var g = Gfx.Box(Root, new Vector3(0, -0.5f, 0), new Vector3(500, 1, 500), grass, 0, "Ground");
            g.GetComponent<Renderer>().sharedMaterial.SetTextureScale("_BaseMap", new Vector2(250, 250));
        }

        static void BuildRoad(Transform Root)
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
            Gfx.Box(Root, new Vector3(0, 0.065f, -23.75f), new Vector3(6f, 0.13f, 3.6f), asphalt, 0, "Approach");
        }

        void BuildCars()
        {
            car = FreePackTest.On ? null : Car.Create(Root, new Vector3(-14f, 0.6f, -26.3f), 90f, new Color(0.55f, 0.08f, 0.1f));
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
            // residents of each cottage
            var rr = new System.Random(9);
            foreach (var h in Estate.Houses)
            {
                int n = h.pl.big || h.P.rooms.Count > 12 ? 2 : 1 + rr.Next(2);
                for (int q = 0; q < n; q++) Resident.Spawn(h, q, rr);
            }
            // estate residents strolling the garden
            var r = MakePerson(new Vector3(-20f, 0, Estate.LaneZ), new Color(0.35f, 0.45f, 0.3f), new Color(0.3f, 0.25f, 0.2f), "Resident");
            r.route.AddRange(new[] { new Vector3(30f, 0, Estate.LaneZ), new Vector3(0f, 0, Estate.LaneZ), new Vector3(0f, 0, 5f), new Vector3(0f, 0, Estate.LaneZ), new Vector3(-30f, 0, Estate.LaneZ) });
            r.speed = 1.0f;
        }

        void Walker(Vector3 a, Vector3 b, Color shirt, bool important)
        {
            var p = MakePerson(a, shirt, new Color(0.15f, 0.15f, 0.25f), important ? "Important" : "Pedestrian");
            p.important = important; p.route.Add(b); p.route.Add(a); p.speed = Random.Range(1.1f, 1.5f);
        }

        public static Person MakePerson(Vector3 pos, Color shirt, Color trousers, string label, bool player = false, string model = null)
        {
            var g = new GameObject(label); g.transform.SetParent(Root, false); g.transform.position = pos;
            var p = g.AddComponent<Person>();
            p.shirt = shirt; p.trousers = trousers; p.label = label; p.isPlayer = player; p.model = model;
            p.Init();
            return p;
        }

        void Update()
        {
            var kb = Keyboard.current; if (kb == null) return;
            if (kb.rKey.wasPressedThisFrame) Rebuild();
            if (kb.gKey.wasPressedThisFrame) Gore.Level = (Gore.Level + 1) % 3;
            // test keys: 1..0 teleport to each cottage's front door; Shift+number puts you inside the hall; T = zebra crossing
            var pc = FindFirstObjectByType<PlayerCtl>();
            if (pc != null && kb.tKey.wasPressedThisFrame) { pc.Teleport(new Vector3(12f, 0.2f, RoadZ + 6.2f), 110f, 10f); lastHouse = "Zebra crossing"; }
            for (int i = 0; i < 10 && pc != null; i++)
            {
                var key = i == 9 ? kb.digit0Key : (UnityEngine.InputSystem.Controls.KeyControl)kb[(Key)((int)Key.Digit1 + i)];
                if (!key.wasPressedThisFrame || i >= Estate.Houses.Count) continue;
                var h = Estate.Houses[i];
                Vector3 door = h.root.TransformPoint(h.FrontDoorLocal);
                Vector3 inward = (h.root.TransformPoint(h.FrontDoorLocal + new Vector3(0, 0, 3f)) - door).normalized;
                if (h.FrontDoorLocal == Vector3.zero) inward = h.root.forward;
                bool inside = kb.leftShiftKey.isPressed;
                Vector3 p = inside ? h.root.TransformPoint(h.MainRoomLocal()) : door - inward * 4.5f;
                pc.Teleport(p + Vector3.up * 0.1f, Quaternion.LookRotation(inside ? inward : inward).eulerAngles.y, inside ? 8f : 12f);
                lastHouse = h.pl.name;
            }
        }
        string lastHouse = "";
        void UpdateDummy()
        {
        }

        void OnGUI()
        {
            string lvl = Gore.Level == 2 ? "Full" : Gore.Level == 1 ? "Reduced" : "Off";
            GUI.Label(new Rect(12, 8, 700, 120), "ASBO — Abbots Wend slice   WASD move/drive • Shift run • E enter/exit car • Space jump/brake • R reset • G gore: " + lvl);
            if (!string.IsNullOrEmpty(Ambulance.Status)) GUI.Label(new Rect(12, 28, 700, 30), Ambulance.Status);
            GUI.Label(new Rect(12, 46, 900, 30), Mathf.RoundToInt(1f / Time.smoothDeltaTime) + " fps   1-0: visit cottage (Shift = inside)   " + lastHouse + "   " + Resident.Stats() + "  traffic " + TrafficCar.All.Count);
        }
    }
}

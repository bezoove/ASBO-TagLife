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
            if (Application.isEditor) Application.runInBackground = true; // keep playing while the editor is unfocused (scripted captures)
            if (Application.isMobilePlatform) { Application.targetFrameRate = 60; Screen.sleepTimeout = SleepTimeout.NeverSleep; }
            // a slow frame never runs more than five physics steps to catch up (more makes the next frame slower still: a spiral)
            Time.maximumDeltaTime = 0.1f;
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
                GameAudio.Create(Root); Perf.Create(Root); Social.Create(Root); GameClock.Create(Root); Tag.Create(Root);
                BuildGround(Root); BuildStatic(Root);
                Physics.SyncTransforms();
                Dressing.Place(Root);
                Nav.Build(new Bounds(new Vector3(0, 4f, 10f), new Vector3(240f, 12f, 96f)));
            }
            else
            {
                // the Abbots Wend scene holds the static world (prefabs, terrain, baked lighting, baked NavMesh)
                Look.ApplyPost(Root);
                GameAudio.Create(Root); Perf.Create(Root); Social.Create(Root); GameClock.Create(Root); Tag.Create(Root);
                Estate.Attach(); TudorManor.Attach();
                Dressing.Place(Root);
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
            Surfaces.Box(Root, new Vector3(0, 0.02f, RoadZ), new Vector3(500, 0.04f, 9f), "asphalt_02", asphalt, "LongRoad", true, new Color(0.78f, 0.78f, 0.8f));
            for (float x = -240; x < 240; x += 6f) Gfx.Box(Root, new Vector3(x, 0.05f, RoadZ), new Vector3(2.5f, 0.01f, 0.15f), line, 0, "Dash", false);
            foreach (int s in new[] { -1, 1 })
            {
                Gfx.Box(Root, new Vector3(0, 0.05f, RoadZ + s * 4.3f), new Vector3(500, 0.01f, 0.12f), line, 0, "EdgeLine", false);
                Surfaces.Box(Root, new Vector3(0, 0.06f, RoadZ + s * 6.1f), new Vector3(500, 0.12f, 3f), "large_grey_tiles", kerb, "Pavement", true, new Color(0.9f, 0.9f, 0.88f));
                // concrete kerb stones along the road edge
                var k = Surfaces.Box(Root, new Vector3(0, 0.065f, RoadZ + s * 4.68f), new Vector3(500, 0.13f, 0.16f), "concrete_layers", kerb, "Kerb", false, new Color(1.25f, 1.24f, 1.22f));
                k.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // zebra crossing at x=16..20
            for (int i = 0; i < 9; i++) Gfx.Box(Root, new Vector3(18f, 0.05f, RoadZ - 3.6f + i * 0.9f), new Vector3(3f, 0.01f, 0.45f), line, 0, "Zebra", false);
            // approach road to the estate gate
            Surfaces.Box(Root, new Vector3(0, 0.065f, -23.75f), new Vector3(6f, 0.13f, 3.6f), "asphalt_02", asphalt, "Approach", true, new Color(0.78f, 0.78f, 0.8f));
        }

        void BuildCars()
        {
            car = FreePackTest.On ? null : Car.Create(Root, new Vector3(-14f, 0.6f, -26.3f), 90f, new Color(0.55f, 0.08f, 0.1f));
            if (FreePackTest.On)
            {
                // the player's car drives like the old game's: our arcade car, parked where the RCC demo car stood
                var rccCar = FindFirstObjectByType<RccVehicle>();
                Vector3 at = rccCar != null ? rccCar.transform.position : new Vector3(-14f, 0f, -26.3f); float yaw = rccCar != null ? rccCar.transform.eulerAngles.y : 90f;
                if (rccCar != null) rccCar.gameObject.SetActive(false);
                if (Physics.Raycast(at + Vector3.up * 2f, Vector3.down, out var gh, 5f, ~((1 << 2) | (1 << 31)), QueryTriggerInteraction.Ignore)) at.y = gh.point.y;
                PlayerCar.Create(at, yaw, new Color(0.1f, 0.2f, 0.42f)).transform.SetParent(Root, true);
            }
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
            CouncilEstate.Locals(); // people out on Hawthorn Way, if the scene has it
            ConstructionSite.Builders();
            CasinoHall.People(); // staff and regulars at the Silver Sixpence
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
            if (pcCache == null) pcCache = FindFirstObjectByType<PlayerCtl>(); // once, not every frame
            var pc = pcCache;
            if (pc != null && kb.f8Key.wasPressedThisFrame) { pc.Teleport(new Vector3(12f, 0.2f, RoadZ + 6.2f), 110f, 10f); lastHouse = "Zebra crossing"; }
            for (int i = 0; i < 10 && pc != null && !DialogueUI.Open; i++)
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

        public static bool HideHud;
        PlayerCtl pcCache; static GUIStyle cashStyle;

        // ---- messages across the top (what people shout, what just happened) and fades to black
        struct ToastLine { public string text; public int kind; public float until; }
        static readonly System.Collections.Generic.List<ToastLine> toasts = new System.Collections.Generic.List<ToastLine>();
        /// <summary>A line across the top for a few seconds. kind: 0 note, 1 warning, 2 good.</summary>
        public static void Toast(string text, int kind = 0)
        {
            if (string.IsNullOrEmpty(text)) return;
            Debug.Log("[ASBO] toast: " + text);
            toasts.Add(new ToastLine { text = text, kind = kind, until = Time.unscaledTime + 3.2f + text.Length * 0.03f });
            while (toasts.Count > 4) toasts.RemoveAt(0);
        }
        static float fade, fadeTarget, fadeSpeed = 2f, fadeHoldUntil = -1f;
        /// <summary>Fade the screen to black (1) or back (0) over secs; with hold > 0, stay dark that long and fade back.</summary>
        public static void FadeTo(float target, float secs, float hold = 0f)
        {
            fadeTarget = target; fadeSpeed = 1f / Mathf.Max(0.05f, secs);
            fadeHoldUntil = hold > 0f ? Time.unscaledTime + secs + hold : -1f;
        }
        static GUIStyle toastStyle;
        string hud1 = "", hud3 = ""; float hudT;
        static Fighter pfCache;
        static Fighter PlayerFighter { get { if (pfCache == null) { var pc = FindFirstObjectByType<PlayerCtl>(); if (pc != null) pfCache = pc.GetComponent<Fighter>(); } return pfCache; } }
        void OnGUI()
        {
            if (Event.current.type == EventType.Repaint)
            {
                if (fadeHoldUntil > 0f && Time.unscaledTime > fadeHoldUntil) { fadeTarget = 0f; fadeHoldUntil = -1f; }
                fade = Mathf.MoveTowards(fade, fadeTarget, fadeSpeed * Time.unscaledDeltaTime);
            }
            if (fade > 0.001f) { var oc = GUI.color; GUI.color = new Color(0f, 0f, 0f, fade); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = oc; }
            if (HideHud || TouchControls.Editing) return;
            if (toasts.Count > 0)
            {
                toastStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(16, Screen.height / 38), fontStyle = FontStyle.Bold };
                float ty = Screen.height * 0.13f, th = toastStyle.fontSize * 1.6f;
                for (int i = toasts.Count - 1; i >= 0; i--) if (Time.unscaledTime > toasts[i].until) toasts.RemoveAt(i);
                for (int i = 0; i < toasts.Count; i++)
                {
                    var tl = toasts[i]; var r = new Rect(0, ty + i * th, Screen.width, th);
                    float a = Mathf.Clamp01((tl.until - Time.unscaledTime) / 0.5f);
                    toastStyle.normal.textColor = new Color(0f, 0f, 0f, 0.7f * a); GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), tl.text, toastStyle);
                    toastStyle.normal.textColor = (tl.kind == 1 ? new Color(1f, 0.78f, 0.35f) : tl.kind == 2 ? new Color(0.55f, 1f, 0.6f) : new Color(1f, 1f, 1f)) * new Color(1, 1, 1, a);
                    GUI.Label(r, tl.text, toastStyle);
                }
            }
            // your health, while it's down after a scrap
            var pf = PlayerFighter; if (pf != null && pf.Health01 < 0.995f)
            {
                float w = 260f, x = (Screen.width - w) * 0.5f, y = 14f;
                var old = GUI.color; GUI.color = new Color(0f, 0f, 0f, 0.55f); GUI.DrawTexture(new Rect(x - 2, y - 2, w + 4, 16), Texture2D.whiteTexture);
                GUI.color = Color.Lerp(new Color(0.85f, 0.1f, 0.08f), new Color(0.2f, 0.75f, 0.25f), pf.Health01); GUI.DrawTexture(new Rect(x, y, w * pf.Health01, 12), Texture2D.whiteTexture); GUI.color = old;
            }
            // money, top left
            cashStyle ??= new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(18, Screen.height / 30), fontStyle = FontStyle.Bold };
            { var cr = new Rect(14, Mathf.Max(Screen.height * 0.075f, 76f), 300, cashStyle.fontSize * 1.5f); /* below the debug lines */ cashStyle.normal.textColor = new Color(0, 0, 0, 0.7f); GUI.Label(new Rect(cr.x + 2, cr.y + 2, cr.width, cr.height), Econ.Fmt(Econ.Cash), cashStyle); cashStyle.normal.textColor = new Color(1f, 0.93f, 0.7f); GUI.Label(cr, Econ.Fmt(Econ.Cash), cashStyle);
              var tr = new Rect(16, cr.yMax - 4, 300, cashStyle.fontSize * 1.2f); int fs = cashStyle.fontSize; cashStyle.fontSize = Mathf.RoundToInt(fs * 0.62f);
              cashStyle.normal.textColor = new Color(0, 0, 0, 0.7f); GUI.Label(new Rect(tr.x + 2, tr.y + 2, tr.width, tr.height), GameClock.Text, cashStyle); cashStyle.normal.textColor = new Color(0.85f, 0.9f, 1f); GUI.Label(tr, GameClock.Text, cashStyle); cashStyle.fontSize = fs; }
            // the tag: how close to the edge, or the violation
            { string tl = Tag.HudLine; if (tl != null) { toastStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(16, Screen.height / 38), fontStyle = FontStyle.Bold };
                var r2 = new Rect(0, Mathf.Max(Screen.height * 0.12f, 80f), Screen.width, toastStyle.fontSize * 1.5f); var keep = toastStyle.normal.textColor; toastStyle.normal.textColor = new Color(0, 0, 0, 0.75f); GUI.Label(new Rect(2, r2.y + 2, r2.width, r2.height), tl, toastStyle);
                toastStyle.normal.textColor = Tag.Outside ? new Color(1f, 0.35f, 0.3f) : new Color(1f, 0.75f, 0.35f); GUI.Label(r2, tl, toastStyle); toastStyle.normal.textColor = keep; } }
            // stamina, while you're fighting or getting your breath back
            if (pf != null && (Fighter.Session.Hot || pf.Stam01 < 0.995f))
            {
                float w = 260f, x = (Screen.width - w) * 0.5f, y = 32f;
                var old = GUI.color; GUI.color = new Color(0f, 0f, 0f, 0.5f); GUI.DrawTexture(new Rect(x - 2, y - 2, w + 4, 9), Texture2D.whiteTexture);
                GUI.color = pf.Stam01 < 0.25f ? new Color(1f, 0.55f, 0.15f) : new Color(0.95f, 0.85f, 0.35f); GUI.DrawTexture(new Rect(x, y, w * pf.Stam01, 5), Texture2D.whiteTexture); GUI.color = old;
            }
            // the text is rebuilt twice a second, not every frame (string building every frame fed the garbage collector)
            if (Time.unscaledTime > hudT)
            {
                hudT = Time.unscaledTime + 0.5f;
                string lvl = Gore.Level == 2 ? "Full" : Gore.Level == 1 ? "Reduced" : "Off";
                hud1 = TouchControls.Wanted
                    ? "ASBO — Abbots Wend (Unity test)   Left thumb: move • Drag: look • PUNCH (hold: big one) / KICK / SHOVE / FISTS • CAR by a car to drive"
                    : "ASBO — Abbots Wend slice   WASD move/drive • Shift run • F punch (hold: overhand) • Q kick • C shove • X fists up • E car • Space jump/brake • R reset • G gore: " + lvl;
                hud3 = "1-0: visit cottage (Shift = inside) • F8 zebra • T talk   " + lastHouse + "   " + Resident.Stats() + "  traffic " + TrafficCar.All.Count;
            }
            GUI.Label(new Rect(12, 8, 760, 40), hud1);
            if (!string.IsNullOrEmpty(Ambulance.Status)) GUI.Label(new Rect(12, 28, 700, 30), Ambulance.Status);
            GUI.Label(new Rect(12, 46, 900, 30), hud3);
        }
    }
}

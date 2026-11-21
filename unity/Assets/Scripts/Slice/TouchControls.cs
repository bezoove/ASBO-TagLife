using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ASBO.Slice
{
    /// <summary>
    /// Phone controls built on the Input System's on-screen controls: they drive a virtual gamepad, so the player
    /// code and RCC's gamepad bindings work unchanged. Left thumb: stick (walk / steer). Right side: drag to look,
    /// buttons for run, jump (handbrake in a car), get in/out, and pedals while driving.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        public static bool Wanted => Application.isMobilePlatform || PlayerPrefs.GetInt("asbo_touch", 0) == 1;
        PlayerCtl pc; GameObject footSet, carSet; static Sprite disc, ring; Font font;

        public static TouchControls Create(PlayerCtl pc)
        {
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<InputSystemUIInputModule>();
            }
            var go = new GameObject("TouchControls");
            var t = go.AddComponent<TouchControls>(); t.pc = pc; t.Build();
            return t;
        }

        static Sprite Circle(bool filled)
        {
            const int N = 128; var tex = new Texture2D(N, N, TextureFormat.RGBA32, false); tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(N / 2f, N / 2f)) / (N / 2f);
                    float a = filled ? Mathf.Clamp01((1f - d) * 40f) : Mathf.Clamp01((1f - d) * 40f) * Mathf.Clamp01((d - 0.86f) * 40f);
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
        }

        RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var g = new GameObject(name, typeof(RectTransform)); var r = (RectTransform)g.transform; r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = anchor; r.pivot = new Vector2(0.5f, 0.5f); r.anchoredPosition = pos; r.sizeDelta = size;
            return r;
        }

        Image Img(RectTransform r, Sprite s, Color c, bool hit = true) { var i = r.gameObject.AddComponent<Image>(); i.sprite = s; i.color = c; i.raycastTarget = hit; return i; }

        void Label(RectTransform r, string text, int size)
        {
            var t = Rect("Label", r, new Vector2(0.5f, 0.5f), Vector2.zero, r.sizeDelta);
            var tx = t.gameObject.AddComponent<Text>(); tx.text = text; tx.font = font; tx.fontSize = size; tx.alignment = TextAnchor.MiddleCenter;
            tx.color = new Color(1f, 1f, 1f, 0.92f); tx.raycastTarget = false; tx.fontStyle = FontStyle.Bold;
        }

        void Button(Transform parent, string text, string path, Vector2 pos, float size, Color tint)
        {
            var r = Rect(text, parent, new Vector2(1f, 0f), pos, new Vector2(size, size));
            Img(r, disc, tint);
            var ring2 = Rect("Ring", r, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size)); Img(ring2, ring, new Color(1, 1, 1, 0.55f), false);
            r.gameObject.AddComponent<OnScreenButton>().controlPath = path;
            Label(r, text, Mathf.RoundToInt(size * 0.2f));
        }

        void Build()
        {
            if (disc == null) { disc = Circle(true); ring = Circle(false); }
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var cs = gameObject.AddComponent<CanvasScaler>(); cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 0.6f;
            gameObject.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)transform;

            // look: the whole screen behind everything else
            var look = Rect("Look", root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            look.anchorMin = Vector2.zero; look.anchorMax = Vector2.one; look.sizeDelta = Vector2.zero;
            Img(look, null, new Color(0, 0, 0, 0)); look.gameObject.AddComponent<TouchLook>().pc = pc;

            // stick: static base, knob with a large invisible grab area
            var stickBase = Rect("StickBase", root, Vector2.zero, new Vector2(270, 270), new Vector2(300, 300));
            Img(stickBase, ring, new Color(1, 1, 1, 0.45f), false);
            var knob = Rect("Knob", root, Vector2.zero, new Vector2(270, 270), new Vector2(150, 150));
            Img(knob, disc, new Color(1, 1, 1, 0.38f));
            var grab = Rect("Grab", knob, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460, 460)); Img(grab, disc, new Color(1, 1, 1, 0f));
            var st = knob.gameObject.AddComponent<OnScreenStick>(); st.controlPath = "<Gamepad>/leftStick"; st.movementRange = 120f;

            footSet = new GameObject("Foot", typeof(RectTransform)); footSet.transform.SetParent(root, false); Stretch((RectTransform)footSet.transform);
            carSet = new GameObject("Car", typeof(RectTransform)); carSet.transform.SetParent(root, false); Stretch((RectTransform)carSet.transform);
            Color dark = new Color(0.05f, 0.06f, 0.08f, 0.42f);
            Button(footSet.transform, "JUMP", "<Gamepad>/buttonSouth", new Vector2(-170, 190), 190, dark);
            Button(footSet.transform, "RUN", "<Gamepad>/leftStickPress", new Vector2(-380, 130), 160, dark);
            Button(footSet.transform, "CAR", "<Gamepad>/select", new Vector2(-150, 430), 140, new Color(0.1f, 0.25f, 0.45f, 0.5f));
            Button(carSet.transform, "GAS", "<Gamepad>/rightTrigger", new Vector2(-160, 200), 210, new Color(0.08f, 0.35f, 0.12f, 0.5f));
            Button(carSet.transform, "BRAKE", "<Gamepad>/leftTrigger", new Vector2(-400, 150), 180, new Color(0.45f, 0.08f, 0.06f, 0.5f));
            Button(carSet.transform, "H/B", "<Gamepad>/buttonSouth", new Vector2(-400, 380), 130, dark);
            Button(carSet.transform, "OUT", "<Gamepad>/select", new Vector2(-150, 460), 140, new Color(0.1f, 0.25f, 0.45f, 0.5f));
            carSet.SetActive(false);
        }

        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.sizeDelta = Vector2.zero; r.anchoredPosition = Vector2.zero; }

        void Update()
        {
            if (pc == null) return;
            bool car = pc.InCar;
            if (carSet.activeSelf != car) { carSet.SetActive(car); footSet.SetActive(!car); }
        }
    }

    /// <summary>Drag anywhere not covered by a control to turn the camera.</summary>
    public class TouchLook : MonoBehaviour, IDragHandler
    {
        public PlayerCtl pc; public float degPerPixel = 0.16f;
        public void OnDrag(PointerEventData e) { if (pc != null) pc.AddLook(e.delta * degPerPixel * (1080f / Mathf.Max(Screen.height, 1))); }
    }
}

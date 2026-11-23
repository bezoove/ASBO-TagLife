using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ASBO.Slice
{
    /// <summary>
    /// Phone controls built on the Input System's on-screen controls (a virtual gamepad, so the player code and RCC's
    /// gamepad bindings work unchanged), customisable like the original game: EDIT opens the layout editor, where you
    /// drag each control where your thumbs want it and tap one to resize or hide it. On-foot and vehicle layouts are
    /// separate, each with presets and a reset; layouts are saved on the phone.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        public static bool Editing => I != null && I.editing;
        public static bool Wanted => Application.isMobilePlatform || PlayerPrefs.GetInt("asbo_touch", 0) == 1;
        public static TouchControls I;

        class Ctl
        {
            public string id, label; public bool foot, car; public bool bottomLeft;
            public RectTransform rt, cover; public float baseSize; public Vector2 defFoot, defCar;
            public Behaviour[] onScreen; public CanvasGroup group;
        }
        [Serializable] class Entry { public string id; public float x, y, s = 1f; public bool hide; }
        [Serializable] class Layout { public List<Entry> e = new List<Entry>(); }

        PlayerCtl pc; static Sprite disc, ring; Font font; Canvas canvas; RectTransform root; RectTransform stickZone; FloatStick floatStick;
        readonly List<Ctl> ctls = new List<Ctl>();
        Layout foot, car; bool carSet;
        bool editing; bool editCar; Ctl selected; int presetIdx;
        GameObject dim, editBar, sizeBar, editBtn; Text editTitle, sizeTitle, hideLabel, presetLabel;

        public static TouchControls Create(PlayerCtl pc)
        {
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<InputSystemUIInputModule>();
            }
            var go = new GameObject("TouchControls");
            var t = go.AddComponent<TouchControls>(); t.pc = pc; I = t; t.Build();
            return t;
        }

        // ------------------------------------------------------------------ building blocks
        static Sprite Circle(bool filled)
        {
            const int N = 128; var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
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
        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.sizeDelta = Vector2.zero; r.anchoredPosition = Vector2.zero; }
        Image Img(RectTransform r, Sprite s, Color c, bool hit = true) { var i = r.gameObject.AddComponent<Image>(); i.sprite = s; i.color = c; i.raycastTarget = hit; return i; }
        Text Label(RectTransform r, string text, int size, Color? col = null)
        {
            var t = Rect("Label", r, new Vector2(0.5f, 0.5f), Vector2.zero, r.sizeDelta);
            var tx = t.gameObject.AddComponent<Text>(); tx.text = text; tx.font = font; tx.fontSize = size; tx.alignment = TextAnchor.MiddleCenter;
            tx.color = col ?? new Color(1f, 1f, 1f, 0.92f); tx.raycastTarget = false; tx.fontStyle = FontStyle.Bold; tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            return tx;
        }
        Button UiButton(Transform parent, string text, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick, out Text label)
        {
            var r = Rect(text, parent, anchor, pos, size);
            var img = Img(r, null, new Color(0.08f, 0.09f, 0.11f, 0.85f));
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = img; b.onClick.AddListener(() => onClick());
            label = Label(r, text, Mathf.RoundToInt(size.y * 0.42f));
            return b;
        }

        Ctl AddCtl(string id, string label, bool footSet, bool carSet2, bool bottomLeft, Vector2 defFoot, Vector2 defCar, float size)
        {
            var c = new Ctl { id = id, label = label, foot = footSet, car = carSet2, bottomLeft = bottomLeft, defFoot = defFoot, defCar = defCar, baseSize = size };
            c.rt = Rect(id, root, bottomLeft ? Vector2.zero : new Vector2(1f, 0f), footSet ? defFoot : defCar, new Vector2(size, size));
            c.group = c.rt.gameObject.AddComponent<CanvasGroup>();
            ctls.Add(c);
            return c;
        }

        void Button(string id, string text, string path, bool footSet, bool carSet2, Vector2 defFoot, Vector2 defCar, float size, Color tint)
        {
            var c = AddCtl(id, text, footSet, carSet2, false, defFoot, defCar, size);
            var face = Rect("Face", c.rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size)); Stretch(face);
            Img(face, disc, tint);
            var ring2 = Rect("Ring", face, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero); Stretch(ring2); Img(ring2, ring, new Color(1, 1, 1, 0.55f), false);
            var osb = face.gameObject.AddComponent<OnScreenButton>(); osb.controlPath = path;
            Label(face, text, Mathf.RoundToInt(size * 0.2f)).rectTransform.sizeDelta = new Vector2(size, size);
            c.onScreen = new Behaviour[] { osb };
            AddCover(c);
        }

        /// <summary>A driving button that holds a TouchCar flag while pressed (FORWARD also boosts when dragged upwards).</summary>
        void Hold(string id, string text, bool bottomLeft, Vector2 pos, float size, Color tint, Action<bool> set)
        {
            var c = AddCtl(id, text, false, true, bottomLeft, default, pos, size);
            var face = Rect("Face", c.rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size)); Stretch(face);
            Img(face, disc, tint);
            var ring2 = Rect("Ring", face, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero); Stretch(ring2); Img(ring2, ring, new Color(1, 1, 1, 0.55f), false);
            var hb = face.gameObject.AddComponent<HoldButton>(); hb.set = set; hb.boost = id == "vfwd";
            Label(face, text, Mathf.RoundToInt(size * 0.15f)).rectTransform.sizeDelta = new Vector2(size, size);
            c.onScreen = new Behaviour[] { hb };
            AddCover(c);
        }

        void AddCover(Ctl c)
        {
            c.cover = Rect("EditCover", c.rt, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero); Stretch(c.cover);
            Img(c.cover, ring, new Color(1f, 0.85f, 0.2f, 0.9f));
            c.cover.gameObject.AddComponent<EditHandle>().Init(this, c);
            c.cover.gameObject.SetActive(false);
        }

        void Build()
        {
            if (disc == null) { disc = Circle(true); ring = Circle(false); }
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var cs = gameObject.AddComponent<CanvasScaler>(); cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 0.6f;
            gameObject.AddComponent<GraphicRaycaster>();
            root = (RectTransform)transform;

            var look = Rect("Look", root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero); Stretch(look);
            Img(look, null, new Color(0, 0, 0, 0)); look.gameObject.AddComponent<TouchLook>().pc = pc;
            dim = Rect("Dim", root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject; Stretch((RectTransform)dim.transform);
            Img((RectTransform)dim.transform, null, new Color(0f, 0f, 0f, 0.45f)); dim.SetActive(false);

            // the stick floats: put your thumb down anywhere on its side of the screen and it's there; a small push walks,
            // past the inner ring runs, pushed out past the edge sprints. Where it rests (and its size) can still be edited.
            {
                var c = AddCtl("stick", "Stick", true, false, true, new Vector2(270, 270), default, 300f);
                var b = Rect("Base", c.rt, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero); Stretch(b); Img(b, ring, new Color(1, 1, 1, 0.45f), false);
                var inner = Rect("RunRing", c.rt, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero); inner.anchorMin = new Vector2(0.19f, 0.19f); inner.anchorMax = new Vector2(0.81f, 0.81f); inner.sizeDelta = Vector2.zero; Img(inner, ring, new Color(1, 1, 1, 0.18f), false);
                var knob = Rect("Knob", c.rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150));
                Img(knob, disc, new Color(1, 1, 1, 0.38f), false);
                var zone = Rect("StickZone", root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero); zone.SetSiblingIndex(look.GetSiblingIndex() + 1);
                Img(zone, null, new Color(0, 0, 0, 0));
                var fs = zone.gameObject.AddComponent<FloatStick>(); fs.stick = c.rt; fs.knob = knob; fs.group = c.group; fs.root = root;
                stickZone = zone; floatStick = fs;
                c.onScreen = new Behaviour[] { fs };
                AddCover(c);
            }
            Color dark = new Color(0.05f, 0.06f, 0.08f, 0.42f), blue = new Color(0.1f, 0.25f, 0.45f, 0.5f);
            Button("jump", "JUMP", "<Gamepad>/buttonSouth", true, false, new Vector2(-170, 190), default, 190, dark);
            Button("car", "CAR", "<Gamepad>/select", true, false, new Vector2(-150, 430), default, 140, blue);
            Button("punch", "PUNCH", "<Gamepad>/buttonWest", true, false, new Vector2(-400, 330), default, 160, new Color(0.45f, 0.12f, 0.08f, 0.5f));
            Button("kick", "KICK", "<Gamepad>/buttonEast", true, false, new Vector2(-600, 170), default, 150, new Color(0.45f, 0.25f, 0.06f, 0.5f));
            Button("shove", "SHOVE", "<Gamepad>/rightShoulder", true, false, new Vector2(-790, 250), default, 125, new Color(0.4f, 0.18f, 0.1f, 0.5f));
            Button("fists", "FISTS", "<Gamepad>/dpad/up", true, false, new Vector2(-560, 560), default, 115, dark);
            Button("talk", "TALK", "<Gamepad>/buttonNorth", true, false, new Vector2(-330, 560), default, 130, new Color(0.15f, 0.35f, 0.25f, 0.55f));
            Button("crouch", "CROUCH", "<Gamepad>/rightStickPress", true, false, new Vector2(-790, 520), default, 115, dark); // crouch / sneak (jump at a wall climbs it)
            // driving, laid out like the old game: pedals on the left, steering buttons on the right
            Hold("vfwd", "FORWARD", true, new Vector2(192, 475), 250, new Color(0.08f, 0.35f, 0.12f, 0.5f), v => TouchCar.fwd = v);
            Hold("vbrake", "BRAKE\nREVERSE", true, new Vector2(440, 200), 220, new Color(0.45f, 0.08f, 0.06f, 0.5f), v => TouchCar.brake = v); // brakes, then reverses once stopped
            Hold("vhand", "HAND\nBRAKE", false, new Vector2(-360, 430), 175, new Color(0.35f, 0.22f, 0.05f, 0.55f), v => TouchCar.hand = v); // skid
            Hold("vleft", "LEFT", false, new Vector2(-595, 216), 265, dark, v => TouchCar.left = v);
            Hold("vright", "RIGHT", false, new Vector2(-221, 216), 265, dark, v => TouchCar.right = v);
            Hold("vhorn", "HORN", false, new Vector2(-560, 500), 135, dark, v => { if (v) TouchCar.horn = true; });
            Button("out", "OUT", "<Gamepad>/select", false, true, default, new Vector2(-192, 540), 170, blue);

            // edit button and the editor's bars
            UiButton(root, "EDIT", new Vector2(1f, 1f), new Vector2(-90, -60), new Vector2(130, 64), ToggleEdit, out _);
            editBtn = root.Find("EDIT").gameObject;
            editBar = Rect("EditBar", root, new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(1500, 130)).gameObject;
            Img((RectTransform)editBar.transform, null, new Color(0.02f, 0.02f, 0.03f, 0.8f));
            editTitle = Label(Rect("Title", editBar.transform, new Vector2(0.5f, 1f), new Vector2(0, -28), new Vector2(1500, 44)), "", 27);
            float bx = -540;
            foreach (var (txt, act) in new (string, Action)[] { ("Switch layout", SwitchEditSet), ("Preset: Default", NextPreset), ("Reset", ResetLayout), ("Done", ToggleEdit) })
            {
                UiButton(editBar.transform, txt, new Vector2(0.5f, 0f), new Vector2(bx, 36), new Vector2(330, 56), act, out var lbl);
                if (txt.StartsWith("Preset")) presetLabel = lbl;
                bx += 360;
            }
            editBar.SetActive(false);
            sizeBar = Rect("SizeBar", root, new Vector2(0.5f, 1f), new Vector2(0, -340), new Vector2(760, 110)).gameObject;
            Img((RectTransform)sizeBar.transform, null, new Color(0.02f, 0.02f, 0.03f, 0.8f));
            sizeTitle = Label(Rect("Title", sizeBar.transform, new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(760, 40)), "", 26);
            UiButton(sizeBar.transform, "Smaller", new Vector2(0.5f, 0f), new Vector2(-250, 32), new Vector2(220, 52), () => Resize(-0.1f), out _);
            UiButton(sizeBar.transform, "Bigger", new Vector2(0.5f, 0f), new Vector2(0, 32), new Vector2(220, 52), () => Resize(0.1f), out _);
            UiButton(sizeBar.transform, "Hide", new Vector2(0.5f, 0f), new Vector2(250, 32), new Vector2(220, 52), ToggleHide, out hideLabel);
            sizeBar.SetActive(false);

            foot = Load("foot"); car = Load("car");
            Apply();
        }

        // ------------------------------------------------------------------ layouts
        Layout Load(string set)
        {
            var def = Default(set == "car");
            try
            {
                var j = PlayerPrefs.GetString("asbo_ctl_" + set, "");
                if (!string.IsNullOrEmpty(j))
                {
                    var l = JsonUtility.FromJson<Layout>(j);
                    if (l != null && l.e != null)
                    {
                        // controls added since the layout was saved go in at their default spot
                        foreach (var e in def.e) if (Find(l, e.id) == null) l.e.Add(e);
                        return l;
                    }
                }
            }
            catch { }
            return def;
        }
        void Save() { PlayerPrefs.SetString("asbo_ctl_foot", JsonUtility.ToJson(foot)); PlayerPrefs.SetString("asbo_ctl_car", JsonUtility.ToJson(car)); PlayerPrefs.Save(); }

        Layout Default(bool forCar)
        {
            var l = new Layout();
            foreach (var c in ctls)
            {
                if (forCar ? !c.car : !c.foot) continue;
                var p = forCar ? c.defCar : c.defFoot;
                l.e.Add(new Entry { id = c.id, x = p.x, y = p.y, s = 1f });
            }
            return l;
        }

        static readonly string[] Presets = { "Default", "Left-handed", "Compact", "Big thumbs" };
        Layout Preset(bool forCar, int k)
        {
            var l = Default(forCar);
            foreach (var e in l.e)
            {
                switch (k)
                {
                    case 1: break; // mirrored: stick on the right, buttons on the left (Apply swaps the sides)
                    case 2: e.s = 0.82f; e.x *= 0.8f; e.y *= 0.8f; break;
                    case 3: e.s = 1.25f; e.x *= 1.12f; e.y *= 1.12f; break;
                }
            }
            if (k == 1) foreach (var e in l.e) e.hide = false;
            l.e.Add(new Entry { id = "_mirror", x = k == 1 ? 1 : 0 });
            return l;
        }

        Entry Find(Layout l, string id) { foreach (var e in l.e) if (e.id == id) return e; return null; }
        bool Mirrored(Layout l) { var m = Find(l, "_mirror"); return m != null && m.x > 0.5f; }

        void Apply()
        {
            var l = carSet ? car : foot;
            bool mirror = Mirrored(l);
            foreach (var c in ctls)
            {
                bool inSet = carSet ? c.car : c.foot;
                var e = inSet ? Find(l, c.id) : null;
                bool show = inSet && e != null && (!e.hide || editing);
                c.rt.gameObject.SetActive(show);
                if (!show) continue;
                bool left = c.bottomLeft ^ mirror;
                c.rt.anchorMin = c.rt.anchorMax = left ? Vector2.zero : new Vector2(1f, 0f);
                c.rt.anchoredPosition = new Vector2(mirror ? -e.x : e.x, e.y);
                c.rt.sizeDelta = Vector2.one * c.baseSize * e.s;
                c.group.alpha = e.hide ? 0.35f : 1f;
                foreach (var b in c.onScreen) b.enabled = !editing;
                c.cover.gameObject.SetActive(editing);
            }
            // the stick's catch area: its own side of the screen (the left, or the right in a mirrored layout), only on foot
            if (stickZone != null)
            {
                var sc = ctls.Find(x => x.id == "stick"); bool on = !carSet && !editing && sc != null && sc.rt.gameObject.activeSelf;
                stickZone.gameObject.SetActive(on);
                bool leftSide = sc == null || sc.rt.anchorMin.x < 0.5f;
                stickZone.anchorMin = new Vector2(leftSide ? 0f : 0.58f, 0f); stickZone.anchorMax = new Vector2(leftSide ? 0.42f : 1f, 0.78f); stickZone.sizeDelta = Vector2.zero; stickZone.anchoredPosition = Vector2.zero;
                if (floatStick != null) floatStick.Rest();
            }
        }

        void Store(Ctl c)
        {
            var l = carSet ? car : foot; var e = Find(l, c.id); if (e == null) return;
            bool mirror = Mirrored(l);
            e.x = mirror ? -c.rt.anchoredPosition.x : c.rt.anchoredPosition.x; e.y = c.rt.anchoredPosition.y;
        }

        // ------------------------------------------------------------------ editor
        public void ToggleEdit()
        {
            editing = !editing;
            if (editing) { editCar = pc != null && pc.InCar; carSet = editCar; }
            else { Save(); carSet = pc != null && pc.InCar; selected = null; }
            dim.SetActive(editing); editBar.SetActive(editing); sizeBar.SetActive(false); editBtn.SetActive(!editing);
            RefreshTitle(); Apply();
        }
        public void SwitchEditSet() { Save(); editCar = !editCar; carSet = editCar; selected = null; sizeBar.SetActive(false); RefreshTitle(); Apply(); }
        public void NextPreset()
        {
            presetIdx = (presetIdx + 1) % Presets.Length;
            if (editCar) car = Preset(true, presetIdx); else foot = Preset(false, presetIdx);
            presetLabel.text = "Preset: " + Presets[presetIdx]; Apply();
        }
        void ResetLayout() { if (editCar) car = Default(true); else foot = Default(false); presetIdx = 0; presetLabel.text = "Preset: Default"; selected = null; sizeBar.SetActive(false); Apply(); }
        void RefreshTitle() { editTitle.text = (editCar ? "Vehicle layout" : "On-foot layout") + "  ·  drag the controls where your thumbs want them, tap one to resize or hide it"; }

        void Select(Ctl c)
        {
            selected = c; sizeBar.SetActive(true);
            var e = Find(carSet ? car : foot, c.id);
            sizeTitle.text = c.label + "  ·  size " + Mathf.RoundToInt((e != null ? e.s : 1f) * 100f) + "%";
            hideLabel.text = e != null && e.hide ? "Show" : "Hide";
        }
        void Resize(float d)
        {
            if (selected == null) return; var e = Find(carSet ? car : foot, selected.id); if (e == null) return;
            e.s = Mathf.Clamp(e.s + d, 0.6f, 1.8f); Apply(); Select(selected);
        }
        void ToggleHide()
        {
            if (selected == null) return; var e = Find(carSet ? car : foot, selected.id); if (e == null) return;
            if (selected.id == "stick" && !e.hide) { sizeTitle.text = "The stick can't be hidden"; return; }
            e.hide = !e.hide; Apply(); Select(selected);
        }

        void Dragged(Ctl c, Vector2 screenDelta)
        {
            c.rt.anchoredPosition += screenDelta / Mathf.Max(0.01f, canvas.scaleFactor);
            // keep it on screen
            var size = root.rect.size; var p = c.rt.anchoredPosition; bool left = c.rt.anchorMin.x < 0.5f; float h = c.rt.sizeDelta.x * 0.5f;
            p.x = left ? Mathf.Clamp(p.x, h, size.x - h) : Mathf.Clamp(p.x, -size.x + h, -h);
            p.y = Mathf.Clamp(p.y, h, size.y - h);
            c.rt.anchoredPosition = p;
            Store(c);
        }
        void Tapped(Ctl c) { Select(c); }

        Ctl talkCtl; bool talkLooked;
        void Update()
        {
            if (pc == null || editing) return;
            bool inCar = pc.InCar;
            if (inCar != carSet) { carSet = inCar; Apply(); }
            // TALK only shows when there's someone to talk to (or a conversation to end)
            if (!talkLooked) { talkLooked = true; foreach (var c in ctls) if (c.id == "talk") talkCtl = c; }
            if (talkCtl != null && talkCtl.rt.gameObject.activeSelf)
            {
                bool can = !inCar && (Social.I != null && (Social.I.Near != null || DialogueUI.Open) || CasinoFloor.Near != null || PropWorld.HasPrompt);
                talkCtl.group.alpha = Mathf.MoveTowards(talkCtl.group.alpha, can ? 1f : 0f, Time.deltaTime * 6f);
                talkCtl.group.blocksRaycasts = can; talkCtl.group.interactable = can;
            }
        }

        class EditHandle : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
        {
            TouchControls owner; Ctl c; float moved;
            public void Init(TouchControls o, Ctl ctl) { owner = o; c = ctl; }
            public void OnPointerDown(PointerEventData e) { moved = 0f; }
            public void OnDrag(PointerEventData e) { moved += e.delta.magnitude; owner.Dragged(c, e.delta); }
            public void OnPointerUp(PointerEventData e) { if (moved < 12f) owner.Tapped(c); }
        }
    }

    /// <summary>
    /// Press-and-hold button for driving. It doesn't wait to be told a finger has lifted (that message can go missing
    /// with several fingers on the glass, which left the accelerator stuck on): every frame it looks at where the
    /// fingers actually are, and it's held only while one is on it. FORWARD also boosts when that finger drags upwards
    /// (it stays held as it slides up off the top). Slide off any other way and it lets go.
    /// </summary>
    public class HoldButton : MonoBehaviour
    {
        public Action<bool> set; public bool boost; float downY; bool held; int track = -1;
        void Update()
        {
            var rt = (RectTransform)transform; int found = -1; Vector2 at = default;
            var ts = Touchscreen.current;
            if (ts != null)
                foreach (var t in ts.touches)
                {
                    if (!t.press.isPressed) continue;
                    int id = t.touchId.ReadValue(); var p = t.position.ReadValue();
                    if (id == track && Covers(rt, p, true)) { found = id; at = p; break; }
                    if (found < 0 && Covers(rt, p, false)) { found = id; at = p; }
                }
            var m = Mouse.current; // in the editor
            if (found < 0 && m != null && m.leftButton.isPressed && Covers(rt, m.position.ReadValue(), track == 9999)) { found = 9999; at = m.position.ReadValue(); }
            if (found >= 0)
            {
                if (found != track) { track = found; downY = at.y; }
                if (!held) { held = true; set?.Invoke(true); }
                if (boost) TouchCar.boost = at.y - downY > Screen.height * 0.035f;
            }
            else Release();
        }
        /// <summary>Is this point on the button (a circle); once it's ours, a FORWARD finger may drag up above it.</summary>
        bool Covers(RectTransform rt, Vector2 p, bool ours)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, p, null, out var l)) return false;
            float r = rt.rect.width * 0.5f;
            if (ours && boost) return Mathf.Abs(l.x) < r * 1.2f && l.y > -r * 1.2f && l.y < r * 3.5f;
            return l.magnitude < r * (ours ? 1.15f : 1f);
        }
        void OnDisable() { Release(); }
        void Release() { track = -1; if (!held) return; held = false; set?.Invoke(false); if (boost) TouchCar.boost = false; }
    }

    /// <summary>
    /// The floating stick: wherever your thumb goes down on its side of the screen, the stick appears there. Push a little
    /// to walk (slow to brisk), past the inner ring to run, and out past the edge to sprint. Lift and it goes back to
    /// where it rests, faded.
    /// </summary>
    public class FloatStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform stick, knob, root; public CanvasGroup group;
        public static Vector2 Value; public static bool Active, Sprint;
        public const float RunAt = 0.62f;
        int pid = int.MinValue, tid = -1; Vector2 rest; bool restSet; Vector2 origin, lastScreen;
        float R => stick.rect.width * 0.4f;

        public void Rest() { if (stick == null || Active) return; rest = stick.anchoredPosition; restSet = true; if (!Active) group.alpha = Mathf.Min(group.alpha, 0.55f); }
        public void OnPointerDown(PointerEventData e)
        {
            if (Active) return;
            if (!restSet) Rest();
            pid = e.pointerId; Active = true; lastScreen = e.position;
            // which finger it is: the UI's pointer id on a phone is not the screen's touch id, so take the touch id itself
            tid = e is UnityEngine.InputSystem.UI.ExtendedPointerEventData x && x.touchId > 0 ? x.touchId : -1;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)stick.parent, e.position, null, out origin);
            // anchored position for the stick's anchor (bottom left or right) from a point in its parent
            var pr = ((RectTransform)stick.parent).rect; var anc = new Vector2(Mathf.Lerp(pr.xMin, pr.xMax, stick.anchorMin.x), Mathf.Lerp(pr.yMin, pr.yMax, stick.anchorMin.y));
            stick.anchoredPosition = origin - anc; group.alpha = 1f;
            Move(e.position);
        }
        public void OnDrag(PointerEventData e) { if (e.pointerId == pid) { lastScreen = e.position; Move(e.position); } }
        public void OnPointerUp(PointerEventData e) { if (e.pointerId == pid) Release(); }
        void Move(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)stick.parent, screen, null, out var p);
            var d = p - origin; float r = R; float m = d.magnitude / r;
            Sprint = m > 1.3f; Value = d / r; if (Value.magnitude > 1f) Value = Value.normalized;
            knob.anchoredPosition = Vector2.ClampMagnitude(d, r);
        }
        void Update()
        {
            if (!Active) return;
            // never stuck: if the finger that owns the stick has gone, let go
            var ts = Touchscreen.current; bool alive = false;
            if (ts != null)
                foreach (var t in ts.touches)
                {
                    if (!t.press.isPressed) continue;
                    int id = t.touchId.ReadValue();
                    // our finger by its id; failing that, a finger still down right where ours last was
                    var at = t.position.ReadValue();
                    if (id == tid || (tid < 0 && (at - lastScreen).sqrMagnitude < 160f * 160f))
                    {
                        alive = true;
                        // follow the finger straight from the screen (doesn't rely on the UI's drag events, which can stop)
                        if ((at - lastScreen).sqrMagnitude > 0.25f) { lastScreen = at; Move(at); }
                        break;
                    }
                }
            var m = Mouse.current; if (!alive && tid < 0 && m != null && m.leftButton.isPressed) alive = true; // the editor's mouse
            if (!alive) Release();
        }
        void OnDisable() { Release(); }
        void Release()
        {
            if (!Active && pid == int.MinValue) return;
            Active = false; Sprint = false; Value = Vector2.zero; pid = int.MinValue; tid = -1;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            if (stick != null && restSet) stick.anchoredPosition = rest;
            if (group != null) group.alpha = 0.55f;
        }
    }

    /// <summary>Drag anywhere not covered by a control to turn the camera.</summary>
    public class TouchLook : MonoBehaviour, IDragHandler
    {
        public PlayerCtl pc; public float degPerPixel = 0.16f;
        public void OnDrag(PointerEventData e) { if (pc != null) pc.AddLook(e.delta * degPerPixel * (1080f / Mathf.Max(Screen.height, 1))); }
    }
}

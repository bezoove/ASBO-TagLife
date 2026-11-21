using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ASBO.Slice
{
    /// <summary>
    /// The conversation panel: who's talking and what they said, with your answers as big buttons underneath (tap them,
    /// or press 1-4). Built in code, drawn over the touch controls, gone when the chat ends.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        static DialogueUI I;
        public static bool Open => I != null && I.panel != null && I.panel.activeSelf;

        GameObject panel; Text nameText, lineText; readonly List<(GameObject go, Text txt)> buttons = new List<(GameObject, Text)>();
        readonly List<Action> acts = new List<Action>(); float hideAt = -1f; Font font;

        static DialogueUI Ensure()
        {
            if (I != null) return I;
            var g = new GameObject("DialogueUI"); DontDestroyOnLoad(g); I = g.AddComponent<DialogueUI>(); I.Build();
            return I;
        }

        void Build()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 120;
            var cs = gameObject.AddComponent<CanvasScaler>(); cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 0.6f;
            gameObject.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null) { var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(); }

            panel = new GameObject("Panel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(transform, false);
            var pr = (RectTransform)panel.transform; pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0f); pr.pivot = new Vector2(0.5f, 0f);
            pr.anchoredPosition = new Vector2(0, 40); pr.sizeDelta = new Vector2(1180, 330);
            panel.GetComponent<Image>().color = new Color(0.04f, 0.045f, 0.055f, 0.86f);

            nameText = MakeText(pr, new Vector2(30, -20), new Vector2(1120, 44), 30, FontStyle.Bold, new Color(1f, 0.82f, 0.35f));
            lineText = MakeText(pr, new Vector2(30, -66), new Vector2(1120, 100), 34, FontStyle.Normal, Color.white);
            for (int i = 0; i < 4; i++)
            {
                var b = new GameObject("Opt" + i, typeof(RectTransform), typeof(Image), typeof(Button)); b.transform.SetParent(pr, false);
                var br = (RectTransform)b.transform; br.anchorMin = br.anchorMax = new Vector2(0f, 0f); br.pivot = new Vector2(0f, 0f);
                br.anchoredPosition = new Vector2(30 + (i % 2) * 565, 26 + (1 - i / 2) * 84); br.sizeDelta = new Vector2(545, 72);
                b.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
                int idx = i; b.GetComponent<Button>().onClick.AddListener(() => Pick(idx));
                var t = MakeText(br, new Vector2(18, 0), new Vector2(510, 72), 30, FontStyle.Bold, Color.white); t.alignment = TextAnchor.MiddleLeft;
                ((RectTransform)t.transform).anchorMin = ((RectTransform)t.transform).anchorMax = new Vector2(0f, 0.5f); ((RectTransform)t.transform).pivot = new Vector2(0f, 0.5f); ((RectTransform)t.transform).anchoredPosition = new Vector2(18, 0);
                buttons.Add((b, t));
            }
            panel.SetActive(false);
        }

        Text MakeText(RectTransform parent, Vector2 pos, Vector2 size, int fs, FontStyle style, Color col)
        {
            var g = new GameObject("Text", typeof(RectTransform), typeof(Text)); g.transform.SetParent(parent, false);
            var r = (RectTransform)g.transform; r.anchorMin = r.anchorMax = new Vector2(0f, 1f); r.pivot = new Vector2(0f, 1f); r.anchoredPosition = pos; r.sizeDelta = size;
            var t = g.GetComponent<Text>(); t.font = font; t.fontSize = fs; t.fontStyle = style; t.color = col; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            var sh = g.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.8f); sh.effectDistance = new Vector2(2, -2);
            return t;
        }

        /// <summary>Show a line: who said it and what.</summary>
        public static void Show(string who, string line)
        {
            var d = Ensure(); d.panel.SetActive(true); d.hideAt = -1f;
            d.nameText.text = who; d.lineText.text = line;
            Debug.Log("[ASBO] say " + who + ": " + line);
        }

        /// <summary>The answers you can give (up to four).</summary>
        public static void Options(List<(string label, Action act)> opts)
        {
            var d = Ensure(); d.acts.Clear();
            for (int i = 0; i < d.buttons.Count; i++)
            {
                bool on = i < opts.Count; d.buttons[i].go.SetActive(on);
                if (on) { d.buttons[i].txt.text = (i + 1) + "   " + opts[i].label; d.acts.Add(opts[i].act); }
            }
        }

        /// <summary>Close the panel (after a moment, so the last line can be read).</summary>
        public static void Hide(float after = 0f)
        {
            if (I == null) return;
            foreach (var b in I.buttons) b.go.SetActive(false);
            I.acts.Clear();
            if (after <= 0f) I.panel.SetActive(false); else I.hideAt = Time.unscaledTime + after;
        }

        void Pick(int i) { if (i < acts.Count) { var a = acts[i]; a?.Invoke(); } }
        /// <summary>Choose an answer from code (scripted tests).</summary>
        public static void Choose(int i) { if (I != null) I.Pick(i); }

        void Update()
        {
            if (hideAt > 0f && Time.unscaledTime > hideAt) { hideAt = -1f; panel.SetActive(false); }
            if (!panel.activeSelf || acts.Count == 0) return;
            var kb = Keyboard.current; if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame) Pick(0); else if (kb.digit2Key.wasPressedThisFrame) Pick(1); else if (kb.digit3Key.wasPressedThisFrame) Pick(2); else if (kb.digit4Key.wasPressedThisFrame) Pick(3);
        }
    }
}

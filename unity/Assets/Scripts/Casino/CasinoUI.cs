using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ASBO.Slice;

namespace ASBO.Casino
{
    /// <summary>Your casino chips, a pound each. Bought and cashed at the cage; they stay in your pocket when you leave.</summary>
    public class CasinoWallet : IWallet
    {
        public static readonly CasinoWallet I = new CasinoWallet();
        public static int Held;
        public int Chips => Held;
        public bool Take(int n) { if (n <= 0 || n > Held) return false; Held -= n; return true; }
        public void Pay(int n) { if (n > 0) Held += n; }
    }

    /// <summary>
    /// The casino's screens, drawn over the game when you sit down at something: the cashier's cage, the roulette wheel,
    /// the blackjack table, a fruit machine and the poker table. Big touch-friendly buttons; everything is drawn in code
    /// (felt, cards, chips, the wheel, the fruit), so there are no logos or borrowed art anywhere.
    /// </summary>
    public partial class CasinoUI : MonoBehaviour
    {
        public enum Game { None, Cage, Roulette, Blackjack, Slots, Poker }
        public static CasinoUI I;
        public static bool Open => I != null && I.game != Game.None;
        static readonly System.Random rng = new System.Random();

        Game game; Font font; RectTransform root, body; Text title, chipsText, msgText;
        readonly Dictionary<Game, RectTransform> panels = new Dictionary<Game, RectTransform>();
        Sprite round, circle, cardFace, cardBack;
        static readonly Color Felt = new Color(0.05f, 0.33f, 0.18f), FeltDark = new Color(0.03f, 0.22f, 0.12f), Gold = new Color(0.95f, 0.78f, 0.3f), Cream = new Color(1f, 0.97f, 0.88f);
        static readonly Color BtnCol = new Color(0.12f, 0.12f, 0.14f, 0.92f), BtnHot = new Color(0.62f, 0.12f, 0.1f, 0.95f);

        // ------------------------------------------------------------------------------------------ open / close
        /// <summary>Sit down at something (seed: which machine, so each fruit machine keeps its own reels).</summary>
        public static void Show(Game g, int seed = 0)
        {
            if (I == null) { var go = new GameObject("CasinoUI"); DontDestroyOnLoad(go); I = go.AddComponent<CasinoUI>(); I.Build(); }
            I.Enter(g, seed);
        }
        public static void Close() { if (I != null) I.Leave(); }

        void Enter(Game g, int seed)
        {
            game = g; root.gameObject.SetActive(true);
            foreach (var kv in panels) kv.Value.gameObject.SetActive(kv.Key == g);
            title.text = g switch { Game.Cage => "CASHIER", Game.Roulette => "ROULETTE", Game.Blackjack => "BLACKJACK", Game.Slots => "FRUIT MACHINE", Game.Poker => "TEXAS HOLD'EM", _ => "" };
            Msg(g switch
            {
                Game.Cage => "Chips are a pound each. Cash them in any time.",
                Game.Roulette => "Pick a chip, tap the layout to bet (on a line for a split, a corner for four, the bottom edge for a street), then SPIN.",
                Game.Blackjack => "Six decks. Dealer stands on all 17s. Blackjack pays 3 to 2.",
                Game.Slots => "Line up three of a kind. A cherry on the first reel always pays.",
                Game.Poker => "Blinds 1/2, no limit. Buy in for 40 to 200 chips.",
                _ => ""
            });
            if (g == Game.Slots) SlotsEnter(seed);
            if (g == Game.Poker) PokerEnter();
            if (g == Game.Blackjack) BjRefresh();
            if (g == Game.Roulette) RouRefresh();
            if (g == Game.Cage) CageRefresh();
            Refresh();
        }

        void Leave()
        {
            if (game == Game.Roulette && rouSpinning) return; // let the ball drop first
            if (game == Game.Roulette && rou.Bets.Count > 0) { rou.ClearBets(CasinoWallet.I); RouRefresh(); }
            if (game == Game.Blackjack && bj.phase != BlackjackGame.Phase.Betting && bj.phase != BlackjackGame.Phase.Done) { Msg("Finish the hand first."); return; }
            if (game == Game.Poker && poker != null && poker.Seated) { int back = poker.Leave(CasinoWallet.I); World.Toast("You get up from the poker table with " + back + " chips.", back > 0 ? 2 : 0); }
            game = Game.None; root.gameObject.SetActive(false);
        }

        /// <summary>Scripted tests: do the obvious thing at whatever's open (bet and spin, deal, sit down).</summary>
        public static void DebugAct()
        {
            if (I == null) return;
            switch (I.game)
            {
                case Game.Roulette: I.chipPick = 5; I.RouPlace(RouletteGame.Make(RouletteGame.Kind.Straight, 17)); I.RouPlace(RouletteGame.Make(RouletteGame.Kind.Corner, 8, 9, 11, 12)); I.RouPlace(RouletteGame.Outside(RouletteGame.Kind.Red)); I.RouPlace(RouletteGame.Make(RouletteGame.Kind.Street, 25, 26, 27)); I.RouSpin(); break;
                case Game.Blackjack: I.bjBet = 10; I.BjDeal(); break;
                case Game.Slots: I.SlotSpin(); break;
                case Game.Poker: I.PkSit(100); break;
            }
        }

        void Msg(string s) { if (msgText != null) msgText.text = s; }
        void Refresh() { if (chipsText != null) chipsText.text = "CHIPS  " + CasinoWallet.Held + "     CASH  " + Econ.Fmt(Econ.Cash); }

        void Update()
        {
            if (game == Game.None) return;
            var kb = Keyboard.current; if (kb != null && kb.escapeKey.wasPressedThisFrame) Leave();
            switch (game)
            {
                case Game.Roulette: RouUpdate(); break;
                case Game.Blackjack: BjUpdate(); break;
                case Game.Slots: SlotsUpdate(); break;
                case Game.Poker: PokerUpdate(); break;
            }
            Refresh();
        }

        // ------------------------------------------------------------------------------------------ building the screens
        void Build()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            round = MakeSprite(64, (x, y) => RoundRect(x, y, 64, 64, 14), 18);
            circle = MakeSprite(64, (x, y) => Disc(x, y, 32, 32, 30));
            cardFace = MakeSprite(96, (x, y) => RoundRect(x, y, 96, 96, 14), 20);
            cardBack = MakeTexSprite(96, 136, (x, y) =>
            {
                float a = RoundRect(x, y, 96, 136, 10); if (a <= 0f) return Color.clear;
                bool border = x < 7 || x > 88 || y < 7 || y > 128;
                bool check = ((int)((x + y) / 8f) + (int)((x - y + 200) / 8f)) % 2 == 0;
                var c = border ? Cream : check ? new Color(0.62f, 0.08f, 0.1f) : new Color(0.48f, 0.05f, 0.08f); c.a = a; return c;
            });

            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 130;
            var cs = gameObject.AddComponent<CanvasScaler>(); cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 0.6f;
            gameObject.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null) { var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(); }

            root = Stretch(transform, "Casino"); var bg = root.gameObject.AddComponent<Image>(); bg.color = FeltDark; // blocks touches to the game underneath
            var felt = Stretch(root, "Felt"); var fi = felt.gameObject.AddComponent<Image>(); fi.sprite = MakeTexSprite(256, 256, FeltPixel); fi.type = Image.Type.Tiled; fi.color = Color.white; fi.raycastTarget = false;
            body = Box(root, "Body", 0, 0, 1920, 1080);
            title = Txt(body, "", 0, 495, 900, 70, 46, Gold, FontStyle.Bold);
            chipsText = Txt(body, "", 560, 495, 760, 60, 30, Cream, FontStyle.Bold, TextAnchor.MiddleRight);
            msgText = Txt(body, "", 0, 430, 1500, 70, 28, Color.white, FontStyle.Normal);
            Btn(body, "LEAVE", -820, 495, 220, 70, BtnCol, Leave);

            panels[Game.Cage] = BuildCage();
            panels[Game.Roulette] = BuildRoulette();
            panels[Game.Blackjack] = BuildBlackjack();
            panels[Game.Slots] = BuildSlots();
            panels[Game.Poker] = BuildPoker();
            root.gameObject.SetActive(false);
        }

        static Color FeltPixel(float x, float y)
        {
            float n = Mathf.PerlinNoise(x * 0.09f, y * 0.09f) * 0.05f + Mathf.PerlinNoise(x * 0.5f, y * 0.5f) * 0.025f;
            return new Color(Felt.r + n * 0.3f, Felt.g + n, Felt.b + n * 0.5f);
        }

        // ---- sprites drawn in code
        static float Smooth(float d) => Mathf.Clamp01(0.5f - d); // d = signed distance in pixels (negative inside)
        static float RoundRect(float x, float y, float w, float h, float r)
        {
            float cx = Mathf.Clamp(x, r, w - r), cy = Mathf.Clamp(y, r, h - r);
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r + 0.5f;
            if (x >= r && x <= w - r || y >= r && y <= h - r) d = Mathf.Max(Mathf.Max(-x, x - w), Mathf.Max(-y, y - h)) + 0.5f;
            return Smooth(d);
        }
        static float Disc(float x, float y, float cx, float cy, float r) => Smooth(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r);
        static Sprite MakeSprite(int s, Func<float, float, float> alpha, int border = 0)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[s * s]; for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) px[y * s + x] = new Color(1, 1, 1, alpha(x + 0.5f, y + 0.5f));
            t.SetPixels32(px); t.Apply();
            return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }
        static Sprite MakeTexSprite(int w, int h, Func<float, float, Color> col)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[w * h]; for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = col(x + 0.5f, y + 0.5f);
            t.SetPixels32(px); t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        // ---- layout helpers (positions in reference pixels from the centre of the 1920 x 1080 screen)
        static RectTransform Stretch(Transform parent, string name)
        {
            var g = new GameObject(name, typeof(RectTransform)); g.transform.SetParent(parent, false);
            var r = (RectTransform)g.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; return r;
        }
        static RectTransform Box(Transform parent, string name, float x, float y, float w, float h)
        {
            var g = new GameObject(name, typeof(RectTransform)); g.transform.SetParent(parent, false);
            var r = (RectTransform)g.transform; r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f); r.pivot = new Vector2(0.5f, 0.5f); r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h); return r;
        }
        Image Img(Transform parent, Sprite s, Color c, float x, float y, float w, float h, bool sliced = true)
        {
            var r = Box(parent, "Img", x, y, w, h); var i = r.gameObject.AddComponent<Image>(); i.sprite = s; i.color = c;
            if (s != null && sliced && s.border != Vector4.zero) i.type = Image.Type.Sliced; i.raycastTarget = false; return i;
        }
        Text Txt(Transform parent, string s, float x, float y, float w, float h, int size, Color c, FontStyle st = FontStyle.Bold, TextAnchor a = TextAnchor.MiddleCenter)
        {
            var r = Box(parent, "Text", x, y, w, h); var t = r.gameObject.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.fontStyle = st; t.color = c; t.alignment = a; t.text = s; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            var sh = r.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.7f); sh.effectDistance = new Vector2(2, -2);
            return t;
        }
        class Bt { public Button b; public Text t; public Image i; public void Set(bool on) { b.gameObject.SetActive(on); } public void Label(string s) { t.text = s; } }
        Bt Btn(Transform parent, string label, float x, float y, float w, float h, Color c, Action onClick, int size = 30)
        {
            var r = Box(parent, label, x, y, w, h); var i = r.gameObject.AddComponent<Image>(); i.sprite = round; i.type = Image.Type.Sliced; i.color = c;
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = i; b.onClick.AddListener(() => onClick());
            var cb = b.colors; cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f); cb.pressedColor = new Color(0.75f, 0.75f, 0.75f); cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); b.colors = cb;
            var t = Txt(r, label, 0, 0, w - 10, h, size, Color.white); return new Bt { b = b, t = t, i = i };
        }

        // ---- chips and cards
        static Color ChipColor(int v) => v >= 100 ? new Color(0.12f, 0.12f, 0.12f) : v >= 25 ? new Color(0.1f, 0.5f, 0.2f) : v >= 10 ? new Color(0.15f, 0.3f, 0.75f) : v >= 5 ? new Color(0.78f, 0.12f, 0.12f) : new Color(0.92f, 0.92f, 0.9f);
        RectTransform Chip(Transform parent, int v, float x, float y, float d = 54f)
        {
            var r = Box(parent, "Chip", x, y, d, d);
            var i = r.gameObject.AddComponent<Image>(); i.sprite = circle; i.color = ChipColor(v); i.raycastTarget = false;
            var ring = Img(r, circle, Cream, 0, 0, d * 0.72f, d * 0.72f); ring.color = new Color(1f, 1f, 1f, 0.85f);
            var inner = Img(r, circle, ChipColor(v), 0, 0, d * 0.6f, d * 0.6f);
            var t = Txt(r, v.ToString(), 0, 0, d, d, Mathf.RoundToInt(d * (v >= 100 ? 0.3f : 0.38f)), v < 5 ? new Color(0.1f, 0.1f, 0.1f) : Color.white);
            t.GetComponent<Shadow>().enabled = false; return r;
        }
        RectTransform CardView(Transform parent, Card c, bool faceUp, float x, float y, float w = 116f)
        {
            float h = w * 1.42f; var r = Box(parent, "Card", x, y, w, h);
            if (!faceUp) { var b = r.gameObject.AddComponent<Image>(); b.sprite = cardBack; b.raycastTarget = false; return r; }
            var i = r.gameObject.AddComponent<Image>(); i.sprite = cardFace; i.type = Image.Type.Sliced; i.color = Color.white; i.raycastTarget = false;
            var col = c.Red ? new Color(0.78f, 0.05f, 0.08f) : new Color(0.08f, 0.08f, 0.1f);
            var a = Txt(r, c.RankText + "\n" + c.SuitText, -w * 0.3f, h * 0.27f, w * 0.4f, h * 0.4f, Mathf.RoundToInt(w * 0.24f), col, FontStyle.Bold); a.GetComponent<Shadow>().enabled = false; a.lineSpacing = 0.85f;
            var big = Txt(r, c.SuitText, w * 0.08f, -h * 0.1f, w, h * 0.6f, Mathf.RoundToInt(w * 0.55f), col, FontStyle.Normal); big.GetComponent<Shadow>().enabled = false;
            return r;
        }
        static void Clear(Transform t) { for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject); }

        // chip picker shared by roulette and blackjack
        static readonly int[] Denoms = { 1, 5, 10, 25, 100 };
        int chipPick = 5;
        readonly List<(int v, RectTransform r)> pickers = new List<(int, RectTransform)>();
        void ChipPicker(Transform parent, float x0, float y)
        {
            for (int k = 0; k < Denoms.Length; k++)
            {
                int v = Denoms[k]; var c = Chip(parent, v, x0 + k * 96, y, 82);
                var img = c.GetComponent<Image>(); img.raycastTarget = true;
                var b = c.gameObject.AddComponent<Button>(); b.targetGraphic = img; b.onClick.AddListener(() => { chipPick = v; HighlightPick(); });
                pickers.Add((v, c));
            }
            HighlightPick();
        }
        void HighlightPick() { foreach (var (v, r) in pickers) r.localScale = Vector3.one * (v == chipPick ? 1.18f : 0.9f); }

        // ================================================================================================ the cage
        Text cageText;
        RectTransform BuildCage()
        {
            var p = Box(body, "Cage", 0, 0, 1920, 1080);
            Img(p, round, new Color(0f, 0f, 0f, 0.35f), 0, 20, 1000, 560);
            cageText = Txt(p, "", 0, 170, 900, 160, 40, Cream);
            int[] buys = { 10, 20, 50, 100 };
            for (int k = 0; k < buys.Length; k++) { int v = buys[k]; Btn(p, "BUY " + v, -330 + k * 220, 0, 200, 90, BtnHot, () => Buy(v)); }
            Btn(p, "CASH IN ALL CHIPS", 0, -140, 520, 90, BtnCol, CashOut);
            Txt(p, "Over-18s only. When the fun stops, stop.", 0, -260, 900, 50, 24, new Color(1f, 1f, 1f, 0.6f), FontStyle.Italic);
            return p;
        }
        void Buy(int v)
        {
            if (!Econ.Can(v)) { Msg("You haven't got " + Econ.Fmt(v) + " on you."); return; }
            Econ.Spend(v, "chips"); CasinoWallet.I.Pay(v); Msg("The cashier counts out " + v + " chips."); CageRefresh();
        }
        void CashOut()
        {
            int n = CasinoWallet.Held; if (n <= 0) { Msg("You've no chips to cash."); return; }
            CasinoWallet.Held = 0; Econ.Give(n); Msg("You cash " + n + " chips for " + Econ.Fmt(n) + "."); CageRefresh();
        }
        void CageRefresh() { if (cageText != null) cageText.text = "Cash on you: " + Econ.Fmt(Econ.Cash) + "\nChips: " + CasinoWallet.Held; }
    }
}

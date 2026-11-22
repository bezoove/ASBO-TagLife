using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ASBO.Slice;

namespace ASBO.Casino
{
    public partial class CasinoUI
    {
        // ================================================================================================ blackjack
        BlackjackGame bj; RectTransform bjDealer, bjHands, bjBetChips; Text bjDealerTotal, bjBetText; Bt bjDeal, bjClear, bjHit, bjStand, bjDouble, bjSplit, bjInsYes, bjInsNo;
        readonly List<RectTransform> bjPickers = new List<RectTransform>();
        int bjBet, bjLastBet = 10, bjVisible = 1; float bjRevealT; bool bjMsgShown = true;

        RectTransform BuildBlackjack()
        {
            bj = new BlackjackGame(rng, CasinoWallet.I);
            var p = Box(body, "Blackjack", 0, 0, 1920, 1080);
            Txt(p, "BLACKJACK PAYS 3 TO 2  ·  DEALER STANDS ON ALL 17s  ·  INSURANCE PAYS 2 TO 1", 0, 40, 1400, 50, 24, new Color(1f, 0.9f, 0.6f, 0.55f));
            bjDealer = Box(p, "Dealer", 0, 230, 1200, 200); bjDealerTotal = Txt(p, "", 0, 355, 400, 40, 28, Cream);
            bjHands = Box(p, "Hands", 0, -110, 1600, 300);
            bjBetChips = Box(p, "BetChips", 0, -300, 300, 80); bjBetText = Txt(p, "", 0, -255, 500, 40, 28, Cream);
            int before = pickers.Count; ChipPicker(p, -820, -400); for (int i = before; i < pickers.Count; i++) bjPickers.Add(pickers[i].r);
            foreach (var r in bjPickers) { var b = r.GetComponent<Button>(); int v = int.Parse(r.GetComponentInChildren<Text>().text); b.onClick.AddListener(() => BjAddBet(v)); }
            bjDeal = Btn(p, "DEAL", 640, -400, 260, 100, BtnHot, BjDeal, 40);
            bjClear = Btn(p, "CLEAR", 370, -400, 220, 100, BtnCol, () => { bjBet = 0; BjRefresh(); });
            bjHit = Btn(p, "HIT", -330, -400, 200, 100, BtnHot, () => { bj.Hit(); Sfx(Snd.Card); BjAfter(); }, 36);
            bjStand = Btn(p, "STAND", -110, -400, 200, 100, BtnCol, () => { bj.Stand(); BjAfter(); }, 36);
            bjDouble = Btn(p, "DOUBLE", 110, -400, 200, 100, BtnCol, () => { bj.Double(); Sfx(Snd.Card); BjAfter(); }, 32);
            bjSplit = Btn(p, "SPLIT", 330, -400, 200, 100, BtnCol, () => { bj.Split(); Sfx(Snd.Card); BjAfter(); }, 32);
            bjInsYes = Btn(p, "INSURANCE", -140, -400, 260, 100, BtnCol, () => { bj.Insure(true); BjAfter(); }, 30);
            bjInsNo = Btn(p, "NO THANKS", 140, -400, 260, 100, BtnCol, () => { bj.Insure(false); BjAfter(); }, 30);
            return p;
        }

        void BjAddBet(int v)
        {
            if (bj.phase != BlackjackGame.Phase.Betting && bj.phase != BlackjackGame.Phase.Done) return;
            if (bjBet + v > 200) { Msg("Table limit is 200 a hand."); return; }
            if (bjBet + v > CasinoWallet.Held) { Msg("Not enough chips. The cashier's by the door."); return; }
            bjBet += v; Sfx(Snd.Chip); BjRefresh();
        }

        void BjDeal()
        {
            if (bjBet <= 0) bjBet = Mathf.Min(bjLastBet, CasinoWallet.Held);
            if (bjBet < 2) { Msg("Minimum bet is 2."); return; }
            if (!bj.Deal(bjBet)) { Msg(bj.Message); return; }
            bjLastBet = bjBet; bjBet = 0; bjVisible = 1; bjMsgShown = false; Sfx(Snd.Card);
            if (bj.ShuffledThisRound) World.Toast("The dealer shuffles a fresh shoe.", 0);
            BjAfter();
        }

        void BjAfter()
        {
            if (bj.phase == BlackjackGame.Phase.Done) { bjRevealT = 0.45f; if (bjVisible < 2) bjVisible = 1; }
            else { Msg(bj.Message); bjMsgShown = true; }
            BjRefresh();
        }

        void BjUpdate()
        {
            if (bj.phase != BlackjackGame.Phase.Done || bjMsgShown) return;
            bjRevealT -= Time.unscaledDeltaTime; if (bjRevealT > 0f) return;
            if (bjVisible < bj.dealer.Count) { bjVisible++; bjRevealT = 0.55f; Sfx(Snd.Card); BjRefresh(); return; }
            bjMsgShown = true; Msg(bj.Message); Sfx(bj.LastReturn > bj.LastStake ? Snd.Win : bj.LastReturn == bj.LastStake ? Snd.Chip : Snd.Drop); BjRefresh();
        }

        void BjRefresh()
        {
            if (bjDealer == null) return;
            var ph = bj.phase; bool betting = ph == BlackjackGame.Phase.Betting || ph == BlackjackGame.Phase.Done && bjMsgShown;
            Clear(bjDealer); Clear(bjHands); Clear(bjBetChips);
            int dn = bj.dealer.Count, shown = ph == BlackjackGame.Phase.Done ? Mathf.Max(bjVisible, 1) : bj.holeShown ? dn : 1;
            for (int i = 0; i < dn; i++) CardView(bjDealer, bj.dealer[i], i < shown, (i - (dn - 1) * 0.5f) * 84f, 0);
            if (dn > 0) { var vis = bj.dealer.Take(shown).ToList(); int t = BlackjackGame.Value(vis, out bool soft); bjDealerTotal.text = "Dealer " + (shown < dn && shown == 1 ? "shows " : "") + t; }
            else bjDealerTotal.text = "";
            int hn = bj.hands.Count;
            for (int h = 0; h < hn; h++)
            {
                var hand = bj.hands[h]; float cx = (h - (hn - 1) * 0.5f) * 380f;
                for (int i = 0; i < hand.cards.Count; i++) CardView(bjHands, hand.cards[i], true, cx + (i - (hand.cards.Count - 1) * 0.5f) * 46f, 20 + i * 6f, 110f);
                bool act = ph == BlackjackGame.Phase.Player && h == bj.active;
                string res = ph == BlackjackGame.Phase.Done && bjMsgShown ? "  " + hand.Result : "";
                var t = Txt(bjHands, (act ? "▶ " : "") + hand.Total + (hand.Soft && hand.Total < 21 ? " soft" : "") + "   bet " + hand.bet + (hand.doubled ? " (doubled)" : "") + res, cx, -125, 380, 40, 26, act ? Gold : Cream);
            }
            if (bj.insurance > 0) Txt(bjHands, "Insurance " + bj.insurance, 0, 150, 400, 36, 24, Cream);
            int showBet = betting ? bjBet : 0;
            bjBetText.text = betting ? (bjBet > 0 ? "Bet " + bjBet : "Tap chips to bet (DEAL repeats " + bjLastBet + ")") : "";
            if (showBet > 0) { int x = 0; foreach (int d in Denoms.Reverse()) { int n = showBet / d; showBet -= n * d; for (int k = 0; k < Mathf.Min(n, 4); k++) Chip(bjBetChips, d, -100 + x * 50 + k * 6, k * 6, 56); if (n > 0) x++; } }
            foreach (var r in bjPickers) r.gameObject.SetActive(betting);
            bjDeal.Set(betting); bjClear.Set(betting && bjBet > 0);
            bool play = ph == BlackjackGame.Phase.Player; bjHit.Set(play); bjStand.Set(play); bjDouble.Set(play && bj.CanDouble); bjSplit.Set(play && bj.CanSplit);
            bool ins = ph == BlackjackGame.Phase.Insurance; bjInsYes.Set(ins); bjInsNo.Set(ins);
        }

        // ================================================================================================ the fruit machine
        SlotMachine slot; int slotSeed = -1, slotStake = 1; RectTransform[] reels = new RectTransform[3]; Text slotWin; Bt slotSpin; readonly List<Bt> stakeBtns = new List<Bt>();
        readonly float[] reelPos = new float[3]; readonly float[] reelStopAt = new float[3]; readonly float[] reelFrom = new float[3]; readonly bool[] reelSpin = new bool[3]; float slotT; int slotPending = -1;
        readonly Dictionary<int, SlotMachine> machines = new Dictionary<int, SlotMachine>();
        Sprite[] fruit; const float SymH = 150f;
        readonly (Image img, Text txt)[,] cells = new (Image, Text)[3, 4];

        RectTransform BuildSlots()
        {
            fruit = FruitSprites();
            var p = Box(body, "Slots", 0, 0, 1920, 1080);
            Img(p, round, new Color(0.45f, 0.06f, 0.08f), -230, 30, 760, 560); // the cabinet
            Img(p, round, new Color(0.95f, 0.78f, 0.3f), -230, 30, 730, 530);
            Img(p, round, new Color(0.12f, 0.02f, 0.03f), -230, 40, 690, 470);
            for (int r = 0; r < 3; r++)
            {
                var win = Box(p, "Reel" + r, -230 + (r - 1) * 215, 40, 200, SymH * 3 - 10); win.gameObject.AddComponent<RectMask2D>();
                var bg = win.gameObject.AddComponent<Image>(); bg.color = Cream; bg.raycastTarget = false;
                reels[r] = Box(win, "Strip", 0, 0, 200, SymH * 4);
                for (int k = 0; k < 4; k++)
                {
                    var im = Img(reels[r], null, Color.white, 0, (k - 2) * SymH, 120, 120, false);
                    var tx = Txt(reels[r], "", 0, (k - 2) * SymH, 190, 120, 70, Color.red, FontStyle.Bold); tx.GetComponent<Shadow>().effectDistance = new Vector2(3, -3);
                    cells[r, k] = (im, tx);
                }
            }
            Img(p, null, new Color(1f, 0.85f, 0.2f, 0.9f), -230, 40, 690, 4); // the win line
            slotWin = Txt(p, "", -230, -265, 700, 60, 40, Gold);
            int[] stakes = { 1, 2, 5 };
            for (int k = 0; k < 3; k++) { int v = stakes[k]; stakeBtns.Add(Btn(p, "STAKE " + v, -560 + k * 200, -400, 180, 90, BtnCol, () => { if (!SlotBusy) { slotStake = v; SlotStakeShow(); } }, 26)); }
            slotSpin = Btn(p, "SPIN", 60, -400, 260, 100, BtnHot, SlotSpin, 40);
            // the pay table, on the glass beside the reels
            Img(p, round, new Color(0f, 0f, 0f, 0.45f), 560, 30, 560, 560);
            Txt(p, "PAYS (times stake)", 560, 270, 540, 40, 28, Gold);
            string[] lines = { "7  7  7", "BAR BAR BAR", "Bell Bell Bell", "Plum Plum Plum", "Orange x3", "Lemon x3", "Cherry x3", "Any bars & 7s", "Cherry Cherry", "Cherry (reel 1)" };
            int[] pays = { 100, 40, 18, 14, 10, 8, 20, 10, 5, 2 };
            for (int i = 0; i < lines.Length; i++) { Txt(p, lines[i], 480, 215 - i * 44, 360, 40, 25, Cream, FontStyle.Bold, TextAnchor.MiddleLeft); Txt(p, pays[i].ToString(), 760, 215 - i * 44, 120, 40, 26, Gold, FontStyle.Bold, TextAnchor.MiddleRight); }
            Txt(p, "Pays back 94p in the pound on average.", 560, -235, 540, 40, 20, new Color(1f, 1f, 1f, 0.6f), FontStyle.Italic);
            return p;
        }
        bool SlotBusy => reelSpin[0] || reelSpin[1] || reelSpin[2];
        void SlotStakeShow() { for (int k = 0; k < stakeBtns.Count; k++) stakeBtns[k].i.color = (new[] { 1, 2, 5 })[k] == slotStake ? BtnHot : BtnCol; }

        void SlotsEnter(int seed)
        {
            if (!machines.TryGetValue(seed, out slot)) { slot = new SlotMachine(rng, 1000 + seed); machines[seed] = slot; }
            slotSeed = seed; for (int r = 0; r < 3; r++) { reelPos[r] = slot.Stops[r]; reelSpin[r] = false; DrawReel(r); }
            slotWin.text = ""; SlotStakeShow();
        }

        void SlotSpin()
        {
            if (SlotBusy) return;
            int win = slot.Spin(slotStake, CasinoWallet.I);
            if (win < 0) { Msg("Not enough chips. The cashier's by the door."); return; }
            slotPending = win; slotT = 0f; slotWin.text = "";
            for (int r = 0; r < 3; r++)
            {
                reelSpin[r] = true;
                float target = slot.Stops[r]; // land exactly on the stop the machine chose
                float cur = reelPos[r]; float turns = 32f * (2 + r); // each reel runs a bit longer than the last
                reelFrom[r] = cur; reelStopAt[r] = cur - Mathf.Repeat(cur - target, 32f) - turns;
            }
            Sfx(Snd.Spin);
        }

        void SlotsUpdate()
        {
            if (!SlotBusy) return;
            slotT += Time.unscaledDeltaTime;
            for (int r = 0; r < 3; r++)
            {
                if (!reelSpin[r]) continue;
                float dur = 1.1f + r * 0.45f, t = Mathf.Clamp01(slotT / dur);
                float e = 1f - Mathf.Pow(1f - t, 2.5f);
                float pos = Mathf.Lerp(reelFrom[r], reelStopAt[r], e);
                reelPos[r] = pos; DrawReel(r);
                if (t >= 1f) { reelSpin[r] = false; reelPos[r] = Mathf.Round(Mathf.Repeat(reelStopAt[r], 32f)) % 32f; DrawReel(r); Sfx(Snd.Thunk); }
            }
            if (!SlotBusy)
            {
                int a = slot.SymbolAt(0, 0), b = slot.SymbolAt(1, 0), c = slot.SymbolAt(2, 0);
                if (slotPending > 0) { slotWin.text = SlotMachine.Describe(a, b, c) + ": WIN " + slotPending; Msg("Pays " + slotPending + "."); Sfx(Snd.Win); }
                else { slotWin.text = ""; Msg("Nothing."); }
            }
        }

        /// <summary>Draw a reel at a (fractional) position: the symbol at the line, one above, one below, and one coming in.</summary>
        void DrawReel(int r)
        {
            float pos = reelPos[r]; int baseStop = Mathf.FloorToInt(pos); float frac = pos - baseStop;
            reels[r].anchoredPosition = new Vector2(0, frac * SymH); // stops count up the reel; spinning runs them down past the line
            for (int k = 0; k < 4; k++)
            {
                int stop = baseStop + (k - 2); int sym = slot.Strips[r][((stop % 32) + 32) % 32];
                var (img, txt) = cells[r, k];
                bool text = sym >= 5; img.enabled = !text; txt.enabled = text;
                if (!text) img.sprite = fruit[sym];
                else { txt.text = sym == 6 ? "7" : "BAR"; txt.fontSize = sym == 6 ? 110 : 52; txt.color = sym == 6 ? new Color(0.85f, 0.05f, 0.08f) : new Color(0.08f, 0.08f, 0.1f); }
            }
        }

        /// <summary>Cherry, lemon, orange, plum and bell, drawn in code.</summary>
        Sprite[] FruitSprites()
        {
            Color A(Color c, float a) { c.a = a; return c; }
            Color Over(Color under, Color top) => Color.Lerp(under, new Color(top.r, top.g, top.b, Mathf.Max(under.a, top.a)), top.a);
            float Ell(float x, float y, float cx, float cy, float rx, float ry) { float dx = (x - cx) / rx, dy = (y - cy) / ry; return Smooth((Mathf.Sqrt(dx * dx + dy * dy) - 1f) * Mathf.Min(rx, ry)); }
            float Line(float x, float y, Vector2 a, Vector2 b, float w) { var p = new Vector2(x, y); var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude); return Smooth(Vector2.Distance(p, a + ab * t) - w); }
            Color Shade(Color c, float x, float y, float cx, float cy, float r) { float l = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), new Vector2(cx - r * 0.35f, cy + r * 0.35f)) / (r * 1.6f)); return Color.Lerp(c * 0.7f, Color.Lerp(c, Color.white, 0.35f), l); }
            var s = new Sprite[5];
            s[0] = MakeTexSprite(128, 128, (x, y) =>
            {
                var c = Color.clear;
                c = Over(c, A(new Color(0.2f, 0.55f, 0.15f), Line(x, y, new Vector2(42, 46), new Vector2(72, 112), 3f)));
                c = Over(c, A(new Color(0.2f, 0.55f, 0.15f), Line(x, y, new Vector2(88, 52), new Vector2(72, 112), 3f)));
                c = Over(c, A(new Color(0.25f, 0.65f, 0.2f), Ell(x, y, 86, 108, 18, 8)));
                c = Over(c, A(Shade(new Color(0.85f, 0.05f, 0.1f), x, y, 42, 40, 26), Ell(x, y, 42, 40, 26, 26)));
                c = Over(c, A(Shade(new Color(0.85f, 0.05f, 0.1f), x, y, 88, 46, 26), Ell(x, y, 88, 46, 26, 26)));
                return c;
            });
            s[1] = MakeTexSprite(128, 128, (x, y) => { var c = A(Shade(new Color(1f, 0.88f, 0.15f), x, y, 64, 64, 50), Ell(x, y, 64, 64, 50, 36)); c = Over(c, A(new Color(0.95f, 0.8f, 0.1f), Ell(x, y, 14, 64, 8, 7))); return Over(c, A(new Color(0.95f, 0.8f, 0.1f), Ell(x, y, 114, 64, 8, 7))); });
            s[2] = MakeTexSprite(128, 128, (x, y) => { var c = A(Shade(new Color(1f, 0.55f, 0.05f), x, y, 64, 58, 46), Ell(x, y, 64, 58, 46, 46)); return Over(c, A(new Color(0.2f, 0.6f, 0.15f), Ell(x, y, 78, 108, 16, 8))); });
            s[3] = MakeTexSprite(128, 128, (x, y) => { var c = A(Shade(new Color(0.45f, 0.12f, 0.5f), x, y, 64, 60, 46), Ell(x, y, 64, 60, 40, 48)); return Over(c, A(new Color(0.3f, 0.5f, 0.15f), Line(x, y, new Vector2(64, 104), new Vector2(70, 120), 3f))); });
            s[4] = MakeTexSprite(128, 128, (x, y) =>
            {
                // a bell: flared at the bottom, domed on top, with a clapper
                float yy = (y - 26f) / 80f; float half = yy < 0 || yy > 1 ? 0 : Mathf.Lerp(52f, 22f, Mathf.Pow(yy, 0.7f)); if (yy > 0.85f) half = Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((yy - 0.85f) / 0.17f, 2f))) * 26f;
                float body2 = Smooth(Mathf.Abs(x - 64f) - half); if (yy < 0f || yy > 1.02f) body2 = 0f;
                var c = A(Shade(new Color(1f, 0.78f, 0.15f), x, y, 64, 70, 50), body2);
                c = Over(c, A(new Color(0.85f, 0.6f, 0.1f), Ell(x, y, 64, 22, 12, 10)));
                return Over(c, A(new Color(0.9f, 0.65f, 0.1f), Smooth(Mathf.Abs(y - 28f) - 4f) * Smooth(Mathf.Abs(x - 64f) - 54f)));
            });
            return s;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ASBO.Casino
{
    public partial class CasinoUI
    {
        HoldemGame poker; RectTransform pkLayer; Text pkPot; float pkT; int pkRaise; bool pkWasInHand;
        Bt pkFold, pkCall, pkRaiseBtn, pkMin, pkHalf, pkPotBtn, pkAllIn, pkSit40, pkSit100, pkSit200, pkTop;
        static readonly Vector2[] SeatAt = { new Vector2(0, -255), new Vector2(-600, -150), new Vector2(-600, 220), new Vector2(0, 335), new Vector2(600, 220), new Vector2(600, -150) };

        RectTransform BuildPoker()
        {
            poker = new HoldemGame(rng);
            var p = Box(body, "Poker", 0, 0, 1920, 1080);
            var table = Img(p, MakeTexSprite(512, 256, (x, y) =>
            {
                float dx = (x - 256f) / 256f, dy = (y - 128f) / 128f, r = Mathf.Sqrt(dx * dx + dy * dy); if (r > 1f) return Color.clear;
                Color c = r > 0.9f ? new Color(0.3f, 0.16f, 0.07f) * (0.8f + 0.25f * (1f - Mathf.Abs(r - 0.95f) * 20f)) : FeltPixel(x, y) * (1.05f - r * 0.25f);
                c.a = Mathf.Clamp01((1f - r) * 200f); return c;
            }), Color.white, 0, 45, 1420, 640, false);
            pkLayer = Box(p, "Seats", 0, 0, 1920, 1080);
            pkPot = Txt(p, "", 0, -40, 500, 44, 30, Cream);
            pkFold = Btn(p, "FOLD", -600, -440, 200, 90, BtnCol, () => PkAct(HoldemGame.Act.Fold), 30);
            pkCall = Btn(p, "CHECK", -370, -440, 240, 90, BtnCol, () => PkAct(HoldemGame.Act.Call), 28);
            pkRaiseBtn = Btn(p, "RAISE", -100, -440, 280, 90, BtnHot, () => PkAct(HoldemGame.Act.Raise), 28);
            pkMin = Btn(p, "MIN", 130, -440, 130, 80, BtnCol, () => PkSize(0), 24);
            pkHalf = Btn(p, "½ POT", 275, -440, 130, 80, BtnCol, () => PkSize(1), 24);
            pkPotBtn = Btn(p, "POT", 420, -440, 130, 80, BtnCol, () => PkSize(2), 24);
            pkAllIn = Btn(p, "ALL IN", 565, -440, 130, 80, BtnCol, () => PkSize(3), 24);
            pkSit40 = Btn(p, "SIT DOWN 40", -300, -440, 260, 90, BtnHot, () => PkSit(40), 26);
            pkSit100 = Btn(p, "SIT DOWN 100", 0, -440, 260, 90, BtnHot, () => PkSit(100), 26);
            pkSit200 = Btn(p, "SIT DOWN 200", 300, -440, 260, 90, BtnHot, () => PkSit(200), 26);
            pkTop = Btn(p, "TOP UP 50", 800, -440, 220, 80, BtnCol, () => { if (poker.TopUp(Mathf.Min(50, HoldemGame.MaxBuy - poker.Me.stack), CasinoWallet.I)) PkRefresh(); else Msg("Top up between hands, up to 200 on the table."); }, 24);
            return p;
        }

        void PokerEnter() { pkT = 0.6f; PkRefresh(); }

        void PkSit(int amt)
        {
            if (!poker.Sit(amt, CasinoWallet.I)) { Msg("You need " + amt + " chips to sit down. The cashier's by the door."); return; }
            Msg("You take the empty seat. Blinds are 1 and 2."); pkT = 0.8f; PkRefresh();
        }

        void PkSize(int k)
        {
            if (!poker.MyTurn) return;
            int pot = poker.Pot, call = poker.ToCall, cur = poker.currentBet;
            int to = k == 0 ? poker.MinRaiseTo : k == 1 ? cur + Mathf.Max(poker.minRaise, (pot + call) / 2) : k == 2 ? cur + Mathf.Max(poker.minRaise, pot + call) : poker.MaxRaiseTo;
            pkRaise = Mathf.Clamp(to, poker.MinRaiseTo, poker.MaxRaiseTo); PkRefresh();
        }

        void PkAct(HoldemGame.Act a)
        {
            if (!poker.MyTurn) return;
            poker.HumanAct(a, pkRaise); Sfx(a == HoldemGame.Act.Fold ? Snd.Card : Snd.Chip); pkT = 0.7f; pkRaise = 0; PkRefresh();
        }

        void PokerUpdate()
        {
            if (!poker.Seated) return;
            pkT -= Time.unscaledDeltaTime; if (pkT > 0f) return;
            if (poker.InHand && !poker.MyTurn) { poker.Step(); Sfx(Snd.Chip); pkT = 0.85f; PkRefresh(); return; }
            if (!poker.InHand)
            {
                if (poker.Me.stack <= 0) { Msg("You're out of chips. Top up or leave the table."); pkT = 1f; return; }
                if (poker.StartHand()) { Sfx(Snd.Card); pkT = 0.9f; PkRefresh(); }
                else pkT = 1f;
            }
        }

        void PkRefresh()
        {
            if (pkLayer == null) return;
            Clear(pkLayer);
            bool seated = poker.Seated, inHand = poker.InHand, mine = poker.MyTurn, over = poker.street == HoldemGame.Street.Showdown;
            for (int i = 0; i < 6; i++)
            {
                var s = poker.seats[i]; var at = SeatAt[i]; if (s.empty && i != HoldemGame.Human) continue;
                if (i == HoldemGame.Human && !seated) { Txt(pkLayer, "Empty seat", at.x, at.y, 300, 40, 26, new Color(1, 1, 1, 0.5f)); continue; }
                bool turn = inHand && poker.toAct == i, won = poker.Winners.Any(w => w.seat == i);
                var plate = Img(pkLayer, round, turn ? new Color(0.55f, 0.42f, 0.1f, 0.95f) : won ? new Color(0.15f, 0.45f, 0.2f, 0.95f) : new Color(0f, 0f, 0f, 0.65f), at.x, at.y, 250, 76);
                Txt(plate.transform, s.name, 0, 14, 240, 34, 26, i == HoldemGame.Human ? Gold : Cream);
                Txt(plate.transform, s.stack.ToString() + (s.allIn && inHand ? "  ALL IN" : ""), 0, -18, 240, 30, 22, Color.white, FontStyle.Normal);
                if (!string.IsNullOrEmpty(s.last)) Txt(pkLayer, s.last, at.x, at.y - 56, 260, 30, 22, new Color(1f, 0.95f, 0.75f), FontStyle.Italic);
                // cards: yours face up, theirs face down until they're shown at the end
                if (s.hole.Count == 2 && !s.folded && (s.inHand || over))
                {
                    bool show = i == HoldemGame.Human || over && poker.Showing.Contains(i);
                    Vector2 cpos = at + (Vector2.zero - at).normalized * (i == HoldemGame.Human ? 0f : 120f) + (i == HoldemGame.Human ? new Vector2(0, 120) : Vector2.zero);
                    float w = i == HoldemGame.Human ? 104f : 64f;
                    for (int k = 0; k < 2; k++) CardView(pkLayer, s.hole[k], show, cpos.x + (k - 0.5f) * (w * 0.62f), cpos.y, w);
                }
                if (s.bet > 0) { var bp = Vector2.Lerp(at, new Vector2(0, 40), 0.42f); Chip(pkLayer, s.bet, bp.x, bp.y, 44); Txt(pkLayer, s.bet.ToString(), bp.x + 46, bp.y, 80, 30, 22, Cream, FontStyle.Bold, TextAnchor.MiddleLeft); }
                if (i == poker.button && (inHand || over)) { var d = Img(pkLayer, circle, Color.white, at.x + 150, at.y + 30, 40, 40); var dt = Txt(d.transform, "D", 0, 0, 40, 40, 24, Color.black); dt.GetComponent<Shadow>().enabled = false; }
            }
            for (int k = 0; k < poker.board.Count; k++) CardView(pkLayer, poker.board[k], true, (k - 2) * 112f, 95f, 100f);
            pkPot.text = inHand || over ? "Pot " + poker.Pot : "";
            if (over && !string.IsNullOrEmpty(poker.Message)) Msg(poker.Message);
            else if (mine) Msg(poker.ToCall > 0 ? poker.ToCall + " to call." : "Check or bet.");
            else if (inHand) Msg(poker.seats[poker.toAct >= 0 ? poker.toAct : 0].name + " is thinking...");
            // your buttons
            pkFold.Set(mine); pkCall.Set(mine); pkRaiseBtn.Set(mine && poker.Me.stack > poker.ToCall); pkMin.Set(mine); pkHalf.Set(mine); pkPotBtn.Set(mine); pkAllIn.Set(mine);
            if (mine)
            {
                if (pkRaise < poker.MinRaiseTo) pkRaise = poker.MinRaiseTo;
                pkCall.Label(poker.ToCall > 0 ? (poker.ToCall >= poker.Me.stack ? "CALL ALL IN" : "CALL " + poker.ToCall) : "CHECK");
                pkRaiseBtn.Label((poker.currentBet == 0 ? "BET " : "RAISE TO ") + pkRaise);
            }
            pkSit40.Set(!seated); pkSit100.Set(!seated); pkSit200.Set(!seated);
            pkTop.Set(seated && !(inHand && poker.Me.Live) && poker.Me.stack < HoldemGame.MaxBuy);
        }
    }
}

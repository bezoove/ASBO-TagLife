using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ASBO.Slice;

namespace ASBO.Casino
{
    public partial class CasinoUI
    {
        RouletteGame rou;
        RectTransform wheel, ball, betLayer, histRow; Text resultText; Bt spinBtn, clearBtn, rebetBtn;
        bool rouSpinning; float rouT, rouDur = 5.2f, wheelFrom, wheelTo, ballFrom, ballTo; int rouResult;
        readonly Dictionary<string, RectTransform> chipViews = new Dictionary<string, RectTransform>();
        List<RouletteGame.Bet> lastBets = new List<RouletteGame.Bet>();
        const float CW = 78f, CH = 78f, GX = -330f, GY = -40f, WR = 230f; // layout cell size and where the number grid starts; wheel radius
        static readonly Vector2 WheelAt = new Vector2(-640f, 40f);
        readonly Dictionary<RouletteGame.Kind, Vector2> outsidePos = new Dictionary<RouletteGame.Kind, Vector2>();
        readonly Dictionary<string, Vector2> outsideKeyPos = new Dictionary<string, Vector2>();

        RectTransform BuildRoulette()
        {
            rou = new RouletteGame(rng);
            var p = Box(body, "Roulette", 0, 0, 1920, 1080);
            // ---- the wheel: drawn into a texture (rim, 37 pockets, cone, turret), numbers as text riding round with it
            wheel = Box(p, "Wheel", WheelAt.x, WheelAt.y, WR * 2, WR * 2);
            var wi = wheel.gameObject.AddComponent<Image>(); wi.sprite = WheelSprite(); wi.raycastTarget = false;
            float a = 360f / 37f;
            for (int k = 0; k < 37; k++)
            {
                int n = RouletteGame.Wheel[k]; float th = k * a * Mathf.Deg2Rad, rr = WR * 0.8f;
                var t = Txt(wheel, n.ToString(), Mathf.Sin(th) * rr, Mathf.Cos(th) * rr, 40, 30, 19, Color.white); t.rectTransform.localRotation = Quaternion.Euler(0, 0, -k * a);
                t.GetComponent<Shadow>().enabled = false;
            }
            ball = Box(p, "Ball", WheelAt.x, WheelAt.y + WR * 0.92f, 20, 20); var bi = ball.gameObject.AddComponent<Image>(); bi.sprite = circle; bi.color = new Color(0.97f, 0.97f, 0.95f); bi.raycastTarget = false;
            resultText = Txt(p, "", WheelAt.x, WheelAt.y - WR - 60, 400, 80, 56, Color.white);
            histRow = Box(p, "History", 130, 300, 1100, 50);

            // ---- the layout: white lines under red, black and green cells
            float gw = CW * 12, gh = CH * 3;
            Img(p, null, Cream, GX + gw * 0.5f - CW * 0.5f + CW * 0.5f, GY + gh * 0.5f, gw + CW + 4, gh + 4); // under the numbers and zero
            for (int c = 0; c < 12; c++) for (int r = 0; r < 3; r++)
                {
                    int n = RouletteGame.NumAt(c, r); float cx = GX + (c + 0.5f) * CW, cy = GY + (r + 0.5f) * CH;
                    Img(p, null, RouletteGame.IsRed(n) ? new Color(0.72f, 0.08f, 0.08f) : new Color(0.08f, 0.08f, 0.09f), cx, cy, CW - 3, CH - 3);
                    Txt(p, n.ToString(), cx, cy, CW, CH, 30, Color.white);
                }
            Img(p, null, new Color(0.0f, 0.45f, 0.2f), GX - CW * 0.5f, GY + gh * 0.5f, CW - 3, gh - 3); Txt(p, "0", GX - CW * 0.5f, GY + gh * 0.5f, CW, CH, 34, Color.white);
            // the strip under the numbers for streets and six lines
            Img(p, null, new Color(1f, 1f, 1f, 0.08f), GX + gw * 0.5f, GY - CH * 0.25f, gw, CH * 0.5f - 2);
            // the tap catcher over numbers, zero and the street strip
            var catcher = Box(p, "Tap", GX + (gw - CW) * 0.5f, GY + (gh - CH * 0.5f) * 0.5f, gw + CW, gh + CH * 0.5f);
            var ci = catcher.gameObject.AddComponent<Image>(); ci.color = new Color(0, 0, 0, 0f);
            catcher.gameObject.AddComponent<Tapper>().onTap = lp =>
            {
                // local point (centre of the catcher = origin) to layout units
                float gx = (lp.x + (gw + CW) * 0.5f) / CW - 1f, gy = (lp.y + (gh + CH * 0.5f) * 0.5f) / CH - 0.5f;
                RouPlace(RouletteGame.BetAt(gx, gy));
            };
            // outside bets: columns (2 to 1) at the right end of each row, dozens, then the even-money boxes
            for (int r = 0; r < 3; r++) { int rr = r; OutsideBox(p, "2 to 1", GX + gw + CW * 0.5f, GY + (r + 0.5f) * CH, CW - 3, CH - 3, RouletteGame.Outside(RouletteGame.Kind.Column, rr)); }
            string[] dz = { "1st 12", "2nd 12", "3rd 12" };
            for (int d = 0; d < 3; d++) OutsideBox(p, dz[d], GX + (d * 4 + 2) * CW, GY - CH * 0.5f - 34, CW * 4 - 3, 62, RouletteGame.Outside(RouletteGame.Kind.Dozen, d));
            var even = new (string, RouletteGame.Kind, Color)[] { ("1-18", RouletteGame.Kind.Low, Felt), ("EVEN", RouletteGame.Kind.Even, Felt), ("RED", RouletteGame.Kind.Red, new Color(0.72f, 0.08f, 0.08f)), ("BLACK", RouletteGame.Kind.Black, new Color(0.08f, 0.08f, 0.09f)), ("ODD", RouletteGame.Kind.Odd, Felt), ("19-36", RouletteGame.Kind.High, Felt) };
            for (int e = 0; e < 6; e++) OutsideBox(p, even[e].Item1, GX + (e * 2 + 1) * CW, GY - CH * 0.5f - 100, CW * 2 - 3, 62, RouletteGame.Outside(even[e].Item2), even[e].Item3);
            betLayer = Box(p, "Bets", 0, 0, 1920, 1080);

            ChipPicker(p, -820, -400);
            spinBtn = Btn(p, "SPIN", 640, -400, 260, 100, BtnHot, RouSpin, 40);
            clearBtn = Btn(p, "CLEAR", 360, -400, 220, 100, BtnCol, () => { if (!rouSpinning) { rou.ClearBets(CasinoWallet.I); RouRefresh(); } });
            rebetBtn = Btn(p, "REBET", 110, -400, 220, 100, BtnCol, RouRebet);
            Txt(p, "Straight 35/1  Split 17/1  Street 11/1  Corner 8/1  Six line 5/1  Dozen, column 2/1  Red, black, odd, even, 1-18, 19-36 evens", 130, -300, 1300, 40, 20, new Color(1f, 1f, 1f, 0.65f), FontStyle.Normal);
            return p;
        }

        void OutsideBox(RectTransform p, string label, float x, float y, float w, float h, RouletteGame.Bet bet, Color? col = null)
        {
            var i = Img(p, null, col ?? Felt, x, y, w, h); i.raycastTarget = true;
            var ol = i.gameObject.AddComponent<Outline>(); ol.effectColor = Cream; ol.effectDistance = new Vector2(2, -2);
            Txt(p, label, x, y, w, h, 26, Color.white);
            var b = i.gameObject.AddComponent<Button>(); b.targetGraphic = i; b.onClick.AddListener(() => RouPlace(bet));
            outsideKeyPos[bet.Key] = new Vector2(x, y);
        }

        Sprite WheelSprite()
        {
            const int S = 512; float c = S / 2f, a = 360f / 37f;
            return MakeTexSprite(S, S, (x, y) =>
            {
                float dx = x - c, dy = y - c, r = Mathf.Sqrt(dx * dx + dy * dy) / c; if (r > 1f) return Color.clear;
                float edge = Mathf.Clamp01((1f - r) * c);
                Color col;
                if (r > 0.93f) col = new Color(0.32f, 0.17f, 0.08f) * (0.85f + 0.15f * Mathf.Sin(r * 80f)); // wooden rim
                else if (r > 0.7f)
                {
                    float ang = (Mathf.Atan2(dx, dy) * Mathf.Rad2Deg + 360f + a * 0.5f) % 360f; int k = (int)(ang / a) % 37; float f = ang / a - (int)(ang / a);
                    int n = RouletteGame.Wheel[k];
                    col = n == 0 ? new Color(0.02f, 0.5f, 0.22f) : RouletteGame.IsRed(n) ? new Color(0.72f, 0.07f, 0.07f) : new Color(0.07f, 0.07f, 0.08f);
                    if (f < 0.04f || f > 0.96f || r > 0.92f || (r < 0.715f)) col = new Color(0.85f, 0.75f, 0.45f); // brass frets
                    else if (r < 0.77f) col *= 0.75f; // the pocket floor where the ball sits
                }
                else if (r > 0.16f) col = Color.Lerp(new Color(0.2f, 0.1f, 0.05f), new Color(0.45f, 0.26f, 0.12f), (r - 0.16f) / 0.54f); // the cone
                else col = new Color(0.85f, 0.72f, 0.38f) * (0.8f + 0.25f * (1f - r / 0.16f)); // turret
                col.a = edge; return col;
            });
        }

        void RouPlace(RouletteGame.Bet b)
        {
            if (rouSpinning || b == null) return;
            if (!rou.Place(b, chipPick, CasinoWallet.I)) { Msg(CasinoWallet.Held < chipPick ? "Not enough chips. The cashier's by the door." : "Table limit: 100 a number, 250 outside, 500 a spin."); return; }
            Msg(b.Label + " (" + b.Pays + " to 1): " + rou.Bets[b.Key].stake + " on it. Total staked " + rou.Staked + ".");
            RouRefresh(); Sfx(Snd.Chip);
        }

        void RouRebet()
        {
            if (rouSpinning || lastBets.Count == 0) return;
            foreach (var b in lastBets) if (!rou.Place(b, b.stake, CasinoWallet.I)) { Msg("Not enough chips to rebet it all."); break; }
            RouRefresh();
        }

        /// <summary>Where a bet's chips sit on the layout: on its number, line, corner or box.</summary>
        Vector2 BetPos(RouletteGame.Bet b)
        {
            if (outsideKeyPos.TryGetValue(b.Key, out var o)) return o;
            Vector2 Cell(int n) => n == 0 ? new Vector2(GX - CW * 0.5f, GY + CH * 1.5f) : new Vector2(GX + ((n - 1) / 3 + 0.5f) * CW, GY + ((n - 1) % 3 + 0.5f) * CH);
            switch (b.kind)
            {
                case RouletteGame.Kind.Street: return new Vector2(Cell(b.nums[0]).x, GY - CH * 0.25f);
                case RouletteGame.Kind.SixLine: return new Vector2((Cell(b.nums[0]).x + Cell(b.nums[5]).x) * 0.5f, GY - CH * 0.25f);
                case RouletteGame.Kind.FirstFour: return new Vector2(GX, GY);
            }
            if (b.nums[0] == 0 && b.nums.Length > 1) { float y = 0; for (int i = 1; i < b.nums.Length; i++) y += Cell(b.nums[i]).y; return new Vector2(GX, y / (b.nums.Length - 1)); }
            Vector2 s = Vector2.zero; foreach (int n in b.nums) s += Cell(n); return s / b.nums.Length;
        }

        void RouRefresh()
        {
            if (betLayer == null) return;
            Clear(betLayer); chipViews.Clear();
            foreach (var b in rou.Bets.Values) { var pos = BetPos(b); chipViews[b.Key] = Chip(betLayer, b.stake, pos.x, pos.y, 46); }
            Clear(histRow);
            for (int i = 0; i < rou.History.Count; i++)
            {
                int n = rou.History[i];
                var im = Img(histRow, circle, n == 0 ? new Color(0f, 0.45f, 0.2f) : RouletteGame.IsRed(n) ? new Color(0.72f, 0.08f, 0.08f) : new Color(0.08f, 0.08f, 0.09f), -500 + i * 64, 0, 52, 52);
                Txt(im.transform, n.ToString(), 0, 0, 52, 52, 24, Color.white);
            }
            spinBtn.b.interactable = !rouSpinning && rou.Bets.Count > 0; clearBtn.b.interactable = !rouSpinning && rou.Bets.Count > 0; rebetBtn.b.interactable = !rouSpinning && lastBets.Count > 0 && rou.Bets.Count == 0;
        }

        void RouSpin()
        {
            if (rouSpinning || rou.Bets.Count == 0) return;
            lastBets = rou.Bets.Values.Select(b => new RouletteGame.Bet { kind = b.kind, nums = b.nums, stake = b.stake }).ToList();
            rouResult = rou.Spin(); rouSpinning = true; rouT = 0f; resultText.text = "";
            wheelFrom = wheel.localEulerAngles.z; wheelTo = wheelFrom + 360f * 3f + Random.Range(0f, 360f);
            // the ball runs the other way and drops into the winning pocket as everything stops
            int k = System.Array.IndexOf(RouletteGame.Wheel, rouResult); float pocketCw = k * 360f / 37f - wheelTo; // clockwise from the top, once the wheel has stopped
            ballFrom = ballAngle; ballTo = pocketCw;
            while (ballTo < ballFrom + 360f * 5f) ballTo += 360f; // clockwise, against the wheel
            Msg("No more bets."); RouRefresh();
            Sfx(Snd.Spin);
        }
        float ballAngle;

        void RouUpdate()
        {
            if (!rouSpinning) return;
            rouT += Time.unscaledDeltaTime; float t = Mathf.Clamp01(rouT / rouDur), e = 1f - Mathf.Pow(1f - t, 3f);
            wheel.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(wheelFrom, wheelTo, e));
            float eb = 1f - Mathf.Pow(1f - t, 2.2f); ballAngle = Mathf.Lerp(ballFrom, ballTo, eb);
            float rad = Mathf.Lerp(WR * 0.92f, WR * 0.66f, Mathf.SmoothStep(0f, 1f, (t - 0.55f) / 0.3f));
            float bounce = t > 0.55f && t < 0.85f ? Mathf.Abs(Mathf.Sin(t * 60f)) * 6f * (0.85f - t) / 0.3f : 0f;
            float th = ballAngle * Mathf.Deg2Rad; ball.anchoredPosition = WheelAt + new Vector2(Mathf.Sin(th), Mathf.Cos(th)) * (rad + bounce);
            if (t >= 1f)
            {
                rouSpinning = false; ballAngle %= 360f;
                int n = rouResult; string col = n == 0 ? "green" : RouletteGame.IsRed(n) ? "red" : "black";
                resultText.text = n.ToString(); resultText.color = n == 0 ? new Color(0.3f, 1f, 0.5f) : RouletteGame.IsRed(n) ? new Color(1f, 0.35f, 0.3f) : Color.white;
                int staked = rou.Staked; int back = rou.Settle(n, CasinoWallet.I, out var winners);
                Msg(n + " " + col + ". " + (back > 0 ? "You win " + (back - staked >= 0 ? back - staked : back) + (back - staked < 0 ? " back" : "") + " (" + string.Join(", ", winners.Select(w => w.Label)) + ")." : "Nothing for you."));
                RouRefresh(); Sfx(back > 0 ? Snd.Win : Snd.Drop);
            }
            else if (t > 0.55f && t < 0.9f && Random.value < Time.unscaledDeltaTime * 14f * (0.9f - t)) Sfx(Snd.Tick); // the ball clattering over the frets
        }

        /// <summary>Taps on the roulette layout, as a point inside the rectangle (centre = 0,0).</summary>
        class Tapper : MonoBehaviour, IPointerClickHandler
        {
            public System.Action<Vector2> onTap;
            public void OnPointerClick(PointerEventData e)
            {
                var r = (RectTransform)transform;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(r, e.position, e.pressEventCamera, out var lp)) onTap?.Invoke(lp);
            }
        }
    }
}

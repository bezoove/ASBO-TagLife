using System;
using System.Collections.Generic;
using System.Linq;

namespace ASBO.Casino
{
    /// <summary>
    /// European roulette: one zero, 37 pockets, every bet on the layout at the real payouts (so the house keeps its real
    /// 2.7%). The layout is three rows of twelve (1 at the bottom left, 3 at the top left, 34-36 at the right) with the
    /// zero on the left; <see cref="BetAt"/> works out which bet a tap means from where on the layout it lands: on a
    /// number, on the line between two (split), on a corner (four), along the bottom edge (street, or six line at a
    /// corner), against the zero (0 splits, trios, first four), or in the outside boxes.
    /// </summary>
    public class RouletteGame
    {
        /// <summary>The pockets round a European wheel, clockwise from zero.</summary>
        public static readonly int[] Wheel = { 0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10, 5, 24, 16, 33, 1, 20, 14, 31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26 };
        public static readonly HashSet<int> Reds = new HashSet<int> { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };
        public static bool IsRed(int n) => Reds.Contains(n);

        public enum Kind { Straight, Split, Street, Corner, SixLine, Trio, FirstFour, Dozen, Column, Red, Black, Odd, Even, Low, High }

        public class Bet
        {
            public Kind kind; public int[] nums; public int stake;
            public string Key => kind + ":" + string.Join(",", nums);
            public int Pays => PaysFor(kind);
            public string Label => kind switch
            {
                Kind.Straight => nums[0].ToString(),
                Kind.Split => "Split " + string.Join("/", nums),
                Kind.Street => "Street " + nums[0] + "-" + nums[2],
                Kind.Corner => "Corner " + string.Join("/", nums),
                Kind.SixLine => "Six line " + nums[0] + "-" + nums[5],
                Kind.Trio => "Trio " + string.Join("/", nums),
                Kind.FirstFour => "First four",
                Kind.Dozen => nums[0] == 1 ? "1st 12" : nums[0] == 13 ? "2nd 12" : "3rd 12",
                Kind.Column => "Column " + (nums[0] % 3 == 1 ? 1 : nums[0] % 3 == 2 ? 2 : 3),
                Kind.Red => "Red", Kind.Black => "Black", Kind.Odd => "Odd", Kind.Even => "Even", Kind.Low => "1-18", Kind.High => "19-36",
                _ => kind.ToString()
            };
        }

        /// <summary>To one (paid on top of the stake back).</summary>
        public static int PaysFor(Kind k) => k switch
        {
            Kind.Straight => 35, Kind.Split => 17, Kind.Street => 11, Kind.Trio => 11, Kind.Corner => 8, Kind.FirstFour => 8, Kind.SixLine => 5,
            Kind.Dozen => 2, Kind.Column => 2, _ => 1
        };

        public static Bet Make(Kind k, params int[] nums) => new Bet { kind = k, nums = nums.OrderBy(x => x).ToArray() };
        static int[] Range(Func<int, bool> f) => Enumerable.Range(1, 36).Where(f).ToArray();
        public static Bet Outside(Kind k, int which = 0) => k switch
        {
            Kind.Dozen => new Bet { kind = k, nums = Range(n => (n - 1) / 12 == which) },
            Kind.Column => new Bet { kind = k, nums = Range(n => (n - 1) % 3 == which) },
            Kind.Red => new Bet { kind = k, nums = Range(IsRed) },
            Kind.Black => new Bet { kind = k, nums = Range(n => !IsRed(n)) },
            Kind.Odd => new Bet { kind = k, nums = Range(n => n % 2 == 1) },
            Kind.Even => new Bet { kind = k, nums = Range(n => n % 2 == 0) },
            Kind.Low => new Bet { kind = k, nums = Range(n => n <= 18) },
            Kind.High => new Bet { kind = k, nums = Range(n => n >= 19) },
            _ => null
        };

        /// <summary>The number at layout column c (0..11, left to right) and row r (0 bottom .. 2 top).</summary>
        public static int NumAt(int c, int r) => c * 3 + r + 1;

        /// <summary>
        /// Which bet a tap at layout position (x, y) means. x runs 0..12 across the twelve number columns (the zero is
        /// x -1..0), y runs 0..3 up the three rows; y below 0 (down to -0.5) is the strip under the bottom row where streets
        /// and six lines go. edge = how close to a line counts as on it (in cells). Null: nowhere.
        /// </summary>
        public static Bet BetAt(float x, float y, float edge = 0.22f)
        {
            if (y < -0.5f || y > 3f || x < -1f || x > 12f) return null;
            // the zero, and the bets on the line between it and 1/2/3
            if (x < edge)
            {
                if (x < -edge || y < 0f) return x < 0f ? Make(Kind.Straight, 0) : null;
                // on the zero's edge: first four at the bottom corner, trios at the row corners, splits along the rows
                if (y < edge) return Make(Kind.FirstFour, 0, 1, 2, 3);
                int r = Clamp((int)y, 0, 2); float fy = y - r;
                if (fy < edge && r > 0) return Make(Kind.Trio, 0, NumAt(0, r - 1), NumAt(0, r));
                if (fy > 1f - edge && r < 2) return Make(Kind.Trio, 0, NumAt(0, r), NumAt(0, r + 1));
                return Make(Kind.Split, 0, NumAt(0, r));
            }
            int c = Clamp((int)x, 0, 11); float fx = x - c;
            bool nearL = fx < edge && c > 0, nearR = fx > 1f - edge && c < 11;
            int cl = nearL ? c - 1 : c; // the column to the left of the line we're on
            if (y < 0f)
            {
                // the strip under the numbers: a street, or a six line where two streets meet
                if (nearL || nearR) return Make(Kind.SixLine, Street(cl).Concat(Street(cl + 1)).ToArray());
                return Make(Kind.Street, Street(c));
            }
            int row = Clamp((int)y, 0, 2); float fyy = y - row;
            bool nearB = fyy < edge && row > 0, nearT = fyy > 1f - edge && row < 2;
            int rb = nearB ? row - 1 : row; // the row below the line we're on
            if ((nearL || nearR) && (nearB || nearT)) return Make(Kind.Corner, NumAt(cl, rb), NumAt(cl, rb + 1), NumAt(cl + 1, rb), NumAt(cl + 1, rb + 1));
            if (nearL || nearR) return Make(Kind.Split, NumAt(cl, row), NumAt(cl + 1, row));
            if (nearB || nearT) return Make(Kind.Split, NumAt(c, rb), NumAt(c, rb + 1));
            if (y < edge * 0.5f) return Make(Kind.Street, Street(c)); // right on the bottom edge
            return Make(Kind.Straight, NumAt(c, row));
        }
        static int[] Street(int c) => new[] { NumAt(c, 0), NumAt(c, 1), NumAt(c, 2) };
        static int Clamp(int v, int a, int b) => v < a ? a : v > b ? b : v;

        // ------------------------------------------------------------------------------------------ a round
        public readonly Dictionary<string, Bet> Bets = new Dictionary<string, Bet>();
        public int Staked { get { int s = 0; foreach (var b in Bets.Values) s += b.stake; return s; } }
        public readonly List<int> History = new List<int>();
        readonly Random rng;
        public RouletteGame(Random rng) { this.rng = rng; }

        /// <summary>Put chips on a bet (stacking on what's already there). Table limits: 1 to 100 a spot, 500 a spin.</summary>
        public bool Place(Bet b, int chips, IWallet w)
        {
            if (b == null || chips <= 0) return false;
            Bets.TryGetValue(b.Key, out var cur);
            int already = cur != null ? cur.stake : 0;
            int max = b.kind >= Kind.Dozen ? 250 : 100;
            if (already + chips > max || Staked + chips > 500 || !w.Take(chips)) return false;
            if (cur == null) { cur = new Bet { kind = b.kind, nums = b.nums }; Bets[b.Key] = cur; }
            cur.stake += chips; return true;
        }
        /// <summary>Take every bet back off the table (before the spin).</summary>
        public void ClearBets(IWallet w) { foreach (var b in Bets.Values) w.Pay(b.stake); Bets.Clear(); }

        public int Spin() { int n = rng.Next(37); History.Insert(0, n); if (History.Count > 12) History.RemoveAt(12); return n; }

        /// <summary>Pay out on the result: winning bets get their stake back plus the payout; the rest go to the house. Returns what came back.</summary>
        public int Settle(int result, IWallet w, out List<Bet> winners)
        {
            int back = 0; winners = new List<Bet>();
            foreach (var b in Bets.Values) if (b.nums.Contains(result)) { int r = b.stake * (b.Pays + 1); back += r; winners.Add(b); }
            if (back > 0) w.Pay(back);
            Bets.Clear(); return back;
        }
    }
}

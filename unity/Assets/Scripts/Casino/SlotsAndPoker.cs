using System;
using System.Collections.Generic;
using System.Linq;

namespace ASBO.Casino
{
    /// <summary>
    /// A three-reel fruit machine with real reel strips and a fixed pay table: 32 stops a reel, every stop equally likely,
    /// so the odds are exactly what the strips say. Over the long run it pays back 94.15p in the pound (worked out by
    /// counting every one of the 32,768 ways the reels can stop) and something wins about one spin in four.
    /// </summary>
    public class SlotMachine
    {
        public static readonly string[] Names = { "Cherry", "Lemon", "Orange", "Plum", "Bell", "Bar", "Seven" };
        // how many of each symbol on each reel (cherry, lemon, orange, plum, bell, bar, seven): 32 stops each
        static readonly int[][] Counts = { new[] { 7, 5, 5, 5, 4, 4, 2 }, new[] { 5, 6, 6, 5, 4, 4, 2 }, new[] { 3, 6, 6, 6, 5, 4, 2 } };
        public static readonly int[] ThreeOfAKind = { 20, 8, 10, 14, 18, 40, 100 };
        public const int AnyBarsAndSevens = 10, TwoCherries = 5, OneCherry = 2;
        public const double Payback = 0.9415;

        public readonly int[][] Strips = new int[3][];
        public readonly int[] Stops = new int[3];
        readonly Random rng;
        public SlotMachine(Random rng, int seed)
        {
            this.rng = rng; var order = new Random(seed); // the strips' order is fixed per machine (looks), the odds come from the counts
            for (int r = 0; r < 3; r++)
            {
                var l = new List<int>(); for (int s = 0; s < 7; s++) for (int k = 0; k < Counts[r][s]; k++) l.Add(s);
                for (int i = l.Count - 1; i > 0; i--) { int j = order.Next(i + 1); var t = l[i]; l[i] = l[j]; l[j] = t; }
                Strips[r] = l.ToArray(); Stops[r] = order.Next(32);
            }
        }
        public int SymbolAt(int reel, int offset) => Strips[reel][((Stops[reel] + offset) % 32 + 32) % 32];

        /// <summary>What the line pays, as a multiple of the stake.</summary>
        public static int Pays(int a, int b, int c)
        {
            if (a == b && b == c) return ThreeOfAKind[a];
            if (a >= 5 && b >= 5 && c >= 5) return AnyBarsAndSevens;
            if (a == 0 && b == 0) return TwoCherries;
            if (a == 0) return OneCherry;
            return 0;
        }
        public static string Describe(int a, int b, int c)
        {
            if (a == b && b == c) return "Three " + Names[a] + (a == 0 ? "ies" : "s");
            if (a >= 5 && b >= 5 && c >= 5) return "Bars and sevens";
            if (a == 0 && b == 0) return "Two cherries";
            if (a == 0) return "Cherry";
            return "";
        }

        /// <summary>Spin: takes the stake, stops the reels, pays the line. Returns the win (0 if nothing).</summary>
        public int Spin(int stake, IWallet w)
        {
            if (!w.Take(stake)) return -1;
            for (int r = 0; r < 3; r++) Stops[r] = rng.Next(32);
            int win = stake * Pays(SymbolAt(0, 0), SymbolAt(1, 0), SymbolAt(2, 0));
            if (win > 0) w.Pay(win);
            return win;
        }

        /// <summary>The exact long-run payback of the strips and pay table (every combination counted).</summary>
        public static double ExactPayback()
        {
            double tot = 0;
            for (int a = 0; a < 7; a++) for (int b = 0; b < 7; b++) for (int c = 0; c < 7; c++) tot += (double)Counts[0][a] * Counts[1][b] * Counts[2][c] * Pays(a, b, c);
            return tot / (32.0 * 32 * 32);
        }
    }

    /// <summary>
    /// Texas Hold'em at a six-seat table: you and five locals who play like people (some tight, some loose, some who
    /// bluff, some who can't fold a pair), 1/2 blinds, no limit, proper side pots when someone's all in, and the house
    /// takes 5% of each pot that sees a flop (no more than 3), like a real card room.
    /// </summary>
    public class HoldemGame
    {
        public class Seat
        {
            public string name = ""; public int stack; public bool human, empty = true, inHand, folded, allIn, acted; public int bet, put;
            public float tight, aggr, bluff; public string last = ""; public readonly List<Card> hole = new List<Card>(2);
            public bool Live => inHand && !folded;
        }
        public enum Street { Waiting, Preflop, Flop, Turn, River, Showdown }
        public enum Act { Fold, Check, Call, Raise }
        public const int SB = 1, BB = 2, MinBuy = 40, MaxBuy = 200, RakePct = 5, RakeCap = 3;
        public const int Human = 0;

        public readonly Seat[] seats = new Seat[6];
        public readonly List<Card> board = new List<Card>();
        public Street street = Street.Waiting;
        public int button, toAct = -1, currentBet, minRaise, rake;
        public string Message = "";
        public readonly List<(int seat, int won, string hand)> Winners = new List<(int, int, string)>();
        public readonly List<int> Showing = new List<int>(); // seats whose cards are turned over at the end
        readonly Random rng; readonly Shoe deck;
        static readonly string[] Locals = { "Big Steve", "Lorraine", "Wayne", "Sharon", "Mr Okafor", "Trev", "Kel", "Dougie", "Pauline", "Mo", "Bev", "Lenny", "Nadia", "Gazza", "Stan" };
        int nameIdx;

        public HoldemGame(Random rng)
        {
            this.rng = rng; deck = new Shoe(1, rng);
            for (int i = 0; i < 6; i++) seats[i] = new Seat();
            seats[Human].human = true;
            nameIdx = rng.Next(Locals.Length);
            for (int i = 1; i < 6; i++) NewLocal(seats[i]);
            button = rng.Next(6);
        }
        void NewLocal(Seat s)
        {
            s.empty = false; s.name = Locals[nameIdx++ % Locals.Length]; s.stack = 80 + rng.Next(15) * 10;
            s.tight = (float)rng.NextDouble(); s.aggr = (float)rng.NextDouble(); s.bluff = (float)(rng.NextDouble() * 0.22);
        }

        public int Pot { get { int p = 0; foreach (var s in seats) p += s.put; return p; } }
        public Seat Me => seats[Human];
        public bool Seated => !Me.empty;
        public bool InHand => street >= Street.Preflop && street <= Street.River;
        public bool MyTurn => InHand && toAct == Human;
        public int ToCall => MyTurn ? Math.Min(currentBet - Me.bet, Me.stack) : 0;
        public int MinRaiseTo => Math.Min(currentBet + minRaise, Me.bet + Me.stack);
        public int MaxRaiseTo => Me.bet + Me.stack;

        public bool Sit(int buyIn, IWallet w)
        {
            if (Seated || buyIn < MinBuy || buyIn > MaxBuy || !w.Take(buyIn)) return false;
            Me.empty = false; Me.name = "You"; Me.stack = buyIn; return true;
        }
        public bool TopUp(int amount, IWallet w)
        {
            if (!Seated || InHand && Me.Live || Me.stack + amount > MaxBuy || !w.Take(amount)) return false;
            Me.stack += amount; return true;
        }
        /// <summary>Get up (folding if you're in a hand): your stack goes back to your chips.</summary>
        public int Leave(IWallet w)
        {
            if (!Seated) return 0;
            if (InHand && Me.Live) { Me.folded = true; Me.last = "Fold"; if (toAct == Human) AfterAction(Human); }
            int s = Me.stack; Me.stack = 0; Me.empty = true; Me.inHand = false; w.Pay(s); return s;
        }

        int Next(int from, Func<Seat, bool> ok) { for (int k = 1; k <= 6; k++) { int i = (from + k) % 6; if (ok(seats[i])) return i; } return -1; }

        public bool StartHand()
        {
            if (InHand) return false;
            for (int i = 1; i < 6; i++) if (seats[i].stack <= 0) NewLocal(seats[i]); // a bust local leaves; someone else sits down
            board.Clear(); Winners.Clear(); Showing.Clear(); rake = 0; deck.Shuffle();
            foreach (var s in seats) { s.inHand = !s.empty && s.stack > 0; s.folded = s.allIn = s.acted = false; s.bet = s.put = 0; s.last = ""; s.hole.Clear(); }
            int players = seats.Count(s => s.inHand); if (players < 2) { Message = "Waiting for players."; street = Street.Waiting; return false; }
            button = Next(button, s => s.inHand);
            int sb = players == 2 ? button : Next(button, s => s.inHand), bb = Next(sb, s => s.inHand);
            Post(sb, SB, "Small blind"); Post(bb, BB, "Big blind");
            for (int r = 0; r < 2; r++) for (int k = 1; k <= 6; k++) { var s = seats[(button + k) % 6]; if (s.inHand) s.hole.Add(deck.Draw()); }
            street = Street.Preflop; currentBet = BB; minRaise = BB;
            toAct = Next(bb, s => s.Live && !s.allIn);
            Message = "";
            if (toAct < 0 || Actors() == 0) RunOut();
            return true;
        }
        void Post(int i, int amt, string what) { var s = seats[i]; int a = Math.Min(amt, s.stack); s.stack -= a; s.bet += a; s.put += a; if (s.stack == 0) s.allIn = true; s.last = what; }
        int Actors() => seats.Count(s => s.Live && !s.allIn);
        int LiveCount() => seats.Count(s => s.Live);

        /// <summary>Your move. raiseTo: the total you're making it this round (for a bet or raise).</summary>
        public void HumanAct(Act a, int raiseTo = 0) { if (MyTurn) Apply(Human, a, raiseTo); }

        /// <summary>The next local acts (the table calls this every so often so you can watch them think). False if it's not their turn.</summary>
        public bool Step()
        {
            if (!InHand || toAct < 0 || toAct == Human) return false;
            var (a, to) = Decide(toAct); Apply(toAct, a, to); return true;
        }

        void Apply(int i, Act a, int raiseTo)
        {
            var s = seats[i]; int call = Math.Min(currentBet - s.bet, s.stack);
            if (a == Act.Check && call > 0) a = Act.Call;
            if (a == Act.Raise && s.stack <= call) a = Act.Call;
            switch (a)
            {
                case Act.Fold: s.folded = true; s.last = "Fold"; break;
                case Act.Check: s.last = "Check"; break;
                case Act.Call: Pay(s, call); s.last = s.allIn ? "All in" : call == 0 ? "Check" : "Call " + call; break;
                case Act.Raise:
                {
                    int target = Math.Max(raiseTo, Math.Min(currentBet + minRaise, s.bet + s.stack)); target = Math.Min(target, s.bet + s.stack);
                    int raiseBy = target - currentBet; bool full = raiseBy >= minRaise;
                    Pay(s, target - s.bet);
                    if (full) { minRaise = raiseBy; foreach (var o in seats) if (o != s) o.acted = false; }
                    s.last = s.allIn ? "All in " + target : (currentBet == 0 ? "Bet " : "Raise to ") + target;
                    currentBet = Math.Max(currentBet, target);
                    break;
                }
            }
            s.acted = true; AfterAction(i);
        }
        void Pay(Seat s, int n) { n = Math.Min(n, s.stack); s.stack -= n; s.bet += n; s.put += n; if (s.stack == 0) s.allIn = true; }

        void AfterAction(int i)
        {
            if (LiveCount() == 1) { Award(); return; }
            bool done = seats.All(s => !s.Live || s.allIn || (s.acted && s.bet == currentBet));
            if (!done) { toAct = Next(i, s => s.Live && !s.allIn); if (toAct >= 0) return; }
            NextStreet();
        }

        void NextStreet()
        {
            foreach (var s in seats) { s.bet = 0; s.acted = false; }
            currentBet = 0; minRaise = BB;
            if (street == Street.River) { Showdown(); return; }
            if (street == Street.Preflop) { deck.Draw(); board.Add(deck.Draw()); board.Add(deck.Draw()); board.Add(deck.Draw()); street = Street.Flop; }
            else { deck.Draw(); board.Add(deck.Draw()); street = street == Street.Flop ? Street.Turn : Street.River; }
            if (Actors() < 2) { toAct = -1; RunOut(); return; }
            toAct = Next(button, s => s.Live && !s.allIn);
        }
        /// <summary>Nobody (or just one) can bet any more: deal the rest of the board and show down.</summary>
        void RunOut() { while (board.Count < 5) { deck.Draw(); board.Add(deck.Draw()); } street = Street.River; Showdown(); }

        int TakeRake() { if (board.Count < 3) return 0; rake = Math.Min(RakeCap, Pot * RakePct / 100); return rake; }

        void Award()
        {
            // everyone else folded: no cards shown
            int w = Array.FindIndex(seats, s => s.Live); int pot = Pot - TakeRake();
            seats[w].stack += pot; Winners.Add((w, pot, ""));
            Message = seats[w].name + (w == Human ? " win " : " wins ") + pot + (rake > 0 ? " (house takes " + rake + ")" : "");
            End();
        }

        void Showdown()
        {
            street = Street.Showdown; toAct = -1;
            var score = new int[6];
            for (int i = 0; i < 6; i++) if (seats[i].Live) { var all = new List<Card>(board); all.AddRange(seats[i].hole); score[i] = PokerHand.Eval(all); Showing.Add(i); }
            int r = TakeRake(); var won = new int[6];
            // side pots: one layer per all-in level; folded players' chips count, but only those still in can win
            var levels = seats.Where(s => s.put > 0).Select(s => s.put).Distinct().OrderBy(x => x).ToList();
            int prev = 0;
            foreach (int lv in levels)
            {
                int layer = 0; foreach (var s in seats) layer += Math.Max(0, Math.Min(s.put, lv) - prev);
                if (r > 0) { int t = Math.Min(r, layer); layer -= t; r -= t; }
                var elig = Enumerable.Range(0, 6).Where(i => seats[i].Live && seats[i].put >= lv).ToList();
                if (elig.Count == 0) { prev = lv; continue; } // (can't happen with a live player putting the most, but be safe)
                int best = elig.Max(i => score[i]); var win = elig.Where(i => score[i] == best).ToList();
                int share = layer / win.Count, odd = layer - share * win.Count;
                foreach (int i in win) won[i] += share;
                if (odd > 0) won[Next(button, s => win.Contains(Array.IndexOf(seats, s)))] += odd; // odd chip to the first winner left of the button
                prev = lv;
            }
            for (int i = 0; i < 6; i++) if (won[i] > 0) { seats[i].stack += won[i]; Winners.Add((i, won[i], PokerHand.Name(score[i]))); }
            var top = Winners.OrderByDescending(x => x.won).First();
            Message = string.Join(", ", Winners.Select(x => seats[x.seat].name + (x.seat == Human ? " win " : " wins ") + x.won)) + " with " + top.hand.ToLowerInvariant() + (rake > 0 ? " (house takes " + rake + ")" : "");
            End();
        }

        void End() { street = Street.Showdown; toAct = -1; foreach (var s in seats) { s.bet = 0; } }

        // ------------------------------------------------------------------------------------------ the locals' heads
        /// <summary>
        /// How a local plays: works out their chance of winning (preflop and after, by dealing the rest out a couple of
        /// hundred times against the players left in), weighs it against the price of calling, and then plays to type:
        /// the tight ones need a better hand, the aggressive ones bet and raise with it, the bluffers sometimes fire with
        /// nothing, and everyone gets a little random so they can't be read.
        /// </summary>
        (Act, int) Decide(int i)
        {
            var s = seats[i]; int call = Math.Min(currentBet - s.bet, s.stack); int pot = Pot; int opp = LiveCount() - 1;
            double eq = Equity(s.hole, board, Math.Max(1, opp), street == Street.Preflop ? 90 : 160);
            double r = rng.NextDouble(), noise = (rng.NextDouble() - 0.5) * 0.08;
            eq += noise;
            int Size(double frac) { int target = currentBet + Math.Max(minRaise, (int)Math.Round((pot + call) * frac)); return Math.Min(target, s.bet + s.stack); }
            double fair = 1.0 / (opp + 1); // what an average hand is worth against this many
            if (call == 0)
            {
                if (eq > fair * (1.45 + s.tight * 0.4) && r < 0.45 + s.aggr * 0.5) return (Act.Raise, Size(0.45 + s.aggr * 0.5));
                if (street != Street.Preflop && r < s.bluff) return (Act.Raise, Size(0.5));
                return (Act.Check, 0);
            }
            double odds = call / (double)(pot + call);
            if (eq > fair * (1.9 + s.tight * 0.5) && r < 0.3 + s.aggr * 0.55 && s.stack > call) return (Act.Raise, Size(0.6 + s.aggr * 0.6));
            if (eq >= odds + 0.03 + s.tight * 0.07) return (Act.Call, 0);
            if (street != Street.Preflop && r < s.bluff * 0.4 && call <= pot / 2) return (Act.Raise, Size(0.8));
            if (call <= BB && street == Street.Preflop && r < 0.35 - s.tight * 0.3) return (Act.Call, 0); // limps along with rubbish now and then
            return (Act.Fold, 0);
        }

        /// <summary>Chance these two cards win against n random hands with this board (deals the rest out at random).</summary>
        public double Equity(List<Card> hole, List<Card> brd, int n, int sims)
        {
            var known = new HashSet<int>(); foreach (var c in hole) known.Add(c.rank * 4 + c.suit); foreach (var c in brd) known.Add(c.rank * 4 + c.suit);
            var rest = new List<Card>(); for (int s = 0; s < 4; s++) for (int rk = 2; rk <= 14; rk++) if (!known.Contains(rk * 4 + s)) rest.Add(new Card(rk, s));
            double wins = 0; var mine = new List<Card>(7); var theirs = new List<Card>(7); var b = new List<Card>(5);
            for (int k = 0; k < sims; k++)
            {
                int need = 5 - brd.Count + n * 2;
                for (int j = 0; j < need; j++) { int x = j + rng.Next(rest.Count - j); var t = rest[j]; rest[j] = rest[x]; rest[x] = t; }
                b.Clear(); b.AddRange(brd); int p = 0; while (b.Count < 5) b.Add(rest[p++]);
                mine.Clear(); mine.AddRange(b); mine.AddRange(hole); int me = PokerHand.Eval(mine);
                bool lost = false; int ties = 0;
                for (int o = 0; o < n && !lost; o++)
                {
                    theirs.Clear(); theirs.AddRange(b); theirs.Add(rest[p++]); theirs.Add(rest[p++]);
                    int sc = PokerHand.Eval(theirs); if (sc > me) lost = true; else if (sc == me) ties++;
                }
                if (!lost) wins += 1.0 / (1 + ties);
            }
            return wins / sims;
        }
    }
}

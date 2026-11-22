using System;
using System.Collections.Generic;

namespace ASBO.Casino
{
    /// <summary>Your chips: what a game takes when you stake and pays back when you win.</summary>
    public interface IWallet { int Chips { get; } bool Take(int n); void Pay(int n); }

    /// <summary>A playing card: rank 2..14 (ace high), suit 0 spades, 1 hearts, 2 diamonds, 3 clubs.</summary>
    public readonly struct Card
    {
        public readonly int rank, suit;
        public Card(int rank, int suit) { this.rank = rank; this.suit = suit; }
        public bool Red => suit == 1 || suit == 2;
        public string RankText => rank <= 10 ? rank.ToString() : rank == 11 ? "J" : rank == 12 ? "Q" : rank == 13 ? "K" : "A";
        public string SuitText => suit == 0 ? "♠" : suit == 1 ? "♥" : suit == 2 ? "♦" : "♣";
        public override string ToString() => RankText + SuitText;
        /// <summary>Blackjack value (aces count 11 here; the hand total brings them down to 1).</summary>
        public int BJValue => rank == 14 ? 11 : rank >= 10 ? 10 : rank;
    }

    /// <summary>One or more 52-card decks, shuffled properly (Fisher-Yates), dealt until the cut card.</summary>
    public class Shoe
    {
        readonly List<Card> cards = new List<Card>(); readonly Random rng; readonly int decks; int pos, cut;
        public Shoe(int decks, Random rng) { this.decks = decks; this.rng = rng; Shuffle(); }
        public int Remaining => cards.Count - pos;
        /// <summary>Past the cut card (three quarters of the way through a multi-deck shoe): shuffle before the next round.</summary>
        public bool PastCut => pos >= cut;
        public void Shuffle()
        {
            cards.Clear();
            for (int d = 0; d < decks; d++) for (int s = 0; s < 4; s++) for (int r = 2; r <= 14; r++) cards.Add(new Card(r, s));
            for (int i = cards.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = cards[i]; cards[i] = cards[j]; cards[j] = t; }
            pos = 0; cut = decks == 1 ? cards.Count : (int)(cards.Count * (0.72 + rng.NextDouble() * 0.06));
        }
        public Card Draw() { if (pos >= cards.Count) Shuffle(); return cards[pos++]; }
    }

    /// <summary>
    /// Poker hands: the best five of up to seven cards, as one number that sorts hands correctly (category first, then
    /// the cards that matter in order), plus its name.
    /// </summary>
    public static class PokerHand
    {
        public static readonly string[] Names = { "High card", "Pair", "Two pair", "Three of a kind", "Straight", "Flush", "Full house", "Four of a kind", "Straight flush" };
        public static int Category(int score) => score >> 20;
        static int Pack(int cat, int a = 0, int b = 0, int c = 0, int d = 0, int e = 0) => (cat << 20) | (a << 16) | (b << 12) | (c << 8) | (d << 4) | e;

        /// <summary>Highest straight in a rank bitmask (bit r for rank r, ace also as 1), or 0.</summary>
        static int Straight(int mask)
        {
            if ((mask & (1 << 14)) != 0) mask |= 1 << 1;
            for (int top = 14; top >= 5; top--) { int run = 0x1F << (top - 4); if ((mask & run) == run) return top; }
            return 0;
        }

        public static int Eval(IList<Card> cs)
        {
            int n = cs.Count; var cnt = new int[15]; var suitCnt = new int[4]; var suitMask = new int[4]; int mask = 0;
            for (int i = 0; i < n; i++) { var c = cs[i]; cnt[c.rank]++; suitCnt[c.suit]++; suitMask[c.suit] |= 1 << c.rank; mask |= 1 << c.rank; }
            int fs = -1; for (int s = 0; s < 4; s++) if (suitCnt[s] >= 5) fs = s;
            if (fs >= 0) { int sf = Straight(suitMask[fs]); if (sf > 0) return Pack(8, sf); }
            int quad = 0, trip1 = 0, trip2 = 0, pair1 = 0, pair2 = 0;
            for (int r = 14; r >= 2; r--)
            {
                if (cnt[r] == 4) { if (quad == 0) quad = r; }
                else if (cnt[r] == 3) { if (trip1 == 0) trip1 = r; else if (trip2 == 0) trip2 = r; }
                else if (cnt[r] == 2) { if (pair1 == 0) pair1 = r; else if (pair2 == 0) pair2 = r; }
            }
            int Kick(int skip1, int skip2, int k, out int k2, out int k3)
            {
                var ks = new List<int>(); for (int r = 14; r >= 2 && ks.Count < 3; r--) if (cnt[r] > 0 && r != skip1 && r != skip2) ks.Add(r);
                while (ks.Count < 3) ks.Add(0);
                k2 = ks[1]; k3 = ks[2]; return ks[0];
            }
            if (quad > 0) { int k = Kick(quad, 0, 1, out _, out _); return Pack(7, quad, k); }
            if (trip1 > 0 && (trip2 > 0 || pair1 > 0)) return Pack(6, trip1, Math.Max(trip2, pair1));
            if (fs >= 0)
            {
                var top = new int[5]; int t = 0; for (int r = 14; r >= 2 && t < 5; r--) if ((suitMask[fs] & (1 << r)) != 0) top[t++] = r;
                return Pack(5, top[0], top[1], top[2], top[3], top[4]);
            }
            int st = Straight(mask); if (st > 0) return Pack(4, st);
            if (trip1 > 0) { int a = Kick(trip1, 0, 2, out int b, out _); return Pack(3, trip1, a, b); }
            if (pair1 > 0 && pair2 > 0) { int a = Kick(pair1, pair2, 1, out _, out _); return Pack(2, pair1, pair2, a); }
            if (pair1 > 0) { int a = Kick(pair1, 0, 3, out int b, out int c); return Pack(1, pair1, a, b, c); }
            var hi = new int[5]; int h = 0; for (int r = 14; r >= 2 && h < 5; r--) if (cnt[r] > 0) hi[h++] = r;
            return Pack(0, hi[0], hi[1], hi[2], hi[3], hi[4]);
        }

        public static string Name(int score) => Names[Category(score)];
    }
}

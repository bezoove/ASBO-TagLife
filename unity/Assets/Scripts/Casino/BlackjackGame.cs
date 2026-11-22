using System;
using System.Collections.Generic;

namespace ASBO.Casino
{
    /// <summary>
    /// Blackjack as dealt in a real casino: six-deck shoe shuffled at the cut card, dealer stands on all 17s, blackjack
    /// pays 3 to 2, double on any first two cards (after a split too), split pairs up to four hands (split aces get one
    /// card each), insurance when the dealer shows an ace (pays 2 to 1). The dealer checks for blackjack under an ace or
    /// a ten before anyone plays, so you never lose a double or a split to it.
    /// </summary>
    public class BlackjackGame
    {
        public class Hand
        {
            public readonly List<Card> cards = new List<Card>();
            public int bet; public bool doubled, done, fromSplit, splitAces;
            public int Total => Value(cards, out _);
            public bool Soft { get { Value(cards, out bool s); return s; } }
            public bool Bust => Total > 21;
            public bool Blackjack => cards.Count == 2 && !fromSplit && Total == 21;
            public string Result = "";
        }
        public enum Phase { Betting, Insurance, Player, Dealer, Done }

        public static int Value(List<Card> cs, out bool soft)
        {
            int t = 0, aces = 0; foreach (var c in cs) { t += c.BJValue; if (c.rank == 14) aces++; }
            while (t > 21 && aces > 0) { t -= 10; aces--; }
            soft = aces > 0; return t;
        }

        public readonly Shoe shoe; public readonly List<Hand> hands = new List<Hand>(); public readonly List<Card> dealer = new List<Card>();
        public Phase phase = Phase.Betting; public int active; public int insurance; public bool holeShown; public string Message = "Place your bet.";
        public int LastReturn, LastStake; public bool ShuffledThisRound;
        readonly IWallet w;
        public BlackjackGame(Random rng, IWallet wallet) { shoe = new Shoe(6, rng); w = wallet; }

        public Hand Current => phase == Phase.Player && active < hands.Count ? hands[active] : null;
        public int DealerTotal => Value(dealer, out _);
        public bool DealerBJ => dealer.Count == 2 && DealerTotal == 21;

        public bool Deal(int bet)
        {
            if (phase != Phase.Betting && phase != Phase.Done) return false;
            if (bet < 1 || !w.Take(bet)) { Message = "Not enough chips."; return false; }
            ShuffledThisRound = false; if (shoe.PastCut) { shoe.Shuffle(); ShuffledThisRound = true; }
            hands.Clear(); dealer.Clear(); insurance = 0; holeShown = false; active = 0; LastReturn = 0; LastStake = bet;
            var h = new Hand { bet = bet }; hands.Add(h);
            h.cards.Add(shoe.Draw()); dealer.Add(shoe.Draw()); h.cards.Add(shoe.Draw()); dealer.Add(shoe.Draw());
            if (dealer[0].rank == 14) { phase = Phase.Insurance; Message = h.Blackjack ? "Blackjack! Even money? (insurance)" : "Insurance?"; return true; }
            AfterPeek(); return true;
        }

        /// <summary>Take insurance (half your bet, pays 2 to 1 if the dealer has blackjack) or not.</summary>
        public void Insure(bool take)
        {
            if (phase != Phase.Insurance) return;
            if (take) { int i = hands[0].bet / 2; if (i > 0 && w.Take(i)) insurance = i; }
            AfterPeek();
        }

        void AfterPeek()
        {
            bool peek = dealer[0].rank == 14 || dealer[0].BJValue == 10;
            if (peek && DealerBJ) { holeShown = true; Settle("Dealer has blackjack."); return; }
            if (insurance > 0) Message = "No blackjack. Insurance lost.";
            if (hands[0].Blackjack) { holeShown = true; Settle("Blackjack!"); return; }
            phase = Phase.Player; if (insurance == 0) Message = "Hit or stand?";
        }

        public bool CanDouble => Current != null && Current.cards.Count == 2 && !Current.splitAces && w.Chips >= Current.bet;
        public bool CanSplit => Current != null && Current.cards.Count == 2 && Current.cards[0].BJValue == Current.cards[1].BJValue && hands.Count < 4 && !Current.splitAces && w.Chips >= Current.bet;

        public void Hit()
        {
            var h = Current; if (h == null) return;
            h.cards.Add(shoe.Draw());
            if (h.Total >= 21) { h.done = true; Next(); }
        }
        public void Stand() { var h = Current; if (h == null) return; h.done = true; Next(); }
        public void Double()
        {
            var h = Current; if (h == null || !CanDouble || !w.Take(h.bet)) return;
            h.bet *= 2; h.doubled = true; h.cards.Add(shoe.Draw()); h.done = true; Next();
        }
        public void Split()
        {
            var h = Current; if (h == null || !CanSplit || !w.Take(h.bet)) return;
            var h2 = new Hand { bet = h.bet, fromSplit = true }; h.fromSplit = true;
            h2.cards.Add(h.cards[1]); h.cards.RemoveAt(1);
            bool aces = h.cards[0].rank == 14; h.splitAces = h2.splitAces = aces;
            h.cards.Add(shoe.Draw()); h2.cards.Add(shoe.Draw());
            hands.Insert(active + 1, h2);
            if (aces) { h.done = h2.done = true; Next(); return; } // one card each on split aces
            if (h.Total == 21) { h.done = true; Next(); }
        }

        void Next()
        {
            while (active < hands.Count && hands[active].done) active++;
            if (active < hands.Count) { if (hands[active].cards.Count == 1) hands[active].cards.Add(shoe.Draw()); if (hands[active].Total == 21) { hands[active].done = true; Next(); } return; }
            DealerPlays();
        }

        void DealerPlays()
        {
            phase = Phase.Dealer; holeShown = true;
            bool anyLive = false; foreach (var h in hands) if (!h.Bust) anyLive = true;
            if (anyLive) while (DealerTotal < 17) dealer.Add(shoe.Draw()); // stands on all 17s, soft ones too
            Settle(null);
        }

        void Settle(string msg)
        {
            int back = 0; int d = DealerTotal; bool dbj = DealerBJ; int stake = insurance;
            if (insurance > 0 && dbj) back += insurance * 3;
            foreach (var h in hands)
            {
                stake += h.bet;
                if (h.Blackjack && !dbj) { back += h.bet + h.bet * 3 / 2; h.Result = "Blackjack"; }
                else if (h.Blackjack && dbj) { back += h.bet; h.Result = "Push"; }
                else if (dbj) h.Result = "Lose";
                else if (h.Bust) h.Result = "Bust";
                else if (d > 21 || h.Total > d) { back += h.bet * 2; h.Result = "Win"; }
                else if (h.Total == d) { back += h.bet; h.Result = "Push"; }
                else h.Result = "Lose";
            }
            if (back > 0) w.Pay(back);
            LastReturn = back; LastStake = stake; phase = Phase.Done;
            int net = back - stake;
            Message = (msg != null ? msg + " " : (d > 21 ? "Dealer busts. " : "Dealer has " + d + ". ")) + (net > 0 ? "You win " + net + "." : net < 0 ? "You lose " + (-net) + "." : "Push.");
        }
    }
}

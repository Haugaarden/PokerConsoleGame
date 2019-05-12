using System;
using System.Collections.Generic;

namespace PokerTestProgram
{
    public class Player
    {
        public string Alias { get; private set; }
        private List<Card> PlayerHand = new List<Card>();
        public int ChipsAmount { get; set; }
        public Blinds Blind { get; set; } = Blinds.NoBlind;
        public HandStrength HandStrength { get; set; }
        public int BettedThisRound { get; set; }
        public bool IsAllIn { get; set; } = false;
        public bool IsFolded { get; set; } = false;

        public Player(string alias, int chipsAmount)
        {
            Alias = alias;
            ChipsAmount = chipsAmount;
        }

        public void GiveCardsToPlayer(List<Card> cards)
        {
            PlayerHand.AddRange(cards);
        }

        public List<Card> SeePlayerCards()
        {
            return PlayerHand;
        }

        public int PlaceBet()
        {
            var parseSucceeded = false;
            int bet;
            do
            {
                Console.WriteLine("Player: " + Alias + ", place your bet...");

                var playerBet = Console.ReadLine();

                if (playerBet != null && playerBet.ToLower().Equals("f"))
                {
                    IsFolded = true;
                    throw new PlayerIsFoldedException();
                }

                parseSucceeded = int.TryParse(playerBet, out bet);
            } while (!parseSucceeded);

            return bet;
        }

        public void RemoveCardsFromPlayer()
        {
            PlayerHand = new List<Card>();
        }
    }
}
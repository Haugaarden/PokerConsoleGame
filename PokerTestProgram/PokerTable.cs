using System;
using System.Collections.Generic;

namespace PokerTestProgram
{
    public class PokerTable
    {
        public List<Player> players = new List<Player>();
        public List<Player> spectators = new List<Player>();
        public List<Card> communityCards = new List<Card>();
        public List<Pot> Pots { get; set; }
        public int HighestBetInRound { get; set; }
        public int SmallBlindAmount { get; set; } = 10;
        public int BigBlindAmount { get; set; } = 20;

        public void AddPlayerToRoom(Player newPlayer)
        {
            players.Add(newPlayer);
        }

        public void AddCommunityCard(Card communityCard)
        {
            communityCards.Add(communityCard);
        }

        public void ResetTable()
        {
            //players = new List<Player>();
            communityCards = new List<Card>();
            Pots = new List<Pot>();
            HighestBetInRound = 0;
            UnfoldPlayersAndTakeTheirCards();
        }

        private void UnfoldPlayersAndTakeTheirCards()
        {
            foreach (var player in players)
            {
                player.IsFolded = false;
                player.RemoveCardsFromPlayer();
            }
        }

        public void AddNewPot()
        {
            Pots.Add(new Pot());

            Console.WriteLine("New pot has been created");
        }
    }
}
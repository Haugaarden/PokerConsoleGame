using System.Collections.Generic;

namespace PokerTestProgram
{
    public class Pot
    {
        public List<Player> players = new List<Player>();
        public int PotAmount { get; set; } = 0;

        public bool IsLocked { get; set; } =
            false; // A pot is locked when a newer pot has been created, and the round this pot was currently used in has ended

        public bool IsAllInPot { get; set; } = false;
        public int AllInAmountToMatch { get; set; }

        public void AddToPot(Player player, int chipsAmount)
        {
            AddPlayerToPot(player);

            PotAmount += chipsAmount;
        }

        public void AddPlayerToPot(Player player)
        {
            if (IsPlayerAlreadyInPot(player) == false)
                players.Add(player);
        }

        public void RemovePlayerFromPot(Player playerToRemove)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].Alias == playerToRemove.Alias)
                {
                    players.RemoveAt(i);

                    return;
                }
            }
        }

        private bool IsPlayerAlreadyInPot(Player playerToCheck)
        {
            foreach (var player in players)
            {
                if (player.Alias == playerToCheck.Alias)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
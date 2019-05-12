using System;

namespace PokerTestProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            var deck = new CardDeck();
            var pokerRoom = new PokerTable();
            var dealer = new Dealer(pokerRoom, deck);

            pokerRoom.AddPlayerToRoom(new Player("Phil Ivey", 100));
            pokerRoom.AddPlayerToRoom(new Player("Phil Hellmuth", 100));
            pokerRoom.AddPlayerToRoom(new Player("Phil Laak", 100));
            pokerRoom.AddPlayerToRoom(new Player("Phil Phil", 200));

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine();

                dealer.ShowPlayersChips();

                dealer.RemoveBankruptPlayers();
                dealer.DistributeButtons();
                dealer.TakePaymentFromBlinds();
                dealer.DealCards();

                Console.WriteLine("Preflop bet:");
                dealer.TakeBets(true);
                dealer.DrawFlopCards();

                Console.WriteLine("\nFlop bet:");
                dealer.TakeBets();
                dealer.DrawTurnCard();

                Console.WriteLine("\nTurn bet:");
                dealer.TakeBets();
                dealer.DrawRiverCard();

                Console.WriteLine("\nRiver bet:");
                dealer.TakeBets();
                var winners = dealer.FindWinners();

                dealer.NewRound();
            }
        }
    }
}
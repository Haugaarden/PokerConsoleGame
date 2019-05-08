using System;
using System.Collections.Generic;
using System.Linq;

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
            
            dealer.DistributeBlinds();
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
            
        }
    }

    // https://exceptionnotfound.net/modeling-the-card-game-war-in-c-part-2-the-code/
    public enum Suit
    {
        Clubs,
        Diamonds,
        Hearts,
        Spades
    }

    public enum Blinds
    {
        SmallBlind,
        BigBlind,
        NoBlind
    }
    
    public class Card
    {
        public Suit Suit { get; private set; }
        
        public int Value { get; private set; }

        public Card(Suit suit, int value)
        {
            Suit = suit;    
            Value = value;
        }
    }

    public class Pot
    {
        public List<Player> players = new List<Player>();
        public int PotAmount { get; set; }
    }

    public class Player
    {
        public string Alias { get; private set; }
        private List<Card> PlayerHand = new List<Card>();
        public int ChipsAmount { get; set; }
        public Blinds Blind { get; set; } = Blinds.NoBlind;
        public HandStrength HandStrength { get; set; }
        public int BettedThisRound { get; set; }
        public bool IsAllIn { get; set; } = false;

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
                Console.WriteLine("Player: " + Alias +", place your bet...");
                parseSucceeded = int.TryParse(Console.ReadLine(), out bet);
            } while (!parseSucceeded);

            return bet;
        }

        public void RemoveCardsFromPlayer()
        {
            PlayerHand = new List<Card>();
        }
    }

    public class CardDeck
    {
        private List<Card> cards = new List<Card>();

        public CardDeck()
        {
            ResetDeck();
        }

        public void ResetDeck()
        {
            cards = new List<Card>();
            
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                for (var value = 1; value <= 13; value++)
                {
                    cards.Add(new Card(suit, value));
                }
            }
        }

        public Card PeekDeck()
        {
            return cards.First();
        }

        public Card TakeTopCard()
        {
            var cardToGive = cards.First();
            cards.Remove(cardToGive);
            return cardToGive;
        }

        // https://stackoverflow.com/a/1262619
        public void ShuffleCards(int numberOfShuffles = 1)
        {
            var rnd = new Random();

            for (int i = 0; i < numberOfShuffles; i++)
            {
                var n = cards.Count;
                while (n > 1)
                {
                    n--;
                    int k = rnd.Next(n + 1);
                    var value = cards[k];
                    cards[k] = cards[n];
                    cards[n] = value;
                }
            }
        }
    }

    public class PokerTable
    {
        public List<Player> players = new List<Player>();
        public List<Card> communityCards = new List<Card>();
        public int Pot { get; set; }
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

        public void AddToPot(int chipsAmount)
        {
            Pot += chipsAmount;
        }

        public void ResetRoom()
        {
            players = new List<Player>();
            communityCards = new List<Card>();
            Pot = 0;
        }
    }

    public class Dealer
    {
        private PokerTable _pokerTable;
        private CardDeck _cardDeck;
        private HandLogic _handLogic = new HandLogic();
        
        public Dealer(PokerTable pokerTable, CardDeck cardDeck)
        {
            _pokerTable = pokerTable;
            _cardDeck = cardDeck;
        }

        public void DistributeBlinds()
        {
            if (IsFirstRound())
            {
                _pokerTable.players[0].Blind = Blinds.SmallBlind;
                _pokerTable.players[1].Blind = Blinds.BigBlind;
            }
            else
            {
                for (int i = 0; i < _pokerTable.players.Count; i++)
                {
                    if (_pokerTable.players[i].Blind == Blinds.BigBlind)
                    {
                        // Pass on the big blind to the next player
                        if (i + 1 < _pokerTable.players.Count)
                        {
                            _pokerTable.players[i + 1].Blind = Blinds.BigBlind;
                        }
                        else
                        {
                            _pokerTable.players.First().Blind = Blinds.BigBlind;
                        }
                        
                        // Remove the small blind from the previous player
                        if(i - 1 >= 0)
                        {
                            _pokerTable.players[i - 1].Blind = Blinds.NoBlind;
                        }
                        else
                        {
                            _pokerTable.players.Last().Blind = Blinds.NoBlind;
                        }
                        
                        //Assign small blind to the previous big blind
                        _pokerTable.players[i].Blind = Blinds.SmallBlind;
                        
                        break;
                    }
                }
            }
        }

        public void TakePaymentFromBlinds()
        {
            foreach (var pokerPlayer in _pokerTable.players)
            {
                if (pokerPlayer.Blind == Blinds.SmallBlind)
                {
                    pokerPlayer.ChipsAmount -= 10;
                    _pokerTable.AddToPot(10);
                }
                else if (pokerPlayer.Blind == Blinds.BigBlind)
                {
                    pokerPlayer.ChipsAmount -= 20;
                    _pokerTable.AddToPot(20);
                }
            }
        }

        public void DealCards()
        {
            _cardDeck.ShuffleCards(10);

            _cardDeck.TakeTopCard(); // Remove the first card
            
            foreach (var pokerPlayer in _pokerTable.players)
            {
                var cardsToGive = new List<Card>();
                
                cardsToGive.Add(_cardDeck.TakeTopCard());
                cardsToGive.Add(_cardDeck.TakeTopCard());
                
                pokerPlayer.GiveCardsToPlayer(cardsToGive);
            }
        }

        public void DrawFlopCards()
        {
            DrawCommunityCard(true);
            DrawCommunityCard();
            DrawCommunityCard();
        }

        public void DrawTurnCard()
        {
            DrawCommunityCard(true);
        }

        public void DrawRiverCard()
        {
            DrawCommunityCard(true);
        }

        public List<Player> FindWinners()
        {
            foreach (var pokerPlayer in _pokerTable.players)
            {
                var playerHandAndCommunityCards = new List<Card>();
                playerHandAndCommunityCards.AddRange(pokerPlayer.SeePlayerCards());
                playerHandAndCommunityCards.AddRange(_pokerTable.communityCards);
                
//                handStrengths.Add(_handLogic.HandCalculator(playerHandAndCommunityCards));
                pokerPlayer.HandStrength = _handLogic.HandCalculator(playerHandAndCommunityCards);
            }

            var playersSortedByStrength = _pokerTable.players.OrderByDescending(player => player.HandStrength.HandStrongestValue);

            var winners = new List<Player>();
            int highestStrength = playersSortedByStrength.First().HandStrength.HandStrongestValue;
            foreach (var player in playersSortedByStrength)
            {
                if (player.HandStrength.HandStrongestValue == highestStrength)
                    winners.Add(player);
                else
                    break;
            }
            
            return winners;
        }
        
        private void DrawCommunityCard(bool removeFirstCard = false)
        {
            if(removeFirstCard) 
                _cardDeck.TakeTopCard();
            
            _pokerTable.AddCommunityCard(_cardDeck.TakeTopCard());
        }

        //TODO: Shift player list so the same person doesn't start every round
        
        public void TakeBets(bool isPreFlop = false)
        {
            ResetHighestBetInRound();

            // Count blinds as chips that are betted in the round
            if (isPreFlop)
            {
                _pokerTable.players[0].BettedThisRound = _pokerTable.SmallBlindAmount;
                _pokerTable.players[1].BettedThisRound = _pokerTable.BigBlindAmount;
            }
            
            // Go through all players and let them bet
            foreach (var pokerPlayer in _pokerTable.players)
            {
                TakeBetFromPlayer(pokerPlayer);
            }

            while (IsUnevenBets())
            {   
                // Go through the players again until everyone has bet the same amount
                foreach (var pokerPlayer in _pokerTable.players)
                {
                    if (pokerPlayer.IsAllIn)
                    {
                        continue;
                    }
                        
                    if (IsUnevenBets())
                    {
                        TakeBetFromPlayer(pokerPlayer);
                    }
                    else
                    {
                        return;    // Betting round is done
                    }
                }
            }
        }

        private void TakeBetFromPlayer(Player pokerPlayer)
        {
            int bet;
            
            UpdateHighestBetInRound();
            
            // Request bet as long as the bet is not valid
            do
            {
                bet = pokerPlayer.PlaceBet();
            } while (IsBetAllowed(pokerPlayer, bet) == false);
            
            // TODO: Set all-in to true when applicable
                            
            pokerPlayer.ChipsAmount -= bet;
            pokerPlayer.BettedThisRound += bet;
            _pokerTable.Pot += bet;
        }

        private bool IsBetAllowed(Player player, int bet)
        {
            // check that player has enough chips
            if (player.ChipsAmount < bet)
            {
                return false;
            }
            
            // check that player's bet is >= other player's bets
            if (player.BettedThisRound + bet < _pokerTable.HighestBetInRound)
            {
                return false;
            }

            return true;
        }

        private bool IsUnevenBets()
        {
            var bettedThisRound = _pokerTable.players.First().BettedThisRound;
            // Go through the players again until everyone has bet the same amount
            foreach (var pokerPlayer in _pokerTable.players)
            {
                if (bettedThisRound != pokerPlayer.BettedThisRound && pokerPlayer.IsAllIn == false)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateHighestBetInRound()
        {
            _pokerTable.HighestBetInRound = _pokerTable.players.OrderByDescending(player => player.BettedThisRound).First().BettedThisRound;
        }

        private void ResetHighestBetInRound()
        {
            _pokerTable.HighestBetInRound = 0;
        }

        private bool IsFirstRound()
        {
            foreach (var pokerPlayer in _pokerTable.players)
            {
                if (pokerPlayer.Blind != Blinds.NoBlind)
                {
                    return false; // It's not the first round if some players already have blinds
                }
            }

            return true;
        }
    }
}
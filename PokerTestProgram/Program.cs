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
            var pokerRoom = new PokerRoom();
            var dealer = new Dealer(pokerRoom, deck);

            pokerRoom.AddPlayerToRoom(new Player("Phil Ivey", 100));
            pokerRoom.AddPlayerToRoom(new Player("Phil Hellmuth", 100));
            pokerRoom.AddPlayerToRoom(new Player("Phil Laak", 100));
            
            dealer.DistributeBlinds();
            dealer.TakePaymentFromBlinds();
            dealer.DealCards();
            
            dealer.DrawFlopCards();
            dealer.DrawTurnCard();
            dealer.DrawRiverCard();

            dealer.FindWinner();
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

    public class Player
    {
        public string Alias { get; private set; }
        private List<Card> PlayerHand = new List<Card>();
        public int ChipsAmount { get; set; }
        public Blinds Blind { get; set; } = Blinds.NoBlind;

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

    public class PokerRoom
    {
        public List<Player> players = new List<Player>();
        public List<Card> communityCards = new List<Card>();
        public int Pot { get; set; }

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
        private PokerRoom _pokerRoom;
        private CardDeck _cardDeck;
        private HandLogic _handLogic = new HandLogic();
        
        public Dealer(PokerRoom pokerRoom, CardDeck cardDeck)
        {
            _pokerRoom = pokerRoom;
            _cardDeck = cardDeck;
        }

        public void DistributeBlinds()
        {
            if (IsFirstRound())
            {
                _pokerRoom.players[0].Blind = Blinds.SmallBlind;
                _pokerRoom.players[1].Blind = Blinds.BigBlind;
            }
            else
            {
                for (int i = 0; i < _pokerRoom.players.Count; i++)
                {
                    if (_pokerRoom.players[i].Blind == Blinds.BigBlind)
                    {
                        // Pass on the big blind to the next player
                        if (i + 1 < _pokerRoom.players.Count)
                        {
                            _pokerRoom.players[i + 1].Blind = Blinds.BigBlind;
                        }
                        else
                        {
                            _pokerRoom.players.First().Blind = Blinds.BigBlind;
                        }
                        
                        // Remove the small blind from the previous player
                        if(i - 1 >= 0)
                        {
                            _pokerRoom.players[i - 1].Blind = Blinds.NoBlind;
                        }
                        else
                        {
                            _pokerRoom.players.Last().Blind = Blinds.NoBlind;
                        }
                        
                        //Assign small blind to the previous big blind
                        _pokerRoom.players[i].Blind = Blinds.SmallBlind;
                        
                        break;
                    }
                }
            }
        }

        public void TakePaymentFromBlinds()
        {
            foreach (var pokerPlayer in _pokerRoom.players)
            {
                if (pokerPlayer.Blind == Blinds.SmallBlind)
                {
                    pokerPlayer.ChipsAmount -= 10;
                    _pokerRoom.AddToPot(10);
                }
                else if (pokerPlayer.Blind == Blinds.BigBlind)
                {
                    pokerPlayer.ChipsAmount -= 20;
                    _pokerRoom.AddToPot(20);
                }
            }
        }

        public void DealCards()
        {
            _cardDeck.ShuffleCards(10);

            _cardDeck.TakeTopCard(); // Remove the first card
            
            foreach (var pokerPlayer in _pokerRoom.players)
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

        public Player FindWinner()
        {
            //var communityRank = _handLogic.HandCalculator(_pokerRoom.communityCards);
            
            var handRankings = new List<HandStrength>();
            
            foreach (var pokerPlayer in _pokerRoom.players)
            {
                var playerHandAndCommunityCards = new List<Card>();
                playerHandAndCommunityCards.AddRange(pokerPlayer.SeePlayerCards());
                playerHandAndCommunityCards.AddRange(_pokerRoom.communityCards);
                
                handRankings.Add(_handLogic.HandCalculator(playerHandAndCommunityCards));
            }
            
            return _pokerRoom.players[0];
        }
        
        private void DrawCommunityCard(bool removeFirstCard = false)
        {
            if(removeFirstCard) 
                _cardDeck.TakeTopCard();
            
            _pokerRoom.AddCommunityCard(_cardDeck.TakeTopCard());
        }

        private bool IsFirstRound()
        {
            foreach (var pokerPlayer in _pokerRoom.players)
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
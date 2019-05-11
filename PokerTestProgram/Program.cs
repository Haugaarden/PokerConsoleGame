using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Serialization;

namespace PokerTestProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            var deck = new CardDeck();
            var pokerRoom = new PokerTable();
            var dealer = new Dealer(pokerRoom, deck);

            pokerRoom.AddPlayerToRoom(new Player("Phil Ivey", 200));
            pokerRoom.AddPlayerToRoom(new Player("Phil Hellmuth", 100));
            pokerRoom.AddPlayerToRoom(new Player("Phil Laak", 200));
            
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
        Dealer,
        NoBlind
    }
    
    public class PlayerIsFoldedException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public PlayerIsFoldedException()
        {
        }

        public PlayerIsFoldedException(string message) : base(message)
        {
        }

        public PlayerIsFoldedException(string message, Exception inner) : base(message, inner)
        {
        }

        protected PlayerIsFoldedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
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
        public int PotAmount { get; set; } = 0;
        public bool IsLocked { get; set; } = false;    // A pot is locked when a newer pot has been created, and the round this pot was currently used in has ended
        public bool IsAllInPot { get; set; } = false;
        public int AllInAmountToMatch { get; set; }
        
        public void AddToPot(Player player, int chipsAmount)
        {
            AddPlayerToPot(player);
            
            PotAmount += chipsAmount;    //TODO: Fix bug where more chips are in chipsAmount than what was betted (fx: Ivey bets 80, but chipsAmount is 100)
        }

        private void AddPlayerToPot(Player player)
        {
            if(IsPlayerAlreadyInPot(player) == false)
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
                Console.WriteLine("Player: " + Alias +", place your bet...");

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

        public void ResetRoom()
        {
            players = new List<Player>();
            communityCards = new List<Card>();
            Pots = new List<Pot>();
        }
        
        public void AddNewPot()
        {
            Pots.Add(new Pot());
            
            Console.WriteLine("New pot has been created");
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

        private void NewBettingRoundReset()
        {
            ResetHighestBetInRound();
            LockOldPots();
            ResetBettedThisRound();
        }

        private void LockOldPots()
        {
            for (int i = 0; i < _pokerTable.Pots.Count - 1; i++)
            {
                _pokerTable.Pots[i].IsLocked = true;
            }
        }

        private void ResetBettedThisRound()
        {
            foreach (var pokerPlayer in _pokerTable.players)
            {
                pokerPlayer.BettedThisRound = 0;
            }
        }
        
        // TODO: Handle bankrupt players, perhaps just fold them?

        public void DistributeButtons()
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
            _pokerTable.Pots = new List<Pot>();
            _pokerTable.AddNewPot();
            
            foreach (var pokerPlayer in _pokerTable.players)
            {
                if (pokerPlayer.Blind == Blinds.SmallBlind)
                {
                    HandleABet(pokerPlayer, _pokerTable.SmallBlindAmount, true);
                    
//                    pokerPlayer.ChipsAmount -= _pokerTable.SmallBlindAmount;
//                    _pokerTable.Pots.Last().AddToPot(_pokerTable.SmallBlindAmount);
                }
                else if (pokerPlayer.Blind == Blinds.BigBlind)
                {
                    HandleABet(pokerPlayer, _pokerTable.BigBlindAmount, true);
                    
//                    pokerPlayer.ChipsAmount -= _pokerTable.BigBlindAmount;
//                    _pokerTable.Pots.Last().AddToPot(_pokerTable.BigBlindAmount);
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
            // Calculate each player's hand strength
            foreach (var pokerPlayer in _pokerTable.players)
            {
                var playerHandAndCommunityCards = new List<Card>();
                playerHandAndCommunityCards.AddRange(pokerPlayer.SeePlayerCards());
                playerHandAndCommunityCards.AddRange(_pokerTable.communityCards);
                
                pokerPlayer.HandStrength = _handLogic.HandCalculator(playerHandAndCommunityCards);
            }

            var winners = new List<Player>();
            
            foreach (var pot in _pokerTable.Pots)
            {
                if(pot.players.Count == 0) 
                    break; // Don't find winners in an empty pot
                
                var potWinners = FindPotWinners(pot);

                PayPotWinners(potWinners, pot);
            }

            return winners;
        }

        private void PayPotWinners(List<Player> potWinners, Pot pot)
        {
            var chipsAmountForEachWinner = (pot.PotAmount / potWinners.Count);
            
            foreach (var winner in potWinners)
            {
                winner.ChipsAmount += chipsAmountForEachWinner;
                
                Console.WriteLine("Paid " + chipsAmountForEachWinner + " to " + winner.Alias);
            }
        }

        private List<Player> FindPotWinners(Pot pot)
        {
            var playersSortedByStrength = pot.players.OrderByDescending(player => player.HandStrength.HandStrongestValue);
            
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
            NewBettingRoundReset();
            
            if(removeFirstCard) 
                _cardDeck.TakeTopCard();
            
            _pokerTable.AddCommunityCard(_cardDeck.TakeTopCard());
        }

        //TODO: Make chips go to the next pot if there's more chips in it than the all-in amount to match
        
        //TODO: Perhaps use the correct rules for starting players: https://boardgames.stackexchange.com/questions/1617/texas-holdem-heads-up-blind-structure

        //TODO: Make sure a new pot is only created when it should instead of everytime someone is all-in
        
        //TODO: Shift player list so the same person doesn't start every round

        public void TakeBets(bool isPreFlop = false)
        {
            // Count blinds as chips that are betted in the round
            if (isPreFlop)
            {
                //TODO: Don't use 0 and 1, but go through players and find the ones who has actually paid the blinds
                _pokerTable.players[0].BettedThisRound = _pokerTable.SmallBlindAmount;
                _pokerTable.players[1].BettedThisRound = _pokerTable.BigBlindAmount;
            }
            
            // Go through all players and let them bet
            foreach (var pokerPlayer in _pokerTable.players)
            {
                if (pokerPlayer.IsFolded == false)
                    TakeBetFromPlayer(pokerPlayer, isPreFlop);
            }

            while (IsUnevenBets())
            {   
                // Go through the players again until everyone has bet the same amount
                foreach (var pokerPlayer in _pokerTable.players)
                {
                    if (pokerPlayer.IsFolded)
                    {
                        continue;
                    }
                    if (pokerPlayer.IsAllIn)
                    {
                        continue;
                    }
                        
                    if (IsUnevenBets())
                    {
                        TakeBetFromPlayer(pokerPlayer, isPreFlop);
                    }
                    else
                    {
                        return;    // Betting round is done
                    }
                }
            }
        }

        private void TakeBetFromPlayer(Player pokerPlayer, bool isPreFlop)
        {
            int bet;
            
            UpdateHighestBetInRound();
            
            // Request bet as long as the bet is not valid
            do
            {
                try
                {
                    bet = pokerPlayer.PlaceBet();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Catched: Player: " + pokerPlayer.Alias + " has folded and can not make a bet");
                    FoldPlayer(pokerPlayer);
                    return;
                }
            } while (IsBetAllowed(pokerPlayer, bet) == false);
            
            HandleABet(pokerPlayer, bet, isPreFlop);
        }

        private void HandleABet(Player player, int bet, bool isPreFlop)
        {
            var isGoingAllIn = IsGoingAllIn(player, bet); // placed before taking the chips from the player, so taking the chips can be done just one place

            if (bet > 0)
            {
                if (isGoingAllIn)
                {
                    HandleAllInBet(player, bet, isPreFlop);
                }
                else if (IsMultipleOpenPots())
                {
                    HandleBetInMultiplePots(player, bet);
                }
                else
                {
                    HandleNormalBet(player, bet);
                }
            }
            else
            {
                Console.WriteLine("Player: " + player.Alias + " is checking");
            }
            
            player.ChipsAmount -= bet;    //TODO: Check that placing these two lines at the bottom, has fixed bug in handleBetInMultiplePots -> All-in stuff
            player.BettedThisRound += bet;

            MoveChipsToTheCorrectPots();
        }

        private void MoveChipsToTheCorrectPots()
        {
            if (_pokerTable.Pots.Count <= 1) return;
            
            for (var i = 0; i < _pokerTable.Pots.Count - 1; i++)
            {
                var pot = _pokerTable.Pots[i];

                if (pot.IsAllInPot && (pot.PotAmount != (pot.AllInAmountToMatch * pot.players.Count)))
                {
                    var chipsToMove = pot.PotAmount - pot.AllInAmountToMatch * pot.players.Count;

                    pot.PotAmount -= chipsToMove;
                    _pokerTable.Pots[i + 1].PotAmount += chipsToMove;
                }
            }
        }

        private void HandleNormalBet(Player player, int bet)
        {
            _pokerTable.Pots.Last().AddToPot(player, bet);  
        }

        private void HandleAllInBet(Player player, int bet, bool isPreFlop = false)
        {
            player.IsAllIn = true;

            if (IsMultipleOpenPots())
            {
                HandleBetInMultiplePots(player, bet, true);
            }
            else
            {
                _pokerTable.Pots.Last().AddToPot(player, bet);
                _pokerTable.Pots.Last().AllInAmountToMatch = player.BettedThisRound + bet;
                _pokerTable.Pots.Last().IsAllInPot = true;

//                if (isPreFlop)
//                {
//                    _pokerTable.Pots.Last().AllInAmountToMatch += _pokerTable.BigBlindAmount; // If someone goes all in in pre flop, the largest blind amount should also be added to the AllInAmount
//                    // TODO: This only works if a player can't join the round with a chips amount that is smaller than the big blind. Either count players as bankrupt if they can't play the blinds, or change this logic to something better
//                }
                
                _pokerTable.AddNewPot(); // New pot to be used if other players keeps playing
                
                Console.WriteLine("Player: " + player.Alias + " is now ALL-IN!");
            }
        }

        private void HandleBetInMultiplePots(Player player, int bet, bool isAllIn = false)
        {
            var betAmountStillLeft = bet;

            foreach (var pot in _pokerTable.Pots)
            {
                if (pot.IsLocked == false && betAmountStillLeft > 0)
                {
                    if (pot.IsAllInPot)
                    {
                        pot.AddToPot(player, pot.AllInAmountToMatch - player.BettedThisRound);

                        betAmountStillLeft -= pot.AllInAmountToMatch;

                        if (isAllIn)
                        {
                            Console.WriteLine("Player: " + player.Alias + " is now ALL-IN!");
                        }
                    }
                    else
                    {
                        pot.AddToPot(player, betAmountStillLeft);

                        betAmountStillLeft = 0;

                        if (isAllIn)
                        {
                            pot.IsAllInPot = true;
                            _pokerTable.AddNewPot(); // If a player is all in, a new pot should be created when the player has filled all other pots
                            
                            Console.WriteLine("Player: " + player.Alias + " is now ALL-IN!");
                        }
                    }
                }
            }
        }

        private void UpdateHighestBetInRound()
        {
            _pokerTable.HighestBetInRound = _pokerTable.players.OrderByDescending(player => player.BettedThisRound).First().BettedThisRound;
        }

        private void ResetHighestBetInRound()
        {
            _pokerTable.HighestBetInRound = 0;
        }

        private void FoldPlayer(Player player)
        {
            player.IsFolded = true;
            
            foreach (var pot in _pokerTable.Pots)
            {
                pot.RemovePlayerFromPot(player);
            }

            if (IsOnlyOnePlayerLeft())
            {
                Console.WriteLine("Only one player left, the round should be over now!"); //TODO: Find out how to end the round, and make the last unfolded player win
            }
        }

        private bool IsOnlyOnePlayerLeft()
        {
            var unfoldedPlayers = 0;
            
            foreach (var player in _pokerTable.players)
            {
                if (player.IsFolded == false)
                {
                    unfoldedPlayers++;
                }
            }

            if (unfoldedPlayers == 1)
                return true;

            return false;
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
        
        private bool IsBetAllowed(Player player, int bet)
        {
            if (IsPlayerBettingWithEmptyHand(player, bet))
            {
                Console.WriteLine("Player: " + player.Alias + " you are ALL-IN. You can't bet any chips"); // TODO: This should only happen if the player is ALL-IN. A bankrupt player should never get this far!!
                return false;
            }

            if (IsGoingAllIn(player, bet))
            {
                return true;
            }
            
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

        private bool IsPlayerBettingWithEmptyHand(Player player, int bet)
        {
            if (bet > 0)
            {
                if (player.ChipsAmount == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsUnevenBets()
        {
            var playerWithHighestBetThisRound = _pokerTable.players.OrderByDescending(player => player.BettedThisRound).First();

            var highestBetToMatch = playerWithHighestBetThisRound.BettedThisRound;
            // Go through the players again until everyone has bet the same amount
            foreach (var pokerPlayer in _pokerTable.players)
            {
                if (highestBetToMatch != pokerPlayer.BettedThisRound && pokerPlayer.IsAllIn == false && pokerPlayer.IsFolded == false)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsGoingAllIn(Player player, int bet)
        {
            return player.ChipsAmount == bet;
        }

        private bool IsMultipleOpenPots()
        {
            var openPots = 0;
            
            foreach (var pot in _pokerTable.Pots)
            {
                if (pot.IsLocked == false)
                    openPots++;

                if (openPots > 1)
                    return true;
            }

            return false;
        }
    }
}
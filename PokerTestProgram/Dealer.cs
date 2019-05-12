using System;
using System.Collections.Generic;
using System.Linq;

namespace PokerTestProgram
{
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

        public void RemoveBankruptPlayers()
        {
            for (int i = 0; i < _pokerTable.players.Count; i++)
            {
                var player = _pokerTable.players[i];

                if (player.ChipsAmount == 0)
                {
                    Console.WriteLine("Player: " + player.Alias + " has gone bankrupt");

                    _pokerTable.spectators.Add(player);
                    _pokerTable.players.Remove(player);
                }
            }
        }

        public void DistributeButtons()
        {
            if (IsFirstRound())
            {
                _pokerTable.players[0].Blind = Blinds.SmallBlind;
                _pokerTable.players[1].Blind = Blinds.BigBlind;

                if (_pokerTable.players.Count > 2)
                {
                    _pokerTable.players.Last().Blind =
                        Blinds.Dealer; // TODO: Find other way to distribute buttons, should work for 2 players AND for more than 2 players
                }
                else
                {
                    _pokerTable.players[1].Blind = Blinds.Dealer;
                }
            }
            else
            {
                for (int i = 0; i < _pokerTable.players.Count; i++)
                {
                    // TODO: Make better circular handling please
                    // TODO: Check if dealer button is assigned to the right person when only two players are playing
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

                        // Remove make the previous small blind player, the dealer
                        if (i - 1 >= 0)
                        {
                            _pokerTable.players[i - 1].Blind = Blinds.Dealer;
                        }
                        else
                        {
                            _pokerTable.players.Last().Blind = Blinds.Dealer;
                        }

                        //Assign small blind to the previous big blind
                        _pokerTable.players[i].Blind = Blinds.SmallBlind;

                        // Remove dealer button from the player before the small blind
                        if (i - 2 >= 0)
                        {
                            _pokerTable.players[i - 2].Blind = Blinds.NoBlind;
                        }
                        else if (i - 2 == -1)
                        {
                            _pokerTable.players.Last().Blind = Blinds.NoBlind;
                        }
                        else
                        {
                            _pokerTable.players[_pokerTable.players.Count - 1].Blind = Blinds.NoBlind;
                        }

                        break;
                    }
                }
            }
        }

        public void TakePaymentFromBlinds()
        {
            _pokerTable.Pots = new List<Pot>();
            _pokerTable.AddNewPot();

            foreach (var player in _pokerTable.players)
            {
                _pokerTable.Pots.Last().AddPlayerToPot(player);
            }

            foreach (var pokerPlayer in _pokerTable.players)
            {
                if (pokerPlayer.Blind == Blinds.SmallBlind)
                {
                    // If a player can't play the blind, goes all in
                    if (pokerPlayer.ChipsAmount < _pokerTable.SmallBlindAmount)
                    {
                        HandleABet(pokerPlayer, pokerPlayer.ChipsAmount, true);
                    }
                    else
                    {
                        HandleABet(pokerPlayer, _pokerTable.SmallBlindAmount, true);
                    }
                }
                else if (pokerPlayer.Blind == Blinds.BigBlind)
                {
                    // If a player can't play the blind, goes all in
                    if (pokerPlayer.ChipsAmount < _pokerTable.BigBlindAmount)
                    {
                        HandleABet(pokerPlayer, pokerPlayer.ChipsAmount, true);
                    }
                    else
                    {
                        HandleABet(pokerPlayer, _pokerTable.BigBlindAmount, true);
                    }
                }
            }
        }

        public void DealCards()
        {
            _cardDeck.ResetDeck();

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
                if (pot.players.Count == 0)
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
            var playersSortedByStrength =
                pot.players.OrderByDescending(player => player.HandStrength.HandStrongestValue);

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

            if (removeFirstCard)
                _cardDeck.TakeTopCard();

            _pokerTable.AddCommunityCard(_cardDeck.TakeTopCard());
        }

        //TODO: Perhaps use the correct rules for starting players: https://boardgames.stackexchange.com/questions/1617/texas-holdem-heads-up-blind-structure

        //TODO: Shift player list so the correct person is the first to bet

        //TODO: If all players are all in, no more bets should be requested

        public void TakeBets(bool isPreFlop = false)
        {
            // Count blinds as chips that are betted in the round
            if (isPreFlop)
            {
                foreach (var player in _pokerTable.players)
                {
                    if (player.Blind == Blinds.SmallBlind)
                    {
                        player.BettedThisRound = _pokerTable.SmallBlindAmount;
                    }

                    if (player.Blind == Blinds.BigBlind)
                    {
                        player.BettedThisRound = _pokerTable.BigBlindAmount;
                    }
                }
            }

            // Go through all players and let them bet
            foreach (var pokerPlayer in _pokerTable.players)
            {
                if (IsOnlyOnePlayerLeft())
                {
                    break;
                }

                if (pokerPlayer.IsFolded == false)
                    TakeBetFromPlayer(pokerPlayer, isPreFlop);
            }

            while (IsUnevenBets() && IsOnlyOnePlayerLeft() == false)
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
                        return; // Betting round is done
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
            var isGoingAllIn =
                IsGoingAllIn(player,
                    bet); // placed before taking the chips from the player, so taking the chips can be done just one place

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

            player.ChipsAmount -= bet;
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

                _pokerTable.AddNewPot(); // New pot to be used if other players keeps playing

                Console.WriteLine("Player: " + player.Alias + " is now ALL-IN!");
            }
        }

        private void HandleBetInMultiplePots(Player player, int bet, bool isAllIn = false)
        {
            var betAmountStillLeft = bet;

            for (int i = 0; i < _pokerTable.Pots.Count; i++)
            {
                var pot = _pokerTable.Pots[i];

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
                            _pokerTable
                                .AddNewPot(); // If a player is all in, a new pot should be created when the player has filled all other pots
                            _pokerTable.Pots.Last().AddPlayerToPot(player);

                            Console.WriteLine("Player: " + player.Alias + " is now ALL-IN!");
                        }
                    }
                }
            }
        }

        private void UpdateHighestBetInRound()
        {
            _pokerTable.HighestBetInRound = _pokerTable.players.OrderByDescending(player => player.BettedThisRound)
                .First().BettedThisRound;
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
                Console.WriteLine(
                    "Only one player left, the round should be over now!");
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
                Console.WriteLine("Player: " + player.Alias + " you are ALL-IN. You can't bet any chips");
                return false;
            }

            if (IsGoingAllIn(player, bet))
            {
                return true;
            }

            // check that player has enough chips
            if (player.ChipsAmount < bet)
            {
                Console.WriteLine("You only have " + player.ChipsAmount + " chips, so you can't make that bet!");

                return false;
            }

            // check that player's bet is >= other player's bets
            if (player.BettedThisRound + bet < _pokerTable.HighestBetInRound)
            {
                Console.WriteLine("You have to bet atleast " + _pokerTable.HighestBetInRound +
                                  " chips, to make a valid bet!");

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
            var playerWithHighestBetThisRound =
                _pokerTable.players.OrderByDescending(player => player.BettedThisRound).First();

            var highestBetToMatch = playerWithHighestBetThisRound.BettedThisRound;

            // Go through the players again until everyone has bet the same amount
            foreach (var pokerPlayer in _pokerTable.players)
            {
                if (highestBetToMatch != pokerPlayer.BettedThisRound && pokerPlayer.IsAllIn == false &&
                    pokerPlayer.IsFolded == false)
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

        public void NewRound()
        {
            _pokerTable.ResetTable();
        }

        public void ShowPlayersChips()
        {
            Console.WriteLine("Players' chips amount:");

            foreach (var player in _pokerTable.players)
            {
                Console.WriteLine(player.Alias + ":\t" + player.ChipsAmount);
            }

            Console.WriteLine();
        }
    }
}
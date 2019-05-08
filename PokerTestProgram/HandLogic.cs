using System;
using System.Collections.Generic;
using System.Linq;

namespace PokerTestProgram
{
    // https://github.com/SaarSch/Texas_Holdem/blob/master/TexasHoldem/TexasHoldem/Logics/HandLogic.cs
    public class HandLogic
    {
        public HandStrength HandCalculator(List<Card> cards)
        {
            int handValue;
            HandRank handRank;
            var hand = new List<Card>();
            var orderedByValue = cards.OrderBy(card => card.Value).ToList();
            var boost = (int) Math.Pow(10, 6);

            //Look for cards of the same kind:
            var pairsList = new List<Card>();
            var threeOfAKindList = new List<Card>();
            var fourOfAKindList = new List<Card>();
            var i = 0;
            
            while (i < 6)
            {
                if (orderedByValue.ElementAt(i).Value == orderedByValue.ElementAt(i + 1).Value)
                {
                    if (i == 5 || orderedByValue.ElementAt(i + 2).Value != orderedByValue.ElementAt(i).Value)
                    {
                        pairsList.AddRange(orderedByValue.GetRange(i, 2));
                        i = i + 2;
                        continue;
                    }
                    if (i < 4 && orderedByValue.ElementAt(i + 3).Value == orderedByValue.ElementAt(i).Value)
                    {
                        fourOfAKindList.AddRange(orderedByValue.GetRange(i, 4));
                        break;
                    }
                    threeOfAKindList.AddRange(orderedByValue.GetRange(i, 3));
                    i = i + 3;
                    continue;
                }
                i++;
            }
            if (pairsList.Count == 6) pairsList.RemoveRange(0, 2);    // if there's more than 2 pairs, remove the pair of least value
            if (threeOfAKindList.Count == 6) threeOfAKindList.RemoveRange(0, 3);  // if there's 2x Three of a kind, remove the three of a kind of least value

            //Look for similar shape:
            var sameShapeList = cards.Where(card => card.Suit == Suit.Clubs).OrderBy(card => card.Value).ToList();
            if (sameShapeList.Count < 5)
                sameShapeList = cards.Where(card => card.Suit == Suit.Diamonds).OrderBy(card => card.Value).ToList();
            if (sameShapeList.Count < 5)
                sameShapeList = cards.Where(card => card.Suit == Suit.Hearts).OrderBy(card => card.Value).ToList();
            if (sameShapeList.Count < 5)
                sameShapeList = cards.Where(card => card.Suit == Suit.Spades).OrderBy(card => card.Value).ToList();

            //Look for ascending
            var ascending = new List<Card>();
            for (var j = 0; j < 6; j++)
            {
                for (var q = j + 1; q < 7; q++)
                {
                    var tempOrdered = new List<Card>();
                    tempOrdered.AddRange(orderedByValue);
                    tempOrdered.RemoveAt(q);
                    tempOrdered.RemoveAt(j);

                    var tempAscending = 0;
                    for (var m = 0; m < 4; m++)
                    {
                        if (tempOrdered[m].Value + 1 == tempOrdered[m + 1].Value)
                            tempAscending++;
                    }

                    if (tempAscending == 4 && SumListCard(ascending) < SumListCard(tempOrdered))
                        ascending = tempOrdered;
                }
            }

            //Decide Hand
            var temp = IsStraightFlush(ascending, sameShapeList);
            if (temp != null)
            {
                hand = temp;
                handRank = hand.ElementAt(0).Value == 10 ? HandRank.RoyalFlush : HandRank.StraightFlush;
                handValue = CalculateHandValue(hand, 8 * boost);
            }
            else if (fourOfAKindList.Count == 4)
            {
                handRank = HandRank.FourOfAKind;
                orderedByValue.RemoveAll(card => fourOfAKindList.Contains(card));
                hand.Add(orderedByValue.ElementAt(2));
                hand.AddRange(fourOfAKindList);
                handValue = CalculateHandValue(hand, 7 * boost);
            }
            else if (threeOfAKindList.Count == 3 && pairsList.Count >= 2)
            {
                handRank = HandRank.FullHouse;
                hand.AddRange(pairsList.GetRange(pairsList.Count - 2, 2));
                hand.AddRange(threeOfAKindList);
                handValue = CalculateHandValue(hand, 6 * boost);
            }
            else if (sameShapeList.Count >= 5)
            {
                hand.AddRange(sameShapeList.GetRange(sameShapeList.Count - 5, 5));
                handRank = HandRank.Flush;
                handValue = CalculateHandValue(hand, 5 * boost);
            }
            else if (ascending.Count >= 5)
            {
                hand.AddRange(ascending.GetRange(ascending.Count - 5, 5));
                handRank = HandRank.Straight;
                handValue = CalculateHandValue(hand, 4 * boost);
            }
            else if (threeOfAKindList.Count == 3)
            {
                handRank = HandRank.ThreeOfAKind;
                orderedByValue.RemoveAll(card => threeOfAKindList.Contains(card));
                hand.AddRange(orderedByValue.GetRange(2, 2));
                hand.AddRange(threeOfAKindList);
                handValue = CalculateHandValue(hand, 3 * boost);
            }
            else
            {
                switch (pairsList.Count)
                {
                    case 4:
                        handRank = HandRank.TwoPair;
                        orderedByValue.RemoveAll(card => pairsList.Contains(card));
                        hand.Add(orderedByValue.ElementAt(2));
                        hand.AddRange(pairsList);
                        handValue = CalculateHandValue(hand, 2 * boost);
                        break;
                    case 2:
                        handRank = HandRank.Pair;
                        orderedByValue.RemoveAll(card => pairsList.Contains(card));
                        hand.AddRange(orderedByValue.GetRange(2, 3));
                        hand.AddRange(pairsList);
                        handValue = CalculateHandValue(hand, boost);
                        break;
                    default:
                        handRank = HandRank.HighCard;
                        hand.AddRange(orderedByValue.GetRange(2, 5));
                        handValue = CalculateHandValue(hand, 0);
                        break;
                }
            }
            return new HandStrength(handValue, handRank, hand);
        }

        public int CalculateHandValue(List<Card> hand, int boost)
        {
            var ans = boost;
            for (var i = 0; i < 5; i++)
                ans = ans + (int) Math.Pow(10, i) * hand.ElementAt(i).Value;
            return ans;
        }

        private List<Card> IsStraightFlush(List<Card> ascending, List<Card> similarShape)
        {
            if (ascending.Count < 5 || similarShape.Count < 5) return null;
            return ascending.Count == 5 ? IsStrightFlushHelper(ascending, similarShape) : null;
        }

        private List<Card> IsStrightFlushHelper(List<Card> ascending, List<Card> similarShape)
        {
            var hand = new List<Card>();
            hand.AddRange(ascending);
            ascending.RemoveAll(similarShape.Contains);
            return ascending.Count == 0 ? hand : null;
        }

        private int SumListCard(List<Card> cards)
        {
            var sum = 0;
            for (var i = 0; i < cards.Count; i++) sum += cards[i].Value;
            return sum;
        }
    }
    
    // https://github.com/SaarSch/Texas_Holdem/blob/master/TexasHoldem/TexasHoldem/Game/HandStrength.cs
    public enum HandRank
    {
        RoyalFlush,
        StraightFlush,
        FourOfAKind,
        FullHouse,
        Flush,
        Straight,
        ThreeOfAKind,
        TwoPair,
        Pair,
        HighCard,
        Fold
    }

    public class HandStrength
    {
        public List<Card> HandCards;
        public HandRank Handrank;
        public int HandStrongestValue;

        public HandStrength(int handValue, HandRank handRank, List<Card> cards)
        {
            HandStrongestValue = handValue;
            HandCards = cards;
            Handrank = handRank;
        }
    }
}
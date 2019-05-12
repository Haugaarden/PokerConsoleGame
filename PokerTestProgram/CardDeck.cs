using System;
using System.Collections.Generic;
using System.Linq;

namespace PokerTestProgram
{
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
}
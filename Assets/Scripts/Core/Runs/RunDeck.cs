using System;
using System.Collections;
using System.Collections.Generic;
using Graphaclysm.Core.Cards;

namespace Graphaclysm.Core.Runs
{
    /// <summary>
    /// Persistent deck owned by a run. Battle zones are created from this collection.
    /// </summary>
    public sealed class RunDeck : IReadOnlyList<CardDefinition>
    {
        public const int MaximumCards = 64;

        private readonly CardDefinition[] cards = new CardDefinition[MaximumCards];
        private int count;

        public RunDeck(IReadOnlyList<CardDefinition> startingCards)
        {
            if (startingCards == null)
            {
                throw new ArgumentNullException(nameof(startingCards));
            }

            if (startingCards.Count == 0 || startingCards.Count > MaximumCards)
            {
                throw new ArgumentOutOfRangeException(nameof(startingCards));
            }

            for (int i = 0; i < startingCards.Count; i++)
            {
                Add(startingCards[i]);
            }
        }

        public int Count
        {
            get { return count; }
        }

        public CardDefinition this[int index]
        {
            get
            {
                if (index < 0 || index >= count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return cards[index];
            }
        }

        public bool TryAdd(CardDefinition card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (count >= cards.Length)
            {
                return false;
            }

            cards[count] = card;
            count++;
            return true;
        }

        public void Add(CardDefinition card)
        {
            if (!TryAdd(card))
            {
                throw new InvalidOperationException("The run deck has reached its maximum capacity.");
            }
        }
        public bool TryRemoveAt(int index)
        {
            if (index < 0 || index >= count || count <= 1) return false;
            for (int i = index + 1; i < count; i++) cards[i - 1] = cards[i];
            cards[--count] = null; return true;
        }

        public IEnumerator<CardDefinition> GetEnumerator()
        {
            for (int i = 0; i < count; i++)
            {
                yield return cards[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

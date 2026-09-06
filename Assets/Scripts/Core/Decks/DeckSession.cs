using System;
using System.Collections.Generic;
using Graphaclysm.Core.Cards;

namespace Graphaclysm.Core.Decks
{
    /// <summary>
    /// Owns every card zone for one battle. Fixed arrays keep zone transfers allocation-free.
    /// </summary>
    public sealed class DeckSession
    {
        private readonly CardDefinition[] startingDeck;
        private readonly CardDefinition[] drawPile;
        private readonly CardDefinition[] discardPile;
        private readonly CardDefinition[] hand;
        private readonly IRandomSource random;
        private readonly bool retainHand;
        private readonly int openingHandSize;
        private readonly CardDefinition[] reserved;
        private int reservedCount;
        public int ReservedCount => reservedCount;

        private int drawCount;
        private int discardCount;
        private int handCount;

        public DeckSession(
            IReadOnlyList<CardDefinition> cards,
            int handCapacity,
            IRandomSource random, bool retainHand = false, int openingHandSize = 0)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            if (cards.Count == 0)
            {
                throw new ArgumentException("A deck requires at least one card.", nameof(cards));
            }

            if (handCapacity <= 0 || handCapacity > cards.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(handCapacity));
            }

            this.retainHand = retainHand; this.openingHandSize = openingHandSize > 0 ? Math.Min(handCapacity, openingHandSize) : handCapacity;
            reserved = new CardDefinition[cards.Count];
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            startingDeck = new CardDefinition[cards.Count];
            drawPile = new CardDefinition[cards.Count];
            discardPile = new CardDefinition[cards.Count];
            hand = new CardDefinition[handCapacity];

            bool containsBase = false;
            for (int i = 0; i < cards.Count; i++)
            {
                CardDefinition card = cards[i];
                if (card == null)
                {
                    throw new ArgumentException("A deck cannot contain null cards.", nameof(cards));
                }

                startingDeck[i] = card;
                containsBase |= card.IsBaseFunction || card.IsCalculator || card.IsFragment;
            }

            if (!containsBase)
            {
                throw new ArgumentException("A deck requires at least one base function.", nameof(cards));
            }

            ResetForBattle();
        }

        public int HandCapacity
        {
            get { return hand.Length; }
        }

        public int HandCount
        {
            get { return handCount; }
        }

        public int DrawCount
        {
            get { return drawCount; }
        }

        public int DiscardCount
        {
            get { return discardCount; }
        }

        public int TotalCardCount
        {
            get { return startingDeck.Length; }
        }

        public CardDefinition GetHandCard(int index)
        {
            if (index < 0 || index >= handCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return hand[index];
        }

        public void ResetForBattle()
        {
            ClearReferences(drawPile, drawCount);
            ClearReferences(discardPile, discardCount);
            ClearReferences(hand, handCount);

            ClearReferences(reserved, reservedCount); reservedCount = 0;
            Array.Copy(startingDeck, drawPile, startingDeck.Length);
            drawCount = startingDeck.Length;
            discardCount = 0;
            handCount = 0;
            Shuffle(drawPile, drawCount);
            DrawNewHand();
        }

        public CardDefinition DiscardFromHand(int handIndex)
        {
            CardDefinition card = GetHandCard(handIndex);
            discardPile[discardCount] = card;
            discardCount++;

            handCount--;
            for (int i = handIndex; i < handCount; i++)
            {
                hand[i] = hand[i + 1];
            }

            hand[handCount] = null;
            return card;
        }

        public bool CanRestoreLastDiscard(int handIndex, CardDefinition expectedCard)
        {
            if (retainHand) return expectedCard != null && handIndex >= 0 && handIndex <= handCount && handCount < hand.Length && reservedCount > 0 && ReferenceEquals(reserved[reservedCount-1], expectedCard);
            return expectedCard != null
                && handIndex >= 0
                && handIndex <= handCount
                && handCount < hand.Length
                && discardCount > 0
                && ReferenceEquals(discardPile[discardCount - 1], expectedCard);
        }

        public bool TryRestoreLastDiscardToHand(int handIndex, CardDefinition expectedCard)
        {
            if (!CanRestoreLastDiscard(handIndex, expectedCard))
            {
                return false;
            }

            for (int i = handCount; i > handIndex; i--)
            {
                hand[i] = hand[i - 1];
            }

            if (retainHand) { reservedCount--; hand[handIndex] = reserved[reservedCount]; reserved[reservedCount] = null; }
            else { discardCount--; hand[handIndex] = discardPile[discardCount]; discardPile[discardCount] = null; }
            handCount++;
            return true;
        }

        public void ReserveFromHand(int index)
        {
            if (!retainHand) throw new InvalidOperationException("Reservation requires retained mode.");
            CardDefinition card = DiscardFromHand(index); discardPile[--discardCount] = null; reserved[reservedCount++] = card;
        }
        public void ReleaseReserved()
        { for(int i=0;i<reservedCount;i++) { discardPile[discardCount++] = reserved[i]; reserved[i] = null; } reservedCount = 0; }
        public int DrawRetained(int amount)
        { if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount)); int drawn=0; while(drawn < amount && handCount < hand.Length && TryDrawOne(out CardDefinition card)) { hand[handCount++] = card; drawn++; } return drawn; }
        public void EndTurnAndDraw()
        {
            DiscardEntireHand();
            DrawNewHand();
        }

        private void DrawNewHand()
        {
            while (handCount < openingHandSize && TryDrawOne(out CardDefinition card))
            {
                hand[handCount] = card;
                handCount++;
            }

            GuaranteeBaseFunction();
        }

        private bool TryDrawOne(out CardDefinition card)
        {
            if (drawCount == 0)
            {
                RecycleDiscardPile();
            }

            if (drawCount == 0)
            {
                card = null;
                return false;
            }

            drawCount--;
            card = drawPile[drawCount];
            drawPile[drawCount] = null;
            return true;
        }

        private void RecycleDiscardPile()
        {
            if (discardCount == 0)
            {
                return;
            }

            Array.Copy(discardPile, drawPile, discardCount);
            ClearReferences(discardPile, discardCount);
            drawCount = discardCount;
            discardCount = 0;
            Shuffle(drawPile, drawCount);
        }

        private void DiscardEntireHand()
        {
            for (int i = 0; i < handCount; i++)
            {
                discardPile[discardCount] = hand[i];
                discardCount++;
                hand[i] = null;
            }

            handCount = 0;
        }

        private void GuaranteeBaseFunction()
        {
            if (handCount == 0 || HandContainsBaseFunction())
            {
                return;
            }

            int baseIndex = FindBaseFunction(drawPile, drawCount);
            if (baseIndex >= 0)
            {
                SwapIntoHand(drawPile, baseIndex);
                return;
            }

            baseIndex = FindBaseFunction(discardPile, discardCount);
            if (baseIndex >= 0)
            {
                SwapIntoHand(discardPile, baseIndex);
            }
        }

        private bool HandContainsBaseFunction()
        {
            for (int i = 0; i < handCount; i++)
            {
                if (hand[i].IsBaseFunction)
                {
                    return true;
                }
            }

            return false;
        }

        private static int FindBaseFunction(CardDefinition[] zone, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (zone[i].IsBaseFunction)
                {
                    return i;
                }
            }

            return -1;
        }

        private void SwapIntoHand(CardDefinition[] sourceZone, int sourceIndex)
        {
            int replacedHandIndex = handCount - 1;
            CardDefinition replacement = sourceZone[sourceIndex];
            sourceZone[sourceIndex] = hand[replacedHandIndex];
            hand[replacedHandIndex] = replacement;
        }

        private void Shuffle(CardDefinition[] cards, int count)
        {
            for (int i = count - 1; i > 0; i--)
            {
                int other = random.Next(i + 1);
                CardDefinition temporary = cards[i];
                cards[i] = cards[other];
                cards[other] = temporary;
            }
        }

        private static void ClearReferences(CardDefinition[] cards, int count)
        {
            for (int i = 0; i < count; i++)
            {
                cards[i] = null;
            }
        }
    }
}

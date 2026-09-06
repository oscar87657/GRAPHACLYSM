using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Decks;
using NUnit.Framework;

namespace Graphaclysm.Tests.Decks
{
    public sealed class DeckSessionTests
    {
        [Test]
        public void InitialDraw_PreservesCardCountAndGuaranteesBaseFunction()
        {
            for (uint seed = 1; seed <= 64; seed++)
            {
                DeckSession deck = CreateDeck(seed);

                Assert.That(deck.HandCount, Is.EqualTo(5));
                Assert.That(deck.DrawCount + deck.DiscardCount + deck.HandCount,
                    Is.EqualTo(deck.TotalCardCount));
                Assert.That(ContainsBaseFunction(deck), Is.True, "Failed for seed " + seed);
            }
        }

        [Test]
        public void EndTurn_MovesHandAndRecyclesDiscardWithoutLosingCards()
        {
            DeckSession deck = CreateDeck(77u);
            deck.DiscardFromHand(0);

            deck.EndTurnAndDraw();
            AssertDeckInvariant(deck);
            Assert.That(deck.HandCount, Is.EqualTo(5));

            deck.EndTurnAndDraw();
            AssertDeckInvariant(deck);
            Assert.That(deck.HandCount, Is.EqualTo(5));
            Assert.That(ContainsBaseFunction(deck), Is.True);
        }

        [Test]
        public void SameSeed_ProducesSameOpeningHand()
        {
            DeckSession first = CreateDeck(12345u);
            DeckSession second = CreateDeck(12345u);

            for (int i = 0; i < first.HandCount; i++)
            {
                Assert.That(first.GetHandCard(i).Id, Is.EqualTo(second.GetHandCard(i).Id));
            }
        }

        [Test]
        public void RestoreLastDiscard_ReturnsCardToOriginalHandPosition()
        {
            DeckSession deck = CreateDeck(55u);
            CardDefinition first = deck.GetHandCard(0);
            CardDefinition selected = deck.GetHandCard(2);
            CardDefinition following = deck.GetHandCard(3);

            deck.DiscardFromHand(2);

            Assert.That(deck.TryRestoreLastDiscardToHand(2, selected), Is.True);
            Assert.That(deck.GetHandCard(0), Is.SameAs(first));
            Assert.That(deck.GetHandCard(2), Is.SameAs(selected));
            Assert.That(deck.GetHandCard(3), Is.SameAs(following));
            AssertDeckInvariant(deck);
        }

        private static DeckSession CreateDeck(uint seed)
        {
            return new DeckSession(
                PrototypeDeckFactory.CreateStartingDeck(),
                PrototypeDeckFactory.HandSize,
                new XorShiftRandom(seed));
        }

        private static bool ContainsBaseFunction(DeckSession deck)
        {
            for (int i = 0; i < deck.HandCount; i++)
            {
                if (deck.GetHandCard(i).IsBaseFunction)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AssertDeckInvariant(DeckSession deck)
        {
            Assert.That(deck.DrawCount + deck.DiscardCount + deck.HandCount,
                Is.EqualTo(deck.TotalCardCount));
        }
    }
}

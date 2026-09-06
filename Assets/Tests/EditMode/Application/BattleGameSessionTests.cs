using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Decks;
using NUnit.Framework;

namespace Graphaclysm.Tests.Application
{
    public sealed class BattleGameSessionTests
    {
        [Test]
        public void FailedCardCommand_DoesNotRemoveCardFromHand()
        {
            BattleGameSession game = CreateTwoCardGame();
            int modifierIndex = FindCardIndex(game.Deck, false);
            int handBefore = game.Deck.HandCount;
            int discardBefore = game.Deck.DiscardCount;

            bool played = game.TryPlayHandCard(modifierIndex, out _, out CardPlayFailure failure);

            Assert.That(played, Is.False);
            Assert.That(failure, Is.EqualTo(CardPlayFailure.MissingBaseFunction));
            Assert.That(game.Deck.HandCount, Is.EqualTo(handBefore));
            Assert.That(game.Deck.DiscardCount, Is.EqualTo(discardBefore));
        }

        [Test]
        public void SuccessfulCardCommand_UpdatesBattleAndDeckTogether()
        {
            BattleGameSession game = CreateTwoCardGame();
            int baseIndex = FindCardIndex(game.Deck, true);

            bool played = game.TryPlayHandCard(baseIndex, out CardDefinition card, out CardPlayFailure failure);

            Assert.That(played, Is.True);
            Assert.That(failure, Is.EqualTo(CardPlayFailure.None));
            Assert.That(card.IsBaseFunction, Is.True);
            Assert.That(game.Battle.Equation.HasBase, Is.True);
            Assert.That(game.Deck.HandCount, Is.EqualTo(1));
            Assert.That(game.Deck.DiscardCount, Is.EqualTo(1));
        }

        [Test]
        public void UndoCommand_RestoresBattleAndDeckTogether()
        {
            BattleGameSession game = CreateTwoCardGame();
            int baseIndex = FindCardIndex(game.Deck, true);
            CardDefinition expected = game.Deck.GetHandCard(baseIndex);

            game.TryPlayHandCard(baseIndex, out _, out _);
            Assert.That(game.TryUndoLastPlayedCard(out CardDefinition restored), Is.True);

            Assert.That(restored, Is.SameAs(expected));
            Assert.That(game.Battle.Equation.HasBase, Is.False);
            Assert.That(game.Battle.Energy, Is.EqualTo(game.Battle.PlayerMaxEnergy));
            Assert.That(game.Deck.HandCount, Is.EqualTo(2));
            Assert.That(game.Deck.DiscardCount, Is.Zero);
            Assert.That(game.Deck.GetHandCard(baseIndex), Is.SameAs(expected));
        }

        private static BattleGameSession CreateTwoCardGame()
        {
            CardDefinition[] cards =
            {
                PrototypeCardCatalog.All[0],
                PrototypeCardCatalog.All[4]
            };
            DeckSession deck = new DeckSession(cards, 2, new XorShiftRandom(19u));
            BattleSession battle = new BattleSession(PrototypeBattleFactory.Create());
            return new BattleGameSession(battle, deck);
        }

        private static int FindCardIndex(DeckSession deck, bool findBase)
        {
            for (int i = 0; i < deck.HandCount; i++)
            {
                if (deck.GetHandCard(i).IsBaseFunction == findBase)
                {
                    return i;
                }
            }

            Assert.Fail("Expected card type was not found in hand.");
            return -1;
        }
    }
}

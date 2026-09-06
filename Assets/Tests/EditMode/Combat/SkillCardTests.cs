using System;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Decks;
using Graphaclysm.Core.Equations;
using NUnit.Framework;

namespace Graphaclysm.Tests
{
    public sealed class SkillCardTests
    {
        private static BattleSession Battle(double enemyX = 4, double enemyY = -2)
            => new BattleSession(new BattleDefinition(42, 6, new[] {
                new EnemyDefinition("target", "Target", enemyX, enemyY, 100, 0),
                new EnemyDefinition("miss", "Miss", 9, -3, 100, 0)
            }, CombatArchetype.Ian));
        private static void Play(BattleSession b, string id)
            => Assert.That(b.TryPlayCard(SkillCardCatalog.Find(id), out _), Is.True);

        [Test]
        public void EveryPlayableCardBundlesOneGraphComponentAndBoundedAbilities()
        {
            Assert.That(SkillCardCatalog.All.Count, Is.EqualTo(13));
            foreach (var card in SkillCardCatalog.All)
            {
                Assert.That(card.IsSkill, Is.True);
                Assert.That(card.IsInscription, Is.False);
                Assert.That(card.AbilityCount, Is.InRange(1, 2));
                Assert.That(card.Type == CardType.Input || card.Type == CardType.Operation || card.Type == CardType.Coefficient, Is.True);
            }
            foreach (var c in PrototypeCharacterCatalog.All)
            {
                Assert.That(c.HandSize, Is.EqualTo(5)); Assert.That(c.StartingDeckCount, Is.EqualTo(12));
                for (int i = 0; i < c.StartingDeckCount; i++) Assert.That(c.GetStartingCard(i).IsSkill, Is.True);
            }
        }

        [Test]
        public void SelectionChangesGraphButAbilitiesWaitForActualHit()
        {
            var b = Battle(); Play(b, "skill.focus"); Play(b, "skill.fall");
            Assert.That(b.PreviewPlayerHit, Is.True);
            Assert.That(b.Tactics.Statuses.Get(CombatStatusKind.Shield), Is.Zero);
            Assert.That(b.Enemies[0].Statuses.Get(CombatStatusKind.Burn), Is.Zero);
            Assert.That(b.TryBeginPlot(), Is.True); var report = b.ResolvePlot();
            Assert.That(report.PlayerHit, Is.True); Assert.That(report.HitCount, Is.EqualTo(1));
            Assert.That(b.Tactics.Statuses.Get(CombatStatusKind.Shield), Is.EqualTo(7));
            Assert.That(b.Enemies[0].Statuses.Get(CombatStatusKind.Burn), Is.EqualTo(2));
            Assert.That(b.Enemies[0].Statuses.Get(CombatStatusKind.Shield), Is.Zero);
            Assert.That(b.Tactics.Statuses.Get(CombatStatusKind.Burn), Is.Zero);
            Assert.That(b.Enemies[1].Health, Is.EqualTo(100));
            Assert.That(b.Enemies[1].Statuses.Get(CombatStatusKind.Burn), Is.Zero);
        }

        [Test]
        public void UndoAtomicallyRestoresHandEnergyAndRemovesBundledEffects()
        {
            var b = Battle(); var cards = new[] { SkillCardCatalog.Find("skill.focus"), SkillCardCatalog.Find("skill.fall") };
            var game = new BattleGameSession(b, new DeckSession(cards, 2, new XorShiftRandom(1)));
            int input = game.Deck.GetHandCard(0).IsBaseFunction ? 0 : 1;
            Assert.That(game.TryPlayHandCard(input, out _, out _), Is.True);
            Assert.That(game.TryPlayHandCard(0, out _, out _), Is.True);
            Assert.That(b.Energy, Is.EqualTo(5));
            Assert.That(game.TryUndoLastPlayedCard(out var restored), Is.True);
            Assert.That(restored.Id, Is.EqualTo("skill.fall")); Assert.That(b.Energy, Is.EqualTo(6));
            Assert.That(game.Deck.HandCount, Is.EqualTo(1)); Assert.That(b.PreviewPlayerHit, Is.False);
            b.TryBeginPlot(); b.ResolvePlot();
            Assert.That(b.Tactics.Statuses.Get(CombatStatusKind.Shield), Is.Zero);
            Assert.That(b.Enemies[0].Statuses.Get(CombatStatusKind.Burn), Is.Zero);
        }

        [Test]
        public void MultipleBundledRidersAreNotLimitedLikeLegacyInscriptions()
        {
            var b = Battle(); Play(b, "skill.focus"); Play(b, "skill.fall"); Play(b, "skill.fall"); Play(b, "skill.rise");
            Assert.That(b.TryBeginPlot(), Is.True); b.ResolvePlot();
            Assert.That(b.Enemies[0].Statuses.Get(CombatStatusKind.Burn), Is.EqualTo(4));
            Assert.That(b.Tactics.Statuses.Get(CombatStatusKind.Shield), Is.EqualTo(9));
        }

        [Test]
        public void NewExposureDoesNotRetroactivelyIncreaseCurrentPlot()
        {
            var b = Battle(6, 2); Play(b, "skill.focus"); Play(b, "skill.rupture");
            int expected = b.PreviewDamage(b.Enemies[0]);
            b.TryBeginPlot(); b.ResolvePlot();
            Assert.That(b.Enemies[0].Health, Is.EqualTo(100 - expected));
            Assert.That(b.Enemies[0].Statuses.Get(CombatStatusKind.Exposure), Is.EqualTo(2));
        }

        [Test]
        public void SkillOwnsItsAbilityArrayAndRejectsInvalidDefinitions()
        {
            var abilities = new[] { new CardAbility(CardAbilityKind.Shield, AbilityTarget.Player, 2, 1) };
            var skill = CardDefinition.CreateSkill("owned", "Owned", "x", "Start", CardType.Input, CardRarity.Common,
                BaseFunctionKind.InputX, default, abilities);
            abilities[0] = new CardAbility(CardAbilityKind.Shield, AbilityTarget.Player, 9, 1);
            Assert.That(skill.GetAbility(0).Magnitude, Is.EqualTo(2));
            Assert.Throws<ArgumentException>(() => new CardAbility(CardAbilityKind.Heal, AbilityTarget.Enemy, 2, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CardAbility(CardAbilityKind.Burn, AbilityTarget.Enemy, 25, 1));
            Assert.Throws<ArgumentException>(() => CardDefinition.CreateSkill("empty", "Empty", "x", "Start", CardType.Input,
                CardRarity.Common, BaseFunctionKind.InputX, default));
        }
    }
}

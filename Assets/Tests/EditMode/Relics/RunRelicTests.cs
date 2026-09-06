using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Relics;
using Graphaclysm.Core.Runs;
using NUnit.Framework;

namespace Graphaclysm.Tests.Relics
{
    public sealed class RunRelicTests
    {
        [Test]
        public void Collection_RejectsDuplicateIdsAndAggregatesEffects()
        {
            var collection = new RunRelicCollection();
            RelicDefinition first = new RelicDefinition(
                "energy", "Energy", "", RelicEffectKind.BonusEnergy, 1);
            RelicDefinition duplicate = new RelicDefinition(
                "energy", "Duplicate", "", RelicEffectKind.BonusEnergy, 5);
            RelicDefinition second = new RelicDefinition(
                "energy_two", "Energy Two", "", RelicEffectKind.BonusEnergy, 2);

            Assert.That(collection.TryAdd(first), Is.True);
            Assert.That(collection.TryAdd(duplicate), Is.False);
            Assert.That(collection.TryAdd(second), Is.True);
            Assert.That(collection.Count, Is.EqualTo(2));
            Assert.That(collection.GetTotalMagnitude(RelicEffectKind.BonusEnergy), Is.EqualTo(3));
        }

        [Test]
        public void EliteVictory_OffersUniqueRelicsAndSelectedEffectReachesNextBattle()
        {
            RelicDefinition energy = new RelicDefinition(
                "energy", "Energy", "+1", RelicEffectKind.BonusEnergy, 1);
            RelicDefinition hand = new RelicDefinition(
                "hand", "Hand", "+1", RelicEffectKind.BonusHandSize, 1);
            RelicDefinition damage = new RelicDefinition(
                "damage", "Damage", "+2", RelicEffectKind.BonusPlotDamage, 2);
            RunGameSession run = CreateEliteRun(new[] { energy, hand, damage });

            Assert.That(run.TrySelectMapNode(0), Is.True);
            WinCurrentEncounter(run);

            Assert.That(run.Phase, Is.EqualTo(RunPhase.RelicReward));
            Assert.That(run.RelicRewardOptions[0], Is.Not.SameAs(run.RelicRewardOptions[1]));
            Assert.That(run.RelicRewardOptions[1], Is.Not.SameAs(run.RelicRewardOptions[2]));

            int energyOption = FindRelicOption(run, "energy");
            Assert.That(run.TrySelectRelicReward(energyOption), Is.True);
            Assert.That(run.Relics.Contains("energy"), Is.True);

            Assert.That(run.TrySelectMapNode(1), Is.True);
            Assert.That(run.CurrentBattle.Battle.PlayerMaxEnergy, Is.EqualTo(4));
            Assert.That(run.CurrentBattle.Battle.Energy, Is.EqualTo(4));
        }

        [Test]
        public void PlotDamageBonus_IsAppliedOncePerHit()
        {
            var definition = new BattleDefinition(
                40,
                3,
                new[] { new EnemyDefinition("target", "Target", 5.0, 0.0, 100, 0) });
            var normal = new BattleSession(definition);
            var empowered = new BattleSession(definition, 40, 0, 3);

            int normalDamage = ResolveSingleHit(normal);
            int empoweredDamage = ResolveSingleHit(empowered);

            Assert.That(empoweredDamage, Is.EqualTo(normalDamage + 3));
        }

        private static RunGameSession CreateEliteRun(RelicDefinition[] relicPool)
        {
            BattleDefinition battle = new BattleDefinition(
                40,
                3,
                new[] { new EnemyDefinition("test", "Test", 5.0, 0.0, 1, 0) });
            var map = new RunMapDefinition(new[]
            {
                new RunMapNodeDefinition(
                    "elite", "Elite", 0, 0, RunNodeKind.Elite, battle, new[] { 1 }),
                new RunMapNodeDefinition(
                    "boss", "Boss", 1, 0, RunNodeKind.Boss, battle, System.Array.Empty<int>())
            });
            CardDefinition[] deck = { PrototypeCardCatalog.All[0] };
            return new RunGameSession(
                map,
                deck,
                PrototypeCardCatalog.All,
                1,
                123u,
                relicPool);
        }

        private static void WinCurrentEncounter(RunGameSession run)
        {
            Assert.That(run.TryPlayHandCard(0, out _, out _), Is.True);
            Assert.That(run.TryBeginPlot(), Is.True);
            run.ResolvePlot();
        }

        private static int FindRelicOption(RunGameSession run, string id)
        {
            for (int i = 0; i < RunGameSession.RelicRewardOptionCount; i++)
            {
                if (run.RelicRewardOptions[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int ResolveSingleHit(BattleSession battle)
        {
            battle.TryPlayCard(PrototypeCardCatalog.All[0], out _);
            battle.TryBeginPlot();
            return battle.ResolvePlot().TotalDamage;
        }
    }
}

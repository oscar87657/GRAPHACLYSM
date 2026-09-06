using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Combat;
using NUnit.Framework;

namespace Graphaclysm.Tests.Runs
{
    public sealed class RunGameSessionTests
    {
        [Test]
        public void Victory_OffersThreeUniqueRewardsAndSelectedCardPersists()
        {
            RunGameSession run = CreateRun(enemyHealth: 1, enemyAttack: 0);
            int deckCountBefore = run.Deck.Count;
            Assert.That(run.TrySelectMapNode(0), Is.True);

            WinCurrentEncounter(run);

            Assert.That(run.Phase, Is.EqualTo(RunPhase.CardReward));
            Assert.That(run.RewardOptions.Count, Is.EqualTo(3));
            Assert.That(run.RewardOptions[0], Is.Not.SameAs(run.RewardOptions[1]));
            Assert.That(run.RewardOptions[1], Is.Not.SameAs(run.RewardOptions[2]));
            Assert.That(run.RewardOptions[0], Is.Not.SameAs(run.RewardOptions[2]));

            Assert.That(run.TrySelectReward(1), Is.True);

            Assert.That(run.Deck.Count, Is.EqualTo(deckCountBefore + 1));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.MapSelection));
            Assert.That(run.Map.IsAvailable(1), Is.True);
            Assert.That(run.TrySelectMapNode(1), Is.True);
            Assert.That(run.EncounterNumber, Is.EqualTo(2));
        }

        [Test]
        public void FinalEncounterVictory_CompletesRunWithoutAnotherReward()
        {
            RunGameSession run = CreateRun(enemyHealth: 1, enemyAttack: 0);
            run.TrySelectMapNode(0);
            WinCurrentEncounter(run);
            run.TrySkipReward();
            run.TrySelectMapNode(1);

            WinCurrentEncounter(run);

            Assert.That(run.Phase, Is.EqualTo(RunPhase.Completed));
        }

        [Test]
        public void PlayerHealth_PersistsBetweenEncounters()
        {
            RunGameSession run = CreateRun(enemyHealth: 18, enemyAttack: 3);
            run.TrySelectMapNode(0);

            PlayOneTurn(run);
            Assert.That(run.CurrentBattle.Battle.PlayerHealth, Is.EqualTo(37));
            PlayOneTurn(run);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.CardReward));

            run.TrySkipReward();
            run.TrySelectMapNode(1);

            Assert.That(run.CurrentBattle.Battle.PlayerHealth, Is.EqualTo(37));
        }

        [Test]
        public void MapSelection_RejectsLockedNodeAndStartsSelectedEncounter()
        {
            RunGameSession run = CreateRun(enemyHealth: 1, enemyAttack: 0);

            Assert.That(run.Phase, Is.EqualTo(RunPhase.MapSelection));
            Assert.That(run.TrySelectMapNode(1), Is.False);
            Assert.That(run.TrySelectMapNode(0), Is.True);
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Battle));
            Assert.That(run.EncounterNumber, Is.EqualTo(1));
        }

        private static RunGameSession CreateRun(int enemyHealth, int enemyAttack)
        {
            BattleDefinition encounter = new BattleDefinition(
                40,
                3,
                new[] { new EnemyDefinition("test", "Test", 5.0, 0.0, enemyHealth, enemyAttack) });
            CardDefinition[] startingDeck = { PrototypeCardCatalog.All[0] };
            return new RunGameSession(
                new[] { encounter, encounter },
                startingDeck,
                PrototypeCardCatalog.All,
                1,
                314159u);
        }

        private static void WinCurrentEncounter(RunGameSession run)
        {
            PlayOneTurn(run);
            Assert.That(run.CurrentBattle.Battle.Phase, Is.EqualTo(BattlePhase.Victory));
        }

        private static void PlayOneTurn(RunGameSession run)
        {
            Assert.That(run.TryPlayHandCard(0, out _, out _), Is.True);
            Assert.That(run.TryBeginPlot(), Is.True);
            run.ResolvePlot();

            if (run.Phase == RunPhase.Battle
                && run.CurrentBattle.Battle.Phase == BattlePhase.EnemyTurn)
            {
                run.ResolveEnemyTurn();
            }
        }
    }
}

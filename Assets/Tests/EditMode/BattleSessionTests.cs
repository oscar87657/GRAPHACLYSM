using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Combat;
using NUnit.Framework;

namespace Graphaclysm.Tests
{
    public sealed class BattleSessionTests
    {
        [Test]
        public void ModifierCannotBePlayedBeforeBaseFunction()
        {
            BattleSession battle = CreateBattle();
            CardDefinition modifier = PrototypeCardCatalog.All[4];

            CardPlayFailure failure;
            bool played = battle.TryPlayCard(modifier, out failure);

            Assert.That(played, Is.False);
            Assert.That(failure, Is.EqualTo(CardPlayFailure.MissingBaseFunction));
            Assert.That(battle.Energy, Is.EqualTo(battle.PlayerMaxEnergy));
        }

        [Test]
        public void PlotAndEnemyTurn_FollowExplicitPhaseOrder()
        {
            BattleSession battle = CreateBattle();
            CardDefinition absolute = PrototypeCardCatalog.All[3];

            Assert.That(battle.TryPlayCard(absolute, out _), Is.True);
            Assert.That(battle.TryBeginPlot(), Is.True);
            Assert.That(battle.Phase, Is.EqualTo(BattlePhase.Plotting));

            PlotReport report = battle.ResolvePlot();

            Assert.That(report.HitCount, Is.EqualTo(3));
            Assert.That(battle.Phase, Is.EqualTo(BattlePhase.EnemyTurn));

            int incomingDamage = battle.ResolveEnemyTurn();

            Assert.That(incomingDamage, Is.EqualTo(7));
            Assert.That(battle.Phase, Is.EqualTo(BattlePhase.PlayerPlanning));
            Assert.That(battle.Turn, Is.EqualTo(2));
            Assert.That(battle.Equation.HasBase, Is.False);
        }

        [Test]
        public void RepeatedValidPlots_CanReachVictoryWithoutEnemyTurnAfterFinalHit()
        {
            BattleSession battle = CreateBattle();
            CardDefinition absolute = PrototypeCardCatalog.All[3];

            for (int turn = 0; turn < 3; turn++)
            {
                battle.TryPlayCard(absolute, out _);
                battle.TryBeginPlot();
                battle.ResolvePlot();

                if (battle.Phase == BattlePhase.Victory)
                {
                    break;
                }

                battle.ResolveEnemyTurn();
            }

            Assert.That(battle.Phase, Is.EqualTo(BattlePhase.Victory));
            Assert.That(battle.PlayerHealth, Is.GreaterThan(0));
        }

        [Test]
        public void UndoLastCard_RestoresEquationAndEnergyInReverseOrder()
        {
            BattleSession battle = CreateBattle();
            CardDefinition baseCard = PrototypeCardCatalog.All[0];
            CardDefinition modifier = PrototypeCardCatalog.All[4];

            battle.TryPlayCard(baseCard, out _);
            battle.TryPlayCard(modifier, out _);
            Assert.That(battle.Energy, Is.EqualTo(1));

            Assert.That(battle.TryUndoLastCard(out CardDefinition firstUndo), Is.True);
            Assert.That(firstUndo, Is.SameAs(modifier));
            Assert.That(battle.Equation.HasBase, Is.True);
            Assert.That(battle.Equation.ModifierCount, Is.Zero);
            Assert.That(battle.Energy, Is.EqualTo(2));

            Assert.That(battle.TryUndoLastCard(out CardDefinition secondUndo), Is.True);
            Assert.That(secondUndo, Is.SameAs(baseCard));
            Assert.That(battle.Equation.HasBase, Is.False);
            Assert.That(battle.Energy, Is.EqualTo(3));
        }

        private static BattleSession CreateBattle()
        {
            return new BattleSession(PrototypeBattleFactory.Create());
        }
    }
}

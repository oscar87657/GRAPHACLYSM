using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Combat;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class EnemyBehaviorTests
    {
        [Test]
        public void ChargeBurst_PreviewsRestTurnThenDealsDoubleDamage()
        {
            BattleSession battle = CreateBattle(
                EnemyBehaviorDefinition.ChargeBurst(),
                9.0,
                4.0,
                3);
            EnemyState enemy = battle.Enemies[0];

            Assert.That(enemy.Intent.Kind, Is.EqualTo(EnemyIntentKind.Charge));
            ResolveMissedPlayerPlot(battle);
            Assert.That(battle.ResolveEnemyTurn(), Is.Zero);

            Assert.That(enemy.Intent.Kind, Is.EqualTo(EnemyIntentKind.Attack));
            Assert.That(enemy.Intent.Damage, Is.EqualTo(6));
            ResolveMissedPlayerPlot(battle);
            Assert.That(battle.ResolveEnemyTurn(), Is.EqualTo(6));
        }

        [Test]
        public void AlternatingPosition_MovesBeforeAttackingFromNewCoordinate()
        {
            BattleSession battle = CreateBattle(
                EnemyBehaviorDefinition.AlternatingPosition(5.0, 0.0),
                9.0,
                4.0,
                3);
            EnemyState enemy = battle.Enemies[0];

            Assert.That(enemy.Intent.Kind, Is.EqualTo(EnemyIntentKind.Reposition));
            Assert.That(enemy.Intent.TargetX, Is.EqualTo(5.0));
            ResolveMissedPlayerPlot(battle);
            Assert.That(battle.ResolveEnemyTurn(), Is.Zero);
            Assert.That(enemy.X, Is.EqualTo(5.0));
            Assert.That(enemy.Y, Is.EqualTo(0.0));

            CardDefinition line = PrototypeCardCatalog.All[0];
            Assert.That(battle.TryPlayCard(line, out _), Is.True);
            Assert.That(battle.PreviewDamage(enemy), Is.GreaterThan(0));
            Assert.That(enemy.Intent.Kind, Is.EqualTo(EnemyIntentKind.Attack));
            Assert.That(enemy.Intent.Damage, Is.EqualTo(3));
        }

        [Test]
        public void Reset_RestoresPositionAndFirstIntent()
        {
            BattleSession battle = CreateBattle(
                EnemyBehaviorDefinition.AlternatingPosition(5.0, 0.0),
                9.0,
                4.0,
                3);
            EnemyState enemy = battle.Enemies[0];

            ResolveMissedPlayerPlot(battle);
            battle.ResolveEnemyTurn();
            battle.Reset();

            Assert.That(enemy.X, Is.EqualTo(9.0));
            Assert.That(enemy.Y, Is.EqualTo(4.0));
            Assert.That(enemy.Intent.Kind, Is.EqualTo(EnemyIntentKind.Reposition));
            Assert.That(enemy.Intent.TargetX, Is.EqualTo(5.0));
        }

        private static BattleSession CreateBattle(
            EnemyBehaviorDefinition behavior,
            double x,
            double y,
            int attack)
        {
            var definition = new BattleDefinition(
                40,
                3,
                new[]
                {
                    new EnemyDefinition("behavior_test", "행동 시험체", x, y, 200, attack, behavior)
                });
            return new BattleSession(definition);
        }

        private static void ResolveMissedPlayerPlot(BattleSession battle)
        {
            CardDefinition line = PrototypeCardCatalog.All[0];
            Assert.That(battle.TryPlayCard(line, out _), Is.True);
            Assert.That(battle.TryBeginPlot(), Is.True);
            PlotReport report = battle.ResolvePlot();
            Assert.That(report.HitCount, Is.Zero);
            Assert.That(battle.Phase, Is.EqualTo(BattlePhase.EnemyTurn));
        }
    }
}

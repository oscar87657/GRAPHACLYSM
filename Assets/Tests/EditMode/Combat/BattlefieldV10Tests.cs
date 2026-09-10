using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class BattlefieldV10Tests
    {
        private static EnemyDefinition Target(double x = 9, double y = 3,
            EnemyBehaviorDefinition behavior = null)
            => new EnemyDefinition("target", "Target", x, y, 999, 0,
                behavior ?? EnemyBehaviorDefinition.SteadyAttack());

        [Test]
        public void ObstacleSlidesPlayerLandingAndBlocksEnemyReposition()
        {
            var obstacle = new BattleTerrainDefinition("pillar", BattleTerrainKind.Obstacle, 2.5, -2, 0.68);
            var enemy = Target(8, 2, EnemyBehaviorDefinition.AlternatingPosition(2.5, -2));
            var battle = new BattleSession(new BattleDefinition(42, 4, new[] { enemy },
                CombatArchetype.Ian, fragments: true, terrain: new[] { obstacle }));

            Assert.That(battle.TryMovePlayerTo(2.5, -2), Is.True);
            double dx = battle.Tactics.X - obstacle.X, dy = battle.Tactics.Y - obstacle.Y;
            Assert.That(dx * dx + dy * dy, Is.GreaterThanOrEqualTo(
                (obstacle.Radius + TacticalCombatState.PlayerRadius) * (obstacle.Radius + TacticalCombatState.PlayerRadius)));
            battle.TryPlayCard(FragmentCardCatalog.Find("frag.echo"), out _);
            battle.TryBeginPlot();
            battle.ResolvePlot();
            battle.ResolveEnemyTurn();

            Assert.That(battle.Enemies[0].X, Is.EqualTo(8));
            Assert.That(battle.Enemies[0].Y, Is.EqualTo(2));
        }

        [Test]
        public void PrismIntersectionAddsOneVisibleDamageBonus()
        {
            var prism = new BattleTerrainDefinition("prism", BattleTerrainKind.Prism, 4, -.4, 0.3);
            var plain = new BattleSession(new BattleDefinition(42, 4, new[] { Target(5.6, -2) },
                CombatArchetype.Ian, fragments: true));
            var charged = new BattleSession(new BattleDefinition(42, 4, new[] { Target(5.6, -2) },
                CombatArchetype.Ian, fragments: true, terrain: new[] { prism }));
            CardDefinition orbit = FragmentCardCatalog.Find("frag.orbit");

            plain.TryPlayCard(orbit, out _);
            charged.TryPlayCard(orbit, out _);

            Assert.That(charged.PrismCharged, Is.True);
            Assert.That(charged.PreviewDamage(charged.Enemies[0]),
                Is.EqualTo(plain.PreviewDamage(plain.Enemies[0]) + BattleSession.PrismDamageBonus));
        }

        [Test]
        public void GeneratedTerrainIsSeededValidAndSeparatedFromStartingActors()
        {
            for (uint seed = 1; seed <= 50; seed++)
            {
                var map = DungeonGenerator.Generate(seed, PrototypeCharacterCatalog.All[0]);
                var same = DungeonGenerator.Generate(seed, PrototypeCharacterCatalog.All[0]);
                for (int i = 0; i < map.NodeCount; i++)
                {
                    BattleDefinition battle = map.GetNode(i).Battle;
                    BattleDefinition copy = same.GetNode(i).Battle;
                    if (battle == null) continue;
                    Assert.That(battle.TerrainCount, Is.EqualTo(copy.TerrainCount));
                    Assert.That(battle.TerrainCount, Is.EqualTo(map.GetNode(i).Kind == Graphaclysm.Core.Runs.RunNodeKind.Boss ? 3 : 2));
                    for (int t = 0; t < battle.TerrainCount; t++)
                    {
                        BattleTerrainDefinition item = battle.GetTerrain(t);
                        BattleTerrainDefinition repeated = copy.GetTerrain(t);
                        Assert.That(item.Kind, Is.EqualTo(repeated.Kind));
                        Assert.That(item.X, Is.EqualTo(repeated.X));
                        Assert.That(item.Y, Is.EqualTo(repeated.Y));
                        double playerDx = item.X - 4, playerDy = item.Y + 2;
                        Assert.That(playerDx * playerDx + playerDy * playerDy,
                            Is.GreaterThan((item.Radius + TacticalCombatState.PlayerRadius) *
                                           (item.Radius + TacticalCombatState.PlayerRadius)));
                    }
                    bool moveAvailable = new BattleSession(battle).TryMovePlayer(-1.5, 0)
                        || new BattleSession(battle).TryMovePlayer(1.5, 0)
                        || new BattleSession(battle).TryMovePlayer(0, 1.5)
                        || new BattleSession(battle).TryMovePlayer(0, -1.5);
                    Assert.That(moveAvailable, Is.True);
                }
            }
        }
    }
}

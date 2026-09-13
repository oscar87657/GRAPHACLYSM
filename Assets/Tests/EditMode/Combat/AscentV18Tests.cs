using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Runs;
using Graphaclysm.Core.Equations;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class AscentV18Tests
    {
        private static BattleSession Make(BattleTerrainKind kind, double enemyX = 5.6, double enemyY = -2)
            => new BattleSession(new BattleDefinition(42,4,
                new[] { new EnemyDefinition("target","Target",enemyX,enemyY,100,0) },
                CombatArchetype.Ian, fragments:true,
                terrain:new[] { new BattleTerrainDefinition("device",kind,4,-.4,.3) }));

        [Test]
        public void EnemyContactIsLegalAndCanBeUndone()
        {
            var battle = Make(BattleTerrainKind.Prism);
            Assert.That(battle.TryMovePlayerTo(5.6,-2),Is.True);
            Assert.That(battle.Tactics.X,Is.EqualTo(5.6).Within(.001));
            Assert.That(battle.TryUndoMove(),Is.True);
            Assert.That(battle.Tactics.X,Is.EqualTo(4));
        }

        [Test]
        public void PulseIsTelegraphedAndDamagesBothSidesBeforeActions()
        {
            var battle = Make(BattleTerrainKind.Pulse,4,-.4);
            Assert.That(battle.IsPulseActive(0),Is.False);
            battle.TryUnravel(); battle.ResolveEnemyTurn();
            Assert.That(battle.IsPulseActive(0),Is.True);
            Assert.That(battle.TryMovePlayerTo(4,-.4),Is.True);
            Assert.That(battle.PreviewPulseDamage(4,-.4,TacticalCombatState.PlayerRadius,false),Is.EqualTo(6));
            battle.TryUnravel();
            Assert.That(battle.ResolveEnemyTurn(),Is.EqualTo(6));
            Assert.That(battle.PlayerHealth,Is.EqualTo(36));
            Assert.That(battle.Enemies[0].Health,Is.EqualTo(94));
        }

        [Test]
        public void ActualPlotSuppressesPulseButCondensingTheSameTraceDoesNot()
        {
            var battle = Make(BattleTerrainKind.Pulse,4,-.4);
            battle.TryUnravel(); battle.ResolveEnemyTurn();
            battle.TryPlayCard(FragmentCardCatalog.Find("frag.orbit"),out _);
            Assert.That(battle.TraceTouchesDevice(0),Is.True);
            Assert.That(battle.PreviewPulseDamage(4,-.4,.48,true),Is.Zero);
            Assert.That(battle.PreviewPulseDamage(4,-.4,.48,false),Is.EqualTo(6));
            battle.TryBeginPlot(); battle.ResolvePlot();
            Assert.That(battle.IsPulseActive(0),Is.False);
            int health = battle.Enemies[0].Health;
            battle.ResolveEnemyTurn();
            Assert.That(battle.Enemies[0].Health,Is.EqualTo(health));
        }

        [TestCase(BattleTerrainKind.Capacitor)]
        [TestCase(BattleTerrainKind.Aegis)]
        public void TraceDevicesAreSingleUseAndResetWithTheEncounter(BattleTerrainKind kind)
        {
            var battle = Make(kind);
            battle.TryPlayCard(FragmentCardCatalog.Find("frag.orbit"),out _);
            int damage = battle.PreviewDamage(battle.Enemies[0]) + battle.PreviewDeviceDamage(battle.Enemies[0]);
            battle.TryBeginPlot(); battle.ResolvePlot();
            Assert.That(battle.IsDeviceSpent(0),Is.True);
            Assert.That(battle.Enemies[0].Health,Is.EqualTo(100-damage));
            if (kind == BattleTerrainKind.Aegis)
                Assert.That(battle.Tactics.Statuses.Get(CombatStatusKind.Shield),Is.EqualTo(5));
            battle.Reset();
            Assert.That(battle.IsDeviceSpent(0),Is.False);
        }

        [Test]
        public void HealthPreviewAccountsForCleanseBeforeTheDeviceExplosion()
        {
            var battle = new BattleSession(new BattleDefinition(42,4,
                new[] { new EnemyDefinition("target","Target",5.6,-2,100,0,EnemyBehaviorDefinition.ChargeBurst()) },
                CombatArchetype.Ian,fragments:true,
                terrain:new[] { new BattleTerrainDefinition("device",BattleTerrainKind.Capacitor,4,-.4,.3) }));
            var cleanse = CardDefinition.CreateWeaveFragment("test.cleanse","Cleanse",FragmentKind.Orbit,CardRarity.Common,"Test",0,
                new CardAbility(CardAbilityKind.Cleanse,AbilityTarget.Enemy,1,1));
            Assert.That(battle.TryPlayCard(cleanse,out _),Is.True);
            int preview = battle.PreviewHealthDamage(battle.Enemies[0]);
            Assert.That(preview,Is.GreaterThanOrEqualTo(BattleSession.CapacitorDamage));
            battle.TryBeginPlot(); battle.ResolvePlot();
            Assert.That(100-battle.Enemies[0].Health,Is.EqualTo(preview));
        }

        [Test]
        public void NewEnemyPatternsHaveBoundedPressureAndMovementWindows()
        {
            var pressure = EnemyBehaviorDefinition.EscalatingAttack();
            Assert.That(pressure.CreateIntent(1,2,5,0).Damage,Is.EqualTo(2));
            Assert.That(pressure.CreateIntent(9,2,5,0).Damage,Is.EqualTo(6));
            Assert.That(pressure.CreateIntent(100,2,5,0).Damage,Is.EqualTo(6));
            var mobile = EnemyBehaviorDefinition.Skirmisher(7,2);
            Assert.That(mobile.CreateIntent(1,2,5,0).Kind,Is.EqualTo(EnemyIntentKind.Reposition));
            Assert.That(mobile.CreateIntent(2,2,5,0).Kind,Is.EqualTo(EnemyIntentKind.Attack));
            Assert.That(mobile.CreateIntent(3,2,5,0).Kind,Is.EqualTo(EnemyIntentKind.Attack));
            Assert.That(mobile.CreateIntent(4,2,5,0).TargetX,Is.EqualTo(5));
        }

        [TestCase(CombatArchetype.Ian)]
        [TestCase(CombatArchetype.Luna)]
        public void FirstNodeOpensOnlyThreeDistributedChoices(CombatArchetype archetype)
        {
            var growth = new RunGrowthState(archetype);
            growth.TryPurchase(growth.IndexOf(GrowthTreePaths.RootId));
            int choices=0;
            for(int i=0;i<growth.NodeCount;i++) if(growth.CanPurchase(i)) choices++;
            Assert.That(choices,Is.EqualTo(3));
        }
    }
}

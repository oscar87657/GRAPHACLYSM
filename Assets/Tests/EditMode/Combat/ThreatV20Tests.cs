using Graphaclysm.Application;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Equations;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class ThreatV20Tests
    {
        private static BattleSession Gunner() => new BattleSession(new BattleDefinition(42,4,new[] {
            new EnemyDefinition("gun","Gunner",1,-2,100,5,EnemyBehaviorDefinition.LineGunner())
        },CombatArchetype.Ian,fragments:true));
        private static BattleSession Scribe() => new BattleSession(new BattleDefinition(42,4,new[] {
            new EnemyDefinition("scribe","Scribe",8,2,100,0,EnemyBehaviorDefinition.SealScribe())
        },CombatArchetype.Ian,fragments:true,terrain:new[] {
            new BattleTerrainDefinition("prism",BattleTerrainKind.Prism,4,-.4,.3)
        }));
        [Test]
        public void LineRequiresLateralMovementAndDoesNotRetarget()
        {
            var b=Gunner(); var e=b.Enemies[0];
            Assert.That(e.IsAimingAt(5.8,-2,.48),Is.True);
            Assert.That(e.IsAimingAt(4,-.2,.48),Is.False);
            Assert.That(e.LineEndX,Is.EqualTo(10).Within(.0001));
            Assert.That(b.TryMovePlayerTo(4,-.2),Is.True);
            Assert.That(e.AimY,Is.EqualTo(-2));
            b.TryUnravel(); Assert.That(b.ResolveEnemyTurn(),Is.Zero);
        }
        [Test]
        public void MovingBackAlongTheLineStillTakesDamage()
        {
            var b=Gunner(); Assert.That(b.TryMovePlayerTo(5.8,-2),Is.True);
            b.TryUnravel(); Assert.That(b.ResolveEnemyTurn(),Is.EqualTo(5));
        }
        [Test]
        public void SealHasOneTurnWarningAndExpiresAfterOneActiveTurn()
        {
            var b=Scribe(); var e=b.Enemies[0];
            Assert.That(b.SealPending(e),Is.True); Assert.That(b.SealActive(e),Is.False);
            Assert.That(e.Statuses.Get(CombatStatusKind.Shield),Is.Zero);
            b.TryUnravel(); b.ResolveEnemyTurn();
            Assert.That(b.SealActive(e),Is.True); Assert.That(b.PositionSealed(4,-.4,.48),Is.True);
            b.TryUnravel(); b.ResolveEnemyTurn(); Assert.That(b.SealActive(e),Is.False);
        }
        [Test]
        public void PlotInterruptsSealAndResetRestoresIt()
        {
            var b=Scribe(); var e=b.Enemies[0];
            Assert.That(b.TryPlayCard(FragmentCardCatalog.Find("frag.orbit"),out _),Is.True);
            Assert.That(b.TraceBreaksSeal(e),Is.True);
            b.TryBeginPlot(); b.ResolvePlot(); b.ResolveEnemyTurn();
            Assert.That(b.SealActive(e),Is.False);
            b.Reset(); Assert.That(b.SealPending(e),Is.True);
        }
        [Test]
        public void PreviewDoesNotCancelSealButActualPlotReopensMovement()
        {
            var b=Scribe(); b.TryUnravel(); b.ResolveEnemyTurn();
            Assert.That(b.TryPlayCard(FragmentCardCatalog.Find("frag.orbit"),out _),Is.True);
            Assert.That(b.PositionSealed(4,-.4,.48),Is.True);
            Assert.That(b.PositionSealed(4,-.4,.48,true),Is.False);
            Assert.That(b.PrismCharged,Is.True,"Preview includes breaking seal before prism use");
            b.TryBeginPlot(); b.ResolvePlot();
            Assert.That(b.PositionSealed(4,-.4,.48),Is.False);
        }
        [TestCase(false)]
        [TestCase(true)]
        public void GuardianPreviewMatchesActualRegardlessOfEnemyOrder(bool reverse)
        {
            var ally=new EnemyDefinition("ally","Ally",5,0,100,0);
            var guard=new EnemyDefinition("guard","Guard",7,2,100,0,EnemyBehaviorDefinition.LinkGuardian());
            var b=new BattleSession(new BattleDefinition(42,4,reverse?new[]{guard,ally}:new[]{ally,guard},CombatArchetype.Ian));
            b.Equation.SetBase(BaseFunctionKind.InputX);
            var g=b.Enemies[reverse?0:1]; var a=b.Enemies[reverse?1:0];
            Assert.That(b.TraceBreaksGuard(g),Is.True);
            int expected=b.PreviewHealthDamage(a);
            b.TryBeginPlot(); b.ResolvePlot();
            Assert.That(100-a.Health,Is.EqualTo(expected)); Assert.That(g.GuardBroken,Is.True);
            b.ResolveEnemyTurn(); Assert.That(b.GuardActive(g),Is.True);
        }
        [Test]
        public void GuardianReducesOnlyProtectedGraphHitAndDeathRemovesProtection()
        {
            var b=new BattleSession(new BattleDefinition(42,4,new[]{
                new EnemyDefinition("ally","Ally",5,0,100,0),
                new EnemyDefinition("guard","Guard",8,-2,100,0,EnemyBehaviorDefinition.LinkGuardian())
            },CombatArchetype.Ian));
            b.Equation.SetBase(BaseFunctionKind.InputX);
            var a=b.Enemies[0]; var g=b.Enemies[1];
            Assert.That(b.TraceBreaksGuard(g),Is.False);
            int reduced=b.PreviewDamage(a);
            Assert.That(b.PreviewGuardReduction(a),Is.EqualTo(4));
            g.TakeDamage(100);
            Assert.That(b.PreviewDamage(a),Is.EqualTo(reduced+4));
        }
        [Test]
        public void GuardedHitWithoutBreakingUsesDisplayedDamage()
        {
            var b=new BattleSession(new BattleDefinition(42,4,new[]{
                new EnemyDefinition("ally","Ally",5,0,100,0),
                new EnemyDefinition("guard","Guard",8,-2,100,0,EnemyBehaviorDefinition.LinkGuardian())
            },CombatArchetype.Ian));
            b.Equation.SetBase(BaseFunctionKind.InputX);
            int preview=b.PreviewHealthDamage(b.Enemies[0]);
            Assert.That(b.PreviewGuardReduction(b.Enemies[0]),Is.EqualTo(4));
            b.TryBeginPlot(); b.ResolvePlot();
            Assert.That(100-b.Enemies[0].Health,Is.EqualTo(preview));
            Assert.That(b.GuardActive(b.Enemies[1]),Is.True);
        }
        [Test]
        public void CondensingAMarkedTraceDoesNotInterruptTheSeal()
        {
            var b=Scribe(); b.TryPlayCard(FragmentCardCatalog.Find("frag.orbit"),out _);
            Assert.That(b.TraceBreaksSeal(b.Enemies[0]),Is.True);
            Assert.That(b.TryCondense(),Is.True);
            b.ResolveEnemyTurn(); Assert.That(b.SealActive(b.Enemies[0]),Is.True);
        }
        [Test]
        public void GeneratedSealStateSurvivesSaveReplay()
        {
            var run=PrototypeRunFactory.Create(302,PrototypeCharacterCatalog.All[0]);
            Assert.That(run.TrySelectMapNode(0),Is.True);
            Assert.That(run.TryUnravel(),Is.True); run.ResolveEnemyTurn();
            var original=run.CurrentBattle.Battle;
            Assert.That(original.SealActive(original.Enemies[1]),Is.True);
            Assert.That(RunSaveStore.TryDecode(RunSaveStore.Encode(run.CaptureSave()),out var data),Is.True);
            Assert.That(RunGameSession.TryRestore(data,out var restored,out _),Is.True);
            var battle=restored.CurrentBattle.Battle;
            Assert.That(battle.SealActive(battle.Enemies[1]),Is.True);
            Assert.That(battle.Enemies[1].SealX,Is.EqualTo(original.Enemies[1].SealX));
            Assert.That(battle.Enemies[0].LineEndX,Is.EqualTo(original.Enemies[0].LineEndX));
            Assert.That(battle.PlayerHealth,Is.EqualTo(original.PlayerHealth));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void ThreeRepeatableEncountersHaveTwoDistinctRoles(int variant)
        {
            var d=DungeonGenerator.TacticalEncounter(variant,PrototypeCharacterCatalog.All[0],42);
            Assert.That(d.EnemyCount,Is.EqualTo(2));
            Assert.That(d.GetEnemy(0).Behavior.Kind,Is.Not.EqualTo(d.GetEnemy(1).Behavior.Kind));
            Assert.That(d.GetEnemy(0).MaxHealth,Is.EqualTo(24));
        }
    }
}

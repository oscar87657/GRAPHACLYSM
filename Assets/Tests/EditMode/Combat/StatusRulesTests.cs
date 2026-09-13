using System;
using System.Reflection;
using System.Security.Cryptography;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class StatusRulesTests
    {
        private static object Call(object instance,string name,params object[] args)
            => instance.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(instance,args);
        private static void Add(CombatStatusState s,CombatStatusKind kind,int n=1,int d=2) => Call(s,"Add",kind,n,d);
        private static BattleSession Battle(EnemyBehaviorDefinition behavior=null,double x=6,double y=-2)
        {
            var b=new BattleSession(new BattleDefinition(100,3,new[]{new EnemyDefinition("target","target",x,y,999,6,behavior ?? EnemyBehaviorDefinition.SteadyAttack())},CombatArchetype.Luna,fragments:true,approach:CombatApproach.Tuning));
            b.EnableStatusRules(); return b;
        }
        [Test] public void WardBlocksOneApplicationAndCleanseRecognizesNewDebuffs()
        {
            var s=Battle().Tactics.Statuses;
            Add(s,CombatStatusKind.Ward);Add(s,CombatStatusKind.Wound,4);
            Assert.That(s.Get(CombatStatusKind.Wound),Is.Zero);Assert.That(s.Get(CombatStatusKind.Ward),Is.Zero);
            Add(s,CombatStatusKind.Wound,4);Add(s,CombatStatusKind.Guidance);Add(s,CombatStatusKind.FrailShield,3);Add(s,CombatStatusKind.Focus,2);
            Call(s,"Cleanse",true);
            Assert.That(s.Get(CombatStatusKind.Wound)+s.Get(CombatStatusKind.Guidance)+s.Get(CombatStatusKind.FrailShield),Is.Zero);
            Assert.That(s.Get(CombatStatusKind.Focus),Is.EqualTo(2));
        }
        [Test] public void FrailShieldReducesOnlyNewShieldAndNeverMakesItNegative()
        {
            var s=Battle().Tactics.Statuses; Add(s,CombatStatusKind.Shield,5);Add(s,CombatStatusKind.FrailShield,3);
            Assert.That(s.Get(CombatStatusKind.Shield),Is.EqualTo(5));
            Add(s,CombatStatusKind.Shield,2);Assert.That(s.Get(CombatStatusKind.Shield),Is.EqualTo(5));
            Add(s,CombatStatusKind.Shield,6);Assert.That(s.Get(CombatStatusKind.Shield),Is.EqualTo(8));
        }
        [Test] public void EnhancedMomentumMergesAndHasteMoveUndoRestoresItsRange()
        {
            var b=Battle();var s=b.Tactics.Statuses;
            Add(s,CombatStatusKind.Focus,2);Add(s,CombatStatusKind.Momentum,3);Add(s,CombatStatusKind.Haste);
            Assert.That(s.Get(CombatStatusKind.Focus),Is.EqualTo(5));Assert.That(s.Get(CombatStatusKind.Momentum),Is.Zero);
            Assert.That(b.TryMovePlayerTo(6.5,-2),Is.True);Assert.That(b.Tactics.X,Is.EqualTo(6.5));
            Assert.That(s.Get(CombatStatusKind.Haste),Is.Zero);Assert.That(b.TryUndoMove(),Is.True);
            Assert.That(b.Tactics.CurrentMoveDistance,Is.EqualTo(2.7).Within(1e-8));Assert.That(s.Get(CombatStatusKind.Focus),Is.EqualTo(5));
        }
        [Test] public void PullMovesMuzzleAndWarningTogetherWithoutRetargetingPlayer()
        {
            var b=Battle(EnemyBehaviorDefinition.LineGunner());var e=b.Enemies[0];
            Assert.That(e.IsAimingAt(4,-2,.1),Is.True);
            Assert.That(b.PreviewLunaPull(6,0,0,out double x,out double y),Is.True);
            Assert.That(b.TryUseLunaPull(6,0),Is.True);
            Assert.That(e.LineStartX,Is.EqualTo(x));Assert.That(e.LineStartY,Is.EqualTo(y));
            Assert.That(e.LineEndY,Is.EqualTo(y).Within(1e-8));Assert.That(e.LineEndX,Is.Zero.Within(1e-8));
            Assert.That(e.IsAimingAt(4,-2,.1),Is.False);Assert.That(e.IsAimingAt(4,y,.1),Is.True);
        }
        [Test] public void GuidanceExtendsPullAndWoundOnlyTriggersOnSuccessfulMovement()
        {
            var b=Battle();var e=b.Enemies[0];Add(e.Statuses,CombatStatusKind.Guidance);Add(e.Statuses,CombatStatusKind.Wound,4);
            Assert.That(b.PreviewLunaPull(4,-2,0,out double x,out _),Is.True);Assert.That(x,Is.EqualTo(4));
            Assert.That(e.Health,Is.EqualTo(999));Assert.That(b.TryUseLunaPull(4,-2),Is.True);
            Assert.That(e.Health,Is.EqualTo(995));Assert.That(e.Statuses.Get(CombatStatusKind.Guidance),Is.Zero);
        }
        [Test] public void AnchorPreventsBothGuidanceConsumptionAndWoundDamage()
        {
            var b=Battle();var e=b.Enemies[0];Add(e.Statuses,CombatStatusKind.Guidance);Add(e.Statuses,CombatStatusKind.Wound,4);Add(e.Statuses,CombatStatusKind.Anchor);
            Assert.That(b.TryUseLunaPull(4,-2),Is.False);Assert.That(e.Health,Is.EqualTo(999));
            Assert.That(e.Statuses.Get(CombatStatusKind.Guidance),Is.EqualTo(1));Assert.That(b.CanUseLunaPull,Is.True);
        }
        [Test] public void RingMovesButMortarTargetStaysAtAnnouncedGroundPosition()
        {
            var ring=Battle(EnemyBehaviorDefinition.RingSentinel());var mortar=Battle(EnemyBehaviorDefinition.TwinMortar());
            double aim=mortar.Enemies[0].AimX;
            ring.TryUseLunaPull(4,-2);mortar.TryUseLunaPull(4,-2);
            Assert.That(ring.Enemies[0].AimX,Is.EqualTo(ring.Enemies[0].X));
            Assert.That(mortar.Enemies[0].AimX,Is.EqualTo(aim));
        }
        [Test] public void PullCardWorksForIanAndUsesGuidanceAndWoundAfterDirectHit()
        {
            var card=FragmentCardCatalog.Current("frag.contract");
            var b=new BattleSession(new BattleDefinition(100,3,new[]{new EnemyDefinition("t","t",5.12,-2,999,0)},CombatArchetype.Ian,fragments:true));
            b.EnableStatusRules();var e=b.Enemies[0];Add(e.Statuses,CombatStatusKind.Guidance);Add(e.Statuses,CombatStatusKind.Wound,4);
            Assert.That(b.TryPlayCard(card,out _),Is.True);int damage=b.PreviewDamage(e);Assert.That(damage,Is.GreaterThan(0));
            Assert.That(b.TryBeginPlot(),Is.True);b.ResolvePlot();
            Assert.That(e.X,Is.EqualTo(4).Within(1e-8));Assert.That(e.Health,Is.EqualTo(999-damage-4));
            Assert.That(e.Statuses.Get(CombatStatusKind.Guidance),Is.Zero);
        }
        [Test] public void PierceBypassesShieldWithoutAddingTotalDamage()
        {
            var b=Battle(x:5.6);var e=b.Enemies[0];Add(e.Statuses,CombatStatusKind.Shield,20);Add(b.Tactics.Statuses,CombatStatusKind.Pierce,4);
            b.TryPlayCard(FragmentCardCatalog.Current("frag.home"),out _);int damage=b.PreviewDamage(e);
            Assert.That(damage,Is.GreaterThanOrEqualTo(4));b.TryBeginPlot();b.ResolvePlot();
            Assert.That(e.Health,Is.EqualTo(995));Assert.That(e.Statuses.Get(CombatStatusKind.Shield),Is.EqualTo(20-(damage-4)));
            Assert.That(b.Tactics.Statuses.Get(CombatStatusKind.Pierce),Is.Zero);
        }
        [Test] public void EveryNewStatusHasCardSourcesAndCatalogRetainsColorsAndGeometry()
        {
            var counts=new int[20];var tiers=new int[4];int pulls=0;
            foreach(var c in FragmentCardCatalog.All)
            {
                tiers[(int)c.Rarity]++;Assert.That(c.Fragment,Is.EqualTo(FragmentCardCatalog.MarketCard(c.Id).Fragment));
                for(int a=0;a<c.AbilityCount;a++) {counts[(int)c.GetAbility(a).Kind]++;if(c.GetAbility(a).Kind==CardAbilityKind.Pull)pulls++;}
            }
            for(int i=14;i<=19;i++) Assert.That(counts[i],Is.GreaterThanOrEqualTo(4));
            Assert.That(counts[(int)CardAbilityKind.Momentum],Is.Zero);Assert.That(pulls,Is.EqualTo(9));
            CollectionAssert.AreEqual(new[]{21,21,42,21},tiers);
        }
        [Test] public void PullWoundCanFinishBattleImmediately()
        {
            var b=Battle();var e=b.Enemies[0];e.TakeDamage(996);Add(e.Statuses,CombatStatusKind.Wound,4);
            Assert.That(b.TryUseLunaPull(4,-2),Is.True);Assert.That(b.Phase,Is.EqualTo(BattlePhase.Victory));
            Assert.That(b.LastPullDamage,Is.EqualTo(3));
        }
        [Test] public void WeakenChangesSkillPreviewAndActualDamageTogether()
        {
            var b=new BattleSession(new BattleDefinition(100,3,new[]{new EnemyDefinition("t","t",6,-2,99,0)},CombatArchetype.Ian,fragments:true,approach:CombatApproach.Execution));
            b.EnableStatusRules();Add(b.Tactics.Statuses,CombatStatusKind.Weaken,3);
            var p=b.PreviewExecutionDash(9,-2);Assert.That(p.Damage,Is.EqualTo(7));
            Assert.That(b.TryUseExecutionDash(9,-2),Is.True);Assert.That(b.Enemies[0].Health,Is.EqualTo(92));
        }
        [Test] public void Rules30RoundTripAndRules29HeaderRejectsNewCommand()
        {
            var flow=new PrototypeGameFlow(PrototypeCharacterCatalog.All,302);flow.OpenCharacterSelection();flow.TrySelectCharacter(1);flow.TryStartRun(CombatApproach.Tuning, false);
            var run=flow.CurrentRun;Assert.That(run.HasStatusRules,Is.True);run.TrySelectMapNode(0);run.TryUseLunaPull(5.5,-1);
            var bytes=RunSaveStore.Encode(run.CaptureSave());Assert.That(RunSaveStore.TryDecode(bytes,out var data),Is.True);
            Assert.That(RunGameSession.TryRestore(data,out var copy,out _),Is.True);Assert.That(copy.HasStatusRules,Is.True);
            Assert.That(copy.CurrentBattle.Battle.Enemies[0].X,Is.EqualTo(run.CurrentBattle.Battle.Enemies[0].X));
            Array.Copy(BitConverter.GetBytes(29),0,bytes,8,4);using(var hash=SHA256.Create())Array.Copy(hash.ComputeHash(bytes,0,bytes.Length-32),0,bytes,bytes.Length-32,32);
            Assert.That(RunSaveStore.TryDecode(bytes,out _),Is.False);
        }
    }
}

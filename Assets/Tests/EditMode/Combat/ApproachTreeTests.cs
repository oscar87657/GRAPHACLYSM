using System.Collections.Generic;
using Graphaclysm.Application;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Equations;
using Graphaclysm.Core.Runs;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class ApproachTreeTests
    {
        private static RunGrowthState Growth(CombatApproach a)
        { var g=new RunGrowthState(a>=CombatApproach.Tuning?CombatArchetype.Luna:CombatArchetype.Ian,a);g.AddPoints(100);return g; }
        private static void Buy(RunGrowthState g,int i)
        {if(g.IsUnlocked(i))return;Buy(g,ApproachGrowthCatalog.Parent(i));Assert.That(g.TryPurchase(i),Is.True,g.GetNode(i).Id);}
        private static BattleSession Battle(CombatApproach a,CompiledGrowthBuild build)
        {
            var b=new BattleSession(new BattleDefinition(100,3,new[]{new EnemyDefinition("a","a",5.6,-2,999,0)},
                a>=CombatApproach.Tuning?CombatArchetype.Luna:CombatArchetype.Ian,fragments:true,approach:a),100,0,0,6);
            b.EnableStatusRules();b.EnableApproachUltimates();b.EnableSpecialization(build);return b;
        }
        [TestCase(CombatApproach.Execution)] [TestCase(CombatApproach.Recording)]
        [TestCase(CombatApproach.Tuning)] [TestCase(CombatApproach.Observation)]
        public void EveryNodeIsReachableAndLayoutOnlyGrowsUp(CombatApproach a)
        {
            var g=Growth(a);var children=new int[55];
            Assert.That(g.NodeCount,Is.EqualTo(55));Assert.That(g.IsUnlocked(0),Is.True);Assert.That(g.GetNode(0).Cost,Is.Zero);
            for(int i=1;i<55;i++)
            {
                int p=ApproachGrowthCatalog.Parent(i);Assert.That(p,Is.LessThan(i));children[p]++;
                Assert.That(ApproachGrowthCatalog.Depth(i),Is.GreaterThan(ApproachGrowthCatalog.Depth(p)));
                var candidate=Growth(a);Buy(candidate,i);Assert.That(candidate.CanRefund(i),Is.True);
                var entry=ApproachGrowthCatalog.Get(a,i);var battle=Battle(a,candidate.CreateCompiledBuild());
                if(entry.Rule!=ApproachRule.None)Assert.That(battle.HasRule(entry.Rule),Is.True);
                else Assert.That(entry.Trigger,Is.InRange(0,14));
                Assert.That(candidate.TryRefund(i),Is.True);Assert.That(candidate.TryReset() || candidate.SpentPoints==0,Is.True);
                Assert.That(candidate.IsUnlocked(0),Is.True);
            }
            foreach(int count in children)Assert.That(count,Is.LessThanOrEqualTo(3));
        }
        [Test] public void AllStableIdsAndCommandsAreUnique()
        {
            var ids=new HashSet<string>();var commands=new HashSet<int>();
            for(int a=1;a<=4;a++)for(int i=0;i<55;i++)
            {var n=ApproachGrowthCatalog.Get((CombatApproach)a,i).Node;Assert.That(ids.Add(n.Id),Is.True);Assert.That(commands.Add(n.CommandId),Is.True);}
        }
        private static BattleSession With(CombatApproach a, params ApproachRule[] rules)
        {
            var ids=new List<string>();
            for(int i=1;i<55;i++)foreach(var rule in rules)if(ApproachGrowthCatalog.Get(a,i).Rule==rule)ids.Add(ApproachGrowthCatalog.Get(a,i).Node.Id);
            return Battle(a,new CompiledGrowthBuild(ids.ToArray(),new string[0]));
        }
        private static void Play(BattleSession b,bool ultimate=false)
        {
            Assert.That(b.TryPlayCard(CardDefinition.CreateWeaveFragment("test.plain","plain",FragmentKind.HomeAnchor,CardRarity.Common,"test",1),out _),Is.True);
            if(ultimate)Assert.That(b.TryToggleUltimate(),Is.True);
            Assert.That(b.TryBeginPlot(),Is.True);b.ResolvePlot();
        }
        [Test] public void ExecutionUpgradeChangesPreviewAndActualUltimateDamage()
        {
            var b=With(CombatApproach.Execution,ApproachRule.DashReach,ApproachRule.BurstReach,ApproachRule.BurstPower,ApproachRule.BurstFocus);
            Assert.That(b.CurrentExecutionTravel,Is.EqualTo(4.5).Within(.001));Assert.That(b.CurrentBurstRadius,Is.EqualTo(2.4).Within(.001));
            b.TryPlayCard(CardDefinition.CreateWeaveFragment("test.plain","plain",FragmentKind.HomeAnchor,CardRarity.Common,"test",1),out _);b.TryToggleUltimate();
            Assert.That(b.PreviewApproachUltimateDamage(b.Enemies[0]),Is.EqualTo(20));
            int damage=b.PreviewHealthDamage(b.Enemies[0]);b.TryBeginPlot();b.ResolvePlot();
            Assert.That(999-b.Enemies[0].Health,Is.EqualTo(damage));Assert.That(b.Tactics.Statuses.Get(CombatStatusKind.Focus),Is.EqualTo(4));
        }
        [Test] public void RecordingUltimateGainsAThirdRealReplayAndGuard()
        {
            var b=With(CombatApproach.Recording,ApproachRule.EchoCount,ApproachRule.EchoPower,ApproachRule.EchoGuard);Play(b,true);
            Assert.That(b.UltimateEchoes,Is.EqualTo(3));
            for(int i=0;i<3;i++)
            {b.ResolveEnemyTurn();Play(b);Assert.That(b.LastUltimateHits,Is.EqualTo(1));Assert.That(b.UltimateEchoes,Is.EqualTo(2-i));Assert.That(b.Tactics.Statuses.Get(CombatStatusKind.Shield),Is.GreaterThanOrEqualTo(6));}
        }
        [Test] public void TuningReflectionAndExtraAssemblyGuardAreRealRules()
        {
            var b=With(CombatApproach.Tuning,ApproachRule.Reflect,ApproachRule.TuneReach,ApproachRule.PullArea,ApproachRule.ExtraGuard);
            Assert.That(b.CanReflectDiagram,Is.True);Assert.That(b.CurrentDiagramReach,Is.EqualTo(4));Assert.That(b.CurrentPullRadius,Is.EqualTo(3).Within(.001));
            Play(b,true);b.ResolveEnemyTurn();Assert.That(b.InExtraAssembly,Is.True);Play(b);
            Assert.That(b.Tactics.Statuses.Get(CombatStatusKind.Shield),Is.GreaterThanOrEqualTo(8));
        }
        [Test] public void ObservationSanctuaryLastsThreeActionsAndUsesEnhancedShield()
        {
            var b=With(CombatApproach.Observation,ApproachRule.SanctuaryTime,ApproachRule.SanctuaryShield,ApproachRule.SatelliteReach,ApproachRule.SatelliteFocus);
            Assert.That(b.CurrentSatelliteReach,Is.EqualTo(4));Assert.That(b.TryPlaceSatellite(5.6,-2),Is.True);
            Assert.That(b.Tactics.Statuses.Get(CombatStatusKind.Focus),Is.EqualTo(3));Play(b,true);
            Assert.That(b.SanctuaryActions,Is.EqualTo(3));Assert.That(b.CurrentSanctuaryShield,Is.EqualTo(16));
            b.ResolveEnemyTurn();Assert.That(b.SanctuaryActions,Is.EqualTo(2));
        }
        [TestCase(CombatApproach.Execution)] [TestCase(CombatApproach.Recording)]
        [TestCase(CombatApproach.Tuning)] [TestCase(CombatApproach.Observation)]
        public void ChoicesRefundAndSaveReplayPreserveTheNewTree(CombatApproach a)
        {
            var flow=new PrototypeGameFlow(PrototypeCharacterCatalog.All,37);flow.OpenCharacterSelection();flow.TrySelectCharacter(a>=CombatApproach.Tuning?1:0);
            Assert.That(flow.TryStartRun(a, false),Is.True);var r=flow.CurrentRun;
            // Restore a genuine previous-rules journal, without the new StyleTree opt-in.
            var old=r.CaptureSave();System.Array.Resize(ref old.Commands,11);
            Assert.That(RunGameSession.TryRestore(old,out r,out _),Is.True);
            Assert.That(r.Growth.NodeCount,Is.EqualTo(55));Assert.That(r.TryPurchaseGrowthNode(1),Is.True);Assert.That(r.TryPurchaseGrowthNode(2),Is.True);
            Assert.That(r.TryPurchaseGrowthNode(3),Is.False);Assert.That(r.TryPurchaseGrowthNode(5),Is.False);
            Assert.That(r.TryRefundGrowthNode(1),Is.False);Assert.That(r.TryRefundGrowthNode(0),Is.False);
            Assert.That(RunSaveStore.TryDecode(RunSaveStore.Encode(r.CaptureSave()),out var data),Is.True);
            Assert.That(RunGameSession.TryRestore(data,out var restored,out _),Is.True);
            Assert.That(restored.Growth.NodeCount,Is.EqualTo(55));Assert.That(restored.Growth.IsUnlocked(2),Is.True);
            Assert.That(restored.Growth.Points,Is.EqualTo(r.Growth.Points));Assert.That(restored.TryRefundGrowthNode(2),Is.True);
            Assert.That(restored.TryPurchaseGrowthNode(3),Is.True);
        }
        [TestCase(CombatApproach.Execution)] [TestCase(CombatApproach.Recording)]
        [TestCase(CombatApproach.Tuning)] [TestCase(CombatApproach.Observation)]
        public void ShortPlotRewardActuallyAppliesOnlyOnce(CombatApproach a)
        {
            var e=ApproachGrowthCatalog.Get(a,4);
            Assert.That(e.Trigger,Is.Zero);
            var b=Battle(a,new CompiledGrowthBuild(new[]{e.Node.Id},new string[0]));
            var card=CardDefinition.CreateWeaveFragment("test.plain","plain",FragmentKind.HomeAnchor,CardRarity.Common,"test",1);
            Assert.That(b.TryPlayCard(card,out _),Is.True);Assert.That(b.TryBeginPlot(),Is.True);b.ResolvePlot();
            Assert.That(b.LastGrowthTriggers,Is.EqualTo(1));
            Assert.That(e.Enemy?b.Enemies[0].Statuses.Get(e.Status):b.Tactics.Statuses.Get(e.Status),Is.EqualTo(e.Magnitude));
        }
    }
}

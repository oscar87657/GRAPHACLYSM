using System;
using Graphaclysm.Application;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class NodeTrialTests
    {
        private static PrototypeGameFlow Start(bool luna=false)
        {
            var flow=new PrototypeGameFlow(PrototypeCharacterCatalog.All,712);
            flow.OpenCharacterSelection(); flow.TrySelectCharacter(luna?1:0);
            Assert.That(flow.TryStartRun(luna?CombatApproach.Tuning:CombatApproach.Execution, false),Is.True);
            return flow;
        }
        private static byte[] Fingerprint(RunGameSession run)
        {
            var save=run.CaptureSave(); save.SavedUtcTicks=0;
            return RunSaveStore.Encode(save);
        }
        [TestCase("ian.inscription.core",false)]
        [TestCase("ian.archive.core",false)]
        [TestCase("luna.orbit.core",true)]
        [TestCase("luna.binary.core",true)]
        public void LockedCoreCanBeTriedAndOriginalObjectJournalAndNextSeedRemainUnchanged(string id,bool luna)
        {
            var flow=Start(luna); var original=flow.CurrentRun; var character=flow.CurrentCharacter;
            var baseline=Start(luna); var bytes=Fingerprint(original);
            int points=original.Growth.Points;
            Assert.That(original.Growth.IsUnlocked(id),Is.False);
            Assert.That(flow.TryStartNodeTrial(id),Is.True);
            Assert.That(flow.IsNodeTrial,Is.True); Assert.That(flow.TrialReturnRun,Is.SameAs(original));
            Assert.That(flow.CurrentRun.IsPractice,Is.True); Assert.That(flow.CurrentRun.CanSave,Is.False);
            Assert.That(flow.CurrentRun.CurrentBattle.Battle.PlayedCardCount,Is.EqualTo(2));
            Assert.That(flow.CurrentRun.CurrentBattle.Battle.Approach,Is.EqualTo(NodeTrials.ApproachFor(id)));
            Assert.That(flow.TryStartNodeTrial(id),Is.False);
            Assert.That(flow.CurrentRun.TryBeginPlot(),Is.True); flow.CurrentRun.ResolvePlot(); flow.CurrentRun.ResolveEnemyTurn();
            Assert.That(flow.TryRestartRun(),Is.True);
            Assert.That(flow.CurrentRun.CurrentBattle.Battle.Turn,Is.EqualTo(1));
            Assert.That(flow.CurrentRun.CurrentBattle.Battle.PlayedCardCount,Is.EqualTo(2));
            Assert.That(flow.TryReturnFromNodeTrial(),Is.True);
            Assert.That(flow.CurrentRun,Is.SameAs(original)); Assert.That(flow.CurrentCharacter,Is.SameAs(character));
            Assert.That(flow.CurrentRun.Growth.Points,Is.EqualTo(points)); Assert.That(flow.CurrentRun.Growth.IsUnlocked(id),Is.False);
            CollectionAssert.AreEqual(bytes,Fingerprint(original));
            Assert.That(flow.TrialReturnRun,Is.Null); Assert.That(flow.TrialNodeId,Is.Null);
            flow.TryRestartRun(); baseline.TryRestartRun(); Assert.That(flow.CurrentRun.Seed,Is.EqualTo(baseline.CurrentRun.Seed));
        }
        [Test]
        public void UnsupportedForeignAndBattleEntriesAreRejectedWithoutMutation()
        {
            var flow=Start(); var original=flow.CurrentRun; var bytes=Fingerprint(original);
            Assert.That(flow.TryStartNodeTrial("unknown"),Is.False);
            Assert.That(flow.TryStartNodeTrial("luna.binary.core"),Is.False);
            Assert.That(flow.TryStartNodeTrial("ian.execute.m2"),Is.False);
            CollectionAssert.AreEqual(bytes,Fingerprint(original));
            original.TrySelectMapNode(0);
            Assert.That(flow.TryStartNodeTrial("ian.archive.core"),Is.False); Assert.That(flow.CurrentRun,Is.SameAs(original));
        }
        [TestCase(false)]
        [TestCase(true)]
        public void GenericExitCannotDiscardSuspendedCampaign(bool characterSelection)
        {
            var flow=Start(); var original=flow.CurrentRun;
            flow.TryStartNodeTrial("ian.archive.core");
            if(characterSelection) flow.ReturnToCharacterSelection(); else flow.ReturnToMainMenu();
            Assert.That(flow.CurrentRun,Is.SameAs(original)); Assert.That(flow.Phase,Is.EqualTo(GameFlowPhase.Run));
            Assert.That(flow.IsNodeTrial,Is.False);
        }
        [TestCase("ian.inscription.core")]
        [TestCase("ian.archive.core")]
        [TestCase("luna.orbit.core")]
        [TestCase("luna.binary.core")]
        public void ComparisonRunsActualSkillOnSamePreparedDiagram(string id)
        {
            var comparison=new NodeTrialComparison(id);
            var before=comparison.Before.CurrentBattle.Battle; var after=comparison.After.CurrentBattle.Battle;
            Assert.That(before.PlayedCardCount,Is.EqualTo(2)); Assert.That(after.PlayedCardCount,Is.EqualTo(2));
            Assert.That(before.Tactics.X,Is.EqualTo(4)); Assert.That(before.Equation.Fragments.OriginX,Is.EqualTo(4));
            Assert.That(comparison.Before.CanSave || comparison.After.CanSave,Is.False);
            switch(comparison.Approach)
            {
                case CombatApproach.Execution:
                    Assert.That(after.Tactics.X,Is.EqualTo(7.6).Within(.001));
                    Assert.That(after.Enemies[0].Health,Is.LessThan(before.Enemies[0].Health));
                    Assert.That(after.Enemies[0].Statuses.Get(CombatStatusKind.Rupture),Is.EqualTo(6)); break;
                case CombatApproach.Recording:
                    Assert.That(after.RecordingArmed,Is.True); Assert.That(comparison.RecordGhost,Is.Not.Null);
                    Assert.That(Graphaclysm.Core.Equations.EquationAnalyzer.IntersectsCircle(comparison.RecordGhost,
                        after.Enemies[1].X,after.Enemies[1].Y,BattleSession.EnemyHitRadius,comparison.RecordGhost.CurveSegmentCount),Is.True);
                    Assert.That(after.PreviewDamage(after.Enemies[0]),Is.LessThan(before.PreviewDamage(before.Enemies[0]))); break;
                case CombatApproach.Tuning:
                    Assert.That(after.Equation.Fragments.OriginX,Is.EqualTo(5.25)); Assert.That(after.Tactics.X,Is.EqualTo(4)); break;
                case CombatApproach.Observation:
                    Assert.That(before.PreviewPlayerHit,Is.False); Assert.That(after.SatellitePlotHit,Is.True);
                    Assert.That(after.BodyPlotHit,Is.False); break;
            }
        }
        [Test]
        public void ComparisonDoesNotPretendUnimplementedNodesWork()
        {
            Assert.Throws<ArgumentException>(()=>new NodeTrialComparison("luna.rotate.awaken"));
            Assert.That(NodeTrials.ApproachFor("luna.u.timeless.form"),Is.EqualTo(CombatApproach.None));
        }
    }
}

using System;
using System.Reflection;
using System.Security.Cryptography;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Runs;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class ApproachCampaignTests
    {
        [Test]
        public void IntroVictoryGivesGrowthAndContinuesIntoAnotherRealBattle()
        {
            var run = Start(CombatApproach.Tuning);
            int startingPoints = run.Growth.Points;
            for (int turn = 0; turn < 8 && run.Phase == RunPhase.Battle; turn++)
            {
                foreach (string id in new[] { "frag.ellipse", "frag.expand" })
                {
                    bool played = false;
                    for (int i = 0; i < run.CurrentBattle.Deck.HandCount; i++)
                        if (run.CurrentBattle.Deck.GetHandCard(i).Id == id)
                        { played = run.TryPlayHandCard(i, out _, out _); break; }
                    Assert.That(played, Is.True);
                }
                Assert.That(run.TryUseDiagramAbility(turn < 2 ? 5.25 : 5.5, turn < 2 ? -1.5 : .5, turn < 2 ? -90 : 0), Is.True);
                Assert.That(run.TryBeginPlot(), Is.True); run.ResolvePlot();
                if (run.Phase == RunPhase.Battle) run.ResolveEnemyTurn();
                run = Restore(run);
            }
            Assert.That(run.Phase, Is.EqualTo(RunPhase.CardReward));
            Assert.That(run.Growth.Points, Is.GreaterThan(startingPoints));
            Assert.That(run.TrySkipReward(), Is.True);
            for (int i = 0; i < run.Map.Definition.NodeCount; i++)
                if (run.Map.IsAvailable(i)) { Assert.That(run.TrySelectMapNode(i), Is.True); break; }
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Battle));
            Assert.That(run.CurrentBattle.Battle.Approach, Is.EqualTo(CombatApproach.Tuning));
            Restore(run);
        }
        [Test]
        public void InvalidCharacterChoiceDoesNotCreateOrConsumeARun()
        {
            var flow = new PrototypeGameFlow(PrototypeCharacterCatalog.All, 302);
            flow.OpenCharacterSelection(); flow.TrySelectCharacter(0);
            Assert.That(flow.TryStartRun(CombatApproach.Tuning, false), Is.False);
            Assert.That(flow.CurrentRun, Is.Null); Assert.That(flow.CurrentCharacter, Is.Null);
            Assert.That(flow.TryStartRun(CombatApproach.Recording, false), Is.True);
            Assert.That(flow.CurrentRun.Seed, Is.EqualTo(302));
            Assert.That(flow.TryRestartRun(), Is.True);
            Assert.That(flow.CurrentRun.StartingApproach, Is.EqualTo(CombatApproach.Recording));
        }

        [TestCase(CombatApproach.Execution, "ian.execute.m2")]
        [TestCase(CombatApproach.Recording, "ian.archive.m3")]
        [TestCase(CombatApproach.Tuning, "luna.rotate.m2")]
        public void TraitCanBeAcquiredAndEquippedThroughTheExistingTree(CombatApproach approach, string id)
        {
            var run = PrototypeRunFactory.Create(302, PrototypeCharacterCatalog.All[approach == CombatApproach.Tuning ? 1 : 0]);
            run.TryChooseApproach(approach);
            var growth = run.Growth; growth.AddPoints(29); // Core progression fixture: final 35-point budget.
            var plans = new GrowthAcquisitionPlanner(growth).Plan(growth.IndexOf(id));
            Assert.That(plans.Count, Is.GreaterThan(0));
            var order = plans[0].NodeIndices;
            for (int step = 0; step < 35 && !growth.IsUnlocked(id); step++)
            {
                int candidate = -1;
                for (int i = 0; i < order.Count; i++) if (growth.CanPurchase(order[i])) { candidate = order[i]; break; }
                if (candidate < 0)
                    for (int i = 0; i < growth.NodeCount; i++)
                    {
                        var node = growth.GetNode(i);
                        if (node.Owner == GrowthOwner.Common && string.IsNullOrEmpty(node.ExclusiveGroup) && growth.CanPurchase(i)) { candidate = i; break; }
                    }
                if (candidate < 0) break;
                Assert.That(run.TryPurchaseGrowthNode(candidate), Is.True);
            }
            Assert.That(growth.IsUnlocked(id), Is.True, "Trait should not be a dead path.");
            var trait = growth.GetNode(growth.IndexOf(id));
            for (int i = 0; i < trait.RequiresAll.Length; i++)
            {
                int parent = growth.IndexOf(trait.RequiresAll[i]);
                if (!growth.IsEquipped(parent)) growth.TrySelect(parent);
            }
            if (!growth.IsEquipped(id)) growth.TrySelect(growth.IndexOf(id));
            Assert.That(growth.IsEquipped(id), Is.True);
            Assert.That(growth.CreateLoadout().Growth.IsEquipped(id), Is.True);
        }
        private static RunGameSession Start(CombatApproach approach)
        {
            var run = PrototypeRunFactory.Create(302, PrototypeCharacterCatalog.All[approach == CombatApproach.Tuning ? 1 : 0]);
            Assert.That(run.TryChooseApproach(approach), Is.True);
            Assert.That(run.TrySelectMapNode(0), Is.True); return run;
        }
        private static RunGameSession Restore(RunGameSession run)
        {
            Assert.That(RunSaveStore.TryDecode(RunSaveStore.Encode(run.CaptureSave()), out var data), Is.True);
            Assert.That(RunGameSession.TryRestore(data, out var result, out _), Is.True);
            Assert.That(result.StartingApproach, Is.EqualTo(run.StartingApproach));
            Assert.That(result.Revision, Is.EqualTo(run.Revision));
            return result;
        }
        [TestCase(CombatApproach.Execution)] [TestCase(CombatApproach.Recording)] [TestCase(CombatApproach.Tuning)]
        public void StartingChoiceSurvivesDiskAndCannotChangeAfterEntry(CombatApproach approach)
        {
            var run = Start(approach);
            Assert.That(run.IsPractice, Is.False); Assert.That(run.CanSave, Is.True);
            var restored = Restore(run);
            Assert.That(restored.CurrentBattle.Battle.Approach, Is.EqualTo(approach));
            Assert.That(restored.TryChooseApproach(approach), Is.False);
        }
        [TestCase(CombatApproach.Recording)] [TestCase(CombatApproach.Tuning)]
        public void DiagramCommandAndPendingRecordReplayExactly(CombatApproach approach)
        {
            var run = Start(approach); var b = run.CurrentBattle.Battle;
            Assert.That(run.TryPlayHandCard(0, out _, out _), Is.True);
            double ox = b.Equation.Fragments.OriginX, oy = b.Equation.Fragments.OriginY;
            int angle = approach == CombatApproach.Tuning ? 60 : 0;
            Assert.That(run.TryUseDiagramAbility(ox + .1234, oy + .2178, angle), Is.True);
            var restored = Restore(run); var other = restored.CurrentBattle.Battle;
            for (int i = 0; i < 101; i++)
            {
                b.Equation.Sample(i / 101.0, out double x, out double y);
                other.Equation.Sample(i / 101.0, out double rx, out double ry);
                Assert.That(rx, Is.EqualTo(x)); Assert.That(ry, Is.EqualTo(y));
            }
            Assert.That(other.RecordingArmed, Is.EqualTo(b.RecordingArmed));
            Assert.That(run.TryBeginPlot(), Is.True); run.ResolvePlot();
            restored = Restore(run);
            Assert.That(restored.CurrentBattle.Battle.HasRecording, Is.EqualTo(b.HasRecording));
            if (b.HasRecording)
            {
                b.RecordedDiagram.Sample(.31, out double x, out double y);
                restored.CurrentBattle.Battle.RecordedDiagram.Sample(.31, out double rx, out double ry);
                Assert.That(rx, Is.EqualTo(x)); Assert.That(ry, Is.EqualTo(y));
            }
        }
        [Test]
        public void Version21SaveRetainsClassicRules()
        {
            var run = PrototypeRunFactory.Create(302); run.TrySelectMapNode(0);
            byte[] data = RunSaveStore.Encode(run.CaptureSave());
            Array.Copy(BitConverter.GetBytes(21), 0, data, 8, 4);
            using (var sha = SHA256.Create()) Array.Copy(sha.ComputeHash(data, 0, data.Length - 32), 0, data, data.Length - 32, 32);
            Assert.That(RunSaveStore.TryDecode(data, out var decoded), Is.True);
            Assert.That(RunGameSession.TryRestore(decoded, out var restored, out _), Is.True);
            Assert.That(restored.StartingApproach, Is.EqualTo(CombatApproach.None));
            Assert.That(restored.CurrentBattle.Battle.Approach, Is.EqualTo(CombatApproach.None));
        }
        private static BattleSession WithTrait(CombatApproach approach, string trait)
        {
            var build = new CompiledGrowthBuild(new[] { trait }, new[] { trait });
            return new BattleSession(new BattleDefinition(100, 3, new[] {
                new EnemyDefinition("target", "Target", 6.2, -2, 100, 0)
            }, approach == CombatApproach.Tuning ? CombatArchetype.Luna : CombatArchetype.Ian,
                fragments: true, approach: approach), 100, 0, 0, skillLoadout: new BattleSkillLoadout(0, 0, 0, growth: build));
        }
        [Test]
        public void CondenseTraitRecordsWithoutSpendingThePendingCardEffect()
        {
            var b = WithTrait(CombatApproach.Recording, "ian.archive.m3");
            b.TryPlayCard(FragmentCardCatalog.Find("frag.expand"), out _);
            int health = b.Enemies[0].Health;
            Assert.That(b.TryCondense(), Is.True);
            Assert.That(b.HasRecording, Is.True); Assert.That(b.Enemies[0].Health, Is.EqualTo(health));
            Assert.That(b.Enemies[0].Statuses.Get(CombatStatusKind.Exposure), Is.Zero);
            b.ResolveEnemyTurn(); b.TryBeginPlot(); b.ResolvePlot();
            Assert.That(b.LastRecordingHits, Is.EqualTo(1)); Assert.That(b.HasRecording, Is.False);
        }
        [Test]
        public void ReflectionTraitChangesGeometryAndPreviewAgrees()
        {
            var b = WithTrait(CombatApproach.Tuning, "luna.rotate.m2");
            b.TryPlayCard(FragmentCardCatalog.Find("frag.down"), out _);
            b.Equation.Sample(.2, out double x, out double y);
            var preview = b.PreviewDiagramAbility(4, -2, 180);
            Assert.That(preview, Is.Not.Null);
            preview.Sample(.2, out double px, out double py);
            Assert.That(px, Is.EqualTo(x)); Assert.That(py, Is.EqualTo(-4-y).Within(1e-10));
            Assert.That(b.TryUseDiagramAbility(4, -2, 180), Is.True);
            b.Equation.Sample(.2, out x, out y);
            Assert.That(x, Is.EqualTo(px)); Assert.That(y, Is.EqualTo(py));
        }
        [Test]
        public void ExecutionTraitConsumesRuptureWithMatchingPreview()
        {
            var b = WithTrait(CombatApproach.Execution, "ian.execute.m2");
            typeof(CombatStatusState).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(b.Enemies[0].Statuses, new object[] { CombatStatusKind.Rupture, 6, 2 });
            var preview = b.PreviewCombatSkillContact(b.PreviewCombatSkill(0), 0);
            Assert.That(preview.HealthDamage, Is.EqualTo(16));
            Assert.That(b.TryUseCombatSkill(0), Is.True);
            Assert.That(b.LastSkillDamage, Is.EqualTo(preview.HealthDamage));
            Assert.That(b.Enemies[0].Statuses.Get(CombatStatusKind.Rupture), Is.Zero);
        }
    }
}

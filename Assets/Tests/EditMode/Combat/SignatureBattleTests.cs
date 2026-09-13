using System;
using System.Reflection;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Equations;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class SignatureBattleTests
    {
        [TestCase(5, 0, true)]
        [TestCase(9, 3, false)]
        [TestCase(0, 0, false)]
        public void RecordingInvalidPlacementRetainsDisplayWithoutEnablingUse(double x, double y, bool expected)
        {
            var run = SignatureBattleFactory.Create(CombatApproach.Recording);
            Play(run, "frag.ellipse");
            var battle = run.CurrentBattle.Battle;
            var preview = battle.PreviewRecordingPlacement(x, y, out bool allowed);
            Assert.That(preview, Is.Not.Null);
            Assert.That(allowed, Is.EqualTo(expected));
            Assert.That(battle.RecordingArmed, Is.False);
            Assert.That(battle.CanUseDiagramAbility, Is.True);
            if (!expected)
            {
                Assert.That(battle.PreviewDiagramAbility(x, y, 0), Is.Null);
                Assert.That(run.TryUseDiagramAbility(x, y, 0), Is.False);
                Assert.That(battle.CanUseDiagramAbility, Is.True);
            }
        }

        [Test]
        public void PracticeDoesNotAdvanceCampaignSeedAndRestartRetainsApproach()
        {
            var normal = new PrototypeGameFlow(PrototypeCharacterCatalog.All, 901);
            normal.OpenCharacterSelection(); normal.TryStartRun();
            var flow = new PrototypeGameFlow(PrototypeCharacterCatalog.All, 901);
            Assert.That(flow.TryStartPractice(CombatApproach.Recording), Is.True);
            Assert.That(flow.TryRestartRun(), Is.True);
            Assert.That(flow.CurrentRun.IsPractice, Is.True);
            Assert.That(flow.CurrentRun.CurrentBattle.Battle.Approach, Is.EqualTo(CombatApproach.Recording));
            flow.ReturnToMainMenu(); flow.OpenCharacterSelection(); flow.TryStartRun();
            Assert.That(flow.CurrentRun.Seed, Is.EqualTo(normal.CurrentRun.Seed));
            Assert.That(flow.CurrentRun.CanSave, Is.True);
        }

        [Test]
        public void CuttingPowerActuallyPreventsGunnerDamageAndResetRestoresIt()
        {
            var b = new BattleSession(new BattleDefinition(42, 3, new[] {
                new EnemyDefinition("signature.cannon", "Cannon", 8, 1.8, 100, 12, EnemyBehaviorDefinition.LineGunner())
            }, CombatArchetype.Luna, fragments: true, approach: CombatApproach.Tuning));
            b.TryPlayCard(FragmentCardCatalog.Find("frag.ellipse"), out _);
            b.TryPlayCard(FragmentCardCatalog.Find("frag.expand"), out _);
            Assert.That(b.TryUseDiagramAbility(5.25, -1.5, -90), Is.True);
            Assert.That(b.PreviewCannonCut(b.Equation), Is.True);
            Assert.That(b.CannonDisconnected, Is.False);
            b.TryBeginPlot(); b.ResolvePlot();
            typeof(CombatStatusState).GetMethod("Remove", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(b.Tactics.Statuses, new object[] { CombatStatusKind.Shield });
            Assert.That(b.ResolveEnemyTurn(), Is.Zero);
            b.Reset(); Assert.That(b.CannonDisconnected, Is.False);
            b.TryUnravel(); Assert.That(b.ResolveEnemyTurn(), Is.EqualTo(12));
        }

        [Test]
        public void ArrangementAppearsInFormulaAndDoesNotLeakIntoNextTurn()
        {
            var run = SignatureBattleFactory.Create(CombatApproach.Tuning); var b = run.CurrentBattle.Battle;
            Play(run, "frag.ellipse"); b.TryUseDiagramAbility(5, -1, 60);
            Assert.That(b.Equation.BuildFormula(), Does.Contain("60°"));
            b.TryUnravel(); b.ResolveEnemyTurn();
            b.TryPlayCard(FragmentCardCatalog.Find("frag.ellipse"), out _);
            Assert.That(b.CanUseDiagramAbility, Is.True);
            Assert.That(b.Equation.Fragments.OriginX, Is.EqualTo(4));
            Assert.That(b.Equation.Fragments.OriginY, Is.EqualTo(-2));
            Assert.That(b.Equation.BuildFormula(), Does.Not.Contain("60°"));
        }

        [Test]
        public void RecordingConsumesAnExistingMarkExactlyOnce()
        {
            var run = SignatureBattleFactory.Create(CombatApproach.Recording); var b = run.CurrentBattle.Battle;
            Play(run, "frag.ellipse"); Play(run, "frag.expand"); b.TryUseDiagramAbility(5.5, .5);
            b.TryMovePlayerTo(2.2, -2);
            b.TryBeginPlot(); b.ResolvePlot(); b.ResolveEnemyTurn();
            typeof(CombatStatusState).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(b.Enemies[2].Statuses, new object[] { CombatStatusKind.Rupture, 6, 2 });
            b.TryPlayCard(FragmentCardCatalog.Find("frag.contract"), out _);
            int before = b.Enemies[2].Health, predicted = b.PreviewHealthDamage(b.Enemies[2]);
            Assert.That(predicted, Is.GreaterThan(6));
            b.TryBeginPlot(); b.ResolvePlot();
            Assert.That(before - b.Enemies[2].Health, Is.EqualTo(predicted));
            Assert.That(b.Enemies[2].Statuses.Get(CombatStatusKind.Rupture), Is.Zero);
            b.ResolveEnemyTurn(); b.TryPlayCard(FragmentCardCatalog.Find("frag.contract"), out _);
            b.TryBeginPlot(); b.ResolvePlot(); Assert.That(b.LastRecordingHits, Is.Zero);
        }
        private static void Play(RunGameSession run, string id)
        {
            var game = run.CurrentBattle;
            for (int i = 0; i < game.Deck.HandCount; i++)
                if (game.Deck.GetHandCard(i).Id == id)
                { Assert.That(game.TryPlayHandCard(i, out _, out _), Is.True); return; }
            Assert.Fail("Missing starting material " + id);
        }

        [Test]
        public void ThreeApproachesHaveSameHandHealthEnemiesAndNoSave()
        {
            var a = SignatureBattleFactory.Create(CombatApproach.Execution);
            foreach (CombatApproach approach in new[] { CombatApproach.Recording, CombatApproach.Tuning })
            {
                var b = SignatureBattleFactory.Create(approach);
                Assert.That(b.IsPractice, Is.True); Assert.That(b.CanSave, Is.False);
                Assert.Throws<InvalidOperationException>(() => b.CaptureSave());
                Assert.That(b.CurrentBattle.Battle.PlayerHealth, Is.EqualTo(a.CurrentBattle.Battle.PlayerHealth));
                for (int i = 0; i < 6; i++) Assert.That(b.CurrentBattle.Deck.GetHandCard(i).Id, Is.EqualTo(a.CurrentBattle.Deck.GetHandCard(i).Id));
                for (int i = 0; i < 3; i++) Assert.That(b.CurrentBattle.Battle.Enemies[i].Definition.X, Is.EqualTo(a.CurrentBattle.Battle.Enemies[i].Definition.X));
            }
        }

        [Test]
        public void TuningPreviewIsPureAndMatchesEveryCommittedSample()
        {
            var run = SignatureBattleFactory.Create(CombatApproach.Tuning); var b = run.CurrentBattle.Battle;
            Play(run, "frag.ellipse"); Play(run, "frag.expand");
            b.Equation.Sample(.21, out double beforeX, out double beforeY);
            EquationState preview = null;
            for (int i = 0; i < 100; i++) preview = b.PreviewDiagramAbility(5, -1, 60);
            b.Equation.Sample(.21, out double afterX, out double afterY);
            Assert.That(afterX, Is.EqualTo(beforeX)); Assert.That(afterY, Is.EqualTo(beforeY));
            Assert.That(b.CanUseDiagramAbility, Is.True); Assert.That(run.CurrentBattle.Deck.HandCount, Is.EqualTo(4));
            Assert.That(run.TryUseDiagramAbility(5, -1, 60), Is.True);
            for (int i = 0; i < 1600; i++)
            {
                preview.Sample(i / 1600.0, out double x, out double y);
                b.Equation.Sample(i / 1600.0, out double ax, out double ay);
                Assert.That(ax, Is.EqualTo(x).Within(1e-10)); Assert.That(ay, Is.EqualTo(y).Within(1e-10));
            }
            Assert.That(run.TryUseDiagramAbility(5, -1, 60), Is.False);
            Assert.That(run.CurrentBattle.TryUndoLastPlayedCard(out _), Is.False);
            Play(run, "frag.right"); Assert.That(run.CurrentBattle.TryUndoLastPlayedCard(out _), Is.True);
        }

        [TestCase(double.NaN, 0, 0)] [TestCase(9, 3, 0)] [TestCase(5, -1, 31)] [TestCase(5, -1, 120)]
        public void InvalidAimHasNoCost(double x, double y, int angle)
        {
            var run = SignatureBattleFactory.Create(CombatApproach.Tuning);
            Play(run, "frag.ellipse"); var b = run.CurrentBattle.Battle;
            Assert.That(b.PreviewDiagramAbility(x, y, angle), Is.Null);
            Assert.That(run.TryUseDiagramAbility(x, y, angle), Is.False);
            Assert.That(b.CanUseDiagramAbility, Is.True);
        }

        [Test]
        public void ActualStartingHandCanHitMultipleEnemiesProtectSelfAndCutCannon()
        {
            var run = SignatureBattleFactory.Create(CombatApproach.Tuning); var b = run.CurrentBattle.Battle;
            Play(run, "frag.ellipse"); Play(run, "frag.expand");
            int initial = 0;
            foreach (var e in b.Enemies) if (b.PreviewDamage(e) > 0) initial++;
            Assert.That(initial, Is.EqualTo(1));
            for (int angle = -90; angle <= 90; angle += 30)
            for (double x = 3; x <= 7; x += .25)
            for (double y = -1.5; y <= 1; y += .25)
            {
                var p = b.PreviewDiagramAbility(x, y, angle);
                if (p == null || !b.PreviewCannonCut(p)
                    || EquationAnalyzer.CalculateIntersectionDamage(p, 4, -2, .48) == 0) continue;
                int hits = 0;
                foreach (var e in b.Enemies) if (EquationAnalyzer.CalculateIntersectionDamage(p, e.X, e.Y, .48) > 0) hits++;
                if (hits < 2) continue;
                Assert.That(run.TryUseDiagramAbility(x, y, angle), Is.True);
                Assert.That(b.PreviewPlayerHit, Is.True);
                b.TryBeginPlot(); var report = b.ResolvePlot();
                Assert.That(b.CannonDisconnected, Is.True);
                Assert.That(b.Tactics.Statuses.Get(CombatStatusKind.Shield), Is.GreaterThan(0));
                TestContext.WriteLine("Representative action: ellipse + expand; origin " + x + "," + y + "; angle " + angle + "; enemies " + hits);
                return;
            }
            Assert.Fail("No legal representative action exists in the actual starting encounter.");
        }

        [Test]
        public void RecordingWaitsForNextPlotAndDoesNotRepeatCardAbilities()
        {
            var run = SignatureBattleFactory.Create(CombatApproach.Recording); var b = run.CurrentBattle.Battle;
            Play(run, "frag.ellipse"); Play(run, "frag.expand");
            int full = b.PreviewDamage(b.Enemies[0]);
            Assert.That(run.TryUseDiagramAbility(5.5, .5), Is.True);
            Assert.That(b.PreviewDamage(b.Enemies[0]), Is.EqualTo(full * 3 / 4));
            b.TryBeginPlot(); b.ResolvePlot();
            Assert.That(b.HasRecording, Is.True); Assert.That(b.LastRecordingHits, Is.Zero);
            b.ResolveEnemyTurn();
            int predicted = b.PreviewRecordingDamage(b.Enemies[2]); Assert.That(predicted, Is.GreaterThan(0));
            int before = b.Enemies[2].Health;
            Assert.That(b.TryPlayCard(FragmentCardCatalog.Find("frag.contract"), out _), Is.True);
            Assert.That(b.PreviewDamage(b.Enemies[2]), Is.Zero);
            b.TryBeginPlot(); b.ResolvePlot();
            Assert.That(b.Enemies[2].Health, Is.EqualTo(before - predicted));
            Assert.That(b.LastRecordingHits, Is.GreaterThan(0)); Assert.That(b.HasRecording, Is.False);
            Assert.That(b.Enemies[2].Statuses.Get(CombatStatusKind.Weaken), Is.Zero);
            Assert.That(b.Enemies[2].Statuses.Get(CombatStatusKind.Exposure), Is.Zero);
        }

        [Test]
        public void ExecutionCreatesConsumableRuptureAtTheStart()
        {
            var run = SignatureBattleFactory.Create(CombatApproach.Execution); var b = run.CurrentBattle.Battle;
            Play(run, "frag.ellipse"); Play(run, "frag.expand");
            int before = b.PreviewDamage(b.Enemies[0]);
            Assert.That(run.TryUseCombatSkill(0), Is.True);
            Assert.That(b.Enemies[0].Statuses.Get(CombatStatusKind.Rupture), Is.EqualTo(6));
            Assert.That(b.PreviewDamage(b.Enemies[0]), Is.EqualTo(before + 6));
            b.TryBeginPlot(); b.ResolvePlot();
            Assert.That(b.Enemies[0].IsAlive, Is.False);
            Assert.That(b.Enemies[0].Statuses.Get(CombatStatusKind.Rupture), Is.Zero);
        }
    }
}

using Graphaclysm.Application;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Runs;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class GrowthUxTests
    {
        [Test]
        public void EveryCurrentNodeHasUiMappingAndSearchableName()
        {
            Assert.That(GrowthUiCatalog.Count, Is.EqualTo(240));
            foreach (var n in GrowthCatalog.All)
            {
                var e = GrowthUiCatalog.Get(n.Id);
                Assert.That(e.Title, Is.Not.Empty); Assert.That(e.Summary, Is.Not.Empty); Assert.That(e.Symbol, Is.Not.Empty);
                Assert.That(GrowthUiCatalog.Matches(n, n.Name), Is.True);
                Assert.That(GrowthCurrentTitle.For(n), Is.Not.Empty);
            }
        }
        [Test]
        public void SearchUsesEffectAndCooldownAliases()
        {
            var g = new RunGrowthState(CombatArchetype.Ian); var n = g.GetNode(g.IndexOf("ian.execute.form"));
            Assert.That(GrowthUiCatalog.Matches(n, "처치"), Is.True);
            Assert.That(GrowthUiCatalog.Matches(n, "쿨타임"), Is.True);
            Assert.That(GrowthUiCatalog.Matches(n, "존재하지않는말"), Is.False);
            Assert.That(GrowthCurrentTitle.For(g.GetNode(g.IndexOf("ian.u.immortal.form"))), Does.Contain("이동 봉쇄"));
        }
        private static LegacyProgression Funded()
        {
            var p = new LegacyProgression(); for (uint i = 1; i <= 5; i++) p.AwardRun(i, true, 3); return p;
        }
        [Test]
        public void RankCartSumsIndividualPricesAndAppliesOnlyAfterPersist()
        {
            var p = Funded(); int[] target = { 3, 0, 2, 0, 0, 0 }; int[] expected = new int[6];
            Assert.That(p.QuoteRanks(target), Is.EqualTo(16));
            Assert.That(p.TryPurchaseRanks(target, expected, 40, candidate => {
                Assert.That(p.Currency, Is.EqualTo(40)); Assert.That(p.GetRank(0), Is.Zero);
                Assert.That(candidate.Currency, Is.EqualTo(24)); return true;
            }), Is.True);
            Assert.That(p.Currency, Is.EqualTo(24)); Assert.That(p.Benefits.MaxHealth, Is.EqualTo(6));
            Assert.That(p.Benefits.StartingResonance, Is.EqualTo(2));
            Assert.That(p.TryPurchaseRanks(target, expected, 40, _ => true), Is.False);
            Assert.That(p.AwardRun(1, true, 3), Is.Zero);
        }
        [Test]
        public void FailedPersistenceDoesNotChargeOrGrantRanks()
        {
            var p = Funded(); int[] target = { 3, 0, 2, 0, 0, 0 };
            Assert.That(p.TryPurchaseRanks(target, new int[6], 40, _ => false), Is.False);
            Assert.That(p.Currency, Is.EqualTo(40)); Assert.That(p.GetRank(0), Is.Zero);
            Assert.That(p.TryPurchaseRanks(target, new int[6], 40, _ => throw new System.IO.IOException()), Is.False);
            Assert.That(p.Currency, Is.EqualTo(40));
        }
        [Test]
        public void InsufficientCartDoesNotPartiallyBuy()
        {
            var p = new LegacyProgression(); p.AwardRun(1, true, 3); bool saved = false;
            Assert.That(p.TryPurchaseRanks(new[] {3,0,2,0,0,0}, new int[6], 8, _ => {saved = true; return true;}), Is.False);
            Assert.That(saved, Is.False); Assert.That(p.Currency, Is.EqualTo(8)); Assert.That(p.GetRank(0), Is.Zero);
        }
        [Test]
        public void ExistingRanksOnlyChargeMissingStepsAndDoNotRefund()
        {
            var p = Funded(); p.TryPurchase(0);
            Assert.That(p.QuoteRanks(new[] {3,0,0,0,0,0}), Is.EqualTo(7));
            Assert.That(p.QuoteRanks(new int[6]), Is.EqualTo(-1));
            Assert.That(p.QuoteRanks(new[] {4,0,0,0,0,0}), Is.EqualTo(-1));
        }
        [Test]
        public void SharedRouteAncestorsCountOnceAndPlanningDoesNotMutate()
        {
            var g = new RunGrowthState(CombatArchetype.Ian);
            var planner = new GrowthAcquisitionPlanner(g);
            var plans = planner.Plan(g.IndexOf("common.origin.01"));
            Assert.That(plans.Count, Is.EqualTo(1)); Assert.That(plans[0].Cost, Is.EqualTo(2));
            Assert.That(plans[0].NodeIndices.Count, Is.EqualTo(2)); Assert.That(plans[0].Remaining, Is.EqualTo(4));
            Assert.That(plans[0].UnsupportedCount, Is.EqualTo(2));
            Assert.That(g.Points, Is.EqualTo(6)); Assert.That(g.AcquiredCount, Is.Zero);
        }
        [Test]
        public void ExistingAncestorExcludedAndEnumerationLimitIsExplicit()
        {
            var g = new RunGrowthState(CombatArchetype.Ian); g.TryPurchase(g.IndexOf(GrowthTreePaths.RootId));
            var plans = new GrowthAcquisitionPlanner(g).Plan(g.IndexOf("common.origin.01"));
            Assert.That(plans[0].Cost, Is.EqualTo(1)); Assert.That(plans[0].NodeIndices.Count, Is.EqualTo(1));
            var limited = new GrowthAcquisitionPlanner(g, 1);
            Assert.That(limited.Plan(g.IndexOf("ian.triple.awaken")).Count, Is.Zero); Assert.That(limited.NeedsChoices, Is.True);
        }
        [Test]
        public void InvestmentGateIsNotFilledWithArbitraryNodes()
        {
            var g = new RunGrowthState(CombatArchetype.Ian);
            // Execution sits above the initial tier; triple is intentionally available in tier 1.
            var plans = new GrowthAcquisitionPlanner(g).Plan(g.IndexOf("ian.execute.form"));
            Assert.That(plans.Count, Is.GreaterThan(0));
            Assert.That(plans[0].Blocker, Is.Not.Empty); Assert.That(g.AcquiredCount, Is.Zero);
        }
        [Test]
        public void LegacyCartPersistsCompatibleSaveAndProtectsUnreadableSource()
        {
            string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GraphaclysmGrowthUx-" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(directory);
            try
            {
                var store = new LegacyProgressionStore(directory); var p = Funded();
                Assert.That(p.TryPurchaseRanks(new[] {3,0,2,0,0,0}, new int[6], 40, store.TrySave), Is.True);
                var restored = store.Load();
                Assert.That(restored.IsReadOnly, Is.False); Assert.That(restored.Currency, Is.EqualTo(24));
                Assert.That(restored.GetRank(0), Is.EqualTo(3)); Assert.That(restored.AwardRun(1,true,3), Is.Zero);
                byte[] corrupt = { 1, 2, 3 };
                string path = System.IO.Path.Combine(directory, "legacy.save"); System.IO.File.WriteAllBytes(path, corrupt);
                var broken = store.Load(); Assert.That(broken.IsReadOnly, Is.True); Assert.That(store.TrySave(broken), Is.False);
                Assert.That(System.IO.File.ReadAllBytes(path), Is.EqualTo(corrupt));
            }
            finally { System.IO.Directory.Delete(directory, true); }
        }
    }
}

using Graphaclysm.Application;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Runs;
using NUnit.Framework;
namespace Graphaclysm.Tests.Combat
{
    public sealed class GrowthLimitTests
    {
        private static RunGrowthState Growth()=>new RunGrowthState(CombatArchetype.Ian,CombatApproach.Execution,true,true);
        [Test] public void AllAwardsIncludingHugeBonusStopAtNine()
        {
            var g=Growth();Assert.That(g.Points,Is.EqualTo(6));g.AddPoints(2);Assert.That(g.Points,Is.EqualTo(8));
            g.AddPoints(int.MaxValue);Assert.That(g.TotalPointBudget,Is.EqualTo(9));Assert.That(g.AwardedPoints,Is.EqualTo(3));
            g.AddPoints(int.MaxValue);Assert.That(g.Points,Is.EqualTo(9));
        }
        [Test] public void RefundAndResetNeverReopenTheAwardAllowance()
        {
            var g=Growth();g.AddPoints(100);
            for(int repeat=0;repeat<8;repeat++)
            {
                foreach(int i in new[]{1,2,3,4,5,6,7})Assert.That(g.TryPurchase(i),Is.True);
                Assert.That(g.SpentPoints,Is.EqualTo(9));Assert.That(g.Points,Is.Zero);Assert.That(g.CanPurchase(11),Is.False);
                var plans=new GrowthAcquisitionPlanner(g).Plan(11);
                Assert.That(plans.Count,Is.EqualTo(1));Assert.That(plans[0].Cost,Is.EqualTo(1));Assert.That(plans[0].Remaining,Is.EqualTo(-1));
                Assert.That(g.TryRefund(3),Is.True);g.AddPoints(100);Assert.That(g.Points,Is.EqualTo(2));
                Assert.That(g.TryReset(),Is.True);g.AddPoints(100);Assert.That(g.Points,Is.EqualTo(9));Assert.That(g.IsUnlocked(0),Is.True);
            }
        }
        [TestCase(CombatApproach.Execution)] [TestCase(CombatApproach.Recording)]
        [TestCase(CombatApproach.Tuning)] [TestCase(CombatApproach.Observation)]
        public void NewBudgetSurvivesSaveWhilePreviousRunRemainsUncapped(CombatApproach approach)
        {
            var f=new PrototypeGameFlow(PrototypeCharacterCatalog.All,51);f.OpenCharacterSelection();f.TrySelectCharacter(approach>=CombatApproach.Tuning?1:0);
            Assert.That(f.TryStartRun(approach, false),Is.True);var r=f.CurrentRun;Assert.That(r.Growth.HasPointLimit,Is.True);
            Assert.That(r.TryPurchaseGrowthNode(1),Is.True);
            Assert.That(RunSaveStore.TryDecode(RunSaveStore.Encode(r.CaptureSave()),out var data),Is.True);
            Assert.That(RunGameSession.TryRestore(data,out var restored,out _),Is.True);Assert.That(restored.Growth.HasPointLimit,Is.True);
            Assert.That(restored.Growth.Points,Is.EqualTo(r.Growth.Points));Assert.That(restored.Growth.IsUnlocked(1),Is.True);
            var old=r.CaptureSave();System.Array.Resize(ref old.Commands,12);
            Assert.That(RunGameSession.TryRestore(old,out var legacy,out _),Is.True);Assert.That(legacy.Growth.IsStyleTree,Is.True);
            Assert.That(legacy.Growth.HasPointLimit,Is.False);legacy.Growth.AddPoints(100);Assert.That(legacy.Growth.Points,Is.EqualTo(106));
        }
    }
}

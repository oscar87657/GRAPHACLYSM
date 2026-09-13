using System.Collections.Generic;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Runs;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class GrowthTreePathsTests
    {
        [TestCase(CombatArchetype.Ian)]
        [TestCase(CombatArchetype.Luna)]
        public void EveryNodeIsConnectedToTheSinglePlayableRootWithoutCycles(CombatArchetype archetype)
        {
            var growth = new RunGrowthState(archetype);
            var visiting = new HashSet<string>(); var visited = new HashSet<string>();
            int available = 0;
            for (int i = 0; i < growth.NodeCount; i++)
            {
                if (growth.CanPurchase(i)) available++;
                Visit(growth, growth.GetNode(i).Id, visiting, visited);
            }
            Assert.That(available, Is.EqualTo(1), "Only the bottom root is initially purchasable.");
            Assert.That(visited.Count, Is.EqualTo(144));
            Assert.That(growth.CanPurchase(growth.IndexOf(GrowthTreePaths.RootId)), Is.True);
        }

        private static void Visit(RunGrowthState growth, string id, HashSet<string> visiting, HashSet<string> visited)
        {
            if (visited.Contains(id)) return;
            Assert.That(visiting.Add(id), Is.True, "Cycle at " + id);
            int index = growth.IndexOf(id);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), "A route cannot require the other character: " + id);
            var node = growth.GetNode(index);
            int parentCount = node.RequiresAll.Length + node.RequiresAtLeast.Length + GrowthTreePaths.Entrances(id).Length;
            if (id != GrowthTreePaths.RootId) Assert.That(parentCount, Is.GreaterThan(0), "Disconnected node: " + id);
            foreach (string parent in node.RequiresAll) Visit(growth, parent, visiting, visited);
            foreach (string parent in node.RequiresAtLeast) Visit(growth, parent, visiting, visited);
            foreach (string parent in GrowthTreePaths.Entrances(id)) Visit(growth, parent, visiting, visited);
            visiting.Remove(id); visited.Add(id);
        }

        [Test]
        public void SkillCanBeEnteredFromARuleOrFromAnIdentityAndItsTraitStillRequiresTheSkill()
        {
            var ruleRoute = new RunGrowthState(CombatArchetype.Luna);
            ruleRoute.AddPoints(10);
            Buy(ruleRoute, GrowthTreePaths.RootId, "common.fragments.01", "common.fragments.02");
            Invest(ruleRoute,5);
            Assert.That(ruleRoute.CanPurchase(ruleRoute.IndexOf("luna.rotate.form")), Is.True);
            Assert.That(ruleRoute.CanPurchase(ruleRoute.IndexOf("luna.rotate.m1")), Is.False);
            Buy(ruleRoute, "luna.rotate.form", "luna.rotate.m1");

            var identityRoute = new RunGrowthState(CombatArchetype.Luna);
            identityRoute.AddPoints(10);
            Buy(identityRoute, GrowthTreePaths.RootId, "common.origin.01", "luna.orbit.core", "luna.rotate.form");
            Assert.That(identityRoute.IsEquipped("luna.rotate.form"), Is.True);
        }

        [Test]
        public void MajorTurnChangingUltimateCannotBeBoughtDirectlyAndResetClosesItsRoutes()
        {
            var growth = new RunGrowthState(CombatArchetype.Luna);
            growth.AddPoints(29);
            int ultimate = growth.IndexOf("luna.u.timeless.form");
            Assert.That(growth.CanPurchase(ultimate), Is.False);
            Buy(growth, GrowthTreePaths.RootId, "common.fragments.01", "common.condense.01",
                "common.condense.02", "common.condense.03", "common.condense.07", "luna.u.timeless.form");
            Assert.That(growth.IsEquipped(ultimate), Is.True);
            Assert.That(growth.SpentPoints, Is.LessThanOrEqualTo(35));
            Assert.That(growth.TryReset(), Is.True);
            Assert.That(growth.CanPurchase(ultimate), Is.False);
            Assert.That(growth.Points, Is.EqualTo(35));
        }

        private static void Buy(RunGrowthState growth, params string[] ids)
        {
            foreach (string id in ids)
            {
                Invest(growth,GrowthTreePaths.RequiredInvestment(id));
                if (!growth.IsUnlocked(id)) Assert.That(growth.TryPurchase(growth.IndexOf(id)), Is.True, id);
            }
        }
        private static void Invest(RunGrowthState growth,int target)
        {
            while(growth.SpentPoints<target)
            {
                bool changed=false;
                for(int i=0;i<growth.NodeCount;i++)
                    if(growth.GetNode(i).Owner==GrowthOwner.Common && growth.GetNode(i).Cost==1 && growth.CanPurchase(i))
                    { growth.TryPurchase(i); changed=true; break; }
                Assert.That(changed,Is.True,"Investment route must remain available");
            }
        }
    }
}

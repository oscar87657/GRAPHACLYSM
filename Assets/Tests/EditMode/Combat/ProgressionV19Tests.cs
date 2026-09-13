using System.Collections.Generic;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Runs;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class ProgressionV19Tests
    {
        [Test]
        public void IndividualRefundReturnsOnlyItsCostAndPreservesOtherNodes()
        {
            var growth = new RunGrowthState(CombatArchetype.Ian);
            int root = growth.IndexOf(GrowthTreePaths.RootId), leaf = growth.IndexOf("common.origin.01");
            Assert.That(growth.TryPurchase(root), Is.True);
            Assert.That(growth.TryPurchase(leaf), Is.True);
            Assert.That(growth.CanRefund(root), Is.False);
            Assert.That(growth.IsUnlocked(root), Is.True, "Queries must not change state");
            Assert.That(growth.TryRefund(leaf), Is.True);
            Assert.That(growth.Points, Is.EqualTo(5));
            Assert.That(growth.IsUnlocked(root), Is.True);
            Assert.That(growth.TryRefund(leaf), Is.False);
            Assert.That(growth.TryRefund(-1), Is.False);
            Assert.That(growth.TryRefund(root), Is.True);
            Assert.That(growth.Points, Is.EqualTo(6));
        }

        [Test]
        public void RefundUnequipsAnIdentityAndAllowsBuyingItAgain()
        {
            var growth = new RunGrowthState(CombatArchetype.Luna);
            foreach (string id in new[] { GrowthTreePaths.RootId, "common.origin.01", "luna.orbit.core" })
                Assert.That(growth.TryPurchase(growth.IndexOf(id)), Is.True);
            int core = growth.IndexOf("luna.orbit.core");
            Assert.That(growth.IsEquipped(core), Is.True);
            Assert.That(growth.TryRefund(core), Is.True);
            Assert.That(growth.IsEquipped(core), Is.False);
            Assert.That(growth.TryPurchase(core), Is.True);
            Assert.That(growth.IsEquipped(core), Is.True);
        }

        [Test]
        public void RefundCannotBypassUpperTierInvestment()
        {
            var growth = new RunGrowthState(CombatArchetype.Luna);
            foreach (string id in new[] { GrowthTreePaths.RootId, "common.origin.01", "luna.orbit.core",
                "common.geometry.02", "common.fragments.01", "luna.fulljump.form" })
                Assert.That(growth.TryPurchase(growth.IndexOf(id)), Is.True, id);
            Assert.That(growth.CanRefund(growth.IndexOf("common.fragments.01")), Is.False);
            Assert.That(growth.TryRefund(growth.IndexOf("luna.fulljump.form")), Is.True);
            Assert.That(growth.TryRefund(growth.IndexOf("common.fragments.01")), Is.True);
        }

        private static RunGameSession RoomRun(RoomStory story, RunNodeKind kind)
        {
            var c=PrototypeCharacterCatalog.All[0];
            var b=new BattleDefinition(42,4,new[]{new EnemyDefinition("enemy","Enemy",8,0,100,3)},CombatArchetype.Ian,fragments:true);
            return new RunGameSession(new RunMapDefinition(new[]{
                new RunMapNodeDefinition("room","Room",0,0,kind,null,new[]{1},story,42),
                new RunMapNodeDefinition("boss","Boss",1,0,RunNodeKind.Boss,b,new int[0])
            }),c.CreateStartingDeckCopy(),FragmentCardCatalog.All,5,73,Graphaclysm.Core.Relics.FragmentRelicCatalog.All);
        }

        [Test]
        public void EveryNonBossRowHasDifferentRoomTypesAndAllSourcesHaveChoices()
        {
            var allKinds=new HashSet<RunNodeKind>();
            for(uint seed=1;seed<=40;seed++)
            {
                var map=DungeonGenerator.Generate(seed,PrototypeCharacterCatalog.All[0]);
                for(int layer=0;layer<24;layer++)
                {
                    var kinds=new HashSet<RunNodeKind>();
                    for(int i=0;i<map.NodeCount;i++)
                    {
                        var node=map.GetNode(i); if(node.Layer!=layer)continue;
                        kinds.Add(node.Kind); allKinds.Add(node.Kind);
                        if(layer<23 && layer%8!=6) Assert.That(node.NextNodeCount,Is.GreaterThanOrEqualTo(2));
                    }
                    if(layer%8!=7) Assert.That(kinds.Count,Is.GreaterThanOrEqualTo(2));
                }
            }
            Assert.That(allKinds.Count,Is.EqualTo(9));
            Assert.That(RoomStoryCatalog.EventCount,Is.EqualTo(10));
        }

        [Test]
        public void WorkshopTrainingIsPersistentAndCannotBeClaimedTwice()
        {
            var run=RoomRun(RoomStoryCatalog.Workshop,RunNodeKind.Workshop);
            run.TrySelectMapNode(0);
            Assert.That(run.TryChooseRoomOption(0),Is.True);
            Assert.That(run.TrainingPower,Is.EqualTo(1));
            Assert.That(run.PlayerHealth,Is.EqualTo(38));
            Assert.That(run.TryChooseRoomOption(0),Is.False);
            run.TrySelectMapNode(1);
            Assert.That(run.CurrentBattle.Battle.PlotDamageBonus,Is.EqualTo(1));
        }

        [Test]
        public void SuppliesAreConsumedOnlyWhenTheNextBattleStarts()
        {
            var run=RoomRun(RoomStoryCatalog.Supply,RunNodeKind.Supply);
            run.TrySelectMapNode(0); run.TryChooseRoomOption(0);
            Assert.That(run.PreparedShield,Is.EqualTo(8));
            Assert.That(run.Phase,Is.EqualTo(RunPhase.MapSelection));
            run.TrySelectMapNode(1);
            Assert.That(run.PreparedShield,Is.Zero);
            Assert.That(run.CurrentBattle.Battle.Tactics.Statuses.Get(CombatStatusKind.Shield),Is.EqualTo(8));
        }

        [Test]
        public void FiveInvestedPointsGiveRealBattleBonusesAndRespecRemovesThem()
        {
            var run=RoomRun(RoomStoryCatalog.Supply,RunNodeKind.Supply);
            foreach(string id in new[]{GrowthTreePaths.RootId,"common.geometry.02","common.origin.01","common.fragments.01","common.self.01"})
                Assert.That(run.TryPurchaseGrowthNode(run.Growth.IndexOf(id)),Is.True,id);
            Assert.That(run.MasteryRank,Is.EqualTo(1));
            Assert.That(run.TryResetGrowth(),Is.True);
            Assert.That(run.MasteryRank,Is.Zero);
            foreach(string id in new[]{GrowthTreePaths.RootId,"common.geometry.02","common.origin.01","common.fragments.01","common.self.01"})
                Assert.That(run.TryPurchaseGrowthNode(run.Growth.IndexOf(id)),Is.True,id);
            run.TrySelectMapNode(0); run.TryLeaveRoom(); run.TrySelectMapNode(1);
            Assert.That(run.CurrentBattle.Battle.PlotDamageBonus,Is.EqualTo(1));
            Assert.That(run.CurrentBattle.Battle.Tactics.Statuses.Get(CombatStatusKind.Shield),Is.EqualTo(1));
        }

        [Test]
        public void ObservatoryGivesExtraPointsInAdditionToRoomCompletion()
        {
            var run=RoomRun(RoomStoryCatalog.Observatory,RunNodeKind.Observatory);
            run.TrySelectMapNode(0); run.TryChooseRoomOption(0);
            Assert.That(run.Growth.Points,Is.EqualTo(9));
            Assert.That(run.PlayerHealth,Is.EqualTo(37));
        }

        [Test]
        public void DamageFeedbackSeparatesAbsorptionFromHealthLoss()
        {
            var b=new BattleDefinition(42,4,new[]{new EnemyDefinition("enemy","Enemy",8,0,100,3)},CombatArchetype.Ian,fragments:true);
            var battle=new BattleSession(b,42,0,0,startingShieldBonus:5);
            battle.TryUnravel(); battle.ResolveEnemyTurn();
            Assert.That(battle.IncomingEventSerial,Is.EqualTo(1));
            Assert.That(battle.LastPlayerShieldDamage,Is.EqualTo(3));
            Assert.That(battle.LastPlayerHealthDamage,Is.Zero);
            battle.TryUnravel(); battle.ResolveEnemyTurn();
            Assert.That(battle.IncomingEventSerial,Is.EqualTo(2));
            Assert.That(battle.LastPlayerShieldDamage,Is.Zero);
            Assert.That(battle.LastPlayerHealthDamage,Is.EqualTo(3));
            battle.Reset(); Assert.That(battle.IncomingEventSerial,Is.Zero);
        }

        [Test]
        public void AllNodesHaveReadableSummariesAndHighTiersRequireInvestment()
        {
            var growth=new RunGrowthState(CombatArchetype.Luna);
            growth.AddPoints(100);
            var tiers=new HashSet<int>();
            foreach(var node in GrowthCatalog.All)
            {
                string text=GrowthPlayerText.Summary(node);
                Assert.That(text,Is.Not.Null.And.Not.Empty,node.Id);
                Assert.That(text.Length,Is.LessThanOrEqualTo(200),node.Id);
                tiers.Add(GrowthTreePaths.Tier(node.Id));
            }
            Assert.That(tiers.Count,Is.EqualTo(5));
            Assert.That(GrowthTreePaths.RequiredInvestment("luna.u.timeless.form"),Is.GreaterThanOrEqualTo(10));
            Assert.That(growth.CanPurchase(growth.IndexOf("luna.u.timeless.form")),Is.False);
        }
    }
}

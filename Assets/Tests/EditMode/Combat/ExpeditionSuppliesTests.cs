using System;
using System.Reflection;
using System.Security.Cryptography;
using Graphaclysm.Application;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Relics;
using Graphaclysm.Core.Runs;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class ExpeditionSuppliesTests
    {
        private static RunGameSession Start(uint seed=302,CombatApproach approach=CombatApproach.Tuning,int experience=0)
        {
            var flow=new PrototypeGameFlow(PrototypeCharacterCatalog.All,seed);
            flow.LegacyBenefits=new LegacyBenefits(0,0,0,experience,0,0);
            flow.OpenCharacterSelection(); flow.TrySelectCharacter(approach>=CombatApproach.Tuning?1:0);
            Assert.That(flow.TryStartRun(approach,true,true),Is.True); // Historical rule37 starter regression.
            return flow.CurrentRun;
        }
        private static RunGameSession Restore(RunGameSession run)
        {
            Assert.That(RunSaveStore.TryDecode(RunSaveStore.Encode(run.CaptureSave()),out var data),Is.True);
            Assert.That(RunGameSession.TryRestore(data,out var r,out _),Is.True);
            Assert.That(r.Phase,Is.EqualTo(run.Phase)); Assert.That(r.Growth.Points,Is.EqualTo(run.Growth.Points));
            Assert.That(r.GrowthCombatCredit,Is.EqualTo(run.GrowthCombatCredit)); Assert.That(r.Keys,Is.EqualTo(run.Keys));
            Assert.That(r.Coins,Is.EqualTo(run.Coins)); Assert.That(r.ResearchTickets,Is.EqualTo(run.ResearchTickets));
            Assert.That(r.Deck.Count,Is.EqualTo(run.Deck.Count)); Assert.That(r.Relics.Count,Is.EqualTo(run.Relics.Count));
            Assert.That(r.IsStartingRelic,Is.EqualTo(run.IsStartingRelic)); Assert.That(r.PendingLootCount,Is.EqualTo(run.PendingLootCount));
            for(int i=0;i<run.LootCount;i++)
            { Assert.That(r.GetLoot(i).Title,Is.EqualTo(run.GetLoot(i).Title)); Assert.That(r.GetLoot(i).Resolved,Is.EqualTo(run.GetLoot(i).Resolved)); Assert.That(r.GetLoot(i).Result,Is.EqualTo(run.GetLoot(i).Result)); }
            return r;
        }
        private static void Win(RunGameSession run)
        {
            for(int turn=0;turn<26 && run.Phase==RunPhase.Battle;turn++)
            {
                EnemyState target=null;
                foreach(var enemy in run.CurrentBattle.Battle.Enemies) if(enemy.IsAlive) {target=enemy;break;}
                run.TryMovePlayerTo(Math.Max(.5,target.X-3),target.Y);
                foreach(string id in new[]{"frag.ellipse","frag.expand"})
                    for(int i=0;i<run.CurrentBattle.Deck.HandCount;i++)
                        if(run.CurrentBattle.Deck.GetHandCard(i).Id==id) {run.TryPlayHandCard(i,out _,out _);break;}
                run.TryUseDiagramAbility(Math.Max(.5,target.X-3),target.Y);
                Assert.That(run.TryBeginPlot(),Is.True); run.ResolvePlot();
                if(run.Phase==RunPhase.Battle)run.ResolveEnemyTurn();
            }
            Assert.That(run.Phase,Is.EqualTo(RunPhase.Loot));
        }
        private static RunGameSession Victory()
        { var run=Start();Assert.That(run.TrySelectRelicReward(0),Is.True);run.TrySelectMapNode(0);Win(run);return Restore(run); }
        private static void Invoke(RunGameSession r,string name,params object[] args)
        { typeof(RunGameSession).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(r,args); }

        [TestCase(CombatApproach.Execution)] [TestCase(CombatApproach.Recording)]
        [TestCase(CombatApproach.Tuning)] [TestCase(CombatApproach.Observation)]
        public void ThreePointStartIncludesFreeRelicAndOneKey(CombatApproach approach)
        {
            var r=Restore(Start(302,approach)); Assert.That(r.Growth.InitialPoints,Is.EqualTo(3)); Assert.That(r.Growth.Points,Is.EqualTo(3));
            Assert.That(r.Keys,Is.EqualTo(1)); Assert.That(r.Relics.Count,Is.Zero); Assert.That(r.TrySelectMapNode(0),Is.False);
            for(int i=0;i<3;i++)Assert.That(r.RelicRewardOptions[i],Is.Not.Null);
            Assert.That(r.TrySelectRelicReward(1),Is.True); Assert.That(r.Relics.Count,Is.EqualTo(1));
            Assert.That(r.TrySelectRelicReward(1),Is.False); Assert.That(r.TryEnableExpeditionSupplies(),Is.False);
            r=Restore(r); Assert.That(r.TryPurchaseGrowthNode(1),Is.True); Assert.That(r.TryPurchaseGrowthNode(2),Is.True);
            Assert.That(r.TryPurchaseGrowthNode(3),Is.False); // 1+1+2 cannot fit in the initial three points.
            Restore(r);
        }
        [Test] public void StartingExperienceIsCreditNotExtraStartingPointsAndPlanningUsesThree()
        {
            var r=Restore(Start(experience:3)); Assert.That(r.Growth.Points,Is.EqualTo(3)); Assert.That(r.GrowthCombatCredit,Is.EqualTo(3));
            r.TrySkipReward(); var plans=new GrowthAcquisitionPlanner(r.Growth).Plan(3);
            Assert.That(plans[0].Cost,Is.EqualTo(4)); Assert.That(plans[0].Remaining,Is.EqualTo(-1));
            Invoke(r,"AwardEarnedGrowth",RunNodeKind.Battle); Assert.That(r.Growth.Points,Is.EqualTo(3));
            Invoke(r,"AwardEarnedGrowth",RunNodeKind.Battle); Assert.That(r.Growth.Points,Is.EqualTo(4));
            r.Growth.AddPoints(int.MaxValue); Assert.That(r.Growth.AwardedPoints,Is.EqualTo(6)); Assert.That(r.Growth.Points,Is.EqualTo(9));
        }
        [Test] public void RoomsNoLongerGiveFreePointsAndFiveFloorProgressEarnsSix()
        {
            var r=Start();r.TrySkipReward();
            // Progression-only fixture: no fabricated battle commands are claimed as replayable.
            int bossPoints=0,combatPoints=0;
            for(int depth=0;depth<40;depth++)
            {
                int selected=-1;
                for(int i=0;i<r.Map.Definition.NodeCount;i++) if(r.Map.IsAvailable(i)) { if(selected<0)selected=i; if(r.Map.Definition.GetNode(i).Kind==RunNodeKind.Battle){selected=i;break;} }
                var node=r.Map.Definition.GetNode(selected);
                Assert.That(r.Map.TryEnterNode(selected),Is.True);Assert.That(r.Map.TryCompleteActiveNode(),Is.True);
                Invoke(r,"AwardExploration",node.Kind);
                if(node.Kind==RunNodeKind.Boss)bossPoints+=r.LastExplorationPoints;
                else if(node.Battle!=null)combatPoints+=r.LastExplorationPoints;
                else Assert.That(r.LastExplorationPoints,Is.Zero);
                if(depth==0)Assert.That(r.Growth.Points,Is.EqualTo(3));
                if(depth==7)Assert.That(r.Growth.Points,Is.InRange(4,5));
            }
            Assert.That(bossPoints,Is.EqualTo(4)); Assert.That(combatPoints,Is.EqualTo(2)); Assert.That(r.Growth.Points,Is.EqualTo(9));
            Assert.That(r.Growth.TryPurchase(1),Is.True); Assert.That(r.Growth.TryReset(),Is.True);r.Growth.AddPoints(99);
            Assert.That(r.Growth.Points,Is.EqualTo(9));
        }
        [Test] public void RealVictoryLootIsOptionalAndSelectionReplayCannotDuplicateRewards()
        {
            var r=Victory();Assert.That(r.Coins,Is.Zero);Assert.That(r.ResearchTickets,Is.Zero);Assert.That(r.Growth.Points,Is.EqualTo(3));
            int coins=r.GetLoot(0).Amount;Assert.That(r.TryClaimLoot(0),Is.True);r=Restore(r);
            Assert.That(r.Coins,Is.EqualTo(coins));Assert.That(r.TryClaimLoot(0),Is.False);Assert.That(r.TryClaimLoot(-1),Is.False);
            Assert.That(r.TryClaimLoot(2),Is.True);r=Restore(r);int count=r.Deck.Count;
            Assert.That(r.TrySelectReward(0),Is.True);r=Restore(r);Assert.That(r.Deck.Count,Is.EqualTo(count+1));
            Assert.That(r.GetLoot(2).Result,Is.EqualTo("획득 완료"));
            Assert.That(r.TryClaimLoot(2),Is.False);Assert.That(r.TryLeaveLoot(),Is.True);r=Restore(r);
            Assert.That(r.ResearchTickets,Is.Zero);Assert.That(r.TryLeaveLoot(),Is.False);Assert.That(r.TryClaimLoot(1),Is.False);
        }
        [Test] public void SkippingCardDraftReturnsToLootAndResearchIsCollectedOnlyOnRequest()
        {
            var r=Victory();int cards=r.Deck.Count;r.TryClaimLoot(2);r.TrySkipReward();r=Restore(r);
            Assert.That(r.Phase,Is.EqualTo(RunPhase.Loot));Assert.That(r.Deck.Count,Is.EqualTo(cards));
            Assert.That(r.GetLoot(2).Result,Is.EqualTo("받지 않고 넘김"));
            r.TryClaimLoot(1);r.TryLeaveLoot();r=Restore(r);Assert.That(r.ResearchTickets,Is.EqualTo(1));
            Assert.That(r.TryUseResearch(),Is.True);r=Restore(r);r.TrySkipReward();r=Restore(r);
            Assert.That(r.Phase,Is.EqualTo(RunPhase.MapSelection));Assert.That(r.ResearchTickets,Is.Zero);
        }
        [Test] public void ChestKeyCostsOneAndFailedAttemptsCannotRepeat()
        {
            int success=0,fail=0;
            for(uint seed=1;seed<=40;seed++)
            {
                var r=Start(seed);r.TrySkipReward();Invoke(r,"PrepareVaultSupplies");
                Assert.That(r.TryClaimLoot(0),Is.False);Assert.That(r.TryOpenLootChest(-1,true),Is.False);
                Assert.That(r.TryOpenLootChest(0,false),Is.True);Assert.That(r.Keys,Is.EqualTo(1));
                if(r.Phase==RunPhase.RelicReward){success++;r.TrySkipReward();}else fail++;
                Assert.That(r.TryOpenLootChest(0,true),Is.False);Assert.That(r.TryOpenLootChest(0,false),Is.False);
            }
            Assert.That(success,Is.InRange(8,32));Assert.That(fail,Is.InRange(8,32));
            var keyed=Start();keyed.TrySkipReward();Invoke(keyed,"PrepareVaultSupplies");
            Assert.That(keyed.TryOpenLootChest(0,true),Is.True);Assert.That(keyed.Keys,Is.Zero);Assert.That(keyed.Phase,Is.EqualTo(RunPhase.RelicReward));
            keyed.TrySelectRelicReward(0);Assert.That(keyed.Phase,Is.EqualTo(RunPhase.Loot));Assert.That(keyed.Relics.Count,Is.EqualTo(1));
            Invoke(keyed,"PrepareVaultSupplies");Assert.That(keyed.TryOpenLootChest(0,true),Is.False);
            foreach(var relic in FragmentRelicCatalog.All)keyed.Relics.TryAdd(relic);
            Assert.That(keyed.TryOpenLootChest(0,false),Is.False);Assert.That(keyed.TryLeaveLoot(),Is.True);
        }
        private static bool LeadsToTreasure(RunMapDefinition map,int index)
        {
            var n=map.GetNode(index);if(n.Layer==4)return n.Kind==RunNodeKind.Treasure;
            if(n.Layer>4)return false;
            for(int i=0;i<n.NextNodeCount;i++)if(LeadsToTreasure(map,n.GetNextNodeIndex(i)))return true;
            return false;
        }
        [TestCase(true)] [TestCase(false)]
        public void ActualTreasureRoomAndChestChoiceSurviveReplay(bool useKey)
        {
            var r=Start();r.TrySelectRelicReward(0);
            for(int depth=0;depth<=4;depth++)
            {
                int next=-1;
                for(int i=0;i<r.Map.Definition.NodeCount;i++)if(r.Map.IsAvailable(i) && LeadsToTreasure(r.Map.Definition,i)){next=i;break;}
                Assert.That(next,Is.GreaterThanOrEqualTo(0)); Assert.That(r.TrySelectMapNode(next),Is.True);
                if(depth==4)break;
                if(r.Phase==RunPhase.Battle){Win(r);r.TryLeaveLoot();}
                else if(r.CurrentRoom.GetChoice(0).Effect==RoomEffect.Training)r.TryChooseRoomOption(0);
                else r.TryLeaveRoom();
                r=Restore(r);
            }
            Assert.That(r.CurrentRoom.Title,Is.EqualTo("봉인된 유물 상자"));
            Assert.That(r.TryChooseRoomOption(0),Is.True);r=Restore(r);Assert.That(r.GetLoot(0).Kind,Is.EqualTo(LootKind.Chest));
            int keys=r.Keys;Assert.That(r.TryOpenLootChest(0,useKey),Is.True);r=Restore(r);
            Assert.That(r.Keys,Is.EqualTo(keys-(useKey?1:0)));
            if(r.Phase==RunPhase.RelicReward){Assert.That(r.TrySelectRelicReward(0),Is.True);r=Restore(r);}
            Assert.That(r.TryOpenLootChest(0,true),Is.False);Assert.That(r.TryOpenLootChest(0,false),Is.False);
            Assert.That(r.TryLeaveLoot(),Is.True);r=Restore(r);Assert.That(r.Phase,Is.EqualTo(RunPhase.MapSelection));
        }
        [Test] public void FinalBossLootRemainsOptionalBeforeCompletion()
        {
            var r=Start();r.TrySkipReward();
            for(int depth=0;depth<40;depth++)
                for(int i=0;i<r.Map.Definition.NodeCount;i++)if(r.Map.IsAvailable(i)){r.Map.TryEnterNode(i);r.Map.TryCompleteActiveNode();break;}
            Invoke(r,"PrepareCombatSupplies",RunNodeKind.Boss);
            Assert.That(r.Phase,Is.EqualTo(RunPhase.Loot));Assert.That(r.Coins,Is.Zero);
            Assert.That(r.TryLeaveLoot(),Is.True);Assert.That(r.Phase,Is.EqualTo(RunPhase.Completed));
        }
        [Test] public void Version36KeepsSixPointStartAndRejectsNewCommandsInOldHeader()
        {
            var r=Start();var data=r.CaptureSave();Array.Resize(ref data.Commands,13);
            Assert.That(RunGameSession.TryRestore(data,out var old,out _),Is.True);Assert.That(old.HasExpeditionSupplies,Is.False);
            Assert.That(old.Growth.Points,Is.EqualTo(6));Assert.That(old.Phase,Is.EqualTo(RunPhase.MapSelection));
            byte[] bytes=RunSaveStore.Encode(r.CaptureSave());Array.Copy(BitConverter.GetBytes(36),0,bytes,8,4);
            using(var hash=SHA256.Create())Array.Copy(hash.ComputeHash(bytes,0,bytes.Length-32),0,bytes,bytes.Length-32,32);
            Assert.That(RunSaveStore.TryDecode(bytes,out _),Is.False);
        }
    }
}

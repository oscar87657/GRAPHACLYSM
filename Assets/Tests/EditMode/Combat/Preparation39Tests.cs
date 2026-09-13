using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Decks;
using Graphaclysm.Core.Runs;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class Preparation39Tests
    {
        private static RunGameSession Start(int preparation=0,uint seed=302)
        {
            var flow=new PrototypeGameFlow(PrototypeCharacterCatalog.All,seed){PreparationUpgrades=preparation};
            flow.OpenCharacterSelection();flow.TrySelectCharacter(1);
            Assert.That(flow.TryStartRun(CombatApproach.Tuning),Is.True);return flow.CurrentRun;
        }
        private static RunGameSession Restore(RunGameSession run)
        {
            Assert.That(RunSaveStore.TryDecode(RunSaveStore.Encode(run.CaptureSave()),out var data),Is.True);
            Assert.That(RunGameSession.TryRestore(data,out var r,out _),Is.True);
            Assert.That(r.PreparationSnapshot,Is.EqualTo(run.PreparationSnapshot));Assert.That(r.Squad,Is.EqualTo(run.Squad));
            Assert.That(r.Coins,Is.EqualTo(run.Coins));Assert.That(r.Phase,Is.EqualTo(run.Phase));
            Assert.That(r.ActiveDraftGrade,Is.EqualTo(run.ActiveDraftGrade));
            for(int i=0;i<3;i++)Assert.That(r.RewardOptions[i]?.Id,Is.EqualTo(run.RewardOptions[i]?.Id));
            return r;
        }
        private static void Invoke(RunGameSession r,string name,params object[] args)
        {typeof(RunGameSession).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(r,args);}
        private static void Win(RunGameSession r)
        {
            for(int turn=0;turn<26 && r.Phase==RunPhase.Battle;turn++)
            {
                EnemyState target=null;foreach(var e in r.CurrentBattle.Battle.Enemies)if(e.IsAlive){target=e;break;}
                r.TryMovePlayerTo(Math.Max(.5,target.X-3),target.Y);
                foreach(string id in new[]{"frag.ellipse","frag.expand"})for(int i=0;i<r.CurrentBattle.Deck.HandCount;i++)
                    if(r.CurrentBattle.Deck.GetHandCard(i).Id==id){r.TryPlayHandCard(i,out _,out _);break;}
                r.TryUseDiagramAbility(Math.Max(.5,target.X-3),target.Y);
                Assert.That(r.TryBeginPlot(),Is.True);r.ResolvePlot();if(r.Phase==RunPhase.Battle)r.ResolveEnemyTurn();
            }
            Assert.That(r.Phase,Is.EqualTo(RunPhase.Loot));
        }
        [Test] public void ResearchIsReplacedByTwoImmediateDraftsWithStableReplay()
        {
            var r=Start();r.TrySelectMapNode(0);Win(r);r=Restore(r);
            Assert.That(r.ResearchTickets,Is.Zero);Assert.That(r.SupplyInventoryText,Does.Not.Contain("연구권"));
            Assert.That(r.GetLoot(1).Kind,Is.EqualTo(LootKind.Cards));Assert.That(r.GetLoot(2).Kind,Is.EqualTo(LootKind.Cards));
            int deck=r.Deck.Count;Assert.That(r.TryClaimLoot(1),Is.True);r=Restore(r);
            Assert.That(r.Phase,Is.EqualTo(RunPhase.CardReward));Assert.That(r.TrySelectReward(0),Is.True);r=Restore(r);
            Assert.That(r.Deck.Count,Is.EqualTo(deck+1));Assert.That(r.TryClaimLoot(1),Is.False);
            Assert.That(r.TryClaimLoot(2),Is.True);r=Restore(r);Assert.That(r.TrySkipReward(),Is.True);r=Restore(r);
            Assert.That(r.Deck.Count,Is.EqualTo(deck+1));Assert.That(r.ActiveDraftGrade,Is.EqualTo(-1));
            r.TryLeaveLoot();Assert.That(r.TryUseResearch(),Is.False);Restore(r);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void DraftColoursHaveDistinctMeasuredOddsAndNoDuplicateOptions(int grade)
        {
            var random=new XorShiftRandom(392);var options=new CardDefinition[3];var counts=new int[4];
            for(int n=0;n<6000;n++)for(int i=0;i<3;i++)
            {
                options[i]=FragmentDraft.Draw(random,FragmentCardCatalog.All,grade,options,i);
                counts[(int)options[i].Rarity]++;
                for(int j=0;j<i;j++)Assert.That(options[i].Id,Is.Not.EqualTo(options[j].Id));
            }
            for(int t=0;t<4;t++)Assert.That(counts[t]/180.0,Is.EqualTo(FragmentDraft.Weight(grade,t)).Within(1.7));
        }
        [Test] public void BetterDraftsAreOneStepAndBossesHaveNoGreyPacks()
        {
            var a=new XorShiftRandom(19);var b=new XorShiftRandom(19);
            int[] kinds=new int[4];
            for(int i=0;i<2000;i++)
            {
                int normal=FragmentDraft.RollGrade(a,0,false);kinds[normal]++;
                Assert.That(FragmentDraft.RollGrade(b,0,true),Is.EqualTo(Math.Min(3,normal+1)));
                Assert.That(FragmentDraft.RollGrade(new XorShiftRandom((uint)i+1),2,false),Is.InRange(1,3));
            }
            foreach(int count in kinds)Assert.That(count,Is.GreaterThan(0));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void SquadAndPermanentSnapshotSurviveSaveAndReachBattle(int squad)
        {
            var r=Restore(Start(2|(2<<2)|32|64|(squad<<7)));
            Assert.That(r.Keys,Is.EqualTo(2));Assert.That(r.Growth.Points,Is.EqualTo(3));
            Assert.That(r.Coins,Is.EqualTo(8+(squad==3?12:0)));Assert.That(r.BetterDrafts,Is.True);
            Assert.That(r.PermanentPlotPower,Is.EqualTo(2+(squad==1?2:0)));
            Assert.That(r.TryEnablePreparation(0),Is.False);Assert.That(r.TrySelectMapNode(0),Is.True);Restore(r);
            var baseline=Start();baseline.TrySelectMapNode(0);
            // The constructor-installed bonuses affect the actual battle, not just the displayed snapshot.
            Assert.That(r.CurrentBattle.Battle.PlotDamageBonus-baseline.CurrentBattle.Battle.PlotDamageBonus,Is.EqualTo(r.PermanentPlotPower));
        }
        [Test] public void OnlyPurchasedRelicUnlockOpensAStartingChoice()
        {
            var r=Restore(Start(16));Assert.That(r.IsStartingRelic,Is.True);Assert.That(r.Relics.Count,Is.Zero);
            Assert.That(r.TrySelectMapNode(0),Is.False);r.TrySelectRelicReward(1);r=Restore(r);
            Assert.That(r.Relics.Count,Is.EqualTo(1));Assert.That(r.TrySelectRelicReward(1),Is.False);
            var skip=Start(16);skip.TrySkipReward();skip=Restore(skip);Assert.That(skip.Relics.Count,Is.Zero);
        }
        [Test] public void ShopDraftPaysOnceThenReturnsToTheSameShop()
        {
            var r=Start(8);
            // Progression-only fixture. Do not claim this artificial traversal as journal replay.
            int shop=-1;for(int i=0;i<r.Map.Definition.NodeCount;i++)if(r.Map.Definition.GetNode(i).Kind==RunNodeKind.Shop){shop=i;break;}
            while(!r.Map.IsAvailable(shop))
            {
                int next=-1;for(int i=0;i<r.Map.Definition.NodeCount;i++)if(r.Map.IsAvailable(i)){next=i;break;}
                Assert.That(next,Is.GreaterThanOrEqualTo(0));r.Map.TryEnterNode(next);r.Map.TryCompleteActiveNode();
            }
            Assert.That(r.TrySelectMapNode(shop),Is.True);
            typeof(RunGameSession).GetField("<Coins>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(r,100);
            var offer=r.GetShopOffer(12);Assert.That(offer.Kind,Is.EqualTo(ShopItemKind.Draft));
            int deck=r.Deck.Count;Assert.That(r.TryBuyShop(12),Is.True);Assert.That(r.Coins,Is.EqualTo(100-offer.Price));
            Assert.That(r.ActiveDraftGrade,Is.EqualTo(offer.DraftGrade));r.TrySelectReward(0);
            Assert.That(r.IsShop,Is.True);Assert.That(r.Deck.Count,Is.EqualTo(deck+1));Assert.That(r.ShopSold(12),Is.True);
            Assert.That(r.TryBuyShop(12),Is.False);Assert.That(r.ResearchTickets,Is.Zero);
        }
        [Test] public void ActualShopPurchaseAndSkipReplayWithoutRerollOrDoubleCharge()
        {
            var r=Start(2|8|128);
            for(int depth=0;depth<5;depth++)
            {
                int next=-1;
                for(int i=0;i<r.Map.Definition.NodeCount;i++)if(r.Map.IsAvailable(i))
                {
                    var node=r.Map.Definition.GetNode(i);
                    if(next<0)next=i;
                    if(node.Kind==RunNodeKind.Shop){next=i;break;}
                    if(node.Battle==null)next=i;
                }
                Assert.That(r.TrySelectMapNode(next),Is.True);
                if(r.IsShop)break;
                if(r.Phase==RunPhase.Battle){Win(r);r.TryClaimLoot(0);r.TryLeaveLoot();}
                else Assert.That(r.TryLeaveRoom(),Is.True);
            }
            Assert.That(r.IsShop,Is.True);r=Restore(r);int before=r.Coins;int price=r.GetShopOffer(12).Price;
            Assert.That(r.TryBuyShop(12),Is.True);r=Restore(r);Assert.That(r.Coins,Is.EqualTo(before-price));
            Assert.That(r.TrySkipReward(),Is.True);r=Restore(r);Assert.That(r.IsShop,Is.True);
            Assert.That(r.ShopSold(12),Is.True);Assert.That(r.Coins,Is.EqualTo(before-price));Assert.That(r.TryBuyShop(12),Is.False);
        }
        [Test] public void EarlierHeaderCannotAcceptPreparationCommand()
        {
            byte[] bytes=RunSaveStore.Encode(Start().CaptureSave());Array.Copy(BitConverter.GetBytes(38),0,bytes,8,4);
            using(var hash=SHA256.Create())Array.Copy(hash.ComputeHash(bytes,0,bytes.Length-32),0,bytes,bytes.Length-32,32);
            Assert.That(RunSaveStore.TryDecode(bytes,out _),Is.False);
            var flow=new PrototypeGameFlow(PrototypeCharacterCatalog.All,39);flow.OpenCharacterSelection();flow.TrySelectCharacter(1);
            flow.TryStartRun(CombatApproach.Tuning,true,false,false);var r=flow.CurrentRun;int count=r.Revision;
            foreach(int bad in new[]{-1,3,12,512})Assert.That(r.TryEnablePreparation(bad),Is.False);
            Assert.That(r.Revision,Is.EqualTo(count));Assert.That(r.HasRewardPacks,Is.False);
            Assert.That(r.TryEnablePreparation(0),Is.True);Assert.That(r.TryEnablePreparation(0),Is.False);
        }
        [Test] public void AccountPrerequisitesCartAndUnlockedSquadsAreReal()
        {
            var p=new LegacyProgression();for(uint i=1;i<20;i++)p.AwardRun(i,true,5);
            Assert.That(p.IsSquadUnlocked(ExpeditionSquad.Assault),Is.False);Assert.That(p.TryPurchase(11),Is.False);
            var ranks=new int[LegacyProgression.NodeCount];ranks[6]=1;ranks[11]=1;ranks[8]=1;
            int money=p.Currency;Assert.That(p.TryPurchaseRanks(ranks,new int[ranks.Length],money,_=>false),Is.False);
            Assert.That(p.Currency,Is.EqualTo(money));Assert.That(p.TryPurchaseRanks(ranks,new int[ranks.Length],money,_=>true),Is.True);
            Assert.That(p.IsSquadUnlocked(ExpeditionSquad.Assault),Is.True);
            Assert.That(p.Preparation(ExpeditionSquad.Assault),Is.EqualTo(1|16|128));
            Assert.That(p.Preparation(ExpeditionSquad.Shelter),Is.EqualTo(1|16));
        }
        [Test] public void VersionOneAccountIsPreservedAndNewIdsRoundTrip()
        {
            string directory=Path.Combine(Path.GetTempPath(),"Graphaclysm39-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            try
            {
                using(var stream=new MemoryStream())
                {
                    using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))
                    {writer.Write(0x4C474359);writer.Write(1);writer.Write(70);writer.Write(6);foreach(int rank in new[]{2,1,1,2,1,1})writer.Write(rank);writer.Write(1);writer.Write((uint)39);}
                    byte[] payload=stream.ToArray();using(var sha=SHA256.Create()){byte[] hash=sha.ComputeHash(payload);stream.Write(hash,0,hash.Length);}
                    File.WriteAllBytes(Path.Combine(directory,"legacy.save"),stream.ToArray());
                }
                var store=new LegacyProgressionStore(directory);var p=store.Load();Assert.That(p.IsReadOnly,Is.False);
                Assert.That(p.Currency,Is.EqualTo(70));Assert.That(p.GetRank(0),Is.EqualTo(2));Assert.That(p.GetRank(8),Is.Zero);
                Assert.That(p.AwardRun(39,true,5),Is.Zero);p.TryPurchase(8);Assert.That(store.TrySave(p),Is.True);
                var restored=store.Load();Assert.That(restored.GetRank(8),Is.EqualTo(1));Assert.That(restored.Currency,Is.EqualTo(66));
                Assert.That(restored.Benefits.MaxHealth,Is.EqualTo(4));Assert.That(restored.Benefits.VictoryHealing,Is.EqualTo(1));
            }
            finally{Directory.Delete(directory,true);}
        }
    }
}

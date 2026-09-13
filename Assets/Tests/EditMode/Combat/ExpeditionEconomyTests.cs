using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Graphaclysm.Application;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Decks;
using Graphaclysm.Core.Runs;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class ExpeditionEconomyTests
    {
        private static RunGameSession Start(uint seed = 302, bool market = false)
        {
            var flow = new PrototypeGameFlow(PrototypeCharacterCatalog.All, seed);
            flow.OpenCharacterSelection(); flow.TrySelectCharacter(1);
            Assert.That(flow.TryStartRun(CombatApproach.Tuning, false), Is.True);
            if (market) return flow.CurrentRun;
            var historical = PrototypeRunFactory.Create(seed, PrototypeCharacterCatalog.All[1]);
            historical.TryChooseApproach(CombatApproach.Tuning); historical.TryEnableOpeningRoute();
            historical.TryEnableEconomy(); historical.TryEnableContentExpansion(); historical.TryEnableGrandArchive();
            return historical;
        }
        private static RunGameSession Restore(RunGameSession run)
        {
            Assert.That(RunSaveStore.TryDecode(RunSaveStore.Encode(run.CaptureSave()), out var data), Is.True);
            Assert.That(RunGameSession.TryRestore(data, out var restored, out _), Is.True);
            Assert.That(restored.Phase, Is.EqualTo(run.Phase));
            Assert.That(restored.Coins, Is.EqualTo(run.Coins));
            Assert.That(restored.ResearchTickets, Is.EqualTo(run.ResearchTickets));
            Assert.That(restored.Deck.Count, Is.EqualTo(run.Deck.Count));
            Assert.That(restored.Relics.Count, Is.EqualTo(run.Relics.Count));
            Assert.That(restored.Growth.Points, Is.EqualTo(run.Growth.Points));
            Assert.That(restored.TrainingPower, Is.EqualTo(run.TrainingPower));
            if (run.IsShop)
                for (int i = 0; i < run.ShopOfferCount; i++)
                {
                    Assert.That(restored.GetShopOffer(i).Title, Is.EqualTo(run.GetShopOffer(i).Title));
                    Assert.That(restored.ShopSold(i), Is.EqualTo(run.ShopSold(i)));
                }
            return restored;
        }
        private static void Win(RunGameSession run)
        {
            for (int turn = 0; turn < 24 && run.Phase == RunPhase.Battle; turn++)
            {
                EnemyState target = null;
                foreach (var enemy in run.CurrentBattle.Battle.Enemies) if (enemy.IsAlive) { target = enemy; break; }
                run.TryMovePlayerTo(Math.Max(.5, target.X - 3), target.Y);
                foreach (string id in new[] { "frag.ellipse", "frag.expand" })
                    for (int i = 0; i < run.CurrentBattle.Deck.HandCount; i++)
                        if (run.CurrentBattle.Deck.GetHandCard(i).Id == id) { run.TryPlayHandCard(i, out _, out _); break; }
                run.TryUseDiagramAbility(Math.Max(.5, target.X - 3), target.Y);
                Assert.That(run.TryBeginPlot(), Is.True); run.ResolvePlot();
                if (run.Phase == RunPhase.Battle) run.ResolveEnemyTurn();
            }
            Assert.That(run.Phase, Is.EqualTo(RunPhase.CardReward));
        }
        private static RunGameSession AtShop(uint seed = 302, bool market = false)
        {
            var run = Start(seed, market);
            while (run.Map.CompletedNodeCount < 4 || run.Phase != RunPhase.MapSelection)
            {
                if (run.Phase == RunPhase.MapSelection)
                {
                    int target = -1;
                    for (int i = 0; i < run.Map.Definition.NodeCount; i++)
                        if (run.Map.IsAvailable(i))
                        { if (target < 0) target = i; if (run.Map.Definition.GetNode(i).Kind == RunNodeKind.Workshop) { target = i; break; }
                            if (run.Map.Definition.GetNode(i).Kind == RunNodeKind.Battle) target = i; }
                    run.TrySelectMapNode(target);
                }
                else if (run.Phase == RunPhase.Battle) Win(run);
                else if (run.Phase == RunPhase.CardReward || run.Phase == RunPhase.RelicReward) run.TrySkipReward();
                else if (run.Phase == RunPhase.Room) Assert.That(run.TryChooseRoomOption(0), Is.True);
                else Assert.Fail("Unexpected " + run.Phase);
            }
            for (int i = 0; i < run.Map.Definition.NodeCount; i++)
                if (run.Map.IsAvailable(i) && run.Map.Definition.GetNode(i).Kind == RunNodeKind.Shop)
                { run.TrySelectMapNode(i); break; }
            Assert.That(run.IsShop, Is.True);
            return run;
        }
        [Test]
        public void MapsHaveReachableShopsEveryFloorAndVariedEnemyAndRoomContent()
        {
            var names = new HashSet<string>(); var behaviors = new HashSet<EnemyBehaviorKind>();
            for (uint seed = 1; seed <= 40; seed++)
            {
                var run = Start(seed); var map = run.Map.Definition; int shops = 0;
                for (int i = 0; i < map.NodeCount; i++)
                {
                    var node = map.GetNode(i); names.Add(node.DisplayName);
                    if (node.Kind == RunNodeKind.Shop) shops++;
                    if (node.Layer % 8 == 3)
                        Assert.That(map.GetNode(node.GetNextNodeIndex(0)).Kind, Is.EqualTo(RunNodeKind.Shop));
                    if (node.Battle != null)
                        for (int e = 0; e < node.Battle.EnemyCount; e++) behaviors.Add(node.Battle.GetEnemy(e).Behavior.Kind);
                    if (node.Layer > 0)
                    {
                        bool incoming = false;
                        for (int p = 0; p < i; p++)
                            for (int e = 0; e < map.GetNode(p).NextNodeCount; e++) incoming |= map.GetNode(p).GetNextNodeIndex(e) == i;
                        Assert.That(incoming, Is.True, node.Id);
                    }
                }
                Assert.That(shops, Is.EqualTo(3));
            }
            Assert.That(names.Count, Is.GreaterThan(25)); Assert.That(behaviors.Count, Is.GreaterThanOrEqualTo(7));
        }
        [Test]
        public void VictoryGivesCoinsTicketsAndResearchIsSeparateFromCombatAndSingleUse()
        {
            var run = Start(); Assert.That(run.Coins, Is.Zero); Assert.That(run.TryUseResearch(), Is.False);
            run.TrySelectMapNode(0); Win(run);
            Assert.That(run.Coins, Is.InRange(10,13)); Assert.That(run.ResearchTickets, Is.EqualTo(1));
            int coins = run.Coins; Assert.That(run.TryUseResearch(), Is.False);
            run = Restore(run); run.TrySkipReward();
            Assert.That(run.TryUseResearch(), Is.True); Assert.That(run.IsResearchReward, Is.True);
            run = Restore(run); Assert.That(run.TryUseResearch(), Is.False);
            int cards = run.Deck.Count;
            Assert.That(run.TrySelectReward(0), Is.True);
            Assert.That(run.Deck.Count, Is.EqualTo(cards+1)); Assert.That(run.Coins, Is.EqualTo(coins));
            Assert.That(run.ResearchTickets, Is.Zero); Assert.That(run.TryUseResearch(), Is.False);
            Restore(run);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(5)]
        public void PurchasesDeductExactlyOnceAndRestoreInventory(int item)
        {
            var run = AtShop(); int coins = run.Coins, price = run.GetShopOffer(item).Price;
            Assert.That(run.TryBuyShop(-1), Is.False); Assert.That(run.TryBuyShop(6), Is.False);
            Assert.That(run.TryChooseRoomOption(0), Is.False);
            Assert.That(run.TryBuyShop(item), Is.True, run.ShopBlocker(item));
            Assert.That(run.Coins, Is.EqualTo(coins-price)); Assert.That(run.ShopSold(item), Is.True);
            Assert.That(run.TryBuyShop(item), Is.False); run = Restore(run);
            Assert.That(run.TryBuyShop(item), Is.False);
            Assert.That(run.TryLeaveRoom(), Is.True); Assert.That(run.TryLeaveRoom(), Is.False);
            Assert.That(run.TryBuyShop(item), Is.False); Restore(run);
        }
        [Test]
        public void CardRemovalOnlyChargesOnConfirmationAndCancelReturnsToSameShop()
        {
            var run = AtShop();
            Assert.That(run.TryBuyShop(4), Is.False); // six-card minimum
            Assert.That(run.TryBuyShop(0), Is.True);
            int coins = run.Coins, count = run.Deck.Count;
            Assert.That(run.TryBuyShop(4), Is.True); Assert.That(run.Coins, Is.EqualTo(coins));
            run = Restore(run); Assert.That(run.TrySkipRefinement(), Is.True);
            Assert.That(run.IsShop, Is.True); Assert.That(run.ShopSold(4), Is.False); Assert.That(run.Coins, Is.EqualTo(coins));
            Assert.That(run.TryBuyShop(4), Is.True); run = Restore(run);
            Assert.That(run.TryRemoveDeckCard(0), Is.True); Assert.That(run.IsShop, Is.True);
            Assert.That(run.Coins, Is.EqualTo(coins-10)); Assert.That(run.Deck.Count, Is.EqualTo(count-1));
            Assert.That(run.ShopSold(4), Is.True); Restore(run);
        }
        [Test]
        public void NormalBattleBonusRelicAppearsAndItsRewardChainReplays()
        {
            uint seed = 1;
            for (; seed < 100; seed++)
            {
                var rng = new XorShiftRandom(seed ^ 0xA14C67D3u);
                rng.Next(4); rng.Next(4);
                if (rng.Next(100) < 25) break;
            }
            var run = AtShop(seed);
            int rewards = 0;
            foreach (var command in run.CaptureSave().Commands) if (command.Kind == RunCommandKind.SkipReward) rewards++;
            Assert.That(rewards, Is.GreaterThanOrEqualTo(4),"Three base card rewards plus at least one bonus relic");
            Restore(run);
            // Restore every intermediate reward boundary, including between card and bonus relic.
            var save = run.CaptureSave();
            for (int i=0;i<save.Commands.Length;i++)
                if (save.Commands[i].Kind==RunCommandKind.SkipReward)
                {
                    var prefix = new RunCommand[i]; Array.Copy(save.Commands,prefix,i);
                    Assert.That(RunGameSession.TryRestore(new RunSaveData { Seed=save.Seed,CharacterId=save.CharacterId,
                        LegacyBenefits=save.LegacyBenefits,Commands=prefix },out var before,out _),Is.True);
                    Assert.That(before.TrySkipReward(),Is.True); Restore(before);
                }
        }
        [Test]
        public void UnsupportedLegacyCombatFormsAreNotAdvertisedAsWorkingInNewApproaches()
        {
            var run=Start(); var growth=run.Growth;
            Assert.That(GrowthRuntimeSupport.HasEffect(growth.GetNode(growth.IndexOf("luna.fulljump.form")),CombatApproach.Tuning,true),Is.False);
            Assert.That(GrowthRuntimeSupport.HasEffect(growth.GetNode(growth.IndexOf("luna.rotate.m2")),CombatApproach.Tuning,true),Is.True);
            Assert.That(GrowthRuntimeSupport.HasEffect(growth.GetNode(growth.IndexOf("luna.u.timeless.form")),CombatApproach.Tuning,true),Is.False);
            Assert.That(GrowthRuntimeSupport.HasEffect(growth.GetNode(growth.IndexOf("luna.binary.core")),CombatApproach.Tuning,false),Is.False);
            Assert.That(GrowthRuntimeSupport.HasEffect(growth.GetNode(growth.IndexOf("luna.binary.core")),CombatApproach.Tuning,true),Is.True);
        }
        [Test]
        public void InsufficientMoneyDoesNotConsumeStockOrJournalCommand()
        {
            var run=AtShop(); Assert.That(run.TryBuyShop(1),Is.True);
            int coins=run.Coins, revision=run.Revision;
            Assert.That(coins,Is.LessThan(16)); Assert.That(run.TryBuyShop(3),Is.False);
            Assert.That(run.ShopSold(3),Is.False); Assert.That(run.Coins,Is.EqualTo(coins));
            Assert.That(run.Revision,Is.EqualTo(revision)); Restore(run);
        }
        [Test]
        public void LunaCanActuallySwitchToObservationByEquippingCoreAndReplayIt()
        {
            var run=PrototypeRunFactory.Create(302,PrototypeCharacterCatalog.All[1],new LegacyBenefits(0,0,0,20,0,0));
            run.TryChooseApproach(CombatApproach.Tuning); run.TryEnableOpeningRoute(); run.TryEnableEconomy();
            var g=run.Growth; int target=g.IndexOf("luna.binary.core");
            var plans=new GrowthAcquisitionPlanner(g).Plan(target);
            Assert.That(plans.Count,Is.GreaterThan(0));
            for(int step=0;step<30 && !g.IsUnlocked(target);step++)
            {
                int candidate=-1;
                foreach(int index in plans[0].NodeIndices) if(g.CanPurchase(index)) { candidate=index;break; }
                if(candidate<0)
                    for(int i=0;i<g.NodeCount;i++)
                        if(g.GetNode(i).Owner==GrowthOwner.Common && string.IsNullOrEmpty(g.GetNode(i).ExclusiveGroup) && g.CanPurchase(i)) {candidate=i;break;}
                if(candidate<0)break;
                Assert.That(run.TryPurchaseGrowthNode(candidate),Is.True);
            }
            Assert.That(g.IsUnlocked(target),Is.True);
            if(!g.IsEquipped(target)) Assert.That(run.TrySelectGrowthNode(target),Is.True);
            Assert.That(run.EffectiveApproach,Is.EqualTo(CombatApproach.Observation));
            run=Restore(run); Assert.That(run.EffectiveApproach,Is.EqualTo(CombatApproach.Observation));
            run.TrySelectMapNode(0); Assert.That(run.CurrentBattle.Battle.Approach,Is.EqualTo(CombatApproach.Observation));
            Assert.That(run.TryPlaceSatellite(6,-2),Is.True); Restore(run);
        }
        [Test]
        public void Rules25RetainOldMapWithoutEconomyAndRejectEconomyCommands()
        {
            var old = PrototypeRunFactory.Create(302, PrototypeCharacterCatalog.All[1]);
            old.TryChooseApproach(CombatApproach.Tuning); old.TryEnableOpeningRoute(); old.TrySelectMapNode(0);
            var bytes = Rules25(old);
            Assert.That(RunSaveStore.TryDecode(bytes, out var data), Is.True);
            Assert.That(RunGameSession.TryRestore(data, out var restored, out _), Is.True);
            Assert.That(restored.HasEconomy, Is.False); Assert.That(restored.Coins, Is.Zero);
            Assert.That(restored.TryEnableEconomy(), Is.False);
            Assert.That(RunSaveStore.TryDecode(Rules25(Start()), out _), Is.False);
            Assert.That(SignatureBattleFactory.Create(CombatApproach.Tuning).TryEnableEconomy(), Is.False);
        }
        private static byte[] Rules25(RunGameSession run)
        {
            var bytes = RunSaveStore.Encode(run.CaptureSave()); Array.Copy(BitConverter.GetBytes(25),0,bytes,8,4);
            using (var hash = SHA256.Create()) Array.Copy(hash.ComputeHash(bytes,0,bytes.Length-32),0,bytes,bytes.Length-32,32);
            return bytes;
        }
        [Test]
        public void ExpandedCardAndRelicCanActuallyBeBoughtAndReplayed()
        {
            bool cardFound=false,relicFound=false;
            var oldCards=new HashSet<string>(); var oldRelics=new HashSet<string>();
            foreach(var c in Graphaclysm.Core.Cards.FragmentCardCatalog.Version27) oldCards.Add(c.Id);
            foreach(var r in Graphaclysm.Core.Relics.FragmentRelicCatalog.Legacy) oldRelics.Add(r.Id);
            for(uint seed=1;seed<=60 && (!cardFound || !relicFound);seed++)
            {
                var run=AtShop(seed); Assert.That(run.HasContentExpansion,Is.True);
                var card=run.GetShopOffer(0).Card; var relic=run.GetShopOffer(1).Relic;
                if(!cardFound && !oldCards.Contains(card.Id))
                {
                    Assert.That(run.TryBuyShop(0),Is.True); var restored=Restore(run);
                    Assert.That(restored.Deck[restored.Deck.Count-1].Id,Is.EqualTo(card.Id)); cardFound=true;
                }
                else if(!relicFound && !oldRelics.Contains(relic.Id))
                {
                    Assert.That(run.TryBuyShop(1),Is.True); var restored=Restore(run);
                    Assert.That(restored.Relics.Contains(relic.Id),Is.True); relicFound=true;
                }
            }
            Assert.That(cardFound && relicFound,Is.True,"Expanded items must occur in actual shop stock, not just the codex.");
        }
        [Test] public void MarketHasFiveDistinctCardsThreeDistinctRelicsAndFiveServices()
        {
            for(uint seed=301;seed<311;seed++)
            {
                var run=AtShop(seed,true); Assert.That(run.HasMarketBalance,Is.True); Assert.That(run.ShopOfferCount,Is.EqualTo(13));
                var cards=new HashSet<string>();var relics=new HashSet<string>();int services=0;
                for(int i=0;i<run.ShopOfferCount;i++)
                {
                    var offer=run.GetShopOffer(i);
                    if(offer.Card!=null)
                    {
                        Assert.That(cards.Add(offer.Card.Id),Is.True);
                        Assert.That(offer.Price,Is.EqualTo(Graphaclysm.Core.Cards.CardMarketBalance.Price(offer.Card.Rarity)));
                    }
                    else if(offer.Relic!=null) { Assert.That(relics.Add(offer.Relic.Id),Is.True);Assert.That(run.Relics.Contains(offer.Relic.Id),Is.False); }
                    else services++;
                }
                Assert.That(cards.Count,Is.EqualTo(5));Assert.That(relics.Count,Is.EqualTo(3));Assert.That(services,Is.EqualTo(5));
                Restore(run);
            }
        }
        [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)] [TestCase(10)] [TestCase(11)] [TestCase(12)]
        public void NewShelfSlotsPurchaseExactlyOnceAndReplay(int slot)
        {
            var run=AtShop(302,true);int price=run.GetShopOffer(slot).Price,coins=run.Coins,tickets=run.ResearchTickets;
            Assert.That(run.TryBuyShop(13),Is.False);Assert.That(run.TryBuyShop(slot),Is.True,run.ShopBlocker(slot));
            Assert.That(run.Coins,Is.EqualTo(coins-price)); Assert.That(run.TryBuyShop(slot),Is.False);
            if(slot==12) Assert.That(run.ResearchTickets,Is.EqualTo(tickets+1));
            run=Restore(run);Assert.That(run.ShopSold(slot),Is.True);Assert.That(run.TryBuyShop(slot),Is.False);
        }
    }
}

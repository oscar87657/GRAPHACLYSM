using System;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Decks;
using Graphaclysm.Core.Relics;
using Graphaclysm.Core.Runs;

namespace Graphaclysm.Application
{
    public sealed partial class RunGameSession
    {
        public bool HasMarketBalance { get; private set; }
        public bool HasStatusRules { get; private set; }
        public bool HasTowerArchive { get; private set; }
        public bool TryEnableTowerArchive()
        {
            if(IsPractice || !HasStatusRules || HasTowerArchive || journal==null || journal.Count!=7
                || journal[6].Kind!=RunCommandKind.StatusRules || Phase!=RunPhase.MapSelection
                || Map.ActiveNodeIndex>=0 || Map.CompletedNodeCount!=0) return false;
            relicPool=new RelicDefinition[FragmentRelicCatalog.All.Count];
            for(int i=0;i<relicPool.Length;i++) relicPool[i]=FragmentRelicCatalog.All[i];
            Map=new RunMapProgress(TowerArchiveContent.Create(Map.Definition,Seed));
            HasTowerArchive=true;
            return Record(true,RunCommandKind.TowerArchive);
        }
        public bool TryEnableStatusRules()
        {
            if (IsPractice || !HasMarketBalance || HasStatusRules || journal == null || journal.Count != 6
                || journal[5].Kind != RunCommandKind.MarketBalance || Phase != RunPhase.MapSelection
                || Map.ActiveNodeIndex >= 0 || Map.CompletedNodeCount != 0) return false;
            rewardPool = new CardDefinition[FragmentCardCatalog.All.Count];
            for(int i=0;i<rewardPool.Length;i++) rewardPool[i]=FragmentCardCatalog.All[i];
            var starting=new CardDefinition[Deck.Count];
            for(int i=0;i<starting.Length;i++) starting[i]=FragmentCardCatalog.Current(Deck[i].Id);
            Deck=new RunDeck(starting); HasStatusRules=true;
            return Record(true,RunCommandKind.StatusRules);
        }
        public bool TryEnableMarketBalance()
        {
            if (IsPractice || !HasGrandArchive || HasMarketBalance || journal == null || journal.Count != 5
                || journal[4].Kind != RunCommandKind.GrandArchive || Phase != RunPhase.MapSelection
                || Map.ActiveNodeIndex >= 0 || Map.CompletedNodeCount != 0) return false;
            rewardPool = new CardDefinition[FragmentCardCatalog.Version29.Count];
            for (int i = 0; i < rewardPool.Length; i++) rewardPool[i] = FragmentCardCatalog.Version29[i];
            var starting = new CardDefinition[Deck.Count];
            for (int i = 0; i < starting.Length; i++) starting[i] = FragmentCardCatalog.MarketCard(Deck[i].Id);
            Deck = new RunDeck(starting); HasMarketBalance = true;
            return Record(true, RunCommandKind.MarketBalance);
        }

        private void PrepareExpandedShop(int nodeIndex)
        {
            var random = new XorShiftRandom(Seed ^ ((uint)(nodeIndex + 1) * 0x45D9F3Bu));
            Array.Clear(shopSold, 0, shopSold.Length);
            shopOffers = new ShopOffer[13];
            var cards = new CardDefinition[5];
            for (int i = 0; i < cards.Length; i++)
            {
                var card = CardMarketBalance.Draw(random, rewardPool, true, cards, i); cards[i] = card;
                int slot = i == 0 ? 0 : i + 5;
                shopOffers[slot] = new ShopOffer(ShopItemKind.Card, CardMarketBalance.Price(card.Rarity), card.DisplayName, card.Description, card: card);
            }
            for (int i = 0; i < 3; i++)
            {
                int available = 0;
                foreach (var relic in relicPool) if (ShopRelicAvailable(relic)) available++;
                RelicDefinition selected = null;
                if (available > 0)
                {
                    int roll = random.Next(available);
                    foreach (var relic in relicPool)
                        if (ShopRelicAvailable(relic) && roll-- == 0) { selected = relic; break; }
                }
                int slot = i == 0 ? 1 : i + 9;
                shopOffers[slot] = new ShopOffer(ShopItemKind.Relic, 24, selected?.DisplayName ?? "유물 품절",
                    selected?.Description ?? "미보유 유물이 없습니다", relic: selected);
            }
            shopOffers[2] = new ShopOffer(ShopItemKind.Heal, 8, "회복약", "최대 체력의 30% 회복");
            shopOffers[3] = new ShopOffer(ShopItemKind.Training, 16, "작도 도구 연마", "원정 그래프 피해 +1 · 최대 +3");
            shopOffers[4] = new ShopOffer(ShopItemKind.RemoveCard, 10, "기록 삭제", "카드 1장 제거 · 확정 때 결제");
            shopOffers[5] = new ShopOffer(ShopItemKind.Growth, HasExpeditionSupplies?24:12, "성좌의 조각", HasExpeditionSupplies?"원정 성장점 +1 · 총한도 내에서 먼저 확보":"원정 성장점 +1");
            if(HasRewardPacks)
            {
                int grade=FragmentDraft.RollGrade(random,1,false);
                shopOffers[12]=new ShopOffer(ShopItemKind.Draft,FragmentDraft.Price(grade),FragmentDraft.Name(grade),FragmentDraft.Description(grade),draftGrade:grade);
            }
            else shopOffers[12] = new ShopOffer(ShopItemKind.Research, 8, "파편 연구권", "연구권 +1 · 지도에서 카드 3장 중 선택");
        }
        private bool ShopRelicAvailable(RelicDefinition relic)
        {
            if (Relics.Contains(relic.Id)) return false;
            foreach (var offer in shopOffers) if (offer?.Relic?.Id == relic.Id) return false;
            return true;
        }
    }
}

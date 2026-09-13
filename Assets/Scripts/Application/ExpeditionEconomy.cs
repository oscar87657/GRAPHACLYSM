using System;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Decks;
using Graphaclysm.Core.Relics;
using Graphaclysm.Core.Runs;

namespace Graphaclysm.Application
{
    public enum ShopItemKind { Card, Relic, Heal, Training, RemoveCard, Growth, Research, Draft }
    public sealed class ShopOffer
    {
        public ShopOffer(ShopItemKind kind, int price, string title, string description, CardDefinition card = null, RelicDefinition relic = null, int draftGrade=-1)
        { Kind = kind; Price = price; Title = title; Description = description; Card = card; Relic = relic; DraftGrade=draftGrade; }
        public int DraftGrade { get; }
        public ShopItemKind Kind { get; }
        public int Price { get; }
        public string Title { get; }
        public string Description { get; }
        public CardDefinition Card { get; }
        public RelicDefinition Relic { get; }
    }

    public sealed partial class RunGameSession
    {
        public bool HasEconomy { get; private set; }
        public bool HasContentExpansion { get; private set; }
        public bool HasGrandArchive { get; private set; }
        public bool TryEnableGrandArchive()
        {
            if(IsPractice || !HasContentExpansion || HasGrandArchive || journal==null || journal.Count!=4
                || journal[3].Kind!=RunCommandKind.ContentExpansion || Phase!=RunPhase.MapSelection
                || Map.ActiveNodeIndex>=0 || Map.CompletedNodeCount!=0) return false;
            rewardPool=new CardDefinition[FragmentCardCatalog.Version28.Count];
            for(int i=0;i<rewardPool.Length;i++) rewardPool[i]=FragmentCardCatalog.Version28[i];
            var starting=new CardDefinition[Deck.Count];
            for(int i=0;i<starting.Length;i++) starting[i]=FragmentCardCatalog.Previous(Deck[i].Id);
            Deck=new RunDeck(starting); HasGrandArchive=true;
            return Record(true,RunCommandKind.GrandArchive);
        }
        public bool TryEnableContentExpansion()
        {
            if(IsPractice || !HasEconomy || HasContentExpansion || journal==null || journal.Count!=3
                || journal[2].Kind!=RunCommandKind.ExpeditionEconomy || Phase!=RunPhase.MapSelection
                || Map.ActiveNodeIndex>=0 || Map.CompletedNodeCount!=0) return false;
            CharacterDefinition character=null;
            foreach(var candidate in PrototypeCharacterCatalog.All) if(candidate.Id==journalCharacter) character=candidate;
            if(character==null) return false;
            rewardPool=new CardDefinition[FragmentCardCatalog.Version27.Count];
            for(int i=0;i<rewardPool.Length;i++) rewardPool[i]=FragmentCardCatalog.Version27[i];
            relicPool=new RelicDefinition[FragmentRelicCatalog.Version30.Count];
            for(int i=0;i<relicPool.Length;i++) relicPool[i]=FragmentRelicCatalog.Version30[i];
            var baseline=OpeningRoute.Create(DungeonGenerator.Generate(Seed,character,appliedLegacyBenefits.MaxHealth),character);
            Map=new RunMapProgress(ExpeditionContent.Create(baseline,character,Seed,true));
            HasContentExpansion=true;
            return Record(true,RunCommandKind.ContentExpansion);
        }
        public int Coins { get; private set; }
        public int ResearchTickets { get; private set; }
        public int LastCoinsAwarded { get; private set; }
        public string LastLootText { get; private set; } = "";
        public bool IsResearchReward { get; private set; }
        private XorShiftRandom lootRandom;
        private bool pendingBonusRelic;
        private ShopOffer[] shopOffers;
        private readonly bool[] shopSold = new bool[13];
        private int shopRemovalIndex = -1;
        public bool IsShop => Phase == RunPhase.Room && Map.ActiveNodeIndex >= 0
            && Map.Definition.GetNode(Map.ActiveNodeIndex).Kind == RunNodeKind.Shop;
        public int ShopOfferCount => IsShop ? shopOffers?.Length ?? 0 : 0;
        public ShopOffer GetShopOffer(int index) => shopOffers[index];
        public bool ShopSold(int index) => shopSold[index];
        public bool CanUseResearch => HasEconomy && !HasRewardPacks && !IsPractice && Phase == RunPhase.MapSelection
            && ResearchTickets > 0 && Deck.Count < RunDeck.MaximumCards;

        public bool TryEnableEconomy()
        {
            if (IsPractice || HasEconomy || !HasOpeningRoute || journal == null || journal.Count != 2
                || journal[1].Kind != RunCommandKind.OpeningRoute || Phase != RunPhase.MapSelection
                || Map.ActiveNodeIndex >= 0 || Map.CompletedNodeCount != 0) return false;
            CharacterDefinition character = null;
            foreach (var candidate in PrototypeCharacterCatalog.All) if (candidate.Id == journalCharacter) character = candidate;
            if (character == null) return false;
            Map = new RunMapProgress(ExpeditionContent.Create(Map.Definition, character, Seed));
            lootRandom = new XorShiftRandom(Seed ^ 0xA14C67D3u);
            HasEconomy = true;
            return Record(true, RunCommandKind.ExpeditionEconomy);
        }

        private void AwardCombatLoot(RunNodeKind kind)
        {
            if (HasExpeditionSupplies) return;
            int layer = Map.Definition.GetNode(Map.LastCompletedNodeIndex).Layer;
            LastCoinsAwarded = 10 + layer / 8 * 2 + lootRandom.Next(4)
                + (kind == RunNodeKind.Elite ? 6 : kind == RunNodeKind.Boss ? 12 : 0);
            Coins += LastCoinsAwarded;
            ResearchTickets++;
            // The introduction is predictable; later ordinary battles can add a second reward screen.
            pendingBonusRelic = kind == RunNodeKind.Battle && layer >= 2 && lootRandom.Next(100) < 25 && CanOfferRelicReward();
            LastLootText = "전리품 · 은화 +" + LastCoinsAwarded + " · 연구권 +1"
                + (pendingBonusRelic ? " · 추가 유물 발견" : "");
        }

        public bool TryUseResearch()
        {
            if (!CanUseResearch) return false;
            ResearchTickets--;
            IsResearchReward = true;
            LastLootText = "연구권 1장 사용 · 카드 하나 선택 (넘겨도 연구권은 반환되지 않습니다)";
            GenerateRewardOptions(); Phase = RunPhase.CardReward;
            return Record(true, RunCommandKind.UseResearch);
        }

        private void PrepareShop(int nodeIndex)
        {
            if (HasMarketBalance) { PrepareExpandedShop(nodeIndex); return; }
            var random = new XorShiftRandom(Seed ^ ((uint)(nodeIndex + 1) * 0x45D9F3Bu));
            Array.Clear(shopSold, 0, shopSold.Length);
            var card = rewardPool[random.Next(rewardPool.Length)];
            RelicDefinition relic = null;
            int start = random.Next(relicPool.Length);
            for (int i = 0; i < relicPool.Length; i++)
            {
                var candidate = relicPool[(start + i) % relicPool.Length];
                if (!Relics.Contains(candidate.Id)) { relic = candidate; break; }
            }
            shopOffers = new[] {
                new ShopOffer(ShopItemKind.Card, 8 + (int)card.Rarity * 2, card.DisplayName, card.Description, card: card),
                new ShopOffer(ShopItemKind.Relic, 24, relic?.DisplayName ?? "유물 품절", relic?.Description ?? "모든 유물을 이미 보유했습니다.", relic: relic),
                new ShopOffer(ShopItemKind.Heal, 8, "회복약", "최대 체력의 30% 회복"),
                new ShopOffer(ShopItemKind.Training, 16, "작도 도구 연마", "이번 원정의 그래프 피해 +1 · 훈련 최대 +3"),
                new ShopOffer(ShopItemKind.RemoveCard, 10, "기록 삭제", "덱에서 카드 1장 제거 · 선택 확정 때 결제"),
                new ShopOffer(ShopItemKind.Growth, 12, "성좌의 조각", "이번 원정 성장점 +1")
            };
        }

        public string ShopBlocker(int index)
        {
            if (!IsShop || index < 0 || index >= ShopOfferCount) return "상점에서만 구매할 수 있습니다";
            if (shopSold[index]) return "구매 완료";
            var offer = shopOffers[index];
            if (offer.Kind == ShopItemKind.Growth && Growth.HasPointLimit && Growth.TotalPointBudget >= Growth.MaximumPointBudget) return "성장점 최대 한도";
            if (Coins < offer.Price) return "은화 부족";
            if ((offer.Kind == ShopItemKind.Card || offer.Kind==ShopItemKind.Draft) && Deck.Count >= RunDeck.MaximumCards) return "덱이 가득 찼습니다";
            if (offer.Kind == ShopItemKind.Relic && (offer.Relic == null || Relics.Contains(offer.Relic.Id))) return "이미 보유 / 품절";
            if (offer.Kind == ShopItemKind.Relic && Relics.Count >= RunRelicCollection.MaximumRelics) return "유물 보유 한도";
            if (offer.Kind == ShopItemKind.Heal && persistentHealth >= PlayerMaxHealth) return "체력이 가득 찼습니다";
            if (offer.Kind == ShopItemKind.Training && TrainingPower >= 3) return "훈련 최대 단계";
            if (offer.Kind == ShopItemKind.RemoveCard && Deck.Count <= handSize) return "시작 손패 수보다 덱을 줄일 수 없습니다";
            return "";
        }

        public bool TryBuyShop(int index)
        {
            if (!string.IsNullOrEmpty(ShopBlocker(index))) return false;
            var offer = shopOffers[index];
            switch (offer.Kind)
            {
                case ShopItemKind.Card: if (!Deck.TryAdd(offer.Card)) return false; break;
                case ShopItemKind.Relic: if (!Relics.TryAdd(offer.Relic)) return false; break;
                case ShopItemKind.Heal: persistentHealth = Math.Min(PlayerMaxHealth, persistentHealth + (int)Math.Ceiling(PlayerMaxHealth * .3)); break;
                case ShopItemKind.Training: TrainingPower++; break;
                case ShopItemKind.Growth: Growth.AddPoints(1); break;
                case ShopItemKind.Research: ResearchTickets++; break;
                case ShopItemKind.Draft:
                    GenerateDraft(offer.DraftGrade); returnToShop=true; Phase=RunPhase.CardReward; break;
                case ShopItemKind.RemoveCard:
                    shopRemovalIndex = index; removalHealthCost = 0; Phase = RunPhase.DeckRefinement;
                    return Record(true, RunCommandKind.BuyShop, index);
            }
            CompleteShopPurchase(index);
            return Record(true, RunCommandKind.BuyShop, index);
        }

        private void CompleteShopPurchase(int index)
        {
            Coins -= shopOffers[index].Price; shopSold[index] = true;
            RoomResult = shopOffers[index].Title + " 구매 · 은화 -" + shopOffers[index].Price;
        }
    }
}

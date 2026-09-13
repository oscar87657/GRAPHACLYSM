using System;
using Graphaclysm.Core.Decks;
using Graphaclysm.Core.Runs;

namespace Graphaclysm.Application
{
    public enum LootKind { Coins, Research, Cards, Relics, Key, Chest }
    public sealed class LootOffer
    {
        public LootOffer(LootKind kind, int amount, string title, string description, int draftGrade=-1)
        { Kind=kind; Amount=amount; Title=title; Description=description; DraftGrade=draftGrade; }
        public int DraftGrade { get; }
        public LootKind Kind { get; }
        public int Amount { get; }
        public string Title { get; }
        public string Description { get; }
        public bool Resolved { get; internal set; }
        public string Result { get; internal set; } = "";
    }

    public sealed partial class RunGameSession
    {
        public bool HasExpeditionSupplies { get; private set; }
        public bool IsStartingRelic { get; private set; }
        public int Keys { get; private set; }
        public int GrowthCombatCredit { get; private set; }
        public string GrowthProgressText { get; private set; } = "";
        public string GrowthTreeProgressText { get; private set; } = "";
        public string SupplyInventoryText => supplyInventoryText;
        private string supplyInventoryText = "";
        private readonly LootOffer[] lootOffers = new LootOffer[6];
        private int lootCount;
        private bool returnToLoot;
        private int lootChoiceIndex=-1;
        private bool chestWillOpen;
        private RoomStory supplyRoomStory;
        private void PrepareSupplyRoom(RoomStory source)
        {
            supplyRoomStory=source; if(source==null) return;
            bool changed=false; var choices=new RoomChoice[source.ChoiceCount];
            for(int i=0;i<choices.Length;i++)
            {
                var choice=source.GetChoice(i); choices[i]=choice;
                if(choice.Effect!=RoomEffect.GrowthPoints) continue;
                int cost=Math.Max(6,choice.HealthCost);
                choices[i]=new RoomChoice(choice.Title,"체력 "+cost+" 소모 · 성장점 +1 (총한도 내)",RoomEffect.GrowthPoints,1,cost); changed=true;
            }
            if(changed) supplyRoomStory=new RoomStory(source.Title,source.Body,choices);
        }
        public int LootCount => Phase==RunPhase.Loot?lootCount:0;
        public LootOffer GetLoot(int index) => index>=0 && index<lootCount?lootOffers[index]:null;
        public int PendingLootCount { get { int count=0; for(int i=0;i<lootCount;i++) if(!lootOffers[i].Resolved) count++; return count; } }
        private static readonly RoomStory SupplyVaultStory = new RoomStory("봉인된 유물 상자",
            "잠긴 상자가 남아 있다. 열쇠로 안전하게 열거나, 잠금장치를 직접 풀어 볼 수 있다.",
            new RoomChoice("상자를 살펴본다", "열쇠 1개로 확정 개방 / 열쇠 없이 성공률 50% · 시도는 한 번", RoomEffect.Leave),
            new RoomChoice("상자를 두고 쉰다", "최대 체력의 15% 회복", RoomEffect.Heal,15));

        public bool TryEnableExpeditionSupplies(bool legacyFreeRelic=true)
        {
            if (IsPractice || HasExpeditionSupplies || !Growth.HasPointLimit || journal==null || journal.Count!=13
                || journal[12].Kind!=RunCommandKind.GrowthLimit || Phase!=RunPhase.MapSelection
                || Map.CompletedNodeCount!=0 || Map.ActiveNodeIndex>=0) return false;
            HasExpeditionSupplies=true;
            Growth=new RunGrowthState(Growth.Archetype,StartingApproach,true,true,true);
            // Previously bought starting experience remains useful, without exceeding the three-point start.
            GrowthCombatCredit=Math.Min(3,Math.Max(0,appliedLegacyBenefits.StartingExperience));
            Keys=1;
            IsStartingRelic=legacyFreeRelic;
            if(legacyFreeRelic)
            {
                string[] starterIds={"relic.silver_quill","relic.moon_winding","relic.ivory_seal"};
                for(int i=0;i<starterIds.Length;i++)
                    foreach(var relic in relicPool) if(relic.Id==starterIds[i]) { relicRewardOptions[i]=relic; break; }
            }
            LastLootText=legacyFreeRelic?"출발 준비 · 유물 하나 무료 선택 · 상자 열쇠 1개":"출발 준비 · 성장점 3 · 상자 열쇠 1개";
            Phase=legacyFreeRelic?RunPhase.RelicReward:RunPhase.MapSelection;
            RefreshSupplyText();
            return Record(true,RunCommandKind.ExpeditionSupplies,legacyFreeRelic?0:1);
        }

        private void RefreshSupplyText()
        {
            supplyInventoryText="은화 "+Coins+(HasRewardPacks?"":" · 연구권 "+ResearchTickets)+" · 열쇠 "+Keys;
            if(HasRewardPacks)MapPreparationText=ExpeditionSquads.Names[(int)Squad]+" · 원정 보조\n그래프 피해 +"+(MasteryRank+TrainingPower+PermanentPlotPower)
                +" · 시작 보호막 +"+(legacyStartingShield+MasteryRank+PreparedShield)+(PermanentOpeningHand>0?" · 시작 손패 +1":"")+"\n"+(BetterDrafts?"전투 파편 보상 한 단계 상승":"성장점 5점 투자마다 숙련 상승");
            GrowthProgressText=Growth.TotalPointBudget>=9?"성장 9점 완성 · 반환해도 총한도는 그대로입니다":
                "시작 3점 · 진행 최대 6점 / 전투 경험 "+GrowthCombatCredit+" / 10 (5마다 +1, 최대 2점) · 1~4층 보스마다 +1";
            GrowthTreeProgressText="시작 3점 · 진행 "+Growth.AwardedPoints+" / 6점\n전투 경험 "+GrowthCombatCredit+" / 10 · 5마다 +1\n1~4층 보스마다 +1 · 기술/궁극 각성 각 1개";
        }

        private void AwardEarnedGrowth(RunNodeKind kind)
        {
            int points=0;
            if(kind==RunNodeKind.Boss && Map.Phase!=RunMapProgressPhase.Completed) points=1;
            else if((kind==RunNodeKind.Battle || kind==RunNodeKind.Elite) && GrowthCombatCredit<10)
            { GrowthCombatCredit++; if(GrowthCombatCredit==5 || GrowthCombatCredit==10) points=1; }
            int before=Growth.TotalPointBudget; Growth.AddPoints(points);
            LastExplorationPoints=Growth.TotalPointBudget-before;
            RefreshSupplyText();
        }

        private void ResetLoot()
        { Array.Clear(lootOffers,0,lootOffers.Length); lootCount=0; returnToLoot=false; lootChoiceIndex=-1; }
        private void AddLoot(LootKind kind,int amount,string title,string description)
        { lootOffers[lootCount++]=new LootOffer(kind,amount,title,description); }
        private void AddChest()
        { AddLoot(LootKind.Chest,1,"봉인된 유물 상자","열쇠 1개: 확정 개방\n직접 열기: 성공률 50%\n실패하면 상자를 잃습니다. 시도는 한 번."); }

        private void PrepareCombatSupplies(RunNodeKind kind)
        {
            ResetLoot();
            int layer=Map.Definition.GetNode(Map.LastCompletedNodeIndex).Layer;
            int coins=10+layer/8*2+lootRandom.Next(4)+(kind==RunNodeKind.Elite?6:kind==RunNodeKind.Boss?12:0);
            AddLoot(LootKind.Coins,coins,"은화 "+coins,"상점에서 카드·유물·정비를 구매합니다.");
            if(HasRewardPacks)
            {
                int danger=kind==RunNodeKind.Boss?2:kind==RunNodeKind.Elite?1:0;
                AddDraftLoot(Graphaclysm.Core.Cards.FragmentDraft.RollGrade(lootRandom,danger,BetterDrafts));
                AddDraftLoot(Graphaclysm.Core.Cards.FragmentDraft.RollGrade(lootRandom,danger,BetterDrafts));
            }
            else
            {
                AddLoot(LootKind.Research,1,"파편 연구권","지도에서 사용하면 카드 3장 중 하나를 고릅니다.");
                AddLoot(LootKind.Cards,1,"새로운 파편","카드 3장 중 하나 선택. 원하지 않으면 넘길 수 있습니다.");
            }
            if((kind==RunNodeKind.Elite || kind==RunNodeKind.Boss || lootRandom.Next(100)<25) && CanOfferRelicReward())
                AddLoot(LootKind.Relics,1,"유물 발견","미보유 유물 중 하나를 골라 이번 원정에 가져갑니다.");
            if(kind==RunNodeKind.Boss || lootRandom.Next(100)<25) AddLoot(LootKind.Key,1,"상자 열쇠","유물 상자 하나를 실패 없이 엽니다. 이번 원정에서만 사용합니다.");
            if(layer>0 && lootRandom.Next(100)<20 && CanOfferRelicReward()) AddChest();
            chestWillOpen=lootRandom.Next(100)<50;
            LastCoinsAwarded=0;
            LastLootText="전리품을 발견했습니다. 받은 것만 보유 목록에 추가됩니다.";
            Phase=RunPhase.Loot; RefreshSupplyText();
        }

        private void PrepareVaultSupplies()
        {
            ResetLoot();
            if(CanOfferRelicReward()) AddChest();
            var random=new XorShiftRandom(Seed ^ ((uint)(Map.LastCompletedNodeIndex+1)*0x93F21A65u));
            chestWillOpen=random.Next(100)<50;
            LastLootText="봉인된 수장고 · 받지 않은 상자는 떠나면 사라집니다.";
            Phase=RunPhase.Loot; RefreshSupplyText();
        }

        public string LootBlocker(int index)
        {
            if(!HasExpeditionSupplies || Phase!=RunPhase.Loot || index<0 || index>=lootCount) return "보상 화면에서만 가능";
            var offer=lootOffers[index];
            if(offer.Resolved) return offer.Result;
            if(offer.Kind==LootKind.Cards && Deck.Count>=RunDeck.MaximumCards) return "덱이 가득 찼습니다";
            if((offer.Kind==LootKind.Relics || offer.Kind==LootKind.Chest) && (Relics.Count>=RunRelicCollection.MaximumRelics || !CanOfferRelicReward())) return "유물 보유 한도 / 남은 유물 없음";
            return "";
        }

        public bool TryClaimLoot(int index)
        {
            if(LootBlocker(index)!="") return false;
            var offer=lootOffers[index]; if(offer.Kind==LootKind.Chest) return false;
            switch(offer.Kind)
            {
                case LootKind.Coins: Coins+=offer.Amount; LastCoinsAwarded+=offer.Amount; break;
                case LootKind.Research: ResearchTickets+=offer.Amount; break;
                case LootKind.Key: Keys+=offer.Amount; break;
                case LootKind.Cards:
                    if(HasRewardPacks)GenerateDraft(Math.Max(0,offer.DraftGrade));else GenerateRewardOptions();
                    returnToLoot=true; Phase=RunPhase.CardReward; break;
                case LootKind.Relics: GenerateRelicRewardOptions(); returnToLoot=true; Phase=RunPhase.RelicReward; break;
            }
            if(returnToLoot)lootChoiceIndex=index;
            offer.Resolved=true; offer.Result=returnToLoot?"선택 중":"획득 완료";
            RefreshSupplyText();
            return Record(true,RunCommandKind.ClaimLoot,index);
        }

        public bool TryOpenLootChest(int index, bool useKey)
        {
            if(LootBlocker(index)!="" || lootOffers[index].Kind!=LootKind.Chest || (useKey && Keys<1)) return false;
            if(useKey) Keys--;
            bool success=useKey || chestWillOpen;
            var offer=lootOffers[index]; offer.Resolved=true; offer.Result=success?"개방 성공":"개방 실패 · 상자 소실";
            LastLootText=success?"상자가 열렸습니다. 유물 하나를 고르세요.":"잠금장치가 부서져 상자를 잃었습니다.";
            if(success) { GenerateRelicRewardOptions(); returnToLoot=true; lootChoiceIndex=index; Phase=RunPhase.RelicReward; }
            RefreshSupplyText();
            return Record(true,RunCommandKind.OpenLootChest,index*2+(useKey?1:0));
        }

        public bool TryLeaveLoot()
        {
            if(!HasExpeditionSupplies || Phase!=RunPhase.Loot) return false;
            for(int i=0;i<lootCount;i++) if(!lootOffers[i].Resolved) { lootOffers[i].Resolved=true; lootOffers[i].Result="두고 감"; }
            Phase=Map.Phase==RunMapProgressPhase.Completed?RunPhase.Completed:RunPhase.MapSelection;
            CurrentBattle=null;
            return Record(true,RunCommandKind.LeaveLoot);
        }
    }
}

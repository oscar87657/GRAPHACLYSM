using System;
using Graphaclysm.Core.Cards;

namespace Graphaclysm.Application
{
    public enum ExpeditionSquad { Survey, Assault, Shelter, Salvage }
    public static class ExpeditionSquads
    {
        public static readonly string[] Names={"탐사 분대","돌파 분대","수호 분대","발굴 분대"};
        public static readonly string[] Descriptions={"추가 규칙 없이 출발", "그래프 피해 +2", "전투 시작 보호막 +6 · 승리 회복 +2", "시작 은화 +12 · 전투 파편 보상 한 단계 상승"};
    }
    public sealed partial class RunGameSession
    {
        public bool HasRewardPacks { get; private set; }
        public ExpeditionSquad Squad { get; private set; }
        public int PermanentPlotPower { get; private set; }
        public int PermanentOpeningHand { get; private set; }
        public int PreparationSnapshot { get; private set; }
        public bool BetterDrafts { get; private set; }
        public int ActiveDraftGrade { get; private set; } = -1;
        public string MapPreparationText { get; private set; } = "";
        private bool returnToShop;
        // Bits: plot rank 0..1, coin rank 2..3, starter relic 4, extra key 5,
        // improved loot 6, selected squad 7..8. Bit 9 remains reserved.
        // Expansion: coins 10, plot 11, key 12, opening hand 13. Journal owns the snapshot.
        public bool TryEnablePreparation(int snapshot)
        {
            if(IsPractice || HasRewardPacks || !HasExpeditionSupplies || IsStartingRelic
                || journal==null || journal.Count!=14 || Phase!=RunPhase.MapSelection
                || Map.CompletedNodeCount!=0 || Map.ActiveNodeIndex>=0
                || snapshot<0 || (snapshot & ~15871)!=0 || (snapshot&3)>2 || ((snapshot>>2)&3)>2)return false;
            HasRewardPacks=true;PreparationSnapshot=snapshot;
            Squad=(ExpeditionSquad)((snapshot>>7)&3);
            PermanentPlotPower=(snapshot&3)+(Squad==ExpeditionSquad.Assault?2:0);
            Coins+=((snapshot>>2)&3)*4+(Squad==ExpeditionSquad.Salvage?12:0);
            if((snapshot&1024)!=0)Coins+=8;
            if((snapshot&2048)!=0)PermanentPlotPower++;
            if((snapshot&4096)!=0)Keys++;
            PermanentOpeningHand=(snapshot&8192)!=0?1:0;
            BetterDrafts=(snapshot&64)!=0 || Squad==ExpeditionSquad.Salvage;
            if((snapshot&32)!=0)Keys++;
            if(Squad==ExpeditionSquad.Shelter){legacyStartingShield+=6;legacyVictoryHealing+=2;}
            if((snapshot&16)!=0)
            {
                string[] ids={"relic.silver_quill","relic.moon_winding","relic.ivory_seal"};
                for(int i=0;i<ids.Length;i++)foreach(var relic in relicPool)if(relic.Id==ids[i]){relicRewardOptions[i]=relic;break;}
                IsStartingRelic=true;Phase=RunPhase.RelicReward;
            }
            LastLootText=ExpeditionSquads.Names[(int)Squad]+" · 출발 준비 완료";
            RefreshSupplyText();
            return Record(true,RunCommandKind.ExpeditionPreparation,snapshot);
        }
        private void GenerateDraft(int grade)
        {
            ActiveDraftGrade=grade;
            for(int i=0;i<rewardOptions.Length;i++)rewardOptions[i]=FragmentDraft.Draw(rewardRandom,rewardPool,grade,rewardOptions,i);
        }
        private void AddDraftLoot(int grade)
        {lootOffers[lootCount++]=new LootOffer(LootKind.Cards,1,FragmentDraft.Name(grade),FragmentDraft.Description(grade),grade);}
    }
}

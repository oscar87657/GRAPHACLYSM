using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Graphaclysm.Application
{
    public readonly struct LegacyBenefits
    {
        public LegacyBenefits(int maxHealth, int victoryHealing, int startingResonance,
            int startingExperience, int startingShield, int rewardBonus)
        { MaxHealth = maxHealth; VictoryHealing = victoryHealing; StartingResonance = startingResonance;
            StartingExperience = startingExperience; StartingShield = startingShield; RewardBonus = rewardBonus; }
        public int MaxHealth { get; }
        public int VictoryHealing { get; }
        public int StartingResonance { get; }
        public int StartingExperience { get; }
        public int StartingShield { get; }
        public int RewardBonus { get; }
    }

    public sealed class LegacyNode
    {
        public LegacyNode(string name, string description, int maxRank, int baseCost)
        { Name = name; Description = description; MaxRank = maxRank; BaseCost = baseCost; }
        public string Name { get; }
        public string Description { get; }
        public int MaxRank { get; }
        public int BaseCost { get; }
    }

    /// <summary>Account-scoped assistance purchased with after-run starlight.</summary>
    public sealed class LegacyProgression
    {
        public const int NodeCount = 24;
        internal static readonly string[] Ids={"health","healing","resonance","experience","shield","income","plot","coins","starter_relic","key","draft","squad_assault","squad_shelter","squad_salvage",
            "health_deep","healing_deep","resonance_deep","shield_deep","safe_departure","steady_return","travel_fund","plot_mastery","spare_key","opening_hand"};
        private static readonly int[] Parents={-1,-1,-1,-1,-1,-1,-1,-1,-1,7,8,6,4,7,0,1,2,4,14,15,7,6,9,3};
        public static int Prerequisite(int index) => index>=0 && index<NodeCount?Parents[index]:-1;
        private static readonly LegacyNode[] Nodes =
        {
            new LegacyNode("튼튼한 여행 가방", "새 원정 최대 체력 +2 / 단계", 3, 2),
            new LegacyNode("남겨 둔 붕대", "전투 승리 후 체력 회복 +1 / 단계", 3, 2),
            new LegacyNode("울리는 기억", "전투 시작 공명 +1 / 단계", 2, 3),
            new LegacyNode("낡은 지도 주석", "새 원정 성장 경험 +1 / 단계 · 경험 5마다 성장점 +1 (최대 2점)", 3, 2),
            new LegacyNode("초행자의 부적", "전투 시작 보호막 +2 / 단계", 2, 3),
            new LegacyNode("되비치는 별빛", "원정 종료 시 잔광 획득 +1 / 단계", 3, 4),
            new LegacyNode("정밀 작도", "모든 전투의 그래프 피해 +1 / 단계", 2, 3),
            new LegacyNode("여행 경비", "새 원정 시작 은화 +4 / 단계", 2, 3),
            new LegacyNode("유물 지참", "출발할 때 유물 3개 중 1개 선택", 1, 4),
            new LegacyNode("예비 열쇠", "새 원정 시작 열쇠 +1", 1, 4),
            new LegacyNode("파편 감별", "전투의 파편 선택 보상 색이 한 단계 상승", 1, 6),
            new LegacyNode("돌파 분대 개방", "선택 시 원정 내내 그래프 피해 +2", 1, 4),
            new LegacyNode("수호 분대 개방", "선택 시 전투 시작 보호막 +6 · 승리 회복 +2", 1, 4),
            new LegacyNode("발굴 분대 개방", "선택 시 시작 은화 +12 · 전투 파편 보상 한 단계 상승\n파편 감별과 중첩하지 않음", 1, 4),
            new LegacyNode("먼 길의 체력", "새 원정 최대 체력 +4", 1, 4),
            new LegacyNode("여분의 붕대", "전투 승리 후 체력 회복 +1", 1, 4),
            new LegacyNode("깊게 울리는 기억", "전투 시작 공명 +1", 1, 5),
            new LegacyNode("겹쳐 쓴 부적", "전투 시작 보호막 +3", 1, 5),
            new LegacyNode("든든한 출발", "새 원정 최대 체력 +2\n전투 시작 보호막 +2", 1, 6),
            new LegacyNode("다음 싸움의 준비", "전투 승리 후 체력 회복 +1\n전투 시작 공명 +1", 1, 6),
            new LegacyNode("넉넉한 여비", "새 원정 시작 은화 +8", 1, 5),
            new LegacyNode("숙련된 작도", "모든 전투의 그래프 피해 +1", 1, 6),
            new LegacyNode("두 번째 예비 열쇠", "새 원정 시작 열쇠 +1\n예비 열쇠와 함께 적용", 1, 6),
            new LegacyNode("넓게 펼친 손패", "전투 시작 손패 +1\n덱이 7장 이상일 때부터 유효\n매 턴 뽑는 카드 수는 그대로", 1, 7)
        };
        private readonly int[] ranks = new int[NodeCount];
        private readonly List<uint> rewardedSeeds = new List<uint>(32);

        public int Currency { get; private set; }
        public bool IsReadOnly { get; internal set; }
        public bool TreeResetApplied { get; internal set; } = true;
        public int RefundValue
        {
            get { int sum=0;for(int i=0;i<NodeCount;i++)for(int rank=0;rank<ranks[i];rank++)sum+=Nodes[i].BaseCost+rank;return sum; }
        }
        public bool TryResetForTree(Func<LegacyProgression,bool> persist)
        {
            if(IsReadOnly || TreeResetApplied || persist==null)return false;
            int refund=RefundValue;if(Currency>int.MaxValue-refund)return false;
            var candidate=new LegacyProgression();candidate.Restore(Currency+refund,new int[NodeCount],CopyRewardedSeeds());
            try{if(!persist(candidate))return false;}catch(Exception ex)when(RunSaveStore.IsFileError(ex)){return false;}
            Restore(candidate.Currency,candidate.CopyRanks(),candidate.CopyRewardedSeeds());TreeResetApplied=true;return true;
        }
        public LegacyNode GetNode(int index) => Nodes[index];
        public int GetRank(int index) => ranks[index];
        public int Cost(int index) => Nodes[index].BaseCost + ranks[index];
        public bool CanPurchase(int index) => !IsReadOnly && index >= 0 && index < NodeCount
            && ranks[index] < Nodes[index].MaxRank && Currency >= Cost(index)
            && (Prerequisite(index)<0 || ranks[Prerequisite(index)]>0);
        public bool IsSquadUnlocked(ExpeditionSquad squad) => squad==ExpeditionSquad.Survey
            || ((int)squad>=1 && (int)squad<=3 && ranks[10+(int)squad]>0);
        public int Preparation(ExpeditionSquad squad=ExpeditionSquad.Survey)
        {
            if(!IsSquadUnlocked(squad))squad=ExpeditionSquad.Survey;
            return ranks[6] | (ranks[7]<<2) | (ranks[8]<<4) | (ranks[9]<<5) | (ranks[10]<<6) | ((int)squad<<7)
                | (ranks[20]<<10) | (ranks[21]<<11) | (ranks[22]<<12) | (ranks[23]<<13);
        }
        public bool TryPurchase(int index)
        { if (!CanPurchase(index)) return false; Currency -= Cost(index); ranks[index]++; return true; }

        public int QuoteRanks(int[] targets)
        {
            if (targets == null || (targets.Length != NodeCount && targets.Length!=6)) return -1;
            int cost = 0;
            for (int i = 0; i < NodeCount; i++)
            {
                int target=i<targets.Length?targets[i]:ranks[i];
                if (target < ranks[i] || target > Nodes[i].MaxRank) return -1;
                int parent=Prerequisite(i);
                if(target>ranks[i] && parent>=0 && (parent<targets.Length?targets[parent]:ranks[parent])==0)return -1;
                for (int rank = ranks[i]; rank < target; rank++) cost += Nodes[i].BaseCost + rank;
            }
            return cost;
        }

        // Candidate-first: persistence must succeed before any live account value changes.
        public bool TryPurchaseRanks(int[] targets, int[] expectedRanks, int expectedCurrency,
            Func<LegacyProgression, bool> persist)
        {
            if (IsReadOnly || persist == null || expectedRanks == null || (expectedRanks.Length != NodeCount && expectedRanks.Length!=6) || Currency != expectedCurrency) return false;
            for (int i = 0; i < expectedRanks.Length; i++) if (expectedRanks[i] != ranks[i]) return false;
            int cost = QuoteRanks(targets);
            if (cost <= 0 || cost > Currency) return false;
            var candidate = new LegacyProgression();
            int[] expanded=CopyRanks();Array.Copy(targets,expanded,targets.Length);
            candidate.Restore(Currency - cost, expanded, CopyRewardedSeeds());
            candidate.TreeResetApplied=TreeResetApplied;
            try { if (!persist(candidate)) return false; }
            catch (Exception ex) when (RunSaveStore.IsFileError(ex)) { return false; }
            Restore(candidate.Currency, candidate.CopyRanks(), candidate.CopyRewardedSeeds());
            return true;
        }

        public LegacyBenefits Benefits => new LegacyBenefits(ranks[0]*2+ranks[14]*4+ranks[18]*2,
            ranks[1]+ranks[15]+ranks[19], ranks[2]+ranks[16]+ranks[19],
            ranks[3], ranks[4]*2+ranks[17]*3+ranks[18]*2, ranks[5]);

        public int AwardRun(uint seed, bool completed, int floor)
        {
            if (IsReadOnly) return 0;
            if (rewardedSeeds.Contains(seed)) return 0;
            int amount = (completed ? 8 : 2 + Math.Max(0, floor - 1)) + ranks[5];
            Currency += amount; rewardedSeeds.Add(seed);
            if (rewardedSeeds.Count > 64) rewardedSeeds.RemoveAt(0);
            return amount;
        }

        internal int[] CopyRanks() { var copy = new int[NodeCount]; Array.Copy(ranks, copy, NodeCount); return copy; }
        internal uint[] CopyRewardedSeeds() => rewardedSeeds.ToArray();
        internal void Restore(int currency, int[] savedRanks, uint[] seeds)
        {
            Currency = Math.Max(0, currency);
            for (int i = 0; i < NodeCount; i++) ranks[i] = i<savedRanks.Length?Math.Max(0, Math.Min(Nodes[i].MaxRank, savedRanks[i])):0;
            rewardedSeeds.Clear();
            for (int i = 0; i < seeds.Length && rewardedSeeds.Count < 64; i++)
                if (seeds[i] != 0 && !rewardedSeeds.Contains(seeds[i])) rewardedSeeds.Add(seeds[i]);
        }
    }

    public sealed class LegacyProgressionStore
    {
        private const int Magic = 0x4C474359, Version = 4;
        private readonly string path;
        public LegacyProgressionStore(string directory) { path = Path.Combine(directory, "legacy.save"); }

        public LegacyProgression Load()
        {
            var progression = new LegacyProgression();
            try
            {
                if (!File.Exists(path)) return progression;
                progression.IsReadOnly = true;
                byte[] bytes = File.ReadAllBytes(path); if (bytes.Length < 64) return progression;
                int payloadLength = bytes.Length - 32;
                using (var sha = SHA256.Create())
                {
                    byte[] expected = sha.ComputeHash(bytes, 0, payloadLength);
                    for (int i = 0; i < expected.Length; i++) if (expected[i] != bytes[payloadLength + i]) return progression;
                }
                using (var stream = new MemoryStream(bytes, 0, payloadLength, false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    if (reader.ReadInt32() != Magic) return progression;
                    int version=reader.ReadInt32();if(version<1 || version>Version)return progression;
                    int currency = reader.ReadInt32(), count = reader.ReadInt32();
                    if (currency < 0 || (version==1?count!=6:version==4?count!=LegacyProgression.NodeCount:count!=14 && count!=LegacyProgression.NodeCount)) return progression;
                    var ranks = new int[LegacyProgression.NodeCount];var seen=new bool[LegacyProgression.NodeCount];
                    for (int i = 0; i < count; i++)
                    {
                        int index=version==1?i:Array.IndexOf(LegacyProgression.Ids,reader.ReadString());
                        if(index<0 || seen[index])return progression;
                        seen[index]=true;int rank=reader.ReadInt32();
                        if(rank<0 || rank>progression.GetNode(index).MaxRank)return progression;
                        ranks[index]=rank;
                    }
                    for(int i=6;i<ranks.Length;i++)if(ranks[i]>0 && LegacyProgression.Prerequisite(i)>=0 && ranks[LegacyProgression.Prerequisite(i)]==0)return progression;
                    int seedCount = reader.ReadInt32(); if (seedCount < 0 || seedCount > 64) return progression;
                    var seeds = new uint[seedCount]; for (int i = 0; i < seedCount; i++) seeds[i] = reader.ReadUInt32();
                    if (stream.Position != stream.Length) return progression;
                    progression.Restore(currency, ranks, seeds);
                    progression.TreeResetApplied=version>=3;
                    progression.IsReadOnly = false;
                }
            }
            catch (Exception ex) when (RunSaveStore.IsFileError(ex) || ex is ArgumentException) { }
            return progression;
        }

        public bool TrySave(LegacyProgression progression)
        {
            if (progression == null || progression.IsReadOnly) return false;
            try
            {
                using (var stream = new MemoryStream())
                {
                    using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                    {
                        writer.Write(Magic); writer.Write(progression.TreeResetApplied?Version:2); writer.Write(progression.Currency);
                        int[] ranks = progression.CopyRanks(); writer.Write(ranks.Length);
                        for (int i = 0; i < ranks.Length; i++) {writer.Write(LegacyProgression.Ids[i]);writer.Write(ranks[i]);}
                        uint[] seeds = progression.CopyRewardedSeeds(); writer.Write(seeds.Length);
                        for (int i = 0; i < seeds.Length; i++) writer.Write(seeds[i]);
                    }
                    byte[] payload = stream.ToArray();
                    using (var sha = SHA256.Create()) { byte[] hash = sha.ComputeHash(payload); stream.Write(hash, 0, hash.Length); }
                    RunSaveStore.AtomicWrite(path, stream.ToArray(), true);
                }
                return true;
            }
            catch (Exception ex) when (RunSaveStore.IsFileError(ex)) { return false; }
        }
    }
}

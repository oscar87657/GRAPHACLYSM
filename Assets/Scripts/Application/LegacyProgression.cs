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
        public const int NodeCount = 6;
        private static readonly LegacyNode[] Nodes =
        {
            new LegacyNode("튼튼한 여행 가방", "새 원정 최대 체력 +2 / 단계", 3, 2),
            new LegacyNode("남겨 둔 붕대", "전투 승리 후 체력 회복 +1 / 단계", 3, 2),
            new LegacyNode("울리는 기억", "전투 시작 공명 +1 / 단계", 2, 3),
            new LegacyNode("낡은 지도 주석", "새 원정 탐사 경험 +1 / 단계", 3, 2),
            new LegacyNode("초행자의 부적", "전투 시작 보호막 +2 / 단계", 2, 3),
            new LegacyNode("되비치는 별빛", "원정 종료 시 잔광 획득 +1 / 단계", 3, 4)
        };
        private readonly int[] ranks = new int[NodeCount];
        private readonly List<uint> rewardedSeeds = new List<uint>(32);

        public int Currency { get; private set; }
        public LegacyNode GetNode(int index) => Nodes[index];
        public int GetRank(int index) => ranks[index];
        public int Cost(int index) => Nodes[index].BaseCost + ranks[index];
        public bool CanPurchase(int index) => index >= 0 && index < NodeCount
            && ranks[index] < Nodes[index].MaxRank && Currency >= Cost(index);
        public bool TryPurchase(int index)
        { if (!CanPurchase(index)) return false; Currency -= Cost(index); ranks[index]++; return true; }

        public LegacyBenefits Benefits => new LegacyBenefits(ranks[0] * 2, ranks[1], ranks[2],
            ranks[3], ranks[4] * 2, ranks[5]);

        public int AwardRun(uint seed, bool completed, int floor)
        {
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
            for (int i = 0; i < NodeCount; i++) ranks[i] = Math.Max(0, Math.Min(Nodes[i].MaxRank, savedRanks[i]));
            rewardedSeeds.Clear();
            for (int i = 0; i < seeds.Length && rewardedSeeds.Count < 64; i++)
                if (seeds[i] != 0 && !rewardedSeeds.Contains(seeds[i])) rewardedSeeds.Add(seeds[i]);
        }
    }

    public sealed class LegacyProgressionStore
    {
        private const int Magic = 0x4C474359, Version = 1;
        private readonly string path;
        public LegacyProgressionStore(string directory) { path = Path.Combine(directory, "legacy.save"); }

        public LegacyProgression Load()
        {
            var progression = new LegacyProgression();
            try
            {
                if (!File.Exists(path)) return progression;
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
                    if (reader.ReadInt32() != Magic || reader.ReadInt32() != Version) return progression;
                    int currency = reader.ReadInt32(), count = reader.ReadInt32();
                    if (currency < 0 || count != LegacyProgression.NodeCount) return progression;
                    var ranks = new int[count]; for (int i = 0; i < count; i++) ranks[i] = reader.ReadInt32();
                    int seedCount = reader.ReadInt32(); if (seedCount < 0 || seedCount > 64) return progression;
                    var seeds = new uint[seedCount]; for (int i = 0; i < seedCount; i++) seeds[i] = reader.ReadUInt32();
                    if (stream.Position != stream.Length) return progression;
                    progression.Restore(currency, ranks, seeds);
                }
            }
            catch (Exception ex) when (RunSaveStore.IsFileError(ex) || ex is ArgumentException) { }
            return progression;
        }

        public bool TrySave(LegacyProgression progression)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                    {
                        writer.Write(Magic); writer.Write(Version); writer.Write(progression.Currency);
                        int[] ranks = progression.CopyRanks(); writer.Write(ranks.Length);
                        for (int i = 0; i < ranks.Length; i++) writer.Write(ranks[i]);
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

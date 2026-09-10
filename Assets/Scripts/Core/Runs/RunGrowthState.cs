using System;
using Graphaclysm.Core.Combat;

namespace Graphaclysm.Core.Runs
{
    public sealed class RunGrowthNode
    {
        public RunGrowthNode(string name, string description, int cost, int prerequisite, int group)
        { Name = name; Description = description; Cost = cost; Prerequisite = prerequisite; Group = group; }
        public string Name { get; }
        public string Description { get; }
        public int Cost { get; }
        public int Prerequisite { get; }
        public int Group { get; }
    }

    /// <summary>Run-scoped progression. It is rebuilt by command replay and intentionally resets with a new run.</summary>
    public sealed class RunGrowthState
    {
        public const int NodeCount = 6;
        private readonly RunGrowthNode[] nodes;
        private int unlockedMask;

        public RunGrowthState(CombatArchetype archetype)
        {
            Archetype = archetype;
            nodes = archetype == CombatArchetype.Luna ? LunaNodes() : IanNodes();
            Level = 1;
        }

        public CombatArchetype Archetype { get; }
        public int Level { get; private set; }
        public int Experience { get; private set; }
        public int Points { get; private set; }
        public int ActiveVariant { get; private set; }
        public int UltimateVariant { get; private set; }
        public int ExperienceToNext => 3 + (Level - 1) / 3;
        public bool ActiveSkillUnlocked => IsUnlocked(0);
        public int StartingResonanceBonus => IsUnlocked(1) ? 1 : 0;

        public RunGrowthNode GetNode(int index)
        {
            if (index < 0 || index >= NodeCount) throw new ArgumentOutOfRangeException(nameof(index));
            return nodes[index];
        }

        public bool IsUnlocked(int index) => index >= 0 && index < NodeCount && (unlockedMask & (1 << index)) != 0;

        public bool CanPurchase(int index)
        {
            if (index < 0 || index >= NodeCount || IsUnlocked(index)) return false;
            RunGrowthNode node = nodes[index];
            return Points >= node.Cost && (node.Prerequisite < 0 || IsUnlocked(node.Prerequisite));
        }

        public bool TryPurchase(int index)
        {
            if (!CanPurchase(index)) return false;
            RunGrowthNode node = nodes[index];
            Points -= node.Cost;
            unlockedMask |= 1 << index;
            if (node.Group == 1) ActiveVariant = index - 1;
            if (node.Group == 2) UltimateVariant = index - 3;
            return true;
        }

        public bool TrySelect(int index)
        {
            if (!IsUnlocked(index)) return false;
            if (nodes[index].Group == 1) { ActiveVariant = index - 1; return true; }
            if (nodes[index].Group == 2) { UltimateVariant = index - 3; return true; }
            return false;
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0) return;
            Experience += amount;
            while (Experience >= ExperienceToNext)
            {
                Experience -= ExperienceToNext;
                Level++;
                Points++;
            }
        }

        public BattleSkillLoadout CreateLoadout()
            => new BattleSkillLoadout(ActiveSkillUnlocked, ActiveVariant, UltimateVariant, StartingResonanceBonus);

        private static RunGrowthNode[] IanNodes() => new[]
        {
            new RunGrowthNode("흑유리 각인", "전투마다 한 번, 보호막 5를 얻는 전투 기술을 엽니다.", 1, -1, 0),
            new RunGrowthNode("깊은 기록", "전투 시작 공명 +1.", 1, -1, 0),
            new RunGrowthNode("가시 장막", "전투 기술을 보호막 8 · 가시 3으로 바꿉니다.", 2, 0, 1),
            new RunGrowthNode("파열 각인", "전투 기술을 추진 5 · 정화로 바꿉니다.", 2, 0, 1),
            new RunGrowthNode("산산조각", "궁극기 피해 보너스 +10 · 적에게 파열 3.", 2, 1, 2),
            new RunGrowthNode("불멸의 기록", "궁극기 피해 +6 · 고정 2 · 자신 적중 시 회복 4.", 2, 1, 2)
        };

        private static RunGrowthNode[] LunaNodes() => new[]
        {
            new RunGrowthNode("월면 보법", "전투마다 한 번, 이동을 되돌리고 경쾌를 얻습니다.", 1, -1, 0),
            new RunGrowthNode("깊은 호흡", "전투 시작 공명 +1.", 1, -1, 0),
            new RunGrowthNode("별의 피난처", "전투 기술을 정화 · 요새화 7로 바꿉니다.", 2, 0, 1),
            new RunGrowthNode("유성의 숨", "전투 기술을 회복 5 · 추진 3으로 바꿉니다.", 2, 0, 1),
            new RunGrowthNode("만월의 포옹", "궁극기의 자신 적중 반경 · 보호막 · 회복이 커집니다.", 2, 1, 2),
            new RunGrowthNode("그믐의 칼날", "궁극기가 피해 +4를 얻고 방어 대신 공격에 치중합니다.", 2, 1, 2)
        };
    }
}

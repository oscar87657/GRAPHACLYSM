using System;
using Graphaclysm.Core.Combat;

namespace Graphaclysm.Core.Runs
{
    public sealed class RunGrowthNode
    {
        public RunGrowthNode(string name, string description, int cost, int requiredLevel, int group)
        { Name = name; Description = description; Cost = cost; RequiredLevel = requiredLevel; Group = group; }
        public string Name { get; }
        public string Description { get; }
        public int Cost { get; }
        public int RequiredLevel { get; }
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
        public int ActiveVariant => IsUnlocked(0) ? 1 : IsUnlocked(1) ? 2 : 0;
        public int ModuleVariant => IsUnlocked(2) ? 1 : IsUnlocked(3) ? 2 : 0;
        public int UltimateVariant => IsUnlocked(4) ? 1 : IsUnlocked(5) ? 2 : 0;
        public int ExperienceToNext => 3 + (Level - 1) / 3;
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
            if (Points < node.Cost || Level < node.RequiredLevel) return false;
            for (int i = 0; i < NodeCount; i++)
                if (i != index && nodes[i].Group == node.Group && IsUnlocked(i)) return false;
            return true;
        }

        public bool TryPurchase(int index)
        {
            if (!CanPurchase(index)) return false;
            RunGrowthNode node = nodes[index];
            Points -= node.Cost;
            unlockedMask |= 1 << index;
            return true;
        }

        public bool TrySelect(int index)
        {
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
            => new BattleSkillLoadout(ActiveVariant, ModuleVariant, UltimateVariant);

        private static RunGrowthNode[] IanNodes() => new[]
        {
            new RunGrowthNode("삼중 유리길", "전투 기술이 넓은 세 갈래 궤적으로 변합니다.", 1, 2, 0),
            new RunGrowthNode("집행의 직선", "피해가 커지고 처치하면 기술 대기시간이 즉시 초기화됩니다.", 1, 2, 0),
            new RunGrowthNode("반사 장막", "기술 적중 수에 따라 보호막을 얻습니다.", 1, 3, 1),
            new RunGrowthNode("파열 흔적", "기술에 맞은 적에게 파열 3을 남깁니다.", 1, 3, 1),
            new RunGrowthNode("성좌 붕괴", "궁극기 피해 +10 · 적중한 적에게 파열 3.", 1, 4, 2),
            new RunGrowthNode("불멸의 기록", "궁극기 피해 +6 · 고정 2 · 자신 적중 시 회복 4.", 1, 4, 2)
        };

        private static RunGrowthNode[] LunaNodes() => new[]
        {
            new RunGrowthNode("삼중 월광", "전투 기술이 넓은 세 갈래 월광 궤적으로 변합니다.", 1, 2, 0),
            new RunGrowthNode("낙성 추격", "피해가 커지고 처치하면 기술 대기시간이 즉시 초기화됩니다.", 1, 2, 0),
            new RunGrowthNode("은하 피난처", "기술 사용 시 정화하고 보호막·요새화를 얻습니다.", 1, 3, 1),
            new RunGrowthNode("유성 호흡", "기술 적중 시 회복하고 추진을 얻습니다.", 1, 3, 1),
            new RunGrowthNode("만월의 포옹", "궁극기의 자신 적중 반경 · 보호막 · 회복이 커집니다.", 1, 4, 2),
            new RunGrowthNode("그믐의 칼날", "궁극기가 피해 +4를 얻고 방어 대신 공격에 치중합니다.", 1, 4, 2)
        };
    }
}

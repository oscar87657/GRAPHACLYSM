using System;
using Graphaclysm.Core.Combat;

namespace Graphaclysm.Core.Runs
{
    public sealed class RunGrowthNode
    {
        public RunGrowthNode(string name, string description, int cost, int requiredLevel, int group, int parentIndex = -1)
        {
            Name = name; Description = description; Cost = cost; RequiredLevel = requiredLevel;
            Group = group; ParentIndex = parentIndex;
        }
        public string Name { get; }
        public string Description { get; }
        public int Cost { get; }
        public int RequiredLevel { get; }
        public int Group { get; }
        public int ParentIndex { get; }
    }

    /// <summary>Run-scoped progression. It is rebuilt by command replay and intentionally resets with a new run.</summary>
    public sealed class RunGrowthState
    {
        public const int NodeCount = 12;
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
        public int ModuleVariant => IsUnlocked(3) ? 1 : IsUnlocked(5) ? 2 : 0;
        public int UltimateVariant => IsUnlocked(6) ? 1 : IsUnlocked(7) ? 2 : 0;
        public int UnlockedMask => unlockedMask;
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
            if (node.ParentIndex >= 0 && !IsUnlocked(node.ParentIndex)) return false;
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
            => new BattleSkillLoadout(ActiveVariant, ModuleVariant, UltimateVariant, unlockedMask);

        private static RunGrowthNode[] IanNodes() => new[]
        {
            new RunGrowthNode("삼중 유리길", "전투 기술을 피해 6의 넓은 세 갈래 궤적으로 바꿉니다.", 1, 2, 0),
            new RunGrowthNode("집행의 직선", "피해 10의 직선으로 바꾸고, 처치하면 즉시 재사용합니다.", 1, 2, 0),
            new RunGrowthNode("오중 굴절", "삼중 유리길이 피해 5의 다섯 갈래로 넓어집니다.", 1, 3, 1, 0),
            new RunGrowthNode("반사 장막", "삼중 유리길 적중마다 보호막 2, 기본 보호막 3을 얻습니다.", 1, 3, 1, 0),
            new RunGrowthNode("연쇄 처형", "집행의 직선으로 처치하면 공명 1을 추가로 얻습니다.", 1, 3, 2, 1),
            new RunGrowthNode("파열 흔적", "집행의 직선에 맞은 적에게 파열 3을 남깁니다.", 1, 3, 2, 1),
            new RunGrowthNode("성좌 붕괴", "궁극기 피해 +10 · 적중한 적에게 파열 3.", 1, 3, 3),
            new RunGrowthNode("불멸의 기록", "궁극기 피해 +6 · 고정 2 · 자신 적중 시 회복 4.", 1, 3, 3),
            new RunGrowthNode("붕괴 반향", "성좌 붕괴로 적 2명 이상 적중하면 공명 1을 돌려받습니다.", 1, 4, 4, 6),
            new RunGrowthNode("날카로운 잔해", "성좌 붕괴의 피해가 추가로 4 증가합니다.", 1, 4, 4, 6),
            new RunGrowthNode("정지된 장", "불멸의 기록이 남기는 고정의 지속시간이 1회 늘어납니다.", 1, 4, 5, 7),
            new RunGrowthNode("불멸의 여백", "자신 적중 시 보호막 4와 회복 4를 추가로 얻습니다.", 1, 4, 5, 7)
        };

        private static RunGrowthNode[] LunaNodes() => new[]
        {
            new RunGrowthNode("삼중 월광", "전투 기술을 피해 5의 넓은 세 갈래 월광으로 바꿉니다.", 1, 2, 0),
            new RunGrowthNode("낙성 추격", "피해 8의 직선으로 바꾸고, 처치하면 즉시 재사용합니다.", 1, 2, 0),
            new RunGrowthNode("오중 월륜", "삼중 월광이 피해 4의 다섯 갈래로 넓어집니다.", 1, 3, 1, 0),
            new RunGrowthNode("은하 피난처", "삼중 월광 사용 시 정화하고 보호막 4·요새화 3을 얻습니다.", 1, 3, 1, 0),
            new RunGrowthNode("별의 포식", "낙성 추격으로 처치하면 공명 1을 추가로 얻습니다.", 1, 3, 2, 1),
            new RunGrowthNode("유성 호흡", "낙성 추격 적중 시 체력 3을 회복하고 추진 3을 얻습니다.", 1, 3, 2, 1),
            new RunGrowthNode("만월의 포옹", "궁극기의 자신 적중 반경 · 보호막 · 회복이 커집니다.", 1, 3, 3),
            new RunGrowthNode("그믐의 칼날", "궁극기가 피해 +4를 얻고 방어 대신 공격에 치중합니다.", 1, 3, 3),
            new RunGrowthNode("월광 회수", "만월의 포옹으로 적 2명 이상 적중하면 공명 1을 돌려받습니다.", 1, 4, 4, 6),
            new RunGrowthNode("넘치는 만월", "자가 적중 반경과 보호막 4·회복 2가 추가됩니다.", 1, 4, 4, 6),
            new RunGrowthNode("긴 밤의 칼날", "그믐의 칼날이 주는 피해가 추가로 4 증가합니다.", 1, 4, 5, 7),
            new RunGrowthNode("새벽의 숨", "자신 적중 시 보호막 4와 회복 4를 추가로 얻습니다.", 1, 4, 5, 7)
        };
    }
}

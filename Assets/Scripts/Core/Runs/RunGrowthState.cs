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
        public const int NodeCount = 18;
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
        public int ActiveVariant => IsUnlocked(0) ? 1 : IsUnlocked(1) ? 2 : IsUnlocked(2) ? 3 : 0;
        public int ModuleVariant => IsUnlocked(3) ? 1 : IsUnlocked(4) ? 2 : IsUnlocked(5) ? 3
            : IsUnlocked(6) ? 4 : IsUnlocked(7) ? 5 : IsUnlocked(8) ? 6 : 0;
        public int UltimateVariant => IsUnlocked(9) ? 1 : IsUnlocked(10) ? 2 : IsUnlocked(11) ? 3 : 0;
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
            new RunGrowthNode("삼중 유리길", "유리 쇄도를 피해 6의 넓은 세 갈래 관통으로 교체합니다.", 1, 2, 0),
            new RunGrowthNode("집행의 직선", "유리 쇄도를 피해 10의 단일 처형선으로 교체합니다. 처치 시 즉시 재사용합니다.", 1, 2, 0),
            new RunGrowthNode("거울 교환", "유리 쇄도를 대상 뒤로 전이해 주변을 피해 7로 폭발시키는 기술로 교체합니다.", 1, 2, 0),
            new RunGrowthNode("오중 굴절", "삼중 유리길을 피해 5의 다섯 갈래로 바꿉니다.", 1, 3, 1, 0),
            new RunGrowthNode("반사 장막", "삼중 유리길 적중마다 보호막 2, 기본 보호막 3을 얻습니다.", 1, 3, 1, 0),
            new RunGrowthNode("연쇄 처형", "집행의 직선으로 처치하면 공명 1을 얻습니다.", 1, 3, 2, 1),
            new RunGrowthNode("파열 흔적", "집행의 직선에 맞은 적에게 파열 3을 남깁니다.", 1, 3, 2, 1),
            new RunGrowthNode("잔상 폭발", "거울 교환의 폭발 반경이 1.15에서 1.85로 넓어집니다.", 1, 3, 3, 2),
            new RunGrowthNode("위상 장막", "거울 교환 뒤 보호막 6과 요새화 3을 얻습니다.", 1, 3, 3, 2),
            new RunGrowthNode("성좌 붕괴", "궁극기를 피해 +10과 파열 3을 주는 공격 형태로 교체합니다.", 1, 3, 4),
            new RunGrowthNode("불멸의 기록", "궁극기를 피해 +6, 고정 2, 자신 적중 시 회복 4 형태로 교체합니다.", 1, 3, 4),
            new RunGrowthNode("흑경 반전", "궁극기를 피해 +8, 적의 강화 제거와 약화 2를 주는 반전 형태로 교체합니다.", 1, 3, 4),
            new RunGrowthNode("붕괴 반향", "성좌 붕괴로 적 2명 이상 적중하면 공명 1을 돌려받습니다.", 1, 4, 5, 9),
            new RunGrowthNode("날카로운 잔해", "성좌 붕괴의 피해가 추가로 4 증가합니다.", 1, 4, 5, 9),
            new RunGrowthNode("정지된 장", "불멸의 기록이 남기는 고정의 지속시간이 1회 늘어납니다.", 1, 4, 6, 10),
            new RunGrowthNode("불멸의 여백", "자신 적중 시 보호막 4와 회복 4를 추가로 얻습니다.", 1, 4, 6, 10),
            new RunGrowthNode("흡광 장막", "흑경 반전으로 자신을 맞히면 해로운 상태를 지우고 보호막 6을 얻습니다.", 1, 4, 7, 11),
            new RunGrowthNode("역상 파열", "흑경 반전이 약화 3과 파열 2를 함께 남깁니다.", 1, 4, 7, 11)
        };

        private static RunGrowthNode[] LunaNodes() => new[]
        {
            new RunGrowthNode("만월 착지", "월광 도약을 넓은 착지 파동으로 교체합니다. 주변 적에게 피해 5를 줍니다.", 1, 2, 0),
            new RunGrowthNode("초승달 회귀", "월광 도약을 피해 9의 왕복 참격으로 교체합니다. 사용 후 원래 자리로 돌아옵니다.", 1, 2, 0),
            new RunGrowthNode("별무리 전이", "월광 도약을 대상에서 주변 적으로 이어지는 피해 5의 연쇄 전이로 교체합니다.", 1, 2, 0),
            new RunGrowthNode("넘치는 만월", "만월 착지의 파동 반경이 1.70에서 2.20으로 넓어집니다.", 1, 3, 1, 0),
            new RunGrowthNode("은하 피난처", "만월 착지 때 해로운 상태를 지우고 보호막 4·요새화 3을 얻습니다.", 1, 3, 1, 0),
            new RunGrowthNode("월식 흉터", "초승달 회귀에 맞은 적에게 노출 3을 남깁니다.", 1, 3, 2, 1),
            new RunGrowthNode("유성 호흡", "초승달 회귀 적중 시 체력 3을 회복하고 추진 3을 얻습니다.", 1, 3, 2, 1),
            new RunGrowthNode("별자리 확장", "별무리 전이의 연결 반경이 2.40에서 3.60으로 넓어집니다.", 1, 3, 3, 2),
            new RunGrowthNode("낙성 추격", "별무리 전이로 적을 처치하면 즉시 재사용하고 공명 1을 얻습니다.", 1, 3, 3, 2),
            new RunGrowthNode("만월의 포옹", "궁극기를 큰 자가 적중 반경과 정화·보호막·회복 형태로 교체합니다.", 1, 3, 4),
            new RunGrowthNode("그믐의 칼날", "궁극기를 피해 +4와 작은 보호막·회복을 갖는 공격 형태로 교체합니다.", 1, 3, 4),
            new RunGrowthNode("월식 정지", "궁극기를 피해 +2와 적 고정 2를 주는 제어 형태로 교체합니다.", 1, 3, 4),
            new RunGrowthNode("월광 회수", "만월의 포옹으로 적 2명 이상 적중하면 공명 1을 돌려받습니다.", 1, 4, 5, 9),
            new RunGrowthNode("차오른 만월", "자가 적중 반경이 1.50이 되고 보호막 4·회복 2가 추가됩니다.", 1, 4, 5, 9),
            new RunGrowthNode("긴 밤의 칼날", "그믐의 칼날이 주는 피해가 추가로 4 증가합니다.", 1, 4, 6, 10),
            new RunGrowthNode("새벽의 숨", "자신 적중 시 보호막 4와 회복 4를 추가로 얻습니다.", 1, 4, 6, 10),
            new RunGrowthNode("시간 회수", "월식 정지를 방출하면 전투 기술의 남은 대기시간을 없앱니다.", 1, 4, 7, 11),
            new RunGrowthNode("정지 파동", "월식 정지가 적에게 약화 3을 함께 남깁니다.", 1, 4, 7, 11)
        };
    }
}

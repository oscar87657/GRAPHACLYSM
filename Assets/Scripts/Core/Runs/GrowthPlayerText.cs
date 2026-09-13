using System.Collections.Generic;

namespace Graphaclysm.Core.Runs
{
    /// <summary>Player-facing summaries. The imported design document remains intact.</summary>
    public static class GrowthPlayerText
    {
        private static readonly Dictionary<string,string> summaries = new Dictionary<string,string> {
            {"common.geometry.01","그래프가 서로 교차한 곳을 기억합니다. 나중에 그 지점에 기술을 쓸 수 있게 하는 시작 노드입니다."},
            {"common.geometry.02","같은 적을 두 번 가로지르면 피해를 두 번에 나눠 줍니다. 총 피해는 늘어나지 않습니다."},
            {"common.geometry.03","그래프의 교차점 한 곳을 터뜨려 주변 적도 공격합니다."},
            {"common.geometry.07","선을 적에게 맞히는 대신, 닫힌 도형 안에 적을 가두면 공격합니다."},
            {"common.geometry.08","도형 안쪽 대신 주변의 빈 공간을 공격합니다. 화면 전체를 공격하지는 않습니다."},
            {"common.origin.01","이전에 식을 그리기 시작한 위치를 기억합니다. 다음 식도 그곳에서 시작할 수 있습니다."},
            {"common.origin.02","이동 한 번을 포기하고, 가까운 곳을 골라 식을 그리기 시작합니다."},
            {"common.origin.03","그래프가 지난 프리즘에서 다음 식을 시작할 수 있습니다."},
            {"common.origin.04","귀환점 카드로 식을 몸에 가져오는 대신, 몸을 식 쪽으로 옮깁니다."},
            {"common.origin.05","프리즘을 중심으로 식을 90도 돌려 빗나간 적을 겨눕니다."},
            {"common.origin.06","프리즘을 지난 공격 일부를 기억해 둔 다른 위치로 보냅니다."},
            {"common.origin.07","같은 식을 두 곳에서 그립니다. 공격력은 60:40으로 나누고 카드 효과는 한 번만 적용합니다."},
            {"common.origin.08","식을 그리는 위치를 고정합니다. 대신 이동한 뒤 식의 방향을 바꿀 수 있습니다."},
            {"common.condense.01","자신을 지나는 식을 응축하면, 다음 적 공격을 막을 보호막을 얻습니다."},
            {"common.condense.02","응축할 때 공격 일부를 먼저 내보냅니다. 나중에 방출할 피해는 그만큼 줄어듭니다."},
            {"common.condense.07","식의 앞부분을 먼저 방출하고 뒷부분은 다음 턴에 이어 씁니다. 첫 방출 뒤에도 적은 행동합니다."},
            {"common.condense.08","만들어 둔 식을 보관하고 다른 식을 조립합니다. 보관한 카드는 그동안 다시 뽑을 수 없습니다."},
            {"common.self.06","보호막으로 적 공격을 막으면 최근 그래프를 약하게 한 번 더 그립니다."},
            {"common.self.07","선에 자신을 맞히는 대신, 도형 안에 들어가면 자기 강화 효과를 받습니다."},
            {"ian.inscription.core","기술로 표시한 적을 그래프로 맞혀 균열을 쌓고, 다음 공격에 활용하는 방식입니다."},
            {"ian.archive.core","이전 그래프를 저장해 두었다가 다시 공격에 사용하는 방식입니다."},
            {"ian.citadel.core","자신을 그래프로 맞혀 만든 보호막을 남겨 두고, 벽이나 공격 장치에 쓰는 방식입니다."},
            {"luna.orbit.core","이동할 때 이미 만든 식도 함께 옮길 수 있게 하는 방식입니다."},
            {"luna.phase.core","방출할 때마다 달의 상태가 바뀝니다. 그 순서에 맞춰 기술을 쓰는 방식입니다."},
            {"luna.binary.core","몸 대신 위성을 그래프에 맞혀 자기 강화 효과를 받는 방식입니다."},
            {"ian.triple.form","현재: 적 쪽으로 이동하며 세 갈래를 공격합니다. 기본 피해 6, 재사용 3턴입니다."},
            {"ian.execute.form","현재: 지정한 적을 향해 직선으로 이동하며 피해 10. 처치하면 바로 다시 쓸 수 있습니다."},
            {"ian.exchange.form","현재: 적 뒤로 이동하며 주변 적에게 피해 7. 재사용 3턴입니다."},
            {"ian.brand.form","적에게 표식을 붙입니다. 다음 그래프가 그 적을 맞히면 주변으로 폭발합니다."},
            {"ian.link.form","두 적을 선으로 잇습니다. 그래프가 그 선을 건드리면 연결된 적에게 공격을 전달합니다."},
            {"ian.ash.form","적의 잔불 피해를 모아 뒀다가 다음 그래프에 맞으면 한꺼번에 터뜨립니다."},
            {"ian.spike.form","쐐기를 설치합니다. 그래프로 쐐기를 건드리면 공격 일부를 다른 방향으로 보냅니다."},
            {"ian.shelf.form","그래프 하나를 저장합니다. 다음 방출에 저장한 그래프도 약하게 다시 공격합니다."},
            {"ian.wall.form","적의 공격을 막아 주는 작은 벽을 설치합니다."},
            {"luna.fulljump.form","현재: 적 곁으로 뛰어들어 주변에 피해 5. 기본 범위 1.7, 재사용 3턴입니다."},
            {"luna.return.form","현재: 적을 향해 왕복하며 피해 9를 주고 원래 자리로 돌아옵니다. 재사용 3턴입니다."},
            {"luna.chain.form","현재: 가까운 적을 차례로 경유하며 피해 5. 기본 연결 거리 2.4, 재사용 3턴입니다."},
            {"luna.rotate.form","이미 만든 식을 돌려서 빗나간 적을 맞힙니다. 카드를 새로 쓸 필요가 없습니다."},
            {"luna.pull.form","적들을 고른 위치로 조금 당겨 그래프에 맞기 쉽게 모읍니다."},
            {"luna.tide.form","안쪽 적은 바깥으로, 바깥 적은 안쪽으로 옮겨 배치를 뒤집습니다."},
            {"luna.pendulum.form","두 지점을 설치합니다. 한쪽을 지난 공격 일부가 다른 쪽에서도 나옵니다."},
            {"luna.observatory.form","멀리서 식을 시작하거나 몸 대신 자기 강화 효과를 받는 관측기를 설치합니다."},
            {"luna.weave.form","기준점과 설치물을 선으로 잇습니다. 그래프로 건드리면 연결된 곳으로 공격이나 방어를 보냅니다."},
            {"ian.u.collapse.form","현재: 이번 그래프 피해 +10, 맞힌 적에게 파열 3을 남깁니다. 공명 6을 씁니다."},
            {"ian.u.immortal.form","현재: 피해 +6, 맞힌 적의 이동을 막습니다. 자신도 맞으면 체력 4를 회복합니다."},
            {"ian.u.invert.form","현재: 피해 +8, 적의 강화를 지우고 약화를 겁니다. 공명 6을 씁니다."},
            {"ian.u.rain.form","그래프 위에 공격을 예약합니다. 적이 행동한 뒤 그 위치에 유리비가 떨어집니다."},
            {"ian.u.critical.form","여러 적을 공격하는 대신, 실제로 맞힌 적 한 명에게 공격을 모읍니다."},
            {"ian.u.library.form","저장한 그래프를 골라 다음 공격에 겹쳐 쓰는 서가를 엽니다."},
            {"ian.u.glassbody.form","잠시 방어를 유리벽으로 바꾸고, 그 벽을 공격 장치로 활용합니다."},
            {"ian.u.enemytext.form","적이 공개한 공격 범위를 기록했다가 적에게 되돌려 씁니다."},
            {"ian.u.illusion.form","같은 식의 공격 위치와 자기 강화 위치를 서로 다른 곳에 둡니다."},
            {"luna.u.embrace.form","현재: 자신도 그래프에 맞으면 정화, 보호막 12, 체력 회복 7을 받습니다. 공명 6을 씁니다."},
            {"luna.u.blade.form","현재: 피해 +4. 자신도 맞으면 정화, 보호막 6, 체력 회복 3을 받습니다."},
            {"luna.u.stop.form","현재: 피해 +2, 맞힌 적의 이동을 막습니다. 적의 공격 자체는 취소하지 않습니다."},
            {"luna.u.white.form","이동하는 자신을 따라다니는 보호 구역을 만듭니다."},
            {"luna.u.twin.form","공격할 그래프와 자신을 보호할 그래프를 서로 다른 곳에 둡니다."},
            {"luna.u.devour.form","적에게 쌓인 상태 일부를 없애는 대신 피해나 보호막을 얻습니다."},
            {"luna.u.meteor.form","그래프 위에 유성우를 예약합니다. 적이 행동한 뒤 떨어집니다."},
            {"luna.u.align.form","적을 그래프 위로 조금 옮긴 뒤 공격합니다. 움직일 수 없는 적은 옮기지 못합니다."},
            {"luna.u.timeless.form","적이 행동하기 전에 남은 손패로 식을 한 번 더 만듭니다. 카드 보충과 이동 초기화는 없습니다."}
        };
        private static readonly Dictionary<string,string> cache = Build();
        private static Dictionary<string,string> Build()
        {
            var result = new Dictionary<string,string>();
            foreach (var node in GrowthCatalog.All)
            {
                if (summaries.TryGetValue(node.Id,out string summary)) { result[node.Id] = summary; continue; }
                string text = node.Effect;
                int end = text.IndexOf(". ",System.StringComparison.Ordinal);
                if (end >= 0) text = text.Substring(0,end+1);
                text = text.Replace("작도 출력","그래프 공격").Replace("작도","그래프")
                    .Replace("도안","그래프 모양").Replace("재현","다시 그리기").Replace("자가 적중","자신도 그래프에 맞기")
                    .Replace("합법적 지점","선택 가능한 위치").Replace("합법적인 지점","선택 가능한 위치")
                    .Replace("출력 일부","공격 일부").Replace("출력","공격력").Replace("투영","그리기")
                    .Replace("정산","적용").Replace("비이동 기술","이동하지 않는 기술")
                    .Replace("소비한다","씁니다").Replace("적용한다","적용합니다").Replace("얻는다","얻습니다")
                    .Replace("된다","됩니다").Replace("한다","합니다").Replace("있다","있습니다");
                result[node.Id] = text;
            }
            return result;
        }
        public static string Summary(GrowthNodeDefinition node) => ApproachGrowthCatalog.IsId(node.Id)?node.Effect:cache[node.Id];
    }
}

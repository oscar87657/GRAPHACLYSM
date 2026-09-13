using Graphaclysm.Core.Combat;

namespace Graphaclysm.Application
{
    public static class ApproachGrowthText
    {
        public static string For(CombatApproach approach, string nodeId)
        {
            if (approach == CombatApproach.None) return null;
            if (approach == CombatApproach.Observation && nodeId == "luna.binary.core")
                return "관측형은 시작부터 위성을 사용합니다. K로 위성을 배치하고 J로 다음 식의 출발점을 선택합니다. 위성이 작도에 닿으면 루나에게 자가 강화가 한 번 전달됩니다. 중복 투자로 위성이 늘어나지는 않습니다.";
            if (nodeId == "ian.archive.core") return "새 원정에서 장착하면 다음 전투부터 기록형으로 전환합니다. K로 기록 위치를 고르고, 다음 방출에 도안을 재현합니다.";
            if (nodeId == "ian.inscription.core") return "새 원정에서 장착하면 다음 전투부터 집행형으로 전환합니다. K → 방향 클릭으로 3.6 돌진. 경로의 적에게 피해 10과 파열 6을 남기고, 다음 작도로 파열을 소비합니다.";
            if (nodeId == "luna.orbit.core") return "새 원정에서 조율형으로 싸웁니다. K로 도안을 옮기고 회전하거나, J로 적을 모읍니다. 한 턴에 둘 중 하나. 시작부터 사용 가능하며 중복 강화되지는 않습니다.";
            if (approach == CombatApproach.Execution && nodeId == "ian.execute.m2")
                return "현재 원정: 적에게 남은 파열을 집행 피해에 더하고 소비합니다. 소비한 파열은 다음 작도에서 다시 쓰지 않습니다.";
            if (approach == CombatApproach.Recording && nodeId == "ian.archive.m3")
                return "현재 원정: 기록이 비어 있을 때 응축하면 현재 위치의 도안을 기록합니다. 다음 방출에 추가 공격하며 카드 능력과 드로우는 복제하지 않습니다.";
            if (approach == CombatApproach.Tuning && nodeId == "luna.rotate.m2")
                return "현재 원정: K로 조준 중 R을 누르면 회전 대신 가로축 거울 반전을 선택합니다. 미리보기 후 클릭으로 확정합니다.";
            return null;
        }
    }
}

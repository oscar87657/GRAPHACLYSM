using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Runs;

namespace Graphaclysm.Application
{
    public static class GrowthRuntimeSupport
    {
        public static bool HasEffect(GrowthNodeDefinition node, CombatApproach approach, bool modern = false)
        {
            if(ApproachGrowthCatalog.IsId(node.Id))return true;
            if (approach == CombatApproach.None) return node.IsImplemented;
            if (node.EquipKind == GrowthEquipKind.Ultimate && node.IsImplemented) return true;
            switch (node.Id)
            {
                case "luna.binary.core": return modern || approach == CombatApproach.Observation;
                case "ian.inscription.core": case "ian.archive.core": case "luna.orbit.core":
                case "ian.execute.m2": case "ian.archive.m3": case "luna.rotate.m2": return true;
                default: return false;
            }
        }
        public static string Effect(GrowthNodeDefinition node, CombatApproach approach, bool modern)
        {
            if(ApproachGrowthCatalog.IsId(node.Id))return node.Effect;
            if (approach == CombatApproach.None) return node.IsImplemented ? GrowthPlayerText.Summary(node) : null;
            if (node.Id == "luna.binary.core" && modern)
                return "장착하면 다음 전투부터 관측형으로 전환합니다. K로 위성을 놓고 J로 다음 식의 출발점을 선택합니다. 위성이 작도에 닿으면 루나가 자가 강화를 받습니다.";
            if (node.Id == "ian.execute.m2" && approach != CombatApproach.Execution)
                return "집행형에서 적용: 적의 파열을 돌진 피해에 더하고 소비합니다. 기록형에서는 발동하지 않습니다.";
            if (node.Id == "ian.archive.m3" && approach != CombatApproach.Recording)
                return "기록형에서 적용: 기록이 비었을 때 응축하면 현재 도안을 기록하고 다음 방출에 추가 공격합니다.";
            if (node.Id == "luna.rotate.m2" && approach != CombatApproach.Tuning)
                return "조율형에서 적용: K 조준 중 R로 도안을 가로축 반전합니다. 관측형에서는 발동하지 않습니다.";
            return ApproachGrowthText.For(approach, node.Id) ?? (HasEffect(node, approach, modern) ? GrowthPlayerText.Summary(node) : null);
        }
    }
}

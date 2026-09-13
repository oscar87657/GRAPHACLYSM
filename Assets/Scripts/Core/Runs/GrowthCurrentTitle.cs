namespace Graphaclysm.Core.Runs
{
    public static class GrowthCurrentTitle
    {
        // PrototypePartial names must describe the working v21 behavior, not the future design.
        public static string For(GrowthNodeDefinition node)
        {
            if (!node.IsImplemented) return GrowthUiCatalog.Get(node.Id).Title;
            switch (node.Id)
            {
                case "ian.triple.form": return "세 갈래 경로 공격";
                case "ian.execute.form": return "처치하면 다시 쓰는 돌진";
                case "ian.exchange.form": return "적 뒤로 이동·주변 공격";
                case "luna.fulljump.form": return "적 곁 착지·주변 공격";
                case "luna.return.form": return "왕복 경로 공격";
                case "luna.chain.form": return "대상 주변 연쇄 공격";
                case "ian.u.collapse.form": return "그래프 강화·파열 남기기";
                case "ian.u.immortal.form": return "그래프 강화·적 이동 봉쇄";
                case "ian.u.invert.form": return "그래프 강화·적 강화 제거";
                case "luna.u.embrace.form": return "자가 적중 정화·보호·회복";
                case "luna.u.blade.form": return "공격 강화·자가 회복";
                case "luna.u.stop.form": return "그래프 강화·적 이동 봉쇄";
                default: return node.Name;
            }
        }
    }
}

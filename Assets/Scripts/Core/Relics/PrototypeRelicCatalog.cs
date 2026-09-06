using System.Collections.Generic;

namespace Graphaclysm.Core.Relics
{
    public static class PrototypeRelicCatalog
    {
        private static readonly RelicDefinition[] Relics =
        {
            new RelicDefinition(
                "relic.brass_astrolabe",
                "황동 아스트롤라베",
                "전투마다 최대 에너지가 1 증가합니다.",
                RelicEffectKind.BonusEnergy,
                1),
            new RelicDefinition(
                "relic.silver_quill",
                "은제 필사침",
                "전투마다 손패가 1장 늘어납니다.",
                RelicEffectKind.BonusHandSize,
                1),
            new RelicDefinition(
                "relic.slope_lens",
                "기울기의 렌즈",
                "그래프가 적중할 때마다 피해가 2 증가합니다.",
                RelicEffectKind.BonusPlotDamage,
                2),
            new RelicDefinition(
                "relic.red_theorem",
                "진홍 정리",
                "전투에서 승리할 때 체력을 4 회복합니다.",
                RelicEffectKind.HealAfterVictory,
                4),
            new RelicDefinition(
                "relic.fractured_prism",
                "분할 프리즘",
                "그래프가 적중할 때마다 피해가 3 증가합니다.",
                RelicEffectKind.BonusPlotDamage,
                3),
            new RelicDefinition(
                "relic.chained_compass",
                "사슬 나침반",
                "전투마다 최대 에너지가 1 증가합니다.",
                RelicEffectKind.BonusEnergy,
                1)
        };

        public static IReadOnlyList<RelicDefinition> All
        {
            get { return Relics; }
        }
    }
}

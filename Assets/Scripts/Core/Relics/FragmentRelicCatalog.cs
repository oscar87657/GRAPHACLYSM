using System.Collections.Generic;

namespace Graphaclysm.Core.Relics
{
    public static class FragmentRelicCatalog
    {
        private static readonly RelicDefinition[] Relics =
        {
            new RelicDefinition(
                "relic.brass_astrolabe",
                "황동 아스트롤라베",
                "방출·해체 뒤 보충 +1장. 응축에는 적용하지 않습니다.",
                RelicEffectKind.BonusDraw,
                1),
            new RelicDefinition(
                "relic.silver_quill",
                "은제 필사침",
                "전투 시작 손패가 1장 늘어납니다. 손패 한도 8장.",
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
                "방출·해체 뒤 보충 +1장. 응축에는 적용하지 않습니다.",
                RelicEffectKind.BonusDraw,
                1),
            new RelicDefinition("relic.ivory_seal","상아 봉인","전투 시작 시 보호막 6. 첫 적 행동까지.",RelicEffectKind.StartShield,6),
            new RelicDefinition("relic.moon_winding","달의 태엽","전투 시작 시 공명 +1. 최대 6.",RelicEffectKind.StartResonance,1),
            new RelicDefinition("relic.lace_aegis","레이스 성막","자신에게 그래프 적중 시 보호막 +2.",RelicEffectKind.SelfShield,2),
            new RelicDefinition("relic.dawn_pin","새벽의 핀","각 전투의 첫 방출 피해 +4.",RelicEffectKind.FirstPlotDamage,4),
            new RelicDefinition("relic.short_verse","짧은 시편","파편 3개 이하로 방출하면 피해 +3.",RelicEffectKind.ShortWeaveDamage,3),
            new RelicDefinition("relic.deep_archive","심층 서판","파편 6개 이상으로 방출하면 피해 +4.",RelicEffectKind.LongWeaveDamage,4),
            new RelicDefinition("relic.traveler_clasp","여행자의 걸쇠","이번 턴 이동 후 방출하면 보호막 3.",RelicEffectKind.MovedPlotShield,3),
            new RelicDefinition("relic.echo_bell","잔향의 종","전투 승리 후 공명 +1. 최대 6.",RelicEffectKind.VictoryResonance,1),
            new RelicDefinition("relic.obsidian_hourglass","흑요 모래시계","응축할 때 보호막 4를 얻습니다.",RelicEffectKind.CondenseShield,4,"Art/Generated/relic-obsidian-hourglass-v12"),
            new RelicDefinition("relic.thorn_circlet","밤가시 관","전투 시작 시 가시 3을 얻습니다.",RelicEffectKind.StartThorns,3,"Art/Generated/relic-thorn-circlet-v12"),
            new RelicDefinition("relic.prism_heart","굴절의 심장","프리즘을 통과한 그래프 피해가 4 더 증가합니다.",RelicEffectKind.PrismDamage,4,"Art/Generated/relic-prism-heart-v12"),
            new RelicDefinition("relic.silver_metronome","월은 박자기","이동한 뒤 추진 2를 얻어 다음 그래프를 강화합니다.",RelicEffectKind.MoveMomentum,2,"Art/Generated/relic-silver-metronome-v12"),
            new RelicDefinition("relic.sealed_inkwell","봉인된 먹병","파편 6개 이상으로 적중하면 파열 2를 남깁니다.",RelicEffectKind.LongWeaveRupture,2,"Art/Generated/relic-sealed-inkwell-v12"),
            new RelicDefinition("relic.astral_key","천구의 열쇠","전투 시작 시 요새화 4를 얻습니다.",RelicEffectKind.StartFortify,4,"Art/Generated/relic-astral-key-v12")
        };

        public static IReadOnlyList<RelicDefinition> All
        {
            get { return Relics; }
        }
    }
}

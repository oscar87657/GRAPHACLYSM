using Graphaclysm.Core.Combat;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private static readonly string[] KeywordDescriptions =
        {
            "받는 피해를 먼저 막습니다. 남은 수치는 지속시간이 끝나면 사라집니다.",
            "작도가 적에게 적중할 때 피해가 수치만큼 증가합니다.",
            "적 행동이 끝날 때 체력을 수치만큼 회복합니다.",
            "턴 시작에 보호막을 무시하는 피해를 받고 지속시간이 줄어듭니다.",
            "가하는 작도·공격 피해가 수치만큼 감소합니다.",
            "받는 작도 피해가 수치만큼 증가합니다.",
            "이동할 수 없습니다. 적에게 적용하면 자리 이동도 봉쇄합니다.",
            "이동 비용을 없애며, 현재 파편 전투에서는 다음 이동 때 소비됩니다.",
            "체력 피해를 받으면 공격자에게 수치만큼 반격 피해를 줍니다.",
            "다음 그래프의 적중 피해가 증가하고 방출 뒤 소모됩니다.",
            "적 행동 직전에 수치만큼 보호막으로 전환됩니다.",
            "다음에 받는 그래프 피해가 증가하고 적중 뒤 소모됩니다.",
            "자가 적중이 발생하면 체력을 즉시 회복합니다.",
            "대상에게 걸린 해로운 상태 또는 강화를 제거합니다."
        };

        private string hoveredKeywordTitle = "", hoveredKeywordBody = "";
        private Rect keywordTooltipRect, keywordHoverBridge, keywordSourceRect, keywordOwnerRect;
        private bool keywordKeepsCard;
        private int hoveredTerrain = -1;
        private static readonly string[] StatusShortNames =
        { "보호", "집중", "재생", "잔불", "약화", "노출", "고정", "경쾌", "가시", "추진", "요새", "파열" };
        private static readonly string[,] StatusValueLabels = BuildStatusValueLabels();
        private static readonly string[] MoreStatusLabels =
        { "+0", "+1", "+2", "+3", "+4", "+5", "+6", "+7", "+8", "+9", "+10", "+11", "+12" };

        private static string[,] BuildStatusValueLabels()
        {
            var labels = new string[CombatStatusState.MaximumMagnitude + 1, 10];
            for (int magnitude = 0; magnitude <= CombatStatusState.MaximumMagnitude; magnitude++)
                for (int duration = 0; duration < 10; duration++) labels[magnitude, duration] = magnitude + " · " + duration + "T";
            return labels;
        }

        private static string StatusValueLabel(int magnitude, int duration)
            => magnitude >= 0 && magnitude <= CombatStatusState.MaximumMagnitude && duration >= 0 && duration < 10
                ? StatusValueLabels[magnitude, duration] : magnitude + " · " + duration + "T";

        private void BeginBattleHoverFrame()
        {
            Vector2 pointer = Event.current.mousePosition;
            bool keep = hoveredKeywordTitle.Length > 0
                && (keywordTooltipRect.Contains(pointer) || keywordHoverBridge.Contains(pointer)
                    || keywordSourceRect.Contains(pointer) || keywordOwnerRect.Contains(pointer));
            if (!keep)
            {
                hoveredKeywordTitle = "";
                hoveredKeywordBody = "";
                keywordTooltipRect = default(Rect);
                keywordHoverBridge = default(Rect);
                keywordSourceRect = default(Rect);
                keywordOwnerRect = default(Rect);
                keywordKeepsCard = false;
            }
            hoveredTerrain = -1;
        }

        private void RegisterKeyword(Rect area, string title, string body, bool keepsCard = false)
        { RegisterKeyword(area, title, body, keepsCard, area); }

        private void RegisterKeyword(Rect area, string title, string body, bool keepsCard, Rect owner)
        {
            if (!area.Contains(Event.current.mousePosition)) return;
            hoveredKeywordTitle = title;
            hoveredKeywordBody = body;
            keywordSourceRect = area;
            keywordOwnerRect = owner;
            float x = area.xMax + 16;
            if (x + 420 > 1896) x = area.x - 436;
            x = Mathf.Clamp(x, 24, 1920 - 444);
            float y = Mathf.Clamp(area.center.y - 86, 78, 1080 - 194);
            keywordTooltipRect = new Rect(x, y, 420, 170);
            keywordHoverBridge = Rect.MinMaxRect(
                Mathf.Min(area.xMin, keywordTooltipRect.xMin) - 8,
                Mathf.Min(area.yMin, keywordTooltipRect.yMin) - 8,
                Mathf.Max(area.xMax, keywordTooltipRect.xMax) + 8,
                Mathf.Max(area.yMax, keywordTooltipRect.yMax) + 8);
            keywordKeepsCard = keepsCard;
        }

        private void RegisterStatusKeyword(Rect area, int index, int magnitude, int duration)
        {
            if (!area.Contains(Event.current.mousePosition)) return;
            RegisterKeyword(area, AbilityNames[index], KeywordDescriptions[index] + "\n현재 수치 " + magnitude
                + " · 남은 적 행동 " + duration + "회");
        }

        private void RegisterStatusListKeyword(Rect area, CombatStatusState statuses)
        {
            if (!area.Contains(Event.current.mousePosition)) return;
            RegisterKeyword(area, "적용 중인 상태 전체", DescribeStatuses(statuses, CombatStatusState.Capacity, true));
        }

        private void DrawKeywordTooltip()
        {
#if UNITY_EDITOR
            if (DiagnosticKeyword >= 0 && DiagnosticKeyword < KeywordDescriptions.Length)
            {
                hoveredKeywordTitle = AbilityNames[DiagnosticKeyword];
                hoveredKeywordBody = DiagnosticKeywordBody.Length > 0 ? DiagnosticKeywordBody : KeywordDescriptions[DiagnosticKeyword];
            }
#endif
            if (hoveredKeywordTitle.Length == 0) return;
            Rect panel = keywordTooltipRect.width > 0 ? keywordTooltipRect : new Rect(1450, 78, 420, 170);
            float x = panel.x, y = panel.y;
            Fill(panel, new Color(.075f, .068f, .12f, .97f));
            Border(panel, Gold, 12);
            Label(new Rect(x + 20, y + 12, panel.width - 40, 34), hoveredKeywordTitle, ui.Light);
            Line(new Vector2(x + 20, y + 52), new Vector2(panel.xMax - 20, y + 52), new Color(Gold.r, Gold.g, Gold.b, .45f));
            Label(new Rect(x + 20, y + 60, panel.width - 40, panel.height - 72), hoveredKeywordBody, ui.SmallLight);
        }

        private void DrawCardKeywords(Rect area, SkillVisual visual, bool light)
        { DrawCardKeywords(area, visual, light, area); }

        private void DrawCardKeywords(Rect area, SkillVisual visual, bool light, Rect owner)
        {
            int count = visual.KeywordNames == null ? 0 : visual.KeywordNames.Length;
            if (count == 0) return;
            float gap = 7;
            float width = Mathf.Min(116, (area.width - gap * (count - 1)) / count);
            for (int i = 0; i < count; i++)
            {
                Rect chip = new Rect(area.x + i * (width + gap), area.y, width, area.height);
                bool hover = chip.Contains(Event.current.mousePosition);
                Fill(chip, light ? new Color(1, 1, 1, hover ? .18f : .08f)
                    : new Color(Violet.r, Violet.g, Violet.b, hover ? .22f : .11f));
                Border(chip, hover ? Gold : new Color(Violet.r, Violet.g, Violet.b, .62f), 7);
                Label(new Rect(chip.x + 4, chip.y, chip.width - 8, chip.height), visual.KeywordNames[i],
                    light ? ui.SmallLight : ui.Small, true);
                RegisterKeyword(chip, visual.KeywordNames[i], visual.KeywordDetails[i], true, owner);
            }
        }

        private void DrawStatusChips(CombatStatusState statuses, Rect area, bool light, int maximum = 4)
        {
            if (statuses == null) return;
            int active = 0;
            for (int i = 0; i < CombatStatusState.Capacity; i++) if (statuses.Get((CombatStatusKind)i) > 0) active++;
            if (active == 0) return;
            int visible = active > maximum ? Mathf.Max(1, maximum - 1) : active;
            int slots = visible + (active > maximum ? 1 : 0);
            float gap = 5;
            float width = (area.width - gap * (slots - 1)) / slots;
            int shown = 0;
            for (int i = 0; i < CombatStatusState.Capacity && shown < visible; i++)
            {
                CombatStatusKind kind = (CombatStatusKind)i;
                int magnitude = statuses.Get(kind);
                if (magnitude <= 0) continue;
                int duration = statuses.Duration(kind);
                Rect chip = new Rect(area.x + shown * (width + gap), area.y, width, area.height);
                bool hover = chip.Contains(Event.current.mousePosition);
                Fill(chip, light ? new Color(1, 1, 1, hover ? .18f : .08f)
                    : new Color(Violet.r, Violet.g, Violet.b, hover ? .22f : .10f));
                bool debuff = i >= (int)CombatStatusKind.Burn && i <= (int)CombatStatusKind.Anchor
                    || i == (int)CombatStatusKind.Rupture;
                Border(chip, debuff ? Threat : Violet, 6);
                Label(new Rect(chip.x + 2, chip.y + 1, chip.width - 4, chip.height * .52f), StatusShortNames[i],
                    light ? ui.SmallLight : ui.Small, true);
                Label(new Rect(chip.x + 2, chip.y + chip.height * .43f, chip.width - 4, chip.height * .52f),
                    StatusValueLabel(magnitude, duration), light ? ui.SmallLight : ui.Small, true);
                RegisterStatusKeyword(chip, i, magnitude, duration);
                shown++;
            }
            if (active > maximum)
            {
                Rect more = new Rect(area.x + shown * (width + gap), area.y, width, area.height);
                bool hover = more.Contains(Event.current.mousePosition);
                Fill(more, new Color(Violet.r, Violet.g, Violet.b, hover ? .24f : .12f));
                Border(more, hover ? Gold : Violet, 6);
                Label(more, MoreStatusLabels[active - shown], light ? ui.SmallLight : ui.Small, true);
                RegisterStatusListKeyword(more, statuses);
            }
        }

        private void DrawTerrain()
        {
            for (int i = 0; i < battle.TerrainCount; i++)
            {
                BattleTerrainDefinition item = battle.GetTerrain(i);
                Vector2 p = FieldPoint(item.X, item.Y);
                float radius = (float)item.Radius * FieldUnit;
                Rect hover = new Rect(p.x - radius - 12, p.y - radius - 12, (radius + 12) * 2, (radius + 12) * 2);
                bool inspected = hover.Contains(Event.current.mousePosition);
                if (inspected) hoveredTerrain = i;
                if (item.Kind == BattleTerrainKind.Obstacle)
                {
                    Disc(p, radius, new Color(.12f, .11f, .17f, .96f));
                    Diamond(p, radius * .92f, inspected ? Gold : new Color(.56f, .53f, .66f), 2.1f);
                    Diamond(p, radius * .58f, new Color(.35f, .31f, .45f), 1.4f);
                    Line(p - Vector2.up * radius * .63f, p + Vector2.up * radius * .63f,
                        new Color(Gold.r, Gold.g, Gold.b, .42f), 1.2f);
                    RegisterKeyword(hover, "기록 기둥", "캐릭터와 적의 이동을 막습니다. 작도선은 기둥 위를 지나갈 수 있습니다.");
                }
                else
                {
                    Color color = battle.PrismCharged ? new Color(.78f, .96f, .93f) : new Color(.56f, .72f, .82f);
                    Disc(p, radius * .7f, new Color(color.r, color.g, color.b, .14f));
                    Diamond(p, radius, color, battle.PrismCharged ? 3 : 1.7f);
                    Ring(p, radius * .72f, new Color(color.r, color.g, color.b, .72f), 1.4f, .76f, .4f);
                    RegisterKeyword(hover, "굴절 프리즘",
                        "작도선이 프리즘을 통과하면 그 방출에서 적중한 모든 적에게 피해 +2를 적용합니다.");
                }
            }
        }

        private void DrawBattleTopBar()
        {
            Fill(new Rect(0, 0, 1920, 72), new Color(.055f, .05f, .09f, .88f));
            Line(new Vector2(32, 71), new Vector2(1888, 71), new Color(Gold.r, Gold.g, Gold.b, .42f));
            Label(new Rect(35, 14, 360, 42), floorTitle, ui.SmallLight);
            Label(new Rect(815, 10, 290, 48), turnText, ui.Light, true);
            if (battle.PrismCharged)
                Label(new Rect(1120, 14, 310, 40), "굴절 활성 · 피해 +2", ui.SmallLight, true);
            if (ui.Button(new Rect(1580, 16, 120, 40), showBattleDetails ? "간략" : "상세"))
            { showBattleDetails = !showBattleDetails; if (!showBattleDetails) showEquation = false; }
            if (ui.Button(new Rect(1712, 16, 104, 40), "메뉴")) paused = true;
            if (ui.Button(new Rect(1828, 16, 58, 40), "?")) OpenHelp();
        }
    }
}

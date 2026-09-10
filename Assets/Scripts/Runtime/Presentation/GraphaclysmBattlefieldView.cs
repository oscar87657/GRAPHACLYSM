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
            "자가 적중이 발생하면 체력을 즉시 회복합니다.",
            "대상에게 걸린 해로운 상태 또는 강화를 제거합니다."
        };

        private string hoveredKeywordTitle = "", hoveredKeywordBody = "";
        private int hoveredTerrain = -1;

        private void BeginBattleHoverFrame()
        {
            hoveredKeywordTitle = "";
            hoveredKeywordBody = "";
            hoveredTerrain = -1;
        }

        private void RegisterKeyword(Rect area, string title, string body)
        {
            if (!area.Contains(Event.current.mousePosition)) return;
            hoveredKeywordTitle = title;
            hoveredKeywordBody = body;
        }

        private void DrawKeywordTooltip()
        {
#if UNITY_EDITOR
            if (DiagnosticKeyword >= 0 && DiagnosticKeyword < KeywordDescriptions.Length)
            {
                hoveredKeywordTitle = AbilityNames[DiagnosticKeyword];
                hoveredKeywordBody = KeywordDescriptions[DiagnosticKeyword];
            }
#endif
            if (hoveredKeywordTitle.Length == 0) return;
            Vector2 pointer = Event.current.mousePosition;
            float x = Mathf.Clamp(pointer.x + 24, 24, 1920 - 374);
            float y = Mathf.Clamp(pointer.y - 126, 78, 1080 - 154);
            Rect panel = new Rect(x, y, 350, 126);
            Fill(panel, new Color(.075f, .068f, .12f, .97f));
            Border(panel, Gold, 12);
            Label(new Rect(x + 18, y + 10, 314, 32), hoveredKeywordTitle, ui.Light);
            Label(new Rect(x + 18, y + 43, 314, 70), hoveredKeywordBody, ui.SmallLight);
        }

        private void DrawCardKeywords(Rect area, SkillVisual visual, bool light)
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
                RegisterKeyword(chip, visual.KeywordNames[i], visual.KeywordDetails[i]);
            }
        }

        private void DrawStatusChips(CombatStatusState statuses, Rect area, bool light, int maximum = 4)
        {
            if (statuses == null) return;
            int shown = 0;
            for (int i = 0; i < CombatStatusState.Capacity && shown < maximum; i++)
            {
                if (statuses.Get((CombatStatusKind)i) <= 0) continue;
                Rect chip = new Rect(area.x + shown * 72, area.y, 66, area.height);
                bool hover = chip.Contains(Event.current.mousePosition);
                Fill(chip, light ? new Color(1, 1, 1, hover ? .18f : .08f)
                    : new Color(Violet.r, Violet.g, Violet.b, hover ? .22f : .10f));
                Border(chip, i >= (int)CombatStatusKind.Burn && i <= (int)CombatStatusKind.Anchor ? Threat : Violet, 6);
                Label(new Rect(chip.x + 3, chip.y, chip.width - 6, chip.height), AbilityNames[i],
                    light ? ui.SmallLight : ui.Small, true);
                RegisterKeyword(chip, AbilityNames[i], KeywordDescriptions[i]);
                shown++;
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

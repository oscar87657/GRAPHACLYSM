using Graphaclysm.Core.Combat;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private static string[] KeywordDescriptions => KeywordText.All;
        private static class KeywordText
        {
        public static readonly string[] All =
        {
            "받는 피해를 먼저 막습니다. 남은 양은 지속시간이 끝나면 사라집니다.",
            "작도가 주는 피해가 수치만큼 늘어납니다.",
            "적 행동이 끝나면 수치만큼 체력을 회복합니다.",
            "적 행동 전에 수치만큼 피해를 받습니다.",
            "주는 공격·작도 피해가 수치만큼 줄어듭니다.",
            "작도에 받는 피해가 수치만큼 늘어납니다.",
            "이동과 끌어당김을 막습니다. 공격은 막지 않습니다.",
            "다음 이동의 비용을 없앱니다. 이동하면 사라집니다.",
            "체력 피해를 받으면 공격자에게 수치만큼 반격합니다.",
            "다음 작도의 피해가 늘어납니다. 방출하면 사라집니다.",
            "적 행동 직전에 수치만큼 보호막을 얻습니다.",
            "다음 작도에 받는 피해가 수치만큼 늘어나고 사라집니다.",
            "체력을 회복합니다.",
            "자신의 해로운 상태 또는 적의 이로운 상태를 제거합니다.",
            "새로 걸리는 해로운 상태를 한 번 막습니다.",
            "다음 작도 피해 중 수치만큼이 보호막을 통과합니다.",
            "새로 얻는 보호막이 수치만큼 줄어듭니다.",
            "이동하면 수치만큼 피해를 받습니다. 내 행동·적 행동마다 한 번씩.",
            "다음 끌어당김 거리가 수치만큼 늘어납니다. 이동하면 사라집니다.",
            "맞힌 적을 내 쪽으로 당깁니다. 고정된 적은 움직이지 않습니다."
        };
        }

        private string hoveredKeywordTitle = "", hoveredKeywordBody = "";
        private Rect keywordTooltipRect, keywordHoverBridge, keywordSourceRect, keywordOwnerRect;
        private bool keywordKeepsCard;
        private bool keywordPinned, keywordDismissRelease;

        private void HandleKeywordInput()
        {
            Event input = Event.current;
            if (keywordDismissRelease && input.type == EventType.MouseUp)
            { keywordDismissRelease = false; input.Use(); return; }
            if (!keywordPinned)
            {
                if (hoveredKeywordTitle.Length == 0 || !keywordTooltipRect.Contains(input.mousePosition)) return;
                if (input.type == EventType.MouseDown && input.button == 0)
                { keywordPinned=true;FollowTooltipKeyword(input.mousePosition);input.Use(); }
                else if(input.isMouse || input.type==EventType.ScrollWheel) input.Use();
                return;
            }
            if (input.type == EventType.KeyDown && input.keyCode == KeyCode.Escape)
            { CloseKeyword(); input.Use(); return; }
            if (input.type == EventType.MouseDown)
            {
                Rect close = new Rect(keywordTooltipRect.xMax - 48, keywordTooltipRect.y + 8, 40, 36);
                if (!keywordTooltipRect.Contains(input.mousePosition) || close.Contains(input.mousePosition))
                { CloseKeyword(); keywordDismissRelease = true; input.Use(); return; }
                if(input.button==0) FollowTooltipKeyword(input.mousePosition);
            }
            if (keywordTooltipRect.Contains(input.mousePosition) && (input.isMouse || input.type==EventType.ScrollWheel)) input.Use();
        }

        private void CloseKeyword()
        {
            keywordPinned = false;
            hoveredKeywordTitle = hoveredKeywordBody = "";
            keywordTooltipRect = keywordHoverBridge = keywordSourceRect = keywordOwnerRect = default(Rect);
            keywordKeepsCard = false;
        }
        private int hoveredTerrain = -1;
        private static readonly string[] StatusShortNames =
        { "보호", "집중", "재생", "잔불", "약화", "노출", "고정", "경쾌", "가시", "추진", "요새", "균열", "수호", "관통", "방벽", "상처", "유도" };
        private static readonly string[,] StatusValueLabels = BuildStatusValueLabels();
        private static readonly string[] MoreStatusLabels =
        { "+0", "+1", "+2", "+3", "+4", "+5", "+6", "+7", "+8", "+9", "+10", "+11", "+12", "+13", "+14", "+15", "+16", "+17" };

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
            bool keep = keywordPinned || hoveredKeywordTitle.Length > 0
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
            if (!GUI.enabled || !area.Contains(Event.current.mousePosition)
                || Event.current.type != EventType.MouseDown || Event.current.button != 0) return;
            OpenKeyword(area, title, body, keepsCard, owner);
            keywordPinned = true;
            handPressedIndex = -1;
            Event.current.Use();
        }

        // Action buttons retain a hover hint: clicking them still performs their action.
        private void RegisterHoverHint(Rect area, string title, string body)
        {
            if (!GUI.enabled || keywordPinned || !area.Contains(Event.current.mousePosition)) return;
            OpenKeyword(area,title,body,false,area);
        }

        private void OpenKeyword(Rect area, string title, string body, bool keepsCard, Rect owner)
        {
            hoveredKeywordTitle = title;
            hoveredKeywordBody = body;
            keywordSourceRect = area;
            keywordOwnerRect = owner;
            float x = area.xMax + 16;
            if (x + 420 > 1896) x = area.x - 436;
            x = Mathf.Clamp(x, 24, 1920 - 444);
            effectMeasure.text=body;
            float height=Mathf.Clamp(ui.SmallLight.CalcHeight(effectMeasure,380)+76,118,650);
            float y = Mathf.Clamp(area.center.y - height*.5f, 78, 1056-height);
            keywordTooltipRect = new Rect(x, y, 420, height);
            keywordHoverBridge = Rect.MinMaxRect(
                Mathf.Min(area.xMin, keywordTooltipRect.xMin) - 8,
                Mathf.Min(area.yMin, keywordTooltipRect.yMin) - 8,
                Mathf.Max(area.xMax, keywordTooltipRect.xMax) + 8,
                Mathf.Max(area.yMax, keywordTooltipRect.yMax) + 8);
            keywordKeepsCard = keepsCard;
        }

        private void RegisterStatusKeyword(Rect area, int index, int magnitude, int duration)
        {
            if (!area.Contains(Event.current.mousePosition) || Event.current.type != EventType.MouseDown) return;
            int ability=StatusAbilityIndex(index);
            string description=index==(int)CombatStatusKind.Haste && battle!=null && battle.HasStatusRules
                ? "다음 일반 이동 거리가 0.9 늘어납니다. 이동하면 사라집니다." : KeywordDescriptions[ability];
            RegisterKeyword(area, AbilityNames[ability], description + "\n현재 수치 " + magnitude
                + " · 남은 적 행동 " + duration + "회");
        }

        private void RegisterStatusListKeyword(Rect area, CombatStatusState statuses)
        {
            if (!GUI.enabled || !area.Contains(Event.current.mousePosition) || Event.current.type != EventType.MouseDown) return;
            RegisterKeyword(area, "적용 중인 상태 전체", DescribeStatuses(statuses, CombatStatusState.Capacity, true));
            int count=0;
            for(int i=0;i<CombatStatusState.Capacity;i++) if(statuses.Get((CombatStatusKind)i)>0) count++;
            keywordTooltipRect.height=Mathf.Max(170,80+count*27);
            keywordTooltipRect.y=Mathf.Clamp(keywordTooltipRect.y,78,1056-keywordTooltipRect.height);
        }

        private void DrawKeywordTooltip()
        {
            if (combatSkillTargeting && run != null && run.Phase == Graphaclysm.Application.RunPhase.Battle) return;
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
            Label(new Rect(x + 20, y + 12, panel.width - 80, 34), hoveredKeywordTitle, ui.Light);
            if (keywordPinned) Label(new Rect(panel.xMax - 48,y + 8,40,36),"×",ui.Light,true);
            Line(new Vector2(x + 20, y + 52), new Vector2(panel.xMax - 20, y + 52), new Color(Gold.r, Gold.g, Gold.b, .45f));
            DrawExplainedText(new Rect(x + 20, y + 60, panel.width - 40, panel.height - 72), hoveredKeywordBody, ui.SmallLight,
                excludedTitle:hoveredKeywordTitle);
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
                bool debuff = CombatStatusState.IsDebuff(kind);
                Border(chip, debuff ? Threat : new Color(.35f,.82f,.75f), debuff ? 1 : 8);
                DrawStatusIcon(new Vector2(chip.x+11,chip.y+10),kind,debuff?Threat:new Color(.35f,.82f,.75f));
                Label(new Rect(chip.x + 22, chip.y + 1, chip.width - 24, chip.height * .52f), StatusShortNames[i],
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

        private static void DrawStatusIcon(Vector2 p,CombatStatusKind kind,Color color)
        {
            const float r=7;
            switch(kind)
            {
                case CombatStatusKind.Shield: case CombatStatusKind.Ward: case CombatStatusKind.FrailShield: case CombatStatusKind.Fortify:
                    Line(p+new Vector2(-r,-r),p+new Vector2(r,-r),color,2);
                    Line(p+new Vector2(-r,-r),p+new Vector2(-r,2),color,2);
                    Line(p+new Vector2(r,-r),p+new Vector2(r,2),color,2);
                    Line(p+new Vector2(-r,2),p+new Vector2(0,r),color,2);
                    Line(p+new Vector2(r,2),p+new Vector2(0,r),color,2);
                    if(kind==CombatStatusKind.Ward) Diamond(p,3,color,1);
                    if(kind==CombatStatusKind.Fortify) Line(p+new Vector2(-4,-10),p+new Vector2(4,-10),color,2);
                    if(kind==CombatStatusKind.FrailShield) Line(p+new Vector2(-3,-7),p+new Vector2(2,7),color,2);
                    break;
                case CombatStatusKind.Regeneration:
                    Line(p+Vector2.left*r,p+Vector2.right*r,color,3);Line(p+Vector2.up*r,p+Vector2.down*r,color,3);break;
                case CombatStatusKind.Guidance: case CombatStatusKind.Pierce: case CombatStatusKind.Haste:
                    Line(p+Vector2.left*r,p+Vector2.right*r,color,2);
                    Line(p+new Vector2(2,-5),p+Vector2.right*r,color,2);Line(p+new Vector2(2,5),p+Vector2.right*r,color,2);
                    if(kind==CombatStatusKind.Guidance) {Line(p+Vector2.left*r,p+new Vector2(-2,-5),color,2);Line(p+Vector2.left*r,p+new Vector2(-2,5),color,2);}
                    if(kind==CombatStatusKind.Pierce) Line(p+new Vector2(0,-7),p+new Vector2(0,7),color,2);
                    if(kind==CombatStatusKind.Haste) Line(p+new Vector2(-7,5),p+new Vector2(-2,5),color,2);
                    break;
                case CombatStatusKind.Anchor:
                    Line(p+Vector2.up*r,p+Vector2.down*r,color,2);Line(p+new Vector2(-r,4),p+Vector2.down*r,color,2);Line(p+new Vector2(r,4),p+Vector2.down*r,color,2);break;
                case CombatStatusKind.Burn:
                    Line(p+new Vector2(-6,5),p+new Vector2(-1,-8),color,2);Line(p+new Vector2(-1,-8),p+new Vector2(6,5),color,2);Line(p+new Vector2(-6,5),p+new Vector2(6,5),color,2);break;
                case CombatStatusKind.Wound: case CombatStatusKind.Rupture: case CombatStatusKind.Weaken:
                    Line(p+new Vector2(-3,-8),p+new Vector2(2,-1),color,2);Line(p+new Vector2(2,-1),p+new Vector2(-2,2),color,2);Line(p+new Vector2(-2,2),p+new Vector2(3,8),color,2);
                    if(kind==CombatStatusKind.Wound) Line(p+new Vector2(-7,-4),p+new Vector2(-4,6),color,2);
                    if(kind==CombatStatusKind.Weaken) Line(p+new Vector2(-7,6),p+new Vector2(7,6),color,2);
                    break;
                default:
                    Ring(p,r,color,1);Line(p+Vector2.left*(r+2),p+Vector2.right*(r+2),color,1);
                    if(kind!=CombatStatusKind.Exposure) Line(p+Vector2.up*(r+2),p+Vector2.down*(r+2),color,1);
                    if(kind==CombatStatusKind.Thorns) Diamond(p,r+2,color,1);
                    break;
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
                else if (item.Kind == BattleTerrainKind.Prism)
                {
                    Color color = battle.PrismCharged ? new Color(.78f, .96f, .93f) : new Color(.56f, .72f, .82f);
                    Disc(p, radius * .7f, new Color(color.r, color.g, color.b, .14f));
                    Diamond(p, radius, color, battle.PrismCharged ? 3 : 1.7f);
                    Ring(p, radius * .72f, new Color(color.r, color.g, color.b, .72f), 1.4f, .76f, .4f);
                    RegisterKeyword(hover, "굴절 프리즘",
                        "작도선이 프리즘을 통과하면 그 방출에서 적중한 모든 적에게 피해 +2를 적용합니다.");
                }
                else
                {
                    bool spent = battle.IsDeviceSpent(i), touched = battle.TraceTouchesDevice(i);
                    bool pulse = item.Kind == BattleTerrainKind.Pulse;
                    Color color = spent ? Muted : pulse ? Threat : item.Kind == BattleTerrainKind.Aegis ? new Color(.5f,.85f,.8f) : Gold;
                    bool active = pulse && battle.IsPulseActive(i);
                    Disc(p, radius, new Color(color.r, color.g, color.b, active ? .25f : .10f));
                    Ring(p, radius, color, touched && !spent ? 3 : 1.5f);
                    string name = pulse ? "맥동 균열" : item.Kind == BattleTerrainKind.Aegis ? "보호 기점" : "파열 결정";
                    string caption = spent ? "소진" : pulse ? active ? touched ? "방출로 봉쇄" : "이번 턴 6"
                        : battle.Turn % 2 == 0 ? "봉쇄됨" : "다음 턴 6" : touched ? "방출로 활성" : "";
                    Label(new Rect(p.x-72,p.y+radius+4,144,26),caption,ui.SmallLight,true);
                    if (!pulse) Diamond(p, radius * .58f, color, 2);
                    if (item.Kind == BattleTerrainKind.Capacitor && !spent && (touched || inspected))
                        Ring(p, (float)BattleSession.DeviceBlastRadius * FieldUnit, new Color(color.r,color.g,color.b,.4f),1);
                    RegisterKeyword(hover, name, pulse
                        ? "짝수 턴의 적 행동 직전, 원 안의 자신과 적 모두 피해 6. 이번 작도로 균열을 통과하면 그 턴의 맥동을 봉쇄합니다. 응축은 봉쇄하지 않습니다."
                        : item.Kind == BattleTerrainKind.Aegis
                            ? "작도로 통과하면 자가 적중과 별도로 보호막 5. 전투당 한 번만 작동하며 이동을 막지 않습니다."
                            : "작도로 통과하면 반경 2.1 안의 모든 적에게 피해 6. 카드 능력·공명을 복제하지 않습니다. 전투당 한 번만 작동합니다.");
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

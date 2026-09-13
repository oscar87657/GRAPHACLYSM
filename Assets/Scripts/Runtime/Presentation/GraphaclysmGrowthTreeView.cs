using System;
using Graphaclysm.Core.Runs;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private int growthHoverNode = -1;
        private string growthSearch = "", growthSearchCached = null;
        private bool[] growthSearchMatches;
        private int growthSearchCount;
        private GUIStyle growthSearchStyle;
        private void UpdateGrowthSearch(RunGrowthState growth)
        {
            if (growthSearchMatches != null && growthSearchCached == growthSearch) return;
            growthSearchMatches = growthSearchMatches ?? new bool[growth.NodeCount];
            growthSearchCached = growthSearch; growthSearchCount = 0;
            for (int i = 0; i < growth.NodeCount; i++)
            { growthSearchMatches[i] = GrowthUiCatalog.Matches(growth.GetNode(i), growthSearch); if (growthSearchMatches[i]) growthSearchCount++; }
        }
        private void NextGrowthSearch(RunGrowthState growth, int direction)
        {
            UpdateGrowthSearch(growth);
            for (int step = 1; step <= growth.NodeCount; step++)
            {
                int index = (Math.Max(0, growthFocusedNode) + step * direction + growth.NodeCount * 2) % growth.NodeCount;
                if (growthSearchMatches[index]) { FocusGrowthNode(growth, index); return; }
            }
        }
#if UNITY_EDITOR
        public int DiagnosticGrowthHover { get; set; } = -1;
        public int DiagnosticGrowthTerm { get; set; } = -1;
#endif
        private void EnsureGrowthLayout(RunGrowthState growth)
        {
            if (growthLayout != null && growthLayoutOwner == growth) return;
            growthLayoutOwner = growth;
            growthSearchMatches = null; growthSearchCached = null;
            growthLayout = new GrowthTreeLayout(growth);
            growthAncestors = new bool[growth.NodeCount];
            FocusGrowthNode(growth, growthLayout.Root);
        }

        private void FocusGrowthNode(RunGrowthState growth, int index)
        {
            if (index < 0 || index >= growth.NodeCount) return;
            if (growthLayout == null || growthLayoutOwner != growth) EnsureGrowthLayout(growth);
            growthFocusedNode = index;
            growthPan = growthLayout.Positions[index] + new Vector2(0, index == growthLayout.Root ? -300 : 65);
            growthZoom = .82f;
            UpdateGrowthAncestors();
        }

        private void UpdateGrowthAncestors()
        {
            Array.Clear(growthAncestors, 0, growthAncestors.Length);
            if (growthFocusedNode >= 0) MarkGrowthAncestors(growthFocusedNode);
        }

        private void MarkGrowthAncestors(int index)
        {
            if (growthAncestors[index]) return;
            growthAncestors[index] = true;
            foreach (int parent in growthLayout.Parents[index]) MarkGrowthAncestors(parent);
        }

        private void FitGrowthTree()
        {
            growthPan = growthLayout.Bounds.center;
            growthZoom = Mathf.Min(1310f / growthLayout.Bounds.width, 790f / growthLayout.Bounds.height);
            growthFocusedNode = -1;
            UpdateGrowthAncestors();
        }

        private void HandleGrowthCanvasInput(Rect viewport, Event input)
        {
            if (input.type == EventType.MouseDown) growthDragDistance = 0;
            if (input.type == EventType.ScrollWheel && viewport.Contains(input.mousePosition))
            {
                if (input.control)
                {
                    Vector2 local = input.mousePosition - viewport.position;
                    Vector2 world = growthPan + (local - viewport.size * .5f) / growthZoom;
                    growthZoom = Mathf.Clamp(growthZoom * (input.delta.y > 0 ? .88f : 1.12f), .12f, 1.3f);
                    growthPan = world - (local - viewport.size * .5f) / growthZoom;
                }
                else growthPan += new Vector2(input.shift ? input.delta.y * 42f : 0, input.shift ? 0 : input.delta.y * 42f) / growthZoom;
                input.Use();
            }
            else if (input.type == EventType.MouseDown && (input.button == 0 || input.button == 2) && viewport.Contains(input.mousePosition))
            { growthDragging = true; growthDragDistance = 0; }
            else if (input.type == EventType.MouseDrag && growthDragging)
            {
                growthDragDistance += input.delta.magnitude;
                growthPan -= input.delta / growthZoom;
                input.Use();
            }
            else if (input.type == EventType.MouseUp && growthDragging)
            {
                growthDragging = false;
                if (growthDragDistance > 5) { GUIUtility.hotControl = 0; input.Use(); }
            }
            Rect bounds = growthLayout.Bounds;
            growthPan.x = Mathf.Clamp(growthPan.x, bounds.xMin - 100, bounds.xMax + 100);
            growthPan.y = Mathf.Clamp(growthPan.y, bounds.yMin - 100, bounds.yMax + 100);
        }

        private void DrawGreatConstellation(RunGrowthState growth, Vector2 viewportSize)
        {
            growthHoverNode = -1;
            for (int tier = 1; !growth.IsStyleTree && tier <= 5; tier++)
            {
                float bottom = float.NegativeInfinity;
                string representative = null;
                for (int i=0;i<growth.NodeCount;i++)
                    if (GrowthTreePaths.Tier(growth.GetNode(i).Id)==tier && growthLayout.Positions[i].y>bottom)
                    { bottom=growthLayout.Positions[i].y; representative=growth.GetNode(i).Id; }
                if (representative==null) continue;
                float y=GrowthWorldToCanvas(new Vector2(0,bottom+145),viewportSize).y;
                if (y<0 || y>viewportSize.y-36) continue;
                Fill(new Rect(0,y,viewportSize.x,1),new Color(.53f,.43f,.7f,.4f));
                Rect tag=new Rect(12,y+5,290,28); Fill(tag,new Color(.03f,.025f,.055f,.96f));
                Label(tag,tier+"단계 · 총 "+GrowthTreePaths.RequiredInvestment(representative)+"점 투자부터",ui.SmallLight);
            }
            for (int target = 0; target < growth.NodeCount; target++)
            {
                foreach (int parent in growthLayout.Parents[target])
                {
                    bool focus = growthAncestors[target] && growthAncestors[parent];
                    // Do not draw an old prerequisite twice when another parent already leads through it.
                    if (growthLayout.RedundantEdges[target,parent]) continue;
                    // The overview shows a connected tree spine. Selecting a node expands every real dependency and alternative.
                    if (!focus && parent != growthLayout.PrimaryParents[target]) continue;
                    Color color = focus ? new Color(.88f, .77f, 1f, .95f)
                        : growth.IsUnlocked(target) && growth.IsUnlocked(parent) ? Gold
                        : growth.IsUnlocked(parent) ? new Color(.68f, .54f, .87f, .85f)
                        : new Color(.39f, .37f, .49f, .70f);
                    if (growthFocusedNode >= 0 && !focus) color.a *= .48f;
                    Vector2 a = growthLayout.Positions[parent], b = growthLayout.Positions[target];
                    if(growth.IsStyleTree)
                    {
                        Vector2 direction=(b-a).normalized;
                        DrawGrowthWorldLine(a+direction*80,b-direction*85,viewportSize,color,focus?3.2f:2f);
                        continue;
                    }
                    float channel = b.x + (a.x < b.x ? -140 : 140);
                    float width = focus ? 3.2f : growth.IsUnlocked(parent) ? 2.4f : 1.4f;
                    Vector2 exit = a + new Vector2(0,-105), entry = b + new Vector2(0,105);
                    DrawGrowthWorldLine(a,exit,viewportSize,color,width);
                    DrawGrowthWorldLine(exit,new Vector2(channel,exit.y),viewportSize,color,width);
                    DrawGrowthWorldLine(new Vector2(channel,exit.y),new Vector2(channel,entry.y),viewportSize,color,width);
                    DrawGrowthWorldLine(new Vector2(channel,entry.y),entry,viewportSize,color,width);
                    DrawGrowthWorldLine(entry,b,viewportSize,color,width);
                }
            }
            for (int index = 0; index < growth.NodeCount; index++)
            {
                Vector2 point = GrowthWorldToCanvas(growthLayout.Positions[index], viewportSize);
                if (point.x < -110 || point.y < -90 || point.x > viewportSize.x + 110 || point.y > viewportSize.y + 90) continue;
                DrawGrowthConstellationNode(index, point, viewportSize, growth);
            }
        }

        private void DrawGrowthConstellationNode(int index, Vector2 center, Vector2 viewportSize, RunGrowthState growth)
        {
            GrowthNodeDefinition node = growth.GetNode(index);
            bool acquired = growth.IsUnlocked(index), equipped = growth.IsEquipped(index);
            bool focused = index == growthFocusedNode;
            bool landmark = growthLayout.Landmarks[index] || index == growthLayout.Root;
            float radius = Mathf.Max(5, (landmark ? 36 : 22) * growthZoom);
            Color color = equipped || acquired ? Gold : growth.CanPurchase(index) ? new Color(.88f, .82f, 1f)
                : new Color(.55f, .52f, .65f);
            if (IsGrowthExclusiveLocked(index, growth)) color = new Color(.46f, .26f, .32f);
            if (growthSearchMatches != null && !growthSearchMatches[index]) color.a *= .3f;
            bool working = !run.IsRetiredUltimateNode(index) && Graphaclysm.Application.GrowthRuntimeSupport.HasEffect(node, run.EffectiveApproach,run.HasEconomy);
            if (growthZoom >= .7f)
                Label(new Rect(center.x-60,center.y-radius-29,120,25),growth.IsStyleTree?(index==0?"출발":(index-1)%5==2?"기술 각성":(index-1)%5==4?"궁극 각성":(index-1)%5==3?"궁극 강화":"기술 강화"):working ? "전투 효과" : "연결 대기",ui.SmallLight,true);
            if (focused) Disc(center, radius + 10, new Color(.69f, .48f, 1f, .24f));
            Disc(center, radius + 2, color);
            Disc(center, Mathf.Max(2, radius - (acquired ? 3 : 2)), acquired
                ? new Color(.30f, .25f, .17f) : new Color(.08f, .065f, .13f));
            if (landmark) DrawGrowthCanvasDiamond(center, radius * .66f, color, 2, viewportSize);
            if(growth.IsStyleTree && index>0 && node.Cost==2)DrawGrowthCanvasDiamond(center,radius*.86f,color,1,viewportSize);
            if (growthZoom >= .55f)
                Label(new Rect(center.x - 23, center.y - 16, 46, 32), acquired ? "✓" : node.Cost.ToString(), ui.SmallLight, true);
            if (growthZoom >= .55f && growthLayout.Parents[index].Length > 1)
                Label(new Rect(center.x + radius - 3, center.y - radius - 15, 24, 25), "+", ui.SmallLight, true);
            if (growthZoom >= .55f || growth.IsStyleTree&&growthZoom>=.42f || landmark && growthZoom >= .28f || index == growthLayout.Root)
            {
                Rect label = new Rect(center.x - 103, center.y + radius + 7, 206, 39);
                Fill(label, new Color(.024f, .024f, .047f, .94f));
                Label(label, GrowthCurrentTitle.For(node), ui.SmallLight, true);
            }
            float hitRadius = Mathf.Max(9, radius + 5);
            Rect hit = new Rect(center.x - hitRadius, center.y - hitRadius, hitRadius * 2, hitRadius * 2);
            Rect nameHit = new Rect(center.x - 103, center.y + radius + 7, 206, 39);
            if ((GUI.Button(hit, GUIContent.none, GUIStyle.none)
                || (growthZoom >= .55f || growth.IsStyleTree&&growthZoom>=.42f) && GUI.Button(nameHit, GUIContent.none, GUIStyle.none)) && growthDragDistance <= 5)
            { growthFocusedNode = index; UpdateGrowthAncestors(); PlayClick(); }
        }

        private void DrawGrowthTree()
        {
            var growth = run.Growth;
            EnsureGrowthLayout(growth);
            if (growthPlanOpen) { DrawGrowthPlan(); return; }
            bool canEdit = run.CanEditGrowth;
            Fill(new Rect(0, 0, 1920, 1080), new Color(.035f, .032f, .065f, 1));
            Label(new Rect(48, 24, 540, 55), growth.IsSpecialized?ApproachGrowthCatalog.Names[(int)growth.SpecializedApproach]+" · "+growth.NodeCount+"노드":"원정 성좌", ui.PageTitleLight);
            Label(new Rect(48, 84, 520, 36), "이번 원정만 적용 · 숙련 " + run.MasteryRank + " / 5", ui.Light);
            GUI.SetNextControlName("GrowthSearch");
            if (growthSearchStyle == null) growthSearchStyle = new GUIStyle(GUI.skin.textField) { font = ui.Light.font, fontSize = 26, alignment = TextAnchor.MiddleLeft };
            growthSearch = GUI.TextField(new Rect(580, 84, 420, 38), growthSearch, 60, growthSearchStyle);
            UpdateGrowthSearch(growth);
            Label(new Rect(1008, 84, 135, 36), "검색 " + growthSearchCount + "개", ui.SmallLight);
            if (ui.Button(new Rect(1140, 84, 75, 38), "이전")) NextGrowthSearch(growth, -1);
            if (ui.Button(new Rect(1220, 84, 75, 38), "다음")) NextGrowthSearch(growth, 1);
            if (ui.Button(new Rect(1300, 84, 100, 38), "지우기")) { growthSearch = ""; GUI.FocusControl(null); }
            if (ui.Button(new Rect(1270, 35, 110, 46), "시작점")) FocusGrowthNode(growth, growthLayout.Root);
            if (ui.Button(new Rect(1010,35,245,46),"전투 효과 노드 찾기"))
                for (int step=1;step<=growth.NodeCount;step++)
                {
                    int next=(Math.Max(0,growthFocusedNode)+step)%growth.NodeCount;
                    if (!Graphaclysm.Application.GrowthRuntimeSupport.HasEffect(growth.GetNode(next),run.EffectiveApproach,run.HasEconomy)) continue;
                    growthSearch=""; FocusGrowthNode(growth,next); break;
                }
            if (ui.Button(new Rect(1390, 35, 120, 46), "전체 보기")) FitGrowthTree();
            if (ui.Button(new Rect(1520, 35, 55, 46), "－")) growthZoom = Mathf.Max(.12f, growthZoom - .1f);
            Label(new Rect(1578, 40, 65, 35), Mathf.RoundToInt(growthZoom * 100) + "%", ui.SmallLight, true);
            if (ui.Button(new Rect(1645, 35, 55, 46), "＋")) growthZoom = Mathf.Min(1.3f, growthZoom + .1f);
            if (ui.Button(new Rect(1740, 35, 125, 46), "닫기  G", true)) { growthOpen = false; return; }

            Rect viewport = new Rect(42, 142, 1380, 870);
            Fill(viewport, new Color(.024f, .024f, .047f, 1));
            HandleGrowthCanvasInput(viewport, Event.current);
            GUI.BeginGroup(viewport);
            DrawGreatConstellation(growth, viewport.size);
            GUI.EndGroup();
#if UNITY_EDITOR
            if (DiagnosticGrowthHover >= 0) growthHoverNode = DiagnosticGrowthHover;
#endif
            // Click selects the inspector; only explicit buttons mutate growth.
            Border(viewport, new Color(.38f, .33f, .46f, .8f), 12);
            Label(new Rect(60, 1020, 960, 36), "클릭 설명 · 드래그/휠 이동 · Ctrl+휠 확대 · 검색은 이름/기능/효과", ui.SmallLight);
            if (ui.Button(new Rect(1040, 1020, 360, 38), "선택 노드의 선행 경로 계획", enabled: growthFocusedNode >= 0)) OpenGrowthPlan();

            Rect detail = new Rect(1442, 142, 426, 870);
            Fill(detail, new Color(.083f, .071f, .126f, 1)); Border(detail, new Color(Gold.r, Gold.g, Gold.b, .5f), 16);
            Label(new Rect(1466, 162, 378, 35),growth.HasPointLimit?"투자 "+growth.SpentPoints+" / 최대 9점 · 남은 "+growth.Points+"점":"성장점 " + growth.Points + "  ·  지금까지 " + growth.SpentPoints + "점 투자", ui.Light);
            Label(new Rect(1466, 205, 378, 75),run.HasExpeditionSupplies?run.GrowthTreeProgressText:growth.HasPointLimit?(growth.TotalPointBudget>=9?"최대 성장점 확보 · 추가 지급 없음":"앞으로 얻을 수 있는 성장점 "+(9-growth.TotalPointBudget)+"점")+"\n반환은 사용한 점수만 돌려줍니다.\n기술 각성 1개 + 궁극 각성 1개 선택 가능": growth.IsSpecialized?"습득하면 자동 적용 · 추가 장착 불필요\n이번 원정은 출발 전투방식을 유지합니다.\n배타 선택은 반환 후 변경할 수 있습니다.":"핵심  " + EquippedGrowthName(growth, GrowthEquipKind.Identity, 0)
                + "\n기술  " + EquippedGrowthName(growth, GrowthEquipKind.CombatForm, 0) + " / "
                + EquippedGrowthName(growth, GrowthEquipKind.CombatForm, 1)
                + "\n궁극  " + EquippedGrowthName(growth, GrowthEquipKind.Ultimate, 0), ui.SmallLight);
            if (growthFocusedNode >= 0 && growthFocusedNode < growth.NodeCount)
                DrawGrowthDetail(growth, canEdit);
            else
            {
                Label(new Rect(1466, 332, 378, 73), "하나의 성장 경로", ui.HeadingLight);
                Label(new Rect(1466, 425, 378, 240), growth.IsSpecialized ? "맨 아래 무료 출발 능력에서 위로 확장합니다.\n\n노드를 클릭해 설명을 확인하고 습득 버튼으로 구매하세요. 배타 갈래는 동시에 선택할 수 없습니다.\n\n드래그/휠 이동 · Ctrl+휠 확대/축소" : "맨 아래 교차점 채집에서 시작합니다.\n\n노드를 선택하면 필요한 경로가 밝아집니다. 서로 다른 능력이 만나는 길을 따라 자신만의 전투 방식을 완성하세요.\n\n상단의 + 버튼으로 확대할 수 있습니다.", ui.Light);
            }
            if (ui.Button(new Rect(1466, 951, 378, 40), "전체 재배분", enabled: canEdit && growth.SpentPoints > 0))
            { if (run.TryResetGrowth()) { PlayClick(); Refresh(); } }
        }

        private void DrawGrowthDetail(RunGrowthState growth, bool canEdit)
        {
            if(growth.IsSpecialized){DrawSpecializationDetail(growth,canEdit);return;}
            var node = growth.GetNode(growthFocusedNode);
            Label(new Rect(1466, 300, 378, 35), "단계 " + GrowthTreePaths.Tier(node.Id)
                + " · 누적 투자 " + GrowthTreePaths.RequiredInvestment(node.Id) + "점부터", ui.SmallLight);
            Label(new Rect(1466, 339, 378, 44), GrowthCurrentTitle.For(node), ui.Light);
            Label(new Rect(1466, 385, 378, 30), node.Name + " · " + node.Kind, ui.SmallLight);
            string approachEffect = Graphaclysm.Application.GrowthRuntimeSupport.Effect(node,run.EffectiveApproach,run.HasEconomy);
            if(run.IsRetiredUltimateNode(growthFocusedNode)) approachEffect="새 원정은 출발한 전투방식의 전용 궁극기를 사용합니다. 이 이전 궁극기 노드는 구매할 수 없습니다. 전용 트리 개편에서 새 분기로 교체됩니다.";
            DrawExplainedGrowthText(new Rect(1466, 420, 378, 181), approachEffect
                ?? "[고유 효과 연결 대기]\n" + GrowthUiCatalog.Get(node.Id).Summary);
#if UNITY_EDITOR
            if (DiagnosticGrowthTerm >= 0 && DiagnosticGrowthTerm < GrowthTerms.Length)
            {
                hoveredKeywordTitle = GrowthTerms[DiagnosticGrowthTerm];
                hoveredKeywordBody = GrowthMeanings[DiagnosticGrowthTerm];
                keywordTooltipRect = new Rect(1000, 420, 420, 170);
            }
#endif
            if(Graphaclysm.Application.NodeTrials.ApproachFor(node.Id) != Graphaclysm.Core.Combat.CombatApproach.None)
            {
                if(ui.Button(new Rect(1466,608,378,48),"변화 비교 · 직접 시험하기")) OpenNodeComparison(node.Id);
            }
            else Label(new Rect(1466, 608, 378, 65), string.IsNullOrEmpty(node.Drawback) ? node.Kind : "대가  ·  " + node.Drawback, ui.SmallLight);
            Label(new Rect(1466, 681, 378, 108), growthLayout.RequirementText[growthFocusedNode], ui.SmallLight);
            Label(new Rect(1466, 782, 378, 48), run.IsRetiredUltimateNode(growthFocusedNode)?"이전 궁극기 노드 · 새 원정 구매 중지":approachEffect != null ? "표시된 효과가 실제 적용됩니다 · 장착/방식 조건 확인"
                : "습득해도 고유 효과 없음 · 선행 경로와 숙련 투자만 반영", ui.SmallLight);
            bool acquired = growth.IsUnlocked(growthFocusedNode);
            string action = acquired ? growth.IsEquipped(growthFocusedNode) ? "장착 해제" : "장착"
                : (approachEffect == null ? "경로용 습득 · 효과 없음 · " : "습득 · ") + node.Cost + "점";
            bool enabled = !run.IsRetiredUltimateNode(growthFocusedNode) && canEdit && (acquired ? growth.CanSelect(growthFocusedNode) : growth.CanPurchase(growthFocusedNode));
            if (ui.Button(new Rect(1466, 835, 378, 44), run.IsRetiredUltimateNode(growthFocusedNode)?"전용 궁극기로 교체됨":enabled ? action : GrowthNodeStatus(growthFocusedNode, growth, canEdit), true, enabled))
            {
                bool changed = acquired ? run.TrySelectGrowthNode(growthFocusedNode) : run.TryPurchaseGrowthNode(growthFocusedNode);
                if (changed) { PlayClick(); Refresh(); }
            }
            if (acquired)
            {
                int blocker = growth.RefundBlocker(growthFocusedNode);
                string refund = !canEdit ? "지도·휴식에서 노드 해제 가능" : blocker >= 0
                    ? "해제 불가 · 다른 노드의 조건 유지 필요" : "노드 해제 · " + node.Cost + "점 반환";
                if (ui.Button(new Rect(1466, 889, 378, 40), refund, enabled: canEdit && blocker < 0))
                { if (run.TryRefundGrowthNode(growthFocusedNode)) { PlayClick(); Refresh(); } }
                if (blocker >= 0)
                    Label(new Rect(1466, 930, 378, 20), "조건을 사용하는 노드: " + growth.GetNode(blocker).Name, ui.SmallLight);
            }
        }
    }
}

using Graphaclysm.Core.Combat;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private static readonly string[] AxisLabels = { "0", "5", "10", "+4", "−4" };
        private static Vector2 FieldPoint(double x, double y) => new Vector2(Field.x + (float)x * FieldUnit, Field.y + (4 - (float)y) * FieldUnit);

        private Vector2 GraphOrigin => battle.UsesFragments ? FieldPoint(battle.Equation.Fragments.OriginX,battle.Equation.Fragments.OriginY) : Field.center;

        private void DrawBattle()
        {
            DrawBoard(); DrawBattleTopBar(); DrawPlayerPanel(); DrawEnemyPanel(); DrawHand(); DrawApproachBattle(); HandleBoardMovement();
            DrawPlayerImpact();
            DrawCombatSkillPreview();
            DrawSpatialSkillPreview();
            DrawSatellite();
            if(flow.IsNodeTrial && ui.Button(new Rect(28,798,314,40),"체험 종료 · 성좌로   G")) ReturnNodeTrial();
            if((message.Length>0 && ViewTime<feedbackUntil) || (hoveredHand>=0 && cardFailures[hoveredHand].Length>0))
            {
                Fill(new Rect(420, 60, 1110, 26), new Color(.06f, .055f, .1f, .88f));
                Label(new Rect(426,60,1098,26),hoveredHand>=0 && cardFailures[hoveredHand].Length>0?cardFailures[hoveredHand]:message,ui.SmallLight,true);
            }
        }

        private void DrawPlayerPanel()
        {
            if(battle.UsesFragments) {DrawQuietPlayer();return;}
            Texture2D portrait = flow.CurrentCharacter.Archetype == CombatArchetype.Ian ? ianPortrait : lunaPortrait;
            Line(new Vector2(100, 143), new Vector2(420, 143), Gold);
            Label(new Rect(100, 160, 330, 45), flow.CurrentCharacter.DisplayName, ui.Heading);
            Label(new Rect(100, 202, 330, 30), flow.CurrentCharacter.Archetype == CombatArchetype.Ian ? "IAN  /  검은 궤적" : "LUNA  /  천문 도구의 조율자", ui.Small);
            if (battle.UsesFragments) DrawFragmentLoom();
            else if (battle.Equation.IsCalculator) DrawCalculator();
            else DrawPortrait(new Rect(72, 225, 380, 455), portrait);
            Fill(new Rect(100, 647, 320, 100), new Color(Paper.r, Paper.g, Paper.b, 0.93f));
            Label(new Rect(108, 652, 130, 28), "생명", ui.Small);
            Label(new Rect(250, 648, 165, 34), hpText, ui.Number);
            Fill(new Rect(108, 691, 300, 3), new Color(Ink.r, Ink.g, Ink.b, 0.12f));
            Fill(new Rect(108, 691, 300 * battle.PlayerHealth / battle.PlayerMaxHealth, 3), Violet);
            Label(new Rect(108, 710, 180, 28), battle.UsesFragments ? "턴 드로우" : "에너지", ui.Small);
            Label(new Rect(290, 704, 125, 36), battle.UsesFragments ? drawStatus : energyText, battle.UsesFragments ? ui.Body : ui.Number);
            Label(new Rect(100, 752, 330, 48), selfStateText, ui.Small);
            if (ui.Button(new Rect(100, 780, 320, 31), "이동 취소", false,
                !castActive && !combatSkillTargeting && battle.Tactics.CanUndoMove)) UndoMove();
            for (int i = 0; i < TacticalCombatState.UltimateCost; i++)
                Fill(new Rect(102 + i * 53, 824, 42, 4), i < battle.Tactics.Resonance ? Violet : new Color(0.75f, 0.73f, 0.77f));
            if (ui.Button(new Rect(100, 843, 320, 60), ultimateText, true,
                !castActive && !combatSkillTargeting && battle.Tactics.Resonance >= TacticalCombatState.UltimateCost))
            { run.TryToggleUltimate(); Refresh(); }
            Rect ultimateInfo = new Rect(100, 911, 330, 73);
            Label(ultimateInfo, ultimateDescription, ui.Small);
        }

        private void DrawBoard()
        {
            Rect frame = new Rect(Field.x-23,Field.y-22,Field.width+46,Field.height+44);
            Fill(new Rect(0, 0, 1920, 1080), new Color(0.062f, 0.058f, 0.105f, 0.985f));
            Fill(frame, new Color(0.10f, 0.092f, 0.15f, 0.34f));
            Border(frame, new Color(Gold.r, Gold.g, Gold.b, .44f), 22);
            if(!battle.UsesFragments) Label(new Rect(Field.x+14,Field.y+8,300,30), "ASTRAL  ARCHIVE", ui.SmallLight);
            if (battle.UsesFragments && showBattleDetails) Label(new Rect(Field.xMax-270,Field.y+8,260,30),burstStatus,ui.SmallLight);
            if (battle.Equation.IsCalculator) Label(new Rect(Field.xMax-260,Field.y+8,250,30), battle.Equation.SelectedAxis == 0 ? "카드 → x(t)" : "카드 → y(t)", ui.SmallLight);
            if ((!battle.UsesFragments || showBattleDetails) && ui.Button(new Rect(1070,43,148,34), showEquation ? "수식 닫기" : "수식 보기")) showEquation = !showEquation;
            Color grid = new Color(0.62f, 0.57f, 0.75f, 0.24f);
            for (int x = 0; x <= 10; x++) for (int y = -4; y <= 4; y++)
                Disc(FieldPoint(x, y), x == 5 || y == 0 ? 1.5f : 1, grid);
            Line(FieldPoint(0, 0), FieldPoint(10, 0), grid);
            Line(FieldPoint(5, -4), FieldPoint(5, 4), grid);
            if(!battle.UsesFragments || showBattleDetails)
            {
            Label(new Rect(Field.x+4,Field.center.y+5,40,24), AxisLabels[0], ui.SmallLight);
            Label(new Rect(Field.center.x+8,Field.center.y+5,40,24), AxisLabels[1], ui.SmallLight);
            Label(new Rect(Field.xMax-40,Field.center.y+5,40,24), AxisLabels[2], ui.SmallLight);
            Label(new Rect(Field.center.x+8,Field.y+4,45,24), AxisLabels[3], ui.SmallLight);
            Label(new Rect(Field.center.x+8,Field.yMax-28,45,24), AxisLabels[4], ui.SmallLight);
            }
            Diamond(GraphOrigin, 9, new Color(Gold.r, Gold.g, Gold.b, 0.55f));
            DrawTerrain();

            hoveredEnemy = -1;
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                EnemyState enemy = battle.Enemies[i]; if (!enemy.IsAlive) continue;
                Vector2 p = FieldPoint(enemy.X, enemy.Y);
                if (Vector2.Distance(Event.current.mousePosition, p) < 40 || EnemyRow(i).Contains(Event.current.mousePosition)) hoveredEnemy = i;
            }
            DrawTacticalThreats();
            // Reposition previews stay available on inspection; every attack is drawn above.
            int aimed = hoveredEnemy;
            if (aimed >= 0)
            {
                EnemyState enemy = battle.Enemies[aimed];
                if (enemy.Intent.Kind == EnemyIntentKind.Reposition)
                {
                    Vector2 target = FieldPoint(enemy.Intent.TargetX, enemy.Intent.TargetY);
                    Diamond(target, 30, new Color(Gold.r, Gold.g, Gold.b, 0.55f));
                    Line(FieldPoint(enemy.X, enemy.Y), target, new Color(Gold.r, Gold.g, Gold.b, 0.35f));
                }
            }
            spellRenderer.Draw(battle.Equation, Field, CastElapsed, castActive && !preferences.ReduceMotion);
            DrawSkillTrail();
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                EnemyState enemy = battle.Enemies[i]; if (!enemy.IsAlive && !(castActive && castHits[i])) continue;
                Vector2 p = FieldPoint(enemy.X, enemy.Y);
                Color color = damagePreview[i] > 0 ? new Color(0.96f, 0.78f, 0.78f) : new Color(0.66f, 0.62f, 0.77f);
                Disc(p, 29, new Color(0.17f, 0.15f, 0.23f));
                if(enemy.Definition.Behavior.Kind>=EnemyBehaviorKind.RotatingGun)
                    DrawArchiveEnemySeal(p,enemy.IsBoss?31:24,EnemyArchive.Find(enemy.Definition.Behavior.Kind));
                else {Diamond(p, 30, color, 1.8f); Diamond(p, 17, color);}
                Line(p + new Vector2(-30, 0), p + new Vector2(0, 17), color);
                Line(p + new Vector2(0, -30), p + new Vector2(17, 0), color);
                Label(new Rect(p.x - 69, p.y - 16, 35, 30), enemyBadges[i], ui.SmallLight, true);
                if (hoveredEnemy == i) Ring(p, (float)BattleSession.EnemyHitRadius * FieldUnit, color);
                if (combatSkillTargeting && !battle.UsesDiagramAbility && battle.Approach != CombatApproach.Execution && battle.Approach != CombatApproach.Observation)
                    Ring(p, (float)BattleSession.EnemyHitRadius * FieldUnit + 10,
                        hoveredEnemy == i ? Gold : new Color(Gold.r, Gold.g, Gold.b, .32f), hoveredEnemy == i ? 2.5f : 1.2f);
                float healthWidth=enemy.IsBoss?116:54, healthHeight=enemy.IsBoss?6:3;
                Fill(new Rect(p.x-healthWidth*.5f,p.y+44,healthWidth,healthHeight),new Color(.4f,.34f,.47f));
                Fill(new Rect(p.x-healthWidth*.5f,p.y+44,healthWidth*enemy.Health/enemy.Definition.MaxHealth,healthHeight),color);
                if (!castActive && damagePreview[i] > 0 && (!battle.UsesFragments || hoveredEnemy==i || showBattleDetails)) Label(new Rect(p.x + 37, p.y - 20, 80, 40), enemyDamage[i], ui.Light);
            }
            Vector2 player = FieldPoint(battle.Tactics.X, battle.Tactics.Y);
            DrawMovementPreview(player);
            float radius = (float)battle.Tactics.HitRadius * FieldUnit;
            Disc(player, radius, new Color(0.27f, 0.23f, 0.37f));
            Ring(player, radius, selfPreview ? new Color(0.76f, 0.91f, 0.85f) : Gold, 2);
            Texture2D portrait = flow.CurrentCharacter.Archetype == CombatArchetype.Ian ? ianPortrait : lunaPortrait;
            // A head crop preserves identification without putting a full character illustration over the graph.
            if (portrait != null) GUI.DrawTextureWithTexCoords(new Rect(player.x - 24, player.y - 29, 48, 58), portrait, flow.CurrentCharacter.Archetype == CombatArchetype.Luna ? new Rect(.44f,.77f,.25f,.21f) : new Rect(0.30f, 0.65f, 0.4f, 0.30f));
            if(selfPreview || !battle.UsesFragments) Label(new Rect(player.x - 60, player.y + radius + 7, 120, 28), selfPreview ? "강화" : flow.CurrentCharacter.DisplayName, ui.SmallLight, true);
            if (castActive && !preferences.ReduceMotion) DrawCastAccents();
            if (showEquation)
            {
                float height = battle.UsesFragments ? 345 : battle.Equation.IsCalculator ? 167 : 64;
                Fill(new Rect(Field.x,Field.yMax-height,Field.width,height), new Color(Ink.r, Ink.g, Ink.b, 0.94f));
                Label(new Rect(Field.x+19,Field.yMax-height+6,Field.width-38,height-11), formulaText, battle.UsesFragments ? ui.SmallLight : ui.Formula, !battle.UsesFragments);
            }
            else if (!battle.UsesFragments && battle.PlayedCardCount > 0)
            {
                float width = Mathf.Min(105, 720f / battle.PlayedCardCount);
                for (int i = 0; i < battle.PlayedCardCount; i++)
                    Label(new Rect(589 + i * width, 785, width - 6, 25), battle.GetPlayedCard(i).DisplayName, ui.SmallLight, true);
            }
        }

        private void DrawEnemyPanel()
        {
            if(battle.UsesFragments) {DrawQuietEnemyPanel();return;}
            Label(new Rect(1490, 134, 330, 37), "다가오는 위협", ui.Heading);
            for (int i = 0; i < battle.Enemies.Count; i++)
            {
                EnemyState enemy = battle.Enemies[i]; float y = 187 + i * 143;
                Fill(new Rect(1489, y, 336, 129), new Color(1, 1, 1, hoveredEnemy == i ? 0.76f : 0.36f));
                Line(new Vector2(1490, y), new Vector2(1825, y), hoveredEnemy == i ? Violet : new Color(0.71f, 0.67f, 0.73f, 0.5f));
                Label(new Rect(1502, y + 10, 45, 37), enemyBadges[i], ui.Number);
                Label(new Rect(1554, y + 10, 258, 37), enemy.Definition.DisplayName, ui.Body);
                Label(new Rect(1504, y + 52, 210, 30), enemy.IsAlive ? enemyIntent[i] : "소멸", ui.Body);
                Label(new Rect(1718, y + 54, 95, 30), enemyHealth[i], ui.Small);
                Label(new Rect(1504, y + 91, 306, 29), enemyStatusSummary[i], ui.Small);
            }
            if (hoveredHand >= 0 && hoveredHand < game.Deck.HandCount)
            {
                SkillVisual skill = Visual(game.Deck.GetHandCard(hoveredHand));
                if (skill != null)
                {
                    Fill(new Rect(1488, 627, 342, 281), new Color(1, 1, 1, 0.92f));
                    Fill(new Rect(1503,641,310,4),Rarity(skill.Card.DiagramRarity));
                    DrawReadableEffectRows(new Rect(1503,658,311,245),skill);
                }
            }
            else if (hoveredEnemy >= 0 && enemyStatuses[hoveredEnemy].Length > 0)
            {
                Fill(new Rect(1488, 627, 342, 181), new Color(1, 1, 1, 0.72f));
                Label(new Rect(1503, 635, 310, 37), battle.Enemies[hoveredEnemy].Definition.DisplayName, ui.Body);
                Label(new Rect(1503, 681, 311, 134), enemyStatuses[hoveredEnemy], ui.Small);
            }
            if (battle.UsesFragments)
            {
                Label(new Rect(1490,809,335,28),castActive ? "선을 새기는 중" : outcomeText,ui.Small,true);
                if(ui.Button(new Rect(1490,847,335,58),castActive ? lastPlotName : "방출   ↵",true,!castActive && !combatSkillTargeting && battle.CanPlot)) StartCast();
                if(ui.Button(new Rect(1490,915,161,43),"응축 · 넘기기",false,!castActive && !combatSkillTargeting && battle.CanCondense)) Condense();
                if(ui.Button(new Rect(1664,915,161,43),"해체 · 넘기기",false,!castActive && !combatSkillTargeting)) Unravel();
                if(ui.Button(new Rect(1490,971,335,31),"마지막 파편 되돌리기",false,!castActive && !combatSkillTargeting && battle.PlayedCardCount > battle.SealedCardCount)) Undo();
                return;
            }
            bool draftReady = !battle.Equation.IsCalculator || calculatorValid;
            Label(new Rect(1490, 835, 335, 35), castActive ? "선을 새기는 중" : !draftReady ? "수식 입력을 마치세요" : outcomeText, ui.Body, true);
            if (ui.Button(new Rect(1490, 882, 335, 68), castActive ? lastPlotName : !draftReady ? "입력 중" : !battle.CanPlot ? "작도 불가" : "작도   ↵", true,
                !castActive && !combatSkillTargeting && battle.CanPlot && (!battle.Equation.IsCalculator || calculatorValid))) StartCast();
            if (ui.Button(new Rect(1490, 961, 335, 33), "한 장 되돌리기", false,
                !castActive && !combatSkillTargeting && battle.PlayedCardCount > 0)) Undo();
        }

        private int handPressedIndex = -1, handPressedCount;

        private int PickHandCard(Vector2 pointer, int count, int focused)
        {
            // The inspection card and lifted card are drawn last, so they own input first.
            if (focused >= 0 && focused < count && (handInspectionRect.Contains(pointer)
                || HandCardRect(focused, count, true).Contains(pointer))) return focused;
            for (int i = count - 1; i >= 0; i--)
                if (i != focused && RotatedContains(HandCardRect(i, count, false), HandCardAngle(i, count), pointer))
                    return i;
            return -1;
        }

        private void DrawHand()
        {
            int count = game.Deck.HandCount;
            if (count == 0) { handPressedIndex = hoveredHand = -1; return; }
            if (combatSkillTargeting)
            {
                ClearHandInspection();
                for (int i = 0; i < count; i++)
                {
                    var visual = Visual(game.Deck.GetHandCard(i));
                    if (visual != null) DrawHandCard(i, count, visual, false);
                }
                return;
            }
            int previousHover = hoveredHand;
            int pointed = PickHandCard(Event.current.mousePosition, count, previousHover);
            hoveredHand = pointed;
            if (hoveredHand < 0 && previousHover >= 0 && previousHover < count
                && (handInspectionRect.Contains(Event.current.mousePosition) || handHoverBridge.Contains(Event.current.mousePosition)
                    || keywordKeepsCard && (keywordTooltipRect.Contains(Event.current.mousePosition) || keywordHoverBridge.Contains(Event.current.mousePosition))))
                hoveredHand = previousHover;
#if UNITY_EDITOR
            if (DiagnosticHoveredCard >= 0 && DiagnosticHoveredCard < count) hoveredHand = DiagnosticHoveredCard;
#endif
            for (int i = 0; i < count; i++)
            {
                if (i == hoveredHand) continue;
                SkillVisual visual = Visual(game.Deck.GetHandCard(i)); if (visual == null) continue;
                DrawHandCard(i, count, visual, false);
            }
            if (hoveredHand >= 0 && hoveredHand < count)
            {
                SkillVisual focused = Visual(game.Deck.GetHandCard(hoveredHand));
                if (focused != null)
                {
                    DrawHandCard(hoveredHand, count, focused, true);
                    float spacing = count <= 1 ? 0 : Mathf.Min(112, 780f / (count - 1));
                    float center = 960 + (hoveredHand - (count - 1) * .5f) * spacing;
                    handInspectionRect = new Rect(Mathf.Clamp(center - 170, 390, 1180), 425, 340, 420);
                    Rect handCard = HandCardRect(hoveredHand, count, true);
                    handHoverBridge = Rect.MinMaxRect(
                        Mathf.Min(handInspectionRect.xMin, handCard.xMin) - 8,
                        handInspectionRect.yMin - 8,
                        Mathf.Max(handInspectionRect.xMax, handCard.xMax) + 8,
                        handCard.yMax + 8);
                    DrawSkillCard(handInspectionRect, focused, true, true);
                }
            }
            else { handInspectionRect = default(Rect); handHoverBridge = default(Rect); }
            HandleHandCardInput(Event.current, pointed, count);
        }

        private void HandleHandCardInput(Event input, int pointed, int count)
        {
            if (combatSkillTargeting) { handPressedIndex = -1; return; }
            if (input.button == 0 && input.type == EventType.MouseDown)
            {
                handPressedIndex = !castActive && !combatSkillTargeting ? pointed : -1;
                handPressedCount = count;
                if (pointed >= 0) input.Use();
            }
            else if (input.button == 0 && input.type == EventType.MouseUp)
            {
                int pressed = handPressedIndex;
                handPressedIndex = -1;
                if (pressed >= 0)
                {
                    input.Use();
                    if (!castActive && !combatSkillTargeting && pressed == pointed && handPressedCount == count)
                        PlayCard(pressed);
                }
            }
        }

        private void DrawMovementPreview(Vector2 player)
        {
            if (castActive || combatSkillTargeting || battle.Phase != BattlePhase.PlayerPlanning || battle.Tactics.HasMoved) return;
            bool anchored = battle.Tactics.Statuses.Get(CombatStatusKind.Anchor) > 0;
            Color range = anchored ? new Color(Threat.r, Threat.g, Threat.b, .28f) : new Color(.66f, .85f, .96f, .34f);
            Ring(player, (float)battle.Tactics.CurrentMoveDistance * FieldUnit, range, 1.4f, .86f, .18f);
            Vector2 pointer = Event.current.mousePosition;
            if (!Field.Contains(pointer) || anchored) return;
            ScreenToField(pointer, out double x, out double y);
            if (!battle.TryResolveMoveDestination(x, y, out double resolvedX, out double resolvedY)) return;
            Vector2 destination = FieldPoint(resolvedX, resolvedY);
            Line(player, destination, new Color(.72f, .91f, 1f, .58f), 1.5f);
            Diamond(destination, 15, new Color(.78f, .94f, 1f, .92f), 2.2f);
        }

        private void HandleBoardMovement()
        {
            HandleBoardInput(Event.current);
        }

        private void HandleBoardInput(Event e)
        {
            if (e.type != EventType.MouseDown || e.button != 0 || ModalOpen || castActive
                || battle.Phase != BattlePhase.PlayerPlanning
                || !combatSkillTargeting && (hoveredHand >= 0
                || handInspectionRect.Contains(e.mousePosition) || keywordTooltipRect.Contains(e.mousePosition)
                || hoveredTerrain < 0 && (keywordSourceRect.Contains(e.mousePosition) || keywordOwnerRect.Contains(e.mousePosition)))) return;
            if (combatSkillTargeting)
            {
                if (battle.Approach == CombatApproach.Observation)
                {
                    if (!Field.Contains(e.mousePosition)) return;
                    ScreenToField(e.mousePosition, out double satelliteX, out double satelliteY);
                    PlaceSatelliteAt(satelliteX, satelliteY); e.Use(); return;
                }
                if (battle.Approach == CombatApproach.Execution || lunaPullTargeting)
                {
                    if (!Field.Contains(e.mousePosition)) return;
                    ScreenToField(e.mousePosition, out double skillX, out double skillY);
                    if (lunaPullTargeting) UseLunaPullAt(skillX, skillY); else UseExecutionAt(skillX, skillY);
                    e.Use(); return;
                }
                if (battle.UsesDiagramAbility)
                {
                    if (!Field.Contains(e.mousePosition)) return;
                    ScreenToField(e.mousePosition, out double aimX, out double aimY);
                    if (run.TryUseDiagramAbility(aimX, aimY, diagramAngle))
                    { combatSkillTargeting = false; message = battle.RecordingArmed ? "기록 예약 · 이번 피해 75% / 다음 방출에 기록 추가 공격" : "조정 완료 · 흰 도안이 실제 공격 위치입니다."; Refresh(); }
                    e.Use(); return;
                }
                if (hoveredEnemy >= 0) UseCombatSkillOnTarget(hoveredEnemy);
                else
                {
                    message = "살아 있는 적 표식이나 오른쪽 목록을 선택하세요.";
                    Refresh();
                }
                e.Use();
                return;
            }
            if (battle.Tactics.HasMoved) return;
            if (!Field.Contains(e.mousePosition)) return;
            if (showEquation)
            {
                float height = battle.UsesFragments ? 345 : battle.Equation.IsCalculator ? 167 : 64;
                if (new Rect(Field.x, Field.yMax - height, Field.width, height).Contains(e.mousePosition)) return;
            }
            ScreenToField(e.mousePosition, out double x, out double y);
            MoveTo(x, y);
            e.Use();
        }

        private static void ScreenToField(Vector2 point, out double x, out double y)
        {
            x = (point.x - Field.x) / FieldUnit;
            y = 4.0 - (point.y - Field.y) / FieldUnit;
        }

        private void ClearHandInspection()
        {
            hoveredHand = handPressedIndex = -1;
            handInspectionRect = handHoverBridge = default(Rect);
            keywordTooltipRect = keywordHoverBridge = keywordSourceRect = keywordOwnerRect = default(Rect);
            keywordKeepsCard = false;
            keywordPinned = false;
            hoveredKeywordTitle = hoveredKeywordBody = "";
        }

        private static float HandCardAngle(int index, int count)
        {
            if (count <= 1) return 0;
            return (index - (count - 1) * .5f) / (count - 1) * 20f;
        }

        private Rect HandCardRect(int index, int count, bool focused)
        {
            float spacing = count <= 1 ? 0 : Mathf.Min(112, 780f / (count - 1));
            float offset = index - (count - 1) * .5f;
            float normalized = count <= 1 ? 0 : offset / ((count - 1) * .5f);
            float lift = focused ? 92 + cardHover[index] * 14 : cardHover[index] * 10;
            return new Rect(960 + offset * spacing - 92, 910 + Mathf.Abs(normalized) * 34 - lift, 184, 244);
        }

        private static bool RotatedContains(Rect rect, float angle, Vector2 point)
        {
            float radians = -angle * Mathf.Deg2Rad;
            Vector2 delta = point - rect.center;
            Vector2 unrotated = new Vector2(delta.x * Mathf.Cos(radians) - delta.y * Mathf.Sin(radians),
                delta.x * Mathf.Sin(radians) + delta.y * Mathf.Cos(radians)) + rect.center;
            return rect.Contains(unrotated);
        }

        private bool DrawHandCard(int index, int count, SkillVisual visual, bool focused)
        {
            Rect rect = HandCardRect(index, count, focused);
            float angle = focused ? 0 : HandCardAngle(index, count);
            Matrix4x4 saved = GUI.matrix;
            GUI.matrix = saved * Matrix4x4.TRS(rect.center, Quaternion.Euler(0, 0, angle), Vector3.one)
                * Matrix4x4.TRS(-rect.center, Quaternion.identity, Vector3.one);
            DrawSkillCard(rect, visual, focused, false);
            GUI.matrix = saved;
            return false;
        }

        private void DrawSkillCard(Rect r, SkillVisual visual, bool hovered, bool large)
        {
            if (visual.Card.IsFragment) { DrawFragmentCard(r,visual,hovered,large); return; }
            Color rarity = Rarity(visual.Card.DiagramRarity);
            Fill(new Rect(r.x + 3, r.y + 5, r.width, r.height), new Color(0.16f, 0.12f, 0.24f, 0.15f));
            Fill(r, hovered ? new Color(0.99f, 0.98f, 0.96f) : new Color(0.93f, 0.92f, 0.92f));
            Border(r, hovered ? Violet : rarity);
            Fill(new Rect(r.x + 12, r.y, r.width - 24, hovered ? 4 : 2), rarity);
            float pad = large ? 25 : 12;
            Label(new Rect(r.x + pad, r.y + 9, r.width - pad * 2 - 20, large ? 55 : 35), visual.Card.DisplayName, large ? ui.Heading : ui.Body);
            Label(new Rect(r.xMax - pad - 21, r.y + 9, 25, large ? 55 : 35), visual.Cost, large ? ui.Number : ui.Body, true);
            Vector2 center = new Vector2(r.center.x, r.y + r.height * (large ? .38f : .43f));
            float size = Mathf.Min(r.width * 0.30f, r.height * (large ? .15f : .18f));
            Line(center - Vector2.right * size * 1.18f, center + Vector2.right * size * 1.18f, new Color(Gold.r, Gold.g, Gold.b, 0.38f));
            Line(center - Vector2.up * size * 1.1f, center + Vector2.up * size * 1.1f, new Color(Gold.r, Gold.g, Gold.b, 0.38f));
            for (int j = 1; j < visual.Glyph.Length; j++)
            {
                Vector2 a = visual.Glyph[j - 1], b = visual.Glyph[j]; a.y = -a.y; b.y = -b.y;
                Line(center + a * size, center + b * size, Violet, large ? 2.4f : 1.8f);
            }
            if (large)
            {
                DrawExplainedText(new Rect(r.x + pad, r.y + 205, r.width - pad * 2, 58), visual.Card.Description, ui.Body, true);
                Line(new Vector2(r.x + pad, r.y + 276), new Vector2(r.xMax - pad, r.y + 276), new Color(rarity.r, rarity.g, rarity.b, 0.55f));
                if (visual.KeywordNames != null && visual.KeywordNames.Length > 0)
                    DrawCardKeywords(new Rect(r.x + pad, r.y + 287, r.width - pad * 2, 34), visual, false, r);
                DrawExplainedText(new Rect(r.x + pad, r.y + 328, r.width - pad * 2, 30), visual.Abilities, ui.Small, true);
            }
            else
            {
                Label(new Rect(r.x + pad, r.yMax - 69, r.width - pad * 2, 40), visual.Card.Description, ui.Small, true);
                Line(new Vector2(r.x + pad, r.yMax - 32), new Vector2(r.xMax - pad, r.yMax - 32), new Color(rarity.r, rarity.g, rarity.b, 0.55f));
                Label(new Rect(r.x + 5, r.yMax - 29, r.width - 10, 25), visual.Abilities, ui.Small, true);
            }
        }

        private void DrawCastAccents()
        {
            float t = CastElapsed;
            if (castUltimate)
            {
                float alpha = Mathf.Clamp01(t * 7) * Mathf.Clamp01((CastDuration - t) * 4);
                Fill(new Rect(Field.x,Field.y+43,Field.width,74), new Color(0.18f, 0.13f, 0.28f, alpha * 0.88f));
                Line(new Vector2(Field.x+42,Field.y+43),new Vector2(Field.xMax-42,Field.y+43), new Color(Gold.r, Gold.g, Gold.b, alpha));
                Label(new Rect(Field.x+42,Field.y+56,Field.width-84,45), ultimateName, ui.Light, true);
            }
            if (t < 0.34f)
            {
                float a = Mathf.Sin(t / 0.34f * Mathf.PI);
                Diamond(GraphOrigin, 8 + t * 45, new Color(0.9f, 0.8f, 1, a));
                Line(GraphOrigin - Vector2.up * 22, GraphOrigin + Vector2.up * 22, new Color(1, 0.94f, 1, a), 2);
            }
            if (t < ImpactTime) return;
            float progress = Mathf.Clamp01((t - ImpactTime) / (CastDuration - ImpactTime));
            for (int i = 0; i < castHits.Length; i++)
            {
                if (!castHits[i]) continue;
                Vector2 p = FieldPoint(battle.Enemies[i].X, battle.Enemies[i].Y);
                HitBloom(p, progress, new Color(0.94f, 0.81f, 0.94f));
                Label(new Rect(p.x - 50, p.y - 70 - progress * 28, 100, 48), hitNumbers[i], ui.Light, true);
            }
            if (castSelfHit) HitBloom(FieldPoint(battle.Tactics.X, battle.Tactics.Y), progress, new Color(0.71f, 0.93f, 0.84f));
        }

        private static void HitBloom(Vector2 p, float progress, Color color)
        {
            color.a = 1 - progress;
            Diamond(p, 29 + progress * 40, color, 1.4f);
            for (int j = 0; j < 8; j++)
            {
                float angle = j * Mathf.PI * 0.25f + 0.3f;
                Vector2 d = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 q = p + d * (22 + progress * (57 + j % 3 * 8));
                Line(q, q + d * (8 * (1 - progress)), color, j % 2 == 0 ? 2 : 1);
            }
        }

        private void DrawSkillTrail()
        {
            if (ViewTime >= skillFxUntil) return;
            Vector2 a = FieldPoint(skillFxOriginX, skillFxOriginY), b = FieldPoint(skillFxEndX, skillFxEndY);
            Vector2 direction = b - a;
            if (direction.sqrMagnitude < .01f) return;
            Vector2 normal = new Vector2(-direction.y, direction.x).normalized;
            float remaining = Mathf.Clamp01((skillFxUntil - ViewTime) / (preferences.ReduceMotion ? .35f : .9f));
            Color glow = skillFxReset ? new Color(1f,.76f,.34f,remaining) : new Color(.76f,.55f,1f,remaining);
            Line(a,b,new Color(glow.r,glow.g,glow.b,remaining*.24f),skillFxStyle == 1 ? 18 : 11);
            if (skillFxStyle == 1)
            {
                Line(a+normal*28,b+normal*28,glow,3);
                Line(a-normal*28,b-normal*28,glow,3);
                if (skillFxLaneCount >= 5)
                {
                    Line(a+normal*55,b+normal*55,glow,2.5f);
                    Line(a-normal*55,b-normal*55,glow,2.5f);
                }
            }
            else if (skillFxStyle == 2)
            {
                Ring(b, 44 + (1 - remaining) * 58, glow, 3);
                Ring(b, 24 + (1 - remaining) * 34, new Color(1,.94f,1,remaining), 1.5f);
            }
            else if (skillFxStyle == 3)
            {
                Line(a + normal * 9, b + normal * 9, glow, 2.5f);
                Line(b - normal * 9, a - normal * 9, new Color(1,.94f,1,remaining), 2.5f);
                Diamond(a, 16 + (1 - remaining) * 18, glow, 2);
            }
            else if (skillFxStyle == 4)
            {
                Ring(b, 30 + (1 - remaining) * 44, glow, 2.5f, .72f, .4f);
                Diamond((a + b) * .5f, 11, glow, 2);
            }
            Line(a,b,new Color(1,.94f,1,remaining),3);
            Diamond(b,skillFxReset?28:18,glow,2);
            if (skillFxReset) Ring(b,40+(1-remaining)*38,new Color(glow.r,glow.g,glow.b,remaining),2);
            Label(new Rect((a.x+b.x)*.5f-90,(a.y+b.y)*.5f-52,180,36),skillFxLabel,ui.SmallLight,true);
        }
    }
}

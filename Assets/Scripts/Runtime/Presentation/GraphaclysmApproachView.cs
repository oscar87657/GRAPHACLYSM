using Graphaclysm.Application;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Equations;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private int diagramAngle;
        private bool diagramAimValid;
        private Vector2 lastDiagramPointer;
        private EquationState aimDiagram;
        private string diagramPreviewText = "";
        private static readonly string[] ApproachNames = { "", "이안 · 균열 집행", "이안 · 기록 포격", "루나 · 궤도 조율", "루나 · 쌍성 관측" };
        private static readonly string[] ApproachHints = { "",
            "K → 방향 클릭: 3.6 돌진 · 경로의 적에게 피해 10 / 파열 +6. 처치 시 재사용.",
            "카드 조립 → K → 기록 위치 선택. 이번 피해 75%, 다음 방출에 기록 추가 공격.",
            "K: 도안 이동·회전 / J: 주변 적을 모으기. 한 턴에 둘 중 하나, 몸은 그대로.",
            "K: 위성 설치·이동 / J: 다음 식의 출발점 선택. 위성에 작도가 닿으면 몸도 보호." };

        private void DrawApproachEntry()
        {
            Fill(new Rect(1150, 680, 660, 300), new Color(.055f,.05f,.095f,.96f));
            Label(new Rect(1180, 690, 600, 42), "새 전투 · 한 번의 작도로 뒤집기", ui.HeadingLight);
            Label(new Rect(1180, 735, 600, 42), "같은 전장과 손패, 네 가지 해결법. 기존 저장은 유지됩니다.", ui.SmallLight);
            for (int i = 1; i <= 4; i++)
                if (ui.Button(new Rect(1180, 780 + (i - 1) * 48, 600, 42), ApproachNames[i], i == 1)) StartApproach((CombatApproach)i);
        }

        private void StartApproach(CombatApproach approach)
        {
            if(flow.IsNodeTrial) return;
            if (flow.Phase != GameFlowPhase.MainMenu) flow.ReturnToMainMenu();
            if (!flow.TryStartPractice(approach)) return;
            castActive = false; combatSkillTargeting = false; diagramAimValid = false;
            paused = helpOpen = growthOpen = inventoryOpen = false; confirmation = Confirmation.None;
            message = "먼저 긴 황혼 + 파열을 조립해 보세요. 포수의 동력선을 끊으면 포격이 멈춥니다.";
            Refresh();
        }

        private void DrawApproachResult()
        {
            if(flow.IsNodeTrial)
            {
                Header("NODE TRIAL", "핵심 능력 체험 결과");
                Label(new Rect(240,300,1440,110),"체험을 마쳤습니다. 원래 원정의 카드·체력·성장점은 그대로입니다.",ui.Heading);
                if(ui.Button(new Rect(350,550,540,72),"같은 능력 다시 시험",true)) RestartNodeTrial();
                if(ui.Button(new Rect(1030,550,540,72),"원래 성좌로 돌아가기")) ReturnNodeTrial();
                return;
            }
            Header("ONE PLOT, FOUR ANSWERS", "새 전투 비교");
            Label(new Rect(240, 250, 1440, 80), run.Phase == RunPhase.Completed ? "전장을 정리했습니다" : "다른 해법으로 다시 그려볼까요?", ui.PageTitle);
            Label(new Rect(240, 360, 1440, 85), "어떤 능력 때문에 카드의 순서나 위치를 바꿨나요?\n다른 방식도 같은 손패와 배치에서 시작합니다. 연습에는 잔광 보상이 없습니다.", ui.Body);
            for (int i = 1; i <= 4; i++)
                if (ui.Button(new Rect(240 + (i - 1) * 365, 535, 345, 72), ApproachNames[i])) StartApproach((CombatApproach)i);
            if (ui.Button(new Rect(240, 650, 460, 60), "기존 원정으로 · 메인 화면")) { flow.ReturnToMainMenu(); Refresh(); }
        }

        private void DrawApproachBattle()
        {
            DrawApproachUltimates();
            if(battle.StylePreviewDiagram!=null)DrawDiagramGhost(battle.StylePreviewDiagram,new Color(.6f,.9f,1,.65f));
            if(battle.HasRule(Graphaclysm.Core.Runs.ApproachRule.SatelliteLink)&&battle.HasSatellite)
                Line(FieldPoint(battle.Tactics.X,battle.Tactics.Y),FieldPoint(battle.SatelliteX,battle.SatelliteY),battle.SatellitePlotHit?Gold:Violet,2);
            if (battle.Approach == CombatApproach.None) return;
            if (Event.current.type == EventType.KeyDown && combatSkillTargeting && !lunaPullTargeting && battle.CanReflectDiagram && Event.current.keyCode == KeyCode.R)
            { diagramAngle = diagramAngle == 180 ? 0 : 180; diagramAimValid = false; Event.current.Use(); }
            if (Event.current.type == EventType.KeyDown && combatSkillTargeting && !lunaPullTargeting && battle.Approach == CombatApproach.Tuning
                && (Event.current.keyCode == KeyCode.Q || Event.current.keyCode == KeyCode.E))
            {
                diagramAngle = Mathf.Clamp((diagramAngle == 180 ? 0 : diagramAngle) + (Event.current.keyCode == KeyCode.Q ? -30 : 30), -battle.CurrentRotationLimit, battle.CurrentRotationLimit);
                diagramAimValid = false; Event.current.Use();
            }
            Label(new Rect(410, 14, 390, 40), ApproachNames[(int)battle.Approach], ui.SmallLight);
            float hintTop = Mathf.Max(414, 165 + battle.Enemies.Count * 82);
            string hint = combatSkillTargeting && !lunaPullTargeting && battle.UsesDiagramAbility ? diagramPreviewText : battle.HasSpecialization ? combatSkillDescription : ApproachHints[(int)battle.Approach];
            Label(new Rect(1580, hintTop, 308, 200), hint + (battle.CanReflectDiagram ? "\nR: 회전 / 가로축 반전" : ""), ui.SmallLight);
            if (battle.HasRecording) DrawDiagramGhost(battle.RecordedDiagram, new Color(.74f,.57f,1,.72f));
            else if (castActive && impactApplied && battle.LastRecordingDiagram != null)
                DrawDiagramGhost(battle.LastRecordingDiagram, new Color(.74f,.57f,1,1));
            if (combatSkillTargeting && !lunaPullTargeting && battle.UsesDiagramAbility && Field.Contains(Event.current.mousePosition))
            {
                Vector2 pointer = Event.current.mousePosition;
                if (!diagramAimValid || (pointer - lastDiagramPointer).sqrMagnitude > .5f)
                {
                    lastDiagramPointer = pointer; diagramAimValid = true;
                    ScreenToField(pointer, out double x, out double y);
                    aimDiagram = battle.PreviewDiagramAbility(x, y, diagramAngle);
                    if (aimDiagram == null) diagramPreviewText = "기준점에서 " + battle.CurrentDiagramReach + " 이내, 전장 안쪽을 선택하세요. Q/E: 회전 · 우클릭: 취소";
                    else
                    {
                        int hits = 0;
                        for (int i = 0; i < battle.Enemies.Count; i++)
                        {
                            var enemy = battle.Enemies[i];
                            if (enemy.IsAlive && EquationAnalyzer.CalculateIntersectionDamage(aimDiagram, enemy.X, enemy.Y, .48) > 0) hits++;
                        }
                        bool self = EquationAnalyzer.CalculateIntersectionDamage(aimDiagram, battle.Tactics.X, battle.Tactics.Y, battle.Tactics.HitRadius) > 0;
                        diagramPreviewText = battle.Approach == CombatApproach.Recording
                            ? "보라색: 다음 방출의 기록 위치 · 현재 배치에서 적 " + hits + "명 · 카드 능력/자가 보호 복제 없음"
                            : "조정 후: 적 " + hits + "명 · " + (self ? "자기 보호 가능" : "자기 보호 없음")
                                + (battle.PreviewCannonCut(aimDiagram) ? " · 포격 차단" : "") + " · " + (diagramAngle == 180 ? "거울 반전" : diagramAngle + "°") + " · Q/E 회전 / 클릭 확정";
                    }
                }
                if (aimDiagram != null) DrawDiagramGhost(aimDiagram, battle.Approach == CombatApproach.Recording
                    ? new Color(.74f,.57f,1,.9f) : new Color(.45f,1,.85f,.9f));
            }
            bool cut = battle.CannonDisconnected;
            bool predicted = !cut && battle.PreviewCannonCut(battle.Equation);
            EnemyState cannon = null;
            for (int i = 0; i < battle.Enemies.Count; i++) if (battle.Enemies[i].Definition.Id == "signature.cannon") cannon = battle.Enemies[i];
            if (cannon != null && cannon.IsAlive)
            {
                Vector2 a = FieldPoint(5, 1.6), b = FieldPoint(cannon.X, cannon.Y);
                if (!cut) Line(a, b, predicted ? new Color(.45f,1,.85f,.9f) : Gold, 3);
                Diamond(a, 12, cut ? Violet : Gold, 2);
                Label(new Rect(a.x-100,a.y-42,410,35), cut ? "동력선 절단 · 포격 중단" : predicted ? "방출하면 동력선 절단" : "동력선 · 작도로 끊으면 포격 중단", ui.SmallLight);
            }
            if (battle.Approach != CombatApproach.Observation && !combatSkillTargeting
                && (battle.HasRecording || battle.LastRecordingHits > 0 || battle.LastCannonCut))
                Label(new Rect(1580, hintTop + 210, 308, 105), battle.HasRecording ? "기록 대기 중\n다음 방출 때 보라색 도안이 공격합니다." : battle.LastRecordingHits > 0 ? "기록이 추가 공격했습니다." : "동력선을 끊어 포격을 막았습니다.", ui.SmallLight);
        }

        private void DrawDiagramGhost(EquationState diagram, Color color)
        {
            spellRenderer.DrawGhost(diagram, Field, color);
        }
    }
}

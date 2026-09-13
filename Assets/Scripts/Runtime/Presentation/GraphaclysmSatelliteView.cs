using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Equations;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private void ToggleSatelliteTargeting()
        {
            if (castActive || run == null) return;
            if (combatSkillTargeting) { combatSkillTargeting = false; message = "위성 조준 취소"; Refresh(); return; }
            if (!battle.CanPlaceSatellite) { message = "이번 턴에는 이미 위성을 배치했습니다."; Refresh(); return; }
            combatSkillTargeting = true;
            message = "몸에서 3 이내에 위성을 놓으세요. 작도가 닿는 곳에 두면 몸이 밖에 있어도 보호받습니다.";
            Refresh();
        }

        private void PlaceSatelliteAt(double x, double y)
        {
            if (castActive || run == null) return;
            if (!run.TryPlaceSatellite(x, y)) { message = "몸에서 3 이내, 기둥·봉인 밖의 위치를 고르세요."; Refresh(); return; }
            combatSkillTargeting = false;
            message = battle.SatellitePlotHit ? "위성 연결 · 이번 작도로 루나 보호 가능" : "위성 설치 · 작도에 닿으면 루나 보호 / 조립 전에 J로 출발점 선택";
            Refresh();
        }

        private void ToggleSatelliteOrigin()
        {
            if (castActive || combatSkillTargeting || run == null) return;
            if (!run.TryToggleSatelliteOrigin()) { message = "위성 설치 후, 첫 카드를 놓기 전에 출발점을 고르세요."; Refresh(); return; }
            message = battle.SatelliteOriginSelected ? "다음 식은 위성에서 시작합니다. 몸은 자유롭게 이동하세요." : "다음 식은 몸에서 시작합니다. 위성은 자가 강화를 전달합니다.";
            Refresh();
        }

        private void DrawSatellite()
        {
            if (battle.Approach != CombatApproach.Observation) return;
            if (battle.HasSatellite)
            {
                Vector2 point = FieldPoint(battle.SatelliteX, battle.SatelliteY);
                Color color = battle.SatellitePlotHit ? Gold : Violet;
                Ring(point, (float)battle.Tactics.HitRadius * FieldUnit, color, 2);
                Diamond(point, 14, color, 2); Ring(point, 8, color, 1);
                if (battle.SatellitePlotHit)
                    Line(point, FieldPoint(battle.Tactics.X, battle.Tactics.Y), new Color(Gold.r,Gold.g,Gold.b,.4f), 2);
                Label(new Rect(point.x-125,point.y+62,250,29),battle.SatellitePlotHit ? "위성 → 루나 보호" : "위성 · 작도에 연결하세요",ui.SmallLight,true);
            }
            string hint = !battle.HasSatellite ? "먼저 K로 위성을 설치하세요 · 조립한 선 위에 놓으면 자가 강화 전달"
                : battle.Equation.HasBase ? "이미 만든 식은 고정 · 다음 조립 전에 J로 출발점 선택 / 위성은 적 공격을 대신 받지 않음"
                : battle.SatelliteOriginSelected ? "다음 식의 출발점: 위성 · J로 몸으로 전환" : "다음 식의 출발점: 몸 · J로 위성으로 전환";
            Vector2 pointer = Event.current.mousePosition;
            if (combatSkillTargeting && Field.Contains(pointer))
            {
                ScreenToField(pointer, out double x, out double y);
                x = System.Math.Round(x, 2); y = System.Math.Round(y, 2);
                bool valid = battle.ValidSatelliteAim(x, y);
                bool hit = valid && battle.Equation.HasBase && EquationAnalyzer.IntersectsCircle(battle.Equation,x,y,battle.Tactics.HitRadius,battle.Equation.CurveSegmentCount);
                Color color = !valid ? Threat : hit ? Gold : Violet;
                Ring(FieldPoint(battle.Tactics.X,battle.Tactics.Y),(float)battle.CurrentSatelliteReach*FieldUnit,new Color(Violet.r,Violet.g,Violet.b,.35f));
                Ring(FieldPoint(x,y),(float)battle.Tactics.HitRadius*FieldUnit,color,2); Diamond(FieldPoint(x,y),14,color,2);
                hint = !valid ? "몸에서 3 이내 · 기둥/봉인 밖을 선택하세요 / 우클릭 취소"
                    : hit ? "이 위치에 설치하면 이번 작도로 루나 보호 · 클릭 확정" : "설치 가능 · 현재 작도에는 닿지 않음 / 클릭 확정";
            }
            Fill(new Rect(1570,614,328,140),new Color(.06f,.055f,.1f,.94f));
            Label(new Rect(1580,622,308,124),hint,ui.SmallLight);
        }
    }
}

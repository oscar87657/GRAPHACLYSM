using Graphaclysm.Core.Combat;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private static readonly string[] ApproachUltimateNames={"","검은 종결선","불멸의 기록","시계 없는 밤","만월의 성역"};
        private static readonly string[] ApproachUltimateDescriptions={"",
            "작도로 맞힌 적마다 반경 1.8 파열. 주변 적에게 피해 12를 한 번 주고 돌진 대기를 초기화합니다.",
            "이번 도안을 남겨 다음 두 번의 방출에 추가 공격합니다. 재현 피해는 8 + 저장한 조립 위력. 카드 능력은 복제하지 않습니다.",
            "첫 방출 뒤 남은 손패로 한 번 더 조립합니다. 그 뒤 적이 행동합니다. 추가 드로우·이동 갱신은 없고, 두 번째 조립은 응축/궁극기 없이 방출하거나 포기합니다.",
            "이번 도안을 두 번의 적 행동까지 성역으로 남깁니다. 적 행동 직전 몸이나 위성이 선의 0.85 안에 있으면 보호막 10. 둘 다 닿아도 한 번입니다."};

        private string ultimateStateText="";
        private string sanctuaryProtectionText="";
        private void RefreshApproachUltimateView()
        {
            sanctuaryProtectionText="성역 접촉 · 적 행동 전 보호막 "+battle.CurrentSanctuaryShield;
            ultimateStateText = !battle.HasApproachUltimates ? "" : battle.InExtraAssembly
                ? "두 번째 조립 · 남은 손패 사용 / 해체로 포기"
                : battle.UltimateEchoes>0 ? "불멸의 기록 · 남은 재현 "+battle.UltimateEchoes+"회"
                : battle.SanctuaryActions>0 ? "성역 · 남은 적 행동 "+battle.SanctuaryActions+"회" : "";
        }
        private void DrawApproachUltimates()
        {
            if(!battle.HasApproachUltimates) return;
            if(battle.UltimateDiagram!=null) DrawDiagramGhost(battle.UltimateDiagram,
                battle.SanctuaryActions>0?new Color(.35f,1f,.8f,.7f):new Color(.9f,.7f,1,.8f));
            if(battle.Approach==CombatApproach.Execution && battle.Tactics.UltimateArmed)
                foreach(var enemy in battle.Enemies) if(battle.PreviewDamage(enemy)>0)
                    DrawClippedWarningRing(enemy.X,enemy.Y,battle.CurrentBurstRadius,new Color(.9f,.7f,1,.8f));
            if(ultimateStateText.Length>0) Label(new Rect(Field.x+16,Field.yMax-60,Field.width-32,27),ultimateStateText,ui.SmallLight);
            if(battle.PreviewSanctuaryProtection) Label(new Rect(Field.x+16,Field.yMax-31,Field.width-32,27),sanctuaryProtectionText,ui.SmallLight);
        }
    }
}

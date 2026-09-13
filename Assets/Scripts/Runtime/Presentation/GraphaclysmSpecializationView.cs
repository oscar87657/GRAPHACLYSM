using Graphaclysm.Core.Runs;
using Graphaclysm.Core.Combat;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;
namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private string StyleCapstoneText(bool ultimate)
        {
            if(!run.Growth.IsStyleTree)return "";
            for(int i=ultimate?5:3;i<16;i+=5)if(run.Growth.IsUnlocked(i))return "\n각성 · "+run.Growth.GetNode(i).Name;
            return "";
        }
        private string SpecializationSkillHint()
        {
            switch(battle.Approach)
            {
                case CombatApproach.Execution: return "K → 방향 클릭: " + battle.CurrentExecutionTravel + " 돌진 · 피해 " + (battle.HasRule(ApproachRule.DashPower)?16:10) + " · 파열 " + (battle.HasRule(ApproachRule.DashMark)?9:6) + ". 처치 시 재사용.";
                case CombatApproach.Recording: return "조립 → K → 기록 위치 선택 (거리 " + battle.CurrentDiagramReach + "). 이번 피해 " + (battle.HasRule(ApproachRule.RecordFull)?"100%":"75%") + ", 다음 " + (battle.HasRule(ApproachRule.RecordRepeat)?"두 번":"한 번") + "의 방출에 기록 재현.";
                case CombatApproach.Tuning: return "K: 도안 이동·회전 (거리 " + battle.CurrentDiagramReach + ") / J: 반경 " + battle.CurrentPullRadius + "의 적을 모으기. "
                    +(battle.HasRule(ApproachRule.TuneRepeat)?"조율은 한 턴 두 번.":battle.HasRule(ApproachRule.PullCompose)?"견인 뒤 조율 가능.":"한 턴에 둘 중 하나.");
                default: return "K: 위성 설치·이동 (거리 " + battle.CurrentSatelliteReach + ") / J: 다음 식의 출발점 선택. 위성에 작도가 닿으면 몸도 보호.";
            }
        }
        private string SpecializationUltimateHint()
        {
            switch(battle.Approach)
            {
                case CombatApproach.Execution: return "맞힌 위치마다 반경 " + battle.CurrentBurstRadius + " 파열 · 피해 " + battle.CurrentBurstPower + ". 겹쳐도 적마다 한 번. 돌진 대기 초기화.";
                case CombatApproach.Recording: return "이번 도안을 다음 " + (battle.HasRule(ApproachRule.EchoCount)?3:2) + "번의 방출에 재현. 재현 피해 " + (battle.HasRule(ApproachRule.EchoPower)?14:8) + " + 작도 위력.";
                case CombatApproach.Tuning: return "적 행동 전에 남은 손패로 한 번 더 조립·방출. 추가 드로우와 이동 갱신 없음." + (battle.HasRule(ApproachRule.ExtraSkill)?" 조율/견인은 다시 사용 가능.":"");
                default: return "이번 도안을 " + (battle.HasRule(ApproachRule.SanctuaryTime)?3:2) + "번의 적 행동 동안 성역으로 보존. 몸이나 위성이 닿으면 보호막 " + battle.CurrentSanctuaryShield + ".";
            }
        }
        private void DrawSpecializationDetail(RunGrowthState growth,bool canEdit)
        {
            int i=growthFocusedNode;var node=growth.GetNode(i);
            Label(new Rect(1466,305,378,60),node.Name,ui.HeadingLight);
            if(growth.IsStyleTree && i>0)Label(new Rect(1466,372,378,30),(i-1)%5>=3?ApproachUltimateNames[(int)growth.SpecializedApproach]+" · 궁극기 강화":"전투 기술 강화",ui.SmallLight);
            DrawExplainedGrowthText(new Rect(1466,405,378,205),node.Effect);
            Label(new Rect(1466,640,378,120),growthLayout.RequirementText[i],ui.SmallLight);
            bool blocked=IsGrowthExclusiveLocked(i,growth);
            string state=blocked?(growth.IsStyleTree?"같은 종류의 다른 각성을 선택했습니다. 기존 각성을 반환하면 변경할 수 있습니다.":"반대 갈래를 선택해 이 경로가 잠겼습니다."):growth.IsUnlocked(i)?"습득 완료 · 다음 전투부터 자동 적용":"선행을 따라 습득하세요. 클릭만으로 점수가 소비되지 않습니다.";
            Label(new Rect(1466,770,378,60),state,ui.SmallLight);
            if(ui.Button(new Rect(1466,835,378,44),i==0?"무료 출발 능력":growth.IsUnlocked(i)?"활성화됨":"습득 · "+node.Cost+"점",true,canEdit && growth.CanPurchase(i)))
            {run.TryPurchaseGrowthNode(i);Refresh();}
            if(i>0 && growth.IsUnlocked(i))
            {
                bool refund=growth.CanRefund(i);
                if(ui.Button(new Rect(1466,891,378,44),refund?"이 노드 반환 · "+node.Cost+"점":"후속 노드부터 반환하세요",false,canEdit&&refund))
                {run.TryRefundGrowthNode(i);Refresh();}
            }
            if(i==0 && ui.Button(new Rect(1466,891,378,44),"기본 운용 비교 · 시험하기"))
                OpenNodeComparison(growth.SpecializedApproach==CombatApproach.Execution?"ian.inscription.core":growth.SpecializedApproach==CombatApproach.Recording?"ian.archive.core":growth.SpecializedApproach==CombatApproach.Tuning?"luna.orbit.core":"luna.binary.core");
        }
    }
}

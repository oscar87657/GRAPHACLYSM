using System.Collections.Generic;
using Graphaclysm.Core.Runs;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private bool growthPlanOpen;
        private string[] growthPlanRows;
        private string growthPlanHeading;
        private Vector2 growthPlanScroll;
        private GUIStyle growthPlanStyle;
        private void OpenGrowthPlan()
        {
            if (growthFocusedNode < 0) return;
            var planner = new GrowthAcquisitionPlanner(run.Growth);
            var plans = planner.Plan(growthFocusedNode);
            growthPlanHeading = planner.NeedsChoices ? "갈림길이 많습니다. 가까운 선행부터 선택하세요."
                : plans.Count == 0 ? "이미 습득했거나 계획할 경로가 없습니다." : "선행 경로 " + plans.Count + "개 · 비용과 조건 비교";
            growthPlanRows = new string[plans.Count];
            for (int i = 0; i < plans.Count; i++)
            {
                var p = plans[i]; string row = "경로 " + (i + 1) + " · " + p.NodeIndices.Count + "개 / 총 " + p.Cost + "점"
                    + (p.Remaining < 0 ? " / " + -p.Remaining + "점 부족" : " / " + p.Remaining + "점 남음") + "\n";
                for (int j = 0; j < p.NodeIndices.Count; j++)
                { if (j > 0) row += " → "; row += run.Growth.GetNode(p.NodeIndices[j]).Name; }
                if (p.Blocker.Length > 0) row += "\n" + p.Blocker;
                if (p.UnsupportedCount > 0) row += "\n개별 효과 미구현 " + p.UnsupportedCount + "개 포함 · 실제 전투 능력으로 판매하지 않음";
                growthPlanRows[i] = row;
            }
            growthPlanScroll = Vector2.zero; growthPlanOpen = true;
        }
        private void DrawGrowthPlan()
        {
            if (growthPlanStyle == null) growthPlanStyle = new GUIStyle(ui.Light) { fontSize = 26 };
            Fill(new Rect(0, 0, 1920, 1080), new Color(.035f, .03f, .06f));
            Label(new Rect(90, 65, 1650, 60), "목표까지 필요한 선행", ui.PageTitleLight);
            Label(new Rect(90, 145, 1650, 45), growthPlanHeading, ui.Light);
            growthPlanScroll = GUI.BeginScrollView(new Rect(90, 215, 1740, 660), growthPlanScroll,
                new Rect(0, 0, 1700, Mathf.Max(650, growthPlanRows.Length * 180)));
            for (int i = 0; i < growthPlanRows.Length; i++)
            {
                Fill(new Rect(0, i * 180, 1680, 164), new Color(.1f, .085f, .16f));
                Label(new Rect(20, i * 180 + 12, 1640, 144), growthPlanRows[i], growthPlanStyle);
            }
            GUI.EndScrollView();
            Label(new Rect(90, 910, 1500, 40), "현재는 계획·비교만 가능합니다. 노드 습득이나 장착은 바뀌지 않습니다.", ui.Light);
            if (ui.Button(new Rect(1440, 980, 390, 55), "트리로 돌아가기", true)) growthPlanOpen = false;
        }
    }
}

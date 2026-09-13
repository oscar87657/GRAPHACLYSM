using Graphaclysm.Application;
using Graphaclysm.Core.Combat;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private NodeTrialComparison nodeComparison;
        private string nodeComparisonMessage="";
        private int trialGrowthNode;
        private Vector2 trialGrowthPan;
        private float trialGrowthZoom;
        private string trialGrowthSearch;

        private void OpenNodeComparison(string nodeId)
        {
            if(run==null || (!run.Growth.IsSpecialized && run.Growth.IndexOf(nodeId)<0) || NodeTrials.ApproachFor(nodeId)==CombatApproach.None) return;
            nodeComparison=new NodeTrialComparison(nodeId); nodeComparisonMessage="";
            hoveredKeywordTitle=hoveredKeywordBody="";
        }
        private void StartSelectedNodeTrial()
        {
            if(nodeComparison==null || !run.CanEditGrowth || run.IsPractice) return;
            if(!SaveCurrent(true)) { nodeComparisonMessage="원정 저장에 실패했습니다. 저장 문제를 해결한 뒤 체험할 수 있습니다."; return; }
            if(!flow.TryStartNodeTrial(nodeComparison.NodeId)) return;
            trialGrowthNode=growthFocusedNode; trialGrowthPan=growthPan; trialGrowthZoom=growthZoom; trialGrowthSearch=growthSearch;
            nodeComparison=null; growthOpen=growthPlanOpen=paused=helpOpen=inventoryOpen=false;
            castActive=combatSkillTargeting=false; confirmation=Confirmation.None;
            message=NodeTrials.Lesson(NodeTrials.ApproachFor(flow.TrialNodeId)); Refresh();
        }
        private void ReturnNodeTrial()
        {
            if(!flow.TryReturnFromNodeTrial()) return;
            nodeComparison=null; castActive=combatSkillTargeting=paused=helpOpen=inventoryOpen=settingsOpen=false;
            confirmation=Confirmation.None; growthPlanOpen=false; growthOpen=true;
            message="체험 종료 · 원래 원정과 성좌 선택으로 돌아왔습니다. 습득은 별도로 결정하세요."; Refresh();
            EnsureGrowthLayout(run.Growth);
            growthFocusedNode=trialGrowthNode; growthPan=trialGrowthPan; growthZoom=trialGrowthZoom;
            growthSearch=trialGrowthSearch??""; growthSearchCached=null; UpdateGrowthAncestors();
        }
        private void RestartNodeTrial()
        {
            if(!flow.IsNodeTrial || !flow.TryRestartRun()) return;
            castActive=combatSkillTargeting=paused=helpOpen=inventoryOpen=growthOpen=false;
            message=NodeTrials.Lesson(NodeTrials.ApproachFor(flow.TrialNodeId)); Refresh();
        }
        private void DrawNodeComparison()
        {
            var comparison=nodeComparison;
            Fill(new Rect(0,0,1920,1080),new Color(.025f,.02f,.045f,.98f));
            Border(new Rect(180,100,1560,895),Gold,20);
            Label(new Rect(240,124,1440,50),NodeTrials.Title(comparison.Approach),ui.HeadingLight);
            Label(new Rect(240,179,1440,40),"같은 손패·적 배치로 보는 대표 운용 · 현재 원정과 별개의 기본 능력 예제입니다.",ui.SmallLight);
            Label(new Rect(250,228,620,35),"기술 사용 전",ui.Light,true);
            Label(new Rect(1050,228,620,35),"기술 사용 후",ui.Light,true);
            DrawTrialBoard(new Rect(250,277,620,496),comparison.Before,comparison.BeforeEnemyText,false);
            DrawTrialBoard(new Rect(1050,277,620,496),comparison.After,comparison.AfterEnemyText,true);
            Label(new Rect(891,456,138,65),"→",ui.PageTitle,true);
            DrawExplainedText(new Rect(250,780,620,40),comparison.BeforeSummary,ui.SmallLight);
            DrawExplainedText(new Rect(1050,778,620,51),comparison.AfterSummary,ui.SmallLight);
            DrawExplainedText(new Rect(250,831,1420,60),NodeTrials.Lesson(comparison.Approach),ui.SmallLight);
            if(ui.Button(new Rect(250,918,420,48),"성좌로 돌아가기   Esc")) nodeComparison=null;
            bool canTry=run!=null && run.CanEditGrowth && !run.IsPractice && !flow.IsNodeTrial;
            if(ui.Button(new Rect(1050,918,620,48),canTry?"직접 시험하기 · 원정에 영향 없음":"직접 체험은 지도·휴식에서 가능",true,canTry)) StartSelectedNodeTrial();
            if(nodeComparisonMessage.Length>0) Label(new Rect(680,919,355,58),nodeComparisonMessage,ui.SmallLight);
        }
        private static Vector2 TrialPoint(Rect rect,double x,double y)
            => new Vector2(rect.x+(float)x*rect.width/10,rect.y+(4-(float)y)*rect.height/8);
        private void DrawTrialBoard(Rect rect,RunGameSession session,string[] enemyText,bool after)
        {
            var b=session.CurrentBattle.Battle;
            Fill(rect,new Color(.075f,.064f,.12f,1)); Border(rect,new Color(.4f,.36f,.5f,.7f));
            Line(TrialPoint(rect,0,0),TrialPoint(rect,10,0),new Color(.5f,.5f,.6f,.25f));
            spellRenderer.DrawGhost(b.Equation,rect,new Color(.85f,.83f,1,.9f));
            if(after && nodeComparison.RecordGhost!=null)
                spellRenderer.DrawGhost(nodeComparison.RecordGhost,rect,new Color(.68f,.38f,1,.7f));
            if(after && b.Approach==CombatApproach.Execution)
            {
                Vector2 from=TrialPoint(rect,b.LastSkillOriginX,b.LastSkillOriginY), to=TrialPoint(rect,b.LastSkillEndX,b.LastSkillEndY);
                Line(from,to,Gold,3); Diamond(from,9,Gold); Diamond(to,12,Gold,2);
            }
            for(int i=0;i<b.Enemies.Count;i++)
            {
                var enemy=b.Enemies[i]; Vector2 point=TrialPoint(rect,enemy.X,enemy.Y);
                Color color=b.PreviewDamage(enemy)>0?Threat:Violet;
                Diamond(point,17,color,2); Ring(point,29,color,1);
                float labelX=Mathf.Clamp(point.x-110,rect.x+3,rect.xMax-223);
                Label(new Rect(labelX,point.y+32,220,62),enemyText[i],ui.SmallLight,true);
            }
            Vector2 body=TrialPoint(rect,b.Tactics.X,b.Tactics.Y);
            Disc(body,15,new Color(.38f,.32f,.55f)); Ring(body,25,b.PreviewPlayerHit?Gold:Violet,2);
            // Put the player label above to keep it separate from enemy health captions.
            Label(new Rect(body.x-65,body.y-64,130,30),b.PreviewPlayerHit?"몸 · 보호":"몸",ui.SmallLight,true);
            if(b.HasSatellite)
            {
                Vector2 satellite=TrialPoint(rect,b.SatelliteX,b.SatelliteY);
                Ring(satellite,21,Gold,2); Diamond(satellite,8,Gold);
                Label(new Rect(satellite.x-55,satellite.y-53,110,28),"위성",ui.SmallLight,true);
                if(b.SatellitePlotHit) Line(satellite,body,Gold,2);
            }
        }
    }
}

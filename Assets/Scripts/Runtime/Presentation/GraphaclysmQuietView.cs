using Graphaclysm.Core.Combat;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;
namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private bool showBattleDetails, showSecondaryActions;
        private string quietWeave = "", quietCondense = "", lastFeedback = "", condenseHint = "";
        private float feedbackUntil;
        private Rect EnemyRow(int index) => battle.UsesFragments ? new Rect(1580,128+index*82,308,74) : new Rect(1490,187+index*143,335,130);
        private void DrawQuietPlayer()
        {
            Fill(new Rect(28,92,314,218),new Color(.055f,.05f,.095f,.88f));
            Border(new Rect(28,92,314,218),new Color(Gold.r,Gold.g,Gold.b,.48f),14);
            Label(new Rect(48,105,180,38),flow.CurrentCharacter.DisplayName,ui.HeadingLight);
            Label(new Rect(225,108,98,34),hpText,ui.SmallLight,true);
            Fill(new Rect(48,151,274,3),new Color(1,1,1,.13f));
            Fill(new Rect(48,151,274f*battle.PlayerHealth/battle.PlayerMaxHealth,3),Violet);
            DrawStatusChips(battle.Tactics.Statuses,new Rect(48,164,274,38),true,4);
            Rect ultimate = new Rect(48,210,274,43);
            if(ui.Button(ultimate,ultimateText,battle.Tactics.UltimateArmed,!castActive && battle.Tactics.Resonance>=6)) {run.TryToggleUltimate();Refresh();}
            for(int i=0;i<6;i++) Fill(new Rect(49+i*46,259,39,3),i<battle.Tactics.Resonance?Violet:new Color(.32f,.30f,.39f));
            Rect skill = new Rect(48,270,274,31);
            if(ui.Button(skill,combatSkillText,false,!castActive && battle.CanUseCombatSkill)) UseCombatSkill();
            RegisterKeyword(skill,GrowthSkillName(),combatSkillDescription);
            if(ultimate.Contains(Event.current.mousePosition) || showBattleDetails)
                RegisterKeyword(ultimate,ultimateText,ultimateDescription);
            DrawQuietLoom();
            if(battle.Tactics.CanUndoMove && !castActive)
                if(ui.Button(new Rect(28,744,145,40),"이동 취소")) UndoMove();
            DrawMovementDock();
            if(showBattleDetails)
            {
                Fill(new Rect(28,930,314,96),new Color(.055f,.05f,.095f,.86f));
                Label(new Rect(45,936,280,31),drawStatus,ui.SmallLight);
                Label(new Rect(45,969,280,48),selfStateText.Length>0?selfStateText:"적용 중인 상태 없음",ui.SmallLight);
            }
        }
        private void DrawQuietLoom()
        {
            Fill(new Rect(28,326,314,398),new Color(.055f,.05f,.095f,.82f));
            Border(new Rect(28,326,314,398),new Color(Gold.r,Gold.g,Gold.b,.34f),14);
            Label(new Rect(48,338,274,35),quietWeave,ui.Light);
            if(battle.PlayedCardCount==0)
            {
                Diamond(new Vector2(185,514),32,new Color(Violet.r,Violet.g,Violet.b,.62f));
                Label(new Rect(48,566,274,38),"첫 파편을 놓으세요",ui.SmallLight,true);
            }
            for(int i=0;i<battle.PlayedCardCount;i++)
            {
                var card=battle.GetPlayedCard(i);float y=383+i*40;
                float enter=!preferences.ReduceMotion && i==battle.PlayedCardCount-1 ? Mathf.Clamp01(1-(ViewTime-fragmentPlacedAt)*5)*10:0;
                Color color=i<battle.SealedCardCount?Muted:Violet;
                Line(new Vector2(48+i*3,y+enter),new Vector2(48+i*3,y+27+enter),color,1.4f);
                Label(new Rect(63+i*3,y-2+enter,168,31),card.DisplayName,ui.SmallLight);
                if(showBattleDetails) Label(new Rect(230,y+enter,91,30),CompactMark(card.Fragment),ui.SmallLight,true);
                else Diamond(new Vector2(315,y+14+enter),3,Rarity(card.Rarity));
            }
            if(battle.PlayedCardCount>battle.SealedCardCount && !castActive)
                if(ui.Button(new Rect(190,744,152,40),"파편 취소")) Undo();
        }

        private string GrowthSkillName()
        {
            int variant = run.Growth.ActiveVariant;
            if (flow.CurrentCharacter.Archetype == CombatArchetype.Ian)
                return variant == 1 ? "삼중 유리길   K" : variant == 2 ? "집행의 직선   K" : "유리 쇄도   K";
            return variant == 1 ? "삼중 월광   K" : variant == 2 ? "낙성 추격   K" : "월광 도약   K";
        }

        private string GrowthSkillDescription()
        {
            int variant = run.Growth.ActiveVariant;
            int module = run.Growth.ModuleVariant;
            bool fiveLanes = run.Growth.IsUnlocked(2);
            bool executionResonance = run.Growth.IsUnlocked(4);
            string form;
            if (flow.CurrentCharacter.Archetype == CombatArchetype.Ian)
            {
                form = variant == 1 ? (fiveLanes ? "넓은 다섯 갈래로 이동하며 피해 5" : "넓은 세 갈래로 이동하며 피해 6") : variant == 2
                    ? "직선으로 이동하며 피해 10 · 처치 시 즉시 재사용" + (executionResonance ? "·공명 +1" : "") : "직선으로 이동하며 피해 7";
                return "대기 3턴 · 가리킨 적 방향으로 " + form + (module == 1 ? " · 적중마다 보호막" : module == 2 ? " · 파열 3" : "");
            }
            form = variant == 1 ? (fiveLanes ? "넓은 다섯 갈래로 이동하며 피해 4" : "넓은 세 갈래로 이동하며 피해 5") : variant == 2
                ? "직선으로 이동하며 피해 8 · 처치 시 즉시 재사용" + (executionResonance ? "·공명 +1" : "") : "직선으로 이동하며 피해 6";
            return "대기 3턴 · 가리킨 적 방향으로 " + form + (module == 1 ? " · 정화/보호막/요새화" : module == 2 ? " · 회복/추진" : "");
        }

        private string UltimateSkillDescription()
        {
            int variant = run.Growth.UltimateVariant;
            bool resonanceRefund = run.Growth.IsUnlocked(8);
            bool firstAugment = run.Growth.IsUnlocked(9);
            bool secondAugment = run.Growth.IsUnlocked(10);
            bool selfAugment = run.Growth.IsUnlocked(11);
            string charge = "\n충전: 적 2명 이상 +1 · 자신과 적 동시 +2" + (resonanceRefund ? " · 궁극 다중 적중 환급 +1" : "");
            if (flow.CurrentCharacter.Archetype == CombatArchetype.Ian)
                return variant == 1 ? "공명 6 · 피해 +" + (firstAugment ? 14 : 10) + ", 파열 3" + charge
                    : variant == 2 ? "공명 6 · 피해 +6, 고정 " + (secondAugment ? 3 : 2) + ", 자신 적중 시 회복 " + (selfAugment ? 8 : 4) + charge
                    : "공명 6 · 피해 +6, 적중한 적 이동 봉쇄" + charge;
            return variant == 1 ? "공명 6 · 자가 반경 " + (firstAugment ? "1.50" : "1.25") + ", 정화, 보호막 " + (firstAugment ? 16 : 12) + ", 회복 " + (firstAugment ? 9 : 7) + charge
                : variant == 2 ? "공명 6 · 피해 +" + (secondAugment ? 8 : 4) + ", 정화, 보호막 " + (selfAugment ? 10 : 6) + ", 회복 " + (selfAugment ? 7 : 3) + charge
                : "공명 6 · 자가 반경 확대, 정화, 보호막 8, 회복 5" + charge;
        }
        private void DrawQuietEnemyPanel()
        {
            Fill(new Rect(1564,92,340,420),new Color(.055f,.05f,.095f,.88f));
            Border(new Rect(1564,92,340,420),new Color(Gold.r,Gold.g,Gold.b,.45f),14);
            Label(new Rect(1580,101,308,32),"다음 행동",ui.Light);
            for(int i=0;i<battle.Enemies.Count;i++)
            {
                var enemy=battle.Enemies[i];Rect r=EnemyRow(i);
                if(hoveredEnemy==i) Fill(r,new Color(1,1,1,.10f));
                Line(new Vector2(r.x,r.yMax),new Vector2(r.xMax,r.yMax),new Color(Gold.r,Gold.g,Gold.b,.25f));
                Label(new Rect(r.x,r.y+2,34,30),enemyBadges[i],ui.SmallLight);
                Label(new Rect(r.x+39,r.y+2,184,30),enemy.Definition.DisplayName,ui.Light);
                Label(new Rect(r.x+224,r.y+3,84,27),enemyHealth[i],ui.SmallLight,true);
                Label(new Rect(r.x+39,r.y+35,150,29),enemy.IsAlive?enemyIntent[i]:"소멸",ui.SmallLight);
                DrawStatusChips(enemy.Statuses,new Rect(r.x+172,r.y+34,136,34),true,2);
            }
            if(hoveredEnemy>=0 || showBattleDetails)
            {
                int index=hoveredEnemy>=0?hoveredEnemy:0;
                Fill(new Rect(1564,526,340,174),new Color(.055f,.05f,.095f,.82f));
                Label(new Rect(1580,539,308,32),battle.Enemies[index].Definition.DisplayName,ui.Light);
                Label(new Rect(1580,578,308,108),enemyStatuses[index].Length>0?enemyStatuses[index]:"적용 중인 상태 없음",ui.SmallLight);
            }
            Fill(new Rect(1564,718,340,300),new Color(.055f,.05f,.095f,.88f));
            Label(new Rect(1580,734,308,31),castActive?"선을 새기는 중":outcomeText,ui.SmallLight,true);
            if(ui.Button(new Rect(1580,777,308,64),castActive?lastPlotName:"방출   ↵",true,!castActive && battle.CanPlot))StartCast();
            if(ui.Button(new Rect(1580,855,308,48),quietCondense,false,!castActive && battle.CanCondense))Condense();
            if(ui.Button(new Rect(1798,921,90,42),"해체",showSecondaryActions,!castActive))showSecondaryActions=!showSecondaryActions;
            if(showSecondaryActions)
            {
                if(ui.Button(new Rect(1580,921,202,42),"해체하고 넘기기",false,!castActive)) {showSecondaryActions=false;Unravel();}
            }
            else Label(new Rect(1580,914,202,55),condenseHint,ui.SmallLight);
        }
    }
}

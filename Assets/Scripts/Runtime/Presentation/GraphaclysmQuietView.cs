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
        private Rect EnemyRow(int index) => battle.UsesFragments ? new Rect(1450,164+index*84,400,76) : new Rect(1490,187+index*143,335,130);
        private void DrawQuietPlayer()
        {
            Label(new Rect(65,115,300,43),flow.CurrentCharacter.DisplayName,ui.Heading);
            Label(new Rect(65,171,300,39),hpText,ui.Number);
            Fill(new Rect(65,221,300,3),new Color(Ink.r,Ink.g,Ink.b,.13f));
            Fill(new Rect(65,221,300f*battle.PlayerHealth/battle.PlayerMaxHealth,3),Violet);
            Rect ultimate = new Rect(65,243,300,43);
            if(ui.Button(ultimate,ultimateText,battle.Tactics.UltimateArmed,!castActive && battle.Tactics.Resonance>=6)) {run.TryToggleUltimate();Refresh();}
            for(int i=0;i<6;i++) Fill(new Rect(65+i*51,298,45,3),i<battle.Tactics.Resonance?Violet:new Color(.78f,.76f,.80f));
            if(ultimate.Contains(Event.current.mousePosition) || showBattleDetails)
                Label(new Rect(65,312,300,48),flow.CurrentCharacter.Archetype==CombatArchetype.Ian ? "공명 6 · 피해 +6 / 적 이동 봉쇄" : "공명 6 · 자기 적중 확대\n정화 · 보호막 8 · 회복 5",ui.Small);
            DrawQuietLoom();
            if(battle.Tactics.HasMoved && !castActive)
                if(ui.Button(new Rect(65,756,142,40),"이동 취소")) UndoMove();
            DrawMovementDock();
            if(showBattleDetails)
            {
                Label(new Rect(65,920,300,33),drawStatus,ui.Small);
                Label(new Rect(65,961,300,86),selfStateText.Length>0?selfStateText:"적용 중인 상태 없음",ui.Small);
            }
        }
        private void DrawQuietLoom()
        {
            Label(new Rect(65,367,300,35),quietWeave,ui.Body);
            if(battle.PlayedCardCount==0)
            {
                Diamond(new Vector2(215,525),28,new Color(Violet.r,Violet.g,Violet.b,.45f));
                Label(new Rect(65,571,300,38),"첫 파편을 놓으세요",ui.Small,true);
            }
            for(int i=0;i<battle.PlayedCardCount;i++)
            {
                var card=battle.GetPlayedCard(i);float y=420+i*39;
                float enter=i==battle.PlayedCardCount-1 ? Mathf.Clamp01(1-(Time.unscaledTime-fragmentPlacedAt)*5)*10:0;
                Color color=i<battle.SealedCardCount?Muted:Violet;
                Line(new Vector2(66+i*3,y+enter),new Vector2(66+i*3,y+27+enter),color,1.4f);
                Label(new Rect(82+i*3,y-2+enter,168,31),card.DisplayName,ui.Body);
                if(showBattleDetails) Label(new Rect(255,y+enter,108,30),CompactMark(card.Fragment),ui.Small,true);
                else Diamond(new Vector2(351,y+14+enter),3,Rarity(card.Rarity));
            }
            if(battle.PlayedCardCount>battle.SealedCardCount && !castActive)
                if(ui.Button(new Rect(223,756,142,40),"파편 취소")) Undo();
        }
        private void DrawQuietEnemyPanel()
        {
            Label(new Rect(1450,118,400,32),"다음 행동",ui.Body);
            for(int i=0;i<battle.Enemies.Count;i++)
            {
                var enemy=battle.Enemies[i];Rect r=EnemyRow(i);
                if(hoveredEnemy==i) Fill(r,new Color(1,1,1,.38f));
                Line(new Vector2(r.x,r.yMax),new Vector2(r.xMax,r.yMax),new Color(Gold.r,Gold.g,Gold.b,.25f));
                Label(new Rect(r.x,r.y+2,39,30),enemyBadges[i],ui.Small);
                Label(new Rect(r.x+44,r.y+2,350,30),enemy.Definition.DisplayName,ui.Body);
                Label(new Rect(r.x+44,r.y+39,245,29),enemy.IsAlive?enemyIntent[i]:"소멸",ui.Body);
                Label(new Rect(r.x+300,r.y+39,100,29),enemyHealth[i],ui.Small);
            }
            if(hoveredHand>=0 && hoveredHand<game.Deck.HandCount)
            {
                var skill=Visual(game.Deck.GetHandCard(hoveredHand));
                if(skill!=null)
                {
                    Fill(new Rect(1440,438,420,357),new Color(1,1,1,.78f));
                    Label(new Rect(1458,455,384,40),skill.Card.DisplayName,ui.Heading);
                    Label(new Rect(1458,505,384,277),skill.Details,ui.Body);
                }
            }
            else if(hoveredEnemy>=0 || showBattleDetails)
            {
                int index=hoveredEnemy>=0?hoveredEnemy:0;
                Label(new Rect(1450,455,400,35),battle.Enemies[index].Definition.DisplayName,ui.Body);
                Label(new Rect(1450,500,400,230),enemyStatuses[index].Length>0?enemyStatuses[index]:"적용 중인 상태 없음",ui.Body);
            }
            Label(new Rect(1450,810,400,29),castActive?"선을 새기는 중":outcomeText,ui.Small,true);
            if(ui.Button(new Rect(1450,850,400,66),castActive?lastPlotName:"방출   ↵",true,!castActive && battle.CanPlot))StartCast();
            if(ui.Button(new Rect(1450,930,400,51),quietCondense,false,!castActive && battle.CanCondense))Condense();
            if(ui.Button(new Rect(1762,1002,88,42),"해체",showSecondaryActions,!castActive))showSecondaryActions=!showSecondaryActions;
            if(showSecondaryActions)
            {
                if(ui.Button(new Rect(1450,1002,295,42),"해체하고 넘기기",false,!castActive)) {showSecondaryActions=false;Unravel();}
            }
            else Label(new Rect(1450,995,290,55),condenseHint,ui.Small);
        }
    }
}

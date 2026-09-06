using UnityEngine;
using Graphaclysm.Core.Equations;
using static Graphaclysm.Runtime.Presentation.AstralUi;
namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private string weaveStatus = "", drawStatus = "", burstStatus = "";
        private float fragmentPlacedAt;
        private void DrawFragmentLoom()
        {
            Fill(new Rect(91,243,339,392),new Color(Paper.r,Paper.g,Paper.b,0.95f));
            Label(new Rect(103,247,312,30),"안쪽의 식을 다음 파편으로 감싸기",ui.Small);
            if (battle.PlayedCardCount == 0)
            {
                Diamond(new Vector2(260,397),47,Violet,1.4f);
                Label(new Rect(112,450,297,65),"어느 파편이든 시작할 수 있습니다.",ui.Body,true);
                Label(new Rect(112,532,297,69),"손패를 고르면 이곳에 쌓입니다.\n순서가 바뀌면 무늬도 바뀝니다.",ui.Small,true);
            }
            for(int i=0;i<battle.PlayedCardCount;i++)
            {
                var card=battle.GetPlayedCard(i); float inset=i*3;
                float enter=i==battle.PlayedCardCount-1 ? Mathf.Clamp01(1-(Time.unscaledTime-fragmentPlacedAt)*5) : 0;
                Rect r=new Rect(100+inset,281+i*40+enter*12,320-inset*2,37);
                Fill(r,i<battle.SealedCardCount ? new Color(.88f,.87f,.9f) : Color.white);
                Border(r,Rarity(card.Rarity),7);
                Label(new Rect(r.x+9,r.y+2,116,32),card.DisplayName,ui.Small);
                Label(new Rect(r.x+124,r.y+2,r.width-134,32),card.FormulaLabel,ui.Small,true);
                if(i>0) Line(new Vector2(r.x-5,r.y-9),new Vector2(r.x-5,r.y+17),Violet,1.5f);
            }
            Label(new Rect(101,607,322,27),weaveStatus,ui.Small);
        }
        private void DrawFragmentCard(Rect r, SkillVisual visual, bool hovered, bool large)
        {
            bool inspect = !codexOpen && (large || (hovered && r.height >= 220) || (!castActive && (run == null || run.Phase != Graphaclysm.Application.RunPhase.Battle)));
            Color rarity = Rarity(visual.Card.Rarity), accent = RoleColor(visual.Card.Fragment);
            Fill(new Rect(r.x+3,r.y+5,r.width,r.height),new Color(.16f,.12f,.24f,.15f));
            Fill(r, hovered ? Color.white : new Color(.95f,.94f,.93f)); Border(r,rarity);
            Fill(new Rect(r.x+10,r.y,r.width-20,3),rarity);
            Label(new Rect(r.x+5,r.y+5,r.width-10,large?48:30),visual.Card.DisplayName,large?ui.Heading:ui.Body,true);
            DrawFragmentEmblem(new Vector2(r.center.x,r.y+r.height*(r.height<180 ? .34f : .35f)),Mathf.Min(r.width*.26f,r.height*(r.height<180 ? .14f : .17f)),visual,accent);
            Label(new Rect(r.x+4,r.y+r.height*(inspect ? .50f : .53f),r.width-8,large?42:(r.height<180?24:28)),r.width<200?CompactMark(visual.Card.Fragment):visual.Card.FormulaLabel,large?ui.Heading:ui.Small,true);
            if(inspect)
            {
                Label(new Rect(r.x+7,r.yMax-(large?119:83),r.width-14,large?44:28),visual.Card.Description,large?ui.Body:ui.Small,true);
                Line(new Vector2(r.x+10,r.yMax-(large?68:51)),new Vector2(r.xMax-10,r.yMax-(large?68:51)),rarity);
                Label(new Rect(r.x+5,r.yMax-(large?61:48),r.width-10,large?55:46),r.width<165?visual.CompactAbilities:visual.Abilities,large?ui.Body:ui.Small,true);
            }
            else
            {
                Label(new Rect(r.x+4,r.y+r.height*(r.height<180?.69f:.71f),r.width-8,r.height<180?22:26),r.width<140?RoleLabels[CardRole(visual.Card.Fragment)]:visual.Card.Description,ui.Small,true);
                Label(new Rect(r.x+4,r.yMax-(r.height<180?24:29),r.width-8,r.height<180?20:25),visual.PowerBadge,ui.Small,true);
            }
        }

        private static string CompactMark(FragmentKind kind)
        {
            switch(kind)
            {
                case FragmentKind.Counterpoint: return "↶□";
                case FragmentKind.Orbit: return "eⁱ□";
                case FragmentKind.Petal: return "≈□";
                case FragmentKind.Expand: return "×□";
                case FragmentKind.Contract: return "□÷";
                case FragmentKind.Mirror: return "□*";
                case FragmentKind.TranslateRight: return "□+";
                case FragmentKind.TranslateDown: return "□−";
                case FragmentKind.Square: return "□²";
                case FragmentKind.Lissajous: return "2:3□";
                case FragmentKind.Epitrochoid: return "□−□₄";
                case FragmentKind.Limacon: return "◔□";
                case FragmentKind.Shear: return "x+.7y";
                case FragmentKind.PhaseOffset: return "¼↔□";
                case FragmentKind.ComplexCube: return "□³/4";
                case FragmentKind.HomeAnchor: return "O→나";
                case FragmentKind.WestAnchor: return "O←2";
                case FragmentKind.NorthAnchor: return "O↑1.5";
                case FragmentKind.TwinEcho: return "¼↶□";
                case FragmentKind.StarPetal: return "5≈□";
                case FragmentKind.Surge: return "1.7□";
                case FragmentKind.Ellipse: return "↔□";
                default: return "□↻";
            }
        }
        private void Condense()
        {
            if(castActive || run == null || !run.TryCondense())return;
            run.ResolveEnemyTurn(); hoveredHand=-1; message="체력을 써서 식을 보존했습니다. 응축 드로우는 최대 2장입니다."; Refresh();
        }
        private void Unravel()
        {
            if(castActive || run == null || !run.TryUnravel())return;
            run.ResolveEnemyTurn(); hoveredHand=-1; message="조립한 파편을 버리고 다시 준비합니다."; Refresh();
        }
    }
}

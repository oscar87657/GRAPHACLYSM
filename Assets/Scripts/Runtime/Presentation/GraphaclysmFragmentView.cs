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
                float enter=!preferences.ReduceMotion && i==battle.PlayedCardCount-1 ? Mathf.Clamp01(1-(ViewTime-fragmentPlacedAt)*5) : 0;
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
            bool inspect = large;
            Color rarity = Rarity(visual.Card.Rarity), accent = RoleColor(visual.Card.Fragment);
            Fill(new Rect(r.x+7,r.y+8,r.width,r.height),new Color(0,0,0,.28f));
            Fill(r, hovered ? new Color(.13f,.12f,.19f,.98f) : new Color(.095f,.09f,.15f,.98f));
            Border(r,rarity,large?17:11);
            Fill(new Rect(r.x,r.y+14,6,r.height-28),accent);
            Fill(new Rect(r.x+13,r.y+12,r.width-26,large?52:39),new Color(1,1,1,.055f));
            Label(new Rect(r.x+18,r.y+10,r.width-36,large?55:42),visual.Card.DisplayName,large?ui.HeadingLight:ui.Light,true);
            Label(new Rect(r.x+18,r.y+(large?62:50),r.width-36,25),RoleLabels[CardRole(visual.Card.Fragment)]+" · "+RarityNames[(int)visual.Card.Rarity],ui.SmallLight,true);
            DrawFragmentEmblem(new Vector2(r.center.x,r.y+r.height*(large?.36f:.42f)),Mathf.Min(r.width*(large?.22f:.27f),r.height*(large?.15f:.16f)),visual,accent);
            Label(new Rect(r.x+12,r.y+r.height*(large?.53f:.61f),r.width-24,large?39:30),large?visual.Card.FormulaLabel:CompactMark(visual.Card.Fragment),large?ui.Light:ui.SmallLight,true);
            if(inspect)
            {
                Label(new Rect(r.x+23,r.y+220,r.width-46,54),visual.Card.Description,ui.SmallLight,true);
                Line(new Vector2(r.x+20,r.y+281),new Vector2(r.xMax-20,r.y+281),new Color(rarity.r,rarity.g,rarity.b,.65f));
                DrawCardKeywords(new Rect(r.x+22,r.y+292,r.width-44,32),visual,true);
                Label(new Rect(r.x+22,r.y+331,r.width-44,28),visual.PowerBadge,ui.SmallLight,true);
            }
            else
            {
                Label(new Rect(r.x+12,r.yMax-73,r.width-24,37),visual.Card.Description,ui.SmallLight,true);
                Fill(new Rect(r.x+15,r.yMax-30,r.width-30,1),new Color(rarity.r,rarity.g,rarity.b,.5f));
                Label(new Rect(r.x+12,r.yMax-29,r.width-24,25),visual.PowerBadge,ui.SmallLight,true);
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

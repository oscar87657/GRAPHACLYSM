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
                var card=battle.GetPlayedCard(i); const float inset=0;
                float enter=!preferences.ReduceMotion && i==battle.PlayedCardCount-1 ? Mathf.Clamp01(1-(ViewTime-fragmentPlacedAt)*5) : 0;
                Rect r=new Rect(100+inset,281+i*40+enter*12,320-inset*2,37);
                Fill(r,i<battle.SealedCardCount ? new Color(.88f,.87f,.9f) : Color.white);
                Border(r,Rarity(card.DiagramRarity),7);
                Label(new Rect(r.x+9,r.y+2,116,32),card.DisplayName,ui.Small);
                Label(new Rect(r.x+124,r.y+2,r.width-134,32),card.FormulaLabel,ui.Small,true);
                if(i>0) Line(new Vector2(r.x-5,r.y-9),new Vector2(r.x-5,r.y+17),Violet,1.5f);
            }
            Label(new Rect(101,607,322,27),weaveStatus,ui.Small);
        }
        private void DrawFragmentCard(Rect r, SkillVisual visual, bool hovered, bool large)
        {
            bool inspect = large;
            Color rarity = Rarity(visual.Card.DiagramRarity), accent = rarity;
            Fill(new Rect(r.x+7,r.y+8,r.width,r.height),new Color(0,0,0,.28f));
            Fill(r, hovered ? new Color(.13f,.12f,.19f,.98f) : new Color(.095f,.09f,.15f,.98f));
            Border(r,rarity,large?17:11);
            Fill(new Rect(r.x,r.y+14,6,r.height-28),accent);
            Fill(new Rect(r.x+13,r.y+12,r.width-26,large?52:39),new Color(rarity.r,rarity.g,rarity.b,.16f));
            Label(new Rect(r.x+18,r.y+10,r.width-36,large?55:42),visual.Card.DisplayName,large?ui.HeadingLight:ui.Light,true);
            Label(new Rect(r.x+18,r.y+(large?62:50),r.width-36,25),RoleLabels[CardRole(visual.Card.Fragment)],ui.SmallLight,true);
            DrawFragmentEmblem(new Vector2(r.center.x,r.y+r.height*(large?.30f:.42f)),Mathf.Min(r.width*(large?.18f:.27f),r.height*(large?.10f:.16f)),visual,accent);
            if(!large) Label(new Rect(r.x+12,r.y+r.height*.61f,r.width-24,30),CompactMark(visual.Card.Fragment),ui.SmallLight,true);
            if(inspect)
            {
                DrawExplainedText(new Rect(r.x+23,r.y+185,r.width-46,54),visual.Card.Description,ui.SmallLight,true);
                Line(new Vector2(r.x+20,r.y+250),new Vector2(r.xMax-20,r.y+250),new Color(rarity.r,rarity.g,rarity.b,.65f));
                DrawReadableCardBadges(new Rect(r.x+18,r.y+263,r.width-36,104),visual,r);
            }
            else
            {
                Label(new Rect(r.x+12,r.yMax-65,r.width-24,49),visual.Card.Description,ui.SmallLight,true);
            }
        }

        private static string CompactMark(FragmentKind kind)
        {
            if(AdvancedWeaves.IsAdvanced(kind)) return AdvancedWeaves.Get(kind).Symbol;
            switch(kind)
            {
                case FragmentKind.VerticalWeave: return ".65x / 1.4y";
                case FragmentKind.QuarterTurn: return "90° ↶";
                case FragmentKind.DiagonalWeave: return "y+.7x";
                case FragmentKind.DoubleLoop: return "1:2 □";
                case FragmentKind.EastAnchor: return "O→2";
                case FragmentKind.SouthAnchor: return "O↓1.5";
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
                case FragmentKind.CometBurst: return "7✦□";
                case FragmentKind.KaleidoscopeFold: return "◇4□";
                case FragmentKind.ShardFracture: return "6⌁□";
                case FragmentKind.NebulaRibbon: return "5:2□";
                default: return "□↻";
            }
        }
        private void Condense()
        {
            if(castActive || combatSkillTargeting || run == null || !run.TryCondense())return;
            run.ResolveEnemyTurn(); hoveredHand=-1; message="체력을 써서 식을 보존했습니다. 응축 드로우는 최대 2장입니다."; Refresh();
        }
        private void Unravel()
        {
            if(castActive || combatSkillTargeting || run == null || !run.TryUnravel())return;
            run.ResolveEnemyTurn(); hoveredHand=-1; message="조립한 파편을 버리고 다시 준비합니다."; Refresh();
        }
    }
}

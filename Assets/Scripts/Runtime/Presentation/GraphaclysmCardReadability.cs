using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        // View-owned styles/content; the strings are cached with each immutable card visual.
        private GUIStyle effectTitleStyle, effectBodyStyle, effectSmallStyle, effectNoteStyle;
        private readonly GUIContent effectMeasure = new GUIContent();
        private float maximumEffectOverflow;
        private void EnsureEffectStyles()
        {
            if(effectTitleStyle!=null) return;
            effectTitleStyle=new GUIStyle(ui.Small) { fontStyle=FontStyle.Bold, alignment=TextAnchor.UpperLeft };
            effectBodyStyle=new GUIStyle(ui.Body) { alignment=TextAnchor.UpperLeft };
            effectSmallStyle=new GUIStyle(ui.Small) { alignment=TextAnchor.UpperLeft };
            effectSmallStyle.normal.textColor=Ink;
            effectNoteStyle=new GUIStyle(ui.Small) { alignment=TextAnchor.UpperLeft, fontSize=16 };
        }
        private float EffectTextHeight(string text,GUIStyle style,float width)
        {
            effectMeasure.text=text; return style.CalcHeight(effectMeasure,width);
        }
        private void DrawReadableCardDetails(Rect rect,SkillVisual visual,bool diagram=true,bool formula=false)
        {
            EnsureEffectStyles();
            GUIStyle body=rect.width<360?effectSmallStyle:effectBodyStyle;
            float y=rect.y;
            if(diagram)
            {
                Label(new Rect(rect.x,y,rect.width,22),"도안 변화 · 밑줄 단어는 클릭",effectTitleStyle);y+=28;
                float h=EffectTextHeight(visual.Card.Description,body,rect.width);
                DrawExplainedText(new Rect(rect.x,y,rect.width,h),visual.Card.Description,body);y+=h+14;
            }
            Label(new Rect(rect.x,y,rect.width-80,23),visual.OutputText,effectNoteStyle);
            Rect rule=new Rect(rect.xMax-67,y,67,25);
            Label(rule,"수식 ⓘ",ui.Small,true);
            RegisterKeyword(rule,"수식",visual.RuleText,true,rect); y+=36;
            float footer=formula?64:0;
            DrawReadableEffectRows(new Rect(rect.x,y,rect.width,rect.yMax-y-footer),visual);
            if(formula)
            {
                Line(new Vector2(rect.x,rect.yMax-52),new Vector2(rect.xMax,rect.yMax-52),new Color(Muted.r,Muted.g,Muted.b,.25f));
                Label(new Rect(rect.x,rect.yMax-42,rect.width,42),visual.RuleText,effectNoteStyle);
            }
        }
        private void DrawReadableEffectRows(Rect rect,SkillVisual visual)
        {
            EnsureEffectStyles();
            int count=visual.EffectTitles.Length;if(count==0)return;
            GUIStyle body=rect.width<360?effectSmallStyle:effectBodyStyle;
            float y=rect.y;
            for(int i=0;i<count;i++)
            {
                float width=rect.width-28;
                float titleHeight=EffectTextHeight(visual.EffectTitles[i],effectTitleStyle,width);
                float bodyHeight=EffectTextHeight(visual.EffectBodies[i],body,width);
                float noteHeight=EffectTextHeight(visual.EffectNotes[i],effectNoteStyle,width);
                float height=12+titleHeight+7+bodyHeight+7+noteHeight+12;
                Rect block=new Rect(rect.x,y,rect.width,height);
                Fill(block,new Color(1,1,1,.72f));
                Fill(new Rect(block.x,block.y,3,block.height),new Color(Violet.r,Violet.g,Violet.b,.4f));
                float textY=y+12;
                DrawExplainedText(new Rect(rect.x+14,textY,width,titleHeight),visual.EffectTitles[i],effectTitleStyle);
                textY+=titleHeight+7;
                DrawExplainedText(new Rect(rect.x+14,textY,width,bodyHeight),visual.EffectBodies[i],body);
                textY+=bodyHeight+7;
                DrawExplainedText(new Rect(rect.x+14,textY,width,noteHeight),visual.EffectNotes[i],effectNoteStyle);
                y+=height+12;
            }
            maximumEffectOverflow=Mathf.Max(maximumEffectOverflow,y-12-rect.yMax);
        }
        private void DrawReadableCardBadges(Rect rect,SkillVisual visual,Rect owner)
        {
            for(int i=0;i<visual.EffectBadges.Length;i++)
            {
                Rect row=new Rect(rect.x,rect.y+i*54,rect.width,48);
                Fill(row,new Color(1,1,1,.075f));
                DrawExplainedText(new Rect(row.x+9,row.y+2,row.width-18,22),visual.EffectBadges[i],ui.SmallLight,
                    keepsCard:true, owner:owner);
                string note=i<visual.Card.AbilityCount?visual.EffectNotes[i]:"응축 보너스 최대 1장";
                Label(new Rect(row.x+9,row.y+25,row.width-18,20),note,ui.SmallLight);
            }
        }
    }
}

using UnityEngine;
using System.Collections.Generic;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private struct GrowthWordHit { public Rect Area; public int Term; }
        private sealed class ExplainedParagraph
        {
            public string Text;
            public Rect Area;
            public GUIStyle Style;
            public TextAnchor Alignment;
            public string ExcludedTitle;
            public readonly List<GrowthWordHit> Hits = new List<GrowthWordHit>(32);
        }
        private readonly List<ExplainedParagraph> explainedParagraphs = new List<ExplainedParagraph>(512);
        private ExplainedParagraph tooltipParagraph;
        private readonly GUIContent glossaryMeasure = new GUIContent();
        private static readonly string[] GrowthTerms = {
            "자가 적중", "기준점", "응축", "방출", "공명", "파편", "도안", "프리즘", "재현", "각성", "교차점", "보호막", "파열", "잔불", "고정", "정화",
            "집중", "재생", "약화", "노출", "경쾌", "가시", "추진", "요새화", "균열", "수호", "관통", "방벽 약화", "상처", "유도", "견인", "끌어당김", "드로우", "원점", "기록", "조립", "주파수", "회복", "약해진 방벽", "위성", "수식"
        };
        private static readonly string[] GrowthMeanings = {
            "도안에 자신을 맞히면 카드의 자기 강화 효과를 받습니다. 대리점과 함께 맞아도 한 번만 받습니다.",
            "도안을 펼치는 출발점입니다. 몸을 움직여도 이미 만든 도안은 따라오지 않습니다.",
            "체력을 쓰고 적 행동을 넘겨 카드를 보충합니다. 조립한 식은 남습니다.",
            "만든 도안으로 적을 공격하고 자신에게 강화 효과를 줍니다.",
            "궁극기를 쓰는 자원입니다. 여러 적을 맞히거나 자신과 적을 함께 맞히면 모입니다.",
            "순서대로 놓아 도안을 만드는 카드입니다.",
            "조립한 카드가 전장에 그리는 선입니다.",
            "도안으로 지나가면 이번 방출의 피해가 2 늘어나는 장치입니다.",
            "저장한 도형으로 다시 공격합니다. 카드의 다른 효과까지 복제하지는 않습니다.",
            "기술의 사용법을 크게 확장하는 상위 노드입니다.",
            "도안의 서로 다른 선이 만나는 지점입니다.",
            KeywordDescriptions[0],
            KeywordDescriptions[11],
            KeywordDescriptions[3],
            KeywordDescriptions[6],
            KeywordDescriptions[13],
            KeywordDescriptions[1],
            KeywordDescriptions[2],
            KeywordDescriptions[4],
            KeywordDescriptions[5],
            "다음 일반 이동 거리가 0.9 늘어납니다. 이동하면 사라집니다.",
            KeywordDescriptions[8],
            KeywordDescriptions[9],
            KeywordDescriptions[10],
            KeywordDescriptions[11],
            KeywordDescriptions[14],
            KeywordDescriptions[15],
            KeywordDescriptions[16],
            KeywordDescriptions[17],
            KeywordDescriptions[18],
            KeywordDescriptions[19],
            KeywordDescriptions[19],
            "다음 보충 때 카드를 더 뽑습니다. 응축의 추가 보충은 최대 1장입니다.",
            "도안을 펼치는 출발점입니다. 기준점과 같은 뜻입니다.",
            "도안을 저장해 다음 방출에 추가 공격합니다.",
            "카드를 순서대로 놓아 하나의 도안을 만드는 행동입니다.",
            "도안이 반복되는 정도입니다. 너무 높아지는 카드는 조립할 수 없습니다.",
            "체력을 회복합니다.",
            KeywordDescriptions[16],
            "관측형 루나의 도구입니다. 도안의 출발점이나 자기 강화 효과를 받는 대리점으로 씁니다.",
            "카드 순서에 따라 도안의 모양을 정하는 식입니다."
        };

        private void DrawExplainedGrowthText(Rect area, string text)
            => DrawExplainedText(area,text,ui.Light);

        private string ExplainMeaning(int term)
            => GrowthTerms[term]=="경쾌" && battle!=null && !battle.HasStatusRules
                ? KeywordDescriptions[7] : GrowthMeanings[term];

        private void DrawExplainedText(Rect area, string text, GUIStyle style, bool center = false,
            bool keepsCard = false, Rect? owner = null, string excludedTitle = null)
        {
            Label(area, text, style, center);
            if (string.IsNullOrEmpty(text)) return;
            TextAnchor alignment=center?TextAnchor.MiddleCenter:style.alignment;
            ExplainedParagraph paragraph=null;
            for(int i=0;i<explainedParagraphs.Count;i++)
            {
                var candidate=explainedParagraphs[i];
                if(candidate.Text==text && candidate.Area==area && candidate.Style==style && candidate.Alignment==alignment
                    && candidate.ExcludedTitle==excludedTitle)
                {paragraph=candidate;break;}
            }
            if(paragraph==null)
            {
                if(explainedParagraphs.Count==512)explainedParagraphs.Clear();
                paragraph=BuildGrowthWordHits(area,text,style,alignment,excludedTitle);explainedParagraphs.Add(paragraph);
            }
            if(text==hoveredKeywordBody)tooltipParagraph=paragraph;
            foreach (var hit in paragraph.Hits)
            {
                Fill(new Rect(hit.Area.x, hit.Area.yMax - 2, hit.Area.width, 2), Gold);
                RegisterKeyword(hit.Area, GrowthTerms[hit.Term], ExplainMeaning(hit.Term), keepsCard, owner ?? area);
            }
        }

        private void FollowTooltipKeyword(Vector2 point)
        {
            if(tooltipParagraph==null || tooltipParagraph.Text!=hoveredKeywordBody
                || tooltipParagraph.ExcludedTitle!=hoveredKeywordTitle)return;
            foreach(var hit in tooltipParagraph.Hits)
                if(hit.Area.Contains(point))
                { OpenKeyword(keywordSourceRect,GrowthTerms[hit.Term],ExplainMeaning(hit.Term),keywordKeepsCard,keywordOwnerRect);return; }
        }

        private static string CanonicalKeyword(string term)
        {
            switch(term)
            {
                case "파열": return "균열";
                case "방패 관통": return "관통";
                case "약해진 방벽": return "방벽 약화";
                case "찢긴 상처": return "상처";
                case "원점": return "기준점";
                case "견인": return "끌어당김";
                default: return term;
            }
        }

        private static bool ShouldExplainKeyword(string term,string excludedTitle)
            => term!="회복" && term!="수식" && CanonicalKeyword(term)!=CanonicalKeyword(excludedTitle);

        private ExplainedParagraph BuildGrowthWordHits(Rect area, string text, GUIStyle style, TextAnchor alignment,string excludedTitle)
        {
            var result=new ExplainedParagraph {Text=text,Area=area,Style=style,Alignment=alignment,ExcludedTitle=excludedTitle};
            var previous=style.alignment;style.alignment=alignment;
            var content=new GUIContent(text);
            for (int term = 0; term < GrowthTerms.Length; term++)
            {
                if(!ShouldExplainKeyword(GrowthTerms[term],excludedTitle))continue;
                int start = 0;
                while ((start = text.IndexOf(GrowthTerms[term], start, System.StringComparison.Ordinal)) >= 0)
                {
                    // Cursor positions use the same font and wrapping as the displayed paragraph.
                    for (int c = start; c < start + GrowthTerms[term].Length; c++)
                    {
                        Vector2 point = style.GetCursorPixelPosition(area, content, c);
                        glossaryMeasure.text=text[c].ToString();
                        float width = style.CalcSize(glossaryMeasure).x;
                        Rect word = new Rect(point.x, point.y, width, style.lineHeight);
                        if (!word.Overlaps(area)) continue;
                        // Tight single-line labels can be slightly shorter than the font's lineHeight.
                        // Clip the visible word instead of discarding its entire clickable underline.
                        word=Rect.MinMaxRect(Mathf.Max(word.xMin,area.xMin),Mathf.Max(word.yMin,area.yMin),
                            Mathf.Min(word.xMax,area.xMax),Mathf.Min(word.yMax,area.yMax));
                        result.Hits.Add(new GrowthWordHit { Area = word, Term = term });
                    }
                    start += GrowthTerms[term].Length;
                }
            }
            style.alignment=previous;
            return result;
        }
    }
}

using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Equations;
using Graphaclysm.Core.Relics;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private bool codexOpen, codexRelics, codexEnemies;
        private int codexPage, codexFilter, codexSelected, codexCount;
        private int codexTier=-1;
        private readonly int[] codexIndices = new int[FragmentCardCatalog.All.Count];
        private readonly Vector2[] codexCurve = new Vector2[129];
        private readonly FragmentEquation codexEquation = new FragmentEquation();
        private readonly FragmentEquation emblemEquation = new FragmentEquation();
        private string codexPageText = "", floorTitle = "";
        private static readonly string[] RoleLabels = { "전체", "직조", "변형", "이동", "지원" };
        private static readonly string[] FloorNames = { "유리의 회랑", "밤의 서고", "무명의 천문대", "격자의 공중정원", "일식의 왕좌" };

        private static int CardRole(FragmentKind kind)
        {
            if(AdvancedWeaves.IsAdvanced(kind)) return 2;
            switch(kind)
            {
                case FragmentKind.Expand: case FragmentKind.Surge: case FragmentKind.Ellipse: case FragmentKind.Shear: case FragmentKind.Limacon:
                case FragmentKind.CometBurst: case FragmentKind.KaleidoscopeFold: case FragmentKind.ShardFracture: case FragmentKind.NebulaRibbon:
                case FragmentKind.VerticalWeave: case FragmentKind.QuarterTurn: case FragmentKind.DiagonalWeave: return 2;
                case FragmentKind.TranslateRight: case FragmentKind.TranslateDown:
                case FragmentKind.HomeAnchor: case FragmentKind.WestAnchor: case FragmentKind.NorthAnchor:
                case FragmentKind.EastAnchor: case FragmentKind.SouthAnchor: return 3;
                case FragmentKind.Contract: case FragmentKind.Mirror: case FragmentKind.PhaseOffset: return 4;
                default: return 1;
            }
        }
        private static Color RoleColor(FragmentKind kind)
        {
            switch(CardRole(kind))
            {
                case 2: return new Color(.67f,.36f,.42f);
                case 3: return new Color(.30f,.49f,.62f);
                case 4: return new Color(.28f,.53f,.47f);
                default: return Violet;
            }
        }
        // Symbolic seals describe a card's identity; these are not promised output curves.
        private Vector2[] MakeFragmentEmblem(FragmentKind kind)
        {
            if(AdvancedWeaves.IsAdvanced(kind))
            {
                // Reuse one fixed equation for initialization, then retain only the 65-point emblem.
                emblemEquation.Clear(); emblemEquation.TryAppend(kind,5,0);
                var shape=new Vector2[65]; float extent=.01f;
                for(int i=0;i<shape.Length;i++)
                { emblemEquation.Sample(i/64.0,out double x,out double y); shape[i]=new Vector2((float)x-5,(float)y); extent=Mathf.Max(extent,Mathf.Max(Mathf.Abs(shape[i].x),Mathf.Abs(shape[i].y))); }
                for(int i=0;i<shape.Length;i++) shape[i]*=.92f/extent;
                return shape;
            }
            var points = new Vector2[65];
            for(int i=0;i<points.Length;i++)
            {
                float t=i*Mathf.PI*2/(points.Length-1), c=Mathf.Cos(t), s=Mathf.Sin(t);
                float x=c,y=s,r=1;
                switch(kind)
                {
                    case FragmentKind.VerticalWeave: x=.34f*c;y=s;break;
                    case FragmentKind.QuarterTurn: x=-s*.9f;y=c*.55f;break;
                    case FragmentKind.DiagonalWeave: x=c*.72f;y=(s+.7f*c)*.72f;break;
                    case FragmentKind.DoubleLoop: x=c*.9f;y=Mathf.Sin(2*t)*.8f;break;
                    case FragmentKind.EastAnchor: x=.6f-Mathf.Abs(s)*1.2f;y=c;break;
                    case FragmentKind.SouthAnchor: x=c;y=-.6f+Mathf.Abs(s)*1.2f;break;
                    case FragmentKind.Lissajous: x=Mathf.Cos(2*t)*.9f;y=Mathf.Sin(3*t)*.8f;break;
                    case FragmentKind.Epitrochoid: x=(c-.4f*Mathf.Cos(4*t))*.7f;y=(s-.4f*Mathf.Sin(4*t))*.7f;break;
                    case FragmentKind.Limacon: r=(1+.55f*c)*.65f;x=c*r-.18f;y=s*r;break;
                    case FragmentKind.Shear: x=(c+.7f*s)*.72f;y=s*.72f;break;
                    case FragmentKind.PhaseOffset: x=-s*.8f;y=s*.8f;break;
                    case FragmentKind.ComplexCube: r=.62f+.34f*Mathf.Cos(6*t);x=c*r;y=s*r;break;
                    case FragmentKind.CometBurst: r=.54f+.44f*Mathf.Cos(7*t);x=c*r;y=s*r;break;
                    case FragmentKind.KaleidoscopeFold: x=Mathf.Cos(4*t)*c-Mathf.Sin(4*t)*s;y=Mathf.Abs(Mathf.Sin(4*t)*c+Mathf.Cos(4*t)*s)-.55f;break;
                    case FragmentKind.ShardFracture: r=.58f+.35f*Mathf.Cos(5*t);float bend=.42f*Mathf.Sin(6*t);x=r*c-bend*s;y=r*s+bend*c;break;
                    case FragmentKind.NebulaRibbon: x=Mathf.Cos(5*t)*.86f;y=Mathf.Sin(2*t)*.82f;break;
                    case FragmentKind.Counterpoint: x=Mathf.Sin(t);y=Mathf.Sin(2*t)*.65f;break;
                    case FragmentKind.TwinEcho: x=Mathf.Sin(2*t)*.85f;y=Mathf.Sin(3*t)*.65f;break;
                    case FragmentKind.Orbit: x=c;y=s*.48f;break;
                    case FragmentKind.Petal: r=.62f+.32f*Mathf.Cos(3*t);x=c*r;y=s*r;break;
                    case FragmentKind.StarPetal: r=.58f+.4f*Mathf.Cos(5*t);x=c*r;y=s*r;break;
                    case FragmentKind.Square: r=.72f+.25f*Mathf.Cos(4*t);x=c*r;y=s*r;break;
                    case FragmentKind.Overtone: r=.68f+.27f*Mathf.Cos(7*t);x=c*r;y=s*r;break;
                    case FragmentKind.Expand: r=.7f+.26f*Mathf.Cos(4*t);x=c*r;y=s*r;break;
                    case FragmentKind.Surge: r=.55f+.44f*Mathf.Cos(8*t);x=c*r;y=s*r;break;
                    case FragmentKind.Contract: x=c*.65f+.2f*Mathf.Cos(2*t);y=s*.8f;break;
                    case FragmentKind.Mirror: x=c*.55f;y=s;break;
                    case FragmentKind.Ellipse: x=c;y=s*.34f;break;
                    case FragmentKind.TranslateRight: x=(i/64f)*1.7f-.85f;y=.45f*Mathf.Sin(t);break;
                    case FragmentKind.TranslateDown: x=.45f*Mathf.Sin(t);y=(i/64f)*1.7f-.85f;break;
                    case FragmentKind.HomeAnchor: r=.6f+.34f*Mathf.Cos(4*t);x=c*r;y=s*r;break;
                    case FragmentKind.WestAnchor: x=-.6f+Mathf.Abs(s)*1.2f;y=c;break;
                    case FragmentKind.NorthAnchor: x=c;y=.6f-Mathf.Abs(s)*1.2f;break;
                }
                points[i]=new Vector2(x,y);
            }
            return points;
        }
        private void DrawFragmentEmblem(Vector2 center,float size,SkillVisual visual,Color color)
        {
            Disc(center,size*1.2f,new Color(color.r,color.g,color.b,.06f));
            Ring(center,size*1.15f,new Color(color.r,color.g,color.b,.24f),1,.78f,.35f);
            for(int j=1;j<visual.Glyph.Length;j++)
                Line(center+visual.Glyph[j-1]*size,center+visual.Glyph[j]*size,color,1.6f);
            if(CardRole(visual.Card.Fragment)==3)
            {
                Line(center-Vector2.right*size*.28f,center+Vector2.right*size*.28f,color);
                Line(center-Vector2.up*size*.28f,center+Vector2.up*size*.28f,color);
            }
            else Diamond(center,3,color);
        }
        private void DrawMovementDock()
        {
            Fill(new Rect(28,797,314,114),new Color(.055f,.05f,.095f,.84f));
            Label(new Rect(48,805,274,30),battle.Tactics.CurrentMoveDistance>2 ? "필드 클릭 이동 · 경쾌 반경 2.7" : "필드 클릭 이동 · 반경 1.8",ui.SmallLight);
            if(ui.Button(new Rect(48,849,61,42),"←",false,!castActive&&!combatSkillTargeting&&!battle.Tactics.HasMoved))Move(-battle.Tactics.CurrentMoveDistance,0);
            if(ui.Button(new Rect(119,849,61,42),"↑",false,!castActive&&!combatSkillTargeting&&!battle.Tactics.HasMoved))Move(0,battle.Tactics.CurrentMoveDistance);
            if(ui.Button(new Rect(190,849,61,42),"↓",false,!castActive&&!combatSkillTargeting&&!battle.Tactics.HasMoved))Move(0,-battle.Tactics.CurrentMoveDistance);
            if(ui.Button(new Rect(261,849,61,42),"→",false,!castActive&&!combatSkillTargeting&&!battle.Tactics.HasMoved))Move(battle.Tactics.CurrentMoveDistance,0);
        }
        private void ChangeCodex(int filter,int page)
        {
            codexFilter=filter;codexCount=0;
            if(codexEnemies) codexCount=EnemyArchive.All.Count;
            else if(codexRelics) codexCount=FragmentRelicCatalog.All.Count;
            else for(int i=0;i<FragmentCardCatalog.All.Count;i++)
                if((filter==0||CardRole(FragmentCardCatalog.All[i].Fragment)==filter)
                    && (codexTier<0||(int)FragmentCardCatalog.All[i].DiagramRarity==codexTier))codexIndices[codexCount++]=i;
            codexPage=Mathf.Clamp(page,0,Mathf.Max(0,(codexCount-1)/8));
            codexPageText=(codexPage+1)+" / "+Mathf.Max(1,(codexCount+7)/8);
            if(codexCount==0) { codexSelected=-1; return; }
            SelectCodex(codexRelics||codexEnemies?codexPage*8:codexIndices[codexPage*8]);
        }
        private void SelectCodex(int index)
        {
            codexSelected=index;
            if(codexRelics||codexEnemies)return;
            codexEquation.Clear();codexEquation.TryAppend(FragmentCardCatalog.All[index].Fragment,4,-2);
            for(int i=0;i<codexCurve.Length;i++)
            {
                codexEquation.Sample(i/(double)(codexCurve.Length-1),out double x,out double y);
                codexCurve[i]=new Vector2((float)x,(float)y);
            }
        }
        private void DrawCodex()
        {
            Fill(new Rect(80,126,1760,914),new Color(Paper.r,Paper.g,Paper.b,.94f));
            Header("THE FRAGMENT INDEX","ARCHIVE");
            Label(new Rect(130,153,900,65),codexEnemies?"탑의 적 사전":codexRelics?"빛이 머문 물건들":"카드 사전",ui.PageTitle);
            if(ui.Button(new Rect(1140,163,200,49),"파편 카드",!codexRelics&&!codexEnemies)){codexRelics=codexEnemies=false;ChangeCodex(0,0);}
            if(ui.Button(new Rect(1360,163,200,49),"유물",codexRelics&&!codexEnemies)){codexRelics=true;codexEnemies=false;ChangeCodex(0,0);}
            if(ui.Button(new Rect(1580,163,200,49),"적 사전",codexEnemies)){codexEnemies=true;codexRelics=false;ChangeCodex(0,0);}
            if(codexEnemies){DrawEnemyArchiveBody();return;}
            if(!codexRelics)
            {
                if(ui.Button(new Rect(660,169,115,39),"모든 색",codexTier<0)) { codexTier=-1; ChangeCodex(codexFilter,0); }
                for(int tier=0;tier<4;tier++)
                {
                    Rect chip=new Rect(795+tier*72,169,55,39); Color color=Rarity((CardRarity)tier);
                    Fill(chip,new Color(color.r,color.g,color.b,.24f)); Border(chip,color,6);
                    Fill(new Rect(chip.x+9,chip.y+15,chip.width-18,8),color);
                    if(codexTier==tier) Border(new Rect(chip.x-3,chip.y-3,chip.width+6,chip.height+6),Ink,6);
                    if(GUI.Button(chip,GUIContent.none,GUIStyle.none)) { codexTier=tier; ChangeCodex(codexFilter,0); }
                }
                for(int i=0;i<RoleLabels.Length;i++)
                    if(ui.Button(new Rect(130+i*194,238,178,43),RoleLabels[i],codexFilter==i))ChangeCodex(i,0);
            }
            else Label(new Rect(130,235,950,45),"유물은 이번 여정의 모든 층에 이어집니다.",ui.Body);
            for(int slot=0;slot<8;slot++)
            {
                int item=codexPage*8+slot;if(item>=codexCount)break;
                int index=codexRelics?item:codexIndices[item];
                Rect r=new Rect(130+(slot%4)*246,315+(slot/4)*294,224,265);
                if(codexRelics)
                {
                    var relic=FragmentRelicCatalog.All[index];Fill(r,new Color(1,1,1,.5f));Border(r,index==codexSelected?Violet:Gold);
                    DrawRelicArt(new Rect(r.x+48,r.y+12,r.width-96,106),relic);
                    Label(new Rect(r.x+13,r.y+121,r.width-26,50),relic.DisplayName,ui.Body,true);
                    DrawExplainedText(new Rect(r.x+15,r.y+174,r.width-30,87),relic.Summary,ui.Small,true);
                }
                else
                {
                    DrawFragmentCard(r,Visual(FragmentCardCatalog.All[index]),index==codexSelected,false);
                    if(index==codexSelected)Border(new Rect(r.x-4,r.y-4,r.width+8,r.height+8),Violet);
                }
                if(GUI.Button(r,GUIContent.none,GUIStyle.none))SelectCodex(index);
            }
            Fill(new Rect(1165,238,620,746),new Color(1,1,1,.53f));
            if(codexSelected<0) Label(new Rect(1200,330,540,100),"이 조건에 맞는 카드가 없습니다.\n다른 역할이나 색을 선택하세요.",ui.Body);
            else if(codexRelics)
            {
                var relic=FragmentRelicCatalog.All[codexSelected];
                DrawRelicArt(new Rect(1260, 262, 420, 300), relic);
                Label(new Rect(1210,570,525,55),relic.DisplayName,ui.Heading,true);
                DrawExplainedText(new Rect(1210,642,525,110),relic.Description,ui.Body,true);
                Label(new Rect(1210,790,525,100),"유물방과 정예·층 보스 보상에서 획득합니다.\n같은 유물은 중복 획득하지 않습니다.",ui.Body,true);
            }
            else
            {
                var visual=Visual(FragmentCardCatalog.All[codexSelected]);
                Label(new Rect(1200,258,545,54),visual.Card.DisplayName,ui.Heading);
                Fill(new Rect(1200,327,545,5),Rarity(visual.Card.DiagramRarity));
                DrawReadableCardDetails(new Rect(1200,347,545,320),visual);
                Label(new Rect(1200,693,545,30),"첫 파편 예시 · 사용 순서에 따라 변합니다",ui.Small);
                Rect graph=new Rect(1325,735,300,240);Fill(graph,Ink);
                Line(new Vector2(graph.x,graph.center.y),new Vector2(graph.xMax,graph.center.y),Muted);
                Line(new Vector2(graph.center.x,graph.y),new Vector2(graph.center.x,graph.yMax),Muted);
                for(int j=1;j<codexCurve.Length;j++)
                {
                    double x0=codexCurve[j-1].x,y0=codexCurve[j-1].y,x1=codexCurve[j].x,y1=codexCurve[j].y;
                    if(GraphSegmentClipper.ClipToField(ref x0,ref y0,ref x1,ref y1))
                        Line(new Vector2(graph.x+(float)x0*30,graph.yMax-((float)y0+4)*30),new Vector2(graph.x+(float)x1*30,graph.yMax-((float)y1+4)*30),new Color(.79f,.70f,.98f),1.6f);
                }
                Diamond(new Vector2(graph.x+(float)codexEquation.OriginX*30,graph.yMax-((float)codexEquation.OriginY+4)*30),4,Gold);
            }
            if(ui.Button(new Rect(130,952,220,50),"메인으로"))codexOpen=false;
            if(ui.Button(new Rect(445,952,180,50),"이전",false,codexPage>0))ChangeCodex(codexFilter,codexPage-1);
            Label(new Rect(650,955,220,44),codexPageText,ui.Body,true);
            if(ui.Button(new Rect(895,952,180,50),"다음",false,(codexPage+1)*8<codexCount))ChangeCodex(codexFilter,codexPage+1);
        }
    }
}

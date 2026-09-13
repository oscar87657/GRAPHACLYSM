using Graphaclysm.Application;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private static readonly Vector2[] LegacyPositions={
            new Vector2(340,270),new Vector2(240,470),new Vector2(470,790),new Vector2(60,710),
            new Vector2(1020,270),new Vector2(1030,1040),new Vector2(-10,1040),new Vector2(1040,470),
            new Vector2(1100,760),new Vector2(1340,380),new Vector2(1390,850),new Vector2(-320,1040),
            new Vector2(1310,100),new Vector2(1340,590),new Vector2(30,150),new Vector2(-70,430),
            new Vector2(460,1100),new Vector2(1020,-80),new Vector2(-260,150),new Vector2(-380,430),
            new Vector2(1640,590),new Vector2(-10,1350),new Vector2(1640,300),new Vector2(-240,730)};
        private static readonly string[] LegacyNodeNames={"최대 체력","승리 회복","시작 공명","성장 경험","시작 보호막","잔광 보너스","작도 피해","시작 은화","유물 지참","예비 열쇠","파편 감별","돌파 분대","수호 분대","발굴 분대",
            "먼 길의 체력","여분의 붕대","깊은 공명","겹친 부적","든든한 출발","다음 싸움 준비","넉넉한 여비","숙련된 작도","추가 열쇠","시작 손패"};
        private static readonly Color LegacyTeal=new Color(.22f,.82f,.77f);
        private static readonly Vector2 LegacyRoot=new Vector2(710,545);
        private int legacySelectedNode=-1;
        // View-lifetime navigation. No account or run state changes while inspecting the tree.
        private static readonly Rect LegacyViewport=new Rect(40,180,1340,770);
        private Vector2 legacyTreePan,legacyTreeDragStart;
        private bool legacyTreePointerDown,legacyTreeDragging;
        private readonly string[] legacyTreeStates=new string[LegacyProgression.NodeCount];
        private int legacyTreeCachedCurrency=-1,legacyTreeCachedSelection=-2;
        private LegacyProgression legacyTreeOwner;
        private string legacyTreeFunds="",legacyTreeProgress="",legacyTreeDetail="",legacyTreePrice="",legacyTreeParent="";

        private void RefreshLegacyTreeLabels()
        {
            if(legacyTreeOwner==legacy && legacyTreeCachedCurrency==legacy.Currency && legacyTreeCachedSelection==legacySelectedNode)return;
            legacyTreeOwner=legacy;legacyTreeCachedCurrency=legacy.Currency;legacyTreeCachedSelection=legacySelectedNode;
            legacyTreeFunds="잔광 "+legacy.Currency;int acquired=0,total=0;
            for(int i=0;i<LegacyProgression.NodeCount;i++)
            {
                int rank=legacy.GetRank(i),max=legacy.GetNode(i).MaxRank;acquired+=rank;total+=max;
                legacyTreeStates[i]=rank+" / "+max+(rank==max?" · 완성":legacy.CanPurchase(i)?" · 습득 가능":"");
            }
            legacyTreeProgress=acquired+" / "+total+" 단계 습득";
            if(legacySelectedNode<0)return;
            var node=legacy.GetNode(legacySelectedNode);int parent=LegacyProgression.Prerequisite(legacySelectedNode);
            legacyTreeDetail=node.Description;
            legacyTreeParent=parent<0?"중앙 핵에서 바로 습득 가능":"선행: "+LegacyNodeNames[parent]+" 1단계";
            legacyTreePrice=legacy.GetRank(legacySelectedNode)==node.MaxRank?"최대 단계 도달":"다음 단계 · 잔광 "+legacy.Cost(legacySelectedNode);
        }
        private void DrawLegacyConstellation()
        {
            HandleLegacyTreeNavigation(Event.current);
            RefreshLegacyTreeLabels();
            Fill(new Rect(0,0,1920,1080),new Color(.025f,.045f,.065f));
            Vector2 root=LegacyRoot+legacyTreePan;
            for(int ray=0;ray<12;ray++)
            {float a=ray*Mathf.PI/6;DrawLegacyClippedLine(root,root+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*348,new Color(.1f,.22f,.26f,.18f),1);}
            Label(new Rect(75,45,1150,60),"잔광의 성좌",ui.PageTitleLight);
            Label(new Rect(80,118,1280,42),"드래그·휠: 이동 (Shift+휠: 좌우) · 노드 클릭: 설명 · 오른쪽 버튼: 습득",ui.Light);
            if(ui.Button(new Rect(1110,55,280,50),"중앙 보기",true))legacyTreePan=Vector2.zero;
            for(int i=0;i<LegacyPositions.Length;i++)
            {
                int parent=LegacyProgression.Prerequisite(i);Vector2 from=(parent<0?LegacyRoot:LegacyPositions[parent])+legacyTreePan,to=LegacyPositions[i]+legacyTreePan;
                Vector2 direction=(to-from).normalized;
                Color colour=legacy.GetRank(i)>0?LegacyTeal:new Color(.18f,.36f,.40f,.7f);
                DrawLegacyClippedLine(from+direction*(parent<0?112:43),to-direction*43,colour,legacy.GetRank(i)>0?2:1);
            }
            if(LegacyViewport.Contains(root-new Vector2(115,115)) && LegacyViewport.Contains(root+new Vector2(115,115)))
            {
                Diamond(root,113,LegacyTeal,2);Diamond(root,98,new Color(.12f,.4f,.45f),1);
                Ring(root,39,LegacyTeal,2);Diamond(root,26,Gold,2);
                Label(new Rect(root.x-100,root.y+40,200,48),"기록의 핵",ui.HeadingLight,true);
            }
            for(int i=0;i<LegacyPositions.Length;i++)
            {
                Vector2 p=LegacyPositions[i]+legacyTreePan;bool selected=i==legacySelectedNode;
                if(!LegacyViewport.Contains(p-new Vector2(112,54)) || !LegacyViewport.Contains(p+new Vector2(112,118)))continue;
                Color colour=legacy.GetRank(i)>0?LegacyTeal:legacy.CanPurchase(i)?new Color(.65f,.85f,.86f):new Color(.32f,.43f,.49f);
                if(selected)Diamond(p,52,Gold,2);
                Diamond(p,43,colour,2);Diamond(p,36,new Color(colour.r,colour.g,colour.b,.4f),1);
                DrawLegacyNodeMark(p,i,colour);
                Label(new Rect(p.x-105,p.y+48,210,36),LegacyNodeNames[i],ui.Light,true);
                Label(new Rect(p.x-110,p.y+86,220,30),legacyTreeStates[i],ui.SmallLight,true);
                if(GUI.Button(new Rect(p.x-65,p.y-48,130,124),GUIContent.none,GUIStyle.none))
                {legacySelectedNode=i;PlayClick();}
            }
            legacyTreePan.x=40-GUI.HorizontalScrollbar(new Rect(40,963,1340,20),40-legacyTreePan.x,1340,-520,1900);
            legacyTreePan.y=180-GUI.VerticalScrollbar(new Rect(1390,180,20,770),180-legacyTreePan.y,770,-260,1580);
            RefreshLegacyTreeLabels();
            Fill(new Rect(1450,150,400,810),new Color(.06f,.085f,.115f,.98f));Border(new Rect(1450,150,400,810),new Color(.22f,.45f,.48f));
            Label(new Rect(1480,185,340,55),legacyTreeFunds,ui.HeadingLight);
            Label(new Rect(1480,250,340,40),legacyTreeProgress,ui.SmallLight);
            if(legacySelectedNode>=0)
            {
                Label(new Rect(1480,325,340,86),legacy.GetNode(legacySelectedNode).Name,ui.HeadingLight);
                Label(new Rect(1480,433,340,165),legacyTreeDetail,ui.Light);
                Label(new Rect(1480,620,340,80),legacyTreeParent,ui.SmallLight);
                Label(new Rect(1480,716,340,60),legacyTreePrice,ui.Light);
                if(ui.Button(new Rect(1480,805,340,62),"다음 단계 습득",true,legacy.CanPurchase(legacySelectedNode) && legacy.TreeResetApplied))
                {
                    ResetLegacyCart();legacyCart[legacySelectedNode]++;
                    if(legacy.TryPurchaseRanks(legacyCart,legacyCartSnapshot,legacyCartCurrency,PersistLegacyCandidate))
                    {
                        flow.LegacyBenefits=legacy.Benefits;flow.PreparationUpgrades=legacy.Preparation(selectedSquad);
                        legacyCartMessage="습득했습니다. 다음 새 원정부터 적용됩니다.";legacyDirty=false;ResetLegacyCart();
                    }
                    else legacyCartMessage="저장하지 못해 잔광을 사용하지 않았습니다.";
                }
                int parent=LegacyProgression.Prerequisite(legacySelectedNode);
                if(parent>=0 && ui.Button(new Rect(1480,885,340,44),"선행 노드 보기",true))
                {legacySelectedNode=parent;legacyTreePan=LegacyViewport.center-LegacyPositions[parent];ClampLegacyTreePan();}
            }
            else Label(new Rect(1480,380,340,250),"중앙 핵에서 뻗은 노드를 선택하세요.\n\n영구 강화는 함께 적용됩니다. 개방한 분대는 출발 때 하나를 선택합니다.",ui.Light);
            Label(new Rect(80,992,1280,50),legacy.IsReadOnly?"기록을 읽지 못해 구매가 잠겼습니다.":legacyCartMessage,ui.SmallLight);
            if(ui.Button(new Rect(1490,995,340,52),"닫기",true)){ResetLegacyCart();legacyOpen=false;legacyTreePointerDown=false;legacyTreeDragging=false;}
        }
        private void ClampLegacyTreePan()
        {legacyTreePan.x=Mathf.Clamp(legacyTreePan.x,-520,560);legacyTreePan.y=Mathf.Clamp(legacyTreePan.y,-630,440);}
        private void HandleLegacyTreeNavigation(Event e)
        {
            if(e.type==EventType.ScrollWheel && LegacyViewport.Contains(e.mousePosition))
            {legacyTreePan-=e.shift?new Vector2(e.delta.y*35,0):new Vector2(e.delta.x*35,e.delta.y*35);ClampLegacyTreePan();e.Use();}
            if(e.type==EventType.MouseDown && e.button==0)
            {legacyTreePointerDown=LegacyViewport.Contains(e.mousePosition);legacyTreeDragging=false;legacyTreeDragStart=e.mousePosition;}
            if(e.type==EventType.MouseDrag && legacyTreePointerDown)
            {
                if(!legacyTreeDragging && (e.mousePosition-legacyTreeDragStart).sqrMagnitude>=36)
                {legacyTreeDragging=true;legacyTreePan+=e.mousePosition-legacyTreeDragStart;}
                else if(legacyTreeDragging)legacyTreePan+=e.delta;
                if(legacyTreeDragging){ClampLegacyTreePan();e.Use();}
            }
            if(e.type==EventType.MouseUp && e.button==0)
            {if(legacyTreeDragging){GUIUtility.hotControl=0;e.Use();}legacyTreeDragging=false;legacyTreePointerDown=false;}
        }
        private static void DrawLegacyClippedLine(Vector2 a,Vector2 b,Color colour,float width)
        {
            Vector2 delta=b-a;float first=0,last=1;
            if(!ClipLegacyEdge(-delta.x,a.x-LegacyViewport.xMin,ref first,ref last)
                || !ClipLegacyEdge(delta.x,LegacyViewport.xMax-a.x,ref first,ref last)
                || !ClipLegacyEdge(-delta.y,a.y-LegacyViewport.yMin,ref first,ref last)
                || !ClipLegacyEdge(delta.y,LegacyViewport.yMax-a.y,ref first,ref last))return;
            Line(a+delta*first,a+delta*last,colour,width);
        }
        private static bool ClipLegacyEdge(float direction,float distance,ref float first,ref float last)
        {
            if(Mathf.Abs(direction)<.0001f)return distance>=0;
            float t=distance/direction;
            if(direction<0){if(t>last)return false;first=Mathf.Max(first,t);}
            else{if(t<first)return false;last=Mathf.Min(last,t);}return true;
        }
        private void DrawLegacyNodeMark(Vector2 p,int index,Color colour)
        {
            if(index>=14)index=index==14 || index==18?0:index==15 || index==19?1:index==16?2:index==17?4:index==20?7:index==21?6:index==22?9:3;
            if(index>=11 && index<=13)
            {for(int n=0;n<3;n++)Diamond(p+new Vector2((n-1)*17,n==1?-12:10),10,colour,2);return;}
            if(index==0 || index==1)
            {Line(p+Vector2.left*20,p+Vector2.right*20,colour,3);Line(p+Vector2.up*20,p+Vector2.down*20,colour,3);return;}
            if(index==4){Diamond(p+Vector2.up*4,22,colour,2);Line(p+Vector2.up*19,p+Vector2.down*20,colour,2);return;}
            if(index==9){Ring(p+Vector2.up*10,10,colour,2);Line(p,p+Vector2.down*25,colour,3);Line(p+Vector2.down*16,p+new Vector2(10,16),colour,3);return;}
            if(index==6){Line(p+new Vector2(-18,18),p+new Vector2(18,-18),colour,4);Line(p+new Vector2(-15,-3),p+new Vector2(3,15),colour,3);return;}
            if(index==7 || index==5){Ring(p,21,colour,2);Line(p+Vector2.up*13,p+Vector2.down*13,colour,2);return;}
            if(index==8 || index==10){Diamond(p,22,colour,2);Diamond(p,12,colour,2);return;}
            Ring(p,21,colour,2);Ring(p,10,colour,2);Line(p+Vector2.left*26,p+Vector2.right*26,colour,1);
        }
    }
}

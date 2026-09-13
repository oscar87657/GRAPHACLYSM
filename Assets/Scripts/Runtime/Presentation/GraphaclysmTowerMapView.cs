using Graphaclysm.Core.Runs;
using Graphaclysm.Application;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private static readonly Rect TowerViewport=new Rect(610,130,800,840);
        private const float TowerLayerStep=170;
        // Map-lifetime labels; navigation belongs to the View, never the saved run.
        private RunMapProgress towerMapOwner;
        private string[] towerMapLabels,towerMapHints,towerFloorLabels;
        private float towerMapHeight,towerMapScroll;
        private int towerMapCompleted=-1;
        private Vector2 towerMapDragStart;
        private bool towerMapPointerDown,towerMapDragging;
        private float TowerMaximumScroll=>Mathf.Max(0,towerMapHeight-TowerViewport.height);
        private float TowerLayerY(int layer)=>(run.Map.Definition.LayerCount-1-layer)*TowerLayerStep+200;

        private void EnsureTowerMap()
        {
            var map=run.Map;
            if(towerMapOwner!=map)
            {
                towerMapOwner=map;towerMapCompleted=-1;towerMapPointerDown=false;towerMapDragging=false;
                towerMapHeight=(map.Definition.LayerCount-1)*TowerLayerStep+440;
                towerMapHeight=Mathf.Max(towerMapHeight,TowerViewport.height);
                towerMapLabels=new string[map.Definition.NodeCount];towerMapHints=new string[map.Definition.NodeCount];
                towerFloorLabels=new string[map.Definition.FloorCount];
                for(int f=0;f<towerFloorLabels.Length;f++)towerFloorLabels[f]=(f+1)+"층 · "+FloorNames[Mathf.Min(f,FloorNames.Length-1)];
                for(int i=0;i<towerMapLabels.Length;i++)
                {
                    var node=map.Definition.GetNode(i);string label=run.HasOpeningRoute?OpeningRoute.MapLabel(node):"";
                    towerMapLabels[i]=string.IsNullOrEmpty(label)?RoomLabels[(int)node.Kind]:label;
                    string hint=run.HasOpeningRoute && node.Layer==1
                        ?node.Kind==RunNodeKind.Workshop?"무료 피해 +1 / 카드 선택 / 카드 제거":"회복 / 다음 전투 보호막 / 공명"
                        :MapRewardHint(node.Kind);
                    if(run.HasEconomy && node.Story!=null && node.Kind!=RunNodeKind.Shop)
                        hint=node.Story.GetChoice(0).Consequence+(node.Story.ChoiceCount>1?"\n"+node.Story.GetChoice(1).Consequence:"");
                    if(run.HasEconomy && node.Battle!=null)
                        hint=run.HasRewardPacks?"은화 · 파편 선택 보상 2개\n정예·보스는 높은 색 확률 증가"
                            :node.Kind==RunNodeKind.Battle?"은화·연구권·카드 보상"+(node.Layer>0?"\n추가 유물 확률 25%":"\n도입전에는 추가 유물 없음"):"은화·연구권·유물 보상";
                    towerMapHints[i]=hint;
                }
            }
            if(towerMapCompleted!=map.CompletedNodeCount)
            {towerMapCompleted=map.CompletedNodeCount;FocusTowerCurrent();}
        }
        private void FocusTowerCurrent()
        {
            int layer=-1;var map=run.Map;
            for(int i=0;i<map.Definition.NodeCount;i++)if(map.IsAvailable(i))
            {int candidate=map.Definition.GetNode(i).Layer;if(layer<0 || candidate<layer)layer=candidate;}
            if(layer<0)layer=map.LastCompletedNodeIndex>=0?map.Definition.GetNode(map.LastCompletedNodeIndex).Layer:0;
            towerMapScroll=Mathf.Clamp(TowerLayerY(layer)-TowerViewport.height*.70f,0,TowerMaximumScroll);
        }
        private void HandleTowerMapPan(Event e)
        {
            if(!GUI.enabled){towerMapPointerDown=false;towerMapDragging=false;return;}
            if(e.type==EventType.ScrollWheel && TowerViewport.Contains(e.mousePosition))
            {towerMapScroll=Mathf.Clamp(towerMapScroll+e.delta.y*55,0,TowerMaximumScroll);e.Use();}
            if(e.type==EventType.MouseDown && e.button==0)
            {towerMapPointerDown=TowerViewport.Contains(e.mousePosition);towerMapDragging=false;towerMapDragStart=e.mousePosition;}
            if(e.type==EventType.MouseDrag && towerMapPointerDown)
            {
                if(!towerMapDragging && (e.mousePosition-towerMapDragStart).sqrMagnitude>=36)
                {towerMapDragging=true;towerMapScroll-=e.mousePosition.y-towerMapDragStart.y;}
                else if(towerMapDragging)towerMapScroll-=e.delta.y;
                if(towerMapDragging){towerMapScroll=Mathf.Clamp(towerMapScroll,0,TowerMaximumScroll);e.Use();}
            }
            if(e.type==EventType.MouseUp && e.button==0)
            {if(towerMapDragging){GUIUtility.hotControl=0;e.Use();}towerMapPointerDown=false;towerMapDragging=false;}
        }

        private void DrawContinuousTowerBackdrop()
        {
            Fill(TowerViewport,new Color(.035f,.038f,.07f));
            // Continuous shaft, with reused interior art as inset masonry rather than a fixed screen image.
            // Every architectural coordinate uses the very same scroll offset as the nodes.
            int rooms=run.Map.Definition.RoomsPerFloor;
            for(int f=0;f<towerFloorLabels.Length;f++)
            {
                float top=TowerViewport.y+TowerLayerY((f+1)*rooms-1)-110-towerMapScroll;
                var section=new Rect(635,top,750,rooms*TowerLayerStep);
                if(section.yMax<TowerViewport.yMin || section.yMin>TowerViewport.yMax)continue;
                if(towerBackdrop!=null)DrawTowerTexture(section,new Rect(.08f,.14f,.84f,.65f));
                DrawTowerFill(section,new Color(.035f+.009f*f,.027f,.07f+.009f*f,.52f));
                DrawTowerFill(new Rect(632,top,756,42),new Color(.09f,.085f,.14f,.97f));
                DrawTowerLine(new Vector2(633,top+42),new Vector2(1387,top+42),Gold,2);
            }
            float baseY=TowerViewport.y+TowerLayerY(0)+100-towerMapScroll;
            float crownY=TowerViewport.y+TowerLayerY(run.Map.Definition.LayerCount-1)-175-towerMapScroll;
            for(int side=0;side<2;side++)
            {
                float x=side==0?635:1360;
                DrawTowerFill(new Rect(x,crownY+90,25,baseY-crownY-90),new Color(.13f,.12f,.19f,.95f));
                DrawTowerLine(new Vector2(x+3,crownY+90),new Vector2(x+3,baseY),new Color(.45f,.39f,.55f),2);
                DrawTowerLine(new Vector2(x+20,crownY+90),new Vector2(x+20,baseY),new Color(.22f,.20f,.31f),2);
            }
            for(int layer=0;layer<run.Map.Definition.LayerCount;layer++)
            {
                float y=TowerViewport.y+TowerLayerY(layer)+84-towerMapScroll;
                if(y<TowerViewport.yMin || y>TowerViewport.yMax)continue;
                DrawTowerFill(new Rect(660,y,700,8),new Color(.17f,.15f,.23f,.75f));
                DrawTowerLine(new Vector2(660,y),new Vector2(1360,y),new Color(.35f,.29f,.43f,.7f),1);
                for(int side=0;side<2;side++)
                {float x=side==0?668:1325;DrawTowerFill(new Rect(x,y-50,26,50),new Color(.13f,.10f,.20f,.65f));DrawTowerLine(new Vector2(x,y-50),new Vector2(x+26,y-50),Gold,1);}
            }
            DrawTowerLine(new Vector2(635,crownY+90),new Vector2(1010,crownY),Gold,3);
            DrawTowerLine(new Vector2(1010,crownY),new Vector2(1385,crownY+90),Gold,3);
            DrawTowerFill(new Rect(632,baseY,756,25),new Color(.32f,.26f,.36f));
        }
        private void DrawTowerFloorBanners()
        {
            int rooms=run.Map.Definition.RoomsPerFloor;
            for(int f=0;f<towerFloorLabels.Length;f++)
            {
                float top=TowerViewport.y+TowerLayerY((f+1)*rooms-1)-110-towerMapScroll;
                if(top<TowerViewport.yMin || top+40>TowerViewport.yMax)continue;
                // A narrow opaque plaque keeps the route visible on either side, not over the letters.
                DrawTowerFill(new Rect(830,top,360,40),new Color(.075f,.065f,.12f));
                Label(new Rect(835,top+3,350,35),towerFloorLabels[f],ui.HeadingLight,true);
            }
        }
        private void DrawTowerTexture(Rect rect,Rect uv)
        {
            float top=Mathf.Max(rect.yMin,TowerViewport.yMin),bottom=Mathf.Min(rect.yMax,TowerViewport.yMax);
            if(bottom<=top)return;
            var clipped=new Rect(rect.x,top,rect.width,bottom-top);
            GUI.DrawTextureWithTexCoords(clipped,towerBackdrop,new Rect(uv.x,uv.y+uv.height*(rect.yMax-bottom)/rect.height,uv.width,uv.height*clipped.height/rect.height));
        }
        private static void DrawTowerFill(Rect rect,Color colour)
        {
            float left=Mathf.Max(rect.xMin,TowerViewport.xMin),right=Mathf.Min(rect.xMax,TowerViewport.xMax);
            float top=Mathf.Max(rect.yMin,TowerViewport.yMin),bottom=Mathf.Min(rect.yMax,TowerViewport.yMax);
            if(right>left && bottom>top)Fill(new Rect(left,top,right-left,bottom-top),colour);
        }
        private static void DrawTowerLine(Vector2 a,Vector2 b,Color colour,float width)
        {
            Vector2 d=b-a;float start=0,end=1;
            if(!ClipTowerEdge(-d.x,a.x-TowerViewport.xMin,ref start,ref end)
                || !ClipTowerEdge(d.x,TowerViewport.xMax-a.x,ref start,ref end)
                || !ClipTowerEdge(-d.y,a.y-TowerViewport.yMin,ref start,ref end)
                || !ClipTowerEdge(d.y,TowerViewport.yMax-a.y,ref start,ref end))return;
            Line(a+d*start,a+d*end,colour,width);
        }
        private static bool ClipTowerEdge(float d,float distance,ref float start,ref float end)
        {
            if(Mathf.Abs(d)<.0001f)return distance>=0;float t=distance/d;
            if(d<0){if(t>end)return false;start=Mathf.Max(start,t);}else{if(t<start)return false;end=Mathf.Min(end,t);}return true;
        }
    }
}

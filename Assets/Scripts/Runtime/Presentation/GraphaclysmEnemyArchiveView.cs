using Graphaclysm.Core.Combat;
using UnityEngine;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private void DrawEnemyArchiveBody()
        {
            Label(new Rect(130,237,1000,42),"일반 적 19종 · 보스 10종 · 새 원정은 층마다 두 후보 중 한 보스 등장",ui.Body);
            for(int slot=0;slot<8;slot++)
            {
                int index=codexPage*8+slot;if(index>=EnemyArchive.All.Count)break;
                var entry=EnemyArchive.All[index];Rect r=new Rect(130+slot%4*246,315+slot/4*294,224,265);
                Fill(r,new Color(1,1,1,.5f));Border(r,index==codexSelected?Violet:Gold);
                DrawArchiveEnemySeal(r.center-new Vector2(0,60),42,entry);
                Label(new Rect(r.x+12,r.y+125,r.width-24,40),entry.Name,ui.Body,true);
                Label(new Rect(r.x+12,r.y+175,r.width-24,32),entry.IsBoss?"층 보스 · 복합 패턴":"일반 적",ui.Small,true);
                Label(new Rect(r.x+12,r.y+211,r.width-24,38),entry.Stats,ui.Small,true);
                if(GUI.Button(r,GUIContent.none,GUIStyle.none)) SelectCodex(index);
            }
            var selected=EnemyArchive.All[Mathf.Clamp(codexSelected,0,EnemyArchive.All.Count-1)];
            Fill(new Rect(1165,238,620,700),new Color(1,1,1,.53f));
            DrawArchiveEnemySeal(new Vector2(1475,365),75,selected);
            Label(new Rect(1200,470,545,52),selected.Name,ui.Heading,true);
            Label(new Rect(1200,532,545,35),selected.Stats,ui.Body,true);
            Label(new Rect(1200,590,545,30),"공격 순서",ui.Heading);
            DrawExplainedText(new Rect(1200,635,545,100),selected.Pattern,ui.Body);
            Label(new Rect(1200,750,545,30),"대응 방법",ui.Heading);
            DrawExplainedText(new Rect(1200,795,545,122),selected.Counter,ui.Body);
            if(ui.Button(new Rect(130,952,220,50),"메인으로"))codexOpen=false;
            if(ui.Button(new Rect(430,952,160,50),"이전",false,codexPage>0))ChangeCodex(0,codexPage-1);
            Label(new Rect(625,957,110,40),codexPageText,ui.Body,true);
            if(ui.Button(new Rect(770,952,160,50),"다음",false,(codexPage+1)*8<codexCount))ChangeCodex(0,codexPage+1);
        }
        private static void DrawArchiveEnemySeal(Vector2 center,float size,EnemyArchiveEntry entry)
        {
            Color color=entry.IsBoss?Threat:Violet;
            Ring(center,size,color,2);int sides=3+(int)entry.Kind%6;
            for(int i=0;i<sides;i++)
            {
                float angle=i*Mathf.PI*2/sides,next=(i+1)*Mathf.PI*2/sides;
                Vector2 a=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*size*.8f;
                Vector2 b=center+new Vector2(Mathf.Cos(next),Mathf.Sin(next))*size*.8f;
                Line(a,b,color,2);if((int)entry.Kind%2==0)Line(a,center,color,1);
            }
            Diamond(center,size*.25f,color,2);
            if(entry.IsBoss){Ring(center,size*1.17f,Gold,2);Diamond(center,size*1.32f,Gold,1);}
        }
    }
}

using UnityEngine;
using Graphaclysm.Core.Combat;
using static Graphaclysm.Runtime.Presentation.AstralUi;

namespace Graphaclysm.Runtime.Presentation
{
    public sealed partial class GraphaclysmModernView
    {
        private static void DrawClippedWarningRing(double x,double y,double radius,Color color)
        {
            double previousX=x+radius,previousY=y;
            for(int i=1;i<=96;i++)
            {
                double angle=i*System.Math.PI*2/96;
                double nextX=x+System.Math.Cos(angle)*radius,nextY=y+System.Math.Sin(angle)*radius;
                double ax=previousX,ay=previousY,bx=nextX,by=nextY;
                if(Graphaclysm.Core.Equations.GraphSegmentClipper.ClipToField(ref ax,ref ay,ref bx,ref by))
                    Line(FieldPoint(ax,ay),FieldPoint(bx,by),color,2);
                previousX=nextX; previousY=nextY;
            }
        }
        private void DrawTacticalThreats()
        {
            for (int i=0;i<battle.Enemies.Count;i++)
            {
                var enemy=battle.Enemies[i];
                if (!enemy.IsAlive) continue;
                if (enemy.Intent.Kind == EnemyIntentKind.Attack && !(battle.CannonDisconnected && enemy.Definition.Id == "signature.cannon"))
                {
                    Color color = new Color(Threat.r,Threat.g,Threat.b,.55f);
                    if (enemy.FieldZoneCount>0)
                    {
                        for(int z=0;z<enemy.FieldZoneCount;z++)
                        {
                            enemy.GetFieldZone(z,out double l,out double b,out double r,out double t);
                            Vector2 a=FieldPoint(l,t),end=FieldPoint(r,b);
                            Rect zone=Rect.MinMaxRect(a.x,a.y,end.x,end.y);
                            Fill(zone,new Color(color.r,color.g,color.b,.16f));Border(zone,color,0);
                        }
                        Label(new Rect(Field.x+16,Field.y+12,Field.width-32,30),enemy.FieldPatternName+" · 붉은 영역 밖으로",ui.SmallLight);
                    }
                    else if (enemy.IsCrossGunner)
                    {
                        Vector2 p=FieldPoint(enemy.AimX,enemy.AimY);
                        float w=(float)EnemyState.CrossHalfWidth*FieldUnit;
                        Fill(new Rect(Field.x,p.y-w,Field.width,w*2),new Color(color.r,color.g,color.b,.12f));
                        Fill(new Rect(p.x-w,Field.y,w*2,Field.height),new Color(color.r,color.g,color.b,.12f));
                        Line(new Vector2(Field.x,p.y-w),new Vector2(Field.xMax,p.y-w),color);
                        Line(new Vector2(Field.x,p.y+w),new Vector2(Field.xMax,p.y+w),color);
                        Line(new Vector2(p.x-w,Field.y),new Vector2(p.x-w,Field.yMax),color);
                        Line(new Vector2(p.x+w,Field.y),new Vector2(p.x+w,Field.yMax),color);
                        Label(new Rect(p.x-130,p.y-65,260,24),"십자 포격 · 대각선 회피",ui.SmallLight);
                    }
                    else if (enemy.IsRingSentinel)
                    {
                        Vector2 p=FieldPoint(enemy.AimX,enemy.AimY);
                        DrawClippedWarningRing(enemy.AimX,enemy.AimY,EnemyState.RingInnerRadius,color);
                        DrawClippedWarningRing(enemy.AimX,enemy.AimY,EnemyState.RingOuterRadius,color);
                        Label(new Rect(p.x-130,p.y-50,260,44),"고리 사이 위험\n중앙 또는 바깥으로",ui.SmallLight,true);
                    }
                    else if (enemy.IsLineGunner)
                    {
                        Vector2 a=FieldPoint(enemy.LineStartX,enemy.LineStartY), b=FieldPoint(enemy.LineEndX,enemy.LineEndY);
                        Vector2 direction=(b-a).normalized;
                        Vector2 normal=new Vector2(-direction.y,direction.x)*(float)EnemyState.LineHalfWidth*FieldUnit;
                        Line(a,b,new Color(color.r,color.g,color.b,.10f),(float)EnemyState.LineHalfWidth*FieldUnit*2);
                        Line(a+normal,b+normal,color,1.5f); Line(a-normal,b-normal,color,1.5f);
                        Ring(a,normal.magnitude,color,1); Ring(b,normal.magnitude,color,1);
                        Label(new Rect(a.x-85,a.y-65,170,24),"직선 · 옆으로 회피",ui.SmallLight);
                    }
                    else
                    {
                        Ring(FieldPoint(enemy.AimX,enemy.AimY),(float)enemy.AimRadius*FieldUnit,color,1.3f);
                        if (enemy.IsTwinMortar)
                        {
                            Ring(FieldPoint(enemy.TwinAimX,enemy.TwinAimY),(float)enemy.AimRadius*FieldUnit,color,1.3f);
                            Label(new Rect(FieldPoint(enemy.AimX,enemy.AimY).x-130,FieldPoint(enemy.AimX,enemy.AimY).y-125,260,24),"쌍점 폭격 · 두 원 밖으로",ui.SmallLight);
                        }
                    }
                }
                if (battle.SealPending(enemy) || battle.SealActive(enemy))
                {
                    Vector2 p=FieldPoint(enemy.SealX,enemy.SealY);
                    bool broken=battle.TraceBreaksSeal(enemy);
                    Color color=broken ? new Color(.48f,1f,.86f,.85f) : new Color(.88f,.65f,1f,.75f);
                    if (battle.SealActive(enemy)) Disc(p,(float)BattleSession.SealRadius*FieldUnit,new Color(color.r,color.g,color.b,.12f));
                    Ring(p,(float)BattleSession.SealRadius*FieldUnit,color,1.5f);
                    Ring(p,(float)BattleSession.SealMarkRadius*FieldUnit,color,2);
                    Diamond(p,(float)BattleSession.SealMarkRadius*FieldUnit*.65f,color,1);
                    Label(new Rect(p.x-170,p.y+100,340,44),broken ? "방출하면 봉인 해제" : battle.SealActive(enemy)
                        ? "봉인 중 · 중앙을 맞혀 해제" : "다음 턴 봉인 · 중앙을 맞혀 취소",ui.SmallLight);
                }
                if (battle.GuardActive(enemy))
                {
                    var target=battle.Enemies[enemy.GuardTarget];
                    Vector2 a=FieldPoint(enemy.X,enemy.Y), b=FieldPoint(target.X,target.Y), mid=(a+b)*.5f;
                    bool broken=battle.TraceBreaksGuard(enemy);
                    Color color=broken ? new Color(.48f,1f,.86f,.8f) : new Color(.86f,.78f,.5f,.8f);
                    Line(a,b,color,2); Ring(mid,(float)BattleSession.GuardKnotRadius*FieldUnit,color,2);
                    Diamond(mid,(float)BattleSession.GuardKnotRadius*FieldUnit*.65f,color,1);
                    Label(new Rect(mid.x-200,mid.y-64,400,44),broken ? "방출하면 보호 연결 해제"
                        : "그래프 피해 −4 · 매듭+수호자 적중으로 해제",ui.SmallLight);
                }
            }
        }
    }
}

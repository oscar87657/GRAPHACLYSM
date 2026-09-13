using System;

namespace Graphaclysm.Core.Combat
{
    public sealed partial class EnemyState
    {
        // These zones are ground-locked for the announced turn; pulling the caster does not re-aim them.
        public int FieldPattern { get; private set; }
        public int FieldPatternOffset { get; private set; }
        public string FieldPatternName => FieldPattern==1?"격자 절단":FieldPattern==2?"교대 가로 포격":FieldPattern==3?"체크무늬 폭격":"";
        public int FieldZoneCount => FieldPattern==1?7:FieldPattern==2?4:FieldPattern==3?10:0;
        private void PrepareFieldPattern(int turn)
        {
            FieldPattern=0;FieldPatternOffset=(turn-1)%2;
            var kind=Definition.Behavior.Kind;
            if(kind==EnemyBehaviorKind.GridSentry)FieldPattern=1;
            if(kind==EnemyBehaviorKind.LaneArtillerist)FieldPattern=2;
            if(kind==EnemyBehaviorKind.LatticeArchitect)FieldPattern=1+(turn-1+(Enraged?1:0))%3;
            if(kind==EnemyBehaviorKind.EclipseEmperor)FieldPattern=1+(turn+1+(Enraged?1:0))%3;
            int beat=((turn-1)%4+(Enraged?1:0))%3;
            switch(kind)
            {
                case EnemyBehaviorKind.MirrorDuelist:
                    AttackShape=beat==1?EnemyBehaviorKind.LineGunner:EnemyBehaviorKind.TwinMortar;
                    break;
                case EnemyBehaviorKind.AshBishop:
                    AttackShape=beat==2?EnemyBehaviorKind.TwinMortar:EnemyBehaviorKind.CrossGunner;
                    if(beat==1)FieldPattern=2;
                    break;
                case EnemyBehaviorKind.OrbitDevourer:
                    AttackShape=beat==0?EnemyBehaviorKind.RingSentinel:EnemyBehaviorKind.LineGunner;
                    if(beat==1)FieldPattern=2;
                    break;
                case EnemyBehaviorKind.VoidCartographer:
                    AttackShape=EnemyBehaviorKind.TwinMortar;
                    FieldPattern=beat==0?3:beat==2?1:0;
                    break;
                case EnemyBehaviorKind.NightConductor:
                    AttackShape=EnemyBehaviorKind.RingSentinel;
                    FieldPattern=beat==0?1:beat==2?3:0;
                    break;
            }
            // A ground pattern must not inherit a body-anchored shape's movement or on-hit effects.
            if(FieldPattern>0) AttackShape=kind;
        }
        public void GetFieldZone(int index,out double left,out double bottom,out double right,out double top)
        {
            if(index<0 || index>=FieldZoneCount)throw new ArgumentOutOfRangeException(nameof(index));
            if(FieldPattern==1)
            {
                const double width=.22;
                if(index<4){double x=2+index*2+(FieldPatternOffset==0?0:.65);left=x-width;right=x+width;bottom=-4;top=4;}
                else {double y=-2+(index-4)*2+(FieldPatternOffset==0?0:.65);left=0;right=10;bottom=y-width;top=y+width;}
            }
            else if(FieldPattern==2)
            {left=0;right=10;bottom=-4+index*2+FieldPatternOffset;top=bottom+.8;}
            else
            {
                int cell=index*2;int row=cell/5,col=cell%5;
                if((row+col)%2!=FieldPatternOffset){cell++;row=cell/5;col=cell%5;}
                left=col*2;right=left+2;bottom=-4+row*2;top=bottom+2;
            }
        }
        private bool FieldPatternHits(double x,double y,double radius)
        {
            for(int i=0;i<FieldZoneCount;i++)
            {
                GetFieldZone(i,out double l,out double b,out double r,out double t);
                double dx=x-Math.Max(l,Math.Min(r,x)),dy=y-Math.Max(b,Math.Min(t,y));
                if(dx*dx+dy*dy<=radius*radius)return true;
            }
            return false;
        }
    }
}

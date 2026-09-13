using System;
using Graphaclysm.Core.Equations;

namespace Graphaclysm.Core.Combat
{
    public sealed partial class BattleSession
    {
        public const double SealRadius = .9, SealMarkRadius = .35, GuardKnotRadius = .3;
        public const int GuardReduction = 4;
        private int[] threatPlotDamage;
        public bool SealPending(EnemyState e) => e.IsAlive && e.Definition.Behavior.Kind == EnemyBehaviorKind.SealScribe
            && e.Intent.Kind == EnemyIntentKind.Charge && !e.SealInterrupted;
        public bool SealActive(EnemyState e) => e.IsAlive && e.SealUntilTurn >= Turn;
        public bool TraceBreaksSeal(EnemyState e) => (SealPending(e) || SealActive(e)) && TraceTouches(e.SealX,e.SealY,SealMarkRadius);
        private bool TraceTouches(double x, double y, double r) => Equation.HasBase
            && EquationAnalyzer.IntersectsCircle(Equation,x,y,r,Equation.CurveSegmentCount);
        public bool PositionSealed(double x, double y, double radius, bool afterPlot = false)
        {
            foreach (var e in enemies)
            {
                if (!SealActive(e) || afterPlot && TraceBreaksSeal(e)) continue;
                double dx=x-e.SealX, dy=y-e.SealY, reach=SealRadius+radius;
                if (dx*dx+dy*dy < reach*reach) return true;
            }
            return false;
        }
        public bool GuardActive(EnemyState e) => e.IsAlive && !e.GuardBroken && e.GuardTarget >= 0
            && e.GuardTarget < enemies.Length && enemies[e.GuardTarget].IsAlive;
        public bool TraceBreaksGuard(EnemyState e)
        {
            if (!GuardActive(e)) return false;
            var target=enemies[e.GuardTarget];
            return TraceTouches(e.X,e.Y,EnemyHitRadius)
                && TraceTouches((e.X+target.X)*.5,(e.Y+target.Y)*.5,GuardKnotRadius);
        }
        public int PreviewGuardReduction(EnemyState target)
        {
            foreach (var e in enemies)
                if (GuardActive(e) && enemies[e.GuardTarget] == target && !TraceBreaksGuard(e)) return GuardReduction;
            return 0;
        }
        private void PrepareThreats()
        {
            for (int i=0;i<enemies.Length;i++)
            {
                var e=enemies[i]; if (!e.IsAlive) continue;
                if (e.Definition.Behavior.Kind == EnemyBehaviorKind.LinkGuardian)
                {
                    e.GuardBroken=false; e.GuardTarget=-1;
                    for (int j=0;j<enemies.Length;j++) if (j!=i && enemies[j].IsAlive) { e.GuardTarget=j; break; }
                }
                if (e.Definition.Behavior.Kind != EnemyBehaviorKind.SealScribe || Turn%2==0) continue;
                e.SealInterrupted=false;
                e.SealX=Math.Max(1,Math.Min(9,(e.X+Tactics.X)*.5));
                e.SealY=Math.Max(-3,Math.Min(3,(e.Y+Tactics.Y)*.5));
                for (int j=0;j<TerrainCount;j++) if (GetTerrain(j).Kind==BattleTerrainKind.Prism)
                { e.SealX=GetTerrain(j).X; e.SealY=GetTerrain(j).Y; break; }
            }
        }
        private void ResolveThreatBreaks()
        {
            // Freeze primary damage before deaths/status changes can affect protection.
            for (int i=0;i<enemies.Length;i++) threatPlotDamage[i]=PreviewDamage(enemies[i]);
            foreach (var e in enemies)
            {
                if (TraceBreaksGuard(e)) e.GuardBroken=true;
                if (TraceBreaksSeal(e)) { e.SealInterrupted=true; e.SealUntilTurn=0; }
            }
        }
    }
}

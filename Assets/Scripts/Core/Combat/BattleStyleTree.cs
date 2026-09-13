using System;
using Graphaclysm.Core.Runs;
using Graphaclysm.Core.Equations;
namespace Graphaclysm.Core.Combat
{
    public sealed partial class BattleSession
    {
        private bool styleTree,styleReplayPending;
        private EquationState styleReplay,styleScratch;
        private int[] styleDamage;
        private int tuneTurn,tuneUses,pullComposeTurn;
        private double sanctuaryBodyX,sanctuaryBodyY;
        public int LastStyleHits { get; private set; }
        public int CurrentRotationLimit=>HasRule(ApproachRule.TuneFullAngle)?150:90;
        private bool AdditionalTune=>Approach==CombatApproach.Tuning &&
            (pullComposeTurn==Turn || HasRule(ApproachRule.TuneRepeat)&&tuneTurn==Turn&&tuneUses<2);
        private EquationState CurrentRecord
        {
            get
            {
                if(!HasRule(ApproachRule.RecordFollow)||!hasRecording)return recordedDiagram;
                recordedDiagram.CopyDiagramTo(styleScratch,Tactics.X,Tactics.Y,0);return styleScratch;
            }
        }
        private EquationState CurrentSanctuary
        {
            get
            {
                if(!HasRule(ApproachRule.SanctuaryMobile)||sanctuaryActions<=0)return ultimateDiagram;
                ultimateDiagram.CopyDiagramTo(styleScratch,ultimateDiagram.Fragments.OriginX+Tactics.X-sanctuaryBodyX,
                    ultimateDiagram.Fragments.OriginY+Tactics.Y-sanctuaryBodyY,0);return styleScratch;
            }
        }
        public EquationState StylePreviewDiagram
        {
            get
            {
                if(!styleTree||!Equation.HasBase)return null;
                if(styleReplayPending)return styleReplay;
                if(HasRule(ApproachRule.SatelliteDual)&&SatelliteOriginSelected&&HasSatellite
                    &&Math.Abs(Equation.Fragments.OriginX-SatelliteX)<.001&&Math.Abs(Equation.Fragments.OriginY-SatelliteY)<.001)
                {Equation.CopyDiagramTo(styleScratch,Tactics.X,Tactics.Y,0);return styleScratch;}
                return null;
            }
        }
        public int PreviewStyleDamage(EnemyState target)
        {
            if(!styleTree||target==null||!target.IsAlive||!Equation.HasBase)return 0;
            int damage=0;var diagram=StylePreviewDiagram;
            if(diagram!=null&&EquationAnalyzer.IntersectsCircle(diagram,target.X,target.Y,EnemyHitRadius,diagram.CurveSegmentCount))
                damage+=styleReplayPending?8:6;
            if(HasRule(ApproachRule.SatelliteLink)&&SatellitePlotHit&&DistanceToSegment(target.X,target.Y,Tactics.X,Tactics.Y,SatelliteX,SatelliteY)<=EnemyHitRadius)damage+=6;
            return damage;
        }
        private void SnapshotStyleTree()
        {
            LastStyleHits=0;if(!styleTree)return;
            for(int i=0;i<enemies.Length;i++)styleDamage[i]=PreviewStyleDamage(enemies[i]);
        }
        private int ResolveStyleTree()
        {
            if(!styleTree)return 0;int total=0;
            for(int i=0;i<enemies.Length;i++)if(enemies[i].IsAlive&&styleDamage[i]>0)
            {int hp=enemies[i].Health;enemies[i].TakeDamage(styleDamage[i]);total+=hp-enemies[i].Health;LastStyleHits++;}
            styleReplayPending=false;
            if(LastUltimateActivated&&(HasRule(ApproachRule.BurstEcho)||HasRule(ApproachRule.ExtraEcho)))
            {Equation.CopyDiagramTo(styleReplay,Equation.Fragments.OriginX,Equation.Fragments.OriginY,0);styleReplayPending=true;}
            if(InExtraAssembly&&HasRule(ApproachRule.ExtraResonance))Tactics.GainResonance(2);
            if(InExtraAssembly&&HasRule(ApproachRule.ExtraPull))
                for(int i=0;i<enemies.Length;i++)
                {
                    var e=enemies[i];if(!e.IsAlive||threatPlotDamage[i]<=0||e.Statuses.Get(CombatStatusKind.Anchor)>0)continue;
                    double dx=Tactics.X-e.X,dy=Tactics.Y-e.Y,l=Math.Sqrt(dx*dx+dy*dy);if(l<.001)continue;
                    double distance=LegalTravel(e.X,e.Y,dx/l,dy/l,Math.Min(1,l),EnemyHitRadius);
                    int hp=e.Health;DisplaceEnemy(e,e.X+dx/l*distance,e.Y+dy/l*distance);total+=hp-e.Health;
                }
            return total;
        }
        private void ResetStyleTree()
        {styleReplayPending=false;tuneTurn=tuneUses=pullComposeTurn=LastStyleHits=0;sanctuaryBodyX=sanctuaryBodyY=0;}
    }
}

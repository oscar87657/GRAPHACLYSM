using System;
using Graphaclysm.Core.Equations;
using Graphaclysm.Core.Runs;

namespace Graphaclysm.Core.Combat
{
    public sealed partial class BattleSession
    {
        private EquationState ultimateDiagram;
        private int ultimateEchoes, sanctuaryActions, ultimatePower;
        private int[] ultimateDamage;
        public bool HasApproachUltimates { get; private set; }
        public bool ExtraAssemblyPending { get; private set; }
        public bool InExtraAssembly { get; private set; }
        public int UltimateEchoes => ultimateEchoes;
        public int SanctuaryActions => sanctuaryActions;
        public EquationState UltimateDiagram => sanctuaryActions>0 ? CurrentSanctuary : ultimateEchoes>0 ? ultimateDiagram : null;
        public int LastUltimateHits { get; private set; }
        public bool LastUltimateActivated { get; private set; }
        public bool SanctuaryProtectsPlayer => sanctuaryActions > 0 && SanctuaryTouchesPlayer(CurrentSanctuary);

        public void EnableApproachUltimates()
        {
            if (HasApproachUltimates || Approach == CombatApproach.None || Turn != 1 || playedCardCount != 0
                || Phase != BattlePhase.PlayerPlanning) throw new InvalidOperationException("Enable ultimates at battle entry.");
            ultimateDamage = new int[enemies.Length];
            HasApproachUltimates = true;
            Tactics.UsesApproachUltimate = true;
            ultimateDiagram = new EquationState();
        }

        private void ResetApproachUltimateState()
        {
            ultimateEchoes = sanctuaryActions = LastUltimateHits = 0;
            ExtraAssemblyPending = InExtraAssembly = LastUltimateActivated = false;
        }

        public int PreviewApproachUltimateDamage(EnemyState target)
        {
            if (!HasApproachUltimates || target == null || !target.IsAlive || !Equation.HasBase) return 0;
            if (Approach == CombatApproach.Recording && ultimateEchoes > 0)
            {
                int multiplier=1;
                if(HasRule(ApproachRule.EchoConcentrate))
                {int hits=0;foreach(var e in enemies)if(e.IsAlive&&EquationAnalyzer.IntersectsCircle(ultimateDiagram,e.X,e.Y,EnemyHitRadius,ultimateDiagram.CurveSegmentCount))hits++;if(hits==1)multiplier=2;}
                return EquationAnalyzer.IntersectsCircle(ultimateDiagram,target.X,target.Y,EnemyHitRadius,ultimateDiagram.CurveSegmentCount)
                    ? ultimatePower*multiplier : 0;
            }
            if (Approach != CombatApproach.Execution || !Tactics.UltimateArmed) return 0;
            foreach(var source in enemies)
            {
                if (PreviewDamage(source) <= 0) continue;
                double dx=target.X-source.X,dy=target.Y-source.Y;
                if(dx*dx+dy*dy<=CurrentBurstRadius*CurrentBurstRadius)
                {
                    EnemyState strongest=null;
                    if(HasRule(ApproachRule.BurstConcentrate))foreach(var e in enemies)
                    {
                        if(!e.IsAlive || strongest!=null&&strongest.Health>=e.Health)continue;
                        foreach(var anchor in enemies){double ex=e.X-anchor.X,ey=e.Y-anchor.Y;if(PreviewDamage(anchor)>0&&ex*ex+ey*ey<=CurrentBurstRadius*CurrentBurstRadius){strongest=e;break;}}
                    }
                    return CurrentBurstPower+(strongest==target?12:0);
                }
            }
            return 0;
        }

        private void SnapshotApproachUltimate()
        {
            LastUltimateHits=0;
            LastUltimateActivated=HasApproachUltimates && Tactics.UltimateArmed;
            if(!HasApproachUltimates) return;
            // Freeze all burst anchors before primary damage kills any of them.
            for(int i=0;i<enemies.Length;i++) ultimateDamage[i]=PreviewApproachUltimateDamage(enemies[i]);
        }

        private int ResolveApproachUltimate()
        {
            if(!HasApproachUltimates) return 0;
            int total=0;
            for(int i=0;i<enemies.Length;i++)
            {
                if(!enemies[i].IsAlive || ultimateDamage[i]<=0) continue;
                enemies[i].TakeDamage(ultimateDamage[i]); total+=ultimateDamage[i]; LastUltimateHits++;
                if(HasRule(ApproachRule.BurstShield))Tactics.Statuses.Add(CombatStatusKind.Shield,4,1);
            }
            if(ultimateEchoes>0)
            { if(HasRule(ApproachRule.EchoGuard))Tactics.Statuses.Add(CombatStatusKind.Shield,6,1);ultimateEchoes--;
                if(ultimateEchoes>0&&HasRule(ApproachRule.EchoMarch))ultimateDiagram.ArrangeDiagram(ultimateDiagram.Fragments.OriginX+.75,ultimateDiagram.Fragments.OriginY,0); }
            if(!LastUltimateActivated) return total;
            switch(Approach)
            {
                case CombatApproach.Execution:
                    CombatSkillCooldown=0;
                    if(HasRule(ApproachRule.BurstFocus))Tactics.Statuses.Add(CombatStatusKind.Focus,4,2);
                    break;
                case CombatApproach.Recording:
                    Equation.CopyDiagramTo(ultimateDiagram,Equation.Fragments.OriginX,Equation.Fragments.OriginY,HasRule(ApproachRule.EchoMirror)?180:0);
                    ultimatePower=8+WeaveDamageBonus+(HasRule(ApproachRule.EchoPower)?6:0); ultimateEchoes=2+(HasRule(ApproachRule.EchoCount)?1:0);
                    break;
                case CombatApproach.Tuning:
                    ExtraAssemblyPending=true;
                    break;
                case CombatApproach.Observation:
                    sanctuaryBodyX=Tactics.X;sanctuaryBodyY=Tactics.Y;
                    Equation.CopyDiagramTo(ultimateDiagram,Equation.Fragments.OriginX,Equation.Fragments.OriginY,0);
                    sanctuaryActions=2+(HasRule(ApproachRule.SanctuaryTime)?1:0);
                    break;
            }
            return total;
        }

        private bool SanctuaryTouchesPlayer(EquationState diagram)
        {
            if(diagram==null || !diagram.HasBase) return false;
            if(HasRule(ApproachRule.SanctuaryRemote)&&HasSatellite)return true;
            double radius=HasRule(ApproachRule.SanctuaryWide)?1.7:.85;
            return EquationAnalyzer.IntersectsCircle(diagram,Tactics.X,Tactics.Y,radius,diagram.CurveSegmentCount)
                || HasSatellite && EquationAnalyzer.IntersectsCircle(diagram,SatelliteX,SatelliteY,radius,diagram.CurveSegmentCount);
        }

        public bool PreviewSanctuaryProtection => HasApproachUltimates &&
            (Approach==CombatApproach.Observation && Tactics.UltimateArmed ? SanctuaryTouchesPlayer(Equation) : SanctuaryProtectsPlayer);

        private void ApplySanctuary()
        {
            if(sanctuaryActions<=0) return;
            if(SanctuaryProtectsPlayer)
            { Tactics.Statuses.Add(CombatStatusKind.Shield,CurrentSanctuaryShield,1);
                if(HasRule(ApproachRule.SanctuaryHeal))PlayerHealth=Math.Min(PlayerMaxHealth,PlayerHealth+3); }
            sanctuaryActions--;
        }

        private void BeginExtraAssembly()
        {
            ExtraAssemblyPending=false; InExtraAssembly=true;
            ResetEquationHistory(); CondenseCount=0; preserveFragments=false;
            Tactics.LockMoveUndo();
            if(HasRule(ApproachRule.ExtraSkill))approachUsedTurn=0;
            Phase=BattlePhase.PlayerPlanning;
        }
    }
}

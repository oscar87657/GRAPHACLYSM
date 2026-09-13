using System;
using Graphaclysm.Core.Runs;

namespace Graphaclysm.Core.Combat
{
    public sealed partial class BattleSession
    {
        private bool[] specializationNodes;
        private readonly bool[] specializationRules=new bool[61];
        private int specializationConditions,specializationSkillTurn;
        private int normalRecordUses;
        public int LastGrowthTriggers { get; private set; }
        public bool HasSpecialization=>specializationNodes!=null;
        public bool HasRule(ApproachRule rule)=>specializationRules[(int)rule];
        public double CurrentExecutionTravel=>ExecutionTravel+(HasRule(ApproachRule.DashReach)?.9:0)+(HasRule(ApproachRule.DashLong)?1.8:0);
        public double CurrentPullRadius=>PullRadius+(HasRule(ApproachRule.PullArea)?.8:0);
        public double CurrentPullTravel=>PullTravel+(HasRule(ApproachRule.PullTravel)?.8:0);
        public double CurrentSatelliteReach=>3+(HasRule(ApproachRule.SatelliteReach)?1:0);
        public double CurrentDiagramReach=>3+(HasRule(ApproachRule.TuneReach)||HasRule(ApproachRule.RecordReach)?1:0);
        public double CurrentBurstRadius=>1.8+(HasRule(ApproachRule.BurstReach)?.6:0);
        public int CurrentBurstPower=>12+(HasRule(ApproachRule.BurstPower)?8:0);
        public int CurrentSanctuaryShield=>10+(HasRule(ApproachRule.SanctuaryShield)?6:0);
        public void EnableSpecialization(CompiledGrowthBuild build)
        {
            if(!HasApproachUltimates || HasSpecialization || Turn!=1 || playedCardCount!=0)throw new InvalidOperationException("Specialization belongs to battle entry.");
            specializationNodes=new bool[ApproachGrowthCatalog.Count];
            styleTree=build.HasAcquired(StyleTreeCatalog.Get(Approach,0).Node.Id);
            if(styleTree)
            {
                specializationNodes=new bool[StyleTreeCatalog.Count];
                styleReplay=new Graphaclysm.Core.Equations.EquationState();styleScratch=new Graphaclysm.Core.Equations.EquationState();styleDamage=new int[enemies.Length];
            }
            for(int i=0;i<specializationNodes.Length;i++)
            {
                var entry=styleTree?StyleTreeCatalog.Get(Approach,i):ApproachGrowthCatalog.Get(Approach,i);
                specializationNodes[i]=build.HasAcquired(entry.Node.Id);
                if(specializationNodes[i] && entry.Rule!=ApproachRule.None)specializationRules[(int)entry.Rule]=true;
            }
        }
        private int SpecializationPlotPower => (InExtraAssembly && HasRule(ApproachRule.ExtraPower)?4:0)
            +(SatelliteOriginSelected && HasRule(ApproachRule.RemotePower)?3:0);
        private void SnapshotSpecialization()
        {
            LastGrowthTriggers=0;specializationConditions=0;
            if(!HasSpecialization)return;
            int hits=0;bool marked=false;
            foreach(var e in enemies){if(PreviewDamage(e)>0)hits++;if(e.IsAlive && e.Statuses.Get(CombatStatusKind.Rupture)>0)marked=true;}
            if(playedCardCount<=3)specializationConditions|=1<<0;
            if(playedCardCount>=5)specializationConditions|=1<<1;
            if(Tactics.HasMoved)specializationConditions|=1<<2;
            if(PreviewPlayerHit)specializationConditions|=1<<3;
            if(hits>=2)specializationConditions|=1<<4;
            if(hits==1)specializationConditions|=1<<5;
            if(plotWasCondensed)specializationConditions|=1<<6;
            if(approachUsedTurn==Turn || specializationSkillTurn==Turn)specializationConditions|=1<<7;
            if(LastUltimateActivated)specializationConditions|=1<<8;
            if(hasRecording || ultimateEchoes>0)specializationConditions|=1<<9;
            if(PlayerHealth*2<=PlayerMaxHealth)specializationConditions|=1<<10;
            if(Tactics.Statuses.Get(CombatStatusKind.Shield)>0)specializationConditions|=1<<11;
            if(marked)specializationConditions|=1<<12;
            if(InExtraAssembly)specializationConditions|=1<<13;
            if(SatellitePlotHit)specializationConditions|=1<<14;
        }
        private void ResolveSpecialization()
        {
            if(!HasSpecialization)return;
            for(int i=1;i<specializationNodes.Length;i++)
            {
                var e=styleTree?StyleTreeCatalog.Get(Approach,i):ApproachGrowthCatalog.Get(Approach,i);
                if(!specializationNodes[i] || e.Trigger<0 || (specializationConditions&(1<<e.Trigger))==0)continue;
                bool applied=false;
                if(e.Enemy)
                {for(int n=0;n<enemies.Length;n++)if(enemies[n].IsAlive && threatPlotDamage[n]>0){enemies[n].Statuses.Add(e.Status,e.Magnitude,2);applied=true;}}
                else{Tactics.Statuses.Add(e.Status,e.Magnitude,2);applied=true;}
                if(applied)LastGrowthTriggers++;
            }
            if(InExtraAssembly && HasRule(ApproachRule.ExtraGuard))Tactics.Statuses.Add(CombatStatusKind.Shield,8,1);
            if(SatellitePlotHit && HasRule(ApproachRule.SatelliteShield))Tactics.Statuses.Add(CombatStatusKind.Shield,4,1);
        }
        private void SpecializationDash(bool hit,bool killed)
        {
            specializationSkillTurn=Turn;
            if(hit && HasRule(ApproachRule.DashGuard))Tactics.Statuses.Add(CombatStatusKind.Shield,6,1);
            if(killed && HasRule(ApproachRule.DashMend))PlayerHealth=Math.Min(PlayerMaxHealth,PlayerHealth+4);
        }
    }
}

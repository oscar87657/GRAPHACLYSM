using Graphaclysm.Core.Runs;
namespace Graphaclysm.Application
{
    public sealed partial class RunGameSession
    {
        public bool TryEnableGrowthLimit()
        {
            if(!Growth.IsStyleTree||Growth.HasPointLimit||journal==null||journal.Count!=12||journal[11].Kind!=RunCommandKind.StyleTree
                ||Phase!=RunPhase.MapSelection||Map.CompletedNodeCount!=0||Map.ActiveNodeIndex>=0)return false;
            int awarded=Growth.AwardedPoints;Growth=new RunGrowthState(Growth.Archetype,StartingApproach,true,true);Growth.AddPoints(awarded);
            return Record(true,RunCommandKind.GrowthLimit);
        }
        public bool TryEnableStyleTree()
        {
            if(!Growth.IsSpecialized||Growth.IsStyleTree||journal==null||journal.Count!=11||journal[10].Kind!=RunCommandKind.ApproachTree
                ||Phase!=RunPhase.MapSelection||Map.CompletedNodeCount!=0||Map.ActiveNodeIndex>=0)return false;
            int awarded=Growth.AwardedPoints;Growth=new RunGrowthState(Growth.Archetype,StartingApproach,true);Growth.AddPoints(awarded);
            return Record(true,RunCommandKind.StyleTree);
        }
        public bool TryEnableApproachTree()
        {
            if(!HasBattleRework || Growth.IsSpecialized || journal==null || journal.Count!=10
                || journal[9].Kind!=RunCommandKind.BattleRework || Phase!=RunPhase.MapSelection || Map.CompletedNodeCount!=0 || Map.ActiveNodeIndex>=0)return false;
            int awarded=Growth.AwardedPoints;
            Growth=new RunGrowthState(Growth.Archetype,StartingApproach);Growth.AddPoints(awarded);
            return Record(true,RunCommandKind.ApproachTree);
        }
    }
}

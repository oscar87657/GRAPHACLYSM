using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Runs;

namespace Graphaclysm.Application
{
    public sealed partial class RunGameSession
    {
        public bool HasBattleRework { get; private set; }
        public bool IsRetiredUltimateNode(int index) => HasBattleRework && index>=0 && index<Growth.NodeCount
            && Growth.GetNode(index).Id.IndexOf(".u.",System.StringComparison.Ordinal)>=0;
        public bool TryEnableBattleRework()
        {
            if(IsPractice || HasBattleRework || !HasFiveFloors || journal==null || journal.Count!=9
                || journal[8].Kind!=RunCommandKind.FiveFloors || Phase!=RunPhase.MapSelection
                || Map.ActiveNodeIndex>=0 || Map.CompletedNodeCount!=0)return false;
            Map=new RunMapProgress(BossLottery.Create(Map.Definition,Seed));HasBattleRework=true;
            return Record(true,RunCommandKind.BattleRework);
        }
    }
    public static class BossLottery
    {
        public static bool UseAlternate(uint seed,int floor)
        {
            uint value=unchecked(seed ^ ((uint)(floor+1)*0x9e3779b9u));
            value^=value>>16; value=unchecked(value*0x85ebca6bu); value^=value>>13;
            value=unchecked(value*0xc2b2ae35u);value^=value>>16;
            return (value&1)!=0;
        }
        public static RunMapDefinition Create(RunMapDefinition source,uint seed)
        {
            var nodes=new RunMapNodeDefinition[source.NodeCount];
            for(int i=0;i<nodes.Length;i++)
            {
                var old=source.GetNode(i);nodes[i]=old;
                int floor=old.Layer/source.RoomsPerFloor;
                if(old.Kind!=RunNodeKind.Boss || !UseAlternate(seed,floor))continue;
                var entry=EnemyArchive.Find((EnemyBehaviorKind)(24+floor));var b=old.Battle;
                var battle=new BattleDefinition(b.PlayerMaxHealth,b.PlayerMaxEnergy,
                    new[]{entry.Create("lottery.boss."+floor,6.6,1.5)},b.Archetype,fragments:true);
                var edges=new int[old.NextNodeCount];
                for(int n=0;n<edges.Length;n++)edges[n]=old.GetNextNodeIndex(n);
                nodes[i]=new RunMapNodeDefinition(old.Id,entry.Name+" · 층 관문",old.Layer,old.Lane,old.Kind,battle,edges,old.Story,source.PlayerMaxHealth);
            }
            return new RunMapDefinition(nodes,source.RoomsPerFloor);
        }
    }
}

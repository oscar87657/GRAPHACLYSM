using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Runs;

namespace Graphaclysm.Application
{
    public static class TowerArchiveContent
    {
        public static RunMapDefinition Create(RunMapDefinition source,uint seed)
        {
            var nodes=new RunMapNodeDefinition[source.NodeCount];
            for(int i=0;i<nodes.Length;i++)
            {
                var old=source.GetNode(i);var battle=old.Battle;string name=old.DisplayName;
                int floor=old.Layer/source.RoomsPerFloor;
                if(battle!=null && (old.Kind==RunNodeKind.Boss || old.Layer>=5))
                {
                    bool boss=old.Kind==RunNodeKind.Boss;
                    int primary=boss?17+floor:(int)((seed+(uint)(old.Layer*7+old.Lane*5))%17);
                    int count=boss?2:old.Kind==RunNodeKind.Elite?3:2;
                    var enemies=new EnemyDefinition[count];
                    for(int e=0;e<count;e++)
                    {
                        int entry=e==0?primary:boss?(floor==1?13:floor==2?16:0):(primary+e*5)%17;
                        enemies[e]=EnemyArchive.All[entry].Create("archive."+i+"."+e,e==0?6.6:e==1?2.5:7.5,e==0?1.5:e==1?.4:-1.5,floor);
                    }
                    var terrain=new BattleTerrainDefinition[battle.TerrainCount];
                    for(int t=0;t<terrain.Length;t++) terrain[t]=battle.GetTerrain(t);
                    battle=new BattleDefinition(battle.PlayerMaxHealth,battle.PlayerMaxEnergy,enemies,battle.Archetype,fragments:true,terrain:terrain);
                    name=EnemyArchive.All[primary].Name+(boss?" · 층 관문":"의 전열");
                }
                var edges=new int[old.NextNodeCount];for(int e=0;e<edges.Length;e++)edges[e]=old.GetNextNodeIndex(e);
                nodes[i]=new RunMapNodeDefinition(old.Id,name,old.Layer,old.Lane,old.Kind,battle,edges,old.Story,source.PlayerMaxHealth);
            }
            return new RunMapDefinition(nodes,source.RoomsPerFloor);
        }
    }
}

using System.Collections.Generic;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Runs;

namespace Graphaclysm.Application
{
    public sealed partial class RunGameSession
    {
        public bool HasFiveFloors { get; private set; }
        public bool TryEnableFiveFloors()
        {
            if(IsPractice || !HasTowerArchive || HasFiveFloors || journal==null || journal.Count!=8
                || journal[7].Kind!=RunCommandKind.TowerArchive || Phase!=RunPhase.MapSelection
                || Map.ActiveNodeIndex>=0 || Map.CompletedNodeCount!=0)return false;
            Map=new RunMapProgress(FiveFloorTower.Create(Map.Definition,Seed));HasFiveFloors=true;
            return Record(true,RunCommandKind.FiveFloors);
        }
    }
    public static class FiveFloorTower
    {
        public static RunMapDefinition Create(RunMapDefinition source,uint seed)
        {
            var template=new List<int>();
            for(int i=0;i<source.NodeCount;i++)if(source.GetNode(i).Layer>=16)template.Add(i);
            int n=source.NodeCount,m=template.Count;
            var nodes=new RunMapNodeDefinition[n+2*m];
            var starts=new List<int>();
            for(int j=0;j<m;j++)if(source.GetNode(template[j]).Layer==16)starts.Add(n+j);
            for(int i=0;i<n;i++)
            {
                var old=source.GetNode(i);int[] edges;
                if(old.NextNodeCount==0)edges=starts.ToArray();
                else {edges=new int[old.NextNodeCount];for(int e=0;e<edges.Length;e++)edges[e]=old.GetNextNodeIndex(e);}
                nodes[i]=new RunMapNodeDefinition(old.Id,old.DisplayName,old.Layer,old.Lane,old.Kind,old.Battle,edges,old.Story,source.PlayerMaxHealth);
            }
            for(int floor=3;floor<5;floor++)for(int j=0;j<m;j++)
            {
                int offset=n+(floor-3)*m;var old=source.GetNode(template[j]);
                int layer=old.Layer+(floor-2)*8;var edges=new List<int>();
                for(int e=0;e<old.NextNodeCount;e++)edges.Add(offset+template.IndexOf(old.GetNextNodeIndex(e)));
                if(old.NextNodeCount==0 && floor==3)for(int k=0;k<starts.Count;k++)edges.Add(starts[k]+m);
                var battle=old.Battle;string name=old.DisplayName;
                if(battle!=null)
                {
                    bool boss=old.Kind==RunNodeKind.Boss;
                    int primary=boss?22+floor-3:20+(int)((seed+(uint)(layer+old.Lane))%2);
                    int count=boss?1:old.Kind==RunNodeKind.Elite?3:2;
                    var enemies=new EnemyDefinition[count];
                    for(int e=0;e<count;e++)
                    {
                        int entry=e==0?primary:(int)((seed+(uint)(layer*7+old.Lane+e*3))%17);
                        enemies[e]=EnemyArchive.All[entry].Create("spire."+floor+"."+j+"."+e,e==0?6.6:e==1?3:8,e==0?1.5:e==1?-2:2.8,floor);
                    }
                    // Large ground patterns need unobstructed escape cells at bosses.
                    var terrain=new BattleTerrainDefinition[boss?0:battle.TerrainCount];
                    for(int t=0;t<terrain.Length;t++)terrain[t]=battle.GetTerrain(t);
                    battle=new BattleDefinition(battle.PlayerMaxHealth,battle.PlayerMaxEnergy,enemies,battle.Archetype,fragments:true,terrain:terrain);
                    name=EnemyArchive.All[primary].Name+(boss?" · 층 관문":"의 전열");
                }
                nodes[offset+j]=new RunMapNodeDefinition("spire."+floor+"."+old.Id,name,layer,old.Lane,old.Kind,battle,edges.ToArray(),old.Story,source.PlayerMaxHealth);
            }
            return new RunMapDefinition(nodes,8);
        }
    }
}

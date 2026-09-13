using System;
using System.Collections.Generic;
using Graphaclysm.Core.Runs;
using UnityEngine;

namespace Graphaclysm.Runtime.Presentation
{
    /// <summary>One ascending dependency graph. Section and equipment slot never determine screen regions.</summary>
    public sealed class GrowthTreeLayout
    {
        public readonly Vector2[] Positions;
        public readonly int[] Heights;
        public readonly int[][] Parents;
        public readonly int[] PrimaryParents;
        public readonly bool[][] AlternativeEdges;
        public readonly bool[,] RedundantEdges;
        public readonly bool[] Landmarks;
        public readonly string[] RequirementText;
        public readonly Rect Bounds;
        public readonly int Root;

        public GrowthTreeLayout(RunGrowthState growth)
        {
            int count = growth.NodeCount;
            Positions = new Vector2[count]; Heights = new int[count]; Parents = new int[count][];
            PrimaryParents = new int[count];
            RedundantEdges = new bool[count,count];
            AlternativeEdges = new bool[count][]; Landmarks = new bool[count]; RequirementText = new string[count];
            if(growth.IsSpecialized)
            {
                Root=0;
                for(int i=0;i<count;i++)
                {
                    int parent=growth.ParentOf(i);Parents[i]=parent<0?Array.Empty<int>():new[]{parent};
                    PrimaryParents[i]=parent;AlternativeEdges[i]=new bool[Parents[i].Length];
                    Heights[i]=growth.IsStyleTree?StyleTreeCatalog.Depth(i):ApproachGrowthCatalog.Depth(i);Landmarks[i]=i==0||growth.GetNode(i).Cost==2;
                    RequirementText[i]=parent<0?"무료 출발 능력 · 반환할 수 없습니다.":"먼저 습득 · "+growth.GetNode(parent).Name;
                    if(growth.GetNode(i).ExclusiveGroup.Length>0)RequirementText[i]+=growth.IsStyleTree?"\n같은 종류의 각성 셋 중 하나 · 기술과 궁극 각성은 병행 가능":"\n양자택일 · 반대 갈래와 동시 습득 불가";
                    if(i==0){Positions[i]=Vector2.zero;continue;}
                    if(growth.IsStyleTree)
                    {
                        int branch=(i-1)/5,part=(i-1)%5;float center=(branch-1)*840;
                        Positions[i]=new Vector2(center+(part==0?0:part<=2?-210:210),-Heights[i]*330-(branch==1?90:0));
                        continue;
                    }
                    int group=(i-1)/18,local=(i-1)%18;
                    float x=local==0?(group-1)*920:Positions[parent].x+(local==1?-280:local==2?280:0);
                    Positions[i]=new Vector2(x,-Heights[i]*300-group*80);
                }
                Bounds=growth.IsStyleTree?Rect.MinMaxRect(-1250,-1190,1250,130):Rect.MinMaxRect(-1380,-2580,1380,160);return;
            }
            Root = growth.IndexOf(GrowthTreePaths.RootId);
            var marks = new byte[count];
            for (int i = 0; i < count; i++)
            {
                GrowthNodeDefinition node = growth.GetNode(i);
                var parents = new List<int>(); var alternatives = new List<bool>();
                AddParents(growth, node.RequiresAll, false, parents, alternatives);
                AddParents(growth, node.RequiresAtLeast, true, parents, alternatives);
                AddParents(growth, GrowthTreePaths.Entrances(node.Id), true, parents, alternatives);
                Parents[i] = parents.ToArray(); AlternativeEdges[i] = alternatives.ToArray();
                Landmarks[i] = node.Cost == 2 || node.EquipKind == GrowthEquipKind.Identity;
                RequirementText[i] = DescribeRequirements(growth, node);
            }
            int maximum = 0;
            for (int i = 0; i < count; i++) maximum = Math.Max(maximum, Height(growth, i, marks));
            var ancestor = new bool[count,count];
            for (int height = 0; height <= maximum; height++)
                for (int child = 0; child < count; child++)
                    if (Heights[child] == height)
                        foreach (int parent in Parents[child])
                        {
                            ancestor[child,parent] = true;
                            for (int a = 0; a < count; a++) ancestor[child,a] |= ancestor[parent,a];
                        }
            for (int child = 0; child < count; child++)
                foreach (int parent in Parents[child])
                    foreach (int other in Parents[child])
                        if (other != parent && ancestor[other,parent]) RedundantEdges[child,parent] = true;
            var rows = new List<int>[maximum + 1];
            for (int row = 0; row <= maximum; row++) rows[row] = new List<int>();
            for (int i = 0; i < count; i++) rows[Heights[i]].Add(i);
            var desired = new float[count];
            float rowTop = 0;
            for (int row = 0; row <= maximum; row++)
            {
                foreach (int index in rows[row])
                {
                    float sum = 0;
                    foreach (int parent in Parents[index]) sum += Positions[parent].x;
                    float ancestry = Parents[index].Length == 0 ? 0 : sum / Parents[index].Length;
                    // Related effects stay near one another; the branch bends and merges instead of mirroring.
                    desired[index] = index == Root ? 0 : ancestry * .8f + Affinity(growth.GetNode(index)) * 160f;
                    Positions[index] = new Vector2(desired[index], 0);
                }
                rows[row].Sort((a, b) => desired[a] == desired[b] ? string.CompareOrdinal(growth.GetNode(a).Id, growth.GetNode(b).Id)
                    : desired[a].CompareTo(desired[b]));
                int bands = Math.Max(1, (rows[row].Count + 5) / 6);
                for (int band = 0; band < bands; band++)
                {
                    int members = (rows[row].Count - band + bands - 1) / bands;
                    float mean = 0;
                    for (int ordinal = band; ordinal < rows[row].Count; ordinal += bands) mean += desired[rows[row][ordinal]];
                    int firstLane = members == 0 ? 0 : Mathf.Clamp(Mathf.RoundToInt(mean / members / 280) - members / 2, -3, 3-members+1);
                    int lane = firstLane;
                    for (int ordinal = band; ordinal < rows[row].Count; ordinal += bands)
                    {
                        int index = rows[row][ordinal];
                        float x = index == Root ? 0 : lane++ * 280f;
                        Positions[index] = new Vector2(x, -rowTop - band * 285f);
                    }
                }
                rowTop += bands * 285f + 55f;
            }
            float minX = 0, maxX = 0;
            for (int i = 0; i < count; i++) { minX = Mathf.Min(minX, Positions[i].x); maxX = Mathf.Max(maxX, Positions[i].x); }
            float minY = 0;
            for (int i = 0; i < count; i++) minY = Mathf.Min(minY, Positions[i].y);
            Bounds = Rect.MinMaxRect(minX - 170, minY - 150, maxX + 170, 180);
            for (int i = 0; i < count; i++)
            {
                PrimaryParents[i] = -1;
                float shortest = float.PositiveInfinity;
                foreach (int parent in Parents[i])
                {
                    if (RedundantEdges[i,parent]) continue;
                    float distance = (Positions[parent] - Positions[i]).sqrMagnitude;
                    if (distance < shortest) { shortest = distance; PrimaryParents[i] = parent; }
                }
            }
        }

        private int Height(RunGrowthState growth, int index, byte[] marks)
        {
            if (marks[index] == 2) return Heights[index];
            if (marks[index] == 1) throw new InvalidOperationException("Growth path cycle: " + growth.GetNode(index).Id);
            marks[index] = 1;
            int height = GrowthTreePaths.MinimumHeight(growth.GetNode(index));
            foreach (int parent in Parents[index]) height = Math.Max(height, Height(growth, parent, marks) + 1);
            Heights[index] = height; marks[index] = 2;
            return height;
        }

        private static void AddParents(RunGrowthState growth, string[] ids, bool alternative, List<int> parents, List<bool> alternatives)
        {
            foreach (string id in ids)
            {
                int parent = growth.IndexOf(id);
                if (parent < 0) throw new InvalidOperationException("Inaccessible growth path: " + id);
                if (parents.Contains(parent)) continue;
                parents.Add(parent); alternatives.Add(alternative);
            }
        }

        private static string DescribeRequirements(RunGrowthState growth, GrowthNodeDefinition node)
        {
            string text = "";
            if (node.RequiresAll.Length > 0) text += "먼저 배우기  ·  " + Names(growth, node.RequiresAll);
            if (node.RequiresAtLeast.Length > 0) text += "\n다음 중 " + node.RequiresAtLeastCount + "개  ·  " + Names(growth, node.RequiresAtLeast);
            string[] entrances = GrowthTreePaths.Entrances(node.Id);
            if (entrances.Length > 0) text += "\n이 길 중 하나  ·  " + Names(growth, entrances);
            return text.Length == 0 ? "이 노드에서 성장을 시작합니다." : text.TrimStart('\n');
        }

        private static string Names(RunGrowthState growth, string[] ids)
        {
            var names = new string[ids.Length];
            for (int i = 0; i < ids.Length; i++) names[i] = growth.GetNode(growth.IndexOf(ids[i])).Name;
            return string.Join(" / ", names);
        }

        private static float Affinity(GrowthNodeDefinition node)
        {
            // Spatial bias describes what the action does, not which button activates it.
            string id = node.Id;
            if (id.Contains(".geometry.")) return -2.0f;
            if (id.Contains(".origin.")) return .2f;
            if (id.Contains(".self.")) return 2.4f;
            if (id.Contains(".fragments.")) return -.7f;
            if (id.Contains(".condense.")) return 1.2f;
            if (id.Contains(".resonance.")) return -1.2f;
            if (id.Contains(".execute.") || id.Contains(".brand.") || id.Contains(".inscription.")
                || id.Contains(".critical.") || id.Contains(".collapse.") || id.Contains(".blade.")) return -2.3f;
            if (id.Contains(".wall.") || id.Contains(".citadel.") || id.Contains(".glassbody.")
                || id.Contains(".observatory.") || id.Contains(".embrace.") || id.Contains(".white.")) return 2.3f;
            if (id.Contains(".archive.") || id.Contains(".shelf.") || id.Contains(".library.")
                || id.Contains(".immortal.") || id.Contains(".timeless.") || id.Contains(".phase.")) return 1f;
            if (id.Contains(".pull.") || id.Contains(".rotate.") || id.Contains(".align.")
                || id.Contains(".triple.") || id.Contains(".spike.") || id.Contains(".rain.")) return -1.3f;
            if (id.Contains(".bridge.")) return 0;
            return .4f;
        }
    }
}

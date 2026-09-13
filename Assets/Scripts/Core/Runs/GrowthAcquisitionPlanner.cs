using System;
using System.Collections.Generic;

namespace Graphaclysm.Core.Runs
{
    public sealed class GrowthAcquisitionPlan
    {
        internal GrowthAcquisitionPlan(int[] order, int cost, int available, string blocker, int unsupported)
        { NodeIndices = Array.AsReadOnly(order); Cost = cost; Remaining = available - cost; Blocker = blocker; UnsupportedCount = unsupported; }
        public IReadOnlyList<int> NodeIndices { get; }
        public int Cost { get; }
        public int Remaining { get; }
        public string Blocker { get; }
        public int UnsupportedCount { get; }
    }

    // Allocations belong to a user-requested planning operation, never to repaint.
    public sealed class GrowthAcquisitionPlanner
    {
        private readonly RunGrowthState source;
        private int visits;
        private readonly int limit;
        public GrowthAcquisitionPlanner(RunGrowthState source, int limit = 10000)
        { this.source = source ?? throw new ArgumentNullException(nameof(source)); this.limit = limit; }
        public bool NeedsChoices { get; private set; }
        public IReadOnlyList<GrowthAcquisitionPlan> Plan(int target)
        {
            visits = 0; NeedsChoices = false;
            var result = new List<GrowthAcquisitionPlan>();
            if (target < 0 || target >= source.NodeCount || source.IsUnlocked(target)) return result;
            try
            {
                var sets = Expand(target, new HashSet<int>());
                var unique = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
                foreach (var set in sets)
                {
                    var ordered = new List<int>(set); ordered.Sort((a,b) => StringComparer.Ordinal.Compare(source.GetNode(a).Id, source.GetNode(b).Id));
                    unique[string.Join(",", ordered)] = set;
                }
                foreach (var set in unique.Values)
                {
                    bool redundant = false;
                    foreach (var other in unique.Values) if (other.Count < set.Count && other.IsSubsetOf(set)) { redundant = true; break; }
                    if (redundant) continue;
                    var simulation = source.CopyForPlanning(); simulation.AddPoints(1000);
                    var remaining = new List<int>(set);
                    remaining.Sort((a,b) => StringComparer.Ordinal.Compare(source.GetNode(a).Id, source.GetNode(b).Id));
                    var sequence = new List<int>(); int cost = 0, unsupported = 0;
                    foreach (int i in remaining) { cost += source.GetNode(i).Cost; if (!source.GetNode(i).IsImplemented) unsupported++; }
                    while (remaining.Count > 0)
                    {
                        int next = -1;
                        for (int i = 0; i < remaining.Count; i++) if (simulation.CanPurchase(remaining[i])) { next = i; break; }
                        if (next < 0) break;
                        int index = remaining[next]; simulation.TryPurchase(index); sequence.Add(index); remaining.RemoveAt(next);
                    }
                    string blocker = remaining.Count == 0 ? "" : "투자 단계 또는 배타 조건 미충족: " + source.GetNode(remaining[0]).Name;
                    sequence.AddRange(remaining);
                    result.Add(new GrowthAcquisitionPlan(sequence.ToArray(), cost, source.Points, blocker, unsupported));
                }
                result.Sort((a,b) => { int c = a.Cost.CompareTo(b.Cost); if (c != 0) return c;
                    for (int i = 0; i < Math.Min(a.NodeIndices.Count,b.NodeIndices.Count); i++)
                    { c = StringComparer.Ordinal.Compare(source.GetNode(a.NodeIndices[i]).Id,source.GetNode(b.NodeIndices[i]).Id); if(c != 0) return c; }
                    return a.NodeIndices.Count.CompareTo(b.NodeIndices.Count); });
            }
            catch (PlanningLimitException) { NeedsChoices = true; result.Clear(); }
            return result;
        }
        private void Visit() { if (++visits > limit) throw new PlanningLimitException(); }
        private sealed class PlanningLimitException : Exception { }
        private List<HashSet<int>> Join(List<HashSet<int>> a, List<HashSet<int>> b)
        {
            var result = new List<HashSet<int>>();
            foreach (var x in a) foreach (var y in b)
            { Visit(); var set = new HashSet<int>(x); set.UnionWith(y); result.Add(set); }
            return result;
        }
        private List<HashSet<int>> Expand(int index, HashSet<int> active)
        {
            Visit();
            if (source.IsUnlocked(index)) return new List<HashSet<int>> { new HashSet<int>() };
            if (index < 0 || !active.Add(index)) throw new InvalidOperationException("Invalid growth dependency.");
            var n = source.GetNode(index);
            var options = new List<HashSet<int>> { new HashSet<int> { index } };
            foreach (string id in n.RequiresAll) options = Join(options, Expand(source.IndexOf(id), active));
            string[] entrances = GrowthTreePaths.Entrances(n.Id);
            if (entrances.Length > 0)
            {
                var alternatives = new List<HashSet<int>>();
                foreach (string id in entrances) alternatives.AddRange(Expand(source.IndexOf(id), active));
                options = Join(options, alternatives);
            }
            if (n.RequiresAtLeastCount > 0)
                options = Join(options, Threshold(n.RequiresAtLeast, 0, n.RequiresAtLeastCount, active));
            active.Remove(index); return options;
        }
        private List<HashSet<int>> Threshold(string[] ids, int start, int count, HashSet<int> active)
        {
            Visit();
            if (count == 0) return new List<HashSet<int>> { new HashSet<int>() };
            var choices = new List<HashSet<int>>();
            for (int i = start; i <= ids.Length - count; i++)
                choices.AddRange(Join(Expand(source.IndexOf(ids[i]), active), Threshold(ids, i + 1, count - 1, active)));
            return choices;
        }
    }
    public sealed partial class RunGrowthState
    {
        internal RunGrowthState CopyForPlanning()
        {
            var copy = new RunGrowthState(Archetype,SpecializedApproach,IsStyleTree,false,InitialPoints==3);
            Array.Copy(acquired, copy.acquired, acquired.Length); Array.Copy(equipped, copy.equipped, equipped.Length);
            copy.awardedPoints = awardedPoints; copy.spentPoints = spentPoints;
            return copy;
        }
    }
}

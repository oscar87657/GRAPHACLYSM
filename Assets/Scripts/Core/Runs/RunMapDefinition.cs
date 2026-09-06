using System;
using System.Collections.Generic;
using Graphaclysm.Core.Combat;

namespace Graphaclysm.Core.Runs
{
    /// <summary>
    /// Immutable, validated directed map definition.
    /// Edges must always lead to a later layer, which prevents cycles by construction.
    /// </summary>
    public sealed class RunMapDefinition
    {
        private readonly RunMapNodeDefinition[] nodes;
        private readonly int[] startNodeIndices;

        public RunMapDefinition(IReadOnlyList<RunMapNodeDefinition> nodes, int roomsPerFloor = 0)
        {
            if (nodes == null || nodes.Count == 0)
            {
                throw new ArgumentException("A run map requires at least one node.", nameof(nodes));
            }

            this.nodes = new RunMapNodeDefinition[nodes.Count];
            int maximumLayer = 0;
            int maximumLane = 0;

            for (int i = 0; i < nodes.Count; i++)
            {
                RunMapNodeDefinition node = nodes[i]
                    ?? throw new ArgumentException("Run map nodes cannot contain null.", nameof(nodes));
                this.nodes[i] = node;
                maximumLayer = Math.Max(maximumLayer, node.Layer);
                maximumLane = Math.Max(maximumLane, node.Lane);

                for (int previous = 0; previous < i; previous++)
                {
                    if (string.Equals(this.nodes[previous].Id, node.Id, StringComparison.Ordinal))
                    {
                        throw new ArgumentException("Run map node ids must be unique.", nameof(nodes));
                    }
                }
            }

            LayerCount = maximumLayer + 1;
            if(roomsPerFloor<0 || (roomsPerFloor>0 && LayerCount%roomsPerFloor!=0)) throw new ArgumentOutOfRangeException(nameof(roomsPerFloor));
            RoomsPerFloor = roomsPerFloor == 0 ? LayerCount : roomsPerFloor;
            LaneCount = maximumLane + 1;
            PlayerMaxHealth = this.nodes[0].PlayerMaxHealth;

            int startCount = 0;
            for (int i = 0; i < this.nodes.Length; i++)
            {
                RunMapNodeDefinition node = this.nodes[i];
                if (node.PlayerMaxHealth != PlayerMaxHealth)
                {
                    throw new ArgumentException(
                        "All encounters in one run must use the same player maximum health.",
                        nameof(nodes));
                }

                if (node.Layer == 0)
                {
                    startCount++;
                }

                ValidateEdges(i, node);
            }

            if (startCount == 0)
            {
                throw new ArgumentException("A run map requires a node on layer zero.", nameof(nodes));
            }

            startNodeIndices = new int[startCount];
            int startIndex = 0;
            for (int i = 0; i < this.nodes.Length; i++)
            {
                if (this.nodes[i].Layer == 0)
                {
                    startNodeIndices[startIndex] = i;
                    startIndex++;
                }
            }

            ValidateReachability();
        }

        public int NodeCount
        {
            get { return nodes.Length; }
        }

        public int StartNodeCount
        {
            get { return startNodeIndices.Length; }
        }

        public int RoomsPerFloor { get; }
        public int FloorCount => LayerCount / RoomsPerFloor;
        public int LayerCount { get; }
        public int LaneCount { get; }
        public int PlayerMaxHealth { get; }

        public RunMapNodeDefinition GetNode(int index)
        {
            return nodes[index];
        }

        public int GetStartNodeIndex(int index)
        {
            return startNodeIndices[index];
        }

        public static RunMapDefinition CreateLinear(IReadOnlyList<BattleDefinition> encounters)
        {
            if (encounters == null || encounters.Count == 0)
            {
                throw new ArgumentException("A linear map requires at least one encounter.", nameof(encounters));
            }

            var linearNodes = new RunMapNodeDefinition[encounters.Count];
            for (int i = 0; i < encounters.Count; i++)
            {
                int[] next = i < encounters.Count - 1 ? new[] { i + 1 } : Array.Empty<int>();
                RunNodeKind kind = i == encounters.Count - 1
                    ? RunNodeKind.Boss
                    : RunNodeKind.Battle;
                linearNodes[i] = new RunMapNodeDefinition(
                    "linear." + i,
                    kind == RunNodeKind.Boss ? "최종 조우" : "조우 " + (i + 1),
                    i,
                    0,
                    kind,
                    encounters[i],
                    next);
            }

            return new RunMapDefinition(linearNodes);
        }

        private void ValidateEdges(int sourceIndex, RunMapNodeDefinition source)
        {
            for (int edge = 0; edge < source.NextNodeCount; edge++)
            {
                int targetIndex = source.GetNextNodeIndex(edge);
                if (targetIndex < 0 || targetIndex >= nodes.Length)
                {
                    throw new ArgumentException("A run map edge points outside the map.", nameof(nodes));
                }

                if (targetIndex == sourceIndex || nodes[targetIndex].Layer <= source.Layer)
                {
                    throw new ArgumentException(
                        "Run map edges must point to a node on a later layer.",
                        nameof(nodes));
                }

                for (int previous = 0; previous < edge; previous++)
                {
                    if (source.GetNextNodeIndex(previous) == targetIndex)
                    {
                        throw new ArgumentException("A run map node cannot repeat an edge.", nameof(nodes));
                    }
                }
            }
        }

        private void ValidateReachability()
        {
            var reachable = new bool[nodes.Length];
            var queue = new int[nodes.Length];
            int read = 0;
            int write = 0;

            for (int i = 0; i < startNodeIndices.Length; i++)
            {
                int nodeIndex = startNodeIndices[i];
                reachable[nodeIndex] = true;
                queue[write++] = nodeIndex;
            }

            while (read < write)
            {
                RunMapNodeDefinition node = nodes[queue[read++]];
                for (int edge = 0; edge < node.NextNodeCount; edge++)
                {
                    int nextIndex = node.GetNextNodeIndex(edge);
                    if (reachable[nextIndex])
                    {
                        continue;
                    }

                    reachable[nextIndex] = true;
                    queue[write++] = nextIndex;
                }
            }

            for (int i = 0; i < reachable.Length; i++)
            {
                if (!reachable[i])
                {
                    throw new ArgumentException("Every run map node must be reachable.", nameof(nodes));
                }
            }
        }
    }
}

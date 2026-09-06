using System;

namespace Graphaclysm.Core.Runs
{
    public enum RunMapProgressPhase
    {
        ChoosingNode,
        ResolvingNode,
        Completed
    }

    /// <summary>
    /// Owns traversal state for a validated run map. Runtime views can query but not mutate its arrays.
    /// </summary>
    public sealed class RunMapProgress
    {
        private readonly bool[] availableNodes;
        private readonly bool[] completedNodes;

        public RunMapProgress(RunMapDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            availableNodes = new bool[definition.NodeCount];
            completedNodes = new bool[definition.NodeCount];

            for (int i = 0; i < definition.StartNodeCount; i++)
            {
                availableNodes[definition.GetStartNodeIndex(i)] = true;
            }

            ActiveNodeIndex = -1;
            LastCompletedNodeIndex = -1;
            Phase = RunMapProgressPhase.ChoosingNode;
        }

        public RunMapDefinition Definition { get; }
        public RunMapProgressPhase Phase { get; private set; }
        public int ActiveNodeIndex { get; private set; }
        public int LastCompletedNodeIndex { get; private set; }
        public int CompletedNodeCount { get; private set; }

        public bool IsAvailable(int nodeIndex)
        {
            ValidateNodeIndex(nodeIndex);
            return availableNodes[nodeIndex];
        }

        public bool IsCompleted(int nodeIndex)
        {
            ValidateNodeIndex(nodeIndex);
            return completedNodes[nodeIndex];
        }

        public bool TryEnterNode(int nodeIndex)
        {
            if (Phase != RunMapProgressPhase.ChoosingNode)
            {
                return false;
            }

            ValidateNodeIndex(nodeIndex);
            if (!availableNodes[nodeIndex])
            {
                return false;
            }

            ClearAvailableNodes();
            ActiveNodeIndex = nodeIndex;
            Phase = RunMapProgressPhase.ResolvingNode;
            return true;
        }

        public bool TryCompleteActiveNode()
        {
            if (Phase != RunMapProgressPhase.ResolvingNode || ActiveNodeIndex < 0)
            {
                return false;
            }

            int completedIndex = ActiveNodeIndex;
            completedNodes[completedIndex] = true;
            CompletedNodeCount++;
            LastCompletedNodeIndex = completedIndex;
            ActiveNodeIndex = -1;

            RunMapNodeDefinition completed = Definition.GetNode(completedIndex);
            if (completed.NextNodeCount == 0)
            {
                Phase = RunMapProgressPhase.Completed;
                return true;
            }

            for (int i = 0; i < completed.NextNodeCount; i++)
            {
                availableNodes[completed.GetNextNodeIndex(i)] = true;
            }

            Phase = RunMapProgressPhase.ChoosingNode;
            return true;
        }

        private void ClearAvailableNodes()
        {
            for (int i = 0; i < availableNodes.Length; i++)
            {
                availableNodes[i] = false;
            }
        }

        private void ValidateNodeIndex(int nodeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= availableNodes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeIndex));
            }
        }
    }
}

using System;
using Graphaclysm.Core.Combat;

namespace Graphaclysm.Core.Runs
{
    public enum RunNodeKind
    {
        Battle,
        Elite,
        Boss,
        Event,
        Treasure,
        Rest
    }

    /// <summary>
    /// Immutable gameplay data for one node in a run map.
    /// Layer and lane are logical positions, not screen coordinates.
    /// </summary>
    public sealed class RunMapNodeDefinition
    {
        private readonly int[] nextNodeIndices;

        public RunMapNodeDefinition(
            string id,
            string displayName,
            int layer,
            int lane,
            RunNodeKind kind,
            BattleDefinition battle,
            int[] nextNodeIndices, RoomStory story = null, int playerMaxHealth = 0)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A run node id is required.", nameof(id));
            }

            if (layer < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(layer));
            }

            if (lane < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lane));
            }

            Id = id;
            DisplayName = displayName ?? string.Empty;
            Layer = layer;
            Lane = lane;
            Kind = kind;
            bool combat = kind == RunNodeKind.Battle || kind == RunNodeKind.Elite || kind == RunNodeKind.Boss;
            if (combat && battle == null) throw new ArgumentNullException(nameof(battle));
            if (!combat && (story == null || playerMaxHealth <= 0)) throw new ArgumentException("A room needs story and player health metadata.");
            Battle = battle; Story = story; PlayerMaxHealth = battle?.PlayerMaxHealth ?? playerMaxHealth;

            int count = nextNodeIndices == null ? 0 : nextNodeIndices.Length;
            this.nextNodeIndices = new int[count];
            if (count > 0)
            {
                Array.Copy(nextNodeIndices, this.nextNodeIndices, count);
            }
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int Layer { get; }
        public int Lane { get; }
        public RunNodeKind Kind { get; }
        public BattleDefinition Battle { get; }
        public RoomStory Story { get; }
        public int PlayerMaxHealth { get; }
        public int NextNodeCount
        {
            get { return nextNodeIndices.Length; }
        }

        public int GetNextNodeIndex(int index)
        {
            return nextNodeIndices[index];
        }
    }
}

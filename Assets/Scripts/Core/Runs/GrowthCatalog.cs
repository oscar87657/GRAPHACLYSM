using System;
using Graphaclysm.Core.Combat;

namespace Graphaclysm.Core.Runs
{
    public enum GrowthOwner
    {
        Common,
        Ian,
        Luna
    }

    public enum GrowthEquipKind
    {
        Passive,
        Identity,
        CombatForm,
        Trait,
        Ultimate
    }

    public enum GrowthImplementationStatus
    {
        Planned,
        PrototypePartial
    }

    public sealed class GrowthNodeDefinition
    {
        public GrowthNodeDefinition(
            string id,
            GrowthOwner owner,
            string section,
            string name,
            string effect,
            string drawback,
            int cost,
            string kind,
            string slot,
            string family,
            string exclusiveGroup,
            string equipLimitGroup,
            int equipLimit,
            string[] requiresAll,
            int requiresAtLeastCount,
            string[] requiresAtLeast,
            GrowthImplementationStatus implementationStatus)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("A growth node needs a stable ID.", nameof(id));
            if (cost < (id.StartsWith("path.",StringComparison.Ordinal)?0:1) || cost > 2) throw new ArgumentOutOfRangeException(nameof(cost));
            Id = id;
            CommandId = GrowthCatalog.StableCommandId(id);
            Owner = owner;
            Section = section ?? "";
            Name = name ?? id;
            Effect = effect ?? "";
            Drawback = drawback ?? "";
            Cost = cost;
            Kind = kind ?? "";
            Slot = slot ?? "";
            Family = family ?? "";
            ExclusiveGroup = exclusiveGroup ?? "";
            EquipLimitGroup = equipLimitGroup ?? "";
            EquipLimit = equipLimit;
            RequiresAll = requiresAll ?? Array.Empty<string>();
            RequiresAtLeastCount = requiresAtLeastCount;
            RequiresAtLeast = requiresAtLeast ?? Array.Empty<string>();
            ImplementationStatus = implementationStatus;
        }

        public string Id { get; }
        public int CommandId { get; }
        public GrowthOwner Owner { get; }
        public string Section { get; }
        public string Name { get; }
        public string Effect { get; }
        public string Drawback { get; }
        public int Cost { get; }
        public string Kind { get; }
        public string Slot { get; }
        public string Family { get; }
        public string ExclusiveGroup { get; }
        public string EquipLimitGroup { get; }
        public int EquipLimit { get; }
        public string[] RequiresAll { get; }
        public int RequiresAtLeastCount { get; }
        public string[] RequiresAtLeast { get; }
        public GrowthImplementationStatus ImplementationStatus { get; }
        public bool IsImplemented => ImplementationStatus != GrowthImplementationStatus.Planned;

        public GrowthEquipKind EquipKind
        {
            get
            {
                if (Kind == "핵심 능력") return GrowthEquipKind.Identity;
                if (Kind == "전투 기술 형태") return GrowthEquipKind.CombatForm;
                if (Kind == "궁극기 형태") return GrowthEquipKind.Ultimate;
                if (Kind == "핵심 능력 특성" || Kind == "기술 특성" || Kind == "궁극기 변형")
                    return GrowthEquipKind.Trait;
                return GrowthEquipKind.Passive;
            }
        }
    }

    /// <summary>
    /// Generated from GRAPHACLYSM_성좌_240노드.json by Tools/GenerateGrowthCatalog.ps1.
    /// Stable string IDs are authoritative; command IDs are deterministic save/replay keys.
    /// </summary>
    public static partial class GrowthCatalog
    {
        private static readonly GrowthNodeDefinition[] definitions = CreateDefinitions();
        private static readonly GrowthNodeDefinition[] ian = Filter(GrowthOwner.Ian);
        private static readonly GrowthNodeDefinition[] luna = Filter(GrowthOwner.Luna);

        static GrowthCatalog()
        {
            if (definitions.Length != 240) throw new InvalidOperationException("The growth catalog must contain exactly 240 unique nodes.");
            for (int i = 0; i < definitions.Length; i++)
            {
                for (int j = i + 1; j < definitions.Length; j++)
                {
                    if (definitions[i].Id == definitions[j].Id)
                        throw new InvalidOperationException("Duplicate growth node ID: " + definitions[i].Id);
                    if (definitions[i].CommandId == definitions[j].CommandId)
                        throw new InvalidOperationException("Growth command ID collision: " + definitions[i].Id + " / " + definitions[j].Id);
                }
                ValidateDependencies(definitions[i]);
            }
            ValidateAcyclic();
            if (ian.Length != 144 || luna.Length != 144)
                throw new InvalidOperationException("Each character must have access to 144 growth nodes.");
        }

        public static GrowthNodeDefinition[] All => definitions;

        public static GrowthNodeDefinition[] For(CombatArchetype archetype)
        {
            if (archetype == CombatArchetype.Luna) return luna;
            // Legacy and test-only runs without an explicit combat archetype never expose
            // constellation UI, but still need a deterministic catalog for replay state.
            return ian;
        }

        public static GrowthNodeDefinition Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < definitions.Length; i++) if (definitions[i].Id == id) return definitions[i];
            return null;
        }

        public static int StableCommandId(string id)
        {
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < id.Length; i++)
                {
                    char character = id[i];
                    hash ^= (byte)character;
                    hash *= 16777619;
                    hash ^= (byte)(character >> 8);
                    hash *= 16777619;
                }
                return (int)hash;
            }
        }

        private static GrowthNodeDefinition[] Filter(GrowthOwner owner)
        {
            var result = new GrowthNodeDefinition[144];
            int count = 0;
            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i].Owner != GrowthOwner.Common && definitions[i].Owner != owner) continue;
                if (count >= result.Length) throw new InvalidOperationException("Too many growth nodes for " + owner);
                result[count++] = definitions[i];
            }
            if (count != result.Length) throw new InvalidOperationException("Wrong growth node count for " + owner + ": " + count);
            return result;
        }

        private static void ValidateDependencies(GrowthNodeDefinition node)
        {
            for (int i = 0; i < node.RequiresAll.Length; i++)
                if (Find(node.RequiresAll[i]) == null) throw new InvalidOperationException("Missing prerequisite " + node.RequiresAll[i]);
            for (int i = 0; i < node.RequiresAtLeast.Length; i++)
                if (Find(node.RequiresAtLeast[i]) == null) throw new InvalidOperationException("Missing prerequisite " + node.RequiresAtLeast[i]);
            if (node.RequiresAtLeastCount < 0 || node.RequiresAtLeastCount > node.RequiresAtLeast.Length)
                throw new InvalidOperationException("Invalid prerequisite count for " + node.Id);
        }

        private static void ValidateAcyclic()
        {
            var marks = new byte[definitions.Length];
            for (int i = 0; i < definitions.Length; i++) Visit(i, marks);
        }

        private static void Visit(int index, byte[] marks)
        {
            if (marks[index] == 2) return;
            if (marks[index] == 1) throw new InvalidOperationException("Cyclic growth prerequisite at " + definitions[index].Id);
            marks[index] = 1;
            GrowthNodeDefinition node = definitions[index];
            for (int i = 0; i < node.RequiresAll.Length; i++) Visit(FindIndex(node.RequiresAll[i]), marks);
            for (int i = 0; i < node.RequiresAtLeast.Length; i++) Visit(FindIndex(node.RequiresAtLeast[i]), marks);
            marks[index] = 2;
        }

        private static int FindIndex(string id)
        {
            for (int i = 0; i < definitions.Length; i++) if (definitions[i].Id == id) return i;
            return -1;
        }

    }
}

using System;

namespace Graphaclysm.Core.Runs
{
    /// <summary>
    /// Immutable encounter snapshot. Battle rule resolvers read this instead of mutable run growth.
    /// Acquired IDs describe available rule nodes; equipped IDs describe the active loadout.
    /// </summary>
    public sealed class CompiledGrowthBuild
    {
        private readonly string[] acquiredIds;
        private readonly string[] equippedIds;

        public CompiledGrowthBuild(string[] acquiredIds, string[] equippedIds)
        {
            this.acquiredIds = Copy(acquiredIds);
            this.equippedIds = Copy(equippedIds);
        }

        public int AcquiredCount => acquiredIds.Length;
        public int EquippedCount => equippedIds.Length;

        public string GetAcquiredId(int index)
        {
            if (index < 0 || index >= acquiredIds.Length) throw new ArgumentOutOfRangeException(nameof(index));
            return acquiredIds[index];
        }

        public string GetEquippedId(int index)
        {
            if (index < 0 || index >= equippedIds.Length) throw new ArgumentOutOfRangeException(nameof(index));
            return equippedIds[index];
        }

        public bool HasAcquired(string id) => Contains(acquiredIds, id);
        public bool IsEquipped(string id) => Contains(equippedIds, id);

        private static bool Contains(string[] values, string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            for (int i = 0; i < values.Length; i++) if (values[i] == id) return true;
            return false;
        }

        private static string[] Copy(string[] source)
        {
            if (source == null || source.Length == 0) return Array.Empty<string>();
            var copy = new string[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }
    }
}

using System;
using Graphaclysm.Core.Relics;

namespace Graphaclysm.Core.Runs
{
    /// <summary>
    /// Fixed-capacity relic ownership for one run. Duplicate relic ids are rejected.
    /// </summary>
    public sealed class RunRelicCollection
    {
        public const int MaximumRelics = 16;

        private readonly RelicDefinition[] relics = new RelicDefinition[MaximumRelics];
        private int count;

        public int Count
        {
            get { return count; }
        }

        public RelicDefinition GetRelic(int index)
        {
            if (index < 0 || index >= count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return relics[index];
        }

        public bool Contains(string relicId)
        {
            if (string.IsNullOrWhiteSpace(relicId))
            {
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                if (string.Equals(relics[i].Id, relicId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryAdd(RelicDefinition relic)
        {
            if (relic == null)
            {
                throw new ArgumentNullException(nameof(relic));
            }

            if (count >= relics.Length || Contains(relic.Id))
            {
                return false;
            }

            relics[count] = relic;
            count++;
            return true;
        }

        public int GetTotalMagnitude(RelicEffectKind effect)
        {
            int total = 0;
            for (int i = 0; i < count; i++)
            {
                if (relics[i].Effect == effect)
                {
                    total += relics[i].Magnitude;
                }
            }

            return total;
        }
    }
}

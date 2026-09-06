using System;

namespace Graphaclysm.Core.Combat
{
    public enum CombatStatusKind { Shield, Focus, Regeneration, Burn, Weaken, Exposure, Anchor, Haste }

    /// <summary>Eight fixed status slots per combatant, owned for one battle. No per-turn allocation.</summary>
    public sealed class CombatStatusState
    {
        public const int Capacity = 8;
        public const int MaximumMagnitude = 24;
        private readonly int[] magnitudes = new int[Capacity];
        private readonly int[] durations = new int[Capacity];

        public int Get(CombatStatusKind kind) => magnitudes[(int)kind];
        public int Duration(CombatStatusKind kind) => durations[(int)kind];

        internal void Add(CombatStatusKind kind, int amount, int duration)
        {
            int index = (int)kind;
            magnitudes[index] = Math.Min(MaximumMagnitude, magnitudes[index] + amount);
            durations[index] = Math.Max(durations[index], duration);
        }

        internal void Remove(CombatStatusKind kind)
        {
            magnitudes[(int)kind] = 0;
            durations[(int)kind] = 0;
        }

        internal int AbsorbDamage(int damage)
        {
            int shield = Math.Min(damage, Get(CombatStatusKind.Shield));
            magnitudes[(int)CombatStatusKind.Shield] -= shield;
            return damage - shield;
        }

        internal void Tick()
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (durations[i] > 0 && --durations[i] == 0) magnitudes[i] = 0;
            }
        }

        internal void Clear()
        {
            Array.Clear(magnitudes, 0, Capacity);
            Array.Clear(durations, 0, Capacity);
        }

        internal void Cleanse(bool debuffs)
        {
            for (int i = 0; i < Capacity; i++)
            {
                bool isDebuff = i >= (int)CombatStatusKind.Burn && i <= (int)CombatStatusKind.Anchor;
                if (isDebuff == debuffs) Remove((CombatStatusKind)i);
            }
        }
    }
}

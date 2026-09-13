using System;

namespace Graphaclysm.Core.Combat
{
    public enum CombatStatusKind
    {
        Shield, Focus, Regeneration, Burn, Weaken, Exposure, Anchor, Haste,
        Thorns, Momentum, Fortify, Rupture, Ward, Pierce, FrailShield, Wound, Guidance
    }

    /// <summary>Seventeen stable-ID slots including legacy Momentum. Owned per battle; no per-turn allocation.</summary>
    public sealed class CombatStatusState
    {
        public const int Capacity = 17;
        public bool Enhanced { get; internal set; }
        public const int MaximumMagnitude = 24;
        private readonly int[] magnitudes = new int[Capacity];
        private readonly int[] durations = new int[Capacity];

        public int Get(CombatStatusKind kind) => magnitudes[(int)kind];
        public int Duration(CombatStatusKind kind) => durations[(int)kind];

        internal void Add(CombatStatusKind kind, int amount, int duration)
        {
            if (Enhanced && kind == CombatStatusKind.Momentum) kind = CombatStatusKind.Focus;
            if (IsDebuff(kind) && Get(CombatStatusKind.Ward) > 0)
            {
                if (--magnitudes[(int)CombatStatusKind.Ward] == 0) Remove(CombatStatusKind.Ward);
                return;
            }
            if (kind == CombatStatusKind.Shield) amount = Math.Max(0, amount - Get(CombatStatusKind.FrailShield));
            if (amount <= 0) return;
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
                CombatStatusKind kind = (CombatStatusKind)i;
                bool isDebuff = IsDebuff(kind);
                if (isDebuff == debuffs) Remove((CombatStatusKind)i);
            }
        }
        public static bool IsDebuff(CombatStatusKind kind) => kind == CombatStatusKind.Burn
            || kind == CombatStatusKind.Weaken || kind == CombatStatusKind.Exposure || kind == CombatStatusKind.Anchor
            || kind == CombatStatusKind.Rupture || kind == CombatStatusKind.FrailShield
            || kind == CombatStatusKind.Wound || kind == CombatStatusKind.Guidance;
    }
}

using System;

namespace Graphaclysm.Core.Cards
{
    // Status entries deliberately mirror CombatStatusKind; Heal and Cleanse are immediate effects.
    public enum CardAbilityKind
    {
        Shield, Focus, Regeneration, Burn, Weaken, Exposure, Anchor, Haste,
        Thorns, Momentum, Fortify, Rupture, Heal, Cleanse
    }
    public enum AbilityTarget { Enemy, Player }

    /// <summary>One immutable on-hit rider, bundled with a graph operation.</summary>
    public readonly struct CardAbility
    {
        public CardAbility(CardAbilityKind kind, AbilityTarget target, int magnitude, int duration)
        {
            if (kind < CardAbilityKind.Shield || kind > CardAbilityKind.Cleanse) throw new ArgumentOutOfRangeException(nameof(kind));
            if (target != AbilityTarget.Player && target != AbilityTarget.Enemy) throw new ArgumentOutOfRangeException(nameof(target));
            if (magnitude < 1 || magnitude > 24 || duration < 1 || duration > 3) throw new ArgumentOutOfRangeException(nameof(magnitude));
            if (kind == CardAbilityKind.Heal && target == AbilityTarget.Enemy) throw new ArgumentException("Healing skills target the player.");
            Kind = kind; Target = target; Magnitude = magnitude; Duration = duration;
        }
        public CardAbilityKind Kind { get; }
        public AbilityTarget Target { get; }
        public int Magnitude { get; }
        public int Duration { get; }
    }
}

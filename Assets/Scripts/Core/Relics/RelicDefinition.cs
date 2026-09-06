using System;

namespace Graphaclysm.Core.Relics
{
    public enum RelicEffectKind
    {
        BonusEnergy,
        BonusHandSize,
        BonusPlotDamage,
        HealAfterVictory,
        BonusDraw,
        StartShield, StartResonance, SelfShield, FirstPlotDamage, ShortWeaveDamage, LongWeaveDamage, MovedPlotShield, VictoryResonance
    }

    /// <summary>
    /// Immutable relic content. A run collection owns only references to these shared definitions.
    /// </summary>
    public sealed class RelicDefinition
    {
        public const int MaximumMagnitude = 1000;

        public RelicDefinition(
            string id,
            string displayName,
            string description,
            RelicEffectKind effect,
            int magnitude)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A relic id is required.", nameof(id));
            }

            if (magnitude <= 0 || magnitude > MaximumMagnitude)
            {
                throw new ArgumentOutOfRangeException(nameof(magnitude));
            }

            Id = id;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
            Effect = effect;
            Magnitude = magnitude;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public RelicEffectKind Effect { get; }
        public int Magnitude { get; }
    }
}

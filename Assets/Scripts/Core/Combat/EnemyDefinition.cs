using System;

namespace Graphaclysm.Core.Combat
{
    public sealed class EnemyDefinition
    {
        public EnemyDefinition(string id, string displayName, double x, double y, int maxHealth, int attack)
            : this(id, displayName, x, y, maxHealth, attack,
                EnemyBehaviorDefinition.SteadyAttack())
        {
        }

        public EnemyDefinition(
            string id,
            string displayName,
            double x,
            double y,
            int maxHealth,
            int attack,
            EnemyBehaviorDefinition behavior)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("An enemy id is required.", nameof(id));
            }

            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            }

            if (attack < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attack));
            }

            if (double.IsNaN(x) || double.IsInfinity(x)
                || double.IsNaN(y) || double.IsInfinity(y))
            {
                throw new ArgumentOutOfRangeException(nameof(x), "Enemy coordinates must be finite.");
            }

            Behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
            if (Behavior.Kind == EnemyBehaviorKind.ChargeBurst
                && attack > int.MaxValue / 2)
            {
                throw new ArgumentOutOfRangeException(nameof(attack));
            }

            Id = id;
            DisplayName = displayName ?? string.Empty;
            X = x;
            Y = y;
            MaxHealth = maxHealth;
            Attack = attack;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public double X { get; }
        public double Y { get; }
        public int MaxHealth { get; }
        public int Attack { get; }
        public EnemyBehaviorDefinition Behavior { get; }
    }
}

using System;

namespace Graphaclysm.Core.Combat
{
    public enum EnemyBehaviorKind
    {
        SteadyAttack,
        ChargeBurst,
        AlternatingPosition
    }

    /// <summary>
    /// Immutable enemy action pattern. It calculates intent without owning battle state.
    /// </summary>
    public sealed class EnemyBehaviorDefinition
    {
        private EnemyBehaviorDefinition(
            EnemyBehaviorKind kind,
            double alternateX,
            double alternateY)
        {
            Kind = kind;
            AlternateX = alternateX;
            AlternateY = alternateY;
        }

        public EnemyBehaviorKind Kind { get; }
        public double AlternateX { get; }
        public double AlternateY { get; }

        public static EnemyBehaviorDefinition SteadyAttack()
        {
            return new EnemyBehaviorDefinition(EnemyBehaviorKind.SteadyAttack, 0.0, 0.0);
        }

        public static EnemyBehaviorDefinition ChargeBurst()
        {
            return new EnemyBehaviorDefinition(EnemyBehaviorKind.ChargeBurst, 0.0, 0.0);
        }

        public static EnemyBehaviorDefinition AlternatingPosition(double alternateX, double alternateY)
        {
            if (!IsFinite(alternateX))
            {
                throw new ArgumentOutOfRangeException(nameof(alternateX),
                    "Alternate coordinates must be finite.");
            }

            if (!IsFinite(alternateY))
            {
                throw new ArgumentOutOfRangeException(nameof(alternateY),
                    "Alternate coordinates must be finite.");
            }

            return new EnemyBehaviorDefinition(
                EnemyBehaviorKind.AlternatingPosition,
                alternateX,
                alternateY);
        }

        public EnemyIntent CreateIntent(
            int turn,
            int baseAttack,
            double primaryX,
            double primaryY)
        {
            if (turn <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(turn));
            }

            switch (Kind)
            {
                case EnemyBehaviorKind.ChargeBurst:
                    return turn % 2 == 1
                        ? EnemyIntent.Charge()
                        : EnemyIntent.Attack(baseAttack * 2);

                case EnemyBehaviorKind.AlternatingPosition:
                    if (turn % 2 == 0)
                    {
                        return EnemyIntent.Attack(baseAttack);
                    }

                    bool moveToAlternate = ((turn - 1) / 2) % 2 == 0;
                    return moveToAlternate
                        ? EnemyIntent.Reposition(AlternateX, AlternateY)
                        : EnemyIntent.Reposition(primaryX, primaryY);

                default:
                    return EnemyIntent.Attack(baseAttack);
            }
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}

using System;

namespace Graphaclysm.Core.Combat
{
    public sealed class EnemyState
    {
        public EnemyState(EnemyDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Restore();
        }

        public EnemyDefinition Definition { get; }
        public int Health { get; private set; }
        public double X { get; private set; }
        public double Y { get; private set; }
        public EnemyIntent Intent { get; private set; }
        public CombatStatusState Statuses { get; } = new CombatStatusState();
        public double AimX { get; private set; }
        public double AimY { get; private set; }
        public double AimRadius => Definition.Behavior.Kind == EnemyBehaviorKind.ChargeBurst ? 1.65 : 0.85;

        public bool IsAlive
        {
            get { return Health > 0; }
        }

        public void TakeDamage(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Health = Math.Max(0, Health - Statuses.AbsorbDamage(amount));
        }

        internal void Restore()
        {
            Health = Definition.MaxHealth;
            X = Definition.X;
            Y = Definition.Y;
            Intent = default(EnemyIntent);
            Statuses.Clear();
        }

        internal void PrepareIntent(int turn)
        {
            Intent = Definition.Behavior.CreateIntent(
                turn,
                Definition.Attack,
                Definition.X,
                Definition.Y);
        }

        internal int ResolveIntent(bool allowReposition = true)
        {
            switch (Intent.Kind)
            {
                case EnemyIntentKind.Reposition:
                    if (!allowReposition || Statuses.Get(CombatStatusKind.Anchor) > 0) return 0;
                    X = Intent.TargetX;
                    Y = Intent.TargetY;
                    return 0;
                case EnemyIntentKind.Attack:
                    return Math.Max(0, Intent.Damage - Statuses.Get(CombatStatusKind.Weaken));
                default:
                    return 0;
            }
        }

        internal void AimAt(double x, double y) { AimX = x; AimY = y; }

        public bool IsAimingAt(double x, double y, double radius)
        {
            double dx = x - AimX;
            double dy = y - AimY;
            double reach = AimRadius + radius;
            return Intent.Kind == EnemyIntentKind.Attack && dx * dx + dy * dy <= reach * reach;
        }
    }
}

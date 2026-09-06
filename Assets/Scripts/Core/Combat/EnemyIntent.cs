namespace Graphaclysm.Core.Combat
{
    public enum EnemyIntentKind
    {
        Attack,
        Charge,
        Reposition
    }

    /// <summary>
    /// Immutable preview of one enemy action. Presentation can read it without changing combat state.
    /// </summary>
    public readonly struct EnemyIntent
    {
        private EnemyIntent(EnemyIntentKind kind, int damage, double targetX, double targetY)
        {
            Kind = kind;
            Damage = damage;
            TargetX = targetX;
            TargetY = targetY;
        }

        public EnemyIntentKind Kind { get; }
        public int Damage { get; }
        public double TargetX { get; }
        public double TargetY { get; }

        public static EnemyIntent Attack(int damage)
        {
            return new EnemyIntent(EnemyIntentKind.Attack, damage, 0.0, 0.0);
        }

        public static EnemyIntent Charge()
        {
            return new EnemyIntent(EnemyIntentKind.Charge, 0, 0.0, 0.0);
        }

        public static EnemyIntent Reposition(double targetX, double targetY)
        {
            return new EnemyIntent(EnemyIntentKind.Reposition, 0, targetX, targetY);
        }
    }
}

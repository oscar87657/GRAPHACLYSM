using System;

namespace Graphaclysm.Core.Combat
{
    public enum EnemyBehaviorKind
    {
        SteadyAttack,
        ChargeBurst,
        AlternatingPosition,
        EscalatingAttack,
        Skirmisher, LineGunner, SealScribe, LinkGuardian, RingSentinel, CrossGunner, TwinMortar,
        RotatingGun, HaloWeaver, RepairPriest, ShieldLeech, AshHunter, HexCaller,
        GlassRegent, ArchiveKeeper, AstralSovereign, GridSentry, LaneArtillerist, LatticeArchitect, EclipseEmperor,
        MirrorDuelist, AshBishop, OrbitDevourer, VoidCartographer, NightConductor
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
        public bool IsBoss => Kind >= EnemyBehaviorKind.GlassRegent && Kind <= EnemyBehaviorKind.AstralSovereign
            || Kind >= EnemyBehaviorKind.LatticeArchitect && Kind <= EnemyBehaviorKind.NightConductor;
        public static EnemyBehaviorDefinition Archive(EnemyBehaviorKind kind)
        {
            if(kind<EnemyBehaviorKind.RotatingGun || kind>EnemyBehaviorKind.NightConductor) throw new ArgumentOutOfRangeException(nameof(kind));
            return new EnemyBehaviorDefinition(kind,7,2);
        }

        public static EnemyBehaviorDefinition LineGunner() => new EnemyBehaviorDefinition(EnemyBehaviorKind.LineGunner, 0, 0);
        public static EnemyBehaviorDefinition SealScribe() => new EnemyBehaviorDefinition(EnemyBehaviorKind.SealScribe, 0, 0);
        public static EnemyBehaviorDefinition LinkGuardian() => new EnemyBehaviorDefinition(EnemyBehaviorKind.LinkGuardian, 0, 0);
        public static EnemyBehaviorDefinition RingSentinel() => new EnemyBehaviorDefinition(EnemyBehaviorKind.RingSentinel, 0, 0);
        public static EnemyBehaviorDefinition CrossGunner() => new EnemyBehaviorDefinition(EnemyBehaviorKind.CrossGunner, 0, 0);
        public static EnemyBehaviorDefinition TwinMortar() => new EnemyBehaviorDefinition(EnemyBehaviorKind.TwinMortar, 0, 0);

        public static EnemyBehaviorDefinition SteadyAttack()
        {
            return new EnemyBehaviorDefinition(EnemyBehaviorKind.SteadyAttack, 0.0, 0.0);
        }

        public static EnemyBehaviorDefinition ChargeBurst()
        {
            return new EnemyBehaviorDefinition(EnemyBehaviorKind.ChargeBurst, 0.0, 0.0);
        }

        public static EnemyBehaviorDefinition EscalatingAttack()
            => new EnemyBehaviorDefinition(EnemyBehaviorKind.EscalatingAttack, 0, 0);

        public static EnemyBehaviorDefinition Skirmisher(double x, double y)
        {
            if (!IsFinite(x) || !IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(x));
            return new EnemyBehaviorDefinition(EnemyBehaviorKind.Skirmisher, x, y);
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
                case EnemyBehaviorKind.GridSentry: case EnemyBehaviorKind.LaneArtillerist:
                    return turn%3==0 ? EnemyIntent.Charge() : EnemyIntent.Attack(baseAttack);
                case EnemyBehaviorKind.LatticeArchitect: case EnemyBehaviorKind.EclipseEmperor:
                case EnemyBehaviorKind.MirrorDuelist: case EnemyBehaviorKind.AshBishop: case EnemyBehaviorKind.OrbitDevourer:
                case EnemyBehaviorKind.VoidCartographer: case EnemyBehaviorKind.NightConductor:
                    return turn%4==0 ? EnemyIntent.Charge() : EnemyIntent.Attack(baseAttack+(turn%4==3?2:0));
                case EnemyBehaviorKind.RepairPriest: case EnemyBehaviorKind.HexCaller:
                    return turn%2==1 ? EnemyIntent.Charge() : EnemyIntent.Attack(baseAttack);
                case EnemyBehaviorKind.AshHunter:
                    return turn%3==1 ? EnemyIntent.Reposition(turn%2==1 ? 7 : 3,2) : EnemyIntent.Attack(baseAttack);
                case EnemyBehaviorKind.GlassRegent: case EnemyBehaviorKind.ArchiveKeeper: case EnemyBehaviorKind.AstralSovereign:
                    return turn%4==0 ? EnemyIntent.Charge() : EnemyIntent.Attack(baseAttack + (turn%4==3 ? 2 : 0));
                case EnemyBehaviorKind.SealScribe:
                    return turn % 2 == 1 ? EnemyIntent.Charge() : EnemyIntent.Attack(baseAttack);
                case EnemyBehaviorKind.EscalatingAttack:
                    return EnemyIntent.Attack(baseAttack + Math.Min(4, (turn - 1) / 2));
                case EnemyBehaviorKind.Skirmisher:
                    if (turn % 3 != 1) return EnemyIntent.Attack(baseAttack);
                    return ((turn - 1) / 3) % 2 == 0
                        ? EnemyIntent.Reposition(AlternateX, AlternateY) : EnemyIntent.Reposition(primaryX, primaryY);
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

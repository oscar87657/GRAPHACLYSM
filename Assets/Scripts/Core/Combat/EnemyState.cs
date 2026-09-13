using System;

namespace Graphaclysm.Core.Combat
{
    public sealed partial class EnemyState
    {
        public EnemyState(EnemyDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Restore();
        }

        public EnemyDefinition Definition { get; }
        public int Health { get; private set; }
        internal int WoundTurn { get; set; } = -1;
        public double X { get; private set; }
        public double Y { get; private set; }
        public EnemyIntent Intent { get; private set; }
        public CombatStatusState Statuses { get; } = new CombatStatusState();
        public double AimX { get; private set; }
        public double AimY { get; private set; }
        public bool IsLineGunner => AttackShape == EnemyBehaviorKind.LineGunner;
        public bool IsRingSentinel => AttackShape == EnemyBehaviorKind.RingSentinel;
        public bool IsCrossGunner => AttackShape == EnemyBehaviorKind.CrossGunner;
        public bool IsTwinMortar => AttackShape == EnemyBehaviorKind.TwinMortar;
        public EnemyBehaviorKind AttackShape { get; private set; }
        public bool IsBoss => Definition.Behavior.IsBoss;
        public bool Enraged { get; private set; }
        public const double RingInnerRadius=1.2, RingOuterRadius=3.2, CrossHalfWidth=.24;
        public double TwinAimX => 10-AimX;
        public double TwinAimY => -AimY;
        public double LineEndX { get; private set; }
        public double LineEndY { get; private set; }
        public double LineStartX { get; private set; }
        public double LineStartY { get; private set; }
        internal void DisplaceTo(double x, double y)
        {
            double dx = x-X, dy = y-Y;
            X = x; Y = y;
            if (!Statuses.Enhanced) return;
            if (IsLineGunner)
            {
                // Keep the announced direction, never re-aim at the player's new position.
                double vx = LineEndX-LineStartX, vy = LineEndY-LineStartY;
                AimAt(X+vx, Y+vy);
            }
            else if (IsRingSentinel) { AimX += dx; AimY += dy; }
        }
        public const double LineHalfWidth = .32;
        public double SealX { get; internal set; }
        public double SealY { get; internal set; }
        public int SealUntilTurn { get; internal set; }
        public bool SealInterrupted { get; internal set; }
        public int GuardTarget { get; internal set; } = -1;
        public bool GuardBroken { get; internal set; }
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
            AttackShape=Definition.Behavior.Kind; Enraged=false;
            FieldPattern=0; FieldPatternOffset=0;
            WoundTurn = -1;
            X = Definition.X;
            Y = Definition.Y;
            Intent = default(EnemyIntent);
            Statuses.Clear();
            SealUntilTurn = 0; SealInterrupted = GuardBroken = false; GuardTarget = -1;
            SealX = SealY = LineEndX = LineEndY = 0;
        }

        internal void PrepareIntent(int turn)
        {
            var kind=Definition.Behavior.Kind;
            Enraged=IsBoss && Health*2<=Definition.MaxHealth;
            int beat=((turn-1)%4+(Enraged?1:0))%3;
            AttackShape=kind;
            if(kind==EnemyBehaviorKind.RotatingGun) AttackShape=turn%2==1?EnemyBehaviorKind.LineGunner:EnemyBehaviorKind.CrossGunner;
            if(kind==EnemyBehaviorKind.HaloWeaver) AttackShape=turn%2==1?EnemyBehaviorKind.RingSentinel:EnemyBehaviorKind.TwinMortar;
            if(kind==EnemyBehaviorKind.HexCaller) AttackShape=EnemyBehaviorKind.CrossGunner;
            if(IsBoss && kind<=EnemyBehaviorKind.AstralSovereign)
            {
                int shape=(beat+(int)kind-(int)EnemyBehaviorKind.GlassRegent)%3;
                AttackShape=shape==0?EnemyBehaviorKind.LineGunner:shape==1?EnemyBehaviorKind.CrossGunner:EnemyBehaviorKind.RingSentinel;
                if(kind==EnemyBehaviorKind.ArchiveKeeper && shape==0) AttackShape=EnemyBehaviorKind.TwinMortar;
            }
            PrepareFieldPattern(turn);
            Intent = Definition.Behavior.CreateIntent(
                turn,
                Definition.Attack+(Enraged?2:0),
                Definition.X,
                Definition.Y);
            if (Statuses.Enhanced && IsAlive)
            {
                if (IsRingSentinel) Statuses.Add(CombatStatusKind.Shield, 3, 1);
                if (Definition.Behavior.Kind == EnemyBehaviorKind.LinkGuardian && turn % 2 == 1)
                    Statuses.Add(CombatStatusKind.Ward, 1, 1);
            }
        }
        internal void Heal(int amount) { if(IsAlive) Health=Math.Min(Definition.MaxHealth,Health+amount); }
        internal void TakePlotDamage(int amount,int pierce)
        {
            int bypass=Math.Min(amount,Math.Min(pierce,Statuses.Get(CombatStatusKind.Shield)));
            Health=Math.Max(0,Health-bypass);
            TakeDamage(amount-bypass);
        }

        internal int ResolveIntent(bool allowReposition = true)
        {
            switch (Intent.Kind)
            {
                case EnemyIntentKind.Reposition:
                    if (!allowReposition || Statuses.Get(CombatStatusKind.Anchor) > 0) return 0;
                    bool moved = Math.Abs(X-Intent.TargetX)+Math.Abs(Y-Intent.TargetY) > .001;
                    DisplaceTo(Intent.TargetX, Intent.TargetY);
                    if (moved) TakeDamage(Statuses.Get(CombatStatusKind.Wound));
                    return 0;
                case EnemyIntentKind.Attack:
                    return Math.Max(0, Intent.Damage - Statuses.Get(CombatStatusKind.Weaken));
                default:
                    return 0;
            }
        }

        internal void AimAt(double x, double y)
        {
            AimX = x; AimY = y; LineStartX = X; LineStartY = Y;
            // Snapshot announced zones. Displacing the enemy does not silently move an existing warning.
            if (IsRingSentinel) { AimX=X; AimY=Y; }
            double dx = x-X, dy = y-Y, length = Math.Sqrt(dx*dx+dy*dy);
            if (length < .00001) { dx = 1; dy = 0; length = 1; }
            dx /= length; dy /= length;
            double distance = 20;
            if (dx > .00001) distance = Math.Min(distance, (10-X)/dx);
            else if (dx < -.00001) distance = Math.Min(distance, -X/dx);
            if (dy > .00001) distance = Math.Min(distance, (4-Y)/dy);
            else if (dy < -.00001) distance = Math.Min(distance, (-4-Y)/dy);
            LineEndX = X+dx*distance; LineEndY = Y+dy*distance;
        }

        public bool IsAimingAt(double x, double y, double radius)
        {
            if (Intent.Kind != EnemyIntentKind.Attack) return false;
            if(FieldPattern!=0) return FieldPatternHits(x,y,radius);
            if (IsCrossGunner) return Math.Abs(x-AimX)<=CrossHalfWidth+radius || Math.Abs(y-AimY)<=CrossHalfWidth+radius;
            if (IsRingSentinel)
            {
                double distance=Math.Sqrt((x-AimX)*(x-AimX)+(y-AimY)*(y-AimY));
                return distance+radius>=RingInnerRadius && distance-radius<=RingOuterRadius;
            }
            if (IsTwinMortar)
            {
                double tx=x-TwinAimX, ty=y-TwinAimY, reachTwin=AimRadius+radius;
                if(tx*tx+ty*ty<=reachTwin*reachTwin) return true;
            }
            if (IsLineGunner)
            {
                double vx = LineEndX-LineStartX, vy = LineEndY-LineStartY, length = vx*vx+vy*vy;
                double t = length < .00001 ? 0 : Math.Max(0, Math.Min(1, ((x-LineStartX)*vx+(y-LineStartY)*vy)/length));
                double px = x-LineStartX-t*vx, py = y-LineStartY-t*vy, width = LineHalfWidth+radius;
                return Intent.Kind == EnemyIntentKind.Attack && px*px+py*py <= width*width;
            }
            double dx = x - AimX;
            double dy = y - AimY;
            double reach = AimRadius + radius;
            return Intent.Kind == EnemyIntentKind.Attack && dx * dx + dy * dy <= reach * reach;
        }
    }
}

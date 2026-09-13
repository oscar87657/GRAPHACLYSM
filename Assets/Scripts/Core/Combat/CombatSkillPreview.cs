using System;

namespace Graphaclysm.Core.Combat
{
    public enum CombatSkillFailure { None, Unavailable, InvalidTarget, CoincidentTarget, NoLanding }

    // Value-only, immediate-use prediction. Commit always resolves again against current state.
    public struct CombatSkillPreview
    {
        public CombatSkillFailure Failure { get; internal set; }
        public bool CanUse => Failure == CombatSkillFailure.None;
        public int TargetIndex { get; internal set; }
        public double OriginX { get; internal set; }
        public double OriginY { get; internal set; }
        public double TargetX { get; internal set; }
        public double TargetY { get; internal set; }
        public double RequestedX { get; internal set; }
        public double RequestedY { get; internal set; }
        public double EndX { get; internal set; }
        public double EndY { get; internal set; }
        public double Distance { get; internal set; }
        public int Style { get; internal set; }
        public int LaneCount { get; internal set; }
        public double Width { get; internal set; }
        public double BurstRadius { get; internal set; }
        public double ChainRadius { get; internal set; }
        public int Damage { get; internal set; }
        public bool ReturnsToOrigin { get; internal set; }
    }

    public readonly struct CombatSkillContact
    {
        public CombatSkillContact(bool hit, bool geometryHit, int healthDamage, int shieldDamage)
        { Hit = hit; GeometryHit = geometryHit; HealthDamage = healthDamage; ShieldDamage = shieldDamage; }
        public bool Hit { get; }
        public bool GeometryHit { get; }
        public int HealthDamage { get; }
        public int ShieldDamage { get; }
    }

    public sealed partial class BattleSession
    {
        public CombatSkillPreview PreviewCombatSkill(int targetIndex)
        {
            var p = new CombatSkillPreview { TargetIndex = targetIndex, Failure = CombatSkillFailure.Unavailable };
            if (!CanUseCombatSkill) return p;
            // Only the explicit legacy quick-target sentinel may select the nearest enemy.
            if (targetIndex == -1) targetIndex = NearestLivingEnemy();
            p.TargetIndex = targetIndex;
            if (targetIndex < 0 || targetIndex >= enemies.Length || !enemies[targetIndex].IsAlive)
            { p.Failure = CombatSkillFailure.InvalidTarget; return p; }
            EnemyState target = enemies[targetIndex];
            p.OriginX = Tactics.X; p.OriginY = Tactics.Y;
            p.TargetX = target.X; p.TargetY = target.Y;
            double dx = target.X - Tactics.X, dy = target.Y - Tactics.Y;
            p.Distance = Math.Sqrt(dx * dx + dy * dy);
            if (p.Distance < .001) { p.Failure = CombatSkillFailure.CoincidentTarget; return p; }
            dx /= p.Distance; dy /= p.Distance;
            int variant = skillLoadout.ActiveVariant;
            bool ian = Tactics.Archetype == CombatArchetype.Ian;
            double travel = Math.Min(2.8, Math.Max(.35,
                p.Distance - TacticalCombatState.PlayerRadius - EnemyHitRadius - .04));
            p.RequestedX = Tactics.X + dx * travel; p.RequestedY = Tactics.Y + dy * travel;
            if (ian && variant == 3)
            { p.RequestedX = target.X + dx * 1.08; p.RequestedY = target.Y + dy * 1.08; }
            else if (!ian && variant != 2)
            { p.RequestedX = target.X - dx * 1.08; p.RequestedY = target.Y - dy * 1.08; }
            if (!FindSkillLanding(p.RequestedX, p.RequestedY, out double x, out double y))
            { p.Failure = CombatSkillFailure.NoLanding; return p; }
            p.EndX = x; p.EndY = y;
            p.Style = ian ? (variant == 1 ? 1 : variant == 3 ? 2 : 0)
                : (variant == 2 ? 3 : variant == 3 ? 4 : 2);
            p.LaneCount = ian && variant == 1 ? (skillLoadout.HasTrait(3) ? 5 : 3) : 1;
            p.Width = p.Style == 1 ? (p.LaneCount == 5 ? 1.65 : 1.15) : .5;
            p.BurstRadius = ian ? (variant == 3 ? (skillLoadout.HasTrait(7) ? 1.85 : 1.15) : 0)
                : (variant == 1 ? (skillLoadout.HasTrait(3) ? 2.2 : 1.7) : variant == 0 ? 1.15 : 0);
            p.ChainRadius = !ian && variant == 3 ? (skillLoadout.HasTrait(7) ? 3.6 : 2.4) : 0;
            p.Damage = ian ? (variant == 1 ? (p.LaneCount == 5 ? 5 : 6) : variant == 2 ? 10 : 7)
                : (variant == 1 ? 5 : variant == 2 ? 9 : variant == 3 ? 5 : 6);
            p.ReturnsToOrigin = !ian && variant == 2;
            if(HasStatusRules) p.Damage=Math.Max(0,p.Damage-Tactics.Statuses.Get(CombatStatusKind.Weaken));
            p.Failure = CombatSkillFailure.None;
            return p;
        }

        public CombatSkillContact PreviewCombatSkillContact(CombatSkillPreview p, int enemyIndex)
        {
            if (!p.CanUse || enemyIndex < 0 || enemyIndex >= enemies.Length || !enemies[enemyIndex].IsAlive)
                return default;
            EnemyState enemy = enemies[enemyIndex];
            double dx = enemy.X - p.TargetX, dy = enemy.Y - p.TargetY;
            bool geometry = p.BurstRadius > 0 ? dx * dx + dy * dy <= Math.Pow(p.BurstRadius + EnemyHitRadius, 2)
                : p.ChainRadius > 0 ? dx * dx + dy * dy <= p.ChainRadius * p.ChainRadius
                : DistanceToSegment(enemy.X, enemy.Y, p.OriginX, p.OriginY, p.EndX, p.EndY) <= p.Width + EnemyHitRadius;
            // Preserve v20's targeted strike, but expose it separately from path contact.
            if(Approach==CombatApproach.Execution&&HasRule(Graphaclysm.Core.Runs.ApproachRule.DashTriple))
            {
                double sx=p.EndX-p.OriginX,sy=p.EndY-p.OriginY,length=Math.Sqrt(sx*sx+sy*sy);geometry=false;
                if(length>.001)for(int lane=-1;lane<=1;lane++)
                {double ox=-sy/length*.8*lane,oy=sx/length*.8*lane;
                    if(DistanceToSegment(enemy.X,enemy.Y,p.OriginX+ox,p.OriginY+oy,p.EndX+ox,p.EndY+oy)<=p.Width+EnemyHitRadius){geometry=true;break;}}
            }
            bool hit = geometry || enemyIndex == p.TargetIndex;
            int damage = p.Damage + ExecutionRupture(enemy);
            int shield = hit ? Math.Min(damage, enemy.Statuses.Get(CombatStatusKind.Shield)) : 0;
            return new CombatSkillContact(hit, geometry, hit ? Math.Min(enemy.Health, damage - shield) : 0, shield);
        }
    }
}

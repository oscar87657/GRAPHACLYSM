using System;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Equations;

namespace Graphaclysm.Core.Combat
{
    public enum CombatArchetype { None, Ian, Luna }

    /// <summary>Player position, buffs and ultimate state. Mutated only by the battle aggregate.</summary>
    public sealed class TacticalCombatState
    {
        public const int UltimateCost = 6;
        public const double MoveDistance = 1.8;
        public const double PlayerRadius = 0.48;
        public CombatArchetype Archetype { get; }
        public int UltimateVariant { get; }
        public int SkillTraitMask { get; }
        public CombatStatusState Statuses { get; } = new CombatStatusState();
        public double X { get; private set; }
        public double Y { get; private set; }
        public int Resonance { get; private set; }
        public bool UltimateArmed { get; private set; }
        public bool HasMoved { get; private set; }
        public bool CanUndoMove { get; private set; }
        private double moveOriginX, moveOriginY;
        private int savedHaste, savedHasteDuration, savedMomentum, savedMomentumDuration;
        private int savedFocus,savedFocusDuration;
        public double HitRadius => !UsesApproachUltimate && UltimateArmed && Archetype == CombatArchetype.Luna
            ? (UltimateVariant == 1 ? (HasTrait(13) ? 1.5 : 1.25) : 1.0) : PlayerRadius;
        public int MoveCost => Statuses.Get(CombatStatusKind.Haste) > 0 ? 0 : 1;
        public double CurrentMoveDistance => MoveDistance + (Statuses.Enhanced && Statuses.Get(CombatStatusKind.Haste) > 0 ? .9 : 0);
        public int AttackBonus => Statuses.Get(CombatStatusKind.Focus)
            - Statuses.Get(CombatStatusKind.Weaken)
            + Statuses.Get(CombatStatusKind.Momentum)
            + (!UsesApproachUltimate && UltimateArmed && Archetype == CombatArchetype.Ian
                ? UltimateVariant == 1 ? 10 + (HasTrait(13) ? 4 : 0) : UltimateVariant == 3 ? 8 : 6 : 0)
            + (!UsesApproachUltimate && UltimateArmed && Archetype == CombatArchetype.Luna
                ? UltimateVariant == 2 ? 4 + (HasTrait(14) ? 4 : 0) : UltimateVariant == 3 ? 2 : 0 : 0);

        internal TacticalCombatState(CombatArchetype archetype, int resonance, int ultimateVariant = 0, int skillTraitMask = 0)
        {
            Archetype = archetype;
            UltimateVariant = Math.Max(0, Math.Min(3, ultimateVariant));
            SkillTraitMask = skillTraitMask;
            Reset(resonance);
        }

        private bool HasTrait(int nodeIndex) => (SkillTraitMask & (1 << nodeIndex)) != 0;

        internal void Reset(int resonance)
        {
            X = 4.0;
            Y = -2.0;
            Resonance = Math.Max(0, Math.Min(UltimateCost, resonance));
            HasMoved = false;
            CanUndoMove = false;
            UltimateArmed = false;
            Statuses.Clear();
        }

        internal bool CanMove(double dx, double dy)
        {
            return CanMoveTo(X + dx, Y + dy);
        }

        internal bool CanMoveTo(double x, double y)
        {
            if (HasMoved || Statuses.Get(CombatStatusKind.Anchor) > 0
                || double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y)) return false;
            double dx = x - X, dy = y - Y;
            double distanceSquared = dx * dx + dy * dy;
            return distanceSquared >= .01 && distanceSquared <= CurrentMoveDistance * CurrentMoveDistance + 1e-8
                && x >= PlayerRadius && x <= 10.0 - PlayerRadius
                && y >= -4.0 + PlayerRadius && y <= 4.0 - PlayerRadius;
        }

        internal void Move(double dx, double dy) => MoveTo(X + dx, Y + dy);

        internal void MoveTo(double x, double y)
        {
            moveOriginX = X; moveOriginY = Y;
            savedHaste = Statuses.Get(CombatStatusKind.Haste); savedHasteDuration = Statuses.Duration(CombatStatusKind.Haste);
            savedMomentum = Statuses.Get(CombatStatusKind.Momentum); savedMomentumDuration = Statuses.Duration(CombatStatusKind.Momentum);
            savedFocus=Statuses.Get(CombatStatusKind.Focus); savedFocusDuration=Statuses.Duration(CombatStatusKind.Focus);
            X = x;
            Y = y;
            HasMoved = true;
            CanUndoMove = true;
            Statuses.Remove(CombatStatusKind.Haste);
        }
        internal void UndoMove()
        {
            X = moveOriginX; Y = moveOriginY; HasMoved = false; CanUndoMove = false;
            Statuses.Remove(CombatStatusKind.Momentum);
            if(Statuses.Enhanced)
            {
                Statuses.Remove(CombatStatusKind.Focus);
                if(savedFocus>0) Statuses.Add(CombatStatusKind.Focus,savedFocus,savedFocusDuration);
            }
            if (savedHaste > 0) Statuses.Add(CombatStatusKind.Haste, savedHaste, savedHasteDuration);
            if (savedMomentum > 0) Statuses.Add(CombatStatusKind.Momentum, savedMomentum, savedMomentumDuration);
            savedHaste = 0; savedHasteDuration = 0; savedMomentum = 0; savedMomentumDuration = 0;
        }

        internal bool UsesApproachUltimate { get; set; }
        internal void LockMoveUndo() { CanUndoMove = false; }
        internal bool ToggleUltimate()
        {
            if (!UltimateArmed && Resonance < UltimateCost) return false;
            UltimateArmed = !UltimateArmed;
            return true;
        }

        internal bool IsHit(EquationState equation)
            => equation.HasBase && EquationAnalyzer.IntersectsCircle(equation, X, Y, HitRadius, equation.CurveSegmentCount);

        internal int BeginSelfHit()
        {
            Statuses.Add(CombatStatusKind.Shield, 3, 1);
            if (!UsesApproachUltimate && UltimateArmed && Archetype == CombatArchetype.Ian && UltimateVariant == 3 && HasTrait(16))
            { Statuses.Cleanse(true); Statuses.Add(CombatStatusKind.Shield, 6, 1); }
            if (UsesApproachUltimate || !UltimateArmed || Archetype != CombatArchetype.Luna) return 0;
            Statuses.Cleanse(true);
            int shield = UltimateVariant == 1 ? 12 : UltimateVariant == 2 ? 6 : 8;
            int healing = UltimateVariant == 1 ? 7 : UltimateVariant == 2 ? 3 : 5;
            if (Archetype == CombatArchetype.Luna && HasTrait(13)) { shield += 4; healing += 2; }
            if (Archetype == CombatArchetype.Luna && HasTrait(15)) { shield += 4; healing += 4; }
            Statuses.Add(CombatStatusKind.Shield, shield, 1);
            return healing;
        }

        internal int ApplySelfInscription(InscriptionKind inscription)
        {
            switch (inscription)
            {
                case InscriptionKind.Ward: Statuses.Add(CombatStatusKind.Shield, 6, 1); break;
                case InscriptionKind.Ember: Statuses.Add(CombatStatusKind.Focus, 2, 2); break;
                case InscriptionKind.Exposure: Statuses.Add(CombatStatusKind.Regeneration, 2, 2); break;
                case InscriptionKind.Mend: return 3;
                case InscriptionKind.Cleanse: Statuses.Cleanse(true); break;
                case InscriptionKind.Phase: Statuses.Add(CombatStatusKind.Haste, 1, 2); break;
            }
            return 0;
        }

        internal static void ApplyEnemyInscription(EnemyState enemy, InscriptionKind inscription)
        {
            switch (inscription)
            {
                case InscriptionKind.Ward: enemy.Statuses.Add(CombatStatusKind.Weaken, 2, 2); break;
                case InscriptionKind.Ember: enemy.Statuses.Add(CombatStatusKind.Burn, 3, 2); break;
                case InscriptionKind.Exposure: enemy.Statuses.Add(CombatStatusKind.Exposure, 3, 2); break;
                case InscriptionKind.Mend: enemy.Statuses.Add(CombatStatusKind.Anchor, 1, 1); break;
                case InscriptionKind.Cleanse: enemy.Statuses.Cleanse(false); break;
                case InscriptionKind.Phase: enemy.Statuses.Add(CombatStatusKind.Anchor, 1, 2); break;
            }
        }

        internal void CompletePlot(bool hitSelf, int enemiesHit)
        {
            bool usedUltimate = UltimateArmed;
            if (usedUltimate) Resonance -= UltimateCost;
            int earned = enemiesHit >= 2 ? 1 : 0;
            if (hitSelf && enemiesHit > 0) earned += 2;
            if (usedUltimate && enemiesHit >= 2 && HasTrait(12)) earned++;
            Resonance = Math.Min(UltimateCost, Resonance + earned);
            UltimateArmed = false;
        }

        internal void GainResonance(int amount)
        { Resonance = Math.Max(0, Math.Min(UltimateCost, Resonance + amount)); }

        internal void SkillDashTo(double x, double y)
        {
            X = Math.Max(PlayerRadius, Math.Min(10 - PlayerRadius, x));
            Y = Math.Max(-4 + PlayerRadius, Math.Min(4 - PlayerRadius, y));
            HasMoved = true;
            CanUndoMove = false;
            Statuses.Remove(CombatStatusKind.Haste);
            savedHaste = 0;
            savedHasteDuration = 0;
            savedMomentum = 0;
            savedMomentumDuration = 0;
        }

        internal static int ApplyAbility(CombatStatusState statuses, CardAbility ability)
        {
            if (ability.Kind == CardAbilityKind.Heal) return ability.Magnitude;
            if (ability.Kind == CardAbilityKind.Cleanse)
                statuses.Cleanse(ability.Target == AbilityTarget.Player);
            else if (ability.Kind != CardAbilityKind.Pull)
                statuses.Add((CombatStatusKind)((int)ability.Kind >= 14 ? (int)ability.Kind - 2 : (int)ability.Kind), ability.Magnitude, ability.Duration);
            return 0;
        }

        internal void BeginNextTurn() { HasMoved = false; CanUndoMove = false; }
    }
}

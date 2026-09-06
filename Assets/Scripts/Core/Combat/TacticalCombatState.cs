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
        public const double MoveDistance = 1.5;
        public const double PlayerRadius = 0.48;
        public CombatArchetype Archetype { get; }
        public CombatStatusState Statuses { get; } = new CombatStatusState();
        public double X { get; private set; }
        public double Y { get; private set; }
        public int Resonance { get; private set; }
        public bool UltimateArmed { get; private set; }
        public bool HasMoved { get; private set; }
        private double moveOriginX, moveOriginY;
        private int savedHaste, savedHasteDuration;
        public double HitRadius => UltimateArmed && Archetype == CombatArchetype.Luna ? 1.0 : PlayerRadius;
        public int MoveCost => Statuses.Get(CombatStatusKind.Haste) > 0 ? 0 : 1;
        public int AttackBonus => Statuses.Get(CombatStatusKind.Focus)
            - Statuses.Get(CombatStatusKind.Weaken)
            + (UltimateArmed && Archetype == CombatArchetype.Ian ? 6 : 0);

        internal TacticalCombatState(CombatArchetype archetype, int resonance)
        {
            Archetype = archetype;
            Reset(resonance);
        }

        internal void Reset(int resonance)
        {
            X = 4.0;
            Y = -2.0;
            Resonance = Math.Max(0, Math.Min(UltimateCost, resonance));
            HasMoved = false;
            UltimateArmed = false;
            Statuses.Clear();
        }

        internal bool CanMove(double dx, double dy)
        {
            if (HasMoved || Statuses.Get(CombatStatusKind.Anchor) > 0) return false;
            bool cardinal = (Math.Abs(dx) == MoveDistance && dy == 0.0)
                || (Math.Abs(dy) == MoveDistance && dx == 0.0);
            return cardinal && X + dx >= PlayerRadius && X + dx <= 10.0 - PlayerRadius
                && Y + dy >= -4.0 + PlayerRadius && Y + dy <= 4.0 - PlayerRadius;
        }

        internal void Move(double dx, double dy)
        {
            moveOriginX = X; moveOriginY = Y;
            savedHaste = Statuses.Get(CombatStatusKind.Haste); savedHasteDuration = Statuses.Duration(CombatStatusKind.Haste);
            X += dx;
            Y += dy;
            HasMoved = true;
            Statuses.Remove(CombatStatusKind.Haste);
        }
        internal void UndoMove()
        {
            X = moveOriginX; Y = moveOriginY; HasMoved = false;
            if (savedHaste > 0) Statuses.Add(CombatStatusKind.Haste, savedHaste, savedHasteDuration);
            savedHaste = 0; savedHasteDuration = 0;
        }

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
            if (!UltimateArmed || Archetype != CombatArchetype.Luna) return 0;
            Statuses.Cleanse(true);
            Statuses.Add(CombatStatusKind.Shield, 8, 1);
            return 5;
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
            if (UltimateArmed) Resonance -= UltimateCost;
            Resonance = Math.Min(UltimateCost, Resonance + 1 + (hitSelf && enemiesHit > 0 ? 1 : 0));
            UltimateArmed = false;
        }

        internal static int ApplyAbility(CombatStatusState statuses, CardAbility ability)
        {
            if (ability.Kind == CardAbilityKind.Heal) return ability.Magnitude;
            if (ability.Kind == CardAbilityKind.Cleanse)
                statuses.Cleanse(ability.Target == AbilityTarget.Player);
            else statuses.Add((CombatStatusKind)ability.Kind, ability.Magnitude, ability.Duration);
            return 0;
        }

        internal void BeginNextTurn() { HasMoved = false; }
    }
}

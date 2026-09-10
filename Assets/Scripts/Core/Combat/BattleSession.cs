using System;
using System.Collections.Generic;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Equations;
using Graphaclysm.Core.Relics;
using Graphaclysm.Core.Runs;

namespace Graphaclysm.Core.Combat
{
    public enum BattlePhase
    {
        PlayerPlanning,
        Plotting,
        EnemyTurn,
        Victory,
        Defeat
    }

    public enum CardPlayFailure
    {
        None,
        WrongPhase,
        MissingBaseFunction,
        BaseFunctionAlreadySelected,
        NotEnoughEnergy,
        DuplicateOrFull,
        InscriptionLimit
    }

    public readonly struct PlotReport
    {
        public PlotReport(int hitCount, int totalDamage, bool playerHit = false, int healing = 0, int shield = 0)
        {
            HitCount = hitCount;
            TotalDamage = totalDamage;
            PlayerHit = playerHit;
            Healing = healing;
            Shield = shield;
        }

        public int HitCount { get; }
        public int TotalDamage { get; }
        public bool PlayerHit { get; }
        public int Healing { get; }
        public int Shield { get; }
    }

    /// <summary>
    /// Authoritative battle state machine. It contains no Unity lifecycle or presentation code.
    /// </summary>
    public sealed class BattleSession
    {
        public const double EnemyHitRadius = 0.48;
        public const double MaximumTraceLength = 64;
        public const int MaximumPlayedCards = EquationState.MaximumModifiers + 3;
        public const int PrismDamageBonus = 2;
        public const int CombatSkillCooldownTurns = 3;

        private readonly BattleDefinition definition;
        private readonly EnemyState[] enemies;
        private readonly int playerMaxEnergy;
        private readonly int plotDamageBonus;
        private readonly CardDefinition[] playedCards =
            new CardDefinition[MaximumPlayedCards];

        private int playedCardCount;
        private bool preserveFragments;
        public bool UsesFragments => definition.UsesFragments;
        public int CondenseCount { get; private set; }
        public int SealedCardCount { get; private set; }
        public int CondenseHealthCost => (CondenseCount + 1) * 2;
        public bool CanCondense => UsesFragments && Phase == BattlePhase.PlayerPlanning && CondenseCount < 2 && PlayerHealth > CondenseHealthCost;
        public int PendingDrawBonus { get { int n = 0; for(int i=SealedCardCount;i<playedCardCount;i++) n += playedCards[i].DrawBonus; return n; } }
        public int WeaveDamageBonus { get { if(!UsesFragments) return 0; int n=0; for(int i=0;i<playedCardCount;i++) n+=FragmentCardCatalog.Power(playedCards[i].Fragment); return Math.Min(10,n); } }
        public bool TryCondense()
        {
            if (!CanCondense) return false;
            PlayerHealth -= CondenseHealthCost;
            if (condenseShield > 0) Tactics?.Statuses.Add(CombatStatusKind.Shield, condenseShield, 1);
            preserveFragments = true; CondenseCount++; SealedCardCount = playedCardCount;
            Phase = BattlePhase.EnemyTurn; return true;
        }
        public bool TryUnravel()
        { if (!UsesFragments || Phase != BattlePhase.PlayerPlanning) return false; preserveFragments = false; CondenseCount = 0; ResetEquationHistory(); Phase = BattlePhase.EnemyTurn; return true; }
        private int movementEnergySpent;
        private readonly int startingResonance;
        private readonly int startShield, selfShield, firstPlotDamage, shortWeaveDamage, longWeaveDamage, movedPlotShield;
        private readonly int condenseShield, startThorns, prismDamage, moveMomentum, longWeaveRupture, startFortify;
        private readonly BattleSkillLoadout skillLoadout;
        private int resolvedPlots;
        public int CombatSkillCooldown { get; private set; }
        public double LastSkillOriginX { get; private set; }
        public double LastSkillOriginY { get; private set; }
        public double LastSkillEndX { get; private set; }
        public double LastSkillEndY { get; private set; }
        public int LastSkillHitCount { get; private set; }
        public int LastSkillDamage { get; private set; }
        public bool LastSkillWide { get; private set; }
        public int LastSkillLaneCount { get; private set; }
        public int LastSkillStyle { get; private set; }
        public bool LastSkillCooldownReset { get; private set; }
        public bool CanUseCombatSkill => Phase == BattlePhase.PlayerPlanning && Tactics != null
            && CombatSkillCooldown == 0 && HasLivingEnemy();

        public BattleSession(BattleDefinition definition)
            : this(definition, definition == null ? 0 : definition.PlayerMaxHealth, 0, 0)
        {
        }

        public BattleSession(BattleDefinition definition, int startingHealth)
            : this(definition, startingHealth, 0, 0)
        {
        }

        public BattleSession(
            BattleDefinition definition,
            int startingHealth,
            int energyBonus,
            int plotDamageBonus,
            int startingResonance = 0, RunRelicCollection relics = null,
            BattleSkillLoadout skillLoadout = default(BattleSkillLoadout), int startingShieldBonus = 0)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (energyBonus < 0 || definition.PlayerMaxEnergy > int.MaxValue - energyBonus)
            {
                throw new ArgumentOutOfRangeException(nameof(energyBonus));
            }

            if (plotDamageBonus < 0 || plotDamageBonus > int.MaxValue - 16)
            {
                throw new ArgumentOutOfRangeException(nameof(plotDamageBonus));
            }

            playerMaxEnergy = definition.PlayerMaxEnergy + energyBonus;
            this.plotDamageBonus = plotDamageBonus;
            this.skillLoadout = skillLoadout;
            this.startingResonance = Math.Min(6, startingResonance + (relics?.GetTotalMagnitude(RelicEffectKind.StartResonance) ?? 0));
            startShield = (relics?.GetTotalMagnitude(RelicEffectKind.StartShield) ?? 0) + Math.Max(0, startingShieldBonus);
            selfShield = relics?.GetTotalMagnitude(RelicEffectKind.SelfShield) ?? 0;
            firstPlotDamage = relics?.GetTotalMagnitude(RelicEffectKind.FirstPlotDamage) ?? 0;
            shortWeaveDamage = relics?.GetTotalMagnitude(RelicEffectKind.ShortWeaveDamage) ?? 0;
            longWeaveDamage = relics?.GetTotalMagnitude(RelicEffectKind.LongWeaveDamage) ?? 0;
            movedPlotShield = relics?.GetTotalMagnitude(RelicEffectKind.MovedPlotShield) ?? 0;
            condenseShield = relics?.GetTotalMagnitude(RelicEffectKind.CondenseShield) ?? 0;
            startThorns = relics?.GetTotalMagnitude(RelicEffectKind.StartThorns) ?? 0;
            prismDamage = relics?.GetTotalMagnitude(RelicEffectKind.PrismDamage) ?? 0;
            moveMomentum = relics?.GetTotalMagnitude(RelicEffectKind.MoveMomentum) ?? 0;
            longWeaveRupture = relics?.GetTotalMagnitude(RelicEffectKind.LongWeaveRupture) ?? 0;
            startFortify = relics?.GetTotalMagnitude(RelicEffectKind.StartFortify) ?? 0;
            if (definition.Archetype != CombatArchetype.None)
                Tactics = new TacticalCombatState(definition.Archetype,
                    this.startingResonance,
                    skillLoadout.UltimateVariant,
                    skillLoadout.TraitMask);
            enemies = new EnemyState[definition.EnemyCount];

            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i] = new EnemyState(definition.GetEnemy(i));
            }

            Equation = new EquationState();
            Reset(startingHealth);
        }

        public EquationState Equation { get; }
        public TacticalCombatState Tactics { get; }
        public bool PreviewPlayerHit => Tactics != null && Tactics.IsHit(Equation);
        public int TerrainCount => definition.TerrainCount;
        public bool PrismCharged
        {
            get
            {
                if (!Equation.HasBase) return false;
                for (int i = 0; i < definition.TerrainCount; i++)
                {
                    BattleTerrainDefinition item = definition.GetTerrain(i);
                    if (item.Kind == BattleTerrainKind.Prism
                        && EquationAnalyzer.IntersectsCircle(Equation, item.X, item.Y, item.Radius, Equation.CurveSegmentCount))
                        return true;
                }
                return false;
            }
        }
        public bool CanPlot => Phase == BattlePhase.PlayerPlanning && Equation.HasBase
            && (!Equation.IsCalculator || (Equation.TraceLength >= 0.05 && Equation.TraceLength <= MaximumTraceLength));
        public IReadOnlyList<EnemyState> Enemies
        {
            get { return enemies; }
        }

        public BattlePhase Phase { get; private set; }
        public int PlayerHealth { get; private set; }
        public int Energy { get; private set; }
        public int Turn { get; private set; }
        public int PlayedCardCount
        {
            get { return playedCardCount; }
        }
        public int PlayerMaxHealth
        {
            get { return definition.PlayerMaxHealth; }
        }

        public int PlayerMaxEnergy
        {
            get { return playerMaxEnergy; }
        }

        public int PlotDamageBonus
        {
            get { return plotDamageBonus; }
        }

        public BattleTerrainDefinition GetTerrain(int index) => definition.GetTerrain(index);

        public void Reset()
        {
            Reset(definition.PlayerMaxHealth);
        }

        public void Reset(int startingHealth)
        {
            if (startingHealth <= 0 || startingHealth > definition.PlayerMaxHealth)
            {
                throw new ArgumentOutOfRangeException(nameof(startingHealth));
            }

            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i].Restore();
            }

            PlayerHealth = startingHealth;
            Energy = playerMaxEnergy;
            Turn = 1; resolvedPlots = 0; CondenseCount = 0; preserveFragments = false; CombatSkillCooldown = 0;
            LastSkillHitCount = 0; LastSkillDamage = 0; LastSkillWide = false; LastSkillLaneCount = 1;
            LastSkillStyle = 0; LastSkillCooldownReset = false;
            movementEnergySpent = 0;
            Tactics?.Reset(startingResonance);
            if(startShield>0) Tactics?.Statuses.Add(CombatStatusKind.Shield,startShield,1);
            if(startThorns>0) Tactics?.Statuses.Add(CombatStatusKind.Thorns,startThorns,3);
            if(startFortify>0) Tactics?.Statuses.Add(CombatStatusKind.Fortify,startFortify,2);
            ResetEquationHistory();
            if (definition.UsesCalculator) Equation.EnableCalculator(true);
            PrepareEnemyIntents();
            Phase = BattlePhase.PlayerPlanning;
        }

        public CardDefinition GetPlayedCard(int index)
        {
            if (index < 0 || index >= playedCardCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return playedCards[index];
        }

        public bool TryPlayCard(CardDefinition card, out CardPlayFailure failure)
        {
            if (!CanPlayCard(card, out failure)) return false;
            if (card.IsFragment) Equation.TryAppendFragment(card.Fragment, Tactics?.X ?? 5, Tactics?.Y ?? 0);
            else if (card.IsBaseFunction) Equation.SetBase(card.BaseFunction);
            else if (!card.IsInscription) Equation.TryAddModifier(card.Modifier);
            Energy -= card.Cost;
            RecordPlayedCard(card);
            return true;
        }

        public bool CanPlayCard(CardDefinition card, out CardPlayFailure failure)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (Phase != BattlePhase.PlayerPlanning)
            {
                failure = CardPlayFailure.WrongPhase;
                return false;
            }

            if (playedCardCount >= MaximumPlayedCards)
            {
                failure = CardPlayFailure.DuplicateOrFull;
                return false;
            }

            if (UsesFragments)
            {
                failure = card.IsFragment && Equation.Fragments.CanAppend(card.Fragment) ? CardPlayFailure.None : CardPlayFailure.DuplicateOrFull;
                return failure == CardPlayFailure.None;
            }
            if (card.IsBaseFunction)
            {
                if (Equation.HasBase)
                {
                    failure = CardPlayFailure.BaseFunctionAlreadySelected;
                    return false;
                }

                if (Energy < card.Cost)
                {
                    failure = CardPlayFailure.NotEnoughEnergy;
                    return false;
                }

                failure = CardPlayFailure.None;
                return true;
            }

            if (!Equation.HasBase)
            {
                failure = CardPlayFailure.MissingBaseFunction;
                return false;
            }

            if (Energy < card.Cost)
            {
                failure = CardPlayFailure.NotEnoughEnergy;
                return false;
            }

            if (card.IsInscription)
            {
                int count = 0;
                for (int i = 0; i < playedCardCount; i++)
                {
                    if (!playedCards[i].IsInscription) continue;
                    count++;
                    if (playedCards[i].Inscription == card.Inscription)
                    {
                        failure = CardPlayFailure.DuplicateOrFull;
                        return false;
                    }
                }
                failure = count >= 2 ? CardPlayFailure.InscriptionLimit : CardPlayFailure.None;
                return count < 2;
            }

            if (!Equation.CanAddModifier(card.Modifier))
            {
                failure = CardPlayFailure.DuplicateOrFull;
                return false;
            }

            failure = CardPlayFailure.None;
            return true;
        }

        public bool TryUndoLastCard(out CardDefinition card)
        {
            if (Phase != BattlePhase.PlayerPlanning || playedCardCount == 0 || (UsesFragments && playedCardCount <= SealedCardCount))
            {
                card = null;
                return false;
            }

            if (!playedCards[playedCardCount - 1].IsInscription && !Equation.TryRemoveLastStep())
            {
                throw new InvalidOperationException("Equation history is out of sync with played cards.");
            }

            playedCardCount--;
            card = playedCards[playedCardCount];
            playedCards[playedCardCount] = null;
            Energy = Math.Min(playerMaxEnergy, Energy + card.Cost);
            return true;
        }

        public void ClearEquation()
        {
            if (Phase != BattlePhase.PlayerPlanning)
            {
                return;
            }

            ResetEquationHistory();
            Energy = playerMaxEnergy - movementEnergySpent;
        }

        public bool TryMovePlayer(double dx, double dy)
            => Tactics != null && TryMovePlayerTo(Tactics.X + dx, Tactics.Y + dy);

        public bool TryResolveMoveDestination(double requestedX, double requestedY, out double resolvedX, out double resolvedY)
        {
            resolvedX = Tactics == null ? 0 : Tactics.X;
            resolvedY = Tactics == null ? 0 : Tactics.Y;
            if (Phase != BattlePhase.PlayerPlanning || Tactics == null
                || (!UsesFragments && Energy < Tactics.MoveCost)
                || double.IsNaN(requestedX) || double.IsInfinity(requestedX)
                || double.IsNaN(requestedY) || double.IsInfinity(requestedY)) return false;

            double dx = requestedX - Tactics.X, dy = requestedY - Tactics.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance < .1) return false;
            if (distance > TacticalCombatState.MoveDistance)
            {
                double scale = TacticalCombatState.MoveDistance / distance;
                dx *= scale; dy *= scale; distance = TacticalCombatState.MoveDistance;
            }
            double candidateX = Math.Max(TacticalCombatState.PlayerRadius,
                Math.Min(10 - TacticalCombatState.PlayerRadius, Tactics.X + dx));
            double candidateY = Math.Max(-4 + TacticalCombatState.PlayerRadius,
                Math.Min(4 - TacticalCombatState.PlayerRadius, Tactics.Y + dy));
            if (Tactics.CanMoveTo(candidateX, candidateY) && !PositionBlocked(candidateX, candidateY))
            { resolvedX = candidateX; resolvedY = candidateY; return true; }

            // Landing on a marker should not discard the input. Search a small fan around the
            // requested point and return the closest legal landing location.
            double heading = Math.Atan2(dy, dx), best = double.MaxValue;
            for (int ring = 0; ring < 5; ring++)
            {
                double radius = distance * (1.0 - ring * .14);
                for (int step = 0; step <= 12; step++)
                {
                    int signedStep = step == 0 ? 0 : (step + 1) / 2 * (step % 2 == 1 ? 1 : -1);
                    double angle = heading + signedStep * Math.PI / 18.0;
                    double x = Math.Max(TacticalCombatState.PlayerRadius,
                        Math.Min(10 - TacticalCombatState.PlayerRadius, Tactics.X + Math.Cos(angle) * radius));
                    double y = Math.Max(-4 + TacticalCombatState.PlayerRadius,
                        Math.Min(4 - TacticalCombatState.PlayerRadius, Tactics.Y + Math.Sin(angle) * radius));
                    if (!Tactics.CanMoveTo(x, y) || PositionBlocked(x, y)) continue;
                    double errorX = x - requestedX, errorY = y - requestedY;
                    double error = errorX * errorX + errorY * errorY;
                    if (error < best) { best = error; resolvedX = x; resolvedY = y; }
                }
            }
            return best < double.MaxValue;
        }

        public bool TryMovePlayerTo(double requestedX, double requestedY)
        {
            if (!TryResolveMoveDestination(requestedX, requestedY, out double x, out double y)) return false;
            movementEnergySpent = UsesFragments ? 0 : Tactics.MoveCost;
            Energy -= movementEnergySpent;
            Tactics.MoveTo(x, y);
            if (moveMomentum > 0) Tactics.Statuses.Add(CombatStatusKind.Momentum, moveMomentum, 2);
            return true;
        }

        public bool TryUndoMove()
        {
            if (Phase != BattlePhase.PlayerPlanning || Tactics == null || !Tactics.CanUndoMove) return false;
            Tactics.UndoMove(); Energy = Math.Min(playerMaxEnergy, Energy + movementEnergySpent); movementEnergySpent = 0;
            return true;
        }
        public bool TrySetCalculator(string x, string y, out string error)
        {
            error = "작도 전에 수식을 바꿀 수 있습니다.";
            return Phase == BattlePhase.PlayerPlanning && Equation.TrySetCalculator(x, y, out error);
        }
        public bool TrySelectFormulaAxis(int axis) => Phase == BattlePhase.PlayerPlanning && Equation.TrySelectAxis(axis);

        public bool TryToggleUltimate()
            => Phase == BattlePhase.PlayerPlanning && Tactics != null && Tactics.ToggleUltimate();

        public bool TryUseCombatSkill(int targetIndex = -1)
        {
            if (!CanUseCombatSkill) return false;
            if (targetIndex < 0 || targetIndex >= enemies.Length || !enemies[targetIndex].IsAlive)
                targetIndex = NearestLivingEnemy();
            if (targetIndex < 0) return false;

            LastSkillOriginX = Tactics.X; LastSkillOriginY = Tactics.Y;
            EnemyState target = enemies[targetIndex];
            double dx = target.X - Tactics.X, dy = target.Y - Tactics.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance < .001) return false;
            dx /= distance; dy /= distance;

            int variant = skillLoadout.ActiveVariant;
            bool ian = Tactics.Archetype == CombatArchetype.Ian;
            double travel = Math.Min(2.8, Math.Max(.35,
                distance - TacticalCombatState.PlayerRadius - EnemyHitRadius - .04));
            double desiredX = Tactics.X + dx * travel, desiredY = Tactics.Y + dy * travel;
            if (ian && variant == 3)
            { desiredX = target.X + dx * 1.08; desiredY = target.Y + dy * 1.08; }
            else if (!ian && variant != 2)
            { desiredX = target.X - dx * 1.08; desiredY = target.Y - dy * 1.08; }
            FindSkillLanding(desiredX, desiredY, out double endX, out double endY);

            LastSkillStyle = ian ? (variant == 1 ? 1 : variant == 3 ? 2 : 0)
                : (variant == 2 ? 3 : variant == 3 ? 4 : 2);
            LastSkillLaneCount = ian && variant == 1 ? (skillLoadout.HasTrait(3) ? 5 : 3) : 1;
            LastSkillWide = LastSkillStyle == 1 || LastSkillStyle == 2;
            double width = LastSkillStyle == 1 ? (LastSkillLaneCount == 5 ? 1.65 : 1.15) : .5;
            double burstRadius = ian ? (variant == 3 ? (skillLoadout.HasTrait(7) ? 1.85 : 1.15) : 0)
                : (variant == 1 ? (skillLoadout.HasTrait(3) ? 2.2 : 1.7) : variant == 0 ? 1.15 : 0);
            double chainRadius = !ian && variant == 3 ? (skillLoadout.HasTrait(7) ? 3.6 : 2.4) : 0;
            int damage = ian
                ? (variant == 1 ? (LastSkillLaneCount == 5 ? 5 : 6) : variant == 2 ? 10 : variant == 3 ? 7 : 7)
                : (variant == 1 ? 5 : variant == 2 ? 9 : variant == 3 ? 5 : 6);
            int hitCount = 0, dealt = 0; bool killed = false;
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyState enemy = enemies[i];
                if (!enemy.IsAlive) continue;
                bool hit = i == targetIndex;
                if (!hit && burstRadius > 0)
                { double ex = enemy.X - target.X, ey = enemy.Y - target.Y; hit = ex * ex + ey * ey <= (burstRadius + EnemyHitRadius) * (burstRadius + EnemyHitRadius); }
                else if (!hit && chainRadius > 0)
                { double ex = enemy.X - target.X, ey = enemy.Y - target.Y; hit = ex * ex + ey * ey <= chainRadius * chainRadius; }
                else if (!hit && burstRadius <= 0 && chainRadius <= 0)
                    hit = DistanceToSegment(enemy.X, enemy.Y, LastSkillOriginX, LastSkillOriginY, endX, endY) <= width + EnemyHitRadius;
                if (!hit) continue;
                int before = enemy.Health;
                enemy.TakeDamage(damage);
                dealt += before - enemy.Health; hitCount++;
                if (enemy.Health == 0) killed = true;
                if (ian && variant == 2 && skillLoadout.HasTrait(6))
                    enemy.Statuses.Add(CombatStatusKind.Rupture, 3, 2);
                if (!ian && variant == 2 && skillLoadout.HasTrait(5))
                    enemy.Statuses.Add(CombatStatusKind.Exposure, 3, 2);
            }
            LastSkillEndX = endX; LastSkillEndY = endY; LastSkillHitCount = hitCount; LastSkillDamage = dealt;
            if (ian || variant != 2) Tactics.SkillDashTo(endX, endY);
            if (ian)
            {
                if (variant == 1 && skillLoadout.HasTrait(4))
                    Tactics.Statuses.Add(CombatStatusKind.Shield, 3 + hitCount * 2, 2);
                if (variant == 3 && skillLoadout.HasTrait(8))
                { Tactics.Statuses.Add(CombatStatusKind.Shield, 6, 2); Tactics.Statuses.Add(CombatStatusKind.Fortify, 3, 2); }
            }
            else
            {
                if (variant == 1 && skillLoadout.HasTrait(4))
                { Tactics.Statuses.Cleanse(true); Tactics.Statuses.Add(CombatStatusKind.Shield, 4, 2); Tactics.Statuses.Add(CombatStatusKind.Fortify, 3, 2); }
                else if (variant == 2 && skillLoadout.HasTrait(6) && hitCount > 0)
                { PlayerHealth = Math.Min(PlayerMaxHealth, PlayerHealth + 3); Tactics.Statuses.Add(CombatStatusKind.Momentum, 3, 2); }
            }
            LastSkillCooldownReset = (ian && variant == 2 && killed)
                || (!ian && variant == 3 && skillLoadout.HasTrait(8) && killed);
            if (LastSkillCooldownReset && ((ian && skillLoadout.HasTrait(5)) || !ian)) Tactics.GainResonance(1);
            CombatSkillCooldown = LastSkillCooldownReset ? 0 : CombatSkillCooldownTurns;
            movementEnergySpent = 0;
            if (AreAllEnemiesDefeated()) Phase = BattlePhase.Victory;
            return true;
        }

        public bool TryBeginPlot()
        {
            if (!CanPlot)
            {
                return false;
            }

            preserveFragments = false; CondenseCount = 0;
            Phase = BattlePhase.Plotting;
            return true;
        }

        public PlotReport ResolvePlot()
        {
            if (Phase != BattlePhase.Plotting)
            {
                throw new InvalidOperationException("Plot resolution is only valid during the Plotting phase.");
            }

            int hitCount = 0;
            int totalDamage = 0;
            bool playerHit = PreviewPlayerHit;
            int healing = 0;
            int shieldBefore = Tactics == null ? 0 : Tactics.Statuses.Get(CombatStatusKind.Shield);
            bool resetSkillFromUltimate = Tactics != null && Tactics.UltimateArmed
                && Tactics.Archetype == CombatArchetype.Luna && Tactics.UltimateVariant == 3
                && skillLoadout.HasTrait(16);

            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyState enemy = enemies[i];
                if (!enemy.IsAlive)
                {
                    continue;
                }

                int damage = PreviewDamage(enemy);

                if (damage <= 0)
                {
                    continue;
                }

                enemy.TakeDamage(damage);
                enemy.Statuses.Remove(CombatStatusKind.Rupture);
                for (int cardIndex = 0; cardIndex < playedCardCount; cardIndex++)
                {
                    if (playedCards[cardIndex].IsInscription)
                        TacticalCombatState.ApplyEnemyInscription(enemy, playedCards[cardIndex].Inscription);
                    ApplyBundledAbilities(playedCards[cardIndex], AbilityTarget.Enemy, enemy.Statuses);
                }
                if (Tactics != null && Tactics.UltimateArmed && Tactics.Archetype == CombatArchetype.Ian)
                {
                    if (Tactics.UltimateVariant == 1) enemy.Statuses.Add(CombatStatusKind.Rupture, 3, 2);
                    else if (Tactics.UltimateVariant == 2)
                        enemy.Statuses.Add(CombatStatusKind.Anchor, 2 + (skillLoadout.HasTrait(14) ? 1 : 0), 1);
                    else if (Tactics.UltimateVariant == 3)
                    {
                        enemy.Statuses.Cleanse(false);
                        enemy.Statuses.Add(CombatStatusKind.Weaken, skillLoadout.HasTrait(17) ? 3 : 2, 2);
                        if (skillLoadout.HasTrait(17)) enemy.Statuses.Add(CombatStatusKind.Rupture, 2, 2);
                    }
                    else enemy.Statuses.Add(CombatStatusKind.Anchor, 1, 1);
                }
                else if (Tactics != null && Tactics.UltimateArmed && Tactics.Archetype == CombatArchetype.Luna
                    && Tactics.UltimateVariant == 3)
                {
                    enemy.Statuses.Add(CombatStatusKind.Anchor, 2, 1);
                    if (skillLoadout.HasTrait(17)) enemy.Statuses.Add(CombatStatusKind.Weaken, 3, 2);
                }
                if (UsesFragments && playedCardCount >= 6 && longWeaveRupture > 0)
                    enemy.Statuses.Add(CombatStatusKind.Rupture, longWeaveRupture, 2);
                hitCount++;
                totalDamage += damage;
            }

            if (Tactics != null)
            {
                Tactics.Statuses.Remove(CombatStatusKind.Focus);
                Tactics.Statuses.Remove(CombatStatusKind.Momentum);
                if (playerHit)
                {
                    healing = Tactics.BeginSelfHit();
                    if(selfShield>0) Tactics.Statuses.Add(CombatStatusKind.Shield,selfShield,1);
                    for (int i = 0; i < playedCardCount; i++)
                    {
                        if (playedCards[i].IsInscription)
                            healing += Tactics.ApplySelfInscription(playedCards[i].Inscription);
                        healing += ApplyBundledAbilities(playedCards[i], AbilityTarget.Player, Tactics.Statuses);
                    }
                    healing = Math.Min(healing, PlayerMaxHealth - PlayerHealth);
                    PlayerHealth += healing;
                }
                if (playerHit && Tactics.UltimateArmed && Tactics.Archetype == CombatArchetype.Ian
                    && Tactics.UltimateVariant == 2)
                {
                    int amount = skillLoadout.HasTrait(15) ? 8 : 4;
                    int ultimateHealing = Math.Min(amount, PlayerMaxHealth - PlayerHealth);
                    PlayerHealth += ultimateHealing;
                    healing += ultimateHealing;
                    if (skillLoadout.HasTrait(15)) Tactics.Statuses.Add(CombatStatusKind.Shield, 4, 1);
                }
                if(Tactics.HasMoved && movedPlotShield>0) Tactics.Statuses.Add(CombatStatusKind.Shield,movedPlotShield,1);
                Tactics.CompletePlot(playerHit, hitCount);
                if (resetSkillFromUltimate) CombatSkillCooldown = 0;
            }

            resolvedPlots++;
            Phase = AreAllEnemiesDefeated() ? BattlePhase.Victory : BattlePhase.EnemyTurn;
            return new PlotReport(hitCount, totalDamage, playerHit, healing,
                Tactics == null ? 0 : Tactics.Statuses.Get(CombatStatusKind.Shield) - shieldBefore);
        }

        public int ResolveEnemyTurn()
        {
            if (Phase != BattlePhase.EnemyTurn)
            {
                throw new InvalidOperationException("Enemy turn resolution is only valid during the EnemyTurn phase.");
            }

            int incomingDamage = 0;
            if (Tactics != null && Tactics.Statuses.Get(CombatStatusKind.Fortify) > 0)
                Tactics.Statuses.Add(CombatStatusKind.Shield, Tactics.Statuses.Get(CombatStatusKind.Fortify), 1);
            for (int i = 0; i < enemies.Length; i++)
                if (enemies[i].IsAlive) enemies[i].TakeDamage(enemies[i].Statuses.Get(CombatStatusKind.Burn));
            if (Tactics != null)
            {
                int burn = Tactics.Statuses.AbsorbDamage(Tactics.Statuses.Get(CombatStatusKind.Burn));
                PlayerHealth = Math.Max(0, PlayerHealth - burn);
                incomingDamage += burn;
            }
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyState enemy = enemies[i];
                if (enemy.IsAlive && PlayerHealth > 0)
                {
                    bool allowReposition = enemy.Intent.Kind != EnemyIntentKind.Reposition
                        || !TerrainBlocks(enemy.Intent.TargetX, enemy.Intent.TargetY, EnemyHitRadius);
                    int damage = enemy.ResolveIntent(allowReposition);
                    if (Tactics != null)
                    {
                        if (!enemy.IsAimingAt(Tactics.X, Tactics.Y, TacticalCombatState.PlayerRadius)) damage = 0;
                        damage = Tactics.Statuses.AbsorbDamage(damage);
                        if (damage > 0)
                        {
                            if (enemy.Definition.Behavior.Kind == EnemyBehaviorKind.ChargeBurst)
                                Tactics.Statuses.Add(CombatStatusKind.Burn, 2, 3);
                            else Tactics.Statuses.Add(CombatStatusKind.Weaken, 1, 2);
                            int thorns = Tactics.Statuses.Get(CombatStatusKind.Thorns);
                            if (thorns > 0) enemy.TakeDamage(thorns);
                        }
                    }
                    incomingDamage += damage;
                    PlayerHealth = Math.Max(0, PlayerHealth - damage);
                }
                enemy.Statuses.Tick();
            }

            if (PlayerHealth == 0)
            {
                Phase = BattlePhase.Defeat;
                return incomingDamage;
            }

            if (Tactics != null)
            {
                PlayerHealth = Math.Min(PlayerMaxHealth,
                    PlayerHealth + Tactics.Statuses.Get(CombatStatusKind.Regeneration));
                Tactics.Statuses.Tick();
            }
            if (AreAllEnemiesDefeated())
            {
                Phase = BattlePhase.Victory;
                return incomingDamage;
            }

            Turn++;
            if (CombatSkillCooldown > 0) CombatSkillCooldown--;
            if (!preserveFragments) ResetEquationHistory();
            Energy = playerMaxEnergy;
            movementEnergySpent = 0;
            Tactics?.BeginNextTurn();
            PrepareEnemyIntents();
            Phase = BattlePhase.PlayerPlanning;
            return incomingDamage;
        }

        public int PreviewDamage(EnemyState enemy)
        {
            if (enemy == null)
            {
                throw new ArgumentNullException(nameof(enemy));
            }

            if (!enemy.IsAlive || !Equation.HasBase)
            {
                return 0;
            }

            int damage = EquationAnalyzer.CalculateIntersectionDamage(
                Equation,
                enemy.X,
                enemy.Y,
                EnemyHitRadius);
            if (damage <= 0) return 0;
            return Math.Max(1, damage + plotDamageBonus + WeaveDamageBonus + (PrismCharged ? PrismDamageBonus + prismDamage : 0)
                + (resolvedPlots==0?firstPlotDamage:0) + (UsesFragments && playedCardCount<=3?shortWeaveDamage:0)
                + (UsesFragments && playedCardCount>=6?longWeaveDamage:0) + (Tactics == null ? 0 : Tactics.AttackBonus)
                + enemy.Statuses.Get(CombatStatusKind.Exposure) + enemy.Statuses.Get(CombatStatusKind.Rupture));
        }

        private void PrepareEnemyIntents()
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i].PrepareIntent(Turn);
                if (Tactics != null)
                {
                    enemies[i].AimAt(Tactics.X, Tactics.Y);
                    if (enemies[i].IsAlive && enemies[i].Intent.Kind == EnemyIntentKind.Charge)
                        enemies[i].Statuses.Add(CombatStatusKind.Shield, 12, 1);
                }
            }
        }

        private bool TerrainBlocks(double x, double y, double radius)
        {
            for (int i = 0; i < definition.TerrainCount; i++)
            {
                BattleTerrainDefinition item = definition.GetTerrain(i);
                if (!item.BlocksMovement) continue;
                double dx = x - item.X, dy = y - item.Y, reach = radius + item.Radius;
                if (dx * dx + dy * dy < reach * reach) return true;
            }
            return false;
        }

        private bool PositionBlocked(double x, double y)
        {
            if (x < TacticalCombatState.PlayerRadius || x > 10 - TacticalCombatState.PlayerRadius
                || y < -4 + TacticalCombatState.PlayerRadius || y > 4 - TacticalCombatState.PlayerRadius
                || TerrainBlocks(x, y, TacticalCombatState.PlayerRadius)) return true;
            for (int i = 0; i < enemies.Length; i++)
            {
                double dx = x - enemies[i].X, dy = y - enemies[i].Y;
                double reach = TacticalCombatState.PlayerRadius + EnemyHitRadius;
                if (enemies[i].IsAlive && dx * dx + dy * dy < reach * reach) return true;
            }
            return false;
        }

        private bool FindSkillLanding(double desiredX, double desiredY, out double resultX, out double resultY)
        {
            resultX = Math.Max(TacticalCombatState.PlayerRadius, Math.Min(10 - TacticalCombatState.PlayerRadius, desiredX));
            resultY = Math.Max(-4 + TacticalCombatState.PlayerRadius, Math.Min(4 - TacticalCombatState.PlayerRadius, desiredY));
            if (!PositionBlocked(resultX, resultY)) return true;
            double best = double.MaxValue, originX = resultX, originY = resultY;
            for (int ring = 1; ring <= 6; ring++)
            {
                double radius = ring * .18;
                for (int step = 0; step < 16; step++)
                {
                    double angle = step * Math.PI / 8.0;
                    double x = Math.Max(TacticalCombatState.PlayerRadius,
                        Math.Min(10 - TacticalCombatState.PlayerRadius, originX + Math.Cos(angle) * radius));
                    double y = Math.Max(-4 + TacticalCombatState.PlayerRadius,
                        Math.Min(4 - TacticalCombatState.PlayerRadius, originY + Math.Sin(angle) * radius));
                    if (PositionBlocked(x, y)) continue;
                    double dx = x - desiredX, dy = y - desiredY, error = dx * dx + dy * dy;
                    if (error < best) { best = error; resultX = x; resultY = y; }
                }
                if (best < double.MaxValue) return true;
            }
            resultX = Tactics.X; resultY = Tactics.Y;
            return false;
        }

        private int NearestLivingEnemy()
        {
            int result = -1; double best = double.MaxValue;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (!enemies[i].IsAlive) continue;
                double dx = enemies[i].X - Tactics.X, dy = enemies[i].Y - Tactics.Y;
                double squared = dx * dx + dy * dy;
                if (squared < best) { best = squared; result = i; }
            }
            return result;
        }

        private bool HasLivingEnemy()
        {
            for (int i = 0; i < enemies.Length; i++) if (enemies[i].IsAlive) return true;
            return false;
        }

        private static double DistanceToSegment(double px, double py, double ax, double ay, double bx, double by)
        {
            double dx = bx - ax, dy = by - ay, lengthSquared = dx * dx + dy * dy;
            if (lengthSquared <= 1e-9) { dx = px - ax; dy = py - ay; return Math.Sqrt(dx * dx + dy * dy); }
            double t = Math.Max(0, Math.Min(1, ((px - ax) * dx + (py - ay) * dy) / lengthSquared));
            double x = px - (ax + t * dx), y = py - (ay + t * dy);
            return Math.Sqrt(x * x + y * y);
        }

        private static int ApplyBundledAbilities(CardDefinition card, AbilityTarget target, CombatStatusState statuses)
        {
            int healing = 0;
            for (int i = 0; i < card.AbilityCount; i++)
            {
                CardAbility ability = card.GetAbility(i);
                if (ability.Target == target) healing += TacticalCombatState.ApplyAbility(statuses, ability);
            }
            return healing;
        }

        private bool AreAllEnemiesDefeated()
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i].IsAlive)
                {
                    return false;
                }
            }

            return true;
        }

        private void RecordPlayedCard(CardDefinition card)
        {
            if (playedCardCount >= playedCards.Length)
            {
                throw new InvalidOperationException("Played card history exceeded equation capacity.");
            }

            playedCards[playedCardCount] = card;
            playedCardCount++;
        }

        private void ResetEquationHistory()
        {
            SealedCardCount = 0;
            for (int i = 0; i < playedCardCount; i++)
            {
                playedCards[i] = null;
            }

            playedCardCount = 0;
            Equation.Reset();
            if (definition.UsesCalculator) Equation.EnableCalculator();
            if (UsesFragments) Equation.EnableFragments();
        }
    }
}

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
        { if (!CanCondense) return false; PlayerHealth -= CondenseHealthCost; preserveFragments = true; CondenseCount++; SealedCardCount = playedCardCount; Phase = BattlePhase.EnemyTurn; return true; }
        public bool TryUnravel()
        { if (!UsesFragments || Phase != BattlePhase.PlayerPlanning) return false; preserveFragments = false; CondenseCount = 0; ResetEquationHistory(); Phase = BattlePhase.EnemyTurn; return true; }
        private int movementEnergySpent;
        private readonly int startingResonance;
        private readonly int startShield, selfShield, firstPlotDamage, shortWeaveDamage, longWeaveDamage, movedPlotShield;
        private int resolvedPlots;

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
            int startingResonance = 0, RunRelicCollection relics = null)
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
            this.startingResonance = Math.Min(6, startingResonance + (relics?.GetTotalMagnitude(RelicEffectKind.StartResonance) ?? 0));
            startShield = relics?.GetTotalMagnitude(RelicEffectKind.StartShield) ?? 0;
            selfShield = relics?.GetTotalMagnitude(RelicEffectKind.SelfShield) ?? 0;
            firstPlotDamage = relics?.GetTotalMagnitude(RelicEffectKind.FirstPlotDamage) ?? 0;
            shortWeaveDamage = relics?.GetTotalMagnitude(RelicEffectKind.ShortWeaveDamage) ?? 0;
            longWeaveDamage = relics?.GetTotalMagnitude(RelicEffectKind.LongWeaveDamage) ?? 0;
            movedPlotShield = relics?.GetTotalMagnitude(RelicEffectKind.MovedPlotShield) ?? 0;
            if (definition.Archetype != CombatArchetype.None)
                Tactics = new TacticalCombatState(definition.Archetype, startingResonance);
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
            Turn = 1; resolvedPlots = 0; CondenseCount = 0; preserveFragments = false;
            movementEnergySpent = 0;
            Tactics?.Reset(startingResonance);
            if(startShield>0) Tactics?.Statuses.Add(CombatStatusKind.Shield,startShield,1);
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
        {
            if (Phase != BattlePhase.PlayerPlanning || Tactics == null
                || !Tactics.CanMove(dx, dy) || (!UsesFragments && Energy < Tactics.MoveCost)) return false;
            for (int i = 0; i < enemies.Length; i++)
            {
                double x = Tactics.X + dx - enemies[i].X;
                double y = Tactics.Y + dy - enemies[i].Y;
                double radius = TacticalCombatState.PlayerRadius + EnemyHitRadius;
                if (enemies[i].IsAlive && x * x + y * y < radius * radius) return false;
            }
            movementEnergySpent = UsesFragments ? 0 : Tactics.MoveCost;
            Energy -= movementEnergySpent;
            Tactics.Move(dx, dy);
            return true;
        }

        public bool TryUndoMove()
        {
            if (Phase != BattlePhase.PlayerPlanning || Tactics == null || !Tactics.HasMoved) return false;
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
                for (int cardIndex = 0; cardIndex < playedCardCount; cardIndex++)
                {
                    if (playedCards[cardIndex].IsInscription)
                        TacticalCombatState.ApplyEnemyInscription(enemy, playedCards[cardIndex].Inscription);
                    ApplyBundledAbilities(playedCards[cardIndex], AbilityTarget.Enemy, enemy.Statuses);
                }
                if (Tactics != null && Tactics.UltimateArmed && Tactics.Archetype == CombatArchetype.Ian)
                    enemy.Statuses.Add(CombatStatusKind.Anchor, 1, 1);
                hitCount++;
                totalDamage += damage;
            }

            if (Tactics != null)
            {
                Tactics.Statuses.Remove(CombatStatusKind.Focus);
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
                if(Tactics.HasMoved && movedPlotShield>0) Tactics.Statuses.Add(CombatStatusKind.Shield,movedPlotShield,1);
                Tactics.CompletePlot(playerHit, hitCount);
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
                    int damage = enemy.ResolveIntent();
                    if (Tactics != null)
                    {
                        if (!enemy.IsAimingAt(Tactics.X, Tactics.Y, TacticalCombatState.PlayerRadius)) damage = 0;
                        damage = Tactics.Statuses.AbsorbDamage(damage);
                        if (damage > 0)
                        {
                            if (enemy.Definition.Behavior.Kind == EnemyBehaviorKind.ChargeBurst)
                                Tactics.Statuses.Add(CombatStatusKind.Burn, 2, 3);
                            else Tactics.Statuses.Add(CombatStatusKind.Weaken, 1, 2);
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
            return Math.Max(1, damage + plotDamageBonus + WeaveDamageBonus
                + (resolvedPlots==0?firstPlotDamage:0) + (UsesFragments && playedCardCount<=3?shortWeaveDamage:0)
                + (UsesFragments && playedCardCount>=6?longWeaveDamage:0) + (Tactics == null ? 0 : Tactics.AttackBonus)
                + enemy.Statuses.Get(CombatStatusKind.Exposure));
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

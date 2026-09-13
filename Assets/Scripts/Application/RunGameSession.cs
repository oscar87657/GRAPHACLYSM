using System;
using System.Collections.Generic;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Decks;
using Graphaclysm.Core.Relics;
using Graphaclysm.Core.Runs;

namespace Graphaclysm.Application
{
    public enum RunPhase
    {
        MapSelection,
        Battle,
        CardReward,
        RelicReward,
        Completed,
        Defeated,
        Room,
        DeckRefinement,
        Loot
    }

    /// <summary>
    /// Owns the lifetime of a run and creates a fresh battle aggregate per encounter.
    /// </summary>
    public sealed partial class RunGameSession
    {
        public const int RewardOptionCount = 3;
        public bool IsPractice { get; internal set; }
        public CombatApproach StartingApproach { get; private set; }
        public CombatApproach EffectiveApproach
        {
            get
            {
                if (StartingApproach == CombatApproach.None) return CombatApproach.None;
                var identity = Growth.GetEquippedNode(GrowthEquipKind.Identity);
                if (identity?.Id == "ian.archive.core") return CombatApproach.Recording;
                if (identity?.Id == "ian.inscription.core") return CombatApproach.Execution;
                if (identity?.Id == "luna.orbit.core") return CombatApproach.Tuning;
                if (HasEconomy && identity?.Id == "luna.binary.core") return CombatApproach.Observation;
                return StartingApproach;
            }
        }
        public bool TryChooseApproach(CombatApproach approach)
        {
            if (IsPractice || journal == null || journal.Count != 0 || Phase != RunPhase.MapSelection
                || approach < CombatApproach.Execution || approach > CombatApproach.Observation
                || (Growth.Archetype == CombatArchetype.Luna) != (approach >= CombatApproach.Tuning)) return false;
            StartingApproach = approach;
            Deck = new RunDeck(SignatureBattleFactory.CreateStartingDeck()); handSize = 6;
            return Record(true, RunCommandKind.ChooseApproach, (int)approach);
        }
        public bool TryUseDiagramAbility(double x, double y, int angle = 0)
        {
            if (Phase != RunPhase.Battle || !CurrentBattle.Battle.TryUseDiagramAbility(x, y, angle)) return false;
            int packed = PackMove(Math.Round(x, 2), Math.Round(y, 2)) | ((angle == 180 ? 7 : (angle + 90) / 30) << 20);
            return Record(true, RunCommandKind.DiagramAbility, packed);
        }
        public bool TryPlaceSatellite(double x, double y)
        {
            if (Phase != RunPhase.Battle || !CurrentBattle.Battle.TryPlaceSatellite(x, y)) return false;
            return Record(true, RunCommandKind.PlaceSatellite, PackMove(Math.Round(x, 2), Math.Round(y, 2)));
        }
        public bool TryToggleSatelliteOrigin() => Phase == RunPhase.Battle
            && Record(CurrentBattle.Battle.TryToggleSatelliteOrigin(), RunCommandKind.SatelliteOrigin);
        public const int RelicRewardOptionCount = 3;

        private CardDefinition[] rewardPool;
        private readonly CardDefinition[] rewardOptions = new CardDefinition[RewardOptionCount];
        private RelicDefinition[] relicPool;
        private readonly RelicDefinition[] relicRewardOptions =
            new RelicDefinition[RelicRewardOptionCount];
        private readonly XorShiftRandom rewardRandom;
        private readonly uint runSeed;
        private int handSize;

        private int persistentHealth;
        private int persistentResonance;
        private int legacyVictoryHealing;
        private int legacyStartingResonance;
        private int legacyStartingShield;
        private LegacyBenefits appliedLegacyBenefits;
        private int encounterNumber;
        private string calculatorX = "t-3", calculatorY = "t-3";

        public RunGameSession(
            IReadOnlyList<BattleDefinition> encounters,
            IReadOnlyList<CardDefinition> startingDeck,
            IReadOnlyList<CardDefinition> rewardPool,
            int handSize,
            uint seed)
            : this(
                RunMapDefinition.CreateLinear(encounters),
                startingDeck,
                rewardPool,
                handSize,
                seed,
                null)
        {
        }

        public RunGameSession(
            RunMapDefinition map,
            IReadOnlyList<CardDefinition> startingDeck,
            IReadOnlyList<CardDefinition> rewardPool,
            int handSize,
            uint seed,
            IReadOnlyList<RelicDefinition> relicPool = null,
            CombatArchetype archetype = CombatArchetype.None)
        {
            if (rewardPool == null || rewardPool.Count < RewardOptionCount)
            {
                throw new ArgumentException("The reward pool requires at least three cards.", nameof(rewardPool));
            }

            if (startingDeck == null)
            {
                throw new ArgumentNullException(nameof(startingDeck));
            }

            if (handSize <= 0 || handSize > startingDeck.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(handSize));
            }

            this.rewardPool = new CardDefinition[rewardPool.Count];
            int distinctCardCount = 0;
            for (int i = 0; i < rewardPool.Count; i++)
            {
                this.rewardPool[i] = rewardPool[i]
                    ?? throw new ArgumentException("Reward definitions cannot contain null.", nameof(rewardPool));
                bool duplicateId = false;
                for (int previous = 0; previous < i; previous++)
                {
                    if (string.Equals(
                            this.rewardPool[previous].Id,
                            this.rewardPool[i].Id,
                            StringComparison.Ordinal))
                    {
                        duplicateId = true;
                        break;
                    }
                }

                if (!duplicateId)
                {
                    distinctCardCount++;
                }
            }

            if (distinctCardCount < RewardOptionCount)
            {
                throw new ArgumentException(
                    "The reward pool requires at least three distinct card ids.",
                    nameof(rewardPool));
            }

            int relicCount = relicPool == null ? 0 : relicPool.Count;
            this.relicPool = new RelicDefinition[relicCount];
            for (int i = 0; i < relicCount; i++)
            {
                this.relicPool[i] = relicPool[i]
                    ?? throw new ArgumentException("Relic definitions cannot contain null.", nameof(relicPool));
                for (int previous = 0; previous < i; previous++)
                {
                    if (string.Equals(
                            this.relicPool[previous].Id,
                            this.relicPool[i].Id,
                            StringComparison.Ordinal))
                    {
                        throw new ArgumentException(
                            "Relic pool ids must be unique.",
                            nameof(relicPool));
                    }
                }
            }

            this.handSize = handSize;
            runSeed = seed == 0u ? 1u : seed;
            rewardRandom = new XorShiftRandom(runSeed ^ 0xA511E9B3u);
            Deck = new RunDeck(startingDeck);
            Relics = new RunRelicCollection();
            Growth = new RunGrowthState(archetype);
            Map = new RunMapProgress(map ?? throw new ArgumentNullException(nameof(map)));
            persistentHealth = Map.Definition.PlayerMaxHealth;
            Phase = RunPhase.MapSelection;
        }

        public RunDeck Deck { get; private set; }
        public RunRelicCollection Relics { get; }
        public RunGrowthState Growth { get; private set; }
        public RunMapProgress Map { get; private set; }
        public BattleGameSession CurrentBattle { get; private set; }
        public RunPhase Phase { get; private set; }
        public int EncounterNumber
        {
            get { return encounterNumber; }
        }
        public uint Seed => runSeed;
        public int CurrentFloor
        {
            get
            {
                int layer = Map.ActiveNodeIndex >= 0 ? Map.Definition.GetNode(Map.ActiveNodeIndex).Layer
                    : Map.LastCompletedNodeIndex < 0 ? 0 : Map.Definition.GetNode(Map.LastCompletedNodeIndex).Layer + 1;
                return Math.Min(Map.Definition.FloorCount, layer / Map.Definition.RoomsPerFloor + 1);
            }
        }
        public int Resonance => persistentResonance;

        internal void ApplyLegacyBenefits(LegacyBenefits benefits)
        {
            appliedLegacyBenefits = benefits;
            legacyVictoryHealing = Math.Max(0, benefits.VictoryHealing);
            legacyStartingResonance = Math.Max(0, benefits.StartingResonance);
            legacyStartingShield = Math.Max(0, benefits.StartingShield);
            // The serialized field name is retained for profile compatibility. Its value now
            // grants starting constellation points directly instead of exploration experience.
            Growth.AddPoints(Math.Max(0, benefits.StartingExperience));
        }

        public int EncounterCount
        {
            get { return Map.Definition.LayerCount; }
        }

        public int PlayerHealth
        {
            get { return persistentHealth; }
        }

        public int PlayerMaxHealth
        {
            get { return Map.Definition.PlayerMaxHealth; }
        }

        public IReadOnlyList<CardDefinition> RewardOptions
        {
            get { return rewardOptions; }
        }

        public IReadOnlyList<RelicDefinition> RelicRewardOptions
        {
            get { return relicRewardOptions; }
        }

        public bool TrySelectMapNode(int nodeIndex)
        {
            if (Phase != RunPhase.MapSelection || !Map.TryEnterNode(nodeIndex))
            {
                return false;
            }

            RunMapNodeDefinition node = Map.Definition.GetNode(nodeIndex);
            if(HasExpeditionSupplies && node.Battle==null) PrepareSupplyRoom(node.Story);
            RoomResult = "";
            LastLootText = ""; LastCoinsAwarded = 0;
            if (node.Battle == null)
            { CurrentBattle = null; encounterNumber = node.Layer + 1; Phase = RunPhase.Room;
                if (node.Kind == RunNodeKind.Shop) PrepareShop(nodeIndex);
                return Record(true, RunCommandKind.SelectNode, nodeIndex); }
            StartEncounter(node, nodeIndex);
            return Record(true, RunCommandKind.SelectNode, nodeIndex);
        }

        public bool TryPlayHandCard(
            int handIndex,
            out CardDefinition playedCard,
            out CardPlayFailure failure)
        {
            if (Phase != RunPhase.Battle || handIndex < 0 || handIndex >= CurrentBattle.Deck.HandCount)
            {
                playedCard = null;
                failure = CardPlayFailure.WrongPhase;
                return false;
            }

            return Record(CurrentBattle.TryPlayHandCard(handIndex, out playedCard, out failure), RunCommandKind.PlayCard, handIndex);
        }

        public bool TryCondense() => Record(Phase == RunPhase.Battle && CurrentBattle.TryCondense(), RunCommandKind.Condense);
        public bool TryUnravel() => Record(Phase == RunPhase.Battle && CurrentBattle.TryUnravel(), RunCommandKind.Unravel);
        public bool TryBeginPlot()
        {
            return Record(Phase == RunPhase.Battle && CurrentBattle.TryBeginPlot(), RunCommandKind.BeginPlot);
        }

        public bool TryMovePlayer(double dx, double dy)
            => Phase == RunPhase.Battle && TryMovePlayerTo(CurrentBattle.Battle.Tactics.X + dx, CurrentBattle.Battle.Tactics.Y + dy);
        public bool TryMovePlayerTo(double x, double y)
        {
            int packed = PackMove(x, y);
            UnpackMove(packed, out double quantizedX, out double quantizedY);
            return Record(Phase == RunPhase.Battle && CurrentBattle.Battle.TryMovePlayerTo(quantizedX, quantizedY), RunCommandKind.MoveTo, packed);
        }
        public bool TryUndoMove() => Record(Phase == RunPhase.Battle && CurrentBattle.Battle.TryUndoMove(), RunCommandKind.UndoMove);
        public bool TrySetCalculator(string x, string y, out string error)
        {
            error = "전투 중에만 계산할 수 있습니다.";
            if (Phase != RunPhase.Battle || !CurrentBattle.Battle.TrySetCalculator(x, y, out error)) return false;
            calculatorX = x; calculatorY = y; return true;
        }
        public bool TrySelectFormulaAxis(int axis) => Phase == RunPhase.Battle && CurrentBattle.Battle.TrySelectFormulaAxis(axis);

        public bool TryToggleUltimate()
            => Record(Phase == RunPhase.Battle && CurrentBattle.Battle.TryToggleUltimate(), RunCommandKind.ToggleUltimate);

        public bool TryUseCombatSkill(int targetIndex = -1)
        {
            if (Phase != RunPhase.Battle || !CurrentBattle.Battle.TryUseCombatSkill(targetIndex)) return false;
            Record(true, RunCommandKind.UseCombatSkill, targetIndex);
            if (CurrentBattle.Battle.Phase == BattlePhase.Victory) CompleteVictory();
            return true;
        }

        public bool TryUseExecutionDash(double x, double y)
        {
            x = Math.Round(x, 2); y = Math.Round(y, 2);
            if (Phase != RunPhase.Battle || !CurrentBattle.Battle.TryUseExecutionDash(x, y)) return false;
            Record(true, RunCommandKind.ExecutionDash, PackMove(x, y));
            if (CurrentBattle.Battle.Phase == BattlePhase.Victory) CompleteVictory();
            return true;
        }

        public bool TryUseLunaPull(double x, double y)
        {
            x = Math.Round(x, 2); y = Math.Round(y, 2);
            if (Phase != RunPhase.Battle || !CurrentBattle.Battle.TryUseLunaPull(x, y)) return false;
            Record(true, RunCommandKind.LunaPull, PackMove(x, y));
            if (CurrentBattle.Battle.Phase == BattlePhase.Victory) CompleteVictory();
            return true;
        }

        public bool CanEditGrowth => Phase == RunPhase.MapSelection
            || (Phase == RunPhase.Room && CurrentRoom != null
                && Map.Definition.GetNode(Map.ActiveNodeIndex).Kind == RunNodeKind.Rest);

        public bool TryPurchaseGrowthNode(int nodeIndex)
        {
            if(IsRetiredUltimateNode(nodeIndex)) return false;
            if (nodeIndex < 0 || nodeIndex >= Growth.NodeCount) return false;
            int commandId = Growth.GetNode(nodeIndex).CommandId;
            return Record(CanEditGrowth && Growth.TryPurchase(nodeIndex), RunCommandKind.PurchaseGrowth, commandId);
        }

        public bool TrySelectGrowthNode(int nodeIndex)
        {
            if(IsRetiredUltimateNode(nodeIndex)) return false;
            if (nodeIndex < 0 || nodeIndex >= Growth.NodeCount) return false;
            int commandId = Growth.GetNode(nodeIndex).CommandId;
            return Record(CanEditGrowth && Growth.TrySelect(nodeIndex), RunCommandKind.SelectGrowth, commandId);
        }

        public bool TryRefundGrowthNode(int nodeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= Growth.NodeCount) return false;
            return Record(CanEditGrowth && Growth.TryRefund(nodeIndex), RunCommandKind.RefundGrowth,
                Growth.GetNode(nodeIndex).CommandId);
        }

        public bool TryResetGrowth()
            => Record(CanEditGrowth && Growth.TryReset(), RunCommandKind.ResetGrowth);

        public bool TryUndoLastPlayedCard(out CardDefinition restoredCard)
        {
            if (Phase != RunPhase.Battle)
            {
                restoredCard = null;
                return false;
            }

            return Record(CurrentBattle.TryUndoLastPlayedCard(out restoredCard), RunCommandKind.UndoCard);
        }

        public PlotReport ResolvePlot()
        {
            if (Phase != RunPhase.Battle)
            {
                throw new InvalidOperationException("A plot can only resolve during a run battle.");
            }

            PlotReport report = CurrentBattle.ResolvePlot();
            Record(true, RunCommandKind.ResolvePlot);
            if (CurrentBattle.Battle.Phase != BattlePhase.Victory)
            {
                return report;
            }

            CompleteVictory();
            return report;
        }

        private void CompleteVictory()
        {
            if (IsPractice)
            {
                persistentHealth = CurrentBattle.Battle.PlayerHealth;
                Map.TryCompleteActiveNode(); Phase = RunPhase.Completed; return;
            }
            persistentHealth = CurrentBattle.Battle.PlayerHealth;
            persistentResonance = Math.Min(6,(CurrentBattle.Battle.Tactics?.Resonance ?? 0) + Relics.GetTotalMagnitude(RelicEffectKind.VictoryResonance));
            int healing = Relics.GetTotalMagnitude(RelicEffectKind.HealAfterVictory) + legacyVictoryHealing;
            persistentHealth = healing >= PlayerMaxHealth - persistentHealth
                ? PlayerMaxHealth
                : persistentHealth + healing;
            RunNodeKind completedNodeKind =
                Map.Definition.GetNode(Map.ActiveNodeIndex).Kind;
            if (!Map.TryCompleteActiveNode())
            {
                throw new InvalidOperationException("The active map node could not be completed.");
            }
            AwardExploration(completedNodeKind);
            if (HasEconomy) AwardCombatLoot(completedNodeKind);

            if(completedNodeKind == RunNodeKind.Boss && Map.Phase != RunMapProgressPhase.Completed)
            { persistentHealth = Math.Min(PlayerMaxHealth,persistentHealth+8); RoomResult = "층 보스 격파 · 체력 8 회복. 다음 층으로 이어집니다."; }
            if (HasExpeditionSupplies) { PrepareCombatSupplies(completedNodeKind); return; }
            if (Map.Phase == RunMapProgressPhase.Completed)
            {
                Phase = RunPhase.Completed;
            }
            else if ((completedNodeKind == RunNodeKind.Elite || completedNodeKind == RunNodeKind.Boss) && CanOfferRelicReward())
            {
                GenerateRelicRewardOptions();
                Phase = RunPhase.RelicReward;
            }
            else
            {
                GenerateRewardOptions();
                Phase = RunPhase.CardReward;
            }

        }

        public int ResolveEnemyTurn()
        {
            if (Phase != RunPhase.Battle)
            {
                throw new InvalidOperationException("An enemy turn can only resolve during a run battle.");
            }

            int damage = CurrentBattle.ResolveEnemyTurn();
            Record(true, RunCommandKind.ResolveEnemy);
            if (CurrentBattle.Battle.Phase == BattlePhase.Victory) CompleteVictory();
            if (CurrentBattle.Battle.Phase == BattlePhase.Defeat)
            {
                Phase = RunPhase.Defeated;
            }

            return damage;
        }

        public bool TrySelectReward(int optionIndex)
        {
            if (Phase != RunPhase.CardReward
                || optionIndex < 0
                || optionIndex >= rewardOptions.Length)
            {
                return false;
            }

            if (!Deck.TryAdd(rewardOptions[optionIndex]))
            {
                return false;
            }

            FinishReward();
            return Record(true, RunCommandKind.SelectCardReward, optionIndex);
        }

        public bool TrySkipReward()
        {
            if (Phase != RunPhase.CardReward && Phase != RunPhase.RelicReward)
            {
                return false;
            }

            FinishReward(true);
            return Record(true, RunCommandKind.SkipReward, 0);
        }

        public bool TrySelectRelicReward(int optionIndex)
        {
            if (Phase != RunPhase.RelicReward
                || optionIndex < 0
                || optionIndex >= relicRewardOptions.Length)
            {
                return false;
            }

            RelicDefinition relic = relicRewardOptions[optionIndex];
            if (relic == null || !Relics.TryAdd(relic))
            {
                return false;
            }

            FinishReward();
            return Record(true, RunCommandKind.SelectRelicReward, optionIndex);
        }

        private void FinishReward(bool skipped=false)
        {
            ActiveDraftGrade=-1;
            IsResearchReward = false;
            ClearRewardOptions();
            ClearRelicRewardOptions();
            CurrentBattle = null;
            if(returnToShop){returnToShop=false;Phase=RunPhase.Room;return;}
            if (returnToLoot)
            {
                var offer=GetLoot(lootChoiceIndex);
                if(offer!=null) offer.Result=skipped?"받지 않고 넘김":"획득 완료";
                lootChoiceIndex=-1; returnToLoot=false; Phase=RunPhase.Loot; return;
            }
            if (IsStartingRelic) { IsStartingRelic=false; Phase=RunPhase.MapSelection; return; }
            if (shopRemovalIndex >= 0)
            {
                shopRemovalIndex = -1; Phase = RunPhase.Room; return;
            }
            if (pendingBonusRelic)
            {
                pendingBonusRelic = false;
                if (CanOfferRelicReward()) { GenerateRelicRewardOptions(); Phase = RunPhase.RelicReward; return; }
            }
            Phase = Map.Phase == RunMapProgressPhase.Completed ? RunPhase.Completed : RunPhase.MapSelection;
        }

        private void StartEncounter(RunMapNodeDefinition node, int nodeIndex)
        {
            BattleDefinition definition = node.Battle;
            if (StartingApproach != CombatApproach.None)
            {
                var enemyDefinitions = new EnemyDefinition[definition.EnemyCount];
                var terrainDefinitions = new BattleTerrainDefinition[definition.TerrainCount];
                for (int i = 0; i < enemyDefinitions.Length; i++)
                {
                    var enemy = definition.GetEnemy(i);
                    enemyDefinitions[i] = enemy.Behavior.Kind == EnemyBehaviorKind.LineGunner
                        ? new EnemyDefinition("signature.cannon", enemy.DisplayName, enemy.X, enemy.Y, enemy.MaxHealth, enemy.Attack, enemy.Behavior) : enemy;
                }
                for (int i = 0; i < terrainDefinitions.Length; i++) terrainDefinitions[i] = definition.GetTerrain(i);
                definition = new BattleDefinition(definition.PlayerMaxHealth, definition.PlayerMaxEnergy, enemyDefinitions,
                    definition.Archetype, fragments: true, terrain: terrainDefinitions, approach: EffectiveApproach);
                if (node.Layer == 0 && !HasOpeningRoute) definition = SignatureBattleFactory.CreateEncounter(EffectiveApproach, definition.PlayerMaxHealth, true);
            }
            int health = Math.Min(persistentHealth, definition.PlayerMaxHealth);
            int energyBonus = Relics.GetTotalMagnitude(RelicEffectKind.BonusEnergy);
            int plotDamageBonus = Relics.GetTotalMagnitude(RelicEffectKind.BonusPlotDamage) + MasteryRank + TrainingPower + PermanentPlotPower;
            BattleSession battle = new BattleSession(
                definition,
                health,
                energyBonus,
                plotDamageBonus,
                Math.Min(6, persistentResonance + legacyStartingResonance), Relics,
                Growth.CreateLoadout(), legacyStartingShield + MasteryRank + PreparedShield);
            PreparedShield = 0;
            if (HasStatusRules) battle.EnableStatusRules();
            if (HasBattleRework) battle.EnableApproachUltimates();
            if (Growth.IsSpecialized) battle.EnableSpecialization(Growth.CreateCompiledBuild());
            if (battle.Equation.IsCalculator) battle.TrySetCalculator(calculatorX, calculatorY, out _);
            uint battleSeed = runSeed + (uint)(nodeIndex + 1) * 0x9E3779B9u;
            int handBonus = Relics.GetTotalMagnitude(RelicEffectKind.BonusHandSize)+PermanentOpeningHand;
            int effectiveHandSize = handBonus >= Deck.Count - handSize
                ? Deck.Count
                : handSize + handBonus;
            DeckSession deck = new DeckSession(
                Deck,
                definition.UsesFragments ? Math.Min(8, Deck.Count) : effectiveHandSize,
                new XorShiftRandom(battleSeed), definition.UsesFragments, effectiveHandSize);
            CurrentBattle = new BattleGameSession(battle, deck, Math.Min(8, 2 + Relics.GetTotalMagnitude(RelicEffectKind.BonusDraw)) );
            encounterNumber = node.Layer + 1;
            Phase = RunPhase.Battle;
        }

        private void GenerateRewardOptions()
        {
            for (int option = 0; option < rewardOptions.Length; option++)
            {
                if (HasMarketBalance)
                {
                    rewardOptions[option] = CardMarketBalance.Draw(rewardRandom, rewardPool, false, rewardOptions, option);
                    continue;
                }
                CardDefinition candidate;
                bool duplicate;
                do
                {
                    candidate = SelectWeightedRewardCandidate();
                    duplicate = false;
                    for (int previous = 0; previous < option; previous++)
                    {
                        if (ReferenceEquals(rewardOptions[previous], candidate))
                        {
                            duplicate = true;
                            break;
                        }
                    }
                }
                while (duplicate);

                rewardOptions[option] = candidate;
            }
        }

        private CardDefinition SelectWeightedRewardCandidate()
        {
            int totalWeight = 0;
            for (int i = 0; i < rewardPool.Length; i++)
            {
                totalWeight += GetRewardWeight(rewardPool[i].Rarity);
            }

            int roll = rewardRandom.Next(totalWeight);
            for (int i = 0; i < rewardPool.Length; i++)
            {
                roll -= GetRewardWeight(rewardPool[i].Rarity);
                if (roll < 0)
                {
                    return rewardPool[i];
                }
            }

            throw new InvalidOperationException("Weighted card reward selection failed.");
        }

        private static int GetRewardWeight(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common:
                    return 100;
                case CardRarity.Uncommon:
                    return 45;
                case CardRarity.Rare:
                    return 15;
                case CardRarity.Legendary:
                    return 3;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rarity));
            }
        }

        private void ClearRewardOptions()
        {
            for (int i = 0; i < rewardOptions.Length; i++)
            {
                rewardOptions[i] = null;
            }
        }

        private bool CanOfferRelicReward()
        {
            int availableCount = 0;
            for (int i = 0; i < relicPool.Length; i++)
            {
                if (!Relics.Contains(relicPool[i].Id))
                {
                    availableCount++;
                }
            }

            return availableCount > 0;
        }

        private void GenerateRelicRewardOptions()
        {
            ClearRelicRewardOptions();
            int available = 0;
            for (int i = 0; i < relicPool.Length; i++) if (!Relics.Contains(relicPool[i].Id)) available++;
            for (int option = 0; option < relicRewardOptions.Length; option++)
            {
                if (option >= available) break;
                RelicDefinition candidate;
                bool unavailable;
                do
                {
                    candidate = relicPool[rewardRandom.Next(relicPool.Length)];
                    unavailable = Relics.Contains(candidate.Id);
                    for (int previous = 0; previous < option && !unavailable; previous++)
                    {
                        unavailable = ReferenceEquals(relicRewardOptions[previous], candidate);
                    }
                }
                while (unavailable);

                relicRewardOptions[option] = candidate;
            }
        }

        private void ClearRelicRewardOptions()
        {
            for (int i = 0; i < relicRewardOptions.Length; i++)
            {
                relicRewardOptions[i] = null;
            }
        }

        public int LastExplorationPoints { get; private set; }

        private void AwardExploration(RunNodeKind kind)
        {
            if (HasExpeditionSupplies) { AwardEarnedGrowth(kind); return; }
            // Every room grants one point; every non-final floor boss grants three extra.
            // This also extends the growth budget when a new run has five floors.
            int points = kind == RunNodeKind.Boss && Map.Phase != RunMapProgressPhase.Completed ? 4 : 1;
            // Move one guaranteed point forward, without increasing the route budget.
            if (HasOpeningRoute && Map.LastCompletedNodeIndex >= 0)
            {
                int layer = Map.Definition.GetNode(Map.LastCompletedNodeIndex).Layer;
                if (layer == 0) points++;
                if (layer == 7) points--;
            }
            int before=Growth.TotalPointBudget;
            Growth.AddPoints(points);
            LastExplorationPoints = Growth.TotalPointBudget-before;
        }
    }
}

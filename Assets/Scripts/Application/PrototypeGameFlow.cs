using System;
using System.Collections.Generic;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;

namespace Graphaclysm.Application
{
    public enum GameFlowPhase
    {
        MainMenu,
        CharacterSelection,
        Run
    }

    /// <summary>
    /// Coordinates front-end screens and owns the current run root.
    /// </summary>
    public sealed class PrototypeGameFlow
    {
        private readonly CharacterDefinition[] characters;
        private uint nextRunSeed;
        private RunGameSession trialReturnRun;
        private CharacterDefinition trialReturnCharacter;
        private int trialReturnSelection;
        public bool IsNodeTrial => trialReturnRun != null;
        public RunGameSession TrialReturnRun => trialReturnRun;
        public CharacterDefinition TrialReturnCharacter => trialReturnCharacter;
        public string TrialNodeId { get; private set; }

        public bool TryStartNodeTrial(string nodeId)
        {
            CombatApproach approach=NodeTrials.ApproachFor(nodeId);
            if(IsNodeTrial || Phase!=GameFlowPhase.Run || CurrentRun==null || CurrentRun.IsPractice
                || !CurrentRun.CanEditGrowth || approach==CombatApproach.None
                || (!CurrentRun.Growth.IsSpecialized && CurrentRun.Growth.IndexOf(nodeId)<0) || CurrentCharacter==null
                || CurrentCharacter.Archetype!=CombatApproachArchetype(approach)) return false;
            var candidate=SignatureBattleFactory.Create(approach);
            NodeTrialComparison.Prepare(candidate);
            trialReturnRun=CurrentRun; trialReturnCharacter=CurrentCharacter; trialReturnSelection=SelectedCharacterIndex;
            TrialNodeId=nodeId; CurrentRun=candidate;
            return true;
        }
        public bool TryReturnFromNodeTrial()
        {
            if(!IsNodeTrial) return false;
            CurrentRun=trialReturnRun; CurrentCharacter=trialReturnCharacter; SelectedCharacterIndex=trialReturnSelection;
            trialReturnRun=null; trialReturnCharacter=null; TrialNodeId=null; Phase=GameFlowPhase.Run;
            return true;
        }

        public PrototypeGameFlow(IReadOnlyList<CharacterDefinition> characters, uint firstRunSeed)
        {
            if (characters == null || characters.Count == 0)
            {
                throw new ArgumentException("At least one playable character is required.", nameof(characters));
            }

            this.characters = new CharacterDefinition[characters.Count];
            for (int i = 0; i < characters.Count; i++)
            {
                this.characters[i] = characters[i]
                    ?? throw new ArgumentException("Character entries cannot contain null.", nameof(characters));
            }

            nextRunSeed = firstRunSeed == 0u ? 1u : firstRunSeed;
            SelectedCharacterIndex = -1;
            Phase = GameFlowPhase.MainMenu;
        }

        public GameFlowPhase Phase { get; private set; }
        public int SelectedCharacterIndex { get; private set; }
        public CharacterDefinition CurrentCharacter { get; private set; }
        public RunGameSession CurrentRun { get; private set; }
        public LegacyBenefits LegacyBenefits { get; set; }
        public int PreparationUpgrades { get; set; }
        public int CharacterCount
        {
            get { return characters.Length; }
        }

        public CharacterDefinition GetCharacter(int index)
        {
            return characters[index];
        }

        public void OpenCharacterSelection()
        {
            if (Phase != GameFlowPhase.MainMenu)
            {
                return;
            }

            SelectedCharacterIndex = 0;
            Phase = GameFlowPhase.CharacterSelection;
        }

        public bool TrySelectCharacter(int index)
        {
            if (Phase != GameFlowPhase.CharacterSelection
                || index < 0
                || index >= characters.Length)
            {
                return false;
            }

            SelectedCharacterIndex = index;
            return true;
        }

        public bool TryStartRun() => TryStartRun(CombatApproach.None);

        public bool TryStartRun(CombatApproach approach, bool supplies = true, bool legacyFreeRelic = false, bool preparation = true)
        {
            if (Phase != GameFlowPhase.CharacterSelection
                || SelectedCharacterIndex < 0)
            {
                return false;
            }

            var character = characters[SelectedCharacterIndex];
            if (approach != CombatApproach.None && (approach < CombatApproach.Execution || approach > CombatApproach.Observation
                || character.Archetype != CombatApproachArchetype(approach))) return false;
            var candidate = PrototypeRunFactory.Create(nextRunSeed, character, LegacyBenefits);
            if (approach != CombatApproach.None && !candidate.TryChooseApproach(approach)) return false;
            if (approach != CombatApproach.None && !candidate.TryEnableOpeningRoute()) return false;
            if (approach != CombatApproach.None && !candidate.TryEnableEconomy()) return false;
            if (approach != CombatApproach.None && !candidate.TryEnableContentExpansion()) return false;
            if (approach != CombatApproach.None && !candidate.TryEnableGrandArchive()) return false;
            if (approach != CombatApproach.None && !candidate.TryEnableMarketBalance()) return false;
            if (approach != CombatApproach.None && !candidate.TryEnableStatusRules()) return false;
            if (approach != CombatApproach.None && !candidate.TryEnableTowerArchive()) return false;
            if (approach != CombatApproach.None && !candidate.TryEnableFiveFloors()) return false;
            if (approach != CombatApproach.None && !candidate.TryEnableBattleRework()) return false;
            if (approach != CombatApproach.None && !candidate.TryEnableApproachTree()) return false;
            if (approach != CombatApproach.None && !candidate.TryEnableStyleTree()) return false;
            if (approach != CombatApproach.None && !candidate.TryEnableGrowthLimit()) return false;
            if (approach != CombatApproach.None && supplies && !candidate.TryEnableExpeditionSupplies(legacyFreeRelic)) return false;
            if(approach!=CombatApproach.None && supplies && !legacyFreeRelic && preparation && !candidate.TryEnablePreparation(PreparationUpgrades))return false;
            CurrentCharacter = character; CurrentRun = candidate;
            nextRunSeed = AdvanceSeed(nextRunSeed);
            Phase = GameFlowPhase.Run;
            return true;
        }

        public bool TryRestartRun()
        {
            if (Phase != GameFlowPhase.Run || CurrentCharacter == null)
            {
                return false;
            }

            if (CurrentRun != null && CurrentRun.IsPractice)
            {
                CurrentRun = SignatureBattleFactory.Create(CurrentRun.CurrentBattle.Battle.Approach);
                if(IsNodeTrial) NodeTrialComparison.Prepare(CurrentRun);
                return true;
            }
            CombatApproach approach = CurrentRun?.StartingApproach ?? CombatApproach.None;
            CurrentRun = PrototypeRunFactory.Create(nextRunSeed, CurrentCharacter, LegacyBenefits);
            if (approach != CombatApproach.None)
            {
                CurrentRun.TryChooseApproach(approach);
                CurrentRun.TryEnableOpeningRoute();
                CurrentRun.TryEnableEconomy();
                CurrentRun.TryEnableContentExpansion();
                CurrentRun.TryEnableGrandArchive();
                CurrentRun.TryEnableMarketBalance();
                CurrentRun.TryEnableStatusRules();
                CurrentRun.TryEnableTowerArchive();
                CurrentRun.TryEnableFiveFloors();
                CurrentRun.TryEnableBattleRework();
                CurrentRun.TryEnableApproachTree();
                CurrentRun.TryEnableStyleTree();
                CurrentRun.TryEnableGrowthLimit();
                CurrentRun.TryEnableExpeditionSupplies(false);
                CurrentRun.TryEnablePreparation(PreparationUpgrades);
            }
            nextRunSeed = AdvanceSeed(nextRunSeed);
            return true;
        }

        public bool TryContinueRun(RunGameSession restored, CharacterDefinition character)
        {
            if (Phase != GameFlowPhase.MainMenu || restored == null || character == null
                || restored.Phase == RunPhase.Completed || restored.Phase == RunPhase.Defeated) return false;
            for (int i = 0; i < characters.Length; i++)
            {
                if (characters[i].Id != character.Id) continue;
                CurrentRun = restored; CurrentCharacter = characters[i]; SelectedCharacterIndex = i;
                nextRunSeed = AdvanceSeed(restored.Seed); Phase = GameFlowPhase.Run;
                return true;
            }
            return false;
        }

        public void ReturnToMainMenu()
        {
            if(TryReturnFromNodeTrial()) return;
            CurrentRun = null;
            CurrentCharacter = null;
            SelectedCharacterIndex = -1;
            Phase = GameFlowPhase.MainMenu;
        }

        public bool TryStartPractice(CombatApproach approach)
        {
            if (Phase != GameFlowPhase.MainMenu || characters.Length < 2
                || approach < CombatApproach.Execution || approach > CombatApproach.Observation) return false;
            CurrentRun = SignatureBattleFactory.Create(approach);
            SelectedCharacterIndex = approach >= CombatApproach.Tuning ? 1 : 0;
            CurrentCharacter = characters[SelectedCharacterIndex];
            Phase = GameFlowPhase.Run;
            return true;
        }

        public void ReturnToCharacterSelection()
        {
            if(TryReturnFromNodeTrial()) return;
            CurrentRun = null;
            CurrentCharacter = null;
            SelectedCharacterIndex = 0;
            Phase = GameFlowPhase.CharacterSelection;
        }

        private static uint AdvanceSeed(uint seed)
        {
            uint next = seed + 0x9E3779B9u;
            return next == 0u ? 1u : next;
        }

        private static CombatArchetype CombatApproachArchetype(CombatApproach approach)
            => approach >= CombatApproach.Tuning ? CombatArchetype.Luna : CombatArchetype.Ian;

    }
}

using System;
using System.Collections.Generic;
using Graphaclysm.Core.Characters;

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

        public bool TryStartRun()
        {
            if (Phase != GameFlowPhase.CharacterSelection
                || SelectedCharacterIndex < 0)
            {
                return false;
            }

            CurrentCharacter = characters[SelectedCharacterIndex];
            CurrentRun = PrototypeRunFactory.Create(nextRunSeed, CurrentCharacter, LegacyBenefits);
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

            CurrentRun = PrototypeRunFactory.Create(nextRunSeed, CurrentCharacter, LegacyBenefits);
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
            CurrentRun = null;
            CurrentCharacter = null;
            SelectedCharacterIndex = -1;
            Phase = GameFlowPhase.MainMenu;
        }

        public void ReturnToCharacterSelection()
        {
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
    }
}

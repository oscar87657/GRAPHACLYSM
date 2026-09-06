using System;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Combat;

namespace Graphaclysm.Core.Characters
{
    /// <summary>
    /// Immutable character configuration. Run-time health remains owned by the run session.
    /// </summary>
    public sealed class CharacterDefinition
    {
        private readonly CardDefinition[] startingDeck;

        public CharacterDefinition(
            string id,
            string displayName,
            string title,
            string playStyle,
            int maxHealth,
            int maxEnergy,
            int handSize,
            CardDefinition[] startingDeck,
            CombatArchetype archetype = CombatArchetype.None)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A character id is required.", nameof(id));
            }

            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            }

            if (maxEnergy <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxEnergy));
            }

            if (startingDeck == null || startingDeck.Length == 0)
            {
                throw new ArgumentException("A character requires a starting deck.", nameof(startingDeck));
            }

            if (handSize <= 0 || handSize > startingDeck.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(handSize));
            }

            bool hasBaseFunction = false;
            this.startingDeck = new CardDefinition[startingDeck.Length];
            for (int i = 0; i < startingDeck.Length; i++)
            {
                CardDefinition card = startingDeck[i]
                    ?? throw new ArgumentException("A starting deck cannot contain null.", nameof(startingDeck));
                this.startingDeck[i] = card;
                hasBaseFunction |= card.IsBaseFunction || card.IsCalculator || card.IsFragment;
            }

            if (!hasBaseFunction)
            {
                throw new ArgumentException("A starting deck requires a base function.", nameof(startingDeck));
            }

            Id = id;
            DisplayName = displayName ?? string.Empty;
            Title = title ?? string.Empty;
            PlayStyle = playStyle ?? string.Empty;
            MaxHealth = maxHealth;
            MaxEnergy = maxEnergy;
            HandSize = handSize;
            Archetype = archetype;
        }

        public string Id { get; }
        public CombatArchetype Archetype { get; }
        public string DisplayName { get; }
        public string Title { get; }
        public string PlayStyle { get; }
        public int MaxHealth { get; }
        public int MaxEnergy { get; }
        public int HandSize { get; }
        public int StartingDeckCount
        {
            get { return startingDeck.Length; }
        }

        public CardDefinition GetStartingCard(int index)
        {
            return startingDeck[index];
        }

        public CardDefinition[] CreateStartingDeckCopy()
        {
            var copy = new CardDefinition[startingDeck.Length];
            Array.Copy(startingDeck, copy, startingDeck.Length);
            return copy;
        }
    }
}

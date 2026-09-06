using Graphaclysm.Core.Characters;
using NUnit.Framework;

namespace Graphaclysm.Tests.Characters
{
    public sealed class CharacterDefinitionTests
    {
        [Test]
        public void PrototypeCharacters_HaveValidDistinctGameplayProfiles()
        {
            CharacterDefinition arca = PrototypeCharacterCatalog.All[0];
            CharacterDefinition noa = PrototypeCharacterCatalog.All[1];

            Assert.That(arca.Id, Is.Not.EqualTo(noa.Id));
            Assert.That(arca.MaxHealth, Is.GreaterThan(noa.MaxHealth));
            Assert.That(noa.MaxEnergy, Is.GreaterThan(arca.MaxEnergy));
            Assert.That(arca.StartingDeckCount, Is.GreaterThanOrEqualTo(arca.HandSize));
            Assert.That(noa.StartingDeckCount, Is.GreaterThanOrEqualTo(noa.HandSize));
        }

        [Test]
        public void StartingDeckCopy_CannotMutateCharacterDefinition()
        {
            CharacterDefinition character = PrototypeCharacterCatalog.All[0];
            var copy = character.CreateStartingDeckCopy();
            var originalFirst = character.GetStartingCard(0);

            copy[0] = null;

            Assert.That(character.GetStartingCard(0), Is.SameAs(originalFirst));
        }
    }
}

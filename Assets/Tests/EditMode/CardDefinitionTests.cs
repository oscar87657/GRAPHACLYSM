using Graphaclysm.Core.Cards;
using NUnit.Framework;

namespace Graphaclysm.Tests.Cards
{
    public sealed class CardDefinitionTests
    {
        [Test]
        public void PrototypeCatalog_AssignsAllRarityTiers()
        {
            bool hasCommon = false;
            bool hasUncommon = false;
            bool hasRare = false;
            bool hasLegendary = false;

            for (int i = 0; i < PrototypeCardCatalog.All.Count; i++)
            {
                switch (PrototypeCardCatalog.All[i].Rarity)
                {
                    case CardRarity.Common:
                        hasCommon = true;
                        break;
                    case CardRarity.Uncommon:
                        hasUncommon = true;
                        break;
                    case CardRarity.Rare:
                        hasRare = true;
                        break;
                    case CardRarity.Legendary:
                        hasLegendary = true;
                        break;
                }
            }

            Assert.That(hasCommon, Is.True);
            Assert.That(hasUncommon, Is.True);
            Assert.That(hasRare, Is.True);
            Assert.That(hasLegendary, Is.True);
        }

        [Test]
        public void HighestOrderPolynomiograph_IsLegendary()
        {
            CardDefinition card = null;
            for (int i = 0; i < PrototypeCardCatalog.All.Count; i++)
            {
                if (PrototypeCardCatalog.All[i].Id
                    == "base.polynomiograph.dodecagram")
                {
                    card = PrototypeCardCatalog.All[i];
                    break;
                }
            }

            Assert.That(card, Is.Not.Null);
            Assert.That(card.Rarity, Is.EqualTo(CardRarity.Legendary));
        }
    }
}

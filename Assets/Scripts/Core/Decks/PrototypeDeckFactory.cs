using Graphaclysm.Core.Cards;

namespace Graphaclysm.Core.Decks
{
    public static class PrototypeDeckFactory
    {
        public const int HandSize = 5;

        public static CardDefinition[] CreateStartingDeck()
        {
            var catalog = PrototypeCardCatalog.All;
            return new[]
            {
                catalog[0],
                catalog[0],
                catalog[1],
                catalog[2],
                catalog[3],
                catalog[3],
                catalog[4],
                catalog[4],
                catalog[5],
                catalog[6],
                catalog[6],
                catalog[7]
            };
        }
    }
}

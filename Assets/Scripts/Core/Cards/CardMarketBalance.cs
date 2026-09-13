using System;
using System.Collections.Generic;
using Graphaclysm.Core.Decks;

namespace Graphaclysm.Core.Cards
{
    // Rules29 immutable definitions. Geometry/IDs are unchanged; effects and economy are explicit.
    public static class CardMarketBalance
    {
        public static int TierWeight(CardRarity rarity, bool shop)
        {
            switch (rarity)
            {
                case CardRarity.Common: return shop ? 40 : 50;
                case CardRarity.Uncommon: return shop ? 30 : 28;
                case CardRarity.Rare: return shop ? 24 : 18;
                case CardRarity.Legendary: return shop ? 6 : 4;
                default: throw new ArgumentOutOfRangeException(nameof(rarity));
            }
        }
        public static int Price(CardRarity rarity) => 8 + 4 * (int)rarity;
        public static CardDefinition Draw(IRandomSource random, IReadOnlyList<CardDefinition> pool, bool shop,
            CardDefinition[] excluded, int excludedCount)
        {
            int total = 0;
            for (int t = 0; t < 4; t++)
                if (Count(pool, (CardRarity)t, excluded, excludedCount) > 0) total += TierWeight((CardRarity)t, shop);
            if (total == 0) throw new InvalidOperationException("No eligible cards.");
            int roll = random.Next(total);
            for (int t = 0; t < 4; t++)
            {
                int count = Count(pool, (CardRarity)t, excluded, excludedCount);
                if (count == 0) continue;
                roll -= TierWeight((CardRarity)t, shop);
                if (roll >= 0) continue;
                int cardRoll = random.Next(count);
                foreach (var card in pool)
                    if ((int)card.Rarity == t && Eligible(card, excluded, excludedCount) && cardRoll-- == 0) return card;
            }
            throw new InvalidOperationException("Card draw failed.");
        }
        private static int Count(IReadOnlyList<CardDefinition> pool, CardRarity tier, CardDefinition[] excluded, int n)
        {
            int count = 0;
            foreach (var card in pool) if (card.Rarity == tier && Eligible(card, excluded, n)) count++;
            return count;
        }
        private static bool Eligible(CardDefinition card, CardDefinition[] excluded, int n)
        {
            for (int i = 0; i < n; i++) if (excluded[i]?.Id == card.Id) return false;
            return true;
        }
        public static CardDefinition[] Build(CardDefinition[] previous)
        {
            var result = new CardDefinition[previous.Length];
            for (int i = 0; i < result.Length; i++)
            {
                var source = previous[i]; var tier = source.DiagramRarity;
                var abilities = new CardAbility[source.AbilityCount];
                for (int a = 0; a < abilities.Length; a++)
                {
                    var old = source.GetAbility(a);
                    bool binary = old.Kind == CardAbilityKind.Cleanse || old.Kind == CardAbilityKind.Anchor || old.Kind == CardAbilityKind.Haste;
                    // Keep utility meaningful, and avoid multiplying binary cleanse or immobilization.
                    int magnitude = binary ? old.Magnitude : Math.Max(1, Math.Min(24, (old.Magnitude * (3 + (int)tier) + 3) / 4));
                    int duration = old.Duration;
                    if (tier == CardRarity.Legendary && !binary && old.Kind != CardAbilityKind.Heal && old.Kind != CardAbilityKind.Shield)
                        duration = Math.Min(3, duration + 1);
                    abilities[a] = new CardAbility(old.Kind, old.Target, magnitude, duration);
                }
                int power = tier == CardRarity.Common ? Math.Min(1, source.WeavePower) : (int)tier + 1;
                result[i] = CardDefinition.Balance(source, tier, abilities, power);
            }
            return result;
        }
    }
}

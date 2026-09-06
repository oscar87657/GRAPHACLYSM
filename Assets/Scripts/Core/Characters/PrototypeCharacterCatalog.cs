using System.Collections.Generic;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Combat;

namespace Graphaclysm.Core.Characters
{
    public static class PrototypeCharacterCatalog
    {
        private static readonly CharacterDefinition[] Characters =
        {
            new CharacterDefinition("character.ian", "이안", "흑유리의 기록자",
                "파편을 겹쳐 잔불을 남기고, 모아 둔 식을 한 번에 펼칩니다.",
                42, 4, 5, Deck("frag.square", "frag.home", "frag.echo", "frag.orbit",
                    "frag.contract", "frag.surge", "frag.right", "frag.down", "frag.west", "frag.petal",
                    "frag.mirror", "frag.shear"), CombatArchetype.Ian),
            new CharacterDefinition("character.luna", "루나", "천문 도구의 조율자",
                "어긋난 회전을, 새로운 궤적으로 잇는다.",
                36, 5, 5, Deck("frag.twin", "frag.home", "frag.lissajous", "frag.contract",
                    "frag.ellipse", "frag.right", "frag.north", "frag.down", "frag.expand", "frag.petal",
                    "frag.overtone", "frag.square"), CombatArchetype.Luna)
        };
        public static IReadOnlyList<CharacterDefinition> All => Characters;
        private static CardDefinition[] Deck(params string[] ids)
        {
            var cards = new CardDefinition[ids.Length];
            for (int i = 0; i < ids.Length; i++) cards[i] = FragmentCardCatalog.Find(ids[i]);
            return cards;
        }
    }
}

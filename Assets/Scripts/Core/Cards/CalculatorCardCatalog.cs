using System;
using System.Collections.Generic;
using Graphaclysm.Core.Equations;

namespace Graphaclysm.Core.Cards
{
    public static class CalculatorCardCatalog
    {
        private static CardAbility Self(CardAbilityKind kind, int n, int duration = 1) => new CardAbility(kind, AbilityTarget.Player, n, duration);
        private static CardAbility Enemy(CardAbilityKind kind, int n, int duration = 2) => new CardAbility(kind, AbilityTarget.Enemy, n, duration);
        private static readonly CardDefinition[] cards = {
            CardDefinition.CreateCalculatorSkill("calc.echo", "잔향", EquationModifierKind.Sine, CardRarity.Common, "사인으로 감싸기", Enemy(CardAbilityKind.Burn,2), Self(CardAbilityKind.Focus,1,2)),
            CardDefinition.CreateCalculatorSkill("calc.silver", "은월", EquationModifierKind.Cosine, CardRarity.Common, "코사인으로 감싸기", Enemy(CardAbilityKind.Anchor,1,1), Self(CardAbilityKind.Heal,2)),
            CardDefinition.CreateCalculatorSkill("calc.frost", "서리꽃", EquationModifierKind.Square, CardRarity.Common, "제곱하기", Enemy(CardAbilityKind.Weaken,1), Self(CardAbilityKind.Shield,3)),
            CardDefinition.CreateCalculatorSkill("calc.rupture", "파열", EquationModifierKind.Double, CardRarity.Uncommon, "두 배로 늘리기", Enemy(CardAbilityKind.Exposure,2), Self(CardAbilityKind.Focus,1,2)),
            CardDefinition.CreateCalculatorSkill("calc.hush", "고요", EquationModifierKind.Half, CardRarity.Common, "절반으로 줄이기", Enemy(CardAbilityKind.Weaken,1), Self(CardAbilityKind.Regeneration,2,2)),
            CardDefinition.CreateCalculatorSkill("calc.rise", "상현", EquationModifierKind.ShiftUp, CardRarity.Common, "1 더하기", Enemy(CardAbilityKind.Anchor,1,1), Self(CardAbilityKind.Heal,3)),
            CardDefinition.CreateCalculatorSkill("calc.fall", "낙화", EquationModifierKind.ShiftDown, CardRarity.Common, "1 빼기", Enemy(CardAbilityKind.Burn,2), Self(CardAbilityKind.Shield,3)),
            CardDefinition.CreateCalculatorSkill("calc.garden", "유리정원", EquationModifierKind.AbsoluteValue, CardRarity.Uncommon, "음수 접기", Enemy(CardAbilityKind.Exposure,2), Self(CardAbilityKind.Shield,3)),
            CardDefinition.CreateCalculatorSkill("calc.bloom", "검은 개화", EquationModifierKind.Cube, CardRarity.Rare, "세제곱하기", Enemy(CardAbilityKind.Burn,3), Self(CardAbilityKind.Focus,2,2)),
            CardDefinition.CreateCalculatorSkill("calc.mirror", "거울밤", EquationModifierKind.Negate, CardRarity.Rare, "부호 뒤집기", Enemy(CardAbilityKind.Cleanse,1,1), Self(CardAbilityKind.Cleanse,1)),
            CardDefinition.CreateCalculatorSkill("calc.eclipse", "월식", EquationModifierKind.Negate, CardRarity.Legendary, "부호 뒤집기", Enemy(CardAbilityKind.Anchor,1), Self(CardAbilityKind.Haste,1,2))
        };
        public static IReadOnlyList<CardDefinition> All => cards;
        public static CardDefinition Find(string id)
        { for (int i=0;i<cards.Length;i++) if (cards[i].Id == id) return cards[i]; throw new ArgumentException("Unknown calculator card.", nameof(id)); }
    }
}

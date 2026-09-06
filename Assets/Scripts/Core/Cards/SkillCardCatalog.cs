using System;
using System.Collections.Generic;
using Graphaclysm.Core.Equations;

namespace Graphaclysm.Core.Cards
{
    public static class SkillCardCatalog
    {
        private static CardAbility Self(CardAbilityKind kind, int amount, int turns = 1)
            => new CardAbility(kind, AbilityTarget.Player, amount, turns);
        private static CardAbility Enemy(CardAbilityKind kind, int amount, int turns = 2)
            => new CardAbility(kind, AbilityTarget.Enemy, amount, turns);
        private static readonly CardDefinition[] Cards =
        {
            CardDefinition.CreateSkill("skill.focus", "초점", "u = x − 5", "직선으로 시작", CardType.Input,
                CardRarity.Common, BaseFunctionKind.InputX, default, Self(CardAbilityKind.Shield, 2)),
            CardDefinition.CreateSkill("skill.moon", "월륜", "r = θ", "나선으로 시작", CardType.Input,
                CardRarity.Common, BaseFunctionKind.InputAngle, default, Self(CardAbilityKind.Regeneration, 1)),
            CardDefinition.CreateSkill("skill.frost", "서리꽃", "v → v²", "제곱으로 굽히기", CardType.Operation,
                CardRarity.Common, default, EquationModifierKind.Square, Enemy(CardAbilityKind.Weaken, 1), Self(CardAbilityKind.Shield, 2)),
            CardDefinition.CreateSkill("skill.echo", "잔향", "v → sin(v)", "물결로 바꾸기", CardType.Operation,
                CardRarity.Common, default, EquationModifierKind.Sine, Enemy(CardAbilityKind.Burn, 2), Self(CardAbilityKind.Focus, 1, 2)),
            CardDefinition.CreateSkill("skill.silver", "은월", "v → cos(v)", "물결의 시작 바꾸기", CardType.Operation,
                CardRarity.Uncommon, default, EquationModifierKind.Cosine, Enemy(CardAbilityKind.Anchor, 1, 1), Self(CardAbilityKind.Heal, 2)),
            CardDefinition.CreateSkill("skill.garden", "유리정원", "v → |v|", "음수를 접어 올리기", CardType.Operation,
                CardRarity.Uncommon, default, EquationModifierKind.AbsoluteValue, Enemy(CardAbilityKind.Exposure, 2), Self(CardAbilityKind.Shield, 2)),
            CardDefinition.CreateSkill("skill.bloom", "검은 개화", "v → v³", "세제곱으로 비틀기", CardType.Operation,
                CardRarity.Rare, default, EquationModifierKind.Cube, Enemy(CardAbilityKind.Burn, 2), Self(CardAbilityKind.Focus, 2, 2)),
            CardDefinition.CreateSkill("skill.rupture", "파열", "v → 2v", "현재 값을 두 배로", CardType.Coefficient,
                CardRarity.Uncommon, default, EquationModifierKind.Double, Enemy(CardAbilityKind.Exposure, 2), Self(CardAbilityKind.Focus, 1, 2)),
            CardDefinition.CreateSkill("skill.hush", "고요", "v → ½v", "현재 값을 절반으로", CardType.Coefficient,
                CardRarity.Common, default, EquationModifierKind.Half, Enemy(CardAbilityKind.Weaken, 1), Self(CardAbilityKind.Regeneration, 1, 2)),
            CardDefinition.CreateSkill("skill.rise", "상현", "v → v + 1", "현재 값에 1 더하기", CardType.Coefficient,
                CardRarity.Common, default, EquationModifierKind.ShiftUp, Enemy(CardAbilityKind.Anchor, 1, 1), Self(CardAbilityKind.Heal, 2)),
            CardDefinition.CreateSkill("skill.fall", "낙화", "v → v − 1", "현재 값에서 1 빼기", CardType.Coefficient,
                CardRarity.Common, default, EquationModifierKind.ShiftDown, Enemy(CardAbilityKind.Burn, 2), Self(CardAbilityKind.Shield, 2)),
            CardDefinition.CreateSkill("skill.mirror", "거울밤", "v → −v", "부호를 뒤집기", CardType.Coefficient,
                CardRarity.Rare, default, EquationModifierKind.Negate, Enemy(CardAbilityKind.Cleanse, 1, 1), Self(CardAbilityKind.Cleanse, 1)),
            CardDefinition.CreateSkill("skill.eclipse", "월식", "v → −v", "부호를 뒤집기", CardType.Coefficient,
                CardRarity.Legendary, default, EquationModifierKind.Negate, Enemy(CardAbilityKind.Anchor, 1), Self(CardAbilityKind.Haste, 1, 2))
        };
        public static IReadOnlyList<CardDefinition> All => Cards;
        public static CardDefinition Find(string id)
        {
            for (int i = 0; i < Cards.Length; i++) if (Cards[i].Id == id) return Cards[i];
            throw new ArgumentException("Unknown skill card.", nameof(id));
        }
    }
}

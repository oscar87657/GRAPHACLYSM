using System;
using Graphaclysm.Core.Equations;

namespace Graphaclysm.Core.Cards
{
    public enum CardType
    {
        BaseFunction,
        Modifier,
        Input,
        Operation,
        Coefficient,
        Inscription,
        Utility
    }

    public enum InscriptionKind { None, Ward, Ember, Exposure, Mend, Cleanse, Phase }

    public enum CardRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }

    /// <summary>
    /// Immutable card data. Runtime state such as hand position must never be stored here.
    /// </summary>
    public sealed class CardDefinition
    {
        private readonly CardAbility[] abilities;
        private readonly int balancedPower = -1;
        private CardDefinition(
            string id,
            string displayName,
            string formulaLabel,
            int cost,
            CardType type,
            CardRarity rarity,
            BaseFunctionKind baseFunction,
            EquationModifierKind modifier,
            string description = "",
            string enemyEffect = "",
            string playerEffect = "",
            InscriptionKind inscription = InscriptionKind.None,
            CardAbility[] abilities = null, bool calculator = false, bool fragment = false, FragmentKind fragmentKind = default, int drawBonus = 0, int balancedPower = -1)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A card id is required.", nameof(id));
            }

            if (cost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cost));
            }

            Id = id;
            DisplayName = displayName ?? string.Empty;
            FormulaLabel = formulaLabel ?? string.Empty;
            Cost = cost;
            Type = type;
            Rarity = rarity;
            this.balancedPower = balancedPower;
            BaseFunction = baseFunction;
            Modifier = modifier;
            Description = description ?? string.Empty;
            EnemyEffect = enemyEffect ?? string.Empty;
            PlayerEffect = playerEffect ?? string.Empty;
            Inscription = inscription;
            IsCalculator = calculator; IsFragment = fragment; Fragment = fragmentKind; DrawBonus = drawBonus;
            this.abilities = abilities == null ? Array.Empty<CardAbility>() : (CardAbility[])abilities.Clone();
        }

        public string Id { get; }
        public bool IsCalculator { get; }
        public bool IsFragment { get; }
        public FragmentKind Fragment { get; }
        public int DrawBonus { get; }
        public static CardDefinition CreateWeaveFragment(string id, string name, FragmentKind kind, CardRarity rarity, string description, int drawBonus, params CardAbility[] abilities)
        {
            if (kind < FragmentKind.Counterpoint || kind > FragmentKind.Weave15_3 || drawBonus < 0 || drawBonus > 2) throw new ArgumentOutOfRangeException(nameof(kind));
            if (abilities == null || abilities.Length + (drawBonus > 0 ? 1 : 0) < 1 || abilities.Length + (drawBonus > 0 ? 1 : 0) > 2) throw new ArgumentException("One or two attached abilities required.");
            for (int i = 0; i < abilities.Length; i++) if (abilities[i].Magnitude <= 0) throw new ArgumentException("Invalid ability.");
            return new CardDefinition(id, name, FragmentEquation.Symbol(kind), 0, CardType.Operation, rarity, default, default,
                description, abilities: abilities, fragment: true, fragmentKind: kind, drawBonus: drawBonus);
        }
        public static CardDefinition CreateCalculatorSkill(string id, string name, EquationModifierKind operation,
            CardRarity rarity, string action, params CardAbility[] abilities)
        {
            if (operation < EquationModifierKind.ShiftUp || operation > EquationModifierKind.Half)
                throw new ArgumentOutOfRangeException(nameof(operation));
            if (abilities == null || abilities.Length < 1 || abilities.Length > 2) throw new ArgumentException("One or two abilities required.");
            for (int i = 0; i < abilities.Length; i++) if (abilities[i].Magnitude <= 0) throw new ArgumentException("Invalid ability.");
            return new CardDefinition(id, name, CalculatorModifiers.Wrap("v", operation), 1, CardType.Operation,
                rarity, default, operation, action, abilities: abilities, calculator: true);
        }
        public int AbilityCount => abilities.Length;
        public CardAbility GetAbility(int index) => abilities[index];
        public bool IsSkill => abilities.Length > 0;
        public string DisplayName { get; }
        public string FormulaLabel { get; }
        public int Cost { get; }
        public CardType Type { get; }
        public CardRarity Rarity { get; }
        // Diagram classification is independent of the historical reward/price band.
        public CardRarity DiagramRarity => IsFragment ? WeaveArchive.DiagramGrade(Fragment) : Rarity;
        public int WeavePower => balancedPower >= 0 ? balancedPower : FragmentCardCatalog.Power(Fragment);
        public bool HasMarketBalance => balancedPower >= 0;
        internal static CardDefinition Balance(CardDefinition source, CardRarity rarity, CardAbility[] abilities, int power)
        {
            return new CardDefinition(source.Id, source.DisplayName, source.FormulaLabel, source.Cost, source.Type,
                rarity, source.BaseFunction, source.Modifier, source.Description, abilities: abilities,
                fragment: true, fragmentKind: source.Fragment, drawBonus: source.DrawBonus, balancedPower: power);
        }
        public BaseFunctionKind BaseFunction { get; }
        public EquationModifierKind Modifier { get; }
        public string Description { get; }
        public string EnemyEffect { get; }
        public string PlayerEffect { get; }
        public InscriptionKind Inscription { get; }
        public bool IsInscription => Type == CardType.Inscription || Type == CardType.Utility;

        public bool IsBaseFunction
        {
            get { return Type == CardType.BaseFunction || Type == CardType.Input; }
        }

        public static CardDefinition CreateInput(string id, string name, string formula,
            BaseFunctionKind input, string description)
        {
            if (input != BaseFunctionKind.InputX && input != BaseFunctionKind.InputAngle)
                throw new ArgumentOutOfRangeException(nameof(input));
            return new CardDefinition(id, name, formula, 0, CardType.Input, CardRarity.Common,
                input, default, description, "선이 닿으면 피해 8 + 기울기 보너스", "선이 닿으면 기본 보호막 3");
        }

        public static CardDefinition CreateSkill(string id, string name, string formula, string graphAction,
            CardType type, CardRarity rarity, BaseFunctionKind input, EquationModifierKind operation,
            params CardAbility[] abilities)
        {
            if (type != CardType.Input && type != CardType.Operation && type != CardType.Coefficient)
                throw new ArgumentOutOfRangeException(nameof(type));
            if (type == CardType.Input && input != BaseFunctionKind.InputX && input != BaseFunctionKind.InputAngle)
                throw new ArgumentOutOfRangeException(nameof(input));
            if (operation < EquationModifierKind.ShiftUp || operation > EquationModifierKind.Half)
                throw new ArgumentOutOfRangeException(nameof(operation));
            if (abilities == null || abilities.Length < 1 || abilities.Length > 2)
                throw new ArgumentException("A skill bundles one graph operation and one or two abilities.", nameof(abilities));
            for (int i = 0; i < abilities.Length; i++)
                if (abilities[i].Magnitude <= 0) throw new ArgumentException("An ability must be initialized.", nameof(abilities));
            return new CardDefinition(id, name, formula, type == CardType.Input ? 0 : 1,
                type, rarity, input, operation, graphAction, abilities: abilities);
        }

        public static CardDefinition CreateFragment(string id, string name, string formula,
            EquationModifierKind modifier, CardType type, CardRarity rarity, string description)
        {
            if (type != CardType.Operation && type != CardType.Coefficient)
                throw new ArgumentOutOfRangeException(nameof(type));
            return new CardDefinition(id, name, formula, 1, type, rarity, default, modifier,
                description, "변경된 선으로 적중·피해를 다시 계산", "변경된 선이 자신에게 닿으면 버프");
        }

        public static CardDefinition CreateInscription(string id, string name, InscriptionKind kind,
            CardRarity rarity, string enemyEffect, string playerEffect, bool utility = false)
        {
            if (kind <= InscriptionKind.None || kind > InscriptionKind.Phase)
                throw new ArgumentOutOfRangeException(nameof(kind));
            return new CardDefinition(id, name, "적 / 자신", 1,
                utility ? CardType.Utility : CardType.Inscription, rarity, default, default,
                "작도 시 적중한 대상마다 한 번 적용. 각인·유틸 합계 2개까지.", enemyEffect, playerEffect, kind);
        }

        public static CardDefinition CreateBase(
            string id,
            string displayName,
            string formulaLabel,
            int cost,
            BaseFunctionKind function,
            CardRarity rarity = CardRarity.Common)
        {
            return new CardDefinition(
                id,
                displayName,
                formulaLabel,
                cost,
                CardType.BaseFunction,
                rarity,
                function,
                default(EquationModifierKind));
        }

        public static CardDefinition CreateModifier(
            string id,
            string displayName,
            string formulaLabel,
            int cost,
            EquationModifierKind modifier,
            CardRarity rarity = CardRarity.Common)
        {
            return new CardDefinition(
                id,
                displayName,
                formulaLabel,
                cost,
                CardType.Modifier,
                rarity,
                default(BaseFunctionKind),
                modifier);
        }
    }
}

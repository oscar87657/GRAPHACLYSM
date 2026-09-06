using System;
using System.Collections.Generic;
using Graphaclysm.Core.Equations;

namespace Graphaclysm.Core.Cards
{
    /// <summary>Playable V2 fragments. Legacy complete curves remain in the mathematics catalog.</summary>
    public static class SpellCardCatalog
    {
        private static readonly CardDefinition[] Cards =
        {
            CardDefinition.CreateInput("input.x", "좌표 조각", "u = x - 5", BaseFunctionKind.InputX,
                "중앙을 0으로 읽는 입력 u. 연결한 연산의 결과가 높이 y가 됩니다."),
            CardDefinition.CreateInput("input.angle", "각도 조각", "r = θ", BaseFunctionKind.InputAngle,
                "0~2π의 각도 θ를 입력합니다. 연결한 연산의 결과가 중심 (5,0)에서의 반지름 r입니다."),
            CardDefinition.CreateFragment("op.square", "제곱", "v → v²", EquationModifierKind.Square,
                CardType.Operation, CardRarity.Common, "지금까지 계산한 값을 제곱합니다. 음수도 양수로 바뀝니다."),
            CardDefinition.CreateFragment("op.sin", "사인", "v → sin(v)", EquationModifierKind.Sine,
                CardType.Operation, CardRarity.Common, "현재 값을 -1~1로 진동시킵니다. 앞쪽 계수는 주기를, 뒤쪽 계수는 크기를 바꿉니다."),
            CardDefinition.CreateFragment("op.cos", "코사인", "v → cos(v)", EquationModifierKind.Cosine,
                CardType.Operation, CardRarity.Uncommon, "사인과 시작점이 다른 진동을 만듭니다. 입력 0에서 결과는 1입니다."),
            CardDefinition.CreateFragment("op.abs", "절댓값", "v → |v|", EquationModifierKind.AbsoluteValue,
                CardType.Operation, CardRarity.Common, "현재 값의 음수 부분을 양수로 접어 올립니다."),
            CardDefinition.CreateFragment("op.cube", "세제곱", "v → v³", EquationModifierKind.Cube,
                CardType.Operation, CardRarity.Rare, "부호를 보존하며 크기를 세제곱합니다. 압축과 조합해 굴곡을 조정하세요."),
            CardDefinition.CreateFragment("coef.double", "배율 2", "v → 2v", EquationModifierKind.Double,
                CardType.Coefficient, CardRarity.Uncommon, "현재 값을 2배로 만듭니다. 사인 앞과 뒤에서 효과가 달라집니다."),
            CardDefinition.CreateFragment("coef.half", "배율 ½", "v → 0.5v", EquationModifierKind.Half,
                CardType.Coefficient, CardRarity.Common, "현재 값을 절반으로 줄입니다. 큰 제곱값이나 극좌표 반지름을 압축합니다."),
            CardDefinition.CreateFragment("coef.up", "상수 +1", "v → v + 1", EquationModifierKind.ShiftUp,
                CardType.Coefficient, CardRarity.Common, "현재 값에 1을 더합니다. 직교식은 높이, 극좌표식은 반지름이 바뀝니다."),
            CardDefinition.CreateFragment("coef.down", "상수 -1", "v → v - 1", EquationModifierKind.ShiftDown,
                CardType.Coefficient, CardRarity.Common, "현재 값에서 1을 뺍니다. 연산 전에 놓으면 입력 구간도 달라집니다."),
            CardDefinition.CreateFragment("coef.negate", "부호 반전", "v → -v", EquationModifierKind.Negate,
                CardType.Coefficient, CardRarity.Uncommon, "현재 값의 부호를 뒤집습니다. 극좌표의 음수 반지름은 반대 방향에 그려집니다."),
            CardDefinition.CreateInscription("sigil.ward", "수호 각인", InscriptionKind.Ward,
                CardRarity.Common, "약화 2 · 적 행동 2회", "보호막 +6 · 이번 적 행동까지"),
            CardDefinition.CreateInscription("sigil.ember", "잔불 각인", InscriptionKind.Ember,
                CardRarity.Uncommon, "잔불 3 · 적 행동 시작 2회", "집중 +2 · 다음 작도 피해에 사용"),
            CardDefinition.CreateInscription("sigil.exposure", "노출 각인", InscriptionKind.Exposure,
                CardRarity.Rare, "노출 3 · 다음 작도 피해 +3", "재생 2 · 적 행동 종료 2회"),
            CardDefinition.CreateInscription("sigil.mend", "봉합 각인", InscriptionKind.Mend,
                CardRarity.Rare, "고정 · 이번 적 행동의 이동 억제", "체력 3 회복"),
            CardDefinition.CreateInscription("utility.cleanse", "정화", InscriptionKind.Cleanse,
                CardRarity.Uncommon, "보호막·집중·재생·경쾌 해제", "잔불·약화·노출·고정 해제", true),
            CardDefinition.CreateInscription("utility.phase", "위상 유예", InscriptionKind.Phase,
                CardRarity.Legendary, "고정 · 적 행동 2회", "경쾌 · 다음 턴 이동 비용 0", true)
        };

        public static IReadOnlyList<CardDefinition> All => Cards;

        public static CardDefinition Find(string id)
        {
            for (int i = 0; i < Cards.Length; i++)
                if (Cards[i].Id == id) return Cards[i];
            throw new ArgumentException("Unknown spell card id.", nameof(id));
        }
    }
}

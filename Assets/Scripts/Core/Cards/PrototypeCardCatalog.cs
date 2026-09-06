using System.Collections.Generic;
using Graphaclysm.Core.Equations;

namespace Graphaclysm.Core.Cards
{
    /// <summary>
    /// Temporary in-code content catalog. Replace this adapter with validated content assets later.
    /// </summary>
    public static class PrototypeCardCatalog
    {
        private static readonly CardDefinition[] Cards =
        {
            CardDefinition.CreateBase("base.line", "직선", "0.7(x-5)", 1, BaseFunctionKind.Line),
            CardDefinition.CreateBase("base.parabola", "포물선", "0.22(x-5)^2-2", 1, BaseFunctionKind.Parabola),
            CardDefinition.CreateBase("base.sine", "파동", "2sin(1.15x)", 1, BaseFunctionKind.Sine),
            CardDefinition.CreateBase("base.absolute", "절댓값", "|x-5|-2", 1, BaseFunctionKind.Absolute),
            CardDefinition.CreateModifier("modifier.up", "상승", "+ 1", 1, EquationModifierKind.ShiftUp),
            CardDefinition.CreateModifier("modifier.down", "하강", "- 1", 1, EquationModifierKind.ShiftDown),
            CardDefinition.CreateModifier("modifier.stretch", "증폭", "x 1.5", 1,
                EquationModifierKind.Stretch, CardRarity.Uncommon),
            CardDefinition.CreateModifier("modifier.negate", "반전", "-f(x)", 1,
                EquationModifierKind.Negate, CardRarity.Uncommon),
            CardDefinition.CreateBase("base.circle", "환", "(5+3cos t, 3sin t)", 2,
                BaseFunctionKind.Circle, CardRarity.Uncommon),
            CardDefinition.CreateBase("base.rose", "오엽 장미", "r=3.1sin(5θ)", 2,
                BaseFunctionKind.Rose, CardRarity.Rare),
            CardDefinition.CreateBase("base.lissajous", "조화 매듭", "Lissajous 3:2", 2,
                BaseFunctionKind.Lissajous, CardRarity.Rare),
            CardDefinition.CreateBase("base.spiral", "이중 나선", "r=0.2+2.8t", 2,
                BaseFunctionKind.Spiral, CardRarity.Uncommon),
            CardDefinition.CreateBase("base.polyomino_t", "T-폴리모노 회로", "T-polyomino boundary", 2,
                BaseFunctionKind.PolyominoCircuit, CardRarity.Uncommon),
            CardDefinition.CreateBase("base.cubic", "삼차 왜곡", "0.08(x-5)^3-0.6(x-5)", 1,
                BaseFunctionKind.Cubic, CardRarity.Uncommon),
            CardDefinition.CreateBase("base.quartic", "사차 성배", "0.012(x-5)^4-0.35(x-5)^2+1.5", 2,
                BaseFunctionKind.Quartic, CardRarity.Rare),
            CardDefinition.CreateBase("base.polynomiograph.pentacle", "오근 수렴문", "N(z)=z-(z^5-1)/(5z^4)", 3,
                BaseFunctionKind.PolynomiographPentacle, CardRarity.Rare),
            CardDefinition.CreateBase("base.polynomiograph.octave", "팔중 성좌문", "N(z)=z-(z^8-1)/(8z^7)", 3,
                BaseFunctionKind.PolynomiographOctave, CardRarity.Rare),
            CardDefinition.CreateBase("base.polynomiograph.dodecagram", "십이각 대성식", "N(z)=z-(z^12-1)/(12z^11)", 3,
                BaseFunctionKind.PolynomiographDodecagram, CardRarity.Legendary),
            CardDefinition.CreateBase("base.hypotrochoid", "기요셰 톱니환", "(.45(2cos t+5cos 2t/3), .45(2sin t-5sin 2t/3))", 2,
                BaseFunctionKind.Hypotrochoid, CardRarity.Uncommon),
            CardDefinition.CreateBase("base.maurer_rose", "마우러 장미망", "r=3.1sin(2θ), d=71°", 2,
                BaseFunctionKind.MaurerRose, CardRarity.Rare),
            CardDefinition.CreateBase("base.harmonograph", "감쇠 조화진", "x,y=Σ A sin(ft+p)e^(-dt)", 2,
                BaseFunctionKind.Harmonograph, CardRarity.Rare),
            CardDefinition.CreateBase("base.superformula", "초월 성형식", "r=3(|cos(7φ/4)|^1.7+|sin(7φ/4)|^1.7)^(-1/.32)", 3,
                BaseFunctionKind.Superformula, CardRarity.Legendary)
        };

        public static IReadOnlyList<CardDefinition> All
        {
            get { return Cards; }
        }
    }
}

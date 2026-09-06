using System;
using Graphaclysm.Core.Equations;
using NUnit.Framework;

namespace Graphaclysm.Tests
{
    public sealed class EquationStateTests
    {
        [Test]
        public void RemoveLastStep_UndoesModifiersBeforeBaseFunction()
        {
            var equation = new EquationState();
            equation.SetBase(BaseFunctionKind.Line);
            equation.TryAddModifier(EquationModifierKind.ShiftUp);

            Assert.That(equation.TryRemoveLastStep(), Is.True);
            Assert.That(equation.HasBase, Is.True);
            Assert.That(equation.ModifierCount, Is.Zero);

            Assert.That(equation.TryRemoveLastStep(), Is.True);
            Assert.That(equation.HasBase, Is.False);
            Assert.That(equation.TryRemoveLastStep(), Is.False);
        }

        [Test]
        public void Evaluate_ThrowsWhenEquationIsEmpty()
        {
            EquationState equation = new EquationState();

            Assert.Throws<InvalidOperationException>(() => equation.Evaluate(5.0));
        }

        [Test]
        public void Evaluate_AppliesModifiersInCardOrder()
        {
            EquationState equation = new EquationState();
            equation.SetBase(BaseFunctionKind.Line);
            equation.TryAddModifier(EquationModifierKind.ShiftUp);
            equation.TryAddModifier(EquationModifierKind.Stretch);

            Assert.That(equation.Evaluate(6.0), Is.EqualTo(2.55).Within(0.0001));
            Assert.That(equation.BuildFormula(), Is.EqualTo("y = 1.5((0.7(x-5)) + 1)"));
        }

        [Test]
        public void TryAddModifier_RejectsDuplicateWithoutChangingPipeline()
        {
            EquationState equation = new EquationState();
            equation.SetBase(BaseFunctionKind.Sine);

            Assert.That(equation.TryAddModifier(EquationModifierKind.Negate), Is.True);
            Assert.That(equation.TryAddModifier(EquationModifierKind.Negate), Is.False);
            Assert.That(equation.ModifierCount, Is.EqualTo(1));
        }

        [Test]
        public void SetBase_ClearsPreviousModifiers()
        {
            EquationState equation = new EquationState();
            equation.SetBase(BaseFunctionKind.Line);
            equation.TryAddModifier(EquationModifierKind.ShiftUp);

            equation.SetBase(BaseFunctionKind.Parabola);

            Assert.That(equation.ModifierCount, Is.Zero);
            Assert.That(equation.BuildFormula(), Is.EqualTo("y = 0.22(x-5)^2-2"));
        }

        [Test]
        public void ParametricCurves_CanLoopWithoutCartesianEvaluation()
        {
            EquationState equation = new EquationState();
            equation.SetBase(BaseFunctionKind.Circle);

            equation.Sample(0.0, out double startX, out double startY);
            equation.Sample(1.0, out double endX, out double endY);

            Assert.That(equation.IsCartesianFunction, Is.False);
            Assert.That(startX, Is.EqualTo(endX).Within(0.0001));
            Assert.That(startY, Is.EqualTo(endY).Within(0.0001));
            Assert.Throws<InvalidOperationException>(() => equation.Evaluate(5.0));
        }

        [Test]
        public void VerticalModifiers_AlsoTransformParametricCurves()
        {
            EquationState equation = new EquationState();
            equation.SetBase(BaseFunctionKind.Circle);
            equation.TryAddModifier(EquationModifierKind.ShiftUp);
            equation.TryAddModifier(EquationModifierKind.Negate);

            equation.Sample(0.25, out _, out double y);

            Assert.That(y, Is.EqualTo(-4.0).Within(0.0001));
            Assert.That(equation.BuildFormula(), Does.Contain("circle"));
            Assert.That(equation.BuildFormula(), Does.Contain("y -> -y"));
        }

        [Test]
        public void Polynomiograph_IsSampledAsAFieldInsteadOfACurve()
        {
            EquationState equation = new EquationState();
            equation.SetBase(BaseFunctionKind.PolynomiographPentacle);

            PolynomiographSample sample = equation.SampleField(7.38095238095, 0.0);

            Assert.That(equation.IsFieldFunction, Is.True);
            Assert.That(sample.Converged, Is.True);
            Assert.That(sample.RootIndex, Is.Zero);
            Assert.That(equation.BuildFormula(), Does.Contain("z^5-1"));
            Assert.That(equation.FieldLineSegmentCount, Is.GreaterThan(100));
            Assert.That(
                equation.FieldLineSegmentCount,
                Is.LessThan(PolynomiographContourSet.MaximumSegments));
            Assert.Throws<InvalidOperationException>(() => equation.Sample(0.5, out _, out _));
        }

        [Test]
        public void PolynomiographFamilies_ShareEvaluatorWithDifferentRootCounts()
        {
            AssertFieldFormula(BaseFunctionKind.PolynomiographPentacle, "z^5-1");
            AssertFieldFormula(BaseFunctionKind.PolynomiographOctave, "z^8-1");
            AssertFieldFormula(BaseFunctionKind.PolynomiographDodecagram, "z^12-1");
        }

        [Test]
        public void VerticalModifier_WarpsPolynomiographDomain()
        {
            EquationState baseline = new EquationState();
            baseline.SetBase(BaseFunctionKind.PolynomiographPentacle);
            PolynomiographSample original = baseline.SampleField(7.38095238095, 0.0);

            EquationState shifted = new EquationState();
            shifted.SetBase(BaseFunctionKind.PolynomiographPentacle);
            shifted.TryAddModifier(EquationModifierKind.ShiftUp);
            PolynomiographSample transformed = shifted.SampleField(7.38095238095, 1.0);

            Assert.That(transformed.RootIndex, Is.EqualTo(original.RootIndex));
            Assert.That(transformed.IterationCount, Is.EqualTo(original.IterationCount));
        }

        private static void AssertFieldFormula(BaseFunctionKind kind, string expectedFormula)
        {
            EquationState equation = new EquationState();
            equation.SetBase(kind);

            Assert.That(equation.IsFieldFunction, Is.True);
            Assert.That(equation.SampleField(7.38095238095, 0.0).Converged, Is.True);
            Assert.That(equation.FieldLineSegmentCount, Is.GreaterThan(100));
            Assert.That(
                equation.FieldLineSegmentCount,
                Is.LessThan(PolynomiographContourSet.MaximumSegments));
            Assert.That(equation.BuildFormula(), Does.Contain(expectedFormula));
        }
    }
}

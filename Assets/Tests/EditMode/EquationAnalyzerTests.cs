using Graphaclysm.Core.Equations;
using NUnit.Framework;

namespace Graphaclysm.Tests
{
    public sealed class EquationAnalyzerTests
    {
        [Test]
        public void AbsoluteFunction_IntersectsAllPrototypeEnemyPositions()
        {
            EquationState equation = new EquationState();
            equation.SetBase(BaseFunctionKind.Absolute);

            Assert.That(EquationAnalyzer.IntersectsCircle(equation, 2.0, 1.0, 0.48, 32), Is.True);
            Assert.That(EquationAnalyzer.IntersectsCircle(equation, 5.0, -2.0, 0.48, 32), Is.True);
            Assert.That(EquationAnalyzer.IntersectsCircle(equation, 8.0, 1.0, 0.48, 32), Is.True);
        }

        [Test]
        public void IntersectionDamage_UsesSlopeBonusAndReturnsZeroOnMiss()
        {
            EquationState equation = new EquationState();
            equation.SetBase(BaseFunctionKind.Line);

            int hitDamage = EquationAnalyzer.CalculateIntersectionDamage(equation, 5.0, 0.0, 0.48);
            int missDamage = EquationAnalyzer.CalculateIntersectionDamage(equation, 5.0, 3.0, 0.48);

            Assert.That(hitDamage, Is.GreaterThan(8));
            Assert.That(missDamage, Is.Zero);
        }

        [Test]
        public void NonCartesianCurves_UseTheSameCollisionPipeline()
        {
            AssertCurveHits(BaseFunctionKind.Circle, 8.0, 0.0);
            AssertCurveHits(BaseFunctionKind.Rose, 5.0, 0.0);
            AssertCurveHits(BaseFunctionKind.Lissajous, 8.4, 0.0);
            AssertCurveHits(BaseFunctionKind.Spiral, 7.9, 0.0);
            AssertCurveHits(BaseFunctionKind.PolyominoCircuit, 5.0, 2.25);
            AssertCurveHits(BaseFunctionKind.Cubic, 5.0, 0.0);
            AssertCurveHits(BaseFunctionKind.Quartic, 5.0, 1.5);
            AssertCurveHits(BaseFunctionKind.Hypotrochoid, 8.15, 0.0);
            AssertCurveHits(BaseFunctionKind.MaurerRose, 5.0, 0.0);
            AssertCurveHits(BaseFunctionKind.Harmonograph, 6.7, 1.2124355653);
            AssertCurveHits(BaseFunctionKind.Superformula, 8.0, 0.0);
        }

        [Test]
        public void Polynomiograph_DamagesExtractedContourLines()
        {
            EquationState equation = new EquationState();
            equation.SetBase(BaseFunctionKind.PolynomiographPentacle);
            equation.GetFieldLineSegment(
                0,
                out double startX,
                out double startY,
                out double endX,
                out double endY);

            int contourDamage = EquationAnalyzer.CalculateIntersectionDamage(
                equation,
                (startX + endX) * 0.5,
                (startY + endY) * 0.5,
                0.05);

            Assert.That(contourDamage, Is.GreaterThan(0));
        }

        private static void AssertCurveHits(
            BaseFunctionKind kind,
            double targetX,
            double targetY)
        {
            EquationState equation = new EquationState();
            equation.SetBase(kind);

            int damage = EquationAnalyzer.CalculateIntersectionDamage(
                equation,
                targetX,
                targetY,
                0.48);

            Assert.That(damage, Is.GreaterThan(0));
        }
    }
}

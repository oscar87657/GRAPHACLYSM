using System;

namespace Graphaclysm.Core.Equations
{
    /// <summary>
    /// Allocation-free result of a Newton-method sample on the complex plane.
    /// </summary>
    public readonly struct PolynomiographSample
    {
        public PolynomiographSample(bool converged, int rootIndex, int iterationCount, double residual)
        {
            Converged = converged;
            RootIndex = rootIndex;
            IterationCount = iterationCount;
            Residual = residual;
        }

        public bool Converged { get; }
        public int RootIndex { get; }
        public int IterationCount { get; }
        public double Residual { get; }
    }

    /// <summary>
    /// Pure complex-number evaluator for prototype polynomiographs.
    /// Complex values are represented by scalar pairs to avoid per-sample allocations.
    /// </summary>
    public static class PolynomiographEvaluator
    {
        public const int PentacleRootCount = 5;
        public const int DefaultMaximumIterations = 28;
        public const int MinimumRootCount = 3;
        public const int MaximumRootCount = 16;

        private const double ConvergenceThresholdSquared = 0.0000000001;
        private const double MinimumDerivativeSquared = 0.00000000000001;

        public static PolynomiographSample SamplePentacle(
            double real,
            double imaginary,
            int maximumIterations = DefaultMaximumIterations)
        {
            return SampleUnityRoots(real, imaginary, PentacleRootCount, maximumIterations);
        }

        public static PolynomiographSample SampleUnityRoots(
            double real,
            double imaginary,
            int rootCount,
            int maximumIterations = DefaultMaximumIterations)
        {
            if (rootCount < MinimumRootCount || rootCount > MaximumRootCount)
            {
                throw new ArgumentOutOfRangeException(nameof(rootCount));
            }

            if (maximumIterations < 1 || maximumIterations > 128)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumIterations));
            }

            double zReal = real;
            double zImaginary = imaginary;
            double residualSquared = double.MaxValue;

            for (int iteration = 0; iteration < maximumIterations; iteration++)
            {
                Power(zReal, zImaginary, rootCount - 1,
                    out double derivativePowerReal, out double derivativePowerImaginary);
                Multiply(
                    derivativePowerReal,
                    derivativePowerImaginary,
                    zReal,
                    zImaginary,
                    out double functionPowerReal,
                    out double functionPowerImaginary);

                double functionReal = functionPowerReal - 1.0;
                double functionImaginary = functionPowerImaginary;
                residualSquared = functionReal * functionReal
                                  + functionImaginary * functionImaginary;
                if (residualSquared <= ConvergenceThresholdSquared)
                {
                    return new PolynomiographSample(
                        true,
                        FindNearestUnityRoot(zReal, zImaginary, rootCount),
                        iteration,
                        Math.Sqrt(residualSquared));
                }

                double derivativeReal = rootCount * derivativePowerReal;
                double derivativeImaginary = rootCount * derivativePowerImaginary;
                double derivativeSquared = derivativeReal * derivativeReal
                                           + derivativeImaginary * derivativeImaginary;
                if (derivativeSquared <= MinimumDerivativeSquared
                    || double.IsNaN(derivativeSquared)
                    || double.IsInfinity(derivativeSquared))
                {
                    break;
                }

                Divide(
                    functionReal,
                    functionImaginary,
                    derivativeReal,
                    derivativeImaginary,
                    derivativeSquared,
                    out double correctionReal,
                    out double correctionImaginary);
                zReal -= correctionReal;
                zImaginary -= correctionImaginary;
            }

            return new PolynomiographSample(
                false,
                -1,
                maximumIterations,
                Math.Sqrt(residualSquared));
        }

        private static int FindNearestUnityRoot(double real, double imaginary, int rootCount)
        {
            double angle = Math.Atan2(imaginary, real);
            if (angle < 0.0)
            {
                angle += Math.PI * 2.0;
            }

            int rootIndex = (int)Math.Round(
                angle * rootCount / (Math.PI * 2.0),
                MidpointRounding.AwayFromZero);
            return rootIndex % rootCount;
        }

        private static void Power(
            double real,
            double imaginary,
            int exponent,
            out double resultReal,
            out double resultImaginary)
        {
            resultReal = 1.0;
            resultImaginary = 0.0;
            for (int i = 0; i < exponent; i++)
            {
                Multiply(
                    resultReal,
                    resultImaginary,
                    real,
                    imaginary,
                    out double nextReal,
                    out double nextImaginary);
                resultReal = nextReal;
                resultImaginary = nextImaginary;
            }
        }

        private static void Multiply(
            double leftReal,
            double leftImaginary,
            double rightReal,
            double rightImaginary,
            out double resultReal,
            out double resultImaginary)
        {
            resultReal = leftReal * rightReal - leftImaginary * rightImaginary;
            resultImaginary = leftReal * rightImaginary + leftImaginary * rightReal;
        }

        private static void Divide(
            double numeratorReal,
            double numeratorImaginary,
            double denominatorReal,
            double denominatorImaginary,
            double denominatorSquared,
            out double resultReal,
            out double resultImaginary)
        {
            resultReal = (numeratorReal * denominatorReal
                          + numeratorImaginary * denominatorImaginary)
                         / denominatorSquared;
            resultImaginary = (numeratorImaginary * denominatorReal
                               - numeratorReal * denominatorImaginary)
                              / denominatorSquared;
        }
    }
}

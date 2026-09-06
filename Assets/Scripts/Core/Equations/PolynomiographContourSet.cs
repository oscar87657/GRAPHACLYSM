using System;

namespace Graphaclysm.Core.Equations
{
    /// <summary>
    /// Fixed-capacity line geometry extracted from a polynomiograph field.
    /// Basin boundaries and iteration-count level sets are converted to graph-space segments.
    /// </summary>
    public sealed class PolynomiographContourSet
    {
        public const int Columns = 64;
        public const int Rows = 48;
        public const int MaximumSegments = 8192;
        public const double DomainMinimum = 0.0;
        public const double DomainMaximum = 10.0;
        public const double RangeMinimum = -4.0;
        public const double RangeMaximum = 4.0;

        private static readonly int[] IterationLevels = { 4, 6, 8, 11, 15, 20 };

        private readonly sbyte[] rootIndices = new sbyte[Columns * Rows];
        private readonly byte[] iterationCounts = new byte[Columns * Rows];
        private readonly float[] segmentStartX = new float[MaximumSegments];
        private readonly float[] segmentStartY = new float[MaximumSegments];
        private readonly float[] segmentEndX = new float[MaximumSegments];
        private readonly float[] segmentEndY = new float[MaximumSegments];
        private readonly double[] crossingX = new double[4];
        private readonly double[] crossingY = new double[4];

        public int SegmentCount { get; private set; }
        public bool WasTruncated { get; private set; }

        public void Rebuild(EquationState equation)
        {
            if (equation == null)
            {
                throw new ArgumentNullException(nameof(equation));
            }

            if (!equation.IsFieldFunction)
            {
                throw new InvalidOperationException(
                    "Contour geometry requires a polynomiograph field.");
            }

            SegmentCount = 0;
            WasTruncated = false;
            SampleField(equation);

            double cellWidth = (DomainMaximum - DomainMinimum) / (Columns - 1);
            double cellHeight = (RangeMaximum - RangeMinimum) / (Rows - 1);
            for (int row = 0; row < Rows - 1 && !WasTruncated; row++)
            {
                double bottom = RangeMinimum + row * cellHeight;
                double top = bottom + cellHeight;
                for (int column = 0; column < Columns - 1 && !WasTruncated; column++)
                {
                    double left = DomainMinimum + column * cellWidth;
                    double right = left + cellWidth;
                    int bottomLeft = ToIndex(column, row);
                    int bottomRight = ToIndex(column + 1, row);
                    int topRight = ToIndex(column + 1, row + 1);
                    int topLeft = ToIndex(column, row + 1);

                    ExtractRootBoundary(
                        left,
                        right,
                        bottom,
                        top,
                        rootIndices[bottomLeft],
                        rootIndices[bottomRight],
                        rootIndices[topRight],
                        rootIndices[topLeft]);

                    for (int levelIndex = 0;
                         levelIndex < IterationLevels.Length && !WasTruncated;
                         levelIndex++)
                    {
                        ExtractIterationContour(
                            left,
                            right,
                            bottom,
                            top,
                            iterationCounts[bottomLeft],
                            iterationCounts[bottomRight],
                            iterationCounts[topRight],
                            iterationCounts[topLeft],
                            IterationLevels[levelIndex]);
                    }
                }
            }
        }

        public void GetSegment(
            int index,
            out double startX,
            out double startY,
            out double endX,
            out double endY)
        {
            if (index < 0 || index >= SegmentCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            startX = segmentStartX[index];
            startY = segmentStartY[index];
            endX = segmentEndX[index];
            endY = segmentEndY[index];
        }

        private void SampleField(EquationState equation)
        {
            for (int row = 0; row < Rows; row++)
            {
                double graphY = RangeMinimum
                                + (RangeMaximum - RangeMinimum) * row / (Rows - 1.0);
                for (int column = 0; column < Columns; column++)
                {
                    double graphX = DomainMinimum
                                    + (DomainMaximum - DomainMinimum) * column / (Columns - 1.0);
                    PolynomiographSample sample = equation.SampleField(graphX, graphY);
                    int index = ToIndex(column, row);
                    rootIndices[index] = sample.Converged
                        ? (sbyte)sample.RootIndex
                        : (sbyte)-1;
                    iterationCounts[index] = (byte)Math.Min(255, sample.IterationCount);
                }
            }
        }

        private void ExtractRootBoundary(
            double left,
            double right,
            double bottom,
            double top,
            sbyte bottomLeft,
            sbyte bottomRight,
            sbyte topRight,
            sbyte topLeft)
        {
            int count = 0;
            if (bottomLeft != bottomRight)
            {
                AddCrossing(ref count, (left + right) * 0.5, bottom);
            }
            if (bottomRight != topRight)
            {
                AddCrossing(ref count, right, (bottom + top) * 0.5);
            }
            if (topRight != topLeft)
            {
                AddCrossing(ref count, (left + right) * 0.5, top);
            }
            if (topLeft != bottomLeft)
            {
                AddCrossing(ref count, left, (bottom + top) * 0.5);
            }

            ConnectCrossings(count, left, right, bottom, top);
        }

        private void ExtractIterationContour(
            double left,
            double right,
            double bottom,
            double top,
            byte bottomLeft,
            byte bottomRight,
            byte topRight,
            byte topLeft,
            int level)
        {
            int count = 0;
            AddLevelCrossing(
                ref count,
                left,
                bottom,
                bottomLeft,
                right,
                bottom,
                bottomRight,
                level);
            AddLevelCrossing(
                ref count,
                right,
                bottom,
                bottomRight,
                right,
                top,
                topRight,
                level);
            AddLevelCrossing(
                ref count,
                right,
                top,
                topRight,
                left,
                top,
                topLeft,
                level);
            AddLevelCrossing(
                ref count,
                left,
                top,
                topLeft,
                left,
                bottom,
                bottomLeft,
                level);
            ConnectCrossings(count, left, right, bottom, top);
        }

        private void AddLevelCrossing(
            ref int count,
            double startX,
            double startY,
            int startValue,
            double endX,
            double endY,
            int endValue,
            int level)
        {
            if ((startValue < level) == (endValue < level) || startValue == endValue)
            {
                return;
            }

            double amount = (level - startValue) / (double)(endValue - startValue);
            AddCrossing(
                ref count,
                startX + (endX - startX) * amount,
                startY + (endY - startY) * amount);
        }

        private void AddCrossing(ref int count, double x, double y)
        {
            if (count >= crossingX.Length)
            {
                return;
            }

            crossingX[count] = x;
            crossingY[count] = y;
            count++;
        }

        private void ConnectCrossings(
            int count,
            double left,
            double right,
            double bottom,
            double top)
        {
            if (count == 2)
            {
                AddSegment(crossingX[0], crossingY[0], crossingX[1], crossingY[1]);
            }
            else if (count == 3)
            {
                double centerX = (left + right) * 0.5;
                double centerY = (bottom + top) * 0.5;
                for (int i = 0; i < count; i++)
                {
                    AddSegment(centerX, centerY, crossingX[i], crossingY[i]);
                }
            }
            else if (count == 4)
            {
                AddSegment(crossingX[0], crossingY[0], crossingX[1], crossingY[1]);
                AddSegment(crossingX[2], crossingY[2], crossingX[3], crossingY[3]);
            }
        }

        private void AddSegment(double startX, double startY, double endX, double endY)
        {
            if (SegmentCount >= MaximumSegments)
            {
                WasTruncated = true;
                return;
            }

            segmentStartX[SegmentCount] = (float)startX;
            segmentStartY[SegmentCount] = (float)startY;
            segmentEndX[SegmentCount] = (float)endX;
            segmentEndY[SegmentCount] = (float)endY;
            SegmentCount++;
        }

        private static int ToIndex(int column, int row)
        {
            return row * Columns + column;
        }
    }
}

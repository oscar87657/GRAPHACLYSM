using System;

namespace Graphaclysm.Core.Equations
{
    /// <summary>
    /// Pure mathematical queries used by combat. No collections or temporary objects are allocated.
    /// </summary>
    public static class EquationAnalyzer
    {
        public static bool IntersectsCircle(
            EquationState equation,
            double centerX,
            double centerY,
            double radius,
            int sampleCount)
        {
            if (equation == null)
            {
                throw new ArgumentNullException(nameof(equation));
            }

            if (!equation.HasBase || radius <= 0.0 || sampleCount < 2)
            {
                return false;
            }

            if (equation.IsFieldFunction)
            {
                return TryFindFieldLineIntersection(
                    equation,
                    centerX,
                    centerY,
                    radius,
                    out _,
                    out _);
            }

            return TryFindIntersectionParameter(
                equation,
                centerX,
                centerY,
                radius,
                sampleCount,
                out _);
        }

        public static int CalculateIntersectionDamage(
            EquationState equation,
            double targetX,
            double targetY,
            double hitRadius)
        {
            if (equation == null)
            {
                throw new ArgumentNullException(nameof(equation));
            }

            if (equation.IsFieldFunction)
            {
                if (!TryFindFieldLineIntersection(
                        equation,
                        targetX,
                        targetY,
                        hitRadius,
                        out double fieldDeltaX,
                        out double fieldDeltaY))
                {
                    return 0;
                }

                double fieldSlope = Math.Abs(fieldDeltaX) < 0.000001
                    ? 4.0
                    : fieldDeltaY / fieldDeltaX;
                double fieldSlopeBonus = Math.Min(4.0, Math.Abs(fieldSlope)) * 2.0;
                return 8 + (int)Math.Round(fieldSlopeBonus, MidpointRounding.AwayFromZero);
            }

            if (!TryFindIntersectionParameter(
                    equation,
                    targetX,
                    targetY,
                    hitRadius,
                    equation.CurveSegmentCount,
                    out double parameter))
            {
                return 0;
            }

            const double derivativeStep = 0.001;
            double before = Math.Max(0.0, parameter - derivativeStep);
            double after = Math.Min(1.0, parameter + derivativeStep);
            equation.Sample(before, out double beforeX, out double beforeY);
            equation.Sample(after, out double afterX, out double afterY);
            double deltaX = afterX - beforeX;
            double deltaY = afterY - beforeY;
            double slope = Math.Abs(deltaX) < 0.000001
                ? 4.0
                : deltaY / deltaX;
            double slopeBonus = Math.Min(4.0, Math.Abs(slope)) * 2.0;
            return 8 + (int)Math.Round(slopeBonus, MidpointRounding.AwayFromZero);
        }

        private static bool TryFindIntersectionParameter(
            EquationState equation,
            double centerX,
            double centerY,
            double radius,
            int sampleCount,
            out double intersectionParameter)
        {
            double radiusSquared = radius * radius;
            equation.Sample(0.0, out double previousX, out double previousY);

            for (int i = 1; i <= sampleCount; i++)
            {
                double parameter = i / (double)sampleCount;
                equation.Sample(parameter, out double currentX, out double currentY);
                double segmentStartX = previousX, segmentStartY = previousY;
                double segmentEndX = currentX, segmentEndY = currentY;
                if (!GraphSegmentClipper.ClipToField(ref segmentStartX, ref segmentStartY,
                        ref segmentEndX, ref segmentEndY))
                {
                    previousX = currentX;
                    previousY = currentY;
                    continue;
                }
                double segmentParameter;
                double distanceSquared = DistanceSquaredToSegment(
                    centerX,
                    centerY,
                    segmentStartX,
                    segmentStartY,
                    segmentEndX,
                    segmentEndY,
                    out segmentParameter);
                if (distanceSquared <= radiusSquared)
                {
                    intersectionParameter = (i - 1 + segmentParameter) / sampleCount;
                    return true;
                }

                previousX = currentX;
                previousY = currentY;
            }

            intersectionParameter = 0.0;
            return false;
        }

        private static bool TryFindFieldLineIntersection(
            EquationState equation,
            double centerX,
            double centerY,
            double radius,
            out double segmentDeltaX,
            out double segmentDeltaY)
        {
            double radiusSquared = radius * radius;
            int segmentCount = equation.FieldLineSegmentCount;
            for (int i = 0; i < segmentCount; i++)
            {
                equation.GetFieldLineSegment(
                    i,
                    out double startX,
                    out double startY,
                    out double endX,
                    out double endY);
                double distanceSquared = DistanceSquaredToSegment(
                    centerX,
                    centerY,
                    startX,
                    startY,
                    endX,
                    endY,
                    out _);
                if (distanceSquared <= radiusSquared)
                {
                    segmentDeltaX = endX - startX;
                    segmentDeltaY = endY - startY;
                    return true;
                }
            }

            segmentDeltaX = 0.0;
            segmentDeltaY = 0.0;
            return false;
        }

        private static double DistanceSquaredToSegment(
            double pointX,
            double pointY,
            double startX,
            double startY,
            double endX,
            double endY,
            out double segmentParameter)
        {
            double segmentX = endX - startX;
            double segmentY = endY - startY;
            double lengthSquared = segmentX * segmentX + segmentY * segmentY;
            if (lengthSquared <= double.Epsilon)
            {
                segmentParameter = 0.0;
                double pointDeltaX = pointX - startX;
                double pointDeltaY = pointY - startY;
                return pointDeltaX * pointDeltaX + pointDeltaY * pointDeltaY;
            }

            segmentParameter = ((pointX - startX) * segmentX
                                + (pointY - startY) * segmentY)
                               / lengthSquared;
            segmentParameter = Math.Max(0.0, Math.Min(1.0, segmentParameter));
            double closestX = startX + segmentX * segmentParameter;
            double closestY = startY + segmentY * segmentParameter;
            double deltaX = pointX - closestX;
            double deltaY = pointY - closestY;
            return deltaX * deltaX + deltaY * deltaY;
        }
    }
}

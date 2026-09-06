using System;

namespace Graphaclysm.Core.Equations
{
    /// <summary>Shared visible geometry. Rejects undefined/overflowed samples without altering the equation.</summary>
    public static class GraphSegmentClipper
    {
        public const int CurveSegments = 160;

        public static bool ClipToField(ref double x0, ref double y0, ref double x1, ref double y1)
        {
            if (!Finite(x0) || !Finite(y0) || !Finite(x1) || !Finite(y1)) return false;
            double dx = x1 - x0, dy = y1 - y0, enter = 0, leave = 1;
            if (!Edge(-dx, x0, ref enter, ref leave) || !Edge(dx, 10 - x0, ref enter, ref leave)
                || !Edge(-dy, y0 + 4, ref enter, ref leave) || !Edge(dy, 4 - y0, ref enter, ref leave)) return false;
            x1 = x0 + leave * dx; y1 = y0 + leave * dy;
            x0 += enter * dx; y0 += enter * dy;
            return true;
        }

        public static bool ClipReveal(double progress, ref double x0, ref double y0, ref double x1, ref double y1, double centerX = 5, double centerY = 0)
        {
            if (progress <= 0) return false;
            if (progress >= 1) return true;
            double dx = x1 - x0, dy = y1 - y0, ox = x0 - centerX, oy = y0 - centerY;
            double farX = Math.Max(centerX,10-centerX), farY = Math.Max(centerY+4,4-centerY);
            double radiusSquared = (farX*farX+farY*farY) * progress * progress;
            double a = dx * dx + dy * dy;
            double c = ox * ox + oy * oy - radiusSquared;
            if (a < 1e-12) return c <= 0;
            double b = 2 * (ox * dx + oy * dy);
            double discriminant = b * b - 4 * a * c;
            if (discriminant < 0) return false;
            double root = Math.Sqrt(discriminant);
            double enter = Math.Max(0, (-b - root) / (2 * a));
            double leave = Math.Min(1, (-b + root) / (2 * a));
            if (enter > leave) return false;
            x1 = x0 + leave * dx; y1 = y0 + leave * dy;
            x0 += enter * dx; y0 += enter * dy;
            return true;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && Math.Abs(value) < 1e100;
        private static bool Edge(double p, double q, ref double enter, ref double leave)
        {
            if (p == 0) return q >= 0;
            double ratio = q / p;
            if (p < 0) enter = Math.Max(enter, ratio);
            else leave = Math.Min(leave, ratio);
            return enter <= leave;
        }
    }
}

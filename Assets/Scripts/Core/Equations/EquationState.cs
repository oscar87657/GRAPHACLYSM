using System;
using System.Text;

namespace Graphaclysm.Core.Equations
{
    public enum BaseFunctionKind
    {
        Line,
        Parabola,
        Sine,
        Absolute,
        Circle,
        Rose,
        Lissajous,
        Spiral,
        PolyominoCircuit,
        Cubic,
        Quartic,
        PolynomiographPentacle,
        PolynomiographOctave,
        PolynomiographDodecagram,
        Hypotrochoid,
        MaurerRose,
        Harmonograph,
        Superformula,
        InputX,
        InputAngle,
        Calculator,
        Fragments
    }

    public enum EquationModifierKind
    {
        ShiftUp,
        ShiftDown,
        Stretch,
        Negate,
        Square,
        Sine,
        Cosine,
        AbsoluteValue,
        Cube,
        Double,
        Half
    }

    /// <summary>
    /// Reusable, allocation-free equation pipeline for combat evaluation.
    /// Formula text is generated only when the equation changes.
    /// </summary>
    public sealed class EquationState
    {
        public const int MaximumModifiers = 8;

        private static readonly double[] PolyominoCircuitX =
            { 2.75, 7.25, 7.25, 5.75, 5.75, 4.25, 4.25, 2.75, 2.75 };
        private static readonly double[] PolyominoCircuitY =
            { 2.25, 2.25, 0.75, 0.75, -2.25, -2.25, 0.75, 0.75, 2.25 };

        private readonly EquationModifierKind[] modifiers =
            new EquationModifierKind[MaximumModifiers];

        private BaseFunctionKind baseFunction;
        private bool hasBase;
        private int modifierCount;
        private PolynomiographContourSet fieldContours;
        private bool fieldContoursDirty;
        private CalculatorEquation calculator;
        private FragmentEquation fragments;
        public bool IsFragmentMode => fragments != null && baseFunction == BaseFunctionKind.Fragments;
        public FragmentEquation Fragments => fragments;
        internal void CopyDiagramTo(EquationState target, double ox, double oy, double degrees)
        {
            if (!IsFragmentMode || !HasBase) throw new InvalidOperationException("A diagram needs a fragment.");
            target.EnableFragments(); fragments.CopyDiagramTo(target.fragments, ox, oy, degrees);
            target.hasBase = true;
        }
        internal void ArrangeDiagram(double ox, double oy, double degrees) => fragments.Arrange(ox, oy, degrees);
        public void EnableFragments() { if (fragments == null) fragments = new FragmentEquation(); fragments.Clear(); baseFunction = BaseFunctionKind.Fragments; hasBase = false; modifierCount = 0; }
        public bool TryAppendFragment(FragmentKind kind, double playerX = 5, double playerY = 0) { if (!IsFragmentMode || !fragments.TryAppend(kind, playerX, playerY)) return false; hasBase = true; return true; }
        private bool traceDirty = true;
        private double traceLength;
        private readonly int[] modifierAxes = new int[MaximumModifiers];
        public bool IsCalculator => hasBase && baseFunction == BaseFunctionKind.Calculator;
        public CalculatorEquation Calculator => calculator;
        public int SelectedAxis { get; private set; } = 1;
        public int CurveSegmentCount => IsFragmentMode ? FragmentEquation.Segments : IsCalculator ? 768 : GraphSegmentClipper.CurveSegments;
        public void EnableCalculator(bool reset = false)
        {
            if (calculator == null) calculator = new CalculatorEquation();
            else if (reset) calculator.TrySet("t-3", "t-3", out _);
            hasBase = true; baseFunction = BaseFunctionKind.Calculator; modifierCount = 0; SelectedAxis = 1; traceDirty = true;
        }
        public bool TrySetCalculator(string x, string y, out string error)
        {
            error = "계산기 전투에서만 입력할 수 있습니다.";
            if (!IsCalculator || !calculator.TrySet(x, y, out error)) return false;
            traceDirty = true; return true;
        }
        public double TraceLength
        {
            get
            {
                if (!IsCalculator) return 0;
                if (!traceDirty) return traceLength;
                traceLength = 0; Sample(0, out double px, out double py);
                for (int i = 1; i <= CurveSegmentCount; i++)
                {
                    Sample(i / (double)CurveSegmentCount, out double nx, out double ny);
                    double ax = px, ay = py, bx = nx, by = ny;
                    if (GraphSegmentClipper.ClipToField(ref ax, ref ay, ref bx, ref by))
                        traceLength += Math.Sqrt((bx-ax)*(bx-ax)+(by-ay)*(by-ay));
                    px = nx; py = ny;
                }
                traceDirty = false; return traceLength;
            }
        }
        public bool TrySelectAxis(int axis)
        { if (!IsCalculator || axis < 0 || axis > 1) return false; SelectedAxis = axis; return true; }

        public bool HasBase
        {
            get { return hasBase; }
        }

        public BaseFunctionKind BaseFunction
        {
            get
            {
                if (!hasBase)
                {
                    throw new InvalidOperationException("The equation has no base function.");
                }

                return baseFunction;
            }
        }

        public int ModifierCount
        {
            get { return modifierCount; }
        }

        public bool IsCartesianFunction
        {
            get
            {
                if (!hasBase)
                {
                    return false;
                }

                switch (baseFunction)
                {
                    case BaseFunctionKind.Line:
                    case BaseFunctionKind.Parabola:
                    case BaseFunctionKind.Sine:
                    case BaseFunctionKind.Absolute:
                    case BaseFunctionKind.Cubic:
                    case BaseFunctionKind.Quartic:
                    case BaseFunctionKind.InputX:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool IsFieldFunction
        {
            get
            {
                if (!hasBase)
                {
                    return false;
                }

                switch (baseFunction)
                {
                    case BaseFunctionKind.PolynomiographPentacle:
                    case BaseFunctionKind.PolynomiographOctave:
                    case BaseFunctionKind.PolynomiographDodecagram:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public void SetBase(BaseFunctionKind function)
        {
            baseFunction = function;
            hasBase = true;
            modifierCount = 0;
            fieldContoursDirty = true;
        }

        public bool TryAddModifier(EquationModifierKind modifier)
        {
            if (!CanAddModifier(modifier))
            {
                return false;
            }

            modifiers[modifierCount] = modifier;
            traceDirty = true;
            modifierAxes[modifierCount] = SelectedAxis;
            modifierCount++;
            fieldContoursDirty = true;
            return true;
        }

        public bool IsComposed => hasBase && (baseFunction == BaseFunctionKind.InputX
            || baseFunction == BaseFunctionKind.InputAngle);

        public bool CanAddModifier(EquationModifierKind modifier)
        {
            if (!hasBase || modifierCount >= modifiers.Length
                || modifier < EquationModifierKind.ShiftUp || modifier > EquationModifierKind.Half) return false;
            // Legacy fields require invertible vertical transforms; V2 inputs are composed freely.
            return IsCalculator || IsComposed || (modifier <= EquationModifierKind.Negate && !ContainsModifier(modifier));
        }

        public bool ContainsModifier(EquationModifierKind modifier)
        {
            for (int i = 0; i < modifierCount; i++)
            {
                if (modifiers[i] == modifier)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryRemoveLastStep()
        {
            if (IsFragmentMode) { bool ok = fragments.TryUndo(); hasBase = fragments.Count > 0; return ok; }
            if (modifierCount > 0)
            {
                modifierCount--;
                traceDirty = true;
                fieldContoursDirty = true;
                return true;
            }

            if (!hasBase)
            {
                return false;
            }

            hasBase = false;
            fieldContoursDirty = true;
            return true;
        }

        public void Reset()
        {
            hasBase = false;
            modifierCount = 0;
            fieldContoursDirty = true;
        }

        public double Evaluate(double x)
        {
            if (!hasBase)
            {
                throw new InvalidOperationException("Cannot evaluate an empty equation.");
            }

            if (!IsCartesianFunction)
            {
                throw new InvalidOperationException(
                    "This curve is not a single-valued Cartesian function. Use Sample instead.");
            }

            double centered = x - 5.0;
            double value;

            switch (baseFunction)
            {
                case BaseFunctionKind.InputX:
                    value = centered;
                    break;
                case BaseFunctionKind.Line:
                    value = 0.7 * centered;
                    break;
                case BaseFunctionKind.Parabola:
                    value = 0.22 * centered * centered - 2.0;
                    break;
                case BaseFunctionKind.Sine:
                    value = 2.0 * Math.Sin(1.15 * x);
                    break;
                case BaseFunctionKind.Absolute:
                    value = Math.Abs(centered) - 2.0;
                    break;
                case BaseFunctionKind.Cubic:
                    value = 0.08 * centered * centered * centered - 0.6 * centered;
                    break;
                case BaseFunctionKind.Quartic:
                    double squared = centered * centered;
                    value = 0.012 * squared * squared - 0.35 * squared + 1.5;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return ApplyVerticalModifiers(value);
        }

        public void Sample(double normalizedParameter, out double x, out double y)
        {
            if (IsFragmentMode) { fragments.Sample(normalizedParameter, out x, out y); return; }
            if (IsCalculator)
            {
                calculator.Sample(normalizedParameter, out x, out y);
                for (int i = 0; i < modifierCount; i++)
                {
                    if (modifierAxes[i] == 0) x = CalculatorModifiers.Apply(x, modifiers[i]);
                    else y = CalculatorModifiers.Apply(y, modifiers[i]);
                }
                x += 5; return;
            }
            if (!hasBase)
            {
                throw new InvalidOperationException("Cannot sample an empty equation.");
            }

            if (normalizedParameter < 0.0 || normalizedParameter > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(normalizedParameter));
            }

            if (IsFieldFunction)
            {
                throw new InvalidOperationException(
                    "A polynomiograph is a field, not a single curve. Use SampleField instead.");
            }

            if (IsCartesianFunction)
            {
                x = normalizedParameter * 10.0;
                y = Evaluate(x);
                return;
            }

            double angle = normalizedParameter * Math.PI * 2.0;
            switch (baseFunction)
            {
                case BaseFunctionKind.InputAngle:
                    double radius = ApplyVerticalModifiers(angle);
                    x = 5.0 + radius * Math.Cos(angle);
                    y = radius * Math.Sin(angle);
                    return;
                case BaseFunctionKind.Circle:
                    x = 5.0 + 3.0 * Math.Cos(angle);
                    y = 3.0 * Math.Sin(angle);
                    break;
                case BaseFunctionKind.Rose:
                    double roseRadius = 3.1 * Math.Sin(5.0 * angle);
                    x = 5.0 + roseRadius * Math.Cos(angle);
                    y = roseRadius * Math.Sin(angle);
                    break;
                case BaseFunctionKind.Lissajous:
                    x = 5.0 + 3.4 * Math.Sin(3.0 * angle + Math.PI * 0.5);
                    y = 2.7 * Math.Sin(2.0 * angle);
                    break;
                case BaseFunctionKind.Spiral:
                    double spiralAngle = normalizedParameter * Math.PI * 4.0;
                    double spiralRadius = 0.2 + 2.8 * normalizedParameter;
                    x = 5.0 + spiralRadius * Math.Cos(spiralAngle);
                    y = spiralRadius * Math.Sin(spiralAngle);
                    break;
                case BaseFunctionKind.PolyominoCircuit:
                    SamplePolyline(
                        PolyominoCircuitX,
                        PolyominoCircuitY,
                        normalizedParameter,
                        out x,
                        out y);
                    break;
                case BaseFunctionKind.Hypotrochoid:
                    double gearAngle = normalizedParameter * Math.PI * 6.0;
                    x = 5.0 + 0.45 * (2.0 * Math.Cos(gearAngle)
                                      + 5.0 * Math.Cos(2.0 * gearAngle / 3.0));
                    y = 0.45 * (2.0 * Math.Sin(gearAngle)
                                - 5.0 * Math.Sin(2.0 * gearAngle / 3.0));
                    break;
                case BaseFunctionKind.MaurerRose:
                    SampleMaurerRose(normalizedParameter, out x, out y);
                    break;
                case BaseFunctionKind.Harmonograph:
                    double harmonicTime = normalizedParameter * 45.0;
                    x = 5.0
                        + 1.7 * Math.Sin(2.01 * harmonicTime + Math.PI * 0.5)
                        * Math.Exp(-0.012 * harmonicTime)
                        + 1.4 * Math.Sin(3.0 * harmonicTime)
                        * Math.Exp(-0.016 * harmonicTime);
                    y = 1.7 * Math.Sin(2.0 * harmonicTime)
                        * Math.Exp(-0.013 * harmonicTime)
                        + 1.4 * Math.Sin(3.02 * harmonicTime + Math.PI / 3.0)
                        * Math.Exp(-0.015 * harmonicTime);
                    break;
                case BaseFunctionKind.Superformula:
                    const double symmetry = 7.0;
                    const double exponentOne = 0.32;
                    const double exponentTwo = 1.7;
                    double superAngle = angle;
                    double cosineTerm = Math.Pow(
                        Math.Abs(Math.Cos(symmetry * superAngle / 4.0)),
                        exponentTwo);
                    double sineTerm = Math.Pow(
                        Math.Abs(Math.Sin(symmetry * superAngle / 4.0)),
                        exponentTwo);
                    double superRadius = 3.0 * Math.Pow(
                        cosineTerm + sineTerm,
                        -1.0 / exponentOne);
                    x = 5.0 + superRadius * Math.Cos(superAngle);
                    y = superRadius * Math.Sin(superAngle);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            y = ApplyVerticalModifiers(y);
        }

        public PolynomiographSample SampleField(double graphX, double graphY)
        {
            if (!IsFieldFunction)
            {
                throw new InvalidOperationException(
                    "The selected equation is not a polynomiograph field.");
            }

            double localY = ReverseVerticalModifiers(graphY);
            double real = (graphX - 5.0) * 0.42;
            double imaginary = localY * 0.52;
            int rootCount;
            switch (baseFunction)
            {
                case BaseFunctionKind.PolynomiographPentacle:
                    rootCount = 5;
                    break;
                case BaseFunctionKind.PolynomiographOctave:
                    rootCount = 8;
                    break;
                case BaseFunctionKind.PolynomiographDodecagram:
                    rootCount = 12;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return PolynomiographEvaluator.SampleUnityRoots(real, imaginary, rootCount);
        }

        public int FieldLineSegmentCount
        {
            get
            {
                EnsureFieldContours();
                return fieldContours.SegmentCount;
            }
        }

        public void GetFieldLineSegment(
            int index,
            out double startX,
            out double startY,
            out double endX,
            out double endY)
        {
            EnsureFieldContours();
            fieldContours.GetSegment(index, out startX, out startY, out endX, out endY);
        }

        private void EnsureFieldContours()
        {
            if (!IsFieldFunction)
            {
                throw new InvalidOperationException(
                    "Only polynomiograph fields contain extracted line geometry.");
            }

            if (fieldContours == null)
            {
                fieldContours = new PolynomiographContourSet();
                fieldContoursDirty = true;
            }

            if (fieldContoursDirty)
            {
                fieldContours.Rebuild(this);
                fieldContoursDirty = false;
            }
        }

        private double ApplyVerticalModifiers(double value)
        {
            for (int i = 0; i < modifierCount; i++)
            {
                switch (modifiers[i])
                {
                    case EquationModifierKind.ShiftUp:
                        value += 1.0;
                        break;
                    case EquationModifierKind.ShiftDown:
                        value -= 1.0;
                        break;
                    case EquationModifierKind.Stretch:
                        value *= 1.5;
                        break;
                    case EquationModifierKind.Negate:
                        value = -value;
                        break;
                    case EquationModifierKind.Square: value *= value; break;
                    case EquationModifierKind.Cube: value = value * value * value; break;
                    case EquationModifierKind.Sine: value = Math.Sin(value); break;
                    case EquationModifierKind.Cosine: value = Math.Cos(value); break;
                    case EquationModifierKind.AbsoluteValue: value = Math.Abs(value); break;
                    case EquationModifierKind.Double: value *= 2.0; break;
                    case EquationModifierKind.Half: value *= 0.5; break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return value;
        }

        private double ReverseVerticalModifiers(double value)
        {
            for (int i = modifierCount - 1; i >= 0; i--)
            {
                switch (modifiers[i])
                {
                    case EquationModifierKind.ShiftUp:
                        value -= 1.0;
                        break;
                    case EquationModifierKind.ShiftDown:
                        value += 1.0;
                        break;
                    case EquationModifierKind.Stretch:
                        value /= 1.5;
                        break;
                    case EquationModifierKind.Negate:
                        value = -value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return value;
        }

        public string BuildFormula()
        {
            if (IsFragmentMode) return fragments.BuildFormula();
            if (IsCalculator)
            {
                string x = calculator.XText, y = calculator.YText;
                for (int i = 0; i < modifierCount; i++)
                    if (modifierAxes[i] == 0) x = CalculatorModifiers.Wrap(x, modifiers[i]); else y = CalculatorModifiers.Wrap(y, modifiers[i]);
                return "x(t) = 5 + " + x + "\ny(t) = " + y;
            }
            if (!hasBase)
            {
                return "y = ?";
            }

            if (IsComposed) return BuildComposedFormula();

            if (!IsCartesianFunction)
            {
                return BuildParametricFormula();
            }

            StringBuilder builder = new StringBuilder(96);
            builder.Append("y = ");

            switch (baseFunction)
            {
                case BaseFunctionKind.Line:
                    builder.Append("0.7(x-5)");
                    break;
                case BaseFunctionKind.Parabola:
                    builder.Append("0.22(x-5)^2-2");
                    break;
                case BaseFunctionKind.Sine:
                    builder.Append("2sin(1.15x)");
                    break;
                case BaseFunctionKind.Absolute:
                    builder.Append("|x-5|-2");
                    break;
                case BaseFunctionKind.Cubic:
                    builder.Append("0.08(x-5)^3-0.6(x-5)");
                    break;
                case BaseFunctionKind.Quartic:
                    builder.Append("0.012(x-5)^4-0.35(x-5)^2+1.5");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            const int formulaStart = 4;
            for (int i = 0; i < modifierCount; i++)
            {
                switch (modifiers[i])
                {
                    case EquationModifierKind.ShiftUp:
                        builder.Insert(formulaStart, '(');
                        builder.Append(") + 1");
                        break;
                    case EquationModifierKind.ShiftDown:
                        builder.Insert(formulaStart, '(');
                        builder.Append(") - 1");
                        break;
                    case EquationModifierKind.Stretch:
                        builder.Insert(formulaStart, "1.5(");
                        builder.Append(')');
                        break;
                    case EquationModifierKind.Negate:
                        builder.Insert(formulaStart, "-(");
                        builder.Append(')');
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return builder.ToString();
        }

        private string BuildComposedFormula()
        {
            string expression = baseFunction == BaseFunctionKind.InputX ? "(x-5)" : "θ";
            for (int i = 0; i < modifierCount; i++)
            {
                switch (modifiers[i])
                {
                    case EquationModifierKind.ShiftUp: expression = "(" + expression + "+1)"; break;
                    case EquationModifierKind.ShiftDown: expression = "(" + expression + "-1)"; break;
                    case EquationModifierKind.Stretch: expression = "1.5(" + expression + ")"; break;
                    case EquationModifierKind.Negate: expression = "-(" + expression + ")"; break;
                    case EquationModifierKind.Square: expression = "(" + expression + ")²"; break;
                    case EquationModifierKind.Cube: expression = "(" + expression + ")³"; break;
                    case EquationModifierKind.Sine: expression = "sin(" + expression + ")"; break;
                    case EquationModifierKind.Cosine: expression = "cos(" + expression + ")"; break;
                    case EquationModifierKind.AbsoluteValue: expression = "|" + expression + "|"; break;
                    case EquationModifierKind.Double: expression = "2(" + expression + ")"; break;
                    case EquationModifierKind.Half: expression = "0.5(" + expression + ")"; break;
                }
            }
            return (baseFunction == BaseFunctionKind.InputX ? "y = " : "r = ") + expression;
        }

        private string BuildParametricFormula()
        {
            StringBuilder builder = new StringBuilder(96);
            switch (baseFunction)
            {
                case BaseFunctionKind.Circle:
                    builder.Append("C(t) = circle(3)");
                    break;
                case BaseFunctionKind.Rose:
                    builder.Append("C(t) = rose(5, 3.1)");
                    break;
                case BaseFunctionKind.Lissajous:
                    builder.Append("C(t) = Lissajous(3:2)");
                    break;
                case BaseFunctionKind.Spiral:
                    builder.Append("C(t) = spiral(2 turns)");
                    break;
                case BaseFunctionKind.PolyominoCircuit:
                    builder.Append("C(t) = T-polyomino boundary");
                    break;
                case BaseFunctionKind.PolynomiographPentacle:
                    builder.Append("N(z) = z-(z^5-1)/(5z^4)");
                    break;
                case BaseFunctionKind.PolynomiographOctave:
                    builder.Append("N(z) = z-(z^8-1)/(8z^7)");
                    break;
                case BaseFunctionKind.PolynomiographDodecagram:
                    builder.Append("N(z) = z-(z^12-1)/(12z^11)");
                    break;
                case BaseFunctionKind.Hypotrochoid:
                    builder.Append("C(t) = (5+.45(2cos t+5cos(2t/3)), .45(2sin t-5sin(2t/3)))");
                    break;
                case BaseFunctionKind.MaurerRose:
                    builder.Append("C(k) = 3.1sin(2theta_k)(cos theta_k, sin theta_k), theta_k=71k deg");
                    break;
                case BaseFunctionKind.Harmonograph:
                    builder.Append("x=5+1.7sin(2.01t+pi/2)e^-.012t+1.4sin(3t)e^-.016t; y=1.7sin(2t)e^-.013t+1.4sin(3.02t+pi/3)e^-.015t");
                    break;
                case BaseFunctionKind.Superformula:
                    builder.Append("r(phi) = 3(|cos(7phi/4)|^1.7+|sin(7phi/4)|^1.7)^(-1/.32)");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            for (int i = 0; i < modifierCount; i++)
            {
                switch (modifiers[i])
                {
                    case EquationModifierKind.ShiftUp:
                        builder.Append("  |  y + 1");
                        break;
                    case EquationModifierKind.ShiftDown:
                        builder.Append("  |  y - 1");
                        break;
                    case EquationModifierKind.Stretch:
                        builder.Append("  |  y x 1.5");
                        break;
                    case EquationModifierKind.Negate:
                        builder.Append("  |  y -> -y");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return builder.ToString();
        }

        private static void SamplePolyline(
            double[] pointsX,
            double[] pointsY,
            double normalizedParameter,
            out double x,
            out double y)
        {
            double scaled = normalizedParameter * (pointsX.Length - 1);
            int segment = Math.Min(pointsX.Length - 2, (int)scaled);
            double local = scaled - segment;
            x = pointsX[segment] + (pointsX[segment + 1] - pointsX[segment]) * local;
            y = pointsY[segment] + (pointsY[segment + 1] - pointsY[segment]) * local;
        }

        private static void SampleMaurerRose(
            double normalizedParameter,
            out double x,
            out double y)
        {
            const int segmentCount = 360;
            double scaled = normalizedParameter * segmentCount;
            int segment = Math.Min(segmentCount - 1, (int)scaled);
            double local = scaled - segment;
            GetMaurerPoint(segment, out double startX, out double startY);
            GetMaurerPoint(segment + 1, out double endX, out double endY);
            x = startX + (endX - startX) * local;
            y = startY + (endY - startY) * local;
        }

        private static void GetMaurerPoint(int index, out double x, out double y)
        {
            double angle = index * 71.0 * Math.PI / 180.0;
            double radius = 3.1 * Math.Sin(2.0 * angle);
            x = 5.0 + radius * Math.Cos(angle);
            y = radius * Math.Sin(angle);
        }
    }
}

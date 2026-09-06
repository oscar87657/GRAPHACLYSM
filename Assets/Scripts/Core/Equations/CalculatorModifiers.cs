namespace Graphaclysm.Core.Equations
{
    public static class CalculatorModifiers
    {
        public static double Apply(double v, EquationModifierKind op)
        {
            switch (op)
            {
                case EquationModifierKind.ShiftUp: return v + 1;
                case EquationModifierKind.ShiftDown: return v - 1;
                case EquationModifierKind.Stretch: case EquationModifierKind.Double: return v * 2;
                case EquationModifierKind.Negate: return -v;
                case EquationModifierKind.Square: return v * v;
                case EquationModifierKind.Cube: return v * v * v;
                case EquationModifierKind.Sine: return System.Math.Sin(v);
                case EquationModifierKind.Cosine: return System.Math.Cos(v);
                case EquationModifierKind.AbsoluteValue: return System.Math.Abs(v);
                case EquationModifierKind.Half: return v * 0.5;
                default: return v;
            }
        }
        public static string Wrap(string v, EquationModifierKind op)
        {
            switch (op)
            {
                case EquationModifierKind.ShiftUp: return "(" + v + ")+1";
                case EquationModifierKind.ShiftDown: return "(" + v + ")-1";
                case EquationModifierKind.Stretch: case EquationModifierKind.Double: return "2*(" + v + ")";
                case EquationModifierKind.Negate: return "-(" + v + ")";
                case EquationModifierKind.Square: return "sqr(" + v + ")";
                case EquationModifierKind.Cube: return "(" + v + ")³";
                case EquationModifierKind.Sine: return "sin(" + v + ")";
                case EquationModifierKind.Cosine: return "cos(" + v + ")";
                case EquationModifierKind.AbsoluteValue: return "abs(" + v + ")";
                case EquationModifierKind.Half: return "0.5*(" + v + ")";
                default: return v;
            }
        }
    }
}

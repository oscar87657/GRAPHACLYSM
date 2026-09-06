using System;

namespace Graphaclysm.Core.Equations
{
    public sealed class CalculatorEquation
    {
        private readonly ExpressionProgram x = new ExpressionProgram(), y = new ExpressionProgram();
        private readonly ExpressionProgram scratchX = new ExpressionProgram(), scratchY = new ExpressionProgram();
        public string XText => x.Text;
        public string YText => y.Text;
        public int OperationCount => x.OperationCount + y.OperationCount;
        public CalculatorEquation() { TrySet("t-3", "t-3", out _); }
        public bool TrySet(string xText, string yText, out string error)
        {
            if (!scratchX.TryCompile(xText, out error) || !scratchY.TryCompile(yText, out error)) return false;
            x.CopyFrom(scratchX); y.CopyFrom(scratchY); return true;
        }
        public void Sample(double parameter, out double px, out double py)
        { double t = parameter * Math.PI * 2; px = x.Evaluate(t); py = y.Evaluate(t); }
    }
}

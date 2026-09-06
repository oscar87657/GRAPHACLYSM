using System;
using System.Globalization;

namespace Graphaclysm.Core.Equations
{
    /// <summary>Bounded infix compiler. No reflection/eval; evaluation uses a reusable topological buffer.</summary>
    public sealed class ExpressionProgram
    {
        public const int Capacity = 64, MaximumTextLength = 128, MaximumOperations = 24;
        private enum Op { Number, Parameter, Add, Subtract, Multiply, Divide, Negate, Sin, Cos, Abs, Square }
        private struct Node { public Op Kind; public int A, B; public double Number; }
        private readonly Node[] nodes = new Node[Capacity];
        private readonly double[] values = new double[Capacity];
        private string source;
        private int cursor, count, operations, depth;
        public string Text { get; private set; } = "0";
        public int OperationCount => operations;

        public bool TryCompile(string text, out string error)
        {
            count = cursor = operations = depth = 0; source = text;
            error = "";
            if (string.IsNullOrWhiteSpace(text)) { error = "수식을 입력하세요."; return false; }
            if (text.Length > MaximumTextLength) { error = "수식은 좌표마다 128자까지입니다."; return false; }
            try
            {
                ParseSum(); Skip();
                if (cursor != source.Length) throw new FormatException("연산자나 괄호를 확인하세요.");
                Text = text; return true;
            }
            catch (FormatException ex) { count = 0; error = ex.Message; return false; }
        }

        public void CopyFrom(ExpressionProgram other)
        {
            Array.Copy(other.nodes, nodes, other.count); count = other.count; operations = other.operations; Text = other.Text;
        }

        public double Evaluate(double t)
        {
            for (int i = 0; i < count; i++)
            {
                Node n = nodes[i]; double a = values[n.A], b = values[n.B];
                switch (n.Kind)
                {
                    case Op.Number: values[i] = n.Number; break;
                    case Op.Parameter: values[i] = t; break;
                    case Op.Add: values[i] = a + b; break;
                    case Op.Subtract: values[i] = a - b; break;
                    case Op.Multiply: values[i] = a * b; break;
                    case Op.Divide: values[i] = Math.Abs(b) < 1e-8 ? double.NaN : a / b; break;
                    case Op.Negate: values[i] = -a; break;
                    case Op.Sin: values[i] = Math.Sin(a); break;
                    case Op.Cos: values[i] = Math.Cos(a); break;
                    case Op.Abs: values[i] = Math.Abs(a); break;
                    case Op.Square: values[i] = a * a; break;
                }
                if (double.IsInfinity(values[i]) || Math.Abs(values[i]) > 1e9) values[i] = double.NaN;
            }
            return count == 0 ? double.NaN : values[count - 1];
        }

        private int Add(Op kind, int a = 0, int b = 0, double number = 0)
        {
            if (count == Capacity || (kind >= Op.Add && ++operations > MaximumOperations))
                throw new FormatException("수식이 너무 복잡합니다. 연산은 좌표마다 24개까지입니다.");
            nodes[count] = new Node { Kind = kind, A = a, B = b, Number = number }; return count++;
        }
        private void Skip() { while (cursor < source.Length && char.IsWhiteSpace(source[cursor])) cursor++; }
        private bool Take(char c) { Skip(); if (cursor >= source.Length || source[cursor] != c) return false; cursor++; return true; }
        private int ParseSum()
        {
            int left = ParseProduct();
            while (true)
            {
                if (Take('+')) left = Add(Op.Add, left, ParseProduct());
                else if (Take('-') || Take('−')) left = Add(Op.Subtract, left, ParseProduct());
                else return left;
            }
        }
        private int ParseProduct()
        {
            int left = ParseAtom();
            while (true)
            {
                if (Take('*') || Take('×')) left = Add(Op.Multiply, left, ParseAtom());
                else if (Take('/') || Take('÷')) left = Add(Op.Divide, left, ParseAtom());
                else return left;
            }
        }
        private int ParseAtom()
        {
            if (++depth > 16) throw new FormatException("괄호는 16단계까지 사용할 수 있습니다.");
            int result;
            if (Take('-') || Take('−')) result = Add(Op.Negate, ParseAtom());
            else if (Take('+')) result = ParseAtom();
            else if (Take('(')) { result = ParseSum(); if (!Take(')')) throw new FormatException("닫는 괄호가 필요합니다."); }
            else
            {
                Skip(); if (cursor >= source.Length) throw new FormatException("숫자나 t가 필요합니다.");
                char c = source[cursor];
                if (c == 't' || c == 'θ') { cursor++; result = Add(Op.Parameter); }
                else if (char.IsLetter(c))
                {
                    int start = cursor; while (cursor < source.Length && char.IsLetter(source[cursor])) cursor++;
                    string name = source.Substring(start, cursor - start);
                    if (name == "pi") result = Add(Op.Number, number: Math.PI);
                    else
                    {
                        Op op;
                        switch (name) { case "sin": op = Op.Sin; break; case "cos": op = Op.Cos; break; case "abs": op = Op.Abs; break; case "sqr": op = Op.Square; break; default: throw new FormatException("sin, cos, abs, sqr, pi, t를 사용할 수 있습니다."); }
                        if (!Take('(')) throw new FormatException("함수 뒤에 괄호를 여세요.");
                        int argument = ParseSum(); if (!Take(')')) throw new FormatException("닫는 괄호가 필요합니다.");
                        result = Add(op, argument);
                    }
                }
                else
                {
                    int start = cursor; while (cursor < source.Length && (char.IsDigit(source[cursor]) || source[cursor] == '.')) cursor++;
                    if (start == cursor || !double.TryParse(source.Substring(start, cursor - start), NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture, out double number) || number > 12) throw new FormatException("숫자는 0~12 범위로 입력하세요.");
                    result = Add(Op.Number, number: number);
                }
            }
            depth--; return result;
        }
    }
}

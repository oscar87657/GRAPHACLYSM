using System;
using System.Text;

namespace Graphaclysm.Core.Equations
{
    public enum FragmentKind { Counterpoint, Orbit, Petal, Expand, Contract, Mirror, TranslateRight, TranslateDown, Square, Overtone, HomeAnchor, WestAnchor, NorthAnchor, TwinEcho, StarPetal, Surge, Ellipse, Lissajous, Epitrochoid, Limacon, Shear, PhaseOffset, ComplexCube, CometBurst, KaleidoscopeFold, ShardFracture, NebulaRibbon }

    /// <summary>Each card wraps the entire preceding periodic curve. Fixed prefix tables avoid exponential evaluation.</summary>
    public sealed class FragmentEquation
    {
        public const int Capacity = 8, Segments = 1536, MaximumFrequency = 96;
        private readonly double[] x = new double[(Capacity + 1) * Segments];
        private readonly double[] y = new double[(Capacity + 1) * Segments];
        private readonly double[] originX = new double[Capacity + 1], originY = new double[Capacity + 1];
        public double OriginX => originX[Count];
        public double OriginY => originY[Count];
        private readonly FragmentKind[] steps = new FragmentKind[Capacity];
        private readonly int[] frequencies = new int[Capacity + 1];
        public int Count { get; private set; }
        public int Frequency => frequencies[Count];
        public FragmentEquation()
        {
            frequencies[0] = 1; originX[0] = 5;
            for (int i = 0; i < Segments; i++) { double t = i * Math.PI * 2 / Segments; x[i] = 1.6 * Math.Cos(t); y[i] = 1.6 * Math.Sin(t); }
        }
        public static string Symbol(FragmentKind kind)
        {
            switch (kind)
            {
                case FragmentKind.Lissajous: return "Re□₂ + iIm□₃";
                case FragmentKind.Epitrochoid: return "□ − 0.4□₄";
                case FragmentKind.Limacon: return "(1+.55cos t)□";
                case FragmentKind.Shear: return "x + 0.7y";
                case FragmentKind.PhaseOffset: return "Re□₊π/₂ + iIm□";
                case FragmentKind.ComplexCube: return "[□]³ / 4";
                case FragmentKind.CometBurst: return "[1+.62cos7t]□";
                case FragmentKind.KaleidoscopeFold: return "fold[e⁴ⁱᵗ□]";
                case FragmentKind.ShardFracture: return "[.72+.38cos5t+.46isin6t]□";
                case FragmentKind.NebulaRibbon: return "Re□₅ + iIm□₂";
                case FragmentKind.HomeAnchor: return "O ← 나";
                case FragmentKind.WestAnchor: return "O ← −2";
                case FragmentKind.NorthAnchor: return "O ↑ 1.5";
                case FragmentKind.TwinEcho: return "□ + ¼□↶";
                case FragmentKind.StarPetal: return "cos[5t]·□";
                case FragmentKind.Surge: return "1.7[□]";
                case FragmentKind.Ellipse: return "1.4x / 0.65y";
                case FragmentKind.Counterpoint: return "□ + ½□↶";
                case FragmentKind.Orbit: return "e²ⁱᵗ[□]";
                case FragmentKind.Petal: return "cos[3t]·□";
                case FragmentKind.Expand: return "1.35[□]";
                case FragmentKind.Contract: return "0.72[□]";
                case FragmentKind.Mirror: return "[□]*";
                case FragmentKind.TranslateRight: return "[□]+0.8";
                case FragmentKind.TranslateDown: return "[□]−0.8i";
                case FragmentKind.Square: return "[□]²/2";
                default: return "□ + ⅓□↻";
            }
        }
        public static string Rule(FragmentKind kind)
        {
            switch (kind)
            {
                case FragmentKind.Lissajous: return "Re(F(2t)) + i Im(F(3t))";
                case FragmentKind.Epitrochoid: return "F(t) − 0.4 F(4t)";
                case FragmentKind.Limacon: return "(1 + 0.55 cos(t)) × F(t)";
                case FragmentKind.Shear: return "Re(F) + 0.7 Im(F) + i Im(F)";
                case FragmentKind.PhaseOffset: return "Re(F(t + π/2)) + i Im(F(t))";
                case FragmentKind.ComplexCube: return "F(t)³ / 4";
                case FragmentKind.CometBurst: return "(1 + 0.62 cos(7t)) × F(t)";
                case FragmentKind.KaleidoscopeFold: return "회전한 F(t)를 거울축으로 접기";
                case FragmentKind.ShardFracture: return "(0.72 + 0.38cos(5t) + 0.46i sin(6t)) × F(t)";
                case FragmentKind.NebulaRibbon: return "Re(F(5t)) + i Im(F(2t))";
                case FragmentKind.HomeAnchor: return "O ← 카드 사용 시 플레이어 좌표; F 유지";
                case FragmentKind.WestAnchor: return "O.x ← max(0.5, O.x − 2); F 유지";
                case FragmentKind.NorthAnchor: return "O.y ← min(3.5, O.y + 1.5); F 유지";
                case FragmentKind.TwinEcho: return "F(t) + 0.25F(−2t)";
                case FragmentKind.StarPetal: return "(0.55 + 0.45cos(5t)) × F(t)";
                case FragmentKind.Surge: return "1.7 × F(t)";
                case FragmentKind.Ellipse: return "1.4 Re(F(t)) + 0.65i Im(F(t))";
                case FragmentKind.Counterpoint: return "F(t) + 0.5F(−3t)";
                case FragmentKind.Orbit: return "exp(2it) × F(t)";
                case FragmentKind.Petal: return "(0.65 + 0.35cos(3t)) × F(t)";
                case FragmentKind.Expand: return "1.35 × F(t)";
                case FragmentKind.Contract: return "0.72 × F(t)";
                case FragmentKind.Mirror: return "conjugate(F(t))";
                case FragmentKind.TranslateRight: return "F(t) + 0.8";
                case FragmentKind.TranslateDown: return "F(t) − 0.8i";
                case FragmentKind.Square: return "F(t)² / 2";
                default: return "F(t) + F(5t) / 3";
            }
        }
        private int NextFrequency(FragmentKind kind)
        {
            switch(kind)
            {
                case FragmentKind.Counterpoint: case FragmentKind.Lissajous: case FragmentKind.ComplexCube: return Frequency*3;
                case FragmentKind.Overtone: return Frequency*5;
                case FragmentKind.Epitrochoid: return Frequency*4;
                case FragmentKind.Orbit: return Frequency+2;
                case FragmentKind.Petal: return Frequency+3;
                case FragmentKind.Square: case FragmentKind.TwinEcho: return Frequency*2;
                case FragmentKind.StarPetal: return Frequency+5;
                case FragmentKind.Limacon: return Frequency+1;
                case FragmentKind.CometBurst: return Frequency+7;
                case FragmentKind.KaleidoscopeFold: return (Frequency+4)*2;
                case FragmentKind.ShardFracture: return Frequency+6;
                case FragmentKind.NebulaRibbon: return Frequency*5;
                default: return Frequency;
            }
        }
        public bool CanAppend(FragmentKind kind) => kind >= FragmentKind.Counterpoint && kind <= FragmentKind.NebulaRibbon && Count < Capacity && NextFrequency(kind) <= MaximumFrequency;
        public bool TryAppend(FragmentKind kind, double playerX = 5, double playerY = 0)
        {
            if (!CanAppend(kind) || double.IsNaN(playerX) || double.IsNaN(playerY) || double.IsInfinity(playerX) || double.IsInfinity(playerY)) return false;
            if (Count == 0)
            {
                originX[0] = Math.Max(.5, Math.Min(9.5, playerX));
                originY[0] = Math.Max(-3.5, Math.Min(3.5, playerY));
            }
            int previous = Count * Segments, next = previous + Segments;
            frequencies[Count + 1] = NextFrequency(kind); steps[Count] = kind;
            originX[Count+1] = originX[Count]; originY[Count+1] = originY[Count];
            if(kind == FragmentKind.HomeAnchor) {originX[Count+1] = Math.Max(.5,Math.Min(9.5,playerX)); originY[Count+1] = Math.Max(-3.5,Math.Min(3.5,playerY));}
            if(kind == FragmentKind.WestAnchor) originX[Count+1] = Math.Max(.5,originX[Count]-2);
            if(kind == FragmentKind.NorthAnchor) originY[Count+1] = Math.Min(3.5,originY[Count]+1.5);
            for (int i = 0; i < Segments; i++)
            {
                double a = x[previous + i], b = y[previous + i], nx = a, ny = b;
                double t = i * Math.PI * 2 / Segments;
                switch (kind)
                {
                    case FragmentKind.Lissajous:
                        nx=x[previous+(i*2)%Segments]; ny=y[previous+(i*3)%Segments]; break;
                    case FragmentKind.Epitrochoid:
                        int outer=(i*4)%Segments; nx-=.4*x[previous+outer];ny-=.4*y[previous+outer];break;
                    case FragmentKind.Limacon:
                        double lobe=1+.55*Math.Cos(t);nx*=lobe;ny*=lobe;break;
                    case FragmentKind.Shear: nx=a+.7*b;break;
                    case FragmentKind.PhaseOffset: nx=x[previous+(i+Segments/4)%Segments];break;
                    case FragmentKind.ComplexCube: nx=(a*a*a-3*a*b*b)/4;ny=(3*a*a*b-b*b*b)/4;break;
                    case FragmentKind.CometBurst:
                        double comet=1+.62*Math.Cos(7*t);nx*=comet;ny*=comet;break;
                    case FragmentKind.KaleidoscopeFold:
                        double kc=Math.Cos(4*t),ks=Math.Sin(4*t);
                        nx=kc*a-ks*b;ny=Math.Abs(ks*a+kc*b)-.72;break;
                    case FragmentKind.ShardFracture:
                        double shard=.72+.38*Math.Cos(5*t),bend=.46*Math.Sin(6*t);
                        nx=shard*a-bend*b;ny=shard*b+bend*a;break;
                    case FragmentKind.NebulaRibbon:
                        nx=x[previous+(i*5)%Segments];ny=y[previous+(i*2)%Segments];break;
                    case FragmentKind.TwinEcho:
                        int twin = (Segments - i * 2 % Segments) % Segments;
                        nx += .25*x[previous+twin]; ny += .25*y[previous+twin]; break;
                    case FragmentKind.StarPetal:
                        double star = .55 + .45*Math.Cos(5*t); nx *= star; ny *= star; break;
                    case FragmentKind.Surge: nx *= 1.7; ny *= 1.7; break;
                    case FragmentKind.Ellipse: nx *= 1.4; ny *= .65; break;
                    case FragmentKind.Counterpoint:
                        int reverse = (Segments - i * 3 % Segments) % Segments;
                        nx += 0.5 * x[previous + reverse]; ny += 0.5 * y[previous + reverse]; break;
                    case FragmentKind.Overtone:
                        int faster = i * 5 % Segments;
                        nx += x[previous + faster] / 3; ny += y[previous + faster] / 3; break;
                    case FragmentKind.Orbit:
                        double c = Math.Cos(2*t), s = Math.Sin(2*t); nx = c*a-s*b; ny = s*a+c*b; break;
                    case FragmentKind.Petal:
                        double radius = 0.65 + 0.35 * Math.Cos(3*t); nx *= radius; ny *= radius; break;
                    case FragmentKind.Expand: nx *= 1.35; ny *= 1.35; break;
                    case FragmentKind.Contract: nx *= 0.72; ny *= 0.72; break;
                    case FragmentKind.Mirror: ny = -ny; break;
                    case FragmentKind.TranslateRight: nx += 0.8; break;
                    case FragmentKind.TranslateDown: ny -= 0.8; break;
                    case FragmentKind.Square: nx = (a*a-b*b)/2; ny = a*b; break;
                }
                x[next+i] = nx; y[next+i] = ny;
            }
            Count++; return true;
        }
        public bool TryUndo() { if (Count == 0) return false; Count--; return true; }
        public void Clear() { Count = 0; }
        public void Sample(double normalized, out double px, out double py)
        {
            double position = (normalized - Math.Floor(normalized)) * Segments;
            int index = (int)position, offset = Count * Segments; double f = position-index;
            int next = (index+1)%Segments;
            px = OriginX + x[offset+index] + (x[offset+next]-x[offset+index])*f;
            py = OriginY + y[offset+index] + (y[offset+next]-y[offset+index])*f;
        }
        public string BuildFormula()
        {
            var text = new StringBuilder("F₀(t) = 1.6 exp(it)\n");
            for (int i = 0; i < Count; i++) text.Append(i+1).Append("  ").Append(Rule(steps[i])).Append(i == Count-1 ? "" : "\n");
            text.Append("\n전장 = O + F(t); O = (").Append(OriginX.ToString("0.##")).Append(", ").Append(OriginY.ToString("0.##")).Append(")");
            return text.ToString();
        }
    }
}

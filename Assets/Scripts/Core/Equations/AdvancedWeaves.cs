using System;
namespace Graphaclysm.Core.Equations
{
    // Immutable operation profiles. Different periods reuse a mathematical family, not a renamed translation.
    public static class AdvancedWeaves
    {
        public const int Count=64;
        private static readonly WeaveProfile[] profiles={
            new WeaveProfile(0,3,0,1,"삼엽의 숨","이전 도안을 3갈래 꽃잎으로 부풀리기","꽃잎 3"),
            new WeaveProfile(0,4,0,1,"사엽의 잔","이전 도안을 4갈래 꽃잎으로 부풀리기","꽃잎 4"),
            new WeaveProfile(0,6,0,1,"여섯 꽃의 밤","이전 도안을 6갈래 꽃잎으로 부풀리기","꽃잎 6"),
            new WeaveProfile(0,8,0,1,"팔엽 유리꽃","이전 도안을 8갈래 꽃잎으로 부풀리기","꽃잎 8"),
            new WeaveProfile(1,2,0,2,"공전 화관","전체를 회전하며 2겹 화관으로 펼치기","화관 2"),
            new WeaveProfile(1,3,0,2,"달맞이 회랑","전체를 회전하며 3겹 화관으로 펼치기","화관 3"),
            new WeaveProfile(1,4,0,2,"별의 소용돌이","전체를 회전하며 4겹 화관으로 펼치기","화관 4"),
            new WeaveProfile(1,5,0,2,"나선 왕관","전체를 회전하며 5겹 화관으로 펼치기","화관 5"),
            new WeaveProfile(2,3,2,3,"갈라진 혜성","3겹 반경 파동과 2겹 비틀림을 함께 걸기","파쇄 3:2"),
            new WeaveProfile(2,4,3,3,"유리 폭풍","4겹 반경 파동과 3겹 비틀림을 함께 걸기","파쇄 4:3"),
            new WeaveProfile(2,5,4,3,"만조의 파편","5겹 반경 파동과 4겹 비틀림을 함께 걸기","파쇄 5:4"),
            new WeaveProfile(2,7,5,3,"칠흑의 파쇄우","7겹 반경 파동과 5겹 비틀림을 함께 걸기","파쇄 7:5"),
            new WeaveProfile(3,2,0,2,"뒤따르는 별","역행 도안 2바퀴를 회전시켜 원본에 겹치기","역행 2"),
            new WeaveProfile(3,3,0,2,"거꾸로 핀 밤","역행 도안 3바퀴를 회전시켜 원본에 겹치기","역행 3"),
            new WeaveProfile(3,4,0,2,"역행의 조류","역행 도안 4바퀴를 회전시켜 원본에 겹치기","역행 4"),
            new WeaveProfile(3,5,0,2,"다섯 번째 잔상","역행 도안 5바퀴를 회전시켜 원본에 겹치기","역행 5"),
            new WeaveProfile(4,2,3,2,"비껴가는 춤","가로·세로에 2:3 교차 궤적을 섞기","교차 2:3"),
            new WeaveProfile(4,3,4,2,"엇갈린 실","가로·세로에 3:4 교차 궤적을 섞기","교차 3:4"),
            new WeaveProfile(4,4,5,2,"교차하는 바람","가로·세로에 4:5 교차 궤적을 섞기","교차 4:5"),
            new WeaveProfile(4,5,7,2,"일곱 길의 매듭","가로·세로에 5:7 교차 궤적을 섞기","교차 5:7"),
            new WeaveProfile(5,2,0,3,"쌍익의 불꽃","세로 궤적을 두 번 짜고 2겹 날개로 펼치기","나비 2"),
            new WeaveProfile(5,3,0,3,"나비의 장막","세로 궤적을 두 번 짜고 3겹 날개로 펼치기","나비 3"),
            new WeaveProfile(5,4,0,3,"접힌 은하","세로 궤적을 두 번 짜고 4겹 날개로 펼치기","나비 4"),
            new WeaveProfile(5,5,0,3,"다섯 날개의 꿈","세로 궤적을 두 번 짜고 5겹 날개로 펼치기","나비 5"),
            new WeaveProfile(6,1,0,2,"외로운 공전","전체를 1번 공전시키고 역행 잔상을 남기기","위성 1"),
            new WeaveProfile(6,2,0,2,"이중 천구","전체를 2번 공전시키고 역행 잔상을 남기기","위성 2"),
            new WeaveProfile(6,3,0,2,"세 겹의 위성","전체를 3번 공전시키고 역행 잔상을 남기기","위성 3"),
            new WeaveProfile(6,4,0,2,"사중 관측륜","전체를 4번 공전시키고 역행 잔상을 남기기","위성 4"),
            new WeaveProfile(7,1,0,3,"깨진 거울꽃","제곱한 도안에 1겹 역행 잔상을 겹치기","제곱 1"),
            new WeaveProfile(7,2,0,3,"흑유리 쌍정","제곱한 도안에 2겹 역행 잔상을 겹치기","제곱 2"),
            new WeaveProfile(7,3,0,3,"세 번의 굴절","제곱한 도안에 3겹 역행 잔상을 겹치기","제곱 3"),
            new WeaveProfile(7,4,0,3,"거울 속 폭풍","제곱한 도안에 4겹 역행 잔상을 겹치기","제곱 4"),
            new WeaveProfile(8,2,1,2,"흔들리는 수면","두 축을 2:1 물결로 서로 휘감기","물결 2:1"),
            new WeaveProfile(8,3,2,2,"조석의 격자","두 축을 3:2 물결로 서로 휘감기","물결 3:2"),
            new WeaveProfile(8,5,3,2,"물결의 봉합","두 축을 5:3 물결로 서로 휘감기","물결 5:3"),
            new WeaveProfile(8,7,4,2,"일곱 물결의 문","두 축을 7:4 물결로 서로 휘감기","물결 7:4"),
            new WeaveProfile(9,3,2,2,"삼중 리본","가로 3번·세로 2번으로 전체를 다시 직조","리본 3:2"),
            new WeaveProfile(9,4,3,2,"네 겹의 비단","가로 4번·세로 3번으로 전체를 다시 직조","리본 4:3"),
            new WeaveProfile(9,5,4,2,"오중 직조","가로 5번·세로 4번으로 전체를 다시 직조","리본 5:4"),
            new WeaveProfile(9,7,5,2,"일곱 현의 서곡","가로 7번·세로 5번으로 전체를 다시 직조","리본 7:5"),
            new WeaveProfile(10,2,3,2,"잔향 레이스","원본에 2겹 정행과 3겹 역행을 포개기","레이스 2:3"),
            new WeaveProfile(10,3,4,2,"새벽의 자수","원본에 3겹 정행과 4겹 역행을 포개기","레이스 3:4"),
            new WeaveProfile(10,4,5,2,"어긋난 성가","원본에 4겹 정행과 5겹 역행을 포개기","레이스 4:5"),
            new WeaveProfile(10,5,7,2,"밤을 꿰는 실","원본에 5겹 정행과 7겹 역행을 포개기","레이스 5:7"),
            new WeaveProfile(11,2,1,3,"돌아오는 매듭","2겹 도안과 1겹 역행을 공전 매듭으로 묶기","매듭 2:1"),
            new WeaveProfile(11,3,2,3,"쌍성의 결속","3겹 도안과 2겹 역행을 공전 매듭으로 묶기","매듭 3:2"),
            new WeaveProfile(11,4,3,3,"천구 봉합","4겹 도안과 3겹 역행을 공전 매듭으로 묶기","매듭 4:3"),
            new WeaveProfile(11,5,4,3,"궤도의 미궁","5겹 도안과 4겹 역행을 공전 매듭으로 묶기","매듭 5:4"),
            new WeaveProfile(12,3,2,3,"초승의 부채","가로 3겹·세로 2겹 부채 날개로 펼치기","날개 3:2"),
            new WeaveProfile(12,4,3,3,"유리 날갯짓","가로 4겹·세로 3겹 부채 날개로 펼치기","날개 4:3"),
            new WeaveProfile(12,5,4,3,"오엽의 비상","가로 5겹·세로 4겹 부채 날개로 펼치기","날개 5:4"),
            new WeaveProfile(12,7,5,3,"은하의 날개","가로 7겹·세로 5겹 부채 날개로 펼치기","날개 7:5"),
            new WeaveProfile(13,2,0,1,"안으로 핀 꽃","이전 도안을 안팎으로 뒤집히는 2겹 꽃으로 접기","내화 2"),
            new WeaveProfile(13,3,0,1,"숨겨진 삼엽","이전 도안을 안팎으로 뒤집히는 3겹 꽃으로 접기","내화 3"),
            new WeaveProfile(13,4,0,1,"깊은 네 꽃잎","이전 도안을 안팎으로 뒤집히는 4겹 꽃으로 접기","내화 4"),
            new WeaveProfile(13,6,0,1,"육엽의 성소","이전 도안을 안팎으로 뒤집히는 6겹 꽃으로 접기","내화 6"),
            new WeaveProfile(14,2,3,3,"쌍면 프리즘","2겹 도안과 3겹 거울상을 회전시키기","프리즘 2:3"),
            new WeaveProfile(14,3,4,3,"삼면 성운","3겹 도안과 4겹 거울상을 회전시키기","프리즘 3:4"),
            new WeaveProfile(14,4,5,3,"사면 흑경","4겹 도안과 5겹 거울상을 회전시키기","프리즘 4:5"),
            new WeaveProfile(14,5,7,3,"일곱 빛의 파국","5겹 도안과 7겹 거울상을 회전시키기","프리즘 5:7"),
            new WeaveProfile(15,4,2,3,"유성의 사중주","4겹 유성 파동에 2회전 역행 꼬리를 더하기","유성 4:2"),
            new WeaveProfile(15,5,3,3,"다섯 별의 낙하","5겹 유성 파동에 3회전 역행 꼬리를 더하기","유성 5:3"),
            new WeaveProfile(15,7,4,3,"재를 품은 유성","7겹 유성 파동에 4회전 역행 꼬리를 더하기","유성 7:4"),
            new WeaveProfile(15,9,5,3,"아홉 꼬리의 밤","9겹 유성 파동에 5회전 역행 꼬리를 더하기","유성 9:5"),
        };
        public static bool IsAdvanced(FragmentKind kind)=>kind>=FragmentKind.Weave0_0 && kind<=FragmentKind.Weave15_3;
        public static WeaveProfile Get(FragmentKind kind)
        { if(!IsAdvanced(kind)) throw new ArgumentOutOfRangeException(nameof(kind)); return profiles[(int)kind-(int)FragmentKind.Weave0_0]; }
    }
    public sealed class WeaveProfile
    {
        public int Family { get; }
        public int P { get; }
        public int Q { get; }
        public int Tier { get; }
        public string Name { get; }
        public string Description { get; }
        public string Symbol { get; }
        public string Rule { get; }
        public WeaveProfile(int family,int p,int q,int tier,string name,string description,string symbol)
        {
            Family=family;P=p;Q=q;Tier=tier;Name=name;Description=description;Symbol=symbol;
            string a="F("+p+"t)",b="F("+q+"t)",reverse="F(−"+p+"t)";
            switch(family)
            {
                case 0: Rule="(.8+.45cos("+p+"t)) F(t)";break;
                case 1: Rule="exp(it) (.85+.4cos("+p+"t)) F(t)";break;
                case 2: Rule="(.8+.35cos("+p+"t)+.45i sin("+q+"t)) F(t)";break;
                case 3: Rule="F(t)+.45exp(it) "+reverse;break;
                case 4: Rule="(x(t)+.45y("+p+"t), y(t)+.35x("+q+"t))";break;
                case 5: Rule="((.8+.4cos("+p+"t)) x(t), .8y(2t))";break;
                case 6: Rule="exp("+p+"it) F(t)+.35F(−t)";break;
                case 7: Rule="F(t)²/3+.5"+reverse;break;
                case 8: Rule="(.85x+.35cos("+p+"t)y, .85y+.35sin("+q+"t)x)";break;
                case 9: Rule="Re("+a+") + i Im("+b+")";break;
                case 10: Rule="F(t)+.28"+a+"−.23F(−"+q+"t)";break;
                case 11: Rule=".7"+a+"+.45exp(it)F(−"+q+"t)";break;
                case 12: Rule="((1+.55cos("+p+"t))x, (.6+.5sin("+q+"t))y)";break;
                case 13: Rule="(.4+.65cos("+p+"t)) F(t)";break;
                case 14: Rule="exp(it)(.75"+a+"+.35conj("+b+"))";break;
                default: Rule="(.8+.35cos("+p+"t))F(t)+.35exp("+q+"it)F(−t)";break;
            }
        }
        public int NextFrequency(int f)
        {
            switch(Family)
            {
                case 0: case 13: return f+P;
                case 1: return f+P+1;
                case 2: case 8: case 12: case 15: return f+Math.Max(P,Q);
                case 3: return f*P+1;
                case 4: case 9: case 10: return f*Math.Max(P,Q);
                case 5: return Math.Max(f+P,f*2);
                case 6: return f+P;
                case 7: return f*Math.Max(2,P);
                case 11: case 14: return f*Math.Max(P,Q)+1;
                default: return f;
            }
        }
        private static int Wrap(int i) { int n=FragmentEquation.Segments; return (i%n+n)%n; }
        public void Apply(double[] xs,double[] ys,int offset,int i,out double nx,out double ny)
        {
            double t=i*Math.PI*2/FragmentEquation.Segments,a=xs[offset+i],b=ys[offset+i];
            double c=Math.Cos(t),s=Math.Sin(t),cp=Math.Cos(P*t),sq=Math.Sin(Q*t);
            double px=xs[offset+Wrap(i*P)],py=ys[offset+Wrap(i*P)];
            double qx=xs[offset+Wrap(i*Q)],qy=ys[offset+Wrap(i*Q)];
            double rx=xs[offset+Wrap(-i*P)],ry=ys[offset+Wrap(-i*P)];
            nx=a;ny=b;
            switch(Family)
            {
                case 0: nx=a*(.8+.45*cp);ny=b*(.8+.45*cp);break;
                case 1: nx=(c*a-s*b)*(.85+.4*cp);ny=(s*a+c*b)*(.85+.4*cp);break;
                case 2: nx=(.8+.35*cp)*a-.45*sq*b;ny=(.8+.35*cp)*b+.45*sq*a;break;
                case 3: nx=a+.45*(c*rx-s*ry);ny=b+.45*(s*rx+c*ry);break;
                case 4: nx=a+.45*py;ny=b+.35*qx;break;
                case 5: nx=a*(.8+.4*cp);ny=.8*ys[offset+Wrap(i*2)];break;
                case 6: double c6=Math.Cos(P*t),s6=Math.Sin(P*t);
                    nx=c6*a-s6*b+.35*xs[offset+Wrap(-i)];ny=s6*a+c6*b+.35*ys[offset+Wrap(-i)];break;
                case 7: nx=(a*a-b*b)/3+.5*rx;ny=2*a*b/3+.5*ry;break;
                case 8: nx=.85*a+.35*cp*b;ny=.85*b+.35*sq*a;break;
                case 9: nx=px;ny=qy;break;
                case 10: nx=a+.28*px-.23*xs[offset+Wrap(-i*Q)];ny=b+.28*py-.23*ys[offset+Wrap(-i*Q)];break;
                case 11: double bx=xs[offset+Wrap(-i*Q)],by=ys[offset+Wrap(-i*Q)];
                    nx=.7*px+.45*(c*bx-s*by);ny=.7*py+.45*(s*bx+c*by);break;
                case 12: nx=(1+.55*cp)*a;ny=(.6+.5*sq)*b;break;
                case 13: nx=(.4+.65*cp)*a;ny=(.4+.65*cp)*b;break;
                case 14: double ux=.75*px+.35*qx,uy=.75*py-.35*qy;
                    nx=c*ux-s*uy;ny=s*ux+c*uy;break;
                case 15: double vx=xs[offset+Wrap(-i)],vy=ys[offset+Wrap(-i)],cq=Math.Cos(Q*t);
                    nx=(.8+.35*cp)*a+.35*(cq*vx-sq*vy);ny=(.8+.35*cp)*b+.35*(sq*vx+cq*vy);break;
            }
        }
    }
}

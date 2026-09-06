using System;
using Graphaclysm.Core.Equations;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class WideV8Tests
    {
        [TestCase(FragmentKind.Lissajous)]
        [TestCase(FragmentKind.Epitrochoid)]
        [TestCase(FragmentKind.Limacon)]
        [TestCase(FragmentKind.Shear)]
        [TestCase(FragmentKind.PhaseOffset)]
        [TestCase(FragmentKind.ComplexCube)]
        public void NewFragmentWrapsPriorCurveAndPreservesAnchoredCoordinates(FragmentKind kind)
        {
            // Test both the first-card shape and a noncircular, relocated prior curve.
            foreach (bool compound in new[] { false, true })
            {
                var before = new FragmentEquation();
                if (compound)
                {
                    before.TryAppend(FragmentKind.Counterpoint);
                    before.TryAppend(FragmentKind.TranslateRight);
                    before.TryAppend(FragmentKind.HomeAnchor, 3, -1);
                }
                var expectedX = new double[24]; var expectedY = new double[24];
                for (int i = 0; i < 24; i++)
                {
                    double u = i / 24.0, t = u * Math.PI * 2;
                    Local(before, u, out double x, out double y);
                    double nx=x, ny=y;
                    switch(kind)
                    {
                        case FragmentKind.Lissajous: Local(before, 2*u, out nx, out _); Local(before, 3*u, out _, out ny); break;
                        case FragmentKind.Epitrochoid: Local(before, 4*u, out double qx, out double qy); nx-=.4*qx; ny-=.4*qy; break;
                        case FragmentKind.Limacon: nx*=1+.55*Math.Cos(t); ny*=1+.55*Math.Cos(t); break;
                        case FragmentKind.Shear: nx=x+.7*y; break;
                        case FragmentKind.PhaseOffset: Local(before, u+.25, out nx, out _); break;
                        case FragmentKind.ComplexCube:
                            double radius=Math.Sqrt(x*x+y*y), angle=Math.Atan2(y,x);
                            nx=Math.Pow(radius,3)*Math.Cos(3*angle)/4; ny=Math.Pow(radius,3)*Math.Sin(3*angle)/4; break;
                    }
                    expectedX[i]=before.OriginX+nx; expectedY[i]=before.OriginY+ny;
                }
                before.Sample(.125, out double oldX, out double oldY);
                Assert.That(before.TryAppend(kind), Is.True);
                for(int i=0;i<24;i++)
                {
                    before.Sample(i/24.0,out double x,out double y);
                    Assert.That(x,Is.EqualTo(expectedX[i]).Within(1e-8));
                    Assert.That(y,Is.EqualTo(expectedY[i]).Within(1e-8));
                }
                before.Sample(0,out double startX,out double startY); before.Sample(1,out double endX,out double endY);
                Assert.That(endX,Is.EqualTo(startX)); Assert.That(endY,Is.EqualTo(startY));
                Assert.That(before.TryUndo(),Is.True);before.Sample(.125,out double undoX,out double undoY);
                Assert.That(undoX,Is.EqualTo(oldX));Assert.That(undoY,Is.EqualTo(oldY));
            }
        }
        private static void Local(FragmentEquation f,double u,out double x,out double y)
        { f.Sample(u,out x,out y);x-=f.OriginX;y-=f.OriginY; }

        [Test] public void ShearAndLissajousOrderChangesActualGeometry()
        {
            var a=new FragmentEquation();a.TryAppend(FragmentKind.Shear);a.TryAppend(FragmentKind.Lissajous);
            var b=new FragmentEquation();b.TryAppend(FragmentKind.Lissajous);b.TryAppend(FragmentKind.Shear);
            a.Sample(.125,out double ax,out double ay);b.Sample(.125,out double bx,out double by);
            Assert.That(Math.Abs(ax-bx)+Math.Abs(ay-by),Is.GreaterThan(.1));
        }
        [TestCase(FragmentKind.Epitrochoid,3,64)]
        [TestCase(FragmentKind.Lissajous,4,81)]
        [TestCase(FragmentKind.ComplexCube,4,81)]
        public void ComplexityRejectionLeavesCurveAndUndoHistoryUntouched(FragmentKind kind,int count,int frequency)
        {
            var f=new FragmentEquation();for(int i=0;i<count;i++)Assert.That(f.TryAppend(kind),Is.True);
            f.Sample(.125,out double x,out double y);
            Assert.That(f.TryAppend(kind),Is.False);Assert.That(f.Count,Is.EqualTo(count));Assert.That(f.Frequency,Is.EqualTo(frequency));
            f.Sample(.125,out double nx,out double ny);Assert.That(nx,Is.EqualTo(x));Assert.That(ny,Is.EqualTo(y));
            Assert.That(f.TryUndo(),Is.True);Assert.That(f.TryAppend(FragmentKind.PhaseOffset),Is.True);
        }
        [Test] public void AxisAndEnvelopeTransformsKeepAUsableEightCardChain()
        {
            var f=new FragmentEquation();
            var chain=new[]{FragmentKind.HomeAnchor,FragmentKind.Lissajous,FragmentKind.Shear,FragmentKind.Limacon,FragmentKind.PhaseOffset,FragmentKind.ComplexCube,FragmentKind.Epitrochoid,FragmentKind.Contract};
            foreach(var kind in chain)Assert.That(f.TryAppend(kind),Is.True);
            Assert.That(f.Frequency,Is.EqualTo(48));Assert.That(f.Count,Is.EqualTo(8));
            for(int i=0;i<FragmentEquation.Segments;i++)
            {f.Sample((double)i/FragmentEquation.Segments,out double x,out double y);Assert.That(double.IsNaN(x)||double.IsInfinity(x)||double.IsNaN(y)||double.IsInfinity(y),Is.False);}
        }
    }
}

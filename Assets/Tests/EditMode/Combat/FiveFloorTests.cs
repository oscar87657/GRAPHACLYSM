using System;
using System.Reflection;
using Graphaclysm.Application;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Runs;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class FiveFloorTests
    {
        [TestCase(1u)] [TestCase(17u)] [TestCase(400u)]
        public void FiveFloorsReachFortyRoomsAndOldRunStaysThree(uint seed)
        {
            var f=new PrototypeGameFlow(PrototypeCharacterCatalog.All,seed);f.OpenCharacterSelection();f.TrySelectCharacter(0);
            Assert.That(f.TryStartRun(CombatApproach.Execution, false),Is.True);
            var run=f.CurrentRun;Assert.That(run.HasFiveFloors,Is.True);Assert.That(run.Map.Definition.FloorCount,Is.EqualTo(5));
            var p=new RunMapProgress(run.Map.Definition);
            for(int step=0;step<40;step++)
            {
                int next=-1;for(int i=0;i<p.Definition.NodeCount;i++)if(p.IsAvailable(i)){next=i;break;}
                Assert.That(next,Is.GreaterThanOrEqualTo(0));Assert.That(p.Definition.GetNode(next).Layer,Is.EqualTo(step));
                Assert.That(p.TryEnterNode(next),Is.True);Assert.That(p.TryCompleteActiveNode(),Is.True);
                Assert.That(p.Phase==RunMapProgressPhase.Completed,Is.EqualTo(step==39));
            }
            var save=run.CaptureSave();Assert.That(RunGameSession.TryRestore(save,out var restored,out _),Is.True);
            Assert.That(restored.HasFiveFloors,Is.True);Assert.That(restored.Map.Definition.FloorCount,Is.EqualTo(5));
            Array.Resize(ref save.Commands,8); // Before FiveFloors and later opt-ins.
            Assert.That(RunGameSession.TryRestore(save,out var old,out _),Is.True);
            Assert.That(old.HasFiveFloors,Is.False);Assert.That(old.Map.Definition.FloorCount,Is.EqualTo(3));
        }
        [TestCase(EnemyBehaviorKind.GridSentry)] [TestCase(EnemyBehaviorKind.LaneArtillerist)]
        [TestCase(EnemyBehaviorKind.LatticeArchitect)] [TestCase(EnemyBehaviorKind.EclipseEmperor)]
        public void FieldWarningsAndDamageUseSameRectangles(EnemyBehaviorKind kind)
        {
            var b=new BattleSession(new BattleDefinition(999,3,new[]{EnemyArchive.Find(kind).Create("b",6,1)},CombatArchetype.Ian,fragments:true));b.EnableStatusRules();
            var e=b.Enemies[0];var seen=new System.Collections.Generic.HashSet<int>();
            for(int turn=1;turn<=8;turn++)
            {
                if(e.Intent.Kind==EnemyIntentKind.Attack)
                {
                    seen.Add(e.FieldPattern);int safe=0,danger=0;
                    for(double x=.5;x<10;x+=.25)for(double y=-3.5;y<4;y+=.25)
                    {
                        bool expected=false;
                        for(int i=0;i<e.FieldZoneCount;i++)
                        {
                            e.GetFieldZone(i,out var l,out var bot,out var r,out var t);
                            Assert.That(l,Is.GreaterThanOrEqualTo(0));Assert.That(r,Is.LessThanOrEqualTo(10));
                            Assert.That(bot,Is.GreaterThanOrEqualTo(-4));Assert.That(t,Is.LessThanOrEqualTo(4));
                            double dx=x-Math.Max(l,Math.Min(r,x)),dy=y-Math.Max(bot,Math.Min(t,y));
                            expected|=dx*dx+dy*dy<=.48*.48;
                        }
                        Assert.That(e.IsAimingAt(x,y,.48),Is.EqualTo(expected));if(expected)danger++;else safe++;
                    }
                    Assert.That(safe,Is.GreaterThan(0));Assert.That(danger,Is.GreaterThan(0));
                    e.GetFieldZone(0,out var before,out _,out _,out _);
                    typeof(EnemyState).GetMethod("DisplaceTo",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(e,new object[]{4d,0d});
                    e.GetFieldZone(0,out var after,out _,out _,out _);Assert.That(after,Is.EqualTo(before));
                }
                int healthBefore=b.PlayerHealth;
                int expectedDamage=e.IsAimingAt(b.Tactics.X,b.Tactics.Y,TacticalCombatState.PlayerRadius)?e.Intent.Damage:0;
                Assert.That(b.TryUnravel(),Is.True);
                Assert.That(b.ResolveEnemyTurn(),Is.EqualTo(expectedDamage));
                Assert.That(healthBefore-b.PlayerHealth,Is.EqualTo(expectedDamage),"Intersections must not multiply damage.");
            }
            Assert.That(seen.Count,Is.EqualTo(e.IsBoss?3:1));
            if(e.IsBoss)
            {
                e.TakeDamage(e.Definition.MaxHealth/2+30);Assert.That(e.Enraged,Is.False);
                b.TryUnravel();b.ResolveEnemyTurn();Assert.That(e.Enraged,Is.True);
            }
        }
    }
}

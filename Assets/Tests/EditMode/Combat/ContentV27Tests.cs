using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Equations;
using Graphaclysm.Core.Relics;
using Graphaclysm.Core.Runs;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class ContentV27Tests
    {
        [TestCase(FragmentKind.VerticalWeave)] [TestCase(FragmentKind.QuarterTurn)]
        [TestCase(FragmentKind.DiagonalWeave)] [TestCase(FragmentKind.DoubleLoop)]
        public void NewTransformsWrapActualPreviousCurveAndUndoExactly(FragmentKind kind)
        {
            var f=new FragmentEquation(); f.TryAppend(FragmentKind.Limacon,4,-1);
            f.Sample(.125,out double x,out double y); f.Sample(.25,out _,out double twiceY);
            Assert.That(f.TryAppend(kind),Is.True); f.Sample(.125,out double nx,out double ny);
            double a=x-4,b=y+1;
            Assert.That(nx,Is.EqualTo(4+(kind==FragmentKind.VerticalWeave?.65*a:kind==FragmentKind.QuarterTurn?-b:a)).Within(1e-9));
            double expected=kind==FragmentKind.VerticalWeave?1.4*b:kind==FragmentKind.QuarterTurn?a:kind==FragmentKind.DiagonalWeave?b+.7*a:twiceY+1;
            Assert.That(ny,Is.EqualTo(-1+expected).Within(1e-9));
            Assert.That(f.TryUndo(),Is.True); f.Sample(.125,out nx,out ny);
            Assert.That(nx,Is.EqualTo(x)); Assert.That(ny,Is.EqualTo(y));
        }
        [Test] public void AnchorsClampPreserveShapeAndDoubleLoopHonorsFrequencyLimit()
        {
            var f=new FragmentEquation(); f.TryAppend(FragmentKind.EastAnchor,9,0);
            Assert.That(f.OriginX,Is.EqualTo(9.5)); f.Sample(0,out double x,out _);
            Assert.That(x,Is.EqualTo(11.1).Within(1e-9));
            for(int i=0;i<3;i++) f.TryAppend(FragmentKind.SouthAnchor);
            Assert.That(f.OriginY,Is.EqualTo(-3.5)); f.TryUndo(); Assert.That(f.OriginY,Is.EqualTo(-3));
            f.Clear(); for(int i=0;i<6;i++) Assert.That(f.TryAppend(FragmentKind.DoubleLoop),Is.True);
            Assert.That(f.Frequency,Is.EqualTo(64)); Assert.That(f.TryAppend(FragmentKind.DoubleLoop),Is.False);
            Assert.That(f.Count,Is.EqualTo(6));
        }
        private static EnemyBehaviorDefinition Behavior(int kind) => kind==0?EnemyBehaviorDefinition.RingSentinel():kind==1?EnemyBehaviorDefinition.CrossGunner():EnemyBehaviorDefinition.TwinMortar();
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void NewWarningGeometryMatchesActualDamageAndDoesNotRetargetOnMovement(int kind)
        {
            double[,] destinations={{4,-2},{4,-1},{5,-1},{5.5,-2},{3,-1},{3,-2.5}};
            for(int i=0;i<destinations.GetLength(0);i++)
            {
                var b=new BattleSession(new BattleDefinition(100,1,new[]{new EnemyDefinition("test","Test",6,-2,100,7,Behavior(kind))},CombatArchetype.Ian,fragments:true));
                double x=destinations[i,0],y=destinations[i,1];
                if(i!=0) Assert.That(b.TryMovePlayerTo(x,y),Is.True);
                var e=b.Enemies[0]; double radius=TacticalCombatState.PlayerRadius;
                double d=Math.Sqrt((x-6)*(x-6)+(y+2)*(y+2));
                bool expected=kind==0 ? d+radius>=EnemyState.RingInnerRadius && d-radius<=EnemyState.RingOuterRadius
                    :kind==1 ? Math.Abs(x-4)<=EnemyState.CrossHalfWidth+radius || Math.Abs(y+2)<=EnemyState.CrossHalfWidth+radius
                    :(x-4)*(x-4)+(y+2)*(y+2)<=Math.Pow(e.AimRadius+radius,2) || (x-6)*(x-6)+(y-2)*(y-2)<=Math.Pow(e.AimRadius+radius,2);
                Assert.That(e.AimX,Is.EqualTo(kind==0?6:4)); Assert.That(e.AimY,Is.EqualTo(-2));
                Assert.That(e.IsAimingAt(x,y,radius),Is.EqualTo(expected));
                Assert.That(b.TryUnravel(),Is.True); b.ResolveEnemyTurn();
                Assert.That(b.PlayerHealth,Is.EqualTo(expected?93:100),kind+" / "+i);
            }
        }
        private static BattleSession RelicBattle(RelicEffectKind effect,bool two=false)
        {
            var relics=new RunRelicCollection(); foreach(var r in FragmentRelicCatalog.All) if(r.Effect==effect) relics.TryAdd(r);
            var targets=two?new[]{new EnemyDefinition("a","a",5.6,-2,999,0),new EnemyDefinition("b","b",2.4,-2,999,0)}
                :new[]{new EnemyDefinition("a","a",5.6,-2,999,0)};
            return new BattleSession(new BattleDefinition(100,1,targets,CombatArchetype.Ian,fragments:true),100,0,0,relics:relics);
        }
        [Test] public void SoloAnchorRequiresExactlyOnePrimaryTarget()
        {
            foreach(bool two in new[]{false,true})
            {
                var b=RelicBattle(RelicEffectKind.SoloAnchor,two);
                Assert.That(b.TryPlayCard(FragmentCardCatalog.Find("frag.home"),out _),Is.True);
                b.TryBeginPlot(); var report=b.ResolvePlot(); Assert.That(report.HitCount,Is.EqualTo(two?2:1));
                Assert.That(b.Enemies[0].Statuses.Get(CombatStatusKind.Anchor),Is.EqualTo(two?0:1));
                b.ResolveEnemyTurn(); Assert.That(b.Enemies[0].Statuses.Get(CombatStatusKind.Anchor),Is.Zero);
            }
        }
        [Test] public void CondensedRuptureWaitsForPlotAndEmptyUnravelCannotFarmShield()
        {
            var b=RelicBattle(RelicEffectKind.CondenseRupture);
            b.TryPlayCard(FragmentCardCatalog.Find("frag.home"),out _);
            b.TryCondense(); b.ResolveEnemyTurn(); Assert.That(b.Enemies[0].Statuses.Get(CombatStatusKind.Rupture),Is.Zero);
            b.TryBeginPlot(); b.ResolvePlot(); Assert.That(b.Enemies[0].Statuses.Get(CombatStatusKind.Rupture),Is.EqualTo(3));
            b=RelicBattle(RelicEffectKind.UnravelShield); b.TryUnravel(); Assert.That(b.Tactics.Statuses.Get(CombatStatusKind.Shield),Is.Zero);
            b.ResolveEnemyTurn(); b.TryPlayCard(FragmentCardCatalog.Find("frag.home"),out _); b.TryUnravel();
            Assert.That(b.Tactics.Statuses.Get(CombatStatusKind.Shield),Is.EqualTo(5)); Assert.That(b.TryUnravel(),Is.False);
            b.ResolveEnemyTurn(); Assert.That(b.Tactics.Statuses.Get(CombatStatusKind.Shield),Is.Zero);
        }
        [Test] public void SharedBellRequiresSelfAndEnemyContactAndAddsOnlyOneCharge()
        {
            var b=RelicBattle(RelicEffectKind.SharedResonance);
            b.TryPlayCard(FragmentCardCatalog.Find("frag.home"),out _);
            Assert.That(b.TryMovePlayerTo(5.6,-2),Is.True); Assert.That(b.PreviewPlayerHit,Is.True);
            b.TryBeginPlot(); b.ResolvePlot(); Assert.That(b.Tactics.Resonance,Is.EqualTo(3));
            b=RelicBattle(RelicEffectKind.SharedResonance); b.TryPlayCard(FragmentCardCatalog.Find("frag.home"),out _);
            b.TryBeginPlot(); b.ResolvePlot(); Assert.That(b.Tactics.Resonance,Is.Zero);
        }
        [Test] public void ExpansionIsJournaledOptInAndOld26PayloadKeepsOldPoolsAndMap()
        {
            var run=PrototypeRunFactory.Create(302,PrototypeCharacterCatalog.All[1]);
            run.TryChooseApproach(CombatApproach.Tuning); run.TryEnableOpeningRoute(); run.TryEnableEconomy();
            var bytes=RunSaveStore.Encode(run.CaptureSave()); Array.Copy(BitConverter.GetBytes(26),0,bytes,8,4);
            using(var hash=SHA256.Create()) Array.Copy(hash.ComputeHash(bytes,0,bytes.Length-32),0,bytes,bytes.Length-32,32);
            Assert.That(RunSaveStore.TryDecode(bytes,out var old),Is.True);
            Assert.That(RunGameSession.TryRestore(old,out var restored,out _),Is.True); Assert.That(restored.HasContentExpansion,Is.False);
            Assert.That(run.TryEnableContentExpansion(),Is.True); Assert.That(run.TryEnableContentExpansion(),Is.False);
            var incompatible=RunSaveStore.Encode(run.CaptureSave()); Array.Copy(BitConverter.GetBytes(26),0,incompatible,8,4);
            using(var hash=SHA256.Create()) Array.Copy(hash.ComputeHash(incompatible,0,incompatible.Length-32),0,incompatible,incompatible.Length-32,32);
            Assert.That(RunSaveStore.TryDecode(incompatible,out _),Is.False,"v27 command must be rejected in v26 payload");
            Assert.That(RunGameSession.TryRestore(run.CaptureSave(),out restored,out _),Is.True); Assert.That(restored.HasContentExpansion,Is.True);
            Assert.That(restored.TrySelectMapNode(0),Is.True); Assert.That(restored.TryEnableContentExpansion(),Is.False);
            Assert.That(FragmentCardCatalog.Legacy.Count,Is.EqualTo(35)); Assert.That(FragmentRelicCatalog.Legacy.Count,Is.EqualTo(20));
            var kinds=new HashSet<EnemyBehaviorKind>();
            for(uint seed=1;seed<=40;seed++)
            {
                run=PrototypeRunFactory.Create(seed,PrototypeCharacterCatalog.All[1]); run.TryChooseApproach(CombatApproach.Tuning); run.TryEnableOpeningRoute(); run.TryEnableEconomy(); run.TryEnableContentExpansion();
                for(int i=0;i<run.Map.Definition.NodeCount;i++)
                { var def=run.Map.Definition.GetNode(i).Battle; if(def!=null) for(int e=0;e<def.EnemyCount;e++) kinds.Add(def.GetEnemy(e).Behavior.Kind); }
            }
            Assert.That(kinds,Does.Contain(EnemyBehaviorKind.RingSentinel)); Assert.That(kinds,Does.Contain(EnemyBehaviorKind.CrossGunner)); Assert.That(kinds,Does.Contain(EnemyBehaviorKind.TwinMortar));
        }
    }
}

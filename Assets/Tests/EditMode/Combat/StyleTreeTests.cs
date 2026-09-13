using System.Collections.Generic;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Equations;
using Graphaclysm.Core.Runs;
using NUnit.Framework;
namespace Graphaclysm.Tests.Combat
{
    public sealed class StyleTreeTests
    {
        private static RunGrowthState Growth(CombatApproach a)
        {var g=new RunGrowthState(a>=CombatApproach.Tuning?CombatArchetype.Luna:CombatArchetype.Ian,a,true);g.AddPoints(100);return g;}
        private static void Buy(RunGrowthState g,int i)
        {if(g.IsUnlocked(i))return;Buy(g,g.ParentOf(i));Assert.That(g.TryPurchase(i),Is.True,g.GetNode(i).Id);}
        private static BattleSession Battle(CombatApproach a,int node)
        {
            var g=Growth(a);Buy(g,node);
            var b=new BattleSession(new BattleDefinition(100,3,new[]{new EnemyDefinition("a","a",5.6,-2,999,0)},
                a>=CombatApproach.Tuning?CombatArchetype.Luna:CombatArchetype.Ian,fragments:true,approach:a),100,0,0,6);
            b.EnableStatusRules();b.EnableApproachUltimates();b.EnableSpecialization(g.CreateCompiledBuild());return b;
        }
        private static void Card(BattleSession b)=>Assert.That(b.TryPlayCard(CardDefinition.CreateWeaveFragment("test.plain","plain",FragmentKind.HomeAnchor,CardRarity.Common,"test",1),out _),Is.True);
        private static void Plot(BattleSession b)
        {int hp=b.Enemies[0].Health,expected=b.PreviewHealthDamage(b.Enemies[0]);Assert.That(b.TryBeginPlot(),Is.True);b.ResolvePlot();Assert.That(hp-b.Enemies[0].Health,Is.EqualTo(expected));}
        public static IEnumerable<object[]> Capstones()
        {for(int a=1;a<=4;a++)foreach(int n in new[]{3,8,13,5,10,15})yield return new object[]{(CombatApproach)a,n};}
        [TestCaseSource(nameof(Capstones))]
        public void EveryCapstoneBuysAndRunsWithMatchingDamagePreview(CombatApproach a,int node)
        {
            var b=Battle(a,node);Assert.That(b.HasRule(StyleTreeCatalog.Get(a,node).Rule),Is.True);Card(b);
            if(a==CombatApproach.Recording)Assert.That(b.TryUseDiagramAbility(5,-2),Is.True);
            if(a==CombatApproach.Observation)Assert.That(b.TryPlaceSatellite(5.6,-2),Is.True);
            if((node-1)%5==4)Assert.That(b.TryToggleUltimate(),Is.True);
            Plot(b);b.ResolveEnemyTurn();Card(b);Plot(b);
        }
        [TestCase(CombatApproach.Execution)] [TestCase(CombatApproach.Recording)]
        [TestCase(CombatApproach.Tuning)] [TestCase(CombatApproach.Observation)]
        public void AllNodesHaveRealRulesAndOnlyCapstonesConflict(CombatApproach a)
        {
            var g=Growth(a);Assert.That(g.NodeCount,Is.EqualTo(16));var ids=new HashSet<int>();
            for(int i=0;i<16;i++)
            {Assert.That(ids.Add(g.GetNode(i).CommandId),Is.True);if(i==0)continue;
                var one=Growth(a);Buy(one,i);Assert.That(one.TryRefund(i),Is.True);
                Assert.That(StyleTreeCatalog.Get(a,i).Trigger,Is.EqualTo(-1));Assert.That(StyleTreeCatalog.Get(a,i).Rule,Is.Not.EqualTo(ApproachRule.None));}
            Buy(g,3);Buy(g,7);Assert.That(g.TryPurchase(8),Is.False);Buy(g,5);
            Assert.That(g.IsUnlocked(3)&&g.IsUnlocked(5),Is.True);Assert.That(g.CanRefund(1),Is.False);
            Assert.That(g.TryRefund(3),Is.True);Assert.That(g.TryPurchase(8),Is.True);
        }
        [TestCase(CombatApproach.Execution)] [TestCase(CombatApproach.Recording)]
        [TestCase(CombatApproach.Tuning)] [TestCase(CombatApproach.Observation)]
        public void NewRunAndPurchasedPathSurviveRestore(CombatApproach a)
        {
            var f=new PrototypeGameFlow(PrototypeCharacterCatalog.All,71);f.OpenCharacterSelection();f.TrySelectCharacter(a>=CombatApproach.Tuning?1:0);Assert.That(f.TryStartRun(a, false),Is.True);
            var r=f.CurrentRun;Assert.That(r.Growth.IsStyleTree,Is.True);
            foreach(int i in new[]{1,2,3})Assert.That(r.TryPurchaseGrowthNode(i),Is.True);
            Assert.That(RunSaveStore.TryDecode(RunSaveStore.Encode(r.CaptureSave()),out var data),Is.True);
            Assert.That(RunGameSession.TryRestore(data,out var copy,out _),Is.True);
            Assert.That(copy.Growth.IsStyleTree,Is.True);Assert.That(copy.Growth.IsUnlocked(3),Is.True);Assert.That(copy.Growth.Points,Is.EqualTo(r.Growth.Points));
        }
        [Test] public void ReturnDashReallyReturnsAndTripleDashUsesThreeLanes()
        {
            var b=Battle(CombatApproach.Execution,3);double x=b.Tactics.X,y=b.Tactics.Y;
            Assert.That(b.PreviewExecutionDash(8,-2).ReturnsToOrigin,Is.True);Assert.That(b.TryUseExecutionDash(8,-2),Is.True);
            Assert.That(b.Tactics.X,Is.EqualTo(x));Assert.That(b.Tactics.Y,Is.EqualTo(y));Assert.That(b.Enemies[0].Health,Is.LessThan(999));
            var triple=Battle(CombatApproach.Execution,8);Assert.That(triple.PreviewExecutionDash(8,-2).LaneCount,Is.EqualTo(3));
        }
        [Test] public void TuningCapsChangeActionRulesWithoutUnlimitedUses()
        {
            var b=Battle(CombatApproach.Tuning,13);Card(b);
            Assert.That(b.TryUseDiagramAbility(5,-2,30),Is.True);Assert.That(b.TryUseDiagramAbility(5,-2,-30),Is.True);Assert.That(b.TryUseDiagramAbility(5,-2,30),Is.False);
            var pull=Battle(CombatApproach.Tuning,3);Card(pull);Assert.That(pull.TryUseLunaPull(5,-2),Is.True);
            Assert.That(pull.CanUseLunaPull,Is.False);Assert.That(pull.TryUseDiagramAbility(5,-2,30),Is.True);Assert.That(pull.TryUseDiagramAbility(5,-2,30),Is.False);
            var wide=Battle(CombatApproach.Tuning,8);Card(wide);Assert.That(wide.TryUseDiagramAbility(5,-2,150),Is.True);
        }
        [Test] public void SatelliteRecallMovesBodyAndDoesNotRestoreMovement()
        {var b=Battle(CombatApproach.Observation,8);Assert.That(b.TryPlaceSatellite(6,-2),Is.True);Assert.That(b.Tactics.X,Is.EqualTo(6));Assert.That(b.Tactics.HasMoved,Is.True);}
        [Test] public void UltimateEchoIsFiniteAndDoesNotGenerateAnotherEcho()
        {
            var b=Battle(CombatApproach.Execution,5);Card(b);b.TryToggleUltimate();Plot(b);b.ResolveEnemyTurn();Card(b);
            Assert.That(b.PreviewStyleDamage(b.Enemies[0]),Is.EqualTo(8));Plot(b);b.ResolveEnemyTurn();Card(b);Assert.That(b.PreviewStyleDamage(b.Enemies[0]),Is.Zero);
        }
        [Test] public void RemoteOriginProducesAnActualSecondDiagramAndDamage()
        {
            var b=Battle(CombatApproach.Observation,3);Assert.That(b.TryPlaceSatellite(7,-2),Is.True);Assert.That(b.TryToggleSatelliteOrigin(),Is.True);
            Assert.That(b.TryPlayCard(CardDefinition.CreateWeaveFragment("test.counterpoint","counterpoint",FragmentKind.Counterpoint,CardRarity.Common,"test",1),out _),Is.True);
            Assert.That(b.StylePreviewDiagram.Fragments.OriginX,Is.EqualTo(b.Tactics.X));
            Assert.That(b.Equation.Fragments.OriginX,Is.EqualTo(7));Assert.That(b.PreviewStyleDamage(b.Enemies[0]),Is.EqualTo(6));Plot(b);
            Assert.That(b.LastStyleHits,Is.EqualTo(1));
        }
        [Test] public void FollowingRecordAndMarchingUltimateChangeActualOrigins()
        {
            var b=Battle(CombatApproach.Recording,8);Card(b);b.TryUseDiagramAbility(6,-2);Plot(b);b.ResolveEnemyTurn();
            Assert.That(b.TryMovePlayerTo(3.5,-2),Is.True);Assert.That(b.RecordedDiagram.Fragments.OriginX,Is.EqualTo(3.5));
            var echo=Battle(CombatApproach.Recording,10);Card(echo);echo.TryToggleUltimate();Plot(echo);
            double x=echo.UltimateDiagram.Fragments.OriginX;echo.ResolveEnemyTurn();Card(echo);Plot(echo);
            Assert.That(echo.UltimateDiagram.Fragments.OriginX,Is.EqualTo(x+.75).Within(.0001));
        }
        [Test] public void HomeAnchorDoesNotCountAsAStillRemoteOrigin()
        {
            var b=Battle(CombatApproach.Observation,3);b.TryPlaceSatellite(7,-2);b.TryToggleSatelliteOrigin();Card(b);
            Assert.That(b.Equation.Fragments.OriginX,Is.EqualTo(b.Tactics.X));Assert.That(b.StylePreviewDiagram,Is.Null);Assert.That(b.PreviewStyleDamage(b.Enemies[0]),Is.Zero);
        }
        [Test] public void MirroredRecordUsesReflectedGeometryNotJustAnIcon()
        {
            var b=Battle(CombatApproach.Recording,3);Card(b);
            Assert.That(b.TryPlayCard(CardDefinition.CreateWeaveFragment("test.petal","petal",FragmentKind.Petal,CardRarity.Common,"test",1),out _),Is.True);
            b.Equation.Sample(.17,out double x,out double y);double ox=b.Equation.Fragments.OriginX,oy=b.Equation.Fragments.OriginY;
            Assert.That(b.TryUseDiagramAbility(5,-2),Is.True);Plot(b);
            b.RecordedDiagram.Sample(.17,out double rx,out double ry);
            Assert.That(rx,Is.EqualTo(5+x-ox).Within(.001));Assert.That(ry,Is.EqualTo(-2-(y-oy)).Within(.001));
        }
        [Test] public void SkippingExtraAssemblyDiscardsItsReservedEcho()
        {
            var b=Battle(CombatApproach.Tuning,5);Card(b);b.TryToggleUltimate();Plot(b);b.ResolveEnemyTurn();
            Assert.That(b.InExtraAssembly,Is.True);Assert.That(b.TryUnravel(),Is.True);b.ResolveEnemyTurn();Card(b);
            Assert.That(b.PreviewStyleDamage(b.Enemies[0]),Is.Zero);
        }
    }
}

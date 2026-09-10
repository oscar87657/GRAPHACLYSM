using System;
using System.Collections.Generic;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Decks;
using Graphaclysm.Core.Equations;
using Graphaclysm.Core.Relics;
using Graphaclysm.Core.Runs;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class ExpeditionV7Tests
    {
        private static CardDefinition Card(string id)=>FragmentCardCatalog.Find("frag."+id);
        private static CardDefinition[] Deck(string id="echo")
        {var d=new CardDefinition[12];for(int i=0;i<d.Length;i++)d[i]=Card(id);return d;}
        private static RunRelicCollection Relics(params RelicEffectKind[] effects)
        {
            var r=new RunRelicCollection();
            foreach(var e in effects)foreach(var item in FragmentRelicCatalog.All)if(item.Effect==e){r.TryAdd(item);break;}
            return r;
        }
        private static BattleDefinition Definition(int health=100,int enemyHp=999,double enemyX=7.4,double enemyY=0)
            =>new BattleDefinition(health,1,new[]{new EnemyDefinition("target","Target",enemyX,enemyY,enemyHp,0)},CombatArchetype.Ian,fragments:true);
        [Test] public void AnchorRelocatesWholeCurveAndFutureOperationsPreserveItsOrigin()
        {
            var b=new BattleSession(Definition());b.TryPlayCard(Card("echo"),out _);
            b.Equation.Sample(.125,out double x,out double y);
            Assert.That(b.TryMovePlayer(-1.5,0),Is.True);
            b.TryPlayCard(Card("home"),out _);b.Equation.Sample(.125,out double nx,out double ny);
            Assert.That(nx,Is.EqualTo(x-2.5).Within(1e-9));Assert.That(ny,Is.EqualTo(y-2).Within(1e-9));
            b.TryPlayCard(Card("orbit"),out _);Assert.That(b.Equation.Fragments.OriginX,Is.EqualTo(2.5));
            b.TryUndoLastCard(out _);b.TryUndoLastCard(out _);b.Equation.Sample(.125,out nx,out ny);
            Assert.That(nx,Is.EqualTo(x));Assert.That(ny,Is.EqualTo(y));
        }
        [Test] public void AnchoredGeometryUsesSameSamplesForActualEnemyHitsAndClearsAfterRelease()
        {
            var b=new BattleSession(Definition(enemyX:5.6,enemyY:-2));b.TryPlayCard(Card("home"),out _);
            Assert.That(b.PreviewDamage(b.Enemies[0]),Is.GreaterThan(0));
            b.TryBeginPlot();Assert.That(b.ResolvePlot().HitCount,Is.EqualTo(1));b.ResolveEnemyTurn();
            Assert.That(b.Equation.Fragments.OriginX,Is.EqualTo(5));Assert.That(b.Equation.Fragments.OriginY,Is.Zero);
        }
        [Test] public void DirectionalAnchorsAndUndoKeepOriginInsideField()
        {
            var f=new FragmentEquation();for(int i=0;i<4;i++)f.TryAppend(FragmentKind.WestAnchor);
            Assert.That(f.OriginX,Is.EqualTo(.5));f.TryAppend(FragmentKind.NorthAnchor);Assert.That(f.OriginY,Is.EqualTo(1.5));
            f.TryUndo();Assert.That(f.OriginY,Is.Zero);f.Clear();Assert.That(f.OriginX,Is.EqualTo(5));
        }
        [Test] public void NewCoefficientsProduceDifferentActualCurves()
        {
            var f=new FragmentEquation();f.TryAppend(FragmentKind.Surge);f.Sample(0,out double x,out _);
            Assert.That(x,Is.EqualTo(7.72).Within(1e-9));f.Clear();f.TryAppend(FragmentKind.TwinEcho);f.Sample(0,out x,out _);
            Assert.That(x,Is.EqualTo(7).Within(1e-9));f.Clear();f.TryAppend(FragmentKind.Ellipse);f.Sample(.25,out _,out double y);
            Assert.That(y,Is.EqualTo(1.04).Within(1e-9));
            f.Clear();f.TryAppend(FragmentKind.StarPetal);f.Sample(.1,out x,out y);
            Assert.That(Math.Sqrt((x-5)*(x-5)+y*y),Is.EqualTo(.16).Within(.002));
        }
        [Test] public void RevealClipsAroundRelocatedOriginAndFullyRevealsField()
        {
            double x0=1,y0=-2,x1=4,y1=-2;
            Assert.That(GraphSegmentClipper.ClipReveal(.05,ref x0,ref y0,ref x1,ref y1,2,-2),Is.True);
            Assert.That(x0,Is.GreaterThan(1));Assert.That(x1,Is.LessThan(4));
            x0=0;y0=4;x1=10;y1=4;
            Assert.That(GraphSegmentClipper.ClipReveal(1,ref x0,ref y0,ref x1,ref y1,.5,-3.5),Is.True);
        }
        [Test] public void CondensePaysEscalatingHealthEvenWithShieldAndCannotSpendLastHealth()
        {
            var b=new BattleSession(Definition(20),20,0,0,0,Relics(RelicEffectKind.StartShield));
            Assert.That(b.TryCondense(),Is.True);Assert.That(b.PlayerHealth,Is.EqualTo(18));b.ResolveEnemyTurn();
            Assert.That(b.TryCondense(),Is.True);Assert.That(b.PlayerHealth,Is.EqualTo(14));b.ResolveEnemyTurn();Assert.That(b.CanCondense,Is.False);
            var low=new BattleSession(Definition(20),2);Assert.That(low.TryCondense(),Is.False);Assert.That(low.PlayerHealth,Is.EqualTo(2));
            Assert.That(low.TryUnravel(),Is.True);
        }
        [Test] public void CondenseCapsDrawAndConsumesOverflowWithoutApplyingTurnDrawRelics()
        {
            var g=new BattleGameSession(new BattleSession(Definition()),new DeckSession(Deck("overtone"),8,new XorShiftRandom(1),true,5),5);
            g.TryPlayHandCard(0,out _,out _);g.TryPlayHandCard(0,out _,out _);
            Assert.That(g.CondenseDrawCount,Is.EqualTo(2));g.TryCondense();g.ResolveEnemyTurn();
            Assert.That(g.Deck.HandCount,Is.EqualTo(5));Assert.That(g.Battle.PendingDrawBonus,Is.Zero);Assert.That(g.NextDrawCount,Is.EqualTo(5));
            Assert.That(g.TryUndoLastPlayedCard(out _),Is.False);
        }
        [Test] public void UtilityFragmentsDoNotGenerateUniformDamageAndOffenseIsCapped()
        {
            var b=new BattleSession(Definition());b.TryPlayCard(Card("home"),out _);b.TryPlayCard(Card("contract"),out _);
            Assert.That(b.WeaveDamageBonus,Is.Zero);
            for(int i=0;i<6;i++)b.TryPlayCard(Card("surge"),out _);
            Assert.That(b.WeaveDamageBonus,Is.EqualTo(10));b.TryUndoLastCard(out _);Assert.That(b.WeaveDamageBonus,Is.EqualTo(10));
        }
        [Test] public void OpeningRelicsApplyThroughRealRunAndResetWithoutAccumulation()
        {
            var run=PrototypeRunFactory.Create(12);foreach(var r in FragmentRelicCatalog.All)
                if(r.Effect==RelicEffectKind.StartShield||r.Effect==RelicEffectKind.StartResonance)run.Relics.TryAdd(r);
            run.TrySelectMapNode(0);var b=run.CurrentBattle.Battle;
            Assert.That(b.Tactics.Resonance,Is.EqualTo(1));Assert.That(b.Tactics.Statuses.Get(CombatStatusKind.Shield),Is.EqualTo(6));
            b.Reset();Assert.That(b.Tactics.Resonance,Is.EqualTo(1));Assert.That(b.Tactics.Statuses.Get(CombatStatusKind.Shield),Is.EqualTo(6));
        }
        [Test] public void FirstPlotAndShortWeaveRelicsAffectPreviewAndOnlyCorrectReleases()
        {
            var plain=new BattleSession(Definition());plain.TryPlayCard(Card("echo"),out _);
            int baseline=plain.PreviewDamage(plain.Enemies[0]);
            var b=new BattleSession(Definition(),100,0,0,0,Relics(RelicEffectKind.FirstPlotDamage,RelicEffectKind.ShortWeaveDamage));
            b.TryPlayCard(Card("echo"),out _);Assert.That(b.PreviewDamage(b.Enemies[0]),Is.EqualTo(baseline+7));
            b.TryBeginPlot();Assert.That(b.ResolvePlot().TotalDamage,Is.EqualTo(baseline+7));b.ResolveEnemyTurn();
            b.TryPlayCard(Card("echo"),out _);Assert.That(b.PreviewDamage(b.Enemies[0]),Is.EqualTo(baseline+3));
        }
        [Test] public void SelfHitAndMovementRelicsTriggerOnReleaseNotUndoableMovement()
        {
            var b=new BattleSession(Definition(),100,0,0,0,Relics(RelicEffectKind.SelfShield,RelicEffectKind.MovedPlotShield));
            b.TryMovePlayer(0,1.5);b.TryUndoMove();Assert.That(b.Tactics.Statuses.Get(CombatStatusKind.Shield),Is.Zero);
            b.TryPlayCard(Card("down"),out _);b.TryBeginPlot();b.ResolvePlot();Assert.That(b.Tactics.Statuses.Get(CombatStatusKind.Shield),Is.EqualTo(5));
            b.ResolveEnemyTurn();b.TryMovePlayer(-1.5,0);b.TryPlayCard(Card("echo"),out _);b.TryBeginPlot();b.ResolvePlot();
            Assert.That(b.Tactics.Statuses.Get(CombatStatusKind.Shield),Is.GreaterThanOrEqualTo(3));
        }
        [Test] public void IntermediateBossContinuesWithRelicWhileThirdBossCompletesRun()
        {
            var nodes=new RunMapNodeDefinition[3];for(int i=0;i<3;i++)nodes[i]=new RunMapNodeDefinition("b"+i,"Boss",i,0,RunNodeKind.Boss,Definition(enemyHp:1),i<2?new[]{i+1}:new int[0]);
            var run=new RunGameSession(new RunMapDefinition(nodes,1),Deck(),FragmentCardCatalog.All,5,13,FragmentRelicCatalog.All);
            foreach(var r in FragmentRelicCatalog.All)if(r.Effect==RelicEffectKind.VictoryResonance)run.Relics.TryAdd(r);
            for(int i=0;i<3;i++)
            {
                Assert.That(run.CurrentFloor,Is.EqualTo(i+1));Assert.That(run.TrySelectMapNode(i),Is.True);
                run.TryPlayHandCard(0,out _,out _);run.TryBeginPlot();run.ResolvePlot();
                Assert.That(run.Resonance,Is.EqualTo(Math.Min(6,(i+1)*2)));
                if(i<2){Assert.That(run.Phase,Is.EqualTo(RunPhase.RelicReward));int choice=run.RelicRewardOptions[0].Effect==RelicEffectKind.StartResonance?1:0;Assert.That(run.TrySelectRelicReward(choice),Is.True);Assert.That(run.Phase,Is.EqualTo(RunPhase.MapSelection));}
                else Assert.That(run.Phase,Is.EqualTo(RunPhase.Completed));
            }
            Assert.That(run.Deck.Count,Is.EqualTo(12));Assert.That(run.Relics.Count,Is.EqualTo(3));
        }
        [Test] public void LongWeaveRelicWaitsForSixFragments()
        {
            var b=new BattleSession(Definition(enemyX:5.6,enemyY:-2),100,0,0,0,Relics(RelicEffectKind.LongWeaveDamage));
            for(int i=0;i<5;i++)b.TryPlayCard(Card("home"),out _);
            int five=b.PreviewDamage(b.Enemies[0]);Assert.That(five,Is.GreaterThan(0));
            b.TryPlayCard(Card("home"),out _);Assert.That(b.PreviewDamage(b.Enemies[0]),Is.EqualTo(five+4));
            b.TryUndoLastCard(out _);Assert.That(b.PreviewDamage(b.Enemies[0]),Is.EqualTo(five));
        }
        [Test] public void ExpandedCatalogsHaveUniqueIdsAndAllFloorsHaveStrongerBosses()
        {
            var ids=new HashSet<string>();foreach(var c in FragmentCardCatalog.All)Assert.That(ids.Add(c.Id),Is.True);
            Assert.That(ids.Count,Is.EqualTo(31));ids.Clear();foreach(var r in FragmentRelicCatalog.All)Assert.That(ids.Add(r.Id),Is.True);Assert.That(ids.Count,Is.EqualTo(20));
            var map=DungeonGenerator.Generate(21,PrototypeCharacterCatalog.All[0]);int hp=0,bosses=0;
            for(int i=0;i<map.NodeCount;i++){var node=map.GetNode(i);if(node.Kind!=RunNodeKind.Boss)continue;int sum=0;for(int j=0;j<node.Battle.EnemyCount;j++)sum+=node.Battle.GetEnemy(j).MaxHealth;Assert.That(sum,Is.GreaterThan(hp));hp=sum;bosses++;}
            Assert.That(bosses,Is.EqualTo(3));
        }
    }
}

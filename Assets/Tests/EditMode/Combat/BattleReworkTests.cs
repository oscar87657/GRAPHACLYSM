using System;
using System.Collections.Generic;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Decks;
using Graphaclysm.Core.Equations;
using Graphaclysm.Core.Runs;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class BattleReworkTests
    {
        private static CardDefinition Card(int draw=1) => CardDefinition.CreateWeaveFragment("test.plain","plain",FragmentKind.HomeAnchor,CardRarity.Common,"test",draw);
        private static BattleSession Battle(CombatApproach approach,int attack=0)
        {
            var b=new BattleSession(new BattleDefinition(100,3,new[]{new EnemyDefinition("a","a",5.6,-2,999,attack),
                new EnemyDefinition("b","b",6.4,-2,999,0)},approach>=CombatApproach.Tuning?CombatArchetype.Luna:CombatArchetype.Ian,
                fragments:true,approach:approach),100,0,0,6);
            b.EnableStatusRules();b.EnableApproachUltimates();return b;
        }
        private static void Plot(BattleSession b,bool ultimate=false)
        {
            Assert.That(b.TryPlayCard(Card(),out _),Is.True);
            if(ultimate)Assert.That(b.TryToggleUltimate(),Is.True);
            Assert.That(b.TryBeginPlot(),Is.True);b.ResolvePlot();
        }
        [Test] public void ExecutionBurstsUseActualHitsAndDoNotStackOverlappingAnchors()
        {
            var b=Battle(CombatApproach.Execution);b.TryPlayCard(Card(),out _);b.TryToggleUltimate();
            Assert.That(b.PreviewDamage(b.Enemies[1]),Is.Zero);
            Assert.That(b.PreviewApproachUltimateDamage(b.Enemies[1]),Is.EqualTo(12));
            int a=b.PreviewHealthDamage(b.Enemies[0]),c=b.PreviewHealthDamage(b.Enemies[1]);
            b.TryBeginPlot();b.ResolvePlot();
            Assert.That(999-b.Enemies[0].Health,Is.EqualTo(a));Assert.That(999-b.Enemies[1].Health,Is.EqualTo(c));
            Assert.That(b.CombatSkillCooldown,Is.Zero);Assert.That(b.Tactics.Resonance,Is.LessThan(6));
            var cancel=Battle(CombatApproach.Execution);cancel.TryToggleUltimate();cancel.TryToggleUltimate();
            Assert.That(cancel.Tactics.Resonance,Is.EqualTo(6));
        }
        [Test] public void RecordingPreservesGeometryForExactlyTwoLaterPlots()
        {
            var b=Battle(CombatApproach.Recording);Plot(b,true);
            Assert.That(b.UltimateEchoes,Is.EqualTo(2));Assert.That(b.LastUltimateHits,Is.Zero);
            b.UltimateDiagram.Sample(.25,out var x,out var y);
            for(int i=0;i<2;i++)
            {
                b.ResolveEnemyTurn();b.TryPlayCard(Card(),out _);
                int expected=b.PreviewHealthDamage(b.Enemies[0]),before=b.Enemies[0].Health;
                b.UltimateDiagram.Sample(.25,out var nowX,out var nowY);Assert.That(nowX,Is.EqualTo(x));Assert.That(nowY,Is.EqualTo(y));
                b.TryBeginPlot();b.ResolvePlot();Assert.That(before-b.Enemies[0].Health,Is.EqualTo(expected));
                Assert.That(b.LastUltimateHits,Is.EqualTo(1));Assert.That(b.UltimateEchoes,Is.EqualTo(1-i));
            }
            b.ResolveEnemyTurn();Plot(b);Assert.That(b.LastUltimateHits,Is.Zero);Assert.That(b.UltimateDiagram,Is.Null);
        }
        [TestCase(false)] [TestCase(true)]
        public void DoubleAssemblyDoesNotActDrawTickOrRefreshMovementUntilSecondResolution(bool skip)
        {
            var b=Battle(CombatApproach.Tuning,12);
            var cards=new[]{Card(1),Card(1),Card(1),Card(1),Card(1),Card(1),Card(1),Card(1)};
            var deck=new DeckSession(cards,8,new XorShiftRandom(3),true,4);deck.ResetForBattle();
            var game=new BattleGameSession(b,deck);
            game.TryPlayHandCard(0,out _,out _);b.TryMovePlayerTo(4.5,-2);b.TryToggleUltimate();
            game.TryBeginPlot();game.ResolvePlot();int hand=deck.HandCount,health=b.PlayerHealth;
            Assert.That(b.ExtraAssemblyPending,Is.True);Assert.That(game.ResolveEnemyTurn(),Is.Zero);
            Assert.That(b.Turn,Is.EqualTo(1));Assert.That(deck.HandCount,Is.EqualTo(hand));Assert.That(b.PlayerHealth,Is.EqualTo(health));
            Assert.That(b.PlayedCardCount,Is.Zero);Assert.That(b.InExtraAssembly,Is.True);Assert.That(deck.ReservedCount,Is.Zero);
            Assert.That(b.Tactics.HasMoved,Is.True);Assert.That(b.TryUndoMove(),Is.False);Assert.That(b.TryCondense(),Is.False);Assert.That(b.TryToggleUltimate(),Is.False);
            if(skip)Assert.That(game.TryUnravel(),Is.True);
            else { Assert.That(game.TryPlayHandCard(0,out _,out _),Is.True);Assert.That(game.TryBeginPlot(),Is.True);game.ResolvePlot(); }
            Assert.That(b.ExtraAssemblyPending,Is.False);game.ResolveEnemyTurn();
            Assert.That(b.Turn,Is.EqualTo(2));Assert.That(b.InExtraAssembly,Is.False);Assert.That(deck.ReservedCount,Is.Zero);
            Assert.That(deck.HandCount,Is.EqualTo(6)); // 3+2+first draw; or 2+2+both draws.
        }
        [Test] public void SanctuaryUsesSatelliteContactAndExpiresAfterTwoEnemyActions()
        {
            var b=Battle(CombatApproach.Observation,12);Assert.That(b.TryPlaceSatellite(5.6,-2),Is.True);Plot(b,true);
            Assert.That(b.BodyPlotHit,Is.False);Assert.That(b.SanctuaryProtectsPlayer,Is.True);
            Assert.That(b.SanctuaryActions,Is.EqualTo(2));b.ResolveEnemyTurn();
            Assert.That(b.LastPlayerShieldDamage,Is.EqualTo(12));Assert.That(b.PlayerHealth,Is.EqualTo(100));
            b.TryUnravel();b.ResolveEnemyTurn();Assert.That(b.SanctuaryActions,Is.Zero);Assert.That(b.PlayerHealth,Is.EqualTo(98));
            b.TryUnravel();b.ResolveEnemyTurn();Assert.That(b.PlayerHealth,Is.EqualTo(86));
        }
        [Test] public void BossPoolsVaryAcrossSeedsAndSaveKeepsChoiceAndOldRules()
        {
            var seen=new HashSet<int>[5];for(int f=0;f<5;f++)seen[f]=new HashSet<int>();
            for(uint seed=1;seed<=32;seed++)
            {
                var flow=new PrototypeGameFlow(PrototypeCharacterCatalog.All,seed);flow.OpenCharacterSelection();flow.TrySelectCharacter(0);flow.TryStartRun(CombatApproach.Execution, false);
                var r=flow.CurrentRun;Assert.That(r.HasBattleRework,Is.True);
                var save=r.CaptureSave();Assert.That(RunGameSession.TryRestore(save,out var copy,out _),Is.True);
                for(int i=0;i<r.Map.Definition.NodeCount;i++)
                {
                    var n=r.Map.Definition.GetNode(i);if(n.Kind!=RunNodeKind.Boss)continue;
                    int k=(int)n.Battle.GetEnemy(0).Behavior.Kind;seen[n.Layer/8].Add(k);
                    Assert.That(copy.Map.Definition.GetNode(i).Battle.GetEnemy(0).Behavior.Kind,Is.EqualTo((EnemyBehaviorKind)k));
                }
                Array.Resize(ref save.Commands,9);Assert.That(RunGameSession.TryRestore(save,out var old,out _),Is.True);
                Assert.That(old.HasBattleRework,Is.False);Assert.That(old.Map.Definition.FloorCount,Is.EqualTo(5));
                for(int i=0;i<old.Map.Definition.NodeCount;i++)if(old.Map.IsAvailable(i)){old.TrySelectMapNode(i);break;}
                Assert.That(old.CurrentBattle.Battle.HasApproachUltimates,Is.False);
            }
            foreach(var pool in seen)Assert.That(pool.Count,Is.EqualTo(2));
        }
        [TestCase(24)] [TestCase(25)] [TestCase(26)] [TestCase(27)] [TestCase(28)]
        public void AlternateBossesCycleAttackShapesAndHaveRecoveryTurns(int kind)
        {
            var b=new BattleSession(new BattleDefinition(999,3,new[]{EnemyArchive.All[kind].Create("boss",6.6,1.5)},CombatArchetype.Ian,fragments:true));b.EnableStatusRules();
            var shapes=new HashSet<string>();
            for(int turn=1;turn<=8;turn++)
            {
                var e=b.Enemies[0];Assert.That(e.IsBoss,Is.True);
                Assert.That(e.Intent.Kind==EnemyIntentKind.Charge,Is.EqualTo(turn%4==0));
                if(e.Intent.Kind==EnemyIntentKind.Attack)shapes.Add(e.FieldPattern>0?"field"+e.FieldPattern:e.AttackShape.ToString());
                b.TryUnravel();b.ResolveEnemyTurn();
            }
            Assert.That(shapes.Count,Is.GreaterThanOrEqualTo(2));
        }

        [TestCase(CombatApproach.Execution)] [TestCase(CombatApproach.Recording)]
        [TestCase(CombatApproach.Tuning)] [TestCase(CombatApproach.Observation)]
        public void NewUltimateStateAndCardsSurviveJournalReplay(CombatApproach approach)
        {
            var r=PrototypeRunFactory.Create(37,PrototypeCharacterCatalog.All[approach>=CombatApproach.Tuning?1:0],new LegacyBenefits(100,0,6,0,0,0));
            r.TryChooseApproach(approach);r.TryEnableOpeningRoute();r.TryEnableEconomy();r.TryEnableContentExpansion();r.TryEnableGrandArchive();
            r.TryEnableMarketBalance();r.TryEnableStatusRules();r.TryEnableTowerArchive();r.TryEnableFiveFloors();Assert.That(r.TryEnableBattleRework(),Is.True);
            int points=r.Growth.Points;
            for(int i=0;i<r.Growth.NodeCount;i++)if(r.IsRetiredUltimateNode(i))Assert.That(r.TryPurchaseGrowthNode(i),Is.False);
            Assert.That(r.Growth.Points,Is.EqualTo(points));
            for(int i=0;i<r.Map.Definition.NodeCount;i++)if(r.Map.IsAvailable(i)){Assert.That(r.TrySelectMapNode(i),Is.True);break;}
            Assert.That(r.CurrentBattle.Battle.HasApproachUltimates,Is.True);
            Assert.That(r.TryPlayHandCard(0,out _,out _),Is.True);Assert.That(r.TryToggleUltimate(),Is.True);Assert.That(r.TryBeginPlot(),Is.True);
            AssertReplay(r);r.ResolvePlot();AssertReplay(r);
            Assert.That(r.Phase,Is.EqualTo(RunPhase.Battle));r.ResolveEnemyTurn();AssertReplay(r);
            if(approach==CombatApproach.Tuning)
            {
                Assert.That(r.CurrentBattle.Battle.InExtraAssembly,Is.True);
                Assert.That(r.TryPlayHandCard(0,out _,out _),Is.True);AssertReplay(r);
                Assert.That(r.TryUnravel(),Is.True);r.ResolveEnemyTurn();AssertReplay(r);
            }
        }
        private static void AssertReplay(RunGameSession original)
        {
            Assert.That(RunSaveStore.TryDecode(RunSaveStore.Encode(original.CaptureSave()),out var data),Is.True);
            Assert.That(RunGameSession.TryRestore(data,out var restored,out _),Is.True);
            var a=original.CurrentBattle.Battle;var b=restored.CurrentBattle.Battle;
            Assert.That(b.Phase,Is.EqualTo(a.Phase));Assert.That(b.Turn,Is.EqualTo(a.Turn));Assert.That(b.PlayerHealth,Is.EqualTo(a.PlayerHealth));
            Assert.That(b.UltimateEchoes,Is.EqualTo(a.UltimateEchoes));Assert.That(b.SanctuaryActions,Is.EqualTo(a.SanctuaryActions));
            Assert.That(b.ExtraAssemblyPending,Is.EqualTo(a.ExtraAssemblyPending));Assert.That(b.InExtraAssembly,Is.EqualTo(a.InExtraAssembly));
            Assert.That(b.Tactics.Resonance,Is.EqualTo(a.Tactics.Resonance));Assert.That(b.PlayedCardCount,Is.EqualTo(a.PlayedCardCount));
            var ad=original.CurrentBattle.Deck;var bd=restored.CurrentBattle.Deck;
            Assert.That(bd.HandCount,Is.EqualTo(ad.HandCount));Assert.That(bd.ReservedCount,Is.EqualTo(ad.ReservedCount));
            for(int i=0;i<ad.HandCount;i++)Assert.That(bd.GetHandCard(i).Id,Is.EqualTo(ad.GetHandCard(i).Id));
            for(int i=0;i<a.Enemies.Count;i++)Assert.That(b.Enemies[i].Health,Is.EqualTo(a.Enemies[i].Health));
        }
    }
}

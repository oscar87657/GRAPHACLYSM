using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Relics;
using Graphaclysm.Core.Runs;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class TowerArchiveTests
    {
        private static void Add(CombatStatusState status,CombatStatusKind kind,int value=1)
            => typeof(CombatStatusState).GetMethod("Add",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(status,new object[]{kind,value,3});
        public static IEnumerable<int> ConditionalCases { get {for(int i=24;i<100;i++)yield return i;} }
        [Test] public void HundredRelicsAndTwentyEnemyEntriesHaveStableUniqueIdsAndText()
        {
            Assert.That(FragmentRelicCatalog.All.Count,Is.EqualTo(100));Assert.That(FragmentRelicCatalog.Version30.Count,Is.EqualTo(24));
            var ids=new HashSet<string>();var pairs=new HashSet<string>();
            foreach(var r in FragmentRelicCatalog.All)
            {
                Assert.That(ids.Add(r.Id),Is.True);Assert.That(r.DisplayName,Is.Not.Empty);Assert.That(r.Description,Is.Not.Empty);
                if(r.Id.StartsWith("relic.archive."))Assert.That(pairs.Add(r.Trigger+"/"+r.Reward),Is.True);
            }
            Assert.That(pairs.Count,Is.EqualTo(76));Assert.That(EnemyArchive.All.Count,Is.EqualTo(29));
            for(int i=0;i<20;i++){var e=EnemyArchive.All[i];Assert.That((int)e.Kind,Is.EqualTo(i));Assert.That(e.Counter,Is.Not.Empty);Assert.That(e.Create("test",6,1),Is.Not.Null);}
            Assert.That(FragmentRelicCatalog.All[4].Effect,Is.EqualTo(RelicEffectKind.PrismDamage));
            Assert.That(FragmentRelicCatalog.Version30[4].Effect,Is.EqualTo(RelicEffectKind.BonusPlotDamage));
        }
        [TestCaseSource(nameof(ConditionalCases))]
        public void EveryNewRelicActivatesThroughARealMatchingPlot(int index)
        {
            var relic=FragmentRelicCatalog.All[index];var collection=new RunRelicCollection();collection.TryAdd(relic);
            var behavior=relic.Trigger==RelicTrigger.Boss?EnemyBehaviorDefinition.Archive(EnemyBehaviorKind.GlassRegent):EnemyBehaviorDefinition.SteadyAttack();
            var targets=relic.Trigger==RelicTrigger.Multiple?new[]{new EnemyDefinition("a","a",5.6,-2,999,0),new EnemyDefinition("b","b",2.4,-2,999,0)}
                :new[]{new EnemyDefinition("a","a",5.6,-2,999,0,behavior)};
            var terrain=relic.Trigger==RelicTrigger.Prism?new[]{new BattleTerrainDefinition("p",BattleTerrainKind.Prism,5.6,-2,.2)}:null;
            var b=new BattleSession(new BattleDefinition(100,3,targets,CombatArchetype.Ian,fragments:true,terrain:terrain),
                relic.Trigger==RelicTrigger.Wounded?40:100,0,0,relics:collection);
            b.EnableStatusRules();
            for(int c=0;c<(relic.Trigger==RelicTrigger.Long?6:1);c++)Assert.That(b.TryPlayCard(FragmentCardCatalog.Current("frag.home"),out _),Is.True);
            if(relic.Trigger==RelicTrigger.Moved || relic.Trigger==RelicTrigger.Self || relic.Trigger==RelicTrigger.Shared)
                Assert.That(b.TryMovePlayerTo(5.6,-2),Is.True);
            if(relic.Trigger==RelicTrigger.Condensed){Assert.That(b.TryCondense(),Is.True);b.ResolveEnemyTurn();}
            if(relic.Trigger==RelicTrigger.Shielded)Add(b.Tactics.Statuses,CombatStatusKind.Shield,3);
            if(relic.Trigger==RelicTrigger.Burning)Add(b.Enemies[0].Statuses,CombatStatusKind.Burn);
            if(relic.Trigger==RelicTrigger.Cracked)Add(b.Enemies[0].Statuses,CombatStatusKind.Rupture);
            if(relic.Trigger==RelicTrigger.Guided)Add(b.Enemies[0].Statuses,CombatStatusKind.Guidance);
            if(relic.Trigger==RelicTrigger.Rooted)Add(b.Enemies[0].Statuses,CombatStatusKind.Anchor);
            Assert.That(b.TryBeginPlot(),Is.True);b.ResolvePlot();
            Assert.That(b.LastRelicActivations,Is.EqualTo(1),relic.DisplayName);
            var status=(int)relic.Reward>=8?b.Enemies[0].Statuses:b.Tactics.Statuses;
            Assert.That(status.Get(RelicArchive.RewardStatus(relic.Reward)),Is.GreaterThanOrEqualTo(relic.Magnitude));
        }
        [Test] public void ConditionsDoNotRetroactivelySeeStatusesCreatedBySamePlot()
        {
            var owned=new RunRelicCollection();owned.TryAdd(FragmentRelicCatalog.All[24+12*4]);
            var b=new BattleSession(new BattleDefinition(100,3,new[]{new EnemyDefinition("a","a",5.6,-2,999,0)},CombatArchetype.Ian,fragments:true),100,0,0,relics:owned);
            b.EnableStatusRules();
            var burnCard=CardDefinition.CreateWeaveFragment("test.burn","burn",Graphaclysm.Core.Equations.FragmentKind.HomeAnchor,CardRarity.Common,"test",0,new CardAbility(CardAbilityKind.Burn,AbilityTarget.Enemy,1,3));
            b.TryPlayCard(burnCard,out _);b.TryBeginPlot();b.ResolvePlot();
            Assert.That(b.LastRelicActivations,Is.Zero,"Burning-target relic cannot trigger against a clean target");
            Assert.That(b.Enemies[0].Statuses.Get(CombatStatusKind.Burn),Is.EqualTo(1));
            b.ResolveEnemyTurn();b.TryPlayCard(FragmentCardCatalog.Current("frag.home"),out _);b.TryBeginPlot();b.ResolvePlot();
            Assert.That(b.LastRelicActivations,Is.EqualTo(1));
        }
        [TestCase(EnemyBehaviorKind.GlassRegent)] [TestCase(EnemyBehaviorKind.ArchiveKeeper)] [TestCase(EnemyBehaviorKind.AstralSovereign)]
        public void BossHasThreeAttackShapesAndARecoveryBeatAndAnnouncedSecondPhase(EnemyBehaviorKind kind)
        {
            var b=new BattleSession(new BattleDefinition(999,3,new[]{EnemyArchive.Find(kind).Create("boss",6,1)},CombatArchetype.Ian,fragments:true));b.EnableStatusRules();
            var e=b.Enemies[0];var shapes=new HashSet<EnemyBehaviorKind>();
            for(int turn=1;turn<=4;turn++)
            {
                if(turn<4){Assert.That(e.Intent.Kind,Is.EqualTo(EnemyIntentKind.Attack));shapes.Add(e.AttackShape);}
                else Assert.That(e.Intent.Kind,Is.EqualTo(EnemyIntentKind.Charge));
                Assert.That(b.TryUnravel(),Is.True);b.ResolveEnemyTurn();
            }
            Assert.That(shapes.Count,Is.EqualTo(3));
            e.Statuses.Get(CombatStatusKind.Shield);
            e.TakeDamage(e.Definition.MaxHealth/2+20);var old=e.AttackShape;
            Assert.That(e.Enraged,Is.False,"An announced attack must not change during planning");
            b.TryUnravel();b.ResolveEnemyTurn();Assert.That(e.Enraged,Is.True);
            Assert.That(e.Intent.Damage,Is.GreaterThanOrEqualTo(e.Definition.Attack+2));
        }
        [Test] public void RepairPriestHealsOnlyLivingAllies()
        {
            var b=new BattleSession(new BattleDefinition(100,3,new[]{EnemyArchive.All[13].Create("h",7,2),new EnemyDefinition("a","a",2,1,30,0),new EnemyDefinition("d","d",8,1,10,0)},CombatArchetype.Ian,fragments:true));
            b.EnableStatusRules();b.Enemies[1].TakeDamage(10);b.Enemies[2].TakeDamage(10);b.TryUnravel();b.ResolveEnemyTurn();
            Assert.That(b.Enemies[1].Health,Is.EqualTo(25));Assert.That(b.Enemies[2].Health,Is.Zero);
        }
        [TestCase(1u)] [TestCase(302u)] [TestCase(9381u)]
        public void EachFloorEndsInItsOwnBossAndNewRulesRoundTrip(uint seed)
        {
            var flow=new PrototypeGameFlow(PrototypeCharacterCatalog.All,seed);flow.OpenCharacterSelection();flow.TrySelectCharacter(1);Assert.That(flow.TryStartRun(CombatApproach.Tuning, false),Is.True);
            var run=flow.CurrentRun;Assert.That(run.HasTowerArchive,Is.True);var bosses=new int[5];
            for(int i=0;i<run.Map.Definition.NodeCount;i++)
            {
                var n=run.Map.Definition.GetNode(i);if(n.Kind!=RunNodeKind.Boss)continue;
                Assert.That(n.Layer%8,Is.EqualTo(7));int floor=n.Layer/8;bosses[floor]++;
                int expected=BossLottery.UseAlternate(run.Seed,floor)?24+floor:floor<3?17+floor:22+floor-3;
                Assert.That((int)n.Battle.GetEnemy(0).Behavior.Kind,Is.EqualTo(expected));
            }
            CollectionAssert.AreEqual(new[]{1,1,1,1,1},bosses);
            var bytes=RunSaveStore.Encode(run.CaptureSave());Assert.That(RunSaveStore.TryDecode(bytes,out var data),Is.True);Assert.That(RunGameSession.TryRestore(data,out var copy,out _),Is.True);
            Assert.That(copy.HasTowerArchive,Is.True);
            Array.Copy(BitConverter.GetBytes(30),0,bytes,8,4);using(var hash=SHA256.Create())Array.Copy(hash.ComputeHash(bytes,0,bytes.Length-32),0,bytes,bytes.Length-32,32);
            Assert.That(RunSaveStore.TryDecode(bytes,out _),Is.False);
        }
    }
}

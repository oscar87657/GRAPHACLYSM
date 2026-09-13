using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Decks;
using Graphaclysm.Core.Equations;
using Graphaclysm.Application;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class CardMarketBalanceTests
    {
        [Test] public void PoolIsExactlyTwentyTwentyFortyTwentyAndEffectsMatchColors()
        {
            var counts = new int[4];
            foreach (var card in FragmentCardCatalog.Version29)
            {
                int tier = (int)card.DiagramRarity; counts[tier]++;
                Assert.That(card.Rarity,Is.EqualTo(card.DiagramRarity),card.Id);
                Assert.That(card.HasMarketBalance,Is.True);
                Assert.That(card.WeavePower, tier == 0 ? Is.InRange(0,1) : Is.EqualTo(tier+1),card.Id);
                var old = FragmentCardCatalog.Previous(card.Id);
                Assert.That(old.HasMarketBalance,Is.False);
                Assert.That(card.Fragment,Is.EqualTo(old.Fragment));
                Assert.That(card.AbilityCount,Is.EqualTo(old.AbilityCount));
                for(int a=0;a<card.AbilityCount;a++)
                {
                    var now=card.GetAbility(a); var before=old.GetAbility(a);
                    Assert.That(now.Kind,Is.EqualTo(before.Kind)); Assert.That(now.Target,Is.EqualTo(before.Target));
                    if(tier>0) Assert.That(now.Magnitude,Is.GreaterThanOrEqualTo(before.Magnitude));
                    Assert.That(now.Duration,Is.InRange(1,3));
                }
            }
            CollectionAssert.AreEqual(new[]{21,21,42,21},counts);
            Assert.That(FragmentCardCatalog.Current("frag.weave.9.3").DiagramRarity,Is.EqualTo(CardRarity.Legendary));
        }
        [TestCase(false)] [TestCase(true)]
        public void TierProbabilityIsIndependentOfHowManyCardsBelongToTheTier(bool shop)
        {
            var observed=new int[4];
            for(int roll=0;roll<100;roll++)
                observed[(int)CardMarketBalance.Draw(new FixedRoll(roll),FragmentCardCatalog.All,shop,null,0).Rarity]++;
            CollectionAssert.AreEqual(shop?new[]{40,30,24,6}:new[]{50,28,18,4},observed);
        }
        [TestCase(false)] [TestCase(true)]
        public void RepeatedDrawsAreUniqueAndSeededDistributionTracksTheConfiguredOdds(bool shop)
        {
            var random=new XorShiftRandom(917);var counts=new int[4];var excluded=new CardDefinition[5];
            for(int sample=0;sample<12000;sample++)
            {
                var card=CardMarketBalance.Draw(random,FragmentCardCatalog.All,shop,null,0);counts[(int)card.Rarity]++;
            }
            for(int t=0;t<4;t++) Assert.That(counts[t]/120.0,Is.EqualTo(CardMarketBalance.TierWeight((CardRarity)t,shop)).Within(1.5));
            for(int batch=0;batch<100;batch++)
                for(int i=0;i<5;i++)
                {
                    excluded[i]=CardMarketBalance.Draw(random,FragmentCardCatalog.All,shop,excluded,i);
                    for(int j=0;j<i;j++) Assert.That(excluded[i].Id,Is.Not.EqualTo(excluded[j].Id));
                }
        }
        [Test] public void GoldActuallyHasMoreOutputAndStrongerRidersWithoutChangingItsCurve()
        {
            var old=FragmentCardCatalog.Previous("frag.weave.15.3");var card=FragmentCardCatalog.Current(old.Id);
            Assert.That(card.WeavePower,Is.EqualTo(4));Assert.That(old.WeavePower,Is.EqualTo(3));
            Assert.That(card.GetAbility(1).Magnitude,Is.GreaterThan(old.GetAbility(1).Magnitude));
            var f=new FragmentEquation();f.TryAppend(card.Fragment,4,-2);f.Sample(.125,out double x,out double y);
            var battle=new BattleSession(new BattleDefinition(100,1,new[]{new EnemyDefinition("t","target",x,y,999,0)},CombatArchetype.Ian,fragments:true));
            Assert.That(battle.TryPlayCard(card,out _),Is.True);Assert.That(battle.WeaveDamageBonus,Is.EqualTo(4));
            var enemy=battle.Enemies[0];int expected=battle.PreviewDamage(enemy);Assert.That(expected,Is.GreaterThan(0));
            Assert.That(battle.TryBeginPlot(),Is.True);
            battle.ResolvePlot();Assert.That(999-enemy.Health,Is.EqualTo(expected));
        }
        [Test] public void ActivationIsOnlyAtDepartureAndHistoricalRunsRemainHistorical()
        {
            var run=PrototypeRunFactory.Create(302,PrototypeCharacterCatalog.All[1]);
            run.TryChooseApproach(CombatApproach.Tuning);run.TryEnableOpeningRoute();run.TryEnableEconomy();run.TryEnableContentExpansion();run.TryEnableGrandArchive();
            Assert.That(RunGameSession.TryRestore(run.CaptureSave(),out var old,out _),Is.True);
            Assert.That(old.HasMarketBalance,Is.False);Assert.That(old.Deck[0].HasMarketBalance,Is.False);
            Assert.That(run.TryEnableMarketBalance(),Is.True);Assert.That(run.TryEnableMarketBalance(),Is.False);
            Assert.That(RunGameSession.TryRestore(run.CaptureSave(),out var current,out _),Is.True);
            Assert.That(current.HasMarketBalance,Is.True);Assert.That(current.Deck[0].HasMarketBalance,Is.True);
            var bytes=RunSaveStore.Encode(run.CaptureSave());Array.Copy(BitConverter.GetBytes(28),0,bytes,8,4);
            using(var hash=SHA256.Create())Array.Copy(hash.ComputeHash(bytes,0,bytes.Length-32),0,bytes,bytes.Length-32,32);
            Assert.That(RunSaveStore.TryDecode(bytes,out _),Is.False,"Rules28 must reject MarketBalance commands");
            old.TrySelectMapNode(0);Assert.That(old.TryEnableMarketBalance(),Is.False);
        }
        private sealed class FixedRoll : IRandomSource
        {
            private int roll; public FixedRoll(int value) { roll=value; }
            public int Next(int max) { int value=roll;roll=0;return value%max; }
        }
    }
}

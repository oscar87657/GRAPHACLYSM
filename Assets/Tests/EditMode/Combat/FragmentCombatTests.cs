using System;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Decks;
using Graphaclysm.Core.Equations;
using NUnit.Framework;
namespace Graphaclysm.Tests.Combat
{
    public sealed class FragmentCombatTests
    {
        private static BattleSession Battle() => new BattleSession(new BattleDefinition(100,1,
            new[]{new EnemyDefinition("target","Target",7.4,0,999,2)},CombatArchetype.Ian,fragments:true));
        private static BattleGameSession Game(string id="frag.contract")
        {
            var cards=new CardDefinition[12];for(int i=0;i<cards.Length;i++)cards[i]=FragmentCardCatalog.Find(id);
            return new BattleGameSession(Battle(),new DeckSession(cards,8,new XorShiftRandom(12),true,5));
        }
        private static void Conservation(DeckSession d) => Assert.That(d.HandCount+d.DrawCount+d.DiscardCount+d.ReservedCount,Is.EqualTo(d.TotalCardCount));
        [Test] public void SharedCurveDamagesEnemyAndBuffsPlayerWithoutSelfDamage()
        {
            var b=new BattleSession(new BattleDefinition(100,1,new[]{new EnemyDefinition("a","Target",5,-2.4,100,0)},CombatArchetype.Ian,fragments:true));
            b.TryPlayCard(FragmentCardCatalog.Find("frag.down"),out _);Assert.That(b.PreviewPlayerHit,Is.True);
            Assert.That(b.TryBeginPlot(),Is.True);var r=b.ResolvePlot();Assert.That(r.HitCount,Is.EqualTo(1));Assert.That(r.PlayerHit,Is.True);
            Assert.That(b.PlayerHealth,Is.EqualTo(100));Assert.That(b.Tactics.Statuses.Get(CombatStatusKind.Shield),Is.EqualTo(3));
        }
        [Test] public void LiveRunDrawRelicChangesRefillAndOpeningHandRemainsFive()
        {
            var run=PrototypeRunFactory.Create(37,Graphaclysm.Core.Characters.PrototypeCharacterCatalog.All[0]);
            run.Relics.TryAdd(Graphaclysm.Core.Relics.FragmentRelicCatalog.All[0]);run.TrySelectMapNode(0);
            Assert.That(run.CurrentBattle.Battle.UsesFragments,Is.True);Assert.That(run.CurrentBattle.Deck.HandCount,Is.EqualTo(5));
            Assert.That(run.CurrentBattle.NextDrawCount,Is.EqualTo(3));Assert.That(run.CurrentBattle.Deck.HandCapacity,Is.EqualTo(8));
            foreach(var relic in Graphaclysm.Core.Relics.FragmentRelicCatalog.All) Assert.That(relic.Effect,Is.Not.EqualTo(Graphaclysm.Core.Relics.RelicEffectKind.BonusEnergy));
        }
        [Test] public void EveryFragmentCanStartWithoutInputOrEnergy()
        {
            foreach(var card in FragmentCardCatalog.All)
            { var b=Battle();Assert.That(b.Equation.HasBase,Is.False);Assert.That(b.TryBeginPlot(),Is.False);Assert.That(b.TryPlayCard(card,out _),Is.True);Assert.That(b.Energy,Is.EqualTo(1));Assert.That(card.Cost,Is.Zero);Assert.That(b.CanPlot,Is.True); }
        }
        [Test] public void CounterpointEvaluatesActualReverseOrbitAndHitsItsCurve()
        {
            var b=Battle();b.TryPlayCard(FragmentCardCatalog.Find("frag.echo"),out _);
            for(int i=0;i<24;i++)
            { double t=i*Math.PI*2/24;b.Equation.Sample(i/24.0,out double x,out double y);
              Assert.That(x,Is.EqualTo(5+1.6*Math.Cos(t)+.8*Math.Cos(-3*t)).Within(1e-9));
              Assert.That(y,Is.EqualTo(1.6*Math.Sin(t)+.8*Math.Sin(-3*t)).Within(1e-9)); }
            Assert.That(b.PreviewDamage(b.Enemies[0]),Is.GreaterThan(0));
        }
        [Test] public void WrappingOrderChangesGeometryAndUndoRestoresInnerCurve()
        {
            var a=new FragmentEquation();a.TryAppend(FragmentKind.Counterpoint);a.Sample(.0625,out double px,out double py);a.TryAppend(FragmentKind.Orbit);a.Sample(.0625,out double ax,out double ay);
            var b=new FragmentEquation();b.TryAppend(FragmentKind.Orbit);b.TryAppend(FragmentKind.Counterpoint);b.Sample(.0625,out double bx,out double by);
            Assert.That(Math.Abs(ax-bx)+Math.Abs(ay-by),Is.GreaterThan(.2));
            a.TryUndo();a.Sample(.0625,out ax,out ay);Assert.That(ax,Is.EqualTo(px));Assert.That(ay,Is.EqualTo(py));
        }
        [Test] public void FrequencyGuardAndEightSocketsRejectWithoutChangingInnerGeometry()
        {
            var a=new FragmentEquation();for(int i=0;i<4;i++)Assert.That(a.TryAppend(FragmentKind.Counterpoint),Is.True);
            a.Sample(.2,out double x,out double y);Assert.That(a.TryAppend(FragmentKind.Counterpoint),Is.False);a.Sample(.2,out double nx,out double ny);Assert.That(nx,Is.EqualTo(x));Assert.That(ny,Is.EqualTo(y));
            a.Clear();for(int i=0;i<8;i++)Assert.That(a.TryAppend(FragmentKind.Contract),Is.True);Assert.That(a.TryAppend(FragmentKind.Mirror),Is.False);
        }
        [Test] public void ReservationUndoAndFreeMovementConserveResources()
        {
            var g=Game();int energy=g.Battle.Energy;g.TryPlayHandCard(2,out var card,out _);Conservation(g.Deck);Assert.That(g.Deck.ReservedCount,Is.EqualTo(1));Assert.That(g.Deck.DiscardCount,Is.Zero);
            g.Battle.TryMovePlayer(-1.5,0);Assert.That(g.TryUndoLastPlayedCard(out var restored),Is.True);Assert.That(restored,Is.SameAs(card));Assert.That(g.Battle.TryUndoMove(),Is.True);Assert.That(g.Battle.Energy,Is.EqualTo(energy));Conservation(g.Deck);
        }
        [Test] public void CondenseRetainsQueueAndHandAwardsDrawOnceAndSealsUndo()
        {
            var g=Game();g.TryPlayHandCard(0,out _,out _);Assert.That(g.NextDrawCount,Is.EqualTo(3));
            Assert.That(g.TryCondense(),Is.True);g.ResolveEnemyTurn();
            Assert.That(g.Battle.PlayedCardCount,Is.EqualTo(1));Assert.That(g.Deck.HandCount,Is.EqualTo(6));Assert.That(g.Deck.ReservedCount,Is.EqualTo(1));
            Assert.That(g.TryUndoLastPlayedCard(out _),Is.False);Assert.That(g.NextDrawCount,Is.EqualTo(2));Conservation(g.Deck);
            g.TryPlayHandCard(0,out _,out _);Assert.That(g.TryUndoLastPlayedCard(out _),Is.True);Assert.That(g.TryUndoLastPlayedCard(out _),Is.False);
        }
        [Test] public void CondenseAllowsTwoEnemyTurnsThenRequiresReleaseOrUnravel()
        {
            var g=Game();int hp=g.Battle.PlayerHealth;
            for(int i=0;i<2;i++){Assert.That(g.TryCondense(),Is.True);g.ResolveEnemyTurn();}
            Assert.That(g.Battle.PlayerHealth,Is.LessThan(hp));Assert.That(g.TryCondense(),Is.False);Assert.That(g.Deck.HandCount,Is.EqualTo(7));
            Assert.That(g.TryUnravel(),Is.True);g.ResolveEnemyTurn();Assert.That(g.Battle.CanCondense,Is.True);Conservation(g.Deck);
        }
        [Test] public void ReleaseResolvesStoredAbilitiesOnceAndResetsQueueButKeepsHand()
        {
            var g=Game("frag.echo");g.TryPlayHandCard(0,out _,out _);g.TryCondense();g.ResolveEnemyTurn();
            int hand=g.Deck.HandCount;Assert.That(g.TryBeginPlot(),Is.True);Assert.That(g.Deck.ReservedCount,Is.Zero);
            var report=g.ResolvePlot();Assert.That(report.HitCount,Is.EqualTo(1));Assert.That(g.Battle.Enemies[0].Statuses.Get(CombatStatusKind.Burn),Is.EqualTo(2));g.ResolveEnemyTurn();
            Assert.That(g.Battle.PlayedCardCount,Is.Zero);Assert.That(g.Battle.Equation.HasBase,Is.False);Assert.That(g.Deck.HandCount,Is.EqualTo(Math.Min(8,hand+2)));Conservation(g.Deck);
        }
        [Test] public void DrawFragmentsDoNotRecycleReservedCardsOrFarmBonusesOnUndo()
        {
            var g=Game();for(int i=0;i<5;i++)g.TryPlayHandCard(0,out _,out _);
            Assert.That(g.Deck.ReservedCount,Is.EqualTo(5));Assert.That(g.TryUndoLastPlayedCard(out _),Is.True);Assert.That(g.NextDrawCount,Is.EqualTo(6));
            g.TryCondense();g.ResolveEnemyTurn();Assert.That(g.Deck.ReservedCount,Is.EqualTo(4));Assert.That(g.Deck.HandCount,Is.EqualTo(3));Assert.That(g.NextDrawCount,Is.EqualTo(2));Conservation(g.Deck);
        }
        [Test] public void UnravelReleasesCardsWithoutAttackOrExtraDrawAndBurstScalesWithFragments()
        {
            var g=Game("frag.expand");g.TryPlayHandCard(0,out _,out _);g.TryPlayHandCard(0,out _,out _);Assert.That(g.Battle.WeaveDamageBonus,Is.EqualTo(2));int hp=g.Battle.Enemies[0].Health;
            g.TryUnravel();g.ResolveEnemyTurn();Assert.That(g.Battle.Enemies[0].Health,Is.EqualTo(hp));Assert.That(g.Battle.WeaveDamageBonus,Is.Zero);Assert.That(g.Deck.ReservedCount,Is.Zero);Conservation(g.Deck);
        }
    }
}

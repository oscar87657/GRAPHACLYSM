using System;
using System.Security.Cryptography;
using Graphaclysm.Application;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Equations;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class ObservationTests
    {
        private static RunGameSession Practice() => SignatureBattleFactory.Create(CombatApproach.Observation);
        private static void Ellipse(RunGameSession run)
        {
            for (int i=0;i<run.CurrentBattle.Deck.HandCount;i++)
                if (run.CurrentBattle.Deck.GetHandCard(i).Id == "frag.ellipse")
                { Assert.That(run.TryPlayHandCard(i,out _,out _),Is.True); return; }
            Assert.Fail("Ellipse missing");
        }
        [Test]
        public void SatelliteProtectsBodyOutsideGraphAndDoesNotDuplicateOnOverlap()
        {
            var run=Practice(); var b=run.CurrentBattle.Battle;
            Ellipse(run);
            Assert.That(b.BodyPlotHit,Is.False);
            Assert.That(run.TryPlaceSatellite(6.2,-2),Is.True);
            Assert.That(b.SatellitePlotHit,Is.True); Assert.That(b.PreviewPlayerHit,Is.True);
            int before=b.Tactics.Statuses.Get(CombatStatusKind.Shield);
            run.TryBeginPlot(); run.ResolvePlot();
            int gain=b.Tactics.Statuses.Get(CombatStatusKind.Shield)-before;
            Assert.That(gain,Is.GreaterThanOrEqualTo(3));
            var both=Practice(); Ellipse(both); var c=both.CurrentBattle.Battle;
            Assert.That(both.TryMovePlayerTo(5.8,-2),Is.True);
            Assert.That(both.TryPlaceSatellite(6.2,-2),Is.True);
            Assert.That(c.BodyPlotHit && c.SatellitePlotHit,Is.True);
            before=c.Tactics.Statuses.Get(CombatStatusKind.Shield);
            both.TryBeginPlot(); both.ResolvePlot();
            Assert.That(c.Tactics.Statuses.Get(CombatStatusKind.Shield)-before,Is.EqualTo(gain));
        }
        [Test]
        public void OriginChoiceUsesSatelliteWithoutMovingBodyAndUndoAllowsReselection()
        {
            var run=Practice(); var b=run.CurrentBattle.Battle;
            Assert.That(run.TryPlaceSatellite(6,-2),Is.True);
            Assert.That(run.TryToggleSatelliteOrigin(),Is.True); Ellipse(run);
            Assert.That(b.Equation.Fragments.OriginX,Is.EqualTo(6)); Assert.That(b.Tactics.X,Is.EqualTo(4));
            Assert.That(run.TryToggleSatelliteOrigin(),Is.False);
            Assert.That(run.TryUndoLastPlayedCard(out _),Is.True);
            Assert.That(run.TryToggleSatelliteOrigin(),Is.True); Ellipse(run);
            Assert.That(b.Equation.Fragments.OriginX,Is.EqualTo(4));
        }
        [Test]
        public void PlacementDoesNotMoveExistingDiagramAndPersistsAcrossTurns()
        {
            var run=Practice(); var b=run.CurrentBattle.Battle; Ellipse(run);
            Assert.That(run.TryPlaceSatellite(6.2,-2),Is.True);
            Assert.That(b.Equation.Fragments.OriginX,Is.EqualTo(4));
            Assert.That(run.TryPlaceSatellite(5,-2),Is.False);
            Assert.That(b.Tactics.HasMoved,Is.False);
            Assert.That(run.TryUnravel(),Is.True); run.ResolveEnemyTurn();
            Assert.That(b.HasSatellite,Is.True); Assert.That(b.SatelliteX,Is.EqualTo(6.2));
            Assert.That(b.CanPlaceSatellite,Is.True); Assert.That(run.TryPlaceSatellite(5,-2),Is.True);
            b.Reset(); Assert.That(b.HasSatellite,Is.False); Assert.That(b.SatelliteOriginSelected,Is.False);
        }
        [TestCase(double.NaN,0)]
        [TestCase(9,-2)]
        [TestCase(0,-4)]
        public void InvalidPlacementConsumesNothing(double x,double y)
        {
            var run=Practice(); var b=run.CurrentBattle.Battle;
            int count=run.CurrentBattle.Deck.HandCount;
            Assert.That(run.TryPlaceSatellite(x,y),Is.False);
            Assert.That(b.CanPlaceSatellite,Is.True); Assert.That(b.HasSatellite,Is.False);
            Assert.That(run.CurrentBattle.Deck.HandCount,Is.EqualTo(count));
            Assert.That(run.TryToggleSatelliteOrigin(),Is.False);
        }
        [Test]
        public void ObservationIsDistinctFromTuningAndDoesNotGetItsSkills()
        {
            var run=Practice(); Ellipse(run);
            Assert.That(run.TryUseDiagramAbility(4,-2),Is.False);
            Assert.That(run.TryUseLunaPull(5.5,-1),Is.False);
            Assert.That(run.TryUseCombatSkill(0),Is.False);
            var tuning=SignatureBattleFactory.Create(CombatApproach.Tuning);
            Assert.That(tuning.TryPlaceSatellite(6,-2),Is.False);
        }
        [Test]
        public void HomeAnchorStillReturnsToBodyAndPlacementAvoidsPillars()
        {
            var b=new BattleSession(new BattleDefinition(42,3,new[] { new EnemyDefinition("a","a",8,0,50,3) },
                CombatArchetype.Luna,fragments:true,terrain:new[] { new BattleTerrainDefinition("p",BattleTerrainKind.Obstacle,6,-2,.4) },approach:CombatApproach.Observation));
            Assert.That(b.TryPlaceSatellite(6,-2),Is.False);
            Assert.That(b.TryPlaceSatellite(4,-3.51),Is.False);
            Assert.That(b.TryPlaceSatellite(4,0),Is.True); Assert.That(b.TryToggleSatelliteOrigin(),Is.True);
            foreach(var card in FragmentCardCatalog.All)
                if(card.Fragment==FragmentKind.HomeAnchor)
                { Assert.That(b.TryPlayCard(card,out _),Is.True); Assert.That(b.Equation.Fragments.OriginY,Is.EqualTo(-2)); return; }
            Assert.Fail("HomeAnchor missing");
        }
        [Test]
        public void CampaignAndRestartSelectLunaAndReplaySatelliteOriginExactly()
        {
            var flow=new PrototypeGameFlow(PrototypeCharacterCatalog.All,302);
            flow.OpenCharacterSelection(); flow.TrySelectCharacter(0);
            Assert.That(flow.TryStartRun(CombatApproach.Observation, false),Is.False);
            flow.TrySelectCharacter(1);
            Assert.That(flow.TryStartRun(CombatApproach.Observation, false),Is.True);
            var run=flow.CurrentRun;
            Assert.That(run.TrySelectMapNode(0),Is.True);
            Assert.That(run.CurrentBattle.Battle.Approach,Is.EqualTo(CombatApproach.Observation));
            run.TryPlaceSatellite(6.1234,-1.2345); run.TryToggleSatelliteOrigin(); Ellipse(run);
            Assert.That(RunSaveStore.TryDecode(RunSaveStore.Encode(run.CaptureSave()),out var data),Is.True);
            Assert.That(RunGameSession.TryRestore(data,out var copy,out _),Is.True);
            Assert.That(copy.CurrentBattle.Battle.SatelliteX,Is.EqualTo(6.12));
            Assert.That(copy.CurrentBattle.Battle.Equation.Fragments.OriginY,Is.EqualTo(-1.23));
            Assert.That(copy.CurrentBattle.Battle.SatelliteOriginSelected,Is.True);
            Assert.That(copy.CurrentBattle.Battle.CanPlaceSatellite,Is.False);
            Assert.That(flow.TryRestartRun(),Is.True);
            Assert.That(flow.CurrentRun.StartingApproach,Is.EqualTo(CombatApproach.Observation));
        }
        [Test]
        public void PracticeStaysUnsavedAndRules23RemainReadable()
        {
            var run=Practice(); Assert.That(run.CanSave,Is.False);
            var old=PrototypeRunFactory.Create(302,PrototypeCharacterCatalog.All[1]);
            old.TryChooseApproach(CombatApproach.Tuning); old.TrySelectMapNode(0); old.TryUseLunaPull(5.5,-1);
            byte[] bytes=RunSaveStore.Encode(old.CaptureSave());
            Array.Copy(BitConverter.GetBytes(23),0,bytes,8,4);
            using(var hash=SHA256.Create()) Array.Copy(hash.ComputeHash(bytes,0,bytes.Length-32),0,bytes,bytes.Length-32,32);
            Assert.That(RunSaveStore.TryDecode(bytes,out var data),Is.True);
            Assert.That(RunGameSession.TryRestore(data,out var restored,out _),Is.True);
            Assert.That(restored.CurrentBattle.Battle.Enemies[0].X,Is.EqualTo(old.CurrentBattle.Battle.Enemies[0].X));
        }
    }
}

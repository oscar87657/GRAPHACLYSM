using System;
using System.Reflection;
using System.Security.Cryptography;
using Graphaclysm.Application;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Equations;
using Graphaclysm.Core.Runs;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class OpeningRouteTests
    {
        private static RunGameSession Start(CombatApproach approach, uint seed = 302)
        {
            var flow = new PrototypeGameFlow(PrototypeCharacterCatalog.All, seed);
            flow.OpenCharacterSelection(); flow.TrySelectCharacter(approach >= CombatApproach.Tuning ? 1 : 0);
            Assert.That(flow.TryStartRun(approach, false), Is.True);
            return flow.CurrentRun;
        }
        private static RunGameSession Restore(RunGameSession run)
        {
            Assert.That(RunSaveStore.TryDecode(RunSaveStore.Encode(run.CaptureSave()), out var data), Is.True);
            Assert.That(RunGameSession.TryRestore(data, out var copy, out _), Is.True);
            Assert.That(copy.HasOpeningRoute, Is.EqualTo(run.HasOpeningRoute));
            Assert.That(copy.Growth.Points, Is.EqualTo(run.Growth.Points));
            Assert.That(copy.Map.CompletedNodeCount, Is.EqualTo(run.Map.CompletedNodeCount));
            Assert.That(copy.TrainingPower, Is.EqualTo(run.TrainingPower));
            return copy;
        }
        [Test]
        public void FirstFloorHasDistinctRolesAndLaterFloorsAreUntouchedAcrossSeeds()
        {
            for (uint seed = 1; seed <= 50; seed++)
            {
                var character = PrototypeCharacterCatalog.All[0];
                var old = DungeonGenerator.Generate(seed, character);
                var map = OpeningRoute.Create(old, character);
                Assert.That(map.NodeCount, Is.EqualTo(old.NodeCount));
                for (int i = 0; i < map.NodeCount; i++)
                {
                    var node = map.GetNode(i);
                    if (node.Layer >= 8) Assert.That(node, Is.SameAs(old.GetNode(i)));
                    if (node.Layer == 0)
                    {
                        Assert.That(node.Kind, Is.EqualTo(RunNodeKind.Battle));
                        Assert.That(node.Battle.EnemyCount, Is.EqualTo(2));
                        Assert.That(node.Battle.TerrainCount, Is.Zero);
                    }
                    if (node.Layer == 0 || node.Layer == 2 || node.Layer == 5)
                        Assert.That(map.GetNode(node.GetNextNodeIndex(0)).Kind,
                            Is.Not.EqualTo(map.GetNode(node.GetNextNodeIndex(1)).Kind));
                    if (node.Layer == 1 || node.Layer == 6) Assert.That(node.Battle, Is.Null);
                    if (node.Layer == 1)
                        Assert.That(OpeningRoute.MapLabel(map.GetNode(node.GetNextNodeIndex(0))),
                            Is.Not.EqualTo(OpeningRoute.MapLabel(map.GetNode(node.GetNextNodeIndex(1)))));
                    if (node.Layer == 2 || node.Layer == 3) Assert.That(node.Battle, Is.Not.Null);
                    if (node.Layer > 0)
                    {
                        bool incoming = false;
                        for (int p = 0; p < i; p++)
                            for (int e = 0; e < map.GetNode(p).NextNodeCount; e++)
                                incoming |= map.GetNode(p).GetNextNodeIndex(e) == i;
                        Assert.That(incoming, Is.True, "Unreachable " + node.Id);
                    }
                }
            }
        }
        [TestCase(CombatApproach.Execution)]
        [TestCase(CombatApproach.Recording)]
        [TestCase(CombatApproach.Tuning)]
        [TestCase(CombatApproach.Observation)]
        public void FreshStartOptInRoundTripsAndCannotBeEnabledAgain(CombatApproach approach)
        {
            var run = Start(approach);
            Assert.That(run.HasOpeningRoute, Is.True);
            Assert.That(run.TryEnableOpeningRoute(), Is.False);
            run = Restore(run);
            Assert.That(run.TrySelectMapNode(0), Is.True);
            Assert.That(run.CurrentBattle.Battle.Approach, Is.EqualTo(approach));
            Assert.That(run.CurrentBattle.Battle.Enemies.Count, Is.EqualTo(2));
            Assert.That(run.TryEnableOpeningRoute(), Is.False);
            Restore(run);
        }
        [Test]
        public void Rules24JournalRetainsOldIntroAndRejectsNewCommand()
        {
            var run = PrototypeRunFactory.Create(302, PrototypeCharacterCatalog.All[0]);
            run.TryChooseApproach(CombatApproach.Execution); run.TrySelectMapNode(0);
            var bytes = AsRules24(run);
            Assert.That(RunSaveStore.TryDecode(bytes, out var data), Is.True);
            Assert.That(RunGameSession.TryRestore(data, out var restored, out _), Is.True);
            Assert.That(restored.HasOpeningRoute, Is.False);
            Assert.That(restored.CurrentBattle.Battle.Enemies.Count, Is.EqualTo(3));
            Assert.That(restored.TryEnableOpeningRoute(), Is.False);
            Assert.That(RunSaveStore.TryDecode(AsRules24(Start(CombatApproach.Execution)), out _), Is.False);
        }
        private static byte[] AsRules24(RunGameSession run)
        {
            var bytes = RunSaveStore.Encode(run.CaptureSave());
            Array.Copy(BitConverter.GetBytes(24), 0, bytes, 8, 4);
            using (var hash = SHA256.Create())
                Array.Copy(hash.ComputeHash(bytes, 0, bytes.Length - 32), 0, bytes, bytes.Length - 32, 32);
            return bytes;
        }
        [TestCase(CombatApproach.Execution)]
        [TestCase(CombatApproach.Recording)]
        [TestCase(CombatApproach.Tuning)]
        [TestCase(CombatApproach.Observation)]
        public void ActualIntroVictoryPreparationAndApplicationBattleReplay(CombatApproach approach)
        {
            var run = Start(approach);
            run.TrySelectMapNode(0);
            for (int turn = 0; turn < 16 && run.Phase == RunPhase.Battle; turn++)
            {
                var b = run.CurrentBattle.Battle;
                EnemyState target = null;
                foreach (var enemy in b.Enemies) if (enemy.IsAlive) { target = enemy; break; }
                run.TryMovePlayerTo(Math.Max(.5, target.X - 3), target.Y);
                foreach (string id in new[] { "frag.ellipse", "frag.expand" })
                    for (int i = 0; i < run.CurrentBattle.Deck.HandCount; i++)
                        if (run.CurrentBattle.Deck.GetHandCard(i).Id == id) { run.TryPlayHandCard(i, out _, out _); break; }
                if (approach == CombatApproach.Tuning)
                    run.TryUseDiagramAbility(Math.Max(.5, target.X - 3), target.Y);
                if (approach == CombatApproach.Recording)
                    run.TryUseDiagramAbility(b.Equation.Fragments.OriginX, b.Equation.Fragments.OriginY);
                if (approach == CombatApproach.Observation)
                    run.TryPlaceSatellite(Math.Min(9.5, b.Equation.Fragments.OriginX + 2.8), b.Equation.Fragments.OriginY);
                Assert.That(run.TryBeginPlot(), Is.True);
                run.ResolvePlot();
                if (run.Phase == RunPhase.Battle) run.ResolveEnemyTurn();
                run = Restore(run);
            }
            Assert.That(run.Phase, Is.EqualTo(RunPhase.CardReward));
            Assert.That(run.LastExplorationPoints, Is.EqualTo(2));
            Assert.That(run.Growth.Points, Is.EqualTo(8));
            Assert.That(run.TrySkipReward(), Is.True);
            int workshop = -1;
            for (int i = 0; i < run.Map.Definition.NodeCount; i++)
                if (run.Map.IsAvailable(i) && run.Map.Definition.GetNode(i).Kind == RunNodeKind.Workshop) workshop = i;
            Assert.That(workshop, Is.GreaterThanOrEqualTo(0));
            Assert.That(run.TrySelectMapNode(workshop), Is.True);
            run = Restore(run);
            Assert.That(run.TryChooseRoomOption(0), Is.True);
            Assert.That(run.TrainingPower, Is.EqualTo(1));
            Assert.That(run.TryChooseRoomOption(0), Is.False);
            run = Restore(run);
            for (int i = 0; i < run.Map.Definition.NodeCount; i++)
                if (run.Map.IsAvailable(i)) { run.TrySelectMapNode(i); break; }
            Assert.That(run.CurrentBattle.Battle.PlotDamageBonus, Is.EqualTo(1));
            Assert.That(run.CurrentBattle.Battle.Approach, Is.EqualTo(approach));
            Restore(run);
        }
        [Test]
        public void WaveOffersRealMultiKillWithEarlyTrainingAndMastery()
        {
            var map = Start(CombatApproach.Tuning).Map.Definition;
            BattleDefinition wave = null;
            for (int i = 0; i < map.NodeCount; i++)
                if (map.GetNode(i).Layer == 3 && map.GetNode(i).Kind == RunNodeKind.Battle) { wave = map.GetNode(i).Battle; break; }
            var enemies = new EnemyDefinition[wave.EnemyCount];
            for (int i = 0; i < enemies.Length; i++) enemies[i] = wave.GetEnemy(i);
            var b = new BattleSession(new BattleDefinition(wave.PlayerMaxHealth, wave.PlayerMaxEnergy, enemies,
                wave.Archetype, fragments: true, approach: CombatApproach.Tuning), wave.PlayerMaxHealth, 0, 2);
            b.TryPlayCard(FragmentCardCatalog.Find("frag.ellipse"), out _);
            b.TryPlayCard(FragmentCardCatalog.Find("frag.expand"), out _);
            int best = 0, bestAngle = 0; double bestX = 4, bestY = -2;
            for (double x = 3; x <= 6; x += .5)
                for (double y = -2; y <= 1; y += .5)
                    for (int angle = -90; angle <= 90; angle += 30)
                    {
                        var preview = b.PreviewDiagramAbility(x, y, angle);
                        if (preview == null) continue;
                        int kills = 0;
                        foreach (var enemy in b.Enemies)
                        {
                            int damage = EquationAnalyzer.CalculateIntersectionDamage(preview, enemy.X, enemy.Y, BattleSession.EnemyHitRadius);
                            if (damage > 0 && damage + 2 + b.WeaveDamageBonus >= enemy.Health) kills++;
                        }
                        if (kills > best) { best = kills; bestX = x; bestY = y; bestAngle = angle; }
                    }
            Assert.That(best, Is.GreaterThanOrEqualTo(2));
            Assert.That(b.TryUseDiagramAbility(bestX, bestY, bestAngle), Is.True);
            b.TryBeginPlot(); b.ResolvePlot();
            int dead = 0; foreach (var enemy in b.Enemies) if (!enemy.IsAlive) dead++;
            Assert.That(dead, Is.GreaterThanOrEqualTo(2));
        }
        [TestCase(false)] [TestCase(true)]
        public void RouteAwardsRespectSavedBudgetAndNeverDoubleComplete(bool bounded)
        {
            var run = Start(CombatApproach.Execution);
            if(!bounded){var old=run.CaptureSave();System.Array.Resize(ref old.Commands,12);Assert.That(RunGameSession.TryRestore(old,out run,out _),Is.True);}
            // Progression-only fixture: combat outcomes are tested separately with real commands.
            var award = typeof(RunGameSession).GetMethod("AwardExploration", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int depth = 0; depth < 23; depth++)
            {
                int selected = -1;
                for (int i = 0; i < run.Map.Definition.NodeCount; i++) if (run.Map.IsAvailable(i)) { selected = i; break; }
                var node = run.Map.Definition.GetNode(selected);
                Assert.That(run.Map.TryEnterNode(selected), Is.True);
                Assert.That(run.Map.TryCompleteActiveNode(), Is.True);
                Assert.That(run.Map.TryCompleteActiveNode(), Is.False);
                award.Invoke(run, new object[] { node.Kind });
                if (depth == 0) Assert.That(run.Growth.Points, Is.EqualTo(8));
            }
            Assert.That(run.Growth.Points, Is.EqualTo(bounded?9:35));
            if(bounded)Assert.That(run.LastExplorationPoints,Is.Zero);
        }
    }
}

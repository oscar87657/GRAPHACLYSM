using System;
using System.IO;
using Graphaclysm.Application;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using NUnit.Framework;

namespace Graphaclysm.Tests.Application
{
    public sealed class SaveAndPreferencesTests
    {
        private string directory;
        [SetUp] public void SetUp() { directory = Path.Combine(Path.GetTempPath(), "GraphaclysmSaveTests", Guid.NewGuid().ToString("N")); }
        [TearDown] public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        private static RunGameSession Start(int character = 0)
        {
            var run = PrototypeRunFactory.Create(302, PrototypeCharacterCatalog.All[character]);
            Assert.That(run.TrySelectMapNode(0), Is.True);
            return run;
        }

        private static bool TryAnyMove(RunGameSession run)
            => run.TryMovePlayer(-1.5, 0) || run.TryMovePlayer(1.5, 0)
                || run.TryMovePlayer(0, 1.5) || run.TryMovePlayer(0, -1.5);

        private static RunGameSession RoundTrip(RunGameSession original)
        {
            Assert.That(RunSaveStore.TryDecode(RunSaveStore.Encode(original.CaptureSave()), out var data), Is.True);
            Assert.That(RunGameSession.TryRestore(data, out var restored, out _), Is.True);
            EqualState(original, restored); return restored;
        }

        internal static void EqualState(RunGameSession a, RunGameSession b)
        {
            Assert.That(b.Seed, Is.EqualTo(a.Seed)); Assert.That(b.Phase, Is.EqualTo(a.Phase));
            Assert.That(b.PlayerHealth, Is.EqualTo(a.PlayerHealth)); Assert.That(b.Resonance, Is.EqualTo(a.Resonance));
            Assert.That(b.PlayerMaxHealth, Is.EqualTo(a.PlayerMaxHealth));
            Assert.That(b.Growth.Level, Is.EqualTo(a.Growth.Level)); Assert.That(b.Growth.Experience, Is.EqualTo(a.Growth.Experience));
            Assert.That(b.Growth.Points, Is.EqualTo(a.Growth.Points)); Assert.That(b.Growth.ActiveVariant, Is.EqualTo(a.Growth.ActiveVariant));
            Assert.That(b.Growth.ModuleVariant, Is.EqualTo(a.Growth.ModuleVariant));
            Assert.That(b.Growth.UltimateVariant, Is.EqualTo(a.Growth.UltimateVariant));
            Assert.That(b.Map.ActiveNodeIndex, Is.EqualTo(a.Map.ActiveNodeIndex));
            Assert.That(b.Map.LastCompletedNodeIndex, Is.EqualTo(a.Map.LastCompletedNodeIndex));
            Assert.That(b.Deck.Count, Is.EqualTo(a.Deck.Count)); Assert.That(b.Relics.Count, Is.EqualTo(a.Relics.Count));
            for (int i = 0; i < a.Map.Definition.NodeCount; i++) Assert.That(b.Map.IsCompleted(i), Is.EqualTo(a.Map.IsCompleted(i)));
            for (int i = 0; i < a.Deck.Count; i++) Assert.That(b.Deck[i].Id, Is.EqualTo(a.Deck[i].Id));
            for (int i = 0; i < a.Relics.Count; i++) Assert.That(b.Relics.GetRelic(i).Id, Is.EqualTo(a.Relics.GetRelic(i).Id));
            for (int i = 0; i < 3; i++)
            { Assert.That(b.RewardOptions[i]?.Id, Is.EqualTo(a.RewardOptions[i]?.Id)); Assert.That(b.RelicRewardOptions[i]?.Id, Is.EqualTo(a.RelicRewardOptions[i]?.Id)); }
            if (a.CurrentBattle == null) { Assert.That(b.CurrentBattle, Is.Null); return; }
            var x = a.CurrentBattle.Battle; var y = b.CurrentBattle.Battle;
            Assert.That(y.TerrainCount, Is.EqualTo(x.TerrainCount));
            for (int i = 0; i < x.TerrainCount; i++)
            {
                Assert.That(y.GetTerrain(i).Kind, Is.EqualTo(x.GetTerrain(i).Kind));
                Assert.That(y.GetTerrain(i).X, Is.EqualTo(x.GetTerrain(i).X));
                Assert.That(y.GetTerrain(i).Y, Is.EqualTo(x.GetTerrain(i).Y));
            }
            Assert.That(y.Phase, Is.EqualTo(x.Phase)); Assert.That(y.Turn, Is.EqualTo(x.Turn));
            Assert.That(y.PlayerHealth, Is.EqualTo(x.PlayerHealth)); Assert.That(y.CondenseCount, Is.EqualTo(x.CondenseCount));
            Assert.That(y.SealedCardCount, Is.EqualTo(x.SealedCardCount)); Assert.That(y.PlayedCardCount, Is.EqualTo(x.PlayedCardCount));
            Assert.That(y.PendingDrawBonus, Is.EqualTo(x.PendingDrawBonus));
            Assert.That(y.Tactics.X, Is.EqualTo(x.Tactics.X)); Assert.That(y.Tactics.Y, Is.EqualTo(x.Tactics.Y));
            Assert.That(y.Tactics.HasMoved, Is.EqualTo(x.Tactics.HasMoved));
            Assert.That(y.CombatSkillCooldown, Is.EqualTo(x.CombatSkillCooldown));
            Assert.That(y.Tactics.UltimateArmed, Is.EqualTo(x.Tactics.UltimateArmed)); Assert.That(y.Tactics.Resonance, Is.EqualTo(x.Tactics.Resonance));
            for (int i = 0; i < CombatStatusState.Capacity; i++)
            { Assert.That(y.Tactics.Statuses.Get((CombatStatusKind)i), Is.EqualTo(x.Tactics.Statuses.Get((CombatStatusKind)i))); Assert.That(y.Tactics.Statuses.Duration((CombatStatusKind)i), Is.EqualTo(x.Tactics.Statuses.Duration((CombatStatusKind)i))); }
            for (int i = 0; i < x.Enemies.Count; i++)
            {
                var e = x.Enemies[i]; var f = y.Enemies[i];
                Assert.That(f.Health, Is.EqualTo(e.Health)); Assert.That(f.X, Is.EqualTo(e.X)); Assert.That(f.Y, Is.EqualTo(e.Y));
                Assert.That(f.AimX, Is.EqualTo(e.AimX)); Assert.That(f.AimY, Is.EqualTo(e.AimY));
                for (int j = 0; j < CombatStatusState.Capacity; j++)
                { Assert.That(f.Statuses.Get((CombatStatusKind)j), Is.EqualTo(e.Statuses.Get((CombatStatusKind)j))); Assert.That(f.Statuses.Duration((CombatStatusKind)j), Is.EqualTo(e.Statuses.Duration((CombatStatusKind)j))); }
            }
            for (int i = 0; i < 48; i++)
            {
                x.Equation.Sample(i / 48d, out var ax, out var ay); y.Equation.Sample(i / 48d, out var bx, out var by);
                Assert.That(bx, Is.EqualTo(ax)); Assert.That(by, Is.EqualTo(ay));
            }
            var d = a.CurrentBattle.Deck; var e2 = b.CurrentBattle.Deck;
            Assert.That(e2.HandCount, Is.EqualTo(d.HandCount)); Assert.That(e2.DrawCount, Is.EqualTo(d.DrawCount));
            Assert.That(e2.DiscardCount, Is.EqualTo(d.DiscardCount)); Assert.That(e2.ReservedCount, Is.EqualTo(d.ReservedCount));
            for (int i = 0; i < d.HandCount; i++) Assert.That(e2.GetHandCard(i).Id, Is.EqualTo(d.GetHandCard(i).Id));
        }

        [TestCase(0)] [TestCase(1)] public void SavesInitialMapAndCharacter(int character)
        {
            var run = PrototypeRunFactory.Create(202, PrototypeCharacterCatalog.All[character]);
            var restored = RoundTrip(run);
            var flow = new PrototypeGameFlow(PrototypeCharacterCatalog.All, 1);
            Assert.That(flow.TryContinueRun(restored, PrototypeCharacterCatalog.All[character]), Is.True);
            Assert.That(flow.CurrentCharacter.Id, Is.EqualTo(PrototypeCharacterCatalog.All[character].Id));
        }

        [Test] public void MidTurnSavePreservesCardOrderAndIndependentUndo()
        {
            var run = Start(); Assert.That(TryAnyMove(run), Is.True);
            Assert.That(run.TryPlayHandCard(2, out _, out _), Is.True); Assert.That(run.TryPlayHandCard(0, out _, out _), Is.True);
            var restored = RoundTrip(run);
            Assert.That(restored.TryUndoMove(), Is.EqualTo(run.TryUndoMove()));
            Assert.That(restored.TryUndoLastPlayedCard(out var b), Is.EqualTo(run.TryUndoLastPlayedCard(out var a)));
            Assert.That(b.Id, Is.EqualTo(a.Id)); EqualState(run, restored);
        }

        [Test] public void CondenseSavePreservesSealsCostsBonusConsumptionAndFutureDraws()
        {
            var run = Start(1); run.TryMovePlayer(-1.5, 0); run.TryPlayHandCard(0, out _, out _);
            Assert.That(run.TryCondense(), Is.True); RoundTrip(run); run.ResolveEnemyTurn();
            var restored = RoundTrip(run);
            Assert.That(restored.TryUndoLastPlayedCard(out _), Is.False);
            Assert.That(restored.TryCondense(), Is.EqualTo(run.TryCondense()));
            restored.ResolveEnemyTurn(); run.ResolveEnemyTurn(); EqualState(run, restored);
            Assert.That(restored.TryCondense(), Is.False);
            Assert.That(restored.TryUnravel(), Is.EqualTo(run.TryUnravel()));
            restored.ResolveEnemyTurn(); run.ResolveEnemyTurn(); EqualState(run, restored);
            Assert.That(restored.CurrentBattle.Battle.CondenseCount, Is.Zero);
        }

        [Test] public void SavesBothSidesOfImpactWithoutDuplicatingDamage()
        {
            var run = Start(); run.TryPlayHandCard(0, out _, out _); Assert.That(run.TryBeginPlot(), Is.True);
            var before = RoundTrip(run);
            var a = run.ResolvePlot(); var b = before.ResolvePlot(); Assert.That(b.TotalDamage, Is.EqualTo(a.TotalDamage)); EqualState(run, before);
            var after = RoundTrip(run);
            if (run.Phase == RunPhase.Battle)
            { run.ResolveEnemyTurn(); after.ResolveEnemyTurn(); EqualState(run, after); }
        }

        [Test] public void InvalidAndFailedCommandsDoNotChangeSaveRevision()
        {
            var run = Start(); int revision = run.Revision;
            Assert.That(run.TryPlayHandCard(-1, out _, out _), Is.False);
            Assert.That(run.TryPlayHandCard(99, out _, out _), Is.False);
            Assert.That(run.TryBeginPlot(), Is.False); Assert.That(run.TrySelectReward(0), Is.False);
            Assert.That(run.Revision, Is.EqualTo(revision)); RoundTrip(run);
        }

        [Test] public void CorruptTruncatedOrUnknownVersionBytesAreRejected()
        {
            byte[] bytes = RunSaveStore.Encode(Start().CaptureSave());
            bytes[4] = 99; Assert.That(RunSaveStore.TryDecode(bytes, out _), Is.False);
            Assert.That(RunSaveStore.TryDecode(new byte[8], out _), Is.False);
            Assert.That(RunSaveStore.TryDecode(null, out _), Is.False);
        }

        [Test] public void SemanticallyInvalidReplayDoesNotReplaceActiveFlow()
        {
            var flow = new PrototypeGameFlow(PrototypeCharacterCatalog.All, 99); flow.OpenCharacterSelection(); flow.TryStartRun();
            var original = flow.CurrentRun;
            var data = Start().CaptureSave(); data.Commands = new[] { new RunCommand(RunCommandKind.ResolveEnemy) };
            Assert.That(RunGameSession.TryRestore(data, out _, out _), Is.False);
            data.CharacterId = "missing"; Assert.That(RunGameSession.TryRestore(data, out _, out _), Is.False);
            Assert.That(flow.CurrentRun, Is.SameAs(original));
        }

        [Test] public void AtomicSaveAndBackupRecoverAfterCorruptionAndRemainRecoverable()
        {
            var store = new RunSaveStore(directory); var run = Start();
            Assert.That(store.TrySave(run, out _), Is.True);
            run.TryPlayHandCard(0, out _, out _); Assert.That(store.TrySave(run, out _), Is.True);
            File.WriteAllText(store.FilePath, "interrupted write");
            Assert.That(store.TryLoad(out var restored, out _, out var message), Is.True);
            Assert.That(message, Does.Contain("복구")); Assert.That(restored.CurrentBattle.Battle.PlayedCardCount, Is.Zero);
            Assert.That(store.TrySave(restored, out _), Is.True);
            File.WriteAllText(store.FilePath, "corrupt again");
            Assert.That(store.TryLoad(out _, out _, out _), Is.True);
        }

        [Test] public void FileWriteFailureReturnsFailureAndKeepsExistingSave()
        {
            var store = new RunSaveStore(directory); var run = Start(); store.TrySave(run, out _);
            byte[] original = File.ReadAllBytes(store.FilePath);
            Directory.CreateDirectory(store.FilePath + ".tmp");
            Assert.That(store.TrySave(run, out string error), Is.False); Assert.That(error, Is.Not.Empty);
            CollectionAssert.AreEqual(original, File.ReadAllBytes(store.FilePath));
        }

        [Test] public void TerminalSaveCannotResurrectPreviousBattle()
        {
            var run = Start();
            for (int i = 0; i < 100 && run.Phase == RunPhase.Battle; i++)
            { Assert.That(run.TryUnravel(), Is.True); run.ResolveEnemyTurn(); }
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Defeated));
            var restored = RoundTrip(run);
            var flow = new PrototypeGameFlow(PrototypeCharacterCatalog.All, 4);
            Assert.That(flow.TryContinueRun(restored, PrototypeCharacterCatalog.All[0]), Is.False);
        }

        [Test] public void PreferencesPersistClampAndRecoverFromBackup()
        {
            var store = new GamePreferencesStore(directory);
            var defaults = store.Load(); Assert.That(defaults.MasterVolume, Is.EqualTo(80));
            var value = new GamePreferences { MasterVolume = 250, EffectsVolume = -10, Resolution = 99, Fullscreen = false, ReduceMotion = true, TutorialCompleted = true };
            Assert.That(store.TrySave(value), Is.True);
            var restored = store.Load(); Assert.That(restored.MasterVolume, Is.EqualTo(100)); Assert.That(restored.EffectsVolume, Is.Zero);
            Assert.That(restored.Resolution, Is.EqualTo(2)); Assert.That(restored.Fullscreen, Is.False); Assert.That(restored.TutorialCompleted, Is.True);
            store.TrySave(value); File.WriteAllText(Path.Combine(directory, "settings.save"), "broken");
            Assert.That(store.Load().ReduceMotion, Is.True);
        }

        [Test] public void FinishedRunBackupAlsoStaysFinished()
        {
            var run = Start(); var store = new RunSaveStore(directory); store.TrySave(run, out _);
            while (run.Phase == RunPhase.Battle) { run.TryUnravel(); run.ResolveEnemyTurn(); }
            Assert.That(store.TrySave(run, out _), Is.True);
            File.WriteAllText(store.FilePath, "broken");
            Assert.That(store.TryLoad(out var restored, out _, out _), Is.True);
            Assert.That(restored.Phase, Is.EqualTo(RunPhase.Defeated));
        }

        [TestCase(202u)] [TestCase(302u)]
        public void GeneratedJourneyRestoresRewardsRoomsAndNextFloor(uint seed)
        {
            var run = PrototypeRunFactory.Create(seed, PrototypeCharacterCatalog.All[1]);
            bool cardReward = false, relicReward = false, room = false;
            int commands = 0;
            while (run.CurrentFloor < 2 && run.Phase != RunPhase.Defeated && commands++ < 250)
            {
                switch (run.Phase)
                {
                    case RunPhase.MapSelection:
                        RoundTrip(run);
                        for (int i = 0; i < run.Map.Definition.NodeCount; i++)
                            if (run.Map.IsAvailable(i)) { run.TrySelectMapNode(i); break; }
                        break;
                    case RunPhase.Battle:
                        PlayPlanningTurn(run);
                        if (run.Phase == RunPhase.Battle && run.CurrentBattle.Battle.Phase == BattlePhase.EnemyTurn) run.ResolveEnemyTurn();
                        break;
                    case RunPhase.CardReward:
                        cardReward = true; RoundTrip(run); Assert.That(run.TrySelectReward(0), Is.True); break;
                    case RunPhase.RelicReward:
                        relicReward = true; RoundTrip(run); Assert.That(run.TrySelectRelicReward(0), Is.True); break;
                    case RunPhase.Room:
                        room = true; RoundTrip(run);
                        var left = RoundTrip(run); Assert.That(left.TryLeaveRoom(), Is.True); RoundTrip(left);
                        for (int option = 0; option < run.CurrentRoom.ChoiceCount; option++)
                        {
                            var branch = RoundTrip(run);
                            if (!branch.TryChooseRoomOption(option)) continue;
                            RoundTrip(branch);
                            if (branch.Phase == RunPhase.DeckRefinement)
                            {
                                var skipped = RoundTrip(branch); Assert.That(skipped.TrySkipRefinement(), Is.True); RoundTrip(skipped);
                                Assert.That(branch.TryRemoveDeckCard(0), Is.True); RoundTrip(branch);
                            }
                            else if (branch.Phase == RunPhase.CardReward || branch.Phase == RunPhase.RelicReward)
                            { Assert.That(branch.TrySkipReward(), Is.True); RoundTrip(branch); }
                        }
                        if (!run.TryChooseRoomOption(0)) run.TryLeaveRoom();
                        break;
                    case RunPhase.DeckRefinement:
                        RoundTrip(run); Assert.That(run.TryRemoveDeckCard(0), Is.True); break;
                    default: Assert.Fail("Unexpected journey phase " + run.Phase); break;
                }
            }
            RoundTrip(run);
            Assert.That(cardReward && relicReward && room, Is.True, "Generated journey did not exercise rewards and rooms: " + run.Phase);
            Assert.That(run.CurrentFloor, Is.EqualTo(2), "Could not reach the floor transition in " + commands + " commands");
            if (run.Phase == RunPhase.RelicReward) { Assert.That(run.TrySelectRelicReward(0), Is.True); RoundTrip(run); }
            if (run.Phase == RunPhase.CardReward) { Assert.That(run.TrySelectReward(0), Is.True); RoundTrip(run); }
            for (int i = 0; i < run.Map.Definition.NodeCount; i++)
                if (run.Map.IsAvailable(i)) { run.TrySelectMapNode(i); break; }
            RoundTrip(run);
        }

        // A bounded deterministic player for persistence coverage. It uses normal run commands;
        // speculative card probes are completely undone before selecting the actual move/weave.
        private static void PlayPlanningTurn(RunGameSession run)
        {
            var battle = run.CurrentBattle.Battle; var deck = run.CurrentBattle.Deck;
            if (battle.CanUseCombatSkill)
            {
                run.TryUseCombatSkill();
                if (run.Phase != RunPhase.Battle) return;
            }
            if (battle.Tactics.Resonance == 6) run.TryToggleUltimate();
            int[] best = new int[3], path = new int[3]; int bestCount = 0, bestMove = -1;
            bool[] used = new bool[8]; double bestScore = double.NegativeInfinity;
            for (int move = -1; move < 4; move++)
            {
                double dx = move == 0 ? -1.5 : move == 1 ? 1.5 : 0, dy = move == 2 ? 1.5 : move == 3 ? -1.5 : 0;
                if (move >= 0 && !battle.TryMovePlayer(dx, dy)) continue;
                SearchWeave(run, path, used, 0, move, best, ref bestCount, ref bestMove, ref bestScore);
                if (move >= 0) Assert.That(battle.TryUndoMove(), Is.True);
            }
            if (bestMove >= 0) run.TryMovePlayer(bestMove == 0 ? -1.5 : bestMove == 1 ? 1.5 : 0, bestMove == 2 ? 1.5 : bestMove == 3 ? -1.5 : 0);
            for (int i = 0; i < bestCount; i++)
            {
                int index = best[i]; for (int j = 0; j < i; j++) if (best[j] < best[i]) index--;
                Assert.That(run.TryPlayHandCard(index, out _, out _), Is.True);
            }
            if (run.TryBeginPlot()) run.ResolvePlot(); else run.TryUnravel();
        }

        private static void SearchWeave(RunGameSession run, int[] path, bool[] used, int depth, int move,
            int[] best, ref int bestCount, ref int bestMove, ref double bestScore)
        {
            var battle = run.CurrentBattle.Battle; var deck = run.CurrentBattle.Deck;
            if (depth > 0)
            {
                double score = -depth * .2;
                for (int i = 0; i < battle.Enemies.Count; i++)
                {
                    var e = battle.Enemies[i]; if (!e.IsAlive) continue;
                    int damage = battle.PreviewDamage(e); score += Math.Min(damage, e.Health) * 2;
                    if (damage >= e.Health) score += 12;
                    double dx = battle.Tactics.X - e.AimX, dy = battle.Tactics.Y - e.AimY;
                    if (e.Intent.Kind == EnemyIntentKind.Attack && dx * dx + dy * dy < e.AimRadius * e.AimRadius) score -= e.Intent.Damage * 3;
                }
                if (battle.PreviewPlayerHit) score += 5;
                if (score > bestScore)
                { bestScore = score; bestCount = depth; bestMove = move; Array.Copy(path, best, depth); }
            }
            if (depth == path.Length) return;
            for (int i = 0; i < deck.HandCount; i++)
            {
                if (used[i] || !battle.TryPlayCard(deck.GetHandCard(i), out _)) continue;
                used[i] = true; path[depth] = i;
                SearchWeave(run, path, used, depth + 1, move, best, ref bestCount, ref bestMove, ref bestScore);
                used[i] = false; Assert.That(battle.TryUndoLastCard(out _), Is.True);
            }
        }
    }
}

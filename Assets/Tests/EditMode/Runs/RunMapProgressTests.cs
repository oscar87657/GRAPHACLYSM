using System;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Runs;
using NUnit.Framework;

namespace Graphaclysm.Tests.Runs
{
    public sealed class RunMapProgressTests
    {
        [Test]
        public void CompletingNode_OnlyUnlocksItsOutgoingBranches()
        {
            RunMapProgress progress = new RunMapProgress(CreateBranchingMap());

            Assert.That(progress.IsAvailable(0), Is.True);
            Assert.That(progress.IsAvailable(1), Is.True);
            Assert.That(progress.IsAvailable(2), Is.False);
            Assert.That(progress.IsAvailable(3), Is.False);

            Assert.That(progress.TryEnterNode(0), Is.True);
            Assert.That(progress.TryCompleteActiveNode(), Is.True);

            Assert.That(progress.IsCompleted(0), Is.True);
            Assert.That(progress.IsAvailable(2), Is.True);
            Assert.That(progress.IsAvailable(3), Is.False);
        }

        [Test]
        public void TerminalNode_CompletesMap()
        {
            RunMapProgress progress = new RunMapProgress(CreateBranchingMap());
            progress.TryEnterNode(1);
            progress.TryCompleteActiveNode();
            progress.TryEnterNode(3);

            Assert.That(progress.TryCompleteActiveNode(), Is.True);
            Assert.That(progress.Phase, Is.EqualTo(RunMapProgressPhase.Completed));
            Assert.That(progress.CompletedNodeCount, Is.EqualTo(2));
        }

        [Test]
        public void Definition_RejectsBackwardEdge()
        {
            BattleDefinition battle = CreateBattle();
            var nodes = new[]
            {
                new RunMapNodeDefinition("start", "Start", 0, 0, RunNodeKind.Battle, battle, Array.Empty<int>()),
                new RunMapNodeDefinition("bad", "Bad", 1, 0, RunNodeKind.Boss, battle, new[] { 0 })
            };

            Assert.That(() => new RunMapDefinition(nodes), Throws.ArgumentException);
        }

        private static RunMapDefinition CreateBranchingMap()
        {
            BattleDefinition battle = CreateBattle();
            return new RunMapDefinition(new[]
            {
                new RunMapNodeDefinition("left", "Left", 0, 0, RunNodeKind.Battle, battle, new[] { 2 }),
                new RunMapNodeDefinition("right", "Right", 0, 1, RunNodeKind.Battle, battle, new[] { 3 }),
                new RunMapNodeDefinition("left.end", "Left End", 1, 0, RunNodeKind.Boss, battle, Array.Empty<int>()),
                new RunMapNodeDefinition("right.end", "Right End", 1, 1, RunNodeKind.Boss, battle, Array.Empty<int>())
            });
        }

        private static BattleDefinition CreateBattle()
        {
            return new BattleDefinition(
                40,
                3,
                new[] { new EnemyDefinition("test", "Test", 5.0, 0.0, 1, 0) });
        }
    }
}

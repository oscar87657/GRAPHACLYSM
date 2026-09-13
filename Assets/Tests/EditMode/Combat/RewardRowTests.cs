using System;
using System.Security.Cryptography;
using Graphaclysm.Application;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using NUnit.Framework;
namespace Graphaclysm.Tests.Combat
{
    public sealed class RewardRowTests
    {
        [TestCase(CombatApproach.Execution)] [TestCase(CombatApproach.Recording)]
        [TestCase(CombatApproach.Tuning)] [TestCase(CombatApproach.Observation)]
        public void NewRunDoesNotGrantAFreeRelicAndReplayKeepsTheDecision(CombatApproach approach)
        {
            var f=new PrototypeGameFlow(PrototypeCharacterCatalog.All,302);
            f.OpenCharacterSelection();f.TrySelectCharacter(approach>=CombatApproach.Tuning?1:0);
            Assert.That(f.TryStartRun(approach),Is.True);var r=f.CurrentRun;
            Assert.That(r.IsStartingRelic,Is.False);Assert.That(r.Relics.Count,Is.Zero);
            Assert.That(r.Phase,Is.EqualTo(RunPhase.MapSelection));Assert.That(r.Growth.Points,Is.EqualTo(3));
            Assert.That(r.Keys,Is.EqualTo(1));
            Assert.That(RunSaveStore.TryDecode(RunSaveStore.Encode(r.CaptureSave()),out var data),Is.True);
            Assert.That(RunGameSession.TryRestore(data,out var restored,out _),Is.True);
            Assert.That(restored.Relics.Count,Is.Zero);Assert.That(restored.IsStartingRelic,Is.False);
            Assert.That(restored.TrySelectMapNode(0),Is.True);
            Assert.That(f.TryRestartRun(),Is.True);Assert.That(f.CurrentRun.Phase,Is.EqualTo(RunPhase.MapSelection));Assert.That(f.CurrentRun.Relics.Count,Is.Zero);
        }
        [Test] public void PreviousFreeRelicChoiceRemainsReplayableAndOldHeaderRejectsNewArgument()
        {
            var f=new PrototypeGameFlow(PrototypeCharacterCatalog.All,302);f.OpenCharacterSelection();f.TrySelectCharacter(0);
            f.TryStartRun(CombatApproach.Execution,true,true);var old=f.CurrentRun.CaptureSave();
            Assert.That(RunGameSession.TryRestore(old,out var r,out _),Is.True);Assert.That(r.IsStartingRelic,Is.True);
            Assert.That(r.TrySelectRelicReward(0),Is.True);Assert.That(RunGameSession.TryRestore(r.CaptureSave(),out r,out _),Is.True);
            Assert.That(r.Relics.Count,Is.EqualTo(1));
            f.TryRestartRun();byte[] bytes=RunSaveStore.Encode(f.CurrentRun.CaptureSave());Array.Copy(BitConverter.GetBytes(37),0,bytes,8,4);
            using(var hash=SHA256.Create())Array.Copy(hash.ComputeHash(bytes,0,bytes.Length-32),0,bytes,bytes.Length-32,32);
            Assert.That(RunSaveStore.TryDecode(bytes,out _),Is.False);
        }
    }
}

using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Graphaclysm.Application;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Cards;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class LegacyTreeTests
    {
        private static LegacyProgression Fund()
        {var p=new LegacyProgression();for(uint i=1;i<=40;i++)p.AwardRun(i,true,5);return p;}
        private static RunGameSession Start(LegacyProgression p)
        {
            var flow=new PrototypeGameFlow(PrototypeCharacterCatalog.All,302){LegacyBenefits=p.Benefits,PreparationUpgrades=p.Preparation()};
            flow.OpenCharacterSelection();flow.TrySelectCharacter(1);
            Assert.That(flow.TryStartRun(CombatApproach.Tuning),Is.True);return flow.CurrentRun;
        }
        [Test] public void ExpansionHasConnectedPrerequisitesAndRealStackingBenefits()
        {
            var p=Fund();Assert.That(LegacyProgression.NodeCount,Is.EqualTo(24));
            for(int i=14;i<24;i++)
            {
                Assert.That(LegacyProgression.Prerequisite(i),Is.InRange(0,i-1));
                Assert.That(p.TryPurchase(i),Is.False,"Locked node "+i);
            }
            for(int i=0;i<24;i++)for(int r=0;r<p.GetNode(i).MaxRank;r++)Assert.That(p.TryPurchase(i),Is.True);
            Assert.That(p.Benefits.MaxHealth,Is.EqualTo(12));Assert.That(p.Benefits.VictoryHealing,Is.EqualTo(5));
            Assert.That(p.Benefits.StartingResonance,Is.EqualTo(4));Assert.That(p.Benefits.StartingShield,Is.EqualTo(9));
            Assert.That(p.Benefits.StartingExperience,Is.EqualTo(3));Assert.That(p.Benefits.RewardBonus,Is.EqualTo(3));
            Assert.That(p.Preparation() & 15360,Is.EqualTo(15360));
        }
        [Test] public void ExpansionResourcesAndOpeningHandSurviveActualBattleReplay()
        {
            var plain=Fund();foreach(int i in new[]{3,6,7,9})Assert.That(plain.TryPurchase(i),Is.True);
            var expanded=Fund();foreach(int i in new[]{3,6,7,9,20,21,22,23})Assert.That(expanded.TryPurchase(i),Is.True);
            var before=Start(plain);var after=Start(expanded);
            Assert.That(after.Coins,Is.EqualTo(before.Coins+8));Assert.That(after.Keys,Is.EqualTo(before.Keys+1));
            Assert.That(after.PermanentPlotPower,Is.EqualTo(before.PermanentPlotPower+1));
            Assert.That(before.TrySelectMapNode(0),Is.True);Assert.That(after.TrySelectMapNode(0),Is.True);
            Assert.That(after.PermanentOpeningHand,Is.EqualTo(1));
            Assert.That(after.CurrentBattle.Deck.HandCount,Is.EqualTo(Math.Min(after.Deck.Count,before.CurrentBattle.Deck.HandCount+1)));
            Assert.That(after.CurrentBattle.Battle.PlotDamageBonus,Is.EqualTo(before.CurrentBattle.Battle.PlotDamageBonus+1));
            Assert.That(RunSaveStore.TryDecode(RunSaveStore.Encode(after.CaptureSave()),out var data),Is.True);
            Assert.That(RunGameSession.TryRestore(data,out var restored,out _),Is.True);
            Assert.That(restored.CurrentBattle.Deck.HandCount,Is.EqualTo(after.CurrentBattle.Deck.HandCount));
            Assert.That(restored.PermanentPlotPower,Is.EqualTo(after.PermanentPlotPower));
            Assert.That(restored.Coins,Is.EqualTo(after.Coins));Assert.That(restored.Keys,Is.EqualTo(after.Keys));
            byte[] old=RunSaveStore.Encode(after.CaptureSave());Array.Copy(BitConverter.GetBytes(39),0,old,8,4);
            using(var hash=SHA256.Create())Array.Copy(hash.ComputeHash(old,0,old.Length-32),0,old,old.Length-32,32);
            Assert.That(RunSaveStore.TryDecode(old,out _),Is.False);
            old=RunSaveStore.Encode(before.CaptureSave());Array.Copy(BitConverter.GetBytes(39),0,old,8,4);
            using(var hash=SHA256.Create())Array.Copy(hash.ComputeHash(old,0,old.Length-32),0,old,old.Length-32,32);
            Assert.That(RunSaveStore.TryDecode(old,out data),Is.True);
            Assert.That(RunGameSession.TryRestore(data,out restored,out _),Is.True);
            Assert.That(restored.PermanentOpeningHand,Is.Zero);
            // Seven-card deck fixture: the default six-card deck is already entirely in hand.
            // Keep this non-journal fixture after the real save/replay assertions above.
            before.Deck.TryAdd(FragmentCardCatalog.All[0]);after.Deck.TryAdd(FragmentCardCatalog.All[0]);
            var start=typeof(RunGameSession).GetMethod("StartEncounter",BindingFlags.NonPublic|BindingFlags.Instance);
            start.Invoke(before,new object[]{before.Map.Definition.GetNode(0),0});
            start.Invoke(after,new object[]{after.Map.Definition.GetNode(0),0});
            Assert.That(after.CurrentBattle.Deck.HandCount,Is.EqualTo(before.CurrentBattle.Deck.HandCount+1));
            Assert.That(after.CurrentBattle.NextDrawCount,Is.EqualTo(before.CurrentBattle.NextDrawCount));
        }
        [Test] public void FourteenNodeAccountLoadsWithoutAnotherResetAndAddsNewRanks()
        {
            string directory=Path.Combine(Path.GetTempPath(),"LegacyExpansion-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            try
            {
                var ids=(string[])typeof(LegacyProgression).GetField("Ids",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
                byte[] payload;
                using(var stream=new MemoryStream())
                {
                    using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))
                    {writer.Write(0x4C474359);writer.Write(3);writer.Write(73);writer.Write(14);for(int i=0;i<14;i++){writer.Write(ids[i]);writer.Write(i==0?2:0);}writer.Write(1);writer.Write((uint)55);}
                    payload=stream.ToArray();
                }
                byte[] bytes=new byte[payload.Length+32];Array.Copy(payload,bytes,payload.Length);
                using(var hash=SHA256.Create())Array.Copy(hash.ComputeHash(payload),0,bytes,payload.Length,32);
                File.WriteAllBytes(Path.Combine(directory,"legacy.save"),bytes);
                var store=new LegacyProgressionStore(directory);var p=store.Load();
                Assert.That(p.IsReadOnly,Is.False);Assert.That(p.TreeResetApplied,Is.True);
                Assert.That(p.Currency,Is.EqualTo(73));Assert.That(p.GetRank(0),Is.EqualTo(2));
                for(int i=14;i<24;i++)Assert.That(p.GetRank(i),Is.Zero);
                Assert.That(p.TryResetForTree(store.TrySave),Is.False);Assert.That(p.AwardRun(55,true,5),Is.Zero);
                Assert.That(p.TryPurchase(14),Is.True);Assert.That(store.TrySave(p),Is.True);
                var loaded=store.Load();Assert.That(loaded.Currency,Is.EqualTo(69));Assert.That(loaded.GetRank(14),Is.EqualTo(1));
                Assert.That(loaded.GetRank(0),Is.EqualTo(2));Assert.That(loaded.TreeResetApplied,Is.True);
            }
            finally{Directory.Delete(directory,true);}
        }
        [Test] public void ResetRefundsEveryRankOnceAndPreservesSettlementHistory()
        {
            var p=new LegacyProgression();for(uint i=1;i<10;i++)p.AwardRun(i,true,5);
            p.TryPurchase(0);p.TryPurchase(0);p.TryPurchase(6);p.TryPurchase(11);p.TryPurchase(8);
            int before=p.Currency,refund=p.RefundValue;
            typeof(LegacyProgression).GetProperty("TreeResetApplied").SetValue(p,false);
            Assert.That(p.TryResetForTree(_=>false),Is.False);Assert.That(p.Currency,Is.EqualTo(before));
            Assert.That(p.TryResetForTree(_=>true),Is.True);Assert.That(p.Currency,Is.EqualTo(before+refund));
            for(int i=0;i<LegacyProgression.NodeCount;i++)Assert.That(p.GetRank(i),Is.Zero);
            Assert.That(p.AwardRun(1,true,5),Is.Zero);Assert.That(p.TryResetForTree(_=>true),Is.False);
            p.TryPurchase(8);Assert.That(p.TryResetForTree(_=>true),Is.False);Assert.That(p.GetRank(8),Is.EqualTo(1));
        }
        [Test] public void ResetMarkerPersistsAndKeepsTheOriginalBackup()
        {
            string directory=Path.Combine(Path.GetTempPath(),"LegacyTree-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            try
            {
                var store=new LegacyProgressionStore(directory);var p=new LegacyProgression();p.AwardRun(1,true,5);p.TryPurchase(0);
                typeof(LegacyProgression).GetProperty("TreeResetApplied").SetValue(p,false);
                Assert.That(store.TrySave(p),Is.True);byte[] original=File.ReadAllBytes(Path.Combine(directory,"legacy.save"));
                p=store.Load();Assert.That(p.TreeResetApplied,Is.False);Assert.That(p.TryResetForTree(store.TrySave),Is.True);
                var loaded=store.Load();Assert.That(loaded.TreeResetApplied,Is.True);Assert.That(loaded.Currency,Is.EqualTo(8));
                Assert.That(loaded.GetRank(0),Is.Zero);Assert.That(File.ReadAllBytes(Path.Combine(directory,"legacy.save.bak")),Is.EqualTo(original));
            }
            finally{Directory.Delete(directory,true);}
        }
    }
}

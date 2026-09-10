using System;
using System.IO;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Relics;
using Graphaclysm.Core.Runs;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class CombatGrowthV12Tests
    {
        private static BattleDefinition Battle(CombatArchetype archetype = CombatArchetype.Ian)
            => new BattleDefinition(50, 4,
                new[] { new EnemyDefinition("target", "Target", 5.6, -2, 100, 2) }, archetype, fragments: true);

        private static RelicDefinition Relic(string id)
        {
            foreach (var relic in FragmentRelicCatalog.All) if (relic.Id == id) return relic;
            throw new InvalidOperationException("Missing relic " + id);
        }

        [Test]
        public void RunGrowthLevelsPurchasesAndSwitchesVariantsWithoutLeakingIntoANewRun()
        {
            var growth = new RunGrowthState(CombatArchetype.Ian);
            growth.AddExperience(30);
            Assert.That(growth.Level, Is.GreaterThan(6));
            Assert.That(growth.TryPurchase(0), Is.True);
            Assert.That(growth.TryPurchase(2), Is.True);
            Assert.That(growth.ActiveVariant, Is.EqualTo(1));
            Assert.That(growth.TryPurchase(3), Is.True);
            Assert.That(growth.ActiveVariant, Is.EqualTo(2));
            Assert.That(growth.TrySelect(2), Is.True);
            Assert.That(growth.ActiveVariant, Is.EqualTo(1));
            Assert.That(growth.TryPurchase(1), Is.True);
            var resonanceBattle = new BattleSession(Battle(), 50, 0, 0, 0, null, growth.CreateLoadout());
            Assert.That(resonanceBattle.Tactics.Resonance, Is.EqualTo(1));
            var fresh = new RunGrowthState(CombatArchetype.Ian);
            Assert.That(fresh.Level, Is.EqualTo(1));
            Assert.That(fresh.Points, Is.Zero);
            Assert.That(fresh.ActiveSkillUnlocked, Is.False);
        }

        [Test]
        public void CharacterCombatSkillUsesSelectedRoleOncePerBattle()
        {
            var ian = new BattleSession(Battle(), 50, 0, 0, 0, null,
                new BattleSkillLoadout(true, 1, 0, 0));
            Assert.That(ian.TryUseCombatSkill(), Is.True);
            Assert.That(ian.Tactics.Statuses.Get(CombatStatusKind.Shield), Is.EqualTo(8));
            Assert.That(ian.Tactics.Statuses.Get(CombatStatusKind.Thorns), Is.EqualTo(3));
            Assert.That(ian.TryUseCombatSkill(), Is.False);

            var luna = new BattleSession(Battle(CombatArchetype.Luna), 50, 0, 0, 0, null,
                new BattleSkillLoadout(true, 0, 0, 0));
            Assert.That(luna.TryMovePlayer(-1.5, 0), Is.True);
            Assert.That(luna.TryUseCombatSkill(), Is.True);
            Assert.That(luna.Tactics.HasMoved, Is.False);
            Assert.That(luna.TryMovePlayer(0, 1.5), Is.True);
        }

        [Test]
        public void MomentumChangesTheNextGraphPreviewAndIsConsumedOnRelease()
        {
            var baseline = new BattleSession(Battle());
            Assert.That(baseline.TryPlayCard(FragmentCardCatalog.Find("frag.home"), out _), Is.True);
            int normal = baseline.PreviewDamage(baseline.Enemies[0]);
            Assert.That(normal, Is.GreaterThan(0));

            var boosted = new BattleSession(Battle(), 50, 0, 0, 0, null,
                new BattleSkillLoadout(true, 2, 0, 0));
            Assert.That(boosted.TryUseCombatSkill(), Is.True);
            Assert.That(boosted.TryPlayCard(FragmentCardCatalog.Find("frag.home"), out _), Is.True);
            Assert.That(boosted.PreviewDamage(boosted.Enemies[0]), Is.EqualTo(normal + 5));
            Assert.That(boosted.TryBeginPlot(), Is.True);
            boosted.ResolvePlot();
            Assert.That(boosted.Tactics.Statuses.Get(CombatStatusKind.Momentum), Is.Zero);
        }

        [Test]
        public void RuptureFortifyAndThornsResolveInTheirDeclaredOrder()
        {
            var ruptured = new BattleSession(Battle(), 50, 0, 0, 6, null,
                new BattleSkillLoadout(false, 0, 1, 0));
            Assert.That(ruptured.TryToggleUltimate(), Is.True);
            Assert.That(ruptured.TryPlayCard(FragmentCardCatalog.Find("frag.home"), out _), Is.True);
            Assert.That(ruptured.TryBeginPlot(), Is.True);
            ruptured.ResolvePlot();
            Assert.That(ruptured.Enemies[0].Statuses.Get(CombatStatusKind.Rupture), Is.EqualTo(3));
            ruptured.ResolveEnemyTurn();
            Assert.That(ruptured.TryPlayCard(FragmentCardCatalog.Find("frag.home"), out _), Is.True);
            int rupturedDamage = ruptured.PreviewDamage(ruptured.Enemies[0]);
            Assert.That(ruptured.TryBeginPlot(), Is.True);
            Assert.That(ruptured.ResolvePlot().TotalDamage, Is.EqualTo(rupturedDamage));
            Assert.That(ruptured.Enemies[0].Statuses.Get(CombatStatusKind.Rupture), Is.Zero);

            var fortifyRelics = new RunRelicCollection();
            Assert.That(fortifyRelics.TryAdd(Relic("relic.astral_key")), Is.True);
            var fortified = new BattleSession(Battle(), 50, 0, 0, 0, fortifyRelics);
            Assert.That(fortified.TryUnravel(), Is.True);
            Assert.That(fortified.ResolveEnemyTurn(), Is.Zero);
            Assert.That(fortified.PlayerHealth, Is.EqualTo(fortified.PlayerMaxHealth));

            var thornRelics = new RunRelicCollection();
            Assert.That(thornRelics.TryAdd(Relic("relic.thorn_circlet")), Is.True);
            var thorned = new BattleSession(Battle(), 50, 0, 0, 0, thornRelics);
            int enemyHealth = thorned.Enemies[0].Health;
            Assert.That(thorned.TryUnravel(), Is.True);
            Assert.That(thorned.ResolveEnemyTurn(), Is.EqualTo(2));
            Assert.That(thorned.Enemies[0].Health, Is.EqualTo(enemyHealth - 3));
        }

        [Test]
        public void NewRelicsHaveArtAndApplyTheirOpeningAndActionEffects()
        {
            int illustrated = 0;
            var relics = new RunRelicCollection();
            foreach (var relic in FragmentRelicCatalog.All)
            {
                if (string.IsNullOrEmpty(relic.ImageResource)) continue;
                illustrated++;
                if (relic.Effect == RelicEffectKind.StartThorns || relic.Effect == RelicEffectKind.StartFortify
                    || relic.Effect == RelicEffectKind.MoveMomentum || relic.Effect == RelicEffectKind.CondenseShield)
                    Assert.That(relics.TryAdd(relic), Is.True);
            }
            Assert.That(illustrated, Is.EqualTo(6));
            var battle = new BattleSession(Battle(), 50, 0, 0, 0, relics);
            Assert.That(battle.Tactics.Statuses.Get(CombatStatusKind.Thorns), Is.EqualTo(3));
            Assert.That(battle.Tactics.Statuses.Get(CombatStatusKind.Fortify), Is.EqualTo(4));
            Assert.That(battle.TryMovePlayer(-1.5, 0), Is.True);
            Assert.That(battle.Tactics.Statuses.Get(CombatStatusKind.Momentum), Is.EqualTo(2));
            Assert.That(battle.TryUndoMove(), Is.True);
            Assert.That(battle.Tactics.Statuses.Get(CombatStatusKind.Momentum), Is.Zero);
            Assert.That(battle.TryMovePlayer(-1.5, 0), Is.True);
            Assert.That(battle.TryCondense(), Is.True);
            Assert.That(battle.Tactics.Statuses.Get(CombatStatusKind.Shield), Is.EqualTo(4));
        }

        [Test]
        public void LegacyProgressionAwardsOncePersistsAndIsCapturedByRunSave()
        {
            string directory = Path.Combine(Path.GetTempPath(), "GraphaclysmLegacyTests", Guid.NewGuid().ToString("N"));
            try
            {
                var legacy = new LegacyProgression();
                Assert.That(legacy.AwardRun(91, false, 2), Is.EqualTo(3));
                Assert.That(legacy.AwardRun(91, true, 3), Is.Zero);
                Assert.That(legacy.TryPurchase(0), Is.True);
                var store = new LegacyProgressionStore(directory);
                Assert.That(store.TrySave(legacy), Is.True);
                var loaded = store.Load();
                Assert.That(loaded.GetRank(0), Is.EqualTo(1));
                Assert.That(loaded.Currency, Is.EqualTo(1));

                var benefits = new LegacyBenefits(6, 1, 2, 20, 4, 0);
                var run = PrototypeRunFactory.Create(481, PrototypeCharacterCatalog.All[0], benefits);
                Assert.That(run.PlayerMaxHealth, Is.EqualTo(PrototypeCharacterCatalog.All[0].MaxHealth + 6));
                Assert.That(run.TryPurchaseGrowthNode(0), Is.True);
                Assert.That(run.TryPurchaseGrowthNode(2), Is.True);
                Assert.That(RunSaveStore.TryDecode(RunSaveStore.Encode(run.CaptureSave()), out var data), Is.True);
                Assert.That(RunGameSession.TryRestore(data, out var restored, out _), Is.True);
                Assert.That(restored.PlayerMaxHealth, Is.EqualTo(run.PlayerMaxHealth));
                Assert.That(restored.Growth.Level, Is.EqualTo(run.Growth.Level));
                Assert.That(restored.Growth.ActiveVariant, Is.EqualTo(1));
                Assert.That(restored.TrySelectMapNode(0), Is.True);
                Assert.That(restored.CurrentBattle.Battle.Tactics.Resonance, Is.EqualTo(2));
                Assert.That(restored.CurrentBattle.Battle.Tactics.Statuses.Get(CombatStatusKind.Shield), Is.EqualTo(4));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
    }
}

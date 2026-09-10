using System;
using System.IO;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Relics;
using Graphaclysm.Core.Runs;
using Graphaclysm.Core.Equations;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class CombatFlowV13Tests
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
        public void RunGrowthOffersThreeExclusiveBranchChoicesAndResetsWithANewRun()
        {
            var growth = new RunGrowthState(CombatArchetype.Ian);
            growth.AddExperience(30);
            Assert.That(growth.Level, Is.GreaterThan(6));
            Assert.That(growth.TryPurchase(0), Is.True);
            Assert.That(growth.TryPurchase(1), Is.False);
            Assert.That(growth.TryPurchase(2), Is.True);
            Assert.That(growth.ActiveVariant, Is.EqualTo(1));
            Assert.That(growth.ModuleVariant, Is.EqualTo(1));
            Assert.That(growth.TryPurchase(3), Is.False);
            Assert.That(growth.TryPurchase(4), Is.True);
            Assert.That(growth.UltimateVariant, Is.EqualTo(1));
            Assert.That(growth.TryPurchase(5), Is.False);
            Assert.That(growth.TrySelect(0), Is.False);
            var fresh = new RunGrowthState(CombatArchetype.Ian);
            Assert.That(fresh.Level, Is.EqualTo(1));
            Assert.That(fresh.Points, Is.Zero);
            Assert.That(fresh.ActiveVariant, Is.Zero);
        }

        [Test]
        public void CombatSkillMovesAlongAPathHitsAndUsesAThreeTurnCooldown()
        {
            var ian = new BattleSession(Battle(), 50, 0, 0, 0, null,
                new BattleSkillLoadout(1, 1, 0));
            double startX = ian.Tactics.X;
            Assert.That(ian.TryUseCombatSkill(), Is.True);
            Assert.That(ian.Tactics.X, Is.GreaterThan(startX));
            Assert.That(ian.Enemies[0].Health, Is.EqualTo(94));
            Assert.That(ian.Tactics.Statuses.Get(CombatStatusKind.Shield), Is.EqualTo(5));
            Assert.That(ian.CombatSkillCooldown, Is.EqualTo(3));
            Assert.That(ian.TryUseCombatSkill(), Is.False);
            for (int i = 0; i < 3; i++) { Assert.That(ian.TryUnravel(), Is.True); ian.ResolveEnemyTurn(); }
            Assert.That(ian.CombatSkillCooldown, Is.Zero);
            Assert.That(ian.CanUseCombatSkill, Is.True);

            var luna = new BattleSession(Battle(CombatArchetype.Luna), 50, 0, 0, 0, null,
                new BattleSkillLoadout(2, 0, 0));
            Assert.That(luna.TryUseCombatSkill(), Is.True);
            Assert.That(luna.Enemies[0].Health, Is.EqualTo(92));
            Assert.That(luna.Tactics.HasMoved, Is.True);
            Assert.That(luna.Tactics.CanUndoMove, Is.False);
        }

        [Test]
        public void SkillModulesChangeThePathAttackInsteadOfOnlyRaisingNumbers()
        {
            var fracture = new BattleSession(Battle(), 50, 0, 0, 0, null,
                new BattleSkillLoadout(0, 2, 0));
            Assert.That(fracture.TryUseCombatSkill(), Is.True);
            Assert.That(fracture.Enemies[0].Statuses.Get(CombatStatusKind.Rupture), Is.EqualTo(3));

            var execution = new BattleSession(new BattleDefinition(50, 4,
                new[] { new EnemyDefinition("target", "Target", 5.6, -2, 8, 0) }, CombatArchetype.Ian, fragments: true),
                50, 0, 0, 0, null, new BattleSkillLoadout(2, 0, 0));
            Assert.That(execution.TryUseCombatSkill(), Is.True);
            Assert.That(execution.Enemies[0].IsAlive, Is.False);
            Assert.That(execution.LastSkillCooldownReset, Is.True);
            Assert.That(execution.CombatSkillCooldown, Is.Zero);
        }

        [Test]
        public void WideBranchHitsThreeLanesAndNewFragmentsChangeTheWholeSilhouette()
        {
            var wide = new BattleSession(new BattleDefinition(50, 4, new[]
            {
                new EnemyDefinition("center", "Center", 7, -2, 30, 0),
                new EnemyDefinition("upper", "Upper", 5.3, -.9, 30, 0),
                new EnemyDefinition("lower", "Lower", 5.3, -3.1, 30, 0)
            }, CombatArchetype.Ian, fragments: true), 50, 0, 0, 0, null, new BattleSkillLoadout(1, 0, 0));
            Assert.That(wide.TryUseCombatSkill(0), Is.True);
            Assert.That(wide.LastSkillHitCount, Is.EqualTo(3));

            FragmentKind[] kinds = { FragmentKind.CometBurst, FragmentKind.KaleidoscopeFold,
                FragmentKind.ShardFracture, FragmentKind.NebulaRibbon };
            int[] frequencies = { 8, 10, 7, 5 };
            for (int i = 0; i < kinds.Length; i++)
            {
                var equation = new FragmentEquation();
                Assert.That(equation.TryAppend(kinds[i], 2.5, -1.25), Is.True);
                Assert.That(equation.OriginX, Is.EqualTo(2.5));
                Assert.That(equation.OriginY, Is.EqualTo(-1.25));
                Assert.That(equation.Frequency, Is.EqualTo(frequencies[i]));
                Assert.That(equation.BuildFormula(), Does.Contain(FragmentEquation.Rule(kinds[i])));
            }
        }

        [Test]
        public void ResonanceRequiresMultiTargetOrSelfEnemyGeometry()
        {
            var battle = new BattleSession(new BattleDefinition(50, 4, new[]
            {
                new EnemyDefinition("east", "East", 5.6, -2, 50, 0),
                new EnemyDefinition("north", "North", 4, -.4, 50, 0)
            }, CombatArchetype.Ian, fragments: true));
            Assert.That(battle.TryPlayCard(FragmentCardCatalog.Find("frag.home"), out _), Is.True);
            Assert.That(battle.TryBeginPlot(), Is.True);
            Assert.That(battle.ResolvePlot().HitCount, Is.EqualTo(2));
            Assert.That(battle.Tactics.Resonance, Is.EqualTo(1));
        }

        [Test]
        public void RuptureFortifyAndThornsResolveInTheirDeclaredOrder()
        {
            var ruptured = new BattleSession(Battle(), 50, 0, 0, 6, null,
                new BattleSkillLoadout(0, 0, 1));
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

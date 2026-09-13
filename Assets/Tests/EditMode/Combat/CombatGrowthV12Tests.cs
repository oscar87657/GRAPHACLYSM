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
    public sealed class CombatReadabilityV14Tests
    {
        private static BattleDefinition Battle(CombatArchetype archetype = CombatArchetype.Ian)
            => new BattleDefinition(50, 4,
                new[] { new EnemyDefinition("target", "Target", 5.6, -2, 100, 2) }, archetype, fragments: true);

        private static RelicDefinition Relic(string id)
        {
            foreach (var relic in FragmentRelicCatalog.Version30) if (relic.Id == id) return relic;
            throw new InvalidOperationException("Missing relic " + id);
        }

        [Test]
        public void LargeGrowthCatalogSeparatesAcquisitionFromTwoSlotLoadoutAndResets()
        {
            var growth = new RunGrowthState(CombatArchetype.Ian);
            Assert.That(growth.NodeCount, Is.EqualTo(144));
            Assert.That(GrowthCatalog.All.Length, Is.EqualTo(240));
            Assert.That(growth.Points, Is.EqualTo(6));

            Assert.That(growth.TryPurchase(growth.IndexOf(GrowthTreePaths.RootId)), Is.True);
            growth.AddPoints(4);
            foreach (string id in new[] { "common.origin.01", "common.fragments.01", "common.geometry.02", "ian.inscription.core" })
                Assert.That(growth.TryPurchase(growth.IndexOf(id)), Is.True);
            int triple = growth.IndexOf("ian.triple.form");
            int tripleTrait = growth.IndexOf("ian.triple.m1");
            int execute = growth.IndexOf("ian.execute.form");
            int brand = growth.IndexOf("ian.brand.form");
            int spike = growth.IndexOf("ian.spike.form");
            Assert.That(growth.TryPurchase(tripleTrait), Is.False, "A trait needs its form first.");
            Assert.That(growth.TryPurchase(triple), Is.True);
            Assert.That(growth.ActiveVariant, Is.EqualTo(1));
            Assert.That(growth.TryPurchase(brand), Is.True);
            Assert.That(growth.EquippedCombatFormCount, Is.EqualTo(2));
            Assert.That(growth.TryPurchase(execute), Is.True, "Forms can be acquired independently.");
            Assert.That(growth.IsEquipped(execute), Is.False, "A second form from the same family cannot be equipped.");
            Assert.That(growth.TryPurchase(spike), Is.True);
            Assert.That(growth.IsEquipped(spike), Is.False, "A third combat form waits outside the two-slot loadout.");
            Assert.That(growth.TrySelect(triple), Is.True);
            Assert.That(growth.TrySelect(spike), Is.True);
            Assert.That(growth.IsEquipped(spike), Is.True);

            CompiledGrowthBuild build = growth.CreateCompiledBuild();
            Assert.That(build.HasAcquired("ian.execute.form"), Is.True);
            Assert.That(build.IsEquipped("ian.spike.form"), Is.True);
            Assert.That(build.IsEquipped("ian.triple.form"), Is.False);

            Assert.That(growth.TryReset(), Is.True);
            Assert.That(growth.AcquiredCount, Is.Zero);
            Assert.That(growth.Points, Is.EqualTo(10));
            Assert.That(build.IsEquipped("ian.spike.form"), Is.True, "An encounter snapshot is immutable after respec.");
            var fresh = new RunGrowthState(CombatArchetype.Ian);
            Assert.That(fresh.Points, Is.EqualTo(6));
            Assert.That(fresh.ActiveVariant, Is.Zero);
        }

        [Test]
        public void AwakeningNeedsTwoOfThreeTraitsAndIdentityBranchesAreExclusive()
        {
            var growth = new RunGrowthState(CombatArchetype.Ian);
            growth.AddPoints(6);
            Assert.That(growth.TryPurchase(growth.IndexOf(GrowthTreePaths.RootId)), Is.True);
            Assert.That(growth.TryPurchase(growth.IndexOf("common.geometry.02")), Is.True);
            Assert.That(growth.TryPurchase(growth.IndexOf("common.origin.01")), Is.True);
            int identity = growth.IndexOf("ian.inscription.core");
            int otherIdentity = growth.IndexOf("ian.archive.core");
            int form = growth.IndexOf("ian.triple.form");
            int first = growth.IndexOf("ian.triple.m1");
            int second = growth.IndexOf("ian.triple.m2");
            int awakening = growth.IndexOf("ian.triple.awaken");

            Assert.That(growth.TryPurchase(identity), Is.True);
            Assert.That(growth.TryPurchase(otherIdentity), Is.False);
            Assert.That(growth.TryPurchase(form), Is.True);
            Assert.That(growth.TryPurchase(first), Is.True);
            Assert.That(growth.TryPurchase(awakening), Is.False);
            Assert.That(growth.TryPurchase(second), Is.True);
            Assert.That(growth.CanPurchase(awakening), Is.False, "Awakening also requires the third tier.");
            for(int i=0;i<growth.NodeCount && growth.SpentPoints<10;i++)
                if(growth.GetNode(i).Owner==GrowthOwner.Common && growth.GetNode(i).Cost==1 && growth.CanPurchase(i)) growth.TryPurchase(i);
            Assert.That(growth.TryPurchase(awakening), Is.True);
            Assert.That(growth.Points, Is.Zero);
        }

        [Test]
        public void CombatSkillMovesAlongAPathHitsAndUsesAThreeTurnCooldown()
        {
            var ian = new BattleSession(Battle(), 50, 0, 0, 0, null,
                new BattleSkillLoadout(1, 0, 0, 1 << 4));
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
            Assert.That(luna.Enemies[0].Health, Is.EqualTo(91));
            Assert.That(luna.Tactics.HasMoved, Is.False, "Crescent Return ends at its origin instead of copying Ian's dash.");
            Assert.That(luna.Tactics.CanUndoMove, Is.False);
        }

        [Test]
        public void CombatSkillReportsShieldDamageWhenHealthIsFullyProtected()
        {
            var battle = new BattleSession(new BattleDefinition(50, 4,
                new[] { new EnemyDefinition("target", "Target", 5.6, -2, 100, 0,
                    EnemyBehaviorDefinition.ChargeBurst()) }, CombatArchetype.Ian, fragments: true), 50);

            Assert.That(battle.Enemies[0].Statuses.Get(CombatStatusKind.Shield), Is.EqualTo(12));
            Assert.That(battle.TryUseCombatSkill(0), Is.True);
            Assert.That(battle.Enemies[0].Health, Is.EqualTo(100));
            Assert.That(battle.Enemies[0].Statuses.Get(CombatStatusKind.Shield), Is.EqualTo(5));
            Assert.That(battle.LastSkillDamage, Is.Zero);
            Assert.That(battle.LastSkillShieldDamage, Is.EqualTo(7));
            Assert.That(battle.LastSkillHitCount, Is.EqualTo(1));
        }

        [Test]
        public void IanPiercesALineWhileLunaCreatesALandingBurst()
        {
            var definition = new BattleDefinition(50, 4, new[]
            {
                new EnemyDefinition("target", "Target", 5.6, -2, 100, 0),
                new EnemyDefinition("side", "Side", 5.6, -.9, 100, 0)
            }, CombatArchetype.Ian, fragments: true);
            var ian = new BattleSession(definition, 50);
            Assert.That(ian.TryUseCombatSkill(0), Is.True);
            Assert.That(ian.LastSkillHitCount, Is.EqualTo(1));
            Assert.That(ian.LastSkillStyle, Is.EqualTo(0));

            var lunaDefinition = new BattleDefinition(50, 4, new[]
            {
                new EnemyDefinition("target", "Target", 5.6, -2, 100, 0),
                new EnemyDefinition("side", "Side", 5.6, -.9, 100, 0)
            }, CombatArchetype.Luna, fragments: true);
            var luna = new BattleSession(lunaDefinition, 50);
            Assert.That(luna.TryUseCombatSkill(0), Is.True);
            Assert.That(luna.LastSkillHitCount, Is.EqualTo(2));
            Assert.That(luna.LastSkillStyle, Is.EqualTo(2));
        }

        [Test]
        public void ThirdSkillFormsUseMirrorBurstAndConstellationChain()
        {
            var enemies = new[]
            {
                new EnemyDefinition("target", "Target", 5.6, -2, 100, 0),
                new EnemyDefinition("side", "Side", 5.6, -.2, 100, 0)
            };
            var ian = new BattleSession(new BattleDefinition(50, 4, enemies,
                CombatArchetype.Ian, fragments: true), 50, 0, 0, 0, null,
                new BattleSkillLoadout(3, 0, 0, 1 << 7));
            Assert.That(ian.TryUseCombatSkill(0), Is.True);
            Assert.That(ian.LastSkillStyle, Is.EqualTo(2));
            Assert.That(ian.LastSkillHitCount, Is.EqualTo(2));
            Assert.That(ian.Tactics.X, Is.GreaterThan(ian.Enemies[0].X));

            var luna = new BattleSession(new BattleDefinition(50, 4, enemies,
                CombatArchetype.Luna, fragments: true), 50, 0, 0, 0, null,
                new BattleSkillLoadout(3, 0, 0, 1 << 7));
            Assert.That(luna.TryUseCombatSkill(0), Is.True);
            Assert.That(luna.LastSkillStyle, Is.EqualTo(4));
            Assert.That(luna.LastSkillHitCount, Is.EqualTo(2));
        }

        [Test]
        public void EclipseUltimateCanRecoverTheCombatSkillCooldown()
        {
            var luna = new BattleSession(Battle(CombatArchetype.Luna), 50, 0, 0, 6, null,
                new BattleSkillLoadout(0, 0, 3, 1 << 16));
            Assert.That(luna.TryUseCombatSkill(), Is.True);
            Assert.That(luna.CombatSkillCooldown, Is.EqualTo(3));
            Assert.That(luna.TryPlayCard(FragmentCardCatalog.Find("frag.home"), out _), Is.True);
            Assert.That(luna.TryToggleUltimate(), Is.True);
            Assert.That(luna.TryBeginPlot(), Is.True);
            luna.ResolvePlot();
            Assert.That(luna.CombatSkillCooldown, Is.Zero);
            Assert.That(luna.Tactics.Resonance, Is.Zero);
        }

        [Test]
        public void SkillModulesChangeThePathAttackInsteadOfOnlyRaisingNumbers()
        {
            var fracture = new BattleSession(Battle(), 50, 0, 0, 0, null,
                new BattleSkillLoadout(2, 0, 0, 1 << 6));
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
        public void DeepSkillNodesCreateFiveLanesAndRewardExecutionKills()
        {
            var five = new BattleSession(Battle(), 50, 0, 0, 0, null,
                new BattleSkillLoadout(1, 0, 0, 1 << 3));
            Assert.That(five.TryUseCombatSkill(), Is.True);
            Assert.That(five.LastSkillLaneCount, Is.EqualTo(5));
            Assert.That(five.LastSkillDamage, Is.EqualTo(5));

            var execution = new BattleSession(new BattleDefinition(50, 4,
                new[] { new EnemyDefinition("target", "Target", 5.6, -2, 8, 0) }, CombatArchetype.Ian, fragments: true),
                50, 0, 0, 0, null, new BattleSkillLoadout(2, 0, 0, 1 << 5));
            Assert.That(execution.TryUseCombatSkill(), Is.True);
            Assert.That(execution.LastSkillCooldownReset, Is.True);
            Assert.That(execution.Tactics.Resonance, Is.EqualTo(1));
        }

        [Test]
        public void DeepUltimateNodesChangeDamageAndRefundResonance()
        {
            var sharp = new BattleSession(Battle(), 50, 0, 0, 6, null,
                new BattleSkillLoadout(0, 0, 1, 1 << 13));
            Assert.That(sharp.TryToggleUltimate(), Is.True);
            Assert.That(sharp.Tactics.AttackBonus, Is.EqualTo(14));

            var refund = new BattleSession(new BattleDefinition(50, 4, new[]
            {
                new EnemyDefinition("east", "East", 5.6, -2, 50, 0),
                new EnemyDefinition("north", "North", 4, -.4, 50, 0)
            }, CombatArchetype.Ian, fragments: true), 50, 0, 0, 6, null,
                new BattleSkillLoadout(0, 0, 1, 1 << 12));
            Assert.That(refund.TryPlayCard(FragmentCardCatalog.Find("frag.home"), out _), Is.True);
            Assert.That(refund.TryToggleUltimate(), Is.True);
            Assert.That(refund.TryBeginPlot(), Is.True);
            Assert.That(refund.ResolvePlot().HitCount, Is.EqualTo(2));
            Assert.That(refund.Tactics.Resonance, Is.EqualTo(2));
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
            foreach (var relic in FragmentRelicCatalog.Version30)
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
                int identity = run.Growth.IndexOf("ian.inscription.core");
                int triple = run.Growth.IndexOf("ian.triple.form");
                Assert.That(run.TryPurchaseGrowthNode(run.Growth.IndexOf(GrowthTreePaths.RootId)), Is.True);
                Assert.That(run.TryPurchaseGrowthNode(run.Growth.IndexOf("common.geometry.02")), Is.True);
                Assert.That(run.TryPurchaseGrowthNode(run.Growth.IndexOf("common.origin.01")), Is.True);
                Assert.That(run.TryPurchaseGrowthNode(identity), Is.True);
                Assert.That(run.TryPurchaseGrowthNode(triple), Is.True);
                Assert.That(RunSaveStore.TryDecode(RunSaveStore.Encode(run.CaptureSave()), out var data), Is.True);
                Assert.That(RunGameSession.TryRestore(data, out var restored, out _), Is.True);
                Assert.That(restored.PlayerMaxHealth, Is.EqualTo(run.PlayerMaxHealth));
                Assert.That(restored.Growth.AcquiredCount, Is.EqualTo(run.Growth.AcquiredCount));
                Assert.That(restored.Growth.ActiveVariant, Is.EqualTo(1));
                Assert.That(restored.Growth.IsUnlocked("ian.inscription.core"), Is.True);
                Assert.That(restored.Growth.IsUnlocked("ian.triple.form"), Is.True);
                Assert.That(restored.TrySelectMapNode(0), Is.True);
                Assert.That(restored.CurrentBattle.Battle.Tactics.Resonance, Is.EqualTo(2));
                Assert.That(restored.CurrentBattle.Battle.Tactics.Statuses.Get(CombatStatusKind.Shield), Is.EqualTo(4 + restored.MasteryRank));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
    }
}

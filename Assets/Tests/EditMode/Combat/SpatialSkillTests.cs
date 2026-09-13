using System;
using System.Security.Cryptography;
using System.Reflection;
using Graphaclysm.Application;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class SpatialSkillTests
    {
        private static BattleSession Battle(CombatApproach approach, EnemyDefinition[] enemies, BattleTerrainDefinition[] terrain = null)
            => new BattleSession(new BattleDefinition(42, 3, enemies,
                approach == CombatApproach.Tuning ? CombatArchetype.Luna : CombatArchetype.Ian,
                fragments: true, terrain: terrain, approach: approach));

        [Test]
        public void DashPassesThroughEnemiesAndUsesOnlyActualPathContact()
        {
            var b = Battle(CombatApproach.Execution, new[] {
                new EnemyDefinition("a", "a", 4, -2, 30, 1),
                new EnemyDefinition("b", "b", 6, -2, 30, 1),
                new EnemyDefinition("c", "c", 9, -2, 30, 1),
                new EnemyDefinition("d", "d", 6, 0, 30, 1) });
            var p = b.PreviewExecutionDash(9, -2);
            Assert.That(p.CanUse, Is.True);
            Assert.That(p.EndX, Is.EqualTo(7.6).Within(1e-9));
            Assert.That(b.PreviewExecutionDash(4.2, -2).EndX, Is.EqualTo(p.EndX));
            for (int i = 0; i < 4; i++)
                Assert.That(b.PreviewCombatSkillContact(p, i).Hit, Is.EqualTo(i < 2));
            Assert.That(b.Tactics.X, Is.EqualTo(4)); Assert.That(b.Enemies[0].Health, Is.EqualTo(30));
            Assert.That(b.TryUseExecutionDash(9, -2), Is.True);
            Assert.That(b.Tactics.X, Is.EqualTo(p.EndX)); Assert.That(b.LastSkillHitCount, Is.EqualTo(2));
            Assert.That(b.Enemies[0].Health, Is.EqualTo(20)); Assert.That(b.Enemies[2].Health, Is.EqualTo(30));
            Assert.That(b.Enemies[1].Statuses.Get(CombatStatusKind.Rupture), Is.EqualTo(6));
            Assert.That(b.Tactics.CanUndoMove, Is.False);
            Assert.That(b.TryUseExecutionDash(4, -2), Is.False);
        }

        [Test]
        public void DashStopsAtPillarOnStraightRouteNotAtFreeLandingBehindIt()
        {
            var b = Battle(CombatApproach.Execution, new[] { new EnemyDefinition("a", "a", 8, -2, 30, 1) },
                new[] { new BattleTerrainDefinition("wall", BattleTerrainKind.Obstacle, 6, -2, .2) });
            var p = b.PreviewExecutionDash(9, -2);
            Assert.That(p.EndX, Is.InRange(5.319, 5.32)); Assert.That(p.EndY, Is.EqualTo(-2));
            Assert.That(b.TryUseExecutionDash(9, -2), Is.True);
            Assert.That(b.Tactics.X, Is.EqualTo(p.EndX)); Assert.That(b.LastSkillHitCount, Is.Zero);
        }

        [TestCase(4, 4, 4, 1.6)]
        [TestCase(0, -2, .48, -2)]
        [TestCase(4, -4, 4, -3.52)]
        public void DashHasFixedDistanceExceptAtFieldEdge(double x, double y, double ex, double ey)
        {
            var b = new BattleSession(SignatureBattleFactory.CreateEncounter(CombatApproach.Execution, 42));
            var p = b.PreviewExecutionDash(x, y);
            Assert.That(p.EndX, Is.EqualTo(ex).Within(1e-8)); Assert.That(p.EndY, Is.EqualTo(ey).Within(1e-8));
        }

        [TestCase(double.NaN, 0)]
        [TestCase(double.PositiveInfinity, 0)]
        [TestCase(4, -2)]
        [TestCase(11, 0)]
        public void BadDashAimDoesNotConsumeAnything(double x, double y)
        {
            var b = new BattleSession(SignatureBattleFactory.CreateEncounter(CombatApproach.Execution, 42));
            Assert.That(b.TryUseExecutionDash(x, y), Is.False);
            Assert.That(b.CombatSkillCooldown, Is.Zero); Assert.That(b.Tactics.HasMoved, Is.False);
        }

        [Test]
        public void DashKillResetsAndShieldPredictionMatchesCommit()
        {
            var b = Battle(CombatApproach.Execution, new[] {
                new EnemyDefinition("a", "a", 6, -2, 4, 1), new EnemyDefinition("b", "b", 4, 2, 30, 1) });
            AddStatus(b.Enemies[0], CombatStatusKind.Shield, 3);
            var c = b.PreviewCombatSkillContact(b.PreviewExecutionDash(9, -2), 0);
            Assert.That(b.TryUseExecutionDash(9, -2), Is.True);
            Assert.That(b.LastSkillDamage, Is.EqualTo(c.HealthDamage));
            Assert.That(b.LastSkillShieldDamage, Is.EqualTo(c.ShieldDamage));
            Assert.That(b.LastSkillCooldownReset, Is.True); Assert.That(b.CanUseCombatSkill, Is.True);
        }

        [Test]
        public void PullPreviewMatchesCommitAndSharesTurnBudgetWithoutMovingPlayerOrDrawing()
        {
            var run = SignatureBattleFactory.Create(CombatApproach.Tuning); var b = run.CurrentBattle.Battle;
            int hand = run.CurrentBattle.Deck.HandCount;
            Assert.That(b.PreviewLunaPull(5.5, -1, 0, out double x, out double y), Is.True);
            Assert.That(b.Enemies[0].X, Is.EqualTo(6.9));
            Assert.That(run.TryUseLunaPull(5.5, -1), Is.True);
            Assert.That(b.Enemies[0].X, Is.EqualTo(x)); Assert.That(b.Enemies[0].Y, Is.EqualTo(y));
            Assert.That(b.LastPullCount, Is.EqualTo(1)); Assert.That(b.CanUseLunaPull, Is.False);
            Assert.That(b.TryUseDiagramAbility(4, -2), Is.False);
            Assert.That(run.CurrentBattle.Deck.HandCount, Is.EqualTo(hand));
            Assert.That(b.Tactics.HasMoved, Is.False); Assert.That(b.Enemies[0].Health, Is.EqualTo(30));
            Assert.That(run.TryUnravel(), Is.True); run.ResolveEnemyTurn();
            Assert.That(b.CanUseLunaPull, Is.True);
        }

        [Test]
        public void TuningAlsoPreventsPullInSameTurn()
        {
            var run = SignatureBattleFactory.Create(CombatApproach.Tuning);
            Assert.That(run.TryPlayHandCard(0, out _, out _), Is.True);
            Assert.That(run.TryUseDiagramAbility(4, -2), Is.True);
            Assert.That(run.TryUseLunaPull(5.5, -1), Is.False);
        }

        [Test]
        public void PullAnchoredEmptyAndOutOfRangeAreFreeFailures()
        {
            var b = Battle(CombatApproach.Tuning, new[] { new EnemyDefinition("a", "a", 6, -2, 30, 1) });
            AddStatus(b.Enemies[0], CombatStatusKind.Anchor, 1);
            Assert.That(b.TryUseLunaPull(5, -2), Is.False); Assert.That(b.CanUseLunaPull, Is.True);
            Assert.That(b.TryUseLunaPull(9, -2), Is.False); Assert.That(b.TryUseLunaPull(double.NaN, -2), Is.False);
            typeof(CombatStatusState).GetMethod("Remove", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(b.Enemies[0].Statuses, new object[] { CombatStatusKind.Anchor });
            Assert.That(b.TryUseLunaPull(2, -2), Is.False); Assert.That(b.CanUseLunaPull, Is.True);
        }

        [Test]
        public void PullCannotCrossPillarAndDoesNotChangeFixedGunnerAim()
        {
            var b = Battle(CombatApproach.Tuning, new[] {
                new EnemyDefinition("gun", "gun", 6, -2, 30, 6, EnemyBehaviorDefinition.LineGunner()) },
                new[] { new BattleTerrainDefinition("wall", BattleTerrainKind.Obstacle, 4.8, -1.5, .2) });
            var e = b.Enemies[0]; double sx = e.LineStartX, sy = e.LineStartY, ex = e.LineEndX, ey = e.LineEndY;
            bool aimed = e.IsAimingAt(4, -2, .48);
            Assert.That(b.PreviewLunaPull(4.5, -1, 0, out double px, out double py), Is.True);
            Assert.That(b.TryUseLunaPull(4.5, -1), Is.True);
            Assert.That(e.X, Is.EqualTo(px)); Assert.That(e.Y, Is.EqualTo(py));
            Assert.That(Math.Sqrt(Math.Pow(e.X - 4.8, 2) + Math.Pow(e.Y + 1.5, 2)), Is.GreaterThanOrEqualTo(.68));
            Assert.That(e.LineStartX, Is.EqualTo(sx)); Assert.That(e.LineStartY, Is.EqualTo(sy));
            Assert.That(e.LineEndX, Is.EqualTo(ex)); Assert.That(e.LineEndY, Is.EqualTo(ey));
            Assert.That(e.IsAimingAt(4, -2, .48), Is.EqualTo(aimed));
        }

        [TestCase(CombatApproach.Execution)]
        [TestCase(CombatApproach.Tuning)]
        public void NewSkillsRoundTripThroughBinaryJournal(CombatApproach approach)
        {
            var run = Start(approach);
            Assert.That(approach == CombatApproach.Execution ? run.TryUseExecutionDash(9.1234, -1.6789)
                : run.TryUseLunaPull(5.51234, -1.23456), Is.True);
            Assert.That(RunSaveStore.TryDecode(RunSaveStore.Encode(run.CaptureSave()), out var data), Is.True);
            Assert.That(RunGameSession.TryRestore(data, out var copy, out _), Is.True);
            var a = run.CurrentBattle.Battle; var b = copy.CurrentBattle.Battle;
            Assert.That(b.Tactics.X, Is.EqualTo(a.Tactics.X)); Assert.That(b.Tactics.Y, Is.EqualTo(a.Tactics.Y));
            Assert.That(b.CanUseLunaPull, Is.EqualTo(a.CanUseLunaPull));
            Assert.That(b.CombatSkillCooldown, Is.EqualTo(a.CombatSkillCooldown));
            for (int i = 0; i < a.Enemies.Count; i++)
            {
                Assert.That(b.Enemies[i].X, Is.EqualTo(a.Enemies[i].X)); Assert.That(b.Enemies[i].Y, Is.EqualTo(a.Enemies[i].Y));
                Assert.That(b.Enemies[i].Health, Is.EqualTo(a.Enemies[i].Health));
            }
        }

        [Test]
        public void Rules22TargetedDashRestoresOldLandingAndRejectsNewCommandIds()
        {
            var run = Start(CombatApproach.Execution);
            Assert.That(run.TryUseCombatSkill(0), Is.True);
            var bytes = AsRules22(run);
            Assert.That(RunSaveStore.TryDecode(bytes, out var data), Is.True);
            Assert.That(RunGameSession.TryRestore(data, out var copy, out _), Is.True);
            Assert.That(copy.CurrentBattle.Battle.Tactics.X, Is.EqualTo(run.CurrentBattle.Battle.Tactics.X));
            Assert.That(copy.CurrentBattle.Battle.Tactics.X, Is.LessThan(6.9));
            var newer = Start(CombatApproach.Execution); newer.TryUseExecutionDash(9, -2);
            Assert.That(RunSaveStore.TryDecode(AsRules22(newer), out _), Is.False);
        }

        private static byte[] AsRules22(RunGameSession run)
        {
            var bytes = RunSaveStore.Encode(run.CaptureSave());
            Array.Copy(BitConverter.GetBytes(22), 0, bytes, 8, 4);
            using (var hash = SHA256.Create()) Array.Copy(hash.ComputeHash(bytes, 0, bytes.Length - 32), 0, bytes, bytes.Length - 32, 32);
            return bytes;
        }

        private static void AddStatus(EnemyState enemy, CombatStatusKind kind, int magnitude)
            => typeof(CombatStatusState).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(enemy.Statuses, new object[] { kind, magnitude, 2 });

        private static RunGameSession Start(CombatApproach approach)
        {
            var run = PrototypeRunFactory.Create(302, PrototypeCharacterCatalog.All[approach == CombatApproach.Tuning ? 1 : 0]);
            Assert.That(run.TryChooseApproach(approach), Is.True); Assert.That(run.TrySelectMapNode(0), Is.True);
            return run;
        }
    }
}

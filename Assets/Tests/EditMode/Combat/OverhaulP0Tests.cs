using Graphaclysm.Core.Combat;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class OverhaulP0Tests
    {
        private static BattleSession Create(CombatArchetype archetype = CombatArchetype.Ian, int variant = 0,
            BattleTerrainDefinition[] terrain = null)
        {
            return new BattleSession(new BattleDefinition(42, 4, new[] {
                new EnemyDefinition("a", "A", 8, -2, 40, 1),
                new EnemyDefinition("b", "B", 6, -1.8, 40, 1)
            }, archetype, fragments: true, terrain: terrain), 42, 0, 0,
                skillLoadout: new BattleSkillLoadout(variant, 0, 0));
        }

        [TestCase(CombatArchetype.Ian, 0)] [TestCase(CombatArchetype.Ian, 1)]
        [TestCase(CombatArchetype.Ian, 2)] [TestCase(CombatArchetype.Ian, 3)]
        [TestCase(CombatArchetype.Luna, 0)] [TestCase(CombatArchetype.Luna, 1)]
        [TestCase(CombatArchetype.Luna, 2)] [TestCase(CombatArchetype.Luna, 3)]
        public void PreviewIsPureAndMatchesEveryLegacyForm(CombatArchetype archetype, int variant)
        {
            var b = Create(archetype, variant);
            var add = typeof(CombatStatusState).GetMethod("Add", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            add.Invoke(b.Enemies[0].Statuses, new object[] { CombatStatusKind.Shield, 24, 2 });
            add.Invoke(b.Enemies[1].Statuses, new object[] { CombatStatusKind.Shield, 2, 2 });
            var p = b.PreviewCombatSkill(0);
            var a = b.PreviewCombatSkillContact(p, 0); var c = b.PreviewCombatSkillContact(p, 1);
            for (int i = 0; i < 100; i++) Assert.That(b.PreviewCombatSkill(0).EndX, Is.EqualTo(p.EndX));
            Assert.That(b.CombatSkillCooldown, Is.Zero);
            Assert.That(b.Tactics.X, Is.EqualTo(4)); Assert.That(b.Tactics.HasMoved, Is.False);
            Assert.That(b.Enemies[0].Health, Is.EqualTo(40));
            Assert.That(b.Enemies[0].Statuses.Get(CombatStatusKind.Shield), Is.EqualTo(24));
            Assert.That(b.TryUseCombatSkill(0), Is.True);
            Assert.That(b.LastSkillDamage, Is.EqualTo(a.HealthDamage + c.HealthDamage));
            Assert.That(b.LastSkillShieldDamage, Is.EqualTo(a.ShieldDamage + c.ShieldDamage));
            Assert.That(b.LastSkillHitCount, Is.EqualTo((a.Hit ? 1 : 0) + (c.Hit ? 1 : 0)));
            Assert.That(b.Tactics.X, Is.EqualTo(p.ReturnsToOrigin ? p.OriginX : p.EndX));
            Assert.That(b.Tactics.Y, Is.EqualTo(p.ReturnsToOrigin ? p.OriginY : p.EndY));
            Assert.That(a.Hit, Is.True); Assert.That(a.HealthDamage, Is.Zero);
        }

        [TestCase(-2)] [TestCase(99)] [TestCase(0)]
        public void InvalidOrDeadExplicitTargetNeverFallsBack(int target)
        {
            var b = Create(); b.Enemies[0].TakeDamage(100);
            Assert.That(b.PreviewCombatSkill(target).Failure, Is.EqualTo(CombatSkillFailure.InvalidTarget));
            Assert.That(b.TryUseCombatSkill(target), Is.False);
            Assert.That(b.Enemies[1].Health, Is.EqualTo(40));
            Assert.That(b.CombatSkillCooldown, Is.Zero); Assert.That(b.Tactics.HasMoved, Is.False);
            Assert.That(b.LastSkillDamage, Is.Zero);
        }

        [Test]
        public void LegacyQuickTargetSentinelRemainsExplicitlyAvailable()
        { var b = Create(); Assert.That(b.PreviewCombatSkill(-1).TargetIndex, Is.EqualTo(1)); Assert.That(b.TryUseCombatSkill(), Is.True); }

        [Test]
        public void BlockedLandingRefusesWithoutDamageOrCooldown()
        {
            var b = Create(terrain: new[] { new BattleTerrainDefinition("wall", BattleTerrainKind.Obstacle, 6.8, -2, 1.25) });
            Assert.That(b.PreviewCombatSkill(0).Failure, Is.EqualTo(CombatSkillFailure.NoLanding));
            Assert.That(b.TryUseCombatSkill(0), Is.False);
            Assert.That(b.CombatSkillCooldown, Is.Zero); Assert.That(b.Tactics.HasMoved, Is.False);
            Assert.That(b.Enemies[0].Health, Is.EqualTo(40));
        }

        [Test]
        public void TargetedHitIsNotMisreportedAsPathGeometry()
        {
            var b = Create(); b.TryMovePlayerTo(2.2, -2);
            var p = b.PreviewCombatSkill(0); var c = b.PreviewCombatSkillContact(p, 0);
            Assert.That(c.Hit, Is.True); Assert.That(c.GeometryHit, Is.False);
            Assert.That(b.TryUseCombatSkill(0), Is.True);
            Assert.That(b.LastSkillDamage, Is.GreaterThanOrEqualTo(c.HealthDamage));
        }
    }
}

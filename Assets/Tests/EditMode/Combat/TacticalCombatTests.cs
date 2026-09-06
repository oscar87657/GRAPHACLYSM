using System;
using Graphaclysm.Application;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Decks;
using Graphaclysm.Core.Equations;
using NUnit.Framework;

namespace Graphaclysm.Tests.Combat
{
    public sealed class TacticalCombatTests
    {
        private static CardDefinition Card(string id) => SpellCardCatalog.Find(id);
        private static BattleDefinition Definition(CombatArchetype archetype = CombatArchetype.Ian,
            int health = 200, int attack = 4, EnemyBehaviorDefinition behavior = null, double x = 5, double y = -1)
            => new BattleDefinition(42, 12, new[] { new EnemyDefinition("target", "Target", x, y, health, attack,
                behavior ?? EnemyBehaviorDefinition.SteadyAttack()) }, archetype);
        private static BattleSession Battle(CombatArchetype archetype = CombatArchetype.Ian, int resonance = 0,
            int startingHealth = 42) => new BattleSession(Definition(archetype), startingHealth, 0, 0, resonance);
        private static void Play(BattleSession battle, params string[] ids)
        {
            foreach (string id in ids) Assert.That(battle.TryPlayCard(Card(id), out _), Is.True, id);
        }
        private static PlotReport Plot(BattleSession battle)
        {
            Assert.That(battle.TryBeginPlot(), Is.True);
            return battle.ResolvePlot();
        }

        [Test]
        public void OperationOrder_ChangesFrequencyVersusAmplitude_AndAllowsRepeatedCoefficients()
        {
            var first = new EquationState(); first.SetBase(BaseFunctionKind.InputX);
            first.TryAddModifier(EquationModifierKind.Double); first.TryAddModifier(EquationModifierKind.Sine);
            var second = new EquationState(); second.SetBase(BaseFunctionKind.InputX);
            second.TryAddModifier(EquationModifierKind.Sine); second.TryAddModifier(EquationModifierKind.Double);
            Assert.That(first.Evaluate(5.5), Is.EqualTo(Math.Sin(1)).Within(1e-10));
            Assert.That(second.Evaluate(5.5), Is.EqualTo(2 * Math.Sin(0.5)).Within(1e-10));
            Assert.That(second.TryAddModifier(EquationModifierKind.Double), Is.True);
            Assert.That(second.Evaluate(5.5), Is.EqualTo(4 * Math.Sin(0.5)).Within(1e-10));
        }

        [Test]
        public void PolarComposition_TransformsRadiusOnce_AndPreservesNegativeRadius()
        {
            var equation = new EquationState(); equation.SetBase(BaseFunctionKind.InputAngle);
            equation.TryAddModifier(EquationModifierKind.Sine);
            equation.TryAddModifier(EquationModifierKind.Double);
            equation.Sample(0.75, out double x, out double y);
            Assert.That(x, Is.EqualTo(5).Within(1e-9));
            Assert.That(y, Is.EqualTo(2).Within(1e-9));
        }

        [Test]
        public void DualHit_AppliesEnemyDebuffAndSelfShieldOnce_WithoutSelfDamage()
        {
            var battle = Battle();
            Play(battle, "input.x", "coef.down", "sigil.ward");
            Assert.That(battle.PreviewPlayerHit, Is.True);
            int preview = battle.PreviewDamage(battle.Enemies[0]);
            var report = Plot(battle);
            Assert.That(report.TotalDamage, Is.EqualTo(preview));
            Assert.That(report.Shield, Is.EqualTo(9));
            Assert.That(battle.PlayerHealth, Is.EqualTo(42));
            Assert.That(battle.Enemies[0].Statuses.Get(CombatStatusKind.Weaken), Is.EqualTo(2));
            Assert.That(battle.Tactics.Resonance, Is.EqualTo(2));
            Assert.That(battle.ResolveEnemyTurn(), Is.Zero);
            Assert.That(battle.Tactics.Statuses.Get(CombatStatusKind.Shield), Is.Zero);
        }

        [Test]
        public void SelfOnlyHit_GrantsBuff_AndMissDoesNotGrantBuff()
        {
            var battle = new BattleSession(Definition(x: 9, y: -3));
            Play(battle, "input.x", "coef.down", "sigil.ember");
            var report = Plot(battle);
            Assert.That(report.HitCount, Is.Zero);
            Assert.That(report.PlayerHit, Is.True);
            Assert.That(battle.Tactics.Statuses.Get(CombatStatusKind.Focus), Is.EqualTo(2));
            Assert.That(battle.Tactics.Resonance, Is.EqualTo(1));
            battle.ResolveEnemyTurn();
            Play(battle, "input.x", "op.square", "sigil.ward");
            report = Plot(battle);
            Assert.That(report.PlayerHit, Is.False);
            Assert.That(report.Shield, Is.Zero);
        }

        [Test]
        public void Move_ConsumesEnergyOnce_DodgesFixedAim_AndUndoDoesNotRefundMove()
        {
            var battle = Battle();
            Assert.That(battle.TryMovePlayer(0, 1.5), Is.True);
            Assert.That(battle.TryMovePlayer(0, 1.5), Is.False);
            Assert.That(battle.Enemies[0].AimY, Is.EqualTo(-2));
            Play(battle, "input.x", "coef.up");
            Assert.That(battle.TryUndoLastCard(out _), Is.True);
            Assert.That(battle.Energy, Is.EqualTo(11));
            Plot(battle);
            Assert.That(battle.ResolveEnemyTurn(), Is.Zero);
            Assert.That(battle.Enemies[0].AimY, Is.EqualTo(-0.5));
        }

        [Test]
        public void Movement_RejectsOverlapNonCardinalAndOutOfBounds_WithoutSpendingEnergy()
        {
            var battle = new BattleSession(Definition(x: 5.5, y: -2));
            Assert.That(battle.TryMovePlayer(1.5, 0), Is.False);
            Assert.That(battle.TryMovePlayer(1.5, 1.5), Is.False);
            Assert.That(battle.TryMovePlayer(double.NaN, 0), Is.False);
            Assert.That(battle.Energy, Is.EqualTo(12));
            Assert.That(battle.TryMovePlayer(0, -1.5), Is.True);
            Play(battle, "input.x"); Plot(battle); battle.ResolveEnemyTurn();
            Assert.That(battle.TryMovePlayer(0, -1.5), Is.False);
        }

        [Test]
        public void ChargedAttack_StillReachesAfterOneMove_AndAppliesBurn()
        {
            var battle = new BattleSession(Definition(behavior: EnemyBehaviorDefinition.ChargeBurst()));
            Play(battle, "input.x"); Plot(battle); battle.ResolveEnemyTurn();
            Assert.That(battle.TryMovePlayer(0, 1.5), Is.True);
            Play(battle, "input.x", "op.square"); Plot(battle);
            Assert.That(battle.ResolveEnemyTurn(), Is.EqualTo(8));
            Assert.That(battle.Tactics.Statuses.Get(CombatStatusKind.Burn), Is.EqualTo(2));
        }

        [Test]
        public void InscriptionUndo_PreservesEquation_AndLimitRejectsWithoutMutation()
        {
            var battle = Battle();
            Play(battle, "input.x", "coef.down", "sigil.ward", "sigil.ember");
            string formula = battle.Equation.BuildFormula(); int energy = battle.Energy;
            Assert.That(battle.TryPlayCard(Card("sigil.mend"), out var failure), Is.False);
            Assert.That(failure, Is.EqualTo(CardPlayFailure.InscriptionLimit));
            Assert.That(battle.Energy, Is.EqualTo(energy));
            Assert.That(battle.TryUndoLastCard(out var card), Is.True);
            Assert.That(card.Id, Is.EqualTo("sigil.ember"));
            Assert.That(battle.Equation.BuildFormula(), Is.EqualTo(formula));
            Assert.That(battle.TryPlayCard(Card("sigil.ward"), out failure), Is.False);
        }

        [Test]
        public void FocusAndExposure_AffectNextPlot_NotThePlotThatAppliesThem()
        {
            var battle = Battle();
            Play(battle, "input.x", "coef.down", "sigil.ember", "sigil.exposure");
            int initial = battle.PreviewDamage(battle.Enemies[0]);
            Assert.That(Plot(battle).TotalDamage, Is.EqualTo(initial));
            battle.ResolveEnemyTurn();
            Play(battle, "input.x", "coef.down");
            // The initial shield absorbs 3 of attack 4; one Weaken remains for this plot.
            Assert.That(battle.PreviewDamage(battle.Enemies[0]), Is.EqualTo(initial + 2 + 3 - 1));
            Plot(battle);
            Assert.That(battle.Tactics.Statuses.Get(CombatStatusKind.Focus), Is.Zero);
        }

        [Test]
        public void Anchor_SuppressesReposition_AndHasteMakesNextMoveFree()
        {
            var battle = new BattleSession(Definition(behavior: EnemyBehaviorDefinition.AlternatingPosition(8, 2)));
            Play(battle, "input.x", "coef.down", "utility.phase"); Plot(battle); battle.ResolveEnemyTurn();
            Assert.That(battle.Enemies[0].X, Is.EqualTo(5));
            int energy = battle.Energy;
            Assert.That(battle.TryMovePlayer(0, 1.5), Is.True);
            Assert.That(battle.Energy, Is.EqualTo(energy));
            Assert.That(battle.Tactics.Statuses.Get(CombatStatusKind.Haste), Is.Zero);
        }

        [Test]
        public void Cleanse_RemovesPlayerWeaken_AndMendClampsHealth()
        {
            var battle = Battle(startingHealth: 40);
            Play(battle, "input.x"); Plot(battle); battle.ResolveEnemyTurn();
            Assert.That(battle.Tactics.Statuses.Get(CombatStatusKind.Weaken), Is.GreaterThan(0));
            Play(battle, "input.x", "coef.down", "utility.cleanse", "sigil.mend");
            var report = Plot(battle);
            Assert.That(report.Healing, Is.EqualTo(3));
            Assert.That(battle.Tactics.Statuses.Get(CombatStatusKind.Weaken), Is.Zero);
            var full = Battle(startingHealth: 41);
            Play(full, "input.x", "coef.down", "sigil.mend");
            Assert.That(Plot(full).Healing, Is.EqualTo(1));
        }

        [Test]
        public void IanUltimate_CanCancelWithoutSpending_ConsumesOnlyOnPlot()
        {
            var battle = Battle(resonance: 6);
            Play(battle, "input.x", "coef.down");
            int damage = battle.PreviewDamage(battle.Enemies[0]);
            Assert.That(battle.TryToggleUltimate(), Is.True);
            Assert.That(battle.PreviewDamage(battle.Enemies[0]), Is.EqualTo(damage + 6));
            Assert.That(battle.TryToggleUltimate(), Is.True);
            Assert.That(battle.Tactics.Resonance, Is.EqualTo(6));
            Assert.That(battle.TryToggleUltimate(), Is.True);
            Plot(battle);
            Assert.That(battle.Tactics.Resonance, Is.EqualTo(2));
            Assert.That(battle.Tactics.UltimateArmed, Is.False);
            Assert.That(battle.Enemies[0].Statuses.Get(CombatStatusKind.Anchor), Is.EqualTo(1));
            Assert.That(battle.TryToggleUltimate(), Is.False);
        }

        [Test]
        public void LunaUltimate_ExpandsSelfHit_AndHealsWithShield()
        {
            var battle = Battle(CombatArchetype.Luna, 6, 30);
            Play(battle, "input.x");
            Assert.That(battle.PreviewPlayerHit, Is.False);
            Assert.That(battle.TryToggleUltimate(), Is.True);
            Assert.That(battle.PreviewPlayerHit, Is.True);
            var report = Plot(battle);
            Assert.That(report.Healing, Is.EqualTo(5));
            Assert.That(report.Shield, Is.EqualTo(11));
            Assert.That(battle.PlayerHealth, Is.EqualTo(35));
        }

        [Test]
        public void BurnVictory_AdvancesRunRewards_AndResonanceCarriesToNextEncounter()
        {
            var definition = Definition(health: 12, attack: 0);
            var run = new RunGameSession(new[] { definition, definition },
                new[] { Card("input.x"), Card("coef.down"), Card("sigil.ember") }, SpellCardCatalog.All, 3, 99);
            Assert.That(run.TrySelectMapNode(0), Is.True);
            PlayFromHand(run, "input.x"); PlayFromHand(run, "coef.down"); PlayFromHand(run, "sigil.ember");
            run.TryBeginPlot(); run.ResolvePlot();
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Battle));
            Assert.That(run.CurrentBattle.Battle.Enemies[0].Health, Is.EqualTo(2));
            run.ResolveEnemyTurn();
            Assert.That(run.Phase, Is.EqualTo(RunPhase.CardReward));
            run.TrySkipReward(); run.TrySelectMapNode(1);
            Assert.That(run.CurrentBattle.Battle.Tactics.Resonance, Is.EqualTo(2));
        }

        [Test]
        public void NewRuns_UseFragmentsAndSelectedArchetype()
        {
            foreach (var character in PrototypeCharacterCatalog.All)
            {
                var run = PrototypeRunFactory.Create(7, character);
                run.TrySelectMapNode(0);
                Assert.That(run.CurrentBattle.Battle.Tactics.Archetype, Is.EqualTo(character.Archetype));
                for (int i = 0; i < character.StartingDeckCount; i++)
                    Assert.That(character.GetStartingCard(i).Type, Is.Not.EqualTo(CardType.BaseFunction));
            }
        }

        [Test]
        public void Reveal_ClipsFromCenter_AndOverflowedGeometryCannotHit()
        {
            double x0 = 0, y0 = 0, x1 = 10, y1 = 0;
            Assert.That(GraphSegmentClipper.ClipReveal(0, ref x0, ref y0, ref x1, ref y1), Is.False);
            Assert.That(GraphSegmentClipper.ClipReveal(0.25, ref x0, ref y0, ref x1, ref y1), Is.True);
            Assert.That(x0, Is.EqualTo(5 - Math.Sqrt(41) / 4).Within(1e-9));
            Assert.That(x1, Is.EqualTo(5 + Math.Sqrt(41) / 4).Within(1e-9));
            x0 = double.PositiveInfinity;
            Assert.That(GraphSegmentClipper.ClipToField(ref x0, ref y0, ref x1, ref y1), Is.False);
            var equation = new EquationState(); equation.SetBase(BaseFunctionKind.InputX);
            for (int i = 0; i < 8; i++) equation.TryAddModifier(EquationModifierKind.Cube);
            Assert.That(EquationAnalyzer.IntersectsCircle(equation, 9, 0, 0.48, 160), Is.False);
        }

        private static void PlayFromHand(RunGameSession run, string id)
        {
            for (int i = 0; i < run.CurrentBattle.Deck.HandCount; i++)
                if (run.CurrentBattle.Deck.GetHandCard(i).Id == id)
                {
                    Assert.That(run.TryPlayHandCard(i, out _, out _), Is.True); return;
                }
            Assert.Fail("Missing fixture card " + id);
        }

        [Test]
        public void FullAssemblyUndo_RestoresEveryCardAndEnergy_IncludingTwoInscriptions()
        {
            var cards = new CardDefinition[BattleSession.MaximumPlayedCards];
            cards[0] = Card("input.x");
            for (int i = 1; i <= 8; i++) cards[i] = Card("coef.half");
            cards[9] = Card("sigil.ward"); cards[10] = Card("sigil.ember");
            var game = new BattleGameSession(Battle(), new DeckSession(cards, cards.Length, new XorShiftRandom(12)));
            for (int step = 0; step < cards.Length; step++)
            {
                int found = -1;
                for (int i = 0; i < game.Deck.HandCount; i++)
                    if (game.Deck.GetHandCard(i).Id == cards[step].Id) { found = i; break; }
                Assert.That(game.TryPlayHandCard(found, out _, out _), Is.True);
            }
            Assert.That(game.PlayedCardCount, Is.EqualTo(11));
            for (int i = 0; i < cards.Length; i++) Assert.That(game.TryUndoLastPlayedCard(out _), Is.True);
            Assert.That(game.Deck.HandCount, Is.EqualTo(11));
            Assert.That(game.Deck.DiscardCount, Is.Zero);
            Assert.That(game.Battle.Energy, Is.EqualTo(12));
            Assert.That(game.Battle.Equation.HasBase, Is.False);
        }

        [Test]
        public void StatusStacks_CapAt24_AndExpireAfterTheirLastEnemyPhase()
        {
            var battle = new BattleSession(Definition(health: 1000, attack: 0));
            for (int i = 0; i < 15; i++)
            {
                Play(battle, "input.x", "coef.down", "sigil.ward"); Plot(battle); battle.ResolveEnemyTurn();
            }
            Assert.That(battle.Enemies[0].Statuses.Get(CombatStatusKind.Weaken), Is.EqualTo(24));
            Play(battle, "input.x"); Plot(battle); battle.ResolveEnemyTurn();
            Assert.That(battle.Enemies[0].Statuses.Get(CombatStatusKind.Weaken), Is.Zero);
        }

        [Test]
        public void Cleanse_RemovesChargeShieldRemainingAfterTheHit()
        {
            var battle = new BattleSession(Definition(behavior: EnemyBehaviorDefinition.ChargeBurst()));
            Assert.That(battle.Enemies[0].Statuses.Get(CombatStatusKind.Shield), Is.EqualTo(12));
            Play(battle, "input.x", "coef.down", "utility.cleanse"); Plot(battle);
            Assert.That(battle.Enemies[0].Statuses.Get(CombatStatusKind.Shield), Is.Zero);
            Assert.That(battle.Enemies[0].Health, Is.EqualTo(200));
        }
    }
}

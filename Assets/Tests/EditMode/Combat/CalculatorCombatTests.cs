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
    public sealed class CalculatorCombatTests
    {
        private static BattleSession Battle(bool calculator = true) => new BattleSession(new BattleDefinition(42, 6,
            new[] { new EnemyDefinition("target", "Target", 8, 0, 200, 0) }, CombatArchetype.Ian, calculator));

        [Test]
        public void InfixCompilerRespectsPrecedenceParenthesesAndTrigonometry()
        {
            var p = new ExpressionProgram(); Assert.That(p.TryCompile("2+3*cos(t)-1/2", out _), Is.True);
            Assert.That(p.Evaluate(0), Is.EqualTo(4.5).Within(1e-10));
            Assert.That(p.Evaluate(Math.PI), Is.EqualTo(-1.5).Within(1e-10));
            Assert.That(p.TryCompile("sqr(-2)+abs(-3)×2", out _), Is.True); Assert.That(p.Evaluate(0), Is.EqualTo(10));
        }
        [Test]
        public void TwoCoordinateExpressionsProduceActualSpirographGeometry()
        {
            var b = Battle(); Assert.That(b.TrySetCalculator("2*cos(3*t)+cos(7*t)", "2*sin(3*t)-sin(7*t)", out _), Is.True);
            b.Equation.Sample(0, out double x, out double y); Assert.That(x, Is.EqualTo(8).Within(1e-10)); Assert.That(y, Is.Zero.Within(1e-10));
            b.Equation.Sample(0.25, out x, out y); Assert.That(x, Is.EqualTo(5).Within(1e-10)); Assert.That(y, Is.EqualTo(-1).Within(1e-10));
            Assert.That(b.Equation.CurveSegmentCount, Is.EqualTo(768));
            Assert.That(b.Equation.TraceLength, Is.InRange(30.0, 64.0)); Assert.That(b.PreviewDamage(b.Enemies[0]), Is.GreaterThan(0));
            Assert.That(b.TryBeginPlot(), Is.True); Assert.That(b.ResolvePlot().HitCount, Is.EqualTo(1));
        }
        [Test]
        public void InvalidDraftCannotPartiallyReplaceEitherCoordinate()
        {
            var b = Battle(); b.TrySetCalculator("cos(t)", "sin(t)", out _);
            Assert.That(b.TrySetCalculator("2*cos(t)", "sin(", out _), Is.False);
            Assert.That(b.Equation.Calculator.XText, Is.EqualTo("cos(t)"));
            Assert.That(b.Equation.Calculator.YText, Is.EqualTo("sin(t)"));
            Assert.That(b.TrySetCalculator(new string('(', 17) + "t" + new string(')', 17), "0", out _), Is.False);
            Assert.That(b.TrySetCalculator("System.IO.File()", "0", out _), Is.False);
        }
        [Test]
        public void EveryOpeningHandCanPlayWithoutAStarterAndCardsRespectSelectedAxis()
        {
            foreach (var c in PrototypeCharacterCatalog.All)
            {
                var run = PrototypeRunFactory.Create(12, c); run.TrySelectMapNode(0);
                var b = run.CurrentBattle.Battle;
                Assert.That(b.Equation.IsFragmentMode, Is.True);
                for (int i = 0; i < run.CurrentBattle.Deck.HandCount; i++)
                { var card = run.CurrentBattle.Deck.GetHandCard(i); Assert.That(card.IsBaseFunction, Is.False); Assert.That(b.CanPlayCard(card, out _), Is.True); }
            }
            var battle = Battle(); battle.TrySetCalculator("t", "2", out _); battle.TrySelectFormulaAxis(0);
            Assert.That(battle.TryPlayCard(CalculatorCardCatalog.Find("calc.rupture"), out _), Is.True);
            battle.Equation.Sample(0.25, out double x, out double y);
            Assert.That(x, Is.EqualTo(5 + Math.PI).Within(1e-10)); Assert.That(y, Is.EqualTo(2));
            battle.TrySetCalculator("t+1", "2", out _); battle.TryUndoLastCard(out _);
            battle.Equation.Sample(0.25, out x, out y); Assert.That(x, Is.EqualTo(6 + Math.PI/2).Within(1e-10));
            Assert.That(battle.Energy, Is.EqualTo(6));
        }
        [Test]
        public void InkLimitRejectsDenseCoverageAndUndefinedOrEmptyTraces()
        {
            var b = Battle(); b.TrySetCalculator("3*cos(12*12*t)", "3*sin(12*12*t)", out _);
            Assert.That(b.Equation.TraceLength, Is.GreaterThan(64)); Assert.That(b.TryBeginPlot(), Is.False);
            b.TrySetCalculator("1/0", "0", out _); Assert.That(b.TryBeginPlot(), Is.False);
            b.TrySetCalculator("0", "0", out _); Assert.That(b.TryBeginPlot(), Is.False);
            b.TrySetCalculator("2*cos(t)", "2*sin(t)", out _); Assert.That(b.TryBeginPlot(), Is.True);
        }
        [Test]
        public void EquationNotePersistsNextTurnButCardLayersExpire()
        {
            var b = Battle(); b.TrySetCalculator("cos(t)", "sin(t)", out _);
            b.TryPlayCard(CalculatorCardCatalog.Find("calc.rupture"), out _); b.TryBeginPlot(); b.ResolvePlot(); b.ResolveEnemyTurn();
            Assert.That(b.Equation.Calculator.XText, Is.EqualTo("cos(t)")); Assert.That(b.Equation.ModifierCount, Is.Zero);
            Assert.That(b.Equation.HasBase, Is.True); Assert.That(b.PlayedCardCount, Is.Zero);
        }
        [Test]
        public void MoveUndoRestoresPositionEnergyAndAllowsAnotherDirection()
        {
            var b = Battle(); Assert.That(b.TryMovePlayer(-1.5, 0), Is.True);
            b.TryPlayCard(CalculatorCardCatalog.Find("calc.frost"), out _);
            Assert.That(b.TryUndoMove(), Is.True); Assert.That(b.Tactics.X, Is.EqualTo(4)); Assert.That(b.Tactics.Y, Is.EqualTo(-2));
            Assert.That(b.Energy, Is.EqualTo(5)); Assert.That(b.PlayedCardCount, Is.EqualTo(1)); Assert.That(b.TryUndoMove(), Is.False);
            Assert.That(b.TryMovePlayer(0, 1.5), Is.True); b.TryBeginPlot(); Assert.That(b.TryUndoMove(), Is.False);
        }
        [Test]
        public void FreeMoveUndoRestoresHasteWithoutGeneratingEnergy()
        {
            var b = Battle(false);
            b.TryPlayCard(SpellCardCatalog.Find("input.x"), out _); b.TryPlayCard(SpellCardCatalog.Find("coef.down"), out _);
            b.TryPlayCard(SpellCardCatalog.Find("utility.phase"), out _);
            b.TryBeginPlot(); b.ResolvePlot(); b.ResolveEnemyTurn();
            int haste = b.Tactics.Statuses.Get(CombatStatusKind.Haste); Assert.That(haste, Is.GreaterThan(0));
            int duration = b.Tactics.Statuses.Duration(CombatStatusKind.Haste);
            Assert.That(b.TryMovePlayer(-1.5,0), Is.True); Assert.That(b.Energy, Is.EqualTo(6));
            Assert.That(b.TryUndoMove(), Is.True); Assert.That(b.Energy, Is.EqualTo(6));
            Assert.That(b.Tactics.Statuses.Get(CombatStatusKind.Haste), Is.EqualTo(haste));
            Assert.That(b.Tactics.Statuses.Duration(CombatStatusKind.Haste), Is.EqualTo(duration));
        }
    }
}

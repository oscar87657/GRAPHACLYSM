using System;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Decks;
using Graphaclysm.Core.Equations;

namespace Graphaclysm.Application
{
    /// <summary>
    /// Coordinates state changes that span multiple Core aggregates.
    /// Presentation code issues one command and cannot leave battle/deck state half-updated.
    /// </summary>
    public sealed class BattleGameSession
    {
        private readonly int[] playedHandIndices =
            new int[BattleSession.MaximumPlayedCards];

        private int playedCommandCount;
        private int nextDrawBonus;
        private readonly int turnDraw;
        private bool condensing;
        public int CondenseDrawCount => 1 + Math.Min(1, Battle.PendingDrawBonus);
        public int NextDrawCount => turnDraw + Battle.PendingDrawBonus;

        public BattleGameSession(BattleSession battle, DeckSession deck, int turnDraw = 2)
        {
            if (turnDraw < 0 || turnDraw > 8) throw new ArgumentOutOfRangeException(nameof(turnDraw)); this.turnDraw = turnDraw;
            Battle = battle ?? throw new ArgumentNullException(nameof(battle));
            Deck = deck ?? throw new ArgumentNullException(nameof(deck));
        }

        public BattleSession Battle { get; }
        public DeckSession Deck { get; }
        public int PlayedCardCount
        {
            get { return Battle.PlayedCardCount; }
        }

        public CardDefinition GetPlayedCard(int index)
        {
            return Battle.GetPlayedCard(index);
        }

        public bool TryPlayHandCard(
            int handIndex,
            out CardDefinition playedCard,
            out CardPlayFailure failure)
        {
            CardDefinition card = Deck.GetHandCard(handIndex);
            if (!Battle.TryPlayCard(card, out failure))
            {
                playedCard = null;
                return false;
            }

            playedHandIndices[playedCommandCount] = handIndex;
            playedCommandCount++;
            if (Battle.UsesFragments) Deck.ReserveFromHand(handIndex); else Deck.DiscardFromHand(handIndex);
            playedCard = card;
            return true;
        }

        public bool TryUndoLastPlayedCard(out CardDefinition restoredCard)
        {
            if (playedCommandCount == 0 || Battle.PlayedCardCount != playedCommandCount || (Battle.UsesFragments && playedCommandCount <= Battle.SealedCardCount))
            {
                restoredCard = null;
                return false;
            }

            CardDefinition expectedCard = Battle.GetPlayedCard(playedCommandCount - 1);
            int handIndex = playedHandIndices[playedCommandCount - 1];
            if (!Deck.CanRestoreLastDiscard(handIndex, expectedCard))
            {
                restoredCard = null;
                return false;
            }

            if (!Battle.TryUndoLastCard(out restoredCard)
                || !Deck.TryRestoreLastDiscardToHand(handIndex, restoredCard))
            {
                throw new InvalidOperationException("Card undo transaction failed after validation.");
            }

            playedCommandCount--;
            playedHandIndices[playedCommandCount] = 0;
            return true;
        }

        public bool TryBeginPlot()
        {
            if (!Battle.CanPlot) return false;
            condensing = false; nextDrawBonus = Battle.PendingDrawBonus;
            if (!Battle.TryBeginPlot()) return false;
            if (Battle.UsesFragments) Deck.ReleaseReserved();
            return true;
        }

        public bool TryCondense()
        { if (!Battle.CanCondense) return false; nextDrawBonus = Math.Min(1, Battle.PendingDrawBonus); condensing = true; return Battle.TryCondense(); }
        public bool TryUnravel()
        { if (!Battle.TryUnravel()) return false; condensing = false; nextDrawBonus = 0; Deck.ReleaseReserved(); ClearCommandHistory(); return true; }
        public PlotReport ResolvePlot()
        {
            return Battle.ResolvePlot();
        }

        public int ResolveEnemyTurn()
        {
            int damage = Battle.ResolveEnemyTurn();
            if (Battle.Phase == BattlePhase.PlayerPlanning)
            {
                if (Battle.UsesFragments)
                { if (Battle.PlayedCardCount == 0) ClearCommandHistory(); Deck.DrawRetained((condensing ? 1 : turnDraw) + nextDrawBonus); nextDrawBonus = 0; condensing = false; }
                else { ClearCommandHistory(); Deck.EndTurnAndDraw(); }
            }

            return damage;
        }

        public void ResetBattle()
        {
            nextDrawBonus = 0; condensing = false;
            Battle.Reset();
            Deck.ResetForBattle();
            ClearCommandHistory();
        }

        private void ClearCommandHistory()
        {
            for (int i = 0; i < playedCommandCount; i++)
            {
                playedHandIndices[i] = 0;
            }

            playedCommandCount = 0;
        }
    }
}

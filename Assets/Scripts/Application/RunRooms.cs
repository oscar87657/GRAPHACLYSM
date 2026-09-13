using System;
using Graphaclysm.Core.Runs;

namespace Graphaclysm.Application
{
    public sealed partial class RunGameSession
    {
        private int removalHealthCost;
        public int TrainingPower { get; private set; }
        public int PreparedShield { get; private set; }
        public int MasteryRank => Math.Min(5, Growth.SpentPoints / 5);
        public string RoomResult { get; private set; } = "";
        public RoomStory CurrentRoom => Phase == RunPhase.Room && Map.ActiveNodeIndex >= 0 ?
            HasExpeditionSupplies ? (Map.Definition.GetNode(Map.ActiveNodeIndex).Kind==RunNodeKind.Treasure ? SupplyVaultStory : supplyRoomStory) : Map.Definition.GetNode(Map.ActiveNodeIndex).Story : null;
        public bool CanChooseRoomOption(int index)
        {
            var story = CurrentRoom;
            if (story == null || IsShop || index < 0 || index >= story.ChoiceCount) return false;
            var choice = story.GetChoice(index);
            if (persistentHealth <= choice.HealthCost) return false;
            if (choice.Effect == RoomEffect.Training && TrainingPower >= 3) return false;
            if (choice.Effect == RoomEffect.GrowthPoints && Growth.HasPointLimit && Growth.TotalPointBudget >= Growth.MaximumPointBudget) return false;
            if (choice.Effect == RoomEffect.Supply && PreparedShield >= choice.Amount) return false;
            if (choice.Effect == RoomEffect.RelicReward && !CanOfferRelicReward()) return false;
            if (choice.Effect == RoomEffect.CardReward && Deck.Count >= RunDeck.MaximumCards) return false;
            if (choice.Effect == RoomEffect.RemoveCard && Deck.Count <= handSize) return false;
            return true;
        }
        public bool TryChooseRoomOption(int index)
        {
            if (!CanChooseRoomOption(index)) return false;
            bool vault=HasExpeditionSupplies && Map.Definition.GetNode(Map.ActiveNodeIndex).Kind==RunNodeKind.Treasure && index==0;
            var choice = CurrentRoom.GetChoice(index);
            removalHealthCost = choice.Effect == RoomEffect.RemoveCard ? choice.HealthCost : 0;
            if (choice.Effect != RoomEffect.RemoveCard) persistentHealth -= choice.HealthCost;
            RoomResult = choice.Consequence;
            RunPhase next = RunPhase.MapSelection;
            switch (choice.Effect)
            {
                case RoomEffect.Training: TrainingPower = Math.Min(3,TrainingPower+choice.Amount); break;
                case RoomEffect.GrowthPoints: Growth.AddPoints(choice.Amount); break;
                case RoomEffect.Coins: Coins += choice.Amount; break;
                case RoomEffect.Research: ResearchTickets += choice.Amount; break;
                case RoomEffect.Supply: PreparedShield = Math.Max(PreparedShield,choice.Amount); break;
                case RoomEffect.Heal:
                    persistentHealth = Math.Min(PlayerMaxHealth, persistentHealth + (int)Math.Ceiling(PlayerMaxHealth * choice.Amount / 100.0)); break;
                case RoomEffect.Resonance:
                    persistentResonance = Math.Min(Graphaclysm.Core.Combat.TacticalCombatState.UltimateCost, persistentResonance + choice.Amount); break;
                case RoomEffect.CardReward: GenerateRewardOptions(); next = RunPhase.CardReward; break;
                case RoomEffect.RelicReward: GenerateRelicRewardOptions(); next = RunPhase.RelicReward; break;
                case RoomEffect.RemoveCard: next = RunPhase.DeckRefinement; break;
            }
            RunNodeKind completedKind = Map.Definition.GetNode(Map.ActiveNodeIndex).Kind;
            if (!Map.TryCompleteActiveNode()) throw new InvalidOperationException("Room completion lost its active node.");
            AwardExploration(completedKind);
            Phase = next == RunPhase.MapSelection && Map.Phase == RunMapProgressPhase.Completed ? RunPhase.Completed : next;
            if (vault) PrepareVaultSupplies();
            return Record(true, RunCommandKind.ChooseRoom, index);
        }
        public bool TryLeaveRoom()
        {
            if (Phase != RunPhase.Room) return false;
            RunNodeKind completedKind = Map.Definition.GetNode(Map.ActiveNodeIndex).Kind;
            if (!Map.TryCompleteActiveNode()) return false;
            AwardExploration(completedKind);
            RoomResult = "방을 조용히 지나쳤습니다.";
            Phase = Map.Phase == RunMapProgressPhase.Completed ? RunPhase.Completed : RunPhase.MapSelection;
            return Record(true, RunCommandKind.LeaveRoom);
        }
        public bool CanRemoveDeckCard(int index)
        {
            if (Phase != RunPhase.DeckRefinement || index < 0 || index >= Deck.Count || Deck.Count <= handSize) return false;
            for (int i = 0; i < Deck.Count; i++) if (i != index && (Deck[i].IsBaseFunction || Deck[i].IsCalculator || Deck[i].IsFragment)) return true;
            return false;
        }
        public bool TryRemoveDeckCard(int index)
        {
            if (shopRemovalIndex >= 0 && Coins < shopOffers[shopRemovalIndex].Price) return false;
            if (!CanRemoveDeckCard(index) || !Deck.TryRemoveAt(index)) return false;
            if (shopRemovalIndex >= 0) CompleteShopPurchase(shopRemovalIndex);
            persistentHealth -= removalHealthCost; removalHealthCost = 0;
            RoomResult = "기록 한 장을 덜어냈습니다."; FinishReward(); return Record(true, RunCommandKind.RemoveCard, index);
        }
        public bool TrySkipRefinement()
        { if (Phase != RunPhase.DeckRefinement) return false; removalHealthCost = 0; RoomResult = "기록을 그대로 가져갑니다."; FinishReward(); return Record(true, RunCommandKind.SkipRefinement); }
    }
}

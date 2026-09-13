using System;
using System.Collections.Generic;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;

namespace Graphaclysm.Application
{
    // Stable numeric IDs are part of the run save format. Bump RulesVersion when content or rules change.
    public enum RunCommandKind
    {
        SelectNode, PlayCard, UndoCard, Move, UndoMove, ToggleUltimate, Condense,
        Unravel, BeginPlot, ResolvePlot, ResolveEnemy, SelectCardReward, SelectRelicReward,
        SkipReward, ChooseRoom, LeaveRoom, RemoveCard, SkipRefinement,
        PurchaseGrowth, SelectGrowth, UseCombatSkill, MoveTo, ResetGrowth, RefundGrowth, ChooseApproach, DiagramAbility,
        ExecutionDash, LunaPull, PlaceSatellite, SatelliteOrigin, OpeningRoute, ExpeditionEconomy, BuyShop, UseResearch, ContentExpansion, GrandArchive, MarketBalance, StatusRules, TowerArchive, FiveFloors, BattleRework, ApproachTree, StyleTree, GrowthLimit,
        ExpeditionSupplies, ClaimLoot, OpenLootChest, LeaveLoot, ExpeditionPreparation
    }

    public readonly struct RunCommand
    {
        public RunCommand(RunCommandKind kind, int argument = 0) { Kind = kind; Argument = argument; }
        public RunCommandKind Kind { get; }
        public int Argument { get; }
    }

    public sealed class RunSaveData
    {
        public const int FormatVersion = 2;
        public const int RulesVersion = 41;
        public const int MaximumCommands = 65536;
        public uint Seed;
        public string CharacterId;
        public long SavedUtcTicks;
        public LegacyBenefits LegacyBenefits;
        public RunCommand[] Commands;
    }

    public sealed partial class RunGameSession
    {
        private List<RunCommand> journal;
        private string journalCharacter;
        private bool journalOverflow;
        private int practiceRevision;
        public int Revision => IsPractice ? practiceRevision : journal == null ? 0 : journal.Count + (journalOverflow ? 1 : 0);
        public bool CanSave => !IsPractice && journal != null && !journalOverflow;

        internal void EnableJournal(string characterId)
        {
            journalCharacter = characterId;
            journal = new List<RunCommand>(256);
        }

        private bool Record(bool succeeded, RunCommandKind kind, int argument = 0)
        {
            if(succeeded && HasExpeditionSupplies) RefreshSupplyText();
            if (succeeded && IsPractice) practiceRevision++;
            if (succeeded && journal != null)
            {
                if (journal.Count < RunSaveData.MaximumCommands) journal.Add(new RunCommand(kind, argument));
                else journalOverflow = true;
            }
            return succeeded;
        }

        public RunSaveData CaptureSave()
        {
            if (!CanSave) throw new InvalidOperationException("This run cannot be saved.");
            return new RunSaveData { Seed = Seed, CharacterId = journalCharacter, LegacyBenefits = appliedLegacyBenefits,
                SavedUtcTicks = DateTime.UtcNow.Ticks, Commands = journal.ToArray() };
        }

        public static bool TryRestore(RunSaveData data, out RunGameSession restored, out CharacterDefinition character)
        {
            restored = null; character = null;
            if (data == null || data.Seed == 0 || data.Commands == null || data.Commands.Length > RunSaveData.MaximumCommands) return false;
            for (int i = 0; i < PrototypeCharacterCatalog.All.Count; i++)
                if (PrototypeCharacterCatalog.All[i].Id == data.CharacterId) character = PrototypeCharacterCatalog.All[i];
            if (character == null) return false;
            try
            {
                var candidate = PrototypeRunFactory.Create(data.Seed, character, data.LegacyBenefits);
                for (int i = 0; i < data.Commands.Length; i++)
                    if (!candidate.Replay(data.Commands[i])) return false;
                restored = candidate;
                return true;
            }
            catch (ArgumentException) { return false; }
            catch (InvalidOperationException) { return false; }
            catch (IndexOutOfRangeException) { return false; }
        }

        private bool Replay(RunCommand command)
        {
            int a = command.Argument;
            switch (command.Kind)
            {
                case RunCommandKind.SelectNode: return a >= 0 && a < Map.Definition.NodeCount && TrySelectMapNode(a);
                case RunCommandKind.PlayCard: return CurrentBattle != null && a >= 0 && a < CurrentBattle.Deck.HandCount && TryPlayHandCard(a, out _, out _);
                case RunCommandKind.UndoCard: return TryUndoLastPlayedCard(out _);
                case RunCommandKind.Move: return a >= 0 && a < 4 && TryMovePlayer(a == 0 ? -1.5 : a == 1 ? 1.5 : 0, a == 2 ? 1.5 : a == 3 ? -1.5 : 0);
                case RunCommandKind.UndoMove: return TryUndoMove();
                case RunCommandKind.ToggleUltimate: return TryToggleUltimate();
                case RunCommandKind.Condense: return TryCondense();
                case RunCommandKind.Unravel: return TryUnravel();
                case RunCommandKind.BeginPlot: return TryBeginPlot();
                case RunCommandKind.ResolvePlot:
                    if (Phase != RunPhase.Battle || CurrentBattle.Battle.Phase != BattlePhase.Plotting) return false;
                    ResolvePlot(); return true;
                case RunCommandKind.ResolveEnemy:
                    if (Phase != RunPhase.Battle || CurrentBattle.Battle.Phase != BattlePhase.EnemyTurn) return false;
                    ResolveEnemyTurn(); return true;
                case RunCommandKind.SelectCardReward: return TrySelectReward(a);
                case RunCommandKind.SelectRelicReward: return TrySelectRelicReward(a);
                case RunCommandKind.SkipReward: return TrySkipReward();
                case RunCommandKind.ChooseRoom: return TryChooseRoomOption(a);
                case RunCommandKind.LeaveRoom: return TryLeaveRoom();
                case RunCommandKind.RemoveCard: return TryRemoveDeckCard(a);
                case RunCommandKind.SkipRefinement: return TrySkipRefinement();
                case RunCommandKind.PurchaseGrowth: return CanEditGrowth && Growth.TryPurchaseCommand(a)
                    && Record(true, RunCommandKind.PurchaseGrowth, a);
                case RunCommandKind.SelectGrowth: return CanEditGrowth && Growth.TrySelectCommand(a)
                    && Record(true, RunCommandKind.SelectGrowth, a);
                case RunCommandKind.UseCombatSkill: return TryUseCombatSkill(a);
                case RunCommandKind.MoveTo:
                    if (a < 0) return false;
                    UnpackMove(a, out double moveX, out double moveY);
                    return TryMovePlayerTo(moveX, moveY);
                case RunCommandKind.ResetGrowth: return TryResetGrowth();
                case RunCommandKind.RefundGrowth: return TryRefundGrowthNode(Growth.IndexOfCommand(a));
                case RunCommandKind.ChooseApproach: return TryChooseApproach((CombatApproach)a);
                case RunCommandKind.ExecutionDash:
                case RunCommandKind.LunaPull:
                case RunCommandKind.PlaceSatellite:
                    if (a < 0 || a >= (1 << 20)) return false;
                    UnpackMove(a, out double skillX, out double skillY);
                    if (command.Kind == RunCommandKind.PlaceSatellite) return TryPlaceSatellite(skillX, skillY);
                    return command.Kind == RunCommandKind.ExecutionDash ? TryUseExecutionDash(skillX, skillY) : TryUseLunaPull(skillX, skillY);
                case RunCommandKind.SatelliteOrigin: return a == 0 && TryToggleSatelliteOrigin();
                case RunCommandKind.OpeningRoute: return a == 0 && TryEnableOpeningRoute();
                case RunCommandKind.ExpeditionEconomy: return a == 0 && TryEnableEconomy();
                case RunCommandKind.BuyShop: return TryBuyShop(a);
                case RunCommandKind.UseResearch: return a == 0 && TryUseResearch();
                case RunCommandKind.ContentExpansion: return a == 0 && TryEnableContentExpansion();
                case RunCommandKind.GrandArchive: return a == 0 && TryEnableGrandArchive();
                case RunCommandKind.MarketBalance: return a == 0 && TryEnableMarketBalance();
                case RunCommandKind.StatusRules: return a == 0 && TryEnableStatusRules();
                case RunCommandKind.TowerArchive: return a == 0 && TryEnableTowerArchive();
                case RunCommandKind.FiveFloors: return a == 0 && TryEnableFiveFloors();
                case RunCommandKind.BattleRework: return a == 0 && TryEnableBattleRework();
                case RunCommandKind.ApproachTree: return a == 0 && TryEnableApproachTree();
                case RunCommandKind.StyleTree: return a == 0 && TryEnableStyleTree();
                case RunCommandKind.GrowthLimit: return a == 0 && TryEnableGrowthLimit();
                case RunCommandKind.ExpeditionSupplies: return (a==0 || a==1) && TryEnableExpeditionSupplies(a==0);
                case RunCommandKind.ClaimLoot: return TryClaimLoot(a);
                case RunCommandKind.OpenLootChest: return a>=0 && a<12 && TryOpenLootChest(a/2,a%2==1);
                case RunCommandKind.LeaveLoot: return a==0 && TryLeaveLoot();
                case RunCommandKind.ExpeditionPreparation: return TryEnablePreparation(a);
                case RunCommandKind.DiagramAbility:
                    if (a < 0 || a >= (1 << 23)) return false;
                    UnpackMove(a & ((1 << 20) - 1), out double diagramX, out double diagramY);
                    int rotation = a >> 20;
                    return TryUseDiagramAbility(diagramX, diagramY, rotation == 7 ? 180 : rotation * 30 - 90);
                default: return false;
            }
        }

        private static int PackMove(double x, double y)
        {
            int quantizedX = Math.Max(0, Math.Min(1000, (int)Math.Round(x * 100)));
            int quantizedY = Math.Max(0, Math.Min(800, (int)Math.Round((y + 4) * 100)));
            return (quantizedX << 10) | quantizedY;
        }

        private static void UnpackMove(int packed, out double x, out double y)
        {
            x = ((packed >> 10) & 1023) / 100.0;
            y = (packed & 1023) / 100.0 - 4.0;
        }
    }
}

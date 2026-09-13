using System;
using Graphaclysm.Core.Combat;

namespace Graphaclysm.Core.Runs
{
    /// <summary>
    /// Run-scoped constellation state. Definitions are immutable catalog data; acquisition and
    /// loadout selection are stored separately and rebuilt by command replay.
    /// </summary>
    public sealed partial class RunGrowthState
    {
        public const int StartingPoints = 6;
        public const int ExpectedFinalBudget = 35;
        public const int StylePointLimit = 9;
        public const int MaximumCombatForms = 2;

        private readonly GrowthNodeDefinition[] nodes;
        private readonly bool[] acquired;
        private readonly bool[] equipped;
        private int awardedPoints;
        private int spentPoints;

        public RunGrowthState(CombatArchetype archetype, CombatApproach approach=CombatApproach.None, bool styleTree=false, bool boundedPoints=false, bool earnedStart=false)
        {
            Archetype = archetype;
            SpecializedApproach=approach;
            IsStyleTree=styleTree;
            if(boundedPoints&&!styleTree)throw new ArgumentException("Point limit requires a style tree.",nameof(boundedPoints));
            HasPointLimit=boundedPoints;
            InitialPoints=earnedStart?3:StartingPoints;
            nodes = approach==CombatApproach.None?GrowthCatalog.For(archetype):styleTree?StyleTreeCatalog.Nodes(approach):ApproachGrowthCatalog.Nodes(approach);
            acquired = new bool[nodes.Length];
            equipped = new bool[nodes.Length];
            if(IsSpecialized) acquired[0]=true;
        }

        public CombatArchetype Archetype { get; }
        public bool IsStyleTree { get; }
        public bool HasPointLimit { get; }
        public int InitialPoints { get; }
        public int MaximumPointBudget => HasPointLimit?StylePointLimit:int.MaxValue;
        public int ParentOf(int index)=>IsStyleTree?StyleTreeCatalog.Parent(index):ApproachGrowthCatalog.Parent(index);
        public CombatApproach SpecializedApproach { get; }
        public bool IsSpecialized=>SpecializedApproach!=CombatApproach.None;
        public int NodeCount => nodes.Length;
        public int Points => InitialPoints + awardedPoints - spentPoints;
        public int SpentPoints => spentPoints;
        public int AwardedPoints => awardedPoints;
        public int TotalPointBudget => InitialPoints + awardedPoints;
        public int AcquiredCount { get { int count = 0; for (int i = 0; i < acquired.Length; i++) if (acquired[i]) count++; return count; } }
        public int EquippedCombatFormCount { get { int count = 0; for (int i = 0; i < equipped.Length; i++) if (equipped[i] && nodes[i].EquipKind == GrowthEquipKind.CombatForm) count++; return count; } }

        // Compatibility projections for the battle prototype. Only the six already playable
        // v15 skill forms and six original ultimate forms map to legacy battle variants.
        public int ActiveVariant => PrototypeCombatVariant(0);
        public int SecondaryActiveVariant => PrototypeCombatVariant(1);
        public int ModuleVariant => 0;
        public int UltimateVariant => PrototypeUltimateVariant();
        public int UnlockedMask { get { int mask = 0; for (int i = 0; i < acquired.Length && i < 31; i++) if (acquired[i]) mask |= 1 << i; return mask; } }

        public GrowthNodeDefinition GetNode(int index)
        {
            if (index < 0 || index >= nodes.Length) throw new ArgumentOutOfRangeException(nameof(index));
            return nodes[index];
        }

        public int IndexOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            for (int i = 0; i < nodes.Length; i++) if (nodes[i].Id == id) return i;
            return -1;
        }

        public int IndexOfCommand(int commandId)
        {
            for (int i = 0; i < nodes.Length; i++) if (nodes[i].CommandId == commandId) return i;
            return -1;
        }

        public bool IsUnlocked(int index) => index >= 0 && index < acquired.Length && acquired[index];
        public bool IsEquipped(int index) => index >= 0 && index < equipped.Length && equipped[index];
        public bool IsUnlocked(string id) => IsUnlocked(IndexOf(id));
        public bool IsEquipped(string id) => IsEquipped(IndexOf(id));

        public GrowthNodeDefinition GetEquippedNode(GrowthEquipKind kind, int ordinal = 0)
        {
            if (ordinal < 0) return null;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (!equipped[i] || nodes[i].EquipKind != kind) continue;
                if (ordinal-- == 0) return nodes[i];
            }
            return null;
        }

        public bool CanPurchase(int index)
        {
            if (index < 0 || index >= nodes.Length || acquired[index]) return false;
            GrowthNodeDefinition node = nodes[index];
            if (Points < node.Cost) return false;
            if (SpentPoints < GrowthTreePaths.RequiredInvestment(node.Id)) return false;
            if (!GrowthTreePaths.IsReachable(this, index)) return false;
            for (int i = 0; i < node.RequiresAll.Length; i++) if (!IsUnlocked(node.RequiresAll[i])) return false;
            if (node.RequiresAtLeastCount > 0)
            {
                int count = 0;
                for (int i = 0; i < node.RequiresAtLeast.Length; i++) if (IsUnlocked(node.RequiresAtLeast[i])) count++;
                if (count < node.RequiresAtLeastCount) return false;
            }
            if (!string.IsNullOrEmpty(node.ExclusiveGroup))
                for (int i = 0; i < nodes.Length; i++)
                    if (acquired[i] && nodes[i].ExclusiveGroup == node.ExclusiveGroup) return false;
            return true;
        }

        public bool TryPurchase(int index)
        {
            if (!CanPurchase(index)) return false;
            acquired[index] = true;
            spentPoints += nodes[index].Cost;
            TryAutoEquip(index);
            return true;
        }

        public bool TryPurchaseCommand(int commandId)
        {
            int index = IndexOfCommand(commandId);
            return index >= 0 && TryPurchase(index);
        }

        public bool CanSelect(int index)
        {
            if (index < 0 || index >= nodes.Length || !acquired[index]) return false;
            GrowthNodeDefinition node = nodes[index];
            if (node.EquipKind == GrowthEquipKind.Passive) return false;
            if (equipped[index]) return node.EquipKind == GrowthEquipKind.CombatForm || node.EquipKind == GrowthEquipKind.Trait;
            if (node.EquipKind == GrowthEquipKind.CombatForm)
            {
                if (EquippedCombatFormCount >= MaximumCombatForms) return false;
                for (int i = 0; i < nodes.Length; i++)
                    if (equipped[i] && nodes[i].EquipKind == GrowthEquipKind.CombatForm && nodes[i].Family == node.Family) return false;
            }
            if (node.EquipKind == GrowthEquipKind.Trait)
            {
                for (int i = 0; i < node.RequiresAll.Length; i++)
                {
                    int dependency = IndexOf(node.RequiresAll[i]);
                    if (dependency >= 0 && nodes[dependency].EquipKind != GrowthEquipKind.Passive && !equipped[dependency])
                        return false;
                }
                string group = TraitGroup(node);
                int limit = node.EquipLimit > 0 ? node.EquipLimit : 1;
                int selected = 0;
                for (int i = 0; i < nodes.Length; i++) if (equipped[i] && TraitGroup(nodes[i]) == group) selected++;
                if (selected >= limit) return false;
            }
            return true;
        }

        public bool TrySelect(int index)
        {
            if (!CanSelect(index)) return false;
            GrowthNodeDefinition node = nodes[index];
            if (equipped[index])
            {
                UnequipWithDependents(index);
                return true;
            }
            if (node.EquipKind == GrowthEquipKind.Identity || node.EquipKind == GrowthEquipKind.Ultimate)
                for (int i = 0; i < nodes.Length; i++) if (nodes[i].EquipKind == node.EquipKind) UnequipWithDependents(i);
            equipped[index] = true;
            return true;
        }

        public bool TrySelectCommand(int commandId)
        {
            int index = IndexOfCommand(commandId);
            return index >= 0 && TrySelect(index);
        }

        public void AddPoints(int amount)
        {
            if (amount <= 0) return;
            if(HasPointLimit)amount=Math.Min(amount,Math.Max(0,StylePointLimit-TotalPointBudget));
            if (awardedPoints > int.MaxValue - amount) throw new ArgumentOutOfRangeException(nameof(amount));
            awardedPoints += amount;
        }

        // Single-node refunds never silently remove another purchase.
        public bool CanRefund(int index) => !(IsSpecialized && index==0) && IsUnlocked(index) && RefundBlocker(index) < 0;

        public int RefundBlocker(int index)
        {
            if (!IsUnlocked(index)) return -1;
            acquired[index] = false;
            int remaining = spentPoints - nodes[index].Cost;
            try
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    if (!acquired[i]) continue;
                    var node = nodes[i];
                    if (remaining - node.Cost < GrowthTreePaths.RequiredInvestment(node.Id)
                        || !GrowthTreePaths.IsReachable(this, i)) return i;
                    for (int j = 0; j < node.RequiresAll.Length; j++)
                        if (!IsUnlocked(node.RequiresAll[j])) return i;
                    int count = 0;
                    for (int j = 0; j < node.RequiresAtLeast.Length; j++)
                        if (IsUnlocked(node.RequiresAtLeast[j])) count++;
                    if (count < node.RequiresAtLeastCount) return i;
                }
                return -1;
            }
            finally { acquired[index] = true; }
        }

        public bool TryRefund(int index)
        {
            if (!CanRefund(index)) return false;
            UnequipWithDependents(index);
            acquired[index] = false;
            spentPoints -= nodes[index].Cost;
            return true;
        }

        public bool TryReset()
        {
            if(IsSpecialized && spentPoints==0)return false;
            if (spentPoints == 0 && AcquiredCount == 0) return false;
            Array.Clear(acquired, 0, acquired.Length);
            Array.Clear(equipped, 0, equipped.Length);
            spentPoints = 0;
            if(IsSpecialized) acquired[0]=true;
            return true;
        }

        public BattleSkillLoadout CreateLoadout()
            => new BattleSkillLoadout(ActiveVariant, ModuleVariant, UltimateVariant, 0,
                SecondaryActiveVariant, CreateCompiledBuild());

        public CompiledGrowthBuild CreateCompiledBuild()
        {
            var acquiredIds = new string[AcquiredCount];
            int equippedCount = 0;
            for (int i = 0; i < equipped.Length; i++) if (equipped[i]) equippedCount++;
            var equippedIds = new string[equippedCount];
            int acquiredCursor = 0, equippedCursor = 0;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (acquired[i]) acquiredIds[acquiredCursor++] = nodes[i].Id;
                if (equipped[i]) equippedIds[equippedCursor++] = nodes[i].Id;
            }
            return new CompiledGrowthBuild(acquiredIds, equippedIds);
        }

        private void TryAutoEquip(int index)
        {
            if (nodes[index].EquipKind != GrowthEquipKind.Passive) TrySelect(index);
        }

        private void UnequipWithDependents(int index)
        {
            if (index < 0 || index >= equipped.Length || !equipped[index]) return;
            equipped[index] = false;
            string id = nodes[index].Id;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (!equipped[i] || nodes[i].EquipKind != GrowthEquipKind.Trait) continue;
                for (int requirement = 0; requirement < nodes[i].RequiresAll.Length; requirement++)
                    if (nodes[i].RequiresAll[requirement] == id) { equipped[i] = false; break; }
            }
        }

        private static string TraitGroup(GrowthNodeDefinition node)
        {
            if (!string.IsNullOrEmpty(node.EquipLimitGroup)) return node.EquipLimitGroup;
            if (!string.IsNullOrEmpty(node.ExclusiveGroup)) return node.ExclusiveGroup;
            return node.RequiresAll.Length > 0 ? node.RequiresAll[0] : node.Id;
        }

        private int PrototypeCombatVariant(int slot)
        {
            int found = 0;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (!equipped[i] || nodes[i].EquipKind != GrowthEquipKind.CombatForm || !nodes[i].IsImplemented) continue;
                if (found++ != slot) continue;
                string id = nodes[i].Id;
                if (id == "ian.triple.form" || id == "luna.fulljump.form") return 1;
                if (id == "ian.execute.form" || id == "luna.return.form") return 2;
                if (id == "ian.exchange.form" || id == "luna.chain.form") return 3;
            }
            return 0;
        }

        private int PrototypeUltimateVariant()
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                if (!equipped[i] || nodes[i].EquipKind != GrowthEquipKind.Ultimate || !nodes[i].IsImplemented) continue;
                string id = nodes[i].Id;
                if (id == "ian.u.collapse.form" || id == "luna.u.embrace.form") return 1;
                if (id == "ian.u.immortal.form" || id == "luna.u.blade.form") return 2;
                if (id == "ian.u.invert.form" || id == "luna.u.stop.form") return 3;
            }
            return 0;
        }
    }
}

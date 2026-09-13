using Graphaclysm.Core.Runs;

namespace Graphaclysm.Core.Combat
{
    public readonly struct BattleSkillLoadout
    {
        public BattleSkillLoadout(int activeVariant, int moduleVariant, int ultimateVariant, int traitMask = 0,
            int secondaryActiveVariant = 0, CompiledGrowthBuild growth = null)
        {
            ActiveVariant = activeVariant;
            SecondaryActiveVariant = secondaryActiveVariant;
            ModuleVariant = moduleVariant;
            UltimateVariant = ultimateVariant;
            TraitMask = traitMask;
            Growth = growth;
        }

        public int ActiveVariant { get; }
        public int SecondaryActiveVariant { get; }
        public int ModuleVariant { get; }
        public int UltimateVariant { get; }
        public int TraitMask { get; }
        public CompiledGrowthBuild Growth { get; }
        public bool HasTrait(int nodeIndex) => nodeIndex >= 0 && nodeIndex < 31 && (TraitMask & (1 << nodeIndex)) != 0;
    }
}

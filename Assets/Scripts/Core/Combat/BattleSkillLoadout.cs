namespace Graphaclysm.Core.Combat
{
    public readonly struct BattleSkillLoadout
    {
        public BattleSkillLoadout(int activeVariant, int moduleVariant, int ultimateVariant, int traitMask = 0)
        {
            ActiveVariant = activeVariant;
            ModuleVariant = moduleVariant;
            UltimateVariant = ultimateVariant;
            TraitMask = traitMask;
        }

        public int ActiveVariant { get; }
        public int ModuleVariant { get; }
        public int UltimateVariant { get; }
        public int TraitMask { get; }
        public bool HasTrait(int nodeIndex) => nodeIndex >= 0 && nodeIndex < 31 && (TraitMask & (1 << nodeIndex)) != 0;
    }
}

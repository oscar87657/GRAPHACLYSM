namespace Graphaclysm.Core.Combat
{
    public readonly struct BattleSkillLoadout
    {
        public BattleSkillLoadout(int activeVariant, int moduleVariant, int ultimateVariant)
        {
            ActiveVariant = activeVariant;
            ModuleVariant = moduleVariant;
            UltimateVariant = ultimateVariant;
        }

        public int ActiveVariant { get; }
        public int ModuleVariant { get; }
        public int UltimateVariant { get; }
    }
}

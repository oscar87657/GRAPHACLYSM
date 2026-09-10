namespace Graphaclysm.Core.Combat
{
    public readonly struct BattleSkillLoadout
    {
        public BattleSkillLoadout(bool activeUnlocked, int activeVariant, int ultimateVariant, int startingResonanceBonus)
        {
            ActiveUnlocked = activeUnlocked;
            ActiveVariant = activeVariant;
            UltimateVariant = ultimateVariant;
            StartingResonanceBonus = startingResonanceBonus;
        }

        public bool ActiveUnlocked { get; }
        public int ActiveVariant { get; }
        public int UltimateVariant { get; }
        public int StartingResonanceBonus { get; }
    }
}

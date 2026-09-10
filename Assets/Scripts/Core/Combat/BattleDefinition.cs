using System;

namespace Graphaclysm.Core.Combat
{
    public sealed class BattleDefinition
    {
        private readonly EnemyDefinition[] enemies;
        private readonly BattleTerrainDefinition[] terrain;

        public BattleDefinition(int playerMaxHealth, int playerMaxEnergy, EnemyDefinition[] enemies,
            CombatArchetype archetype = CombatArchetype.None, bool calculator = false, bool fragments = false,
            BattleTerrainDefinition[] terrain = null)
        {
            if (playerMaxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerMaxHealth));
            }

            if (playerMaxEnergy <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerMaxEnergy));
            }

            if (enemies == null || enemies.Length == 0)
            {
                throw new ArgumentException("A battle requires at least one enemy.", nameof(enemies));
            }

            PlayerMaxHealth = playerMaxHealth;
            Archetype = archetype;
            if (calculator && fragments) throw new ArgumentException("A battle uses one equation mode.");
            UsesCalculator = calculator; UsesFragments = fragments;
            PlayerMaxEnergy = playerMaxEnergy;
            this.enemies = new EnemyDefinition[enemies.Length];
            Array.Copy(enemies, this.enemies, enemies.Length);
            this.terrain = terrain == null ? Array.Empty<BattleTerrainDefinition>() : new BattleTerrainDefinition[terrain.Length];
            if (terrain != null) Array.Copy(terrain, this.terrain, terrain.Length);

            for (int i = 0; i < this.enemies.Length; i++)
            {
                if (this.enemies[i] == null)
                {
                    throw new ArgumentException("Enemy definitions cannot contain null.", nameof(enemies));
                }
            }
            for (int i = 0; i < this.terrain.Length; i++)
            {
                if (this.terrain[i] == null)
                    throw new ArgumentException("Terrain definitions cannot contain null.", nameof(terrain));
            }
        }

        public int PlayerMaxHealth { get; }
        public CombatArchetype Archetype { get; }
        public bool UsesCalculator { get; }
        public bool UsesFragments { get; }
        public int PlayerMaxEnergy { get; }
        public int EnemyCount
        {
            get { return enemies.Length; }
        }
        public int TerrainCount => terrain.Length;

        public EnemyDefinition GetEnemy(int index)
        {
            return enemies[index];
        }

        public BattleTerrainDefinition GetTerrain(int index)
        {
            return terrain[index];
        }
    }
}

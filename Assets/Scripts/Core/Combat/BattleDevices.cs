using System;
using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Equations;

namespace Graphaclysm.Core.Combat
{
    public sealed partial class BattleSession
    {
        public const int PulseDamage = 6, DeviceShield = 5, CapacitorDamage = 6;
        public const double DeviceBlastRadius = 2.1;
        private bool[] terrainSpent;
        private int[] terrainSilencedTurn;
        public bool IsDeviceSpent(int index) => terrainSpent[index];
        public bool IsPulseActive(int index) => GetTerrain(index).Kind == BattleTerrainKind.Pulse
            && Turn % 2 == 0 && terrainSilencedTurn[index] != Turn;
        public bool TraceTouchesDevice(int index) => Equation.HasBase
            && EquationAnalyzer.IntersectsCircle(Equation, GetTerrain(index).X, GetTerrain(index).Y,
                GetTerrain(index).Radius, Equation.CurveSegmentCount);
        public int PreviewDeviceDamage(EnemyState enemy)
        {
            if (!enemy.IsAlive) return 0;
            int damage = 0;
            for (int i = 0; i < TerrainCount; i++)
                if (GetTerrain(i).Kind == BattleTerrainKind.Capacitor && !terrainSpent[i]
                    && TraceTouchesDevice(i) && InDeviceRadius(i, enemy.X, enemy.Y, DeviceBlastRadius + EnemyHitRadius))
                    damage += CapacitorDamage;
            return damage;
        }
        public int PreviewHealthDamage(EnemyState enemy)
        {
            int trace = PreviewDamage(enemy), device = PreviewDeviceDamage(enemy);
            int shield = enemy.Statuses.Get(CombatStatusKind.Shield);
            int healthDamage = Math.Max(0, trace - shield);
            shield = Math.Max(0, shield - trace);
            if (trace > 0)
            {
                for (int card = 0; card < playedCardCount; card++)
                    for (int a = 0; a < playedCards[card].AbilityCount; a++)
                    {
                        var ability = playedCards[card].GetAbility(a);
                        if (ability.Target == AbilityTarget.Enemy && ability.Kind == CardAbilityKind.Cleanse) shield = 0;
                    }
                if (!HasApproachUltimates && Tactics != null && Tactics.UltimateArmed && Tactics.Archetype == CombatArchetype.Ian
                    && Tactics.UltimateVariant == 3) shield = 0;
            }
            int deviceHealth = Math.Max(0, device - shield);
            shield = Math.Max(0, shield - device);
            return Math.Min(enemy.Health, healthDamage + deviceHealth + Math.Max(0, PreviewRecordingFollowupDamage(enemy) + PreviewApproachUltimateDamage(enemy) + PreviewStyleDamage(enemy) - shield));
        }
        public int PreviewPulseDamage(double x, double y, double radius, bool afterPlot)
        {
            int damage = 0;
            for (int i = 0; i < TerrainCount; i++)
                if (IsPulseActive(i) && !(afterPlot && TraceTouchesDevice(i))
                    && InDeviceRadius(i, x, y, GetTerrain(i).Radius + radius)) damage += PulseDamage;
            return damage;
        }
        private bool InDeviceRadius(int index, double x, double y, double reach)
        {
            double dx = x - GetTerrain(index).X, dy = y - GetTerrain(index).Y;
            return dx * dx + dy * dy <= reach * reach;
        }
        private int ResolvePlotDevices()
        {
            int damage = 0;
            for (int i = 0; i < TerrainCount; i++)
            {
                var device = GetTerrain(i);
                if (terrainSpent[i] || !TraceTouchesDevice(i)) continue;
                if (device.Kind == BattleTerrainKind.Pulse) terrainSilencedTurn[i] = Turn;
                else if (device.Kind == BattleTerrainKind.Aegis && Tactics != null)
                {
                    Tactics.Statuses.Add(CombatStatusKind.Shield, DeviceShield, 1);
                    terrainSpent[i] = true;
                }
                else if (device.Kind == BattleTerrainKind.Capacitor)
                {
                    terrainSpent[i] = true;
                    foreach (var enemy in enemies)
                        if (enemy.IsAlive && InDeviceRadius(i, enemy.X, enemy.Y, DeviceBlastRadius + EnemyHitRadius))
                        { enemy.TakeDamage(CapacitorDamage); damage += CapacitorDamage; }
                }
            }
            return damage;
        }
        private int ResolvePulseTerrain()
        {
            int incoming = Tactics == null ? 0 : PreviewPulseDamage(Tactics.X, Tactics.Y, TacticalCombatState.PlayerRadius, false);
            foreach (var enemy in enemies)
                if (enemy.IsAlive) enemy.TakeDamage(PreviewPulseDamage(enemy.X, enemy.Y, EnemyHitRadius, false));
            if (Tactics != null) incoming = AbsorbPlayerDamage(incoming);
            PlayerHealth = Math.Max(0, PlayerHealth - incoming);
            return incoming;
        }
        private int AbsorbPlayerDamage(int amount)
        {
            int health = Tactics.Statuses.AbsorbDamage(amount);
            LastPlayerHealthDamage += health;
            LastPlayerShieldDamage += amount-health;
            return health;
        }
    }
}

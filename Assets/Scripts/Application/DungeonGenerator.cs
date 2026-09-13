using System;
using System.Collections.Generic;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Decks;
using Graphaclysm.Core.Runs;

namespace Graphaclysm.Application
{
    /// <summary>Seeded, reachable three-floor dungeon with eight rooms per path per floor. Allocation occurs only when a new run is constructed.</summary>
    public static class DungeonGenerator
    {
        public const int RoomsPerFloor = 8, FloorCount = 3, Depths = RoomsPerFloor * FloorCount;
        private static readonly string[] EncounterNames = { "부서진 관측실", "흔들리는 다리", "유리의 정원", "빈 악보실", "낮은 회랑", "침묵의 기계", "뒤틀린 중정", "빛의 매듭" };
        private static readonly string[] EnemyNames = { "축 포식자", "공백체", "분기 잔영", "유리 파수꾼", "금속의 메아리", "좌표 도둑" };
        private static readonly double[] TerrainX = { 2.0, 3.1, 5.0, 6.8, 8.1, 2.2, 5.2, 7.7, 3.0, 6.4, 8.4, 1.6 };
        private static readonly double[] TerrainY = { 1.8, 0.2, 2.6, 1.2, -1.8, -2.4, -0.8, 2.7, 3.0, -2.7, 0.3, -0.5 };
        public static RunMapDefinition Generate(uint seed, CharacterDefinition character, int maxHealthBonus = 0)
        {
            int playerMaxHealth = character.MaxHealth + Math.Max(0, maxHealthBonus);
            var random = new XorShiftRandom(seed ^ 0x6C8E9CF5u);
            var starts = new int[Depths]; var widths = new int[Depths]; int total = 0;
            for (int d = 0; d < Depths; d++) { starts[d] = total; widths[d] = d % RoomsPerFloor == RoomsPerFloor - 1 ? 1 : 2 + random.Next(2); total += widths[d]; }
            var lanes = new int[total]; var edges = new List<int>[total];
            for (int d = 0; d < Depths; d++)
                for (int j = 0; j < widths[d]; j++)
                { int i = starts[d] + j; lanes[i] = widths[d] == 1 ? 1 : widths[d] == 2 ? j * 2 : j; edges[i] = new List<int>(3); }
            for (int d = 0; d < Depths - 1; d++)
            {
                // Every source has a way forward; every destination has at least one predecessor.
                for (int j = 0; j < widths[d]; j++)
                {
                    int source = starts[d] + j; int first = random.Next(widths[d + 1]);
                    edges[source].Add(starts[d + 1] + first);
                    if (widths[d + 1] > 1)
                        edges[source].Add(starts[d + 1] + (first + 1 + random.Next(widths[d + 1] - 1)) % widths[d + 1]);
                }
                for (int j = 0; j < widths[d + 1]; j++)
                {
                    int target = starts[d + 1] + j; bool reached = false;
                    for (int k = 0; k < widths[d]; k++) reached |= edges[starts[d] + k].Contains(target);
                    if (!reached) edges[starts[d] + random.Next(widths[d])].Add(target);
                }
            }
            var nodes = new RunMapNodeDefinition[total];
            for (int d = 0; d < Depths; d++)
                for (int j = 0; j < widths[d]; j++)
                {
                    int index = starts[d] + j; RunNodeKind kind = PickKind(d, j, random);
                    RoomStory story = kind == RunNodeKind.Event ? RoomStoryCatalog.Event(random.Next(RoomStoryCatalog.EventCount))
                        : kind == RunNodeKind.Rest ? RoomStoryCatalog.Rest : kind == RunNodeKind.Treasure ? RoomStoryCatalog.Treasure
                        : kind == RunNodeKind.Workshop ? RoomStoryCatalog.Workshop : kind == RunNodeKind.Observatory ? RoomStoryCatalog.Observatory
                        : kind == RunNodeKind.Supply ? RoomStoryCatalog.Supply : null;
                    int template = random.Next(EncounterNames.Length);
                    string name = story?.Title ?? (kind == RunNodeKind.Boss ? (random.Next(2) == 0 ? "경계의 기억" : "멎은 성운") : EncounterNames[template]);
                    if(kind == RunNodeKind.Boss) name = d < 8 ? "첫 문 · 유리 감시자" : d < 16 ? "둘째 문 · 밤의 기록자" : "심층 · 무명의 원점";
                    BattleDefinition battle = story == null ? Encounter(d, kind, template, random, character, playerMaxHealth) : null;
                    if (d < 7 && battle != null)
                    {
                        int trial = (d+j)%3;
                        battle = TacticalEncounter(trial, character, playerMaxHealth, kind == RunNodeKind.Elite);
                        name = trial == 0 ? "사선과 봉인" : trial == 1 ? "끊어야 할 연결" : "봉인된 매듭";
                    }
                    nodes[index] = new RunMapNodeDefinition("dungeon." + d + "." + j, name, d, lanes[index], kind, battle,
                        edges[index].ToArray(), story, playerMaxHealth);
                }
            return new RunMapDefinition(nodes, RoomsPerFloor);
        }
        // Three short, repeatable first-floor encounters for evaluating different answers with the same deck.
        public static BattleDefinition TacticalEncounter(int variant, CharacterDefinition c, int maxHealth, bool elite = false)
        {
            if (variant < 0 || variant > 2) throw new ArgumentOutOfRangeException(nameof(variant));
            var first = variant == 2 ? EnemyBehaviorDefinition.LinkGuardian() : EnemyBehaviorDefinition.LineGunner();
            var second = variant == 1 ? EnemyBehaviorDefinition.LinkGuardian() : EnemyBehaviorDefinition.SealScribe();
            int health = elite ? 32 : 24, attack = elite ? 4 : 3;
            return new BattleDefinition(maxHealth,c.MaxEnergy,new[] {
                new EnemyDefinition("trial.first",variant == 2 ? "연결 수호자" : "선형 포수",2,1,health,attack,first),
                new EnemyDefinition("trial.second",variant == 1 ? "연결 수호자" : "봉인 서기관",8,1,health+2,attack,second)
            },c.Archetype,fragments:true,terrain:new[] {
                new BattleTerrainDefinition("trial.prism",BattleTerrainKind.Prism,5,0,.48),
                new BattleTerrainDefinition("trial.aegis",BattleTerrainKind.Aegis,2,-.7,.48)
            });
        }

        private static RunNodeKind PickKind(int depth, int option, IRandomSource random)
        {
            depth %= RoomsPerFloor;
            if (depth == 0 || depth == 3 || depth == 5) return option % 2 == 0 ? RunNodeKind.Battle : RunNodeKind.Elite;
            if (depth == 7) return RunNodeKind.Boss;
            if (depth == 2) return option == 0 ? RunNodeKind.Event : option == 1 ? RunNodeKind.Rest : RunNodeKind.Observatory;
            if (depth == 4) return option == 0 ? RunNodeKind.Treasure : option == 1 ? RunNodeKind.Workshop : RunNodeKind.Supply;
            if (depth == 6) return option == 0 ? RunNodeKind.Elite : option == 1 ? RunNodeKind.Rest : RunNodeKind.Workshop;
            if (option == 0) return RunNodeKind.Battle;
            if (option == 1) return random.Next(2) == 0 ? RunNodeKind.Event : RunNodeKind.Observatory;
            return random.Next(2) == 0 ? RunNodeKind.Supply : RunNodeKind.Rest;
        }
        private static BattleDefinition Encounter(int depth, RunNodeKind kind, int template, IRandomSource random, CharacterDefinition c, int playerMaxHealth)
        {
            int count = kind == RunNodeKind.Boss ? 3 : 2 + random.Next(2);
            var enemies = new EnemyDefinition[count];
            double phase = random.Next(628) / 100.0;
            bool mirrorX = random.Next(2) == 0, mirrorY = random.Next(2) == 0;
            for (int i = 0; i < count; i++)
            {
                double angle = phase + i * Math.PI * 2 / count;
                double x = 5 + (2.0 + template % 3 * 0.55) * Math.Cos(angle);
                double y = (1.4 + template % 2 * 0.75) * Math.Sin(angle);
                double spread = i / (double)(count - 1);
                switch (template)
                {
                    case 1: x = 1.3 + spread * 7.4; y = 2.8; break;
                    case 2: x = 1.8 + spread * 6.4; y = -2.8; break;
                    case 3: x = 4 + i * 1.25; y = i % 2 == 0 ? 0.6 : -0.6; break;
                    case 4: x = i == 0 ? 1 : i == 1 ? 9 : 5; y = i == 0 ? 3 : i == 1 ? -3 : 0; break;
                    case 5: x = 5; y = -2.8 + spread * 5.6; break;
                    case 6: x = 1.4 + spread * 7.2; y = -2.7 + spread * 5.4; break;
                    case 7: x = i == 0 ? 2 : i == 1 ? 8.5 : 5.8; y = i == 0 ? 0 : i == 1 ? 2.8 : -2.4; break;
                }
                if (mirrorX) x = 10 - x;
                if (mirrorY) y = -y;
                if (Math.Abs(x - 4) < 1 && Math.Abs(y + 2) < 1) y = Math.Abs(y) + 0.3;
                x += (random.Next(51) - 25) / 100.0; y += (random.Next(31) - 15) / 100.0;
                int health = 22 + (depth % RoomsPerFloor) * 4 + (depth / RoomsPerFloor) * 20 + random.Next(9) + (kind == RunNodeKind.Elite ? 16 : 0) + (kind == RunNodeKind.Boss && i == 1 ? 34 : 0);
                int attack = 3 + (depth % RoomsPerFloor) / 3 + (depth / RoomsPerFloor) * 2 + (kind == RunNodeKind.Elite ? 2 : 0);
                int behavior = (template + i + random.Next(5)) % 5;
                var ai = behavior == 0 ? EnemyBehaviorDefinition.SteadyAttack() : behavior == 1 ? EnemyBehaviorDefinition.ChargeBurst()
                    : behavior == 3 ? EnemyBehaviorDefinition.EscalatingAttack()
                    : behavior == 4 ? EnemyBehaviorDefinition.Skirmisher(Math.Max(1, Math.Min(9, 10 - x)), -y * 0.8)
                    : EnemyBehaviorDefinition.AlternatingPosition(Math.Max(1, Math.Min(9, 10 - x)), -y * 0.8);
                enemies[i] = new EnemyDefinition("enemy." + i, EnemyNames[(template + i) % EnemyNames.Length], x, y, health, attack, ai);
            }
            BattleTerrainDefinition[] terrain = CreateTerrain(kind, random, enemies);
            return new BattleDefinition(playerMaxHealth, c.MaxEnergy, enemies, c.Archetype, fragments: true, terrain: terrain);
        }

        private static BattleTerrainDefinition[] CreateTerrain(RunNodeKind encounterKind, IRandomSource random, EnemyDefinition[] enemies)
        {
            int wanted = encounterKind == RunNodeKind.Boss ? 3 : 2;
            var result = new List<BattleTerrainDefinition>(wanted);
            int start = random.Next(TerrainX.Length);
            for (int scan = 0; scan < TerrainX.Length && result.Count < wanted; scan++)
            {
                int candidate = (start + scan * 5) % TerrainX.Length;
                double x = TerrainX[candidate], y = TerrainY[candidate];
                // One terrain obstacle at most; the rest invite a choice about where and when to draw.
                BattleTerrainKind kind = result.Count == 0 ? (start % 2 == 0 ? BattleTerrainKind.Pulse : BattleTerrainKind.Obstacle)
                    : result.Count == 1 ? (start % 3 == 0 ? BattleTerrainKind.Aegis : start % 3 == 1 ? BattleTerrainKind.Capacitor : BattleTerrainKind.Prism)
                    : start % 3 == 1 ? BattleTerrainKind.Aegis : BattleTerrainKind.Capacitor;
                double radius = kind == BattleTerrainKind.Pulse ? 1.05 : kind == BattleTerrainKind.Obstacle ? 0.68 : 0.48;
                x = Math.Max(radius, Math.Min(10 - radius, x));
                y = Math.Max(-4 + radius, Math.Min(4 - radius, y));
                if (!CanPlaceTerrain(x, y, radius, enemies, result)) continue;
                result.Add(new BattleTerrainDefinition("terrain." + result.Count, kind, x, y, radius));
            }
            return result.ToArray();
        }

        private static bool CanPlaceTerrain(double x, double y, double radius, EnemyDefinition[] enemies,
            List<BattleTerrainDefinition> placed)
        {
            double dx = x - 4.0, dy = y + 2.0, playerReach = radius + TacticalCombatState.PlayerRadius + 0.35;
            if (dx * dx + dy * dy < playerReach * playerReach) return false;
            for (int i = 0; i < enemies.Length; i++)
            {
                dx = x - enemies[i].X; dy = y - enemies[i].Y;
                double reach = radius + BattleSession.EnemyHitRadius + 0.25;
                if (dx * dx + dy * dy < reach * reach) return false;
            }
            for (int i = 0; i < placed.Count; i++)
            {
                dx = x - placed[i].X; dy = y - placed[i].Y;
                double reach = radius + placed[i].Radius + 0.55;
                if (dx * dx + dy * dy < reach * reach) return false;
            }
            return true;
        }
    }
}

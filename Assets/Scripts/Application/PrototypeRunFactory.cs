using Graphaclysm.Core.Cards;
using Graphaclysm.Core.Combat;
using Graphaclysm.Core.Characters;
using Graphaclysm.Core.Relics;
using Graphaclysm.Core.Runs;

namespace Graphaclysm.Application
{
    public static class PrototypeRunFactory
    {
        public static RunGameSession Create(uint seed)
        {
            return Create(seed, PrototypeCharacterCatalog.All[0]);
        }

        public static RunGameSession Create(uint seed, CharacterDefinition character)
        {
            if (character == null) throw new System.ArgumentNullException(nameof(character));
            var run = new RunGameSession(DungeonGenerator.Generate(seed, character), character.CreateStartingDeckCopy(),
                FragmentCardCatalog.All, character.HandSize, seed, FragmentRelicCatalog.All);
            run.EnableJournal(character.Id);
            return run;
        }

        public static RunGameSession CreateLegacy(uint seed, CharacterDefinition character)
        {
            if (character == null)
            {
                throw new System.ArgumentNullException(nameof(character));
            }

            RunMapNodeDefinition[] nodes =
            {
                new RunMapNodeDefinition(
                    "entry.lower",
                    "낮은 축",
                    0,
                    0,
                    RunNodeKind.Battle,
                    CreateLowerEntryBattle(character),
                    new[] { 2, 3 }),
                new RunMapNodeDefinition(
                    "entry.upper",
                    "높은 축",
                    0,
                    2,
                    RunNodeKind.Battle,
                    CreateUpperEntryBattle(character),
                    new[] { 2, 3 }),
                new RunMapNodeDefinition(
                    "middle.crossing",
                    "교차 회랑",
                    1,
                    0,
                    RunNodeKind.Battle,
                    CreateCrossingBattle(character),
                    new[] { 4 }),
                new RunMapNodeDefinition(
                    "middle.distortion",
                    "왜곡 밀집지",
                    1,
                    2,
                    RunNodeKind.Elite,
                    CreateDistortionBattle(character),
                    new[] { 4 }),
                new RunMapNodeDefinition(
                    "boss.origin",
                    "원점의 문지기",
                    2,
                    1,
                    RunNodeKind.Boss,
                    CreateBossBattle(character),
                    System.Array.Empty<int>())
            };

            return new RunGameSession(
                new RunMapDefinition(nodes),
                character.CreateStartingDeckCopy(),
                SkillCardCatalog.All,
                character.HandSize,
                seed,
                PrototypeRelicCatalog.All);
        }

        private static BattleDefinition CreateLowerEntryBattle(CharacterDefinition character)
        {
            return new BattleDefinition(
                character.MaxHealth,
                character.MaxEnergy,
                new[]
                {
                    new EnemyDefinition("lower_axis_eater", "축 포식자", 2.0, -1.0, 18, 2),
                    new EnemyDefinition(
                        "lower_blank", "공백체", 7.0, 1.5, 18, 2,
                        EnemyBehaviorDefinition.AlternatingPosition(7.0, -1.5))
                }, character.Archetype);
        }

        private static BattleDefinition CreateUpperEntryBattle(CharacterDefinition character)
        {
            return new BattleDefinition(
                character.MaxHealth,
                character.MaxEnergy,
                new[]
                {
                    new EnemyDefinition(
                        "upper_constant_thief", "상수 도둑", 3.0, 2.0, 20, 2,
                        EnemyBehaviorDefinition.ChargeBurst()),
                    new EnemyDefinition("upper_blank", "공백체", 8.0, -1.0, 16, 2)
                }, character.Archetype);
        }

        private static BattleDefinition CreateCrossingBattle(CharacterDefinition character)
        {
            return new BattleDefinition(
                character.MaxHealth,
                character.MaxEnergy,
                new[]
                {
                    new EnemyDefinition(
                        "crossing_left", "분기 잔영", 2.0, 1.0, 16, 2,
                        EnemyBehaviorDefinition.AlternatingPosition(3.5, -1.0)),
                    new EnemyDefinition(
                        "crossing_center", "교점 포식자", 5.0, -2.0, 24, 3,
                        EnemyBehaviorDefinition.ChargeBurst()),
                    new EnemyDefinition(
                        "crossing_right", "분기 잔영", 8.0, 1.0, 16, 2,
                        EnemyBehaviorDefinition.AlternatingPosition(6.5, -1.0))
                }, character.Archetype);
        }

        private static BattleDefinition CreateDistortionBattle(CharacterDefinition character)
        {
            return new BattleDefinition(
                character.MaxHealth,
                character.MaxEnergy,
                new[]
                {
                    new EnemyDefinition(
                        "distortion_high", "왜곡핵", 3.0, 2.5, 30, 4,
                        EnemyBehaviorDefinition.ChargeBurst()),
                    new EnemyDefinition(
                        "distortion_low", "역위상체", 7.0, -2.5, 30, 4,
                        EnemyBehaviorDefinition.AlternatingPosition(4.0, 1.5))
                }, character.Archetype);
        }

        private static BattleDefinition CreateBossBattle(CharacterDefinition character)
        {
            return new BattleDefinition(
                character.MaxHealth,
                character.MaxEnergy,
                new[]
                {
                    new EnemyDefinition(
                        "origin_left", "원점의 좌익", 2.0, 0.0, 22, 3,
                        EnemyBehaviorDefinition.AlternatingPosition(2.0, 2.0)),
                    new EnemyDefinition(
                        "origin_guardian", "원점의 문지기", 5.0, 0.0, 42, 5,
                        EnemyBehaviorDefinition.ChargeBurst()),
                    new EnemyDefinition(
                        "origin_right", "원점의 우익", 8.0, 0.0, 22, 3,
                        EnemyBehaviorDefinition.AlternatingPosition(8.0, -2.0))
                }, character.Archetype);
        }
    }
}

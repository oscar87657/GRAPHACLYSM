namespace Graphaclysm.Core.Combat
{
    public static class PrototypeBattleFactory
    {
        public static BattleDefinition Create()
        {
            return new BattleDefinition(
                40,
                3,
                new[]
                {
                    new EnemyDefinition("axis_eater", "축 포식자", 2.0, 1.0, 18, 2),
                    new EnemyDefinition("blank", "공백체", 5.0, -2.0, 24, 3),
                    new EnemyDefinition("constant_thief", "상수 도둑", 8.0, 1.0, 20, 2)
                });
        }
    }
}

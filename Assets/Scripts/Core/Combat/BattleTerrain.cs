using System;

namespace Graphaclysm.Core.Combat
{
    public enum BattleTerrainKind
    {
        Obstacle,
        Prism
    }

    /// <summary>Immutable terrain placed in the shared equation field.</summary>
    public sealed class BattleTerrainDefinition
    {
        public BattleTerrainDefinition(string id, BattleTerrainKind kind, double x, double y, double radius)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Terrain requires an id.", nameof(id));
            if (kind < BattleTerrainKind.Obstacle || kind > BattleTerrainKind.Prism)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y)
                || double.IsNaN(radius) || double.IsInfinity(radius)
                || radius < 0.2 || radius > 1.25
                || x - radius < 0 || x + radius > 10 || y - radius < -4 || y + radius > 4)
                throw new ArgumentOutOfRangeException(nameof(radius), "Terrain must fit inside the combat field.");

            Id = id;
            Kind = kind;
            X = x;
            Y = y;
            Radius = radius;
        }

        public string Id { get; }
        public BattleTerrainKind Kind { get; }
        public double X { get; }
        public double Y { get; }
        public double Radius { get; }
        public bool BlocksMovement => Kind == BattleTerrainKind.Obstacle;
    }
}

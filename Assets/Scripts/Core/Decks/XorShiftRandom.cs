using System;

namespace Graphaclysm.Core.Decks
{
    public sealed class XorShiftRandom : IRandomSource
    {
        private const uint NonZeroFallbackSeed = 0x6D2B79F5u;
        private uint state;

        public XorShiftRandom(uint seed)
        {
            state = seed == 0u ? NonZeroFallbackSeed : seed;
        }

        public uint State
        {
            get { return state; }
        }

        public int Next(int exclusiveMaximum)
        {
            if (exclusiveMaximum <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
            }

            uint value = state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            state = value;
            return (int)(value % (uint)exclusiveMaximum);
        }
    }
}

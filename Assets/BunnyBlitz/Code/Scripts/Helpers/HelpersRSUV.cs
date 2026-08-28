using System;

namespace BunnyBlitz
{
    public static class HelpersRSUV
    {

        // Set a bit to 0 or 1 at a specific bitIndex in a uint
        public static uint SetBit(uint value, int bitIndex, bool b)
        {
            if (bitIndex < 0 || bitIndex > 31)
                throw new ArgumentOutOfRangeException(nameof(bitIndex), "bitIndex must be between 0 and 31");

            if (b)
                value |= (1u << bitIndex);
            else
                value &= ~(1u << bitIndex);

            return value;
        }

        // Encode "value" into X bitCount starting at bitOffset
        public static uint EncodeDataIntoBits(uint flags, int value, int bitOffset, int bitCount)
        {
            // Extract and set individual bits into result
            for (int i = 0; i < bitCount; i++)
            {
                bool bit = ((value >> i) & 1) != 0;
                flags = SetBit(flags, bitOffset + i, bit);
            }

            return flags;
        }

    }
}
using System;

namespace GGJ2026.Utils
{
    public static class BitUtil
    {
        public static float HalfToFloat(ushort half)
        {
            var sign = (uint)(half & 0x8000) << 16;
            var exp = (uint)(half & 0x7C00) >> 10;
            var mant = (uint)(half & 0x03FF);
            uint f;
            if (exp == 0)
            {
                if (mant == 0)
                {
                    f = sign;
                }
                else
                {
                    // Subnormal
                    exp = 127 - 15 + 1;
                    while ((mant & 0x0400) == 0)
                    {
                        mant <<= 1;
                        exp--;
                    }
                    mant &= 0x03FF;
                    f = sign | (exp << 23) | (mant << 13);
                }
            }
            else if (exp == 31)
            {
                // Inf/NaN
                f = sign | 0x7F800000 | (mant << 13);
            }
            else
            {
                exp = exp + (127 - 15);
                f = sign | (exp << 23) | (mant << 13);
            }
            return BitConverter.Int32BitsToSingle((int)f);
        }
    }
}
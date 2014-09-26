using System;

namespace Cross.Drawing
{
    public static class Converter
    {
        /// <summary>
        /// Reverse byte order of an int. (Useful for converting ARGB to/from BGRA
        /// </summary>
        public static int Reverse(int i)
        {
            return (int)Reverse((uint)i);
        }

        public static short Reverse(short i)
        {
            return (short)Reverse((UInt16)i);
        }

        public static long Reverse(long i)
        {
            return (long)Reverse((UInt64)i);
        }

        public static UInt16 Reverse(UInt16 i)
        {
            return (UInt16)((i & 0xFFU) << 8 | (i & 0xFF00U) >> 8);
        }

        /// <summary>
        /// Example of reverse for uint.
        /// </summary>
        public static UInt32 Reverse(UInt32 i)
        {
            return (i & 0x000000FFU) << 24 | (i & 0x0000FF00U) << 8 | (i & 0x00FF0000U) >> 8 | (i & 0xFF000000U) >> 24;
        }

        public static UInt64 Reverse(UInt64 i)
        {
            return (i & 0x00000000000000FFUL) << 56 | (i & 0x000000000000FF00UL) << 40 |
                   (i & 0x0000000000FF0000UL) << 24 | (i & 0x00000000FF000000UL) << 8 |
                   (i & 0x000000FF00000000UL) >> 8 | (i & 0x0000FF0000000000UL) >> 24 |
                   (i & 0x00FF000000000000UL) >> 40 | (i & 0xFF00000000000000UL) >> 56;
        }

        public static byte[] ToByteArray(int[] p)
        {
            int len = p.Length << 2;
            byte[] result = new byte[len];
            Buffer.BlockCopy(p, 0, result, 0, len);
            return result;
        }
    }
}

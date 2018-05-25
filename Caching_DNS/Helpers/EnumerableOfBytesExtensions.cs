using System;
using System.Collections.Generic;

namespace Caching_DNS.Helpers
{
    public static class EnumerableOfBytesExtensions
    {
        public static byte[] SwapEndianness(this byte[] data, int offset = 0)
        {
            var tmp = data[offset];
            data[offset] = data[offset + 3];
            data[offset + 3] = tmp;

            tmp = data[offset + 1];
            data[offset + 1] = data[offset + 2];
            data[offset + 2] = tmp;

            return data;
        }

        public static void CopyTo(this byte[] data, List<byte> list, int index)
        {
            for (var i = 0; i < data.Length; i++)
            {
                while (i + index >= list.Count)
                    list.Add(0);
                list[index + i] = data[i];
            }
        }

        public static ushort GetUInt16(this byte[] data, int offset)
        {
            return BitConverter.ToUInt16(data, offset).SwapEndianness();
        }

        public static void AddByte(this List<byte> data, int value, int offset)
        {
            ((byte) value).CopyTo(data, offset);
        }
    }
}
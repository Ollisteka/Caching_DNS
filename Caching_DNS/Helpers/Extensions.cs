using System;
using System.ComponentModel;

namespace Caching_DNS.Helpers
{
    public static class Extensions
    {
        public static byte[] SwapEndianness(byte[] data, int offset = 0)
        {
            var tmp = data[offset];
            data[offset] = data[offset + 3];
            data[offset + 3] = tmp;

            tmp = data[offset + 1];
            data[offset + 1] = data[offset + 2];
            data[offset + 2] = tmp;

            return data;
        }
        public static ushort SwapEndianness(this ushort val)
        {
            ushort value = (ushort)((val << 8) | (val >> 8));
            return value;
        }

        public static uint SwapEndianness(this uint val)
        {
            uint value = (val << 24) | ((val << 8) & 0x00ff0000) | ((val >> 8) & 0x0000ff00) | (val >> 24);
            return value;
        }
        public static string GetCustomDescription(object objEnum)
        {
            var fi = objEnum.GetType().GetField(objEnum.ToString());
            var attributes = (DescriptionAttribute[])fi?.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return (attributes?.Length > 0) ? attributes[0].Description : objEnum.ToString();
        }

        public static string Description(this Enum value)
        {
            return GetCustomDescription(value);
        }
    }
}
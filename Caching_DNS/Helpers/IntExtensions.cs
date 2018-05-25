using System;

namespace Caching_DNS.Helpers
{
    public static class IntExtensions
    {
        public static ushort SwapEndianness(this ushort val)
        {
            var value = (ushort) ((val << 8) | (val >> 8));
            return value;
        }

        public static uint SwapEndianness(this uint val)
        {
            var value = (val << 24) | ((val << 8) & 0x00ff0000) | ((val >> 8) & 0x0000ff00) | (val >> 24);
            return value;
        }

        public static byte[] GetBytes(this ushort val)
        {
            return BitConverter.GetBytes(val);
        }

        public static byte[] GetBytes(this uint val)
        {
            return BitConverter.GetBytes(val);
        }

        public static byte[] GetSwappedBytes(this ushort val)
        {
            return val.SwapEndianness().GetBytes();
        }

        public static byte[] GetSwappedBytes(this uint val)
        {
            return val.SwapEndianness().GetBytes();
        }
    }
}
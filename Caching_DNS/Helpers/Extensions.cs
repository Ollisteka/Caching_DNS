using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Caching_DNS.Helpers
{
    public static class Extensions
    {
        public static void CopyTo(this byte b, List<byte> list, int index)
        {
            while (index >= list.Count)
                list.Add(0);
            list[index] = b;
        }


        public static string GetCustomDescription(object objEnum)
        {
            var fi = objEnum.GetType().GetField(objEnum.ToString());
            var attributes = (DescriptionAttribute[]) fi?.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes?.Length > 0 ? attributes[0].Description : objEnum.ToString();
        }

        public static string Description(this Enum value)
        {
            return GetCustomDescription(value);
        }

        public static string ExtractDnsString(this byte[] data, ref int offset)
        {
            var result = new StringBuilder();
            var compressionOffset = -1;
            while (true)
            {
                var pieceLength = data[offset];

                if ((pieceLength & 0b1100_0000) == 0xc0)
                {
                    var firstPart = pieceLength & 0b0011_1111;
                    offset++;
                    if (compressionOffset == -1)
                        compressionOffset = offset;

                    offset = (firstPart << 8) | data[offset];
                    pieceLength = data[offset];
                }
                else if (pieceLength == 0)
                {
                    if (compressionOffset != -1)
                        offset = compressionOffset;

                    offset++;
                    break;
                }

                offset++;
                result.Append($"{Encoding.UTF8.GetString(data, offset, pieceLength)}.");
                offset += pieceLength;
            }

            return result.ToString().Trim('.');
        }
    }
}
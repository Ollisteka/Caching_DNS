using System;
using System.Net;

namespace Caching_DNS.DnsQueries
{
    [Serializable]
    public class AddressData : IData
    {
        public IPAddress IpAddress;

        public AddressData(IPAddress ipAddress)
        {
            IpAddress = ipAddress;
        }

        public AddressData(byte[] data, ref int offset)
        {
            var addressBytes = BitConverter.ToUInt32(data, offset);
            var address = new IPAddress(addressBytes);
            offset += 4;
            IpAddress = address;
        }

        public override string ToString()
        {
            return IpAddress.ToString();
        }
    }
}
using System;
using Caching_DNS.Helpers;

namespace Caching_DNS.DnsQueries
{
    [Serializable]
    public class ServerNameData : IData
    {
        public string NameServer;

        public ServerNameData(string nameServer)
        {
            NameServer = nameServer;
        }

        public ServerNameData(byte[] data, ref int offset)
        {
            var name = data.ExtractDnsString(ref offset);
            NameServer = name;
        }

        public override string ToString()
        {
            return NameServer;
        }
    }
}
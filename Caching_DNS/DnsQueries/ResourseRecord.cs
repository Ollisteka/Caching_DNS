using System;

namespace Caching_DNS.DnsQueries
{
    public class ResourseRecord
    {
        public string Name;
        public ResourceType Type;
        public ResourceClass Class;
        public uint Ttl;
        public readonly DateTime AbsoluteExpitationDate;
        public ushort DataLength;
    }
}
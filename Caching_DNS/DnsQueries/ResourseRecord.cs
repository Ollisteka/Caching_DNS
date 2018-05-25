using System;
using Caching_DNS.Enums;

namespace Caching_DNS.DnsQueries
{
    [Serializable]
    public class ResourseRecord
    {
        public readonly DateTime AbsoluteExpitationDate;
        public readonly ResourceClass Class;
        public readonly IData Data;
        public readonly ushort DataLength;
        public readonly string Name;
        public readonly uint Ttl;
        public readonly ResourceType Type;

        public ResourseRecord(string name, ResourceType type, ResourceClass resClass, uint ttl, ushort dataLength,
            IData data)
        {
            Name = name;
            Type = type;
            Class = resClass;
            Ttl = ttl;
            DataLength = dataLength;
            var now = DateTime.Now;
            AbsoluteExpitationDate = now.AddSeconds(ttl);
            Data = data;
        }

        public override string ToString()
        {
            return $"{Name}  {Type}  {Class}  Exp: {AbsoluteExpitationDate} Data: {Data}";
        }
    }
}
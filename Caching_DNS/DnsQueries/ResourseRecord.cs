using System;
using System.Net;
using Caching_DNS.DnsStructure;
using Caching_DNS.Helpers;

namespace Caching_DNS.DnsQueries
{
    [Serializable]
    public class ResourseRecord
    {
        public string Name;
        public ResourceType Type;
        public ResourceClass Class;
        public uint Ttl;
        public readonly DateTime AbsoluteExpitationDate;
        public ushort DataLength;
        public ResourseData Data;

        public ResourseRecord(string name, ResourceType type, ResourceClass resClass, uint ttl, ushort dataLength, ResourseData data)
        {
            Name = name;
            Type = type;
            Class = resClass;
            Ttl = ttl;
            DataLength = dataLength;
            var now = DateTime.Now;
            AbsoluteExpitationDate =now.AddSeconds(ttl);
            Data = data;
        }

        public override string ToString()
        {
            return $"{Name}  {Type}  {Class}  Exp: {AbsoluteExpitationDate} Data: {Data}";
        }
    }
    [Serializable]
    public class ResourseData
    {
        public IPAddress IpAddress;
        public string NameServer;

        public ResourseData(IPAddress ipAddress=null, string nameServer=null)
        {
            IpAddress = ipAddress;
            NameServer = nameServer;
        }

        public static ResourseData ParseAddressRecord(byte[] data, ref int offset)
        {
            var addressBytes = BitConverter.ToUInt32(data, offset);
            var address = new IPAddress(addressBytes);
            offset += 4;
            return new ResourseData(address);
        }

        public static ResourseData ParseNameServer(byte[] data, ref int offset)
        {
            var name = data.ExtractDnsString(ref offset);
            return new ResourseData(nameServer:name);
        }

        public override string ToString()
        {
            return $"{IpAddress} {NameServer}";
        }
    }
}
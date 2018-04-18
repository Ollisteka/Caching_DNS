using System;
using System.Net;
using Caching_DNS.DnsStructure;

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
        public ResourseData Data;

        public ResourseRecord(string name, ResourceType type, ResourceClass resClass, uint ttl, ushort dataLength, ResourseData data)
        {
            Name = name;
            Type = type;
            Class = resClass;
            Ttl = ttl;
            DataLength = dataLength;
            AbsoluteExpitationDate = DateTime.Now.AddSeconds(ttl);
            Data = data;
        }

        public override string ToString()
        {
            return $"{Name}  {Type}  {Class}  Exp: {AbsoluteExpitationDate} Data: {Data}";
        }
    }

    public class ResourseData
    {
        public IPAddress IpAddress;
        public string NameServer;

        public ResourseData(IPAddress ipAddress=null, string nameServer=null)
        {
            IpAddress = ipAddress;
            NameServer = nameServer;
        }

        public static ResourseData ParseAddressRecord(byte[] data, int offset)
        {
            uint addressBytes = BitConverter.ToUInt32(data, offset);
            var address = new IPAddress(addressBytes);
            return new ResourseData(address);
        }

        public static ResourseData ParseNameServer(byte[] data, int offset)
        {
            var name = DnsPacket.ExtractString(data, ref offset);
            return new ResourseData(nameServer:name);
        }

        public override string ToString()
        {
            return $"{IpAddress} {NameServer}";
        }
    }
}
namespace Caching_DNS.DnsStructure
{
    public class DnsPacketParser
    {
        public DnsPacket Parse(byte[] data)
        {
            return new DnsPacket(data);
        }
    }
}
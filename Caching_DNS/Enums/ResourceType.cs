using System;
using System.ComponentModel;

namespace Caching_DNS.Enums
{
    [Serializable]
    public enum ResourceType : ushort
    {
        [Description("A")] A = 0x0001, //name->IP
        [Description("NS")] NS = 0x0002, //authoritive name server
        [Description("PTR")] CNAME = 0x0005, //canonical name
        [Description("SOA")] SOA = 0x0006, //start of authority
        [Description("PTR")] PTR = 0x000c, //Ip->name
        [Description("MX")] MX = 0x000f, //mail exchanger
        [Description("AAAA")] AAAA = 0x001c //IPv6
    }
}
using System;
using System.ComponentModel;

namespace Caching_DNS.DnsQueries
{
    [Serializable]
    public enum ResourceClass : ushort
    {
        [Description("None")]
        None = 0,
        [Description("IN")]
        IN = 1,
    }
}
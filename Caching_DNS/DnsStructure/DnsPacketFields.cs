namespace Caching_DNS.DnsStructure
{
    public static class DnsPacketFields
    {
        public static int TransactionId = 0;
        public static int Flags = 2;
        public static int Questions = 4;
        public static int Answers = 6;
        public static int Authority = 8;
        public static int Additional = 10;
        public static int Queries = 12;
    }
}
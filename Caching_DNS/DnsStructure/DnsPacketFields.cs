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

        public static uint AuthorativeAnswerMask = 0b0000_0100_0000_0000;
        public static uint TruncatedMask = 0b0000_0010_0000_0000;
        public static uint RecursionDesiredMask = 0b0000_0001_0000_0000;
        public static uint RecursionAvailableMask = 0b0000_0000_1000_0000;
    }
}
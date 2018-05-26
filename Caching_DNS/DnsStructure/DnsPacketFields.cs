namespace Caching_DNS.DnsStructure
{
    public static class DnsPacketFields
    {
        public const int TransactionId = 0;
        public const int Flags = 2;
        public const int Questions = 4;
        public const int Answers = 6;
        public const int Authority = 8;
        public const int Additional = 10;
        public const int Queries = 12;

        public const uint AuthorativeAnswerMask = 0b0000_0100_0000_0000;
        public const uint TruncatedMask = 0b0000_0010_0000_0000;
        public const uint RecursionDesiredMask = 0b0000_0001_0000_0000;
        public const uint RecursionAvailableMask = 0b0000_0000_1000_0000;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Caching_DNS.DnsQueries;
using Caching_DNS.Helpers;

namespace Caching_DNS.DnsStructure
{
    public class DnsPacket
    {
        public readonly byte[] Data;
        public uint AdditionalNumber;
        public uint AnswersNumber;
        public uint AuthorityNumber;
        public uint Flags;
        public uint QuestionNumber;

        public List<Question> Questions = new List<Question>();
        private int totalOffset;
        public uint TransactionId;

        public DnsPacket(byte[] data)
        {
            Data = data;
            ParseFields();
        }

        public bool IsQuery => (Flags & 0b10000000) == 0;
        public bool IsResponse => !IsQuery;
        public bool IsAuthoritive => (Flags & 0b00001000) == 1;
        public bool RecursionDesired => (Flags & 0b00000001) == 1;

        private void ParseFields()
        {
            QuestionNumber = BitConverter.ToUInt16(Data, DnsPacketFields.Questions).SwapEndianness();
            Flags = BitConverter.ToUInt16(Data, DnsPacketFields.Flags).SwapEndianness();
            AnswersNumber = BitConverter.ToUInt16(Data, DnsPacketFields.Answers).SwapEndianness();
            AuthorityNumber = BitConverter.ToUInt16(Data, DnsPacketFields.Authority).SwapEndianness();
            AdditionalNumber = BitConverter.ToUInt16(Data, DnsPacketFields.Additional).SwapEndianness();
            totalOffset = DnsPacketFields.Queries;
            if (QuestionNumber > 0)
                ParseQuestions();
        }

        private void ParseQuestions()
        {
            for (var i = 0; i < QuestionNumber; i++)
            {
                var question = new Question();
                question.Name = ExtractString(Data, ref totalOffset);
                question.Type = (ResourceType) BitConverter.ToUInt16(Data, totalOffset).SwapEndianness();
                totalOffset += 2;
                question.Class = (ResourceClass) BitConverter.ToUInt16(Data, totalOffset).SwapEndianness();
                totalOffset += 2;
                Questions.Add(question);
            }
        }

        private static string ExtractString(byte[] data, ref int offset)
        {
            var result = new StringBuilder();
            var counter = data[offset];
            while (counter != 0)
            {
                offset++;
                result.Append($"{Encoding.UTF8.GetString(data, offset, counter)}.");
                offset += counter;
                counter = data[offset];
            }

            offset++;
            return result.ToString().Trim('.');
        }
    }
}
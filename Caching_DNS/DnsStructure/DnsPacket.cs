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
        public List<ResourseRecord> Answers = new List<ResourseRecord>();
        public List<ResourseRecord> AuthoritiveServers = new List<ResourseRecord>();
        public List<int> TtlIndexes = new List<int>();
        private int totalOffset;
        public uint TransactionId;

        public DnsPacket(byte[] data)
        {
            Data = data;
            ParseFields();
        }

        public uint Opcode => (Flags & 0b0111_1000_0000_0000) >> 11;
        public uint ReplyCode => (Flags & 0b0000_0000_0000_1111);
        public bool NoErrorInReply => ReplyCode == 0;
        public bool IsQuery => (Flags & 0b1000_0000_0000_0000) == 0;
        public bool IsResponse => !IsQuery;
        public bool AuthorativeAnswer => (Flags & DnsPacketFields.AuthorativeAnswerMask) == DnsPacketFields.AuthorativeAnswerMask;
        public bool IsTruncted => (Flags & DnsPacketFields.TruncatedMask) == DnsPacketFields.TruncatedMask;
        public bool RecursionDesired => (Flags & DnsPacketFields.RecursionDesiredMask) == DnsPacketFields.RecursionDesiredMask;
        public bool RecursionAvailable => (Flags & DnsPacketFields.RecursionAvailableMask) == DnsPacketFields.RecursionAvailableMask;

        public override string ToString()
        {
            var result = new StringBuilder("---\n");

            if (QuestionNumber != 0)
                result.AppendLine($"Questions:\n{string.Join("\n", Questions)}\n");

            if (AnswersNumber != 0)
                result.AppendLine($"Answers:\n{string.Join("\n", Answers)}\n");

            if (AuthorityNumber != 0)
                result.AppendLine($"Authorative nameservers:\n{string.Join("\n", AuthoritiveServers)}\n");

            result.AppendLine("---");
            return result.ToString();
        }

        private void ParseFields()
        {
            TransactionId = BitConverter.ToUInt16(Data, DnsPacketFields.TransactionId).SwapEndianness();
            QuestionNumber = BitConverter.ToUInt16(Data, DnsPacketFields.Questions).SwapEndianness();
            Flags = BitConverter.ToUInt16(Data, DnsPacketFields.Flags).SwapEndianness();
            AnswersNumber = BitConverter.ToUInt16(Data, DnsPacketFields.Answers).SwapEndianness();
            AuthorityNumber = BitConverter.ToUInt16(Data, DnsPacketFields.Authority).SwapEndianness();
            AdditionalNumber = BitConverter.ToUInt16(Data, DnsPacketFields.Additional).SwapEndianness();
            totalOffset = DnsPacketFields.Queries;
            if (QuestionNumber > 0)
                ParseQuestions();
            if (AnswersNumber > 0)
                ParseAnswers(Answers, AnswersNumber);
            if (AuthorityNumber > 0)
                ParseAnswers(AuthoritiveServers, AuthorityNumber);
        }

        private void ParseAnswers(List<ResourseRecord> list, uint count)
        {
            for (int i = 0; i < count; i++)
            {
                var name = ExtractString(Data, ref totalOffset);
                var type = (ResourceType) BitConverter.ToUInt16(Data, totalOffset).SwapEndianness();
                totalOffset += 2;
                var resClass = (ResourceClass)BitConverter.ToUInt16(Data, totalOffset).SwapEndianness();
                totalOffset += 2;
                var ttl = BitConverter.ToUInt32(Data, totalOffset).SwapEndianness();
                TtlIndexes.Add(totalOffset);
                totalOffset += 4;
                var dataLength = BitConverter.ToUInt16(Data, totalOffset);
                totalOffset += 2;
                ResourseData data = null;
                switch (type)
                {
                    case ResourceType.A:
                        data = ResourseData.ParseAddressRecord(Data, ref totalOffset);
                        break;
                    case ResourceType.NS:
                        data = ResourseData.ParseNameServer(Data, ref totalOffset);
                        break;
                    default:
                        Console.Error.WriteLine($"Message with the type code {Convert.ToString((int)type, 16)} is not currently supported!");
                        data = ResourseData.ParseAddressRecord(Data, ref totalOffset);
                        break;
                }
                list.Add(new ResourseRecord(name, type, resClass, ttl, dataLength, data));
            }
            
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

        public static string ExtractString(byte[] data, ref int offset)
        {
            var result = new StringBuilder();
            var compressionOffset = -1;
            while (true)
            {
                var nextLength = data[offset];

                if (nextLength == 0xc0)
                {
                    var firstPart = nextLength & 0b0011_1111;
                    offset++;
                    if (compressionOffset == -1) 
                        compressionOffset = offset;
                    
                    offset = (firstPart << 8) | data[offset];
                    nextLength = data[offset];
                }
                else if (nextLength == 0)
                {
                    if (compressionOffset != -1)
                        offset = compressionOffset;

                    offset++;
                    break;
                }
                
                offset++;
                result.Append($"{Encoding.UTF8.GetString(data, offset, nextLength)}.");
                offset += nextLength;
                
            }
            
            return result.ToString().Trim('.');
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Caching_DNS.DnsQueries;
using Caching_DNS.Enums;
using Caching_DNS.Helpers;

namespace Caching_DNS.DnsStructure
{
    [Serializable]
    public class DnsPacket
    {
        public readonly byte[] Data;
        private readonly List<int> ttlIndexes = new List<int>();
        public uint AdditionalNumber;
        public List<ResourseRecord> Answers = new List<ResourseRecord>();
        public uint AnswersNumber;
        public List<ResourseRecord> AuthoritiveServers = new List<ResourseRecord>();
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

        public uint Opcode => (Flags & 0b0111_1000_0000_0000) >> 11;
        public uint ReplyCode => Flags & 0b0000_0000_0000_1111;
        public bool NoErrorInReply => ReplyCode == 0;
        public bool IsQuery => (Flags & 0b1000_0000_0000_0000) == 0;
        public bool IsResponse => !IsQuery;

        public bool AuthorativeAnswer =>
            (Flags & DnsPacketFields.AuthorativeAnswerMask) == DnsPacketFields.AuthorativeAnswerMask;

        public bool IsTruncted => (Flags & DnsPacketFields.TruncatedMask) == DnsPacketFields.TruncatedMask;

        public bool RecursionDesired =>
            (Flags & DnsPacketFields.RecursionDesiredMask) == DnsPacketFields.RecursionDesiredMask;

        public bool RecursionAvailable =>
            (Flags & DnsPacketFields.RecursionAvailableMask) == DnsPacketFields.RecursionAvailableMask;

        public override string ToString()
        {
            var result = new StringBuilder("---\n");

            result.AppendLine($"Id: {TransactionId}");

            if (QuestionNumber != 0)
                result.AppendLine($"Questions:\n{string.Join("\n", Questions)}\n");

            if (AnswersNumber != 0)
                result.AppendLine($"Answers:\n{string.Join("\n", Answers)}\n");

            if (AuthorityNumber != 0)
                result.AppendLine($"Authorative nameservers:\n{string.Join("\n", AuthoritiveServers)}\n");

            result.AppendLine("---");
            return result.ToString();
        }

        public bool IsOutdated()
        {
            var now = DateTime.Now;
            if (AnswersNumber == 0)
                return false;
            var exp = Answers[0].AbsoluteExpitationDate;
            return exp <= now;
        }

        public void UpdateTtl()
        {
            for (var i = 0; i < Answers.Count; i++)
            {
                var index = ttlIndexes[i];
                var answer = Answers[i];
                var oldExpDate = answer.AbsoluteExpitationDate;
                var now = DateTime.Now;
                var newTtl = (uint) oldExpDate.Subtract(now).TotalSeconds;
                var newTtlB = BitConverter.GetBytes(newTtl.SwapEndianness());
                for (var j = 0; j < newTtlB.Length; j++) Data[index + j] = newTtlB[j];
            }
        }

        public void UpdateTransactionId(uint newId)
        {
            TransactionId = newId;
            var newIdB = BitConverter.GetBytes(newId.SwapEndianness());
            for (var j = 2; j < newIdB.Length; j++)
                Data[DnsPacketFields.TransactionId + j - 2] = newIdB[j];
        }

        private static void EncodeString(Dictionary<string, int> map, string  str, List<byte> data, ref int offset)
        {
            if (!map.ContainsKey(str))
            {
                var parts = str.Split('.').Where(s => s != "").ToList();

                for (var i = 0; i < parts.Count; i++)
                {
                    var suffix = string.Join(".", parts.Skip(i));
                    if (suffix != "")
                    {
                        if (!map.ContainsKey(suffix))
                            map[suffix] = offset;
                        else
                        {
                            EncodeKnownString(suffix, data, ref offset, map);
                            return;
                        }
                    }

                    var part = parts[i];
                    EncodeSimpleString(part, data, ref offset);
                }
                ((byte)0).CopyTo(data, offset++);
            }
            else
            {
                EncodeKnownString(str, data, ref offset, map);
            }
        }

        private static void EncodeKnownString(string str, List<byte> data, ref int offset, Dictionary<string, int> map)
        {
            ((byte)192).CopyTo(data, offset++);
            ((byte)map[str]).CopyTo(data, offset++);
        }
        private static void EncodeSimpleString(string str, List<byte> data, ref int offset)
        {
            var decoded = Encoding.UTF8.GetBytes(str);
            var length = ((ushort)decoded.Length).SwapEndianness().GetBytes()[1];
            
            length.CopyTo(data, offset++);
            decoded.CopyTo(data, offset);
            offset += decoded.Length;
        }
        public static DnsPacket GenerateAnswer(ushort id, List<Question> questions,
            List<ResourseRecord> authoritiveServers)
        {
            var map = new Dictionary<string, int>();
            var data = new List<byte>();
            var idb = BitConverter.GetBytes(id.SwapEndianness());
            var flags = BitConverter.GetBytes(((ushort) (0b1000_0100_0000_0000)).SwapEndianness());
            var qNum = BitConverter.GetBytes(((ushort) questions.Count).SwapEndianness());
            var ansNum = BitConverter.GetBytes(((ushort) authoritiveServers.Count).SwapEndianness());
            idb.CopyTo(data, DnsPacketFields.TransactionId);
            flags.CopyTo(data, DnsPacketFields.Flags);
            qNum.CopyTo(data, DnsPacketFields.Questions);
            ansNum.CopyTo(data, DnsPacketFields.Answers);
            var offset = DnsPacketFields.Queries;
            foreach (var question in questions)
            {
                
                EncodeString(map, question.Name, data, ref offset);
                var type = ((ushort) question.Type).SwapEndianness().GetBytes();
                type.CopyTo(data, offset);
                offset += 2;
                var rClass = ((ushort) question.Class).SwapEndianness().GetBytes();
                rClass.CopyTo(data, offset);
                offset += 2;
            }

            foreach (var answer in authoritiveServers)
            {
                
                EncodeString(map, answer.Name, data, ref offset);
               
                var type = ((ushort)answer.Type).SwapEndianness().GetBytes();
                type.CopyTo(data, offset);
                offset += 2;
                var rClass = ((ushort)answer.Class).SwapEndianness().GetBytes();
                rClass.CopyTo(data, offset);
                offset += 2;
                var oldExpDate = answer.AbsoluteExpitationDate;
                var now = DateTime.Now;
                var newTtl = ((uint)oldExpDate.Subtract(now).TotalSeconds).SwapEndianness().GetBytes();
                newTtl.CopyTo(data, offset);
                offset += 4;
                var rdatal = answer.DataLength.GetBytes();
                rdatal.CopyTo(data, offset);
                offset += rdatal.Length;
                if (answer.Data is ServerNameData serverNData)
                    EncodeString(map, serverNData.NameServer, data, ref offset);
                    
                
            }

            return new DnsPacket(data.ToArray());
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
            for (var i = 0; i < count; i++)
            {
                var start = totalOffset;
                var name = Data.ExtractDnsString(ref totalOffset);
                var type = (ResourceType) BitConverter.ToUInt16(Data, totalOffset).SwapEndianness();
                totalOffset += 2;
                var resClass = (ResourceClass) BitConverter.ToUInt16(Data, totalOffset).SwapEndianness();
                totalOffset += 2;
                var ttl = BitConverter.ToUInt32(Data, totalOffset).SwapEndianness();
                ttlIndexes.Add(totalOffset);
                totalOffset += 4;
                var dataLength = BitConverter.ToUInt16(Data, totalOffset);
                totalOffset += 2;
                IData data;
                switch (type)
                {
                    case ResourceType.A:
                        data = new AddressData(Data, ref totalOffset);
                        break;
                    case ResourceType.NS:
                        data = new ServerNameData(Data, ref totalOffset);
                        break;
                    default:
                        Console.Error.WriteLine(
                            $"Message with the type code {Convert.ToString((int) type, 16)} is not currently supported!");
                        data = new AddressData(Data, ref totalOffset);
                        break;
                }

                var finish = totalOffset;
                list.Add(new ResourseRecord(name, type, resClass, ttl, dataLength, data,
                    Data.Skip(start).Take(finish - start).ToArray()));
            }
        }

        private void ParseQuestions()
        {
            for (var i = 0; i < QuestionNumber; i++)
            {
                var name = Data.ExtractDnsString(ref totalOffset);
                var type = (ResourceType) BitConverter.ToUInt16(Data, totalOffset).SwapEndianness();
                totalOffset += 2;
                var rClass = (ResourceClass) BitConverter.ToUInt16(Data, totalOffset).SwapEndianness();
                totalOffset += 2;
                Questions.Add(new Question(rClass, name, type));
            }
        }
    }
}
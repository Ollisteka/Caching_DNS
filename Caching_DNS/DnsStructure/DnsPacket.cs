using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Caching_DNS.DnsQueries;
using Caching_DNS.Enums;
using Caching_DNS.Helpers;
#pragma warning disable 618

namespace Caching_DNS.DnsStructure
{
    [Serializable]
    public class DnsPacket
    {
        public readonly byte[] Data;
        private readonly List<int> ttlIndexes = new List<int>();

        public List<ResourseRecord> Answers = new List<ResourseRecord>();
        public List<ResourseRecord> AuthoritiveServers = new List<ResourseRecord>();
        public List<ResourseRecord> AdditionalRecords = new List<ResourseRecord>();
        public List<Question> Questions = new List<Question>();

        private int totalOffset;

        public DnsPacket(byte[] data)
        {
            Data = data;
            ParseFields();
        }

        public uint Flags => Data.GetUInt16(DnsPacketFields.Flags);

        public ushort TransactionId => Data.GetUInt16(DnsPacketFields.TransactionId);
        public ushort QuestionNumber => Data.GetUInt16(DnsPacketFields.Questions);
        public ushort AnswersNumber => Data.GetUInt16(DnsPacketFields.Answers);
        public ushort AuthorityNumber => Data.GetUInt16(DnsPacketFields.Authority);
        public ushort AdditionalNumber => Data.GetUInt16(DnsPacketFields.Additional);

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

            if (AdditionalNumber != 0)
                result.AppendLine($"Additional records:\n{string.Join("\n", AdditionalRecords)}\n");

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
                var updatedTtl = GetUpdatedTll(answer.AbsoluteExpitationDate);
                for (var j = 0; j < updatedTtl.Length; j++) Data[index + j] = updatedTtl[j];
            }
        }

        public void UpdateTransactionId(ushort newId)
        {
            var newIdB = newId.GetSwappedBytes();
            newIdB.CopyTo(Data, DnsPacketFields.TransactionId);
        }

        private static void EncodeString(Dictionary<string, int> map, string str, List<byte> data, ref int offset)
        {
            if (map.ContainsKey(str))
            {
                EncodeKnownString(map, data, str, ref offset);
                return;
            }

            var parts = str.Split('.').Where(s => s != "").ToList();
            for (var i = 0; i < parts.Count; i++)
            {
                var suffix = string.Join(".", parts.Skip(i));

                if (!map.ContainsKey(suffix))
                {
                    map[suffix] = offset;
                    var part = parts[i];
                    EncodePartOfString(data, part, ref offset);
                }
                else
                {
                    EncodeKnownString(map, data, suffix, ref offset);
                    return;
                }
            }

            data.AddByte(0, offset++);
        }

        private static void EncodeKnownString(Dictionary<string, int> map, List<byte> data, string str, ref int offset)
        {
            data.AddByte(192, offset++);
            data.AddByte(map[str], offset++);
        }

        private static void EncodePartOfString(List<byte> data, string str, ref int offset)
        {
            var decoded = Encoding.UTF8.GetBytes(str);
            var length = ((ushort) decoded.Length).SwapEndianness().GetBytes()[1];
            length.CopyTo(data, offset++);
            decoded.CopyTo(data, offset);
            offset += decoded.Length;
        }

        public static DnsPacket GenerateAnswer(ushort id, List<Question> questions,
            List<ResourseRecord> answers)
        {
            var map = new Dictionary<string, int>();
            var data = new List<byte>();
            var idb = id.GetSwappedBytes();
            var flags = ((ushort) 0b1000_0100_0000_0000).GetSwappedBytes();
            var qNum = ((ushort) questions.Count).GetSwappedBytes();
            var ansNum = ((ushort) answers.Count).GetSwappedBytes();
            idb.CopyTo(data, DnsPacketFields.TransactionId);
            flags.CopyTo(data, DnsPacketFields.Flags);
            qNum.CopyTo(data, DnsPacketFields.Questions);
            ansNum.CopyTo(data, DnsPacketFields.Answers);
            var offset = DnsPacketFields.Queries;
            foreach (var question in questions)
            {
                EncodeString(map, question.Name, data, ref offset);
                var type = ((ushort) question.Type).GetSwappedBytes();
                type.CopyTo(data, offset);
                offset += 2;
                var rClass = ((ushort) question.Class).GetSwappedBytes();
                rClass.CopyTo(data, offset);
                offset += 2;
            }

            foreach (var answer in answers)
            {
                EncodeString(map, answer.Name, data, ref offset);

                var type = ((ushort) answer.Type).SwapEndianness().GetBytes();
                type.CopyTo(data, offset);
                offset += 2;
                var rClass = ((ushort) answer.Class).SwapEndianness().GetBytes();
                rClass.CopyTo(data, offset);
                offset += 2;

                var newTtl = GetUpdatedTll(answer.AbsoluteExpitationDate);
                newTtl.CopyTo(data, offset);
                offset += 4;
                var rdatal = answer.DataLength.GetBytes();
                rdatal.CopyTo(data, offset);
                offset += rdatal.Length;
                switch (answer.Data)
                {
                    case ServerNameData serverNData:
                        EncodeString(map, serverNData.NameServer, data, ref offset);
                        break;
                    case AddressData addressData:
                        var ipBytes = BitConverter.GetBytes((uint) addressData.IpAddress.Address);
                        ipBytes.CopyTo(data, offset);
                        offset += 4;
                        break;
                }
            }

            return new DnsPacket(data.ToArray());
        }

        private static byte[] GetUpdatedTll(DateTime absoluteExpirationDate)
        {
            var now = DateTime.Now;
            return ((uint) absoluteExpirationDate.Subtract(now).TotalSeconds).GetSwappedBytes();
        }

        private void ParseFields()
        {
            totalOffset = DnsPacketFields.Queries;
            if (QuestionNumber > 0)
                ParseQuestions();
            if (AnswersNumber > 0)
                ParseAnswers(Answers, AnswersNumber);
            if (AuthorityNumber > 0)
                ParseAnswers(AuthoritiveServers, AuthorityNumber);
            if (AdditionalNumber > 0)
                ParseAnswers(AdditionalRecords, AdditionalNumber);
        }

        private void ParseAnswers(List<ResourseRecord> list, uint count)
        {
            for (var i = 0; i < count; i++)
            {
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

                list.Add(new ResourseRecord(name, type, resClass, ttl, dataLength, data));
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
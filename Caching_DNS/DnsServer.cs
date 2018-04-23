using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using Caching_DNS.DnsQueries;
using Caching_DNS.DnsStructure;
using Caching_DNS.Helpers;
using Caching_DNS.Network;

namespace Caching_DNS
{
    public class DnsServer
    {

        private readonly Dictionary<string, DnsPacket> CacheForTypeA;

        private readonly Dictionary<string, DnsPacket> CacheForTypeNs;

        private const string AFilename = "CacheA.dat";
        private const string NsFilename = "CacheNs.dat";

        private bool closed;
        private IPEndPoint remoteDns = new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53);
        private UdpListener udpListener;

        public DnsServer()
        {
            CacheForTypeA = DeserializeCache(AFilename);
            CacheForTypeNs = DeserializeCache(NsFilename);
        }

        public void Run()
        {

            using (udpListener = new UdpListener(new IPEndPoint(IPAddress.Loopback, 53)))
            {
                udpListener.OnRequest += HandleRequest;
                Task.Run(() => udpListener.Start());
                while (!closed)
                {
                    Thread.Sleep(1000);
                    RemoveOldEntries(CacheForTypeA);
                    RemoveOldEntries(CacheForTypeNs);
                }
            }
        }

        private void RemoveOldEntries(Dictionary<string, DnsPacket> cache)
        {
            var toDelete = new List<string>();
            foreach (var record in cache)
            {
                var now = DateTime.Now;
                var exp = record.Value.Answers[0].AbsoluteExpitationDate;
                //  Console.WriteLine($"EXP: {exp}  NOW: {now}  <={exp<=now}");
                if (exp <= now)
                    toDelete.Add(record.Key);
            }

            foreach (var element in toDelete)
            {
                Console.WriteLine("Deleting element from cache...");
                cache.Remove(element);
            }
        }

        private byte[] HandleRequest(byte[] data)
        {
            var query = new DnsPacket(data);

            Console.WriteLine($"GOT:\n{query}");

            if (!query.IsQuery)
                return null;
            foreach (var question in query.Questions)
                switch (question.Type)
                {
                    case ResourceType.A:
                        return FindCachedAnswerOrResend(query, CacheForTypeA);
                    case ResourceType.NS:
                        return FindCachedAnswerOrResend(query, CacheForTypeNs);
                    default:
                        Console.Error.WriteLine(
                            $"Message with the type code {question.Type} is not currently supported!");
                        continue;
                }

            return null;
        }

        private byte[] FindCachedAnswerOrResend(DnsPacket query, Dictionary<string, DnsPacket> cache)
        {
            return cache.TryGetValue(query.Questions[0].Name, out var cachedPacket)
                ? UpdatePacketFromCache(cachedPacket, query.TransactionId)
                : GetAnswerFromBetterServer(query.Data, cache);
        }
        private byte[] GetAnswerFromBetterServer(byte[] query, Dictionary<string, DnsPacket> cache)
        {
            using (var client = new UdpClient())
            {
                client.Send(query, query.Length, remoteDns);
                var response = client.Receive(ref remoteDns);
                var responsePacket = new DnsPacket(response);
                Console.WriteLine($"RECEIVED:\n{responsePacket}");
                cache[responsePacket.Questions[0].Name] = responsePacket;
                return response;
            }
        }

        private static byte[] UpdatePacketFromCache(DnsPacket packet, uint newId)
        {
            var updatedTtlData = UpdateTtl(packet);
            var updated = UpdateTransactionId(updatedTtlData, newId);
            Console.WriteLine($"MESSAGE FROM CACHE:\n{new DnsPacket(updated)}");
            return updated;
        }

        private static byte[] UpdateTransactionId(byte[] data, uint newId)
        {
            var newIdB = BitConverter.GetBytes(newId.SwapEndianness());
            for (var j = 2; j < newIdB.Length; j++)
                data[DnsPacketFields.TransactionId + j - 2] = newIdB[j];
            return data;
        }


        private static byte[] UpdateTtl(DnsPacket packet)
        {
            var dataToSend = packet.Data;
            for (var i = 0; i < packet.TtlIndexes.Count; i++)
            {
                var index = packet.TtlIndexes[i];
                var answer = packet.Answers[i];
                var oldExpDate = answer.AbsoluteExpitationDate;
                var now = DateTime.Now;
                var newTtl = (uint) oldExpDate.Subtract(now).TotalSeconds;
                var newTtlB = BitConverter.GetBytes(newTtl.SwapEndianness());
                Console.WriteLine($"Old TTL: {answer.Ttl} New TTL: {newTtl}");
                for (var j = 0; j < newTtlB.Length; j++) dataToSend[index + j] = newTtlB[j];
            }

            return dataToSend;
        }

        public void Quit()
        {
            Console.WriteLine("Saving data to ***...");
            SerializeCache(CacheForTypeA, AFilename);
            SerializeCache(CacheForTypeNs, NsFilename);
            closed = true;
            udpListener?.Dispose();

        }

        private void SerializeCache(Dictionary<string, DnsPacket> cache, string filename)
        {
            if (cache.Count == 0)
                return;
            var formatter = new BinaryFormatter();
            using (var fs = new FileStream(filename, FileMode.OpenOrCreate))
            {
                formatter.Serialize(fs, cache);
            }
        }

        private Dictionary<string, DnsPacket> DeserializeCache(string filename)
        {
            try
            {
                var formatter = new BinaryFormatter();
                using (var fs = new FileStream(filename, FileMode.Open))
                {
                    return (Dictionary<string, DnsPacket>) formatter.Deserialize(fs);
                }
            }

            catch (FileNotFoundException e)
            {
                return new Dictionary<string, DnsPacket>();
            }
            catch (SerializationException e)
            {
                return new Dictionary<string, DnsPacket>();
            }
        }
    }
}
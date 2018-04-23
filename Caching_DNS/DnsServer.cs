using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Caching_DNS.DnsQueries;
using Caching_DNS.DnsStructure;
using Caching_DNS.Helpers;
using Caching_DNS.Network;
using NetTopologySuite.IO;
using Newtonsoft.Json;

namespace Caching_DNS
{
    public class DnsServer
    {
        private Dictionary<string, DnsPacket> DomainToIpCache =
            new Dictionary<string, DnsPacket>();

        private readonly DnsPacketParser packetParser = new DnsPacketParser();
        private UdpListener udpListener;
        private bool closed = false;
        private const string D2IFilename = "D2I.json";
        private IPEndPoint remoteDns = new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53);

        public void Run()
        {
            // DomainToIpCache = DeserializeCache(D2IFilename);
            // IpToDomainCache = DeserializeCache(I2DFilename);

            using (udpListener = new UdpListener(new IPEndPoint(IPAddress.Loopback, 53)))
            {
                udpListener.OnRequest += HandleRequest;
                Task.Run(() => udpListener.Start());
                while (!closed)
                {
                    Thread.Sleep(1000);
                    RemoveOldEntries(DomainToIpCache);
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

        private byte[] HandleRequest(IPEndPoint sender, byte[] data)
        {
            var packet = packetParser.Parse(data);

            Console.WriteLine($"GOT:\n{packet}");

            if (packet.IsQuery)
            {
                foreach (var question in packet.Questions)
                {
                    if (question.Type != ResourceType.A && question.Type != ResourceType.NS)
                    {
                        Console.Error.WriteLine(
                            $"Message with the type code {question.Type} is not currently supported!");
                        continue;
                    }

                    if (DomainToIpCache.ContainsKey(question.Name))
                    {
                        var cached = DomainToIpCache[question.Name];
                        Console.WriteLine($"Sending pack from cache:\n{cached}");
                        var updatedTtlData = GetPacketWIthUpdatedTtl(cached);
                        var newId = packet.TransactionId;
                        var oldId = cached.TransactionId;
                        Console.WriteLine($"OLD ID: {oldId} NEW ID:{newId}");
                        return UpdateTransactionId(updatedTtlData, newId);
                    }

                    using (var client = new UdpClient())
                    {
                        client.Send(data, data.Length, remoteDns);
                        var response = client.Receive(ref remoteDns);
                        var responsePacket = packetParser.Parse(response);
                       Console.WriteLine($"RECEIVED:\n{responsePacket}");
                        DomainToIpCache[question.Name] = responsePacket;
                        return response;
                    }
                }
            }

            return null;
        }

        private byte[] UpdateTransactionId(byte[] updatedTtlData, uint newId)
        {
            var newIdB = BitConverter.GetBytes(newId.SwapEndianness());
            for (int j = 2; j < newIdB.Length; j++)
            {
                updatedTtlData[DnsPacketFields.TransactionId + j - 2] = newIdB[j];
            }

            var pack = packetParser.Parse(updatedTtlData);
            Console.WriteLine($"Id: {pack.TransactionId}");
            return updatedTtlData;
        }


        private byte[] GetPacketWIthUpdatedTtl(DnsPacket packet)
        {
            
            var dataToSend = packet.Data;
            for (var i = 0; i < packet.TtlIndexes.Count; i++)
            {
                var index = packet.TtlIndexes[i];
                var answer = packet.Answers[i];
                var oldExpDate = answer.AbsoluteExpitationDate;
                var now = DateTime.Now;
                var newTtl = (UInt32)oldExpDate.Subtract(now).TotalSeconds;
                var newTtlB = BitConverter.GetBytes(newTtl.SwapEndianness());
                Console.WriteLine($"Old TTL: {answer.Ttl} New TTL: {newTtl}");
                for (int j = 0; j < newTtlB.Length; j++)
                {
                    dataToSend[index + j] = newTtlB[j];
                }
            }

            return dataToSend;
        }
        public void Quit()
        {
            closed = true;
            udpListener?.Dispose();
            Console.WriteLine("Saving data to ***...");
            //  SerializeCache(DomainToIpCache, D2IFilename);
            // SerializeCache(IpToDomainCache, I2DFilename);
        }

        private void SerializeCache(Dictionary<string, DnsPacket> cache, string filename)
        {
            if (cache.Count == 0)
                return;
            File.WriteAllText(filename, JsonConvert.SerializeObject(cache));
        }

        private Dictionary<string, DnsPacket> DeserializeCache(string filename)
        {
            try
            {
                var str = File.ReadAllText(filename);
                return JsonConvert.DeserializeObject<Dictionary<string, DnsPacket>>(str);
            }
            catch (FileNotFoundException e)
            {
                return new Dictionary<string, DnsPacket>();
            }
        }
    }
}
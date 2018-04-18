using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Caching_DNS.DnsQueries;
using Caching_DNS.DnsStructure;
using Caching_DNS.Network;
using NetTopologySuite.IO;
using Newtonsoft.Json;

namespace Caching_DNS
{

    public class DnsServer
    {
        private Dictionary<string, List<ResourseRecord>> DomainToIpCache = new Dictionary<string, List<ResourseRecord>>();
        private readonly DnsPacketParser packetParser = new DnsPacketParser();
        private UdpListener udpListener;
        private bool closed = false;
        private const string D2IFilename = "D2I.json";
        private const string I2DFilename = "I2D.json";
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

        private static void RemoveOldEntries(Dictionary<string, List<ResourseRecord>> cache)
        {
            
            foreach (var record in cache)
                for (int i = record.Value.Count - 1; i >= 0; i--)
                {
                    var resRecors = record.Value[i];
                    if (resRecors.AbsoluteExpitationDate <= DateTime.Now)
                    {
                        record.Value.RemoveAt(i);
                        Console.WriteLine("Deleted old cache entry!");
                    }
                }
        }
        private void HandleRequest(IPEndPoint sender, byte[] data)
        {
            var packet = packetParser.Parse(data);

            Console.WriteLine(packet);

            if (packet.IsQuery)
            {
                foreach (var question in packet.Questions)
                {
                    if (DomainToIpCache.ContainsKey(question.Name))
                    {
                        using (var client = new UdpClient())
                        {
                            client.Send(new byte[48], 48, sender);
                        }
                    }
                    else
                    {
                        using (var client = new UdpClient())
                        {
                            client.Send(data, data.Length, remoteDns);
                            var response = client.Receive(ref remoteDns);
                            var responsePacket = packetParser.Parse(response);

                            Console.WriteLine(responsePacket);

                            Console.WriteLine($"Sending answer back to {sender}\n\n");
                            client.Send(response, response.Length, sender);
                           // DomainToIpCache.Add(question.Name, responsePacket.Answers);
                        }
                    }
                }
            }
            
        }

        public void Quit()
        {
            closed = true;
            udpListener?.Dispose();
            Console.WriteLine("Saving data to ***...");
          //  SerializeCache(DomainToIpCache, D2IFilename);
           // SerializeCache(IpToDomainCache, I2DFilename);
        }

        private void SerializeCache(Dictionary<string, ResourseRecord> cache, string filename)
        {
            if (cache.Count == 0)
                return;
            File.WriteAllText(filename, JsonConvert.SerializeObject(cache));
        }

        private Dictionary<string, ResourseRecord> DeserializeCache(string filename)
        {
            try
            {
                var str = File.ReadAllText(filename);
                return JsonConvert.DeserializeObject<Dictionary<string, ResourseRecord>>(str);
            }
            catch (FileNotFoundException e)
            {
                return new Dictionary<string, ResourseRecord>();
            }
            
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using Caching_DNS.DnsStructure;
using Caching_DNS.Enums;
using Caching_DNS.Network;

namespace Caching_DNS
{
    public class DnsServer
    {
        private const string CacheFilename = "cache.dat";
        private static readonly ResourceType[] SupportedTypes = {ResourceType.A, ResourceType.NS};
        private readonly Dictionary<ResourceType, Dictionary<string, DnsPacket>> cache;

        private bool closed;
        private IPEndPoint remoteDns = new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53);
        private UdpListener udpListener;

        public DnsServer()
        {
            cache = DeserializeCache();
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
                    RemoveOldEntries();
                }
            }
        }

        private void RemoveOldEntries()
        {
            var toDelete = new List<(ResourceType, string)>();
            foreach (var recordType in cache)
            foreach (var record in recordType.Value)
                if (record.Value.IsOutdated())
                    toDelete.Add((recordType.Key, record.Key));


            foreach (var element in toDelete)
            {
                Console.WriteLine($"Deleting {element.Item2} entry from cache...");
                cache[element.Item1].Remove(element.Item2);
            }
        }

        private byte[] HandleRequest(byte[] data)
        {
            var query = new DnsPacket(data);

            Console.WriteLine($"GOT:\n{query}");

            if (!query.IsQuery)
                return null;

            foreach (var question in query.Questions)
            {
                if (SupportedTypes.Contains(question.Type))
                    return FindCachedAnswerOrResend(query, cache[question.Type]);

                Console.Error.WriteLine(
                    $"Message with the type code {question.Type} is not currently supported!");
            }

            return null;
        }

        private byte[] FindCachedAnswerOrResend(DnsPacket query, Dictionary<string, DnsPacket> subCache)
        {
            return subCache.TryGetValue(query.Questions[0].Name, out var cachedPacket)
                ? UpdatePacketFromCache(cachedPacket, query.TransactionId)
                : GetAnswerFromBetterServer(query.Data, subCache);
        }

        private byte[] GetAnswerFromBetterServer(byte[] query, Dictionary<string, DnsPacket> subCache)
        {
            using (var client = new UdpClient())
            {
                client.Client.ReceiveTimeout = 2000;
                client.Send(query, query.Length, remoteDns);
                byte[] response;
                try
                {
                    response = client.Receive(ref remoteDns);
                }
                catch (SocketException)
                {
                    Console.Error.WriteLine("Couldn't connect ot the upper server. Check internet connection");
                    return null;
                }

                var responsePacket = new DnsPacket(response);
                Console.WriteLine($"RECEIVED:\n{responsePacket}");
                subCache[responsePacket.Questions[0].Name] = responsePacket;
                return response;
            }
        }

        private static byte[] UpdatePacketFromCache(DnsPacket packet, uint newId)
        {
            packet.UpdateTtl();
            packet.UpdateTransactionId(newId);
            Console.WriteLine($"MESSAGE FROM CACHE:\n{packet}");
            return packet.Data;
        }


        public void Quit()
        {
            Console.WriteLine($"Saving data to {CacheFilename}...");
            SerializeCache();
            closed = true;
            udpListener?.Dispose();
        }

        private void SerializeCache()
        {
            if (cache.Count == 0)
                return;
            var formatter = new BinaryFormatter();
            using (var fs = new FileStream(CacheFilename, FileMode.OpenOrCreate))
            {
                formatter.Serialize(fs, cache);
            }
        }

        private Dictionary<ResourceType, Dictionary<string, DnsPacket>> DeserializeCache()
        {
            try
            {
                var formatter = new BinaryFormatter();
                using (var fs = new FileStream(CacheFilename, FileMode.Open))
                {
                    return (Dictionary<ResourceType, Dictionary<string, DnsPacket>>) formatter.Deserialize(fs);
                }
            }

            catch (FileNotFoundException)
            {
                return SupportedTypes.ToDictionary(supportedType => supportedType,
                    supportedType => new Dictionary<string, DnsPacket>());
            }
            catch (SerializationException)
            {
                return SupportedTypes.ToDictionary(supportedType => supportedType,
                    supportedType => new Dictionary<string, DnsPacket>());
            }
        }
    }
}
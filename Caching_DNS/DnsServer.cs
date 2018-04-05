using System;
using System.Net;
using System.Threading.Tasks;
using Caching_DNS.DnsStructure;
using Caching_DNS.Network;

namespace Caching_DNS
{
    public class DnsServer
    {
        private readonly DnsPacketParser packetParser = new DnsPacketParser();
        public void Run()
        {
            using (var udpListener = new UdpListener(new IPEndPoint(IPAddress.Loopback, 53)))
            {
                udpListener.OnRequest += HandleRequest;
                Task.Run(() => udpListener.Start());
                while (true)
                {
                    //   Console.WriteLine("Here");
                }
            }
        }

        private void HandleRequest(byte[] data)
        {
            Console.WriteLine("GOT SMTH");
            var packet = packetParser.Parse(data);
        }
    }
}
using System;
using System.Net;
using System.Net.Sockets;

namespace Caching_DNS.Network
{
    public class UdpListener : IDisposable
    {
        private const int ListenPort = 53;
        private readonly UdpClient listener;
        private bool closed;
        public Func<byte[], byte[]> OnRequest;

        public UdpListener(IPEndPoint iPEndPoint)
        {
            listener = new UdpClient(iPEndPoint);
        }

        public void Dispose()
        {
            Console.WriteLine("Closing UDP listener");
            closed = true;
        }

        public void Start()
        {
            using (listener)
            {
                var sender = new IPEndPoint(IPAddress.Any, ListenPort);
                while (!closed)
                    try
                    {
                        Console.WriteLine("Waiting for message");
                        var bytes = listener.Receive(ref sender);
                        Console.WriteLine($"Received message from {sender}");
                        var response = OnRequest?.Invoke(bytes);

                        if (response == null) continue;

                        Console.WriteLine($"Sending answer back to {sender}\n\n");
                        listener.Send(response, response.Length, sender);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
            }
        }
    }
}
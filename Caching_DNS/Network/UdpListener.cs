using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Caching_DNS.Network
{
    public class UdpListener : IDisposable
    {
        private const int ListenPort = 53;
        private readonly UdpClient listener;
        public Func<IPEndPoint, byte[], byte[]> OnRequest;
        private bool closed = false;

        public UdpListener(IPEndPoint iPEndPoint)
        {
            listener = new UdpClient(iPEndPoint);
        }

        public void Dispose()
        {
            Console.WriteLine("Closing UDP listener");
            closed = true;
           // listener.Close();
        }

        public void Start()
        {
            using (listener)
            {
                var groupEP = new IPEndPoint(IPAddress.Any, ListenPort);
                while (!closed)
                    try
                    {
                        Console.WriteLine("Waiting for message");
                        var bytes = listener.Receive(ref groupEP);
                        Console.WriteLine($"Received message from {groupEP}");
                        var response = OnRequest?.Invoke(groupEP, bytes);

                        if (response != null)
                        {
                            Console.WriteLine($"Sending answer back to {groupEP}\n\n");
                            listener.Send(response, response.Length, groupEP);
                        }

                    }


                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
            }
        }
    }
}
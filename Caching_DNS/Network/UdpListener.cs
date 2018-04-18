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
        public Action<IPEndPoint, byte[]> OnRequest;
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
                        Console.WriteLine($"Received broadcast from {groupEP}");
                        OnRequest?.Invoke(groupEP, bytes);
                        
                    }


                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
            }
        }
    }
}
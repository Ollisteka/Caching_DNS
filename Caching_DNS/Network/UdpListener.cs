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
        public Action<byte[]> OnRequest;

        public UdpListener(IPEndPoint iPEndPoint)
        {
            listener = new UdpClient(iPEndPoint);
        }

        public void Dispose()
        {
            listener.Close();
        }

        public void Start()
        {
            using (listener)
            {
                var groupEP = new IPEndPoint(IPAddress.Any, ListenPort);
                while (true)
                    try
                    {
                        Console.WriteLine("Waiting for message");
                        var bytes = listener.Receive(ref groupEP);
                        OnRequest?.Invoke(bytes);
                        Console.WriteLine("Received broadcast from {0} :\n {1}\n",
                            groupEP,
                            Encoding.ASCII.GetString(bytes, 0, bytes.Length));
                    }


                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
            }
        }
    }
}
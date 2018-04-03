using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Caching_DNS
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new DnsServer();
            Task.Run(() => Quit());
            server.Run();
        }

        private static void Quit()
        {
            while (true)
                if (Console.ReadKey().Key == ConsoleKey.Escape)
                    Environment.Exit(1);
        }
    }

    public class DnsServer
    {
        private readonly IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Loopback, 53);

        public void Run()
        {
            using (var udpListener = new UdpListener())
            {
                udpListener.OnRequest += HandleRequest;
                while (true)
                {
                }
            }
        }

        private void HandleRequest(SocketAsyncEventArgs obj)
        {
            Console.WriteLine("GOT SMTH");
        }
    }

    public class UdpListener : IDisposable
    {
        private const int listenPort = 53;
        //private readonly Socket listener;
        private readonly UdpClient listener;
        public Action<SocketAsyncEventArgs> OnRequest;

        public UdpListener()
        {
            //listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // listener.Bind(new IPEndPoint(IPAddress.Any, 53));
            listener = new UdpClient(new IPEndPoint(IPAddress.Loopback, 53));
            Start();
        }

        private async void Start()
        {
            bool done = false;

            using (UdpClient listener = new UdpClient(listenPort))
            {
                IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, listenPort);

                try
                {
                    while (!done)
                    {
                        Console.WriteLine("Waiting for broadcast");
                        byte[] bytes = listener.Receive(ref groupEP);

                        Console.WriteLine("Received broadcast from {0} :\n {1}\n",
                            groupEP.ToString(),
                            Encoding.ASCII.GetString(bytes, 0, bytes.Length));
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            
            
            
        }

        public void Dispose()
        {
            listener.Close();
        }
    }
}
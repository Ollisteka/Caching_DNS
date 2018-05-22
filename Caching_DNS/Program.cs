using System;
using System.Threading.Tasks;

namespace Caching_DNS
{
    internal class Program
    {
        private static DnsServer server;

        private static void Main(string[] args)
        {
            server = new DnsServer();
            Task.Run(() => Quit());
            server.Run();
        }

        private static void Quit()
        {
            Console.WriteLine("Press Esc to exit");
            while (true)
                if (Console.ReadKey().Key == ConsoleKey.Escape)
                {
                    server.Quit();
                    Environment.Exit(0);
                }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
}
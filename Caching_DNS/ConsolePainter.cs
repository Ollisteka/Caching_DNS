using System;

namespace Caching_DNS
{
    public static class ConsolePainter
    {
        public static void WriteColoredLine(ConsoleColor background = ConsoleColor.Black,
            ConsoleColor font = ConsoleColor.White, string text = "")
        {
            Console.BackgroundColor = background;
            Console.ForegroundColor = font;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void WriteWarning(string info)
        {
            WriteColoredLine(ConsoleColor.Red, ConsoleColor.Green, info);
        }

        public static void WriteRequest(string info)
        {
            WriteColoredLine(ConsoleColor.DarkGreen, ConsoleColor.White, info);
        }

        public static void WriteResponse(string info)
        {
            WriteColoredLine(ConsoleColor.DarkBlue, ConsoleColor.White, info);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CBR
{
    public static class Utility
    {
        public static void Log(string text)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(text);
            Console.ResetColor();
        }
    }
}

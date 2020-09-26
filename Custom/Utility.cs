using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

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

        public static string HexToString(string hex)
        {
            string ascii = "";

            for (int i = 0; i < hex.Length / 2; i++)
            {
                string chars = $"{hex[i * 2]}{hex[(i * 2) + 1]}";

                if (chars != "00")
                {
                    ascii += (char) Convert.ToInt32(chars, 16);
                }
            }

            return ascii;
        }

        public static byte[] HexToBytes(string hex)
        {
            int length = hex.Length / 2;

            byte[] bytes = new byte[length];

            for (int i = 0; i < length; i++)
            {
                string chars = $"{hex[i * 2]}{hex[(i * 2) + 1]}";
                byte b = byte.Parse(chars, NumberStyles.HexNumber, CultureInfo.InvariantCulture); ;

                bytes[i] = b;

                //Console.WriteLine($"Byte {i + 1}:\n\tfrom: {chars} ({chars.GetType()})\n\tto: {b} ({b.GetType()})");
            }

            return bytes;
        }
    }
}

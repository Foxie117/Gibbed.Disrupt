using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CBR
{
    public static class FileLogger
    {
        public static readonly string DefaultLogFile = "log.txt";

        private static string path = "";

        public static void Initialize(string dir)
        {
            path = Path.Combine(dir, DefaultLogFile);

            //File.CreateText(path);
            File.WriteAllText(path, "");
        }

        public static void Log(string text)
        {
            string prefix = $"[{DateTime.Now.ToString()}]";

            using (StreamWriter sw = File.AppendText(path))
            {
                sw.WriteLine($"{prefix} {text}");
            }
        }
    }
}

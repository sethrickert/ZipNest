using System;
using System.IO;
using System.Text;

namespace ZipNest
{
    internal static class Log
    {
        private static readonly object Gate = new object();

        public static void Write(string message)
        {
            try
            {
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZipNest", "Logs");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, "ZipNest.log");
                lock (Gate)
                {
                    File.AppendAllText(path, DateTimeOffset.Now.ToString("u") + " " + message + Environment.NewLine, Encoding.UTF8);
                    TrimIfNeeded(path);
                }
            }
            catch
            {
                // Logging must never interfere with extraction.
            }
        }

        private static void TrimIfNeeded(string path)
        {
            var info = new FileInfo(path);
            if (info.Length <= 2 * 1024 * 1024)
                return;
            string[] lines = File.ReadAllLines(path);
            int start = Math.Max(0, lines.Length / 2);
            string[] retained = new string[lines.Length - start];
            Array.Copy(lines, start, retained, 0, retained.Length);
            File.WriteAllLines(path, retained, Encoding.UTF8);
        }
    }
}

using System;

namespace Shared
{
    internal static class Logger
    {
        private static string LogPath()
        {

            return System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "log.txt");
        }

        internal static void New()
        {
            string logFilePath = LogPath();
            if (!System.IO.File.Exists(logFilePath))
            {
                using (System.IO.File.Create(logFilePath)) { }
            }
            else
                System.IO.File.WriteAllText(logFilePath, string.Empty);
        }

        private static void Add(string logMessage)
        {
            string logFilePath = LogPath();
            if (!System.IO.File.Exists(logFilePath))
            {
                using (System.IO.File.Create(logFilePath)) { }
            }
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(LogPath(), true))
            {
                file.WriteLine(logMessage);
            }

        }

        private static string TimeStamp()
        {
            return System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static void Info(string message)
        {
            Add($"[{TimeStamp()}] 🏁 INFO: {message}");
        }
        public static void Warning(string message)
        {
            Add($"[{TimeStamp()}] ⚠ WARNING: {message}!");
        }
        public static void Error(string message)
        {
            Add($"[{TimeStamp()}] ⨉ ERROR: {message}!!!");
        }
    }
}

using Diagnostics = System.Diagnostics;

namespace UniGetUI.Core.Logging
{
    public static class Logger
    {
        private static readonly List<LogEntry> LogContents = [];

        // String parameter log functions
        public static void ImportantInfo(string s, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            Diagnostics.Debug.WriteLine($"[{caller}] " + s);
            LogContents.Add(new LogEntry(s, LogEntry.SeverityLevel.Success));
        }

        public static void Debug(string s, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            Diagnostics.Debug.WriteLine($"[{caller}] " + s);
            LogContents.Add(new LogEntry(s, LogEntry.SeverityLevel.Debug));
        }

        public static void Info(string s, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            Diagnostics.Debug.WriteLine($"[{caller}] " + s);
            LogContents.Add(new LogEntry(s, LogEntry.SeverityLevel.Info));
        }

        public static void Warn(string s, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            Diagnostics.Debug.WriteLine($"[{caller}] " + s);
            LogContents.Add(new LogEntry(s, LogEntry.SeverityLevel.Warning));
        }

        public static void Error(string s, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            Diagnostics.Debug.WriteLine($"[{caller}] " + s);
            LogContents.Add(new LogEntry(s, LogEntry.SeverityLevel.Error));
        }

        // Exception parameter log functions
        public static void ImportantInfo(Exception e, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            Diagnostics.Debug.WriteLine($"[{caller}] " + e.ToString());
            LogContents.Add(new LogEntry(e.ToString(), LogEntry.SeverityLevel.Success));
        }

        public static void Debug(Exception e, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            Diagnostics.Debug.WriteLine($"[{caller}] " + e.ToString());
            LogContents.Add(new LogEntry(e.ToString(), LogEntry.SeverityLevel.Debug));
        }

        public static void Info(Exception e, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            Diagnostics.Debug.WriteLine($"[{caller}] " + e.ToString());
            LogContents.Add(new LogEntry(e.ToString(), LogEntry.SeverityLevel.Info));
        }

        public static void Warn(Exception e, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            Diagnostics.Debug.WriteLine($"[{caller}] " + e.ToString());
            LogContents.Add(new LogEntry(e.ToString(), LogEntry.SeverityLevel.Warning));
        }

        public static void Error(Exception e, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            Diagnostics.Debug.WriteLine($"[{caller}] " + e.ToString());
            LogContents.Add(new LogEntry(e.ToString(), LogEntry.SeverityLevel.Error));
        }

        public static LogEntry[] GetLogs()
        {
            return LogContents.ToArray();
        }
    }
}

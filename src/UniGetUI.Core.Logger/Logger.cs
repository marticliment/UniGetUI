namespace UniGetUI.Core.Logging
{
    public static class Logger
    {
        private static readonly List<LogEntry> LogContents = new();

        public static void Log(string s)
        {
            LogContents.Add(new LogEntry(s, LogEntry.SeverityLevel.Debug));
        }

        public static void Log(object e)
        {
            LogContents.Add(new LogEntry(e != null ? e.ToString() : "[object was null]", LogEntry.SeverityLevel.Debug));
        }

        public static LogEntry[] GetLogs()
        {
            return LogContents.ToArray();
        }
    }
}

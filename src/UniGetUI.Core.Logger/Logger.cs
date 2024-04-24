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
            LogContents.Add(new LogEntry(e.ToString() ?? "[NullObject]", LogEntry.SeverityLevel.Debug));
        }


        
        // String parameter log functions
        public static void Success(string s)
        { LogContents.Add(new LogEntry(s, LogEntry.SeverityLevel.Success)); }

        public static void Debug(string s)
        { LogContents.Add(new LogEntry(s, LogEntry.SeverityLevel.Debug)); }

        public static void Info(string s)
        { LogContents.Add(new LogEntry(s, LogEntry.SeverityLevel.Info)); }

        public static void Warn(string s)
        { LogContents.Add(new LogEntry(s, LogEntry.SeverityLevel.Warning)); }

        public static void Error(string s)
        { LogContents.Add(new LogEntry(s, LogEntry.SeverityLevel.Error)); }


        // Exception parameter log functions
        public static void Success(Exception e)
        { LogContents.Add(new LogEntry(e.ToString() , LogEntry.SeverityLevel.Success)); }

        public static void Debug(Exception e)
        { LogContents.Add(new LogEntry(e.ToString(), LogEntry.SeverityLevel.Debug)); }

        public static void Info(Exception e)
        { LogContents.Add(new LogEntry(e.ToString(), LogEntry.SeverityLevel.Info)); }

        public static void Warn(Exception e)
        { LogContents.Add(new LogEntry(e.ToString(), LogEntry.SeverityLevel.Warning)); }

        public static void Error(Exception e)
        { LogContents.Add(new LogEntry(e.ToString(), LogEntry.SeverityLevel.Error)); }


        public static LogEntry[] GetLogs()
        {
            return LogContents.ToArray();
        }
    }
}

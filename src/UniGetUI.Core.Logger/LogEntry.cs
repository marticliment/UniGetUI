namespace UniGetUI.Core.Logging
{
    public readonly struct LogEntry
    {
        public enum SeverityLevel
        {
            Debug,
            Info,
            Warning,
            Error,
        }
        public DateTime Time { get; }
        public String Content { get; }
        public SeverityLevel Severity { get; }

        public LogEntry(string content, SeverityLevel severity)
        {
            Time = DateTime.Now;
            Content = content;
            Severity = severity;
        }

    }
}

namespace UniGetUI.Core.Logging
{
    public readonly struct LogEntry
    {
        public enum SeverityLevel
        {
            Debug,
            Info,
            Success,
            Warning,
            Error,
        }

        public readonly DateTime Time { get; }
        public readonly string Content { get; }
        public readonly SeverityLevel Severity { get; }

        public LogEntry(string content, SeverityLevel severity)
        {
            Time = DateTime.Now;
            Content = content;
            Severity = severity;
        }

    }
}

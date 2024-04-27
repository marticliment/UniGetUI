namespace UniGetUI.Core.Logging.Tests
{
    public class LogEntryTests
    {
        [Fact]
        public void TestLogEntry()
        {
            var startTime = DateTime.Now;
            var logEntry1 = new LogEntry("Hello World", LogEntry.SeverityLevel.Info);
            var logEntry2 = new LogEntry("Hello World 2", LogEntry.SeverityLevel.Debug);
            var logEntry3 = new LogEntry("Hello World 3", LogEntry.SeverityLevel.Error);

            var endTime = DateTime.Now;

            Assert.Equal("Hello World", logEntry1.Content);
            Assert.Equal("Hello World 2", logEntry2.Content);
            Assert.Equal("Hello World 3", logEntry3.Content);

            Assert.Equal(LogEntry.SeverityLevel.Info, logEntry1.Severity);
            Assert.Equal(LogEntry.SeverityLevel.Debug, logEntry2.Severity);
            Assert.Equal(LogEntry.SeverityLevel.Error, logEntry3.Severity);

            Assert.True(logEntry1.Time > startTime && logEntry1.Time < endTime);
            Assert.True(logEntry2.Time > startTime && logEntry2.Time < endTime);
            Assert.True(logEntry3.Time > startTime && logEntry3.Time < endTime);
        }
    }
}
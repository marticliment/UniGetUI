namespace UniGetUI.Core.Logging.Tests
{
    public class LogEntryTests
    {
        [Fact]
        public async Task TestLogEntry()
        {
            DateTime startTime = DateTime.Now;

            await Task.Delay(100);

            LogEntry logEntry1 = new("Hello World", LogEntry.SeverityLevel.Info);
            await Task.Delay(50);
            LogEntry logEntry2 = new("Hello World 2", LogEntry.SeverityLevel.Debug);
            await Task.Delay(50);
            LogEntry logEntry3 = new("Hello World 3", LogEntry.SeverityLevel.Error);

            await Task.Delay(100);

            DateTime endTime = DateTime.Now;

            Assert.Equal("Hello World", logEntry1.Content);
            Assert.Equal("Hello World 2", logEntry2.Content);
            Assert.Equal("Hello World 3", logEntry3.Content);

            Assert.Equal(LogEntry.SeverityLevel.Info, logEntry1.Severity);
            Assert.Equal(LogEntry.SeverityLevel.Debug, logEntry2.Severity);
            Assert.Equal(LogEntry.SeverityLevel.Error, logEntry3.Severity);

            Assert.True(logEntry1.Time > startTime && logEntry1.Time < endTime);
            Assert.True(logEntry2.Time > startTime && logEntry2.Time < endTime);
            Assert.True(logEntry3.Time > startTime && logEntry3.Time < endTime);
            Assert.True(logEntry1.Time < logEntry2.Time);
            Assert.True(logEntry2.Time < logEntry3.Time);
        }
    }
}
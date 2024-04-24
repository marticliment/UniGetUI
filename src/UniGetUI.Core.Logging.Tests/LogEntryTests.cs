namespace UniGetUI.Core.Logging.Tests
{
    [TestClass]
    public class LogEntryTests
    {
        [TestMethod]
        public void TestLogEntry()
        {
            var startTime = DateTime.Now;
            var logEntry1 = new LogEntry("Hello World", LogEntry.SeverityLevel.Info);
            var logEntry2 = new LogEntry("Hello World 2", LogEntry.SeverityLevel.Debug);
            var logEntry3 = new LogEntry("Hello World 3", LogEntry.SeverityLevel.Error);

            var endTime = DateTime.Now;

            Assert.AreEqual(logEntry1.Content, "Hello World");
            Assert.AreEqual(logEntry2.Content, "Hello World 2");
            Assert.AreEqual(logEntry3.Content, "Hello World 3");

            Assert.AreEqual(logEntry1.Severity, LogEntry.SeverityLevel.Info);
            Assert.AreEqual(logEntry2.Severity, LogEntry.SeverityLevel.Debug);
            Assert.AreEqual(logEntry3.Severity, LogEntry.SeverityLevel.Error);

            Assert.IsTrue(logEntry1.Time > startTime && logEntry1.Time < endTime);
            Assert.IsTrue(logEntry2.Time > startTime && logEntry2.Time < endTime);
            Assert.IsTrue(logEntry3.Time > startTime && logEntry3.Time < endTime);
        }
    }
}
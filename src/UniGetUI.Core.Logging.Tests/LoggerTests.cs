using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniGetUI.Core.Logging.Tests
{
    [TestClass]
    public class LoggerTests
    {
        [TestMethod]
        public void TestLogger()
        {
            var startTime = DateTime.Now;
            Logger.Info("Hello World");
            Logger.Debug("Hello World 2");
            Logger.Error("Hello World 3");
            Logger.Warn(new Exception("Test exception"));

            var endTime = DateTime.Now;

            var logs = Logger.GetLogs();

            Assert.AreEqual(logs[0].Content, "Hello World");
            Assert.AreEqual(logs[1].Content, "Hello World 2");
            Assert.AreEqual(logs[2].Content, "Hello World 3");
            Assert.AreEqual(logs[3].Content, "System.Exception: Test exception");

            Assert.AreEqual(logs[0].Severity, LogEntry.SeverityLevel.Info);
            Assert.AreEqual(logs[1].Severity, LogEntry.SeverityLevel.Debug);
            Assert.AreEqual(logs[2].Severity, LogEntry.SeverityLevel.Error);
            Assert.AreEqual(logs[3].Severity, LogEntry.SeverityLevel.Warning);

            Assert.IsTrue(logs[0].Time > startTime && logs[0].Time < endTime);
            Assert.IsTrue(logs[1].Time > startTime && logs[1].Time < endTime);
            Assert.IsTrue(logs[2].Time > startTime && logs[2].Time < endTime);
            Assert.IsTrue(logs[3].Time > startTime && logs[3].Time < endTime);
        }
    }
}

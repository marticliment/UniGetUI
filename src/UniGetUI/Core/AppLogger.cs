using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UniGetUI.Core.Data;
using UniGetUI.PackageEngine.Classes;

namespace UniGetUI.Core
{
    public class AppLogger : ILogger
    {
        private static readonly Lazy<AppLogger> _instance = new(() => new AppLogger());

        /// <summary>
        /// This should only be accessed by DI or removed
        /// </summary>
        public static AppLogger Instance => _instance.Value;

        /// <summary>
        /// This should only be accessed by DI or removed
        /// </summary>
        private AppLogger() { }

        void ILogger.Log(string s)
        {
            LogInternal(s);
        }

        private static void LogInternal(string s)
        {
            CoreData.UniGetUILog += s + "\n";
            Debug.WriteLine(s);
        }

        void ILogger.Log(Exception e)
        { LogInternal(e.ToString()); }

        void ILogger.Log(object o)
        { if (o != null) LogInternal(o.ToString()); else LogInternal("null"); }

        void ILogger.LogManagerOperation(IPackageManager manager, Process process, string output)
        {
            output = Regex.Replace(output, "\n.{0,6}\n", "\n");
            CoreData.ManagerLogs += $"\n▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄";
            CoreData.ManagerLogs += $"\n█▀▀▀▀▀▀▀▀▀ [{DateTime.Now}] {manager.Name} ▀▀▀▀▀▀▀▀▀▀▀";
            CoreData.ManagerLogs += $"\n█  Executable: {process.StartInfo.FileName}";
            CoreData.ManagerLogs += $"\n█  Arguments: {process.StartInfo.Arguments}";
            CoreData.ManagerLogs += "\n";
            CoreData.ManagerLogs += output;
            CoreData.ManagerLogs += "\n";
            CoreData.ManagerLogs += $"[{DateTime.Now}] Exit Code: {process.ExitCode}";
            CoreData.ManagerLogs += "\n";
            CoreData.ManagerLogs += "\n";
        }
    }
}

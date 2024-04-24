using System.Diagnostics;
using UniGetUI.PackageEngine.Classes;

namespace UniGetUI.Core
{
    /// <summary>
    /// General logging functionality, should be injected at runtime.
    /// </summary>
    public interface ILogger { 
        void Log(string message);
        void Log(Exception exception);
        void Log(object type);
        void LogManagerOperation(IPackageManager manager, Process process, string output);
    }
}
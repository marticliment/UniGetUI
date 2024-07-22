using System.Diagnostics;
using UniGetUI.PackageEngine.Enums;

namespace UniGetUI.PackageEngine.ManagerClasses.Classes;
public interface IManagerLogger
{
    public IEnumerable<ITaskLogger> Operations { get; }
    INativeTaskLogger CreateNew(LoggableTaskType type);
    IProcessTaskLogger CreateNew(LoggableTaskType type, Process process);
}

public interface ITaskLogger
{
    IEnumerable<string> AsColoredString(bool verbose = false);
    void Close(int returnCode);
}

public interface INativeTaskLogger : ITaskLogger
{
    void Error(Exception? e);
    void Error(IEnumerable<string> lines);
    void Error(string? line);
    void Log(IEnumerable<string> lines);
    void Log(string? line);
}

public interface IProcessTaskLogger : ITaskLogger
{
    void AddToStdErr(IEnumerable<string> lines);
    void AddToStdErr(string? line);
    void AddToStdIn(IEnumerable<string> lines);
    void AddToStdIn(string? line);
    void AddToStdOut(IEnumerable<string> lines);
    void AddToStdOut(string? line);
}

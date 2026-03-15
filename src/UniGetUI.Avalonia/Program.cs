using System.Threading;
using Avalonia;
using AvaloniaUI.DiagnosticsProtocol;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;

namespace UniGetUI.Avalonia;

internal sealed class Program
{
    // Kept alive for the lifetime of the process to enforce single-instance
    // ReSharper disable once NotAccessedField.Local
    private static Mutex? _singleInstanceMutex;

    [STAThread]
    public static void Main(string[] args)
    {
        _singleInstanceMutex = new Mutex(
            initiallyOwned: true,
            name: "UniGetUI_" + CoreData.MainWindowIdentifier,
            createdNew: out bool createdNew);

        if (!createdNew)
        {
            // Another instance already holds the mutex — exit gracefully.
            Logger.Warn("UniGetUI is already running. Exiting duplicate instance.");
            _singleInstanceMutex.Close();
            _singleInstanceMutex = null;
            return;
        }

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            _singleInstanceMutex.ReleaseMutex();
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

#if DEBUG
        builder = builder.WithDeveloperTools(options =>
        {
            options.ApplicationName = "UniGetUI.Avalonia";
            options.ConnectOnStartup = true;
            options.EnableDiscovery = true;
            options.DiagnosticLogger = DiagnosticLogger.CreateConsole();
        });
#endif

        return builder;
    }
}

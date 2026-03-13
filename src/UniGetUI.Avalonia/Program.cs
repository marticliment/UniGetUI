using Avalonia;
using AvaloniaUI.DiagnosticsProtocol;

namespace UniGetUI.Avalonia;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

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
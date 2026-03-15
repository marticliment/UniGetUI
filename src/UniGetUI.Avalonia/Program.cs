using System.Threading;
using Avalonia;
using AvaloniaUI.DiagnosticsProtocol;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;

namespace UniGetUI.Avalonia;

internal sealed class Program
{
    // Kept alive for the lifetime of the process to enforce single-instance
    // ReSharper disable once NotAccessedField.Local
    private static Mutex? _singleInstanceMutex;

    [STAThread]
    public static void Main(string[] args)
    {
        // ── Pre-UI headless CLI dispatch (A1) ──────────────────────────────
        // These commands operate purely on settings/files and exit without
        // showing any UI.  They mirror WinUI's EntryPoint.cs dispatch block.
        if (args.Contains("--import-settings"))
        {
            Environment.Exit(RunCli_ImportSettings(args));
            return;
        }
        if (args.Contains("--export-settings"))
        {
            Environment.Exit(RunCli_ExportSettings(args));
            return;
        }
        if (args.Contains("--enable-setting"))
        {
            Environment.Exit(RunCli_EnableSetting(args));
            return;
        }
        if (args.Contains("--disable-setting"))
        {
            Environment.Exit(RunCli_DisableSetting(args));
            return;
        }
        if (args.Contains("--set-setting-value"))
        {
            Environment.Exit(RunCli_SetSettingValue(args));
            return;
        }

        // ── Single-instance enforcement ────────────────────────────────────
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
        catch (Exception ex)
        {
            // A7: top-level crash handler
            ReportFatalException(ex);
        }
        finally
        {
            _singleInstanceMutex.ReleaseMutex();
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }
    }

    // ── Headless CLI helpers ───────────────────────────────────────────────

    private static int RunCli_ImportSettings(string[] args)
    {
        int pos = Array.IndexOf(args, "--import-settings");
        if (pos < 0 || pos + 1 >= args.Length) return -1073741811; // STATUS_INVALID_PARAMETER
        string file = args[pos + 1].Trim('"').Trim('\'');
        if (!File.Exists(file)) return -1073741809; // STATUS_NO_SUCH_FILE
        try { Settings.ImportFromFile_JSON(file); return 0; }
        catch (Exception ex) { return ex.HResult; }
    }

    private static int RunCli_ExportSettings(string[] args)
    {
        int pos = Array.IndexOf(args, "--export-settings");
        if (pos < 0 || pos + 1 >= args.Length) return -1073741811;
        string file = args[pos + 1].Trim('"').Trim('\'');
        try { Settings.ExportToFile_JSON(file); return 0; }
        catch (Exception ex) { return ex.HResult; }
    }

    private static int RunCli_EnableSetting(string[] args)
    {
        int pos = Array.IndexOf(args, "--enable-setting");
        if (pos < 0 || pos + 1 >= args.Length) return -1073741811;
        string name = args[pos + 1].Trim('"').Trim('\'');
        if (!Enum.TryParse(name, out Settings.K key)) return -2; // STATUS_UNKNOWN_SETTINGS_KEY
        try { Settings.Set(key, true); return 0; }
        catch (Exception ex) { return ex.HResult; }
    }

    private static int RunCli_DisableSetting(string[] args)
    {
        int pos = Array.IndexOf(args, "--disable-setting");
        if (pos < 0 || pos + 1 >= args.Length) return -1073741811;
        string name = args[pos + 1].Trim('"').Trim('\'');
        if (!Enum.TryParse(name, out Settings.K key)) return -2;
        try { Settings.Set(key, false); return 0; }
        catch (Exception ex) { return ex.HResult; }
    }

    private static int RunCli_SetSettingValue(string[] args)
    {
        int pos = Array.IndexOf(args, "--set-setting-value");
        if (pos < 0 || pos + 2 >= args.Length) return -1073741811;
        string name  = args[pos + 1].Trim('"').Trim('\'');
        string value = args[pos + 2];
        if (!Enum.TryParse(name, out Settings.K key)) return -2;
        try { Settings.SetValue(key, value); return 0; }
        catch (Exception ex) { return ex.HResult; }
    }

    // ── A7: crash reporter ─────────────────────────────────────────────────

    private static void ReportFatalException(Exception ex)
    {
        try
        {
            // Write a crash log next to the settings directory
            string crashPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "UniGetUI",
                $"crash_{DateTime.UtcNow:yyyyMMdd_HHmmss}.log");
            Directory.CreateDirectory(Path.GetDirectoryName(crashPath)!);
            File.WriteAllText(crashPath,
                $"UniGetUI fatal crash at {DateTime.UtcNow:O}\n\n{ex}");
            Logger.Error("FATAL EXCEPTION — crash log written to: " + crashPath);
            Logger.Error(ex);
        }
        catch { /* best-effort */ }
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

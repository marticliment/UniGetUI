using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using UniGetUI.Avalonia.Views;
using UniGetUI.Core.Data;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface;
using UniGetUI.PackageEngine;

namespace UniGetUI.Avalonia;

public partial class App : Application
{
    public static App Instance { get; private set; } = null!;

    private MainWindow? _mainWindow;
    private readonly BackgroundApiRunner _backgroundApi = new();

    public override void Initialize()
    {
        Instance = this;
        AvaloniaXamlLoader.Load(this);
        ApplyTheme();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _mainWindow = new MainWindow();
            desktop.MainWindow = _mainWindow;
            _ = LoadComponentsAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ApplyTheme()
    {
        string preferredTheme = Settings.GetValue(Settings.K.PreferredTheme);
        RequestedThemeVariant = preferredTheme switch
        {
            "dark" => ThemeVariant.Dark,
            "light" => ThemeVariant.Light,
            _ => ThemeVariant.Default
        };
    }

    private async Task LoadComponentsAsync()
    {
        RegisterErrorHandling();

        Logger.ImportantInfo($"Welcome to UniGetUI Avalonia PoC — Version {CoreData.VersionName}");
        Logger.ImportantInfo($"Data directory: {CoreData.UniGetUIDataDirectory}");

        // Step 1: Load loaders (must complete before UI can use them)
        await Task.Run(PEInterface.LoadLoaders);
        Logger.Info("Package loaders initialized.");

        // Step 2: Parallel initialization
        IEnumerable<Task> initTasks = [
            Task.Run(PEInterface.LoadManagers),
            Task.Run(IconDatabase.Instance.LoadFromCacheAsync),
            InitializeBackgroundApi(),
        ];

        await Task.WhenAll(initTasks);

        Logger.Info("All managers loaded. Proceeding to interface.");
        _mainWindow?.OnManagersReady();
    }

    private async Task InitializeBackgroundApi()
    {
        if (Settings.Get(Settings.K.DisableApi))
        {
            Logger.Info("Background API is disabled via settings.");
            return;
        }

        await _backgroundApi.Start();
        Logger.Info("Background API started.");
    }

    private void RegisterErrorHandling()
    {
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Logger.Error("Unobserved Task Exception");
            Logger.Error(args.Exception);
            args.SetObserved();
        };
    }
}

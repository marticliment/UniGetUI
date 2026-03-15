using Avalonia.Threading;
using UniGetUI.Avalonia.Models;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface;
using UniGetUI.Interface.Telemetry;
using UniGetUI.PackageEngine;

namespace UniGetUI.Avalonia.Infrastructure;

internal static class AvaloniaBootstrapper
{
    private static bool _hasStarted;
    private static BackgroundApiRunner? _backgroundApi;

    public static async Task InitializeAsync()
    {
        if (_hasStarted)
        {
            return;
        }

        _hasStarted = true;
        Logger.Info("Starting Avalonia shell bootstrap");

        await Task.WhenAll(
            InitializeSharedServicesAsync(),
            InitializePackageEngineAsync()
        );

        Logger.Info("Avalonia shell bootstrap completed");
    }

    private static Task InitializeSharedServicesAsync()
    {
        CoreTools.ReloadLanguageEngineInstance();
        MainWindow.ApplyProxyVariableToProcess();
        _ = Task.Run(AvaloniaAutoUpdater.UpdateCheckLoopAsync)
            .ContinueWith(
                t => Logger.Error(t.Exception!),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        _ = Task.Run(InitializeBackgroundApiAsync)
            .ContinueWith(
                t => Logger.Error(t.Exception!),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        _ = TelemetryHandler.InitializeAsync()
            .ContinueWith(
                t => Logger.Error(t.Exception!),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        return Task.CompletedTask;
    }

    private static Task InitializePackageEngineAsync()
    {
        return Task.Run(() =>
        {
            PEInterface.LoadLoaders();
            _ = Task.Run(PEInterface.LoadManagers)
                .ContinueWith(
                    t => Logger.Error(t.Exception!),
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted,
                    TaskScheduler.Default);
        });
    }

    private static async Task InitializeBackgroundApiAsync()
    {
        try
        {
            if (Settings.Get(Settings.K.DisableApi))
                return;

            _backgroundApi = new BackgroundApiRunner();

            _backgroundApi.OnOpenWindow += (_, _) =>
                Dispatcher.UIThread.Post(() => MainWindow.Instance?.ShowFromTray());

            _backgroundApi.OnOpenUpdatesPage += (_, _) =>
                Dispatcher.UIThread.Post(() =>
                {
                    MainWindow.Instance?.NavigateShell(ShellPageType.Updates);
                    MainWindow.Instance?.ShowFromTray();
                });

            _backgroundApi.OnShowSharedPackage += (_, pkg) =>
                Dispatcher.UIThread.Post(() =>
                {
                    Logger.Info($"BackgroundApi: ShowSharedPackage {pkg.Key}/{pkg.Value}");
                    MainWindow.Instance?.ShowFromTray();
                    if (MainWindow.Instance?.Content is Views.MainShellView shell)
                    {
                        shell.OpenSharedPackage(pkg.Key, pkg.Value);
                    }
                });

            _backgroundApi.OnUpgradeAll += (_, _) =>
                Dispatcher.UIThread.Post(() => _ = AvaloniaPackageOperationHelper.UpdateAllAsync());

            _backgroundApi.OnUpgradeAllForManager += (_, managerName) =>
                Dispatcher.UIThread.Post(() =>
                    _ = AvaloniaPackageOperationHelper.UpdateAllForManagerAsync(managerName));

            _backgroundApi.OnUpgradePackage += (_, packageId) =>
                Dispatcher.UIThread.Post(() =>
                    _ = AvaloniaPackageOperationHelper.UpdateForIdAsync(packageId));

            await _backgroundApi.Start();
        }
        catch (Exception ex)
        {
            Logger.Error("Could not initialize Background API:");
            Logger.Error(ex);
        }
    }

    public static void StopBackgroundApi() => _backgroundApi?.Stop();
}

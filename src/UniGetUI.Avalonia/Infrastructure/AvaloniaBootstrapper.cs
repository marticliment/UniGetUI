using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine;

namespace UniGetUI.Avalonia.Infrastructure;

internal static class AvaloniaBootstrapper
{
    private static bool _hasStarted;

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
            InitializePackageEngineAsync(),
            SimulateShellWarmupAsync()
        );

        Logger.Info("Avalonia shell bootstrap completed");
    }

    private static Task InitializeSharedServicesAsync()
    {
        CoreTools.ReloadLanguageEngineInstance();
        return Task.CompletedTask;
    }

    private static Task InitializePackageEngineAsync()
    {
        return Task.Run(() =>
        {
            PEInterface.LoadLoaders();
            _ = Task.Run(PEInterface.LoadManagers);
        });
    }

    private static Task SimulateShellWarmupAsync()
    {
        return Task.Delay(150);
    }
}
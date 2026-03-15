using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Packages.Classes;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageOperations;

namespace UniGetUI.Avalonia.Infrastructure;

/// <summary>
/// Global registry of <see cref="AbstractOperation"/> instances for the Avalonia shell.
/// The operations panel in <see cref="UniGetUI.Avalonia.Views.MainShellView"/> binds to
/// <see cref="Operations"/> to show active, queued, or recently finished operations.
/// </summary>
public static class AvaloniaOperationRegistry
{
    public static readonly ObservableCollection<AbstractOperation> Operations = new();

    /// <summary>
    /// Register an operation. Must be called before <c>operation.MainThread()</c>.
    /// </summary>
    public static void Add(AbstractOperation op)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!Operations.Contains(op))
                Operations.Add(op);
        });

        op.OperationSucceeded += (_, _) =>
        {
            if (!Settings.Get(Settings.K.MaintainSuccessfulInstalls))
                _ = RemoveAfterDelayAsync(op, milliseconds: 4000);

            _ = Task.Run(() => AppendOperationHistory(op));

            _ = RunPostOperationChecksAsync();
            Dispatcher.UIThread.Post(UpdateTrayStatus);
        };

        op.OperationFailed += (_, _) =>
        {
            _ = Task.Run(() => AppendOperationHistory(op));
            Dispatcher.UIThread.Post(UpdateTrayStatus);
        };

        op.StatusChanged += (_, status) =>
        {
            if (status is OperationStatus.Canceled)
                _ = RemoveAfterDelayAsync(op, milliseconds: 2500);
            Dispatcher.UIThread.Post(UpdateTrayStatus);
        };
    }

    private static async Task RemoveAfterDelayAsync(AbstractOperation op, int milliseconds)
    {
        await Task.Delay(milliseconds);
        Dispatcher.UIThread.Post(() => Operations.Remove(op));
    }

    private static void UpdateTrayStatus()
    {
        if (Application.Current?.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime { MainWindow: UniGetUI.Avalonia.MainWindow mw })
            mw.UpdateSystemTrayStatus();
    }

    private static void AppendOperationHistory(AbstractOperation op)
    {
        try
        {
            var rawOutput = new List<string>
            {
                "                           ",
                "▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄",
            };
            foreach (var (text, _) in op.GetOutput())
                rawOutput.Add(text);

            var oldLines = Settings.GetValue(Settings.K.OperationHistory).Split('\n');
            if (oldLines.Length > 300)
                oldLines = oldLines.Take(300).ToArray();

            Settings.SetValue(
                Settings.K.OperationHistory,
                string.Join('\n', rawOutput.Concat(oldLines)));
        }
        catch (Exception ex)
        {
            Logger.Warn("Failed to write operation history");
            Logger.Warn(ex);
        }
    }

    private static async Task RunPostOperationChecksAsync()
    {
        // Let all remaining operations settle before making decisions
        await Task.Delay(500);

        bool anyStillRunning = Operations.Any(
            o => o.Status is OperationStatus.Running or OperationStatus.InQueue);

        // Clear UAC cache after the last operation in a batch finishes
        if (!anyStillRunning && Settings.Get(Settings.K.DoCacheAdminRightsForBatches))
        {
            Logger.Info("Clearing UAC prompt since there are no remaining operations");
            await CoreTools.ResetUACForCurrentProcess();
        }

        // Show desktop shortcut dialog if applicable
        if (!anyStillRunning
            && Settings.Get(Settings.K.AskToDeleteNewDesktopShortcuts)
            && DesktopShortcutsDatabase.GetUnknownShortcuts().Count > 0)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var window = new UniGetUI.Avalonia.Views.Pages.DesktopShortcutsWindow();
                if (Application.Current?.ApplicationLifetime
                        is IClassicDesktopStyleApplicationLifetime { MainWindow: Window mainWin })
                    _ = window.ShowDialog(mainWin);
                else
                    window.Show();
            });
        }
    }
}
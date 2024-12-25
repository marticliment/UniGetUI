using System.Collections.ObjectModel;
using System.Diagnostics;
using ExternalLibraries.Clipboard;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Classes.Packages.Classes;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.Pages.DialogPages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface
{
    public abstract partial class AbstractOperation : UserControl
    {
    }

    /*

    public AbstractOperation(bool IgnoreParallelInstalls = false)
    {
        Status = OperationStatus.InQueue;

        ActionButton.Click += ActionButtonClicked;
        OutputViewewBlock.Click += (_, _) =>
        {
            OpenLiveViewDialog();
        };
    }

    public async void OpenLiveViewDialog()
    {
        OutputDialog.XamlRoot = XamlRoot;
        LiveOutputTextBlock.Blocks.Clear();
        Paragraph p = new()
        {
            LineHeight = 4.8
        };
        foreach (OutputLine line in ProcessOutput)
        {
            if (line.Type is OutputLine.LineType.STDOUT)
                p.Inlines.Add(new Run { Text = line.Contents + "\x0a" });
            else if (line.Type is OutputLine.LineType.Header)
                // TODO: Theme-aware colors
                p.Inlines.Add(new Run { Text = line.Contents + "\x0a", Foreground = new SolidColorBrush(Colors.Azure)});
            else
                p.Inlines.Add(new Run { Text = line.Contents + "\x0a", Foreground = new SolidColorBrush(Colors.Red)});
        }
        LiveOutputTextBlock.Blocks.Add(p);
        IsDialogOpen = true;

        if (await MainApp.Instance.MainWindow.ShowDialogAsync(OutputDialog) == ContentDialogResult.Secondary)
        {
            LiveOutputScrollBar.ScrollToVerticalOffset(LiveOutputScrollBar.ScrollableHeight);
            WindowsClipboard.SetText(string.Join('\n', ProcessOutput.ToArray()));
        }
        IsDialogOpen = false;
    }




    protected async Task MainThread()
    {
        try
        {

            if (Status is OperationStatus.Canceled)
            {
                return; // If the operation was canceled, do nothing.
            }

            MainApp.Instance.TooltipStatus.OperationsInProgress += 1;

            LineInfoText = CoreTools.Translate("Launching subprocess...");
            ProcessStartInfo startInfo = new()
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardInputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };

            Process = new Process();

            Process.StartInfo = await BuildProcessInstance(startInfo);
            // Process.StartInfo = CoreTools.UpdateEnvironmentVariables(Process.StartInfo);

            foreach (string infoLine in GenerateProcessLogHeader())
            {
                ProcessOutput.Add(new(infoLine, OutputLine.LineType.Header));
            }

            ProcessOutput.Add(new("Process Executable     : " + Process.StartInfo.FileName, OutputLine.LineType.Header));
            ProcessOutput.Add(new("Process Call Arguments : " + Process.StartInfo.Arguments, OutputLine.LineType.Header));
            ProcessOutput.Add(new("Working Directory      : " + Process.StartInfo.WorkingDirectory, OutputLine.LineType.Header));
            ProcessOutput.Add(new("Process Start Time     : " + DateTime.Now, OutputLine.LineType.Header));

            Process.OutputDataReceived += (_, e) => DispatcherQueue.TryEnqueue(async () =>
            {
                if (e.Data?.Trim() is string line && line != String.Empty)
                {
                    if (line.Contains("For the question below") ||
                        line.Contains("Would remove:")) // Mitigate chocolatey timeouts
                    {
                        await Process.StandardInput.WriteLineAsync("");
                    }

                    if (Status is not OperationStatus.Canceled)
                    {
                        LineInfoText = line;
                        if(line.Length > 3) ProcessOutput.Add(new(line, OutputLine.LineType.STDOUT));
                    }
                }
            });

            Process.ErrorDataReceived += (_, e) => DispatcherQueue.TryEnqueue(async () =>
            {
                if (e.Data?.Trim() is string line && line != String.Empty)
                {
                    if (Status is not OperationStatus.Canceled)
                    {
                        LineInfoText = line;
                        if(line.Length > 3) ProcessOutput.Add(new(line, OutputLine.LineType.STDERR));
                    }
                }
            });


            Process.Start();
            PostProcessStartAction();

            Process.BeginOutputReadLine();
            Process.BeginErrorReadLine();

            Status = OperationStatus.Running;

            await Process.WaitForExitAsync();
            PostProcessEndAction();

            ProcessOutput.Add(new("Process Exit Code      : " + Process.ExitCode, OutputLine.LineType.Header));
            ProcessOutput.Add(new("Process End Time       : " + DateTime.Now, OutputLine.LineType.Header));

            AfterFinshAction postAction = AfterFinshAction.ManualClose;

            OperationVeredict OperationVeredict = await GetProcessVeredict(Process.ExitCode, RawProcessOutput);


            if (Status is not OperationStatus.Canceled)
            {
                switch (OperationVeredict)
                {
                    case OperationVeredict.Success or OperationVeredict.RestartRequired:
                        Status = OperationStatus.Succeeded;
                        postAction = await HandleSuccess();
                        RemoveFromQueue();
                        break;

                    case OperationVeredict.Canceled:
                        Status = OperationStatus.Canceled;
                        RemoveFromQueue();
                        postAction = AfterFinshAction.ManualClose;
                        await HandleCancelation();
                        break;

                    case OperationVeredict.AutoRetry:
                        Status = OperationStatus.InQueue;
                        postAction = AfterFinshAction.Retry;
                        break;

                    case OperationVeredict.Failure:
                        Status = OperationStatus.Failed;
                        RemoveFromQueue();
                        MainApp.Instance.TooltipStatus.ErrorsOccurred += 1;
                        postAction = await HandleFailure();
                        MainApp.Instance.TooltipStatus.ErrorsOccurred -= 1;
                        break;

                    default:
                        throw new ArgumentException($"Unexpected OperationVeredict {OperationVeredict}");
                }
            }


            switch (postAction)
            {
                case AfterFinshAction.TimeoutClose:
                    if (MainApp.Operations._operationList.Count == 0)
                    {
                        if (Settings.Get("DoCacheAdminRightsForBatches"))
                        {
                            await CoreTools.ResetUACForCurrentProcess();
                        }
                    }

                    await Task.Delay(5000);
                    if (!Settings.Get("MaintainSuccessfulInstalls"))
                    {
                        _ = Close();
                    }

                    break;

                case AfterFinshAction.ManualClose:
                    if (MainApp.Operations._operationList.Count == 0)
                    {
                        if (Settings.Get("DoCacheAdminRightsForBatches"))
                        {
                            await CoreTools.ResetUACForCurrentProcess();
                        }
                    }

                    break;

                case AfterFinshAction.Retry:
                    Retry();
                    break;
            }

            if (MainApp.Operations._operationList.Count == 0 && DesktopShortcutsDatabase.GetUnknownShortcuts().Any() && Settings.Get("AskToDeleteNewDesktopShortcuts"))
            {
                await DialogHelper.HandleNewDesktopShortcuts();
            }

            List<string> rawOutput = RawProcessOutput.ToList();

            rawOutput.Insert(0, "                           ");
            rawOutput.Insert(0, "▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄");
            rawOutput.Add("");
            rawOutput.Add("");
            rawOutput.Add("");

            string[] oldHistory = Settings.GetValue("OperationHistory").Split("\n");

            if (oldHistory.Length > 1000)
            {
                oldHistory = oldHistory.Take(1000).ToArray();
            }

            List<string> newHistory = [.. rawOutput, .. oldHistory];

            Settings.SetValue("OperationHistory", string.Join('\n', newHistory).Replace(" | ", " ║ "));
        }
        catch (Exception e)
        {
            Logger.Error("Operation crashed: ");
            Logger.Error(e);
            LineInfoText = CoreTools.Translate("An unexpected error occurred:") + " " + e.Message;
            RemoveFromQueue();
            try { Status = OperationStatus.Failed; } catch { }
        }
        MainApp.Instance.TooltipStatus.OperationsInProgress -= 1;
    }
    protected async Task Close()
    {
        while (IsDialogOpen)
        {
            await Task.Delay(1000);
        }

        RemoveFromQueue();
        PEInterface.OperationList.Remove(this);
    }

    protected void Retry()
    {
        AddToQueue_Priority();
        LineInfoText = CoreTools.Translate("Retrying, please wait...");
        ProcessOutput.Clear();
        Status = OperationStatus.InQueue;
        _ = MainThread();
    }

}*/
}

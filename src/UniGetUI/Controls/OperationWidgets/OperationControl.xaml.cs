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

    protected async Task MainThread()
    {
        try
        {

            MainApp.Instance.TooltipStatus.OperationsInProgress += 1;


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


            if (Status is not OperationStatus.Canceled)
            {
                switch (OperationVeredict)
                {

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
}*/
}

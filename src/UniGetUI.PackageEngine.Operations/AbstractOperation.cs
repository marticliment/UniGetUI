using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Packages.Classes;
using UniGetUI.PackageEngine.Enums;
using Windows.Devices.Bluetooth.Advertisement;

namespace UniGetUI.PackageOperations;

public abstract class AbstractOperation
{
    public readonly static List<AbstractOperation> OperationQueue = new();

    protected enum FinishAction
    {
        Retry,
        Success,
        Error,
        Canceled,
    }

    public enum LineType
    {
        Debug,
        Progress,
        StdOUT,
        StdERR
    }

    private OperationStatus _status = OperationStatus.InQueue;
    public OperationStatus Status
    {
        get => _status;
        set { _status = value; StatusChanged(); }
    }

    protected bool QUEUE_ENABLED;

    public AbstractOperation(bool queue_enabled)
    {
        QUEUE_ENABLED = queue_enabled;
        Status = OperationStatus.InQueue;
        _ = MainThread();
    }

    protected abstract Task CancelRequested();
    protected abstract Task StatusChanged();

    public async void Cancel()
    {
        switch (_status)
        {
            case OperationStatus.Canceled:
                break;
            case OperationStatus.Failed:
                break;
            case OperationStatus.Running:
                await CancelRequested();
                Status = OperationStatus.Canceled;
                break;
            case OperationStatus.InQueue:
                OperationQueue.Remove(this);
                Status = OperationStatus.Canceled;
                break;
            case OperationStatus.Succeeded:
                break;
        }
    }

    protected abstract string Line(string line, LineType type);

    private async Task MainThread()
    {
        if (OperationQueue.Contains(this))
            throw new InvalidOperationException("This operation was already on the queue");

        Status = OperationStatus.InQueue;

        // BEGIN QUEUE HANDLER
        if (QUEUE_ENABLED)
        {
            OperationQueue.Add(this);
            int lastPos = -1;

            while (OperationQueue.First() != this)
            {
                int pos = OperationQueue.IndexOf(this);
                if (pos != lastPos)
                {
                    lastPos = pos;
                    Line(CoreTools.Translate("Operation on queue (position {0})...", pos), LineType.Progress);
                }
                await Task.Delay(100);
            }
        }
        // END QUEUE HANDLER

        // BEGIN ACTUAL OPERATION
        FinishAction result;
        Line(CoreTools.Translate("Starting operation..."), LineType.Progress);
        Status = OperationStatus.Running;
        await PreOperation();
        do
        {
            try
            {
                result = await PerformOperation();
            }
            catch (Exception e)
            {
                result = FinishAction.Error;
                Logger.Error(e);
                foreach (string l in e.ToString().Split("\n")) Line(l, LineType.StdERR);
            }
        }
        while (result == FinishAction.Retry);
        OperationQueue.Remove(this);
        // END OPERATION

        await PostOperation();

        if (result == FinishAction.Success)
        {
            Status = OperationStatus.Succeeded;
            await HandleSuccess();
        }
        else if (result == FinishAction.Error)
        {
            Status = OperationStatus.Failed;
            await HandleFaliure();
        }
        else if (result == FinishAction.Canceled)
        {
            Status = OperationStatus.Canceled;
        }
    }


    public void SkipQueue()
    {
        if (Status != OperationStatus.InQueue) return;
        OperationQueue.Remove(this);
        OperationQueue.Insert(0, this);
    }

    protected abstract Task<FinishAction> PerformOperation();
    protected abstract Task PreOperation();
    protected abstract Task PostOperation();
    protected abstract Task HandleSuccess();
    protected abstract Task HandleFaliure();

    /*protected async Task MainThread_()
    {
        try
        {
            LiveLine = CoreTools.Translate("Launching subprocess...");
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

            foreach (string infoLine in GenerateProcessLogHeader())
            {
                ProcessOutput.Add(new(infoLine, OutputLine.LineType.Debug));
            }

            ProcessOutput.Add(new("Process Executable     : " + Process.StartInfo.FileName, OutputLine.LineType.Debug));
            ProcessOutput.Add(new("Process Call Arguments : " + Process.StartInfo.Arguments, OutputLine.LineType.Debug));
            ProcessOutput.Add(new("Working Directory      : " + Process.StartInfo.WorkingDirectory, OutputLine.LineType.Debug));
            ProcessOutput.Add(new("Process Start Time     : " + DateTime.Now, OutputLine.LineType.Debug));

            Process.OutputDataReceived += async (_, e) =>
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
                        LiveLine = line;
                        if (line.Length > 3) ProcessOutput.Add(new(line, OutputLine.LineType.StdOUT));
                    }
                }
            };

            Process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data?.Trim() is string line && line != String.Empty)
                {
                    if (Status is not OperationStatus.Canceled)
                    {
                        LiveLine = line;
                        if (line.Length > 3) ProcessOutput.Add(new(line, OutputLine.LineType.StdERR));
                    }
                }
            };

            Status = OperationStatus.Running;
            Process.Start();
            PostProcessStartAction();

            Process.BeginOutputReadLine();
            Process.BeginErrorReadLine();

            await Process.WaitForExitAsync();
            PostProcessEndAction();

            ProcessOutput.Add(new("Process Exit Code      : " + Process.ExitCode, OutputLine.LineType.Debug));
            ProcessOutput.Add(new("Process End Time       : " + DateTime.Now, OutputLine.LineType.Debug));

            FinishAction postAction = FinishAction.KeepVisible;

            OperationVeredict OperationVeredict = await GetProcessVeredict(Process.ExitCode, RawProcessOutput);


            if (Status is not OperationStatus.Canceled)
            {
                switch (OperationVeredict)
                {
                    case OperationVeredict.Succeeded or OperationVeredict.RestartRequired:
                        Status = OperationStatus.Succeeded;
                        postAction = await HandleSuccess();
                        RemoveFromQueue();
                        break;

                    case OperationVeredict.Canceled:
                        Status = OperationStatus.Canceled;
                        RemoveFromQueue();
                        postAction = FinishAction.KeepVisible;
                        await HandleCancelation();
                        break;

                    case OperationVeredict.AutoRetry:
                        Status = OperationStatus.InQueue;
                        postAction = FinishAction.AutoRestart;
                        break;

                    case OperationVeredict.Failed:
                        Status = OperationStatus.Failed;
                        RemoveFromQueue();
                        // MainApp.Instance.TooltipStatus.ErrorsOccurred += 1;
                        postAction = await HandleFailure();
                        // MainApp.Instance.TooltipStatus.ErrorsOccurred -= 1;
                        break;

                    default:
                        throw new ArgumentException($"Unexpected OperationVeredict {OperationVeredict}");
                }
            }


            switch (postAction)
            {
                case FinishAction.AutoHide:
                    if (Opera.Count == 0)
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

                case FinishAction.KeepVisible:
                    if (MainApp.Instance.OperationQueue.Count == 0)
                    {
                        if (Settings.Get("DoCacheAdminRightsForBatches"))
                        {
                            await CoreTools.ResetUACForCurrentProcess();
                        }
                    }

                    break;

                case FinishAction.AutoRestart:
                    Retry();
                    break;
            }

            if (MainApp.Instance.OperationQueue.Count == 0 && DesktopShortcutsDatabase.GetUnknownShortcuts().Any() && Settings.Get("AskToDeleteNewDesktopShortcuts"))
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
            LiveLine = CoreTools.Translate("An unexpected error occurred:") + " " + e.Message;
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
    }*/
}

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection.Metadata;
using ExternalLibraries.Clipboard;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Enums;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.PackageEngine.Operations
{
    public abstract partial class AbstractOperation : UserControl
    {
        protected enum AfterFinshAction
        {
            TimeoutClose,
            ManualClose,
            Retry,
        }

        private enum WidgetLayout
        {
            Default,
            Compact,
        }

        private OperationStatus __status = OperationStatus.Pending;
        private bool IsDialogOpen;

        private WidgetLayout __layout_mode;
        private WidgetLayout LayoutMode
        {
            set
            {
                if (value == WidgetLayout.Compact)
                {
                    Grid.SetColumn(OutputViewewBlock, 0);
                    Grid.SetColumnSpan(OutputViewewBlock, 4);
                    Grid.SetRow(OutputViewewBlock, 1);
                    Grid.SetColumn(ProgressIndicator, 0);
                    Grid.SetColumnSpan(ProgressIndicator, 4);
                    Grid.SetRow(ProgressIndicator, 1);
                    if (MainGrid.RowDefinitions.Count < 2)
                    {
                        MainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    }
                }
                else
                {
                    Grid.SetColumn(OutputViewewBlock, 2);
                    Grid.SetColumnSpan(OutputViewewBlock, 1);
                    Grid.SetRow(OutputViewewBlock, 0);
                    Grid.SetColumn(ProgressIndicator, 2);
                    Grid.SetColumnSpan(ProgressIndicator, 1);
                    Grid.SetRow(ProgressIndicator, 0);
                    if (MainGrid.RowDefinitions.Count >= 2)
                    {
                        MainGrid.RowDefinitions.RemoveAt(1);
                    }
                }
                __layout_mode = value;
            }
            get { return __layout_mode; }
        }

        protected string ButtonText
        {
            set => ActionButton.Content = value;
        }
        protected string LineInfoText
        {
            set => OutputViewewBlock.Content = value;
        }
        protected Uri IconSource
        {
            set => PackageIcon.Source = new BitmapImage(value);
        }
        protected string OperationTitle
        {
            set => InfoTextBlock.Text = value;
        }

#pragma warning disable CS0067
        protected event EventHandler<OperationCanceledEventArgs>? CancelRequested;
        protected event EventHandler<OperationCanceledEventArgs>? CloseRequested;
#pragma warning restore CS0067
        protected Process Process = new();
        protected ObservableCollection<string> ProcessOutput = [];

        private readonly ContentDialog OutputDialog = new();
        private readonly ScrollViewer LiveOutputScrollBar = new();
        private readonly RichTextBlock LiveOutputTextBlock = new();

        public OperationStatus Status
        {
            get => __status;
            set
            {
                MainGrid.RequestedTheme = MainApp.Instance.MainWindow.ContentRoot.RequestedTheme;
                __status = value;
                switch (__status)
                {
                    case OperationStatus.Pending:
                        ProgressIndicator.IsIndeterminate = false;
                        ProgressIndicator.Foreground = (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBrush"];
                        MainGrid.Background = (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBackgroundBrush"];
                        ButtonText = CoreTools.Translate("Cancel");
                        break;

                    case OperationStatus.Running:
                        ProgressIndicator.IsIndeterminate = true;
                        ProgressIndicator.Foreground = (SolidColorBrush)Application.Current.Resources["SystemFillColorAttentionBrush"];
                        MainGrid.Background = (SolidColorBrush)Application.Current.Resources["SystemFillColorAttentionBackgroundBrush"];
                        ButtonText = CoreTools.Translate("Cancel");
                        break;

                    case OperationStatus.Succeeded:
                        ProgressIndicator.Foreground = (SolidColorBrush)Application.Current.Resources["SystemFillColorSuccessBrush"];
                        MainGrid.Background = (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBackgroundBrush"];
                        ProgressIndicator.IsIndeterminate = false;
                        ButtonText = CoreTools.Translate("Close");
                        break;

                    case OperationStatus.Failed:
                        ProgressIndicator.IsIndeterminate = false;
                        ProgressIndicator.Foreground = (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                        MainGrid.Background = (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBackgroundBrush"];
                        ButtonText = CoreTools.Translate("Close");
                        break;

                    case OperationStatus.Canceled:
                        ProgressIndicator.IsIndeterminate = false;
                        ProgressIndicator.Foreground = (SolidColorBrush)Application.Current.Resources["SystemFillColorCautionBrush"];
                        MainGrid.Background = (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBackgroundBrush"];
                        ButtonText = CoreTools.Translate("Close");
                        LineInfoText = CoreTools.Translate("Operation canceled by user");
                        break;
                }
            }
        }
        protected bool IGNORE_PARALLEL_OPERATION_SETTINGS;
        public AbstractOperation(bool IgnoreParallelInstalls = false)
        {
            IGNORE_PARALLEL_OPERATION_SETTINGS = IgnoreParallelInstalls;

            InitializeComponent();

            OutputDialog = new ContentDialog
            {
                Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
                XamlRoot = XamlRoot
            };
            OutputDialog.Resources["ContentDialogMaxWidth"] = 1200;
            OutputDialog.Resources["ContentDialogMaxHeight"] = 1000;

            LiveOutputTextBlock = new RichTextBlock
            {
                Margin = new Thickness(8),
                FontFamily = new FontFamily("Consolas")
            };

            LiveOutputScrollBar = new ScrollViewer
            {
                CornerRadius = new CornerRadius(6),
                Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"],
                Height = 400,
                Width = 600,
                Content = LiveOutputTextBlock
            };

            OutputDialog.Title = CoreTools.Translate("Live output");
            OutputDialog.CloseButtonText = CoreTools.Translate("Close");

            OutputDialog.SizeChanged += (s, e) =>
            {
                if (!IsDialogOpen)
                {
                    return;
                }

                LiveOutputScrollBar.MinWidth = MainApp.Instance.MainWindow.NavigationPage.ActualWidth - 400;
                LiveOutputScrollBar.MinHeight = MainApp.Instance.MainWindow.NavigationPage.ActualHeight - 200;
            };

            OutputDialog.Content = LiveOutputScrollBar;

            ProcessOutput.CollectionChanged += async (s, e) =>
            {
                if (!IsDialogOpen)
                {
                    return;
                }

                LiveOutputTextBlock.Blocks.Clear();
                Paragraph p = new();
                foreach (string line in ProcessOutput)
                {
                    if (line.Contains("  | "))
                    {
                        p.Inlines.Add(new Run { Text = line.Replace(" | ", "").Trim() + "\x0a" });
                    }
                }
                LiveOutputTextBlock.Blocks.Add(p);
                await Task.Delay(100);
                LiveOutputScrollBar.ScrollToVerticalOffset(LiveOutputScrollBar.ScrollableHeight);
            };

            Status = OperationStatus.Pending;

            ActionButton.Click += ActionButtonClicked;
            OutputViewewBlock.Click += (s, e) =>
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
            foreach (string line in ProcessOutput)
            {
                if (Status != OperationStatus.Failed)
                {
                    if (line.Contains("  | "))
                    {
                        p.Inlines.Add(new Run { Text = line.Replace(" | ", "").Trim() + "\x0a" });
                    }
                }
                else
                {
                    p.Inlines.Add(new Run { Text = line + "\x0a" });
                }
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

        public void ActionButtonClicked(object sender, RoutedEventArgs args)
        {
            if (Status is OperationStatus.Pending or OperationStatus.Running)
            {
                CancelButtonClicked();
            }
            else
            {
                CloseButtonClicked();
            }
        }

        protected void RemoveFromQueue()
        {
            while (MainApp.Instance.OperationQueue.IndexOf(this) != -1)
            {
                MainApp.Instance.OperationQueue.Remove(this);
            }
        }
        protected void AddToQueue()
        {
            if (!MainApp.Instance.OperationQueue.Contains(this))
            {
                MainApp.Instance.OperationQueue.Add(this);
            }
        }

        public async void CancelButtonClicked()
        {
            RemoveFromQueue();

            if (Status is OperationStatus.Running)
            {
                Process.Kill();
            }

            await HandleCancelation();
            Status = OperationStatus.Canceled;
        }

        public void CloseButtonClicked()
        {
            _ = Close();
        }

        protected void AddToQueue_Priority()
        {
            MainApp.Instance.OperationQueue.Insert(0, this);
        }

        protected virtual async Task WaitForAvailability()
        {
            AddToQueue();
            int currentIndex = -2;
            int oldIndex = -1;
            while (currentIndex != 0)
            {
                if (Status is OperationStatus.Canceled)
                {
                    RemoveFromQueue();
                    return;
                }

                currentIndex = MainApp.Instance.OperationQueue.IndexOf(this);
                if (currentIndex != oldIndex)
                {
                    LineInfoText = CoreTools.Translate("Operation on queue (position {0})...", currentIndex);
                    oldIndex = currentIndex;
                }
                await Task.Delay(100);
            }
        }
        protected async Task PreMainThread()
        {
            Status = OperationStatus.Pending;
            await WaitForAvailability();
            await MainThread();
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

                Process = new Process { StartInfo = await BuildProcessInstance(startInfo) };

                foreach (string infoLine in GenerateProcessLogHeader())
                {
                    ProcessOutput.Add(infoLine);
                }

                ProcessOutput.Add("Process Executable     : " + Process.StartInfo.FileName);
                ProcessOutput.Add("Process Call Arguments : " + Process.StartInfo.Arguments);
                ProcessOutput.Add("Working Directory      : " + Process.StartInfo.WorkingDirectory);
                ProcessOutput.Add("Process Start Time     : " + DateTime.Now);

                Process.Start();
                Status = OperationStatus.Running;

                string? line;
                while ((line = await Process.StandardOutput.ReadLineAsync()) != null)
                {
                    if (line.Trim() != "")
                    {
                        if (line.Contains("For the question below") ||
                            line.Contains("Would remove:")) // Mitigate chocolatey timeouts
                        {
                            await Process.StandardInput.WriteLineAsync("");
                        }

                        if (Status is not OperationStatus.Canceled)
                        {
                            LineInfoText = line.Trim();

                            if (line.Length > 5 || ProcessOutput.Count == 0)
                            {
                                ProcessOutput.Add("    | " + line);
                            }
                            else
                            {
                                ProcessOutput[^1] = "    | " + line;
                            }
                        }
                    }
                }

                foreach (string errorLine in (await Process.StandardError.ReadToEndAsync()).Split('\n'))
                {
                    if (errorLine.Trim() != "")
                    {
                        ProcessOutput.Add("ERR | " + errorLine);
                    }
                }

                await Process.WaitForExitAsync();

                ProcessOutput.Add("Process Exit Code      : " + Process.ExitCode);
                ProcessOutput.Add("Process End Time       : " + DateTime.Now);

                AfterFinshAction postAction = AfterFinshAction.ManualClose;

                OperationVeredict OperationVeredict = await GetProcessVeredict(Process.ExitCode, ProcessOutput.ToArray());


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
                            postAction = AfterFinshAction.ManualClose;
                            await HandleCancelation();
                            break;

                        case OperationVeredict.AutoRetry:
                            Status = OperationStatus.Pending;
                            postAction = AfterFinshAction.Retry;
                            break;

                        case OperationVeredict.Failed:
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
                        if (MainApp.Instance.OperationQueue.Count == 0)
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
                        if (MainApp.Instance.OperationQueue.Count == 0)
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

                ProcessOutput.Insert(0, "                           ");
                ProcessOutput.Insert(0, "▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄");
                ProcessOutput.Add("");
                ProcessOutput.Add("");
                ProcessOutput.Add("");

                string[] oldHistory = Settings.GetValue("OperationHistory").Split("\n");

                if (oldHistory.Length > 1000)
                {
                    oldHistory = oldHistory.Take(1000).ToArray();
                }

                List<string> newHistory = [.. ProcessOutput, .. oldHistory];

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
            if (MainApp.Instance.MainWindow.NavigationPage.OperationStackPanel.Children.Contains(this))
            {
                MainApp.Instance.MainWindow.NavigationPage.OperationStackPanel.Children.Remove(this);
            }
        }

        protected abstract void Initialize();
        protected abstract Task<ProcessStartInfo> BuildProcessInstance(ProcessStartInfo startInfo);
        protected abstract Task<OperationVeredict> GetProcessVeredict(int ReturnCode, string[] Output);
        protected abstract Task<AfterFinshAction> HandleFailure();
        protected abstract Task<AfterFinshAction> HandleSuccess();
        protected abstract Task HandleCancelation();
        protected abstract string[] GenerateProcessLogHeader();

        protected void Retry()
        {
            AddToQueue_Priority();
            LineInfoText = CoreTools.Translate("Retrying, please wait...");
            ProcessOutput.Clear();
            Status = OperationStatus.Pending;
            _ = MainThread();
        }

        protected void MainProcedure()
        {
            Initialize();
            _ = PreMainThread();
        }
        private void ResizeEvent(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width < 500)
            {
                if (LayoutMode != WidgetLayout.Compact)
                {
                    LayoutMode = WidgetLayout.Compact;
                }
            }
            else
            {
                if (LayoutMode != WidgetLayout.Default)
                {
                    LayoutMode = WidgetLayout.Default;
                }
            }

        }
    }
}

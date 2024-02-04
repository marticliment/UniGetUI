using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.PackageEngine
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

        public static AppTools bindings = AppTools.Instance;

        private string __button_text;
        private string __line_info_text = "Please wait...";
        private Uri __icon_source = new Uri("ms-appx://wingetui/resources/package_color.png");
        private string __operation_description = "$Package Install";
        private Color? __progressbar_color = null;
        private OperationStatus __status = OperationStatus.Pending;
        private bool IsDialogOpen = false;

        private WidgetLayout __layout_mode;
        private WidgetLayout LayoutMode
        {
            set
            {
                if(value == WidgetLayout.Compact)
                {
                    Grid.SetColumn(OutputViewewBlock, 0);
                    Grid.SetColumnSpan(OutputViewewBlock, 4);
                    Grid.SetRow(OutputViewewBlock, 1);
                    Grid.SetColumn(ProgressIndicator, 0);
                    Grid.SetColumnSpan(ProgressIndicator, 4);
                    Grid.SetRow(ProgressIndicator, 1);
                    if (MainGrid.RowDefinitions.Count < 2)
                        MainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto});
                } else
                {
                    Grid.SetColumn(OutputViewewBlock, 2);
                    Grid.SetColumnSpan(OutputViewewBlock, 1);
                    Grid.SetRow(OutputViewewBlock, 0);
                    Grid.SetColumn(ProgressIndicator, 2);
                    Grid.SetColumnSpan(ProgressIndicator, 1);
                    Grid.SetRow(ProgressIndicator, 0);
                    if (MainGrid.RowDefinitions.Count >= 2)
                        MainGrid.RowDefinitions.RemoveAt(1);
                }
                __layout_mode = value;
            }
            get { return __layout_mode; }
        }

        protected string ButtonText
        {
            get { return __button_text; }
            set { __button_text = value; if (ActionButton != null) ActionButton.Content = __button_text; }
        }
        protected string LineInfoText
        {
            get { return __line_info_text; }
            set { __line_info_text = value; if (OutputViewewBlock != null) OutputViewewBlock.Content = __line_info_text; }
        }
        protected Uri IconSource
        {
            get { return __icon_source; }
            set { __icon_source = value; if (PackageIcon != null) PackageIcon.Source = new BitmapImage(__icon_source); }
        }
        protected string OperationTitle
        {
            get { return __operation_description; }
            set { __operation_description = value; if (InfoTextBlock != null) InfoTextBlock.Text = __operation_description; }
        }
        protected Color? ProgressBarColor
        {
            get { return __progressbar_color; }
            set { __progressbar_color = value; if (ProgressIndicator != null) ProgressIndicator.Foreground = (__progressbar_color != null) ? new SolidColorBrush((Color)__progressbar_color) : null; }
        }

        protected event EventHandler<OperationCancelledEventArgs> CancelRequested;
        protected event EventHandler<OperationCancelledEventArgs> CloseRequested;
        protected Process Process;
        protected ObservableCollection<string> ProcessOutput = new ObservableCollection<string>();

        public OperationStatus Status
        {
            get { return __status; }
            set
            {
                __status = value;
                switch (__status)
                {
                    case OperationStatus.Pending:
                        ProgressIndicator.IsIndeterminate = true;
                        ProgressBarColor = Colors.Gray;
                        ButtonText = bindings.Translate("Cancel");
                        break;

                    case OperationStatus.Running:
                        ProgressIndicator.IsIndeterminate = true;
                        ProgressBarColor = new Windows.UI.ViewManagement.UISettings().GetColorValue(Windows.UI.ViewManagement.UIColorType.AccentLight1);
                        ButtonText = bindings.Translate("Cancel");
                        break;

                    case OperationStatus.Succeeded:
                        ProgressBarColor = CommunityToolkit.WinUI.Helpers.ColorHelper.ToColor("#11945a");
                        ProgressIndicator.IsIndeterminate = false;
                        ButtonText = bindings.Translate("Close");
                        break;

                    case OperationStatus.Failed:
                        ProgressIndicator.IsIndeterminate = false;
                        ProgressBarColor = CommunityToolkit.WinUI.Helpers.ColorHelper.ToColor("#fe890b");
                        ButtonText = bindings.Translate("Close");
                        break;

                    case OperationStatus.Cancelled:
                        ProgressIndicator.IsIndeterminate = false;
                        ProgressBarColor = CommunityToolkit.WinUI.Helpers.ColorHelper.ToColor("#fec10b");
                        ButtonText = bindings.Translate("Close");
                        break;
                }
            }
        }
        public AbstractOperation()
        {
            this.InitializeComponent();

            OutputDialog.Title = bindings.Translate("Live output");
            OutputDialog.CloseButtonText = bindings.Translate("Close");
            OutputDialog.SecondaryButtonText = bindings.Translate("Copy and close");
            ProcessOutput.CollectionChanged += async (s, e) => {
                LiveOutputTextBlock.Blocks.Clear();
                Paragraph p = new();
                foreach (var line in ProcessOutput)
                {
                    if(line.Contains("  | "))
                    p.Inlines.Add(new Run() { Text = line.Replace(" | ", "").Trim() + "\x0a"});
                }
                LiveOutputTextBlock.Blocks.Add(p);
                await Task.Delay(100);
                LiveOutputScrollBar.ScrollToVerticalOffset(LiveOutputScrollBar.ScrollableHeight);
            };
            
            Status = OperationStatus.Pending;

            ActionButton.Click += ActionButtonClicked;
            OutputViewewBlock.Click += async (s, e) => { 
                OutputDialog.XamlRoot = this.XamlRoot;
                IsDialogOpen = true;
                if (await OutputDialog.ShowAsync() == ContentDialogResult.Secondary)
                {
                    Clipboard.WindowsClipboard.SetText(string.Join('\n', ProcessOutput.ToArray()));
                }
                IsDialogOpen = false;
            };
        }


        public void ActionButtonClicked(object sender, RoutedEventArgs args)
        {
            if (Status == OperationStatus.Pending || Status == OperationStatus.Running)
            {
                CancelButtonClicked(Status);
            }
            else
                CloseButtonClicked(Status);
        }

        private void RemoveFromQueue()
        {
            int Index = bindings.OperationQueue.IndexOf(this);
            if (Index != -1)
                bindings.OperationQueue.Remove(this);
        }
        private void AddToQueue()
        {
            if (!bindings.OperationQueue.Contains(this))
                bindings.OperationQueue.Add(this);
        }

        public void CancelButtonClicked(OperationStatus OldStatus)
        {
            RemoveFromQueue();
            this.Status = OperationStatus.Cancelled;
            LineInfoText = bindings.Translate("Operation cancelled");
            if (OldStatus == OperationStatus.Running)
            {
                Process.Kill();
                ProcessOutput.Add("Operation was cancelled by the user!");
            }
        }

        public void CloseButtonClicked(OperationStatus OldStatus)
        {
            _ = Close();
        }

        private void AddToQueue_Priority()
        {
            bindings.OperationQueue.Insert(0, this);
        }

        private async Task WaitForAvailability()
        {
            AddToQueue();
            int currentIndex = -2;
            int oldIndex = -1;
            while (currentIndex != 0)
            {
                if (Status == OperationStatus.Cancelled)
                    return; // If th operation has been cancelled

                currentIndex = bindings.OperationQueue.IndexOf(this);
                if (currentIndex != oldIndex)
                {
                    LineInfoText = bindings.Translate("Operation on queue (position {0})...").Replace("{0}", currentIndex.ToString());
                    oldIndex = currentIndex;
                }
                await Task.Delay(100);
            }
        }
        private async Task PreMainThread()
        {
            this.Status = OperationStatus.Pending;
            await WaitForAvailability();
            await MainThread();
        }
        private async Task MainThread()
        {
            try
            {
                bindings.TooltipStatus.OperationsInProgress = bindings.TooltipStatus.OperationsInProgress + 1;

                this.Status = OperationStatus.Running;
                LineInfoText = bindings.Translate("Launching subprocess...");
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;

                Process = BuildProcessInstance(startInfo);

                foreach (string infoLine in GenerateProcessLogHeader())
                    ProcessOutput.Add(infoLine);

                ProcessOutput.Add("Process Executable     : " + Process.StartInfo.FileName);
                ProcessOutput.Add("Process Call Arguments : " + Process.StartInfo.Arguments);
                ProcessOutput.Add("Working Directory      : " + Process.StartInfo.WorkingDirectory);
                ProcessOutput.Add("Process Start Time     : " + DateTime.Now.ToString());

                Process.Start();

                string line;
                while ((line = await Process.StandardOutput.ReadLineAsync()) != null)
                {
                    if (line.Trim() != "")
                    {
                        if(line.Contains("For the question below") || line.Contains("Would remove:")) // Mitigate chocolatey timeouts
                            Process.StandardInput.WriteLine("");

                        LineInfoText = line.Trim();
                        if (line.Length > 5 || ProcessOutput.Count == 0)
                            ProcessOutput.Add("    | " + line);
                        else
                            ProcessOutput[^1] = "    | " + line;
                    }
                }

                foreach (var errorLine in (await Process.StandardError.ReadToEndAsync()).Split(' '))
                    ProcessOutput.Add("ERR | " + errorLine);

                await Process.WaitForExitAsync();

                ProcessOutput.Add("Process Exit Code      : " + Process.ExitCode.ToString());
                ProcessOutput.Add("Process End Time       : " + DateTime.Now.ToString());

                if (Status == OperationStatus.Cancelled)
                {
                    return;
                }

                var OperationVeredict = GetProcessVeredict(Process.ExitCode, ProcessOutput.ToArray());

                AfterFinshAction postAction = AfterFinshAction.ManualClose;
                switch (OperationVeredict)
                {
                    case OperationVeredict.Failed:
                        this.Status = OperationStatus.Failed;
                        RemoveFromQueue();
                        postAction = await HandleFailure();
                        break;

                    case OperationVeredict.Succeeded:
                        this.Status = OperationStatus.Succeeded;
                        postAction = await HandleSuccess();
                        RemoveFromQueue();
                        break;

                    case OperationVeredict.AutoRetry:
                        this.Status = OperationStatus.Pending;
                        postAction = AfterFinshAction.Retry;
                        break;
                }

                switch (postAction)
                {
                    case AfterFinshAction.TimeoutClose:
                        await Task.Delay(5000);
                        _ = Close();
                        break;

                    case AfterFinshAction.ManualClose:
                        break;

                    case AfterFinshAction.Retry:
                        AddToQueue_Priority();
                        Retry();
                        break;
                }

                // TODO: Logger log operation
                Console.WriteLine(String.Join('\n', ProcessOutput.ToArray()));
            }
            catch (Exception e)
            {
                Console.WriteLine("Operation failed: " + e.ToString());
                LineInfoText = bindings.Translate("An unexpected error occurred:") + " " + e.Message;
                RemoveFromQueue();
                try { this.Status = OperationStatus.Failed; } catch { }
            }
            bindings.TooltipStatus.OperationsInProgress = bindings.TooltipStatus.OperationsInProgress - 1;


        }
        protected async Task Close()
        {
            while (IsDialogOpen)
                await Task.Delay(1000);

            RemoveFromQueue();
            if(bindings.App.mainWindow.NavigationPage.OperationStackPanel.Children.Contains(this))
                bindings.App.mainWindow.NavigationPage.OperationStackPanel.Children.Remove(this);

        }

        protected abstract void Initialize();
        protected abstract Process BuildProcessInstance(ProcessStartInfo startInfo);
        protected abstract OperationVeredict GetProcessVeredict(int ReturnCode, string[] Output);
        protected abstract Task<AfterFinshAction> HandleFailure();
        protected abstract Task<AfterFinshAction> HandleSuccess();
        protected abstract string[] GenerateProcessLogHeader();

        protected void Retry()
        {
            LineInfoText = bindings.Translate("Retrying, please wait...");
            ProcessOutput.Clear();
            Status = OperationStatus.Pending;
            _ = MainThread();
        }

        protected void MainProcedure()
        {
            AddToQueue();
            Initialize();
            _ = PreMainThread();
        }
        private void ResizeEvent(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width < 500)
            { 
                if (LayoutMode != WidgetLayout.Compact)
                    LayoutMode = WidgetLayout.Compact;
            } else {
                if(LayoutMode != WidgetLayout.Default)
                    LayoutMode = WidgetLayout.Default;
            }

        }
    }
}

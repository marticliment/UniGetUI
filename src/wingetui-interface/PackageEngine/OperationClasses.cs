using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.UI;
using Windows.Web.UI;
using CommunityToolkit.WinUI.Helpers;

namespace ModernWindow.PackageEngine
{
    public enum OperationVeredict
    {
        Succeeded,
        Failed,
        AutoRetry,
    }
    public enum OperationStatus
    {
        Pending,
        Running,
        Succeeded,
        Failed,
        Cancelled
    }
    public class OperationCancelledEventArgs : EventArgs
    {
        public OperationStatus OldStatus;
        public OperationCancelledEventArgs(OperationStatus OldStatus)
        { 
            this.OldStatus = OldStatus;
        }
    }
    public abstract class AbstractOperation
    {
        protected enum AfterFinshAction
        {
            TimeoutClose,
            ManualClose,
            Retry,
        }
        
        public static MainAppBindings bindings = MainAppBindings.Instance;

        private Button ActionButton;
        private Button OutputViewewBlock;
        private ProgressBar Progress;
        private Image PackageIcon;
        private TextBlock OperationDescription;
        private Color DefaultProgressbarColor = Colors.AntiqueWhite;

        private string __button_text;
        private string __line_info_text = "Please wait...";
        private Uri __icon_source = new Uri("ms-appx://wingetui/resources/package_color.png");
        private string __operation_description = "$Package Install";
        private Color? __progressbar_color = null;
        private OperationStatus __status = OperationStatus.Pending;

        protected string ButtonText
        {   get { return __button_text; }
            set { __button_text = value; if(ActionButton != null) ActionButton.Content = __button_text; } }
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
            set { __operation_description = value; if (OperationDescription != null) OperationDescription.Text = __operation_description; }
        }
        protected Color? ProgressBarColor
        {
            get { return __progressbar_color; }
            set { __progressbar_color = value; if (Progress != null) Progress.Foreground = (__progressbar_color != null)? new SolidColorBrush((Color)__progressbar_color): null; }
        }

        protected event EventHandler<OperationCancelledEventArgs> CancelRequested;
        protected event EventHandler<OperationCancelledEventArgs> CloseRequested;
        protected Process Process;
        protected List<string> ProcessOutput = new List<string>();

        public OperationStatus Status
        {
            get { return __status; }
            set { 
                __status = value;
                switch (__status)
                {
                    case OperationStatus.Pending:
                        if (Progress != null) Progress.IsIndeterminate = true;
                        ProgressBarColor = Colors.Gray;
                        ButtonText = bindings.Translate("Cancel");
                        break;
                    
                    case OperationStatus.Running:
                        if (Progress != null) Progress.IsIndeterminate = true;
                        ProgressBarColor = new Windows.UI.ViewManagement.UISettings().GetColorValue(Windows.UI.ViewManagement.UIColorType.Accent);
                        ButtonText = bindings.Translate("Cancel");
                        break;

                    case OperationStatus.Succeeded:
                        ProgressBarColor = CommunityToolkit.WinUI.Helpers.ColorHelper.ToColor("#11945a");
                        if (Progress != null) Progress.IsIndeterminate = false;
                        ButtonText = bindings.Translate("Close");
                        break;
                    
                    case OperationStatus.Failed:
                        if (Progress != null) Progress.IsIndeterminate = false;
                        ProgressBarColor = CommunityToolkit.WinUI.Helpers.ColorHelper.ToColor("#fe890b");
                        ButtonText = bindings.Translate("Close");
                        break;
                    
                    case OperationStatus.Cancelled:
                        if (Progress != null) Progress.IsIndeterminate = false;
                        ProgressBarColor = CommunityToolkit.WinUI.Helpers.ColorHelper.ToColor("#fec10b");
                        ButtonText = bindings.Translate("Close");
                        break;
                }
            }
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
            int Index = bindings.OperationList.IndexOf(this);
            if(Index != -1)
                bindings.OperationList.RemoveAt(Index);
        }
        private void AddToQueue()
        {
            bindings.OperationList.Add(this);
        }

        public void CancelButtonClicked(OperationStatus OldStatus)
        {
            RemoveFromQueue();
            this.Status = OperationStatus.Cancelled;
            LineInfoText = bindings.Translate("Operation cancelled");
            if(OldStatus == OperationStatus.Running)
            {
                Process.Kill();
                ProcessOutput.Add("Operation was cancelled by the user!");
            }
        }

        public void CloseButtonClicked(OperationStatus OldStatus)
        {
            bindings.App.mainWindow.NavigationPage.OperationList.Remove(this);
        }

        private void AddToQueue_Priority()
        {
            bindings.OperationList.Insert(0, this);
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

                currentIndex = bindings.OperationList.IndexOf(this);
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
                        LineInfoText = line.Trim();
                        ProcessOutput.Add("   | " + line);
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

                switch(postAction)
                {
                    case AfterFinshAction.TimeoutClose:
                        await Task.Delay(5000);
                        bindings.App.mainWindow.NavigationPage.OperationList.Remove(this);
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
            ProcessOutput = new List<string>();
            Status = OperationStatus.Pending;
            _ = MainThread();
        }

        protected void MainProcedure()
        {
            Initialize();
            _ = PreMainThread();
        }

        public void ImageIcon_Loaded(object sender, RoutedEventArgs e)
        {
            PackageIcon = sender as Image;
            PackageIcon.Source = new BitmapImage(__icon_source);
        }

        public void TextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            OperationDescription = sender as TextBlock;
            OperationDescription.Text = __operation_description;
        }

        public void ProgressBar_Loaded(object sender, RoutedEventArgs e)
        {
            Progress = sender as ProgressBar;
            Progress.Foreground = (__progressbar_color != null) ? new SolidColorBrush((Color)__progressbar_color) : null;
            Progress.IsIndeterminate = (Status == OperationStatus.Pending || Status == OperationStatus.Running);
        }

        public void ViewLogButton_Loaded(object sender, RoutedEventArgs e)
        {
            OutputViewewBlock = sender as Button;
            OutputViewewBlock.Content = __line_info_text;
        }

        public void ActionButton_Loaded(object sender, RoutedEventArgs e)
        {
            ActionButton = sender as Button;
            ActionButton.Content = __button_text;
            ActionButton.Click += ActionButtonClicked;
        }
    }

    public abstract class PackageOperation : AbstractOperation
    {
        
        protected Package Package;
        protected InstallationOptions Options;
        public PackageOperation(Package package, InstallationOptions options)
        {
            this.Package = package;
            this.Options = options;
            MainProcedure();
        }
    }

    public class InstallPackageOperation : PackageOperation
    {

        public InstallPackageOperation(Package package, InstallationOptions options) : base(package, options) { }
        protected override Process BuildProcessInstance(ProcessStartInfo startInfo)
        {
            startInfo.FileName = Package.Manager.Status.ExecutablePath;
            startInfo.Arguments = Package.Manager.Properties.ExecutableCallArgs + " " + String.Join(" ", Package.Manager.GetInstallParameters(Package, Options));
            Process process = new Process();
            process.StartInfo = startInfo;
            return process;
        }

        protected override string[] GenerateProcessLogHeader()
        {
            return new string[]
            {
                "Starting package install operation for package id=" + Package.Id + "with Manager name=" + Package.Manager.Name,
                "Given installation options are " + Options.ToString()
            };
        }

        protected override OperationVeredict GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Package.Manager.GetInstallOperationVeredict(Package, Options, ReturnCode, Output);
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = bindings.Translate("{package} installation failed!").Replace("{package}", Package.Name);
            await Task.Delay(0);
            return AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            LineInfoText = bindings.Translate("{package} installation succeeded!").Replace("{package}", Package.Name);
            await Task.Delay(0);
            return AfterFinshAction.TimeoutClose;
        }

        protected override void Initialize()
        {
            OperationTitle = bindings.Translate("{package} Installation").Replace("{package}", Package.Name); 
            IconSource = Package.GetIconUrl();
        }
    }

    public class UpdatePackageOperation : PackageOperation
    {

        public UpdatePackageOperation(Package package, InstallationOptions options) : base(package, options) { }
        protected override Process BuildProcessInstance(ProcessStartInfo startInfo)
        {
            startInfo.FileName = Package.Manager.Status.ExecutablePath;
            startInfo.Arguments = Package.Manager.Properties.ExecutableCallArgs + " " + String.Join(" ", Package.Manager.GetUpdateParameters(Package, Options));
            Process process = new Process();
            process.StartInfo = startInfo;
            return process;
        }

        protected override string[] GenerateProcessLogHeader()
        {
            return new string[]
            {
                "Starting package update operation for package id=" + Package.Id + "with Manager name=" + Package.Manager.Name,
                "Given installation options are " + Options.ToString()
            };
        }

        protected override OperationVeredict GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Package.Manager.GetUpdateOperationVeredict(Package, Options, ReturnCode, Output);
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = bindings.Translate("{package} update failed. Click here for more details.").Replace("{package}", Package.Name);
            await Task.Delay(0);
            return AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            LineInfoText = bindings.Translate("{package} update succeeded!").Replace("{package}", Package.Name);
            await Task.Delay(0);
            return AfterFinshAction.TimeoutClose;
        }

        protected override void Initialize()
        {
            OperationTitle = bindings.Translate("{package} Update").Replace("{package}", Package.Name);
            IconSource = Package.GetIconUrl();
        }
    }

    public class UninstallPackageOperation : PackageOperation
    {

        public UninstallPackageOperation(UpgradablePackage package, InstallationOptions options) : base(package, options) { }
        protected override Process BuildProcessInstance(ProcessStartInfo startInfo)
        {
            startInfo.FileName = Package.Manager.Status.ExecutablePath;
            startInfo.Arguments = Package.Manager.Properties.ExecutableCallArgs + " " + String.Join(" ", Package.Manager.GetUninstallParameters(Package, Options));
            Process process = new Process();
            process.StartInfo = startInfo;
            return process;
        }

        protected override string[] GenerateProcessLogHeader()
        {
            return new string[]
            {
                "Starting package uninstall operation for package id=" + Package.Id + "with Manager name=" + Package.Manager.Name,
                "Given installation options are " + Options.ToString()
            };
        }

        protected override OperationVeredict GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Package.Manager.GetUninstallOperationVeredict(Package, Options, ReturnCode, Output);
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = bindings.Translate("{package} uninstallation failed!").Replace("{package}", Package.Name);
            await Task.Delay(0);
            return AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            LineInfoText = bindings.Translate("{package} uninstallation succeeded!").Replace("{package}", Package.Name);
            await Task.Delay(0);
            return AfterFinshAction.TimeoutClose;
        }

        protected override void Initialize()
        {
            OperationTitle = bindings.Translate("{package} Uninstallation").Replace("{package}", Package.Name);
            IconSource = Package.GetIconUrl();
        }
    }
}

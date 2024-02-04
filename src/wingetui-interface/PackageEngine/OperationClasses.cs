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
        public PackageOperation(Package package) : this(package, new InstallationOptions(package)) { }
    }

    public class InstallPackageOperation : PackageOperation
    {

        public InstallPackageOperation(Package package, InstallationOptions options) : base(package, options) { }
        public InstallPackageOperation(Package package) : base(package) { }
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
        public UpdatePackageOperation(Package package) : base(package) { }
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

        public UninstallPackageOperation(Package package, InstallationOptions options) : base(package, options) { }
        public UninstallPackageOperation(Package package) : base(package) { }
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
            bindings.TooltipStatus.ErrorsOccurred = bindings.TooltipStatus.ErrorsOccurred + 1;
            await Task.Delay(0);
            bindings.TooltipStatus.ErrorsOccurred = bindings.TooltipStatus.ErrorsOccurred - 1;
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

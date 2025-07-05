using System.Collections.ObjectModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Controls.OperationWidgets;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface;
using UniGetUI.Interface.Telemetry;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Managers.CargoManager;
using UniGetUI.PackageEngine.Managers.PowerShellManager;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageOperations;
using UniGetUI.Pages.DialogPages;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine;

namespace UniGetUI;

public partial class MainApp
{
    public static class Operations
    {
        public static bool AreThereRunningOperations()
        {
            return _operationList.Any() &&
                   _operationList.Any(x => x.Operation.Status is OperationStatus.Running or OperationStatus.InQueue);
        }

        public static ObservableCollection<OperationControl> _operationList = new();

        public static void Add(AbstractOperation op)
            => _operationList.Add(new(op));

        public static void Remove(OperationControl control)
            => _operationList.Remove(control);

        public static void Remove(AbstractOperation op)
        {
            foreach(var control in _operationList.Where(x => x.Operation == op).ToArray())
            {
                _operationList.Remove(control);
            }
        }

        /*
         *
         * OPERATION CREATION HELPERS
         *
         */
        public static async Task<AbstractOperation?> AskLocationAndDownload(IPackage? package, TEL_InstallReferral referral)
        {
            if (package is null) return null;
            try
            {
                await DialogHelper.ShowLoadingDialog(CoreTools.Translate("Please wait..."));

                var details = package.Details;
                await details.Load();

                if (details.InstallerUrl is null)
                {
                    DialogHelper.HideLoadingDialog();
                    var dialog = new ContentDialog { Title = CoreTools.Translate("Download failed"),
                        Content = CoreTools.Translate("No applicable installer was found for the package {0}", package.Name),
                        PrimaryButtonText = CoreTools.Translate("Ok"),
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = Instance.MainWindow.Content.XamlRoot,
                    };
                    await Instance.MainWindow.ShowDialogAsync(dialog);
                    return null;
                }

                FileSavePicker savePicker = new();
                MainWindow window = Instance.MainWindow;
                IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);
                savePicker.SuggestedStartLocation = PickerLocationId.Downloads;

                string extension = CoreTools.MakeValidFileName(details.InstallerUrl.ToString().Split('.')[^1]);
                if (package.Manager is Cargo) extension = "zip";
                savePicker.SuggestedFileName = package.Id + " installer." + extension;

                if (package.Manager is BaseNuGet)
                {
                    extension = "nupkg";
                    savePicker.FileTypeChoices.Add("Compressed file", [".zip"]);
                }

                savePicker.FileTypeChoices.Add("Automatic", [$".{extension}"]);
                savePicker.FileTypeChoices.Add("Executable", [".exe"]);
                savePicker.FileTypeChoices.Add("MSI", [".msi"]);
                savePicker.FileTypeChoices.Add("ZIP file", [".zip"]);
                savePicker.FileTypeChoices.Add("MSIX", [".msix"]);
                savePicker.FileTypeChoices.Add("APPX", [".appx"]);
                savePicker.FileTypeChoices.Add("Tarball", [".tar"]);
                savePicker.FileTypeChoices.Add("Compressed Tarball", [".tgz"]);


                StorageFile file = await savePicker.PickSaveFileAsync();

                DialogHelper.HideLoadingDialog();
                if (file is not null)
                {
                    var op = new DownloadOperation(package, file.Path);
                    op.OperationSucceeded += (_, _) => TelemetryHandler.DownloadPackage(package, TEL_OP_RESULT.SUCCESS, referral);
                    op.OperationFailed += (_, _) => TelemetryHandler.DownloadPackage(package, TEL_OP_RESULT.FAILED, referral);
                    Add(op);
                    Instance.MainWindow.UpdateSystemTrayStatus();
                    return op;
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"An error occurred while downloading the installer for the package {package.Id}");
                Logger.Error(ex);
                DialogHelper.HideLoadingDialog();
                return null;
            }
        }

        /*
         *
         *
         * PACKAGE INSTALLATION
         *
         *
         */
        public static async Task<AbstractOperation?> Install(IPackage? package, TEL_InstallReferral referral,
            bool? elevated = null, bool? interactive = null, bool? no_integrity = null, bool ignoreParallel = false,
            AbstractOperation? req = null)
        {
            if (package is null) return null;

            var options = await InstallOptionsFactory.LoadApplicableAsync(package, elevated, interactive, no_integrity);
            var operation = new InstallPackageOperation(package, options, ignoreParallel, req);
            operation.OperationSucceeded += (_, _) => TelemetryHandler.InstallPackage(package, TEL_OP_RESULT.SUCCESS, referral);
            operation.OperationFailed += (_, _) => TelemetryHandler.InstallPackage(package, TEL_OP_RESULT.FAILED, referral);
            Add(operation);
            Instance.MainWindow.UpdateSystemTrayStatus();
            return operation;
        }

        public static void Install(IReadOnlyList<IPackage> packages, TEL_InstallReferral referral, bool? elevated = null, bool? interactive = null, bool? no_integrity = null)
        {
            foreach (var package in packages)
            {
                _ = Install(package, referral, elevated, interactive, no_integrity);
            }
        }

        public static async Task<AbstractOperation?> UninstallThenReinstall(IPackage? package, TEL_InstallReferral referral)
        {
            if (package is null) return null;

            var options = await InstallOptionsFactory.LoadApplicableAsync(package);

            var uninstallOp = new UninstallPackageOperation(package, options);
            uninstallOp.OperationSucceeded += (_, _) => TelemetryHandler.UninstallPackage(package, TEL_OP_RESULT.SUCCESS);
            uninstallOp.OperationFailed += (_, _) => TelemetryHandler.UninstallPackage(package, TEL_OP_RESULT.FAILED);

            var installOp = new InstallPackageOperation(package, options, req: uninstallOp);
            installOp.OperationSucceeded += (_, _) => TelemetryHandler.InstallPackage(package, TEL_OP_RESULT.SUCCESS, referral);
            installOp.OperationFailed += (_, _) => TelemetryHandler.InstallPackage(package, TEL_OP_RESULT.FAILED, referral);

            Add(installOp);
            Instance.MainWindow.UpdateSystemTrayStatus();
            return installOp;
        }

        /*
         *
         *
         * PACKAGE UPDATE
         *
         *
         */
        public static async Task<AbstractOperation?> Update(IPackage? package, bool? elevated = null, bool? interactive = null, bool? no_integrity = null, bool ignoreParallel = false, AbstractOperation? req = null)
        {
            if (package is null) return null;

            var options = await InstallOptionsFactory.LoadApplicableAsync(package, elevated, interactive, no_integrity);
            var operation = new UpdatePackageOperation(package, options, ignoreParallel, req);
            operation.OperationSucceeded += (_, _) => TelemetryHandler.UpdatePackage(package, TEL_OP_RESULT.SUCCESS);
            operation.OperationFailed += (_, _) => TelemetryHandler.UpdatePackage(package, TEL_OP_RESULT.FAILED);
            Add(operation);
            Instance.MainWindow.UpdateSystemTrayStatus();
            return operation;
        }

        public static void Update(IReadOnlyList<IPackage> packages, bool? elevated = null, bool? interactive = null, bool? no_integrity = null)
        {
            foreach (var package in packages)
            {
                _ = Update(package, elevated, interactive, no_integrity);
            }
        }

        public static async void UpdateAll()
        {
            foreach (IPackage package in PEInterface.UpgradablePackagesLoader.Packages)
                if (package.Tag is not PackageTag.BeingProcessed and not PackageTag.OnQueue)
                    await Update(package);
        }

        public static async void UpdateAllForManager(string managerName)
        {
            foreach (IPackage package in PEInterface.UpgradablePackagesLoader.Packages)
            {
                if (package.Tag is not PackageTag.OnQueue and not PackageTag.BeingProcessed
                    && package.Manager.Name == managerName || package.Manager.DisplayName == managerName)
                    await Update(package);
            }
        }

        public static async void UpdateForId(string packageId)
        {
            foreach (IPackage package in PEInterface.UpgradablePackagesLoader.Packages)
            {
                if (package.Id == packageId)
                {
                    await Update(package);
                    Logger.Info($"[WIDGETS] Updating package with id {packageId}");
                    return;
                }
            }

            Logger.Warn($"[WIDGETS] No package with id={packageId} was found");
        }

        public static async Task<AbstractOperation?> UninstallThenUpdate(IPackage? package)
        {
            if (package is null) return null;

            var options = await InstallOptionsFactory.LoadApplicableAsync(package);

            var uninstallOp = new UninstallPackageOperation(package, options);
            uninstallOp.OperationSucceeded += (_, _) => TelemetryHandler.UninstallPackage(package, TEL_OP_RESULT.SUCCESS);
            uninstallOp.OperationFailed += (_, _) => TelemetryHandler.UninstallPackage(package, TEL_OP_RESULT.FAILED);

            var installOp = new UpdatePackageOperation(package, options, req: uninstallOp);
            installOp.OperationSucceeded += (_, _) => TelemetryHandler.UpdatePackage(package, TEL_OP_RESULT.SUCCESS);
            installOp.OperationFailed += (_, _) => TelemetryHandler.UpdatePackage(package, TEL_OP_RESULT.FAILED);

            Add(installOp);
            Instance.MainWindow.UpdateSystemTrayStatus();
            return installOp;
        }

        /*
         *
         *
         * PACKAGE UNINSTALL
         *
         *
         */

        public static async void ConfirmAndUninstall(IReadOnlyList<IPackage> packages, bool? elevated = null, bool? interactive = null, bool? remove_data = null)
        {
            if (!await DialogHelper.ConfirmUninstallation(packages))
                return;

            Uninstall(packages, elevated, interactive, remove_data);
        }

        public static async void ConfirmAndUninstall(IPackage? package, bool? elevated = null, bool? interactive = null, bool? remove_data = null)
        {
            if (package is null) return;
            if (!await DialogHelper.ConfirmUninstallation(package)) return;

            _ = Uninstall(package, elevated, interactive, remove_data);
        }

        public static async Task<AbstractOperation?> Uninstall(IPackage? package, bool? elevated = null, bool? interactive = null, bool? remove_data = null, bool ignoreParallel = false, AbstractOperation? req = null)
        {
            if (package is null) return null;

            var options = await InstallOptionsFactory.LoadApplicableAsync(package, elevated, interactive, remove_data: remove_data);
            var operation = new UninstallPackageOperation(package, options, ignoreParallel, req);
            operation.OperationSucceeded += (_, _) => TelemetryHandler.UninstallPackage(package, TEL_OP_RESULT.SUCCESS);
            operation.OperationFailed += (_, _) => TelemetryHandler.UninstallPackage(package, TEL_OP_RESULT.FAILED);
            Add(operation);
            Instance.MainWindow.UpdateSystemTrayStatus();
            return operation;
        }

        public static void Uninstall(IReadOnlyList<IPackage> packages, bool? elevated = null, bool? interactive = null, bool? remove_data = null)
        {
            foreach (var package in packages)
            {
                _ = Uninstall(package, elevated, interactive, remove_data);
            }
        }
    }
}

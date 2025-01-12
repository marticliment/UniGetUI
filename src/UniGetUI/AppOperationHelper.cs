using System.Collections.ObjectModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using UniGetUI.Controls.OperationWidgets;
using UniGetUI.Core.Logging;
using UniGetUI.Interface;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Managers.PowerShellManager;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageOperations;
using UniGetUI.Pages.DialogPages;

namespace UniGetUI;

public partial class MainApp
{
    public static class Operations
    {
        public static ObservableCollection<OperationControl> _operationList = new();

        public static void Add(AbstractOperation op)
            => _operationList.Add(new(op));

        public static void Remove(OperationControl control)
            => _operationList.Remove(control);

        public static void Remove(AbstractOperation op)
        {
            foreach(var control in _operationList.Where(x => x.Operation == op).ToArray())
                _operationList.Remove(control);
        }


        /*
         *
         * OPERATION CREATION HELPERS
         *
         */

        public static async void AskLocationAndDownload(IPackage package)
        {
            try
            {
                var details = package.Details;
                if (details.InstallerUrl is null)
                    return;

                FileSavePicker savePicker = new();
                MainWindow window = MainApp.Instance.MainWindow;
                IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);
                savePicker.SuggestedStartLocation = PickerLocationId.Downloads;
                string extension = package.Manager is BaseNuGet
                    ? "nupkg"
                    : details.InstallerUrl.ToString().Split('.')[^1];
                savePicker.SuggestedFileName = package.Id + " installer." + extension;

                if (details.InstallerUrl.ToString().Split('.')[^1] == "nupkg")
                {
                    savePicker.FileTypeChoices.Add("Compressed Manifest File", [".zip"]);
                }

                savePicker.FileTypeChoices.Add("Default", [$".{extension}"]);

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file is not null)
                {
                    Add(new DownloadOperation(package, file.Path));
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"An error occurred while downloading the installer for the package {package.Id}");
                Logger.Error(ex);
            }
        }



        /*
         * PACKAGE INSTALLATION
         */
        public static async void Install(IPackage? package, bool? elevated = null, bool? interactive = null, bool? no_integrity = null, bool ignoreParallel = false)
        {
            if (package is null) return;

            var options = await InstallationOptions.FromPackageAsync(package, elevated, interactive, no_integrity);
            Add(new InstallPackageOperation(package, options, ignoreParallel));
        }

        public static void Install(IReadOnlyList<IPackage> packages, bool? elevated = null, bool? interactive = null, bool? no_integrity = null)
        {
            foreach (var package in packages) Install(package, elevated, interactive, no_integrity);
        }



        /*
         * PACKAGE UPDATE
         */
        public static async void Update(IPackage? package, bool? elevated = null, bool? interactive = null, bool? no_integrity = null, bool ignoreParallel = false)
        {
            if (package is null) return;

            var options = await InstallationOptions.FromPackageAsync(package, elevated, interactive, no_integrity);
            Add(new UpdatePackageOperation(package, options, ignoreParallel));
        }

        public static void Update(IReadOnlyList<IPackage> packages, bool? elevated = null, bool? interactive = null, bool? no_integrity = null)
        {
            foreach (var package in packages) Update(package, elevated, interactive, no_integrity);
        }



        /*
         * PACKAGE UNINSTALL
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

            Uninstall(package, elevated, interactive, remove_data);
        }

        public static async void Uninstall(IPackage? package, bool? elevated = null, bool? interactive = null, bool? remove_data = null, bool ignoreParallel = false)
        {
            if (package is null) return;

            var options = await InstallationOptions.FromPackageAsync(package, elevated, interactive, remove_data: remove_data);
            Add(new UninstallPackageOperation(package, options, ignoreParallel));
        }

        public static void Uninstall(IReadOnlyList<IPackage> packages, bool? elevated = null, bool? interactive = null, bool? remove_data = null)
        {
            foreach (var package in packages) Uninstall(package, elevated, interactive, remove_data);
        }
    }
}

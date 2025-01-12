using System.Collections.ObjectModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using UniGetUI.Controls.OperationWidgets;
using UniGetUI.Core.Logging;
using UniGetUI.Interface;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Managers.PowerShellManager;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageOperations;

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

        public static async void DownloadInstaller(IPackage package)
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
    }
}

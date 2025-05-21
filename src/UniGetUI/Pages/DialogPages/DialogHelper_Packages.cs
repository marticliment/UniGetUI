using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Dialogs;
using UniGetUI.Interface.Telemetry;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Serializable;

namespace UniGetUI.Pages.DialogPages;

public static partial class DialogHelper
{
    /// <summary>
    /// Will update the Installation Options for the given Package, and will return whether the user choose to continue
    /// </summary>
    public static async Task<bool> ShowInstallatOptions_Continue(IPackage package, OperationType operation)
    {
        var options = (await InstallationOptions.FromPackageAsync(package)).AsSerializable();
        var (dialogOptions, dialogResult) = await ShowInstallOptions(package, operation, options);

        if (dialogResult != ContentDialogResult.None)
        {
            InstallationOptions newOptions = await InstallationOptions.FromPackageAsync(package);
            newOptions.FromSerializable(dialogOptions);
            await newOptions.SaveToDiskAsync();
        }

        return dialogResult == ContentDialogResult.Secondary;
    }

    /// <summary>
    /// Will update the Installation Options for the given imported package
    /// </summary>
    public static async Task<ContentDialogResult> ShowInstallOptions_ImportedPackage(ImportedPackage importedPackage)
    {
        var (options, dialogResult) =
            await ShowInstallOptions(importedPackage, OperationType.None, importedPackage.installation_options.Copy());

        if (dialogResult != ContentDialogResult.None)
        {
            importedPackage.installation_options = options;
            importedPackage.FirePackageVersionChangedEvent();
        }

        return dialogResult;
    }

    private static async Task<(SerializableInstallationOptions, ContentDialogResult)> ShowInstallOptions(
        IPackage package,
        OperationType operation,
        SerializableInstallationOptions options)
    {
        InstallOptionsPage OptionsPage = new(package, operation, options);

        ContentDialog OptionsDialog = DialogFactory.Create_AsWindow(true, true);

        OptionsDialog.SecondaryButtonText = operation switch
        {
            OperationType.Install => CoreTools.Translate("Install"),
            OperationType.Uninstall => CoreTools.Translate("Uninstall"),
            OperationType.Update => CoreTools.Translate("Update"),
            _ => ""
        };
        OptionsDialog.PrimaryButtonText = CoreTools.Translate("Save and close");
        OptionsDialog.DefaultButton = ContentDialogButton.Secondary;
        OptionsDialog.Title = CoreTools.Translate("{0} installation options", package.Name);
        OptionsDialog.Content = OptionsPage;

        OptionsPage.Close += (_, _) => { OptionsDialog.Hide(); };

        ContentDialogResult result = await Window.ShowDialogAsync(OptionsDialog);
        return (await OptionsPage.GetUpdatedOptions(), result);
    }

    public static async void ShowPackageDetails(IPackage package, OperationType operation, TEL_InstallReferral referral)
    {
        PackageDetailsPage DetailsPage = new(package, operation, referral);

        ContentDialog DetailsDialog = DialogFactory.Create_AsWindow(false);
        DetailsDialog.Content = DetailsPage;
        DetailsPage.Close += (_, _) => { DetailsDialog.Hide(); };

        await Window.ShowDialogAsync(DetailsDialog);
    }

    public static async Task<bool> ConfirmUninstallation(IPackage package)
    {
        ContentDialog dialog = DialogFactory.Create();
        dialog.Title = CoreTools.Translate("Are you sure?");
        dialog.PrimaryButtonText = CoreTools.Translate("Yes");
        dialog.SecondaryButtonText = CoreTools.Translate("No");
        dialog.DefaultButton = ContentDialogButton.Secondary;
        dialog.Content = CoreTools.Translate("Do you really want to uninstall {0}?", package.Name);

        return await Window.ShowDialogAsync(dialog) is ContentDialogResult.Primary;
    }

    public static async Task<bool> ConfirmUninstallation(IReadOnlyList<IPackage> packages)
    {
        if (!packages.Any())
        {
            return false;
        }

        if (packages.Count == 1)
        {
            return await ConfirmUninstallation(packages[0]);
        }

        ContentDialog dialog = DialogFactory.Create();
        dialog.Title = CoreTools.Translate("Are you sure?");
        dialog.PrimaryButtonText = CoreTools.Translate("Yes");
        dialog.SecondaryButtonText = CoreTools.Translate("No");
        dialog.DefaultButton = ContentDialogButton.Secondary;


        StackPanel p = new();
        p.Children.Add(new TextBlock
        {
            Text = CoreTools.Translate("Do you really want to uninstall the following {0} packages?",
                packages.Count),
            Margin = new Thickness(0, 0, 0, 5)
        });

        string pkgList = "";
        foreach (IPackage package in packages)
        {
            pkgList += " ● " + package.Name + "\x0a";
        }

        TextBlock PackageListTextBlock =
            new() { FontFamily = new FontFamily("Consolas"), Text = pkgList };
        p.Children.Add(new ScrollView { Content = PackageListTextBlock, MaxHeight = 200 });

        dialog.Content = p;

        return await Window.ShowDialogAsync(dialog) is ContentDialogResult.Primary;
    }

    public static void ShowSharedPackage_ThreadSafe(string id, string combinedSourceName)
    {
        var contents = combinedSourceName.Split(':');
        string managerName = contents[0];
        string sourceName = "";
        if (contents.Length > 1) sourceName = contents[1];
        GetPackageFromIdAndManager(id, managerName, sourceName, "LEGACY_COMBINEDSOURCE");
    }

    public static void ShowSharedPackage_ThreadSafe(string id, string managerName, string sourceName)
    {
        MainApp.Instance.MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            GetPackageFromIdAndManager(id, managerName, sourceName, "DEFAULT");
        });
    }

    private static async void GetPackageFromIdAndManager(string id, string managerName, string sourceName, string eventSource)
    {
        try
        {
            Window.Activate();
            ShowLoadingDialog(CoreTools.Translate("Please wait..."));

            var findResult = await Task.Run(() => PEInterface.DiscoveredPackagesLoader.GetPackageFromIdAndManager(id, managerName, sourceName));

            HideLoadingDialog();

            if (findResult.Item1 is null) throw new KeyNotFoundException(findResult.Item2 ?? "Unknown error");

            TelemetryHandler.SharedPackage(findResult.Item1, eventSource);
            ShowPackageDetails(findResult.Item1, OperationType.Install, TEL_InstallReferral.FROM_WEB_SHARE);

        }
        catch (Exception ex)
        {
            Logger.Error($"An error occurred while attempting to show the package with id {id}");
            var warningDialog = new ContentDialog
            {
                Title = CoreTools.Translate("Package not found"),
                Content = CoreTools.Translate("An error occurred when attempting to show the package with Id {0}", id) + ":\n" + ex.Message,
                CloseButtonText = CoreTools.Translate("Ok"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = MainApp.Instance.MainWindow.Content.XamlRoot // Ensure the dialog is shown in the correct context
            };

            HideLoadingDialog();
            await Window.ShowDialogAsync(warningDialog);

        }
    }
}

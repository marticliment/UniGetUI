using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface;
using UniGetUI.Interface.Dialogs;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Serializable;
using AbstractOperation = UniGetUI.PackageOperations.AbstractOperation;

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

    private static async Task<(SerializableInstallationOptions_v1, ContentDialogResult)> ShowInstallOptions(
        IPackage package,
        OperationType operation,
        SerializableInstallationOptions_v1 options)
    {
        InstallOptionsPage OptionsPage = new(package, operation, options);

        ContentDialog OptionsDialog = new()
        {
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            XamlRoot = Window.XamlRoot
        };
        OptionsDialog.Resources["ContentDialogMaxWidth"] = 1200;
        OptionsDialog.Resources["ContentDialogMaxHeight"] = 1000;

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


    public static async void ShowPackageDetails(IPackage package, OperationType operation)
    {
        PackageDetailsPage DetailsPage = new(package, operation);

        ContentDialog DetailsDialog = new()
        {
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            XamlRoot = Window.XamlRoot
        };
        DetailsDialog.Resources["ContentDialogMaxWidth"] = 8000;
        DetailsDialog.Resources["ContentDialogMaxHeight"] = 4000;
        DetailsDialog.Content = DetailsPage;
        DetailsDialog.SizeChanged += (_, _) =>
        {
            int hOffset = (Window.NavigationPage.ActualWidth < 1300) ? 100 : 300;
            DetailsPage.MinWidth = Math.Abs(Window.NavigationPage.ActualWidth - hOffset);
            DetailsPage.MinHeight = Math.Abs(Window.NavigationPage.ActualHeight - 100);
            DetailsPage.MaxWidth = Math.Abs(Window.NavigationPage.ActualWidth - hOffset);
            DetailsPage.MaxHeight = Math.Abs(Window.NavigationPage.ActualHeight - 100);
        };

        DetailsPage.Close += (_, _) => { DetailsDialog.Hide(); };

        await Window.ShowDialogAsync(DetailsDialog);
    }


    public static async Task<bool> ConfirmUninstallation(IPackage package)
    {
        ContentDialog dialog = new()
        {
            XamlRoot = Window.XamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = CoreTools.Translate("Are you sure?"),
            PrimaryButtonText = CoreTools.Translate("Yes"),
            SecondaryButtonText = CoreTools.Translate("No"),
            DefaultButton = ContentDialogButton.Secondary,
            Content = CoreTools.Translate("Do you really want to uninstall {0}?", package.Name)
        };

        return await Window.ShowDialogAsync(dialog) is ContentDialogResult.Primary;
    }


    public static async Task<bool> ConfirmUninstallation(IEnumerable<IPackage> packages)
    {
        if (!packages.Any())
        {
            return false;
        }

        if (packages.Count() == 1)
        {
            return await ConfirmUninstallation(packages.First());
        }

        ContentDialog dialog = new()
        {
            XamlRoot = Window.XamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = CoreTools.Translate("Are you sure?"),
            PrimaryButtonText = CoreTools.Translate("Yes"),
            SecondaryButtonText = CoreTools.Translate("No"),
            DefaultButton = ContentDialogButton.Secondary,
        };

        StackPanel p = new();
        p.Children.Add(new TextBlock
        {
            Text = CoreTools.Translate("Do you really want to uninstall the following {0} packages?",
                packages.Count()),
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
        ShowSharedPackage_ThreadSafe(id, managerName, sourceName);
    }

    public static void ShowSharedPackage_ThreadSafe(string id, string managerName, string sourceName)
    {
        MainApp.Instance.MainWindow.DispatcherQueue.TryEnqueue(async () =>
        {
            IPackage? package = await GetPackageFromIdAndManager(id, managerName, sourceName);
            if (package is not null) DialogHelper.ShowPackageDetails(package, OperationType.Install);
        });
    }

    private static async Task<IPackage?> GetPackageFromIdAndManager(string id, string managerName, string sourceName)
    {
        try
        {
            Logger.Info($"Showing shared package with pId={id} and pSource={managerName}: ´{sourceName} ...");
            MainApp.Instance.MainWindow.Activate();
            DialogHelper.ShowLoadingDialog(CoreTools.Translate("Please wait...", id));

            IPackageManager? manager = null;

            foreach (var candidate in PEInterface.Managers)
            {
                if (candidate.Name == managerName || candidate.DisplayName == managerName)
                {
                    manager = candidate;
                    break;
                }
            }

            if (manager is null)
            {
                throw new ArgumentException(CoreTools.Translate("The package manager \"{0}\" was not found", managerName));
            }

            if(!manager.IsEnabled())
                throw new ArgumentException(CoreTools.Translate("The package manager \"{0}\" is disabled", manager.DisplayName));

            if(!manager.Status.Found)
                throw new ArgumentException(CoreTools.Translate("There is an error with the configuration of the package manager \"{0}\"", manager.DisplayName));

            var results = await Task.Run(() => manager.FindPackages(id));
            var candidates = results.Where(p => p.Id == id).ToArray();

            if (candidates.Length == 0)
            {
                throw new ArgumentException(CoreTools.Translate("The package \"{0}\" was not found on the package manager \"{1}\"", id, manager.DisplayName));
            }

            IPackage package = candidates[0];

            // Get package from best source
            if (candidates.Length >= 1 && manager.Capabilities.SupportsCustomSources)
                foreach (var candidate in candidates)
                    if (candidate.Source.Name == sourceName)
                        package = candidate;

            Logger.ImportantInfo($"Found package {package.Id} on manager {package.Manager.Name}, showing it...");
            DialogHelper.HideLoadingDialog();
            return package;
        }
        catch (Exception ex)
        {
            Logger.Error($"An error occurred while attempting to show the package with id {id}");
            Logger.Error(ex);
            var warningDialog = new ContentDialog
            {
                Title = CoreTools.Translate("Package not found"),
                Content = CoreTools.Translate("An error occurred when attempting to show the package with Id {0}", id) + ":\n" + ex.Message,
                CloseButtonText = CoreTools.Translate("Ok"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = MainApp.Instance.MainWindow.Content.XamlRoot // Ensure the dialog is shown in the correct context
            };

            DialogHelper.HideLoadingDialog();
            await MainApp.Instance.MainWindow.ShowDialogAsync(warningDialog);
            return null;
        }
    }
}

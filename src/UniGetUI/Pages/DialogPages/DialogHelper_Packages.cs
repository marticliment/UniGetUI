using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Dialogs;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Telemetry;
using UniGetUI.Interface.Widgets;
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

    private static async Task<(SerializableInstallationOptions_v1, ContentDialogResult)> ShowInstallOptions(
        IPackage package,
        OperationType operation,
        SerializableInstallationOptions_v1 options)
    {
        InstallOptionsPage OptionsPage = new(package, operation, options);

        ContentDialog OptionsDialog = new()
        {
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            XamlRoot = Window.XamlRoot,
            Resources = {
                ["ContentDialogMaxWidth"] = 1200,
                ["ContentDialogMaxHeight"] = 1000,
            },
            SecondaryButtonText = operation switch
            {
                OperationType.Install => CoreTools.Translate("Install"),
                OperationType.Uninstall => CoreTools.Translate("Uninstall"),
                OperationType.Update => CoreTools.Translate("Update"),
                _ => ""
            },
            PrimaryButtonText = CoreTools.Translate("Save and close"),
            DefaultButton = ContentDialogButton.Secondary,
            Title = CoreTools.Translate("{0} installation options", package.Name),
            Content = OptionsPage,
        };

        OptionsPage.Close += (_, _) => { OptionsDialog.Hide(); };

        ContentDialogResult result = await Window.ShowDialogAsync(OptionsDialog);
        return (await OptionsPage.GetUpdatedOptions(), result);
    }

    public static async void ShowPackageDetails(IPackage package, OperationType operation, TEL_InstallReferral referral)
    {
        PackageDetailsPage DetailsPage = new(package, operation, referral);

        ContentDialog DetailsDialog = new()
        {
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            XamlRoot = Window.XamlRoot,
            Resources = {
                ["ContentDialogMaxWidth"] = 8000,
                ["ContentDialogMaxHeight"] = 4000,
            },
            Content = DetailsPage,
        };
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
}

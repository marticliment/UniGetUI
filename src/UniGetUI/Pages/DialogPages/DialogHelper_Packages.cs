using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Dialogs;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Operations;
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

    public static async Task<ContentDialogResult> ShowOperationFailed(
        IEnumerable<AbstractOperation.OutputLine> processOutput,
        string dialogTitle,
        string shortDescription)
    {
        ContentDialog dialog = new()
        {
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style, XamlRoot = Window.XamlRoot
        };
        dialog.Resources["ContentDialogMaxWidth"] = 850;
        dialog.Resources["ContentDialogMaxHeight"] = 800;
        dialog.Title = dialogTitle;

        Grid grid = new()
        {
            RowSpacing = 16,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        TextBlock headerContent = new()
        {
            TextWrapping = TextWrapping.WrapWholeWords,
            Text = $"{shortDescription}. "
                   + CoreTools.Translate(
                       "Please see the Command-line Output or refer to the Operation History for further information about the issue.")
        };

        StackPanel HeaderPanel = new() { Orientation = Orientation.Horizontal, Spacing = 8 };

        HeaderPanel.Children.Add(new LocalIcon(IconType.Console)
        {
            VerticalAlignment = VerticalAlignment.Center,
            Height = 24,
            Width = 24,
            HorizontalAlignment = HorizontalAlignment.Left
        });

        HeaderPanel.Children.Add(new TextBlock
        {
            Text = CoreTools.Translate("Command-line Output"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });

        RichTextBlock CommandLineOutput = new()
        {
            FontFamily = new FontFamily("Consolas"),
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        ScrollViewer ScrollView = new()
        {
            BorderBrush = new SolidColorBrush(),
            Content = CommandLineOutput,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        Grid OutputGrid = new();
        OutputGrid.Children.Add(ScrollView);
        OutputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        OutputGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(ScrollView, 0);
        Grid.SetRow(ScrollView, 0);

        Expander expander = new()
        {
            Header = HeaderPanel,
            Content = OutputGrid,
            CornerRadius = new CornerRadius(8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        Paragraph par = new();
        foreach (var line in processOutput)
        {
            if (line.Type is AbstractOperation.OutputLine.LineType.STDOUT)
                par.Inlines.Add(new Run { Text = line.Contents + "\x0a" });
            else if (line.Type is AbstractOperation.OutputLine.LineType.Header)
                // TODO: Theme-aware colorss
                par.Inlines.Add(new Run
                {
                    Text = line.Contents + "\x0a", Foreground = new SolidColorBrush(Colors.Azure)
                });
            else
                par.Inlines.Add(new Run
                {
                    Text = line.Contents + "\x0a", Foreground = new SolidColorBrush(Colors.Red)
                });
        }

        CommandLineOutput.Blocks.Add(par);

        grid.Children.Add(headerContent);
        grid.Children.Add(expander);
        Grid.SetRow(headerContent, 0);
        Grid.SetRow(expander, 1);

        dialog.Content = grid;
        dialog.PrimaryButtonText = CoreTools.Translate("Retry");
        dialog.CloseButtonText = CoreTools.Translate("Close");
        dialog.DefaultButton = ContentDialogButton.Primary;

        return await Window.ShowDialogAsync(dialog);
    }

}

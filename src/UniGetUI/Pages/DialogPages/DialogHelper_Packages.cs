using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Dialogs;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Telemetry;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Serializable;
using UniGetUI.Pages.SettingsPages.GeneralPages;

namespace UniGetUI.Pages.DialogPages;

public static partial class DialogHelper
{
    /// <summary>
    /// Will update the Installation Options for the given Package, and will return whether the user choose to continue
    /// </summary>
    public static async Task<bool> ShowInstallatOptions_Continue(IPackage package, OperationType operation)
    {
        var options = await InstallOptionsFactory.LoadForPackageAsync(package);
        var (dialogOptions, dialogResult) = await ShowInstallOptions(package, operation, options);

        if (dialogResult is not ContentDialogResult.None)
        {
            Logger.Debug($"Saving updated options for package {package.Id}");
            await InstallOptionsFactory.SaveForPackageAsync(dialogOptions, package);
        }
        else
        {
            Logger.Debug($"Install options dialog for {package.Id} was canceled, no changes will be saved");
        }

        return dialogResult is ContentDialogResult.Secondary;
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

    private static async Task<(InstallOptions, ContentDialogResult)> ShowInstallOptions(
        IPackage package,
        OperationType operation,
        InstallOptions options)
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
        // OptionsDialog.Title = CoreTools.Translate("{0} installation options", package.Name);
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

    public static async Task ShowBundleSecurityReport(Dictionary<string, List<BundleReportEntry>> packageReport)
    {
        var dialog = DialogFactory.Create_AsWindow(true, true);
        Brush bad = new SolidColorBrush(Colors.PaleVioletRed);
        Brush good = new SolidColorBrush(Colors.Gold);

        if (Window.NavigationPage.ActualTheme is ElementTheme.Light)
        {
            bad = new SolidColorBrush(Colors.Red);
            good = new SolidColorBrush(Colors.DarkGoldenrod);
        }

        var title = CoreTools.Translate("Bundle security report");
        dialog.Title = title;
        Hyperlink a;
        Paragraph p = new();

        foreach(var pair in packageReport)
        {
            p.Inlines.Add(new Run()
            {
                Text = $" - {CoreTools.Translate("Package")}: {pair.Key}:\n",
                FontFamily = new("Consolas")
            });

            foreach (var issue in pair.Value)
            {
                p.Inlines.Add(new Run()
                {
                    Text = $"   * {issue.Line}\n",
                    FontFamily = new("Consolas"),
                    Foreground = issue.Allowed? bad: good
                });
            }
            p.Inlines.Add(new LineBreak());
        }

        dialog.Content = new ScrollViewer()
        {
            MaxWidth = 800,
            Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"],
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Content = new RichTextBlock()
            {
                Blocks = {
                    new Paragraph()
                    {
                        Inlines = {
                            new Run()
                            {
                                Text = CoreTools.Translate("This package bundle had some settings that are potetially dangerous, and may be ignored by default.")
                            },
                            new Run()
                            {
                                Text = "\n - " + CoreTools.Translate("Entries that show in YELLOW will be IGNORED."),
                                Foreground = good,
                            },
                            new Run()
                            {
                                Text = "\n - " + CoreTools.Translate("Entries that show in RED will be IMPORTED."),
                                Foreground = bad
                            },
                            new Run()
                            {
                                Text = "\n" + CoreTools.Translate("You can change this behaviour on UniGetUI security settings.") + " "
                            },
                            (a = new Hyperlink
                            {
                                Inlines = { new Run() { Text = CoreTools.Translate("Open UniGetUI security settings") } },
                            }),
                            new LineBreak(),
                            new Run()
                            {
                                Text = CoreTools.Translate("Should you modify the security settings, you will need to open the bundle again for the changes to take effect.")
                            },
                            new LineBreak(),
                            new LineBreak(),
                            new Run() { Text = CoreTools.Translate("Details of the report:") },
                            new LineBreak(),
                        }
                    },
                    p
                }
            }
        };
        a.Click += (_, _) => {
            dialog.Hide();
            Window.NavigationPage.OpenSettingsPage(typeof(Administrator));
        };
        dialog.SecondaryButtonText = CoreTools.Translate("Close");
        await Window.ShowDialogAsync(dialog);
    }
}

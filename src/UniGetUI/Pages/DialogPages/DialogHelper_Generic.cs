using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface;
using UniGetUI.Interface.Dialogs;
using UniGetUI.PackageEngine;

namespace UniGetUI.Pages.DialogPages;

public static partial class DialogHelper
{
    public static MainWindow Window { private get; set; } = null!;

    public static void ShowLoadingDialog(string text)
    {
        ShowLoadingDialog(text, "");
    }

    public static void ShowLoadingDialog(string title, string description)
    {
        if (Window.LoadingDialogCount == 0 && Window.DialogQueue.Count == 0)
        {
            Window.LoadingSthDalog.Title = title;
            Window.LoadingSthDalogText.Text = description;
            Window.LoadingSthDalog.XamlRoot = Window.NavigationPage.XamlRoot;
            _ = Window.ShowDialogAsync(Window.LoadingSthDalog, HighPriority: true);
        }

        Window.LoadingDialogCount++;
    }

    public static void HideLoadingDialog()
    {
        Window.LoadingDialogCount--;
        if (Window.LoadingDialogCount <= 0)
        {
            Window.LoadingSthDalog.Hide();
        }

        if (Window.LoadingDialogCount < 0)
        {
            Window.LoadingDialogCount = 0;
        }
    }

    public static async Task ShowMissingDependency(string dep_name, string exe_name, string exe_args,
        string fancy_command, int current, int total)
    {
        ContentDialog dialog = new();

        if (Settings.GetDictionaryItem<string, string>("DependencyManagement", dep_name) == "skipped")
        {
            Logger.Error(
                $"Dependency {dep_name} was not found, and the user set it to not be reminded of the missing dependency");
            return;
        }

        bool NotFirstTime = Settings.GetDictionaryItem<string, string>("DependencyManagement", dep_name) == "attempted";
        Settings.SetDictionaryItem("DependencyManagement", dep_name, "attempted");

        dialog.XamlRoot = Window.MainContentGrid.XamlRoot;
        dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
        dialog.Title = CoreTools.Translate("Missing dependency") + (total > 1 ? $" ({current}/{total})" : "");
        dialog.SecondaryButtonText = CoreTools.Translate("Not right now");
        dialog.PrimaryButtonText = CoreTools.Translate("Install {0}", dep_name);
        dialog.DefaultButton = ContentDialogButton.Primary;

        bool has_installed = false;
        bool block_closing = false;

        StackPanel p = new();

        p.Children.Add(new TextBlock
        {
            Text = CoreTools.Translate(
                "UniGetUI requires {0} to operate, but it was not found on your system.", dep_name),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 5)
        });

        TextBlock infotext = new()
        {
            Text = CoreTools.Translate(
                "Click on Install to begin the installation process. If you skip the installation, UniGetUI may not work as expected."),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10),
            Opacity = .7F,
            FontStyle = Windows.UI.Text.FontStyle.Italic,
        };
        p.Children.Add(infotext);

        TextBlock commandInfo = new()
        {
            Text = CoreTools.Translate(
                "Alternatively, you can also install {0} by running the following command in a Windows PowerShell prompt:",
                dep_name),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 4),
            Opacity = .7F,
        };
        p.Children.Add(commandInfo);

        TextBlock manualInstallCommand = new()
        {
            Text = fancy_command,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 4),
            Opacity = .7F,
            IsTextSelectionEnabled = true,
            FontFamily = new FontFamily("Consolas"),
        };
        p.Children.Add(manualInstallCommand);

        CheckBox c = new();
        if (NotFirstTime)
        {
            c.Content = CoreTools.Translate("Do not show this dialog again for {0}", dep_name);
            c.IsChecked = false;
            c.Checked += (_, _) => Settings.SetDictionaryItem("DependencyManagement", dep_name, "skipped");
            c.Unchecked += (_, _) => Settings.SetDictionaryItem("DependencyManagement", dep_name, "attempted");
            p.Children.Add(c);
        }

        ProgressBar progress = new() { IsIndeterminate = false, Opacity = .0F };
        p.Children.Add(progress);

        dialog.PrimaryButtonClick += async (_, _) =>
        {
            if (!has_installed)
            {
                // Begin installing the dependency
                try
                {
                    progress.Opacity = 1.0F;
                    progress.IsIndeterminate = true;
                    block_closing = true;
                    c.IsEnabled = false;
                    dialog.IsPrimaryButtonEnabled = false;
                    dialog.IsSecondaryButtonEnabled = false;
                    dialog.SecondaryButtonText = "";
                    dialog.PrimaryButtonText = CoreTools.Translate("Please wait");
                    infotext.Text =
                        CoreTools.Translate(
                            "Please wait while {0} is being installed. A black window may show up. Please wait until it closes.",
                            dep_name);
                    Process p = new()
                    {
                        StartInfo = new ProcessStartInfo { FileName = exe_name, Arguments = exe_args, },
                    };
                    p.Start();
                    await p.WaitForExitAsync();
                    dialog.IsPrimaryButtonEnabled = true;
                    dialog.IsSecondaryButtonEnabled = true;
                    if (current < total)
                    {
                        // When finished, but more dependencies need to be installed
                        infotext.Text = CoreTools.Translate("{0} has been installed successfully.", dep_name) +
                                        " " + CoreTools.Translate("Please click on \"Continue\" to continue",
                                            dep_name);
                        dialog.SecondaryButtonText = "";
                        dialog.PrimaryButtonText = CoreTools.Translate("Continue");
                    }
                    else
                    {
                        // When finished, and no more dependencies need to be installed
                        infotext.Text =
                            CoreTools.Translate(
                                "{0} has been installed successfully. It is recommended to restart UniGetUI to finish the installation",
                                dep_name);
                        dialog.SecondaryButtonText = CoreTools.Translate("Restart later");
                        dialog.PrimaryButtonText = CoreTools.Translate("Restart UniGetUI");
                    }
                }
                catch (Exception ex)
                {
                    // If an error occurs
                    Logger.Error(ex);
                    dialog.IsPrimaryButtonEnabled = true;
                    dialog.IsSecondaryButtonEnabled = true;
                    infotext.Text = CoreTools.Translate("An error occurred:") + " " + ex.Message + "\n" +
                                    CoreTools.Translate("Please click on \"Continue\" to continue");
                    dialog.SecondaryButtonText = "";
                    dialog.PrimaryButtonText = (current < total)
                        ? CoreTools.Translate("Continue")
                        : CoreTools.Translate("Close");
                }

                has_installed = true;
                progress.Opacity = .0F;
                progress.IsIndeterminate = false;
            }
            else
            {
                // If this is the last dependency
                if (current == total)
                {
                    block_closing = true;
                    MainApp.Instance.KillAndRestart();
                }
            }
        };

        dialog.Closing += (_, e) =>
        {
            e.Cancel = block_closing;
            block_closing = false;
        };
        dialog.Content = p;
        await Window.ShowDialogAsync(dialog);
    }

    public static async Task ManageIgnoredUpdates()
    {
        ContentDialog? UpdatesDialog = new()
        {
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style, XamlRoot = Window.XamlRoot
        };
        UpdatesDialog.Resources["ContentDialogMaxWidth"] = 1400;
        UpdatesDialog.Resources["ContentDialogMaxHeight"] = 1000;

        UpdatesDialog.SecondaryButtonText = CoreTools.Translate("Close");

        //UpdatesDialog.PrimaryButtonText = CoreTools.Translate("Reset");

        UpdatesDialog.DefaultButton = ContentDialogButton.None;
        UpdatesDialog.Title = CoreTools.Translate("Manage ignored updates");
        IgnoredUpdatesManager IgnoredUpdatesPage = new();
        UpdatesDialog.Content = IgnoredUpdatesPage;
        IgnoredUpdatesPage.Close += (_, _) => UpdatesDialog.Hide();

        _ = IgnoredUpdatesPage.UpdateData();
        await Window.ShowDialogAsync(UpdatesDialog);
    }


    public static async void WarnAboutAdminRights()
    {
        ContentDialog AdminDialog = new()
        {
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style
        };

        while (Window.XamlRoot is null)
        {
            await Task.Delay(100);
        }

        AdminDialog.XamlRoot = Window.XamlRoot;
        AdminDialog.PrimaryButtonText = CoreTools.Translate("I understand");
        AdminDialog.DefaultButton = ContentDialogButton.Primary;
        AdminDialog.Title = CoreTools.Translate("Administrator privileges");
        AdminDialog.Content = CoreTools.Translate(
            "WingetUI has been ran as administrator, which is not recommended. When running WingetUI as administrator, EVERY operation launched from WingetUI will have administrator privileges. You can still use the program, but we highly recommend not running WingetUI with administrator privileges.");

        await Window.ShowDialogAsync(AdminDialog);
    }


    public static async Task ShowAboutUniGetUI()
    {
        ContentDialog? AboutDialog = new();
        AboutUniGetUI AboutPage = new();
        AboutDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
        AboutDialog.XamlRoot = Window.XamlRoot;
        AboutDialog.Resources["ContentDialogMaxWidth"] = 1200;
        AboutDialog.Resources["ContentDialogMaxHeight"] = 1000;
        AboutDialog.Content = AboutPage;
        AboutDialog.PrimaryButtonText = CoreTools.Translate("Close");
        AboutPage.Close += (_, _) => AboutDialog.Hide();

        await Window.ShowDialogAsync(AboutDialog);
    }

    public static async void ShowReleaseNotes()
    {
        ContentDialog? NotesDialog = new()
        {
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            XamlRoot = Window.XamlRoot
        };
        NotesDialog.Resources["ContentDialogMaxWidth"] = 12000;
        NotesDialog.Resources["ContentDialogMaxHeight"] = 10000;
        NotesDialog.CloseButtonText = CoreTools.Translate("Close");
        NotesDialog.Title = CoreTools.Translate("Release notes");
        ReleaseNotes? notes = new();
        notes.Close += (_, _) => NotesDialog.Hide();
        NotesDialog.Content = notes;
        NotesDialog.SizeChanged += (_, _) =>
        {
            notes.MinWidth = Math.Abs(Window.NavigationPage.ActualWidth - 300);
            notes.MinHeight = Math.Abs(Window.NavigationPage.ActualHeight - 200);
        };

        await Window.ShowDialogAsync(NotesDialog);
    }

    public static async void HandleBrokenWinGet()
    {
        bool bannerWasOpen = false;
        try
        {
            DialogHelper.ShowLoadingDialog("Attempting to repair WinGet...",
                "WinGet is being repaired. Please wait until the process finishes.");
            bannerWasOpen = Window.WinGetWarningBanner.IsOpen;
            Window.WinGetWarningBanner.IsOpen = false;
            Process p = new Process()
            {
                StartInfo = new()
                {
                    FileName =
                        Path.Join(Environment.SystemDirectory,
                            "windowspowershell\\v1.0\\powershell.exe"),
                    Arguments =
                        "-ExecutionPolicy Bypass -NoLogo -NoProfile -Command \"& {" +
                        "cmd.exe /C \"rmdir /Q /S `\"%temp%\\WinGet`\"\"; " +
                        "cmd.exe /C \"`\"%localappdata%\\Microsoft\\WindowsApps\\winget.exe`\" source reset --force\"; " +
                        "taskkill /im winget.exe /f; " +
                        "taskkill /im WindowsIPackageManagerServer.exe /f; " +
                        "Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force; " +
                        "Install-Module Microsoft.WinGet.Client -Force -Scope AllUsers -AllowClobber; " +
                        "Import-Module Microsoft.WinGet.Client; " +
                        "Repair-WinGetIPackageManager -Force -Latest; " +
                        "Get-AppxPackage -Name 'Microsoft.DesktopAppInstaller' | Reset-AppxPackage" +
                        "}\"",
                    UseShellExecute = true,
                    Verb = "runas"
                }
            };
            p.Start();
            await p.WaitForExitAsync();
            DialogHelper.HideLoadingDialog();

            // Toggle bundled WinGet
            if (Settings.Get("ForceLegacyBundledWinGet"))
                Settings.Set("ForceLegacyBundledWinGet", false);

            var c = new ContentDialog()
            {
                Title = CoreTools.Translate("WinGet was repaired successfully"),
                Content = CoreTools.Translate("It is recommended to restart UniGetUI after WinGet has been repaired") +
                          "\n\n" +
                          CoreTools.Translate(
                              "NOTE: This troubleshooter can be disabled from UniGetUI Settings, on the WinGet section"),
                PrimaryButtonText = CoreTools.Translate("Close"),
                SecondaryButtonText = CoreTools.Translate("Restart"),
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = Window.XamlRoot
            };

            // Restart UniGetUI or reload packages depending on the user's choice
            if (await Window.ShowDialogAsync(c) == ContentDialogResult.Secondary)
            {
                MainApp.Instance.KillAndRestart();
            }
            else
            {
                _ = PEInterface.UpgradablePackagesLoader.ReloadPackages();
                _ = PEInterface.InstalledPackagesLoader.ReloadPackages();
            }
        }
        catch (Exception ex)
        {
            // Show an error message if something goes wrong
            Window.WinGetWarningBanner.IsOpen = bannerWasOpen;
            Logger.Error("An error occurred while trying to repair WinGet");
            Logger.Error(ex);
            DialogHelper.HideLoadingDialog();

            var c = new ContentDialog()
            {
                Title = CoreTools.Translate("WinGet could not be repaired"),
                Content =
                    CoreTools.Translate("An unexpected issue occurred while attempting to repair WinGet. Please try again later") +
                    "\n\n" + ex.Message + "\n\n" +
                    CoreTools.Translate("NOTE: This troubleshooter can be disabled from UniGetUI Settings, on the WinGet section"),
                PrimaryButtonText = CoreTools.Translate("Close"),
                DefaultButton = ContentDialogButton.None,
                XamlRoot = Window.XamlRoot
            };

            await Window.ShowDialogAsync(c);
        }

    }
}


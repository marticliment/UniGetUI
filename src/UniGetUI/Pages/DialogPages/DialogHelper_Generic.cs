using System.Diagnostics;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface;
using UniGetUI.Interface.Dialogs;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Classes.Packages.Classes;

namespace UniGetUI.Pages.DialogPages;

public static partial class DialogHelper
{
    private static class DialogFactory
    {
        public static ContentDialog Create()
        {
            var dialog = new ContentDialog()
            {
                XamlRoot = Window.MainContentGrid.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style
            };
            return dialog;
        }

        public static ContentDialog Create_AsWindow(bool hasTitle, bool hasButtons = false)
        {
            var dialog = Create();
            dialog.Resources["ContentDialogMaxWidth"] = 8192;
            dialog.Resources["ContentDialogMaxHeight"] = 4096;
            dialog.SizeChanged += (_, _) =>
            {
                if (dialog.Content is FrameworkElement page)
                {
                    double maxW, maxH;
                    int tresholdW = 1300, tresholdH = 1300;
                    if (Window.NavigationPage.ActualWidth < tresholdW) maxW = 100;
                    else if (Window.NavigationPage.ActualWidth >= tresholdW + 200) maxW = 300;
                    else maxW = Window.NavigationPage.ActualWidth - (tresholdW - 100);

                    if (Window.NavigationPage.ActualHeight < tresholdH) maxH = (hasTitle? 104: 64) + (hasButtons? 80: 0);
                    else if (Window.NavigationPage.ActualHeight >= tresholdH + 200) maxH = (hasTitle ? 320 : 280) + (hasButtons ? 80 : 0);
                    else maxH = Window.NavigationPage.ActualHeight - (tresholdH - (hasTitle ? 120 : 80)) + (hasButtons ? 80 : 0);

                    page.Width = Math.Min(Math.Abs(Window.NavigationPage.ActualWidth - maxW), 8192);
                    page.Height = Math.Min(Math.Abs(Window.NavigationPage.ActualHeight - maxH), 4096);
                }
            };
            return dialog;
        }
    }

    public static MainWindow Window { private get; set; } = null!;

    public static void ShowLoadingDialog(string text)
    {
        ShowLoadingDialog(text, "");
    }

    public static async void ShowLoadingDialog(string title, string description)
    {
        while (Window.LoadingDialogCount == 0 && Window.DialogQueue.Count != 0) await Task.Delay(100);

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

        if (Settings.GetDictionaryItem<string, string>(Settings.K.DependencyManagement, dep_name) == "skipped")
        {
            Logger.Error(
                $"Dependency {dep_name} was not found, and the user set it to not be reminded of the missing dependency");
            return;
        }

        bool NotFirstTime = Settings.GetDictionaryItem<string, string>(Settings.K.DependencyManagement, dep_name) == "attempted";
        Settings.SetDictionaryItem(Settings.K.DependencyManagement, dep_name, "attempted");

        var dialog = DialogFactory.Create();
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
            c.Checked += (_, _) => Settings.SetDictionaryItem(Settings.K.DependencyManagement, dep_name, "skipped");
            c.Unchecked += (_, _) => Settings.SetDictionaryItem(Settings.K.DependencyManagement, dep_name, "attempted");
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
                    Process install_dep_p = new()
                    {
                        StartInfo = new ProcessStartInfo { FileName = exe_name, Arguments = exe_args, },
                    };
                    install_dep_p.Start();
                    await install_dep_p.WaitForExitAsync();
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
        ContentDialog dialog = DialogFactory.Create_AsWindow(true);

        dialog.Title = CoreTools.Translate("Manage ignored updates");

        IgnoredUpdatesManager IgnoredUpdatesPage = new();
        dialog.Content = IgnoredUpdatesPage;
        IgnoredUpdatesPage.Close += (_, _) => dialog.Hide();
        await Window.ShowDialogAsync(dialog);
    }

    public static async Task ManageDesktopShortcuts(IReadOnlyList<string>? NewShortucts  = null)
    {
        ContentDialog dialog = DialogFactory.Create_AsWindow(true);

        DesktopShortcutsManager DesktopShortcutsPage = new();
        DesktopShortcutsPage.LoadShortcuts(NewShortucts ?? DesktopShortcutsDatabase.GetAllShortcuts());
        DesktopShortcutsPage.Close += (_, _) => dialog.Hide();

        dialog.Title = CoreTools.Translate("Automatic desktop shortcut remover");
        dialog.Content = DesktopShortcutsPage;

        await Window.ShowDialogAsync(dialog);
    }

    public static async Task HandleNewDesktopShortcuts()
    {
        var unknownShortcuts = DesktopShortcutsDatabase.GetUnknownShortcuts();

        if (!Settings.AreNotificationsDisabled())
        {
            await AppNotificationManager.Default.RemoveByTagAsync(CoreData.NewShortcutsNotificationTag.ToString());
            AppNotification notification;

            if (unknownShortcuts.Count == 1)
            {
                AppNotificationBuilder builder = new AppNotificationBuilder()
                    .SetScenario(AppNotificationScenario.Default)
                    .SetTag(CoreData.NewShortcutsNotificationTag.ToString())
                    .AddText(CoreTools.Translate("Desktop shortcut created"))
                    .AddText(CoreTools.Translate("UniGetUI has detected a new desktop shortcut that can be deleted automatically."))
                    .SetAttributionText(unknownShortcuts.First().Split("\\").Last())
                    .AddButton(new AppNotificationButton(CoreTools.Translate("Open UniGetUI").Replace("'", "´"))
                        .AddArgument("action", NotificationArguments.Show)
                    )
                    .AddArgument("action", NotificationArguments.Show);

                notification = builder.BuildNotification();
            }
            else
            {
                string attribution = "";
                foreach (string shortcut in unknownShortcuts)
                {
                    attribution += shortcut.Split("\\").Last() + ", ";
                }

                attribution = attribution.TrimEnd(' ').TrimEnd(',');

                AppNotificationBuilder builder = new AppNotificationBuilder()
                    .SetScenario(AppNotificationScenario.Default)
                    .SetTag(CoreData.NewShortcutsNotificationTag.ToString())
                    .AddText(CoreTools.Translate("{0} desktop shortcuts created", unknownShortcuts.Count))
                    .AddText(CoreTools.Translate("UniGetUI has detected {0} new desktop shortcuts that can be deleted automatically.", unknownShortcuts.Count))
                    .SetAttributionText(attribution)
                    .AddButton(new AppNotificationButton(CoreTools.Translate("Open UniGetUI").Replace("'", "´"))
                        .AddArgument("action", NotificationArguments.ShowOnUpdatesTab)
                    )
                    .AddArgument("action", NotificationArguments.ShowOnUpdatesTab);

                notification = builder.BuildNotification();
            }

            notification.ExpiresOnReboot = true;
            AppNotificationManager.Default.Show(notification);
        }

        await ManageDesktopShortcuts(unknownShortcuts);
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
        ContentDialog AboutDialog = DialogFactory.Create_AsWindow(false, false);
        AboutUniGetUI AboutPage = new();
        AboutDialog.Content = AboutPage;
        AboutPage.Close += (_, _) => AboutDialog.Hide();

        await Window.ShowDialogAsync(AboutDialog);
    }

    public static async void ShowReleaseNotes()
    {
        ContentDialog NotesDialog = DialogFactory.Create_AsWindow(true);

        NotesDialog.Title = CoreTools.Translate("Release notes");
        ReleaseNotes notes = new();
        notes.Close += (_, _) => NotesDialog.Hide();
        NotesDialog.Content = notes;
        await Window.ShowDialogAsync(NotesDialog);
        notes.Dispose();
    }

    public static async void HandleBrokenWinGet()
    {
        bool bannerWasOpen = false;
        try
        {
            ShowLoadingDialog("Attempting to repair WinGet...",
                "WinGet is being repaired. Please wait until the process finishes.");
            bannerWasOpen = Window.WinGetWarningBanner.IsOpen;
            Window.WinGetWarningBanner.IsOpen = false;
            using Process p = new Process
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
                        "taskkill /im WindowsPackageManagerServer.exe /f; " +
                        "Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force; " +
                        "Install-Module Microsoft.WinGet.Client -Force -AllowClobber; " +
                        "Import-Module Microsoft.WinGet.Client; " +
                        "Repair-WinGetPackageManager -Force -Latest; " +
                        "Get-AppxPackage -Name 'Microsoft.DesktopAppInstaller' | Reset-AppxPackage; " +
                        "}\"",
                    UseShellExecute = true,
                    Verb = "runas"
                }
            };
            p.Start();
            await p.WaitForExitAsync();
            HideLoadingDialog();

            // Toggle bundled WinGet
            if (Settings.Get(Settings.K.ForceLegacyBundledWinGet))
                Settings.Set(Settings.K.ForceLegacyBundledWinGet, false);

            var c = DialogFactory.Create();
            c.Title = CoreTools.Translate("WinGet was repaired successfully");
            c.Content = CoreTools.Translate("It is recommended to restart UniGetUI after WinGet has been repaired") +
                        "\n\n" +
                        CoreTools.Translate(
                            "NOTE: This troubleshooter can be disabled from UniGetUI Settings, on the WinGet section");
            c.PrimaryButtonText = CoreTools.Translate("Close");
            c.SecondaryButtonText = CoreTools.Translate("Restart");
            c.DefaultButton = ContentDialogButton.Secondary;

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
            HideLoadingDialog();

            var c = DialogFactory.Create();
            c.Title = CoreTools.Translate("WinGet could not be repaired");
            c.Content = CoreTools.Translate(
                            "An unexpected issue occurred while attempting to repair WinGet. Please try again later") +
                        "\n\n" + ex.Message + "\n\n" + CoreTools.Translate(
                            "NOTE: This troubleshooter can be disabled from UniGetUI Settings, on the WinGet section");
            c.PrimaryButtonText = CoreTools.Translate("Close");
            c.DefaultButton = ContentDialogButton.None;
            await Window.ShowDialogAsync(c);
        }

    }

    public static async void ShowTelemetryDialog()
    {
        var dialog = DialogFactory.Create();
        dialog.Title = CoreTools.Translate("Share anonymous usage data");

        var MessageBlock = new RichTextBlock();
        dialog.Content = MessageBlock;

        var p = new Paragraph();
        MessageBlock.Blocks.Add(p);

        p.Inlines.Add(new Run
        {
            Text = CoreTools.Translate("UniGetUI collects anonymous usage data with the sole purpose of understanding and improving the user experience.")
        });
        p.Inlines.Add(new LineBreak());
        p.Inlines.Add(new Run
        {
            Text = CoreTools.Translate("No personal information is collected nor sent, and the collected data is anonimized, so it can't be back-tracked to you.")
        });
        p.Inlines.Add(new LineBreak());
        p.Inlines.Add(new LineBreak());
        var link = new Hyperlink { NavigateUri = new Uri("https://www.marticliment.com/unigetui/privacy/"), };
        link.Inlines.Add(new Run
        {
            Text = CoreTools.Translate("More details about the shared data and how it will be processed"),
        });

        p.Inlines.Add(link);
        p.Inlines.Add(new LineBreak());
        p.Inlines.Add(new LineBreak());
        p.Inlines.Add(new Run
        {
            Text = CoreTools.Translate("Do you accept that UniGetUI collects and sends anonymous usage statistics, with the sole purpose of understanding and improving the user experience?"),
            FontWeight = FontWeights.SemiBold
        });

        dialog.SecondaryButtonText = CoreTools.Translate("Decline");
        dialog.PrimaryButtonText = CoreTools.Translate("Accept");
        dialog.DefaultButton = ContentDialogButton.Primary;
        dialog.Closing += (_, e) =>
        {
            if (e.Result == ContentDialogResult.None) e.Cancel = true;
        };

        var res = await Window.ShowDialogAsync(dialog);

        if (res is ContentDialogResult.Primary)
        {
            Settings.Set(Settings.K.DisableTelemetry, false);
        }
        else
        {
            Settings.Set(Settings.K.DisableTelemetry, true);
        }
    }

    public static void ShowTelemetryBanner()
    {
        Window.TelemetryWarner.Title = CoreTools.Translate("Share anonymous usage data");
        Window.TelemetryWarner.Message = CoreTools.Translate("UniGetUI collects anonymous usage data in order to improve the user experience.");
        Window.TelemetryWarner.IsOpen = true;

        Window.TelemetryWarner.IsClosable = true;
        Window.TelemetryWarner.Visibility = Visibility.Visible;

        var AcceptBtn = new Button()
        {
            Content = CoreTools.Translate("Accept"),
            Style = Application.Current.Resources["AccentButtonStyle"] as Style
        };
        AcceptBtn.Click += (_, _) =>
        {
            Window.TelemetryWarner.Visibility = Visibility.Collapsed;
            Window.TelemetryWarner.IsOpen = false;
            Settings.Set(Settings.K.ShownTelemetryBanner, true);
        };

        var SettingsBtn = new Button()
        {
            Content = CoreTools.Translate("Settings"),
        };
        SettingsBtn.Click += (_, _) =>
        {
            Window.TelemetryWarner.Visibility = Visibility.Collapsed;
            Window.TelemetryWarner.IsOpen = false;
            ShowTelemetryDialog();
            Settings.Set(Settings.K.ShownTelemetryBanner, true);
        };

        StackPanel btns = new() { Margin = new Thickness(4,0,4,0), Spacing = 4, Orientation = Orientation.Horizontal };
        btns.Children.Add(AcceptBtn);
        btns.Children.Add(SettingsBtn);

        var mainButton = Window.TelemetryWarner.ActionButton = new HyperlinkButton()
        {
            Padding = new Thickness(0),
            Content = btns,
            Background = new SolidColorBrush(Colors.Transparent),
            BorderBrush = new SolidColorBrush(Colors.Transparent),
        };
        mainButton.Resources["HyperlinkButtonBackgroundPointerOver"] = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

        Window.TelemetryWarner.CloseButtonClick += (_, _) => Settings.Set(Settings.K.ShownTelemetryBanner, true);

    }

    public static async Task ConfirmSetDeleteAllShortcutsSetting()
    {
        var dialog = DialogFactory.Create();
        dialog.Title = CoreTools.Translate("Are you sure you want to delete all shortcuts?");
        dialog.Content = CoreTools.Translate("Any new shorcuts created during an install or an update operation will be deleted automatically, instead of showing a confirmation prompt the first time they are detected.")
                            + " " + CoreTools.Translate("Any shorcuts created or modified outside of UniGetUI will be ignored. You will be able to add them via the {0} button.", $"\"{CoreTools.Translate("Manual scan")}\"")
                        + " " + CoreTools.Translate("Are you really sure you want to enable this feature?");
        dialog.PrimaryButtonText = CoreTools.Translate("Yes");
        dialog.CloseButtonText = CoreTools.Translate("No");
        dialog.DefaultButton = ContentDialogButton.Close;
        if (await Window.ShowDialogAsync(dialog) is ContentDialogResult.Primary)
        {
            Settings.Set(Settings.K.RemoveAllDesktopShortcuts, true);
        }
        _ = ManageDesktopShortcuts();
    }

    /*public static async Task ManualScanDidNotFoundNewShortcuts()
    {
        var dialog = DialogFactory.Create();
        dialog.Title = CoreTools.Translate("Manual scan");
        dialog.Content = CoreTools.Translate("No new shortcuts were found during the scan.");
        dialog.PrimaryButtonText = CoreTools.Translate("Ok");
        dialog.DefaultButton = ContentDialogButton.Primary;
        await Window.ShowDialogAsync(dialog);
    }*/

    public static async Task HowToAddPackagesToBundle()
    {
        var dialog = DialogFactory.Create();
        dialog.Title = CoreTools.Translate("How to add packages to a bundle");
        dialog.Content = CoreTools.Translate("In order to add packages to a bundle, you will need to: ")
                         + "\n   " + CoreTools.Translate("1. Navigate to the \"{0}\" or \"{1}\" page.", CoreTools.Translate("Discover packages"), CoreTools.Translate("Installed packages"))
                         + "\n   " + CoreTools.Translate("2. Locate the package(s) you want to add to the bundle, and select their leftmost checkbox.")
                         + "\n   " + CoreTools.Translate("3. When the packages you want to add to the bundle are selected, find and click the option \"{0}\" on the toolbar.", CoreTools.Translate("Add selection to bundle"))
                         + "\n   " + CoreTools.Translate("4. Your packages will have been added to the bundle. You can continue adding packages, or export the bundle.");
        dialog.PrimaryButtonText = CoreTools.Translate("Discover packages");
        dialog.SecondaryButtonText = CoreTools.Translate("Installed packages");
        dialog.CloseButtonText = CoreTools.Translate("Close");
        dialog.DefaultButton = ContentDialogButton.None;
        var result = await Window.ShowDialogAsync(dialog);
        if(result is ContentDialogResult.Primary) Window.NavigationPage.NavigateTo(PageType.Discover);
        else if(result is ContentDialogResult.Secondary) Window.NavigationPage.NavigateTo(PageType.Installed);
    }

    public static void ShowDismissableBalloon(string title, string message)
    {
        Window.DismissableNotification.Title = title;
        Window.DismissableNotification.Content = new TextBlock() { Text = message, TextWrapping = TextWrapping.Wrap };
        Window.DismissableNotification.IsOpen = true;
    }
}

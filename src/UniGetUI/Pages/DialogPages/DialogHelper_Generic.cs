using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface;
using UniGetUI.Interface.Dialogs;

namespace UniGetUI.Pages.DialogPages;

public static partial class DialogHelper
{
    public static MainWindow Window { private get; set; } = null!;

    public static void ShowLoadingDialog(string text)
    {
        if (Window.LoadingDialogCount == 0 && Window.DialogQueue.Count == 0)
        {
            Window.LoadingSthDalog.Title = text;
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

        string PREVIOUSLY_ATTEMPTED_PREF = $"AlreadyAttemptedToInstall{dep_name}";
        string DEP_SKIPPED_PREF = $"SkippedInstalling{dep_name}";

        if (Settings.Get(DEP_SKIPPED_PREF))
        {
            Logger.Error(
                $"Dependency {dep_name} was not found, and the user set it to not be reminded of the midding dependency");
            return;
        }

        bool NotFirstTime = Settings.Get(PREVIOUSLY_ATTEMPTED_PREF);
        Settings.Set(PREVIOUSLY_ATTEMPTED_PREF, true);

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
                $"UniGetUI requires {dep_name} to operate, but it was not found on your system."),
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
            c.Checked += (_, _) => Settings.Set(DEP_SKIPPED_PREF, true);
            c.Unchecked += (_, _) => Settings.Set(DEP_SKIPPED_PREF, false);
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

        UpdatesDialog.DefaultButton = ContentDialogButton.Secondary;
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
}


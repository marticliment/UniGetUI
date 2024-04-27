using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UniGetUI.Core;
using UniGetUI.Core.Data;
using ExternalLibraries.Clipboard;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Language;
using UniGetUI.Core.Tools;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsInterface : Page
    {
        HyperlinkButton ResetBackupDirectory;
        HyperlinkButton OpenBackupDirectory;
        TextBlock BackupDirectoryLabel;


        public SettingsInterface()
        {
            InitializeComponent();

            // General Settings Section
            Dictionary<string, string> lang_dict = new(LanguageData.LanguageReference.AsEnumerable());

            foreach (string key in lang_dict.Keys)
            {
                if (LanguageData.TranslationPercentages.ContainsKey(key) && key != "en")
                    lang_dict[key] = lang_dict[key] + " (" + LanguageData.TranslationPercentages[key] + ")";
            }

            bool isFirst = true;
            foreach (KeyValuePair<string, string> entry in lang_dict)
            {
                LanguageSelector.AddItem(entry.Value, entry.Key.ToString(), isFirst);
                isFirst = false;
            }
            LanguageSelector.ShowAddedItems();



            Dictionary<string, string> updates_dict = new()
            {
                {CoreTools.Translate("{0} minutes").Replace("{0}", "10"), "600"},
                {CoreTools.Translate("{0} minutes").Replace("{0}", "30"), "1800"},
                {CoreTools.Translate("1 hour"), "3600"},
                {CoreTools.Translate("{0} hours").Replace("{0}", "2"), "7200"},
                {CoreTools.Translate("{0} hours").Replace("{0}", "4"), "14400"},
                {CoreTools.Translate("{0} hours").Replace("{0}", "8"), "28800"},
                {CoreTools.Translate("{0} hours").Replace("{0}", "12"), "43200"},
                {CoreTools.Translate("1 day"), "86400"},
                {CoreTools.Translate("{0} days").Replace("{0}", "2"), "172800"},
                {CoreTools.Translate("{0} days").Replace("{0}", "3"), "259200"},
                {CoreTools.Translate("1 week"), "604800"}
            };

            foreach (KeyValuePair<string, string> entry in updates_dict)
            {
                UpdatesCheckIntervalSelector.AddItem(entry.Key, entry.Value.ToString(), false);
            }
            UpdatesCheckIntervalSelector.ShowAddedItems();

            if (Settings.GetValue("PreferredTheme") == "")
                Settings.SetValue("PreferredTheme", "auto");

            ThemeSelector.AddItem(CoreTools.AutoTranslated("Light"), "light");
            ThemeSelector.AddItem(CoreTools.AutoTranslated("Dark"), "dark");
            ThemeSelector.AddItem(CoreTools.AutoTranslated("Follow system color scheme"), "auto");
            ThemeSelector.ShowAddedItems();

            // Backup Section
            BackupDirectoryLabel = (TextBlock)(((StackPanel)ChangeBackupDirectory.Description).Children.ElementAt(0));
            ResetBackupDirectory = (HyperlinkButton)(((StackPanel)ChangeBackupDirectory.Description).Children.ElementAt(1));
            OpenBackupDirectory = (HyperlinkButton)(((StackPanel)ChangeBackupDirectory.Description).Children.ElementAt(2));
            if (!Settings.Get("ChangeBackupOutputDirectory"))
            {
                BackupDirectoryLabel.Text = CoreData.UniGetUI_DefaultBackupDirectory;
                ResetBackupDirectory.IsEnabled = false;
            }
            else
            {
                BackupDirectoryLabel.Text = Settings.GetValue("ChangeBackupOutputDirectory");
                ResetBackupDirectory.IsEnabled = true;
            }

            ResetBackupDirectory.Content = CoreTools.Translate("Reset");

            OpenBackupDirectory.Content = CoreTools.Translate("Open");

            // Admin Settings Section
            int index = 2;
            foreach (PackageManager manager in MainApp.Instance.PackageManagerList)
            {
            }

            // Experimental Settings Section
            ExperimentalSettingsExpander.HideRestartRequiredBanner();


            // Package Manager banners;
            Dictionary<PackageManager, SettingsEntry> PackageManagerExpanders = new();
            Dictionary<PackageManager, List<SettingsCard>> ExtraSettingsCards = new();

            foreach (PackageManager Manager in MainApp.Instance.PackageManagerList)
            {
                ExtraSettingsCards.Add(Manager, new List<SettingsCard>());
            }


            ButtonCard Winget_ResetSources = new() { Text = CoreTools.AutoTranslated("Reset Winget sources (might help if no packages are listed)"), ButtonText = CoreTools.AutoTranslated("Reset") };
            Winget_ResetSources.Click += (s, e) =>
            {
                CoreTools.LaunchBatchFile(Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "reset_winget_sources.cmd"), CoreTools.Translate("Resetting Winget sources - WingetUI"), RunAsAdmin: true);
            };

            ExtraSettingsCards[MainApp.Winget].Add(Winget_ResetSources);

            ButtonCard Scoop_Install = new() { Text = CoreTools.AutoTranslated("Install Scoop"), ButtonText = CoreTools.AutoTranslated("Install") };
            Scoop_Install.Click += (s, e) =>
            {
                CoreTools.LaunchBatchFile(Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "install_scoop.cmd"), CoreTools.Translate("Scoop Installer - WingetUI"));
                PackageManagerExpanders[MainApp.Scoop].ShowRestartRequiredBanner();
            };
            ButtonCard Scoop_Uninstall = new() { Text = CoreTools.AutoTranslated("Uninstall Scoop (and its packages)"), ButtonText = CoreTools.AutoTranslated("Uninstall") };
            Scoop_Uninstall.Click += (s, e) =>
            {
                CoreTools.LaunchBatchFile(Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "uninstall_scoop.cmd"), CoreTools.Translate("Scoop Uninstaller - WingetUI"));
                PackageManagerExpanders[MainApp.Scoop].ShowRestartRequiredBanner();
            };
            ButtonCard Scoop_ResetAppCache = new() { Text = CoreTools.AutoTranslated("Run cleanup and clear cache"), ButtonText = CoreTools.AutoTranslated("Run") };
            Scoop_ResetAppCache.Click += (s, e) =>
            {
                CoreTools.LaunchBatchFile(Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "scoop_cleanup.cmd"), CoreTools.Translate("Clearing Scoop cache - WingetUI"), RunAsAdmin: true);
            };

            ExtraSettingsCards[MainApp.Scoop].Add(Scoop_Install);
            ExtraSettingsCards[MainApp.Scoop].Add(Scoop_Uninstall);
            ExtraSettingsCards[MainApp.Scoop].Add(Scoop_ResetAppCache);

            CheckboxCard Chocolatey_SystemChoco = new() { Text = CoreTools.AutoTranslated("Use system Chocolatey"), SettingName = "UseSystemChocolatey" };
            Chocolatey_SystemChoco.StateChanged += (s, e) =>
            {
                PackageManagerExpanders[MainApp.Choco].ShowRestartRequiredBanner();
            };

            ExtraSettingsCards[MainApp.Choco].Add(Chocolatey_SystemChoco);



            foreach (PackageManager Manager in MainApp.Instance.PackageManagerList)
            {

                SettingsEntry ManagerExpander = new()
                {
                    Text = Manager.Name,
                    Description = Manager.Properties.Description.Replace("<br>", "\n").Replace("<b>", "").Replace("</b>", "")
                };
                PackageManagerExpanders.Add(Manager, ManagerExpander);

                InfoBar ManagerStatus = new();

                TextBlock LongVersion = new();
                HyperlinkButton ShowVersionButton = new();
                ShowVersionButton.Content = CoreTools.Translate("Expand version");
                ShowVersionButton.Visibility = Visibility.Collapsed;
                ManagerStatus.ActionButton = ShowVersionButton;
                ShowVersionButton.Click += (s, e) => { SetManagerStatus(Manager, true); };

                LongVersion.TextWrapping = TextWrapping.Wrap;
                LongVersion.Text = Manager.Status.Version + "\n";
                LongVersion.FontFamily = new FontFamily("Consolas");
                LongVersion.Visibility = Visibility.Collapsed;
                ManagerStatus.Content = LongVersion;

                void SetManagerStatus(PackageManager Manager, bool ShowVersion = false)
                {
                    ShowVersionButton.Visibility = Visibility.Collapsed;
                    LongVersion.Visibility = Visibility.Collapsed;
                    if (Manager.IsEnabled() && Manager.Status.Found)
                    {
                        ManagerStatus.Severity = InfoBarSeverity.Success;
                        ManagerStatus.Title = CoreTools.Translate("{pm} is enabled and ready to go").Replace("{pm}", Manager.Name);
                        if (!Manager.Status.Version.Contains("\n"))
                            ManagerStatus.Message = CoreTools.Translate("{pm} version:").Replace("{pm}", Manager.Name) + " " + Manager.Status.Version;
                        else if (ShowVersion)
                        {
                            ManagerStatus.Message = CoreTools.Translate("{pm} version:").Replace("{pm}", Manager.Name);
                            LongVersion.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            ManagerStatus.Message = "";
                            ShowVersionButton.Visibility = Visibility.Visible;
                        }

                    }
                    else if (Manager.IsEnabled() && !Manager.Status.Found)
                    {
                        ManagerStatus.Severity = InfoBarSeverity.Error;
                        ManagerStatus.Title = CoreTools.Translate("{pm} was not found!").Replace("{pm}", Manager.Name);
                        ManagerStatus.Message = CoreTools.Translate("You may need to install {pm} in order to use it with WingetUI.").Replace("{pm}", Manager.Name);
                    }
                    else if (!Manager.IsEnabled())
                    {
                        ManagerStatus.Severity = InfoBarSeverity.Informational;
                        ManagerStatus.Title = CoreTools.Translate("{pm} is disabled").Replace("{pm}", Manager.Name);
                        ManagerStatus.Message = CoreTools.Translate("Enable it to install packages from {pm}.").Replace("{pm}", Manager.Name);
                    }
                }

                ManagerStatus.IsClosable = false;
                ManagerStatus.IsOpen = true;
                ManagerStatus.CornerRadius = new CornerRadius(0);
                ManagerStatus.BorderThickness = new Thickness(0, 1, 0, 0);
                ManagerExpander.ItemsFooter = ManagerStatus;

                ManagerExpander.HeaderIcon = new LocalIcon(Manager.Properties.IconId);

                ToggleSwitch ManagerSwitch = new()
                {
                    IsOn = Manager.IsEnabled()
                };
                ManagerSwitch.Toggled += (s, e) =>
                {
                    Settings.Set("Disable" + Manager.Name, !ManagerSwitch.IsOn);
                    SetManagerStatus(Manager);
                    EnableOrDisableEntries();
                };

                ManagerExpander.Content = ManagerSwitch;

                void EnableOrDisableEntries()
                {
                    if (ExtraSettingsCards.ContainsKey(Manager))
                        foreach (SettingsCard card in ExtraSettingsCards[Manager])
                        {
                            if (ManagerSwitch.IsOn)
                                card.Visibility = Visibility.Visible;
                            else
                                card.Visibility = Visibility.Collapsed;
                        }
                }


                index = 0;

                SettingsCard ManagerPath = new() { Description = Manager.Status.ExecutablePath + " " + Manager.Properties.ExecutableCallArgs, IsClickEnabled = true };
                ManagerPath.ActionIcon = new SymbolIcon(Symbol.Copy);
                ManagerPath.Click += (s, e) =>
                {
                    WindowsClipboard.SetText(ManagerPath.Description.ToString());
                };
                ExtraSettingsCards[Manager].Insert(index++, ManagerPath);

                CheckboxCard AdminCard = new()
                {
                    Text = CoreTools.AutoTranslated("Always run {pm} operations with administrator rights"),
                    SettingName = "AlwaysElevate" + Manager.Name,
                };
                AdminCard._checkbox.Content = AdminCard._checkbox.Content.ToString().Replace("{pm}", Manager.Name);
                ExtraSettingsCards[Manager].Insert(index++, AdminCard);

                CheckboxCard ParallelCard = new()
                {
                    Text = CoreTools.AutoTranslated("Allow {pm} operations to be performed in parallel"),
                    SettingName = "AllowParallelInstallsForManager" + Manager.Name,
                };
                ParallelCard._checkbox.Content = ParallelCard._checkbox.Content.ToString().Replace("{pm}", Manager.Name);
                ExtraSettingsCards[Manager].Insert(index++, ParallelCard);


                if (Manager.Capabilities.SupportsCustomSources)
                {
                    SettingsCard SourceManagerCard = new();
                    SourceManagerCard.Resources["SettingsCardLeftIndention"] = 10;
                    SourceManager SourceManager = new(Manager);
                    SourceManagerCard.Description = SourceManager;
                    ExtraSettingsCards[Manager].Insert(index++, SourceManagerCard);
                }

                if (ExtraSettingsCards.ContainsKey(Manager))
                    foreach (SettingsCard card in ExtraSettingsCards[Manager])
                    {
                        ManagerExpander.Items.Add(card);
                    }

                SetManagerStatus(Manager);
                EnableOrDisableEntries();

                MainLayout.Children.Add(ManagerExpander);
            }
        }

        public MainWindow GetWindow()
        {
            return MainApp.Instance.MainWindow;
        }
        public int GetHwnd()
        {
            return (int)WinRT.Interop.WindowNative.GetWindowHandle(GetWindow());
        }

        private void OpenWelcomeWizard(object sender, Interface.Widgets.ButtonCardEventArgs e)
        {
        }

        private void ImportSettings(object sender, Interface.Widgets.ButtonCardEventArgs e)
        {
            ExternalLibraries.Pickers.FileOpenPicker picker = new(MainApp.Instance.MainWindow.GetWindowHandle());
            string file = picker.Show(new List<string> { "*.json" });

            if (file != string.Empty)
            {
                ResetWingetUI(sender, e);
                Dictionary<string, string> settings = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file));
                foreach (KeyValuePair<string, string> entry in settings)
                    Settings.SetValue(entry.Key, entry.Value);

                GeneralSettingsExpander.ShowRestartRequiredBanner();
            }
        }

        private async void ExportSettings(object sender, Interface.Widgets.ButtonCardEventArgs e)
        {
            try
            {
                ExternalLibraries.Pickers.FileSavePicker picker = new(MainApp.Instance.MainWindow.GetWindowHandle());
                string file = picker.Show(new List<string> { "*.json" }, CoreTools.Translate("WingetUI Settings") + ".json");

                if (file != String.Empty)
                {
                    MainApp.Instance.MainWindow.ShowLoadingDialog(CoreTools.Translate("Please wait..."));

                    string[] IgnoredSettings = new string[] { "OperationHistory", "CurrentSessionToken", "OldWindowGeometry" };

                    Dictionary<string, string> settings = new();
                    foreach (string path in Directory.EnumerateFiles(CoreData.UniGetUIDataDirectory))
                    {
                        if (Path.GetFileName(path).Contains('.') || IgnoredSettings.Contains(Path.GetFileName(path)))
                            continue;
                        settings.Add(Path.GetFileName(path), await File.ReadAllTextAsync(path));
                    }

                    await File.WriteAllTextAsync(file, JsonConvert.SerializeObject(settings));

                    MainApp.Instance.MainWindow.HideLoadingDialog();
                }
            }
            catch (Exception ex)
            {
                MainApp.Instance.MainWindow.HideLoadingDialog();
                Logger.Log(ex);
            }

        }

        private void ResetWingetUI(object sender, Interface.Widgets.ButtonCardEventArgs e)
        {
            try
            {
                foreach (string path in Directory.EnumerateFiles(CoreData.UniGetUIDataDirectory))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            GeneralSettingsExpander.ShowRestartRequiredBanner();
        }

        private void LanguageSelector_ValueChanged(object sender, Interface.Widgets.ComboCardEventArgs e)
        {
            GeneralSettingsExpander.ShowRestartRequiredBanner();
        }

        private void UpdatesCheckIntervalSelector_ValueChanged(object sender, Interface.Widgets.ComboCardEventArgs e)
        {
            GeneralSettingsExpander.ShowRestartRequiredBanner();
        }

        private void ThemeSelector_ValueChanged(object sender, Interface.Widgets.ComboCardEventArgs e)
        {
            ((MainApp)Application.Current).MainWindow.ApplyTheme();
        }

        private void ResetBackupPath_Click(object sender, dynamic e)
        {
            BackupDirectoryLabel.Text = CoreData.UniGetUI_DefaultBackupDirectory;
            Settings.Set("ChangeBackupOutputDirectory", false);
            ResetBackupDirectory.IsEnabled = false;
        }

        private void ChangeBackupDirectory_Click(object sender, dynamic e)
        {

            ExternalLibraries.Pickers.FolderPicker openPicker = new(MainApp.Instance.MainWindow.GetWindowHandle());
            string folder = openPicker.Show();
            if (folder != String.Empty)
            {
                Settings.SetValue("ChangeBackupOutputDirectory", folder);
                BackupDirectoryLabel.Text = folder;
                ResetBackupDirectory.IsEnabled = true;
            }
            else
            {
                ResetBackupPath_Click(sender, e);
            }

        }

        private void OpenBackupPath_Click(object sender, RoutedEventArgs e)
        {
            string directory = Settings.GetValue("ChangeBackupOutputDirectory");
            if (directory == "")
                directory = CoreData.UniGetUI_DefaultBackupDirectory;

            directory = directory.Replace("/", "\\");

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            Process.Start("explorer.exe", directory);

        }

        private void DoCacheAdminRights_StateChanged(object sender, Interface.Widgets.CheckBoxEventArgs e)
        {
            if (!e.IsChecked)
            {
                AdminSettingsExpander.ShowRestartRequiredBanner();
            }
        }

        private void UseSystemGSudo_StateChanged(object sender, Interface.Widgets.CheckBoxEventArgs e)
        { AdminSettingsExpander.ShowRestartRequiredBanner(); }

        private void DisableWidgetsApi_StateChanged(object sender, CheckBoxEventArgs e)
        { ExperimentalSettingsExpander.ShowRestartRequiredBanner(); }


        private void UseSystemWinget_StateChanged(object sender, CheckBoxEventArgs e)
        { ExperimentalSettingsExpander.ShowRestartRequiredBanner(); }


        private void DisableDownloadingNewTranslations_StateChanged(object sender, CheckBoxEventArgs e)
        { ExperimentalSettingsExpander.ShowRestartRequiredBanner(); }

        private void ForceArmWinget_StateChanged(object sender, CheckBoxEventArgs e)
        { ExperimentalSettingsExpander.ShowRestartRequiredBanner(); }

        private void TextboxCard_ValueChanged(object sender, TextboxEventArgs e)
        { ExperimentalSettingsExpander.ShowRestartRequiredBanner(); }

        private void ResetIconCache_Click(object sender, ButtonCardEventArgs e)
        {
            try
            {
                Directory.Delete(CoreData.UniGetUICacheDirectory_Icons, true);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            ExperimentalSettingsExpander.ShowRestartRequiredBanner();
        }

        private async void DoBackup_Click(object sender, ButtonCardEventArgs e)
        {
            MainApp.Instance.MainWindow.ShowLoadingDialog(CoreTools.Translate("Performing backup, please wait..."));
            await MainApp.Instance.MainWindow.NavigationPage.InstalledPage.BackupPackages();
            MainApp.Instance.MainWindow.HideLoadingDialog();
        }

        private void EditAutostartSettings_Click(object sender, ButtonCardEventArgs e)
        {
            Process p = new();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = "/c start ms-settings:startupapps",
                UseShellExecute = true,
                CreateNoWindow = true
            };
            p.Start();
        }
    }
}

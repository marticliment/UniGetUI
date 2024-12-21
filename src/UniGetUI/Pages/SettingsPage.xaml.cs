using System.Diagnostics;
using CommunityToolkit.WinUI.Controls;
using ExternalLibraries.Clipboard;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Newtonsoft.Json;
using UniGetUI.Core.Data;
using UniGetUI.Core.Language;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Pages;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Managers.VcpkgManager;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.Pages.DialogPages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : IEnterLeaveListener
    {
        private readonly HyperlinkButton ResetBackupDirectory;
        private readonly HyperlinkButton OpenBackupDirectory;
        private readonly TextBlock BackupDirectoryLabel;
        private readonly bool InterfaceLoaded;

        public SettingsPage()
        {
            InitializeComponent();

            // General Settings Section
            Dictionary<string, string> lang_dict = new(LanguageData.LanguageReference.AsEnumerable());

            foreach (string key in lang_dict.Keys)
            {
                if (key != "en" && LanguageData.TranslationPercentages.TryGetValue(key, out var translationPercentage))
                {
                    lang_dict[key] = lang_dict[key] + " (" + translationPercentage + ")";
                }
            }

            bool isFirst = true;
            foreach (KeyValuePair<string, string> entry in lang_dict)
            {
                LanguageSelector.AddItem(entry.Value, entry.Key, isFirst);
                isFirst = false;
            }
            LanguageSelector.ShowAddedItems();

            NotificationSettingsEntry.IsEnabled = DisableSystemTray.Checked;

            Dictionary<string, string> updates_dict = new()
            {
                {CoreTools.Translate("{0} minutes", 10), "600"},
                {CoreTools.Translate("{0} minutes", 30), "1800"},
                {CoreTools.Translate("1 hour"), "3600"},
                {CoreTools.Translate("{0} hours", 2), "7200"},
                {CoreTools.Translate("{0} hours", 4), "14400"},
                {CoreTools.Translate("{0} hours", 8), "28800"},
                {CoreTools.Translate("{0} hours", 12), "43200"},
                {CoreTools.Translate("1 day"), "86400"},
                {CoreTools.Translate("{0} days", 2), "172800"},
                {CoreTools.Translate("{0} days", 3), "259200"},
                {CoreTools.Translate("1 week"), "604800"}
            };

            foreach (KeyValuePair<string, string> entry in updates_dict)
            {
                UpdatesCheckIntervalSelector.AddItem(entry.Key, entry.Value, false);
            }
            UpdatesCheckIntervalSelector.ShowAddedItems();

            if (Settings.GetValue("PreferredTheme") == "")
            {
                Settings.SetValue("PreferredTheme", "auto");
            }

            ThemeSelector.AddItem(CoreTools.AutoTranslated("Light"), "light");
            ThemeSelector.AddItem(CoreTools.AutoTranslated("Dark"), "dark");
            ThemeSelector.AddItem(CoreTools.AutoTranslated("Follow system color scheme"), "auto");
            ThemeSelector.ShowAddedItems();

            StartupPageSelector.AddItem(CoreTools.AutoTranslated("Default"), "default");
            StartupPageSelector.AddItem(CoreTools.AutoTranslated("Discover Packages"), "discover");
            StartupPageSelector.AddItem(CoreTools.AutoTranslated("Software Updates"), "updates");
            StartupPageSelector.AddItem(CoreTools.AutoTranslated("Installed Packages"), "installed");
            StartupPageSelector.AddItem(CoreTools.AutoTranslated("Package Bundles"), "bundles");
            StartupPageSelector.AddItem(CoreTools.AutoTranslated("Settings"), "settings");
            StartupPageSelector.ShowAddedItems();

            // Backup Section
            BackupDirectoryLabel = (TextBlock)((StackPanel)ChangeBackupDirectory.Description).Children.ElementAt(0);
            ResetBackupDirectory = (HyperlinkButton)((StackPanel)ChangeBackupDirectory.Description).Children.ElementAt(1);
            OpenBackupDirectory = (HyperlinkButton)((StackPanel)ChangeBackupDirectory.Description).Children.ElementAt(2);

            EnablePackageBackupUI(Settings.Get("EnablePackageBackup"));
            ResetBackupDirectory.Content = CoreTools.Translate("Reset");
            OpenBackupDirectory.Content = CoreTools.Translate("Open");

            // Experimental Settings Section
            ExperimentalSettingsExpander.HideRestartRequiredBanner();

            // Package Manager banners;
            Dictionary<IPackageManager, SettingsEntry> IPackageManagerExpanders = [];
            Dictionary<IPackageManager, List<SettingsCard>> ExtraSettingsCards = [];

            foreach (IPackageManager Manager in PEInterface.Managers)
            {
                ExtraSettingsCards.Add(Manager, []);
            }

            // ----------------------------------------------------------------------------------------

            ButtonCard WinGet_ResetWindowsIPackageManager = new() {
                Text = CoreTools.AutoTranslated("Reset WinGet") + $" ({CoreTools.Translate("This may help if no packages are listed")})",
                ButtonText = CoreTools.AutoTranslated("Reset")
            };

            WinGet_ResetWindowsIPackageManager.Click += (_, _) =>
            {
                DialogHelper.HandleBrokenWinGet();
            };

            CheckboxCard WinGet_UseBundled = new()
            {
                Text = $"{CoreTools.Translate("Use bundled WinGet instead of system WinGet")} ({CoreTools.Translate("This may help if WinGet packages are not shown")})",
                SettingName = "ForceLegacyBundledWinGet"
            };
            WinGet_UseBundled.StateChanged += (_, _) =>
            {
                IPackageManagerExpanders[PEInterface.WinGet].ShowRestartRequiredBanner();
            };

            CheckboxCard WinGet_EnableTroubleshooter = new()
            {
                Text = CoreTools.Translate("Enable the automatic WinGet troubleshooter"),
                SettingName = "DisableWinGetMalfunctionDetector"
            };
            WinGet_EnableTroubleshooter.StateChanged += (_, _) =>
            {
                MainApp.Instance.MainWindow.WinGetWarningBanner.IsOpen = false;
                _ = PEInterface.InstalledPackagesLoader.ReloadPackages();
            };

            ExtraSettingsCards[PEInterface.WinGet].Add(WinGet_EnableTroubleshooter);
            ExtraSettingsCards[PEInterface.WinGet].Add(WinGet_ResetWindowsIPackageManager);
            ExtraSettingsCards[PEInterface.WinGet].Add(WinGet_UseBundled);

            // ----------------------------------------------------------------------------------------

            ButtonCard Scoop_Install = new() { Text = CoreTools.AutoTranslated("Install Scoop"), ButtonText = CoreTools.AutoTranslated("Install") };
            Scoop_Install.Click += (_, _) =>
            {
                CoreTools.LaunchBatchFile(Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "install_scoop.cmd"), CoreTools.Translate("Scoop Installer - WingetUI"));
                IPackageManagerExpanders[PEInterface.Scoop].ShowRestartRequiredBanner();
            };
            ButtonCard Scoop_Uninstall = new() { Text = CoreTools.AutoTranslated("Uninstall Scoop (and its packages)"), ButtonText = CoreTools.AutoTranslated("Uninstall") };
            Scoop_Uninstall.Click += (_, _) =>
            {
                CoreTools.LaunchBatchFile(Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "uninstall_scoop.cmd"), CoreTools.Translate("Scoop Uninstaller - WingetUI"));
                IPackageManagerExpanders[PEInterface.Scoop].ShowRestartRequiredBanner();
            };
            ButtonCard Scoop_ResetAppCache = new() { Text = CoreTools.AutoTranslated("Run cleanup and clear cache"), ButtonText = CoreTools.AutoTranslated("Run") };
            Scoop_ResetAppCache.Click += (_, _) =>
            {
                CoreTools.LaunchBatchFile(Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "scoop_cleanup.cmd"), CoreTools.Translate("Clearing Scoop cache - WingetUI"), RunAsAdmin: true);
            };

            ExtraSettingsCards[PEInterface.Scoop].Add(Scoop_Install);
            ExtraSettingsCards[PEInterface.Scoop].Add(Scoop_Uninstall);
            ExtraSettingsCards[PEInterface.Scoop].Add(Scoop_ResetAppCache);

            // ----------------------------------------------------------------------------------------

            CheckboxCard Chocolatey_SystemChoco = new() { Text = CoreTools.AutoTranslated("Use system Chocolatey"), SettingName = "UseSystemChocolatey" };
            Chocolatey_SystemChoco.StateChanged += (_, _) =>
            {
                IPackageManagerExpanders[PEInterface.Chocolatey].ShowRestartRequiredBanner();
            };

            ExtraSettingsCards[PEInterface.Chocolatey].Add(Chocolatey_SystemChoco);

            // ----------------------------------------------------------------------------------------

            CheckboxCard Vcpkg_UpdateGitPorts = new()
            {
                Text = CoreTools.Translate("Update vcpkg's Git portfiles automatically (requires Git installed)"),
                SettingName = "DisableUpdateVcpkgGitPorts"
            };
            ExtraSettingsCards[PEInterface.Vcpkg].Add(Vcpkg_UpdateGitPorts);

            // GetDefaultTriplet factors in the `DefaultVcpkgTriplet` setting as its first priority
            Settings.SetValue("DefaultVcpkgTriplet", Vcpkg.GetDefaultTriplet());
            ComboboxCard Vcpkg_DefaultTriplet = new()
            {
                Text = CoreTools.Translate("Default vcpkg triplet"),
                SettingName = "DefaultVcpkgTriplet"
            };
            foreach (string triplet in Vcpkg.GetSystemTriplets())
            {
                Vcpkg_DefaultTriplet.AddItem(triplet, triplet);
            }
            Vcpkg_DefaultTriplet.ShowAddedItems();
            ExtraSettingsCards[PEInterface.Vcpkg].Add(Vcpkg_DefaultTriplet);

            ButtonCard Vcpkg_CustomVcpkgRoot = new()
            {
                Text="Change vcpkg root location",
                ButtonText="Select",
            };
            StackPanel p = new() { Orientation = Orientation.Horizontal, Spacing = 5, };
            var VcPkgRootLabel = new TextBlock() { VerticalAlignment = VerticalAlignment.Center };
            var ResetVcPkgRootLabel = new HyperlinkButton() { Content = CoreTools.Translate("Reset") };
            var OpenVcPkgRootLabel = new HyperlinkButton() { Content = CoreTools.Translate("Open") };

            VcPkgRootLabel.Text = Settings.Get("CustomVcpkgRoot")? Settings.GetValue("CustomVcpkgRoot"): "%VCPKG_ROOT%";
            OpenVcPkgRootLabel.IsEnabled = Settings.Get("CustomVcpkgRoot");
            ResetVcPkgRootLabel.IsEnabled = Settings.Get("CustomVcpkgRoot");

            ResetVcPkgRootLabel.Click += (_, _) =>
            {
                VcPkgRootLabel.Text = "%VCPKG_ROOT%";
                Settings.Set("CustomVcpkgRoot", false);
                ResetVcPkgRootLabel.IsEnabled = false;
                OpenVcPkgRootLabel.IsEnabled = false;
            };

            OpenVcPkgRootLabel.Click += (_, _) =>
            {
                string directory = Settings.GetValue("CustomVcpkgRoot").Replace("/", "\\");
                if(directory.Any()) Process.Start("explorer.exe", directory);
            };

            Vcpkg_CustomVcpkgRoot.Click += (_, _) =>
            {
                ExternalLibraries.Pickers.FolderPicker openPicker = new(MainApp.Instance.MainWindow.GetWindowHandle());
                string folder = openPicker.Show();
                if (folder != string.Empty)
                {
                    Settings.SetValue("CustomVcpkgRoot", folder);
                    VcPkgRootLabel.Text = folder;
                    ResetVcPkgRootLabel.IsEnabled = true;
                    OpenVcPkgRootLabel.IsEnabled = true;
                }
            };

            p.Children.Add(VcPkgRootLabel);
            p.Children.Add(ResetVcPkgRootLabel);
            p.Children.Add(OpenVcPkgRootLabel);
            Vcpkg_CustomVcpkgRoot.Description = p;

            Vcpkg_CustomVcpkgRoot.Click += (_, _) =>
            {
                IPackageManagerExpanders[PEInterface.Vcpkg].ShowRestartRequiredBanner();
            };

            ExtraSettingsCards[PEInterface.Vcpkg].Add(Vcpkg_CustomVcpkgRoot);

            // ----------------------------------------------------------------------------------------

            foreach (IPackageManager Manager in PEInterface.Managers)
            {
                // Creation of the actual expander
                SettingsEntry ManagerExpander = new()
                {
                    Text = Manager.DisplayName,
                    Description = Manager.Properties.Description.Replace("<br>", "\n").Replace("<b>", "").Replace("</b>", "")
                };
                IPackageManagerExpanders.Add(Manager, ManagerExpander);
                ManagerExpander.HeaderIcon = new LocalIcon(Manager.Properties.IconId);

                // Creation of the status footer

                InfoBar ManagerStatus = new();

                TextBlock LongVersion = new();
                HyperlinkButton ShowVersionButton = new()
                {
                    Content = CoreTools.Translate("Expand version"),
                    Visibility = Visibility.Collapsed
                };
                ManagerStatus.ActionButton = ShowVersionButton;
                ShowVersionButton.Click += (_, _) => { SetManagerStatus(Manager, true); };

                LongVersion.TextWrapping = TextWrapping.Wrap;
                LongVersion.Text = Manager.Status.Version + "\n";
                LongVersion.FontFamily = new FontFamily("Consolas");
                LongVersion.Visibility = Visibility.Collapsed;
                ManagerStatus.Content = LongVersion;

                ManagerStatus.IsClosable = false;
                ManagerStatus.IsOpen = true;
                ManagerStatus.CornerRadius = new CornerRadius(0);
                ManagerStatus.BorderThickness = new (0, 1, 0, 0);

                Button managerLogs = new Button()
                {
                    Content = new LocalIcon(IconType.Console),
                    CornerRadius = new(0),
                    Padding = new(14, 4, 14, 4),
                    BorderThickness = new (0),
                    Margin = new (0, 1, 0, 0),
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                managerLogs.Click += (_, _) =>
                {
                    MainApp.Instance.MainWindow.NavigationPage.OpenManagerLogs(Manager as IPackageManager);
                };

                Grid g = new()
                {
                    ColumnSpacing = 1, Margin = new(0, 0, 0, 0),
                    ColumnDefinitions =
                    {
                        new() {Width = new GridLength(1, GridUnitType.Star)},
                        new() {Width = GridLength.Auto}
                    }
                };
                g.Children.Add(ManagerStatus);
                g.Children.Add(managerLogs);
                Grid.SetColumn(ManagerStatus, 0);
                Grid.SetColumn(managerLogs, 1);
                ManagerExpander.ItemsFooter = g;

                void SetManagerStatus(IPackageManager manager, bool ShowVersion = false)
                {
                    ShowVersionButton.Visibility = Visibility.Collapsed;
                    LongVersion.Visibility = Visibility.Collapsed;
                    if (manager.IsEnabled() && manager.Status.Found)
                    {
                        ManagerStatus.Severity = InfoBarSeverity.Success;
                        ManagerStatus.Title = CoreTools.Translate("{pm} is enabled and ready to go", new Dictionary<string, object?> { { "pm", manager.DisplayName } });
                        if (!manager.Status.Version.Contains('\n'))
                        {
                            ManagerStatus.Message = CoreTools.Translate("{pm} version:", new Dictionary<string, object?> { { "pm", manager.DisplayName } }) + " " + manager.Status.Version;
                        }
                        else if (ShowVersion)
                        {
                            ManagerStatus.Message = CoreTools.Translate("{pm} version:", new Dictionary<string, object?> { { "pm", manager.DisplayName } });
                            LongVersion.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            ManagerStatus.Message = "";
                            ShowVersionButton.Visibility = Visibility.Visible;
                        }
                    }
                    else if (manager.IsEnabled() && !manager.Status.Found)
                    {
                        ManagerStatus.Severity = InfoBarSeverity.Error;
                        ManagerStatus.Title = CoreTools.Translate("{pm} was not found!", new Dictionary<string, object?> { { "pm", manager.DisplayName } });
                        ManagerStatus.Message = CoreTools.Translate("You may need to install {pm} in order to use it with WingetUI.", new Dictionary<string, object?> { { "pm", manager.DisplayName } });
                    }
                    else if (!manager.IsEnabled())
                    {
                        ManagerStatus.Severity = InfoBarSeverity.Informational;
                        ManagerStatus.Title = CoreTools.Translate("{pm} is disabled", new Dictionary<string, object?> { { "pm", manager.DisplayName } });
                        ManagerStatus.Message = CoreTools.Translate("Enable it to install packages from {pm}.", new Dictionary<string, object?> { { "pm", manager.DisplayName } });
                    }
                }

                // Switch to enable/disable said manager

                ToggleSwitch ManagerSwitch = new()
                {
                    IsOn = Manager.IsEnabled()
                };
                ManagerSwitch.Toggled += (_, _) =>
                {
                    Settings.SetDictionaryItem("DisabledManagers", Manager.Name, !ManagerSwitch.IsOn);
                    SetManagerStatus(Manager);
                    EnableOrDisableEntries();
                };

                ManagerExpander.Content = ManagerSwitch;

                void EnableOrDisableEntries()
                {
                    if (ExtraSettingsCards.TryGetValue(Manager, out var settingsCard))
                    {
                        foreach (SettingsCard card in settingsCard)
                        {
                            if (ManagerSwitch.IsOn)
                            {
                                card.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                card.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                }

                int index = 0;
                SettingsCard ManagerPath = new()
                {
                    Description = Manager.Status.ExecutablePath + " " + Manager.Properties.ExecutableCallArgs,
                    IsClickEnabled = true,
                    ActionIcon = new SymbolIcon(Symbol.Copy)
                };

                ManagerPath.Click += async (_, _) =>
                {
                    WindowsClipboard.SetText(ManagerPath.Description.ToString() ?? "");
                    ManagerPath.ActionIcon = new FontIcon() {Glyph = "\uE73E"};
                    await Task.Delay(1000);
                    ManagerPath.ActionIcon = new SymbolIcon(Symbol.Copy);
                };
                ExtraSettingsCards[Manager].Insert(index++, ManagerPath);

                CheckboxCard AdminCard = new()
                {
                    Text = CoreTools.AutoTranslated("Always run {pm} operations with administrator rights"),
                    SettingName = "AlwaysElevate" + Manager.Name,
                };
                AdminCard._checkbox.Content = (AdminCard._checkbox.Content.ToString() ?? "").Replace("{pm}", Manager.DisplayName);
                ExtraSettingsCards[Manager].Insert(index++, AdminCard);

                CheckboxCard ParallelCard = new()
                {
                    Text = CoreTools.AutoTranslated("Allow {pm} operations to be performed in parallel"),
                    SettingName = "AllowParallelInstallsForManager" + Manager.Name,
                };
                ParallelCard._checkbox.Content = (ParallelCard._checkbox.Content.ToString() ?? "").Replace("{pm}", Manager.DisplayName);
                ExtraSettingsCards[Manager].Insert(index++, ParallelCard);

                if (Manager.Capabilities.SupportsCustomSources && Manager is not Vcpkg)
                {
                    SettingsCard SourceManagerCard = new();
                    SourceManagerCard.Resources["SettingsCardLeftIndention"] = 10;
                    SourceManager SourceManager = new(Manager);
                    SourceManagerCard.Description = SourceManager;
                    ExtraSettingsCards[Manager].Insert(index++, SourceManagerCard);
                }

                if (ExtraSettingsCards.TryGetValue(Manager, out var extraSettingsCard))
                {
                    foreach (SettingsCard card in extraSettingsCard)
                    {
                        ManagerExpander.Items.Add(card);
                    }
                }

                SetManagerStatus(Manager);
                EnableOrDisableEntries();
                MainLayout.Children.Add(ManagerExpander);
            }

            InterfaceLoaded = true;
        }

        private async void LoadIconCacheSize()
        {
            double realSize = (await Task.Run(() =>
            {
                return Directory.GetFiles(CoreData.UniGetUICacheDirectory_Icons, "*", SearchOption.AllDirectories)
                    .Sum(file => new FileInfo(file).Length);
            })) / 1048576d;
            double roundedSize = ((int)(realSize*100))/100d;
            ResetIconCache.Header = CoreTools.Translate("The local icon cache currently takes {0} MB", roundedSize);
        }

        private void ImportSettings(object sender, EventArgs e)
        {
            ExternalLibraries.Pickers.FileOpenPicker picker = new(MainApp.Instance.MainWindow.GetWindowHandle());
            string file = picker.Show(["*.json"]);

            if (file != string.Empty)
            {
                if (Path.GetDirectoryName(file) == CoreData.UniGetUIDataDirectory)
                {
                    Directory.CreateDirectory(Path.Join(CoreData.UniGetUIDataDirectory, "import-temp"));
                    File.Copy(file, Path.Join(CoreData.UniGetUIDataDirectory, "import-temp", Path.GetFileName(file)));
                    file = Path.Join(CoreData.UniGetUIDataDirectory, "import-temp", Path.GetFileName(file));
                }
                ResetWingetUI(sender, e);
                Dictionary<string, string> settings = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file)) ?? [];
                foreach (KeyValuePair<string, string> entry in settings)
                {
                    File.WriteAllText(Path.Join(CoreData.UniGetUIDataDirectory, entry.Key), entry.Value);
                }

                if (Directory.Exists(Path.Join(CoreData.UniGetUIDataDirectory, "import-temp")))
                {
                    Directory.Delete(Path.Join(CoreData.UniGetUIDataDirectory, "import-temp"), true);
                }

                GeneralSettingsExpander.ShowRestartRequiredBanner();
            }
        }

        private async void ExportSettings(object sender, EventArgs e)
        {
            try
            {
                ExternalLibraries.Pickers.FileSavePicker picker = new(MainApp.Instance.MainWindow.GetWindowHandle());
                string file = picker.Show(["*.json"], CoreTools.Translate("WingetUI Settings") + ".json");

                if (file != string.Empty)
                {
                    DialogHelper.ShowLoadingDialog(CoreTools.Translate("Please wait..."));

                    string[] IgnoredSettings = ["OperationHistory", "CurrentSessionToken", "OldWindowGeometry"];

                    Dictionary<string, string> settings = [];
                    foreach (string path in Directory.EnumerateFiles(CoreData.UniGetUIDataDirectory))
                    {
                        if (IgnoredSettings.Contains(Path.GetFileName(path)))
                        {
                            continue;
                        }

                        settings.Add(Path.GetFileName(path), await File.ReadAllTextAsync(path));
                    }

                    await File.WriteAllTextAsync(file, JsonConvert.SerializeObject(settings));

                    DialogHelper.HideLoadingDialog();
                }
            }
            catch (Exception ex)
            {
                DialogHelper.HideLoadingDialog();
                Logger.Error("An error occurred when exporting settings");
                Logger.Error(ex);
            }
        }

        private void ResetWingetUI(object sender, EventArgs e)
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
                Logger.Error("An error occurred when resetting UniGetUI");
                Logger.Error(ex);
            }
            GeneralSettingsExpander.ShowRestartRequiredBanner();
        }

        private void LanguageSelector_ValueChanged(object sender, EventArgs e)
        {
            if(InterfaceLoaded) GeneralSettingsExpander.ShowRestartRequiredBanner();
        }

        private void UpdatesCheckIntervalSelector_ValueChanged(object sender, EventArgs e)
        {
            if(InterfaceLoaded) GeneralSettingsExpander.ShowRestartRequiredBanner();
        }

        private void ThemeSelector_ValueChanged(object sender, EventArgs e)
        {
            ((MainApp)Application.Current).MainWindow.ApplyTheme();
        }

        private void ResetBackupPath_Click(object sender, RoutedEventArgs e)
        {
            BackupDirectoryLabel.Text = CoreData.UniGetUI_DefaultBackupDirectory;
            Settings.Set("ChangeBackupOutputDirectory", false);
            ResetBackupDirectory.IsEnabled = false;
        }

        private void ChangeBackupDirectory_Click(object sender, EventArgs e)
        {

            ExternalLibraries.Pickers.FolderPicker openPicker = new(MainApp.Instance.MainWindow.GetWindowHandle());
            string folder = openPicker.Show();
            if (folder != string.Empty)
            {
                Settings.SetValue("ChangeBackupOutputDirectory", folder);
                BackupDirectoryLabel.Text = folder;
                ResetBackupDirectory.IsEnabled = true;
            }
        }

        private void OpenBackupPath_Click(object sender, RoutedEventArgs e)
        {
            string directory = Settings.GetValue("ChangeBackupOutputDirectory");
            if (directory == "")
            {
                directory = CoreData.UniGetUI_DefaultBackupDirectory;
            }

            directory = directory.Replace("/", "\\");

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Process.Start("explorer.exe", directory);
        }

        private void DoCacheAdminRights_StateChanged(object sender, EventArgs e)
        {
            _ = CoreTools.ResetUACForCurrentProcess();
        }

        private void DisableWidgetsApi_StateChanged(object sender, EventArgs e)
        { if(InterfaceLoaded) ExperimentalSettingsExpander.ShowRestartRequiredBanner(); }

        private void DisableDownloadingNewTranslations_StateChanged(object sender, EventArgs e)
        { if(InterfaceLoaded) ExperimentalSettingsExpander.ShowRestartRequiredBanner(); }

        private void TextboxCard_ValueChanged(object sender, EventArgs e)
        { if(InterfaceLoaded) ExperimentalSettingsExpander.ShowRestartRequiredBanner(); }

        private async void DoBackup_Click(object sender, EventArgs e)
        {
            DialogHelper.ShowLoadingDialog(CoreTools.Translate("Performing backup, please wait..."));
            await MainApp.Instance.MainWindow.NavigationPage.InstalledPage.BackupPackages();
            DialogHelper.HideLoadingDialog();
        }

        private void EditAutostartSettings_Click(object sender, EventArgs e)
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ms-settings:startupapps",
                    UseShellExecute = true,
                    CreateNoWindow = true
                }
            };
            p.Start();
        }

        private void DisableSystemTray_StateChanged(object sender, EventArgs e)
        {
            MainApp.Instance.MainWindow.UpdateSystemTrayStatus();
            if (NotificationSettingsEntry is not null)
            {
                NotificationSettingsEntry.IsEnabled = DisableSystemTray.Checked;
            }
        }

        private void EnablePackageBackupCheckBox_StateChanged(object sender, EventArgs e)
        {
            if(InterfaceLoaded) EnablePackageBackupUI(EnablePackageBackupCheckBox.Checked);
        }

        public void EnablePackageBackupUI(bool enabled)
        {
            if (BackupNowButton is null)
            {
                return; // This could happen when this event is triggered but the SettingsPage
            }
            // hasn't finished initializing yet.
            EnableBackupTimestampingCheckBox.IsEnabled = enabled;
            ChangeBackupFileNameTextBox.IsEnabled = enabled;
            ChangeBackupDirectory.IsEnabled = enabled;
            BackupNowButton.IsEnabled = enabled;

            if (enabled)
            {
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
            }
        }

        private void UseUserGSudoToggle_StateChanged(object sender, EventArgs e)
        {
            if(InterfaceLoaded)
                ExperimentalSettingsExpander.ShowRestartRequiredBanner();
        }

        private void ResetIconCache_OnClick(object? sender, EventArgs e)
        {
            try
            {
                Directory.Delete(CoreData.UniGetUICacheDirectory_Icons, true);
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred while deleting icon cache");
                Logger.Error(ex);
            }
            InterfaceSettingsExpander.ShowRestartRequiredBanner();
            PackageWrapper.ResetIconCache();
            Package.ResetIconCache();
            LoadIconCacheSize();
        }

        private void DisableIconsOnPackageLists_OnStateChanged(object? sender, EventArgs e)
        {
            if(InterfaceLoaded)
                InterfaceSettingsExpander.ShowRestartRequiredBanner();
        }

        public void OnEnter()
        {
            LoadIconCacheSize();
        }

        public void OnLeave()
        {
            foreach (var item in MainLayout.Children)
            {
                if (item is SettingsEntry expander)
                    expander.IsExpanded = false;
            }
        }

        private void DisableSelectingUpdatesByDefault_OnClick(object sender, EventArgs e)
        {
            if (InterfaceLoaded) InterfaceSettingsExpander.ShowRestartRequiredBanner();
        }

        private void ForceUpdateUniGetUI_OnClick(object? sender, RoutedEventArgs e)
        {
            _ = AutoUpdater.CheckAndInstallUpdates(MainApp.Instance.MainWindow, MainApp.Instance.MainWindow.UpdatesBanner,
                true);
        }

        private void CheckboxButtonCard_OnClick(object? sender, RoutedEventArgs e)
        {
            _ = DialogHelper.ManageDesktopShortcuts();
        }
    }
}

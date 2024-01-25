using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Python.Runtime;
using Microsoft.VisualBasic.FileIO;
using System.Diagnostics;
using Microsoft.UI;
using System.Windows.Input;
using Windows.UI;
using ModernWindow.Structures;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage;
using ModernWindow.Interface.Widgets;
using CommunityToolkit.WinUI.Controls;
using System.Reflection;
using Windows.ApplicationModel.DataTransfer;
using ModernWindow.PackageEngine;
using ModernWindow.PackageEngine.Managers;
using System.Xml.Schema;
using ModernWindow.Clipboard;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Media.Imaging;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsInterface : Page
    {
        private MainAppBindings bindings = MainAppBindings.Instance;
        HyperlinkButton ResetBackupDirectory;
        HyperlinkButton OpenBackupDirectory;
        TextBlock BackupDirectoryLabel;


        public SettingsInterface()
        {
            this.InitializeComponent();
       
            // General Settings Section
            PyDict lang_dict = new PyDict(bindings.Core.Languages.LangData.languageReference);
            var lang_values = lang_dict.Keys();
            var lang_names = lang_dict.Values();
            bool isFirst = true;
            for (int i = 0; i < lang_values.Count(); i++)
            {
                LanguageSelector.AddItem(lang_names[i].ToString(), lang_values[i].ToString(), isFirst);
                isFirst = false;
            }
            LanguageSelector.ShowAddedItems();


            PyDict updates_dict = new PyDict(bindings.Core.Tools.update_times_reference);
            var time_names = updates_dict.Keys();
            var time_keys = updates_dict.Values();
            for (int i = 0; i < time_names.Count(); i++)
            {
                UpdatesCheckIntervalSelector.AddItem(time_names[i].ToString(), time_keys[i].ToString(), false);
            }
            UpdatesCheckIntervalSelector.ShowAddedItems();

            ThemeSelector.AddItem("Light", "light");
            ThemeSelector.AddItem("Dark", "dark");
            ThemeSelector.AddItem("Follow system color scheme", "auto");
            ThemeSelector.ShowAddedItems();

            // Backup Section
            BackupDirectoryLabel = (TextBlock)(((StackPanel)ChangeBackupDirectory.Description).Children.ElementAt(0));
            ResetBackupDirectory = (HyperlinkButton)(((StackPanel)ChangeBackupDirectory.Description).Children.ElementAt(1));
            OpenBackupDirectory = (HyperlinkButton)(((StackPanel)ChangeBackupDirectory.Description).Children.ElementAt(2));
            if (!bindings.GetSettings("ChangeBackupOutputDirectory"))
            {
                BackupDirectoryLabel.Text = bindings.Globals.DEFAULT_PACKAGE_BACKUP_DIR;
                ResetBackupDirectory.IsEnabled = false;
            }
            else
            {
                BackupDirectoryLabel.Text = bindings.GetSettingsValue("ChangeBackupOutputDirectory");
                ResetBackupDirectory.IsEnabled = true;
            }

            ResetBackupDirectory.Content = bindings.Translate("Reset");

            OpenBackupDirectory.Content = bindings.Translate("Open");

            // Admin Settings Section
            int index = 1;
            foreach(PackageManager manager in bindings.App.PackageManagerList)
            {
                CheckboxCard card = new CheckboxCard()
                {
                    Text = "Always elevate {pm} installations by default",
                    SettingName = "AlwaysElevate" + manager.Name,
                };
                card._checkbox.Content = card._checkbox.Content.ToString().Replace("{pm}", manager.Name);
                AdminSettingsExpander.Items.Insert(index++, card);
            }

            // Experimental Settings Section
            ExperimentalSettingsExpander.HideRestartRequiredBanner();


            // Package Manager banners;
            Dictionary<PackageManager, SettingsEntry> PackageManagerExpanders = new Dictionary<PackageManager, SettingsEntry>();
            Dictionary<PackageManager, List<SettingsCard>> ExtraSettingsCards = new Dictionary<PackageManager, List<SettingsCard>>();

            foreach (var Manager in bindings.App.PackageManagerList)
            {
                ExtraSettingsCards.Add(Manager, new List<SettingsCard>());
            }


            var Winget_ResetSources = new ButtonCard() { Text="Reset Winget sources (might help if no packages are listed", ButtonText="Reset" };
            Winget_ResetSources.Click += (s, e) =>
            {
                // Spawn reset winget sources window
                ((SettingsEntry)PackageManagerExpanders[bindings.App.Winget]).ShowRestartRequiredBanner();
            };

            ExtraSettingsCards[bindings.App.Winget].Add(Winget_ResetSources);

            var Scoop_Install = new ButtonCard() { Text = "Install Scoop", ButtonText = "Install" };
            Scoop_Install.Click += (s, e) =>
            {
                // Spawn install scoop window
                ((SettingsEntry)PackageManagerExpanders[bindings.App.Scoop]).ShowRestartRequiredBanner();
                bindings.SetSettings("DisableScoop", false);
            };
            var Scoop_Uninstall = new ButtonCard() { Text = "Uninstall Scoop (and its packages)", ButtonText = "Uninstall" };
            Scoop_Uninstall.Click += (s, e) =>
            {
                // Spawn uninstall scoop window
                ((SettingsEntry)PackageManagerExpanders[bindings.App.Scoop]).ShowRestartRequiredBanner();
                bindings.SetSettings("DisableScoop", true);
            };
            var Scoop_ResetAppCache = new ButtonCard() { Text = "Reset Scoop's global app cache", ButtonText = "Reset" };
            Scoop_Uninstall.Click += (s, e) =>
            {
                // Spawn Scoop Cache clearer
            };

            ExtraSettingsCards[bindings.App.Scoop].Add(Scoop_Install);
            ExtraSettingsCards[bindings.App.Scoop].Add(Scoop_Uninstall);
            ExtraSettingsCards[bindings.App.Scoop].Add(Scoop_ResetAppCache);

            var Chocolatey_SystemChoco = new CheckboxCard() { Text= "Use system Chocolatey", SettingName="UseSystemChocolatey" };
            Chocolatey_SystemChoco.StateChanged += (s, e) =>
            {
                ((SettingsEntry)PackageManagerExpanders[bindings.App.Choco]).ShowRestartRequiredBanner();
            };

            ExtraSettingsCards[bindings.App.Choco].Add(Chocolatey_SystemChoco);



            foreach (PackageManager Manager in bindings.App.PackageManagerList)
            {

                var ManagerExpander = new SettingsEntry
                {
                    Text = Manager.Name,
                    Description = Manager.Properties.Description.Replace("<br>", "\n").Replace("<b>", "").Replace("</b>", "")
                };
                PackageManagerExpanders.Add(Manager, ManagerExpander);

                InfoBar ManagerStatus = new InfoBar();

                var LongVersion = new TextBlock();
                HyperlinkButton ShowVersionButton = new HyperlinkButton();
                ShowVersionButton.Content = bindings.Translate("Expand version");
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
                    if(Manager.IsEnabled() && Manager.Status.Found)
                    {
                        ManagerStatus.Severity = InfoBarSeverity.Success;
                        ManagerStatus.Title = bindings.Translate("{pm} is enabled and ready to go").Replace("{pm}", Manager.Name);
                        if (!Manager.Status.Version.Contains("\n"))
                            ManagerStatus.Message = bindings.Translate("{pm} version:").Replace("{pm}", Manager.Name) + " " + Manager.Status.Version;
                        else if (ShowVersion)
                        {
                            ManagerStatus.Message = bindings.Translate("{pm} version:").Replace("{pm}", Manager.Name);
                            LongVersion.Visibility = Visibility.Visible;
                        } else {
                            ManagerStatus.Message = "";
                            ShowVersionButton.Visibility = Visibility.Visible;
                        }

                    }
                    else if (Manager.IsEnabled() && !Manager.Status.Found)
                    {
                        ManagerStatus.Severity = InfoBarSeverity.Error;
                        ManagerStatus.Title = bindings.Translate("{pm} was not found!").Replace("{pm}", Manager.Name);
                        ManagerStatus.Message = bindings.Translate("You may need to install {pm} in order to use it with WingetUI.").Replace("{pm}", Manager.Name);
                    }
                    else if (!Manager.IsEnabled())
                    {
                        ManagerStatus.Severity = InfoBarSeverity.Informational;
                        ManagerStatus.Title = bindings.Translate("{pm} is disabled").Replace("{pm}", Manager.Name);
                        ManagerStatus.Message = bindings.Translate("Enable it to install packages from {pm}.").Replace("{pm}", Manager.Name);
                    }
                }

                ManagerStatus.IsClosable = false;
                ManagerStatus.IsOpen = true;
                ManagerStatus.CornerRadius = new CornerRadius(0);
                ManagerStatus.BorderThickness = new Thickness(0, 1, 0, 0);
                ManagerExpander.ItemsFooter = ManagerStatus;

                var icon = new ImageIcon
                {
                    Source = new BitmapImage() { UriSource = new Uri("ms-appx:///wingetui/resources/" + Manager.Properties.IconId + "_white.png") },
                };
                ManagerExpander.HeaderIcon = icon;

                var ManagerSwitch = new ToggleSwitch
                {
                    IsOn = Manager.IsEnabled()
                };
                ManagerSwitch.Toggled += (s, e) => {
                    bindings.SetSettings("Disable" + Manager.Name, !ManagerSwitch.IsOn);
                    SetManagerStatus(Manager); 
                    EnableOrDisableEntries();
                };

                ManagerExpander.Content = ManagerSwitch;

                void EnableOrDisableEntries()
                {
                    if (ExtraSettingsCards.ContainsKey(Manager))
                        foreach (var card in ExtraSettingsCards[Manager])
                        {
                            if (ManagerSwitch.IsOn)
                                card.Visibility = Visibility.Visible;
                            else
                                card.Visibility = Visibility.Collapsed;
                        }
                }
                
                var ManagerPath = new SettingsCard() { Description = Manager.Status.ExecutablePath + " " + Manager.Properties.ExecutableCallArgs, IsClickEnabled = true };
                ManagerPath.ActionIcon = new SymbolIcon(Symbol.Copy);
                ManagerPath.Click += (s, e) =>
                {
                    WindowsClipboard.SetText(ManagerPath.Description.ToString());
                };
                ExtraSettingsCards[Manager].Insert(0, ManagerPath);

                
                if(Manager is PackageManagerWithSources)
                {
                    var SourceManagerCard = new SettingsCard();
                    SourceManagerCard.Resources["SettingsCardLeftIndention"] = 10;
                    var SourceManager = new SourceManager(Manager as PackageManagerWithSources);
                    SourceManagerCard.Description = SourceManager;
                    ExtraSettingsCards[Manager].Insert(1, SourceManagerCard);
                }

                if (ExtraSettingsCards.ContainsKey(Manager))
                    foreach (var card in ExtraSettingsCards[Manager])
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
            return bindings.App.mainWindow;
        }
        public int GetHwnd()
        {
            return (int)WinRT.Interop.WindowNative.GetWindowHandle(GetWindow());
        }

        private void OpenWelcomeWizard(object sender, Interface.Widgets.ButtonCardEventArgs e)
        {
            // TODO: Implement
        }

        private async void ImportSettings(object sender, Interface.Widgets.ButtonCardEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();

            openPicker.ViewMode = PickerViewMode.List;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, (IntPtr)GetHwnd());

            openPicker.FileTypeFilter.Add(".conf");

            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                bindings.App.Tools.ImportSettingsFromFile(file.Path);
                GeneralSettingsExpander.ShowRestartRequiredBanner();
            }
        }

        private async void ExportSettings(object sender, Interface.Widgets.ButtonCardEventArgs e)
        {

            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, (IntPtr)GetHwnd());
            savePicker.FileTypeChoices.Add(bindings.Translate("WingetUI Settings File"), new List<string>() { ".conf" });
            savePicker.SuggestedFileName = bindings.Translate("Exported Settings");

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                bindings.App.Tools.ExportSettingsToFile(file.Path);
            }

        }

        private void ResetWingetUI(object sender, Interface.Widgets.ButtonCardEventArgs e)
        {
            bindings.App.Tools.ResetSettings();
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
            ((MainApp)Application.Current).mainWindow.ApplyTheme();
        }

        private void ResetBackupPath_Click(object sender, dynamic e)
        {
            BackupDirectoryLabel.Text = bindings.Globals.DEFAULT_PACKAGE_BACKUP_DIR;
            bindings.SetSettings("ChangeBackupOutputDirectory", false);
            ResetBackupDirectory.IsEnabled = false;
        }

        private async void ChangeBackupDirectory_Click(object sender, dynamic e)
        {
            FolderPicker openPicker = new Windows.Storage.Pickers.FolderPicker();

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(bindings.App.mainWindow);

            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await openPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                bindings.SetSettingsValue("ChangeBackupOutputDirectory", folder.Path);
                BackupDirectoryLabel.Text = folder.Path;
                ResetBackupDirectory.IsEnabled = true;
            }
            else
            {
                ResetBackupPath_Click(sender, e);
            }

        }

        private void OpenBackupPath_Click(object sender, RoutedEventArgs e)
        {
            string directory = bindings.GetSettingsValue("ChangeBackupOutputDirectory");
            if (directory == "")
                directory = bindings.Globals.DEFAULT_PACKAGE_BACKUP_DIR;
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
            bindings.Core.Tools.ResetCache();
            ExperimentalSettingsExpander.ShowRestartRequiredBanner();
        }
    }
}

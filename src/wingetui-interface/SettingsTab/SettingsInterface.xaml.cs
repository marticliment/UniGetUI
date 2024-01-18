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
using ModernWindow.SettingsTab.Widgets;
using CommunityToolkit.WinUI.Controls;
using System.Reflection;
using Windows.ApplicationModel.DataTransfer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.SettingsTab
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : UserControl
    {
        private MainAppBindings bindings = new MainAppBindings();
        HyperlinkButton ResetBackupDirectory;
        HyperlinkButton OpenBackupDirectory;
        TextBlock BackupDirectoryLabel;


        public MainPage()
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
            foreach(dynamic manager in new PyList(bindings.App.PackageTools.PackageManagersList))
            {
                CheckboxCard card = new CheckboxCard()
                {
                    Text = "Always elevate {pm} installations by default",
                    SettingName = (string)("AlwaysElevate" + manager.NAME),
                };
                card._checkbox.Content = card._checkbox.Content.ToString().Replace("{pm}", manager.NAME.ToString());
                AdminSettingsExpander.Items.Insert(index++, card);
            }

            // Experimental Settings Section
            ExperimentalSettingsExpander.HideRestartRequiredBanner();


            // Package Manager banners;
            Dictionary<PyObject, SettingsEntry> PackageManagerExpanders = new Dictionary<PyObject, SettingsEntry>();
            Dictionary<PyObject, SettingsCard[]> ExtraSettingsCards = new Dictionary<PyObject, SettingsCard[]>();

            var Winget_ResetSources = new ButtonCard() { Text="Reset Winget sources (might help if no packages are listed", ButtonText="Reset" };
            Winget_ResetSources.Click += (s, e) =>
            {
                // Spawn reset winget sources window
                ((SettingsEntry)PackageManagerExpanders[bindings.App.PackageTools.Winget]).ShowRestartRequiredBanner();
            };

            SettingsCard[] Winget_Cards = { Winget_ResetSources };
            ExtraSettingsCards.Add(bindings.App.PackageTools.Winget, Winget_Cards);

            var Scoop_Install = new ButtonCard() { Text = "Install Scoop", ButtonText = "Install" };
            Scoop_Install.Click += (s, e) =>
            {
                // Spawn install scoop window
                ((SettingsEntry)PackageManagerExpanders[bindings.App.PackageTools.Scoop]).ShowRestartRequiredBanner();
                bindings.SetSettings("DisableScoop", false);
            };
            var Scoop_Uninstall = new ButtonCard() { Text = "Uninstall Scoop (and its packages)", ButtonText = "Uninstall" };
            Scoop_Uninstall.Click += (s, e) =>
            {
                // Spawn uninstall scoop window
                ((SettingsEntry)PackageManagerExpanders[bindings.App.PackageTools.Scoop]).ShowRestartRequiredBanner();
                bindings.SetSettings("DisableScoop", true);
            };
            var Scoop_ResetAppCache = new ButtonCard() { Text = "Reset Scoop's global app cache", ButtonText = "Reset" };
            Scoop_Uninstall.Click += (s, e) =>
            {
                // Spawn Scoop Cache clearer
            };

            SettingsCard[] Scoop_Cards = { Scoop_Install, Scoop_Uninstall, Scoop_ResetAppCache };
            ExtraSettingsCards.Add(bindings.App.PackageTools.Scoop, Scoop_Cards);

            var Chocolatey_SystemChoco = new CheckboxCard() { Text= "Use system Chocolatey", SettingName="UseSystemChocolatey" };
            Chocolatey_SystemChoco.StateChanged += (s, e) =>
            {
                ((SettingsEntry)PackageManagerExpanders[bindings.App.PackageTools.Choco]).ShowRestartRequiredBanner();
            };

            SettingsCard[] Choco_Cards = { Chocolatey_SystemChoco };
            ExtraSettingsCards.Add(bindings.App.PackageTools.Choco, Choco_Cards);


            foreach (var Manager in bindings.App.PackageTools.PackageManagersList)
            {
                var ManagerExpander = new SettingsEntry() { UnderText = "{pm} package manager specific preferences" };
                ManagerExpander.Text = bindings.Translate("{pm} preferences").Replace("{pm}", Manager.NAME.ToString());
                ManagerExpander.Description = ManagerExpander.UnderText.Replace("{pm}", Manager.NAME.ToString());
                PackageManagerExpanders.Add(Manager, ManagerExpander);

                var icon = new BitmapIcon();
                icon.UriSource = new Uri(Manager.IconPath.ToString().Replace(Manager.IconPath.ToString().Split("wingetui/resources")[0], "ms-appx:///"));
                ManagerExpander.HeaderIcon = icon;

                var EnableManager = new CheckboxCard() { SettingName = "Disable" + Manager.NAME.ToString() };
                EnableManager._checkbox.Content = bindings.Translate("Enable {pm}").Replace("{pm}", Manager.NAME.ToString());
                EnableManager.StateChanged += (s, e) => { ManagerExpander.ShowRestartRequiredBanner(); };
                ManagerExpander.Items.Add(EnableManager);

                var ManagerPath = new SettingsCard() { Description = Manager.EXECUTABLE, IsClickEnabled = true };
                if (Manager == bindings.App.PackageTools.Scoop)
                    ManagerPath.Description = "scoop";
                ManagerPath.ActionIcon = new SymbolIcon(Symbol.Copy);
                ManagerPath.Click += (s, e) =>
                {
                    // TODO: Implement copy algorihtm;
                };
                ManagerExpander.Items.Add(ManagerPath);
                

                if(Manager.Capabilities.SupportsCustomSources)
                {
                    var SourceManager = new ButtonCard() { Text = "Sources", ButtonText = "LESSSS GOOOOOOO" };
                    ManagerExpander.Items.Add(SourceManager);
                }

                if (ExtraSettingsCards.ContainsKey(Manager))
                    foreach (var card in ExtraSettingsCards[Manager])
                    {
                        ManagerExpander.Items.Add(card);
                    }

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

        private void OpenWelcomeWizard(object sender, Widgets.ButtonCardEventArgs e)
        {
            // TODO: Implement
        }

        private async void ImportSettings(object sender, Widgets.ButtonCardEventArgs e)
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

        private async void ExportSettings(object sender, Widgets.ButtonCardEventArgs e)
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

        private void ResetWingetUI(object sender, Widgets.ButtonCardEventArgs e)
        {
            bindings.App.Tools.ResetSettings();
            GeneralSettingsExpander.ShowRestartRequiredBanner();
        }

        private void LanguageSelector_ValueChanged(object sender, Widgets.ComboCardEventArgs e)
        {
            GeneralSettingsExpander.ShowRestartRequiredBanner();
        }

        private void UpdatesCheckIntervalSelector_ValueChanged(object sender, Widgets.ComboCardEventArgs e)
        {
            GeneralSettingsExpander.ShowRestartRequiredBanner();
        }

        private void ThemeSelector_ValueChanged(object sender, Widgets.ComboCardEventArgs e)
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

        private void DoCacheAdminRights_StateChanged(object sender, Widgets.CheckBoxEventArgs e)
        {
            if (!e.IsChecked)
            {
                AdminSettingsExpander.ShowRestartRequiredBanner();
            }
        }

        private void UseSystemGSudo_StateChanged(object sender, Widgets.CheckBoxEventArgs e)
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

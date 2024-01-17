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
                Console.WriteLine(manager.NAME.ToString()); ;
                CheckboxCard card = new CheckboxCard()
                {
                    Text = "Always elevate {pm} installations by default",
                    SettingName = (string)("AlwaysElevate" + manager.NAME),
                };
                card._checkbox.Content = card._checkbox.Content.ToString().Replace("{pm}", manager.NAME.ToString());
                AdminSettingsExpander.Items.Insert(index++, card);
            }
        }

        public int GetHwnd()
        {
            return (int)WinRT.Interop.WindowNative.GetWindowHandle(bindings.App.mainWindow);
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
                GeneralSettingsExpander.ShowRestartRequiresBanner();
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
            GeneralSettingsExpander.ShowRestartRequiresBanner();
        }

        private void LanguageSelector_ValueChanged(object sender, Widgets.ComboCardEventArgs e)
        {
            GeneralSettingsExpander.ShowRestartRequiresBanner();
        }

        private void UpdatesCheckIntervalSelector_ValueChanged(object sender, Widgets.ComboCardEventArgs e)
        {
            GeneralSettingsExpander.ShowRestartRequiresBanner();
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
                AdminSettingsExpander.ShowRestartRequiresBanner();
            }
        }

        private void UseSystemGSudo_StateChanged(object sender, Widgets.CheckBoxEventArgs e)
        {
            AdminSettingsExpander.ShowRestartRequiresBanner();
        }
    }
}

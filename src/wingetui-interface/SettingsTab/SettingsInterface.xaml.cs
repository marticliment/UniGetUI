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
        TextBlock BackupDirectoryLabel;


        public MainPage()
        {
            this.InitializeComponent();
       
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

            BackupDirectoryLabel = (TextBlock)(((StackPanel)ChangeBackupDirectory.Description).Children.First());
            if(!bindings.GetSettings("ChangeBackupOutputDirectory"))
                BackupDirectoryLabel.Text = bindings.Globals.DEFAULT_PACKAGE_BACKUP_DIR;
            else
                BackupDirectoryLabel.Text = bindings.GetSettingsValue("ChangeBackupOutputDirectory");

            ResetBackupDirectory = (HyperlinkButton)(((StackPanel)ChangeBackupDirectory.Description).Children.Last());
            ResetBackupDirectory.Content = bindings.Translate("Reset");

        }

        public int GetHwnd()
        {
            return (int)WinRT.Interop.WindowNative.GetWindowHandle(this);
        }

        private void OpenWelcomeWizard(object sender, Widgets.ButtonCardEventArgs e)
        {
            // TODO: Implement
        }

        private void ImportSettings(object sender, Widgets.ButtonCardEventArgs e)
        {
            // TODO: Implement
        }

        private void ExportSettings(object sender, Widgets.ButtonCardEventArgs e)
        {
            // TODO: Implement
        }

        private void ResetWingetUI(object sender, Widgets.ButtonCardEventArgs e)
        {
            // TODO: Implement
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
        }

        private async void ChangeBackupDirectory_Click(object sender, dynamic e)
        {
            Console.WriteLine("Picking dir...");
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
            }
            else
            {
                ResetBackupPath_Click(sender, e);
            }

        }
    }
}

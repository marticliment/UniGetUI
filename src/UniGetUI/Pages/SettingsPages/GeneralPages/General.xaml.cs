using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Tools;
using UniGetUI.Pages.DialogPages;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Language;
using UniGetUI.Core.SettingsEngine;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Pages.SettingsPages.GeneralPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class General : Page, ISettingsPage
    {
        public General()
        {
            this.InitializeComponent();

            Dictionary<string, string> lang_dict = new(LanguageData.LanguageReference.AsEnumerable());

            foreach (string key in lang_dict.Keys)
            {
                if (key != "en" &&
                    LanguageData.TranslationPercentages.TryGetValue(key, out var translationPercentage))
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
        }

        public bool CanGoBack => true;
        public string ShortTitle => CoreTools.Translate("General preferences");

        public event EventHandler? RestartRequired;
        public event EventHandler<Type>? NavigationRequested;

        public void ShowRestartBanner(object sender, EventArgs e)
            => RestartRequired?.Invoke(this, e);

        private void ForceUpdateUniGetUI_OnClick(object sender, RoutedEventArgs e)
        {
            var mainWindow = MainApp.Instance.MainWindow;
            _ = AutoUpdater.CheckAndInstallUpdates(mainWindow, mainWindow.UpdatesBanner, true, false, true);
        }

        private void ManageTelemetrySettings_Click(object sender, EventArgs e)
            => DialogHelper.ShowTelemetryDialog();

        private async void ImportSettings(object sender, EventArgs e)
        {
            ExternalLibraries.Pickers.FileOpenPicker picker = new(MainApp.Instance.MainWindow.GetWindowHandle());
            string file = picker.Show(["*.json"]);

            if (file != string.Empty)
            {
                DialogHelper.ShowLoadingDialog(CoreTools.Translate("Please wait..."));
                await Task.Run(() => Settings.ImportFromJSON(file));
                DialogHelper.HideLoadingDialog();
                ShowRestartBanner(this, new());
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
                    await Task.Run(() => Settings.ExportToJSON(file));
                    DialogHelper.HideLoadingDialog();
                    CoreTools.ShowFileOnExplorer(file);
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
                Settings.ResetSettings();
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred when resetting UniGetUI");
                Logger.Error(ex);
            }
            ShowRestartBanner(this, new());
        }

        private void InterfaceSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationRequested?.Invoke(this, typeof(Interface_P));
        }
    }
}

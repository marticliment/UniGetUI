using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Tools;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Pages.SettingsPages.GeneralPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Updates : Page, ISettingsPage
    {
        public Updates()
        {
            this.InitializeComponent();

            Dictionary<string, string> updates_dict = new()
                {
                    { CoreTools.Translate("{0} minutes", 10), "600" },
                    { CoreTools.Translate("{0} minutes", 30), "1800" },
                    { CoreTools.Translate("1 hour"), "3600" },
                    { CoreTools.Translate("{0} hours", 2), "7200" },
                    { CoreTools.Translate("{0} hours", 4), "14400" },
                    { CoreTools.Translate("{0} hours", 8), "28800" },
                    { CoreTools.Translate("{0} hours", 12), "43200" },
                    { CoreTools.Translate("1 day"), "86400" },
                    { CoreTools.Translate("{0} days", 2), "172800" },
                    { CoreTools.Translate("{0} days", 3), "259200" },
                    { CoreTools.Translate("1 week"), "604800" }
                };

            foreach (KeyValuePair<string, string> entry in updates_dict)
            {
                UpdatesCheckIntervalSelector.AddItem(entry.Key, entry.Value, false);
            }

            UpdatesCheckIntervalSelector.ShowAddedItems();
        }

        public bool CanGoBack => true;
        public string ShortTitle => CoreTools.Translate("Package update preferences");

        public event EventHandler? RestartRequired;
        public event EventHandler<Type>? NavigationRequested;

        public void ShowRestartBanner(object sender, EventArgs e)
            => RestartRequired?.Invoke(this, e);

        private void OperationsSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationRequested?.Invoke(this, typeof(Operations));
        }

        private void AdminButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationRequested?.Invoke(this, typeof(Administrator));
        }
    }
}

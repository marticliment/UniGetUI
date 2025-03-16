using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Tools;
using UniGetUI.Pages.DialogPages;
using UniGetUI.PackageOperations;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Pages.SettingsPages.GeneralPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Operations : Page, ISettingsPage
    {
        public Operations()
        {
            this.InitializeComponent();

            for (int i = 1; i <= 10; i++)
            {
                ParallelOperationCount.AddItem(i.ToString(), i.ToString(), false);
            }

            ParallelOperationCount.AddItem("15", "15", false);
            ParallelOperationCount.AddItem("20", "20", false);
            ParallelOperationCount.AddItem("30", "30", false);
            ParallelOperationCount.AddItem("50", "50", false);
            ParallelOperationCount.AddItem("75", "75", false);
            ParallelOperationCount.AddItem("100", "100", false);
            ParallelOperationCount.ShowAddedItems();
        }

        public void ShowRestartBanner(object sender, EventArgs e)
            => RestartRequired?.Invoke(this, e);
        private void ManageDesktopShortcutsButton_Click(object sender, RoutedEventArgs e)
            => _ = DialogHelper.ManageDesktopShortcuts();

        public bool CanGoBack => true;
        public string ShortTitle => CoreTools.Translate("Package operation preferences");

        public event EventHandler? RestartRequired;
        public event EventHandler<Type>? NavigationRequested;

        private void ParallelOperationCount_OnValueChanged(object sender, EventArgs e)
        {
            if (int.TryParse(ParallelOperationCount.SelectedValue(), out int value))
            {
                AbstractOperation.MAX_OPERATIONS = value;
            }
        }

        private void UpdatesSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationRequested?.Invoke(this, typeof(Updates));
        }

        private void AdminButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationRequested?.Invoke(this, typeof(Administrator));
        }
    }
}

using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Pages.SettingsPages.GeneralPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Administrator : Page, ISettingsPage
    {
        public Administrator()
        {
            this.InitializeComponent();

            if(DoCacheAdminRights.Checked && DoCacheAdminRightsForBatches.Checked)
            {
                DoCacheAdminRights.IsEnabled = true;
                DoCacheAdminRightsForBatches.IsEnabled = true;
            }
        }

        public bool CanGoBack => true;
        public string ShortTitle => CoreTools.Translate("Administrator privileges preferences");

        public event EventHandler? RestartRequired;
        public event EventHandler<Type>? NavigationRequested;

        public void ShowRestartBanner(object sender, EventArgs e)
            => RestartRequired?.Invoke(this, e);

        public void RestartCache(object sender, EventArgs e)
            => _ = CoreTools.ResetUACForCurrentProcess();


    }
}

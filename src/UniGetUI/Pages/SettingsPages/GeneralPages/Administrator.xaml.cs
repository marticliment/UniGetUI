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

            WarningTitlebar.Title = CoreTools.Translate("Warning") + "!";
            WarningTitlebar.Message =
                CoreTools.Translate("The following settings may pose a security risk, hence they are disabled by default.") + " " + 
                CoreTools.Translate("Enable the settings below if and only if you fully understand what they do, and the implications they may have.") + "\n\n" + 
                CoreTools.Translate("The settings will list, in their descriptions, the potential security issues they may have.") + " ";

            // The following settings may pose a security risk, hence they are disabled by default. Enable them ONLY if you undertsand what you are doing. Some of those settings will show a UAC prompt before being enabled."

        }

        public bool CanGoBack => true;
        public string ShortTitle => CoreTools.Translate("Administrator rights and other dangerous settings");

        public event EventHandler? RestartRequired;
        public event EventHandler<Type>? NavigationRequested;

        public void ShowRestartBanner(object sender, EventArgs e)
            => RestartRequired?.Invoke(this, e);

        public void RestartCache(object sender, EventArgs e)
            => _ = CoreTools.ResetUACForCurrentProcess();


    }
}

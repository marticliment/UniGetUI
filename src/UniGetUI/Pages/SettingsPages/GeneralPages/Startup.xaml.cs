using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Tools;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Pages.SettingsPages.GeneralPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Startup : Page, ISettingsPage
    {
        public Startup()
        {
            this.InitializeComponent();
        }

        public bool CanGoBack => true;
        public string ShortTitle => CoreTools.Translate("Startup options");

        public event EventHandler? RestartRequired;
        public event EventHandler<Type>? NavigationRequested;

        private void EditAutostartSettings_Click(object sender, EventArgs e)
        {
            using Process p = new()
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
    }
}

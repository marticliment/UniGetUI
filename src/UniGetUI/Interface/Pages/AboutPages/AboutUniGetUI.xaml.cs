using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Data;
using UniGetUI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Pages.AboutPages
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutUniGetUI : Page
    {
        AppTools Tools = AppTools.Instance;
        public AboutUniGetUI()
        {
            this.InitializeComponent();
            VersionText.Text = Tools.Translate("You have installed WingetUI Version {0}").Replace("{0}", CoreData.VersionName);

        }
    }
}

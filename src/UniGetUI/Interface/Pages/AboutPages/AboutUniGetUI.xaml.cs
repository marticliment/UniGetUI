using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Data;
using UniGetUI.Core.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Pages.AboutPages
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutUniGetUI : Page
    {
        public AboutUniGetUI()
        {
            InitializeComponent();
            VersionText.Text = CoreTools.Translate("You have installed WingetUI Version {0}").Replace("{0}", CoreData.VersionName);

        }
    }
}

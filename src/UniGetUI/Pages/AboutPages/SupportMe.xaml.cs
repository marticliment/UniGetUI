using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Pages.AboutPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SupportMe : Page
    {
        public string SupportKofiAutomationName => CoreTools.Translate("Support the developer on Ko-fi");

        public SupportMe()
        {
            InitializeComponent();
        }
    }
}

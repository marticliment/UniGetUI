using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public partial class SettingsPageButton : SettingsCard
    {
        private string _headerText = string.Empty;
        private string _descriptionText = string.Empty;

        private void UpdateAutomationName()
        {
            string name = string.IsNullOrWhiteSpace(_descriptionText)
                ? _headerText
                : $"{_headerText}. {_descriptionText}";
            AutomationProperties.SetName(this, name);
        }

        public string Text
        {
            set
            {
                _headerText = CoreTools.Translate(value);
                Header = _headerText;
                UpdateAutomationName();
            }
        }

        public string UnderText
        {
            set
            {
                _descriptionText = CoreTools.Translate(value);
                Description = _descriptionText;
                UpdateAutomationName();
            }
        }

        public IconType Icon
        {
            set => HeaderIcon = new LocalIcon(value);
        }

        public SettingsPageButton()
        {
            CornerRadius = new CornerRadius(8);
            HorizontalAlignment = HorizontalAlignment.Stretch;
            IsClickEnabled = true;
            IsTabStop = true;
            UseSystemFocusVisuals = true;
        }
    }
}

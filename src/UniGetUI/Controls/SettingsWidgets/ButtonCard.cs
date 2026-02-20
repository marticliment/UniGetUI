using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public sealed partial class ButtonCard : SettingsCard
    {
        private readonly Button _button = new();
        private string _translatedText = string.Empty;
        private string _translatedButtonText = string.Empty;

        private void UpdateAutomationNames()
        {
            if (!string.IsNullOrWhiteSpace(_translatedButtonText))
            {
                string buttonName = string.IsNullOrWhiteSpace(_translatedText)
                    ? _translatedButtonText
                    : $"{_translatedButtonText}. {_translatedText}";
                AutomationProperties.SetName(_button, buttonName);
            }

            string cardName = _translatedText;
            if (!string.IsNullOrWhiteSpace(_translatedButtonText))
            {
                cardName = string.IsNullOrWhiteSpace(cardName)
                    ? _translatedButtonText
                    : $"{cardName}. {_translatedButtonText}";
            }

            if (!string.IsNullOrWhiteSpace(cardName))
            {
                AutomationProperties.SetName(this, cardName);
            }
        }

        public string ButtonText
        {
            set
            {
                _translatedButtonText = CoreTools.Translate(value);
                _button.Content = _translatedButtonText;
                UpdateAutomationNames();
            }
        }

        public string Text
        {
            set
            {
                _translatedText = CoreTools.Translate(value);
                Header = _translatedText;
                UpdateAutomationNames();
            }
        }

        public new event EventHandler<EventArgs>? Click;

        public ButtonCard()
        {
            _button.MinWidth = 200;
            _button.Click += (_, _) => { Click?.Invoke(this, EventArgs.Empty); };
            Content = _button;
            UpdateAutomationNames();
        }
    }
}

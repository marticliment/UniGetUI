using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public sealed partial class ButtonCard : SettingsCard
    {
        private readonly Button _button = new();

        public string ButtonText
        {
            set => _button.Content = CoreTools.Translate(value);
        }

        public string Text
        {
            set => Header = CoreTools.Translate(value);
        }

        public new event EventHandler<EventArgs>? Click;

        public ButtonCard()
        {
            _button.MinWidth = 200;
            _button.Click += (_, _) => { Click?.Invoke(this, EventArgs.Empty); };
            Content = _button;
        }
    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public sealed partial class TranslatedTextBlock : UserControl
    {
        public string __text = "";
        public string Text
        {
            set => ApplyText(value);
        }

        public string __suffix = "";
        public string Suffix
        {
            set { __suffix = value; ApplyText(null); }
        }
        public string __prefix = "";
        public string Prefix
        {
            set { __prefix = value; ApplyText(null); }
        }

        public TextWrapping WrappingMode
        {
            set => _textBlock.TextWrapping = value;
        }

        public TranslatedTextBlock()
        {
            InitializeComponent();
        }

        public void ApplyText(string? text)
        {
            try
            {
                if (text is not null) __text = CoreTools.Translate(text);
                if (_textBlock is not null)
                {
                    _textBlock.Text = __prefix + __text + __suffix;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}

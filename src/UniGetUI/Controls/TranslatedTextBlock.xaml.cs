using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
            set { __text = CoreTools.Translate(value); ApplyText(); }
        }

        public string __suffix = "";
        public string Suffix
        {
            set { __suffix = value; ApplyText(); }
        }
        public string __prefix = "";
        public string Prefix
        {
            set { __prefix = value; ApplyText(); }
        }

        public TextWrapping WrappingMode
        {
            set => __textblock.TextWrapping = value;
        }

        public TranslatedTextBlock()
        {
            InitializeComponent();
        }

        public void ApplyText()
        {
            if(__textblock is not null)
                __textblock.Text = __prefix + __text + __suffix;
        }
    }
}

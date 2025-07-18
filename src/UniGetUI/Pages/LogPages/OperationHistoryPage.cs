using Microsoft.UI.Xaml.Documents;
using UniGetUI.Core.SettingsEngine;

namespace UniGetUI.Interface.Pages.LogPage
{
    public partial class OperationHistoryPage : BaseLogPage
    {
        public OperationHistoryPage() : base(false)
        {

        }

        public override void LoadLog(bool isReload = false)
        {
            Paragraph paragraph = new();
            foreach (string line in Settings.GetValue(Settings.K.OperationHistory).Split("\n"))
            {
                if (line.Replace("\r", "").Replace("\n", "").Trim() == "")
                {
                    continue;
                }

                paragraph.Inlines.Add(new Run { Text = line.Replace("\r", "").Replace("\n", "") });
                paragraph.Inlines.Add(new LineBreak());
            }
            LogTextBox.Blocks.Clear();
            LogTextBox.Blocks.Add(paragraph);

        }

        protected override void LoadLogLevels()
        {
            throw new NotImplementedException();
        }
    }
}

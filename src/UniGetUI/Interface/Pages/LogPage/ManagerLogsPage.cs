using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine;

namespace UniGetUI.Interface.Pages.LogPage
{
    public class ManagerLogsPage : BaseLogPage
    {

        public ManagerLogsPage() : base(true)
        {

        }
        public override void LoadLog()
        {
            bool IS_DARK = ActualTheme == Microsoft.UI.Xaml.ElementTheme.Dark;

            bool verbose = LogLevelCombo.SelectedValue?.ToString()?.Contains("(") ?? false;
            foreach (PackageEngine.ManagerClasses.Manager.PackageManager manager in PEInterface.Managers)
            {
                if (manager.Name.Contains(LogLevelCombo.SelectedValue?.ToString()?.Split(' ')[0] ?? "uncontained_word"))
                {
                    PackageEngine.ManagerClasses.Classes.ManagerLogger TaskLogger = manager.TaskLogger;
                    LogTextBox.Blocks.Clear();
                    Paragraph versionParagraph = new();
                    versionParagraph.Inlines.Add(new Run() { Text = $"Manager {manager.Name} with version:\n" });
                    versionParagraph.Inlines.Add(new Run() { Text = manager.Status.Version });
                    versionParagraph.Inlines.Add(new Run() { Text = $"\n\n——————————————————————————————————————————\n\n" });
                    LogTextBox.Blocks.Add(versionParagraph);

                    foreach (PackageEngine.ManagerClasses.Classes.TaskLogger operation in TaskLogger.Operations)
                    {
                        Paragraph p = new();
                        foreach (string line in operation.AsColoredString(verbose))
                        {
                            Brush color = line[0] switch
                            {
                                '0' => new SolidColorBrush { Color = IS_DARK ? DARK_WHITE : LIGHT_WHITE },
                                '1' => new SolidColorBrush { Color = IS_DARK ? DARK_LIGHT_GREY : LIGHT_LIGHT_GREY },
                                '2' => new SolidColorBrush { Color = IS_DARK ? DARK_RED : LIGHT_RED },
                                '3' => new SolidColorBrush { Color = IS_DARK ? DARK_BLUE : LIGHT_BLUE },
                                '4' => new SolidColorBrush { Color = IS_DARK ? DARK_GREEN : LIGHT_GREEN },
                                '5' => new SolidColorBrush { Color = IS_DARK ? DARK_YELLOW : LIGHT_YELLOW },
                                _ => new SolidColorBrush { Color = IS_DARK ? DARK_YELLOW : LIGHT_YELLOW },
                            };
                            p.Inlines.Add(new Run() { Text = line[1..] + "\n", Foreground = color });
                        }
                        ((Run)p.Inlines[^1]).Text = ((Run)p.Inlines[^1]).Text.TrimEnd();
                        LogTextBox.Blocks.Add(p);
                    }
                    break;
                }

                MainScroller.ScrollToVerticalOffset(MainScroller.ScrollableHeight);
            }
        }

        protected override void LoadLogLevels()
        {
            LogLevelCombo.Items.Clear();
            foreach (PackageEngine.ManagerClasses.Manager.PackageManager manager in PEInterface.Managers)
            {
                LogLevelCombo.Items.Add(manager.Name);
                LogLevelCombo.Items.Add($"{manager.Name} ({CoreTools.Translate("Verbose")})");
            }
            LogLevelCombo.SelectedIndex = 0;
        }
    }
}

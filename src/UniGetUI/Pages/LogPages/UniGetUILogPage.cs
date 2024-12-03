using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;

namespace UniGetUI.Interface.Pages.LogPage
{
    public sealed class UniGetUILogPage : BaseLogPage
    {
        public UniGetUILogPage() : base(true)
        {
        }

        protected override void LoadLogLevels()
        {
            LogLevelCombo.Items.Clear();
            LogLevelCombo.Items.Add(CoreTools.Translate("1 - Errors"));
            LogLevelCombo.Items.Add(CoreTools.Translate("2 - Warnings"));
            LogLevelCombo.Items.Add(CoreTools.Translate("3 - Information (less)"));
            LogLevelCombo.Items.Add(CoreTools.Translate("4 - Information (more)"));
            LogLevelCombo.Items.Add(CoreTools.Translate("5 - information (debug)"));
            LogLevelCombo.SelectedIndex = 3;
        }

        public override void LoadLog(bool isReload = false)
        {
            bool IS_DARK = ActualTheme == Microsoft.UI.Xaml.ElementTheme.Dark;

            LogEntry[] logs = Logger.GetLogs();
            LogTextBox.Blocks.Clear();
            foreach (LogEntry log_entry in logs)
            {
                Paragraph p = new();
                if (log_entry.Content == "")
                {
                    continue;
                }

                if (LOG_LEVEL == 1 && (log_entry.Severity == LogEntry.SeverityLevel.Debug || log_entry.Severity == LogEntry.SeverityLevel.Info || log_entry.Severity == LogEntry.SeverityLevel.Success || log_entry.Severity == LogEntry.SeverityLevel.Warning))
                {
                    continue;
                }

                if (LOG_LEVEL == 2 && (log_entry.Severity == LogEntry.SeverityLevel.Debug || log_entry.Severity == LogEntry.SeverityLevel.Info || log_entry.Severity == LogEntry.SeverityLevel.Success))
                {
                    continue;
                }

                if (LOG_LEVEL == 3 && (log_entry.Severity == LogEntry.SeverityLevel.Debug || log_entry.Severity == LogEntry.SeverityLevel.Info))
                {
                    continue;
                }

                if (LOG_LEVEL == 4 && (log_entry.Severity == LogEntry.SeverityLevel.Debug))
                {
                    continue;
                }

                Brush color = log_entry.Severity switch
                {
                    LogEntry.SeverityLevel.Debug => new SolidColorBrush { Color = IS_DARK ? DARK_GREY : LIGHT_GREY },
                    LogEntry.SeverityLevel.Info => new SolidColorBrush { Color = IS_DARK ? DARK_LIGHT_GREY : LIGHT_LIGHT_GREY },
                    LogEntry.SeverityLevel.Success => new SolidColorBrush { Color = IS_DARK ? DARK_WHITE : LIGHT_WHITE },
                    LogEntry.SeverityLevel.Warning => new SolidColorBrush { Color = IS_DARK ? DARK_YELLOW : LIGHT_YELLOW },
                    LogEntry.SeverityLevel.Error => new SolidColorBrush { Color = IS_DARK ? DARK_RED : LIGHT_RED },
                    _ => new SolidColorBrush { Color = IS_DARK ? DARK_GREY : LIGHT_GREY },
                };
                string[] lines = log_entry.Content.Split('\n');
                int date_length = -1;
                foreach (string line in lines)
                {
                    if (date_length == -1)
                    {
                        p.Inlines.Add(new Run { Text = $"[{log_entry.Time}] {line}\n", Foreground = color });
                        date_length = $"[{log_entry.Time}] ".Length;
                    }
                    else
                    {
                        p.Inlines.Add(new Run { Text = new string(' ', date_length) + line + "\n", Foreground = color });
                    }
                } ((Run)p.Inlines[^1]).Text = ((Run)p.Inlines[^1]).Text.TrimEnd();
                LogTextBox.Blocks.Add(p);
            }
            if (isReload) MainScroller.ScrollToVerticalOffset(MainScroller.ScrollableHeight);
        }
    }
}

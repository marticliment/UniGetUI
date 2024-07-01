using ExternalLibraries.Clipboard;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public enum Logger_LogType
    {
        UniGetUILog,
        ManagerLogs,
        OperationHistory
    }

    public sealed partial class Logger_LogPage : Page, IPageWithKeyboardShortcuts
    {
        private int LOG_LEVEL = 4;
        public Logger_LogType Logger_LogType;
        public Logger_LogPage(Logger_LogType logger_LogType = Logger_LogType.UniGetUILog)
        {
            InitializeComponent();
            Logger_LogType = logger_LogType;
            LoadLog();


            if (Logger_LogType == Logger_LogType.UniGetUILog)
            {
                LogLevelCombo.Items.Clear();
                LogLevelCombo.Items.Add(CoreTools.Translate("1 - Errors"));
                LogLevelCombo.Items.Add(CoreTools.Translate("2 - Warnings"));
                LogLevelCombo.Items.Add(CoreTools.Translate("3 - Information (less)"));
                LogLevelCombo.Items.Add(CoreTools.Translate("4 - Information (more)"));
                LogLevelCombo.Items.Add(CoreTools.Translate("5 - information (debug)"));
                LogLevelCombo.SelectedIndex = 3;
            } else if (Logger_LogType == Logger_LogType.ManagerLogs)
            {
                LogLevelCombo.Items.Clear();
                foreach (PackageEngine.ManagerClasses.Manager.PackageManager manager in PEInterface.Managers)
                {
                    LogLevelCombo.Items.Add(manager.Name);
                    LogLevelCombo.Items.Add($"{manager.Name} ({CoreTools.Translate("Verbose")})");
                }
                LogLevelCombo.SelectedIndex = 0;
            }
            else
            {
                LogLevelCombo.Items.Clear();
                LogLevelCombo.Items.Add(CoreTools.Translate("Operation log"));
                LogLevelCombo.SelectedIndex = 0;
            }
        }

        public void ReloadTriggered()
        {
            LoadLog();
        }

        public void SelectAllTriggered()
        {
            LogTextBox.SelectAll();
        }

        public void SearchTriggered()
        { }

        public void SetText(string body)
        {
            Paragraph paragraph = new();
            foreach (string line in body.Split("\n"))
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

        // Dark theme colors
        Color DARK_GREY = Color.FromArgb(255, 130, 130, 130);
        Color DARK_LIGHT_GREY = Color.FromArgb(255, 190, 190, 190);
        Color DARK_WHITE = Color.FromArgb(255, 250, 250, 250);
        Color DARK_YELLOW = Color.FromArgb(255, 255, 255, 90);
        Color DARK_RED = Color.FromArgb(255, 255, 80, 80);
        Color DARK_GREEN = Color.FromArgb(255, 80, 255, 80);
        Color DARK_BLUE = Color.FromArgb(255, 120, 120, 255);

        // Light theme colors
        Color LIGHT_GREY = Color.FromArgb(255, 125, 125, 225);
        Color LIGHT_LIGHT_GREY = Color.FromArgb(255, 50, 50, 150);
        Color LIGHT_WHITE = Color.FromArgb(255, 0, 0, 0);
        Color LIGHT_YELLOW = Color.FromArgb(255, 150, 150, 0);
        Color LIGHT_RED = Color.FromArgb(255, 205, 0, 0);
        Color LIGHT_GREEN = Color.FromArgb(255, 0, 205, 0);
        Color LIGHT_BLUE = Color.FromArgb(255, 0, 0, 205);

        public void LoadLog()
        {


            bool IS_DARK = MainApp.Instance.ThemeListener.CurrentTheme == ApplicationTheme.Dark;
            
            if (Logger_LogType == Logger_LogType.UniGetUILog)
            {

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
                    else if(LOG_LEVEL == 2 && (log_entry.Severity == LogEntry.SeverityLevel.Debug || log_entry.Severity == LogEntry.SeverityLevel.Info || log_entry.Severity == LogEntry.SeverityLevel.Success))
                    {
                        continue;
                    }
                    else if(LOG_LEVEL == 3 && (log_entry.Severity == LogEntry.SeverityLevel.Debug || log_entry.Severity == LogEntry.SeverityLevel.Info))
                    {
                        continue;
                    }
                    else if(LOG_LEVEL == 4 && (log_entry.Severity == LogEntry.SeverityLevel.Debug))
                    {
                        continue;
                    }

                    Brush color;


                    switch (log_entry.Severity)
                    {
                        case LogEntry.SeverityLevel.Debug:
                            color = new SolidColorBrush { Color = IS_DARK ? DARK_GREY: LIGHT_GREY };
                            break;
                        case LogEntry.SeverityLevel.Info:
                            color = new SolidColorBrush { Color = IS_DARK ? DARK_LIGHT_GREY : LIGHT_LIGHT_GREY };
                            break;
                        case LogEntry.SeverityLevel.Success:
                            color = new SolidColorBrush { Color = IS_DARK ? DARK_WHITE : LIGHT_WHITE};
                            break;
                        case LogEntry.SeverityLevel.Warning:
                            color = new SolidColorBrush { Color = IS_DARK ? DARK_YELLOW : LIGHT_YELLOW };
                            break;
                        case LogEntry.SeverityLevel.Error:
                            color = new SolidColorBrush { Color = IS_DARK ? DARK_RED : LIGHT_RED };
                            break;
                        default:
                            color = new SolidColorBrush { Color = IS_DARK ? DARK_GREY : LIGHT_GREY };
                            break;
                    }

                    string[] lines = log_entry.Content.Split('\n');
                    int date_length = -1;
                    foreach(string line in lines)
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
                MainScroller.ScrollToVerticalOffset(MainScroller.ScrollableHeight);
            }

            else if (Logger_LogType == Logger_LogType.ManagerLogs)
            {
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
                                Brush color;
                                switch (line[0])
                                {
                                    case '0':
                                        color = new SolidColorBrush { Color = IS_DARK ? DARK_WHITE : LIGHT_WHITE};
                                        break;
                                    case '1':
                                        color = new SolidColorBrush { Color = IS_DARK ? DARK_LIGHT_GREY : LIGHT_LIGHT_GREY };
                                        break;
                                    case '2':
                                        color = new SolidColorBrush { Color = IS_DARK ? DARK_RED : LIGHT_RED };
                                        break;
                                    case '3':
                                        color = new SolidColorBrush { Color = IS_DARK ? DARK_BLUE : LIGHT_BLUE };
                                        break;
                                    case '4':
                                        color = new SolidColorBrush { Color = IS_DARK ? DARK_GREEN : LIGHT_GREEN };
                                        break;
                                    case '5':
                                        color = new SolidColorBrush { Color = IS_DARK ? DARK_YELLOW : LIGHT_YELLOW };
                                        break;
                                    default:
                                        color = new SolidColorBrush { Color = IS_DARK ? DARK_YELLOW : LIGHT_YELLOW };
                                        break;
                                }
                                p.Inlines.Add(new Run() { Text = line[1..] + "\n", Foreground = color });
                            }
                            ((Run)p.Inlines[^1]).Text = ((Run)p.Inlines[^1]).Text.TrimEnd();
                            LogTextBox.Blocks.Add(p);
                        }
                        break;
                    }
                }

                MainScroller.ScrollToVerticalOffset(MainScroller.ScrollableHeight);
            }
            else if (Logger_LogType == Logger_LogType.OperationHistory)
            {
                SetText(Settings.GetValue("OperationHistory"));
            }
        }

        public void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.SelectAll();
            WindowsClipboard.SetText(LogTextBox.SelectedText);
            LogTextBox.Select(LogTextBox.SelectionStart, LogTextBox.SelectionStart);
        }

        public async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker savePicker = new();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, WinRT.Interop.WindowNative.GetWindowHandle(MainApp.Instance.MainWindow));
            savePicker.FileTypeChoices.Add(CoreTools.Translate("Text"), new List<string>() { ".txt" });
            savePicker.SuggestedFileName = CoreTools.Translate("WingetUI Log");

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                LogTextBox.SelectAll();
                await File.WriteAllTextAsync(file.Path, LogTextBox.SelectedText);
                LogTextBox.Select(LogTextBox.SelectionStart, LogTextBox.SelectionStart);
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "explorer.exe",
                    Arguments = @$"/select, ""{file.Path}"""
                });

            }
        }

        public void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadLog();
        }


        private void LogLevelCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LOG_LEVEL = LogLevelCombo.SelectedIndex + 1;
            LoadLog();
        }
    }
}

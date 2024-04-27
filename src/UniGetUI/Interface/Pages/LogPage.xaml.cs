using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UniGetUI.Core;
using UniGetUI.Core.Data;
using ExternalLibraries.Clipboard;
using UniGetUI.Core.Logging;
using Windows.Storage;
using Windows.Storage.Pickers;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using System.Linq;
using Windows.UI.WebUI;
using Microsoft.UI.Xaml.Media;
using Windows.Media.Playback;
using Windows.UI;
using CommunityToolkit.WinUI.Controls;
using CommunityToolkit.WinUI.Helpers;

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

    public sealed partial class Logger_LogPage : Page
    {
        public Logger_LogType Logger_LogType;
        public Logger_LogPage(Logger_LogType logger_LogType = Logger_LogType.UniGetUILog)
        {
            InitializeComponent();
            Logger_LogType = logger_LogType;
            LoadLog();
        }

        public void SetText(string body)
        {
            Paragraph paragraph = new();
            foreach (string line in body.Split("\n"))
            {
                if (line.Replace("\r", "").Replace("\n", "").Trim() == "")
                    continue;
                paragraph.Inlines.Add(new Run() { Text = line.Replace("\r", "").Replace("\n", "") });
                paragraph.Inlines.Add(new LineBreak());
            }
            LogTextBox.Blocks.Clear();
            LogTextBox.Blocks.Add(paragraph);
        }

        public void LoadLog()
        {


            if (Logger_LogType == Logger_LogType.UniGetUILog)
            {
                // Dark theme colors
                Color DARK_GREY = Color.FromArgb(255, 128, 128, 128);
                Color DARK_BLUE = Color.FromArgb(255, 115, 121, 191);
                Color DARK_GREEN = Color.FromArgb(255, 90, 204, 94);
                Color DARK_YELLOW = Color.FromArgb(255, 255, 201, 51);
                Color DARK_RED = Color.FromArgb(255, 209, 38, 38);

                // Light theme colors
                Color LIGHT_GREY = Color.FromArgb(255, 100, 100, 100);
                Color LIGHT_BLUE = Color.FromArgb(255, 52, 58, 168);
                Color LIGHT_GREEN = Color.FromArgb(255, 56, 150, 18);
                Color LIGHT_YELLOW = Color.FromArgb(255, 179, 156, 2);
                Color LIGHT_RED = Color.FromArgb(255, 209, 38, 38);

                bool IS_DARK = MainApp.Instance.ThemeListener.CurrentTheme == ApplicationTheme.Dark;

                var logs = Logger.GetLogs();
                LogTextBox.Blocks.Clear();
                foreach (var log_entry in logs)
                {
                    var p = new Paragraph();
                    if (log_entry.Content == "")
                        continue;

                    Brush color;


                    switch (log_entry.Severity)
                    {
                        case LogEntry.SeverityLevel.Debug:
                            color = new SolidColorBrush() { Color = IS_DARK? DARK_GREY: LIGHT_GREY };
                            break;
                        case LogEntry.SeverityLevel.Info:
                            color = new SolidColorBrush() { Color = IS_DARK ? DARK_BLUE : LIGHT_BLUE };
                            break;
                        case LogEntry.SeverityLevel.Success:
                            color = new SolidColorBrush() { Color = IS_DARK ? DARK_GREEN : LIGHT_GREEN };
                            break;
                        case LogEntry.SeverityLevel.Warning:
                            color = new SolidColorBrush() { Color = IS_DARK ? DARK_YELLOW : LIGHT_YELLOW };
                            break;
                        case LogEntry.SeverityLevel.Error:
                            color = new SolidColorBrush() { Color = IS_DARK ? DARK_RED : LIGHT_RED };
                            break;
                        default:
                            color = new SolidColorBrush() { Color = IS_DARK ? DARK_GREY : LIGHT_GREY };
                            break;
                    }

                    var lines = log_entry.Content.Split('\n');
                    var date_length = -1;
                    foreach(var line in lines)
                        if (date_length == -1)
                        {
                            p.Inlines.Add(new Run() { Text = $"[{log_entry.Time}] {line}\n", Foreground = color });
                            date_length = $"[{log_entry.Time}] ".Length;
                        }
                        else
                        {
                            p.Inlines.Add(new Run() { Text = new string(' ', date_length) + line + "\n", Foreground = color });
                        }
                    (p.Inlines[^1] as Run).Text = (p.Inlines[^1] as Run).Text.TrimEnd();
                    LogTextBox.Blocks.Add(p);
                }
                //SetText(text);
            }
            else if (Logger_LogType == Logger_LogType.ManagerLogs)
            {
                SetText(CoreData.ManagerLogs);
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
            savePicker.SuggestedFileName = CoreTools.Translate("WingetUI Logger.Log");

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
    }
}

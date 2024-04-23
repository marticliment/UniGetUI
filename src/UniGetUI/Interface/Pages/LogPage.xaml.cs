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
                SetText(CoreData.UniGetUILog);
            }
            else if (Logger_LogType == Logger_LogType.ManagerLogs)
            {
                SetText(CoreData.ManagerLogs);
            }
            else if (Logger_LogType == Logger_LogType.OperationHistory)
            {
                SetText(AppTools.GetSettingsValue_Static("OperationHistory"));
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
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, WinRT.Interop.WindowNative.GetWindowHandle(AppTools.Instance.App.MainWindow));
            savePicker.FileTypeChoices.Add(AppTools.Instance.Translate("Text"), new List<string>() { ".txt" });
            savePicker.SuggestedFileName = AppTools.Instance.Translate("WingetUI Logger.Log");

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

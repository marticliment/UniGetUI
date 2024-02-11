using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using ModernWindow.Structures;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Security.Cryptography.Core;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.ApplicationModel.DataTransfer;
using System.Diagnostics;
using Windows.Storage.Pickers;
using Windows.Storage;
using ModernWindow.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public enum LogType
    {
        WingetUILog,
        ManagerLogs,
        OperationHistory
    }

    public sealed partial class LogPage : Page
    {
        public LogType LogType;
        public LogPage(LogType logType = LogType.WingetUILog)
        {
            this.InitializeComponent();
            LogType = logType;
            LoadLog();
        }

        public void LoadLog()
        {
            if(LogType == LogType.WingetUILog)
            {
                LogTextBox.Text = CoreData.WingetUILog;
            }
            else if(LogType == LogType.ManagerLogs)
            {
                LogTextBox.Text = CoreData.ManagerLogs;
            }
            else if(LogType == LogType.OperationHistory)
            {
                LogTextBox.Text = AppTools.GetSettingsValue_Static("OperationHistory");
            }
        }

        public void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.WindowsClipboard.SetText(LogTextBox.Text);
        }

        public async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, WinRT.Interop.WindowNative.GetWindowHandle(AppTools.Instance.App.mainWindow));
            savePicker.FileTypeChoices.Add(AppTools.Instance.Translate("Text"), new List<string>() { ".txt" });
            savePicker.SuggestedFileName = AppTools.Instance.Translate("WingetUI Log");

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await File.WriteAllTextAsync(file.Path, LogTextBox.Text);
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

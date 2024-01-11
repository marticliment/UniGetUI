using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.SettingsTab
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainInterface : Window
    {
        private MainApp _app;
        public MainInterface(MainApp app)
        {
            this.InitializeComponent();
            _app = app;
        }

        public bool GetSettings(string setting)
        {
            return true;//return _app.GetSettings(setting);
        }

        public void SetSettings(string setting, bool value)
        {
            //_app.SetSettings(setting, value);
        }

        public int GetHwnd()
        {
            return (int)WinRT.Interop.WindowNative.GetWindowHandle(this);
        }

        public void ShowWindow_SAFE()
        {
            Console.WriteLine("Called from Python!");
            DispatcherQueue.TryEnqueue(() => { this.Activate(); });
        }


    }
}

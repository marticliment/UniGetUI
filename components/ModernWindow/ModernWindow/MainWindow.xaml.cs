using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            Runtime.PythonDLL = @"C:\Users\marti\AppData\Local\Programs\Python\Python311\Python311.dll";
            PythonEngine.Initialize();
            PythonEngine.BeginAllowThreads();
            this.SystemBackdrop = new MicaBackdrop();

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Start Import");

            using (Py.GIL())
            {
                PythonEngine.Exec(@"
import sys
import warnings
warnings.simplefilter(""ignore"", UserWarning)
sys.coinit_flags = 2
import os
print(""Running WingetUI Python Module from: "" + os.getcwd())
import wingetui.__main__");
            }
        }
    }
}

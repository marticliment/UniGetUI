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
using Python.Runtime;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Graphics.DirectX.Direct3D11;
using ModernWindow.Structures;
using System.Threading.Tasks;
using System.Threading;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.SettingsTab.Widgets
{

    public class ManagerSource
    {
        public class Capabilities
        {
            public bool KnowsUpdateDate { get; set; }
            public bool KnowsPackageCount { get; set; }
        }

        public string Name { get; set; }
        public string Url { get; set; }
        public int? PackageCount { get; set; }
        public string UpdateDate { get; set; }
        public dynamic Manager { get; set; }

        public ManagerSource(dynamic manager, string name, string url, int? package_count = null, string update_date = null)
        {
            Name = name;
            Manager = manager;
            Url = url;
            PackageCount = package_count;
            UpdateDate = update_date;
        }

        public ManagerSource(PyObject PythonManagerSource)
        {
            Name = ((dynamic)PythonManagerSource).Name;
            Manager = ((dynamic)PythonManagerSource).Manager;
            Url = ((dynamic)PythonManagerSource).Url;
            if(Manager.Capabilities.Sources.KnowsPackageCount)
                PackageCount = ((dynamic)PythonManagerSource).PackageCount;
            if(Manager.ToPython().Capabilities.Sources.KnowsUpdateDate)
                UpdateDate = ((dynamic)PythonManagerSource).UpdateDate;
        }

        public dynamic GetPythonSource()
        {
            // Needs testing
            MainAppBindings bindings = MainAppBindings.Instance;
            dynamic Source = ((dynamic)bindings).App.PackageClasses.ManagerSource(Manager, Url, PackageCount, UpdateDate);
            return Source;
        }

   
    }
    public sealed partial class SourceManager : UserControl
    {

        private dynamic Manager { get; set; }
        private List<ManagerSource> Sources { get; set; }
        public SourceManager()
        {
            this.InitializeComponent();
            Manager = MainAppBindings.Instance.App.PackageTools.Scoop;

        }

    }
}

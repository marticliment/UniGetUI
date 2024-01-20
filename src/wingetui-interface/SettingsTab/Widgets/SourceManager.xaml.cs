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
using ModernWindow.PackageEngine;
using ModernWindow.PackageEngine.Managers;
using System.Diagnostics;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI;
using CommunityToolkit.WinUI.UI.Controls.Primitives;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.SettingsTab.Widgets
{   public class SourceItem
    {
        public SourceManager Parent;
        public ManagerSource Source;

        public SourceItem(SourceManager Parent, ManagerSource Source)
        {
            this.Parent = Parent;
            this.Source = Source;
        }
        public void Remove(object sender, RoutedEventArgs e)
        {
            Parent.RemoveSource(Source);
        }
    }
    public sealed partial class SourceManager : UserControl
    {
        private dynamic Manager { get; set; }
        private List<SourceItem> Sources { get; set; }

        private ListView _datagrid{ get; set; }
        public SourceManager()
        {
            Sources = new List<SourceItem>();
            this.InitializeComponent();
            _datagrid = DataList;
            DataList.ItemTemplate = (DataTemplate)Resources["ManagerSourceTemplate"];
            _datagrid.ItemsSource = Sources;
            LoadSources();
        }

        public async void LoadSources()
        {
            Sources = new List<SourceItem>();
            Scoop scoop = new Scoop();
            scoop.Initialize();
            foreach(ManagerSource Source in await scoop.GetSources())
            {
                Sources.Add(new SourceItem(this, Source));
            }
            _datagrid.ItemsSource = Sources;
        }

        public void RemoveSource(ManagerSource Source)
        {
            Console.WriteLine("Clicked " + Source.Name);
        }
    }
}

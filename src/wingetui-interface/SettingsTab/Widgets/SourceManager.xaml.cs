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
using System.Collections.ObjectModel;

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
            Parent.RemoveSourceItem(this);
        }
    }
    public sealed partial class SourceManager : UserControl
    {
        private dynamic Manager { get; set; }
        private ObservableCollection<SourceItem> Sources = new ObservableCollection<SourceItem>();

        private ListView _datagrid{ get; set; }
        public SourceManager()
        {
            this.InitializeComponent();
            _datagrid = DataList;
            DataList.ItemTemplate = (DataTemplate)Resources["ManagerSourceTemplate"];
            LoadSources();
        }

        public async void LoadSources()
        {
            LoadingBar.Visibility = Visibility.Visible;
            Sources.Clear();
            Scoop scoop = new Scoop();
            scoop.Initialize();
            foreach(ManagerSource Source in await scoop.GetSources())
            {
                Sources.Add(new SourceItem(this, Source));
            }
            LoadingBar.Visibility = Visibility.Collapsed;
        }

        public void RemoveSourceItem(SourceItem Item)
        {
            Sources.Remove(Item);
            // TODO: Implement source uninstallation
            // Item.Source.Manager.UninstallSource(Item.Source);
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSources();
        }
    }
}

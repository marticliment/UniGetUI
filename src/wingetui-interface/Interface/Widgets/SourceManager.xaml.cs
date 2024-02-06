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
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Graphics.DirectX.Direct3D11;
using ModernWindow.Structures;
using System.Threading.Tasks;
using System.Threading;
using ModernWindow.PackageEngine;
using ModernWindow.PackageEngine.Managers;
using System.Diagnostics;
using Microsoft.UI;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface.Widgets
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
        private PackageManagerWithSources Manager { get; set; }
        private ObservableCollection<SourceItem> Sources = new ObservableCollection<SourceItem>();
        private AppTools bindings = AppTools.Instance;

        private ListView _datagrid{ get; set; }
        public SourceManager(PackageManagerWithSources Manager)
        {
            this.InitializeComponent();
            Header.Text = bindings.Translate("Manage {0} sources").Replace("{0}", Manager.Properties.Name);
            AddSourceButton.Content = bindings.Translate("Add source");
            this.Manager = Manager;
            _datagrid = DataList;
            DataList.ItemTemplate = (DataTemplate)Resources["ManagerSourceTemplate"];
            LoadSources();
        }

        public async void LoadSources()
        {
            if(!Manager.Status.Found)
                return;

            LoadingBar.Visibility = Visibility.Visible;
            Sources.Clear();
            foreach(ManagerSource Source in await Manager.GetSources())
            {
                Sources.Add(new SourceItem(this, Source));
            }
            if (Sources.Count > 0)
                _datagrid.SelectedIndex = 0;
            
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

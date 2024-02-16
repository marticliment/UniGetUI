using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ModernWindow.PackageEngine;
using ModernWindow.Structures;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface.Widgets
{
    public class SourceItem
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
            var op = new RemoveSourceOperation(Source);
            Parent.bindings.AddOperationToList(op);
            op.OperationSucceeded += (sender, e) => { Parent.RemoveSourceItem(this); };
        }
    }
    public sealed partial class SourceManager : UserControl
    {
        private PackageManagerWithSources Manager { get; set; }
        private ObservableCollection<SourceItem> Sources = new();
        public AppTools bindings = AppTools.Instance;

        private ListView _datagrid { get; set; }
        public SourceManager(PackageManagerWithSources Manager)
        {
            InitializeComponent();
            Header.Text = bindings.Translate("Manage {0} sources").Replace("{0}", Manager.Properties.Name);
            AddSourceButton.Content = bindings.Translate("Add source");
            AddSourceButton.Click += async (sender, e) => { 
                ContentDialog d = new ContentDialog();
                d.Title = bindings.Translate("Add source");
                
                var SourcesCombo = new ComboBox();
                var NameSourceRef = new Dictionary<string, ManagerSource>();
                foreach(var source in Manager.KnownSources)
                {
                    SourcesCombo.Items.Add(source.Name);
                    NameSourceRef.Add(source.Name, source);
                }
                d.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
                StackPanel p = new StackPanel();
                p.Spacing = 8;
                p.Children.Add(new TextBlock() { Text = bindings.Translate("Select the source you want to add:") });
                p.Children.Add(SourcesCombo);
                SourcesCombo.Items.Add(bindings.Translate("Other"));
                SourcesCombo.SelectedIndex = 0;
                SourcesCombo.HorizontalAlignment = HorizontalAlignment.Stretch;

                d.XamlRoot = this.XamlRoot;
                d.Content = p;
                d.PrimaryButtonText = bindings.Translate("Add");
                d.SecondaryButtonText = bindings.Translate("Cancel");
                d.DefaultButton = ContentDialogButton.Primary;

                if(await bindings.App.mainWindow.ShowDialog(d) == ContentDialogResult.Primary)
                {
                    if (bindings.Translate("Other") != SourcesCombo.SelectedValue.ToString())
                    {
                        var op = new AddSourceOperation(NameSourceRef[SourcesCombo.SelectedValue.ToString()]);
                        bindings.AddOperationToList(op);
                        op.OperationSucceeded += (sender, e) => { LoadSources(); };
                    }
                }
            };
            this.Manager = Manager;
            _datagrid = DataList;
            DataList.ItemTemplate = (DataTemplate)Resources["ManagerSourceTemplate"];
            LoadSources();
        }

        public async void LoadSources()
        {
            if (!Manager.Status.Found)
                return;

            LoadingBar.Visibility = Visibility.Visible;
            Sources.Clear();
            foreach (ManagerSource Source in await Manager.GetSources())
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
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSources();
        }
    }
}

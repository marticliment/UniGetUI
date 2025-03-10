using System.Collections.ObjectModel;
using System.Data.Common;
using System.Diagnostics;
using ExternalLibraries.Clipboard;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Pages
{
    public class AdvancedOperationHistoryEntry
    {
        public required string Title { get; set; }
        public required string Content { get; set; }
    }

    public class SimplePackage
    {
        public string Description { get; }
        public string[] Tags { get; }
        public string Name { get; }
        public string Id { get; }
        public string VersionString { get; }
        public string NewVersionString { get; }
        public string IconId { get; }
        public string ManagerName { get; }
        public string ManagerDisplayName { get; }
        public string SourceName { get; }
        public Uri SourceUrl { get; }
        public IconType SourceIconId { get; }

        public SimplePackage(IPackage Source)
        {
            // Load the details first
            Source.Details.Load();

            // Now assign the values
            Description = Source.Details.Description ?? "No description";
            Tags = Source.Details.Tags;
            Name = Source.Name;
            Id = Source.Id;
            VersionString = Source.VersionString;
            NewVersionString = Source.NewVersionString;
            IconId = Package.GetPackageIconId(Source.Id, Source.Manager.Name, Source.Source.Name);
            ManagerName = Source.Manager.Name;
            ManagerDisplayName = Source.Manager.DisplayName;
            SourceName = Source.Source.Name;
            SourceUrl = Source.Source.Url;
            SourceIconId = Source.Source.IconId;
        }
    }

    public partial class AdvancedOperationHistoryPage : IKeyboardShortcutListener, IEnterLeaveListener
    {
        private readonly ObservableCollection<AdvancedOperationHistoryEntry> Items = [];

        public AdvancedOperationHistoryPage()
        {
            InitializeComponent();

            AdvancedOperationHistoryList.ItemsSource = Items;
        }

        private void LoadOperationHistory()
        {
            Items.Clear();

            Items.Add(new AdvancedOperationHistoryEntry { Title = "Test 1", Content = "Content 1" });
            Items.Add(new AdvancedOperationHistoryEntry { Title = "Test 2", Content = "Content 2" });
            Items.Add(new AdvancedOperationHistoryEntry { Title = "Test 3", Content = "Content 3" });
            Items.Add(new AdvancedOperationHistoryEntry { Title = "Test 4", Content = "Content 4" });
            Items.Add(new AdvancedOperationHistoryEntry { Title = "Test 5", Content = "Content 5" });
        }

        public void ReloadTriggered()
            => LoadOperationHistory();
        public void SelectAllTriggered()
        { }
        public void SearchTriggered()
        { }
        public void OnEnter()
            => LoadOperationHistory();
        public void OnLeave()
        { }
    }
}

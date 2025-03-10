using System.Collections.ObjectModel;
using System.Diagnostics;
using ExternalLibraries.Clipboard;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Tools;
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

    public partial class AdvancedOperationHistoryPage : IKeyboardShortcutListener, IEnterLeaveListener
    {
        private ObservableCollection<AdvancedOperationHistoryEntry> Items = new ();

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

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
    public class OperationHistoryEntry
    {
        public string Title { get; set; }
        public string Content { get; set; }
    }

    public partial class AdvancedOperationHistoryPage : IKeyboardShortcutListener, IEnterLeaveListener
    {
        private ObservableCollection<OperationHistoryEntry> Items = new ();

        public AdvancedOperationHistoryPage()
        {
            InitializeComponent();

            Items.Add(new OperationHistoryEntry { Title = "Test 1", Content = "Content 1" });
            Items.Add(new OperationHistoryEntry { Title = "Test 2", Content = "Content 2" });
            Items.Add(new OperationHistoryEntry { Title = "Test 3", Content = "Content 3" });
            Items.Add(new OperationHistoryEntry { Title = "Test 4", Content = "Content 4" });
            Items.Add(new OperationHistoryEntry { Title = "Test 5", Content = "Content 5" });

            AdvancedOperationHistoryList.ItemsSource = Items;
        }

        public void ReloadTriggered()
        { }
        public void SelectAllTriggered()
        { }
        public void SearchTriggered()
        { }
        public void OnEnter()
        { }
        public void OnLeave()
        { }
    }
}

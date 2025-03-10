using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.Interface.Pages
{

    public partial class AdvancedOperationHistoryPage : IKeyboardShortcutListener, IEnterLeaveListener
    {
        private ObservableCollection<AdvancedOperationHistoryEntry> Items = [];

        public AdvancedOperationHistoryPage()
        {
            InitializeComponent();

            LoadOperationHistory();
        }

        private void LoadOperationHistory()
        {
            Items.Clear();

            Items = [.. Settings.GetList<AdvancedOperationHistoryEntry>("AdvancedOperationHistory") ?? []];
            AdvancedOperationHistoryList.ItemsSource = Items;
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

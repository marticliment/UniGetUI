using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.Interface.Pages
{
    public class AdvancedOperationHistoryEntry(IPackage Source, OperationType Operation) : SimplePackage(Source)
    {
        public OperationType Operation { get; } = Operation;
    };

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

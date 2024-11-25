using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Classes.Packages.Classes;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.Interface
{

    public sealed partial class NewDesktopShortcutsManager : Page
    {
        public event EventHandler? Close;
        private ObservableCollection<ShortcutEntry> desktopShortcuts = new ObservableCollection<ShortcutEntry>();

        public NewDesktopShortcutsManager()
        {
            InitializeComponent();
            NewDeletableDesktopShortcutsList.ItemsSource = desktopShortcuts;
        }

        public async Task UpdateData()
        {
            desktopShortcuts.Clear();

            foreach (var shortcutPath in DesktopShortcutsDatabase.GetAwaitingVerdicts())
            {
                var shortcutEntry = new ShortcutEntry(shortcutPath, true, desktopShortcuts);
                desktopShortcuts.Add(shortcutEntry);
                NewDeletableDesktopShortcutsList.SelectedItems.Add(shortcutEntry);
            }
        }

        private void CloseButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
        }

        public void ContinueButton_Click(object sender, ContentDialogButtonClickEventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);

            foreach (ShortcutEntry shortcut in NewDeletableDesktopShortcutsList.SelectedItems)
            {
                DesktopShortcutsDatabase.Add(shortcut.ShortcutPath);
                DesktopShortcutsDatabase.DeleteShortcut(shortcut.ShortcutPath);
            }
            foreach (var shortcut in desktopShortcuts)
            {
                if (DesktopShortcutsDatabase.CanShortcutBeDeleted(shortcut.ShortcutPath) == DesktopShortcutsDatabase.ShortcutDeletableStatus.Unknown)
                {
                    DesktopShortcutsDatabase.Add(shortcut.ShortcutPath, false);
                }
                DesktopShortcutsDatabase.RemoveFromAwaitingVerdicts(shortcut.ShortcutPath);
            }
        }

        private async void EnableAllButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            foreach (var shortcut in desktopShortcuts)
            {
                NewDeletableDesktopShortcutsList.SelectedItems.Add(shortcut);
            }
        }

        private void DisableAllButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            NewDeletableDesktopShortcutsList.SelectedItems.Clear();
        }
    }
}

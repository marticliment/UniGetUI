using System.Collections.ObjectModel;
using System.Diagnostics;
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
            DeletableDesktopShortcutsList.ItemsSource = desktopShortcuts;
        }

        public async Task UpdateData()
        {
            desktopShortcuts.Clear();

            foreach (var shortcutPath in DesktopShortcutsDatabase.GetAwaitingVerdicts())
            {
                var shortcutEntry = new ShortcutEntry(shortcutPath, desktopShortcuts);
                desktopShortcuts.Add(shortcutEntry);
                DeletableDesktopShortcutsList.SelectedItems.Add(shortcutEntry);
            }

        }

        private void CloseButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
        }

        public void ContinueButton_Click(object sender, ContentDialogButtonClickEventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);

            foreach (ShortcutEntry shortcut in DeletableDesktopShortcutsList.SelectedItems)
            {
                DesktopShortcutsDatabase.Add(shortcut.ShortcutPath);
                DesktopShortcutsDatabase.DeleteShortcut(shortcut.ShortcutPath);
            }
            foreach (var shortcut in desktopShortcuts)
            {
                DesktopShortcutsDatabase.RemoveFromAwaitingVerdicts(shortcut.ShortcutPath);
            }
        }


        private async void EnableAllButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            foreach (var shortcut in desktopShortcuts)
            {
                DeletableDesktopShortcutsList.SelectedItems.Add(shortcut);
            }
        }

        private void DisableAllButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            DeletableDesktopShortcutsList.SelectedItems.Clear();
        }
    }

    public class ShortcutEntry
    {
        public string ShortcutPath { get; }
        private ObservableCollection<ShortcutEntry> List { get; }

        public ShortcutEntry(string shortcutPath, ObservableCollection<ShortcutEntry> list)
        {
            ShortcutPath = shortcutPath;
            List = list;
        }

        public void OpenShortcutPath()
        {
            Process.Start("explorer.exe", "/select," + $"\"{ShortcutPath}\"");
        }
    }
}

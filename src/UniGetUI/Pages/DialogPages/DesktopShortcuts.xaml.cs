using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.PackageEngine.Classes.Packages.Classes;
using UniGetUI.Pages.DialogPages;
using UniGetUI.Core.SettingsEngine;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DesktopShortcutsManager : Page
    {
        public event EventHandler? Close;
        private readonly ObservableCollection<ShortcutEntry> Shortcuts = [];

        public DesktopShortcutsManager()
        {
            InitializeComponent();
            DeletableDesktopShortcutsList.ItemsSource = Shortcuts;

            AutoDeleteShortcutsCheckbox.IsChecked = Settings.Get(Settings.K.RemoveAllDesktopShortcuts);
            AutoDeleteShortcutsCheckbox.Checked += HandleAllDesktop_Checked;
            AutoDeleteShortcutsCheckbox.Unchecked += HandleAllDesktop_Unchecked;
        }

        public void LoadShortcuts(IReadOnlyList<string> NewShortcuts)
        {
            Shortcuts.Clear();
            List<ShortcutEntry> items = new();
            foreach (var shortcut in NewShortcuts)
            {
                var status = DesktopShortcutsDatabase.GetStatus(shortcut);
                var entry = new ShortcutEntry(shortcut, status is DesktopShortcutsDatabase.Status.Delete);
                entry.OnReset += (_, _) => Shortcuts.Remove(entry);
                items.Add(entry);
            }

            foreach (var item in items.OrderBy(s => s.Name))
            {
                Shortcuts.Add(item);
            }
        }

        /*private async void ManualScanButton_Click(object sender, RoutedEventArgs e)
        {
            SaveChanges();
            var shortcutsOnDesktop = DesktopShortcutsDatabase.GetShortcutsOnDisk();
            List<string> UnknownShortcuts = new();

            foreach (var shortcut in shortcutsOnDesktop)
            {
                if(DesktopShortcutsDatabase.GetStatus(shortcut) is DesktopShortcutsDatabase.Status.Unknown)
                {
                    UnknownShortcuts.Add(shortcut);
                }
            }
            if (UnknownShortcuts.Any())
            {
                LoadShortcuts(UnknownShortcuts);
                ManualScanFlyout.Hide();
            }
            else
            {
                ManualScanFlyout.Hide();
                Close?.Invoke(this, EventArgs.Empty);
                await DialogHelper.ManualScanDidNotFoundNewShortcuts();
                _ = DialogHelper.ManageDesktopShortcuts();
            }
        }*/

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
        }

        private void YesResetButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (ShortcutEntry shortcut in Shortcuts.ToArray())
            {
                shortcut.ResetShortcut();
            }
            ConfirmResetFlyout.Hide();
        }

        private void NoResetButton_Click(object sender, RoutedEventArgs e)
        {
            ConfirmResetFlyout.Hide();
        }

        private async void HandleAllDesktop_Checked(object sender, RoutedEventArgs e)
        {
            SaveChanges();
            Close?.Invoke(this, new());
            _ = DialogHelper.ConfirmSetDeleteAllShortcutsSetting();
        }

        private void HandleAllDesktop_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.Set(Settings.K.RemoveAllDesktopShortcuts, false);
        }

        public void SaveChanges()
        {
            foreach (var shortcut in Shortcuts)
            {
                DesktopShortcutsDatabase.AddToDatabase(shortcut.Path, shortcut.IsDeletable? DesktopShortcutsDatabase.Status.Delete: DesktopShortcutsDatabase.Status.Maintain);
                DesktopShortcutsDatabase.RemoveFromUnknownShortcuts(shortcut.Path);

                if (shortcut.IsDeletable && File.Exists(shortcut.Path))
                {
                    DesktopShortcutsDatabase.DeleteFromDisk(shortcut.Path);
                }
            }
        }

        private void CloseSaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveChanges();
            Close?.Invoke(this, EventArgs.Empty);
        }
    }

    public partial class ShortcutEntry : INotifyPropertyChanged
    {
        public event EventHandler? OnReset;

        public string Path { get; }
        public string Name { get; }
        private bool _deletable;

        public bool IsDeletable
        {
            get =>  _deletable;
            set
            {
                _deletable = value;
                OnPropertyChanged();
            }
        }

        public bool ExistsOnDisk
        {
            get => File.Exists(Path);
        }

        public ShortcutEntry(string path, bool isDeletable)
        {
            Path = path;
            Name = string.Join('.', path.Split("\\")[^1].Split('.')[..^1]);
            IsDeletable = isDeletable;
        }

        public void OpenShortcutPath()
        {
            Process.Start("explorer.exe", "/select," + $"\"{Path}\"");
        }

        public void ResetShortcut()
        {
            DesktopShortcutsDatabase.AddToDatabase(this.Path, DesktopShortcutsDatabase.Status.Unknown);
            OnReset?.Invoke(this, EventArgs.Empty);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

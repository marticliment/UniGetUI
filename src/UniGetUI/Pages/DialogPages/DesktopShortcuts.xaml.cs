using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UniGetUI.PackageEngine.Classes.Packages.Classes;

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
        private readonly ObservableCollection<ShortcutEntry> desktopShortcuts = new ObservableCollection<ShortcutEntry>();

        private bool NewOnly;

        public DesktopShortcutsManager(List<string>? NewShortcuts)
        {
            if (NewShortcuts is not null) NewOnly = true;
            InitializeComponent();
            DeletableDesktopShortcutsList.ItemsSource = desktopShortcuts;
            DeletableDesktopShortcutsList.DoubleTapped += DeletableDesktopShortcutsList_DoubleTapped;
            UpdateData(NewShortcuts);
        }

        private void UpdateData(List<string>? NewShortcuts)
        {
            desktopShortcuts.Clear();

            if (NewShortcuts is not null) foreach (var path in NewShortcuts)
            {
                desktopShortcuts.Add(new(path, false));
            }
            else foreach (var (shortcutPath, shortcutEnabled) in DesktopShortcutsDatabase.GetDatabase())
            {
                var shortcutEntry = new ShortcutEntry(shortcutPath, shortcutEnabled);
                desktopShortcuts.Add(shortcutEntry);
            }

            foreach(var shortcut in desktopShortcuts)
            {
                shortcut.OnReset += (sender, path) =>
                {
                    if (sender is not ShortcutEntry sh) throw new InvalidOperationException();
                    DesktopShortcutsDatabase.ResetShortcut(sh.ShortcutPath);
                    desktopShortcuts.Remove(sh);
                };
            }
        }

        private void DeletableDesktopShortcutsList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (DeletableDesktopShortcutsList.SelectedItem is ShortcutEntry shortcut)
            {
                shortcut.ResetConfiguration();
                desktopShortcuts.Remove(shortcut);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
        }

        private void YesResetButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (ShortcutEntry shortcut in desktopShortcuts.ToArray())
            {
                shortcut.ResetConfiguration();
            }
            desktopShortcuts.Clear();
            ConfirmResetFlyout.Hide();
        }

        private void NoResetButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ConfirmResetFlyout.Hide();
        }

        public void SaveChangesAndClose()
        {
            Close?.Invoke(this, EventArgs.Empty);
            foreach (var shortcut in desktopShortcuts)
            {
                DesktopShortcutsDatabase.AddToDatabase(shortcut.ShortcutPath, shortcut.IsChecked);
                if (shortcut.IsChecked && File.Exists(shortcut.ShortcutPath))
                {
                    DesktopShortcutsDatabase.DeleteFromDisk(shortcut.ShortcutPath);
                }
                DesktopShortcutsDatabase.RemoveFromUnknownShortcuts(shortcut.ShortcutPath);
            }
        }
    }

    public class ShortcutEntry : INotifyPropertyChanged
    {
        public event EventHandler<EventArgs>? OnReset;

        public string ShortcutPath { get; }
        private bool _enabled;
        public bool IsChecked
        {
            get =>  _enabled;
            set
            {
                _enabled = value;
                OnPropertyChanged();
            }
        }
        public bool ShortcutExists
        {
            get => File.Exists(ShortcutPath);
        }

        public ShortcutEntry(string shortcutPath, bool enabled)
        {
            ShortcutPath = shortcutPath;
            IsChecked = enabled;
        }

        public void OpenShortcutPath()
        {
            Process.Start("explorer.exe", "/select," + $"\"{ShortcutPath}\"");
        }

        public void ResetConfiguration()
        {
            OnReset?.Invoke(this, EventArgs.Empty);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}

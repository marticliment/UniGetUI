using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Classes.Packages.Classes;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Managers.WingetManager;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>

    public sealed partial class IgnoredUpdatesManager : Page
    {
        public event EventHandler? Close;
        private readonly ObservableCollection<IgnoredPackageEntry> ignoredPackages = new ObservableCollection<IgnoredPackageEntry>();

        public IgnoredUpdatesManager()
        {
            UpdateData();
            InitializeComponent();
            IgnoredUpdatesList.ItemsSource = ignoredPackages;
            // IgnoredUpdatesList.DoubleTapped += IgnoredUpdatesList_DoubleTapped;
        }

        private void UpdateData()
        {
            Dictionary<string, IPackageManager> ManagerNameReference = [];

            foreach (IPackageManager Manager in PEInterface.Managers)
            {
                ManagerNameReference.Add(Manager.Name.ToLower(), Manager);
            }

            ignoredPackages.Clear();

            var rawIgnoredPackages = IgnoredUpdatesDatabase.GetDatabase();

            foreach (var(ignoredId, version) in rawIgnoredPackages)
            {
                IPackageManager manager = PEInterface.WinGet; // Manager by default
                if (ManagerNameReference.ContainsKey(ignoredId.Split("\\")[0]))
                {
                    manager = ManagerNameReference[ignoredId.Split("\\")[0]];
                }

                ignoredPackages.Add(new IgnoredPackageEntry(ignoredId.Split("\\")[^1], version, manager, ignoredPackages));
            }
        }

        private async void IgnoredUpdatesList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (IgnoredUpdatesList.SelectedItem is IgnoredPackageEntry package)
            {
                await package.RemoveFromIgnoredUpdates();
            }
        }

        private void CloseButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
        }

        private async void YesResetButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            foreach (IgnoredPackageEntry package in ignoredPackages.ToArray())
            {
                await package.RemoveFromIgnoredUpdates();
            }
            ConfirmResetFlyout.Hide();
        }

        private void NoResetButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ConfirmResetFlyout.Hide();
        }
    }

    public class IgnoredPackageEntry
    {
        public string Id { get; }
        public string Name { get; }
        public string Version { get; }
        public string NewVersion { get; }
        public IPackageManager Manager { get; }
        private ObservableCollection<IgnoredPackageEntry> List { get; }
        public IgnoredPackageEntry(string id, string version, IPackageManager manager, ObservableCollection<IgnoredPackageEntry> list)
        {
            Id = id;

            if (manager is WinGet && id.Contains('.')) Name = String.Join(' ', id.Split('.')[1..]);
            else Name = CoreTools.FormatAsName(id);

            if (version == "*")
            {
                Version = CoreTools.Translate("All versions");
            }
            else
            {
                Version = version;
            }

            string CurrentVersion = PEInterface.InstalledPackagesLoader.GetPackageForId(id)?.Version ?? "Unknown";

            if (PEInterface.UpgradablePackagesLoader.IgnoredPackages.TryGetValue(Id, out IPackage? package)
                && package.NewVersion != package.Version)
            {
                NewVersion = CurrentVersion + " \u27a4 " + package.NewVersion;
            }
            else if (CurrentVersion != "Unknown")
            {
                NewVersion = CoreTools.Translate("Up to date") + $" ({CurrentVersion})";;
            }
            else
            {
                NewVersion = CoreTools.Translate("Unknown");
            }

            Manager = manager;
            List = list;
        }

        public async Task RemoveFromIgnoredUpdates()
        {
            string ignoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";
            await Task.Run(() => IgnoredUpdatesDatabase.Remove(ignoredId));

            // If possible, add the package to the software updates tab again
            if (PEInterface.UpgradablePackagesLoader.IgnoredPackages.TryRemove(Id, out IPackage? nativePackage)
                && nativePackage.NewVersion != nativePackage.Version)
            {
                PEInterface.UpgradablePackagesLoader.AddForeign(nativePackage);
            }

            foreach (IPackage package in PEInterface.InstalledPackagesLoader.Packages)
            {
                if (Manager == package.Manager && package.Id == Id)
                {
                    package.SetTag(PackageTag.Default);
                    break;
                }
            }

            List.Remove(this);
        }
    }
}

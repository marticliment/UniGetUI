using System.Text.Json.Nodes;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Classes.Packages.Classes;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

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

        public IgnoredUpdatesManager()
        {
            InitializeComponent();
            IgnoredUpdatesList.DoubleTapped += IgnoredUpdatesList_DoubleTapped;
        }

        public async Task UpdateData()
        {
            Dictionary<string, IPackageManager> ManagerNameReference = [];

            foreach (IPackageManager Manager in PEInterface.Managers)
            {
                ManagerNameReference.Add(Manager.Name.ToLower(), Manager);
            }

            IgnoredUpdatesList.Items.Clear();

            foreach (var(ignoredId, version) in await Task.Run(() => IgnoredUpdatesDatabase.GetDatabase()).ConfigureAwait(false))
            {
                IPackageManager manager = PEInterface.WinGet; // Manager by default
                if (ManagerNameReference.ContainsKey(ignoredId.Split("\\")[0]))
                {
                    manager = ManagerNameReference[ignoredId.Split("\\")[0]];
                }

                IgnoredUpdatesList.Items.Add(new IgnoredPackageEntry(ignoredId.Split("\\")[^1], version, manager, IgnoredUpdatesList));
            }

        }

        private async void IgnoredUpdatesList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (IgnoredUpdatesList.SelectedItem is IgnoredPackageEntry package)
            {
                await package.RemoveFromIgnoredUpdates();
            }
        }

        public async void ManageIgnoredUpdates_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            foreach (IgnoredPackageEntry package in IgnoredUpdatesList.Items.ToArray())
            {
                await package.RemoveFromIgnoredUpdates();
            }
        }

        private void CloseButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
        }
    }

    public class IgnoredPackageEntry
    {
        public string Id { get; }
        public string Name { get; }
        public string Version { get; }
        public IPackageManager Manager { get; }
        private ListView List { get; }
        public IgnoredPackageEntry(string id, string version, IPackageManager manager, ListView list)
        {
            Id = id;
            Name = CoreTools.FormatAsName(id);
            if (version == "*")
            {
                Version = CoreTools.Translate("All versions");
            }
            else
            {
                Version = version;
            }

            Manager = manager;
            List = list;
        }

        public async Task RemoveFromIgnoredUpdates()
        {
            string ignoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";
            await Task.Run(() => IgnoredUpdatesDatabase.Remove(ignoredId)).ConfigureAwait(false);

            foreach (IPackage package in PEInterface.InstalledPackagesLoader.Packages)
            {
                if (Manager == package.Manager && package.Id == Id)
                {
                    package.SetTag(PackageTag.Default);
                    break;
                }
            }

            List.Items.Remove(this);
        }
    }
}

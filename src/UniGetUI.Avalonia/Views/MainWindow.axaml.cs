using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using UniGetUI.Avalonia.Controls;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageLoader;
using UniGetUI.PackageOperations;

namespace UniGetUI.Avalonia.Views;

public partial class MainWindow : Window
{
    private bool _managersReady;

    private PackagesPage? _discoverPage;
    private PackagesPage? _updatesPage;
    private PackagesPage? _installedPage;
    private SettingsPage? _settingsPage;


    private readonly ObservableCollection<OperationControl> _operations = new();

    public MainWindow()
    {
        InitializeComponent();
        OperationList.ItemsSource = _operations;
        _operations.CollectionChanged += (_, _) =>
            Dispatcher.UIThread.Post(() =>
            {
                bool hasOps = _operations.Count > 0;
                OperationPanel.IsVisible = hasOps;
                OperationSplitter.IsVisible = hasOps;
            });
    }

    public void OnManagersReady()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _managersReady = true;
            LoadingBanner.IsOpen = false;

            _discoverPage = new PackagesPage();
            _discoverPage.Initialize(DiscoverablePackagesLoader.Instance, "Discover Packages", OperationType.Install);
            _discoverPage.OnOperationCreated = AddOperation;
            _discoverPage.OnPackageDetailsRequested = ShowPackageDetails;
            _discoverPage.OnInstallOptionsRequested = ShowInstallOptions;

            _updatesPage = new PackagesPage();
            _updatesPage.Initialize(UpgradablePackagesLoader.Instance, "Software Updates", OperationType.Update);
            _updatesPage.OnOperationCreated = AddOperation;
            _updatesPage.OnPackageDetailsRequested = ShowPackageDetails;
            _updatesPage.OnInstallOptionsRequested = ShowInstallOptions;

            _installedPage = new PackagesPage();
            _installedPage.Initialize(InstalledPackagesLoader.Instance, "Installed Packages", OperationType.Uninstall);
            _installedPage.OnOperationCreated = AddOperation;
            _installedPage.OnPackageDetailsRequested = ShowPackageDetails;
            _installedPage.OnInstallOptionsRequested = ShowInstallOptions;

            _settingsPage = new SettingsPage();

            // Listen for update count changes to show badge
            UpgradablePackagesLoader.Instance.PackagesChanged += (_, _) =>
                Dispatcher.UIThread.Post(UpdateUpdatesBadge);
            UpdateUpdatesBadge();

            // Select first item
            NavView.SelectedItem = NavView.MenuItems[0];
        });
    }

    private void UpdateUpdatesBadge()
    {
        int count = UpgradablePackagesLoader.Instance.Packages.Count;
        if (count > 0)
        {
            NavUpdatesItem.InfoBadge = new InfoBadge { Value = count };
        }
        else
        {
            NavUpdatesItem.InfoBadge = null;
        }
    }

    // ─── Package details modal ─────────────────────────────────────────

    private void ShowPackageDetails(IPackage package, OperationType role)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var detailsPage = new PackageDetailsPage(package, role);
            detailsPage.OnOperationCreated = AddOperation;

            PackageDetailsContent.Content = detailsPage;
            PackageDetailsOverlay.IsVisible = true;
        });
    }

    private void ClosePackageDetails()
    {
        PackageDetailsOverlay.IsVisible = false;
        PackageDetailsContent.Content = null;
    }

    private void PackageDetailsOverlay_BackgroundClick(object? sender, global::Avalonia.Input.PointerPressedEventArgs e)
    {
        ClosePackageDetails();
    }

    private void PackageDetailsOverlay_CloseClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        ClosePackageDetails();
    }

    private void ShowInstallOptions(IPackage package, OperationType role)
    {
        _ = InstallOptionsOverlay.Show(package, role);
    }

    // ─── Operations ─────────────────────────────────────────────────────

    private void AddOperation(AbstractOperation operation)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var control = new OperationControl(operation, _operations, DetailDialog);
            _operations.Add(control);
        });
    }

    private void ClearAllOperations_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        for (int i = _operations.Count - 1; i >= 0; i--)
        {
            _operations[i].Detach();
            _operations.RemoveAt(i);
        }
    }

    private void ClearCompletedOps_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        for (int i = _operations.Count - 1; i >= 0; i--)
        {
            var op = _operations[i];
            // Check if the operation is not running/queued
            if (op.IsFinished)
            {
                op.Detach();
                _operations.RemoveAt(i);
            }
        }
    }

    private void RetryFailedOps_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        foreach (var op in _operations)
        {
            op.RetryIfFailed();
        }
    }

    // ─── Navigation ─────────────────────────────────────────────────────

    private void NavView_SelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (e.SelectedItem is NavigationViewItem item)
        {
            string tag = item.Tag?.ToString() ?? "";
            Navigate(tag);
        }
    }

    private void Navigate(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return;

        if (!_managersReady)
        {
            ContentFrame.Content = new TextBlock
            {
                Text = $"{tag} — Loading managers...",
                FontSize = 20,
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
                Opacity = 0.6
            };
            return;
        }

        ContentFrame.Content = tag switch
        {
            "Discover" => _discoverPage,
            "Updates" => _updatesPage,
            "Installed" => _installedPage,
            "Settings" => _settingsPage,
            _ => null
        };
    }
}

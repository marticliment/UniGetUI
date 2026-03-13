using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using UniGetUI.Avalonia.Controls;
using UniGetUI.Avalonia.Models;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.PackageLoader;
using UniGetUI.PackageEngine.Serializable;
using UniGetUI.PackageOperations;

namespace UniGetUI.Avalonia.Views;

public partial class PackagesPage : UserControl
{
    private AbstractPackageLoader? _loader;
    private readonly ObservableCollection<PackageWrapper> _allPackages = new();
    private readonly ObservableCollection<PackageWrapper> _filteredPackages = new();
    private readonly List<PackageWrapper> _wrapperCache = new();
    private string _currentQuery = "";
    private string _currentManagerFilter = "";
    private string _title = "";
    private OperationType _pageRole = OperationType.None;
    private DateTime? _lastLoadTime;

    private enum ViewMode { List, Grid }
    private ViewMode _viewMode = ViewMode.List;

    // Sorting
    private string _sortProperty = "Name";
    private bool _sortAscending = true;

    // Select all checkbox in column header
    private CheckBox? _selectAllCheckBox;
    private bool _suppressSelectAllEvent;

    // Debounce filter updates to avoid rapid rebuilds
    private CancellationTokenSource? _filterDebounce;

    public Action<AbstractOperation>? OnOperationCreated { get; set; }
    public Action<IPackage, OperationType>? OnPackageDetailsRequested { get; set; }
    public Action<IPackage, OperationType>? OnInstallOptionsRequested { get; set; }

    public PackagesPage()
    {
        InitializeComponent();
        SetupDataGridColumns();
        PackageGrid.ItemsSource = _filteredPackages;
        GridItems.ItemsSource = _filteredPackages;
        SetViewMode(ViewMode.List);
    }

    private void SetupDataGridColumns()
    {
        var selectAllCheckBox = new CheckBox
        {
            MinWidth = 0,
            Margin = new global::Avalonia.Thickness(0),
            IsThreeState = true,
        };
        selectAllCheckBox.IsCheckedChanged += SelectAllHeader_Changed;
        _selectAllCheckBox = selectAllCheckBox;

        PackageGrid.Columns.Add(new DataGridCheckBoxColumn
        {
            Header = selectAllCheckBox,
            Binding = new Binding("IsChecked") { Mode = BindingMode.TwoWay },
            Width = new DataGridLength(40),
        });
        PackageGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "",
            Binding = new Binding("StatusIcon"),
            Width = new DataGridLength(30),
            IsReadOnly = true,
        });
        PackageGrid.Columns.Add(new DataGridTemplateColumn
        {
            Header = "Name",
            Width = new DataGridLength(2, DataGridLengthUnitType.Star),
            IsReadOnly = true,
            CellTemplate = new FuncDataTemplate<PackageWrapper>((wrapper, _) =>
            {
                var panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                var img = new global::Avalonia.Controls.Image
                {
                    Width = 20,
                    Height = 20,
                    Stretch = global::Avalonia.Media.Stretch.Uniform,
                    [!global::Avalonia.Controls.Image.SourceProperty] = new Binding("IconBitmap"),
                };
                panel.Children.Add(img);

                var text = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = global::Avalonia.Media.TextTrimming.CharacterEllipsis,
                    [!TextBlock.TextProperty] = new Binding("Package.Name"),
                };
                panel.Children.Add(text);

                return panel;
            }),
        });
        PackageGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Package ID",
            Binding = new Binding("Package.Id"),
            Width = new DataGridLength(1.5, DataGridLengthUnitType.Star),
            IsReadOnly = true,
        });
        PackageGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Version",
            Binding = new Binding("VersionComboString"),
            Width = new DataGridLength(150),
            IsReadOnly = true,
        });
        PackageGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Source",
            Binding = new Binding("Package.Source.Name"),
            Width = new DataGridLength(100),
            IsReadOnly = true,
        });
    }

    public void Initialize(AbstractPackageLoader loader, string title, OperationType role = OperationType.None)
    {
        _loader = loader;
        _title = title;
        _pageRole = role;

        // Configure page header
        PageTitle.Text = title;
        switch (role)
        {
            case OperationType.Install:
                PageIcon.Symbol = Symbol.Find;
                PageSubtitle.Text = "Search and install new packages";
                BatchActionLabel.Text = "Install selected";
                BatchActionIcon.Symbol = Symbol.Download;
                break;
            case OperationType.Update:
                PageIcon.Symbol = Symbol.Up;
                PageSubtitle.Text = "Update your installed packages";
                BatchActionLabel.Text = "Update selected";
                BatchActionIcon.Symbol = Symbol.Up;
                break;
            case OperationType.Uninstall:
                PageIcon.Symbol = Symbol.List;
                PageSubtitle.Text = "Manage your installed packages";
                BatchActionLabel.Text = "Uninstall selected";
                BatchActionIcon.Symbol = Symbol.Delete;
                break;
        }

        // Configure context menu visibility based on role
        CtxInstall.IsVisible = role is OperationType.Install or OperationType.None;
        CtxUpdate.IsVisible = role is OperationType.Update;
        CtxUninstall.IsVisible = role is OperationType.Uninstall or OperationType.None;
        CtxInstallSudo.IsVisible = role is OperationType.Install or OperationType.None;
        CtxUpdateSudo.IsVisible = role is OperationType.Update;
        CtxUninstallSudo.IsVisible = role is OperationType.Uninstall or OperationType.None;

        _loader.PackagesChanged += OnPackagesChanged;
        _loader.StartedLoading += OnStartedLoading;
        _loader.FinishedLoading += OnFinishedLoading;

        if (_loader.IsLoaded || _loader.Packages.Count > 0)
        {
            PopulateFromLoader();
            UpdateFilter();
        }

        if (_loader.IsLoading)
            Dispatcher.UIThread.Post(() => LoadingBar.IsVisible = true);
    }

    // ─── Loader events ──────────────────────────────────────────────────

    private void OnPackagesChanged(object? sender, PackagesChangedEvent e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (e.AddedPackages.Count == 0 && e.RemovedPackages.Count == 0)
            {
                // Metadata-only change (tag update, etc.) — no structural change needed
                return;
            }

            if (e.RemovedPackages.Count > 0 || !e.ProceduralChange)
            {
                PopulateFromLoader();
            }
            else
            {
                // Only new packages added — just add wrappers for them
                foreach (var pkg in e.AddedPackages)
                {
                    // Check we don't already have this package
                    if (_wrapperCache.Any(w => w.Package.GetHash() == pkg.GetHash()))
                        continue;

                    var wrapper = new PackageWrapper(pkg);
                    _wrapperCache.Add(wrapper);
                    _allPackages.Add(wrapper);
                }
            }
            // Debounce the filter update
            ScheduleFilterUpdate();
        });
    }

    private void OnStartedLoading(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            LoadingBar.IsVisible = true;
            StatusText.Text = "Loading packages...";
            EmptyPlaceholder.IsVisible = false;
        });
    }

    private void OnFinishedLoading(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            LoadingBar.IsVisible = false;
            _lastLoadTime = DateTime.Now;
            PopulateFromLoader();
            PopulateManagerFilter();
            UpdateFilter(); // Immediate update on finish (no debounce)
        });
    }

    // ─── Data ───────────────────────────────────────────────────────────

    /// <summary>
    /// Sync wrappers with the loader's package list.
    /// Reuses existing wrappers for packages that are still present (preserves icon cache).
    /// Only creates new wrappers for genuinely new packages.
    /// </summary>
    private void PopulateFromLoader()
    {
        if (_loader is null) return;

        var loaderPackages = _loader.Packages;

        // Build a lookup of existing wrappers by package hash
        var existingByHash = new Dictionary<long, PackageWrapper>();
        foreach (var w in _wrapperCache)
            existingByHash.TryAdd(w.Package.GetHash(), w);

        // Build the new set of wrappers, reusing existing ones
        var newCache = new List<PackageWrapper>(loaderPackages.Count);
        var newAll = new List<PackageWrapper>(loaderPackages.Count);
        var reusedHashes = new HashSet<long>();

        foreach (var pkg in loaderPackages)
        {
            long hash = pkg.GetHash();
            if (existingByHash.TryGetValue(hash, out var existing))
            {
                newCache.Add(existing);
                newAll.Add(existing);
                reusedHashes.Add(hash);
            }
            else
            {
                var wrapper = new PackageWrapper(pkg);
                newCache.Add(wrapper);
                newAll.Add(wrapper);
            }
        }

        // Dispose wrappers that are no longer in the loader
        foreach (var w in _wrapperCache)
        {
            if (!reusedHashes.Contains(w.Package.GetHash()))
                w.Dispose();
        }

        _wrapperCache.Clear();
        _wrapperCache.AddRange(newCache);
        _allPackages.Clear();
        foreach (var w in newAll)
            _allPackages.Add(w);

        PopulateManagerFilter();
        UpdateManagersStatus();
    }

    private void PopulateManagerFilter()
    {
        if (_loader is null) return;

        var managers = _allPackages
            .Select(w => w.Package.Manager.DisplayName)
            .Distinct().OrderBy(n => n).ToList();

        var current = ManagerFilter.SelectedItem as string;
        ManagerFilter.ItemsSource = new[] { "All managers" }.Concat(managers).ToList();

        if (current != null && managers.Contains(current))
            ManagerFilter.SelectedItem = current;
        else
            ManagerFilter.SelectedIndex = 0;
    }

    private void UpdateManagersStatus()
    {
        var names = _allPackages
            .Select(w => w.Package.Manager.DisplayName)
            .Distinct().OrderBy(n => n).ToList();
        ManagersStatusText.Text = names.Count > 0 ? string.Join(" · ", names) : "";
    }

    private void ScheduleFilterUpdate()
    {
        _filterDebounce?.Cancel();
        _filterDebounce = new CancellationTokenSource();
        var token = _filterDebounce.Token;

        _ = Task.Delay(150, token).ContinueWith(_ =>
        {
            if (!token.IsCancellationRequested)
                Dispatcher.UIThread.Post(UpdateFilter);
        }, TaskScheduler.Default);
    }

    // ─── Filter & Sort ──────────────────────────────────────────────────

    private void UpdateFilter()
    {
        var query = NormalizeForSearch(_currentQuery);
        var mgrFilter = _currentManagerFilter;

        // Build the desired list
        var filtered = _allPackages.Where(w =>
        {
            if (!MatchesManagerFilter(w, mgrFilter)) return false;
            if (!string.IsNullOrEmpty(query) && !MatchesQuery(w, query)) return false;
            return true;
        });

        // Sort
        filtered = _sortProperty switch
        {
            "Id" => _sortAscending
                ? filtered.OrderBy(w => w.Package.Id, StringComparer.OrdinalIgnoreCase)
                : filtered.OrderByDescending(w => w.Package.Id, StringComparer.OrdinalIgnoreCase),
            "Version" => _sortAscending
                ? filtered.OrderBy(w => w.Package.VersionString, StringComparer.OrdinalIgnoreCase)
                : filtered.OrderByDescending(w => w.Package.VersionString, StringComparer.OrdinalIgnoreCase),
            "Source" => _sortAscending
                ? filtered.OrderBy(w => w.Package.Source.Name, StringComparer.OrdinalIgnoreCase)
                : filtered.OrderByDescending(w => w.Package.Source.Name, StringComparer.OrdinalIgnoreCase),
            _ => _sortAscending
                ? filtered.OrderBy(w => w.Package.Name, StringComparer.OrdinalIgnoreCase)
                : filtered.OrderByDescending(w => w.Package.Name, StringComparer.OrdinalIgnoreCase),
        };

        var desired = filtered.ToList();

        // Diff update: only add/remove what changed to avoid list flicker
        SyncCollection(_filteredPackages, desired);

        var total = _allPackages.Count;
        var shown = _filteredPackages.Count;
        StatusText.Text = shown == total ? $"{total} packages" : $"{shown} of {total} packages";

        if (_lastLoadTime.HasValue)
            LastLoadTimeText.Text = $"Last loaded: {_lastLoadTime.Value:HH:mm:ss}";

        EmptyPlaceholder.IsVisible = shown == 0 && !LoadingBar.IsVisible;
        if (shown == 0 && total > 0)
            EmptyText.Text = "No packages match your search";
        else if (shown == 0)
            EmptyText.Text = "No packages found";

        UpdateSelectAllState();
    }

    /// <summary>
    /// Sync an ObservableCollection to match a desired list, minimizing add/remove operations.
    /// This avoids Clear() + re-add which causes the DataGrid to flicker.
    /// </summary>
    private static void SyncCollection(ObservableCollection<PackageWrapper> collection, List<PackageWrapper> desired)
    {
        // Remove items not in desired
        for (int i = collection.Count - 1; i >= 0; i--)
        {
            if (!desired.Contains(collection[i]))
                collection.RemoveAt(i);
        }

        // Add/reorder items to match desired
        for (int i = 0; i < desired.Count; i++)
        {
            if (i < collection.Count)
            {
                if (!ReferenceEquals(collection[i], desired[i]))
                {
                    int existingIdx = -1;
                    for (int j = i + 1; j < collection.Count; j++)
                    {
                        if (ReferenceEquals(collection[j], desired[i]))
                        {
                            existingIdx = j;
                            break;
                        }
                    }

                    if (existingIdx >= 0)
                        collection.Move(existingIdx, i);
                    else
                        collection.Insert(i, desired[i]);
                }
            }
            else
            {
                collection.Add(desired[i]);
            }
        }
    }

    private static bool MatchesManagerFilter(PackageWrapper w, string filter) =>
        string.IsNullOrEmpty(filter) || filter == "All managers" || w.Package.Manager.DisplayName == filter;

    private static bool MatchesQuery(PackageWrapper w, string q) =>
        NormalizeForSearch(w.Package.Name).Contains(q) || NormalizeForSearch(w.Package.Id).Contains(q);

    private static string NormalizeForSearch(string input)
    {
        input = input.ToLowerInvariant()
            .Replace("-", "").Replace("_", "").Replace(" ", "")
            .Replace("@", "").Replace(".", "").Replace(",", "").Replace(":", "");

        ReadOnlySpan<(char target, string accented)> map =
        [
            ('a', "àáäâ"), ('e', "èéëê"), ('i', "ìíïî"), ('o', "òóöô"),
            ('u', "ùúüû"), ('y', "ýÿ"), ('c', "ç"), ('n', "ñ"),
        ];
        foreach (var (target, accented) in map)
            foreach (var c in accented)
                input = input.Replace(c, target);
        return input;
    }

    // ─── View mode ──────────────────────────────────────────────────────

    private void SetViewMode(ViewMode mode)
    {
        _viewMode = mode;
        PackageGrid.IsVisible = mode == ViewMode.List;
        GridView.IsVisible = mode == ViewMode.Grid;
        ViewListBtn.Opacity = mode == ViewMode.List ? 1.0 : 0.5;
        ViewGridBtn.Opacity = mode == ViewMode.Grid ? 1.0 : 0.5;
    }

    // ─── Operations ─────────────────────────────────────────────────────

    private PackageWrapper? GetSelectedWrapper()
    {
        return PackageGrid.SelectedItem as PackageWrapper;
    }

    private async Task LaunchOperation(IPackage package, OperationType type, bool elevated = false)
    {
        var options = await InstallOptionsFactory.LoadApplicableAsync(package, elevated: elevated);

        AbstractOperation operation = type switch
        {
            OperationType.Install => new InstallPackageOperation(package, options),
            OperationType.Update => new UpdatePackageOperation(package, options),
            OperationType.Uninstall => new UninstallPackageOperation(package, options),
            _ => throw new ArgumentException($"Unsupported operation type: {type}")
        };

        OnOperationCreated?.Invoke(operation);
    }

    private void CtxInstall_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetSelectedWrapper() is { } w) _ = LaunchOperation(w.Package, OperationType.Install);
    }

    private void CtxUpdate_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetSelectedWrapper() is { } w) _ = LaunchOperation(w.Package, OperationType.Update);
    }

    private void CtxUninstall_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetSelectedWrapper() is { } w) _ = LaunchOperation(w.Package, OperationType.Uninstall);
    }

    private void CtxInstallSudo_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetSelectedWrapper() is { } w) _ = LaunchOperation(w.Package, OperationType.Install, elevated: true);
    }

    private void CtxUpdateSudo_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetSelectedWrapper() is { } w) _ = LaunchOperation(w.Package, OperationType.Update, elevated: true);
    }

    private void CtxUninstallSudo_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetSelectedWrapper() is { } w) _ = LaunchOperation(w.Package, OperationType.Uninstall, elevated: true);
    }

    private void CtxDetails_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetSelectedWrapper() is { } w)
            OnPackageDetailsRequested?.Invoke(w.Package, _pageRole);
    }

    private void CtxInstallOptions_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetSelectedWrapper() is { } w)
            OnInstallOptionsRequested?.Invoke(w.Package, _pageRole);
    }

    private async void CtxCopyId_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetSelectedWrapper() is { } w)
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(w.Package.Id);
        }
    }

    private void PackageList_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (GetSelectedWrapper() is not { } w) return;
        OnPackageDetailsRequested?.Invoke(w.Package, _pageRole);
    }

    // ─── Select all (header checkbox) ──────────────────────────────────

    private void SelectAllHeader_Changed(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_suppressSelectAllEvent || _selectAllCheckBox is null) return;

        bool check = _selectAllCheckBox.IsChecked == true;
        foreach (var w in _filteredPackages)
            w.IsChecked = check;
    }

    /// <summary>
    /// Update the header checkbox state based on individual selections.
    /// Called after filter changes or when packages are modified.
    /// </summary>
    private void UpdateSelectAllState()
    {
        if (_selectAllCheckBox is null || _filteredPackages.Count == 0) return;

        _suppressSelectAllEvent = true;
        int checkedCount = _filteredPackages.Count(w => w.IsChecked);
        if (checkedCount == 0)
            _selectAllCheckBox.IsChecked = false;
        else if (checkedCount == _filteredPackages.Count)
            _selectAllCheckBox.IsChecked = true;
        else
            _selectAllCheckBox.IsChecked = null; // Indeterminate
        _suppressSelectAllEvent = false;
    }

    // ─── Batch action ───────────────────────────────────────────────────

    private void BatchAction_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var selected = _filteredPackages.Where(w => w.IsChecked).ToList();
        if (selected.Count == 0) return;

        foreach (var w in selected)
        {
            _ = LaunchOperation(w.Package, _pageRole);
        }
    }

    // ─── UI Events ──────────────────────────────────────────────────────

    private void SearchBox_KeyUp(object? sender, KeyEventArgs e)
    {
        _currentQuery = SearchBox.Text ?? "";
        UpdateFilter();
    }

    private void ManagerFilter_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _currentManagerFilter = ManagerFilter.SelectedItem as string ?? "";
        UpdateFilter();
    }

    private void ViewMode_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string mode)
            SetViewMode(mode == "Grid" ? ViewMode.Grid : ViewMode.List);
    }

    private void SortCombo_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (SortCombo.SelectedItem is ComboBoxItem item)
        {
            _sortProperty = item.Tag?.ToString() ?? "Name";
            UpdateFilter();
        }
    }

    private void SortDirection_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _sortAscending = !_sortAscending;
        SortDirectionIcon.Symbol = _sortAscending ? Symbol.Up : Symbol.ChevronDown;
        UpdateFilter();
    }

    private void ContextMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        var wrapper = GetSelectedWrapper();
        bool canRunAsAdmin = wrapper?.Package.Manager.Capabilities.CanRunAsAdmin ?? false;

        CtxSudoSeparator.IsVisible = canRunAsAdmin;
        CtxInstallSudo.IsVisible = canRunAsAdmin && (_pageRole is OperationType.Install or OperationType.None);
        CtxUpdateSudo.IsVisible = canRunAsAdmin && _pageRole is OperationType.Update;
        CtxUninstallSudo.IsVisible = canRunAsAdmin && (_pageRole is OperationType.Uninstall or OperationType.None);
    }

    private void ReloadButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_loader is null) return;
        _ = _loader.ReloadPackages();
    }
}

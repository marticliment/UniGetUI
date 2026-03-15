using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using UniGetUI.Avalonia.Infrastructure;
using UniGetUI.Avalonia.Models;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Telemetry;
using UniGetUI.PackageEngine.Classes.Packages.Classes;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageLoader;
using UniGetUI.PackageOperations;
using Avalonia.Controls.Primitives;

namespace UniGetUI.Avalonia.Views.Pages;

public partial class PackagePageView : UserControl, IShellPage
{
    private readonly string _defaultEmptyStateTitle;
    private readonly string _defaultEmptyStateDescription;
    private readonly bool _showNewVersionColumn;
    private readonly PackagePageMode _pageMode;
    private readonly ObservableCollection<PackageRowModel> _visibleRows = [];

    private enum SortColumn { Name, Id, Version, Status, Source }

    private readonly AbstractPackageLoader? _loader;
    private CancellationTokenSource? _discoverSearchCancellationSource;
    private string _searchQuery = string.Empty;
    private PackageRowModel? _selectedRow;
    private HashSet<string>? _sourceFilter; // null = all sources visible
    private AbstractOperation? _lastOperation;
    private SortColumn _sortColumn = SortColumn.Name;
    private bool _sortDescending;

    // Tracks whether the one-time backup-on-load has run for the Installed page
    private static bool _hasBackedUp;

    private TextBlock PageTitleText => GetControl<TextBlock>("PageTitleBlock");

    private TextBlock PageSubtitleText => GetControl<TextBlock>("PageSubtitleBlock");

    private Button PrimaryAction => GetControl<Button>("PrimaryActionButton");
    private Button PrimaryActionDropdown => GetControl<Button>("PrimaryActionDropdownButton");

    private Button InstallOptionsAction => GetControl<Button>("InstallOptionsButton");

    private Button DetailsAction => GetControl<Button>("DetailsButton");

    private Button ShareAction => GetControl<Button>("ShareButton");

    private TextBlock GlyphText => GetControl<TextBlock>("GlyphBlock");

    private TextBlock SourcesTitleText => GetControl<TextBlock>("SourcesTitleBlock");

    private TextBlock SourcesDescriptionText => GetControl<TextBlock>("SourcesDescriptionBlock");

    private TextBlock SourcesEmptyStateText => GetControl<TextBlock>("SourcesEmptyStateBlock");

    private Button ClearFilterBtn => GetControl<Button>("ClearSourceFilterButton");

    private ItemsControl SourceToggles => GetControl<ItemsControl>("SourceTogglesItemsControl");

    private TextBlock FiltersTitleText => GetControl<TextBlock>("FiltersTitleBlock");

    private RadioButton PackageNameFilterOption => GetControl<RadioButton>("PackageNameFilterRadio");

    private RadioButton PackageIdFilterOption => GetControl<RadioButton>("PackageIdFilterRadio");

    private RadioButton BothFilterOption => GetControl<RadioButton>("BothFilterRadio");

    private RadioButton ExactMatchFilterOption => GetControl<RadioButton>("ExactMatchFilterRadio");

    private RadioButton ShowSimilarResultsOption => GetControl<RadioButton>("ShowSimilarResultsRadio");

    private CheckBox InstantSearchOption => GetControl<CheckBox>("InstantSearchCheckBox");
    private CheckBox UpperLowerCaseOption => GetControl<CheckBox>("UpperLowerCaseCheckBox");
    private CheckBox IgnoreSpecialCharsOption => GetControl<CheckBox>("IgnoreSpecialCharsCheckBox");
    private Button ReloadBtn => GetControl<Button>("ReloadButton");

    private Button PackageNameSortButton => GetControl<Button>("PackageNameSortBtn");

    private Button PackageIdSortButton => GetControl<Button>("PackageIdSortBtn");

    private Button VersionSortButton => GetControl<Button>("VersionSortBtn");

    private Button StatusSortButton => GetControl<Button>("UpgradeColumnSortBtn");

    private Button SourceSortButton => GetControl<Button>("SourceSortBtn");

    private TextBlock ActionHeaderText => GetControl<TextBlock>("ActionHeaderBlock");

    private TextBlock EmptyStateTitleText => GetControl<TextBlock>("EmptyStateTitleBlock");

    private TextBlock EmptyStateDescriptionText => GetControl<TextBlock>("EmptyStateDescriptionBlock");

    private TextBlock SearchStateText => GetControl<TextBlock>("SearchStateBlock");

    private TextBlock OperationStateText => GetControl<TextBlock>("OperationStateBlock");

    private Button ViewOutputBtn => GetControl<Button>("ViewOutputButton");
    private Button IgnoreSelectedBtn => GetControl<Button>("IgnoreSelectedButton");
    private Button ExportSelectionBtn => GetControl<Button>("ExportSelectionButton");
    private Button ManageIgnoredBtn => GetControl<Button>("ManageIgnoredButton");

    private CheckBox SelectAllCheckBoxControl => GetControl<CheckBox>("SelectAllCheckBox");

    private ItemsControl PackageRowsItems => GetControl<ItemsControl>("PackageRowsItemsControl");

    private ScrollViewer PackageRowsScrollHost => GetControl<ScrollViewer>("PackageRowsScrollViewer");

    private Border EmptyStateHost => GetControl<Border>("EmptyStateCard");

    private ProgressBar LoadingProgressBarControl => GetControl<ProgressBar>("LoadingProgressBar");

    public PackagePageView()
        : this(
            title: string.Empty,
            subtitle: string.Empty,
            searchPlaceholder: string.Empty,
            primaryActionLabel: string.Empty,
            pageGlyph: "◎",
            emptyStateTitle: string.Empty,
            emptyStateDescription: string.Empty,
            showUpgradeColumn: false,
            filtersTitle: string.Empty,
            pageMode: PackagePageMode.None
        )
    {
    }

    public PackagePageView(
        string title,
        string subtitle,
        string searchPlaceholder,
        string primaryActionLabel,
        string pageGlyph,
        string emptyStateTitle,
        string emptyStateDescription,
        bool showUpgradeColumn,
        string filtersTitle,
        PackagePageMode pageMode
    )
    {
        Title = title;
        Subtitle = subtitle;
        SearchPlaceholder = searchPlaceholder;
        SupportsSearch = pageMode != PackagePageMode.None;
        _defaultEmptyStateTitle = emptyStateTitle;
        _defaultEmptyStateDescription = emptyStateDescription;
        _showNewVersionColumn = showUpgradeColumn;
        _pageMode = pageMode;

        InitializeComponent();
        ApplyStaticTranslations();
        PackageRowsItems.ItemsSource = _visibleRows;
        AttachFilterEvents();

        PageTitleText.Text = title;
        PageSubtitleText.Text = subtitle;
        PrimaryAction.Content = pageMode == PackagePageMode.Updates
            ? CoreTools.Translate("Update all")
            : primaryActionLabel;
        GlyphText.Text = pageGlyph;
        FiltersTitleText.Text = filtersTitle;
        EmptyStateTitleText.Text = emptyStateTitle;
        EmptyStateDescriptionText.Text = emptyStateDescription;
        ActionHeaderText.Text = CoreTools.Translate("Action");

        PrimaryAction.Click += PrimaryAction_OnClick;
        PrimaryActionDropdown.Click += PrimaryActionDropdown_OnClick;
        DetailsAction.Click += DetailsAction_OnClick;
        InstallOptionsAction.Click += InstallOptionsAction_OnClick;
        ShareAction.Click += ShareAction_OnClick;
        PackageRowsItems.AddHandler(
            InputElement.PointerPressedEvent,
            OnPackageRowsPointerPressed,
            RoutingStrategies.Bubble
        );

        if (pageMode == PackagePageMode.Updates)
            ManageIgnoredBtn.IsVisible = true;

        if (pageMode is PackagePageMode.Updates or PackagePageMode.Installed)
            IgnoreSelectedBtn.IsVisible = true;

        if (pageMode is not PackagePageMode.None)
            ExportSelectionBtn.IsVisible = true;

        _loader = ResolveLoader(pageMode);
        UpdateSortIndicators();

        if (_loader is not null)
        {
            AttachLoaderEvents(_loader);
            _ = LoadPackagesAsync();
        }
        else
        {
            SearchStateText.Text = CoreTools.Translate("Waiting for package engine");
            RefreshRows();
        }
    }

    private void ApplyStaticTranslations()
    {
        InstallOptionsAction.Content = CoreTools.Translate("Install options");
        DetailsAction.Content = CoreTools.Translate("Details");
        ShareAction.Content = CoreTools.Translate("Share");
        IgnoreSelectedBtn.Content = CoreTools.Translate("Ignore selected");
        ExportSelectionBtn.Content = CoreTools.Translate("Add to bundle");
        SourcesTitleText.Text = CoreTools.Translate("Sources");
        ClearFilterBtn.Content = CoreTools.Translate("Show all");
        SourcesDescriptionText.Text = CoreTools.Translate("Click a source to filter packages by source.");
        SourcesEmptyStateText.Text = CoreTools.Translate("No packages were found");
        PackageNameFilterOption.Content = CoreTools.Translate("Package Name");
        PackageIdFilterOption.Content = CoreTools.Translate("Package ID");
        BothFilterOption.Content = CoreTools.Translate("Both");
        ExactMatchFilterOption.Content = CoreTools.Translate("Exact match");
        ShowSimilarResultsOption.Content = CoreTools.Translate("Show all packages");
        InstantSearchOption.Content = CoreTools.Translate("Instant search");
        UpperLowerCaseOption.Content = CoreTools.Translate("Distinguish uppercase and lowercase");
        IgnoreSpecialCharsOption.Content = CoreTools.Translate("Ignore special characters");
        ReloadBtn.Content = CoreTools.Translate("Reload");
    }

    public string Title { get; }

    public string Subtitle { get; }

    public bool SupportsSearch { get; }

    public string SearchPlaceholder { get; }

    public void UpdateSearchQuery(string query)
    {
        _searchQuery = query.Trim();

        if (_pageMode == PackagePageMode.Discover)
        {
            // When instant search is disabled, only trigger on empty query (to reset the list)
            if (string.IsNullOrWhiteSpace(_searchQuery) || InstantSearchOption.IsChecked != false)
                ScheduleDiscoverSearch();
            return;
        }

        RefreshRows();
    }

    private static AbstractPackageLoader? ResolveLoader(PackagePageMode pageMode)
    {
        return pageMode switch
        {
            PackagePageMode.Discover => DiscoverablePackagesLoader.Instance,
            PackagePageMode.Updates => UpgradablePackagesLoader.Instance,
            PackagePageMode.Installed => InstalledPackagesLoader.Instance,
            _ => null,
        };
    }

    private void AttachLoaderEvents(AbstractPackageLoader loader)
    {
        loader.StartedLoading += OnLoaderStartedLoading;
        loader.PackagesChanged += OnLoaderPackagesChanged;
        loader.FinishedLoading += OnLoaderFinishedLoading;
    }

    private void AttachFilterEvents()
    {
        PackageNameFilterOption.IsCheckedChanged += FilterOption_OnChecked;
        PackageIdFilterOption.IsCheckedChanged += FilterOption_OnChecked;
        BothFilterOption.IsCheckedChanged += FilterOption_OnChecked;
        ExactMatchFilterOption.IsCheckedChanged += FilterOption_OnChecked;
        ShowSimilarResultsOption.IsCheckedChanged += FilterOption_OnChecked;
        InstantSearchOption.IsCheckedChanged += FilterOption_OnChecked;
        UpperLowerCaseOption.IsCheckedChanged += FilterOption_OnChecked;
        IgnoreSpecialCharsOption.IsCheckedChanged += FilterOption_OnChecked;
    }

    private void FilterOption_OnChecked(object? sender, RoutedEventArgs e)
    {
        RefreshRows();
    }

    private async void PrimaryAction_OnClick(object? sender, RoutedEventArgs e)
    {
        // Operate on checked rows, or fall back to all visible eligible packages when none are checked
        var checkedRows = _visibleRows.Where(r => r.IsChecked).ToArray();
        var packages = (checkedRows.Length > 0
                ? checkedRows.Select(r => r.Package)
                : GetPackageSnapshot())
            .Where(CanRunPrimaryAction)
            .OrderBy(package => package.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (packages.Length == 0)
        {
            return;
        }

        int queuedCount = 0;
        foreach (var package in packages)
        {
            if (await QueuePrimaryActionAsync(package, updateSummaryAfterQueue: false))
            {
                queuedCount++;
            }
        }

        if (queuedCount > 0)
        {
            OperationStateText.Text = _pageMode switch
            {
                PackagePageMode.Discover => CoreTools.Translate("{0} installs queued", queuedCount),
                PackagePageMode.Installed => CoreTools.Translate("{0} uninstalls queued", queuedCount),
                _ => CoreTools.Translate("{0} updates queued", queuedCount),
            };
            RefreshRows();
        }
    }

    private void PrimaryActionDropdown_OnClick(object? sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();

        string adminLabel = _pageMode switch
        {
            PackagePageMode.Discover  => CoreTools.Translate("Install as administrator"),
            PackagePageMode.Updates   => CoreTools.Translate("Update as administrator"),
            PackagePageMode.Installed => CoreTools.Translate("Uninstall as administrator"),
            _ => CoreTools.Translate("Run as administrator"),
        };
        var adminItem = new MenuItem { Header = adminLabel };
        adminItem.Click += async (_, _) => await QueueAllCheckedWithFlagsAsync(elevated: true);
        menu.Items.Add(adminItem);

        string interactiveLabel = _pageMode switch
        {
            PackagePageMode.Discover  => CoreTools.Translate("Interactive installation"),
            PackagePageMode.Updates   => CoreTools.Translate("Interactive update"),
            PackagePageMode.Installed => CoreTools.Translate("Interactive uninstall"),
            _ => CoreTools.Translate("Interactive installation"),
        };
        var interactiveItem = new MenuItem { Header = interactiveLabel };
        interactiveItem.Click += async (_, _) => await QueueAllCheckedWithFlagsAsync(interactive: true);
        menu.Items.Add(interactiveItem);

        if (_pageMode != PackagePageMode.Installed)
        {
            var skipHashItem = new MenuItem { Header = CoreTools.Translate("Skip integrity checks") };
            skipHashItem.Click += async (_, _) => await QueueAllCheckedWithFlagsAsync(skipHash: true);
            menu.Items.Add(skipHashItem);
        }

        if (sender is Control anchor)
        {
            menu.PlacementTarget = anchor;
            menu.Placement = PlacementMode.Bottom;
            menu.Open(anchor);
        }
    }

    private async Task QueueAllCheckedWithFlagsAsync(
        bool elevated = false,
        bool interactive = false,
        bool skipHash = false)
    {
        var checkedRows = _visibleRows.Where(r => r.IsChecked).ToArray();
        var packages = (checkedRows.Length > 0
                ? checkedRows.Select(r => r.Package)
                : GetPackageSnapshot())
            .Where(CanRunPrimaryAction)
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (packages.Length == 0) return;

        int queuedCount = 0;
        foreach (var package in packages)
        {
            var opts = await InstallOptionsFactory.LoadApplicableAsync(package);
            if (elevated) opts.RunAsAdministrator = true;
            if (interactive) opts.InteractiveInstallation = true;
            if (skipHash) opts.SkipHashCheck = true;
            AbstractOperation? op = _pageMode switch
            {
                PackagePageMode.Discover  => new InstallPackageOperation(package, opts),
                PackagePageMode.Updates   => new UpdatePackageOperation(package, opts),
                PackagePageMode.Installed => new UninstallPackageOperation(package, opts),
                _ => null,
            };
            if (op is null) continue;
            AttachOperationEvents(op, package);
            AvaloniaOperationRegistry.Add(op);
            _ = op.MainThread();
            queuedCount++;
        }

        if (queuedCount > 0)
        {
            OperationStateText.Text = _pageMode switch
            {
                PackagePageMode.Discover  => CoreTools.Translate("{0} installs queued", queuedCount),
                PackagePageMode.Installed => CoreTools.Translate("{0} uninstalls queued", queuedCount),
                _ => CoreTools.Translate("{0} updates queued", queuedCount),
            };
            RefreshRows();
        }
    }

    private void SelectAllCheckBox_OnClick(object? sender, RoutedEventArgs e)
    {
        // Cycle: indeterminate/false → check all; true → uncheck all
        bool checkAll = SelectAllCheckBoxControl.IsChecked == true;
        foreach (var row in _visibleRows)
        {
            row.IsChecked = checkAll;
        }
    }

    /// <summary>Keyboard-triggered select-all: check all when nothing or some are checked; uncheck all when all are checked.</summary>
    internal void TriggerSelectAll()
    {
        bool allChecked = _visibleRows.Count > 0 && _visibleRows.All(r => r.IsChecked);
        bool target = !allChecked;
        foreach (var row in _visibleRows)
            row.IsChecked = target;
        UpdateSelectAllState();
    }

    private void RowCheckBox_OnClick(object? sender, RoutedEventArgs e)
    {
        UpdateSelectAllState();
    }

    private void UpdateSelectAllState()
    {
        int checkedCount = _visibleRows.Count(r => r.IsChecked);
        SelectAllCheckBoxControl.IsChecked = checkedCount == 0
            ? false
            : checkedCount == _visibleRows.Count
                ? true
                : null; // indeterminate
    }

    private void OnLoaderStartedLoading(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            LoadingProgressBarControl.IsVisible = true;
            SearchStateText.Text = GetLoadingStateText();
            RefreshRows();
        });
    }

    private void OnLoaderPackagesChanged(object? sender, PackagesChangedEvent e)
    {
        Dispatcher.UIThread.Post(RefreshRows);
    }

    private void OnLoaderFinishedLoading(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            LoadingProgressBarControl.IsVisible = false;
            RefreshRows();
        });

        if (_pageMode == PackagePageMode.Installed && !_hasBackedUp)
        {
            _hasBackedUp = true;
            _ = Task.Run(TriggerInstalledPageBackupAsync);
        }
    }

    private static async Task TriggerInstalledPageBackupAsync()
    {
        try
        {
            if (!Settings.Get(Settings.K.EnablePackageBackup_LOCAL))
                return;

            var packages = InstalledPackagesLoader.Instance.Packages.ToArray();
            string backupContents = await BundlesPageView.CreateBundleStringAsync(packages);

            string dirName = Settings.GetValue(Settings.K.ChangeBackupOutputDirectory);
            if (string.IsNullOrEmpty(dirName))
                dirName = CoreData.UniGetUI_DefaultBackupDirectory;

            if (!Directory.Exists(dirName))
                Directory.CreateDirectory(dirName);

            string fileName = Settings.GetValue(Settings.K.ChangeBackupFileName);
            if (string.IsNullOrEmpty(fileName))
                fileName = CoreTools.Translate("{pcName} installed packages",
                    new Dictionary<string, object?> { { "pcName", Environment.MachineName } });

            if (Settings.Get(Settings.K.EnableBackupTimestamping))
                fileName += " " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");

            fileName += ".ubundle";

            string filePath = Path.Combine(dirName, fileName);
            await File.WriteAllTextAsync(filePath, backupContents);
            Logger.ImportantInfo("Backup saved to " + filePath);
        }
        catch (Exception ex)
        {
            Logger.Error("An error occurred while performing a LOCAL backup");
            Logger.Error(ex);
        }
    }

    private void ScheduleDiscoverSearch()
    {
        _discoverSearchCancellationSource?.Cancel();

        if (string.IsNullOrWhiteSpace(_searchQuery))
        {
            RefreshRows();
            return;
        }

        SearchStateText.Text = CoreTools.Translate("Searching for packages");
        var cancellationSource = new CancellationTokenSource();
        _discoverSearchCancellationSource = cancellationSource;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(350, cancellationSource.Token);
                if (cancellationSource.IsCancellationRequested)
                {
                    return;
                }

                await LoadPackagesAsync();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        });
    }

    private async Task LoadPackagesAsync()
    {
        try
        {
            if (_loader is null)
            {
                return;
            }

            switch (_pageMode)
            {
                case PackagePageMode.Discover:
                    if (!string.IsNullOrWhiteSpace(_searchQuery))
                    {
                        await DiscoverablePackagesLoader.Instance.ReloadPackages(_searchQuery);
                    }
                    break;
                case PackagePageMode.Updates:
                case PackagePageMode.Installed:
                    if (!_loader.IsLoading)
                    {
                        await _loader.ReloadPackages();
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            Dispatcher.UIThread.Post(() =>
            {
                SearchStateText.Text = CoreTools.Translate("Shell Error");
                EmptyStateTitleText.Text = CoreTools.Translate("The package list failed to load");
                EmptyStateDescriptionText.Text = ex.Message;
                PackageRowsScrollHost.IsVisible = false;
                EmptyStateHost.IsVisible = true;
            });
        }
    }

    private void ReloadButton_OnClick(object? sender, RoutedEventArgs e) => _ = LoadPackagesAsync();

    private void NameHeader_OnClick(object? sender, RoutedEventArgs e) => SetSort(SortColumn.Name);

    private void IdHeader_OnClick(object? sender, RoutedEventArgs e) => SetSort(SortColumn.Id);

    private void VersionHeader_OnClick(object? sender, RoutedEventArgs e) => SetSort(SortColumn.Version);

    private void StatusHeader_OnClick(object? sender, RoutedEventArgs e) => SetSort(SortColumn.Status);

    private void SourceHeader_OnClick(object? sender, RoutedEventArgs e) => SetSort(SortColumn.Source);

    private void SetSort(SortColumn col)
    {
        if (_sortColumn == col)
            _sortDescending = !_sortDescending;
        else
        {
            _sortColumn = col;
            _sortDescending = false;
        }

        RefreshRows();
    }

    private void UpdateSortIndicators()
    {
        string indicator = _sortDescending ? " ↓" : " ↑";
        string statusLabel = _showNewVersionColumn
            ? CoreTools.Translate("New version")
            : CoreTools.Translate("Manager");

        PackageNameSortButton.Content = CoreTools.Translate("Package Name")
            + (_sortColumn == SortColumn.Name ? indicator : "");
        PackageIdSortButton.Content = CoreTools.Translate("Package ID")
            + (_sortColumn == SortColumn.Id ? indicator : "");
        VersionSortButton.Content = CoreTools.Translate("Version")
            + (_sortColumn == SortColumn.Version ? indicator : "");
        StatusSortButton.Content = statusLabel
            + (_sortColumn == SortColumn.Status ? indicator : "");
        SourceSortButton.Content = CoreTools.Translate("Source")
            + (_sortColumn == SortColumn.Source ? indicator : "");

        PackageNameSortButton.Classes.Set("active", _sortColumn == SortColumn.Name);
        PackageIdSortButton.Classes.Set("active", _sortColumn == SortColumn.Id);
        VersionSortButton.Classes.Set("active", _sortColumn == SortColumn.Version);
        StatusSortButton.Classes.Set("active", _sortColumn == SortColumn.Status);
        SourceSortButton.Classes.Set("active", _sortColumn == SortColumn.Source);
    }

    private string GetSortKey(IPackage p) => _sortColumn switch
    {
        SortColumn.Name => p.Name,
        SortColumn.Id => p.Id,
        SortColumn.Version => p.VersionString,
        SortColumn.Status => _showNewVersionColumn ? p.NewVersionString : p.Manager.DisplayName,
        SortColumn.Source => p.Source.AsString_DisplayName,
        _ => p.Name,
    };

    private void RefreshRows()
    {
        var packagesSnapshot = GetPackageSnapshot();

        IEnumerable<IPackage> sorted = _sortDescending
            ? packagesSnapshot
                .OrderByDescending(GetSortKey, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(p => p.Name, StringComparer.OrdinalIgnoreCase)
            : packagesSnapshot
                .OrderBy(GetSortKey, StringComparer.OrdinalIgnoreCase)
                .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase);

        var filteredPackages = sorted.ToArray();

        ClearVisibleRows();
        _visibleRows.Clear();
        foreach (var package in filteredPackages)
        {
            _visibleRows.Add(
                new PackageRowModel(
                    package,
                    _showNewVersionColumn,
                    _pageMode,
                    QueuePrimaryActionAsync
                )
            );
        }

        PackageRowsScrollHost.IsVisible = _visibleRows.Count > 0;
        EmptyStateHost.IsVisible = _visibleRows.Count == 0;

        UpdateToolbarActions(filteredPackages);
        UpdateSourceSummary(filteredPackages);
        UpdateEmptyState(filteredPackages.Length);
        UpdateStatusSummary(packagesSnapshot.Count, filteredPackages.Length);
        UpdateSelectAllState();
        UpdateSortIndicators();
    }

    private async Task QueuePrimaryActionAsync(IPackage package)
    {
        await QueuePrimaryActionAsync(package, updateSummaryAfterQueue: true);
    }

    private async Task<bool> QueuePrimaryActionAsync(
        IPackage package,
        bool updateSummaryAfterQueue = true
    )
    {
        if (!CanRunPrimaryAction(package))
        {
            return false;
        }

        try
        {
            var operation = await CreatePrimaryActionOperationAsync(package);
            if (operation is null)
            {
                return false;
            }

            AttachOperationEvents(operation, package);
            AvaloniaOperationRegistry.Add(operation);
            _ = operation.MainThread();

            if (updateSummaryAfterQueue)
            {
                OperationStateText.Text = GetQueuedStateText(package);
                RefreshRows();
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            OperationStateText.Text = ex.Message;
            return false;
        }
    }

    private async Task<AbstractOperation?> CreatePrimaryActionOperationAsync(IPackage package)
    {
        var options = await InstallOptionsFactory.LoadApplicableAsync(package);
        return _pageMode switch
        {
            PackagePageMode.Discover => new InstallPackageOperation(package, options),
            PackagePageMode.Updates => new UpdatePackageOperation(package, options),
            PackagePageMode.Installed => new UninstallPackageOperation(package, options),
            _ => null,
        };
    }

    private async Task QueuePrimaryActionWithFlagsAsync(
        IPackage package,
        bool elevated = false,
        bool interactive = false,
        bool skipHash = false)
    {
        if (!CanRunPrimaryAction(package)) return;
        var opts = await InstallOptionsFactory.LoadApplicableAsync(package);
        if (elevated) opts.RunAsAdministrator = true;
        if (interactive) opts.InteractiveInstallation = true;
        if (skipHash) opts.SkipHashCheck = true;
        AbstractOperation? operation = _pageMode switch
        {
            PackagePageMode.Discover => new InstallPackageOperation(package, opts),
            PackagePageMode.Updates => new UpdatePackageOperation(package, opts),
            PackagePageMode.Installed => new UninstallPackageOperation(package, opts),
            _ => null,
        };
        if (operation is null) return;
        AttachOperationEvents(operation, package);
        AvaloniaOperationRegistry.Add(operation);
        _ = operation.MainThread();
        OperationStateText.Text = GetQueuedStateText(package);
        RefreshRows();
    }

    private async Task DownloadPackageInstallerAsync(IPackage package)
    {
        if (!package.Manager.Capabilities.CanDownloadInstaller) return;
        var topLevel = global::Avalonia.Controls.TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new global::Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = CoreTools.Translate("Download installer"),
            SuggestedFileName = package.Id,
            DefaultExtension = "exe",
            FileTypeChoices =
            [
                new global::Avalonia.Platform.Storage.FilePickerFileType("Executable") { Patterns = ["*.exe"] },
                new global::Avalonia.Platform.Storage.FilePickerFileType("MSI") { Patterns = ["*.msi"] },
                new global::Avalonia.Platform.Storage.FilePickerFileType("Compressed file") { Patterns = ["*.zip"] },
                new global::Avalonia.Platform.Storage.FilePickerFileType("MSIX") { Patterns = ["*.msix"] },
                new global::Avalonia.Platform.Storage.FilePickerFileType("NuGet package") { Patterns = ["*.nupkg"] },
            ],
        });
        if (file is null) return;

        var op = new DownloadOperation(package, file.Path.LocalPath);
        AvaloniaOperationRegistry.Add(op);
        _ = op.MainThread();
        OperationStateText.Text = CoreTools.Translate("Queued installer download for {0}", package.Name);
    }

    private void AttachOperationEvents(AbstractOperation operation, IPackage package)
    {
        _lastOperation = operation;
        Dispatcher.UIThread.Post(() => ViewOutputBtn.IsVisible = true);

        operation.Enqueued += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            OperationStateText.Text = GetQueuedStateText(package);
            RefreshRows();
        });

        operation.OperationStarting += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            OperationStateText.Text = GetRunningStateText(package);
            RefreshRows();
        });

        operation.OperationSucceeded += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            OperationStateText.Text = GetSuccessStateText(package);
            RefreshRows();
            if (operation is InstallPackageOperation)
                TelemetryHandler.InstallPackage(package, TEL_OP_RESULT.SUCCESS, TEL_InstallReferral.DIRECT_SEARCH);
            else if (operation is UpdatePackageOperation)
                TelemetryHandler.UpdatePackage(package, TEL_OP_RESULT.SUCCESS);
            else if (operation is UninstallPackageOperation)
                TelemetryHandler.UninstallPackage(package, TEL_OP_RESULT.SUCCESS);
            else if (operation is DownloadOperation)
                TelemetryHandler.DownloadPackage(package, TEL_OP_RESULT.SUCCESS, TEL_InstallReferral.DIRECT_SEARCH);
        });

        operation.OperationFailed += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            OperationStateText.Text = GetFailureStateText(package);
            RefreshRows();
            if (operation is InstallPackageOperation)
                TelemetryHandler.InstallPackage(package, TEL_OP_RESULT.FAILED, TEL_InstallReferral.DIRECT_SEARCH);
            else if (operation is UpdatePackageOperation)
                TelemetryHandler.UpdatePackage(package, TEL_OP_RESULT.FAILED);
            else if (operation is UninstallPackageOperation)
                TelemetryHandler.UninstallPackage(package, TEL_OP_RESULT.FAILED);
            else if (operation is DownloadOperation)
                TelemetryHandler.DownloadPackage(package, TEL_OP_RESULT.FAILED, TEL_InstallReferral.DIRECT_SEARCH);
        });
    }

    private async void ViewOutputButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_lastOperation is null) return;
        var window = new OperationLogWindow(_lastOperation);
        if (VisualRoot is Window parentWindow)
            await window.ShowDialog(parentWindow);
        else
            window.Show();
    }

    private async void ManageIgnoredButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var window = new IgnoredUpdatesWindow();
        if (VisualRoot is Window parentWindow)
            await window.ShowDialog(parentWindow);
        else
            window.Show();
    }

    private async void IgnoreSelectedButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var checkedRows = _visibleRows.Where(r => r.IsChecked).ToArray();
        var packages = (checkedRows.Length > 0
                ? checkedRows.Select(r => r.Package)
                : (IEnumerable<IPackage>)GetPackageSnapshot())
            .Where(p => !p.Source.IsVirtualManager)
            .ToArray();

        foreach (var package in packages)
            await package.AddToIgnoredUpdatesAsync();

        OperationStateText.Text = CoreTools.Translate("Updates for {0} packages will be ignored", packages.Length);
        RefreshRows();
    }

    private async void ExportSelectionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var checkedRows = _visibleRows.Where(r => r.IsChecked).ToArray();
        var packages = (checkedRows.Length > 0
                ? checkedRows.Select(r => r.Package)
                : (IEnumerable<IPackage>)GetPackageSnapshot())
            .ToArray();

        await PackageBundlesLoader.Instance.AddPackagesAsync(packages);

        // Navigate shell to Bundles page
        var shell = this.FindAncestorOfType<UniGetUI.Avalonia.Views.MainShellView>();
        shell?.OpenPage(ShellPageType.Bundles);
    }

    private void ClearVisibleRows()
    {
        foreach (var row in _visibleRows)
        {
            row.Dispose();
        }
    }

    private void UpdateToolbarActions(IReadOnlyList<IPackage> filteredPackages)
    {
        PrimaryAction.IsEnabled = _pageMode != PackagePageMode.None
            && filteredPackages.Any(CanRunPrimaryAction);
        PrimaryActionDropdown.IsEnabled = PrimaryAction.IsEnabled;
        bool rowSelected = _selectedRow is not null && filteredPackages.Contains(_selectedRow.Package);
        DetailsAction.IsEnabled = rowSelected;
        InstallOptionsAction.IsEnabled = rowSelected;
        ShareAction.IsEnabled = rowSelected;
    }

    private void OnPackageRowsPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Walk up the visual tree from the event source to find a control with a PackageRowModel DataContext
        var visual = e.Source as global::Avalonia.Visual;
        while (visual is not null)
        {
            if (visual is Control ctrl && ctrl.DataContext is PackageRowModel row)
            {
                OnRowSelected(row);
                return;
            }
            visual = visual.GetVisualParent();
        }
    }

    private void OnRowSelected(PackageRowModel row)
    {
        if (_selectedRow == row)
        {
            return;
        }

        if (_selectedRow is not null) _selectedRow.IsSelected = false;

        _selectedRow = row;
        _selectedRow.IsSelected = true;

        var filteredPackages = GetPackageSnapshot();
        bool rowVisible = filteredPackages.Contains(row.Package);
        DetailsAction.IsEnabled = rowVisible;
        InstallOptionsAction.IsEnabled = rowVisible;
        ShareAction.IsEnabled = rowVisible;
    }

    private async void DetailsAction_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedRow is null)
        {
            return;
        }

        var window = new PackageDetailsWindow(_selectedRow.Package, _pageMode);
        if (VisualRoot is Window parentWindow)
        {
            await window.ShowDialog(parentWindow);
        }
        else
        {
            window.Show();
        }
    }

    private async void InstallOptionsAction_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedRow is null)
        {
            return;
        }

        var window = new InstallOptionsWindow(_selectedRow.Package, _pageMode);
        if (VisualRoot is Window parentWindow)
        {
            await window.ShowDialog(parentWindow);
        }
        else
        {
            window.Show();
        }
    }

    private async void ShareAction_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedRow is null)
        {
            return;
        }

        await DoShareAsync(_selectedRow.Package);
    }

    private async Task DoShareAsync(IPackage package)
    {
        if (package.Source.IsVirtualManager)
        {
            OperationStateText.Text = CoreTools.Translate("This package cannot be shared");
            return;
        }

        var url = "https://marticliment.com/unigetui/share?"
            + "name=" + Uri.EscapeDataString(package.Name)
            + "&id=" + Uri.EscapeDataString(package.Id)
            + "&sourceName=" + Uri.EscapeDataString(package.Source.Name)
            + "&managerName=" + Uri.EscapeDataString(package.Manager.DisplayName);

        var clipboard = global::Avalonia.Controls.TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(url);
            OperationStateText.Text = CoreTools.Translate("Share link copied to clipboard");
        }
    }

    private async void PackageRow_ContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (sender is not Control ctrl || ctrl.DataContext is not PackageRowModel row)
        {
            return;
        }

        e.Handled = true;
        OnRowSelected(row);
        var menu = await BuildContextMenuAsync(row);
        ctrl.ContextMenu = menu;
        menu.Open(ctrl);
    }

    private async Task<ContextMenu> BuildContextMenuAsync(PackageRowModel row)
    {
        var menu = new ContextMenu();
        var package = row.Package;

        // Primary action
        if (CanRunPrimaryAction(package))
        {
            string primaryLabel = _pageMode switch
            {
                PackagePageMode.Discover => CoreTools.Translate("Install"),
                PackagePageMode.Updates => CoreTools.Translate("Update"),
                PackagePageMode.Installed => CoreTools.Translate("Uninstall"),
                _ => CoreTools.Translate("Open"),
            };
            var primaryItem = new MenuItem { Header = primaryLabel };
            primaryItem.Click += async (_, _) => await QueuePrimaryActionAsync(package);
            menu.Items.Add(primaryItem);

            // Run as administrator
            if (package.Manager.Capabilities.CanRunAsAdmin)
            {
                string adminLabel = _pageMode switch
                {
                    PackagePageMode.Discover => CoreTools.Translate("Install as administrator"),
                    PackagePageMode.Updates => CoreTools.Translate("Update as administrator"),
                    PackagePageMode.Installed => CoreTools.Translate("Uninstall as administrator"),
                    _ => CoreTools.Translate("Run as administrator"),
                };
                var adminItem = new MenuItem { Header = adminLabel };
                adminItem.Click += async (_, _) => await QueuePrimaryActionWithFlagsAsync(package, elevated: true);
                menu.Items.Add(adminItem);
            }

            // Interactive
            if (package.Manager.Capabilities.CanRunInteractively)
            {
                string interactiveLabel = _pageMode switch
                {
                    PackagePageMode.Discover => CoreTools.Translate("Interactive installation"),
                    PackagePageMode.Updates => CoreTools.Translate("Interactive update"),
                    PackagePageMode.Installed => CoreTools.Translate("Interactive uninstall"),
                    _ => CoreTools.Translate("Interactive installation"),
                };
                var interactiveItem = new MenuItem { Header = interactiveLabel };
                interactiveItem.Click += async (_, _) => await QueuePrimaryActionWithFlagsAsync(package, interactive: true);
                menu.Items.Add(interactiveItem);
            }

            // Uninstall and remove data (Installed page only)
            if (package.Manager.Capabilities.CanRemoveDataOnUninstall && _pageMode == PackagePageMode.Installed)
            {
                var removeDataItem = new MenuItem { Header = CoreTools.Translate("Uninstall and remove data") };
                removeDataItem.Click += async (_, _) =>
                {
                    var opts = await InstallOptionsFactory.LoadApplicableAsync(package);
                    opts.RemoveDataOnUninstall = true;
                    var op = new UninstallPackageOperation(package, opts);
                    AttachOperationEvents(op, package);
                    AvaloniaOperationRegistry.Add(op);
                    _ = op.MainThread();
                    OperationStateText.Text = CoreTools.Translate("Queued uninstall for {0}", package.Name);
                    RefreshRows();
                };
                menu.Items.Add(removeDataItem);
            }

            // Skip integrity checks
            if (package.Manager.Capabilities.CanSkipIntegrityChecks && _pageMode != PackagePageMode.Installed)
            {
                var skipHashItem = new MenuItem { Header = CoreTools.Translate("Skip integrity checks") };
                skipHashItem.Click += async (_, _) => await QueuePrimaryActionWithFlagsAsync(package, skipHash: true);
                menu.Items.Add(skipHashItem);
            }

            // Download installer
            if (package.Manager.Capabilities.CanDownloadInstaller)
            {
                var downloadItem = new MenuItem { Header = CoreTools.Translate("Download installer") };
                downloadItem.Click += async (_, _) => await DownloadPackageInstallerAsync(package);
                menu.Items.Add(downloadItem);
            }

            menu.Items.Add(new Separator());
        }

        // Open install location (Updates + Installed pages)
        if (_pageMode is PackagePageMode.Updates or PackagePageMode.Installed)
        {
            string? installPath = null;
            try { installPath = package.Manager.DetailsHelper.GetInstallLocation(package); }
            catch (Exception ex) { Logger.Warn($"GetInstallLocation failed: {ex.Message}"); }

            var openLocationItem = new MenuItem
            {
                Header = CoreTools.Translate("Open install location"),
                IsEnabled = installPath is not null,
            };
            openLocationItem.Click += (_, _) => CoreTools.Launch(installPath);
            menu.Items.Add(openLocationItem);
            menu.Items.Add(new Separator());
        }

        // Details
        var detailsItem = new MenuItem { Header = CoreTools.Translate("Details") };
        detailsItem.Click += async (_, _) =>
        {
            var window = new PackageDetailsWindow(package, _pageMode);
            if (VisualRoot is Window w) await window.ShowDialog(w);
            else window.Show();
        };
        menu.Items.Add(detailsItem);

        // Install options
        var optionsItem = new MenuItem { Header = CoreTools.Translate("Install options") };
        optionsItem.Click += async (_, _) =>
        {
            var window = new InstallOptionsWindow(package, _pageMode);
            if (VisualRoot is Window w) await window.ShowDialog(w);
            else window.Show();
        };
        menu.Items.Add(optionsItem);

        // Share
        if (!package.Source.IsVirtualManager)
        {
            var shareItem = new MenuItem { Header = CoreTools.Translate("Share") };
            shareItem.Click += async (_, _) => await DoShareAsync(package);
            menu.Items.Add(shareItem);
        }

        // Ignore updates (Updates and Installed pages)
        if (_pageMode is PackagePageMode.Updates or PackagePageMode.Installed)
        {
            menu.Items.Add(new Separator());

            bool alreadyIgnored = _pageMode == PackagePageMode.Installed
                && await package.HasUpdatesIgnoredAsync();

            var ignoreItem = new MenuItem
            {
                Header = alreadyIgnored
                    ? CoreTools.Translate("Do not ignore updates for this package anymore")
                    : CoreTools.Translate("Ignore updates")
            };
            ignoreItem.Click += async (_, _) =>
            {
                if (alreadyIgnored)
                {
                    await package.RemoveFromIgnoredUpdatesAsync();
                    OperationStateText.Text = CoreTools.Translate(
                        "Updates for {0} will no longer be ignored", package.Name);
                }
                else
                {
                    await package.AddToIgnoredUpdatesAsync();
                    OperationStateText.Text = CoreTools.Translate(
                        "Updates for {0} will be ignored", package.Name);
                }
                RefreshRows();
            };
            menu.Items.Add(ignoreItem);

            // Skip this version (Updates page only)
            if (_pageMode == PackagePageMode.Updates)
            {
                var skipItem = new MenuItem { Header = CoreTools.Translate("Skip this version") };
                skipItem.Click += async (_, _) =>
                {
                    await package.AddToIgnoredUpdatesAsync(package.NewVersionString);
                    OperationStateText.Text = CoreTools.Translate(
                        "Version {0} of {1} will be skipped", package.NewVersionString, package.Name);
                    RefreshRows();
                };
                menu.Items.Add(skipItem);

                // Pause updates for… submenu
                var pauseParent = new MenuItem { Header = CoreTools.Translate("Pause updates for") };
                foreach (var pauseTime in new[]
                {
                    new IgnoredUpdatesDatabase.PauseTime { Days = 1 },
                    new IgnoredUpdatesDatabase.PauseTime { Days = 3 },
                    new IgnoredUpdatesDatabase.PauseTime { Weeks = 1 },
                    new IgnoredUpdatesDatabase.PauseTime { Weeks = 2 },
                    new IgnoredUpdatesDatabase.PauseTime { Weeks = 4 },
                    new IgnoredUpdatesDatabase.PauseTime { Months = 3 },
                    new IgnoredUpdatesDatabase.PauseTime { Months = 6 },
                    new IgnoredUpdatesDatabase.PauseTime { Months = 12 },
                })
                {
                    var pt = pauseTime; // capture
                    var pauseItem = new MenuItem { Header = pt.StringRepresentation() };
                    pauseItem.Click += async (_, _) =>
                    {
                        await package.AddToIgnoredUpdatesAsync("<" + pt.GetDateFromNow());
                        UpgradablePackagesLoader.Instance.IgnoredPackages[package.Id] = package;
                        UpgradablePackagesLoader.Instance.Remove(package);
                        OperationStateText.Text = CoreTools.Translate(
                            "Updates for {0} are paused until {1}", package.Name, pt.GetDateFromNow());
                        RefreshRows();
                    };
                    pauseParent.Items.Add(pauseItem);
                }
                menu.Items.Add(pauseParent);

                // Updates-page uninstall actions
                menu.Items.Add(new Separator());

                var uninstallItem = new MenuItem { Header = CoreTools.Translate("Uninstall package") };
                uninstallItem.Click += async (_, _) =>
                {
                    var opts = await InstallOptionsFactory.LoadApplicableAsync(package);
                    var op = new UninstallPackageOperation(package, opts);
                    AttachOperationEvents(op, package);
                    AvaloniaOperationRegistry.Add(op);
                    _ = op.MainThread();
                    OperationStateText.Text = CoreTools.Translate("Queued uninstall for {0}", package.Name);
                    RefreshRows();
                };
                menu.Items.Add(uninstallItem);

                var uninstallThenUpdateItem = new MenuItem
                    { Header = CoreTools.Translate("Uninstall package, then update it") };
                uninstallThenUpdateItem.Click += async (_, _) =>
                {
                    var opts = await InstallOptionsFactory.LoadApplicableAsync(package);
                    var uninstallOp = new UninstallPackageOperation(package, opts.Copy());
                    opts.Version = package.NewVersionString;
                    opts.OverridesNextLevelOpts = true;
                    var installOp = new InstallPackageOperation(package, opts, req: uninstallOp);
                    AttachOperationEvents(uninstallOp, package);
                    AvaloniaOperationRegistry.Add(uninstallOp);
                    AvaloniaOperationRegistry.Add(installOp);
                    _ = uninstallOp.MainThread();
                    OperationStateText.Text = CoreTools.Translate(
                        "Queued reinstall/update for {0}", package.Name);
                    RefreshRows();
                };
                menu.Items.Add(uninstallThenUpdateItem);
            }

            // Installed-page-specific actions
            if (_pageMode == PackagePageMode.Installed && !package.Source.IsVirtualManager)
            {
                menu.Items.Add(new Separator());

                var reinstallItem = new MenuItem { Header = CoreTools.Translate("Reinstall package") };
                reinstallItem.Click += async (_, _) =>
                {
                    var opts = await InstallOptionsFactory.LoadApplicableAsync(package);
                    var op = new InstallPackageOperation(package, opts);
                    AttachOperationEvents(op, package);
                    AvaloniaOperationRegistry.Add(op);
                    _ = op.MainThread();
                    OperationStateText.Text = CoreTools.Translate("Queued reinstall for {0}", package.Name);
                    RefreshRows();
                };
                menu.Items.Add(reinstallItem);

                var uninstallReinstallItem = new MenuItem
                    { Header = CoreTools.Translate("Uninstall package, then reinstall it") };
                uninstallReinstallItem.Click += async (_, _) =>
                {
                    var opts = await InstallOptionsFactory.LoadApplicableAsync(package);
                    var uninstallOp = new UninstallPackageOperation(package, opts.Copy());
                    var installOp = new InstallPackageOperation(package, opts, req: uninstallOp);
                    AttachOperationEvents(uninstallOp, package);
                    AvaloniaOperationRegistry.Add(uninstallOp);
                    AvaloniaOperationRegistry.Add(installOp);
                    _ = uninstallOp.MainThread();
                    OperationStateText.Text = CoreTools.Translate("Queued reinstall for {0}", package.Name);
                    RefreshRows();
                };
                menu.Items.Add(uninstallReinstallItem);
            }
        }

        return menu;
    }

    private bool CanRunPrimaryAction(IPackage package)
    {
        return _pageMode switch
        {
            PackagePageMode.Discover => package.Tag is not PackageTag.AlreadyInstalled
                and not PackageTag.OnQueue
                and not PackageTag.BeingProcessed
                and not PackageTag.Unavailable,
            PackagePageMode.Updates => package.Tag is not PackageTag.OnQueue
                and not PackageTag.BeingProcessed
                and not PackageTag.Unavailable,
            PackagePageMode.Installed => package.Tag is not PackageTag.OnQueue
                and not PackageTag.BeingProcessed
                and not PackageTag.Unavailable,
            _ => false,
        };
    }

    private string GetQueuedStateText(IPackage package)
    {
        return _pageMode switch
        {
            PackagePageMode.Discover => CoreTools.Translate("Queued installation for {0}", package.Name),
            PackagePageMode.Updates => CoreTools.Translate("Queued update for {0}", package.Name),
            PackagePageMode.Installed => CoreTools.Translate("Queued uninstall for {0}", package.Name),
            _ => CoreTools.Translate("Queued operation for {0}", package.Name),
        };
    }

    private string GetRunningStateText(IPackage package)
    {
        return _pageMode switch
        {
            PackagePageMode.Discover => CoreTools.Translate("Installing {0}", package.Name),
            PackagePageMode.Updates => CoreTools.Translate("Updating {0}", package.Name),
            PackagePageMode.Installed => CoreTools.Translate("Uninstalling {0}", package.Name),
            _ => CoreTools.Translate("Running operation for {0}", package.Name),
        };
    }

    private string GetSuccessStateText(IPackage package)
    {
        return _pageMode switch
        {
            PackagePageMode.Discover => CoreTools.Translate("Installed {0}", package.Name),
            PackagePageMode.Updates => CoreTools.Translate("Updated {0}", package.Name),
            PackagePageMode.Installed => CoreTools.Translate("Uninstalled {0}", package.Name),
            _ => CoreTools.Translate("Completed operation for {0}", package.Name),
        };
    }

    private string GetFailureStateText(IPackage package)
    {
        return _pageMode switch
        {
            PackagePageMode.Discover => CoreTools.Translate("Installation failed for {0}", package.Name),
            PackagePageMode.Updates => CoreTools.Translate("Update failed for {0}", package.Name),
            PackagePageMode.Installed => CoreTools.Translate("Uninstall failed for {0}", package.Name),
            _ => CoreTools.Translate("Operation failed for {0}", package.Name),
        };
    }

    private IReadOnlyList<IPackage> GetPackageSnapshot()
    {
        if (_loader is null)
        {
            return [];
        }

        var packages = _loader.Packages.ToArray();
        if (_pageMode == PackagePageMode.Discover && string.IsNullOrWhiteSpace(_searchQuery))
        {
            return [];
        }

        IEnumerable<IPackage> filtered = packages;

        if (!string.IsNullOrWhiteSpace(_searchQuery) && ShowSimilarResultsOption.IsChecked != true)
        {
            filtered = filtered.Where(package => MatchesSearch(package, _searchQuery));
        }

        if (_sourceFilter is not null)
        {
            filtered = filtered.Where(p => _sourceFilter.Contains(p.Source.AsString_DisplayName));
        }

        return filtered.ToArray();
    }

    private bool MatchesSearch(IPackage package, string query)
    {
        bool caseSensitive = UpperLowerCaseOption.IsChecked == true;
        bool ignoreSpecial = IgnoreSpecialCharsOption.IsChecked == true;

        string Treat(string s)
        {
            if (!caseSensitive) s = s.ToLowerInvariant();
            if (ignoreSpecial) s = NormalizeSpecialChars(s);
            return s;
        }

        string treatedQuery = Treat(query);

        if (ExactMatchFilterOption.IsChecked == true)
        {
            return Treat(package.Name) == treatedQuery || Treat(package.Id) == treatedQuery;
        }

        if (PackageNameFilterOption.IsChecked == true)
        {
            return Treat(package.Name).Contains(treatedQuery);
        }

        if (PackageIdFilterOption.IsChecked == true)
        {
            return Treat(package.Id).Contains(treatedQuery);
        }

        return Treat(package.Name).Contains(treatedQuery) || Treat(package.Id).Contains(treatedQuery);
    }

    private static string NormalizeSpecialChars(string s) =>
        s
            .Replace("-", "").Replace("_", "").Replace(" ", "")
            .Replace("@", "").Replace("\t", "").Replace(".", "")
            .Replace(",", "").Replace(":", "")
            .Replace("à", "a").Replace("á", "a").Replace("ä", "a").Replace("â", "a")
            .Replace("è", "e").Replace("é", "e").Replace("ë", "e").Replace("ê", "e")
            .Replace("ì", "i").Replace("í", "i").Replace("ï", "i").Replace("î", "i")
            .Replace("ò", "o").Replace("ó", "o").Replace("ö", "o").Replace("ô", "o")
            .Replace("ù", "u").Replace("ú", "u").Replace("ü", "u").Replace("û", "u")
            .Replace("ý", "y").Replace("ÿ", "y").Replace("ç", "c").Replace("ñ", "n");

    private void UpdateSourceSummary(IReadOnlyList<IPackage> _)
    {
        // Compute all sources from the unfiltered loader snapshot (not just visible)
        IPackage[] allLoaderPackages = _loader?.Packages.ToArray() ?? [];
        if (_pageMode == PackagePageMode.Discover && string.IsNullOrWhiteSpace(_searchQuery))
        {
            allLoaderPackages = [];
        }

        var allSources = allLoaderPackages
            .Select(p => p.Source.AsString_DisplayName)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        SourcesEmptyStateText.IsVisible = allSources.Length == 0;
        SourcesDescriptionText.IsVisible = allSources.Length > 0;

        if (allSources.Length == 0)
        {
            SourcesEmptyStateText.Text = _pageMode == PackagePageMode.Discover && string.IsNullOrWhiteSpace(_searchQuery)
                ? CoreTools.Translate("Type a package name to query available managers")
                : CoreTools.Translate("No sources available");
            SourceToggles.ItemsSource = null;
            ClearFilterBtn.IsVisible = false;
            return;
        }

        // Build toggle buttons - one per source
        var buttons = allSources.Select(source =>
        {
            bool isActive = _sourceFilter is null || _sourceFilter.Contains(source);
            var btn = new Button
            {
                Content = source,
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch,
                Tag = source,
            };
            btn.Classes.Add(isActive ? "toolbar-primary" : "toolbar-secondary");
            btn.Click += SourceToggleButton_OnClick;
            return btn;
        }).ToArray();

        SourceToggles.ItemsSource = buttons;
        ClearFilterBtn.IsVisible = _sourceFilter is not null;
    }

    private void SourceToggleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string source)
        {
            return;
        }

        if (_sourceFilter is null)
        {
            // First exclusion: start a filter set with all sources except the clicked one
            var allLoaderPackages = _loader?.Packages.ToArray() ?? [];
            _sourceFilter = allLoaderPackages
                .Select(p => p.Source.AsString_DisplayName)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            _sourceFilter.Remove(source);
        }
        else if (_sourceFilter.Contains(source))
        {
            _sourceFilter.Remove(source);
            if (_sourceFilter.Count == 0)
            {
                _sourceFilter = null; // back to "show all"
            }
        }
        else
        {
            _sourceFilter.Add(source);
        }

        RefreshRows();
    }

    private void ClearSourceFilterButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _sourceFilter = null;
        RefreshRows();
    }

    private void UpdateEmptyState(int visiblePackageCount)
    {
        if (visiblePackageCount > 0)
        {
            EmptyStateTitleText.Text = _defaultEmptyStateTitle;
            EmptyStateDescriptionText.Text = _defaultEmptyStateDescription;
            return;
        }

        if (_pageMode == PackagePageMode.Discover && string.IsNullOrWhiteSpace(_searchQuery))
        {
            EmptyStateTitleText.Text = CoreTools.Translate("Search for packages");
            EmptyStateDescriptionText.Text = CoreTools.Translate("Enter a package name or package ID to query the active package managers.");
            return;
        }

        if (_loader?.IsLoading == true)
        {
            EmptyStateTitleText.Text = GetLoadingStateText();
            EmptyStateDescriptionText.Text = CoreTools.Translate("Results will appear here as soon as the package managers finish responding.");
            return;
        }

        EmptyStateTitleText.Text = CoreTools.Translate("No results were found matching the input criteria");
        EmptyStateDescriptionText.Text = CoreTools.Translate("Adjust the query or filters to broaden the results shown on this page.");
    }

    private void UpdateStatusSummary(int totalPackageCount, int visiblePackageCount)
    {
        if (_loader?.IsLoading == true)
        {
            SearchStateText.Text = visiblePackageCount > 0
                ? CoreTools.Translate("{0} packages found so far", visiblePackageCount)
                : GetLoadingStateText();
            return;
        }

        if (_pageMode == PackagePageMode.Discover && string.IsNullOrWhiteSpace(_searchQuery))
        {
            SearchStateText.Text = CoreTools.Translate("Type to search package managers");
            return;
        }

        if (_loader is null)
        {
            SearchStateText.Text = CoreTools.Translate("Waiting for package engine");
            return;
        }

        if (totalPackageCount == visiblePackageCount)
        {
            SearchStateText.Text = CoreTools.Translate("{0} packages found", visiblePackageCount);
            return;
        }

        SearchStateText.Text = CoreTools.Translate(
            "{0} packages were found, {1} of which match the specified filters.",
            totalPackageCount,
            visiblePackageCount
        );
    }

    private string GetLoadingStateText()
    {
        return _pageMode switch
        {
            PackagePageMode.Discover => CoreTools.Translate("Searching for packages"),
            PackagePageMode.Updates => CoreTools.Translate("Checking for updates"),
            PackagePageMode.Installed => CoreTools.Translate("Loading installed packages"),
            _ => CoreTools.Translate("Waiting for package engine"),
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private T GetControl<T>(string name)
        where T : Control
    {
        return this.FindControl<T>(name)
            ?? throw new InvalidOperationException($"Control '{name}' was not found.");
    }
}

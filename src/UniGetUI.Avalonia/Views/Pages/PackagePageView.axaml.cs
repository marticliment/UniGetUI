using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using UniGetUI.Avalonia.Models;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageLoader;
using UniGetUI.PackageOperations;

namespace UniGetUI.Avalonia.Views.Pages;

public partial class PackagePageView : UserControl, IShellPage
{
    private readonly string _defaultEmptyStateTitle;
    private readonly string _defaultEmptyStateDescription;
    private readonly bool _showNewVersionColumn;
    private readonly PackagePageMode _pageMode;
    private readonly ObservableCollection<PackageRowModel> _visibleRows = [];

    private readonly AbstractPackageLoader? _loader;
    private CancellationTokenSource? _discoverSearchCancellationSource;
    private string _searchQuery = string.Empty;

    private TextBlock PageTitleText => GetControl<TextBlock>("PageTitleBlock");

    private TextBlock PageSubtitleText => GetControl<TextBlock>("PageSubtitleBlock");

    private Button PrimaryAction => GetControl<Button>("PrimaryActionButton");

    private Button InstallOptionsAction => GetControl<Button>("InstallOptionsButton");

    private Button DetailsAction => GetControl<Button>("DetailsButton");

    private Button ShareAction => GetControl<Button>("ShareButton");

    private TextBlock GlyphText => GetControl<TextBlock>("GlyphBlock");

    private TextBlock SourcesTitleText => GetControl<TextBlock>("SourcesTitleBlock");

    private TextBlock SourcesDescriptionText => GetControl<TextBlock>("SourcesDescriptionBlock");

    private TextBlock SourcesEmptyStateText => GetControl<TextBlock>("SourcesEmptyStateBlock");

    private TextBlock FiltersSectionTitleText => GetControl<TextBlock>("FiltersSectionTitleBlock");

    private TextBlock FiltersSectionDescriptionText => GetControl<TextBlock>("FiltersSectionDescriptionBlock");

    private TextBlock UpgradeColumnText => GetControl<TextBlock>("UpgradeColumnBlock");

    private TextBlock FiltersTitleText => GetControl<TextBlock>("FiltersTitleBlock");

    private RadioButton PackageNameFilterOption => GetControl<RadioButton>("PackageNameFilterRadio");

    private RadioButton PackageIdFilterOption => GetControl<RadioButton>("PackageIdFilterRadio");

    private RadioButton BothFilterOption => GetControl<RadioButton>("BothFilterRadio");

    private RadioButton ExactMatchFilterOption => GetControl<RadioButton>("ExactMatchFilterRadio");

    private TextBlock PackageNameHeaderText => GetControl<TextBlock>("PackageNameHeaderBlock");

    private TextBlock PackageIdHeaderText => GetControl<TextBlock>("PackageIdHeaderBlock");

    private TextBlock VersionHeaderText => GetControl<TextBlock>("VersionHeaderBlock");

    private TextBlock SourceHeaderText => GetControl<TextBlock>("SourceHeaderBlock");

    private TextBlock ActionHeaderText => GetControl<TextBlock>("ActionHeaderBlock");

    private TextBlock EmptyStateTitleText => GetControl<TextBlock>("EmptyStateTitleBlock");

    private TextBlock EmptyStateDescriptionText => GetControl<TextBlock>("EmptyStateDescriptionBlock");

    private TextBlock SearchStateText => GetControl<TextBlock>("SearchStateBlock");

    private TextBlock OperationStateText => GetControl<TextBlock>("OperationStateBlock");

    private ItemsControl PackageRowsItems => GetControl<ItemsControl>("PackageRowsItemsControl");

    private ScrollViewer PackageRowsScrollHost => GetControl<ScrollViewer>("PackageRowsScrollViewer");

    private Border EmptyStateHost => GetControl<Border>("EmptyStateCard");

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

        _loader = ResolveLoader(pageMode);
        UpgradeColumnText.Text = showUpgradeColumn
            ? CoreTools.Translate("New version")
            : CoreTools.Translate("Manager");

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
        SourcesTitleText.Text = CoreTools.Translate("Sources");
        SourcesDescriptionText.Text = CoreTools.Translate("Cross-platform source selection is not wired yet.");
        SourcesEmptyStateText.Text = CoreTools.Translate("No packages were found");
        FiltersSectionTitleText.Text = CoreTools.Translate("Filters");
        FiltersSectionDescriptionText.Text = CoreTools.Translate("Search filters are applied to the package list shown on this page.");
        PackageNameFilterOption.Content = CoreTools.Translate("Package Name");
        PackageIdFilterOption.Content = CoreTools.Translate("Package ID");
        BothFilterOption.Content = CoreTools.Translate("Both");
        ExactMatchFilterOption.Content = CoreTools.Translate("Exact match");
        PackageNameHeaderText.Text = CoreTools.Translate("Package Name");
        PackageIdHeaderText.Text = CoreTools.Translate("Package ID");
        VersionHeaderText.Text = CoreTools.Translate("Version");
        SourceHeaderText.Text = CoreTools.Translate("Source");
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
    }

    private void FilterOption_OnChecked(object? sender, RoutedEventArgs e)
    {
        RefreshRows();
    }

    private async void PrimaryAction_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_pageMode != PackagePageMode.Updates)
        {
            return;
        }

        var packages = GetPackageSnapshot()
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
            OperationStateText.Text = CoreTools.Translate("{0} updates queued", queuedCount);
            RefreshRows();
        }
    }

    private void OnLoaderStartedLoading(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
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
        Dispatcher.UIThread.Post(RefreshRows);
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

    private void RefreshRows()
    {
        var packagesSnapshot = GetPackageSnapshot();

        var filteredPackages = packagesSnapshot
            .OrderBy(package => package.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(package => package.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

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

    private void AttachOperationEvents(AbstractOperation operation, IPackage package)
    {
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
        });

        operation.OperationFailed += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            OperationStateText.Text = GetFailureStateText(package);
            RefreshRows();
        });
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
        PrimaryAction.IsEnabled = _pageMode == PackagePageMode.Updates
            && filteredPackages.Any(CanRunPrimaryAction);
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

        if (string.IsNullOrWhiteSpace(_searchQuery))
        {
            return packages;
        }

        return packages.Where(package => MatchesSearch(package, _searchQuery)).ToArray();
    }

    private bool MatchesSearch(IPackage package, string query)
    {
        if (ExactMatchFilterOption.IsChecked == true)
        {
            return package.Name.Equals(query, StringComparison.OrdinalIgnoreCase)
                || package.Id.Equals(query, StringComparison.OrdinalIgnoreCase);
        }

        if (PackageNameFilterOption.IsChecked == true)
        {
            return package.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
        }

        if (PackageIdFilterOption.IsChecked == true)
        {
            return package.Id.Contains(query, StringComparison.OrdinalIgnoreCase);
        }

        return package.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            || package.Id.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateSourceSummary(IReadOnlyList<IPackage> packages)
    {
        if (packages.Count == 0)
        {
            SourcesEmptyStateText.Text = _pageMode == PackagePageMode.Discover && string.IsNullOrWhiteSpace(_searchQuery)
                ? CoreTools.Translate("Type a package name to query available managers")
                : CoreTools.Translate("No packages were found");
            return;
        }

        var sources = packages
            .Select(package => package.Source.AsString_DisplayName)
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToArray();

        SourcesEmptyStateText.Text = string.Join(Environment.NewLine, sources);
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
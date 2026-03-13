using global::Avalonia.Controls;
using global::Avalonia.Input;
using global::Avalonia.Interactivity;
using global::Avalonia.Markup.Xaml;
using global::Avalonia.VisualTree;
using UniGetUI.Avalonia.Models;
using UniGetUI.Avalonia.Views.Pages;
using UniGetUI.Core.Data;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;

namespace UniGetUI.Avalonia.Views;

public partial class MainShellView : UserControl
{
    private readonly Dictionary<ShellPageType, IShellPage> _pageCache = [];
    private readonly Dictionary<ShellPageType, (Button Button, TextBlock Label)> _navButtons = [];
    private readonly List<ShellPageType> _history = [];

    private ShellPageType _currentPage;
    private bool _navigationExpanded = !Settings.Get(Settings.K.CollapseNavMenuOnWideScreen);

    private Border Sidebar => GetControl<Border>("SidebarBorder");

    private TextBlock ShellSubtitleText => GetControl<TextBlock>("ShellSubtitleBlock");

    private Button DiscoverNavButton => GetControl<Button>("DiscoverButton");

    private TextBlock DiscoverNavLabel => GetControl<TextBlock>("DiscoverLabel");

    private Button UpdatesNavButton => GetControl<Button>("UpdatesButton");

    private TextBlock UpdatesNavLabel => GetControl<TextBlock>("UpdatesLabel");

    private Button InstalledNavButton => GetControl<Button>("InstalledButton");

    private TextBlock InstalledNavLabel => GetControl<TextBlock>("InstalledLabel");

    private Button BundlesNavButton => GetControl<Button>("BundlesButton");

    private TextBlock BundlesNavLabel => GetControl<TextBlock>("BundlesLabel");

    private Button SettingsNavButton => GetControl<Button>("SettingsButton");

    private TextBlock SettingsNavLabel => GetControl<TextBlock>("SettingsLabel");

    private Button ManagersNavButton => GetControl<Button>("ManagersButton");

    private TextBlock ManagersNavLabel => GetControl<TextBlock>("ManagersLabel");

    private Button HelpNavButton => GetControl<Button>("HelpButton");

    private TextBlock HelpNavLabel => GetControl<TextBlock>("HelpLabel");

    private Button BackNavigationButton => GetControl<Button>("BackButton");

    private TextBlock CurrentPageTitleText => GetControl<TextBlock>("CurrentPageTitleBlock");

    private TextBlock CurrentPageSubtitleText => GetControl<TextBlock>("CurrentPageSubtitleBlock");

    private Button ToggleMaximizeWindowControl => GetControl<Button>("ToggleMaximizeWindowButton");

    private TextBox GlobalSearchHost => GetControl<TextBox>("GlobalSearchBox");

    private TextBox GlobalSearchTextBox => GetControl<TextBox>("GlobalSearchBox");

    private ContentControl PageContentHost => GetControl<ContentControl>("PageHost");

    public MainShellView()
    {
        InitializeComponent();

        ApplyLocalizedShellText();
        ShellSubtitleText.Text = BuildShellSubtitle();
        RegisterNavigationButtons();
        ApplyNavigationWidth();
        NavigateTo(GetDefaultPage(), false);
        AttachedToVisualTree += (_, _) => UpdateWindowButtons();
    }

    private void ApplyLocalizedShellText()
    {
        DiscoverNavLabel.Text = CoreTools.Translate("Discover Packages");
        UpdatesNavLabel.Text = CoreTools.Translate("Software Updates");
        InstalledNavLabel.Text = CoreTools.Translate("Installed Packages");
        BundlesNavLabel.Text = CoreTools.Translate("Package Bundles");
        SettingsNavLabel.Text = CoreTools.Translate("Settings");
        ManagersNavLabel.Text = CoreTools.Translate("Package Managers");
        HelpNavLabel.Text = CoreTools.Translate("Help");
    }

    private void RegisterNavigationButtons()
    {
        _navButtons[ShellPageType.Discover] = (DiscoverNavButton, DiscoverNavLabel);
        _navButtons[ShellPageType.Updates] = (UpdatesNavButton, UpdatesNavLabel);
        _navButtons[ShellPageType.Installed] = (InstalledNavButton, InstalledNavLabel);
        _navButtons[ShellPageType.Bundles] = (BundlesNavButton, BundlesNavLabel);
        _navButtons[ShellPageType.Settings] = (SettingsNavButton, SettingsNavLabel);
        _navButtons[ShellPageType.Managers] = (ManagersNavButton, ManagersNavLabel);
        _navButtons[ShellPageType.Help] = (HelpNavButton, HelpNavLabel);
    }

    private static string BuildShellSubtitle()
    {
        var labels = new List<string>();

        if (!string.IsNullOrWhiteSpace(CoreData.VersionName))
        {
            labels.Add(CoreTools.Translate("version {0}", CoreData.VersionName));
        }

#if DEBUG
        labels.Add(CoreTools.Translate("DEBUG BUILD"));
#endif

    labels.Add(CoreTools.Translate("Avalonia preview"));

        return string.Join("  •  ", labels);
    }

    private static ShellPageType GetDefaultPage()
    {
        return Settings.GetValue(Settings.K.StartupPage) switch
        {
            "updates" => ShellPageType.Updates,
            "installed" => ShellPageType.Installed,
            "bundles" => ShellPageType.Bundles,
            "settings" => ShellPageType.Settings,
            _ => ShellPageType.Discover,
        };
    }

    private void NavigateTo(ShellPageType pageType, bool toHistory = true)
    {
        if (_currentPage == pageType && PageContentHost.Content is not null)
        {
            return;
        }

        if (toHistory && PageContentHost.Content is not null)
        {
            _history.Add(_currentPage);
        }

        var page = GetPage(pageType);
        _currentPage = pageType;
        PageContentHost.Content = (Control)page;
        CurrentPageTitleText.Text = page.Title;
        CurrentPageSubtitleText.Text = page.Subtitle;
        GlobalSearchHost.IsVisible = page.SupportsSearch;
        GlobalSearchTextBox.IsEnabled = page.SupportsSearch;
        GlobalSearchTextBox.Watermark = page.SearchPlaceholder;

        if (!page.SupportsSearch)
        {
            GlobalSearchTextBox.Text = string.Empty;
        }

        page.UpdateSearchQuery(GlobalSearchTextBox.Text ?? string.Empty);

        BackNavigationButton.IsVisible = _history.Count > 0;
        UpdateNavigationSelection();
    }

    private IShellPage GetPage(ShellPageType pageType)
    {
        if (_pageCache.TryGetValue(pageType, out var existingPage))
        {
            return existingPage;
        }

        IShellPage page = pageType switch
        {
            ShellPageType.Discover => new PackagePageView(
                title: CoreTools.Translate("Discover Packages"),
                subtitle: CoreTools.Translate("Search across active package managers"),
                searchPlaceholder: CoreTools.Translate("Search for packages"),
                primaryActionLabel: CoreTools.Translate("Install selection"),
                pageGlyph: "◎",
                emptyStateTitle: CoreTools.Translate("Search for packages"),
                emptyStateDescription: CoreTools.Translate("Enter a package name or package ID to query the active package managers."),
                showUpgradeColumn: false,
                filtersTitle: CoreTools.Translate("Search mode"),
                pageMode: PackagePageMode.Discover
            ),
            ShellPageType.Updates => new PackagePageView(
                title: CoreTools.Translate("Software Updates"),
                subtitle: CoreTools.Translate("Cross-platform update surface"),
                searchPlaceholder: CoreTools.Translate("Search for updates"),
                primaryActionLabel: CoreTools.Translate("Update selection"),
                pageGlyph: "↻",
                emptyStateTitle: CoreTools.Translate("No packages were found"),
                emptyStateDescription: CoreTools.Translate("No updates are currently reported by the active package managers."),
                showUpgradeColumn: true,
                filtersTitle: CoreTools.Translate("Search mode"),
                pageMode: PackagePageMode.Updates
            ),
            ShellPageType.Installed => new PackagePageView(
                title: CoreTools.Translate("Installed Packages"),
                subtitle: CoreTools.Translate("Cross-platform installed package surface"),
                searchPlaceholder: CoreTools.Translate("Search installed packages"),
                primaryActionLabel: CoreTools.Translate("Uninstall selection"),
                pageGlyph: "▣",
                emptyStateTitle: CoreTools.Translate("No packages were found"),
                emptyStateDescription: CoreTools.Translate("No installed packages are currently reported by the active package managers."),
                showUpgradeColumn: false,
                filtersTitle: CoreTools.Translate("Search mode"),
                pageMode: PackagePageMode.Installed
            ),
            ShellPageType.Bundles => new SimplePageView(
                title: CoreTools.Translate("Package Bundles"),
                subtitle: CoreTools.Translate("Bundle editor scaffold"),
                lead: CoreTools.Translate("Bundle editing is reserved for a later implementation step."),
                description: CoreTools.Translate("The package data loaders now drive Discover, Updates, and Installed. Package Bundles remain a separate workflow that still needs its own editor surface.")
            ),
            ShellPageType.Settings => new SimplePageView(
                CoreTools.Translate("Settings"),
                CoreTools.Translate("Avalonia settings shell"),
                CoreTools.Translate("The Windows settings experience has not been ported yet."),
                CoreTools.Translate("This placeholder keeps the shell layout and navigation structure in place while the settings pages are split into reusable Avalonia sections.")
            ),
            ShellPageType.Managers => new SimplePageView(
                CoreTools.Translate("Package Managers"),
                CoreTools.Translate("Manager configuration shell"),
                CoreTools.Translate("Package manager configuration is not ported yet."),
                CoreTools.Translate("The next layer here is a source and capability matrix built from the existing package-engine metadata.")
            ),
            ShellPageType.Help => new SimplePageView(
                CoreTools.Translate("Help"),
                CoreTools.Translate("Help and diagnostics shell"),
                CoreTools.Translate("Help content is pending porting."),
                CoreTools.Translate("The main goal of this first Avalonia slice is shell parity, layout parity, and a reusable package-page scaffold.")
            ),
            _ => throw new InvalidOperationException($"Unsupported page type {pageType}"),
        };

        _pageCache[pageType] = page;
        return page;
    }

    private void UpdateNavigationSelection()
    {
        foreach (var (pageType, nav) in _navButtons)
        {
            nav.Button.Classes.Set("selected", pageType == _currentPage);
        }
    }

    private void ApplyNavigationWidth()
    {
        Sidebar.Width = _navigationExpanded ? 232 : 72;

        foreach (var nav in _navButtons.Values)
        {
            nav.Label.IsVisible = _navigationExpanded;
        }

        ShellSubtitleText.IsVisible = _navigationExpanded;
    }

    private void ToggleNavigationButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _navigationExpanded = !_navigationExpanded;
        ApplyNavigationWidth();
    }

    private void BackButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_history.Count == 0)
        {
            return;
        }

        var target = _history[^1];
        _history.RemoveAt(_history.Count - 1);
        NavigateTo(target, false);
    }

    private void GlobalSearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_pageCache.TryGetValue(_currentPage, out var page) && page.SupportsSearch)
        {
            page.UpdateSearchQuery(GlobalSearchTextBox.Text ?? string.Empty);
        }
    }

    private void TitleRegion_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || IsInteractiveSource(e.Source))
        {
            return;
        }

        if (GetHostWindow() is { } window)
        {
            window.BeginMoveDrag(e);
        }
    }

    private void TitleRegion_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (IsInteractiveSource(e.Source) || GetHostWindow() is not { CanResize: true } window)
        {
            return;
        }

        ToggleWindowState(window);
    }

    private void MinimizeWindowButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (GetHostWindow() is { } window)
        {
            window.WindowState = WindowState.Minimized;
            UpdateWindowButtons();
        }
    }

    private void ToggleMaximizeWindowButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (GetHostWindow() is { } window)
        {
            ToggleWindowState(window);
        }
    }

    private void CloseWindowButton_OnClick(object? sender, RoutedEventArgs e)
    {
        GetHostWindow()?.Close();
    }

    private void ToggleWindowState(Window window)
    {
        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

        UpdateWindowButtons();
    }

    private void UpdateWindowButtons()
    {
        if (GetHostWindow() is not { } window)
        {
            return;
        }

        ToggleMaximizeWindowControl.Content = window.WindowState == WindowState.Maximized ? "❐" : "□";
    }

    private Window? GetHostWindow()
    {
        return TopLevel.GetTopLevel(this) as Window;
    }

    private static bool IsInteractiveSource(object? source)
    {
        if (source is not global::Avalonia.Visual visual)
        {
            return false;
        }

        return visual.GetSelfAndVisualAncestors().Any(control => control is Button || control is TextBox);
    }

    private void DiscoverButton_OnClick(object? sender, RoutedEventArgs e) => NavigateTo(ShellPageType.Discover);

    private void UpdatesButton_OnClick(object? sender, RoutedEventArgs e) => NavigateTo(ShellPageType.Updates);

    private void InstalledButton_OnClick(object? sender, RoutedEventArgs e) => NavigateTo(ShellPageType.Installed);

    private void BundlesButton_OnClick(object? sender, RoutedEventArgs e) => NavigateTo(ShellPageType.Bundles);

    private void SettingsButton_OnClick(object? sender, RoutedEventArgs e) => NavigateTo(ShellPageType.Settings);

    private void ManagersButton_OnClick(object? sender, RoutedEventArgs e) => NavigateTo(ShellPageType.Managers);

    private void HelpButton_OnClick(object? sender, RoutedEventArgs e) => NavigateTo(ShellPageType.Help);

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
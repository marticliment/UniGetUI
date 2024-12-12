using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.PackageLoader;
using Windows.System;
using Windows.UI.Core;
using UniGetUI.Interface.Pages;
using UniGetUI.Pages.DialogPages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface
{
    public abstract partial class AbstractPackagesPage : IKeyboardShortcutListener, IEnterLeaveListener
    {

        protected struct PackagesPageData
        {
            public bool DisableAutomaticPackageLoadOnStart;
            public bool MegaQueryBlockEnabled;
            public bool PackagesAreCheckedByDefault;
            public bool ShowLastLoadTime;
            public bool DisableSuggestedResultsRadio;
            public bool DisableFilterOnQueryChange;

            public OperationType PageRole;
            public AbstractPackageLoader Loader;

            public string PageName;
            public string PageTitle;
            public string Glyph;

            public string NoPackages_BackgroundText;
            public string NoPackages_SourcesText;
            public string NoPackages_SubtitleText_Base;
            public string MainSubtitle_StillLoading;
            public string NoMatches_BackgroundText;
        }

        protected enum ReloadReason
        {
            FirstRun,
            Automated,
            Manual,
            External
        }

        protected readonly bool DISABLE_AUTOMATIC_PACKAGE_LOAD_ON_START;
        protected readonly bool MEGA_QUERY_BOX_ENABLED;
        protected readonly bool SHOW_LAST_CHECKED_TIME;
        protected readonly bool DISABLE_FILTER_ON_QUERY_CHANGE;
        protected readonly string PAGE_NAME;
        public readonly bool RoleIsUpdateLike;
        protected DateTime LastPackageLoadTime { get; private set; }
        protected readonly OperationType PAGE_ROLE;

        protected IPackage? SelectedItem
        {
            get => (PackageList.SelectedItem as PackageWrapper)?.Package;
        }

        protected AbstractPackageLoader Loader;
        public ObservablePackageCollection FilteredPackages = [];
        protected List<IPackageManager> UsedManagers = [];
        protected ConcurrentDictionary<IPackageManager, List<IManagerSource>> UsedSourcesForManager = [];
        protected ConcurrentDictionary<IPackageManager, TreeViewNode> RootNodeForManager = [];
        protected ConcurrentDictionary<IManagerSource, TreeViewNode> NodesForSources = [];
        private readonly TreeViewNode LocalPackagesNode;
        public InfoBadge? ExternalCountBadge;

        public readonly int NewVersionLabelWidth;
        public readonly int NewVersionIconWidth;

        protected abstract void WhenPackagesLoaded(ReloadReason reason);
        protected abstract void WhenPackageCountUpdated();
        protected abstract void WhenShowingContextMenu(IPackage package);
        public abstract void GenerateToolBar();
        public abstract BetterMenu GenerateContextMenu();

        protected readonly string NoPackages_BackgroundText;
        protected readonly string NoPackages_SourcesText;
        protected readonly string MainSubtitle_StillLoading;
        protected readonly string NoPackages_SubtitleText_Base;
        protected readonly string NoMatches_BackgroundText;

        private bool PaneIsAnimated = false;

        protected Func<int, int, string> FoundPackages_SubtitleText_Base = (a, b) => CoreTools.Translate("{0} packages were found, {1} of which match the specified filters.", a, b);

        protected string NoPackages_SubtitleText
        {
            get => NoPackages_SubtitleText_Base +
                (SHOW_LAST_CHECKED_TIME ? " " + CoreTools.Translate("(Last checked: {0})", LastPackageLoadTime.ToString()) : "");
        }
        protected string NoMatches_SubtitleText
        {
            get => FoundPackages_SubtitleText_Base(Loader.Count(), FilteredPackages.Count) +
               (SHOW_LAST_CHECKED_TIME ? " " + CoreTools.Translate("(Last checked: {0})", LastPackageLoadTime.ToString()) : "");
        }
        protected string FoundPackages_SubtitleText { get => NoMatches_SubtitleText; }

        private string TypeQuery = "";
        private int LastKeyDown;
        private readonly int QUERY_SEPARATION_TIME = 1000; // 500ms between keypresses starts a new query

        protected AbstractPackagesPage(PackagesPageData data)
        {
            InitializeComponent();

            Loader = data.Loader;

            DISABLE_AUTOMATIC_PACKAGE_LOAD_ON_START = data.DisableAutomaticPackageLoadOnStart;
            DISABLE_FILTER_ON_QUERY_CHANGE = data.DisableFilterOnQueryChange;
            MEGA_QUERY_BOX_ENABLED = data.MegaQueryBlockEnabled;
            SHOW_LAST_CHECKED_TIME = data.ShowLastLoadTime;

            PAGE_ROLE = data.PageRole;
            RoleIsUpdateLike = PAGE_ROLE == OperationType.Update;
            NewVersionLabelWidth = RoleIsUpdateLike ? 125 : 0;
            NewVersionIconWidth = RoleIsUpdateLike ? 24 : 0;

            Loader = data.Loader;

            PAGE_NAME = data.PageName;

            MainTitle.Text = data.PageTitle;
            HeaderIcon.Glyph = data.Glyph;

            NoPackages_BackgroundText = data.NoPackages_BackgroundText;
            NoPackages_SourcesText = data.NoPackages_SourcesText;
            NoPackages_SubtitleText_Base = data.NoPackages_SubtitleText_Base;
            MainSubtitle_StillLoading = data.MainSubtitle_StillLoading;

            NoMatches_BackgroundText = data.NoMatches_BackgroundText;

            SelectAllCheckBox.IsChecked = data.PackagesAreCheckedByDefault;
            QuerySimilarResultsRadio.IsEnabled = !data.DisableSuggestedResultsRadio;
            QueryOptionsGroup.SelectedIndex = 1;
            QueryOptionsGroup.SelectedIndex = 2;
            QueryOptionsGroup.SelectedItem = QueryBothRadio;

            Loader.StartedLoading += Loader_StartedLoading;
            Loader.FinishedLoading += Loader_FinishedLoading;
            Loader.PackagesChanged += Loader_PackagesChanged;

            if (Loader.IsLoading)
            {
                Loader_StartedLoading(this, EventArgs.Empty);
            }
            else
            {
                Loader_FinishedLoading(this, EventArgs.Empty);
                FilterPackages();
            }

            LastPackageLoadTime = DateTime.Now;
            LocalPackagesNode = new TreeViewNode
            {
                Content = CoreTools.Translate("Local"),
                IsExpanded = false
            };

            ReloadButton.Click += async (_, _) => await LoadPackages();

            // Handle Find Button click on the Query Block
            FindButton.Click += (_, _) =>
            {
                MegaQueryBlockGrid.Visibility = Visibility.Collapsed;
                FilterPackages();
            };

            // Handle Enter pressed on the QueryBlock
            QueryBlock.KeyUp += (_, e) =>
            {
                if (e.Key != VirtualKey.Enter)
                {
                    return;
                }

                MegaQueryBlockGrid.Visibility = Visibility.Collapsed;
                if(!DISABLE_FILTER_ON_QUERY_CHANGE)
                    FilterPackages();
            };

            // Handle showing the MegaQueryBlock
            QueryBlock.TextChanged += (_, _) =>
            {
                if (InstantSearchCheckbox.IsChecked == true)
                {
                    if (!DISABLE_FILTER_ON_QUERY_CHANGE)
                        FilterPackages();
                }

                if (MEGA_QUERY_BOX_ENABLED && QueryBlock.Text.Trim() == "")
                {
                    MegaQueryBlockGrid.Visibility = Visibility.Visible;
                    Loader.StopLoading();
                    BackgroundText.Visibility = Visibility.Collapsed;
                    ClearPackageList();
                    UpdatePackageCount();
                    MegaQueryBlock.Focus(FocusState.Programmatic);
                    MegaQueryBlock.Text = "";
                }
            };

            // Handle the Enter Pressed event on the MegaQueryBlock
            MegaQueryBlock.KeyUp += (_, e) =>
            {
                if (e.Key != VirtualKey.Enter)
                {
                    return;
                }

                MegaQueryBlockGrid.Visibility = Visibility.Collapsed;
                QueryBlock.Text = MegaQueryBlock.Text.Trim();
                if(!DISABLE_FILTER_ON_QUERY_CHANGE)
                    FilterPackages();
            };

            // Hande the MegaQueryBlock search button click
            MegaFindButton.Click += (_, _) =>
            {
                MegaQueryBlockGrid.Visibility = Visibility.Collapsed;
                QueryBlock.Text = MegaQueryBlock.Text.Trim();
                FilterPackages();
            };

            // Handle when a source is clicked
            SourcesTreeView.Tapped += (_, e) =>
            {
                TreeViewNode? node = (e.OriginalSource as FrameworkElement)?.DataContext as TreeViewNode;
                if (node is null)
                {
                    return;
                }

                if (SourcesTreeView.SelectedNodes.Contains(node))
                {
                    SourcesTreeView.SelectedNodes.Remove(node);
                }
                else
                {
                    SourcesTreeView.SelectedNodes.Add(node);
                }

                FilterPackages();
            };

            // Handle when a source is double-clicked
            SourcesTreeView.RightTapped += (_, e) =>
            {
                TreeViewNode? node = (e.OriginalSource as FrameworkElement)?.DataContext as TreeViewNode;
                if (node is null)
                {
                    return;
                }

                SourcesTreeView.SelectedNodes.Clear();
                SourcesTreeView.SelectedNodes.Add(node);
                FilterPackages();
            };

            if (MEGA_QUERY_BOX_ENABLED)
            {
                MegaQueryBlockGrid.Visibility = Visibility.Visible;
                MegaQueryBlock.Focus(FocusState.Programmatic);
                BackgroundText.Visibility = Visibility.Collapsed;
            }

            if (Settings.GetDictionaryItem<string, bool>("HideToggleFilters", PAGE_NAME))
            {
                HideFilteringPane(skipAnimation: true);
            }
            else
            {
                ShowFilteringPane(skipAnimation: true);
            }

            QueryBlock.PlaceholderText = CoreTools.Translate("Search for packages");
            MegaQueryBlock.PlaceholderText = CoreTools.Translate("Search for packages");
            InstantSearchCheckbox.IsChecked = !Settings.GetDictionaryItem<string, bool>("DisableInstantSearch", PAGE_NAME);

            HeaderIcon.FontWeight = new Windows.UI.Text.FontWeight(700);
            NameHeader.Content = CoreTools.Translate("Package Name");
            IdHeader.Content = CoreTools.Translate("Package ID");
            VersionHeader.Content = CoreTools.Translate("Version");
            NewVersionHeader.Content = CoreTools.Translate("New version");
            SourceHeader.Content = CoreTools.Translate("Source");

            NameHeader.Click += (_, _) => SortPackagesBy(ObservablePackageCollection.Sorter.Name);
            IdHeader.Click += (_, _) => SortPackagesBy(ObservablePackageCollection.Sorter.Id);
            VersionHeader.Click += (_, _) => SortPackagesBy(ObservablePackageCollection.Sorter.Version);
            NewVersionHeader.Click += (_, _) => SortPackagesBy(ObservablePackageCollection.Sorter.NewVersion);
            SourceHeader.Click += (_, _) => SortPackagesBy(ObservablePackageCollection.Sorter.Source);

            GenerateToolBar();
            PackageList.ContextFlyout = GenerateContextMenu();
        }

        private void Loader_PackagesChanged(object? sender, EventArgs e)
        {
            // Ensure we are in the UI thread
            if (Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() is null)
            {
                DispatcherQueue.TryEnqueue(() => Loader_PackagesChanged(sender, e));
                return;
            }


            if (Loader.Count() == 0)
            {
                ClearPackageList();
            }
            else
            {
                foreach (IPackage package in Loader.Packages)
                {
                    AddPackageToSourcesList(package);
                }
            }
            FilterPackages();
            if (!Settings.Get("DisableIconsOnPackageLists"))
                _ = LoadIconsForNewPackages();
        }

        private void Loader_FinishedLoading(object? sender, EventArgs e)
        {
            // Ensure we are in the UI thread
            if (Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() is null)
            {
                DispatcherQueue.TryEnqueue(() => Loader_FinishedLoading(sender, e));
                return;
            }

            LoadingProgressBar.Visibility = Visibility.Collapsed;
            LastPackageLoadTime = DateTime.Now;
            WhenPackagesLoaded(ReloadReason.External);
            FilterPackages();
        }

        private void Loader_StartedLoading(object? sender, EventArgs e)
        {
            // Ensure we are in the UI thread
            if (Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() is null)
            {
                DispatcherQueue.TryEnqueue(() => Loader_StartedLoading(sender, e));
                return;
            }
            LoadingProgressBar.Visibility = Visibility.Visible;
            UpdatePackageCount();
        }

        public void SearchTriggered()
        {
            QueryBlock.Focus(FocusState.Pointer);
        }

        public void ReloadTriggered()
        {
            _ = LoadPackages(ReloadReason.Manual);
        }

        public void SelectAllTriggered()
        {
            if (QueryBlock.FocusState == FocusState.Unfocused)
            {
                if (!SelectAllCheckBox.IsChecked ?? false)
                {
                    SelectAllCheckBox.IsChecked = true;
                    FilteredPackages.SelectAll();
                }
                else
                {
                    SelectAllCheckBox.IsChecked = false;
                    FilteredPackages.ClearSelection();
                }
            }
        }

        protected void AddPackageToSourcesList(IPackage package)
        {
            IManagerSource source = package.Source;
            if (!UsedManagers.Contains(source.Manager))
            {
                UsedManagers.Add(source.Manager);
                var node = new TreeViewNode { Content = source.Manager.DisplayName + "                                                                                    .", IsExpanded = false };
                SourcesTreeView.RootNodes.Add(node);

                // Smart way to decide whether to check a source or not.
                // - Always check a source by default if no sources are present
                // - Otherwise, Check a source only if half of the sources have already been checked
                if (SourcesTreeView.RootNodes.Count == 0)
                {
                    SourcesTreeView.SelectedNodes.Add(node);
                }
                else if (SourcesTreeView.SelectedNodes.Count >= SourcesTreeView.RootNodes.Count / 2)
                {
                    SourcesTreeView.SelectedNodes.Add(node);
                }

                RootNodeForManager.TryAdd(source.Manager, node);
                UsedSourcesForManager.TryAdd(source.Manager, []);
                SourcesPlaceholderText.Visibility = Visibility.Collapsed;
                SourcesTreeViewGrid.Visibility = Visibility.Visible;
            }

            if ((!UsedSourcesForManager.ContainsKey(source.Manager) || !UsedSourcesForManager[source.Manager].Contains(source)) && source.Manager.Capabilities.SupportsCustomSources)
            {
                UsedSourcesForManager[source.Manager].Add(source);
                TreeViewNode item = new() { Content = source.Name + "                                                                                    ." };
                NodesForSources.TryAdd(source, item);

                if (source.IsVirtualManager)
                {
                    LocalPackagesNode.Children.Add(item);
                    if (!SourcesTreeView.RootNodes.Contains(LocalPackagesNode))
                    {
                        SourcesTreeView.RootNodes.Add(LocalPackagesNode);
                        SourcesTreeView.SelectedNodes.Add(LocalPackagesNode);
                    }
                }
                else
                {
                    RootNodeForManager[source.Manager].Children.Add(item);
                }
            }
        }

        private void FilterOptionsChanged(object sender, RoutedEventArgs e)
        {
            if (QueryBothRadio is null)
            {
                return;
            }

            FilterPackages();
        }

        private void InstantSearchValueChanged(object sender, RoutedEventArgs e)
        { Settings.SetDictionaryItem("DisableInstantSearch", PAGE_NAME, !InstantSearchCheckbox.IsChecked); }
        private void SourcesTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        { FilterPackages(); }

        public virtual async Task LoadPackages()
        { await LoadPackages(ReloadReason.External); }

        protected void ClearPackageList()
        {
            FilteredPackages.Clear();
            UsedManagers.Clear();
            SourcesTreeView.RootNodes.Clear();
            UsedSourcesForManager.Clear();
            RootNodeForManager.Clear();
            NodesForSources.Clear();
            LocalPackagesNode.Children.Clear();
        }

        /// <summary>
        /// Reload the packages for this Page
        /// Calling this method will trigger a reload on the associated PackageLoader, unless it is already loading packages.
        /// </summary>
        protected async Task LoadPackages(ReloadReason reason)
        {
            if (!Loader.IsLoading && (!Loader.IsLoaded || reason == ReloadReason.External || reason == ReloadReason.Manual || reason == ReloadReason.Automated))
            {
                Loader.ClearPackages(emitFinishSignal: false);
                await Loader.ReloadPackages();
            }
            Loader_PackagesChanged(this, EventArgs.Empty);
        }

        private void SelectAndScrollTo(int index)
        {
            PackageList.Select(index);
            PackageList.ScrollView?.ScrollTo(0, Math.Max(0, (index - 3) * 39), new ScrollingScrollOptions
            (
                ScrollingAnimationMode.Disabled,
                ScrollingSnapPointsMode.Ignore
            ));
        }

        public void PackageList_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            string key = ((char)e.Key).ToString().ToLower();
            if ("abcdefghijklmnopqrsztuvwxyz1234567890".IndexOf(key) > -1)
            {
                if (Environment.TickCount - LastKeyDown > QUERY_SEPARATION_TIME)
                {
                    TypeQuery = key;
                }
                else
                {
                    TypeQuery += key;
                }

                int IdQueryIndex = -1;
                int NameSimilarityIndex = -1;
                int IdSimilarityIndex = -1;
                bool SelectedPackage = false;
                for (int i = 0; i < FilteredPackages.Count; i++)
                {
                    if (FilteredPackages[i].Package.Name.ToLower().StartsWith(TypeQuery))
                    {
                        SelectAndScrollTo(i);
                        SelectedPackage = true;
                        break;
                    }
                    // To avoid jumping back high up because an ID matched it (prevent typing "wi" focusing id:"WildfireGames.0AD" instead of name:"Windows")
                    if (IdQueryIndex == -1 && FilteredPackages[i].Package.Id.ToLower().StartsWith(TypeQuery))
                    {
                        IdQueryIndex = i;
                    }
                    if (NameSimilarityIndex == -1 && FilteredPackages[i].Package.Name.ToLower().Contains(TypeQuery))
                    {
                        NameSimilarityIndex = i;
                    }
                    if (IdSimilarityIndex == -1 && FilteredPackages[i].Package.Id.ToLower().Contains(TypeQuery))
                    {
                        IdSimilarityIndex = i;
                    }
                }
                int QueryIndex = IdQueryIndex > -1 ? IdQueryIndex : (NameSimilarityIndex > -1 ? NameSimilarityIndex : IdSimilarityIndex);
                if (!SelectedPackage)
                {
                    bool SameChars = true;
                    char LastChar = TypeQuery.ToCharArray()[0];
                    foreach (var c in TypeQuery)
                    {
                        if (c != LastChar)
                        {
                            SameChars = false;
                            break;
                        }
                        LastChar = c;
                    }

                    if (SameChars)
                    {
                        int IndexOffset = TypeQuery.Length - 1;
                        int FirstIdx = -1;
                        int LastIdx = -1;
                        for (int idx = 0; idx < FilteredPackages.Count; idx++)
                        {
                            if (FilteredPackages[idx].Package.Name.ToLower().StartsWith(LastChar))
                            {
                                if (FirstIdx == -1) FirstIdx = idx;
                                LastIdx = idx;
                            }
                            else if (FirstIdx > -1)
                            {
                                // Break after the LastIdx has been set
                                break;
                            }
                        }

                        SelectAndScrollTo(FirstIdx + (IndexOffset % (LastIdx - FirstIdx + 1)));
                    }
                    else if (QueryIndex > -1)
                    {
                        SelectAndScrollTo(QueryIndex);
                    }
                }
            }
            LastKeyDown = Environment.TickCount;
        }

        /// <summary>
        /// Will filter the packages with the query on QueryBlock.Text and put the
        /// resulting packages on the ItemsView
        /// </summary>
        public void FilterPackages()
        {
            PackageWrapper? previousSelection = PackageList.SelectedItem as PackageWrapper;
            FilteredPackages.Clear();

            List<IManagerSource> VisibleSources = [];
            List<IPackageManager> VisibleManagers = [];

            if (SourcesTreeView.SelectedNodes.Count > 0)
            {
                foreach (TreeViewNode node in SourcesTreeView.SelectedNodes)
                {
                    if (NodesForSources.Values.Contains(node))
                    {
                        VisibleSources.Add(NodesForSources.First(x => x.Value == node).Key);
                    }
                    else if (RootNodeForManager.Values.Contains(node))
                    {
                        IPackageManager manager = RootNodeForManager.First(x => x.Value == node).Key;
                        VisibleManagers.Add(manager);
                        if (manager.Capabilities.SupportsCustomSources)
                        {
                            foreach (IManagerSource source in manager.SourcesHelper.Factory.GetAvailableSources())
                                if (!VisibleSources.Contains(source)) VisibleSources.Add(source);
                        }
                    }
                }
            }

            Func<string, string> CaseFunc;
            if (UpperLowerCaseCheckbox.IsChecked == true)
            {
                CaseFunc = (x) => { return x; };
            }
            else
            {
                CaseFunc = (x) => { return x.ToLower(); };
            }

            Func<string, string> CharsFunc;
            if (IgnoreSpecialCharsCheckbox.IsChecked == true)
            {
                CharsFunc = (x) =>
                {
                    string temp_x = CaseFunc(x).Replace("-", "").Replace("_", "").Replace(" ", "").Replace("@", "").Replace("\t", "").Replace(".", "").Replace(",", "").Replace(":", "");
                    foreach (KeyValuePair<char, string> entry in new Dictionary<char, string>
                        {
                            {'a', "àáäâ"},
                            {'e', "èéëê"},
                            {'i', "ìíïî"},
                            {'o', "òóöô"},
                            {'u', "ùúüû"},
                            {'y', "ýÿ"},
                            {'c', "ç"},
                            {'ñ', "n"},
                        })
                    {
                        foreach (char InvalidChar in entry.Value)
                        {
                            x = x.Replace(InvalidChar, entry.Key);
                        }
                    }
                    return temp_x;
                };
            }
            else
            {
                CharsFunc = (x) => { return CaseFunc(x); };
            }

            string treatedQuery = CharsFunc(QueryBlock.Text.Trim());
            IEnumerable<IPackage> MatchingList;

            if (QueryIdRadio.IsChecked == true)
            {
                MatchingList = Loader.Packages.Where(x => CharsFunc(x.Name).Contains(treatedQuery));
            }
            else if (QueryNameRadio.IsChecked == true)
            {
                MatchingList = Loader.Packages.Where(x => CharsFunc(x.Id).Contains(treatedQuery));
            }
            else if (QueryBothRadio.IsChecked == true)
            {
                MatchingList = Loader.Packages.Where(x => CharsFunc(x.Name).Contains(treatedQuery) | CharsFunc(x.Id).Contains(treatedQuery));
            }
            else if (QueryExactMatch.IsChecked == true)
            {
                MatchingList = Loader.Packages.Where(x => CharsFunc(x.Name) == treatedQuery | CharsFunc(x.Id) == treatedQuery);
            }
            else // QuerySimilarResultsRadio == true
            {
                MatchingList = Loader.Packages;
            }

            FilteredPackages.BlockSorting = true;

            foreach (IPackage match in MatchingList)
            {
                if (VisibleSources.Contains(match.Source) || (!match.Manager.Capabilities.SupportsCustomSources && VisibleManagers.Contains(match.Manager)))
                {
                    FilteredPackages.Add(match);
                }
            }
            FilteredPackages.BlockSorting = false;
            FilteredPackages.Sort();
            UpdatePackageCount();

            if (previousSelection is not null)
            {
                for (int i = 0; i < FilteredPackages.Count; i++)
                {
                    if (FilteredPackages[i].Package.Equals(previousSelection.Package))
                    {
                        PackageList.Select(i);
                        PackageList.ScrollView?.ScrollTo(0, Math.Max(0, (i - 3) * 39), new ScrollingScrollOptions
                        (
                            ScrollingAnimationMode.Enabled,
                            ScrollingSnapPointsMode.Ignore
                        ));
                        break;
                    }
                }
            }
            else
            {
                ForceRedrawByScroll();
            }

        }

        /// <summary>
        /// Updates the UI to reflect the current amount of packages
        /// </summary>
        public void UpdatePackageCount()
        {
            if (!FilteredPackages.Any())
            {
                if (LoadingProgressBar.Visibility == Visibility.Collapsed)
                {
                    if (!Loader.Any())
                    {
                        BackgroundText.Text = NoPackages_BackgroundText;
                        SourcesPlaceholderText.Text = NoPackages_SourcesText;
                        SourcesPlaceholderText.Visibility = Visibility.Visible;
                        MainSubtitle.Text = NoPackages_SubtitleText;
                    }
                    else
                    {
                        BackgroundText.Text = NoMatches_BackgroundText;
                        SourcesPlaceholderText.Visibility = Visibility.Collapsed;
                        MainSubtitle.Text = NoMatches_SubtitleText;
                    }
                    BackgroundText.Visibility = Visibility.Visible;
                }
                else
                {
                    BackgroundText.Visibility = Loader.Any() ? Visibility.Collapsed : Visibility.Visible;
                    BackgroundText.Text = MainSubtitle_StillLoading;
                    SourcesPlaceholderText.Visibility = Loader.Any() ? Visibility.Collapsed : Visibility.Visible;
                    SourcesPlaceholderText.Text = MainSubtitle_StillLoading;
                    MainSubtitle.Text = MainSubtitle_StillLoading;
                }
            }
            else
            {
                BackgroundText.Text = NoPackages_BackgroundText;
                BackgroundText.Visibility = Loader.Any() ? Visibility.Collapsed : Visibility.Visible;
                MainSubtitle.Text = FoundPackages_SubtitleText;
            }

            if (ExternalCountBadge is not null)
            {
                ExternalCountBadge.Visibility = !Loader.Any() ? Visibility.Collapsed : Visibility.Visible;
                ExternalCountBadge.Value = Loader.Count();
            }

            if (MegaQueryBlockGrid.Visibility == Visibility.Visible)
            {
                BackgroundText.Visibility = Visibility.Collapsed;
            }

            WhenPackageCountUpdated();
        }

        /// <summary>
        /// Changes how the packages are sorted
        /// </summary>
        /// <param name="sorter">The information with which to sort the packages</param>
        public void SortPackagesBy(ObservablePackageCollection.Sorter sorter)
        {
            FilteredPackages.Descending = !FilteredPackages.Descending;
            FilteredPackages.SetSorter(sorter);
            FilteredPackages.Sort();
        }

        protected void SelectAllSourcesButton_Click(object sender, RoutedEventArgs e)
        {
            SourcesTreeView.SelectAll();
        }

        protected void ClearSourceSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            SourcesTreeView.SelectedItems.Clear();
            FilterPackages();
        }

        protected void ShowDetailsForPackage(IPackage? package)
        {
            if (package is null || package.Source.IsVirtualManager || package is InvalidImportedPackage)
            {
                return;
            }

            DialogHelper.ShowPackageDetails(package, PAGE_ROLE);
        }

        protected void OpenPackageInstallLocation(IPackage? package)
        {
            string? path = package?.Manager.DetailsHelper.GetInstallLocation(package);

            if(path is not null)
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true,
                    Verb = "open"
                });
        }


        protected void SharePackage(IPackage? package)
        {
            if (package is null)
            {
                return;
            }

            MainApp.Instance.MainWindow.SharePackage(package);
        }

        protected async void ShowInstallationOptionsForPackage(IPackage? package)
        {
            if (package is null || package.Source.IsVirtualManager || package is InvalidImportedPackage)
            {
                return;
            }

            if (await DialogHelper.ShowInstallatOptions_Continue(package, PAGE_ROLE))
            {
                PerformMainPackageAction(package);
            }
        }

        private void SidepanelWidth_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (PaneIsAnimated)
                return;


            if (e.NewSize.Width == ((int)(e.NewSize.Width / 10)) || e.NewSize.Width == 25)
            {
                return;
            }

            if ((int)e.NewSize.Width < 30)
            {
                HideFilteringPane();
                Settings.SetDictionaryItem("SidepanelWidths", PAGE_NAME, 250);
            }
            else
            {
                Settings.SetDictionaryItem("SidepanelWidths", PAGE_NAME, (int)e.NewSize.Width);
            }
        }

        protected void PerformMainPackageAction(IPackage? package)
        {
            if (package is null)
            {
                return;
            }

            if (PAGE_ROLE == OperationType.Install)
            {
                MainApp.Instance.AddOperationToList(new InstallPackageOperation(package));
            }
            else if (PAGE_ROLE == OperationType.Update)
            {
                MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package));
            }
            else // if (PageRole == OperationType.Uninstall)
            {
                MainApp.Instance.AddOperationToList(new UninstallPackageOperation(package));
            }
        }

        public void FocusPackageList()
        { PackageList.Focus(FocusState.Programmatic); }

        private void PackageItemContainer_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is PackageItemContainer container && container.Package is not null)
            {
                PackageList.Select(container.Wrapper.Index);
                WhenShowingContextMenu(container.Package);
            }
        }

        private void PackageItemContainer_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is PackageItemContainer container && container.Package is not null)
            {
                PackageList.Select(container.Wrapper.Index);
                ShowDetailsForPackage(container.Package);
            }
        }

        private void PackageItemContainer_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            IPackage? package = (sender as PackageItemContainer)?.Package;

            bool IS_CONTROL_PRESSED = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            //bool IS_SHIFT_PRESSED = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            bool IS_ALT_PRESSED = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.LeftMenu).HasFlag(CoreVirtualKeyStates.Down);
            IS_ALT_PRESSED |= InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.RightMenu).HasFlag(CoreVirtualKeyStates.Down);

            if (e.Key == VirtualKey.Enter && package is not null)
            {
                if (IS_ALT_PRESSED)
                {
                    if(!package.Source.IsVirtualManager && package is not InvalidImportedPackage)
                        ShowInstallationOptionsForPackage(package);
                }
                else if (IS_CONTROL_PRESSED)
                {
                    if(package is not InvalidImportedPackage)
                        PerformMainPackageAction(package);
                }
                else
                {
                    if(!package.Source.IsVirtualManager && package is not InvalidImportedPackage)
                        ShowDetailsForPackage(package);
                }
            }
            else if (e.Key == VirtualKey.Space && package is not null)
            {
                package.IsChecked = !package.IsChecked;
            }
        }

        private void SelectAllCheckBox_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (SelectAllCheckBox.IsChecked == true)
            {
                FilteredPackages.SelectAll();
            }
            else
            {
                FilteredPackages.ClearSelection();
            }
        }

        private async void ForceRedrawByScroll()
        {
            if (PackageList is not null)
            {
                PackageList.ScrollView?.ScrollBy(0, 1);
                await Task.Delay(10);
                PackageList.ScrollView?.ScrollBy(0, -1);
            }
        }

        private void ToggleFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.SetDictionaryItem("HideToggleFilters", PAGE_NAME, !ToggleFiltersButton.IsChecked ?? false);
            if (ToggleFiltersButton.IsChecked ?? false)
            {
                ShowFilteringPane();
            }
            else
            {
                HideFilteringPane();
            }
        }

        private async void HideFilteringPane(bool skipAnimation = false)
        {
            if (PaneIsAnimated) return;

            PaneIsAnimated = true;
            ToggleFiltersButton.IsChecked = false;

            if (!skipAnimation)
            {
                OutAnimation_FiltersPane.Start();
                double width = BodyGrid.ColumnDefinitions[0].Width.Value;
                while (width > 0)
                {
                    BodyGrid.ColumnDefinitions[0].Width = new GridLength(width);
                    await Task.Delay(10);
                    width -= 40;
                }
            }

            FiltersResizer.Visibility = SidePanel.Visibility = Visibility.Collapsed;
            BodyGrid.ColumnDefinitions[0].Width = new GridLength(0);
            BodyGrid.ColumnSpacing = 0;
            PaneIsAnimated = false;
        }

        private async void ShowFilteringPane(bool skipAnimation = false)
        {
            if (PaneIsAnimated) return;

            PaneIsAnimated = true;
            ToggleFiltersButton.IsChecked = true;
            FiltersResizer.Visibility = SidePanel.Visibility = Visibility.Visible;
            BodyGrid.ColumnSpacing = 12;
            InAnimation_FiltersPane.Start();

            int final_width = 250;
            try
            {
                final_width = Settings.GetDictionaryItem<string, int>("SidepanelWidths", PAGE_NAME);
            }
            catch
            {
                Settings.SetDictionaryItem("SidepanelWidths", PAGE_NAME, 250);
            }

            if (!skipAnimation)
            {
                double width = 0;
                while (width < final_width)
                {
                    BodyGrid.ColumnDefinitions[0].Width = new GridLength(width);
                    await Task.Delay(10);
                    width += 40;
                }
            }

            BodyGrid.ColumnDefinitions[0].Width = new GridLength(final_width);
            PaneIsAnimated = false;
        }

        private async Task LoadIconsForNewPackages()
        {
            var PackagesWithoutIcon = new List<PackageWrapper>();
            // Get the packages to be updated.
            foreach (var wrapper in FilteredPackages)
            {
                if (wrapper.IconWasLoaded) continue;
                wrapper.IconWasLoaded = true;
                PackagesWithoutIcon.Add(wrapper);
            }

            // Load their icons, one at a time.
            foreach (var wrapper in PackagesWithoutIcon)
            {
                var icon = await Task.Run(wrapper.Package.GetIconUrlIfAny);
                if(icon is not null) wrapper.PackageIcon = icon;
            }

            FilterPackages();
        }

        public void OnEnter()
        {
            Visibility = Visibility.Visible;
            IsEnabled = true;
        }

        public void OnLeave()
        {
            Visibility = Visibility.Collapsed;
            IsEnabled = false;
        }
    }
}

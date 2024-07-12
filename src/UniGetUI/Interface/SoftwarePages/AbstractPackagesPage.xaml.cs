using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.XPath;
using UniGetUI.Core.Classes;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Pages;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageClasses;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface
{

    public abstract partial class AbstractPackagesPage : Page, IPageWithKeyboardShortcuts
    {
        protected bool DISABLE_AUTOMATIC_PACKAGE_LOAD_ON_START = false;
        protected bool MEGA_QUERY_BOX_ENABLED = false;
        protected bool SHOW_LAST_CHECKED_TIME = false;

        protected enum ReloadReason
        {
            FirstRun,
            Automated,
            Manual,
            External
        }

        protected enum FilterReason
        {
            FirstRun,
            PackagesChanged,
            FilterChanged,
        }

        protected static Dictionary<char, string> REPLACEABLE_CHARS = new Dictionary<char, string>
        {
            {'a', "àáäâ"},{'e', "èéëê"},{'i', "ìíïî"},{'o', "òóöô"},
            {'u', "ùúüû"},{'y', "ýÿ"},  {'c', "ç"},   {'ñ', "n"},
        };

        protected OperationType PageRole = OperationType.Install;

        public DateTime LastPackageLoadTime { get; protected set; }

        public ObservableCollection<Package> Packages = new();

        // public List<Package> FilteredPackages = new(); // { SortingSelector = (a) => (a.Name) };
        private string FormattedQuery = "";
        protected AdvancedCollectionView PackageCollection;
        private HashSet<ManagerSource> SelectedPackageSources = new();
        private HashSet<PackageManager> SelectedPackageManagers = new();

        protected SortDirection SortOrder = SortDirection.Descending;

        protected List<PackageManager> UsedManagers = new();
        protected Dictionary<PackageManager, List<ManagerSource>> UsedSourcesForManager = new();
        protected Dictionary<PackageManager, TreeViewNode> RootNodeForManager = new();
        protected Dictionary<ManagerSource, TreeViewNode> NodesForSources = new();

        public int NewVersionLabelWidth { get { return RoleIsUpdateLike ? 125 : 0; } }
        public int NewVersionIconWidth { get { return RoleIsUpdateLike ? 24 : 0; } }

        private TreeViewNode LocalPackagesNode;

        public InfoBadge? ExternalCountBadge;

        protected bool Initialized = false;
        private bool AllSelected = true;

        int lastSavedWidth = 0;
        protected string PAGE_NAME = "UNDEFINED";
        public string InstantSearchSettingString { get { return $"DisableInstantSearch{PAGE_NAME}Tab"; } }
        public string SidepalWidthSettingString { get { return $"SidepanelWidth{PAGE_NAME}Page"; } }
        public bool RoleIsUpdateLike { get { return PageRole == OperationType.Update; } }
        protected abstract Task<Package[]> LoadPackagesFromManager(PackageManager manager);
        protected abstract Task<bool> IsPackageValid(Package package);
        protected abstract Task WhenAddingPackage(Package package);
        protected abstract Task WhenPackagesLoaded(ReloadReason reason);
        protected abstract void WhenPackageCountUpdated();
        protected abstract void WhenShowingContextMenu(Package package);
        public abstract void GenerateToolBar();
        public abstract BetterMenu GenerateContextMenu();
        public abstract void GenerateUIText();
        protected string NoPackages_BackgroundText = CoreTools.Translate("Hooray! No updates were found.");
        protected string NoPackages_SourcesText = CoreTools.Translate("Everything is up to date");
        protected string NoPackages_SubtitleMainText = CoreTools.Translate("Everything is up to date");
        protected string NoPackages_SubtitleText
        {
            get
            {
                return NoPackages_SubtitleMainText + " " +
                    (SHOW_LAST_CHECKED_TIME ? CoreTools.Translate("(Last checked: {0})", LastPackageLoadTime.ToString()) : "");
            }
        }
        protected string NoMatches_BackgroundText = CoreTools.Translate("No results were found matching the input criteria");
        protected string NoMatches_SourcesText = CoreTools.Translate("No packages were found");
        protected string NoMatches_SubtitleText
        {
            get
            {
                return CoreTools.Translate("{0} packages were found, {1} of which match the specified filters.",
                        Packages.Count.ToString(), PackageCollection.Count())
                        + " " + (SHOW_LAST_CHECKED_TIME? CoreTools.Translate("(Last checked: {0})", LastPackageLoadTime.ToString()): "");
            }
        }
        protected string FoundPackages_SubtitleText { get { return NoMatches_SubtitleText; } }
        protected string MainTitleText = CoreTools.AutoTranslated("Software Updates");
        protected string MainTitleGlyph = "\uE895";
        protected string MainSubtitle_StillLoading = CoreTools.Translate("Loading...");


        public AbstractPackagesPage()
        {
            InitializeComponent();
            LastPackageLoadTime = DateTime.Now;
            QueryBothRadio.IsChecked = true;
            QueryOptionsGroup.SelectedIndex = 2;
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            Initialized = true;

            PackageCollection = new AdvancedCollectionView(Packages, true);
            PackageCollection.Filter = PackageCollectionFilterer;
            PackageList.ItemsSource = PackageCollection;

            LocalPackagesNode = new TreeViewNode { 
                Content = CoreTools.Translate("Local"), 
                IsExpanded = false
            };

            try
            {
                int width = int.Parse(Settings.GetValue(SidepalWidthSettingString));
                BodyGrid.ColumnDefinitions.ElementAt(0).Width = new GridLength(width);
            }
            catch
            {
                Settings.SetValue(SidepalWidthSettingString, "250");
                BodyGrid.ColumnDefinitions.ElementAt(0).Width = new GridLength(250);
            }
            
            GenerateToolBar();
            LoadInterface();
            _ = LoadPackages(ReloadReason.FirstRun);

            QueryBlock.PlaceholderText = CoreTools.Translate("Search for packages");
            MegaQueryBlock.PlaceholderText = CoreTools.Translate("Search for packages");

            /*
             BEGIN REGISTERING EVENTS AND KEYBINDINGS
             */

            ReloadButton.Click += async (s, e) => { 
                await LoadPackages();
            };
            
            FindButton.Click += (s, e) => { 
                MegaQueryBlockGrid.Visibility = Visibility.Collapsed;
                SortPackages();
            };
            
            QueryBlock.TextChanged += (s, e) => {
                if (InstantSearchCheckbox.IsChecked == true)
                {
                    SortPackages();
                }
            };
            
            QueryBlock.KeyUp += (s, e) => {
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    MegaQueryBlockGrid.Visibility = Visibility.Collapsed;
                    SortPackages();
                }
            };
            
            QueryBlock.TextChanged += (s, e) => {
                if (MEGA_QUERY_BOX_ENABLED &&  QueryBlock.Text.Trim() == "")
                {
                    MegaQueryBlockGrid.Visibility = Visibility.Visible;
                    BackgroundText.Visibility = Visibility.Collapsed;
                    ClearPackageList();
                    UpdatePackageCount();
                    MegaQueryBlock.Focus(FocusState.Programmatic);
                    MegaQueryBlock.Text = "";
                }
            };

            MegaQueryBlock.KeyUp += (s, e) => {
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    MegaQueryBlockGrid.Visibility = Visibility.Collapsed;
                    QueryBlock.Text = MegaQueryBlock.Text.Trim();
                    SortPackages();
                }
            };

            MegaFindButton.Click += (s, e) =>
            {
                MegaQueryBlockGrid.Visibility = Visibility.Collapsed;
                QueryBlock.Text = MegaQueryBlock.Text.Trim();
                SortPackages();
            };

            SourcesTreeView.SelectionChanged += (s, e) =>
            {
                SortPackages();
            };

            SourcesTreeView.Tapped += (s, e) =>
            {
                if (e.OriginalSource != null && (e.OriginalSource as FrameworkElement)?.DataContext is TreeViewNode)
                {
                    TreeViewNode? node = (e.OriginalSource as FrameworkElement)?.DataContext as TreeViewNode;
                    if (node == null) return;
                    if (SourcesTreeView.SelectedNodes.Contains(node))
                        SourcesTreeView.SelectedNodes.Remove(node);
                    else
                        SourcesTreeView.SelectedNodes.Add(node);
                    SortPackages();
                }
            };

            SourcesTreeView.RightTapped += (s, e) =>
            {
                if (e.OriginalSource != null && (e.OriginalSource as FrameworkElement)?.DataContext is TreeViewNode)
                {
                    TreeViewNode? node = (e.OriginalSource as FrameworkElement)?.DataContext as TreeViewNode;
                    if (node == null) return;
                    SourcesTreeView.SelectedNodes.Clear();
                    SourcesTreeView.SelectedNodes.Add(node);
                    SortPackages();
                }
            };

            PackageList.DoubleTapped += (s, e) =>
            {
                ShowDetailsForPackage(PackageList.SelectedItem as Package);
            };

            PackageList.RightTapped += (s, e) =>
            {
                if (e.OriginalSource is FrameworkElement element)
                {
                    try
                    {
                        if (element.DataContext != null && element.DataContext is Package package)
                        {
                            PackageList.SelectedItem = package;
                            WhenShowingContextMenu(package);
                            (PackageList.ContextFlyout as BetterMenu)?.ShowAt(PackageList, e.GetPosition(PackageList));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex);
                    }
                }
            };

            PackageList.KeyUp += (s, e) =>
            {
                bool IS_CONTROL_PRESSED = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
                bool IS_SHIFT_PRESSED = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
                bool IS_ALT_PRESSED = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);

                if (e.Key == Windows.System.VirtualKey.Enter && PackageList.SelectedItem != null)
                {
                    if (IS_ALT_PRESSED)
                        ShowInstallationOptionsForPackage(PackageList.SelectedItem as Package);
                    else if (IS_CONTROL_PRESSED)
                        PerformMainPackageAction(PackageList.SelectedItem as Package);
                    else
                        ShowDetailsForPackage(PackageList.SelectedItem as Package);
                }                
                else if (e.Key == Windows.System.VirtualKey.Space)
                {
                    Package? package = PackageList.SelectedItem as Package;
                    if(package != null)
                        package.IsChecked = !package.IsChecked;
                }
            };

            /*
             END REGISTERING EVENTS AND KEYBINDINGS
             */

            SortPackages("Name");
        }
        
        private string QueryFormatter(string x)
        {
            if (IgnoreSpecialCharsCheckbox.IsChecked == true)
            {
                x = UpperLowerCaseCheckbox.IsChecked == false ? x.ToLower() : x;
                x = x.Replace("-", "").Replace("_", "").Replace(" ", "").Replace(":", "");
                x = x.Replace("@", "").Replace("\t", "").Replace("\n", "").Replace(".", "").Replace(",", "");
                foreach (KeyValuePair<char, string> entry in REPLACEABLE_CHARS)
                {
                    foreach (char InvalidChar in entry.Value)
                    {
                        x = x.Replace(InvalidChar, entry.Key);
                    }
                }
                return x;
            }
            else
            {
                return x;
            }
        }

        private bool CompareQueryAgainsFields(Package x)
        {
            if (QueryIdRadio.IsChecked == true)
            {
                return QueryFormatter(x.Name).Contains(FormattedQuery);
            }
            else if (QueryNameRadio.IsChecked == true)
            {
                return QueryFormatter(x.Id).Contains(FormattedQuery);
            }
            else if (QueryBothRadio.IsChecked == true)
            {
                return QueryFormatter(x.Id).Contains(FormattedQuery) || QueryFormatter(x.Name).Contains(FormattedQuery);
            }
            else if (QueryExactMatch.IsChecked == true)
            {
                return QueryFormatter(x.Id) == FormattedQuery || QueryFormatter(x.Name) == FormattedQuery;
            }
            else // QuerySimilarResultsRadio.IsChecked == true
            {
                return true;
            }
        }

        private bool PackageCollectionFilterer(object? x)
        {
            bool result;
            var package = x as Package;
            if (package == null)
            {
                result = false;
            }
            else if (SelectedPackageManagers.Contains(package.Manager))
            {
                if (!package.Manager.Capabilities.SupportsCustomSources || SelectedPackageSources.Contains(package.Source))
                {
                    result = CompareQueryAgainsFields(package);
                }
                
                else
                {
                    result = false;
                }
            }
            else if (package.Source.IsVirtualManager && SelectedPackageSources.Contains(package.Source))
            {
                result = CompareQueryAgainsFields(package);
            }
            else
            {
                result = false;
            }
            return result;
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
            if (AllSelected)
                ClearItemSelection();
            else
                SelectAllItems();
        }

        protected void AddPackageToSourcesList(Package package)
        {
            if (!Initialized)
                return;
            ManagerSource source = package.Source;
            if (!UsedManagers.Contains(source.Manager))
            {
                UsedManagers.Add(source.Manager);
                TreeViewNode Node;
                Node = new TreeViewNode { Content = source.Manager.Name + "                                                                                    .", IsExpanded = false };
                SourcesTreeView.RootNodes.Add(Node);

                // Smart way to decide whether to check a source or not.
                // - Always check a source by default if no sources are present
                // - Otherwise, Check a source only if half of the sources have already been checked
                if(SourcesTreeView.RootNodes.Count == 0)
                    SourcesTreeView.SelectedNodes.Add(Node);
                else if (SelectedPackageManagers.Count >= SourcesTreeView.RootNodes.Count/2)
                    SourcesTreeView.SelectedNodes.Add(Node);


                RootNodeForManager.Add(source.Manager, Node);
                UsedSourcesForManager.Add(source.Manager, new List<ManagerSource>());
                SourcesPlaceholderText.Visibility = Visibility.Collapsed;
                SourcesTreeViewGrid.Visibility = Visibility.Visible;
            }

            if ((!UsedSourcesForManager.ContainsKey(source.Manager) || !UsedSourcesForManager[source.Manager].Contains(source)) && source.Manager.Capabilities.SupportsCustomSources)
            {
                UsedSourcesForManager[source.Manager].Add(source);
                TreeViewNode item = new() { Content = source.Name + "                                                                                    ." };
                NodesForSources.Add(source, item);

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
                    RootNodeForManager[source.Manager].Children.Add(item);
            }
        }

        private void PackageContextMenu_AboutToShow(object sender, Package package)
        {
            if (!Initialized)
                return;
            PackageList.SelectedItem = package;
        }

        private void FilterOptionsChanged(object sender, RoutedEventArgs e)
        {
            if (!Initialized)
                return;
            SortPackages();
        }

        private void InstantSearchValueChanged(object sender, RoutedEventArgs e)
        {
            if (!Initialized)
                return;
            Settings.Set(InstantSearchSettingString, InstantSearchCheckbox.IsChecked == false);
        }
        
        private void SourcesTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            SortPackages();
        }

        public virtual async Task LoadPackages()
        {
            await LoadPackages(ReloadReason.External);
        }

        protected void ClearPackageList()
        {
            Packages.Clear();
            UsedManagers.Clear();
            SourcesTreeView.RootNodes.Clear();
            UsedSourcesForManager.Clear();
            RootNodeForManager.Clear();
            NodesForSources.Clear();
        }

        protected async Task LoadPackages(ReloadReason reason)
        {
            if (!Initialized)
                return;

            if (LoadingProgressBar.Visibility == Visibility.Visible)
                return; // If already loading, don't load again

            if (DISABLE_AUTOMATIC_PACKAGE_LOAD_ON_START && reason == ReloadReason.FirstRun)
                return;

            MainSubtitle.Text = MainSubtitle_StillLoading;
            BackgroundText.Text = MainSubtitle_StillLoading;
            BackgroundText.Visibility = Visibility.Visible;
            LoadingProgressBar.Visibility = Visibility.Visible;
            SourcesPlaceholderText.Visibility = Visibility.Visible;
            SourcesPlaceholderText.Text = MainSubtitle_StillLoading;
            SourcesTreeViewGrid.Visibility = Visibility.Collapsed;

            ClearPackageList();

            await Task.Delay(100);

            List<Task<Package[]>> tasks = new();

            foreach (PackageManager manager in MainApp.Instance.PackageManagerList)
            {
                if (manager.IsEnabled() && manager.Status.Found)
                {
                    Task<Package[]> task = LoadPackagesFromManager(manager);
                    tasks.Add(task);
                }
            }

            while (tasks.Count > 0)
            {
                foreach (Task<Package[]> task in tasks.ToArray())
                {
                    if (!task.IsCompleted)
                        await Task.Delay(100);

                    if (task.IsCompleted)
                    {
                        if (task.IsCompletedSuccessfully)
                        {
                            int InitialCount = Packages.Count;
                            //using (PackageCollection.DeferRefresh())
                            //{
                                foreach (Package package in task.Result)
                                {
                                    if (!await IsPackageValid(package))
                                        continue;

                                    Packages.Add(package);
                                    await WhenAddingPackage(package);
                                    AddPackageToSourcesList(package);
                                }
                            //}
                            SortPackages();
                        }
                        tasks.Remove(task);
                    }
                }
            }

            LoadingProgressBar.Visibility = Visibility.Collapsed;
            LastPackageLoadTime = DateTime.Now;
            await WhenPackagesLoaded(reason); 
            SortPackages();

        }


        public void UpdatePackageCount()
        {
            if (PackageCollection.Count() == 0)
            {
                if(LoadingProgressBar.Visibility == Visibility.Collapsed)
                {
                    if (Packages.Count() == 0)
                    {
                        BackgroundText.Text = NoPackages_BackgroundText;
                        SourcesPlaceholderText.Text = NoPackages_SourcesText;
                        MainSubtitle.Text = NoPackages_SubtitleText;
                    }
                    else
                    {
                        BackgroundText.Text = NoMatches_BackgroundText;
                        SourcesPlaceholderText.Text = NoMatches_SourcesText;
                        MainSubtitle.Text = NoMatches_SubtitleText;
                    }
                    BackgroundText.Visibility = Visibility.Visible;
                }
                else
                {
                    BackgroundText.Visibility = PackageList.Items.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                    BackgroundText.Text = MainSubtitle_StillLoading;
                    SourcesPlaceholderText.Visibility = Packages.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                    SourcesPlaceholderText.Text = MainSubtitle_StillLoading;
                    MainSubtitle.Text = MainSubtitle_StillLoading;
                }

            }
            else
            {
                BackgroundText.Text = NoPackages_BackgroundText;
                BackgroundText.Visibility = Packages.Count > 0? Visibility.Collapsed: Visibility.Visible;
                MainSubtitle.Text = FoundPackages_SubtitleText;
            }

            if (ExternalCountBadge != null)
            {
                ExternalCountBadge.Visibility = Packages.Count() == 0 ? Visibility.Collapsed : Visibility.Visible;
                ExternalCountBadge.Value = Packages.Count();
            }

            if (MegaQueryBlockGrid.Visibility == Visibility.Visible)
                BackgroundText.Visibility = Visibility.Collapsed;
            
            WhenPackageCountUpdated();
        }

        /// <summary>
        /// Will sort packages. If Sorter is given, the specified order will be used
        /// </summary>
        /// <param name="Sorter"></param>
        public void SortPackages(string? Sorter = null)
        {
            if (!Initialized)
                return;

            Logger.Error("Sorting packages");

            SelectedPackageSources.Clear();
            SelectedPackageManagers.Clear();

            if (SourcesTreeView.SelectedNodes.Count > 0)
            {
                foreach (TreeViewNode node in SourcesTreeView.SelectedNodes)
                {
                    if (NodesForSources.ContainsValue(node))
                        SelectedPackageSources.Add(NodesForSources.First(x => x.Value == node).Key);
                    else if (RootNodeForManager.ContainsValue(node))
                        SelectedPackageManagers.Add(RootNodeForManager.First(x => x.Value == node).Key);
                }
            }

            foreach (var manaber in SelectedPackageManagers)
                Logger.Error(manaber.Name);

            FormattedQuery = QueryFormatter(QueryBlock.Text);

            if (Sorter != null)
            {
                SortOrder = SortOrder == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
                PackageCollection.SortDescriptions.Clear();
                PackageCollection.SortDescriptions.Add(new SortDescription(Sorter, SortOrder));
            }

            PackageCollection.Refresh();

            if (PackageCollection.Count > 0)
                PackageList.ScrollIntoView(PackageCollection[0]);

            UpdatePackageCount();
        }

        public void FocusPackageList()
        {
            PackageList.Focus(FocusState.Programmatic);
        }

        public void LoadInterface()
        {
            if (!Initialized)
                return;

            GenerateUIText();

            if (MEGA_QUERY_BOX_ENABLED)
            {
                MegaQueryBlockGrid.Visibility = Visibility.Visible;
                MegaQueryBlock.Focus(FocusState.Programmatic); 
                BackgroundText.Visibility = Visibility.Collapsed;
            }

            InstantSearchCheckbox.IsChecked = !Settings.Get(InstantSearchSettingString);

            MainTitle.Text = MainTitleText;
            HeaderIcon.Glyph = MainTitleGlyph;
            HeaderIcon.FontWeight = new Windows.UI.Text.FontWeight(700);
            CheckboxHeader.Content = " ";
            NameHeader.Content = CoreTools.Translate("Package Name");
            IdHeader.Content = CoreTools.Translate("Package ID");
            VersionHeader.Content = CoreTools.Translate("Version");
            NewVersionHeader.Content = CoreTools.Translate("New version");
            SourceHeader.Content = CoreTools.Translate("Source");

            CheckboxHeader.Click += (s, e) => { SortPackages("IsCheckedAsString"); };
            NameHeader.Click += (s, e) => { SortPackages("Name"); };
            IdHeader.Click += (s, e) => { SortPackages("Id"); };
            VersionHeader.Click += (s, e) => { SortPackages("VersionAsFloat"); };
            NewVersionHeader.Click += (s, e) => { SortPackages("NewVersionAsFloat"); };
            SourceHeader.Click += (s, e) => { SortPackages("SourceAsString"); };
            PackageList.ContextFlyout = GenerateContextMenu();
        }

        protected void SelectAllSourcesButton_Click(object sender, RoutedEventArgs e)
        {
            SourcesTreeView.SelectAll();
        }

        protected void ClearSourceSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            SourcesTreeView.SelectedItems.Clear();
            SortPackages();
        }

        protected async void ShowDetailsForPackage(Package? package)
        {
            if (package == null)
                return;

            Logger.Warn(PageRole.ToString());
            await MainApp.Instance.MainWindow.NavigationPage.ShowPackageDetails(package, PageRole);
        }

        protected void SharePackage(Package? package)
        {
            if(package == null)
                return;
            MainApp.Instance.MainWindow.SharePackage(package);
        }

        protected async void ShowInstallationOptionsForPackage(Package? package)
        {
            if(package == null)
                return;

            if (await MainApp.Instance.MainWindow.NavigationPage.ShowInstallationSettingsForPackageAndContinue(package, PageRole))
                PerformMainPackageAction(package);
        }

        protected void SelectAllItems()
        {
            foreach (Package package in PackageCollection)
                package.IsChecked = true;
            AllSelected = true;
        }

        protected void ClearItemSelection()
        {
            foreach (Package package in PackageCollection)
                package.IsChecked = false;
            AllSelected = false;
        }

        public void RemoveCorrespondingPackages(Package foreignPackage)
        {
            foreach (Package package in Packages.ToArray())
            {
                if (package == foreignPackage || package.IsEquivalentTo(foreignPackage))
                {
                    package.Tag = PackageTag.Default;
                    Packages.Remove(package);
                    /*package.Tag = PackageTag.Default;
                    if (PackageCollection.Contains(package))
                        PackageCollection.Remove(package);*/
                }
            }
            UpdatePackageCount();
        }

        private void SidepanelWidth_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width == ((int)(e.NewSize.Width / 10)) || e.NewSize.Width == 25)
                return;

            lastSavedWidth = ((int)(e.NewSize.Width / 10));
            Settings.SetValue("SidepanelWidthUpdatesPage", ((int)e.NewSize.Width).ToString());
            foreach (UIElement control in SidePanelGrid.Children)
            {
                control.Visibility = e.NewSize.Width > 20 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        protected void PerformMainPackageAction(Package? package)
        {
            if(package == null) return;

            if (PageRole == OperationType.Install)
                MainApp.Instance.AddOperationToList(new InstallPackageOperation(package));
            else if (PageRole == OperationType.Update)
                MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package));
            else // if (PageRole == OperationType.Uninstall)
                MainApp.Instance.AddOperationToList(new UninstallPackageOperation(package));
        }

    }
}

using CommunityToolkit.WinUI.Notifications;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using UniGetUI.Core;
using UniGetUI.Core.Classes;
using UniGetUI.Core.Data;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.Core.Logging;
using Windows.UI.Core;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.Core.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface
{

    public partial class SoftwareUpdatesPage : Page
    {
        public ObservableCollection<UpgradablePackage> Packages = new();
        public SortableObservableCollection<UpgradablePackage> FilteredPackages = new() { SortingSelector = (a) => (a.Name) };
        protected List<PackageManager> UsedManagers = new();
        protected Dictionary<PackageManager, List<ManagerSource>> UsedSourcesForManager = new();
        protected Dictionary<PackageManager, TreeViewNode> RootNodeForManager = new();
        protected Dictionary<ManagerSource, TreeViewNode> NodesForSources = new();

        protected TranslatedTextBlock MainTitle;
        protected TextBlock MainSubtitle;
        public ListView PackageList;
        protected ProgressBar LoadingProgressBar;
        protected MenuFlyout ContextMenu;

        private bool Initialized = false;
        private bool AllSelected = true;

        private DateTime LastChecked = DateTime.Now;
        int lastSavedWidth = 0;

        public string InstantSearchSettingString = "DisableInstantSearchUpdatesTab";
        public SoftwareUpdatesPage()
        {
            InitializeComponent();
            QueryBothRadio.IsChecked = true;
            QueryOptionsGroup.SelectedIndex = 2;
            MainTitle = __main_title;
            MainSubtitle = __main_subtitle;
            PackageList = __package_list;
            LoadingProgressBar = __loading_progressbar;
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            Initialized = true;
            ReloadButton.Click += async (s, e) => { await LoadPackages(); };
            FindButton.Click += (s, e) => { FilterPackages(QueryBlock.Text); };
            QueryBlock.TextChanged += (s, e) => { if (InstantSearchCheckbox.IsChecked == true) FilterPackages(QueryBlock.Text); };
            QueryBlock.KeyUp += (s, e) => { if (e.Key == Windows.System.VirtualKey.Enter) FilterPackages(QueryBlock.Text); };

            SourcesTreeView.Tapped += (s, e) =>
            {
                if (e.OriginalSource != null && (e.OriginalSource as FrameworkElement).DataContext != null)
                {
                    if ((e.OriginalSource as FrameworkElement).DataContext is TreeViewNode)
                    {
                        TreeViewNode node = (e.OriginalSource as FrameworkElement).DataContext as TreeViewNode;
                        if (node == null)
                            return;
                        if (SourcesTreeView.SelectedNodes.Contains(node))
                            SourcesTreeView.SelectedNodes.Remove(node);
                        else
                            SourcesTreeView.SelectedNodes.Add(node);
                        FilterPackages(QueryBlock.Text.Trim());
                    }
                }
            };
            PackageList.DoubleTapped += (s, e) =>
            {
                _ = MainApp.Instance.MainWindow.NavigationPage.ShowPackageDetails(PackageList.SelectedItem as Package, OperationType.Update);
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
                            MenuAsAdmin.IsEnabled = package.Manager.Capabilities.CanRunAsAdmin;
                            MenuInteractive.IsEnabled = package.Manager.Capabilities.CanRunInteractively;
                            MenuskipHash.IsEnabled = package.Manager.Capabilities.CanSkipIntegrityChecks;
                            PackageContextMenu.ShowAt(PackageList, e.GetPosition(PackageList));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex);
                    }
                }
            };

            PackageList.KeyUp += async (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter && PackageList.SelectedItem != null)
                {
                    if (InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down))
                    {
                        if (await MainApp.Instance.MainWindow.NavigationPage.ShowInstallationSettingsForPackageAndContinue(PackageList.SelectedItem as Package, OperationType.Update))
                            MainApp.Instance.AddOperationToList(new UninstallPackageOperation(PackageList.SelectedItem as Package));
                    }
                    else if (InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
                        MainApp.Instance.AddOperationToList(new UpdatePackageOperation(PackageList.SelectedItem as Package));
                    else
                        _ = MainApp.Instance.MainWindow.NavigationPage.ShowPackageDetails(PackageList.SelectedItem as Package, OperationType.Update);
                }
                else if (e.Key == Windows.System.VirtualKey.A && InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
                {
                    if (AllSelected)
                        ClearItemSelection();
                    else
                        SelectAllItems();
                }
                else if (e.Key == Windows.System.VirtualKey.Space && PackageList.SelectedItem != null)
                {
                    (PackageList.SelectedItem as Package).IsChecked = !(PackageList.SelectedItem as Package).IsChecked;
                }
                else if (e.Key == Windows.System.VirtualKey.F5)
                {
                    _ = LoadPackages();
                }
                else if (e.Key == Windows.System.VirtualKey.F1)
                {
                    MainApp.Instance.MainWindow.NavigationPage.ShowHelp();
                }
                else if (e.Key == Windows.System.VirtualKey.F && InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
                {
                    QueryBlock.Focus(FocusState.Programmatic);
                }
            };

            int width = 250;
            try
            {
                width = int.Parse(Settings.GetValue("SidepanelWidthUpdatesPage"));
            }
            catch
            {
                Settings.SetValue("SidepanelWidthUpdatesPage", "250");
            }
            BodyGrid.ColumnDefinitions.ElementAt(0).Width = new GridLength(width);


            GenerateToolBar();
            LoadInterface();
            _ = LoadPackages(ManualCheck: false);

            QueryBlock.PlaceholderText = CoreTools.Translate("Search for packages");
        }

        protected void AddPackageToSourcesList(UpgradablePackage package)
        {
            if (!Initialized)
                return;

            ManagerSource source = package.Source;
            if (!UsedManagers.Contains(source.Manager))
            {
                UsedManagers.Add(source.Manager);
                TreeViewNode Node;
                Node = new TreeViewNode() { Content = source.Manager.Name + "                                                                                    .", IsExpanded = false };
                SourcesTreeView.RootNodes.Add(Node);
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
                RootNodeForManager[source.Manager].Children.Add(item);

            }
        }

        private void PackageContextMenu_AboutToShow(object sender, Package _package)
        {
            UpgradablePackage package = _package as UpgradablePackage;
            if (!Initialized)
                return;
            PackageList.SelectedItem = package;
        }

        private void FilterOptionsChanged(object sender, RoutedEventArgs e)
        {
            if (!Initialized)
                return;
            FilterPackages(QueryBlock.Text);
        }

        private void InstantSearchValueChanged(object sender, RoutedEventArgs e)
        {
            if (!Initialized)
                return;
            Settings.Set(InstantSearchSettingString, InstantSearchCheckbox.IsChecked == false);
        }
        private void SourcesTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            FilterPackages(QueryBlock.Text);
        }

        /*
         * 
         * 
         *  DO NOT MODIFY THE UPPER PART OF THIS FILE
         * 
         * 
         */

        public async Task LoadPackages(bool ManualCheck = true)
        {
            if (!Initialized)
                return;

            if (LoadingProgressBar.Visibility == Visibility.Visible)
                return; // If already loading, don't load again

            MainSubtitle.Text = CoreTools.Translate("Loading...");
            BackgroundText.Text = CoreTools.AutoTranslated("Loading...");
            BackgroundText.Visibility = Visibility.Visible;
            LoadingProgressBar.Visibility = Visibility.Visible;
            SourcesPlaceholderText.Visibility = Visibility.Visible;
            SourcesPlaceholderText.Text = CoreTools.AutoTranslated("Loading...");
            SourcesTreeViewGrid.Visibility = Visibility.Collapsed;

            Packages.Clear();
            FilteredPackages.Clear();
            UsedManagers.Clear();
            SourcesTreeView.RootNodes.Clear();
            UsedSourcesForManager.Clear();
            RootNodeForManager.Clear();
            NodesForSources.Clear();

            await Task.Delay(100);

            List<Task<UpgradablePackage[]>> tasks = new();

            foreach (PackageManager manager in MainApp.Instance.PackageManagerList)
            {
                if (manager.IsEnabled() && manager.Status.Found)
                {
                    Task<UpgradablePackage[]> task = manager.GetAvailableUpdates();
                    tasks.Add(task);
                }
            }

            while (tasks.Count > 0)
            {
                foreach (Task<UpgradablePackage[]> task in tasks.ToArray())
                {
                    if (!task.IsCompleted)
                        await Task.Delay(100);

                    if (task.IsCompleted)
                    {
                        if (task.IsCompletedSuccessfully)
                        {
                            int InitialCount = Packages.Count;
                            foreach (UpgradablePackage package in task.Result)
                            {
                                if (await package.HasUpdatesIgnoredAsync(package.NewVersion))
                                    continue;

                                if (package.NewVersionIsInstalled())
                                    Logger.Debug($"Package Id={package.Id} with NewVersion={package.NewVersion} is already installed, skipping it...");

                                package.GetAvailablePackage()?.SetTag(PackageTag.IsUpgradable);
                                package.GetInstalledPackage()?.SetTag(PackageTag.IsUpgradable);

                                Packages.Add(package);
                                AddPackageToSourcesList(package);
                            }
                            if (InitialCount < Packages.Count)
                                FilterPackages(QueryBlock.Text.Trim(), StillLoading: true);
                        }
                        tasks.Remove(task);
                    }
                }
            }

            LastChecked = DateTime.Now;

            FilterPackages(QueryBlock.Text);
            LoadingProgressBar.Visibility = Visibility.Collapsed;

            List<UpgradablePackage> upgradablePackages = new();
            foreach (UpgradablePackage package in Packages)
            {
                if (package.Tag != PackageTag.OnQueue && package.Tag != PackageTag.BeingProcessed)
                    upgradablePackages.Add(package);
            }

            if (upgradablePackages.Count > 0)
            {
                string body = "";
                string title = "";
                string attribution = "";
                bool ShowButtons = false;
                if (Settings.Get("AutomaticallyUpdatePackages") || Environment.GetCommandLineArgs().Contains("--updateapps"))
                {
                    if (upgradablePackages.Count == 1)
                    {
                        title = CoreTools.Translate("An update was found!");
                        body = CoreTools.Translate("{0} is being updated to version {1}").Replace("{0}", upgradablePackages[0].Name).Replace("{1}", upgradablePackages[0].NewVersion);
                        attribution = CoreTools.Translate("You have currently version {0} installed").Replace("{0}", upgradablePackages[0].Version);
                    }
                    else
                    {
                        title = CoreTools.Translate("Updates found!");
                        body = CoreTools.Translate("{0} packages are being updated").Replace("{0}", upgradablePackages.Count.ToString()); ;
                        foreach (UpgradablePackage package in upgradablePackages)
                        {
                            attribution += package.Name + ", ";
                        }
                        attribution = attribution.TrimEnd(' ').TrimEnd(',');
                    }
                    UpdateAll();
                }
                else
                {
                    if (upgradablePackages.Count == 1)
                    {
                        title = CoreTools.Translate("An update was found!");
                        body = CoreTools.Translate("{0} can be updated to version {1}").Replace("{0}", upgradablePackages[0].Name).Replace("{1}", upgradablePackages[0].NewVersion);
                        attribution = CoreTools.Translate("You have currently version {0} installed").Replace("{0}", upgradablePackages[0].Version);
                    }
                    else
                    {
                        title = CoreTools.Translate("Updates found!");
                        body = CoreTools.Translate("{0} packages can be updated").Replace("{0}", upgradablePackages.Count.ToString()); ;
                        foreach (UpgradablePackage package in upgradablePackages)
                        {
                            attribution += package.Name + ", ";
                        }
                        attribution = attribution.TrimEnd(' ').TrimEnd(',');
                    }
                    ShowButtons = true;
                }

                if (!Settings.Get("DisableUpdatesNotifications") && !Settings.Get("DisableNotifications"))
                {
                    try
                    {

                        ToastContentBuilder toast = new();
                        toast.AddArgument("action", "openUniGetUIOnUpdatesTab");
                        toast.AddArgument("notificationId", CoreData.UpdatesAvailableNotificationId);
                        toast.AddText(title);
                        toast.AddText(body);
                        toast.AddAttributionText(attribution);
                        if (ShowButtons)
                        {
                            toast.AddButton(new ToastButton()
                                .SetContent(CoreTools.Translate("Open WingetUI"))
                                .AddArgument("action", "openUniGetUIOnUpdatesTab")
                                .SetBackgroundActivation());
                            toast.AddButton(new ToastButton()
                                .SetContent(upgradablePackages.Count == 1 ? CoreTools.Translate("Update") : CoreTools.Translate("Update all"))
                                .AddArgument("action", "updateAll")
                                .SetBackgroundActivation());
                        }
                        toast.Show();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("An error occurred when showing a toast notification regarding available updates");
                        Logger.Warn(ex);
                    }
                }
            }

            if (!Settings.Get("DisableAutoCheckforUpdates") && !ManualCheck)
            {
                long waitTime = 3600;
                try
                {
                    waitTime = long.Parse(Settings.GetValue("UpdatesCheckInterval"));
                    Logger.Debug($"Starting check for updates wait interval with waitTime={waitTime}");
                }
                catch
                {
                    Logger.Debug("Invalid value for UpdatesCheckInterval, using default value of 3600 seconds");
                }
                await Task.Delay(TimeSpan.FromSeconds(waitTime));
                _ = LoadPackages(ManualCheck: false);
            }
        }

        public void UpdateAll()
        {
            foreach (UpgradablePackage package in Packages)
            {
                MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package));
            }
        }

        public void FilterPackages(string query, bool StillLoading = false)
        {
            if (!Initialized)
                return;

            FilteredPackages.Clear();

            List<ManagerSource> VisibleSources = new();
            List<PackageManager> VisibleManagers = new();

            if (SourcesTreeView.SelectedNodes.Count > 0)
            {
                foreach (TreeViewNode node in SourcesTreeView.SelectedNodes)
                {
                    if (NodesForSources.ContainsValue(node))
                        VisibleSources.Add(NodesForSources.First(x => x.Value == node).Key);
                    else if (RootNodeForManager.ContainsValue(node))
                        VisibleManagers.Add(RootNodeForManager.First(x => x.Value == node).Key);
                }
            }


            UpgradablePackage[] MatchingList;

            Func<string, string> CaseFunc;
            if (UpperLowerCaseCheckbox.IsChecked == true)
                CaseFunc = (x) => { return x; };
            else
                CaseFunc = (x) => { return x.ToLower(); };

            Func<string, string> CharsFunc;
            if (IgnoreSpecialCharsCheckbox.IsChecked == true)
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
                            x = x.Replace(InvalidChar, entry.Key);
                    }
                    return temp_x;
                };
            else
                CharsFunc = (x) => { return CaseFunc(x); };

            if (QueryIdRadio.IsChecked == true)
                MatchingList = Packages.Where(x => CharsFunc(x.Name).Contains(CharsFunc(query))).ToArray();
            else if (QueryNameRadio.IsChecked == true)
                MatchingList = Packages.Where(x => CharsFunc(x.Id).Contains(CharsFunc(query))).ToArray();
            else // QueryBothRadio.IsChecked == true
                MatchingList = Packages.Where(x => CharsFunc(x.Name).Contains(CharsFunc(query)) | CharsFunc(x.Id).Contains(CharsFunc(query))).ToArray();

            FilteredPackages.BlockSorting = true;
            foreach (UpgradablePackage match in MatchingList)
            {
                if (VisibleManagers.Contains(match.Manager) || VisibleSources.Contains(match.Source))
                    FilteredPackages.Add(match);
            }
            FilteredPackages.BlockSorting = false;
            FilteredPackages.Sort();
            UpdatePackageCount(StillLoading);
        }

        public void UpdatePackageCount(bool StillLoading = false)
        {
            if (FilteredPackages.Count() == 0)
            {
                if (!StillLoading)
                {
                    if (Packages.Count() == 0)
                    {
                        BackgroundText.Text = SourcesPlaceholderText.Text = CoreTools.AutoTranslated("Hooray! No updates were found.");
                        SourcesPlaceholderText.Text = CoreTools.Translate("Everything is up to date");
                        MainSubtitle.Text = CoreTools.Translate("Everything is up to date") + " " + CoreTools.Translate("(Last checked: {0})").Replace("{0}", LastChecked.ToString());
                    }
                    else
                    {
                        BackgroundText.Text = CoreTools.AutoTranslated("No results were found matching the input criteria");
                        SourcesPlaceholderText.Text = CoreTools.AutoTranslated("No packages were found");
                        MainSubtitle.Text = CoreTools.Translate("{0} packages were found, {1} of which match the specified filters.").Replace("{0}", Packages.Count.ToString()).Replace("{1}", (FilteredPackages.Count()).ToString()) + " " + CoreTools.Translate("(Last checked: {0})").Replace("{0}", LastChecked.ToString());
                    }
                    BackgroundText.Visibility = Visibility.Visible;
                }

            }
            else
            {
                BackgroundText.Visibility = Visibility.Collapsed;
                MainSubtitle.Text = CoreTools.Translate("{0} packages were found, {1} of which match the specified filters.").Replace("{0}", Packages.Count.ToString()).Replace("{1}", (FilteredPackages.Count()).ToString()) + " " + CoreTools.Translate("(Last checked: {0})").Replace("{0}", LastChecked.ToString());
            }

            MainApp.Instance.MainWindow.NavigationPage.UpdatesBadge.Visibility = Packages.Count() == 0 ? Visibility.Collapsed : Visibility.Visible;
            MainApp.Instance.MainWindow.NavigationPage.UpdatesBadge.Value = Packages.Count();
            try
            {
                MainApp.Instance.TooltipStatus.AvailableUpdates = Packages.Count();
            }
            catch (Exception) { }
        }

        public void SortPackages(string Sorter)
        {
            if (!Initialized)
                return;

            FilteredPackages.Descending = !FilteredPackages.Descending;
            FilteredPackages.SortingSelector = (a) => (a.GetType().GetProperty(Sorter).GetValue(a));
            FilteredPackages.Sort();

            if (FilteredPackages.Count > 0)
                PackageList.ScrollIntoView(FilteredPackages[0]);
        }

        public void LoadInterface()
        {
            if (!Initialized)
                return;

            InstantSearchCheckbox.IsChecked = Settings.Get(InstantSearchSettingString);

            MainTitle.Text = CoreTools.AutoTranslated("Software Updates");
            HeaderIcon.Glyph = "\uE895";
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
        }


        public void GenerateToolBar()
        {
            if (!Initialized)
                return;
            AppBarButton UpdateSelected = new();
            AppBarButton UpdateAsAdmin = new();
            AppBarButton UpdateSkipHash = new();
            AppBarButton UpdateInteractive = new();

            AppBarButton InstallationSettings = new();

            AppBarButton PackageDetails = new();
            AppBarButton SharePackage = new();

            AppBarButton SelectAll = new();
            AppBarButton SelectNone = new();

            AppBarButton IgnoreSelected = new();
            AppBarButton ManageIgnored = new();

            AppBarButton HelpButton = new();

            ToolBar.PrimaryCommands.Add(UpdateSelected);
            ToolBar.PrimaryCommands.Add(UpdateAsAdmin);
            ToolBar.PrimaryCommands.Add(UpdateSkipHash);
            ToolBar.PrimaryCommands.Add(UpdateInteractive);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(InstallationSettings);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(PackageDetails);
            ToolBar.PrimaryCommands.Add(SharePackage);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(SelectAll);
            ToolBar.PrimaryCommands.Add(SelectNone);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(IgnoreSelected);
            ToolBar.PrimaryCommands.Add(ManageIgnored);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(HelpButton);

            Dictionary<AppBarButton, string> Labels = new()
            { // Entries with a trailing space are collapsed
              // Their texts will be used as the tooltip
                { UpdateSelected,       CoreTools.Translate("Update selected packages") },
                { UpdateAsAdmin,        " " + CoreTools.Translate("Update as administrator") },
                { UpdateSkipHash,       " " + CoreTools.Translate("Skip integrity checks") },
                { UpdateInteractive,    " " + CoreTools.Translate("Interactive update") },
                { InstallationSettings, " " + CoreTools.Translate("Installation options") },
                { PackageDetails,       " " + CoreTools.Translate("Package details") },
                { SharePackage,         " " + CoreTools.Translate("Share") },
                { SelectAll,            " " + CoreTools.Translate("Select all") },
                { SelectNone,           " " + CoreTools.Translate("Clear selection") },
                { IgnoreSelected,       CoreTools.Translate("Ignore selected packages") },
                { ManageIgnored,        CoreTools.Translate("Manage ignored updates") },
                { HelpButton,           CoreTools.Translate("Help") }
            };

            foreach (AppBarButton toolButton in Labels.Keys)
            {
                toolButton.IsCompact = Labels[toolButton][0] == ' ';
                if (toolButton.IsCompact)
                    toolButton.LabelPosition = CommandBarLabelPosition.Collapsed;
                toolButton.Label = Labels[toolButton].Trim();
            }

            Dictionary<AppBarButton, string> Icons = new()
            {
                { UpdateSelected,       "menu_updates" },
                { UpdateAsAdmin,        "runasadmin" },
                { UpdateSkipHash,       "checksum" },
                { UpdateInteractive,    "interactive" },
                { InstallationSettings, "options" },
                { PackageDetails,       "info" },
                { SharePackage,         "share" },
                { SelectAll,            "selectall" },
                { SelectNone,           "selectnone" },
                { IgnoreSelected,       "pin" },
                { ManageIgnored,        "clipboard_list" },
                { HelpButton,           "help" }
            };

            foreach (AppBarButton toolButton in Icons.Keys)
                toolButton.Icon = new LocalIcon(Icons[toolButton]);


            PackageDetails.Click += (s, e) =>
            {
                if (PackageList.SelectedItem != null)
                    _ = MainApp.Instance.MainWindow.NavigationPage.ShowPackageDetails(PackageList.SelectedItem as Package, OperationType.Update);
            };

            HelpButton.Click += (s, e) => { MainApp.Instance.MainWindow.NavigationPage.ShowHelp(); };

            InstallationSettings.Click += async (s, e) =>
            {
                if (PackageList.SelectedItem != null && await MainApp.Instance.MainWindow.NavigationPage.ShowInstallationSettingsForPackageAndContinue(PackageList.SelectedItem as Package, OperationType.Update))
                    MainApp.Instance.AddOperationToList(new UpdatePackageOperation(PackageList.SelectedItem as UpgradablePackage));
            };

            ManageIgnored.Click += async (s, e) => { await MainApp.Instance.MainWindow.NavigationPage.ManageIgnoredUpdatesDialog(); };
            IgnoreSelected.Click += async (s, e) =>
            {
                foreach (UpgradablePackage package in FilteredPackages.ToArray()) if (package.IsChecked)
                        await package.AddToIgnoredUpdatesAsync();
            };

            UpdateSelected.Click += (s, e) =>
            {
                foreach (UpgradablePackage package in FilteredPackages.ToArray()) if (package.IsChecked)
                        MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package));
            };
            UpdateAsAdmin.Click += (s, e) =>
            {
                foreach (UpgradablePackage package in FilteredPackages.ToArray()) if (package.IsChecked)
                        MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package,
                            new InstallationOptions(package) { RunAsAdministrator = true }));
            };
            UpdateSkipHash.Click += (s, e) =>
            {
                foreach (UpgradablePackage package in FilteredPackages.ToArray()) if (package.IsChecked)
                        MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package,
                            new InstallationOptions(package) { SkipHashCheck = true }));
            };
            UpdateInteractive.Click += (s, e) =>
            {
                foreach (UpgradablePackage package in FilteredPackages.ToArray()) if (package.IsChecked)
                        MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package,
                            new InstallationOptions(package) { InteractiveInstallation = true }));
            };

            SharePackage.Click += (s, e) =>
            {
                if (PackageList.SelectedItem != null)
                    MainApp.Instance.MainWindow.SharePackage(PackageList.SelectedItem as Package);
            };

            SelectAll.Click += (s, e) => { SelectAllItems(); };
            SelectNone.Click += (s, e) => { ClearItemSelection(); };

        }
        private void MenuDetails_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            _ = MainApp.Instance.MainWindow.NavigationPage.ShowPackageDetails(PackageList.SelectedItem as Package, OperationType.Update);
        }

        private void MenuShare_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            MainApp.Instance.MainWindow.SharePackage(PackageList.SelectedItem as UpgradablePackage);
        }

        private void MenuInstall_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            MainApp.Instance.AddOperationToList(new UpdatePackageOperation(PackageList.SelectedItem as UpgradablePackage));
        }

        private void MenuSkipHash_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            MainApp.Instance.AddOperationToList(new UpdatePackageOperation(PackageList.SelectedItem as UpgradablePackage,
                new InstallationOptions(PackageList.SelectedItem as UpgradablePackage) { SkipHashCheck = true }));
        }

        private void MenuInteractive_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            MainApp.Instance.AddOperationToList(new UpdatePackageOperation(PackageList.SelectedItem as UpgradablePackage,
                new InstallationOptions(PackageList.SelectedItem as UpgradablePackage) { InteractiveInstallation = true }));
        }

        private void MenuAsAdmin_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            MainApp.Instance.AddOperationToList(new UpdatePackageOperation(PackageList.SelectedItem as UpgradablePackage,
                new InstallationOptions(PackageList.SelectedItem as UpgradablePackage) { RunAsAdministrator = true }));
        }

        private void MenuUpdateAfterUninstall_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            MainApp.Instance.AddOperationToList(new UninstallPackageOperation(PackageList.SelectedItem as UpgradablePackage));
            MainApp.Instance.AddOperationToList(new InstallPackageOperation(PackageList.SelectedItem as UpgradablePackage));
        }

        private void MenuUninstall_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            MainApp.Instance.AddOperationToList(new UninstallPackageOperation(PackageList.SelectedItem as UpgradablePackage));
        }

        private void MenuIgnorePackage_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            _ = (PackageList.SelectedItem as UpgradablePackage).AddToIgnoredUpdatesAsync();
        }

        private void MenuSkipVersion_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            _ = (PackageList.SelectedItem as UpgradablePackage).AddToIgnoredUpdatesAsync((PackageList.SelectedItem as UpgradablePackage).NewVersion);
        }

        private void SelectAllSourcesButton_Click(object sender, RoutedEventArgs e)
        {
            SourcesTreeView.SelectAll();
        }

        private void ClearSourceSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            SourcesTreeView.SelectedItems.Clear();
            FilterPackages(QueryBlock.Text.Trim());
        }

        private async void MenuInstallSettings_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || (PackageList.SelectedItem as UpgradablePackage) == null)
                return;

            if (await MainApp.Instance.MainWindow.NavigationPage.ShowInstallationSettingsForPackageAndContinue(PackageList.SelectedItem as UpgradablePackage, OperationType.Update))
                MainApp.Instance.AddOperationToList(new UpdatePackageOperation(PackageList.SelectedItem as UpgradablePackage));
        }

        private void SelectAllItems()
        {
            foreach (UpgradablePackage package in FilteredPackages)
                package.IsChecked = true;
            AllSelected = true;
        }

        private void ClearItemSelection()
        {
            foreach (UpgradablePackage package in FilteredPackages)
                package.IsChecked = false;
            AllSelected = false;
        }
        public void RemoveCorrespondingPackages(Package foreignPackage)
        {
            foreach (UpgradablePackage package in Packages.ToArray())
                if (package == foreignPackage || package.Equals(foreignPackage))
                {
                    Packages.Remove(package);
                    package.Tag = PackageTag.Default;
                    if (FilteredPackages.Contains(package))
                        FilteredPackages.Remove(package);
                }
            UpdatePackageCount();
        }

        public void UpdatePackageForId(string id)
        {
            foreach (UpgradablePackage package in Packages)
            {
                if (package.Id == id)
                {
                    MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package));
                    Logger.Info($"[WIDGETS] Updating package with id {id}");
                    break;
                }
            }
            Logger.Warn($"[WIDGETS] No package with id={id} was found");
        }

        public void UpdateAllPackages()
        {
            Logger.Info("[WIDGETS] Updating all packages");
            foreach (UpgradablePackage package in Packages)
                if (package.Tag != PackageTag.OnQueue && package.Tag != PackageTag.BeingProcessed)
                    MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package));
        }

        public void UpdateAllPackagesForManager(string manager)
        {
            Logger.Info($"[WIDGETS] Updating all packages for manager {manager}");
            foreach (UpgradablePackage package in Packages)
                if (package.Tag != PackageTag.OnQueue && package.Tag != PackageTag.BeingProcessed)
                    if (package.Manager.Name == manager)
                        MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package));
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

    }
}

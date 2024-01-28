using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic;
using ModernWindow.Essentials;
using ModernWindow.Interface.Widgets;
using ModernWindow.PackageEngine;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface
{

    public partial class SoftwareUpdatesPage : Page
    {
        public ObservableCollection<UpgradablePackage> Packages = new ObservableCollection<UpgradablePackage>();
        public SortableObservableCollection<UpgradablePackage> FilteredPackages = new SortableObservableCollection<UpgradablePackage>() { SortingSelector = (a) => (a.Name)};
        protected List<PackageManager> UsedManagers = new();
        protected Dictionary<PackageManager, List<ManagerSource>> UsedSourcesForManager = new();
        protected Dictionary<PackageManager, TreeViewNode> RootNodeForManager = new();
        protected Dictionary<ManagerSource, TreeViewNode> NodesForSources = new();
        protected MainAppBindings bindings = MainAppBindings.Instance;

        protected TranslatedTextBlock MainTitle;
        protected TranslatedTextBlock MainSubtitle;
        protected ListView PackageList;
        protected ProgressBar LoadingProgressBar;
        protected MenuFlyout ContextMenu;

        private bool IsDescending = true;
        private bool Initialized = false;

        public string InstantSearchSettingString = "DisableInstantSearchUpdatesTab";
        public SoftwareUpdatesPage()
        {
            this.InitializeComponent();
            MainTitle = __main_title;
            MainSubtitle = __main_subtitle;
            PackageList = __package_list;
            LoadingProgressBar = __loading_progressbar;
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            Initialized = true;
            ReloadButton.Click += async (s, e) => { await LoadPackages(); } ;
            FindButton.Click += (s, e) => { FilterPackages(QueryBlock.Text); };
            QueryBlock.TextChanged += (s, e) => { if (InstantSearchCheckbox.IsChecked == true) FilterPackages(QueryBlock.Text); };
            QueryBlock.KeyUp += (s, e) => { if (e.Key == Windows.System.VirtualKey.Enter) FilterPackages(QueryBlock.Text); };
            PackageList.ItemClick += (s, e) => { if (e.ClickedItem != null) Console.WriteLine("Clicked item " + (e.ClickedItem as Package).Id); };
            GenerateToolBar();
            LoadInterface();
            _ = LoadPackages();
        }

        protected void AddPackageToSourcesList(UpgradablePackage package)
        {
            if (!Initialized)
                return;

            var source = package.Source;
            if (!UsedManagers.Contains(source.Manager))
            {
                UsedManagers.Add(source.Manager);
                TreeViewNode Node;
                Node = new TreeViewNode() { Content = source.Manager.Name + " ", IsExpanded=true };
                SourcesTreeView.RootNodes.Add(Node);
                SourcesTreeView.SelectedNodes.Add(Node);
                RootNodeForManager.Add(source.Manager, Node);
                UsedSourcesForManager.Add(source.Manager, new List<ManagerSource>());
                SourcesPlaceholderText.Visibility = Visibility.Collapsed;
            }

            if ((!UsedSourcesForManager.ContainsKey(source.Manager)  || !UsedSourcesForManager[source.Manager].Contains(source)) && source.Manager.Capabilities.SupportsCustomSources)
            {
                UsedSourcesForManager[source.Manager].Add(source);
                var item = new TreeViewNode() { Content = source.Name + " " };
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
            bindings.SetSettings(InstantSearchSettingString, InstantSearchCheckbox.IsChecked == false);
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

        public async Task LoadPackages()
        {
            if (!Initialized)
                return;

            if (LoadingProgressBar.Visibility == Visibility.Visible)
                return; // If already loading, don't load again
            
            MainSubtitle.Text = "Loading...";
            BackgroundText.Text = "Loading...";
            LoadingProgressBar.Visibility = Visibility.Visible;
            SourcesPlaceholderText.Visibility = Visibility.Visible;
            SourcesPlaceholderText.Text = "Loading...";

            Packages.Clear();
            FilteredPackages.Clear();
            UsedManagers.Clear();
            SourcesTreeView.RootNodes.Clear();
            UsedSourcesForManager.Clear();
            RootNodeForManager.Clear();
            NodesForSources.Clear();

            await Task.Delay(100);

            var tasks = new List<Task<UpgradablePackage[]>>();

            foreach (var manager in bindings.App.PackageManagerList)
            {
                if (manager.IsEnabled() && manager.Status.Found)
                {
                    var task = manager.GetAvailableUpdates();
                    tasks.Add(task);
                }
            }

            foreach (var task in tasks)
            {
                if (!task.IsCompleted)
                    await task;
                foreach (UpgradablePackage package in task.Result)
                {
                    Packages.Add(package);
                    BackgroundText.Visibility = Visibility.Collapsed;
                    AddPackageToSourcesList(package);
                }
            }

            FilterPackages(QueryBlock.Text);

            LoadingProgressBar.Visibility = Visibility.Collapsed;
        }

        public void FilterPackages(string query)
        {
            if (!Initialized)
                return;

            FilteredPackages.Clear();
            bool AllSourcesVisible = true;
            List<ManagerSource> VisibleSources = new();
            List<PackageManager> VisibleManagers = new();

            if (SourcesTreeView.SelectedNodes.Count > 0)
            {
                AllSourcesVisible = false;
                foreach (var node in SourcesTreeView.SelectedNodes)
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
                CharsFunc = (x) => { 
                    var temp_x = CaseFunc(x).Replace("-", "").Replace("_", "").Replace(" ", "").Replace("@", "").Replace("\t", "").Replace(".", "").Replace(",", "").Replace(":", "");
                    foreach(var entry in new Dictionary<char, string>
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
                        foreach(char InvalidChar in entry.Value)
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
            foreach (var match in MatchingList)
            {
                if(AllSourcesVisible || VisibleManagers.Contains(match.Manager) || VisibleSources.Contains(match.Source))
                    FilteredPackages.Add(match);
            }
            FilteredPackages.BlockSorting = false;
            FilteredPackages.Sort();

            if(MatchingList.Count() == 0)
            {
                if (Packages.Count() == 0)
                {
                    BackgroundText.Text = SourcesPlaceholderText.Text = "Hooray! No updates were found.";
                    SourcesPlaceholderText.Text = "Everything is up to date";
                    MainSubtitle.Text = "Everything is up to date";
                }
                else
                {
                    BackgroundText.Text = "No results were found matching the input criteria";
                    SourcesPlaceholderText.Text = "No packages were found";
                    MainSubtitle.Text = bindings.Translate("{0} packages were found, {1} of which match the specified filters.").Replace("{0}", Packages.Count.ToString()).Replace("{1}", MatchingList.Length.ToString());
                }
                BackgroundText.Visibility = Visibility.Visible;
            }
            else
            {
                BackgroundText.Visibility = Visibility.Collapsed;
                MainSubtitle.Text = bindings.Translate("{0} packages were found, {1} of which match the specified filters.").Replace("{0}", Packages.Count.ToString()).Replace("{1}", MatchingList.Length.ToString());
            }
        }

        public void SortPackages(string Sorter)
        {
            if (!Initialized)
                return;

            FilteredPackages.Descending = !FilteredPackages.Descending;
            FilteredPackages.SortingSelector = (a) => (a.GetType().GetProperty(Sorter).GetValue(a));
            var Item = PackageList.SelectedItem;
            FilteredPackages.Sort();

            if (Item != null)
                PackageList.SelectedItem = Item;
                PackageList.ScrollIntoView(Item);
        }

        public void LoadInterface()
        {
            if (!Initialized)
                return;
            MainTitle.Text = "Software Updates";
            //HeaderImage.Source = new BitmapImage(new Uri("ms-appx:///wingetui/resources/desktop_download.png"));
            HeaderIcon.Glyph = "\uE895";
            HeaderIcon.FontWeight = new Windows.UI.Text.FontWeight(700);
            CheckboxHeader.Content = " ";
            NameHeader.Content = bindings.Translate("Package Name");
            IdHeader.Content = bindings.Translate("Package ID");
            VersionHeader.Content = bindings.Translate("Version");
            NewVersionHeader.Content = bindings.Translate("New version");
            SourceHeader.Content = bindings.Translate("Source");

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
            var InstallSelected = new AppBarButton();
            var InstallAsAdmin = new AppBarButton();
            var InstallSkipHash = new AppBarButton();
            var InstallInteractive = new AppBarButton();

            var PackageDetails = new AppBarButton();
            var SharePackage = new AppBarButton();

            var SelectAll = new AppBarButton();
            var SelectNone = new AppBarButton();

            var IgnoreSelected = new AppBarButton();
            var ManageIgnored = new AppBarButton();

            var HelpButton = new AppBarButton();

            ToolBar.PrimaryCommands.Add(InstallSelected);
            ToolBar.PrimaryCommands.Add(InstallAsAdmin);
            ToolBar.PrimaryCommands.Add(InstallSkipHash);
            ToolBar.PrimaryCommands.Add(InstallInteractive);
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

            var Labels = new Dictionary<AppBarButton, string>
            { // Entries with a trailing space are collapsed
              // Their texts will be used as the tooltip
                { InstallSelected,      "Update selected packages" },
                { InstallAsAdmin,       " Update as administrator" },
                { InstallSkipHash,      " Skip integrity checks" },
                { InstallInteractive,   " Interactive update" },
                { PackageDetails,       " Package details" },
                { SharePackage,         " Share" },
                { SelectAll,            " Select all" },
                { SelectNone,           " Clear selection" },
                { IgnoreSelected,       "Ignore selected packages" },
                { ManageIgnored,        "Manage ignored updates" },
                { HelpButton,           "Help" }
            };

            foreach(var toolButton in Labels.Keys)
            {
                toolButton.IsCompact = Labels[toolButton][0] == ' ';
                if(toolButton.IsCompact)
                    toolButton.LabelPosition = CommandBarLabelPosition.Collapsed;
                toolButton.Label = bindings.Translate(Labels[toolButton].Trim());
            }

            var Icons = new Dictionary<AppBarButton, string>
            {
                { InstallSelected,      "menu_updates" },
                { InstallAsAdmin,       "runasadmin" },
                { InstallSkipHash,      "checksum" },
                { InstallInteractive,   "interactive" },
                { PackageDetails,       "info" },
                { SharePackage,         "share" },
                { SelectAll,            "selectall" },
                { SelectNone,           "selectnone" },
                { IgnoreSelected,       "pin" },
                { ManageIgnored,        "blacklist" },
                { HelpButton,           "help" }
            };

            foreach (var toolButton in Icons.Keys)
                toolButton.Icon = new LocalIcon(Icons[toolButton]);

            InstallSelected.IsEnabled = false;
            InstallAsAdmin.IsEnabled = false;
            InstallSkipHash.IsEnabled = false;
            InstallInteractive.IsEnabled = false;
            PackageDetails.IsEnabled = false;
            IgnoreSelected.IsEnabled = false;
            ManageIgnored.IsEnabled = false;
            HelpButton.IsEnabled = false;

            SharePackage.Click += (s, e) => { bindings.App.mainWindow.SharePackage(PackageList.SelectedItem as Package); };

            SelectAll.Click += (s, e) => { foreach (var package in FilteredPackages) package.IsChecked = true; FilterPackages(QueryBlock.Text); };
            SelectNone.Click += (s, e) => { foreach (var package in FilteredPackages) package.IsChecked = false; FilterPackages(QueryBlock.Text); };

        }
        private void MenuDetails_Invoked(object sender, Package package)
        {
            if (!Initialized)
                return;
        }

        private void MenuShare_Invoked(object sender, Package package)
        {
            if (!Initialized)
                return;
            bindings.App.mainWindow.SharePackage(package);
        }

        private void MenuInstall_Invoked(object sender, Package package)
        {
            if (!Initialized)
                return;
        }

        private void MenuSkipHash_Invoked(object sender, Package package)
        {
            if (!Initialized)
                return;
        }

        private void MenuInteractive_Invoked(object sender, Package package)
        {
            if (!Initialized)
                return;
        }

        private void MenuAsAdmin_Invoked(object sender, Package package)
        {
            if (!Initialized)
                return;
        }

        private void MenuUpdateAfterUninstall_Invoked(object sender, Package e)
        {

        }

        private void MenuUninstall_Invoked(object sender, Package e)
        {

        }

        private void MenuIgnorePackage_Invoked(object sender, Package e)
        {

        }

        private void MenuSkipVersion_Invoked(object sender, Package e)
        {

        }
    }
}

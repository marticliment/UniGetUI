using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using ModernWindow.Essentials;
using ModernWindow.Interface.Widgets;
using ModernWindow.PackageEngine;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.UI.Core;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using static ModernWindow.Interface.BundledPackage;
using System.Xml.Serialization;
using System.Text;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface
{

    public class BundledPackage
    {
        public enum BundleFormatType
        {
            JSON,
            YAML,
            XML
        }

        public class __serializable_exportable_packages
        {
            public double export_version { get; set; } = 2.0;
            public List<Package.__serializable_bundled_package_v1> packages { get; set; } = new();
            public string incompatible_packages_info { get; set; } = "Incompatible packages cannot be installed from WingetUI, but they have been listed here for logging purposes.";
            public List<Package.__serializable_incompatible_package_v1> incompatible_packages { get; set; } = new();

        }

        public AppTools bindings = AppTools.Instance;
        public Package Package { get; }
        public InstallationOptions Options { get; }

        public BundledPackage(Package package)
        {
            Package = package;
            Options = new InstallationOptions(package);
        }

        public void ShowOptions(object sender, RoutedEventArgs e)
        {
            _ = bindings.App.mainWindow.NavigationPage.ShowInstallationSettingsForPackageAndContinue(Package, OperationType.None);
        }

        public void RemoveFromList(object sender, RoutedEventArgs e)
        {
            bindings.App.mainWindow.NavigationPage.BundlesPage.Packages.Remove(this);
            bindings.App.mainWindow.NavigationPage.BundlesPage.FilteredPackages.Remove(this);
            bindings.App.mainWindow.NavigationPage.BundlesPage.UpdateCount();
        }

    }

    public partial class PackageBundlePage : Page
    {
        public ObservableCollection<BundledPackage> Packages = new();
        public SortableObservableCollection<BundledPackage> FilteredPackages = new() { SortingSelector = (a) => (a.Package.Name) };
        protected List<PackageManager> UsedManagers = new();
        protected Dictionary<PackageManager, List<ManagerSource>> UsedSourcesForManager = new();
        protected Dictionary<PackageManager, TreeViewNode> RootNodeForManager = new();
        protected Dictionary<ManagerSource, TreeViewNode> NodesForSources = new();
        protected AppTools bindings = AppTools.Instance;

        protected TranslatedTextBlock MainTitle;
        protected TranslatedTextBlock MainSubtitle;
        protected ListView PackageList;
        protected ProgressBar LoadingProgressBar;
        protected MenuFlyout ContextMenu;

        private bool IsDescending = true;
        private bool Initialized = false;
        private bool AllSelected = false;
        TreeViewNode LocalPackagesNode;

        public string InstantSearchSettingString = "DisableInstantSearchInstalledTab";
        public PackageBundlePage()
        {
            InitializeComponent();
            MainTitle = __main_title;
            MainSubtitle = __main_subtitle;
            PackageList = __package_list;
            LoadingProgressBar = __loading_progressbar;
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            LocalPackagesNode = new TreeViewNode() { Content = bindings.Translate("Local"), IsExpanded = false };
            Initialized = true;
            ReloadButton.Visibility = Visibility.Collapsed;
            FindButton.Click += (s, e) => { FilterPackages(QueryBlock.Text); };
            QueryBlock.TextChanged += (s, e) => { if (InstantSearchCheckbox.IsChecked == true) FilterPackages(QueryBlock.Text); };
            QueryBlock.KeyUp += (s, e) => { if (e.Key == Windows.System.VirtualKey.Enter) FilterPackages(QueryBlock.Text); };

            SourcesTreeView.Tapped += (s, e) =>
            {
                if (e.OriginalSource != null && (e.OriginalSource as FrameworkElement).DataContext != null)
                {
                    AppTools.Log(e);
                    AppTools.Log(e.OriginalSource);
                    AppTools.Log(e.OriginalSource as FrameworkElement);
                    AppTools.Log((e.OriginalSource as FrameworkElement).DataContext);
                    if ((e.OriginalSource as FrameworkElement).DataContext is TreeViewNode)
                    {
                        TreeViewNode node = (e.OriginalSource as FrameworkElement).DataContext as TreeViewNode;
                        if (node == null)
                            return;
                        if (SourcesTreeView.SelectedNodes.Contains(node))
                            SourcesTreeView.SelectedNodes.Remove(node);
                        else
                            SourcesTreeView.SelectedNodes.Add(node);
                    }
                    else
                    {
                        AppTools.Log((e.OriginalSource as FrameworkElement).DataContext.GetType());
                    }
                }
            };

            PackageList.DoubleTapped += (s, e) =>
            {
                _ = bindings.App.mainWindow.NavigationPage.ShowPackageDetails((PackageList.SelectedItem as BundledPackage).Package, OperationType.None);
            };

            PackageList.RightTapped += (s, e) =>
            {
                if (e.OriginalSource is FrameworkElement element)
                {
                    try
                    {
                        if (element.DataContext != null && element.DataContext is BundledPackage package)
                        {
                            PackageList.SelectedItem = package;
                            PackageContextMenu.ShowAt(PackageList, e.GetPosition(PackageList));
                        }
                    }
                    catch (Exception ex)
                    {
                        AppTools.Log(ex);
                    }
                }
            };

            PackageList.KeyUp += async (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter && PackageList.SelectedItem != null)
                {
                    if (InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down))
                        _ = bindings.App.mainWindow.NavigationPage.ShowInstallationSettingsForPackageAndContinue((PackageList.SelectedItem as BundledPackage).Package, OperationType.None);
                    else
                        _ = bindings.App.mainWindow.NavigationPage.ShowPackageDetails((PackageList.SelectedItem as BundledPackage).Package, OperationType.None);
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
                    ((PackageList.SelectedItem as BundledPackage).Package).IsChecked = !((PackageList.SelectedItem as BundledPackage).Package).IsChecked;
                }
            };

            GenerateToolBar();
            LoadInterface();
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
                Node = new TreeViewNode() { Content = source.Manager.Name + " ", IsExpanded = false };
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
                TreeViewNode item = new() { Content = source.Name };
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

        public void ClearList()
        {
            Packages.Clear();
            FilteredPackages.Clear();
            UsedManagers.Clear();
            SourcesTreeView.RootNodes.Clear();
            UsedSourcesForManager.Clear();
            RootNodeForManager.Clear();
            NodesForSources.Clear();
            LocalPackagesNode.Children.Clear();
            BackgroundText.Text = "No packages have been added yet";
            BackgroundText.Visibility = Visibility.Visible;
            FilterPackages("");
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

            BundledPackage[] MatchingList;

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
                MatchingList = Packages.Where(x => CharsFunc(x.Package.Name).Contains(CharsFunc(query))).ToArray();
            else if (QueryNameRadio.IsChecked == true)
                MatchingList = Packages.Where(x => CharsFunc(x.Package.Id).Contains(CharsFunc(query))).ToArray();
            else // QueryBothRadio.IsChecked == true
                MatchingList = Packages.Where(x => CharsFunc(x.Package.Name).Contains(CharsFunc(query)) | CharsFunc(x.Package.Id).Contains(CharsFunc(query))).ToArray();

            FilteredPackages.BlockSorting = true;
            int HiddenPackagesDueToSource = 0;
            foreach (BundledPackage match in MatchingList)
            {
                if ( (VisibleManagers.Contains(match.Package.Manager) && match.Package.Manager != bindings.App.Winget) || VisibleSources.Contains(match.Package.Source))
                    FilteredPackages.Add(match);
                else
                    HiddenPackagesDueToSource++;
            }
            FilteredPackages.BlockSorting = false;
            FilteredPackages.Sort();

            if (MatchingList.Count() == 0)
            {
                if (!StillLoading)
                {
                    if (Packages.Count() == 0)
                    {
                        BackgroundText.Text = SourcesPlaceholderText.Text = "We couldn't find any package";
                        SourcesPlaceholderText.Text = "No sources found";
                        MainSubtitle.Text = "No packages found";
                    }
                    else
                    {
                        BackgroundText.Text = "No results were found matching the input criteria";
                        SourcesPlaceholderText.Text = "No packages were found";
                        MainSubtitle.Text = bindings.Translate("{0} packages were found, {1} of which match the specified filters.").Replace("{0}", Packages.Count.ToString()).Replace("{1}", (MatchingList.Length - HiddenPackagesDueToSource).ToString());
                    }
                    BackgroundText.Visibility = Visibility.Visible;
                }

            }
            else
            {
                BackgroundText.Visibility = Visibility.Collapsed;
                MainSubtitle.Text = bindings.Translate("{0} packages were found, {1} of which match the specified filters.").Replace("{0}", Packages.Count.ToString()).Replace("{1}", (MatchingList.Length - HiddenPackagesDueToSource).ToString());
            }
        }

        public void SortPackages(string Sorter)
        {
            if (!Initialized)
                return;

            FilteredPackages.Descending = !FilteredPackages.Descending;
            FilteredPackages.SortingSelector = (a) => (a.GetType().GetProperty(Sorter).GetValue(a));
            object Item = PackageList.SelectedItem;
            FilteredPackages.Sort();

            if (Item != null)
                PackageList.SelectedItem = Item;
            PackageList.ScrollIntoView(Item);
        }

        public void LoadInterface()
        {
            if (!Initialized)
                return;
            MainTitle.Text = "Package Bundles";
            HeaderIcon.Glyph = "\uF133";
            CheckboxHeader.Content = " ";
            NameHeader.Content = bindings.Translate("Package Name");
            IdHeader.Content = bindings.Translate("Package ID");
            VersionHeader.Content = bindings.Translate("Version");
            SourceHeader.Content = bindings.Translate("Source");

            CheckboxHeader.Click += (s, e) => { SortPackages("IsCheckedAsString"); };
            NameHeader.Click += (s, e) => { SortPackages("Name"); };
            IdHeader.Click += (s, e) => { SortPackages("Id"); };
            VersionHeader.Click += (s, e) => { SortPackages("VersionAsFloat"); };
            SourceHeader.Click += (s, e) => { SortPackages("SourceAsString"); };
        }

        public void UpdateCount()
        {
            BackgroundText.Visibility = Packages.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            bindings.App.mainWindow.NavigationPage.BundleBadge.Value = Packages.Count;
            bindings.App.mainWindow.NavigationPage.BundleBadge.Visibility = Packages.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        public void GenerateToolBar()
        {
            if (!Initialized)
                return;
            AppBarButton InstallPackages = new();
            AppBarButton OpenBundle = new();

            AppBarButton RemoveSelected = new();

            AppBarButton ExportBundle = new();

            AppBarButton PackageDetails = new();
            AppBarButton SharePackage = new();

            AppBarButton SelectAll = new();
            AppBarButton SelectNone = new();

            AppBarButton HelpButton = new();

            ToolBar.PrimaryCommands.Add(OpenBundle);
            ToolBar.PrimaryCommands.Add(ExportBundle);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(InstallPackages);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(SelectAll);
            ToolBar.PrimaryCommands.Add(SelectNone);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(RemoveSelected);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(PackageDetails);
            ToolBar.PrimaryCommands.Add(SharePackage);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(HelpButton);

            Dictionary<AppBarButton, string> Labels = new()
            { // Entries with a trailing space are collapsed
              // Their texts will be used as the tooltip
                { InstallPackages,        "Install selection" },
                { OpenBundle,             "Open existing bundle" },
                { RemoveSelected,         "Remove selection from bundle" },
                { ExportBundle,           "Save bundle as" },
                { PackageDetails,         " Package details" },
                { SharePackage,           " Share" },
                { SelectAll,              " Select all" },
                { SelectNone,             " Clear selection" },
                { HelpButton,             "Help" }
            };

            foreach (AppBarButton toolButton in Labels.Keys)
            {
                toolButton.IsCompact = Labels[toolButton][0] == ' ';
                if (toolButton.IsCompact)
                    toolButton.LabelPosition = CommandBarLabelPosition.Collapsed;
                toolButton.Label = bindings.Translate(Labels[toolButton].Trim());
            }

            Dictionary<AppBarButton, string> Icons = new()
            {
                { InstallPackages,        "newversion" },
                { OpenBundle,             "openfolder" },
                { RemoveSelected,         "menu_uninstall" },
                { ExportBundle,           "save" },
                { PackageDetails,         "info" },
                { SharePackage,           "share" },
                { SelectAll,              "selectall" },
                { SelectNone,             "selectnone" },
                { HelpButton,             "help" }
            };

            foreach (AppBarButton toolButton in Icons.Keys)
                toolButton.Icon = new LocalIcon(Icons[toolButton]);

            PackageDetails.Click += (s, e) =>
            {
                if (PackageList.SelectedItem != null)
                    _ = bindings.App.mainWindow.NavigationPage.ShowPackageDetails((PackageList.SelectedItem as BundledPackage).Package, OperationType.None);
            };

            HelpButton.Click += (s, e) => { bindings.App.mainWindow.NavigationPage.ShowHelp(); };



            RemoveSelected.Click += (s, e) =>
            {
                foreach (BundledPackage package in FilteredPackages.ToArray()) 
                    if (package.Package.IsChecked)
                    {
                        FilteredPackages.Remove(package);
                        Packages.Remove(package);
                    }
                UpdateCount();
            };

            InstallPackages.Click += (s, e) => {
                foreach (BundledPackage package in FilteredPackages.ToArray())
                    if (package.Package.IsChecked)
                        bindings.AddOperationToList(new InstallPackageOperation(package.Package));
            };

            OpenBundle.Click += (s, e) => {  };

            ExportBundle.Click += (s, e) => {
                SaveFile();
            };

            SharePackage.Click += (s, e) => { bindings.App.mainWindow.SharePackage((PackageList.SelectedItem as BundledPackage).Package); };

            SelectAll.Click += (s, e) => { SelectAllItems(); };
            SelectNone.Click += (s, e) => { ClearItemSelection(); };

        }

        public async void ConfirmAndUninstall(Package package, InstallationOptions options)
        {
            ContentDialog dialog = new();

            dialog.XamlRoot = XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = bindings.Translate("Are you sure?");
            dialog.PrimaryButtonText = bindings.Translate("No");
            dialog.SecondaryButtonText = bindings.Translate("Yes");
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.Content = bindings.Translate("Do you really want to uninstall {0}?").Replace("{0}", package.Name);

            if (await bindings.App.mainWindow.ShowDialog(dialog) == ContentDialogResult.Secondary)
                bindings.AddOperationToList(new UninstallPackageOperation(package, options));

        }

        private void MenuRemoveFromList_Invoked(object sender, RoutedEventArgs package)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;

            (PackageList.SelectedItem as BundledPackage).RemoveFromList(sender, package);

        }

        private void MenuShare_Invoked(object sender, RoutedEventArgs package)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            bindings.App.mainWindow.SharePackage(((PackageList.SelectedItem as BundledPackage).Package));
        }

        private void MenuDetails_Invoked(object sender, RoutedEventArgs package)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            _ = bindings.App.mainWindow.NavigationPage.ShowPackageDetails((PackageList.SelectedItem as BundledPackage).Package, OperationType.None);
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

        private void MenuInstallSettings_Invoked(object sender, RoutedEventArgs e)
        {
            if ((PackageList.SelectedItem as BundledPackage).Package != null)
                _ = bindings.App.mainWindow.NavigationPage.ShowInstallationSettingsForPackageAndContinue((PackageList.SelectedItem as BundledPackage).Package, OperationType.None);
        }

        private void SelectAllItems()
        {
            foreach (BundledPackage package in FilteredPackages.ToArray())
                package.Package.IsChecked = true;
            AllSelected = true;
        }

        private void ClearItemSelection()
        {
            foreach (BundledPackage package in FilteredPackages.ToArray())
                package.Package.IsChecked = false;
            AllSelected = false;
        }

        public void AddPackage(Package foreignPackage)
        {
            Packages.Add(new BundledPackage(foreignPackage));
            AddPackageToSourcesList(foreignPackage);
            BackgroundText.Visibility = Packages.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            bindings.App.mainWindow.NavigationPage.BundleBadge.Value = Packages.Count;
            bindings.App.mainWindow.NavigationPage.BundleBadge.Visibility = Packages.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            FilterPackages(QueryBlock.Text.Trim());
        }

        public static Package[] GetPackagesFromJsonString(string JsonContents)
        {
            var Packages = new List<Package>();

            var Deserialized = JsonSerializer.Deserialize<BundledPackage.__serializable_exportable_packages>(JsonContents);

            return new Package[0];
        }

        public async void SaveFile()
        {
            try
            {
                // Get file 
                var picker = new Windows.Storage.Pickers.FileSavePicker();
                WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(AppTools.Instance.App.mainWindow));
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                picker.FileTypeChoices.Add("JSON", new List<string>() { ".json" });
                picker.FileTypeChoices.Add("YAML", new List<string>() { ".yaml" });
                picker.FileTypeChoices.Add("XML", new List<string>() { ".xml" });
                picker.SuggestedFileName = bindings.Translate("WingetUI package bundle");

                // Save file
                Windows.Storage.StorageFile file = await picker.PickSaveFileAsync();
                if (file != null)
                {

                    ContentDialog dialog = new();
                    dialog.XamlRoot = XamlRoot;
                    dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
                    dialog.Title = bindings.Translate("Please wait");
                    dialog.Content = new ProgressBar() { IsIndeterminate = true, Width = 300 };

                    _ = dialog.ShowAsync();

                    List<Package> packages = new List<Package>();
                    foreach (BundledPackage package in Packages)
                        packages.Add(package.Package);

                    BundleFormatType formatType;
                    if (file.FileType.ToLower() == ".yaml")
                        formatType = BundleFormatType.YAML;
                    else if (file.FileType.ToLower() == ".xml")
                        formatType = BundleFormatType.XML;
                    else
                        formatType = BundleFormatType.JSON;

                    await Windows.Storage.FileIO.WriteTextAsync(file, await GetBundleStringFromPackages(packages.ToArray(), formatType));

                    dialog.Hide();

                    // Launch file
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = "explorer.exe",
                        Arguments = @$"/select, ""{file.Path}"""
                    });

                }
            }
            catch (Exception ex)
            {
                AppTools.Log(ex);
            }
        }

        public async static Task<string> GetBundleStringFromPackages(Package[] packages, BundleFormatType formatType = BundleFormatType.JSON)
        {
            var exportable = new __serializable_exportable_packages();
            foreach (Package package in packages)
                if(package.Source.IsVirtualManager)
                    exportable.incompatible_packages.Add(package.AsSerializable_IncompatiblePackage());
                else
                    exportable.packages.Add(await package.AsSerializable_BundledPackage());

            AppTools.Log("Finished loading serializable objects. Serializing with format " + formatType.ToString());
            var ExportableData = "";
            
            if(formatType == BundleFormatType.JSON)
                ExportableData = JsonSerializer.Serialize<__serializable_exportable_packages>(exportable, new JsonSerializerOptions() { WriteIndented = true });
            else if (formatType == BundleFormatType.YAML)
            {
                var serializer = new SerializerBuilder()
                    .Build();
                ExportableData = serializer.Serialize(exportable);
            } else
            {
                var tempfile = Path.GetTempFileName();
                var writer = new StreamWriter(tempfile);
                var serializer = new XmlSerializer(typeof(__serializable_exportable_packages));
                serializer.Serialize(writer, exportable);
                writer.Close();
                ExportableData = await File.ReadAllTextAsync(tempfile);
                File.Delete(tempfile);

            }

            AppTools.Log("Finished serializing");

            return ExportableData;
        }
    }
}

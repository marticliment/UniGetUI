using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Xml.Serialization;
using ExternalLibraries.Pickers;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Classes;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Pages;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Classes;
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
    public partial class PackageBundlePage : Page, IPageWithKeyboardShortcuts
    {
        public ObservableCollection<BundledPackage> Packages = [];
        public SortableObservableCollection<BundledPackage> FilteredPackages = new() { SortingSelector = (a) => a.Package.Name };
        protected List<PackageManager> UsedManagers = [];
        protected Dictionary<PackageManager, List<ManagerSource>> UsedSourcesForManager = [];
        protected Dictionary<PackageManager, TreeViewNode> RootNodeForManager = [];
        protected Dictionary<ManagerSource, TreeViewNode> NodesForSources = [];

        protected TranslatedTextBlock MainTitle;
        protected TranslatedTextBlock MainSubtitle;
        public ListView PackageList;
        protected ProgressBar LoadingProgressBar;
        protected MenuFlyout? ContextMenu;

        private readonly bool Initialized;
        private bool AllSelected;
        private readonly TreeViewNode LocalPackagesNode;
        private int lastSavedWidth;

        public string InstantSearchSettingString = "DisableInstantSearchInstalledTab";
        public PackageBundlePage()
        {
            InitializeComponent();
            QueryBothRadio.IsChecked = true;
            QueryOptionsGroup.SelectedIndex = 2;
            MainTitle = __main_title;
            MainSubtitle = __main_subtitle;
            PackageList = __package_list;
            LoadingProgressBar = __loading_progressbar;
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            LocalPackagesNode = new TreeViewNode { Content = CoreTools.Translate("Local"), IsExpanded = false };
            Initialized = true;
            ReloadButton.Visibility = Visibility.Collapsed;
            FindButton.Click += (s, e) => { FilterPackages(QueryBlock.Text); };
            QueryBlock.TextChanged += (s, e) => { if (InstantSearchCheckbox.IsChecked == true) { FilterPackages(QueryBlock.Text); } };
            QueryBlock.KeyUp += (s, e) => { if (e.Key == Windows.System.VirtualKey.Enter) { FilterPackages(QueryBlock.Text); } };

            SourcesTreeView.Tapped += (s, e) =>
            {
                if (e.OriginalSource != null && (e.OriginalSource as FrameworkElement)?.DataContext != null)
                {
                    if ((e.OriginalSource as FrameworkElement)?.DataContext is TreeViewNode)
                    {
                        TreeViewNode? node = (e.OriginalSource as FrameworkElement)?.DataContext as TreeViewNode;
                        if (node == null)
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

                        FilterPackages(QueryBlock.Text.Trim());
                    }
                }
            };

            PackageList.DoubleTapped += (s, e) =>
            {
                if (PackageList.SelectedItem is not BundledPackage package)
                {
                    return;
                }

                _ = MainApp.Instance.MainWindow.NavigationPage.ShowPackageDetails(package.Package, OperationType.None);
            };

            PackageList.RightTapped += (s, e) =>
            {
                if (e.OriginalSource is FrameworkElement element)
                {
                    try
                    {
                        if (element.DataContext is not null and BundledPackage package)
                        {
                            PackageList.SelectedItem = package;
                            MenuInstallSettings.IsEnabled = MenuDetails.IsEnabled = MenuShare.IsEnabled = package.IsValid;
                            PackageContextMenu.ShowAt(PackageList, e.GetPosition(PackageList));
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
                if (e.Key == Windows.System.VirtualKey.Enter && PackageList.SelectedItem != null)
                {
                    if (InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down))
                    {
                        (PackageList.SelectedItem as BundledPackage)?.ShowOptions(s, e);
                    }
                    else
                    {
                        if (PackageList.SelectedItem is not BundledPackage package)
                        {
                            return;
                        }

                        _ = MainApp.Instance.MainWindow.NavigationPage.ShowPackageDetails(package.Package, OperationType.None);
                    }
                }
                else if (e.Key == Windows.System.VirtualKey.A && InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
                {
                    if (AllSelected)
                    {
                        ClearItemSelection();
                    }
                    else
                    {
                        SelectAllItems();
                    }
                }
                else if (e.Key == Windows.System.VirtualKey.Space && PackageList.SelectedItem != null)
                {
                    if (PackageList.SelectedItem is not BundledPackage package)
                    {
                        return;
                    }

                    package.Package.IsChecked = !package.Package.IsChecked;
                }
                else if (e.Key == Windows.System.VirtualKey.F1)
                {
                    MainApp.Instance.MainWindow.NavigationPage.ShowHelp();
                }
            };

            int width = 250;
            try
            {
                width = int.Parse(Settings.GetValue("SidepanelWidthBundlesPage"));
            }
            catch
            {
                Settings.SetValue("SidepanelWidthBundlesPage", "250");
            }
            BodyGrid.ColumnDefinitions.ElementAt(0).Width = new GridLength(width);

            GenerateToolBar();
            LoadInterface();
            QueryBlock.PlaceholderText = CoreTools.Translate("Search for packages");
        }

        public void SearchTriggered()
        {
            QueryBlock.Focus(FocusState.Pointer);
        }

        public void ReloadTriggered()
        {
        }

        public void SelectAllTriggered()
        {
            if (AllSelected)
            {
                ClearItemSelection();
            }
            else
            {
                SelectAllItems();
            }
        }

        protected void AddPackageToSourcesList(Package package)
        {
            if (!Initialized)
            {
                return;
            }

            ManagerSource source = package.Source;
            if (!UsedManagers.Contains(source.Manager))
            {
                UsedManagers.Add(source.Manager);
                TreeViewNode Node;
                Node = new TreeViewNode { Content = source.Manager.Name + "                                                                                    .", IsExpanded = false };
                SourcesTreeView.RootNodes.Add(Node);
                SourcesTreeView.SelectedNodes.Add(Node);
                RootNodeForManager.Add(source.Manager, Node);
                UsedSourcesForManager.Add(source.Manager, []);
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
                {
                    RootNodeForManager[source.Manager].Children.Add(item);
                }
            }
        }

        private void FilterOptionsChanged(object sender, RoutedEventArgs e)
        {
            if (!Initialized)
            {
                return;
            }

            FilterPackages(QueryBlock.Text);
        }

        private void InstantSearchValueChanged(object sender, RoutedEventArgs e)
        {
            if (!Initialized)
            {
                return;
            }

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
            MainApp.Instance.MainWindow.NavigationPage.BundleBadge.Visibility = Visibility.Collapsed;
            FilterPackages("");
            BackgroundText.Text = CoreTools.AutoTranslated("No packages have been added yet");
            BackgroundText.Visibility = Visibility.Visible;
        }

        public void FilterPackages(string query, bool StillLoading = false)
        {
            if (!Initialized)
            {
                return;
            }

            FilteredPackages.Clear();
            List<ManagerSource> VisibleSources = [];
            List<PackageManager> VisibleManagers = [];

            if (SourcesTreeView.SelectedNodes.Count > 0)
            {
                foreach (TreeViewNode node in SourcesTreeView.SelectedNodes)
                {
                    if (NodesForSources.ContainsValue(node))
                    {
                        VisibleSources.Add(NodesForSources.First(x => x.Value == node).Key);
                    }
                    else if (RootNodeForManager.ContainsValue(node))
                    {
                        VisibleManagers.Add(RootNodeForManager.First(x => x.Value == node).Key);
                    }
                }
            }

            BundledPackage[] MatchingList;

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

            if (QueryIdRadio.IsChecked == true)
            {
                MatchingList = Packages.Where(x => CharsFunc(x.Package.Name).Contains(CharsFunc(query))).ToArray();
            }
            else if (QueryNameRadio.IsChecked == true)
            {
                MatchingList = Packages.Where(x => CharsFunc(x.Package.Id).Contains(CharsFunc(query))).ToArray();
            }
            else // QueryBothRadio.IsChecked == true
            {
                MatchingList = Packages.Where(x => CharsFunc(x.Package.Name).Contains(CharsFunc(query)) | CharsFunc(x.Package.Id).Contains(CharsFunc(query))).ToArray();
            }

            FilteredPackages.BlockSorting = true;
            int HiddenPackagesDueToSource = 0;
            foreach (BundledPackage match in MatchingList)
            {
                if ((VisibleManagers.Contains(match.Package.Manager) && match.Package.Manager != PEInterface.WinGet) || VisibleSources.Contains(match.Package.Source))
                {
                    FilteredPackages.Add(match);
                }
                else
                {
                    HiddenPackagesDueToSource++;
                }
            }
            FilteredPackages.BlockSorting = false;
            FilteredPackages.Sort();

            if (MatchingList.Length == 0)
            {
                if (!StillLoading)
                {
                    if (Packages.Count == 0)
                    {
                        BackgroundText.Text = SourcesPlaceholderText.Text = CoreTools.AutoTranslated("We couldn't find any package");
                        SourcesPlaceholderText.Text = CoreTools.AutoTranslated("No packages found");
                        MainSubtitle.Text = CoreTools.AutoTranslated("No packages found");
                    }
                    else
                    {
                        BackgroundText.Text = CoreTools.AutoTranslated("No results were found matching the input criteria");
                        SourcesPlaceholderText.Text = CoreTools.AutoTranslated("No packages were found");
                        MainSubtitle.Text = CoreTools.Translate("{0} packages were found, {1} of which match the specified filters.", Packages.Count, MatchingList.Length - HiddenPackagesDueToSource);
                    }
                    BackgroundText.Visibility = Visibility.Visible;
                }

            }
            else
            {
                BackgroundText.Visibility = Visibility.Collapsed;
                MainSubtitle.Text = CoreTools.Translate("{0} packages were found, {1} of which match the specified filters.", Packages.Count, MatchingList.Length - HiddenPackagesDueToSource);
            }
        }

        public void SortPackages(string Sorter)
        {
            if (!Initialized)
            {
                return;
            }

            FilteredPackages.Descending = !FilteredPackages.Descending;
            FilteredPackages.SortingSelector = (a) =>
            {
                if (a.GetType()?.GetProperty(Sorter)?.GetValue(a) == null)
                {
                    Logger.Warn("Sorter element is null on PackageBundlePage");
                }

                return a.GetType()?.GetProperty(Sorter)?.GetValue(a) ?? 0;
            };
            FilteredPackages.Sort();

            if (FilteredPackages.Count > 0)
            {
                PackageList.ScrollIntoView(FilteredPackages[0]);
            }
        }

        public void LoadInterface()
        {
            if (!Initialized)
            {
                return;
            }

            InstantSearchCheckbox.IsChecked = !Settings.Get(InstantSearchSettingString);
            MainTitle.Text = CoreTools.AutoTranslated("Package Bundles");
            HeaderIcon.Glyph = "\uF133";
            CheckboxHeader.Content = " ";
            NameHeader.Content = CoreTools.Translate("Package Name");
            IdHeader.Content = CoreTools.Translate("Package ID");
            VersionHeader.Content = CoreTools.Translate("Version");
            SourceHeader.Content = CoreTools.Translate("Source");

            CheckboxHeader.Click += (s, e) => { SortPackages("IsCheckedAsString"); };
            NameHeader.Click += (s, e) => { SortPackages("Name"); };
            IdHeader.Click += (s, e) => { SortPackages("Id"); };
            VersionHeader.Click += (s, e) => { SortPackages("VersionAsFloat"); };
            SourceHeader.Click += (s, e) => { SortPackages("SourceAsString"); };
        }

        public void UpdateCount()
        {
            BackgroundText.Visibility = Packages.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            MainApp.Instance.MainWindow.NavigationPage.BundleBadge.Value = Packages.Count;
            MainApp.Instance.MainWindow.NavigationPage.BundleBadge.Visibility = Packages.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        public void GenerateToolBar()
        {
            if (!Initialized)
            {
                return;
            }

            AppBarButton InstallPackages = new();
            AppBarButton OpenBundle = new();
            AppBarButton NewBundle = new();

            AppBarButton RemoveSelected = new();

            AppBarButton ExportBundle = new();

            AppBarButton PackageDetails = new();
            AppBarButton SharePackage = new();

            AppBarButton SelectAll = new();
            AppBarButton SelectNone = new();

            AppBarButton HelpButton = new();

            ToolBar.PrimaryCommands.Add(NewBundle);
            ToolBar.PrimaryCommands.Add(OpenBundle);
            ToolBar.PrimaryCommands.Add(ExportBundle);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(InstallPackages);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(new AppBarElementContainer { Content = new TextBlock { HorizontalAlignment = HorizontalAlignment.Stretch } });
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
                { NewBundle,              CoreTools.Translate("New bundle") },
                { InstallPackages,        CoreTools.Translate("Install selection") },
                { OpenBundle,             CoreTools.Translate("Open existing bundle") },
                { RemoveSelected,         CoreTools.Translate("Remove selection from bundle") },
                { ExportBundle,           CoreTools.Translate("Save bundle as") },
                { PackageDetails,         " " + CoreTools.Translate("Package details") },
                { SharePackage,           " " + CoreTools.Translate("Share") },
                { SelectAll,              " " + CoreTools.Translate("Select all") },
                { SelectNone,             " " + CoreTools.Translate("Clear selection") },
                { HelpButton,             CoreTools.Translate("Help") }
            };

            foreach (AppBarButton toolButton in Labels.Keys)
            {
                toolButton.IsCompact = Labels[toolButton][0] == ' ';
                if (toolButton.IsCompact)
                {
                    toolButton.LabelPosition = CommandBarLabelPosition.Collapsed;
                }

                toolButton.Label = Labels[toolButton].Trim();
            }

            Dictionary<AppBarButton, IconType> Icons = new()
            {
                { NewBundle,              IconType.AddTo },
                { InstallPackages,        IconType.Download },
                { OpenBundle,             IconType.OpenFolder },
                { RemoveSelected,         IconType.Delete },
                { ExportBundle,           IconType.SaveAs },
                { PackageDetails,         IconType.Info_Round },
                { SharePackage,           IconType.Share },
                { SelectAll,              IconType.Empty },
                { SelectNone,             IconType.Empty },
                { HelpButton,             IconType.Help }
            };

            foreach (AppBarButton toolButton in Icons.Keys)
            {
                toolButton.Icon = new LocalIcon(Icons[toolButton]);
            }

            PackageDetails.Click += (s, e) =>
            {
                if (PackageList.SelectedItem is BundledPackage package)
                {
                    _ = MainApp.Instance.MainWindow.NavigationPage.ShowPackageDetails(package.Package, OperationType.None);
                }
            };

            HelpButton.Click += (s, e) => { MainApp.Instance.MainWindow.NavigationPage.ShowHelp(); };

            NewBundle.Click += (s, e) =>
            {
                ClearList();
            };

            RemoveSelected.Click += (s, e) =>
            {
                foreach (BundledPackage package in FilteredPackages.ToArray())
                {
                    if (package.IsChecked)
                    {
                        FilteredPackages.Remove(package);
                        Packages.Remove(package);
                    }
                }

                UpdateCount();
            };

            InstallPackages.Click += async (s, e) =>
            {
                MainApp.Instance.MainWindow.ShowLoadingDialog(CoreTools.Translate("Preparing packages, please wait..."));
                foreach (BundledPackage package in FilteredPackages.ToArray())
                {
                    if (package.IsChecked && package.IsValid)
                    {
                        // Actually import settings
                        package.InstallOptions.SaveToDisk();

                        if (package.UpdateOptions.IgnoredVersion != "")
                        {
                            await package.Package.AddToIgnoredUpdatesAsync(package.UpdateOptions.IgnoredVersion);
                        }
                        else
                        {
                            await package.Package.RemoveFromIgnoredUpdatesAsync();
                        }
                    }
                }

                MainApp.Instance.MainWindow.HideLoadingDialog();

                foreach (BundledPackage package in FilteredPackages.ToArray())
                {
                    if (package.IsChecked && package.IsValid)
                    {
                        MainApp.Instance.AddOperationToList(new InstallPackageOperation(package.Package));
                    }
                }
            };

            OpenBundle.Click += (s, e) =>
            {
                ClearList();
                OpenFile();
            };

            ExportBundle.Click += (s, e) =>
            {
                SaveFile();
            };

            SharePackage.Click += (s, e) =>
            {
                if (PackageList.SelectedItem is BundledPackage package)
                {
                    MainApp.Instance.MainWindow.SharePackage(package.Package);
                }
            };

            SelectAll.Click += (s, e) => SelectAllItems();
            SelectNone.Click += (s, e) => ClearItemSelection();

        }

        private void MenuRemoveFromList_Invoked(object sender, RoutedEventArgs args)
        {
            if (!Initialized || PackageList.SelectedItem is not BundledPackage package)
            {
                return;
            }

            package.RemoveFromList(sender, args);

        }

        private void MenuShare_Invoked(object sender, RoutedEventArgs args)
        {
            if (!Initialized || PackageList.SelectedItem is not BundledPackage package || !package.IsValid)
            {
                return;
            }

            MainApp.Instance.MainWindow.SharePackage(package.Package);
        }

        private void MenuDetails_Invoked(object sender, RoutedEventArgs args)
        {
            if (!Initialized || PackageList.SelectedItem is not BundledPackage package || !package.IsValid)
            {
                return;
            }

            _ = MainApp.Instance.MainWindow.NavigationPage.ShowPackageDetails(package.Package, OperationType.None);
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
            BundledPackage? package = PackageList.SelectedItem as BundledPackage;
            if (package?.Package != null)
            {
                package.ShowOptions(sender, e);
            }
        }

        private void SelectAllItems()
        {
            foreach (BundledPackage package in FilteredPackages.ToArray())
            {
                package.IsChecked = true;
            }

            AllSelected = true;
        }

        private void ClearItemSelection()
        {
            foreach (BundledPackage package in FilteredPackages.ToArray())
            {
                package.IsChecked = false;
            }

            AllSelected = false;
        }

        public async Task AddPackages(IEnumerable<Package> packages)
        {
            MainApp.Instance.MainWindow.ShowLoadingDialog(CoreTools.Translate("Preparing packages, please wait..."));
            List<BundledPackage> bundled = [];
            foreach (Package pkg in packages)
            {
                if (pkg.Source.IsVirtualManager)
                {
                    bundled.Add(new InvalidBundledPackage(pkg));
                }
                else
                {
                    bundled.Add(await BundledPackage.FromPackageAsync(pkg));
                }
            }

            foreach (BundledPackage pkg in bundled)
            {
                AddPackage(pkg);
            }

            MainApp.Instance.MainWindow.HideLoadingDialog();

        }

        public void AddPackage(BundledPackage package)
        {
            Packages.Add(package);
            AddPackageToSourcesList(package.Package);
            BackgroundText.Visibility = Packages.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            MainApp.Instance.MainWindow.NavigationPage.BundleBadge.Value = Packages.Count;
            MainApp.Instance.MainWindow.NavigationPage.BundleBadge.Visibility = Packages.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            FilterPackages(QueryBlock.Text.Trim());
        }

        public async void OpenFile()
        {
            try
            {
                // Select file
                FileOpenPicker picker = new(MainApp.Instance.MainWindow.GetWindowHandle());
                string file = picker.Show(["*.json", "*.yaml", "*.xml"]);
                if (file == string.Empty)
                {
                    return;
                }

                MainApp.Instance.MainWindow.ShowLoadingDialog(CoreTools.Translate("Loading packages, please wait..."));

                // Read file
                BundleFormatType formatType;
                if (file.Split('.')[^1].ToLower() == "yaml")
                {
                    formatType = BundleFormatType.YAML;
                }
                else if (file.Split('.')[^1].ToLower() == "xml")
                {
                    formatType = BundleFormatType.XML;
                }
                else
                {
                    formatType = BundleFormatType.JSON;
                }

                string fileContent = await File.ReadAllTextAsync(file);

                // Import packages to list
                await AddPackagesFromBundleString(fileContent, formatType);

                MainApp.Instance.MainWindow.HideLoadingDialog();

            }
            catch (Exception ex)
            {
                Logger.Error("Could not load packages from a file");
                Logger.Error(ex);
                MainApp.Instance.MainWindow.HideLoadingDialog();
            }
        }
        public async Task AddPackagesFromBundleString(string content, BundleFormatType format)
        {
            // Deserialize data
            SerializableBundle_v1? DeserializedData;
            if (format == BundleFormatType.JSON)
            {
                DeserializedData = JsonSerializer.Deserialize<SerializableBundle_v1>(content);
            }
            else if (format == BundleFormatType.YAML)
            {
                YamlDotNet.Serialization.IDeserializer deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                    .Build();
                DeserializedData = deserializer.Deserialize<SerializableBundle_v1>(content);
            }
            else
            {
                string tempfile = Path.GetTempFileName();
                await File.WriteAllTextAsync(tempfile, content);
                StreamReader reader = new(tempfile);
                XmlSerializer serializer = new(typeof(SerializableBundle_v1));
                DeserializedData = serializer.Deserialize(reader) as SerializableBundle_v1;
                reader.Close();
                File.Delete(tempfile);
            }

            if (DeserializedData == null)
            {
                throw new InvalidOperationException($"Deserialized data was null for content {content} and format {format}");
            }

            // Get a list of all managers
            Dictionary<string, PackageManager> ManagerSourceReference = [];
            foreach (PackageManager manager in PEInterface.Managers)
            {
                ManagerSourceReference.Add(manager.Name, manager);
            }

            foreach (SerializableValidPackage_v1 DeserializedPackage in DeserializedData.packages)
            {
                // Check if the manager exists
                if (!ManagerSourceReference.TryGetValue(DeserializedPackage.ManagerName, out var packageManager))
                {
                    AddPackage(new InvalidBundledPackage(DeserializedPackage.Name, DeserializedPackage.Id, DeserializedPackage.Version, DeserializedPackage.Source, DeserializedPackage.ManagerName));
                    continue;
                }

                // Handle a disabled manager
                if (!packageManager.IsEnabled())
                {
                    AddPackage(new InvalidBundledPackage(DeserializedPackage.Name, DeserializedPackage.Id, DeserializedPackage.Version, DeserializedPackage.Source, DeserializedPackage.ManagerName));
                    continue;
                }
                // Handle a nonworking manager
                if (!packageManager.Status.Found)
                {
                    AddPackage(new InvalidBundledPackage(DeserializedPackage.Name, DeserializedPackage.Id, DeserializedPackage.Version, DeserializedPackage.Source, DeserializedPackage.ManagerName));
                    continue;
                }

                ManagerSource? Source = packageManager.Properties.DefaultSource;

                if (packageManager.Capabilities.SupportsCustomSources)
                {
                    // Check if the source exists
                    string SourceName = DeserializedPackage.Source.Split(':')[^1].Trim();
                    Source = packageManager.GetSourceIfExists(SourceName);

                    if (Source == null)
                    {
                        AddPackage(new InvalidBundledPackage(DeserializedPackage.Name, DeserializedPackage.Id, DeserializedPackage.Version, DeserializedPackage.Source, DeserializedPackage.ManagerName));
                        continue;
                    }
                }

                Package package = new(DeserializedPackage.Name, DeserializedPackage.Id, DeserializedPackage.Version, Source, packageManager);

                InstallationOptions InstallOptions = InstallationOptions.FromSerialized(DeserializedPackage.InstallationOptions, package);
                SerializableUpdatesOptions_v1 UpdateOptions = DeserializedPackage.Updates;

                BundledPackage newPackage = new(package, InstallOptions, UpdateOptions);
                AddPackage(newPackage);
            }
        }

        public static async Task<string> GetBundleStringFromPackages(BundledPackage[] packages, BundleFormatType formatType = BundleFormatType.JSON)
        {
            SerializableBundle_v1 exportable = new();
            foreach (BundledPackage package in packages)
            {
                if (!package.IsValid)
                {
                    exportable.incompatible_packages.Add(package.AsSerializable_Incompatible());
                }
                else
                {
                    exportable.packages.Add(package.AsSerializable());
                }
            }

            Logger.Debug("Finished loading serializable objects. Serializing with format " + formatType.ToString());
            string ExportableData;

            if (formatType == BundleFormatType.JSON)
            {
                ExportableData = JsonSerializer.Serialize<SerializableBundle_v1>(exportable, new JsonSerializerOptions { WriteIndented = true });
            }
            else if (formatType == BundleFormatType.YAML)
            {
                YamlDotNet.Serialization.ISerializer serializer = new YamlDotNet.Serialization.SerializerBuilder()
                    .Build();
                ExportableData = serializer.Serialize(exportable);
            }
            else
            {
                string tempfile = Path.GetTempFileName();
                StreamWriter writer = new(tempfile);
                XmlSerializer serializer = new(typeof(SerializableBundle_v1));
                serializer.Serialize(writer, exportable);
                writer.Close();
                ExportableData = await File.ReadAllTextAsync(tempfile);
                File.Delete(tempfile);

            }

            Logger.Debug("Serialization finished successfully");

            return ExportableData;
        }
        public async void SaveFile()
        {
            try
            {
                // Get file
                // Save file
                string file = new FileSavePicker(MainApp.Instance.MainWindow.GetWindowHandle()).Show(["*.json", "*.yaml", "*.xml"], CoreTools.Translate("Package bundle") + ".json");
                if (file != string.Empty)
                {
                    // Loading dialog
                    MainApp.Instance.MainWindow.ShowLoadingDialog(CoreTools.Translate("Saving packages, please wait..."));

                    List<BundledPackage> packages = [.. Packages];

                    // Select appropriate format
                    BundleFormatType formatType;
                    if (file.Split('.')[^1].ToLower() == "yaml")
                    {
                        formatType = BundleFormatType.YAML;
                    }
                    else if (file.Split('.')[^1].ToLower() == "xml")
                    {
                        formatType = BundleFormatType.XML;
                    }
                    else
                    {
                        formatType = BundleFormatType.JSON;
                    }

                    // Save serialized data
                    await File.WriteAllTextAsync(file, await GetBundleStringFromPackages(packages.ToArray(), formatType));

                    MainApp.Instance.MainWindow.HideLoadingDialog();

                    // Launch file
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = @$"/select, ""{file}"""
                    });

                }
            }
            catch (Exception ex)
            {
                MainApp.Instance.MainWindow.HideLoadingDialog();
                Logger.Error("An error occurred when saving packages to a file");
                Logger.Error(ex);
            }
        }
        private void SidepanelWidth_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width == ((int)(e.NewSize.Width / 10)) || e.NewSize.Width == 25)
            {
                return;
            }

            lastSavedWidth = (int)(e.NewSize.Width / 10);
            Settings.SetValue("SidepanelWidthBundlesPage", ((int)e.NewSize.Width).ToString());
            foreach (UIElement control in SidePanelGrid.Children)
            {
                control.Visibility = e.NewSize.Width > 20 ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}

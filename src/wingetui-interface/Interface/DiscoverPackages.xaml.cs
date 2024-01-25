using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
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
    public partial class DiscoverPackagesPage : Page
    {
        public ObservableCollection<Package> Packages = new ObservableCollection<Package>();
        public SortableObservableCollection<Package> FilteredPackages = new SortableObservableCollection<Package>() { SortingSelector = (a) => (a.Name)};
        protected MainAppBindings bindings = MainAppBindings.Instance;

        protected TranslatedTextBlock MainTitle;
        protected TranslatedTextBlock MainSubtitle;
        protected ListView PackageList;
        protected ProgressBar LoadingProgressBar;
        protected Image HeaderImage;

        private bool IsDescending = true;
        public DiscoverPackagesPage()
        {
            this.InitializeComponent();
            ReloadButton.Click += async (s, e) => { await __load_packages(); } ;
            FindButton.Click += async (s, e) => { await FilterPackages(QueryBlock.Text); };
            QueryBlock.TextChanged += async (s, e) => { await FilterPackages(QueryBlock.Text); };
            MainTitle = __main_title;
            MainSubtitle = __main_subtitle;
            PackageList = __package_list;
            HeaderImage = __header_image;
            LoadingProgressBar= __loading_progressbar;
            LoadInterface();
            __load_packages();
        }

        protected async Task __load_packages()
        {
            MainSubtitle.Text= "Loading...";
            LoadingProgressBar.Visibility = Visibility.Visible;
            //await this.LoadPackages();
            await this.FilterPackages(QueryBlock.Text);
            MainSubtitle.Text = "Found packages: " + Packages.Count().ToString();
            LoadingProgressBar.Visibility = Visibility.Collapsed;
        }

        public async Task LoadPackages()
        {
            MainSubtitle.Text = "Loading...";
            LoadingProgressBar.Visibility = Visibility.Visible;
            var intialQuery = QueryBlock.Text;
            Packages.Clear();
            FilteredPackages.Clear();
            if(QueryBlock.Text == null || QueryBlock.Text.Length < 3)
            {
                MainSubtitle.Text = "Found packages: " + Packages.Count().ToString();
                LoadingProgressBar.Visibility = Visibility.Collapsed;
                return;
            }
            var packages = await bindings.App.Scoop.FindPackages(QueryBlock.Text);
            foreach (var package in packages)
            {
                if (intialQuery != QueryBlock.Text)
                    return;
                Packages.Add(package);
            }
            MainSubtitle.Text = "Found packages: " + Packages.Count().ToString();
            LoadingProgressBar.Visibility = Visibility.Collapsed;
        }

        public async Task FilterPackages(string query)
        {
            await LoadPackages();

            FilteredPackages.Clear();

            var MatchingList = Packages.Where(x => x.Name.ToLower().Contains(query.ToLower())); // Needs tweaking

            foreach (var match in MatchingList)
            {
                FilteredPackages.Add(match);
            }
        }

        public void SortPackages(string Sorter)
        {
            FilteredPackages.Descending = !FilteredPackages.Descending;
            FilteredPackages.SortingSelector = (a) => (a.GetType().GetProperty(Sorter).GetValue(a));
            FilteredPackages.Sort();
        }

        public void LoadInterface()
        {
            MainTitle.Text = "Discover Packages";
            HeaderImage.Source = new BitmapImage(new Uri("ms-appx:///wingetui/resources/desktop_download.png"));
            LoadToolbar();
            CheckboxHeader.Content = " ";
            NameHeader.Content = bindings.Translate("Package Name");
            IdHeader.Content = bindings.Translate("Package ID");
            VersionHeader.Content = bindings.Translate("Version");
            // NewVersionHeader.Content = bindings.Translate("New version");
            SourceHeader.Content = bindings.Translate("Source");

            NameHeader.Click += (s, e) => { SortPackages("Name"); };
            IdHeader.Click += (s, e) => { SortPackages("Id"); };
            VersionHeader.Click += (s, e) => { SortPackages("VersionAsFloat"); };
            SourceHeader.Click += (s, e) => { SortPackages("SourceAsString"); };
        }


        public void LoadToolbar()
        {
            var InstallSelected = new AppBarButton();
            var InstallAsAdmin = new AppBarButton();
            var InstallSkipHash = new AppBarButton();
            var InstallInteractive = new AppBarButton();

            var PackageDetails = new AppBarButton();
            var SharePackage = new AppBarButton();

            var SelectAll = new AppBarButton();
            var SelectNone = new AppBarButton();

            var ImportPackages = new AppBarButton();
            var ExportSelection = new AppBarButton();

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
            ToolBar.PrimaryCommands.Add(ImportPackages);
            ToolBar.PrimaryCommands.Add(ExportSelection);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(HelpButton);

            var Labels = new Dictionary<AppBarButton, string>
            { // Entries with a trailing space are collapsed
                { InstallSelected,      "Install Selected packages" },
                { InstallAsAdmin,       " Install as administrator" },
                { InstallSkipHash,      " Skip integrity checks" },
                { InstallInteractive,   " InteractiveInstallation" },
                { PackageDetails,       " Package details" },
                { SharePackage,         " Share" },
                { SelectAll,            " Select all" },
                { SelectNone,           " Clear selection" },
                { ImportPackages,       "Import packages" },
                { ExportSelection,      "Export selected packages" },
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
                { InstallSelected,      "install" },
                { InstallAsAdmin,       "runasadmin" },
                { InstallSkipHash,      "checksum" },
                { InstallInteractive, "interactive" },
                { PackageDetails,       "info" },
                { SharePackage,         "share" },
                { SelectAll,            "selectall" },
                { SelectNone,           "selectnone" },
                { ImportPackages,       "import" },
                { ExportSelection,      "export" },
                { HelpButton,           "help" }
            };

            foreach (var toolButton in Icons.Keys)
                toolButton.Icon = new LocalIcon(Icons[toolButton]);

        }
    }
}

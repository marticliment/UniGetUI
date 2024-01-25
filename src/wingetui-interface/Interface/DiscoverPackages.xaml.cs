using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using ModernWindow.Interface.Widgets;
using ModernWindow.PackageEngine;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
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
        public ObservableCollection<Package> FilteredPackages = new ObservableCollection<Package>();
        protected MainAppBindings bindings = MainAppBindings.Instance;

        protected TranslatedTextBlock MainTitle;
        protected TranslatedTextBlock MainSubtitle;
        protected ListView PackageList;
        protected ProgressBar LoadingProgressBar;
        protected Image HeaderImage;
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

        public void LoadInterface()
        {
            MainTitle.Text = "Discover Packages";
            HeaderImage.Source = new BitmapImage(new Uri("ms-appx:///wingetui/resources/desktop_download.png"));
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
    }
}

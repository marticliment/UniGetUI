using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using System.Text.Json;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Interfaces;
using Windows.Security.Cryptography.Core;
using UniGetUI.Core.IconEngine;
using UniGetUI.PackageEngine.PackageClasses;
using System.ComponentModel;
using Windows.Devices.SmartCards;
using System.Diagnostics;
using UniGetUI.PackageEngine.Classes.Packages;
using UniGetUI.PackageEngine;
using Windows.ApplicationModel.Activation;
using UniGetUI.Pages.DialogPages;
using UniGetUI.Interface.Telemetry;
using UniGetUI.PackageEngine.Enums;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Controls
{
    public sealed partial class PackagesRanking : UserControl
    {
        public ObservableCollection<PackageRankingItem> PopularRank { get; } = new();
        public ObservableCollection<PackageRankingItem> InstalledRank { get; } = new();
        public ObservableCollection<PackageRankingItem> WallOfShame { get; } = new();

        public PackagesRanking()
        {
            this.InitializeComponent();
        }

        public void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            _ = Reload();
        }

        public async Task Reload()
        {
            try
            {
                PopularRank.Clear();
                InstalledRank.Clear();
                WallOfShame.Clear();
                ReloadRing.Visibility = Visibility.Visible;

                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetStringAsync("https://marticliment.com/unigetui/rankings/daily-ranking.json");
                    var rankings = JsonSerializer.Deserialize<Rankings>(response);

                    foreach (var item in rankings.popular[0..4])
                    {
                        PopularRank.Add(new(item));
                    }

                    foreach (var item in rankings.installed[0..4])
                    {
                        InstalledRank.Add(new(item));
                    }

                    foreach (var item in rankings.uninstalled[0..4])
                    {
                        WallOfShame.Add(new(item));
                    }
                }

                ReloadRing.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                Logger.Error("Could not load package rankings:");
                ReloadRing.Visibility = Visibility.Collapsed;
            }
        }
    }

    public class Rankings
    {
        public long timestamp_utc_seconds { get; set; }
        public List<List<object>> popular { get; set; } = new();  
        public List<List<object>> installed { get; set; } = new();
        public List<List<object>> uninstalled { get; set; } = new();
    }

    public class PackageRankingItem: INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string ManagerName { get; set; }
        public Uri IconUri = new Uri("ms-appx:///Assets/Images/package_color.png");

        private string id;
        private string manager;
        private string source;
        private Package? package;

        public event PropertyChangedEventHandler? PropertyChanged;

        public PackageRankingItem(List<object> item)
        {
            id = item[0].ToString() ?? "";
            manager = item[1].ToString() ?? "";
            source = item[2].ToString() ?? "";
            package = GetPackage();

            Name = package?.Name ?? CoreTools.FormatAsName(id, isWinget: source == "winget");
            ManagerName =  package?.Source.AsString_DisplayName ?? manager + ": " + source.ToString();
            LoadIcon();
        }

        public async void LoadIcon()
        {
            if (package is not null)
            {
                var uri = await Task.Run(package.GetIconUrlIfAny);
                if(uri is not null)
                {
                    IconUri = uri;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconUri)));
                    return;
                }
            }
            
            var icon = await Task.Run(() => IconDatabase.Instance.GetIconUrlForId(Package.NormalizeIconId(id, manager, source)));
            if (icon is not null)
            {
                IconUri = new Uri(icon);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconUri)));
            }
        }

        public Package? GetPackage()
        {
            IPackageManager? _manager = null;
            foreach (var candidate in PEInterface.Managers)
                if (candidate.Name == manager || candidate.DisplayName == manager)
                {
                    _manager = candidate;
                    break;
                }

            if (_manager is null) return null;
            if (!_manager.IsEnabled()) return null;
            if (!_manager.Status.Found) return null;

            IManagerSource? _source = null;
            foreach (var candidate in _manager.Properties.KnownSources)
                if (candidate.Name == source)
                {
                    _source = candidate;
                    break;
                }

            if (_source is null) _source = _manager.DefaultSource;

            return new Package(CoreTools.FormatAsName(id, isWinget: source == "winget"), id, "latest", _source, _manager, new());
        }

        public void ShowDetails()
        {
            if(package is not null) DialogHelper.ShowPackageDetails(package, OperationType.Install, TEL_InstallReferral.FROM_RANKING);
        }
    }
}

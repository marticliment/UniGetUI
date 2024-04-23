using Microsoft.UI.Xaml;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UniGetUI.Core;

namespace UniGetUI.PackageEngine.Classes
{
    public class BundledPackage : INotifyPropertyChanged
    {
        public AppTools Tools = AppTools.Instance;
        public Package Package { get; }
        public bool IsValid { get; set; } = true;
        public InstallationOptions InstallOptions { get; set; }
        public SerializableUpdatesOptions_v1 UpdateOptions = null;

        public double DrawOpacity = 1;

        public event PropertyChangedEventHandler PropertyChanged;

        private bool __is_checked = true;
        public bool IsChecked { get { return __is_checked; } set { __is_checked = value; OnPropertyChanged(); } }


        public virtual string Name
        {
            get
            {
                return Package.Name;
            }
        }

        public virtual string Id
        {
            get
            {
                return Package.Id;
            }
        }

        public virtual string version
        {
            get
            {
                if (UpdateOptions == null || !UpdateOptions.UpdatesIgnored)
                    return Tools.Translate("Latest");
                else
                    return Package.Version;
            }
        }

        public virtual string SourceAsString
        {
            get
            {
                return Package.SourceAsString;
            }
        }

        public virtual string IconId
        {
            get
            {
                return Package.Source.IconId;
            }
        }
        public static async Task<BundledPackage> FromPackageAsync(Package package)
        {
            var iOptions = await InstallationOptions.FromPackageAsync(package);
            var uOptions = await SerializableUpdatesOptions_v1.FromPackageAsync(package);

            return new BundledPackage(package, iOptions, uOptions);
        }

        public BundledPackage(Package package, InstallationOptions options, SerializableUpdatesOptions_v1 updateOptions)
        {
            Package = package;
            InstallOptions = options;
            IsValid = !package.Source.IsVirtualManager;
            UpdateOptions = updateOptions;
        }

        public async virtual void ShowOptions(object sender, RoutedEventArgs e)
        {
            InstallOptions = await Tools.App.MainWindow.NavigationPage.UpdateInstallationSettings(Package, InstallOptions);
        }

        public void RemoveFromList(object sender, RoutedEventArgs e)
        {
            Tools.App.MainWindow.NavigationPage.BundlesPage.Packages.Remove(this);
            Tools.App.MainWindow.NavigationPage.BundlesPage.FilteredPackages.Remove(this);
            Tools.App.MainWindow.NavigationPage.BundlesPage.UpdateCount();
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }



        public virtual SerializableValidPackage_v1 AsSerializable()
        {
            SerializableValidPackage_v1 Serializable = new();
            Serializable.Id = Id;
            Serializable.Name = Name;
            Serializable.Version = Package.Version;
            Serializable.Source = Package.Source.Name;
            Serializable.ManagerName = Package.Manager.Name;
            Serializable.InstallationOptions = this.InstallOptions.Serialized();
            Serializable.Updates = this.UpdateOptions;
            return Serializable;
        }

        public virtual SerializableIncompatiblePackage_v1 AsSerializable_Incompatible()
        {
            SerializableIncompatiblePackage_v1 Serializable = new();
            Serializable.Id = Id;
            Serializable.Name = Name;
            Serializable.Version = version;
            Serializable.Source = SourceAsString;
            return Serializable;
        }

        public static Package FromSerialized(SerializableValidPackage_v1 DeserializedPackage, PackageManager manager, ManagerSource source)
        {
            return new Package(DeserializedPackage.Name, DeserializedPackage.Id, DeserializedPackage.Version, source, manager);
        }



    }

}

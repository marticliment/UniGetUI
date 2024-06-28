using Microsoft.UI.Xaml;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UniGetUI.Core.Classes;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Serializable;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Serializable;

namespace UniGetUI.PackageEngine.Classes
{
    public class BundledPackage : INotifyPropertyChanged, IIndexableListItem
    {
        public IPackage Package { get; }
        public int Index { get; set; }
        public bool IsValid { get; set; } = true;
        public IInstallationOptions InstallOptions { get; set; }
        public SerializableUpdatesOptions_v1 UpdateOptions;

        public double DrawOpacity = 1;

        public event PropertyChangedEventHandler? PropertyChanged;

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
                    return CoreTools.Translate("Latest");
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
        public static async Task<BundledPackage> FromPackageAsync(IPackage package)
        {
            InstallationOptions iOptions = await InstallationOptions.FromPackageAsync(package);
            SerializableUpdatesOptions_v1 uOptions = new();

            uOptions.UpdatesIgnored = await package.HasUpdatesIgnoredAsync();
            uOptions.IgnoredVersion = await package.GetIgnoredUpdatesVersionAsync();

            return new BundledPackage(package, iOptions, uOptions);
        }

        public BundledPackage(IPackage package, IInstallationOptions options, SerializableUpdatesOptions_v1 updateOptions)
        {
            Package = package;
            InstallOptions = options;
            IsValid = !package.Source.IsVirtualManager;
            UpdateOptions = updateOptions;
        }

        public async virtual void ShowOptions(object sender, RoutedEventArgs e)
        {
            InstallOptions = await MainApp.Instance.MainWindow.NavigationPage.UpdateInstallationSettings(Package, InstallOptions);
        }

        public void RemoveFromList(object sender, RoutedEventArgs e)
        {
            //TODO: FIXME
            /*MainApp.Instance.MainWindow.NavigationPage.BundlesPage.Packages.Remove(this);
            MainApp.Instance.MainWindow.NavigationPage.BundlesPage.FilteredPackages.Remove(this);
            MainApp.Instance.MainWindow.NavigationPage.BundlesPage.UpdateCount();
            */
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }



        public virtual SerializablePackage_v1 AsSerializable()
        {
            SerializablePackage_v1 Serializable = new();
            Serializable.Id = Id;
            Serializable.Name = Name;
            Serializable.Version = Package.Version;
            Serializable.Source = Package.Source.Name;
            Serializable.ManagerName = Package.Manager.Name;
            Serializable.InstallationOptions = InstallOptions.AsSerializable();
            Serializable.Updates = UpdateOptions;
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

        public static Package FromSerialized(SerializablePackage_v1 DeserializedPackage, PackageManager manager, ManagerSource source)
        {
            return new Package(DeserializedPackage.Name, DeserializedPackage.Id, DeserializedPackage.Version, source, manager);
        }



    }

    public class InvalidBundledPackage : BundledPackage
    {
        readonly string __name;
        readonly string __id;
        readonly string __version;
        readonly string __source;
        readonly string __manager;

        public override string Name
        {
            get
            {
                return __name;
            }
        }

        public override string Id
        {
            get
            {
                return __id;
            }
        }

        public override string version
        {
            get
            {
                return __version;
            }
        }

        public override string SourceAsString
        {
            get
            {
                if (__source == "")
                    return __manager;
                return __manager + ": " + __source;
            }
        }

        public override string IconId
        {
            get
            {
                return "help";
            }
        }

        public InvalidBundledPackage(string name, string id, string version, string source, string manager) : this(new Package(name, id, version, PEInterface.WinGet.DefaultSource, PEInterface.WinGet))
        {
            IsValid = false;
            DrawOpacity = 0.5;
            __name = name;
            __id = id;
            __version = version;
            __source = source;
            __manager = manager;
        }

        public InvalidBundledPackage(IPackage package) : base(package, InstallationOptions.FromPackage(package), new SerializableUpdatesOptions_v1())
        {
            IsValid = false;
            DrawOpacity = 0.5;
            __name = package.Name;
            __id = package.Id;
            __version = package.Version;
            if (!package.Manager.Capabilities.SupportsCustomSources)
            {
                __source = "";
                __manager = package.Manager.Name;
            }
            else if (package.Source.IsVirtualManager)
            {
                __source = "";
                __manager = package.Source.Name;
            }
            else
            {
                __source = package.Source.Name;
                __manager = package.Manager.Name;
            }
        }
        public async override void ShowOptions(object sender, RoutedEventArgs e)
        {
            await Task.Delay(0);
        }

        public override SerializablePackage_v1 AsSerializable()
        {
            throw new System.Exception("Cannot serialize an invalid package as a bundled package. Call Serialized_Incompatible() instead ");
        }
        public override SerializableIncompatiblePackage_v1 AsSerializable_Incompatible()
        {
            SerializableIncompatiblePackage_v1 Serializable = new();
            Serializable.Id = Id;
            Serializable.Name = Name;
            Serializable.Version = version;
            Serializable.Source = SourceAsString;
            return Serializable;
        }


    }

}

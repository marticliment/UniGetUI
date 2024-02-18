using ModernWindow.Data;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ModernWindow.PackageEngine
{
    public enum PackageScope
    {
        // Repeated entries for coherence with Package Managers
        Global = 1,
        Machine = 1,
        Local = 0,
        User = 0,
    }
    public class Package : INotifyPropertyChanged
    {
        public class __serializable_updates_options_v1
        {
            public bool UpdatesIgnored { get; set; } = false;
            public string IgnoredVersion { get; set; } = "";
            public static async Task<__serializable_updates_options_v1> FromPackage(Package package)
            {
                var Serializable = new __serializable_updates_options_v1();
                Serializable.UpdatesIgnored = await package.HasUpdatesIgnored();
                Serializable.IgnoredVersion = await package.GetIgnoredUpdatesVersion();
                return Serializable;
            }
        }
        public class __serializable_bundled_package_v1
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string Version { get; set; } = "";
            public string Source { get; set; } = "";
            public string ManagerName { get; set; } = "";
            public InstallationOptions.__serializable_options_v1 InstallationOptions { get; set; }
            public __serializable_updates_options_v1 Updates { get; set; }
        }

        public class __serializable_incompatible_package_v1
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string Version { get; set; } = "";
            public string Source { get; set; } = "";
        }


        public AppTools bindings = AppTools.Instance;

        private bool __is_checked = false;

        public bool IsChecked { get { return __is_checked; } set { __is_checked = value; OnPropertyChanged(); } }
        public string IsCheckedAsString { get { return IsChecked ? "True" : "False"; } }
        public string Name { get; }
        public string Id { get; set; }
        public string Version { get; }
        public float VersionAsFloat { get; }
        public ManagerSource Source { get; set; }
        public PackageManager Manager { get; }
        public string UniqueId { get; }
        public string NewVersion { get; }
        public bool IsUpgradable { get; } = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public PackageScope Scope { get; set; }
        public string SourceAsString
        {
            get
            {
                if (Source != null)
                    return Source.ToString();
                else return "";
            }
        }



        public Package(string name, string id, string version, ManagerSource source, PackageManager manager, PackageScope scope = PackageScope.Local)
        {
            Name = name;
            Id = id;
            Version = version;
            Source = source;
            Manager = manager;
            Scope = scope;
            UniqueId = $"{Manager.Properties.Name}\\{Id}\\{Version}";
            NewVersion = "";
            VersionAsFloat = GetFloatVersion();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Package))
                return false;
            else
                return Source == (obj as Package).Source && Id == (obj as Package).Id;
        }

        public Package _get_self_package()
        {
            return this;
        }

        public string GetIconId()
        {
            string iconId = Id.ToLower();
            if (Manager == bindings.App.Winget)
                iconId = String.Join('.', iconId.Split(".")[1..]);
            else if (Manager == bindings.App.Choco)
                iconId = iconId.Replace(".install", "").Replace(".portable", "");
            else if (Manager == bindings.App.Scoop)
                iconId = iconId.Replace(".app", "");
            return iconId;
        }

        public Uri GetIconUrl()
        {
            string iconId = GetIconId();
            if (CoreData.IconDatabaseData.ContainsKey(iconId))
                if (CoreData.IconDatabaseData[iconId].icon != "")
                    return new Uri(CoreData.IconDatabaseData[iconId].icon);

            return new Uri("ms-appx:///Assets/Images/package_color.png"); // TODO: Fallback image!
        }

        public float GetFloatVersion()
        {
            string _ver = "";
            bool _dotAdded = false;
            foreach (char _char in Version)
            {
                if (Char.IsDigit(_char))
                    _ver += _char;
                else if (_char == '.')
                {
                    if (!_dotAdded)
                    {
                        _ver += _char;
                        _dotAdded = true;
                    }
                }
            }
            float res = 0.0F;
            if (_ver != "" && _ver != ".")
                try
                {
                    return float.Parse(_ver);
                }
                catch { }
            return res;
        }

        public async Task AddToIgnoredUpdates(string version = "*")
        {
            string IgnoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";
            JsonObject IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;
            if (IgnoredUpdatesJson.ContainsKey(IgnoredId))
                IgnoredUpdatesJson.Remove(IgnoredId);
            IgnoredUpdatesJson.Add(IgnoredId, version);
            await File.WriteAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile, IgnoredUpdatesJson.ToString());
            bindings.App.mainWindow.NavigationPage.UpdatesPage.RemoveCorrespondingPackages(this);

            // TODO: Change InstalledPackages flag to show that the package is ignored, add to IgnoredPackages if applicable
        }

        public async Task RemoveFromIgnoredUpdates()
        {
            string IgnoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";
            JsonObject IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;
            if (IgnoredUpdatesJson.ContainsKey(IgnoredId))
            {
                IgnoredUpdatesJson.Remove(IgnoredId);
                await File.WriteAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile, IgnoredUpdatesJson.ToString());
            }

            // TODO: Change InstalledPackages flag to show that the package is no longer ignored

        }

        public async Task<bool> HasUpdatesIgnored(string version = "*")
        {
            string IgnoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";
            JsonObject IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;
            if (IgnoredUpdatesJson.ContainsKey(IgnoredId) && (IgnoredUpdatesJson[IgnoredId].ToString() == "*" || IgnoredUpdatesJson[IgnoredId].ToString() == version))
                return true;
            else
                return false;
        }

        public async Task<string> GetIgnoredUpdatesVersion()
        {
            string IgnoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";
            JsonObject IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;
            if (IgnoredUpdatesJson.ContainsKey(IgnoredId))
                return IgnoredUpdatesJson[IgnoredId].ToString();
            else
                return "";
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public async Task<__serializable_bundled_package_v1> AsSerializable_BundledPackage()
        {
            var Serializable = new __serializable_bundled_package_v1();
            Serializable.Id = Id;
            Serializable.Name = Name;
            Serializable.Version = Version;
            Serializable.Source = Source.Name;
            Serializable.ManagerName = Manager.Name;
            var options = new InstallationOptions(this);
            Serializable.InstallationOptions = options.AsSerializable();
            var updates = await __serializable_updates_options_v1.FromPackage(this);
            Serializable.Updates = updates;
            return Serializable;
        }

        public __serializable_incompatible_package_v1 AsSerializable_IncompatiblePackage()
        {
            var Serializable = new __serializable_incompatible_package_v1();
            Serializable.Id = Id;
            Serializable.Name = Name;
            Serializable.Version = Version;
            Serializable.Source = Source.Name;
            return Serializable;
        }

    }

    public class UpgradablePackage : Package
    {
        public new string NewVersion { get; }

        public float NewVersionAsFloat { get; }
        public new bool IsUpgradable { get; } = true;

        public UpgradablePackage(string name, string id, string installed_version, string new_version, ManagerSource source, PackageManager manager, PackageScope scope = PackageScope.Local) : base(name, id, installed_version, source, manager, scope)
        {
            NewVersion = new_version;
            IsChecked = true;
            NewVersionAsFloat = GetFloatNewVersion();
        }

        public float GetFloatNewVersion()
        {
            string _ver = "";
            bool _dotAdded = false;
            foreach (char _char in NewVersion)
            {
                if (Char.IsDigit(_char))
                    _ver += _char;
                else if (_char == '.')
                {
                    if (!_dotAdded)
                    {
                        _ver += _char;
                        _dotAdded = true;
                    }
                }
            }
            float res = 0.0F;
            if (_ver != "" && _ver != ".")
                try
                {
                    return float.Parse(_ver);
                }
                catch (Exception)
                {
                }
            return res;
        }

        public new UpgradablePackage _get_self_package()
        {
            return this;
        }

        public bool NewVersionIsInstalled()
        {
            foreach (Package package in bindings.App.mainWindow.NavigationPage.InstalledPage.Packages)
                if (package.Manager == Manager && package.Id == Id && package.Version == NewVersion && package.Source.Name == Source.Name)
                    return true;
            return false;
        }
    }

    public class PackageDetails
    {
        public Package Package { get; }
        public string Name { get; }
        public string Id { get; }
        public string Version { get; }
        public string NewVersion { get; }
        public ManagerSource Source { get; }
        public string Description { get; set; } = "";
        public string Publisher { get; set; } = "";
        public string Author { get; set; } = "";
        public Uri? HomepageUrl { get; set; } = null;
        public string License { get; set; } = "";
        public Uri? LicenseUrl { get; set; } = null;
        public Uri? InstallerUrl { get; set; } = null;
        public string InstallerHash { get; set; } = "";
        public string InstallerType { get; set; } = "";
        public double InstallerSize { get; set; } = 0; // In Megabytes
        public Uri? ManifestUrl { get; set; } = null;
        public string UpdateDate { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public Uri? ReleaseNotesUrl { get; set; } = null;
        public string[] Versions { get; set; } = new string[0];
        public string[] Architectures { get; set; } = new string[0];
        public string[] Scopes { get; set; } = new string[0];
        public string[] Tags { get; set; } = new string[0];

        public PackageDetails(Package package)
        {
            Package = package;
            Name = package.Name;
            Id = package.Id;
            Version = package.Version;
            Source = package.Source;
            if (package is UpgradablePackage)
                NewVersion = ((UpgradablePackage)package).NewVersion;
            else
                NewVersion = "";
        }
    }

    public class InstallationOptions
    {
        public class __serializable_options_v1
        {
            public bool SkipHashCheck { get; set; } = false;
            public bool InteractiveInstallation { get; set; } = false;
            public bool RunAsAdministrator { get; set; } = false;
            public string Architecture { get; set; } = "";
            public string InstallationScope { get; set; } = "";
            public List<string> CustomParameters { get; set; }
            public bool PreRelease { get; set; } = false;
            public string CustomInstallLocation { get; set; } = "";
            public string Version { get; set; } = "";
        }

        public bool SkipHashCheck { get; set; } = false;
        public bool InteractiveInstallation { get; set; } = false;
        public bool RunAsAdministrator { get; set; } = false;
        public string Version { get; set; } = "";
        public Architecture? Architecture { get; set; } = null;
        public PackageScope? InstallationScope { get; set; } = null;
        public List<string> CustomParameters { get; set; } = new List<string>();
        public bool RemoveDataOnUninstall { get; set; } = false;
        public bool PreRelease { get; set; } = false;
        public string CustomInstallLocation { get; set; } = "";

        public Package Package { get; }

        private string _saveFileName = "Unknown.Unknown.InstallationOptions";

        public InstallationOptions(Package package, bool reset = false)
        {
            Package = package;
            _saveFileName = Package.Manager.Name.Replace(" ", "").Replace(".", "") + "." + Package.Id;
            if (!reset)
            {
                LoadOptionsFromDisk();
            }
        }

        public InstallationOptions(UpgradablePackage package, bool reset = false) : this((Package)package, reset)
        {
            if (!reset)
                LoadOptionsFromDisk();
        }

        public void LoadFromJsonString(string JSON)
        {
            __serializable_options_v1 options = JsonSerializer.Deserialize<__serializable_options_v1>(JSON);
            SkipHashCheck = options.SkipHashCheck;
            InteractiveInstallation = options.InteractiveInstallation;
            RunAsAdministrator = options.RunAsAdministrator;
            CustomInstallLocation = options.CustomInstallLocation;
            Version = options.Version;
            PreRelease = options.PreRelease;
            if (options.Architecture != "" && CommonTranslations.InvertedArchNames.ContainsKey(options.Architecture))
                Architecture = CommonTranslations.InvertedArchNames[options.Architecture];
            if (options.InstallationScope != "" && CommonTranslations.InvertedScopeNames_NonLang.ContainsKey(options.InstallationScope))
                InstallationScope = CommonTranslations.InvertedScopeNames_NonLang[options.InstallationScope];
            CustomParameters = options.CustomParameters;
        }

        public string GetJsonString()
        {
            return JsonSerializer.Serialize(AsSerializable());
        }

        public __serializable_options_v1 AsSerializable()
        {
            __serializable_options_v1 options = new();
            options.SkipHashCheck = SkipHashCheck;
            options.InteractiveInstallation = InteractiveInstallation;
            options.RunAsAdministrator = RunAsAdministrator;
            options.CustomInstallLocation = CustomInstallLocation;
            options.PreRelease = PreRelease;
            options.Version = Version;
            if (Architecture != null)
                options.Architecture = CommonTranslations.ArchNames[Architecture.Value];
            if (InstallationScope != null)
                options.InstallationScope = CommonTranslations.ScopeNames_NonLang[InstallationScope.Value];
            options.CustomParameters = CustomParameters;
            return options;
        }

        public void SaveOptionsToDisk()
        {
            try
            {
                string JSON = GetJsonString();
                string filename = Path.Join(CoreData.WingetUIInstallationOptionsDirectory, Package.Manager.Name + "." + Package.Id + ".json");
                File.WriteAllText(filename, JSON);
            }
            catch (Exception ex)
            {
                AppTools.Log(ex);
            }
        }

        public void LoadOptionsFromDisk()
        {
            try
            {
                string filename = Path.Join(CoreData.WingetUIInstallationOptionsDirectory, Package.Manager.Name + "." + Package.Id + ".json");
                if (!File.Exists(filename))
                    return;
                LoadFromJsonString(File.ReadAllText(filename));
            }
            catch (Exception e)
            {
                AppTools.Log(e.ToString());
            }
        }

        public override string ToString()
        {
            string customparams = CustomParameters != null ? string.Join(",", CustomParameters) : "[]";
            return $"<InstallationOptions: SkipHashCheck={SkipHashCheck};" +
                   $"InteractiveInstallation={InteractiveInstallation};" +
                   $"RunAsAdministrator={RunAsAdministrator};" +
                   $"Version={Version};" +
                   $"Architecture={Architecture};" +
                   $"InstallationScope={InstallationScope};" +
                   $"InstallationScope={CustomInstallLocation};" +
                   $"CustomParameters={customparams};" +
                   $"RemoveDataOnUninstall={RemoveDataOnUninstall}>";
        }
    }
}

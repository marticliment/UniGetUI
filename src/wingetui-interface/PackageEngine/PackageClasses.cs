using Microsoft.WindowsAppSDK.Runtime;
using ModernWindow.Data;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;

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
    public class Package
    {
        public AppTools bindings = AppTools.Instance;
        public bool IsChecked { get; set; } = false;
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

        public PackageScope Scope { get; set; }
        public string SourceAsString { get {
                if (Source != null)
                    return Source.ToString();
                else return "";
            } }

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

        public Package _get_self_package()
        {
            return this;
        }

        public string GetIconId()
        {
            string iconId = Id.ToLower();
            if (Manager == bindings.App.Winget)
                iconId = String.Join('.', iconId.Split(".")[1..]);
            else if(Manager == bindings.App.Choco)
                iconId = iconId.Replace(".install", "").Replace(".portable", "");
            else if(Manager == bindings.App.Scoop)
                iconId = iconId.Replace(".app", "");
            return iconId;
        }

        public Uri GetIconUrl()
        {
            string iconId = GetIconId();
            // TODO: Look up icon URL from iconId
            return new Uri("ms-appx:///wingetui/resources/package_color.png");
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
            if(_ver != "" && _ver != ".")
                try
                {
                    return float.Parse(_ver);
                }
                catch(Exception e)
                {
                }
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
                catch (Exception e)
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
            foreach (var package in bindings.App.mainWindow.NavigationPage.InstalledPage.Packages)
                if(package.Manager == Manager && package.Id == Id && package.Version == NewVersion && package.Source.Name == Source.Name)
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
        public int InstallerSize { get; set; } = 0; // In Megabytes
        public Uri? ManifestUrl { get; set; } = null;
        public string UpdateDate { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public string ReleaseNotesUrl { get; set; } = "";
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
        private List<string> _dataToSave = new List<string>
        {
            "SkipHashCheck",
            "InteractiveInstallation",
            "RunAsAdministrator",
            "Architecture",
            "InstallationScope",
            "CustomParameters",
            "PreRelease",
            "CustomInstallLocation"
        };

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
        }

        public Dictionary<string, object> ToDictionary()
        {
            Dictionary<string, object> optionsToSave = new Dictionary<string, object>();
            foreach (string entry in _dataToSave)
            {
                optionsToSave[entry] = GetType().GetProperty(entry)?.GetValue(this);
            }
            return optionsToSave;
        }

        public void LoadFromDictionary(Dictionary<string, object> data)
        {
            foreach (string entry in _dataToSave)
            {
                if (data.ContainsKey(entry))
                {
                    GetType().GetProperty(entry)?.SetValue(this, data[entry]);
                }
            }
        }

        public void SaveOptionsToDisk()
        {
            // Implement your custom serialization method here.
            // Example: SaveJsonSettings(_saveFileName, ToDictionary(), "InstallationOptions");
        }

        public void LoadOptionsFromDisk()
        {
            // Implement your custom deserialization method here.
            // Example: LoadJsonSettings(_saveFileName, "InstallationOptions", LoadFromDictionary);
        }

        public override string ToString()
        {
            return $"<InstallationOptions: SkipHashCheck={SkipHashCheck};" +
                   $"InteractiveInstallation={InteractiveInstallation};" +
                   $"RunAsAdministrator={RunAsAdministrator};" +
                   $"Version={Version};" +
                   $"Architecture={Architecture};" +
                   $"InstallationScope={InstallationScope};" +
                   $"CustomParameters={string.Join(",", CustomParameters)};" +
                   $"RemoveDataOnUninstall={RemoveDataOnUninstall}>";
        }
    }
}

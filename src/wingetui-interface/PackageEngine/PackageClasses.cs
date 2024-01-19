using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace package_engine
{
    public class Package
    {
        public string Name { get; }
        public string Id { get; set; }
        public string Version { get; }
        public ManagerSource Source { get; set; }
        public PackageManager Manager { get; }
        public string UniqueId { get; }

        public Package(string name, string id, string version, ManagerSource source, PackageManager manager)
        {
            Name = name;
            Id = id;
            Version = version;
            Source = source;
            Manager = manager;
            UniqueId = $"{Manager.Properties.Name}\\{Id}\\{Version}";
        }

        public string GetIconId()
        {
            string iconId = Id.ToLower();
            if (IsWinget())
                iconId = String.Join('.', iconId.Split(".")[1..]);
            else if(IsChocolatey())
                iconId = iconId.Replace(".install", "").Replace(".portable", "");
            else if(IsScoop())
                iconId = iconId.Replace(".app", "");
            return iconId;
        }

        public string GetIconUrl()
        {
            string iconId = GetIconId();
            // TODO: Look up icon URL from iconId
            return "";
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
                    Console.WriteLine(e);
                }
            return res;
        }

        public bool IsWinget()
        {
            return false;
        }

        public bool IsChocolatey()
        {
            return false;
        }

        public bool IsScoop()
        {
            //TODO
            return false;
        }

        public void AddToIgnoredUpdates(string version = "*")
        {
            //TODO
        }

        public void RemoveFromIgnoredUpdates()
        {
            //TODO
        }

        public bool HasUpdatesIgnored(string version = "*")
        {
            // TODO
            return false;
        }

        public string GetIgnoredUpatesVersion()
        {
            // TODO
            return "";
        }



    }

    public class UpgradablePackage : Package
    {
        public string NewVersion { get; }
        public UpgradablePackage(string name, string id, string installed_version, string new_version, Source source, PackageManager manager) : base(name, id, installed_version, source, manager)
        {
            NewVersion = new_version;
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
        public string Architecture { get; set; } = "";
        public string InstallationScope { get; set; } = "";
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

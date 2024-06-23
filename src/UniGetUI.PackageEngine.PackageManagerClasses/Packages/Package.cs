using System.ComponentModel;
using System.ComponentModel.Design;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using UniGetUI.Core.Classes;
using UniGetUI.Core.Data;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Classes.Packages;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using Windows.Globalization;
using Windows.Storage.Search;

namespace UniGetUI.PackageEngine.PackageClasses
{
    public class Package : INotifyPropertyChanged
    {
        // Internal properties
        private bool __is_checked = false;
        public event PropertyChangedEventHandler? PropertyChanged;
        private PackageTag __tag;

        private readonly long __hash;
        private readonly long __versioned_hash;

        private PackageDetails? __details = null;
        public PackageDetails Details
        { 
            get => __details ??= new PackageDetails(this);
        }

        public PackageTag Tag
        {
            get  => __tag;
            set {
                __tag = value;
                OnPropertyChanged(nameof(Tag));
            }
        }
        public bool IsChecked
        {
            get { return __is_checked; }
            set { __is_checked = value; OnPropertyChanged(nameof(IsChecked)); }
        }

        public string Name { get; }
        public string Id { get; }
        public string Version { get; }
        public double VersionAsFloat { get; }
        public double NewVersionAsFloat { get; }
        public ManagerSource Source { get; }
        public PackageManager Manager { get; }
        public string NewVersion { get; }
        public virtual bool IsUpgradable { get; }
        public PackageScope Scope { get; set; }
        public readonly string SourceAsString;
        public readonly string AutomationName;

        /// <summary>
        /// Constuct a package with a given name, id, version, source and manager, and an optional scope.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <param name="version"></param>
        /// <param name="source"></param>
        /// <param name="manager"></param>
        /// <param name="scope"></param>
        public Package(string name, string id, string version, ManagerSource source, PackageManager manager, PackageScope scope = PackageScope.Local)
        {
            Name = name;
            Id = id;
            Version = version;
            VersionAsFloat = CoreTools.GetVersionStringAsFloat(version);
            Source = source;
            Manager = manager;
            Scope = scope;
            NewVersion = "";
            Tag = PackageTag.Default;
            SourceAsString = source.ToString();
            AutomationName = CoreTools.Translate("Package {name} from {manager}", new Dictionary<string, object?> { {"name", Name },{ "manager", SourceAsString } });
            __hash = CoreTools.HashStringAsLong(Manager.Name + "\\" + Source.Name + "\\" + Id);
            __versioned_hash = CoreTools.HashStringAsLong(Manager.Name + "\\" + Source.Name + "\\" + Id + "\\" + Version);
            IsUpgradable = false;
        }

        /// <summary>
        /// Creates an UpgradablePackage object representing a package that can be upgraded; given its name, id, installed version, new version, source and manager, and an optional scope.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <param name="installed_version"></param>
        /// <param name="new_version"></param>
        /// <param name="source"></param>
        /// <param name="manager"></param>
        /// <param name="scope"></param>
        public Package(string name, string id, string installed_version, string new_version, ManagerSource source, PackageManager manager, PackageScope scope = PackageScope.Local)
            : this(name, id, installed_version, source, manager, scope)
        {
            IsUpgradable = true;
            NewVersion = new_version;
            NewVersionAsFloat = CoreTools.GetVersionStringAsFloat(new_version);

            // Packages in the updates tab are checked by default
            IsChecked = true;
        }

        /// <summary>
        /// Returns an identifier that can be used to compare different packahe instances that refer to the same package.
        /// What is taken into account:
        ///    - Manager and Source
        ///    - Package Identifier
        /// For more specific comparsion use GetVersionedHash()
        /// </summary>
        /// <returns></returns>
        public long GetHash()
        {
            return __hash;
        }

        /// <summary>
        /// Returns an identifier that can be used to compare different packahe instances that refer to the same package.
        /// What is taken into account:
        ///    - Manager and Source
        ///    - Package Identifier
        ///    - Package version
        ///    - Package new version (if any)
        /// </summary>
        /// <returns></returns>
        public long GetVersionedHash()
        {
            return __versioned_hash;
        }

        /// <summary>
        /// Check wether two packages are **REALLY** the same package.
        /// What is taken into account:
        ///    - Manager and Source
        ///    - Package Identifier
        ///    - Package version
        ///    - Package new version (if any)
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override bool Equals(object? other)
        {
            return __versioned_hash == (other as Package)?.__versioned_hash;
        }

        /// <summary>
        /// Check wether two package instances represent the same package.
        /// What is taken into account:
        ///    - Manager and Source
        ///    - Package Identifier
        /// For more specific comparsion use package.Equals(object? other)
        /// </summary>
        /// <param name="other">A package</param>
        /// <returns>Wether the two instances refer to the same instance</returns>
        public bool IsEquivalentTo(Package? other)
        { 
            return __hash == other?.__hash;
        }

        /// <summary>
        /// Load the package's normalized icon id,
        /// </summary>
        /// <returns>a string with the package's normalized icon id</returns>
        public string GetIconId()
        {
            string iconId = Id.ToLower(); 
            if (Manager.Name == "Winget")
                iconId = string.Join('.', iconId.Split(".")[1..]);
            else if (Manager.Name == "Chocolatey")
                iconId = iconId.Replace(".install", "").Replace(".portable", "");
            else if (Manager.Name == "Scoop")
                iconId = iconId.Replace(".app", "");
            return iconId;
        }

        /// <summary>
        /// Get the package's icon url. If the package has no icon, a fallback image is returned.
        /// After calling this method, the returned URL points to a location on the local machine
        /// </summary>
        /// <returns>An always-valid URI object, pointing to a file:// or to a ms-appx:// URL</returns>
        public async Task<Uri> GetIconUrl()
        {
            try
            {
                string iconId = GetIconId();

                CacheableIcon? icon = await Manager.GetPackageIconUrl(this);
                string path = await IconCacheEngine.DownloadIconOrCache(icon, Manager.Name, Id);

                Uri Icon;
                if (path == "")
                    Icon = new Uri("ms-appx:///Assets/Images/package_color.png");
                else
                    Icon = new Uri("file:///" + path);

                Logger.Debug($"Icon for package {Id} was loaded from {Icon}");
                return Icon;
            }
            catch (Exception ex)
            {
                Logger.Error($"An error occurred while retrieving the icon for package {Id}");
                Logger.Error(ex);
                return new Uri("ms-appx:///Assets/Images/package_color.png");
            }
        }

        /// <summary>
        /// Retrieves a list og URIs representing the available screenshots for this package.
        /// </summary>
        /// <returns></returns>
        public async Task<Uri[]> GetPackageScreenshots()
        {
            return await Manager.GetPackageScreenshotsUrl(this);
        }


        /// <summary>
        /// Adds the package to the ignored updates list. If no version is provided, all updates are ignored.
        /// Calling this method will override older ignored updates.
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public async Task AddToIgnoredUpdatesAsync(string version = "*")
        {
            try
            {
                string IgnoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";
                JsonObject? IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;

                if (IgnoredUpdatesJson == null)
                    throw new Exception("The IgnoredUpdates database seems to be invalid!");

                if (IgnoredUpdatesJson.ContainsKey(IgnoredId))
                    IgnoredUpdatesJson.Remove(IgnoredId);
                IgnoredUpdatesJson.Add(IgnoredId, version);
                await File.WriteAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile, IgnoredUpdatesJson.ToString());
                GetInstalledPackage()?.SetTag(PackageTag.Pinned);
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not add package {Id} to ignored updates");
                Logger.Error(ex);
            }
        }

        /// <summary>
        /// Removes the package from the ignored updates list, either if it is ignored for all updates or for a specific version only.
        /// </summary>
        /// <returns></returns>
        public async Task RemoveFromIgnoredUpdatesAsync()
        {
            try
            {

                string IgnoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";
                JsonObject? IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;

                if (IgnoredUpdatesJson == null)
                    throw new Exception("The IgnoredUpdates database seems to be invalid!");

                if (IgnoredUpdatesJson.ContainsKey(IgnoredId))
                {
                    IgnoredUpdatesJson.Remove(IgnoredId);
                    await File.WriteAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile, IgnoredUpdatesJson.ToString());
                }

                GetInstalledPackage()?.SetTag(PackageTag.Default);
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not remove package {Id} from ignored updates");
                Logger.Error(ex);
            }
        }

        /// <summary>
        /// Returns true if the package's updates are ignored. If the version parameter
        /// is passed it will be checked if that version is ignored. Please note that if 
        /// all updates are ignored, calling this method with a specific version will 
        /// still return true, although the passed version is not explicitly ignored. 
        /// </summary>
        /// <param name="Version"></param>
        /// <returns></returns>
        public async Task<bool> HasUpdatesIgnoredAsync(string Version = "*")
        {
            try
            {
                string IgnoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";
                JsonObject? IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;

                if (IgnoredUpdatesJson == null)
                    throw new Exception("The IgnoredUpdates database seems to be invalid!");

                if (IgnoredUpdatesJson.ContainsKey(IgnoredId) && (IgnoredUpdatesJson[IgnoredId]?.ToString() == "*" || IgnoredUpdatesJson[IgnoredId]?.ToString() == Version))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not check whether package {Id} has updates ignored");
                Logger.Error(ex);
                return false;
            }

        }

        /// <summary>
        /// Returns (as a string) the version for which a package has been ignored. When no versions 
        /// are ignored, an empty string will be returned; and when all versions are ignored an asterisk
        /// will be returned.
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetIgnoredUpdatesVersionAsync()
        {
            try
            {
                string IgnoredId = $"{Manager.Properties.Name.ToLower()}\\{Id}";
                JsonObject? IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;

                if (IgnoredUpdatesJson == null)
                    throw new Exception("The IgnoredUpdates database seems to be invalid!");
                
                if (IgnoredUpdatesJson.ContainsKey(IgnoredId))
                    return IgnoredUpdatesJson[IgnoredId]?.ToString() ?? "";
                else
                    return "";
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not retrieve the ignored updates version for package {Id}");
                Logger.Error(ex);
                return "";
            }
        }

        /// <summary>
        /// Internal method to raise the PropertyChanged event.
        /// </summary>
        /// <param name="name"></param>
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// Returns the corresponding installed Package object. Will return null if not applicable
        /// </summary>
        /// <returns>a Package object if found, null if not</returns>
        public Package? GetInstalledPackage()
        {
            return PackageCacher.GetInstalledPackageOrNull(this);
        }

        /// <summary>
        /// Returns the corresponding available Package object. Will return null if not applicable
        /// </summary>
        /// <returns>a Package object if found, null if not</returns>
        public Package? GetAvailablePackage()
        {
            return PackageCacher.GetAvailablePackageOrNull(this);
        }

        /// <summary>
        /// Returns the corresponding upgradable Package object. Will return null if not applicable
        /// </summary>
        /// <returns>a Package object if found, null if not</returns>
        public Package? GetUpgradablePackage()
        {
            return PackageCacher.GetUpgradablePackageOrNull(this);
        }

        /// <summary>
        /// Sets the package tag. You may as well use the Tag property.
        /// This function is used for compatibility with the ? operator
        /// </summary>
        /// <param name="tag"></param>
        public void SetTag(PackageTag tag)
        {
            Tag = tag;
        }

        public bool NewerVersionIsInstalled()
        {
            if(!IsUpgradable) return false;
            return PackageCacher.NewerVersionIsInstalled(this);
        }

    }
}

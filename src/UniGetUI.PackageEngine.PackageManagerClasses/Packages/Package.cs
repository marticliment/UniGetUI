using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml;
using UniGetUI.Core.Classes;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Packages;
using UniGetUI.PackageEngine.Classes.Packages.Classes;
using UniGetUI.PackageEngine.Classes.Serializable;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Structs;

namespace UniGetUI.PackageEngine.PackageClasses
{
    public class Package : IPackage
    {
        // Internal properties
        private bool __is_checked;
        public event PropertyChangedEventHandler? PropertyChanged;
        private PackageTag __tag;

        private readonly long __hash;
        private readonly long __versioned_hash;
        private readonly string ignoredId;
        private readonly string _iconId;

        private static ConcurrentDictionary<int, Uri?> _cachedIconPaths = new();

        private IPackageDetails? __details;
        public IPackageDetails Details
        {
            get => __details ??= new PackageDetails(this);
        }

        public PackageTag Tag
        {
            get => __tag;
            set { __tag = value; OnPropertyChanged(); }
        }

        public bool IsChecked
        {
            get => __is_checked;
            set { __is_checked = value; OnPropertyChanged(); }
        }

        private OverridenInstallationOptions _overriden_options;
        public ref OverridenInstallationOptions OverridenOptions { get => ref _overriden_options; }
        public string Name { get; }
        public string AutomationName { get; }
        public string Id { get; }
        public virtual string Version { get; }
        public double VersionAsFloat { get; }
        public double NewVersionAsFloat { get; }
        public bool IsPopulated { get; set; }
        public IManagerSource Source { get; }

        /// <summary>
        /// IPackageManager is guaranteed to be PackageManager, but C# doesn't allow covariant attributes
        /// </summary>
        public IPackageManager Manager { get; }
        public string NewVersion { get; }
        public virtual bool IsUpgradable { get; }

        /// <summary>
        /// Construct a package with a given name, id, version, source and manager, and an optional scope.
        /// </summary>
        public Package(
            string name,
            string id,
            string version,
            IManagerSource source,
            IPackageManager manager,
            OverridenInstallationOptions? options = null)
        {
            Name = name;
            Id = id;
            Version = version;
            VersionAsFloat = CoreTools.GetVersionStringAsFloat(version);
            Source = source;
            Manager = manager;

            if (options is not null)
            {
                _overriden_options = (OverridenInstallationOptions)options;
            }

            NewVersion = "";
            Tag = PackageTag.Default;
            AutomationName = CoreTools.Translate("Package {name} from {manager}",
                new Dictionary<string, object?> { { "name", Name }, { "manager", Source.AsString_DisplayName } });

            __hash = CoreTools.HashStringAsLong(Manager.Name + "\\" + Source.AsString_DisplayName + "\\" + Id);
            __versioned_hash = CoreTools.HashStringAsLong(Manager.Name + "\\" + Source.AsString_DisplayName + "\\" + Id + "\\" + (this as Package).Version);
            IsUpgradable = false;

            ignoredId = IgnoredUpdatesDatabase.GetIgnoredIdForPackage(this);

            _iconId = Manager.Name switch
            {
                "Winget" => string.Join('.', id.ToLower().Split(".")[1..]),
                "Scoop" => id.ToLower().Replace(".app", ""),
                "Chocolatey" => id.ToLower().Replace(".install", "").Replace(".portable", ""),
                _ => id.ToLower()
            };
        }

        /// <summary>
        /// Creates an UpgradablePackage object representing a package that can be upgraded; given its name, id, installed version, new version, source and manager, and an optional scope.
        /// </summary>
        public Package(
            string name,
            string id,
            string installed_version,
            string new_version,
            IManagerSource source,
            IPackageManager manager,
            OverridenInstallationOptions? options = null)
            : this(name, id, installed_version, source, manager, options)
        {
            IsUpgradable = true;
            NewVersion = new_version;
            NewVersionAsFloat = CoreTools.GetVersionStringAsFloat(new_version);
        }

        public long GetHash()
            => __hash;

        public long GetVersionedHash()
            => __versioned_hash;

        public bool Equals(IPackage? other)
            => __versioned_hash == other?.GetHash();

        public override int GetHashCode()
            => (int)__versioned_hash;

        /// <summary>
        /// Check whether two package instances represent the same package.
        /// What is taken into account:
        ///    - Manager and Source
        ///    - Package Identifier
        /// For more specific comparison use package.Equals(object? other)
        /// </summary>
        /// <param name="other">A package</param>
        /// <returns>Whether the two instances refer to the same instance</returns>
        public bool IsEquivalentTo(IPackage? other)
            => __hash == other?.GetHash();

        public string GetIconId()
            => _iconId;

        public virtual Uri GetIconUrl()
        {
            return GetIconUrlIfAny() ?? new Uri("ms-appx:///Assets/Images/package_color.png");
        }

        public virtual Uri? GetIconUrlIfAny()
        {
            if (_cachedIconPaths.TryGetValue(this.GetHashCode(), out Uri? path))
            {
                return path;
            }
            var CachedIcon = LoadIconUrlIfAny();
            _cachedIconPaths.TryAdd(this.GetHashCode(), CachedIcon);
            return CachedIcon;
        }

        private Uri? LoadIconUrlIfAny()
        {
            try
            {
                CacheableIcon? icon = TaskRecycler<CacheableIcon?>.RunOrAttach(Manager.DetailsHelper.GetIcon, this);
                string? path = IconCacheEngine.GetCacheOrDownloadIcon(icon, Manager.Name, Id);
                return path is null? null: new Uri("file:///" + path);
            }
            catch (Exception ex)
            {
                Logger.Error($"An error occurred while retrieving the icon for package {Id}");
                Logger.Error(ex);
                return null;
            }
        }

        public virtual IEnumerable<Uri> GetScreenshots()
        {
            return Manager.DetailsHelper.GetScreenshots(this);
        }

        public virtual async Task AddToIgnoredUpdatesAsync(string version = "*")
        {
            try
            {
                await Task.Run(() => IgnoredUpdatesDatabase.Add(ignoredId, version));
                GetInstalledPackage()?.SetTag(PackageTag.Pinned);
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not add package {Id} to ignored updates");
                Logger.Error(ex);
            }
        }

        public virtual async Task RemoveFromIgnoredUpdatesAsync()
        {
            try
            {
                await Task.Run(() => IgnoredUpdatesDatabase.Remove(ignoredId));
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
        public virtual async Task<bool> HasUpdatesIgnoredAsync(string version = "*")
        {
            try
            {
                return await Task.Run(() => IgnoredUpdatesDatabase.HasUpdatesIgnored(ignoredId, version));
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
        public virtual async Task<string> GetIgnoredUpdatesVersionAsync()
        {
            try
            {
                return await Task.Run(() => IgnoredUpdatesDatabase.GetIgnoredVersion(ignoredId)) ?? "";
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not retrieve the ignored updates version for package {Id}");
                Logger.Error(ex);
                return "";
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public IPackage? GetInstalledPackage()
            => PackageCacher.GetInstalledPackageOrNull(this);

        public IPackage? GetAvailablePackage()
            => PackageCacher.GetAvailablePackageOrNull(this);

        public IPackage? GetUpgradablePackage()
            => PackageCacher.GetUpgradablePackageOrNull(this);

        public virtual void SetTag(PackageTag tag)
        {
            Tag = tag;
        }

        public virtual bool NewerVersionIsInstalled()
        {
            if (!IsUpgradable)
                return false;

            return PackageCacher.NewerVersionIsInstalled(this);
        }

        public virtual SerializablePackage_v1 AsSerializable()
        {
            return new SerializablePackage_v1
            {
                Id = Id,
                Name = Name,
                Version = Version,
                Source = Source.Name,
                ManagerName = Manager.Name,
                InstallationOptions = InstallationOptions.FromPackage(this).AsSerializable(),
                Updates = new SerializableUpdatesOptions_v1
                {
                    IgnoredVersion = GetIgnoredUpdatesVersionAsync().GetAwaiter().GetResult(),
                    UpdatesIgnored = HasUpdatesIgnoredAsync().GetAwaiter().GetResult(),
                }
            };
        }

        public SerializableIncompatiblePackage_v1 AsSerializable_Incompatible()
        {
            return new SerializableIncompatiblePackage_v1
            {
                Id = Id,
                Name = Name,
                Version = Version,
                Source = Source.Name,
            };
        }

        public static void ResetIconCache()
        {
            _cachedIconPaths.Clear();
        }
    }
}


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using UniGetUI.Core.Data;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.Core.Logging;
using UniGetUI.Core.IconEngine;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.PackageClasses
{
    public class Package : INotifyPropertyChanged
    {
        // Internal properties
        private bool __is_checked = false;
        public event PropertyChangedEventHandler? PropertyChanged;
        private string __listed_icon_id;
        private string __name_tooltip;
        private PackageTag __tag;
        private float __opacity;
        private bool __show_icon_highlight;
        private string __hash;

        public PackageTag Tag
        {
            get { return __tag; }

            set
            {
                __tag = value;
                switch (__tag)
                {
                    case PackageTag.Default:
                        ListedIconId = "install";
                        ListIconShowHighlight = false;
                        ListedOpacity = 1;
                        ListedNameTooltip = Name;
                        break;

                    case PackageTag.AlreadyInstalled:
                        ListedIconId = "installed";
                        ListIconShowHighlight = true;
                        ListedOpacity = 1;
                        ListedNameTooltip = CoreTools.Translate("This package is already installed") + " - " + Name;
                        break;

                    case PackageTag.IsUpgradable:
                        ListedIconId = "update";
                        ListIconShowHighlight = true;
                        ListedOpacity = 1;
                        ListedNameTooltip = CoreTools.Translate("This package can be updated") + " - " + Name;
                        break;

                    case PackageTag.Pinned:
                        ListedIconId = "pin_fill";
                        ListIconShowHighlight = false;
                        ListedOpacity = 1;
                        ListedNameTooltip = CoreTools.Translate("Updates for this package are ignored") + " - " + Name;
                        break;

                    case PackageTag.OnQueue:
                        ListedIconId = "sandclock";
                        ListIconShowHighlight = false;
                        ListedOpacity = .5F;
                        ListedNameTooltip = CoreTools.Translate("This package is on the queue") + " - " + Name;
                        break;

                    case PackageTag.BeingProcessed:
                        ListedIconId = "gears";
                        ListIconShowHighlight = false;
                        ListedOpacity = .5F;
                        ListedNameTooltip = CoreTools.Translate("This package is being processed") + " - " + Name;
                        break;

                    case PackageTag.Failed:
                        ListedIconId = "stop";
                        ListIconShowHighlight = true;
                        ListedOpacity = 1;
                        ListedNameTooltip = CoreTools.Translate("An error occurred while processing this package") + " - " + Name;
                        break;
                }
            }
        }

        // Public properties
        public bool ListIconShowHighlight
        {
            get { return __show_icon_highlight; }
            set { __show_icon_highlight = value; OnPropertyChanged(); }
        }

        public bool IsChecked
        {
            get { return __is_checked; }
            set { __is_checked = value; OnPropertyChanged(); }
        }

        public string ListedIconId
        {
            set { __listed_icon_id = value; OnPropertyChanged(); }
            get { return __listed_icon_id; }
        }

        public string ListedNameTooltip
        {
            get { return __name_tooltip; }
            set { __name_tooltip = value; OnPropertyChanged(); }
        }

        public float ListedOpacity
        {
            get { return __opacity; }
            set { __opacity = value; OnPropertyChanged(); }
        }

        public string IsCheckedAsString { get { return IsChecked ? "True" : "False"; } }
        public string Name { get; }
        public string Id { get; set; }
        public string Version { get; }
        public float VersionAsFloat { get; }
        public ManagerSource Source { get; set; }
        public PackageManager Manager { get; }
        public string UniqueId { get; }
        public string NewVersion { get; set; }
        public virtual bool IsUpgradable { get; } = false;
        public PackageScope Scope { get; set; }
        public string SourceAsString
        {
            get
            {
                if (Source != null) return Source.ToString();
                else return "";
            }
        }

        /// <summary>
        /// Construct a package with a given name, id, version, source and manager, and an optional scope.
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
            Source = source;
            Manager = manager;
            Scope = scope;
            UniqueId = $"{Manager.Properties.Name}\\{Id}\\{Version}";
            NewVersion = "";
            VersionAsFloat = GetFloatVersion();
            Tag = PackageTag.Default;
            __hash = Manager.Name + "\\" + Source.Name + "\\" + Id;
        }

        public string GetHash()
        {
            return __hash;
        }


        /// <summary>
        /// Internal method
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object? obj)
        {
            if (!(obj is Package))
                return false;
            else
                return (obj as Package)?.GetHash() == GetHash();
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
            else if (Manager.Name == "vcpkg")
                iconId = iconId.split(":")[0].Split("[")[0];
            return iconId;
        }

        /// <summary>
        /// Get the package's icon url. If the package has no icon, a fallback image is returned.
        /// </summary>
        /// <returns>An always-valid URI object</returns>
        public Uri GetIconUrl()
        {
            string iconId = GetIconId();
            if (IconDatabase.Instance.GetIconUrlForId(iconId) != "")
                return new Uri(IconDatabase.Instance.GetIconUrlForId(iconId));

            return new Uri("ms-appx:///Assets/Images/package_color.png");
        }

        /// <summary>
        /// Returns a float representation of the package's version for comparison purposes.
        /// </summary>
        /// <returns>A float value. Returns 0.0F if the version could not be parsed</returns>
        public float GetFloatVersion()
        {
            string _ver = "";
            bool _dotAdded = false;
            foreach (char _char in Version)
            {
                if (char.IsDigit(_char))
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
                JsonObject IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;
                if (IgnoredUpdatesJson.ContainsKey(IgnoredId))
                    IgnoredUpdatesJson.Remove(IgnoredId);
                IgnoredUpdatesJson.Add(IgnoredId, version);
                await File.WriteAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile, IgnoredUpdatesJson.ToString());
                // FIXME: Tools.App.MainWindow.NavigationPage.UpdatesPage.RemoveCorrespondingPackages(this);

                GetInstalledPackage()?.SetTag(PackageTag.Pinned);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
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
                JsonObject IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;
                if (IgnoredUpdatesJson.ContainsKey(IgnoredId))
                {
                    IgnoredUpdatesJson.Remove(IgnoredId);
                    await File.WriteAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile, IgnoredUpdatesJson.ToString());
                }

                GetInstalledPackage()?.SetTag(PackageTag.Default);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
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
                JsonObject IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;
                if (IgnoredUpdatesJson.ContainsKey(IgnoredId) && (IgnoredUpdatesJson[IgnoredId].ToString() == "*" || IgnoredUpdatesJson[IgnoredId].ToString() == Version))
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
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
                JsonObject IgnoredUpdatesJson = JsonNode.Parse(await File.ReadAllTextAsync(CoreData.IgnoredUpdatesDatabaseFile)) as JsonObject;
                if (IgnoredUpdatesJson.ContainsKey(IgnoredId))
                    return IgnoredUpdatesJson[IgnoredId].ToString();
                else
                    return "";
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return "";
            }
        }

        /// <summary>
        /// Internal method to raise the PropertyChanged event.
        /// </summary>
        /// <param name="name"></param>
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// Returns the corresponding installed Package object. Will return null if not applicable
        /// </summary>
        /// <returns>a Package object if found, null if not</returns>
        public Package? GetInstalledPackage()
        {
            throw new NotImplementedException();
            /* foreach (Package package in Tools.App.MainWindow.NavigationPage.InstalledPage.Packages)
                if (package.Equals(this))
                    return package;
            return null;
            */
        }

        /// <summary>
        /// Returns the corresponding available Package object. Will return null if not applicable
        /// </summary>
        /// <returns>a Package object if found, null if not</returns>
        public Package? GetAvailablePackage()
        {
            throw new NotImplementedException();
            /*
             * foreach (Package package in Tools.App.MainWindow.NavigationPage.DiscoverPage.Packages)
                if (package.Equals(this))
                    return package;
            return null;
            */
        }

        /// <summary>
        /// Returns the corresponding upgradable Package object. Will return null if not applicable
        /// </summary>
        /// <returns>a Package object if found, null if not</returns>
        public Package? GetUpgradablePackage()
        {
            throw new NotImplementedException();
            /*
            foreach (UpgradablePackage package in Tools.App.MainWindow.NavigationPage.UpdatesPage.Packages)
                if (package.Equals(this))
                    return package;
            return null;
            */
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

    }
}

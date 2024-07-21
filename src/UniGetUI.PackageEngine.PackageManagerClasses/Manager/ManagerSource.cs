using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.Interface.Enums;

namespace UniGetUI.PackageEngine.Classes.Manager
{
    public class ManagerSource : IManagerSource
    {
        public virtual IconType IconId { get { return Manager.Properties.IconId; } }
        public readonly bool IsVirtualManager;
        public struct Capabilities
        {
            public bool KnowsUpdateDate { get; set; } = false;
            public bool KnowsPackageCount { get; set; } = false;
            public bool MustBeInstalledAsAdmin { get; set; } = false;
            public Capabilities()
            { }
        }

        public IPackageManager Manager { get; }
        public string Name { get; }
        public Uri Url { get; set; }
        public int? PackageCount { get; }
        public string UpdateDate { get; }
        public string AsString { get; protected set; }
        public string AsString_DisplayName { get; protected set; }

        public ManagerSource(IPackageManager manager, string name, Uri url, int? packageCount = 0, string updateDate = "", bool isVirtualManager = false)
        {
            IsVirtualManager = isVirtualManager;
            Manager = manager;
            Name = name;
            Url = url;
            if (manager.Capabilities.Sources.KnowsPackageCount)
            {
                PackageCount = packageCount;
            }

            UpdateDate = updateDate;

            AsString = Manager.Capabilities.SupportsCustomSources ? $"{Manager.Name}: {Name}" : Name;
            if (Manager.Capabilities.SupportsCustomScopes && Manager.Properties.DisplayName is not null)
            {
                AsString_DisplayName = $"{Manager.DisplayName}: {Name}";
            }
            else
            {
                AsString_DisplayName = AsString;
            }
        }

        public override string ToString()
        {
            throw new NotImplementedException("Use the `AsString` attribute instead");
        }

        /// <summary>
        /// Replaces the current URL with the new one. Must be used only when a placeholder URL is used.
        /// </summary>
        /// <param name="newUrl"></param>
        public void ReplaceUrl(Uri newUrl)
        {
            Url = newUrl;
        }
    }
}

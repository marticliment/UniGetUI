using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers
{
    public class ManagerSource
    {
        public virtual string IconId { get { return Manager.Properties.IconId; } }
        public readonly bool IsVirtualManager;
        public struct Capabilities
        {
            public bool KnowsUpdateDate { get; set; } = false;
            public bool KnowsPackageCount { get; set; } = false;
            public bool MustBeInstalledAsAdmin { get; set; } = false;
            public Capabilities()
            { }
        }

        public PackageManager Manager { get; }
        public string Name { get; }
        public Uri Url { get; private set; }
        public int? PackageCount { get; }
        public string UpdateDate { get; }
        public string AsString { get; protected set; }
        public string AsString_DisplayName { get; protected set; }

        public ManagerSource(PackageManager manager, string name, Uri url, int? packageCount = 0, string updateDate = "", bool isVirtualManager = false)
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
        public void ReplaceUrl(Uri newUrl)
        {
            Url = newUrl;
        }
    }
}

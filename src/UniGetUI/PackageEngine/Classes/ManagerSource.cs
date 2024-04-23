using System;

namespace UniGetUI.PackageEngine.Classes
{
    public class ManagerSource
    {
        public virtual string IconId { get { return Manager.Properties.IconId; } }
        public bool IsVirtualManager = false;
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

        public ManagerSource(PackageManager manager, string name, Uri url = null, int? packageCount = 0, string updateDate = null)
        {
            Manager = manager;
            Name = name;
            Url = url;
            if (manager.Capabilities.Sources.KnowsPackageCount)
                PackageCount = packageCount;
            if (manager.Capabilities.Sources.KnowsUpdateDate)
                UpdateDate = updateDate;
        }

        public override string ToString()
        {
            if (Manager.Capabilities.SupportsCustomSources)
                return Manager.Name + ": " + Name;
            else
                return Manager.Name;
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

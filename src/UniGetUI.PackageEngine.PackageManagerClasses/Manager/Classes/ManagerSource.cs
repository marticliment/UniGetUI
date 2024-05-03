using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers
{
    public class ManagerSource
    {
        public virtual string IconId { get { return Manager.Properties.IconId; } }
        public readonly bool IsVirtualManager = false;
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

        public ManagerSource(PackageManager manager, string name, Uri url, int? packageCount = 0, string updateDate = "", bool isVirtualManager = false)
        {
            IsVirtualManager = isVirtualManager;
            Manager = manager;
            Name = name;
            Url = url;
            if (manager.Capabilities.Sources.KnowsPackageCount)
                PackageCount = packageCount;

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

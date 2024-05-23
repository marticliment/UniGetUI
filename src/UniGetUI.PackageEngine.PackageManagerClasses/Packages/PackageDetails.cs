using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;

namespace UniGetUI.PackageEngine.PackageClasses
{
    /// <summary>
    /// The properties of a given package.
    /// </summary>
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

}

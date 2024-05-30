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
        public string? Description { get; set; } = null;
        public string? Publisher { get; set; } = null;
        public string? Author { get; set; } = null;
        public Uri? HomepageUrl { get; set; } = null;
        public string? License { get; set; } = null;
        public Uri? LicenseUrl { get; set; } = null;
        public Uri? InstallerUrl { get; set; } = null;
        public string? InstallerHash { get; set; } = null;
        public string? InstallerType { get; set; } = null;
        public double InstallerSize { get; set; } = 0; // In Megabytes
        public Uri? ManifestUrl { get; set; } = null;
        public string? UpdateDate { get; set; } = null;
        public string? ReleaseNotes { get; set; } = null;
        public Uri? ReleaseNotesUrl { get; set; } = null;
        public string[] Tags { get; set; } = new string[0];

        public PackageDetails(Package package)
        {
            Package = package;
        }
    }

}

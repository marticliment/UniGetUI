using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Classes.Manager.Interfaces
{
    internal interface IPackageDetailsProvider
    {
        /// <summary>
        /// Returns a PackageDetails object that represents the details for the given Package object.
        /// This method is fail-safe and will return a valid but empty PackageDetails object with the package 
        /// id if an error occurs.
        /// </summary>
        /// <param name="package"></param>
        /// <returns>A PackageDetails object</returns>
        public abstract Task<PackageDetails> GetPackageDetails(Package package);
        

        /// <summary>
        /// Returns the available versions to install for the given package. 
        /// If the manager does not support listing the versions, an empty array will be returned.
        /// This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        /// <param name="package">The package from which to load its versions</param>
        /// <returns>An array of stings containing the found versions, an empty array if none.</returns>
        public abstract Task<string[]> GetPackageVersions(Package package);

        /// <summary>
        /// Returns the path to the icon of this package. 
        /// The icon will be downloaded and cached
        /// This method is fail-safe and will return the path to the generic package icon if it fails.
        /// </summary>
        /// <param name="package">The package from which to load the icon</param>
        /// <returns>A full path to a valid icon file</returns>
        public abstract Task<string> GetPackageIcon(Package package);

        /// <summary>
        /// Returns the path to the screenshots (if any) of this package. 
        /// The screenshots will be downloaded and cached
        /// This method is fail-safe and will return an empty array if it fails.
        /// </summary>
        /// <param name="package">The package from which to load the screenshots</param>
        /// <returns>An array with valid paths to screenshots</returns>
        public abstract Task<string[]> GetPackageScreenshots(Package package);

    }
}

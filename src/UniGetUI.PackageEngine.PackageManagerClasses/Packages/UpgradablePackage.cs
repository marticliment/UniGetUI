using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Classes.Packages;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.PackageClasses
{


    public class UpgradablePackage : Package
    {
        // Public properties
        public float NewVersionAsFloat { get; }
        public override bool IsUpgradable { get; } = true;

        private string __hash;

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
        public UpgradablePackage(string name, string id, string installed_version, string new_version, ManagerSource source, PackageManager manager, PackageScope scope = PackageScope.Local) : base(name, id, installed_version, source, manager, scope)
        {
            NewVersion = new_version;
            IsChecked = true;
            NewVersionAsFloat = GetFloatNewVersion();

            __hash = Manager.Name + "\\" + Source.Name + "\\" + Id + "\\" + Version + "->" + NewVersion;
        }
        public new string GetHash()
        {
            return __hash;
        }

        /// <summary>
        /// Returns a float value representing the new new version of the package, for comparison purposes.
        /// </summary>
        /// <returns></returns>
        public float GetFloatNewVersion()
        {
            string _ver = "";
            bool _dotAdded = false;
            foreach (char _char in NewVersion)
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
                catch (Exception)
                {
                }
            return res;
        }

        /// <summary>
        /// This version will check if the new version of the package is already present 
        /// on the InstalledPackages list, to prevent already installed updates from being updated again.
        /// </summary>
        /// <returns></returns>
        public bool NewVersionIsInstalled()
        {
            return PackageFactory.NewerVersionIsInstalled(this);
        }
    }

}

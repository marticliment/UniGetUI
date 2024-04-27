using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;

namespace UniGetUI.PackageEngine.ManagerClasses.Manager
{
    public struct ManagerCapabilities
    {
        public bool IsDummy = false;
        public bool CanRunAsAdmin = false;
        public bool CanSkipIntegrityChecks = false;
        public bool CanRunInteractively = false;
        public bool CanRemoveDataOnUninstall = false;
        public bool SupportsCustomVersions = false;
        public bool SupportsCustomArchitectures = false;
        public Architecture[] SupportedCustomArchitectures = new Architecture[0];
        public bool SupportsCustomScopes = false;
        public bool SupportsPreRelease = false;
        public bool SupportsCustomLocations = false;
        public bool SupportsCustomSources = false;
        public ManagerSource.Capabilities Sources { get; set; }
        public ManagerCapabilities()
        {
            Sources = new ManagerSource.Capabilities();
        }

        public ManagerCapabilities(bool IsDummy)
        {
            Sources = new ManagerSource.Capabilities();
            this.IsDummy = IsDummy;
        }
    }
}

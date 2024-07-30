using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.PackageEngine.Enums;
using Windows.Devices.Bluetooth.Advertisement;

namespace UniGetUI.PackageEngine.Structs;
public struct OverridenInstallationOptions
{
    public PackageScope? Scope;
    public bool? RunAsAdministrator;

    public OverridenInstallationOptions(PackageScope? scope = null, bool? runAsAdministrator = null)
    {
        Scope = scope;
        RunAsAdministrator = runAsAdministrator;
    }
}

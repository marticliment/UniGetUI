using UniGetUI.PackageEngine.Enums;

namespace UniGetUI.PackageEngine.Structs;
public struct OverridenInstallationOptions
{
    public PackageScope? Scope;
    public bool? RunAsAdministrator;
    public bool PowerShell_DoNotSetScopeParameter = false;
    public bool? WinGet_SpecifyVersion = null;

    public OverridenInstallationOptions(PackageScope? scope = null, bool? runAsAdministrator = null)
    {
        Scope = scope;
        RunAsAdministrator = runAsAdministrator;
    }

    public override string ToString()
    {
        return $"<Scope={Scope};RunAsAdministrator={RunAsAdministrator};WG_SpecifyVersion={WinGet_SpecifyVersion};PS_NoScope={PowerShell_DoNotSetScopeParameter}>";
    }
}

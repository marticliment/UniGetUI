namespace UniGetUI.PackageEngine.Classes
{
    /// <summary>
    /// Represents the scope of a package. To be coherent with package manager naming, the values are repeated.
    /// </summary>
    public enum PackageScope
    {
        // Repeated entries for coherence with Package Managers
        Global = 1,
        Machine = 1,
        Local = 0,
        User = 0,
    }
}

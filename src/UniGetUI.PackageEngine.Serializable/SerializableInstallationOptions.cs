namespace UniGetUI.PackageEngine.Serializable
{
    public class SerializableInstallationOptions_v1
    {
        public bool SkipHashCheck { get; set; } = false;
        public bool InteractiveInstallation { get; set; } = false;
        public bool RunAsAdministrator { get; set; } = false;
        public string Architecture { get; set; } = "";
        public string InstallationScope { get; set; } = "";
        public List<string> CustomParameters { get; set; } = [];
        public bool PreRelease { get; set; } = false;
        public string CustomInstallLocation { get; set; } = "";
        public string Version { get; set; } = "";
    }
}

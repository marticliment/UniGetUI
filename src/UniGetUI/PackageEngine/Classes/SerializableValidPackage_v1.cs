namespace UniGetUI.PackageEngine.Classes
{
    public class SerializableValidPackage_v1
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public string Source { get; set; } = "";
        public string ManagerName { get; set; } = "";
        public SerializableInstallationOptions_v1 InstallationOptions { get; set; }
        public SerializableUpdatesOptions_v1 Updates { get; set; }
    }

}

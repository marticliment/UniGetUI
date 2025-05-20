using System.Text.Json.Nodes;

namespace UniGetUI.PackageEngine.Serializable
{
    public class SerializableInstallationOptions: SerializableComponent
    {
        public bool SkipHashCheck { get; set; }
        public bool InteractiveInstallation { get; set; }
        public bool RunAsAdministrator { get; set; }
        public string Architecture { get; set; } = "";
        public string InstallationScope { get; set; } = "";
        public List<string> CustomParameters { get; set; } = [];
        public bool PreRelease { get; set; }
        public string CustomInstallLocation { get; set; } = "";
        public string Version { get; set; } = "";
        public bool SkipMinorUpdates { get; set; }

        public override SerializableInstallationOptions Copy()
        {
            return new()
            {
                SkipHashCheck = SkipHashCheck,
                Architecture = Architecture,
                CustomInstallLocation = CustomInstallLocation,
                CustomParameters = CustomParameters,
                InstallationScope = InstallationScope,
                InteractiveInstallation = InteractiveInstallation,
                PreRelease = PreRelease,
                RunAsAdministrator = RunAsAdministrator,
                Version = Version,
                SkipMinorUpdates = SkipMinorUpdates,
            };
        }

        public override void LoadFromJson(JsonNode data)
        {
            this.SkipHashCheck = data[nameof(SkipHashCheck)]?.GetValue<bool>() ?? false;
            this.InteractiveInstallation = data[nameof(InteractiveInstallation)]?.GetValue<bool>() ?? false;
            this.RunAsAdministrator = data[nameof(RunAsAdministrator)]?.GetValue<bool>() ?? false;
            this.Architecture = data[nameof(Architecture)]?.GetValue<string>() ?? "";
            this.InstallationScope = data[nameof(InstallationScope)]?.GetValue<string>() ?? "";

            this.CustomParameters = new List<string>();
            foreach(var element in data[nameof(CustomParameters)]?.AsArray() ?? [])
                if (element is not null) this.CustomParameters.Add(element.GetValue<string>());

            this.PreRelease = data[nameof(PreRelease)]?.GetValue<bool>() ?? false;
            this.CustomInstallLocation = data[nameof(CustomInstallLocation)]?.GetValue<string>() ?? "";
            this.Version = data[nameof(Version)]?.GetValue<string>() ?? "";
            this.SkipMinorUpdates = data[nameof(SkipMinorUpdates)]?.GetValue<bool>() ?? false;
        }
    }
}

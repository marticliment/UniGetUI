using System.Text.Json.Nodes;

namespace UniGetUI.PackageEngine.Serializable
{
    public class SerializableInstallationOptions_v1
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

        public SerializableInstallationOptions_v1 Copy()
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

        public static SerializableInstallationOptions_v1 FromJsonString(JsonNode data)
        {
            var options = new SerializableInstallationOptions_v1();
            options.SkipHashCheck = data[nameof(SkipHashCheck)]?.GetValue<bool>() ?? false;
            options.InteractiveInstallation = data[nameof(InteractiveInstallation)]?.GetValue<bool>() ?? false;
            options.RunAsAdministrator = data[nameof(RunAsAdministrator)]?.GetValue<bool>() ?? false;
            options.Architecture = data[nameof(Architecture)]?.GetValue<string>() ?? "";
            options.InstallationScope = data[nameof(InstallationScope)]?.GetValue<string>() ?? "";

            options.CustomParameters = new List<string>();
            foreach(var element in data[nameof(CustomParameters)]?.AsArray() ?? [])
                if (element is not null) options.CustomParameters.Add(element.GetValue<string>());

            options.PreRelease = data[nameof(PreRelease)]?.GetValue<bool>() ?? false;
            options.CustomInstallLocation = data[nameof(CustomInstallLocation)]?.GetValue<string>() ?? "";
            options.Version = data[nameof(Version)]?.GetValue<string>() ?? "";
            options.SkipMinorUpdates = data[nameof(SkipMinorUpdates)]?.GetValue<bool>() ?? false;
            return options;
        }
    }
}

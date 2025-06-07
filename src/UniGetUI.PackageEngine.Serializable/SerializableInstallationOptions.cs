using System.Text.Json.Nodes;
using UniGetUI.Core.Data;

namespace UniGetUI.PackageEngine.Serializable
{
    public class SerializableInstallationOptions: SerializableComponent<SerializableInstallationOptions>
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
        public bool OverridesNextLevelOpts { get; set; }
        public bool RemoveDataOnUninstall { get; set; }

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
                OverridesNextLevelOpts = OverridesNextLevelOpts,
                RemoveDataOnUninstall = RemoveDataOnUninstall,
            };
        }

        public override void LoadFromJson(JsonNode data)
        {
            // RemoveDataOnUninstall should not be loaded from disk

            this.SkipHashCheck = data[nameof(SkipHashCheck)]?.GetVal<bool>() ?? false;
            this.InteractiveInstallation = data[nameof(InteractiveInstallation)]?.GetVal<bool>() ?? false;
            this.RunAsAdministrator = data[nameof(RunAsAdministrator)]?.GetVal<bool>() ?? false;
            this.Architecture = data[nameof(Architecture)]?.GetVal<string>() ?? "";
            this.InstallationScope = data[nameof(InstallationScope)]?.GetVal<string>() ?? "";

            this.CustomParameters = new List<string>();
            foreach (var element in data[nameof(CustomParameters)]?.AsArray2() ?? [])
                if (element is not null)
                    this.CustomParameters.Add(element.GetVal<string>());

            this.PreRelease = data[nameof(PreRelease)]?.GetVal<bool>() ?? false;
            this.CustomInstallLocation = data[nameof(CustomInstallLocation)]?.GetVal<string>() ?? "";
            this.Version = data[nameof(Version)]?.GetVal<string>() ?? "";
            this.SkipMinorUpdates = data[nameof(SkipMinorUpdates)]?.GetVal<bool>() ?? false;

            // if OverridesNextLevelOpts is not found on the JSON, set it to true or false depending
            // on whether the current settings instances are different from the default values.
            // This entry shall be checked the last one, to ensure all other properties are set
            this.OverridesNextLevelOpts =
                data[nameof(OverridesNextLevelOpts)]?.GetValue<bool>() ?? DiffersFromDefault();

            SanitizeOptions();
        }

        private void SanitizeOptions()
        {
            for (int i = 0; i < this.CustomParameters.Count; i++)
            {
                this.CustomParameters[i] = this.CustomParameters[i]
                    .Replace("&", "")
                    .Replace("|", "")
                    .Replace(";", "")
                    .Replace("<", "")
                    .Replace(">", "")
                    .Replace("\n", "");
            }
        }

        public bool DiffersFromDefault()
        {
            return SkipHashCheck is not false ||
                   InteractiveInstallation is not false ||
                   RunAsAdministrator is not false ||
                   PreRelease is not false ||
                   SkipMinorUpdates is not false ||
                   Architecture.Any() ||
                   InstallationScope.Any() ||
                   CustomParameters.Where(x => x != "").Any() ||
                   CustomInstallLocation.Any() ||
                   RemoveDataOnUninstall is not false ||
                   Version.Any();
            // OverridesNextLevelOpts does not need to be checked here, since
            // this method is invoked before this property has been set
        }

        public SerializableInstallationOptions() : base()
        {
        }

        public SerializableInstallationOptions(JsonNode data) : base(data)
        {
        }

        public override string ToString()
        {
            string customparams = CustomParameters.Any() ? string.Join(",", CustomParameters) : "[]";
            return $"<SerializableInstallationOptions: SkipHashCheck={SkipHashCheck};" +
                   $"InteractiveInstallation={InteractiveInstallation};" +
                   $"RunAsAdministrator={RunAsAdministrator};" +
                   $"Version={Version};" +
                   $"Architecture={Architecture};" +
                   $"InstallationScope={InstallationScope};" +
                   $"InstallationScope={CustomInstallLocation};" +
                   $"CustomParameters={customparams};" +
                   $"RemoveDataOnUninstall={RemoveDataOnUninstall};" +
                   $"PreRelease={PreRelease}>";
        }
    }
}

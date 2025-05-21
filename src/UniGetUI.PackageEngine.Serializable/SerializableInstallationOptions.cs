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
        public bool OverrideNextLevelPrefs { get; set; }

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
                OverrideNextLevelPrefs = OverrideNextLevelPrefs,
            };
        }

        public override void LoadFromJson(JsonNode data)
        {
            this.SkipHashCheck = data[nameof(SkipHashCheck)]?.GetVal<bool>() ?? false;
            this.InteractiveInstallation = data[nameof(InteractiveInstallation)]?.GetVal<bool>() ?? false;
            this.RunAsAdministrator = data[nameof(RunAsAdministrator)]?.GetVal<bool>() ?? false;
            this.Architecture = data[nameof(Architecture)]?.GetVal<string>() ?? "";
            this.InstallationScope = data[nameof(InstallationScope)]?.GetVal<string>() ?? "";

            this.CustomParameters = new List<string>();
            foreach(var element in data[nameof(CustomParameters)]?.AsArray2() ?? [])
                if (element is not null) this.CustomParameters.Add(element.GetVal<string>());

            this.PreRelease = data[nameof(PreRelease)]?.GetVal<bool>() ?? false;
            this.CustomInstallLocation = data[nameof(CustomInstallLocation)]?.GetVal<string>() ?? "";
            this.Version = data[nameof(Version)]?.GetVal<string>() ?? "";
            this.SkipMinorUpdates = data[nameof(SkipMinorUpdates)]?.GetVal<bool>() ?? false;

            // if OverrideNextLevelPrefs is not found on the JSON, set it to true or false depending
            // on whether the current settings instances are different from the default values.
            // This entry shall be checked the last one, to ensure all other properties are set
            this.OverrideNextLevelPrefs =
                data[nameof(OverrideNextLevelPrefs)]?.GetValue<bool>() ?? DiffersFromDefault();
        }

        private bool DiffersFromDefault()
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
                   Version.Any();
            // OverrideNextLevelPrefs does not need to be checked here, since
            // this method is invoked before this property has been set
        }

        public SerializableInstallationOptions() : base()
        {
        }

        public SerializableInstallationOptions(JsonNode data) : base(data)
        {
        }
    }
}

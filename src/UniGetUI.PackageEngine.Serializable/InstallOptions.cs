using System.Text.Json;
using System.Text.Json.Nodes;
using UniGetUI.Core.Data;

namespace UniGetUI.PackageEngine.Serializable
{
    public class InstallOptions: SerializableComponent<InstallOptions>
    {
        public bool SkipHashCheck { get; set; }
        public bool InteractiveInstallation { get; set; }
        public bool RunAsAdministrator { get; set; }
        public string Architecture { get; set; } = "";
        public string InstallationScope { get; set; } = "";
        public List<string> CustomParameters_Install { get; set; } = [];
        public List<string> CustomParameters_Update { get; set; } = [];
        public List<string> CustomParameters_Uninstall { get; set; } = [];
        public bool PreRelease { get; set; }
        public string CustomInstallLocation { get; set; } = "";
        public string Version { get; set; } = "";
        public bool SkipMinorUpdates { get; set; }
        public bool OverridesNextLevelOpts { get; set; }
        public bool RemoveDataOnUninstall { get; set; }
        public bool UninstallPreviousVersionsOnUpdate { get; set; }
        public List<string> KillBeforeOperation { get; set; } = [];

        public string PreInstallCommand { get; set; } = "";
        public string PostInstallCommand { get; set; } = "";
        public bool AbortOnPreInstallFail { get; set; } = true;
        public string PreUpdateCommand { get; set; } = "";
        public string PostUpdateCommand { get; set; } = "";
        public bool AbortOnPreUpdateFail { get; set; } = true;
        public string PreUninstallCommand { get; set; } = "";
        public string PostUninstallCommand { get; set; } = "";
        public bool AbortOnPreUninstallFail { get; set; } = true;


        public override InstallOptions Copy()
        {
            return new()
            {
                SkipHashCheck = SkipHashCheck,
                Architecture = Architecture,
                CustomInstallLocation = CustomInstallLocation,
                CustomParameters_Install = CustomParameters_Install.ToList(),
                CustomParameters_Update = CustomParameters_Update.ToList(),
                CustomParameters_Uninstall = CustomParameters_Uninstall.ToList(),
                InstallationScope = InstallationScope,
                InteractiveInstallation = InteractiveInstallation,
                PreRelease = PreRelease,
                RunAsAdministrator = RunAsAdministrator,
                Version = Version,
                SkipMinorUpdates = SkipMinorUpdates,
                OverridesNextLevelOpts = OverridesNextLevelOpts,
                RemoveDataOnUninstall = RemoveDataOnUninstall,
                KillBeforeOperation = KillBeforeOperation.ToList(),
                PreInstallCommand = PreInstallCommand,
                PreUpdateCommand = PreUpdateCommand,
                PreUninstallCommand = PreUninstallCommand,
                PostInstallCommand = PostInstallCommand,
                PostUpdateCommand = PostUpdateCommand,
                PostUninstallCommand = PostUninstallCommand,
                AbortOnPreInstallFail = AbortOnPreInstallFail,
                AbortOnPreUpdateFail = AbortOnPreUpdateFail,
                AbortOnPreUninstallFail = AbortOnPreUninstallFail,
                UninstallPreviousVersionsOnUpdate = UninstallPreviousVersionsOnUpdate,
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

            this.CustomParameters_Install = ReadArrayFromJson(data, nameof(CustomParameters_Install));
            this.CustomParameters_Update = ReadArrayFromJson(data, nameof(CustomParameters_Update));
            this.CustomParameters_Uninstall = ReadArrayFromJson(data, nameof(CustomParameters_Uninstall));

            if (this.CustomParameters_Install.Count is 0 &&
                this.CustomParameters_Update.Count  is 0 &&
                this.CustomParameters_Uninstall.Count is 0 &&
                ((data as JsonObject)?.ContainsKey("CustomParameters") ?? false))
            {
                this.CustomParameters_Install = ReadArrayFromJson(data, "CustomParameters");
                this.CustomParameters_Update = ReadArrayFromJson(data, "CustomParameters");
                this.CustomParameters_Uninstall = ReadArrayFromJson(data, "CustomParameters");
            }

            this.KillBeforeOperation = ReadArrayFromJson(data, nameof(KillBeforeOperation));
            this.PreRelease = data[nameof(PreRelease)]?.GetVal<bool>() ?? false;
            this.CustomInstallLocation = data[nameof(CustomInstallLocation)]?.GetVal<string>() ?? "";
            this.Version = data[nameof(Version)]?.GetVal<string>() ?? "";
            this.SkipMinorUpdates = data[nameof(SkipMinorUpdates)]?.GetVal<bool>() ?? false;
            this.UninstallPreviousVersionsOnUpdate = data[nameof(UninstallPreviousVersionsOnUpdate)]?.GetVal<bool>() ?? false;

            this.PreInstallCommand = data[nameof(PreInstallCommand)]?.GetVal<string>() ?? "";
            this.PreUpdateCommand = data[nameof(PreUpdateCommand)]?.GetVal<string>() ?? "";
            this.PreUninstallCommand = data[nameof(PreUninstallCommand)]?.GetVal<string>() ?? "";
            this.PostInstallCommand = data[nameof(PostInstallCommand)]?.GetVal<string>() ?? "";
            this.PostUpdateCommand = data[nameof(PostUpdateCommand)]?.GetVal<string>() ?? "";
            this.PostUninstallCommand = data[nameof(PostUninstallCommand)]?.GetVal<string>() ?? "";
            this.AbortOnPreInstallFail = data[nameof(AbortOnPreInstallFail)]?.GetVal<bool>() ?? true;
            this.AbortOnPreUpdateFail = data[nameof(AbortOnPreUpdateFail)]?.GetVal<bool>() ?? true;
            this.AbortOnPreUninstallFail = data[nameof(AbortOnPreUninstallFail)]?.GetVal<bool>() ?? true;

            // if OverridesNextLevelOpts is not found on the JSON, set it to true or false depending
            // on whether the current settings instances are different from the default values.
            // This entry shall be checked the last one, to ensure all other properties are set
            this.OverridesNextLevelOpts =
                data[nameof(OverridesNextLevelOpts)]?.GetValue<bool>() ?? DiffersFromDefault();
        }

        public override JsonNode AsJsonNode()
        {
            JsonObject obj = new();

            if (OverridesNextLevelOpts is not false)
                obj.Add(nameof(OverridesNextLevelOpts), OverridesNextLevelOpts);

            if(SkipHashCheck is not false)
                obj.Add(nameof(SkipHashCheck), SkipHashCheck);

            if(InteractiveInstallation is not false)
                obj.Add(nameof(InteractiveInstallation), InteractiveInstallation);

            if(RunAsAdministrator is not false)
                obj.Add(nameof(RunAsAdministrator), RunAsAdministrator);

            if(PreRelease is not false)
                obj.Add(nameof(PreRelease), PreRelease);

            if(SkipMinorUpdates is not false)
                obj.Add(nameof(SkipMinorUpdates), SkipMinorUpdates);

            if(Architecture.Any())
                obj.Add(nameof(Architecture), Architecture);

            if(InstallationScope.Any())
                obj.Add(nameof(InstallationScope), InstallationScope);

            if(CustomParameters_Install.Where(x => x.Any()).Any())
                obj.Add(nameof(CustomParameters_Install),
                    new JsonArray(CustomParameters_Install.Select(x => JsonValue.Create(x) as JsonNode).ToArray()));

            if(CustomParameters_Update.Where(x => x.Any()).Any())
                obj.Add(nameof(CustomParameters_Update),
                    new JsonArray(CustomParameters_Update.Select(x => JsonValue.Create(x) as JsonNode).ToArray()));

            if(CustomParameters_Uninstall.Where(x => x.Any()).Any())
                obj.Add(nameof(CustomParameters_Uninstall),
                    new JsonArray(CustomParameters_Uninstall.Select(x => JsonValue.Create(x) as JsonNode).ToArray()));

            if(KillBeforeOperation.Where(x => x.Any()).Any())
                obj.Add(nameof(KillBeforeOperation),
                    new JsonArray(KillBeforeOperation.Select(x => JsonValue.Create(x) as JsonNode).ToArray()));

            if(CustomInstallLocation.Any())
                obj.Add(nameof(CustomInstallLocation), CustomInstallLocation);

            if(RemoveDataOnUninstall is not false)
                obj.Add(nameof(RemoveDataOnUninstall), RemoveDataOnUninstall);

            if(Version.Any())
                obj.Add(nameof(Version), Version);

            if(PreInstallCommand.Any())
                obj.Add(nameof(PreInstallCommand), PreInstallCommand);

            if(PostInstallCommand.Any())
                obj.Add(nameof(PostInstallCommand), PostInstallCommand);

            if(AbortOnPreInstallFail is not true)
                obj.Add(nameof(AbortOnPreInstallFail), AbortOnPreInstallFail);

            if(PreUpdateCommand.Any())
                obj.Add(nameof(PreUpdateCommand), PreUpdateCommand);

            if(PostUpdateCommand.Any())
                obj.Add(nameof(PostUpdateCommand), PostUpdateCommand);

            if(AbortOnPreUpdateFail is not true)
                obj.Add(nameof(AbortOnPreUpdateFail), AbortOnPreUpdateFail);

            if(PreUninstallCommand.Any())
                obj.Add(nameof(PreUninstallCommand), PreUninstallCommand);

            if(PostUninstallCommand.Any())
                obj.Add(nameof(PostUninstallCommand), PostUninstallCommand);

            if(UninstallPreviousVersionsOnUpdate is not false)
                obj.Add(nameof(UninstallPreviousVersionsOnUpdate), UninstallPreviousVersionsOnUpdate);

            if(AbortOnPreUninstallFail is not true)
                obj.Add(nameof(AbortOnPreUninstallFail), AbortOnPreUninstallFail);

            return obj;
        }

        private static List<string> ReadArrayFromJson(JsonNode data, string name)
        {
            List<string> result = new List<string>();
            foreach (var element in data[name]?.AsArray2() ?? [])
                if (element is not null)
                    result.Add(element.GetVal<string>());
            return result.Where(x => x.Any()).ToList();
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
                   CustomParameters_Install.Where(x => x.Any()).Any() ||
                   CustomParameters_Update.Where(x => x.Any()).Any() ||
                   CustomParameters_Uninstall.Where(x => x.Any()).Any() ||
                   KillBeforeOperation.Where(x => x.Any()).Any() ||
                   CustomInstallLocation.Any() ||
                   RemoveDataOnUninstall is not false ||
                   Version.Any() ||
                   PreInstallCommand.Any() ||
                   PostInstallCommand.Any() ||
                   AbortOnPreInstallFail is not true ||
                   PreUpdateCommand.Any() ||
                   PostUpdateCommand.Any() ||
                   AbortOnPreUpdateFail is not true ||
                   PreUninstallCommand.Any() ||
                   PostUninstallCommand.Any() ||
                   UninstallPreviousVersionsOnUpdate is not false ||
                   AbortOnPreUninstallFail is not true;
            // OverridesNextLevelOpts does not need to be checked here, since
            // this method is invoked before this property has been set
        }

        public InstallOptions() : base()
        {
        }

        public InstallOptions(JsonNode data) : base(data)
        {
        }

        public override string ToString()
        {
            string customparams = CustomParameters_Install.Any() ? string.Join(",", CustomParameters_Install) : "[],";
            customparams += CustomParameters_Update.Any() ? string.Join(",", CustomParameters_Update) : "[],";
            customparams += CustomParameters_Uninstall.Any() ? string.Join(",", CustomParameters_Uninstall) : "[]";
            return $"<InstallOptions: SkipHashCheck={SkipHashCheck};" +
                   $"InteractiveInstallation={InteractiveInstallation};" +
                   $"RunAsAdministrator={RunAsAdministrator};" +
                   $"Version={Version};" +
                   $"Architecture={Architecture};" +
                   $"InstallationScope={InstallationScope};" +
                   $"InstallationScope={CustomInstallLocation};" +
                   $"CustomParameters={customparams};" +
                   $"RemoveDataOnUninstall={RemoveDataOnUninstall};" +
                   $"KillBeforeOperation={KillBeforeOperation};" +
                   $"PreInstallCommand={PreInstallCommand};" +
                   $"PostInstallCommand={PostInstallCommand};" +
                   $"AbortOnPreInstallFail={AbortOnPreInstallFail};" +
                   $"PreUpdateCommand={PreUpdateCommand};" +
                   $"PostUpdateCommand={PostUpdateCommand};" +
                   $"AbortOnPreUpdateFail={AbortOnPreUpdateFail};" +
                   $"PreUninstallCommand={PreUninstallCommand};" +
                   $"PostUninstallCommand={PostUninstallCommand};" +
                   $"UninstallPreviousVersionsOnUpdate={UninstallPreviousVersionsOnUpdate}" +
                   $"AbortOnPreUninstallFail={AbortOnPreUninstallFail};" +
                   $"PreRelease={PreRelease}>";
        }
    }
}

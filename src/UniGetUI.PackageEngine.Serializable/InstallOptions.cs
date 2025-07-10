using System.Text.Json;
using System.Text.Json.Nodes;
using UniGetUI.Core.Data;

namespace UniGetUI.PackageEngine.Serializable
{
    public class InstallOptions: SerializableComponent<InstallOptions>
    {
        public readonly IReadOnlyDictionary<string, bool> _defaultBoolValues = new Dictionary<string, bool>()
        {   // OverridesNextLevelOpts is deliberately skipped here
            { "SkipHashCheck", false },
            { "InteractiveInstallation", false },
            { "RunAsAdministrator", false },
            { "PreRelease", false },
            { "SkipMinorUpdates", false },
            { "RemoveDataOnUninstall", false },
            { "UninstallPreviousVersionsOnUpdate", false },
            { "AbortOnPreInstallFail", true },
            { "AbortOnPreUpdateFail", true },
            { "AbortOnPreUninstallFail", true },
        };

        public readonly IReadOnlyDictionary<string, string> _defaultStringValues = new Dictionary<string, string>()
        {
            {"Architecture", ""},
            {"InstallationScope", ""},
            {"CustomInstallLocation", ""},
            {"Version", ""},
            {"PreInstallCommand", ""},
            {"PostInstallCommand", ""},
            {"PreUpdateCommand", ""},
            {"PostUpdateCommand", ""},
            {"PreUninstallCommand", ""},
            {"PostUninstallCommand", ""},
        };

        public readonly IReadOnlyList<string> _defaultListValues = new List<string>()
        {
            "CustomParameters_Install",
            "CustomParameters_Update",
            "CustomParameters_Uninstall",
            "KillBeforeOperation",
        };


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

        public bool OverridesNextLevelOpts { get; set; }

        public override InstallOptions Copy()
        {
            var copy = new InstallOptions();

            foreach (var (boolKey, _) in _defaultBoolValues)
                copy.SetValueToProperty(boolKey, GetValueFromProperty<bool>(boolKey));

            foreach (var (stringKey, _) in _defaultStringValues)
                copy.SetValueToProperty(stringKey, GetValueFromProperty<string>(stringKey));

            foreach (var listKey in _defaultListValues)
                copy.SetValueToProperty(listKey, GetValueFromProperty<IReadOnlyList<string>>(listKey).ToList());

            // Handle non-automated OverridesNextLevelOpts
            copy.OverridesNextLevelOpts = OverridesNextLevelOpts;
            return copy;
        }

        public void SetValueToProperty<T>(string name, T value)
        {
            var property = this.GetType().GetProperty(name);
            property?.SetValue(this, value);
        }

        public T GetValueFromProperty<T>(string name)
        {
            var property = this.GetType().GetProperty(name);
            return (T)(property?.GetValue(this) ?? throw new InvalidDataException($"Invalid datatype for property {name} (expected {nameof(T)})"));
        }

        public override void LoadFromJson(JsonNode data)
        {
            foreach (var (boolKey, defValue) in _defaultBoolValues)
                SetValueToProperty(boolKey, data[boolKey]?.GetVal<bool>() ?? defValue);

            foreach (var (stringKey, defValue) in _defaultStringValues)
                SetValueToProperty(stringKey, data[stringKey]?.GetVal<string>() ?? defValue);

            foreach (var listKey in _defaultListValues)
                SetValueToProperty(listKey, ReadArrayFromJson(data, listKey));

            // Handle case where setting has not been migrated yet
            if (this.CustomParameters_Install.Count is 0 &&
                this.CustomParameters_Update.Count  is 0 &&
                this.CustomParameters_Uninstall.Count is 0 &&
                ((data as JsonObject)?.ContainsKey("CustomParameters") ?? false))
            {
                this.CustomParameters_Install = ReadArrayFromJson(data, "CustomParameters");
                this.CustomParameters_Update = ReadArrayFromJson(data, "CustomParameters");
                this.CustomParameters_Uninstall = ReadArrayFromJson(data, "CustomParameters");
            }

            // if OverridesNextLevelOpts is not found on the JSON, set it to true or false depending
            // on whether the current settings instances are different from the default values.
            // This entry shall be checked the last one, to ensure all other properties are set
            this.OverridesNextLevelOpts = data[nameof(OverridesNextLevelOpts)]?.GetValue<bool>() ?? DiffersFromDefault();
        }

        public override JsonNode AsJsonNode()
        {
            JsonObject obj = new();

            if (OverridesNextLevelOpts is not false || DiffersFromDefault())
                obj.Add(nameof(OverridesNextLevelOpts), OverridesNextLevelOpts);

            foreach (var (boolKey, defValue) in _defaultBoolValues)
            {
                bool currentValue = GetValueFromProperty<bool>(boolKey);
                if (currentValue != defValue) obj.Add(boolKey, currentValue);
            }

            foreach (var (stringKey, defValue) in _defaultStringValues)
            {
                string currentValue = GetValueFromProperty<string>(stringKey);
                if (currentValue != defValue) obj.Add(stringKey, currentValue);
            }

            foreach (var listKey in _defaultListValues)
            {
                IReadOnlyList<string> currentValue = GetValueFromProperty<IReadOnlyList<string>>(listKey);

                if (currentValue.Where(x => x.Any()).Any())
                {
                    obj.Add(listKey, new JsonArray(currentValue.Select(x => JsonValue.Create(x) as JsonNode).ToArray()));
                }
            }

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
            foreach (var (boolKey, defValue) in _defaultBoolValues)
                if (GetValueFromProperty<bool>(boolKey) != defValue) return true;

            foreach (var (stringKey, defValue) in _defaultStringValues)
                if (GetValueFromProperty<string>(stringKey) != defValue) return true;

            foreach (var listKey in _defaultListValues)
            {
                IReadOnlyList<string> currentValue = GetValueFromProperty<IReadOnlyList<string>>(listKey);
                if (currentValue.Where(x => x.Any()).Any()) return true;
            }

            return false;
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

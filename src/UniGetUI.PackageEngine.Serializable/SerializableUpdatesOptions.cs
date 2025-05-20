using System.Text.Json.Nodes;
using UniGetUI.Core.Data;
using UniGetUI.PackageEngine.Serializable;

namespace UniGetUI.PackageEngine.Classes.Serializable
{
    public class SerializableUpdatesOptions: SerializableComponent<SerializableUpdatesOptions>
    {
        public bool UpdatesIgnored { get; set; }
        public string IgnoredVersion { get; set; } = "";

        public override SerializableUpdatesOptions Copy()
        {
            return new() { UpdatesIgnored = this.UpdatesIgnored, IgnoredVersion = this.IgnoredVersion };
        }

        public override void LoadFromJson(JsonNode data)
        {
            this.UpdatesIgnored = data[nameof(UpdatesIgnored)]?.GetVal<bool>() ?? false;
            this.IgnoredVersion = data[nameof(IgnoredVersion)]?.GetVal<string>() ?? "";
        }

        public SerializableUpdatesOptions() : base()
        {
        }

        public SerializableUpdatesOptions(JsonNode data) : base(data)
        {
        }
    }
}

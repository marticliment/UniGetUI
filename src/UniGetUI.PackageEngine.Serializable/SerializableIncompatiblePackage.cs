using System.Text.Json.Nodes;
using UniGetUI.PackageEngine.Serializable;

namespace UniGetUI.PackageEngine.Classes.Serializable
{
    public class SerializableIncompatiblePackage: SerializableComponent<SerializableIncompatiblePackage>
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public string Source { get; set; } = "";

        public override SerializableIncompatiblePackage Copy()
        {
            return new()
            {
                Id = this.Id, Name = this.Name, Version = this.Version, Source = this.Source,
            };
        }

        public override void LoadFromJson(JsonNode data)
        {
            this.Id = data[nameof(Id)]?.GetValue<string>() ?? "";
            this.Name = data[nameof(Name)]?.GetValue<string>() ?? "";
            this.Version = data[nameof(Version)]?.GetValue<string>() ?? "";
            this.Source = data[nameof(Source)]?.GetValue<string>() ?? "";
        }

        public SerializableIncompatiblePackage(JsonNode data) : base(data)
        {
        }

        public SerializableIncompatiblePackage(): base()
        {
        }
    }
}

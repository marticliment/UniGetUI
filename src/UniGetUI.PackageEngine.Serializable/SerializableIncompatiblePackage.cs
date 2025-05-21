using System.Text.Json.Nodes;
using UniGetUI.Core.Data;
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
            this.Id = data[nameof(Id)]?.GetVal<string>() ?? "";
            this.Name = data[nameof(Name)]?.GetVal<string>() ?? "";
            this.Version = data[nameof(Version)]?.GetVal<string>() ?? "";
            this.Source = data[nameof(Source)]?.GetVal<string>() ?? "";
        }

        public SerializableIncompatiblePackage(JsonNode data) : base(data)
        {
        }

        public SerializableIncompatiblePackage(): base()
        {
        }
    }
}

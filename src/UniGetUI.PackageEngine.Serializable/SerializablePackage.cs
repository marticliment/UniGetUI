using System.Text.Json.Nodes;
using UniGetUI.Core.Data;
using UniGetUI.PackageEngine.Serializable;

namespace UniGetUI.PackageEngine.Classes.Serializable
{
    public class SerializablePackage: SerializableComponent<SerializablePackage>
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public string Source { get; set; } = "";
        public string ManagerName { get; set; } = "";

        public SerializableInstallationOptions InstallationOptions { get; set; } = new();
        public SerializableUpdatesOptions Updates { get; set; } = new();

        public override SerializablePackage Copy()
        {
            return new SerializablePackage()
            {
                Name = this.Name,
                Id = this.Id,
                Version = this.Version,
                Source = this.Source,
                ManagerName = this.ManagerName,
                InstallationOptions = this.InstallationOptions.Copy(),
                Updates = this.Updates.Copy(),
            };
        }

        public override void LoadFromJson(JsonNode data)
        {
            this.Name = data[nameof(Name)]?.GetVal<string>() ?? "";
            this.Id = data[nameof(Id)]?.GetVal<string>() ?? "";
            this.Version = data[nameof(Version)]?.GetVal<string>() ?? "";
            this.Source = data[nameof(Source)]?.GetVal<string>() ?? "";
            this.ManagerName = data[nameof(ManagerName)]?.GetVal<string>() ?? "";

            this.InstallationOptions = new(data[nameof(InstallationOptions)] ?? new JsonObject());
            this.Updates = new(data[nameof(Updates)] ?? new JsonObject());
        }

        public SerializablePackage() : base()
        {
        }

        public SerializablePackage(JsonNode data) : base(data)
        {
        }


        /// <summary>
        /// Returns an equivalent copy of the current package as an Invalid Serializable Package.
        /// The reverse operation is not possible, since data is lost.
        /// </summary>
        public SerializableIncompatiblePackage GetInvalidEquivalent()
        {
            return new SerializableIncompatiblePackage
            {
                Id = Id,
                Name = Name,
                Version = Version,
                Source = Source,
            };
        }
    }
}

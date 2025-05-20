using System.Text.Json.Nodes;
using UniGetUI.PackageEngine.Serializable;
using UniGetUI.Core.Data;

namespace UniGetUI.PackageEngine.Classes.Serializable
{
    public class SerializableBundle: SerializableComponent<SerializableBundle>
    {
        public const double ExpectedVersion = 3;
        public const string IncompatMessage = "Incompatible packages cannot be installed from UniGetUI, " +
                                              "either because they came from a local source (for example Local PC)" +
                                              "or because the package manager was unavailable. " +
                                              "Nevertheless, they have been listed here for logging purposes.";


        public double export_version { get; set; } = 3;
        public List<SerializablePackage> packages { get; set; } = [];
        public string incompatible_packages_info { get; set; } = IncompatMessage;
        public List<SerializableIncompatiblePackage> incompatible_packages { get; set; } = [];

        public override SerializableBundle Copy()
        {
            var _packages = new List<SerializablePackage>();
            var _incompatPackages = new List<SerializableIncompatiblePackage>();

            foreach(var package in this.packages)
                _packages.Add(package.Copy());

            foreach(var incompatPackage in this.incompatible_packages)
                _incompatPackages.Add(incompatPackage.Copy());

            return new()
            {
                export_version = this.export_version,
                packages = _packages,
                incompatible_packages_info = this.incompatible_packages_info,
                incompatible_packages = _incompatPackages
            };
        }

        public override void LoadFromJson(JsonNode data)
        {
            this.export_version = data[nameof(export_version)]?.GetVal<double>() ?? 0;
            this.incompatible_packages_info = data[nameof(incompatible_packages_info)]?.GetVal<string>() ?? IncompatMessage;
            this.packages = new List<SerializablePackage>();
            this.incompatible_packages = new List<SerializableIncompatiblePackage>();

            foreach (JsonNode? pkg in data[nameof(packages)]?.AsArray2() ?? new())
            {
                if (pkg is null) throw new InvalidDataException("JsonNode? pkg was null, when it shouldn't");
                packages.Add(new SerializablePackage(pkg));
            }

            foreach (JsonNode? inc_pkg in data[nameof(incompatible_packages)]?.AsArray2() ?? new())
            {
                if (inc_pkg is null) throw new InvalidDataException("JsonNode? inc_pkg was null, when it shouldn't");
                incompatible_packages.Add(new SerializableIncompatiblePackage(inc_pkg));
            }
        }

        public SerializableBundle() : base()
        {
        }

        public SerializableBundle(JsonNode data) : base(data)
        {
        }
    }
}

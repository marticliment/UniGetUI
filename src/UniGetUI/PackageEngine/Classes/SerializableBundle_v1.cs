using System.Collections.Generic;

namespace UniGetUI.PackageEngine.Classes
{
    public class SerializableBundle_v1
    {
        public double export_version { get; set; } = 2.0;
        public List<SerializableValidPackage_v1> packages { get; set; } = new();
        public string incompatible_packages_info { get; set; } = "Incompatible packages cannot be installed from WingetUI, but they have been listed here for logging purposes.";
        public List<SerializableIncompatiblePackage_v1> incompatible_packages { get; set; } = new();

    }

}

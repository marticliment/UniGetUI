using System.Threading.Tasks;

namespace UniGetUI.PackageEngine.Classes
{
    public class SerializableUpdatesOptions_v1
    {
        public bool UpdatesIgnored { get; set; } = false;
        public string IgnoredVersion { get; set; } = "";
        public static async Task<SerializableUpdatesOptions_v1> FromPackageAsync(Package package)
        {
            SerializableUpdatesOptions_v1 Serializable = new();
            Serializable.UpdatesIgnored = await package.HasUpdatesIgnoredAsync();
            Serializable.IgnoredVersion = await package.GetIgnoredUpdatesVersionAsync();
            return Serializable;
        }
    }

}

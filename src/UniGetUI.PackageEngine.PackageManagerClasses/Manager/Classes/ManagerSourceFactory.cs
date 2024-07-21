using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers
{
    public class ManagerSourceFactory
    {
        private readonly PackageManager __manager;
        private readonly Dictionary<string, ManagerSource> __reference;
        private readonly Uri __default_uri = new("https://marticliment.com/unigetui/");

        public ManagerSourceFactory(PackageManager manager)
        {
            __reference = [];
            __manager = manager;
        }

        public void Reset()
        {
            __reference.Clear();
        }

        /// <summary>
        /// Returns the existing source for the given name, or creates a new one if it does not exist.
        /// </summary>
        /// <param name="name">The name of the source</param>
        /// <returns>A valid ManagerSource</returns>
        public ManagerSource GetSourceOrDefault(string name)
        {
            if (__reference.TryGetValue(name, out ManagerSource? source) && source != null)
            {
                return source;
            }

            ManagerSource new_source = new(__manager, name, __default_uri);
            __reference.Add(name, new_source);
            return new_source;
        }

        /// <summary>
        /// Returns the existing source for the given name, or null if it does not exist.
        /// </summary>
        public ManagerSource? GetSourceIfExists(string name)
        {
            if (__reference.TryGetValue(name, out ManagerSource? source))
            {
                return source;
            }
            return null;
        }

        public void AddSource(ManagerSource source)
        {
            if (!__reference.TryAdd(source.Name, source))
            {
                ManagerSource existing_source = __reference[source.Name];
                if (existing_source.Url == __default_uri)
                {
                    existing_source.ReplaceUrl(source.Url);
                }
            }
        }

        public ManagerSource[] GetAvailableSources()
        {
            return __reference.Values.ToArray();
        }
    }
}

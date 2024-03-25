using DevHome.SetupFlow.Common.WindowsPackageManager;

namespace WingetTest
{
    internal class Program
    {
        static public void Main(string[] args)
        {
            RunWinget();
            Console.ReadLine();
        }

        private static async void RunWinget()
        { 
            var WinGet = new WindowsPackageManagerDefaultFactory();
            var Manager = WinGet.CreatePackageManager();

            var catalogs = Manager.GetPackageCatalogs();
            
            var array_pkgs = catalogs.ToArray();
            
            foreach (var catalog in array_pkgs)
            {
                var connect_result = catalog.Connect();
                var filters = WinGet.CreateFindPackagesOptions();
                var filter = WinGet.CreatePackageMatchFilter();

                filter.Field = Microsoft.Management.Deployment.PackageMatchField.Name;
                filter.Value = "Hello";

                filters.Filters.Add(filter);

                var package_list = await connect_result.PackageCatalog.FindPackagesAsync(filters);
                foreach (var match in package_list.Matches.ToArray())
                {
                    var package = match.CatalogPackage;

                    Console.WriteLine(package.Name);
                }
            }
        }
    }
}

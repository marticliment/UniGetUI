using ABI.Microsoft.Management.Deployment;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using System.Security.Principal;

namespace WingetTest
{
    internal class Program
    {
        static public void Main(string[] args)
        {
            while(true)
            {
                Console.WriteLine("                    ");
                Console.Write("Enter search query: ");
                string? Query = Console.ReadLine();
                if(Query == null || Query == "")
                    break;

                FindPackagesForQuery(Query).Wait();
            }
        }

        private static async Task FindPackagesForQuery(string Query)
        {   
            WindowsPackageManagerFactory WinGetFactory;
            bool IsAdministrator = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

            // If the user is an administrator, use the elevated factory. Otherwhise COM will crash
            if(IsAdministrator)
                WinGetFactory = new WindowsPackageManagerElevatedFactory();
            else
                WinGetFactory = new WindowsPackageManagerStandardFactory();

            // Create Package Manager and get available catalogs
            var Manager = WinGetFactory.CreatePackageManager();
            var AvailableCatalogs = Manager.GetPackageCatalogs();
                        
            foreach (var Catalog in AvailableCatalogs.ToArray())
            {
                // Create a filter to search for packages
                var FilterList = WinGetFactory.CreateFindPackagesOptions();

                // Add the query to the filter
                var NameFilter = WinGetFactory.CreatePackageMatchFilter();
                NameFilter.Field = Microsoft.Management.Deployment.PackageMatchField.Name;
                NameFilter.Value = Query;
                FilterList.Filters.Add(NameFilter);

                // Find the packages with the filters
                var SearchResults = await Catalog.Connect().PackageCatalog.FindPackagesAsync(FilterList);
                foreach (var Match in SearchResults.Matches.ToArray())
                {
                    // Print the packages
                    var Package = Match.CatalogPackage;
                    Console.WriteLine(Package.Name);
                }
            }
        }
    }
}

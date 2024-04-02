#include "pch.h"
#include "InstanceCreator.cpp"
#include "DllCode.h"

static int __get_installed_packages(list<CatalogPackage>& packages);

static const wstring TAB_CHAR = L"\t";
static const wstring NEWLINE_CHAR = L"\n";

static PackageManager WindowsPackageManager = nullptr;

// Pre: Returns the version of this DLL library
// Post: Windows Package Manager gets initialized internally. If initialization fails, false is returned.
const BSTR GetDLLModuleVersion()
{
	return SysAllocString(L"1.1.0.0");
}


// Pre: Windows Package Manager has been  initialized with the InitializePackageManager() method.
// Post: The list of installed packages is returned on a string, with the following format:
//  Name\t Id\t Version\t [Source]\n
const BSTR GetInstalledPackagesFromAllCatalogs()
{
	try {
		if (WindowsPackageManager == nullptr)
		{
			cout << "WindowsPackageManager.DLL.Interop.GetInstalledPackagesFromAllCatalogs(): WindowsPackageManager is not initialized. Please call InitializePackageManager() first." << endl;
			InitializePackageManager(); // and hope for the best, since this may fail.
		}

		list<CatalogPackage> packages;

		int op_res = __get_installed_packages(packages);
		if (op_res != 0)
		{
			// Package fetcher failed.
			cout << "WindowsPackageManager.DLL.Interop.GetInstalledPackagesFromAllCatalogs(): Failed to get installed packages. Error code: " + to_string(op_res) << endl;
			return SysAllocString(L"WindowsPackageManager.DLL.Interop.GetInstalledPackagesFromAllCatalogs(): Failed to get installed packages. Error code on stdout");
		}

		wstring result = L"";

		for (auto package : packages)
		{
			try {
				wstring source = L"";
				if (package.DefaultInstallVersion())
					source = package.DefaultInstallVersion().PackageCatalog().Info().Name();
				result += package.Name() + TAB_CHAR + package.Id() + TAB_CHAR + package.InstalledVersion().Version() + TAB_CHAR + source + NEWLINE_CHAR;
			}
			catch (int code)
			{
				cout << "WindowsPackageManager.DLL.Interop.GetInstalledPackagesFromAllCatalogs(): Failed to read package. Error code: " << code << endl;
			}
		}
		return SysAllocString(result.c_str());
	}
	catch (hresult_error const& ex) {
		cout << "WindowsPackageManager.DLL.Interop.GetInstalledPackagesFromAllCatalogs(): Failed to get installed packages. Error: " + to_string(ex.message()) << endl;
		return SysAllocString(L"WindowsPackageManager.DLL.Interop.GetInstalledPackagesFromAllCatalogs() : Failed to get installed packages. Error code on stout");
	}
	catch (...)
	{
		cout << "WindowsPackageManager.DLL.Interop.GetInstalledPackagesFromAllCatalogs(): Failed to get installed packages." << endl;
		return SysAllocString(L"WindowsPackageManager.DLL.Interop.GetInstalledPackagesFromAllCatalogs(): Failed to get installed packages.");
	}
}

// Pre: Windows Package Manager has been initialized with the InitializePackageManager() method.
// Post: The list of upgradable packages is returned
//  Name\t Id\t Version\t NewVersion\t Source\n
const BSTR GetAvailableUpdatesFromAllCatalogs()
{
    if (WindowsPackageManager == nullptr)
    {
        InitializePackageManager(); // and hope for the best, since this may fail.
    }

	try 
	{
		if (WindowsPackageManager == nullptr)
		{
			// Package fetcher failed.
			cout << "WindowsPackageManager.DLL.Interop.GetAvailableUpdatesFromAllCatalogs(): WindowsPackageManager is not initialized. Please call InitializePackageManager() first." << endl;
			InitializePackageManager(); // and hope for the best, since this may fail.
		}

		list<CatalogPackage> packages;

		int op_res = __get_installed_packages(packages);
		if (op_res != 0)
		{
			cout << "WindowsPackageManager.DLL.Interop.GetAvailableUpdatesFromAllCatalogs(): Failed to get installed packages. Error code: " + to_string(op_res);
			return SysAllocString(L"WindowsPackageManager.DLL.Interop.GetAvailableUpdatesFromAllCatalogs(): Failed to get installed packages. Error code on stdout");
		}

		wstring result = L"";

		for (auto package : packages)
		{
			try {
				wstring source = L"";
				if (package.DefaultInstallVersion() && package.IsUpdateAvailable())
				{
					result += package.Name() + TAB_CHAR + package.Id() + TAB_CHAR + package.InstalledVersion().Version() + TAB_CHAR;
					result += package.DefaultInstallVersion().Version() + TAB_CHAR + package.DefaultInstallVersion().PackageCatalog().Info().Name() + NEWLINE_CHAR;
				}
			}
			catch (int code)
			{
				cout << "WindowsPackageManager.DLL.Interop.GetAvailableUpdatesFromAllCatalogs(): Failed to read package. Error code: " << code << endl;
			}
		}
		return SysAllocString(result.c_str());
	}
	catch (hresult_error const& ex) {
		cout << "WindowsPackageManager.DLL.Interop.GetAvailableUpdatesFromAllCatalogs(): Failed to get installed packages. Error: " + to_string(ex.message()) << endl;
		return SysAllocString(L"WindowsPackageManager.DLL.Interop.GetAvailableUpdatesFromAllCatalogs(): Failed to get installed packages. Error code on  stdout");
	}
	catch (...)
	{
		cout << "WindowsPackageManager.DLL.Interop.GetAvailableUpdatesFromAllCatalogs(): Failed to get installed packages." << endl;
		return SysAllocString(L"WindowsPackageManager.DLL.Interop.GetAvailableUpdatesFromAllCatalogs(): Failed to get installed packages.");
	}
}


// Pre: WinGet is installed on the local machine.
// Post: Windows Package Manager gets initialized internally. If initialization fails, false is returned.
bool InitializePackageManager()
{
	try {
		CoInitialize(nullptr);
		cout << "InitializePackageManager: CoInitialize succeeded" << endl;
		WindowsPackageManager = CreatePackageManager();
		cout << "InitializePackageManager: CreatePackageManager succeeded" << endl;
		return true;
	}
	catch (hresult_error const& ex) {
		cout << "InitializePackageManager: Failed to create PackageManager. Error: " << to_string(ex.message().c_str()) << endl;
		return false;
	}
	catch (...)
	{
		cout << "InitializePackageManager: Failed to create PackageManager. Unknown error." << endl;
		return false;
	}
}


// Pre: an empty valid list of CatalogPackages
// Post: the list is filled with the installed packages. If operation fails, the list is empty and returned value is not zero.
static int __get_installed_packages(list<CatalogPackage>& packages)
{
	// Create composite catalog options
	CreateCompositePackageCatalogOptions createCompositePackageCatalogOptions = CreateCreateCompositePackageCatalogOptions();

	// Load catalogs into composite catalog
	for (auto RemoteCatalogRef : WindowsPackageManager.GetPackageCatalogs())
	{
		createCompositePackageCatalogOptions.Catalogs().Append(RemoteCatalogRef);
	}

	// Configure catalog to find local only
	createCompositePackageCatalogOptions.CompositeSearchBehavior(CompositeSearchBehavior::LocalCatalogs);
	PackageCatalogReference installedSearchCatalogRef = WindowsPackageManager.CreateCompositePackageCatalog(createCompositePackageCatalogOptions);

	// Connect to the catalog
	ConnectResult connectResult{ installedSearchCatalogRef.Connect() };
	PackageCatalog installedCatalog = connectResult.PackageCatalog();
	if (!installedCatalog)
	{
		// Connect Error.
		cout << "WindowsPackageManager.DLL.Interop.GetInstalledPackagesFromAllCatalogs(): Failed to connect to catalog." << endl;
		return 1;
	}

	// Create filters
	FindPackagesOptions findPackagesOptions = CreateFindPackagesOptions();
	PackageMatchFilter filter = CreatePackageMatchFilter();
	filter.Field(PackageMatchField::Id);
	filter.Option(PackageFieldMatchOption::ContainsCaseInsensitive);
	filter.Value(L"");
	findPackagesOptions.Filters().Append(filter);

	auto findResult = installedCatalog.FindPackages(findPackagesOptions);

	if (findResult.Status() != FindPackagesResultStatus::Ok)
	{
		// Find Error.
		cout << "WindowsPackageManager.DLL.Interop.GetInstalledPackagesFromAllCatalogs(): Failed to find packages on catalog: findResult.Status() was" << int(findResult.Status()) << endl;
		return 10 + int(findResult.Status());
	}

	for (auto match : findResult.Matches())
	{
		if (match.CatalogPackage())
		{
			packages.push_back(match.CatalogPackage());
		}
	}
	return 0;
}
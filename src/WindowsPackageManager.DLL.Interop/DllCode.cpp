#include "pch.h"
#include "DllCode.h"

using namespace std;
using namespace winrt;
using namespace winrt::Microsoft::Management::Deployment;
using namespace winrt::Windows::Foundation::Collections;

#ifdef WINDOWSPACKAGEMANAGER_DLL_INTEROP
#define WINDOWSPACKAGEMANAGER_DLL_INTEROP_API __declspec(dllexport)
#else
#define WINDOWSPACKAGEMANAGER_DLL_INTEROP_API __declspec(dllimport)
#endif

const CLSID CLSID_PackageManager = { 0xC53A4F16, 0x787E, 0x42A4, 0xB3, 0x04, 0x29, 0xEF, 0xFB, 0x4B, 0xF5, 0x97 };  //C53A4F16-787E-42A4-B304-29EFFB4BF597
// const CLSID CLSID_InstallOptions = { 0x1095f097, 0xEB96, 0x453B, 0xB4, 0xE6, 0x16, 0x13, 0x63, 0x7F, 0x3B, 0x14 };  //1095F097-EB96-453B-B4E6-1613637F3B14
const CLSID CLSID_FindPackagesOptions = { 0x572DED96, 0x9C60, 0x4526, { 0x8F, 0x92, 0xEE, 0x7D, 0x91, 0xD3, 0x8C, 0x1A } }; //572DED96-9C60-4526-8F92-EE7D91D38C1A
const CLSID CLSID_PackageMatchFilter = { 0xD02C9DAF, 0x99DC, 0x429C, { 0xB5, 0x03, 0x4E, 0x50, 0x4E, 0x4A, 0xB0, 0x00 } }; //D02C9DAF-99DC-429C-B503-4E504E4AB000
const CLSID CLSID_CreateCompositePackageCatalogOptions = { 0x526534B8, 0x7E46, 0x47C8, { 0x84, 0x16, 0xB1, 0x68, 0x5C, 0x32, 0x7D, 0x37 } }; //526534B8-7E46-47C8-8416-B1685C327D37
// const CLSID CLSID_DownloadOptions = { 0X4CBABE76, 0X7322, 0X4BE4, {0X9C, 0XEA, 0X25, 0X89, 0XA8, 0X06, 0X82, 0XDC} };  //4CBABE76-7322-4BE4-9CEA-2589A80682DC

PackageManager WindowsPackageManager = nullptr;

PackageManager CreatePackageManager() {
    return winrt::create_instance<PackageManager>(CLSID_PackageManager, CLSCTX_ALL);
}
/*
InstallOptions CreateInstallOptions() {
    return winrt::create_instance<InstallOptions>(CLSID_InstallOptions, CLSCTX_ALL);
}
*/
FindPackagesOptions CreateFindPackagesOptions() {
    return winrt::create_instance<FindPackagesOptions>(CLSID_FindPackagesOptions, CLSCTX_ALL);
}
CreateCompositePackageCatalogOptions CreateCreateCompositePackageCatalogOptions() {
    return winrt::create_instance<CreateCompositePackageCatalogOptions>(CLSID_CreateCompositePackageCatalogOptions, CLSCTX_ALL);
}
PackageMatchFilter CreatePackageMatchFilter() {
    return winrt::create_instance<PackageMatchFilter>(CLSID_PackageMatchFilter, CLSCTX_ALL);
}
/*
DownloadOptions CreateDownloadOptions() {
    return winrt::create_instance<DownloadOptions>(CLSID_DownloadOptions, CLSCTX_ALL);
}
*/



/*


 END WINGET API HELPERS

 START OWN DEFINED FUNCTIONS


*/

const wstring TAB_CHAR = L"\t";
const wstring NEWLINE_CHAR = L"\n";

const BSTR GetDLLModuleVersion()
{
	return SysAllocString(L"1.0.0.0");
}


// Pre: an empty valid list of CatalogPackages
// Post: the list is filled with the installed packages. If operation fails, the list is empty and returned value is not zero.
int __get_installed_packages(list<CatalogPackage> &packages)
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

	if(findResult.Status() != FindPackagesResultStatus::Ok)
	{
		// Find Error.
		cout << "WindowsPackageManager.DLL.Interop.GetInstalledPackagesFromAllCatalogs(): Failed to find packages on catalog: findResult.Status() was" << int(findResult.Status()) << endl;
		return 10 + int(findResult.Status());
	}

	for(auto match: findResult.Matches())
	{
		if (match.CatalogPackage())
		{
			packages.push_back(match.CatalogPackage());
		}
	}
	return 0;
}

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

bool InitializePackageManager()
{
	try {
		CoInitialize(nullptr);
		WindowsPackageManager = CreatePackageManager();
		return true;
	}
	catch (hresult_error const& ex) {
		cout << "WindowsPackageManager.DLL.Interop.InitializePackageManager(): Failed to create PackageManager. Error: " << ex.message().c_str() << endl;
		return false;
	}
	catch (...)
	{
		cout << "WindowsPackageManager.DLL.Interop.InitializePackageManager(): Failed to create PackageManager. Unknown error." << endl;
		return false;
	}
}


#pragma once
#include "WinRTAct.cpp"
#include "pch.h"
#include "ClassDeclarations.cpp"


static bool ADMIN_DEFINED = false;
static bool IS_ADMIN = false;

static bool IsRunAsAdmin() {
	if(ADMIN_DEFINED)
		return IS_ADMIN;

	BOOL fRet = FALSE;
	HANDLE hToken = NULL;
	if (OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &hToken)) {
		TOKEN_ELEVATION Elevation;
		DWORD cbSize = sizeof(TOKEN_ELEVATION);
		if (GetTokenInformation(hToken, TokenElevation, &Elevation, sizeof(Elevation), &cbSize)) {
			fRet = Elevation.TokenIsElevated;
		}
	}
	if (hToken) {
		CloseHandle(hToken);
	}
	IS_ADMIN = fRet;
	ADMIN_DEFINED = true;
	return fRet;
}

template <typename T>
static T BaseCreateInstance(CLSID CLASS_ID, IID INTERFACE_ID)
{
	cout << "BaseCreateInstance: Entered" << endl;
	T res = nullptr;
	if (IsRunAsAdmin() == 1)
	{
		/*cout << "BaseCreateInstance: Calling WinRTActCreateInstance in ELEVATED mode" << endl;
		T* pointer = WinRTActCreateInstance<T>(CLASS_ID, INTERFACE_ID);
		cout << "BaseCreateInstance: Received pointer from WinRTActCreateInstance. Pointer status: " << (pointer != nullptr? "VALID": "NULLPTR") << endl;
		try {
			res = *pointer; //*(pointer); // TODO: Check this piece of () which is causing 0xC0000005 crashes by being an invalid pointer
		}
		catch (...)
		{
			cout << "BaseCreateInstance: Exception caught when trying to convert pointer to class" << endl;
		}*/
	}
	else
	{
		cout << "BaseCreateInstance: Calling winrt::create_instance in STANDARD mode" << endl;
		res = winrt::create_instance<T>(CLASS_ID, CLSCTX_ALL);
	}
	if(res == nullptr)
		cout << "BaseCreateInstance: Instance is NULLPTR" << endl;
	else
		cout << "BaseCreateInstance: Instance appears to be VALID" << endl;
	return res;
}

static PackageManager CreatePackageManager() {
	return BaseCreateInstance<PackageManager>(CLSID_PackageManager, IID_PackageManager);
}

static InstallOptions CreateInstallOptions() {
	return BaseCreateInstance<InstallOptions>(CLSID_InstallOptions, IID_InstallOptions);
}

static FindPackagesOptions CreateFindPackagesOptions() {
	return BaseCreateInstance<FindPackagesOptions>(CLSID_FindPackagesOptions, IID_FindPackagesOptions);
}

static CreateCompositePackageCatalogOptions CreateCreateCompositePackageCatalogOptions() {
	return BaseCreateInstance<CreateCompositePackageCatalogOptions>(CLSID_CreateCompositePackageCatalogOptions, IID_CreateCompositePackageCatalogOptions);
}

static PackageMatchFilter CreatePackageMatchFilter() {
	return BaseCreateInstance<PackageMatchFilter>(CLSID_PackageMatchFilter, IID_PackageMatchFilter);
}


#pragma once
#include "pch.h"

using namespace std;
using namespace winrt::Microsoft::Management::Deployment;
using namespace winrt::Windows::Foundation::Collections;


#ifdef WINDOWSPACKAGEMANAGER_DLL_INTEROP
#define WINDOWSPACKAGEMANAGER_DLL_INTEROP_API __declspec(dllexport)
#else
#define WINDOWSPACKAGEMANAGER_DLL_INTEROP_API __declspec(dllimport)
#endif

// Pre: Windows Package Manager has been  initialized with the InitializePackageManager() method.
// Post: The list of installed packages is returned on a string, with the following format:
//  Name\t Id\t Version\t [Source]\n
extern "C" WINDOWSPACKAGEMANAGER_DLL_INTEROP_API const BSTR GetInstalledPackagesFromAllCatalogs();

// Pre: Windows Package Manager has been initialized with the InitializePackageManager() method.
// Post: The list of upgradable packages is returned
//  Name\t Id\t Version\t NewVersion\t Source\n
extern "C" WINDOWSPACKAGEMANAGER_DLL_INTEROP_API const BSTR GetAvailableUpdatesFromAllCatalogs();

// Pre: WinGet is installed on the local machine.
// Post: Windows Package Manager gets initialized internally. If initialization fails, false is returned.
extern "C" WINDOWSPACKAGEMANAGER_DLL_INTEROP_API bool InitializePackageManager();

// Pre: Returns the version of this DLL library
// Post: Windows Package Manager gets initialized internally. If initialization fails, false is returned.
extern "C" WINDOWSPACKAGEMANAGER_DLL_INTEROP_API const BSTR GetDLLModuleVersion();


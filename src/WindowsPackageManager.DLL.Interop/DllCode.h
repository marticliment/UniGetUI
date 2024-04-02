#pragma once
#include "pch.h"

extern "C" __declspec(dllexport) bool InitializePackageManager();
extern "C" __declspec(dllexport) const BSTR GetDLLModuleVersion();
extern "C" __declspec(dllexport) const BSTR GetAvailableUpdatesFromAllCatalogs();
extern "C" __declspec(dllexport) const BSTR GetInstalledPackagesFromAllCatalogs();
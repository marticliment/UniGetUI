#pragma once
#include "pch.h"
#include "DllCode.h"
#include "InstanceCreator.cpp"

using namespace std;

int main()
{
	cout << "© 2024, Martí Climent and the contributors.\nThis executable will test the WindowsPackageManager.DLL.Interop.exe module\n";
	cout << "UniGetUI WindowsPackageManager.DLL.Interop Module. Module version: " << to_string(GetDLLModuleVersion()) << endl;
	cout << "Running as administrator: " << IsRunAsAdmin() << endl;
	cout << "\n------------------------------------------------------------------------\n";
	cout << "Press <enter> to create an instance of PackageManager: " << endl;
	
	getline(cin, string());
	
	cout << "\nWindowsPackageManager initialization result (1 = success, 0 = failed): " << endl;
	cout << InitializePackageManager() << endl;
	cout << "\n------------------------------------------------------------------------\n";
	cout << "Now you should see a list of installed packages in the following format:\n";
	cout << "\tName \t Id \t Version \t[Source]" << endl;
	cout << "NOTE: if Source is empty, package is local only\n";
	cout << "Press <enter> when ready: " << endl;
	
	getline(cin, string());
	
	cout << "\n" << to_string(GetInstalledPackagesFromAllCatalogs()) << endl;
	cout << "\n------------------------------------------------------------------------\n";
	cout << "\nNow we are going to test the Updates. The output format is the same as before\n";
	cout << "Press <enter> when ready: " << endl;
	
	getline(cin, string());
	
	cout << to_string(GetAvailableUpdatesFromAllCatalogs()) << endl;
	cout << "\n\nAll tests completed. Press <enter> to exit." << endl;
	
	getline(cin, string());
}
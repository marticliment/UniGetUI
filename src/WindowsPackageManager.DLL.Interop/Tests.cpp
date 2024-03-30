#include "pch.h"
#include "DllCode.h"
#include <iostream>

using namespace std;

int main()
{
	cout << "Hello World!\nThis script will test the WindowsPackageManager.DLL.Interop Module.\n";
	cout << "Press <enter> when ready: " << endl;
	
	getline(cin, string());
	
	cout << "\nWindowsPackageManager initialization result (1 = success, 0 = failed): " << InitializePackageManager() << endl;
	cout << "\n------------------------------------------------------------------------\n";
	cout << "Now you should see a list of installed packages in the following format:\n";
	cout << "\tName \t Id \t Version \t[Source]" << endl;
	cout << "NOTE: if Source is empty, package is local only\n";
	cout << "Press <enter> when ready: " << endl;
	
	getline(cin, string());
	
	cout << "\n" << GetInstalledPackagesFromAllCatalogs() << endl;
	cout << "\n------------------------------------------------------------------------\n";
	cout << "\nNow we are going to test the Updates. The output format is the same as before\n";
	cout << "Press <enter> when ready: " << endl;
	
	getline(cin, string());
	
	cout << GetAvailableUpdatesFromAllCatalogs() << endl;
	cout << "\n\nAll tests completed. Press <enter> to exit." << endl;
	
	getline(cin, string());
}
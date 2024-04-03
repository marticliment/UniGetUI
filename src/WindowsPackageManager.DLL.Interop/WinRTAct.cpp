#pragma once
#include "pch.h"

template <typename T>
T* WinRTActCreateInstance(GUID clsid, GUID iid)
{
    IUnknown* pUnknown = nullptr;
    T* pInterface = nullptr;

    // Load the DLL.
    HMODULE hModule = LoadLibrary("winrtact.dll");
    if (hModule == NULL)
    {
        cout << "WinRTActCreateInstance: Cannot load winrtact.dll" << endl;
        return nullptr;
    }
    cout << "WinRTActCreateInstance: Loaded winrtact.dll successfully" << endl;

    // Get the function pointer.
    typedef HRESULT(WINAPI* WinGetServerManualActivation_CreateInstance)(REFGUID, REFGUID, DWORD, LPVOID*);
    WinGetServerManualActivation_CreateInstance pFunc = (WinGetServerManualActivation_CreateInstance)GetProcAddress(hModule, "WinGetServerManualActivation_CreateInstance");
    if (pFunc == NULL)
    {
        cout << "WinRTActCreateInstance: Cannot load function WinGetServerManualActivation_CreateInstance from winrtact.dll" << endl;
        FreeLibrary(hModule);
        return nullptr;
    }
    cout << "WinRTActCreateInstance: Loaded WinGetServerManualActivation_CreateInstance address successfully" << endl;

    // Call the function.
    HRESULT hr = pFunc(clsid, iid, 0, (void**)&pUnknown);
    if (FAILED(hr))
    {
        cout << "WinRTActCreateInstance: WinGetServerManualActivation_CreateInstance failed with code " << to_string(hr) << endl;
        FreeLibrary(hModule);
        return nullptr;
    }
    cout << "WinRTActCreateInstance: Returned from pFunc. pUnknown status: " << (pUnknown == nullptr ? "NULLPTR" : "VALID") << endl;
    cout << "WinRTActCreateInstance: Attempting to convert to the specified interface." << endl;

    // Query the interface.
    hr = pUnknown->QueryInterface(iid, (void**)&pInterface);
    //hr = pUnknown->QueryInterface(iid, (void**)&pInterface);
    pInterface = (T*)pUnknown;
    if (FAILED(hr))
    {
        cout << "WinRTActCreateInstance: pUnknown->QueryInterface failed with code " << to_string(hr) << endl;
        pUnknown->Release();
        FreeLibrary(hModule);
        return nullptr;
    }
    cout << "WinRTActCreateInstance: Pointer converted to class successfully. Now returning from WinRTActCreateInstance" << endl;

    // Release the IUnknown pointer.
    pUnknown->Release();

    // Unload the DLL.
    FreeLibrary(hModule);

    return pInterface;
}
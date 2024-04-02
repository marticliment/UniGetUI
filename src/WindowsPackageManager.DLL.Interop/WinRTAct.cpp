#pragma once
#include "pch.h"

typedef HRESULT(WINAPI* PFN_WinGetServerManualActivation_CreateInstance)(
    const GUID& clsid,
    const GUID& iid,
    DWORD flags,
    void** instance);

template <typename T>
static T* WinRTActCreateInstance(const GUID& clsid, const GUID& iid)
{
    void* pUnknown = nullptr;
    HRESULT hr = S_OK;

    HMODULE hModule = LoadLibrary("winrtact.dll");
    cout << "WinRTActCreateInstance: Loaded winrtact.dll successfully" << endl;
    if (hModule)
    {
        PFN_WinGetServerManualActivation_CreateInstance WinRTAct_DllCreateInstance = (PFN_WinGetServerManualActivation_CreateInstance)GetProcAddress(hModule, "WinGetServerManualActivation_CreateInstance");
        cout << "WinRTActCreateInstance: Loaded WinGetServerManualActivation_CreateInstance address successfully" << endl;
        try
        {
            hr = WinRTAct_DllCreateInstance(clsid, iid, CLSCTX_ALL, &pUnknown);
            cout << "WinRTActCreateInstance: Returned from WinRTAct_DllCreateInstance." << endl;
            if (FAILED(hr))
            {
                throw hr;
            }
            cout << "WinRTActCreateInstance: Attempting to convert to the specified interface." << endl;
            T* pInterface;
            hr = ((IUnknown*)pUnknown)->QueryInterface(iid, (void**)&pInterface);
            if (FAILED(hr))
            {
                throw hr;
            }
            cout << "WinRTActCreateInstance: Pointer converted to class successfully. Now returning from WinRTActCreateInstance" << endl;
            return pInterface;
        }
        catch (HRESULT hrException)
        {
            cout << "WinRTActCreateInstance: Error loading instance with hrException " << hrException << endl;
            throw hrException;
        }
        catch (...)
        {
            cout << "WinRTActCreateInstance: Error loading instance unknown error" << endl;
            throw;
        }
	}
	else
	{
        cout << "WinRTActCreateInstance: DLL not found or loaded" << endl;
		throw HRESULT_FROM_WIN32(GetLastError());
	}
}
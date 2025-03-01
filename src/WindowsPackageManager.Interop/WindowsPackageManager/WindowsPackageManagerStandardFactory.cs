// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.System.Com;
using WinRT;

namespace WindowsPackageManager.Interop;

public class WindowsPackageManagerStandardFactory : WindowsPackageManagerFactory
{
    public WindowsPackageManagerStandardFactory(ClsidContext clsidContext = ClsidContext.Prod, bool allowLowerTrustRegistration = false)
        : base(clsidContext, allowLowerTrustRegistration)
    {
    }

    protected override T CreateInstance<T>(Guid clsid, Guid iid)
    {
        nint pUnknown = IntPtr.Zero;
        try
        {
            CLSCTX clsctx = CLSCTX.CLSCTX_LOCAL_SERVER;
            if (_allowLowerTrustRegistration)
            {
                clsctx |= CLSCTX.CLSCTX_ALLOW_LOWER_TRUST_REGISTRATION;
            }

            Windows.Win32.Foundation.HRESULT hr = PInvoke.CoCreateInstance(clsid, null, clsctx, iid, out object result);

            //                     !! WARNING !!
            // An exception may be thrown on the line below if UniGetUI
            // runs as administrator and AllowLowerTrustRegistration settings is not checked
            // or when WinGet is not installed on the system.
            // It can be safely ignored if any of the conditions
            // above are met.
            Marshal.ThrowExceptionForHR(hr);

            pUnknown = Marshal.GetIUnknownForObject(result);
            return MarshalGeneric<T>.FromAbi(pUnknown);
        }
        finally
        {
            // CoCreateInstance and FromAbi both AddRef on the native object.
            // Release once to prevent memory leak.
            if (pUnknown != IntPtr.Zero)
            {
                Marshal.Release(pUnknown);
            }
        }
    }
}

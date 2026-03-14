using System.Runtime.InteropServices;

namespace WindowsPackageManager.Interop;

public sealed class WinGetComActivationException : COMException
{
    public Guid Clsid { get; }
    public Guid Iid { get; }
    public bool AllowLowerTrustRegistration { get; }
    public string HResultHex => $"0x{HResult:X8}";

    public string Reason => DescribeHResult(HResult);

    public bool IsExpectedFallbackCondition =>
        HResult is
            unchecked((int)0x80040154) or
            unchecked((int)0x80070490) or
            unchecked((int)0x80070002) or
            unchecked((int)0x8000000F)
        || Message.Contains("Element not found", StringComparison.OrdinalIgnoreCase)
        || Message.Contains(
            "Typename or Namespace was not found in metadata file",
            StringComparison.OrdinalIgnoreCase
        );

    public WinGetComActivationException(
        Guid clsid,
        Guid iid,
        int hresult,
        bool allowLowerTrustRegistration
    )
        : base(CreateMessage(clsid, iid, hresult, allowLowerTrustRegistration), hresult)
    {
        Clsid = clsid;
        Iid = iid;
        AllowLowerTrustRegistration = allowLowerTrustRegistration;
    }

    private static string CreateMessage(
        Guid clsid,
        Guid iid,
        int hresult,
        bool allowLowerTrustRegistration
    ) =>
        $"WinGet COM activation failed for CLSID {clsid} (IID {iid}, AllowLowerTrustRegistration={allowLowerTrustRegistration}): {DescribeHResult(hresult)}";

    private static string DescribeHResult(int hresult) => hresult switch
    {
        unchecked((int)0x80040154) => "Class not registered",
        unchecked((int)0x80070490) => "Element not found",
        unchecked((int)0x80070002) => "File not found",
        unchecked((int)0x8000000F) => "Typename or Namespace was not found in metadata file",
        _ => Marshal.GetExceptionForHR(hresult)?.Message ?? $"HRESULT 0x{hresult:X8}",
    };
}


namespace UniGetUI.Core
{
    /// <summary>
    /// General functions, may be move to helper (static) class
    /// </summary>
    public interface IAppTools
    {
        string FormatAsName(string v);
        Task<double> GetFileSizeAsync(Uri installerUrl);
        bool IsAdministrator();
        string Translate(string text);
        Task<string> Which(string command);
    }
}
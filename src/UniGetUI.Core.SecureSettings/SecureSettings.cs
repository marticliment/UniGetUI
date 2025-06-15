using System.Diagnostics;
using UniGetUI.Core.Data;
using UniGetUI.Core.Tools;
using YamlDotNet.Serialization;

namespace UniGetUI.Core.SettingsEngine.SecureSettings;

public static class SecureSettings
{
    // Various predefined secure settings keys
    public const string ALLOW_CLI_ARGUMENTS = "AllowCLIArguments";
    public const string ALLOW_IMPORTING_CLI_ARGUMENTS = "AllowImportingCLIArguments";
    public const string ALLOW_PREPOST_OPERATIONS = "AllowPrePostInstallCommands";
    public const string ALLOW_IMPORT_PREPOST_OPERATIONS = "AllowImportingPrePostInstallCommands";
    public const string FORCE_USER_GSUDO = "ForceUserGSudo";


    private static readonly Dictionary<string, bool> _cache = new();

    public static class Args
    {
        public const string ENABLE_FOR_USER = "--enable-secure-setting-for-user";
        public const string DISABLE_FOR_USER = "--disable-secure-setting-for-user";
    }

    public static bool Get(string setting)
    {
        string purifiedSetting = CoreTools.MakeValidFileName(setting);
        if (_cache.TryGetValue(purifiedSetting, out var value))
        {
            return value;
        }

        string purifiedUser = CoreTools.MakeValidFileName(Environment.UserName);

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var settingsLocation = Path.Join(appData, "UniGetUI\\SecureSettings", purifiedUser);
        var settingFile = Path.Join(settingsLocation, purifiedSetting);

        if (!Directory.Exists(settingsLocation))
        {
            _cache[purifiedSetting] = false;
            return false;
        }

        bool exists = File.Exists(settingFile);
        _cache[purifiedSetting] = exists;
        return exists;
    }

    public static async Task<bool> TrySet(string setting, bool enabled)
    {
        string purifiedSetting = CoreTools.MakeValidFileName(setting);
        _cache.Remove(purifiedSetting);

        string purifiedUser = CoreTools.MakeValidFileName(Environment.UserName);

        using Process p = new Process();
        p.StartInfo = new()
        {
            UseShellExecute = true,
            CreateNoWindow = true,
            FileName = CoreData.UniGetUIExecutableFile,
            Verb = "runas",
            ArgumentList =
            {
                enabled? Args.ENABLE_FOR_USER: Args.DISABLE_FOR_USER,
                purifiedUser,
                purifiedSetting
            }
        };

        p.Start();
        await p.WaitForExitAsync();
        return p.ExitCode is 0;
    }

    public static int ApplyForUser(string username, string setting, bool enable)
    {
        try
        {
            string purifiedSetting = CoreTools.MakeValidFileName(setting);
            _cache.Remove(purifiedSetting);

            string purifiedUser = CoreTools.MakeValidFileName(username);

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var settingsLocation = Path.Join(appData, "UniGetUI\\SecureSettings", purifiedUser);
            var settingFile = Path.Join(settingsLocation, purifiedSetting);

            if (!Directory.Exists(settingsLocation))
            {
                Directory.CreateDirectory(settingsLocation);
            }

            if (enable)
            {
                File.WriteAllText(settingFile, "");
            }
            else
            {
                if (File.Exists(settingFile))
                {
                    File.Delete(settingFile);
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return -1;
        }
    }
}

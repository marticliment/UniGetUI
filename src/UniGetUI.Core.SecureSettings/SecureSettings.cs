using System.Diagnostics;
using UniGetUI.Core.Data;
using UniGetUI.Core.Tools;

namespace UniGetUI.Core.SettingsEngine.SecureSettings;

public static class SecureSettings
{
    // Various predefined secure settings keys
    public enum K
    {
        AllowCLIArguments,
        AllowImportingCLIArguments,
        AllowPrePostOpCommand,
        AllowImportPrePostOpCommands,
        ForceUserGSudo,
        Unset
    };

    public static string ResolveKey(K key)
    {
        return key switch
        {
            K.AllowCLIArguments => "AllowCLIArguments",
            K.AllowImportingCLIArguments => "AllowImportingCLIArguments",
            K.AllowPrePostOpCommand => "AllowPrePostInstallCommands",
            K.AllowImportPrePostOpCommands => "AllowImportingPrePostInstallCommands",
            K.ForceUserGSudo => "ForceUserGSudo",

            K.Unset => throw new InvalidDataException("SecureSettings key was unset!"),
            _ => throw new KeyNotFoundException($"The SecureSettings key {key} was not found on the ResolveKey map")
        };
    }


    private static readonly Dictionary<string, bool> _cache = new();

    public static class Args
    {
        public const string ENABLE_FOR_USER = "--enable-secure-setting-for-user";
        public const string DISABLE_FOR_USER = "--disable-secure-setting-for-user";
    }

    public static bool Get(K key)
    {
        string purifiedSetting = CoreTools.MakeValidFileName(ResolveKey(key));
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

    public static async Task<bool> TrySet(K key, bool enabled)
    {
        string purifiedSetting = CoreTools.MakeValidFileName(ResolveKey(key));
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

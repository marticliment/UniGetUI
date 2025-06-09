using System.Diagnostics;
using UniGetUI.Core.Data;
using UniGetUI.Core.Tools;

namespace UniGetUI.Core.SettingsEngine.SecureSettings;

public static class SecureSettings
{
    public static class Args
    {
        public const string ENABLE_FOR_USER = "--enable-secure-setting-for-user";
        public const string DISABLE_FOR_USER = "--disable-secure-setting-for-user";
    }

    public static bool Get(string setting)
    {
        string purifiedUser = CoreTools.MakeValidFileName(Environment.UserName);
        string purifiedSetting = CoreTools.MakeValidFileName(setting);

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var settingsLocation = Path.Join(appData, "UniGetUI\\SecureSettings", purifiedUser);
        var settingFile = Path.Join(settingsLocation, purifiedSetting);

        if (!Directory.Exists(settingsLocation))
            return false;

        return File.Exists(settingFile);
    }

    public static async Task<bool> TrySet(string setting, bool enabled)
    {
        string purifiedUser = CoreTools.MakeValidFileName(Environment.UserName);
        string purifiedSetting = CoreTools.MakeValidFileName(setting);

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
            string purifiedUser = CoreTools.MakeValidFileName(username);
            string purifiedSetting = CoreTools.MakeValidFileName(setting);

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

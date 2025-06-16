using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.SettingsEngine.SecureSettings;

namespace UniGetUI;

public static class CLIHandler
{
    public const string HELP = "--help";
    public const string DAEMON = "--daemon";
    public const string MIGRATE_WINGETUI_TO_UNIGETUI = "--migrate-wingetui-to-unigetui";
    public const string UNINSTALL_WINGETUI = "--uninstall-wingetui";
    public const string UNINSTALL_UNIGETUI = "--uninstall-unigetui";
    public const string NO_CORRUPT_DIALOG = "--no-corrupt-dialog";

    public const string IMPORT_SETTINGS = "--import-settings";
    public const string EXPORT_SETTINGS = "--export-settings";

    public const string ENABLE_SETTING = "--enable-setting";
    public const string DISABLE_SETTING = "--disable-setting";
    public const string SET_SETTING_VAL = "--set-setting-value";

    public const string ENABLE_SECURE_SETTING = "--enable-secure-setting";
    public const string DISABLE_SECURE_SETTING = "--disable-secure-setting";
    public const string ENABLE_SECURE_SETTING_FOR_USER = SecureSettings.Args.ENABLE_FOR_USER;
    public const string DISABLE_SECURE_SETTING_FOR_USER = SecureSettings.Args.DISABLE_FOR_USER;


    private enum HRESULT
    {
        SUCCESS = 0,
        STATUS_FAILED = -1,
        STATUS_INVALID_PARAMETER = -1073741811,
        STATUS_NO_SUCH_FILE = -1073741809,
        STATUS_UNKNOWN__SETTINGS_KEY = -2,
    }

    public static int Help()
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://github.com/marticliment/UniGetUI/blob/main/cli-arguments.md#unigetui-command-line-parameters",
            UseShellExecute = true
        });
        return 0;
    }

    public static int ImportSettings()
    {
        var args = Environment.GetCommandLineArgs().ToList();

        var filePos = args.IndexOf(IMPORT_SETTINGS);
        if (filePos < 0)
            return (int)HRESULT.STATUS_INVALID_PARAMETER; // The base paramater --import-settings was not found

        if (filePos +1 >= args.Count)
            return (int)HRESULT.STATUS_INVALID_PARAMETER; // The file parameter does not exist (import settings requires "--import-settings file")

        var file = args[filePos + 1].Trim('"').Trim('\'');
        if (!File.Exists(file))
            return (int)HRESULT.STATUS_NO_SUCH_FILE; // The given file does not exist

        try
        {
            Settings.ImportFromJSON(file);
        }
        catch (Exception ex)
        {
            return ex.HResult;
        }

        return (int)HRESULT.SUCCESS;
    }

    public static int ExportSettings()
    {
        var args = Environment.GetCommandLineArgs().ToList();

        var filePos = args.IndexOf(EXPORT_SETTINGS);
        if (filePos < 0)
            return (int)HRESULT.STATUS_INVALID_PARAMETER; // The base paramater --export-settings was not found

        if (filePos +1 >= args.Count)
            return (int)HRESULT.STATUS_INVALID_PARAMETER; // The file parameter does not exist (export settings requires "--export-settings file")

        var file = args[filePos + 1].Trim('"').Trim('\'');

        try
        {
            Settings.ExportToJSON(file);
        }
        catch (Exception ex)
        {
            return ex.HResult;
        }

        return (int)HRESULT.SUCCESS;
    }

    public static int EnableSetting()
    {
        var args = Environment.GetCommandLineArgs().ToList();

        var basePos = args.IndexOf(ENABLE_SETTING);
        if (basePos < 0)
            return (int)HRESULT.STATUS_INVALID_PARAMETER; // The base paramater --export-settings was not found

        if (basePos +1 >= args.Count)
            return (int)HRESULT.STATUS_INVALID_PARAMETER; // The file parameter does not exist (export settings requires "--export-settings file")

        var setting = args[basePos + 1].Trim('"').Trim('\'');
        if (!Enum.TryParse(setting, out Settings.K validKey))
            return (int)HRESULT.STATUS_UNKNOWN__SETTINGS_KEY;

        try
        {
            Settings.Set(validKey, true);
        }
        catch (Exception ex)
        {
            return ex.HResult;
        }

        return (int)HRESULT.SUCCESS;
    }

    public static int DisableSetting()
    {
        var args = Environment.GetCommandLineArgs().ToList();

        var basePos = args.IndexOf(DISABLE_SETTING);
        if (basePos < 0)
            return (int)HRESULT.STATUS_INVALID_PARAMETER; // The base paramater --export-settings was not found

        if (basePos +1 >= args.Count)
            return (int)HRESULT.STATUS_INVALID_PARAMETER; // The file parameter does not exist (export settings requires "--export-settings file")

        var setting = args[basePos + 1].Trim('"').Trim('\'');
        if (!Enum.TryParse(setting, out Settings.K validKey))
            return (int)HRESULT.STATUS_UNKNOWN__SETTINGS_KEY;
        try
        {
            Settings.Set(validKey, false);
        }
        catch (Exception ex)
        {
            return ex.HResult;
        }

        return (int)HRESULT.SUCCESS;
    }

    public static int SetSettingsValue()
    {
        var args = Environment.GetCommandLineArgs().ToList();

        var basePos = args.IndexOf(SET_SETTING_VAL);
        if (basePos < 0)
            return (int)HRESULT.STATUS_INVALID_PARAMETER; // The base paramater --export-settings was not found

        if (basePos +2 >= args.Count)
            return (int)HRESULT.STATUS_INVALID_PARAMETER; // The file parameter does not exist (export settings requires "--export-settings file")

        var setting = args[basePos + 1].Trim('"').Trim('\'');
        var value = args[basePos + 2];
        if (!Enum.TryParse(setting, out Settings.K validKey))
            return (int)HRESULT.STATUS_UNKNOWN__SETTINGS_KEY;

        try
        {
            Settings.SetValue(validKey, value);
        }
        catch (Exception ex)
        {
            return ex.HResult;
        }

        return (int)HRESULT.SUCCESS;
    }

    public static int WingetUIToUniGetUIMigrator()
    {
        try
        {
            string[] BasePaths =
            [
                // User desktop icon
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),

                // User start menu icon
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),

                // Common desktop icon
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),

                // User start menu icon
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            ];

            foreach (string path in BasePaths)
            {
                foreach (string old_wingetui_icon in new[] { "WingetUI.lnk", "WingetUI .lnk", "UniGetUI (formerly WingetUI) .lnk", "UniGetUI (formerly WingetUI).lnk" })
                {
                    try
                    {
                        string old_file = Path.Join(path, old_wingetui_icon);
                        string new_file = Path.Join(path, "UniGetUI.lnk");
                        if (!File.Exists(old_file))
                        {
                            continue;
                        }

                        if (File.Exists(old_file) && File.Exists(new_file))
                        {
                            Logger.Info("Deleting shortcut " + old_file + " since new shortcut already exists");
                            File.Delete(old_file);
                        }
                        else if (File.Exists(old_file) && !File.Exists(new_file))
                        {
                            Logger.Info("Moving shortcut to " + new_file);
                            File.Move(old_file, new_file);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"An error occurred while migrating the shortcut {Path.Join(path, old_wingetui_icon)}");
                        Logger.Warn(ex);
                    }
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            return ex.HResult;
        }
    }

    public static int UninstallUniGetUI()
    {
        // There is currently no uninstall logic. However, this needs to be maintained, or otherwhise UniGetUI will launch on uninstall
        return 0;
    }

    public static int EnableSecureSetting()
    {
        var args = Environment.GetCommandLineArgs().ToList();

        var basePos = args.IndexOf(ENABLE_SECURE_SETTING);
        if (basePos < 0)
            return (int)HRESULT.STATUS_INVALID_PARAMETER; // The base paramater was not found

        if (basePos +1 >= args.Count)
            return (int)HRESULT.STATUS_INVALID_PARAMETER; // The file parameter does not exist (export settings requires "--export-settings file")

        var setting = args[basePos + 1].Trim('"').Trim('\'');
        if (!Enum.TryParse(setting, out SecureSettings.K validKey))
            return (int)HRESULT.STATUS_UNKNOWN__SETTINGS_KEY;

        try
        {
            bool success = SecureSettings.TrySet(validKey, true).GetAwaiter().GetResult();
            if (!success) return (int)HRESULT.STATUS_FAILED;
            else return (int)HRESULT.SUCCESS;
        }
        catch (Exception ex)
        {
            return ex.HResult;
        }
    }

    public static int DisableSecureSetting()
    {
        var args = Environment.GetCommandLineArgs().ToList();

        var basePos = args.IndexOf(DISABLE_SECURE_SETTING);
        if (basePos < 0)
            return (int)HRESULT.STATUS_INVALID_PARAMETER; // The base paramater was not found

        if (basePos +1 >= args.Count)
            return (int)HRESULT.STATUS_INVALID_PARAMETER; // The first positional argument does not exist

        var setting = args[basePos + 1].Trim('"').Trim('\'');
        if (!Enum.TryParse(setting, out SecureSettings.K validKey))
            return (int)HRESULT.STATUS_UNKNOWN__SETTINGS_KEY;

        try
        {
            bool success = SecureSettings.TrySet(validKey, false).GetAwaiter().GetResult();
            if (!success) return (int)HRESULT.STATUS_FAILED;
            else return (int)HRESULT.SUCCESS;
        }
        catch (Exception ex)
        {
            return ex.HResult;
        }
    }

    public static int EnableSecureSettingForUser()
    {
        var args = Environment.GetCommandLineArgs().ToList();

        var basePos = args.IndexOf(ENABLE_SECURE_SETTING_FOR_USER);
        if (basePos < 0)
            return (int)HRESULT.STATUS_INVALID_PARAMETER; // The base paramater was not found

        if (basePos +2 >= args.Count)
            return (int)HRESULT.STATUS_INVALID_PARAMETER; // The required parameters do not exist

        var user = args[basePos + 1].Trim('"').Trim('\'');
        var setting = args[basePos + 2].Trim('"').Trim('\'');

        try
        {
            return SecureSettings.ApplyForUser(user, setting, true);
        }
        catch (Exception ex)
        {
            return ex.HResult;
        }
    }

    public static int DisableSecureSettingForUser()
    {
        var args = Environment.GetCommandLineArgs().ToList();

        var basePos = args.IndexOf(DISABLE_SECURE_SETTING_FOR_USER);
        if (basePos < 0)
            return (int)HRESULT.STATUS_INVALID_PARAMETER; // The base paramater was not found

        if (basePos +2 >= args.Count)
            return (int)HRESULT.STATUS_INVALID_PARAMETER; // The required parameters do not exist

        var user = args[basePos + 1].Trim('"').Trim('\'');
        var setting = args[basePos + 2].Trim('"').Trim('\'');

        try
        {
            return SecureSettings.ApplyForUser(user, setting, false);
        }
        catch (Exception ex)
        {
            return ex.HResult;
        }
    }
}

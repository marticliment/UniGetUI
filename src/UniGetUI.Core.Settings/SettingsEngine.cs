using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;

namespace UniGetUI.Core.SettingsEngine
{
    public static class Settings
    {
        public static bool AreNotificationsDisabled()
        {
            return Get("DisableSystemTray") || Get("DisableNotifications");
        }

        public static bool AreUpdatesNotificationsDisabled()
        {
            return AreNotificationsDisabled() || Get("DisableUpdatesNotifications");
        }

        public static bool AreErrorNotificationsDisabled()
        {
            return AreNotificationsDisabled() || Get("DisableErrorNotifications");
        }

        public static bool AreSuccessNotificationsDisabled()
        {
            return AreNotificationsDisabled() || Get("DisableSuccessNotifications");
        }

        public static bool Get(string setting, bool invert = false)
        {
            return File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, setting)) ^ invert;
        }

        public static void Set(string setting, bool value)
        {
            try
            {
                if (value)
                {
                    if (!File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, setting)))
                    {
                        File.WriteAllText(Path.Join(CoreData.UniGetUIDataDirectory, setting), "");
                    }
                }
                else
                {
                    if (File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, setting)))
                    {
                        File.Delete(Path.Join(CoreData.UniGetUIDataDirectory, setting));
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error($"CANNOT SET SETTING FOR setting={setting} enabled={value}");
                Logger.Error(e);
            }
        }

        public static string GetValue(string setting)
        {
            if (!File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, setting)))
            {
                return "";
            }

            return File.ReadAllText(Path.Join(CoreData.UniGetUIDataDirectory, setting));
        }

        public static void SetValue(string setting, string value)
        {
            try
            {
                if (value == "")
                {
                    Set(setting, false);
                }
                else
                {
                    File.WriteAllText(Path.Join(CoreData.UniGetUIDataDirectory, setting), value);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"CANNOT SET SETTING VALUE FOR setting={setting} enabled={value}");
                Logger.Error(e);
            }
        }
    }
}

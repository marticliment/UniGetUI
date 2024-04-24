namespace UniGetUI.Core
{
    /// <summary>
    /// Broker for settings. The jury is not out yet: are these UI settings or engine settings?
    /// </summary>
    public interface IAppConfig
    {
        bool GetSettings(string setting, bool invert = false);
        string GetSettingsValue(string setting);
        void SetSettings(string setting, bool value);
        void SetSettingsValue(string setting, string value);
    }
}
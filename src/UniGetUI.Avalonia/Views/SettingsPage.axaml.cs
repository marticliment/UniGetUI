using Avalonia.Controls;
using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using UniGetUI.Core.Data;
using UniGetUI.Core.Language;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine;

namespace UniGetUI.Avalonia.Views;

public partial class SettingsPage : UserControl
{
    // Maps setting name -> whether value is inverted
    // "Disable*" settings are inverted: toggle ON = setting OFF (feature enabled)
    private static readonly HashSet<string> InvertedSettings = new()
    {
        "DisableSystemTray",
        "DisableIconsOnPackageLists",
        "DisableNotifications",
        "DisableUpdatesNotifications",
        "DisableErrorNotifications",
        "DisableSuccessNotifications",
        "DisableProgressNotifications",
        "DisableAutoCheckforUpdates",
        "DisableApi",
        "DisableTimeoutOnPackageListingTasks",
    };

    public SettingsPage()
    {
        InitializeComponent();
        LoadCurrentValues();
        PopulateLanguages();
        PopulateManagersList();

        AboutVersionText.Text = $"UniGetUI v{CoreData.VersionName}";
    }

    private void LoadCurrentValues()
    {
        // Theme
        string theme = Settings.GetValue(Settings.K.PreferredTheme);
        foreach (ComboBoxItem item in ThemeCombo.Items)
        {
            if (item.Tag?.ToString() == theme)
            {
                ThemeCombo.SelectedItem = item;
                break;
            }
        }
        if (ThemeCombo.SelectedItem is null)
            ThemeCombo.SelectedIndex = 0; // "Follow system"

        // Startup page
        string startupPage = Settings.GetValue(Settings.K.StartupPage);
        foreach (ComboBoxItem item in StartupPageCombo.Items)
        {
            if (item.Tag?.ToString() == startupPage)
            {
                StartupPageCombo.SelectedItem = item;
                break;
            }
        }
        if (StartupPageCombo.SelectedItem is null)
            StartupPageCombo.SelectedIndex = 0;

        // Bool toggles
        LoadBoolToggle(SystemTrayToggle, Settings.K.DisableSystemTray);
        LoadBoolToggle(PackageIconsToggle, Settings.K.DisableIconsOnPackageLists);
        LoadBoolToggle(VersionTitleToggle, Settings.K.ShowVersionNumberOnTitlebar);
        LoadBoolToggle(NotificationsToggle, Settings.K.DisableNotifications);
        LoadBoolToggle(UpdateNotifToggle, Settings.K.DisableUpdatesNotifications);
        LoadBoolToggle(ErrorNotifToggle, Settings.K.DisableErrorNotifications);
        LoadBoolToggle(SuccessNotifToggle, Settings.K.DisableSuccessNotifications);
        LoadBoolToggle(ProgressNotifToggle, Settings.K.DisableProgressNotifications);
        LoadBoolToggle(AutoCheckUpdatesToggle, Settings.K.DisableAutoCheckforUpdates);
        LoadBoolToggle(AutoUpdatePkgsToggle, Settings.K.AutomaticallyUpdatePackages);
        LoadBoolToggle(MaintainSuccessToggle, Settings.K.MaintainSuccessfulInstalls);
        LoadBoolToggle(ProhibitElevationToggle, Settings.K.ProhibitElevation);
        LoadBoolToggle(DisableIntegrityToggle, Settings.K.DisableIntegrityChecks);
        LoadBoolToggle(ProxyToggle, Settings.K.EnableProxy);
        LoadBoolToggle(ProxyAuthToggle, Settings.K.EnableProxyAuth);
        LoadBoolToggle(BackgroundApiToggle, Settings.K.DisableApi);
        LoadBoolToggle(DisableTimeoutToggle, Settings.K.DisableTimeoutOnPackageListingTasks);

        // Proxy URL
        ProxyUrlBox.Text = Settings.GetValue(Settings.K.ProxyURL);

        // Update interval
        string interval = Settings.GetValue(Settings.K.UpdatesCheckInterval);
        foreach (ComboBoxItem item in UpdateIntervalCombo.Items)
        {
            if (item.Tag?.ToString() == interval)
            {
                UpdateIntervalCombo.SelectedItem = item;
                break;
            }
        }
        if (UpdateIntervalCombo.SelectedItem is null)
            UpdateIntervalCombo.SelectedIndex = 1; // Default 1 hour

        // Parallel operations
        string parallelOps = Settings.GetValue(Settings.K.ParallelOperationCount);
        foreach (ComboBoxItem item in ParallelOpsCombo.Items)
        {
            if (item.Tag?.ToString() == parallelOps)
            {
                ParallelOpsCombo.SelectedItem = item;
                break;
            }
        }
        if (ParallelOpsCombo.SelectedItem is null)
            ParallelOpsCombo.SelectedIndex = 0; // Default 1
    }

    private void LoadBoolToggle(ToggleSwitch toggle, Settings.K key)
    {
        string settingName = toggle.Tag?.ToString() ?? "";
        bool rawValue = Settings.Get(key);
        bool isInverted = InvertedSettings.Contains(settingName);

        // For inverted settings: Disable* = true means feature is OFF, so toggle should be OFF
        // For normal settings: value = true means toggle should be ON
        toggle.IsChecked = isInverted ? !rawValue : rawValue;
    }

    private void PopulateLanguages()
    {
        var langs = new List<LanguageItem>();
        string currentLang = Settings.GetValue(Settings.K.PreferredLanguage);

        langs.Add(new LanguageItem("System default", "default", ""));

        foreach (var kvp in LanguageData.LanguageReference)
        {
            string pct = LanguageData.TranslationPercentages.TryGetValue(kvp.Key, out var p) ? p : "";
            langs.Add(new LanguageItem(kvp.Value, kvp.Key, pct));
        }

        LanguageCombo.ItemsSource = langs;
        LanguageCombo.DisplayMemberBinding = new global::Avalonia.Data.Binding("Display");

        // Select current
        int idx = 0;
        for (int i = 0; i < langs.Count; i++)
        {
            if (langs[i].Code == currentLang || (string.IsNullOrEmpty(currentLang) && langs[i].Code == "default"))
            {
                idx = i;
                break;
            }
        }
        LanguageCombo.SelectedIndex = idx;
    }

    private void PopulateManagersList()
    {
        foreach (var manager in PEInterface.Managers)
        {
            var disabledManagers = Settings.GetDictionary<string, bool>(Settings.K.DisabledManagers);
            bool isDisabled = disabledManagers.TryGetValue(manager.Name, out var disabled) && disabled;

            var expander = new SettingsExpander
            {
                Header = manager.DisplayName,
                Description = manager.Name,
                IconSource = new SymbolIconSource { Symbol = Symbol.AllApps },
            };

            var toggle = new ToggleSwitch
            {
                IsChecked = !isDisabled,
                Tag = manager.Name,
            };

            toggle.IsCheckedChanged += ManagerToggle_Changed;
            expander.Footer = toggle;

            // Status text
            string statusText;
            if (isDisabled)
                statusText = "Disabled";
            else if (manager.IsReady())
                statusText = $"Ready — {manager.Status.Version}";
            else
                statusText = "Not found";

            var statusItem = new SettingsExpanderItem
            {
                Content = "Status",
                Footer = new TextBlock
                {
                    Text = statusText,
                    FontSize = 13,
                    Opacity = 0.6,
                    VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
                },
            };
            expander.Items.Add(statusItem);

            // Executable path (if available)
            if (manager.Status.ExecutablePath is { } exePath && !string.IsNullOrEmpty(exePath))
            {
                var pathItem = new SettingsExpanderItem
                {
                    Content = "Executable",
                    Footer = new TextBlock
                    {
                        Text = exePath,
                        FontSize = 12,
                        Opacity = 0.45,
                        VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
                        TextTrimming = global::Avalonia.Media.TextTrimming.CharacterEllipsis,
                        MaxWidth = 350,
                    },
                };
                expander.Items.Add(pathItem);
            }

            ManagersPanel.Children.Add(expander);
        }
    }

    // ─── Event handlers ─────────────────────────────────────────────────

    private void ThemeCombo_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (ThemeCombo.SelectedItem is ComboBoxItem item)
        {
            string theme = item.Tag?.ToString() ?? "auto";
            Settings.SetValue(Settings.K.PreferredTheme, theme == "auto" ? "" : theme);
            App.Instance.RequestedThemeVariant = theme switch
            {
                "dark" => ThemeVariant.Dark,
                "light" => ThemeVariant.Light,
                _ => ThemeVariant.Default,
            };
        }
    }

    private void LanguageCombo_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (LanguageCombo.SelectedItem is LanguageItem lang)
        {
            string code = lang.Code == "default" ? "" : lang.Code;
            Settings.SetValue(Settings.K.PreferredLanguage, code);
        }
    }

    private void StartupPageCombo_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (StartupPageCombo.SelectedItem is ComboBoxItem item)
        {
            Settings.SetValue(Settings.K.StartupPage, item.Tag?.ToString() ?? "");
        }
    }

    private void BoolSetting_Changed(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggle) return;
        string settingName = toggle.Tag?.ToString() ?? "";
        if (string.IsNullOrEmpty(settingName)) return;

        bool isChecked = toggle.IsChecked == true;
        bool isInverted = InvertedSettings.Contains(settingName);
        bool valueToStore = isInverted ? !isChecked : isChecked;

        // Find the matching K enum value
        if (TryResolveSettingKey(settingName, out var key))
        {
            Settings.Set(key, valueToStore);
        }
    }

    private void ManagerToggle_Changed(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggle) return;
        string managerName = toggle.Tag?.ToString() ?? "";
        if (string.IsNullOrEmpty(managerName)) return;

        bool enabled = toggle.IsChecked == true;
        Settings.SetDictionaryItem<string, bool>(Settings.K.DisabledManagers, managerName, !enabled);

        // Re-initialize the manager
        var manager = PEInterface.Managers.FirstOrDefault(m => m.Name == managerName);
        if (manager is not null)
            _ = Task.Run(() => manager.Initialize());
    }

    private void UpdateIntervalCombo_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (UpdateIntervalCombo.SelectedItem is ComboBoxItem item)
            Settings.SetValue(Settings.K.UpdatesCheckInterval, item.Tag?.ToString() ?? "60");
    }

    private void ParallelOpsCombo_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (ParallelOpsCombo.SelectedItem is ComboBoxItem item)
            Settings.SetValue(Settings.K.ParallelOperationCount, item.Tag?.ToString() ?? "1");
    }

    private void ProxyUrl_Changed(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        Settings.SetValue(Settings.K.ProxyURL, ProxyUrlBox.Text ?? "");
    }

    private void ResetButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        Settings.ResetSettings();
        LoadCurrentValues();
    }

    // ─── Helpers ────────────────────────────────────────────────────────

    private static bool TryResolveSettingKey(string name, out Settings.K key)
    {
        // Try to parse the setting name to the K enum
        foreach (Settings.K k in Enum.GetValues(typeof(Settings.K)))
        {
            try
            {
                if (Settings.ResolveKey(k) == name)
                {
                    key = k;
                    return true;
                }
            }
            catch
            {
                // Some keys (like Unset) throw - skip
            }
        }
        key = default;
        return false;
    }

    private record LanguageItem(string Name, string Code, string Percentage)
    {
        public string Display => Code == "default"
            ? Name
            : string.IsNullOrEmpty(Percentage) ? Name : $"{Name} ({Percentage}%)";
    }
}

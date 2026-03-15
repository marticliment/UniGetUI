using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.SettingsEngine.SecureSettings;
using UniGetUI.Core.Tools;
using System.Threading.Tasks;

namespace UniGetUI.Avalonia.Views.Pages.SettingsPages;

public partial class AdministratorSettingsView : UserControl, ISettingsSectionView
{
    // Regular CheckBoxes
    private CheckBox DoCacheAdminRightsForBatchesCheckBoxControl => GetControl<CheckBox>("DoCacheAdminRightsForBatchesCheckBox");
    private CheckBox DoCacheAdminRightsCheckBoxControl => GetControl<CheckBox>("DoCacheAdminRightsCheckBox");
    private CheckBox ProhibitElevationCheckBoxControl => GetControl<CheckBox>("ProhibitElevationCheckBox");

    // Secure CheckBoxes
    private CheckBox AllowCLIArgumentsCheckBoxControl => GetControl<CheckBox>("AllowCLIArgumentsCheckBox");
    private CheckBox AllowPrePostCommandsCheckBoxControl => GetControl<CheckBox>("AllowPrePostCommandsCheckBox");
    private CheckBox AllowCustomManagerPathsCheckBoxControl => GetControl<CheckBox>("AllowCustomManagerPathsCheckBox");
    private CheckBox AllowImportingCLIArgumentsCheckBoxControl => GetControl<CheckBox>("AllowImportingCLIArgumentsCheckBox");
    private CheckBox AllowImportingPrePostCommandsCheckBoxControl => GetControl<CheckBox>("AllowImportingPrePostCommandsCheckBox");

    // Pending UAC badges
    private Border AllowCLIArgumentsPendingBadgeControl => GetControl<Border>("AllowCLIArgumentsPendingBadge");
    private Border AllowPrePostCommandsPendingBadgeControl => GetControl<Border>("AllowPrePostCommandsPendingBadge");
    private Border AllowCustomManagerPathsPendingBadgeControl => GetControl<Border>("AllowCustomManagerPathsPendingBadge");
    private Border AllowImportingCLIArgumentsPendingBadgeControl => GetControl<Border>("AllowImportingCLIArgumentsPendingBadge");
    private Border AllowImportingPrePostCommandsPendingBadgeControl => GetControl<Border>("AllowImportingPrePostCommandsPendingBadge");

    // Pending UAC badge texts
    private TextBlock AllowCLIArgumentsPendingTextControl => GetControl<TextBlock>("AllowCLIArgumentsPendingText");
    private TextBlock AllowPrePostCommandsPendingTextControl => GetControl<TextBlock>("AllowPrePostCommandsPendingText");
    private TextBlock AllowCustomManagerPathsPendingTextControl => GetControl<TextBlock>("AllowCustomManagerPathsPendingText");
    private TextBlock AllowImportingCLIArgumentsPendingTextControl => GetControl<TextBlock>("AllowImportingCLIArgumentsPendingText");
    private TextBlock AllowImportingPrePostCommandsPendingTextControl => GetControl<TextBlock>("AllowImportingPrePostCommandsPendingText");

    // Label TextBlocks
    private TextBlock LeadTitleText => GetControl<TextBlock>("LeadTitleBlock");
    private TextBlock LeadDescriptionText => GetControl<TextBlock>("LeadDescriptionBlock");
    private TextBlock WarningTitleText => GetControl<TextBlock>("WarningTitleBlock");
    private TextBlock WarningDescriptionText => GetControl<TextBlock>("WarningDescriptionBlock");
    private TextBlock ElevationTitleText => GetControl<TextBlock>("ElevationTitleBlock");
    private TextBlock ElevationDescriptionText => GetControl<TextBlock>("ElevationDescriptionBlock");
    private TextBlock ElevationHintText => GetControl<TextBlock>("ElevationHintBlock");
    private TextBlock ProhibitTitleText => GetControl<TextBlock>("ProhibitTitleBlock");
    private TextBlock ProhibitDescriptionText => GetControl<TextBlock>("ProhibitDescriptionBlock");
    private TextBlock ProhibitHintText => GetControl<TextBlock>("ProhibitHintBlock");
    private TextBlock OperationsSecureTitleText => GetControl<TextBlock>("OperationsSecureTitleBlock");
    private TextBlock OperationsSecureDescriptionText => GetControl<TextBlock>("OperationsSecureDescriptionBlock");
    private TextBlock AllowCLIArgumentsHintText => GetControl<TextBlock>("AllowCLIArgumentsHintBlock");
    private TextBlock AllowPrePostCommandsHintText => GetControl<TextBlock>("AllowPrePostCommandsHintBlock");
    private TextBlock ManagerPathsTitleText => GetControl<TextBlock>("ManagerPathsTitleBlock");
    private TextBlock ManagerPathsDescriptionText => GetControl<TextBlock>("ManagerPathsDescriptionBlock");
    private TextBlock AllowCustomManagerPathsHintText => GetControl<TextBlock>("AllowCustomManagerPathsHintBlock");
    private TextBlock ImportSecureTitleText => GetControl<TextBlock>("ImportSecureTitleBlock");
    private TextBlock ImportSecureDescriptionText => GetControl<TextBlock>("ImportSecureDescriptionBlock");
    private TextBlock AllowImportingCLIArgumentsHintText => GetControl<TextBlock>("AllowImportingCLIArgumentsHintBlock");
    private TextBlock AllowImportingPrePostCommandsHintText => GetControl<TextBlock>("AllowImportingPrePostCommandsHintBlock");

    public AdministratorSettingsView()
    {
        InitializeComponent();

        DoCacheAdminRightsForBatchesCheckBoxControl.Click += DoCacheAdminRightsForBatches_OnClick;
        DoCacheAdminRightsCheckBoxControl.Click += DoCacheAdminRights_OnClick;
        ProhibitElevationCheckBoxControl.Click += ProhibitElevation_OnClick;
        AllowCLIArgumentsCheckBoxControl.Click += AllowCLIArguments_OnClick;
        AllowPrePostCommandsCheckBoxControl.Click += AllowPrePostCommands_OnClick;
        AllowCustomManagerPathsCheckBoxControl.Click += AllowCustomManagerPaths_OnClick;
        AllowImportingCLIArgumentsCheckBoxControl.Click += AllowImportingCLIArguments_OnClick;
        AllowImportingPrePostCommandsCheckBoxControl.Click += AllowImportingPrePostCommands_OnClick;

        SectionTitle = CoreTools.Translate("Administrator preferences");
        SectionSubtitle = CoreTools.Translate("Administrative execution, elevation caching, and security-gated package behaviors.");
        SectionStatus = CoreTools.Translate("Live settings");

        ApplyLocalizedText();
        LoadStoredValues();
        ApplyControlState();
    }

    public string SectionTitle { get; }
    public string SectionSubtitle { get; }
    public string SectionStatus { get; }

    private void ApplyLocalizedText()
    {
        LeadTitleText.Text = CoreTools.Translate("These settings apply to all package managers and take effect immediately.");
        LeadDescriptionText.Text = CoreTools.Translate("These settings apply to all package managers. Secure settings require a UAC elevation prompt because they affect security-sensitive behaviors.");

        WarningTitleText.Text = CoreTools.Translate("Warning") + "!";
        WarningDescriptionText.Text = CoreTools.Translate("The following settings may pose a security risk, hence they are disabled by default.")
            + " "
            + CoreTools.Translate("Enable the settings below if and only if you fully understand what they do, and the implications they may have.")
            + "\n\n"
            + CoreTools.Translate("The settings will list, in their descriptions, the potential security issues they may have.");

        ElevationTitleText.Text = CoreTools.Translate("Administrator rights caching");
        ElevationDescriptionText.Text = CoreTools.Translate("Change how operations request administrator rights.");
        DoCacheAdminRightsForBatchesCheckBoxControl.Content = CoreTools.Translate("Ask for administrator privileges once for each batch of operations");
        DoCacheAdminRightsCheckBoxControl.Content = CoreTools.Translate("Ask only once for administrator privileges");
        ElevationHintText.Text = CoreTools.Translate("Caching administrator rights reduces the number of UAC prompts. Requires a process-level UAC token reset when changed.");

        ProhibitTitleText.Text = CoreTools.Translate("Prohibit all elevation");
        ProhibitDescriptionText.Text = CoreTools.Translate("Prevent UniGetUI from requesting elevated permissions through the Elevator or GSudo.");
        ProhibitElevationCheckBoxControl.Content = CoreTools.Translate("Prohibit any kind of Elevation via UniGetUI Elevator or GSudo");
        ProhibitHintText.Text = CoreTools.Translate("This option WILL cause issues. Any operation incapable of elevating itself WILL FAIL. Install/update/uninstall as administrator will NOT WORK.");

        OperationsSecureTitleText.Text = CoreTools.Translate("Restrictions on package operations");
        OperationsSecureDescriptionText.Text = CoreTools.Translate("The following settings unlock advanced package behaviors that are disabled by default because they can be abused. Each change requires a UAC prompt.");

        AllowCLIArgumentsCheckBoxControl.Content = CoreTools.Translate("Allow custom command-line arguments");
        AllowCLIArgumentsHintText.Text = CoreTools.Translate("Custom command-line arguments can change the way in which programs are installed, upgraded or uninstalled, in a way UniGetUI cannot control. Using custom command-lines can break packages. Proceed with caution.");

        AllowPrePostCommandsCheckBoxControl.Content = CoreTools.Translate("Allow custom pre-install and post-install commands");
        AllowPrePostCommandsHintText.Text = CoreTools.Translate("Pre and post install commands will be run before and after a package gets installed, upgraded or uninstalled. Be aware that they may break things unless used carefully.");

        ManagerPathsTitleText.Text = CoreTools.Translate("Restrictions on package managers");
        ManagerPathsDescriptionText.Text = CoreTools.Translate("Control whether the executable paths for package managers can be customized.");
        AllowCustomManagerPathsCheckBoxControl.Content = CoreTools.Translate("Allow changing the paths for package manager executables");
        AllowCustomManagerPathsHintText.Text = CoreTools.Translate("Turning this on enables changing the executable file used to interact with package managers. While this allows finer-grained customization of your install processes, it may also be dangerous.");

        ImportSecureTitleText.Text = CoreTools.Translate("Restrictions when importing package bundles");
        ImportSecureDescriptionText.Text = CoreTools.Translate("Limit what security-sensitive data can be imported from a package bundle.");
        AllowImportingCLIArgumentsCheckBoxControl.Content = CoreTools.Translate("Allow importing custom command-line arguments when importing packages from a bundle");
        AllowImportingCLIArgumentsHintText.Text = CoreTools.Translate("Malformed command-line arguments can break packages, or even allow a malicious actor to gain privileged execution. Therefore, importing custom command-line arguments is disabled by default.");
        AllowImportingPrePostCommandsCheckBoxControl.Content = CoreTools.Translate("Allow importing custom pre-install and post-install commands when importing packages from a bundle");
        AllowImportingPrePostCommandsHintText.Text = CoreTools.Translate("Pre and post install commands can do very nasty things to your device, if designed to do so. It can be very dangerous to import the commands from a bundle, unless you trust the source of that package bundle.");
    }

    private void LoadStoredValues()
    {
        DoCacheAdminRightsForBatchesCheckBoxControl.IsChecked = Settings.Get(Settings.K.DoCacheAdminRightsForBatches);
        DoCacheAdminRightsCheckBoxControl.IsChecked = Settings.Get(Settings.K.DoCacheAdminRights);
        ProhibitElevationCheckBoxControl.IsChecked = Settings.Get(Settings.K.ProhibitElevation);

        AllowCLIArgumentsCheckBoxControl.IsChecked = SecureSettings.Get(SecureSettings.K.AllowCLIArguments);
        AllowPrePostCommandsCheckBoxControl.IsChecked = SecureSettings.Get(SecureSettings.K.AllowPrePostOpCommand);
        AllowCustomManagerPathsCheckBoxControl.IsChecked = SecureSettings.Get(SecureSettings.K.AllowCustomManagerPaths);
        AllowImportingCLIArgumentsCheckBoxControl.IsChecked = SecureSettings.Get(SecureSettings.K.AllowImportingCLIArguments);
        AllowImportingPrePostCommandsCheckBoxControl.IsChecked = SecureSettings.Get(SecureSettings.K.AllowImportPrePostOpCommands);
    }

    private void ApplyControlState()
    {
        bool prohibitEnabled = ProhibitElevationCheckBoxControl.IsChecked == true;
        DoCacheAdminRightsForBatchesCheckBoxControl.IsEnabled = !prohibitEnabled;
        DoCacheAdminRightsCheckBoxControl.IsEnabled = !prohibitEnabled;

        AllowImportingCLIArgumentsCheckBoxControl.IsEnabled =
            AllowCLIArgumentsCheckBoxControl.IsChecked == true;
        AllowImportingPrePostCommandsCheckBoxControl.IsEnabled =
            AllowPrePostCommandsCheckBoxControl.IsChecked == true;
    }

    // ── Regular settings (no UAC needed) ──────────────────────────────────

    private void DoCacheAdminRightsForBatches_OnClick(object? sender, RoutedEventArgs e)
    {
        Settings.Set(Settings.K.DoCacheAdminRightsForBatches,
            DoCacheAdminRightsForBatchesCheckBoxControl.IsChecked == true);
        _ = CoreTools.ResetUACForCurrentProcess();
    }

    private void DoCacheAdminRights_OnClick(object? sender, RoutedEventArgs e)
    {
        Settings.Set(Settings.K.DoCacheAdminRights,
            DoCacheAdminRightsCheckBoxControl.IsChecked == true);
        _ = CoreTools.ResetUACForCurrentProcess();
    }

    private void ProhibitElevation_OnClick(object? sender, RoutedEventArgs e)
    {
        Settings.Set(Settings.K.ProhibitElevation,
            ProhibitElevationCheckBoxControl.IsChecked == true);
        ApplyControlState();
    }

    // ── Secure settings (UAC-elevated subprocess on Windows) ────────────

    private async void AllowCLIArguments_OnClick(object? sender, RoutedEventArgs e)
    {
        bool desired = AllowCLIArgumentsCheckBoxControl.IsChecked == true;
        await ApplySecureSetting(
            SecureSettings.K.AllowCLIArguments,
            desired,
            AllowCLIArgumentsCheckBoxControl,
            AllowCLIArgumentsPendingBadgeControl,
            AllowCLIArgumentsPendingTextControl
        );
        ApplyControlState();
    }

    private async void AllowPrePostCommands_OnClick(object? sender, RoutedEventArgs e)
    {
        bool desired = AllowPrePostCommandsCheckBoxControl.IsChecked == true;
        await ApplySecureSetting(
            SecureSettings.K.AllowPrePostOpCommand,
            desired,
            AllowPrePostCommandsCheckBoxControl,
            AllowPrePostCommandsPendingBadgeControl,
            AllowPrePostCommandsPendingTextControl
        );
        ApplyControlState();
    }

    private async void AllowCustomManagerPaths_OnClick(object? sender, RoutedEventArgs e)
    {
        bool desired = AllowCustomManagerPathsCheckBoxControl.IsChecked == true;
        await ApplySecureSetting(
            SecureSettings.K.AllowCustomManagerPaths,
            desired,
            AllowCustomManagerPathsCheckBoxControl,
            AllowCustomManagerPathsPendingBadgeControl,
            AllowCustomManagerPathsPendingTextControl
        );
    }

    private async void AllowImportingCLIArguments_OnClick(object? sender, RoutedEventArgs e)
    {
        bool desired = AllowImportingCLIArgumentsCheckBoxControl.IsChecked == true;
        await ApplySecureSetting(
            SecureSettings.K.AllowImportingCLIArguments,
            desired,
            AllowImportingCLIArgumentsCheckBoxControl,
            AllowImportingCLIArgumentsPendingBadgeControl,
            AllowImportingCLIArgumentsPendingTextControl
        );
    }

    private async void AllowImportingPrePostCommands_OnClick(object? sender, RoutedEventArgs e)
    {
        bool desired = AllowImportingPrePostCommandsCheckBoxControl.IsChecked == true;
        await ApplySecureSetting(
            SecureSettings.K.AllowImportPrePostOpCommands,
            desired,
            AllowImportingPrePostCommandsCheckBoxControl,
            AllowImportingPrePostCommandsPendingBadgeControl,
            AllowImportingPrePostCommandsPendingTextControl
        );
    }

    /// <summary>
    /// Applies a UAC-protected secure setting, shows a pending badge during the UAC prompt,
    /// and reverts the checkbox if the user cancels or the elevation fails.
    /// </summary>
    private static async Task ApplySecureSetting(
        SecureSettings.K key,
        bool desired,
        CheckBox checkbox,
        Border pendingBadge,
        TextBlock pendingText)
    {
        checkbox.IsEnabled = false;
        pendingText.Text = desired
            ? CoreTools.Translate("Waiting for administrator confirmation to enable this setting…")
            : CoreTools.Translate("Waiting for administrator confirmation to disable this setting…");
        pendingBadge.IsVisible = true;

        bool success = await SecureSettings.TrySet(key, desired);

        pendingBadge.IsVisible = false;
        checkbox.IsEnabled = true;

        if (!success)
        {
            // Revert UI to the actual persisted value (UAC was cancelled or failed)
            checkbox.IsChecked = SecureSettings.Get(key);
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private T GetControl<T>(string name)
        where T : Control
    {
        return this.FindControl<T>(name)
            ?? throw new InvalidOperationException($"Control '{name}' was not found.");
    }
}

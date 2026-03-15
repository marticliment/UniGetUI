using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using UniGetUI.Avalonia.Infrastructure;
using UniGetUI.Avalonia.Models;
using UniGetUI.Avalonia.Views;
using UniGetUI.Core.Data;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.SettingsEngine.SecureSettings;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.PackageLoader;
using UniGetUI.PackageEngine.Serializable;

namespace UniGetUI.Avalonia.Views.Pages.ManagersPages;

public partial class ManagerDetailView : UserControl, IManagerSectionView
{
    private bool _isLoading;
    private IPackageManager? _manager;
    private string _currentExecutablePath = string.Empty;

    // Header card
    private TextBlock LeadTitleText => GetControl<TextBlock>("LeadTitleBlock");
    private TextBlock LeadDescriptionText => GetControl<TextBlock>("LeadDescriptionBlock");

    // Status card
    private TextBlock StatusHeadingText => GetControl<TextBlock>("StatusHeadingBlock");
    private Border StatusBadge => GetControl<Border>("StatusBadgeBorder");
    private TextBlock StatusText => GetControl<TextBlock>("StatusTextBlock");
    private TextBlock VersionText => GetControl<TextBlock>("VersionBlock");
    private TextBlock ExecutablePathText => GetControl<TextBlock>("ExecutablePathBlock");
    private Button CopyExecutablePathButtonControl => GetControl<Button>("CopyExecutablePathButton");

    // Enable/disable card
    private TextBlock EnableSectionTitleText => GetControl<TextBlock>("EnableSectionTitleBlock");
    private TextBlock EnableSectionDescText => GetControl<TextBlock>("EnableSectionDescBlock");
    private CheckBox EnabledCheckBoxControl => GetControl<CheckBox>("EnabledCheckBox");

    // Executable card
    private TextBlock ExeSectionTitleText => GetControl<TextBlock>("ExeSectionTitleBlock");
    private TextBlock ExeSectionDescText => GetControl<TextBlock>("ExeSectionDescBlock");
    private ComboBox ExecutableComboBoxControl => GetControl<ComboBox>("ExecutableComboBox");
    private Border ExeWarningCard => GetControl<Border>("ExeWarningBorder");
    private TextBlock ExeWarningText => GetControl<TextBlock>("ExeWarningBlock");
    private Button OpenAdminSettingsButtonControl => GetControl<Button>("OpenAdminSettingsButton");

    // Extra-settings host
    private StackPanel ExtraSettingsPanel => GetControl<StackPanel>("ExtraSettingsHost");

    public ManagerDetailView()
    {
        InitializeComponent();
        SectionTitle = CoreTools.Translate("Package manager");
        SectionSubtitle = CoreTools.Translate("Manager details");
        SectionStatus = CoreTools.Translate("Manager details");
        OpenAdminSettingsButtonControl.Click += OpenAdminSettingsButton_OnClick;
        CopyExecutablePathButtonControl.Click += CopyExecutablePathButton_OnClick;
    }

    public string SectionTitle { get; private set; }
    public string SectionSubtitle { get; private set; }
    public string SectionStatus { get; private set; }

    public void LoadManager(IPackageManager manager)
    {
        _manager = manager;
        var description = manager.Properties.Description.Replace("<br>", Environment.NewLine);

        SectionTitle = CoreTools.Translate("{0} settings", manager.DisplayName);
        SectionSubtitle = CoreTools.Translate("Manage availability and runtime configuration.");
        SectionStatus = BuildStatus(manager);

        // Header
        LeadTitleText.Text = manager.DisplayName;
        LeadDescriptionText.Text = description;

        // Status card
        StatusHeadingText.Text = CoreTools.Translate("Runtime status");
        StatusText.Text = SectionStatus;
        VersionText.Text = string.IsNullOrWhiteSpace(manager.Status.Version)
            ? CoreTools.Translate("Version: unknown")
            : CoreTools.Translate("Version: {0}", manager.Status.Version);
        _currentExecutablePath = manager.Status.ExecutablePath ?? string.Empty;
        ExecutablePathText.Text = string.IsNullOrWhiteSpace(manager.Status.ExecutablePath)
            ? CoreTools.Translate("Executable path: not detected")
            : CoreTools.Translate("Executable path: {0}", manager.Status.ExecutablePath);
        CopyExecutablePathButtonControl.Content = CoreTools.Translate("Copy executable path");
        CopyExecutablePathButtonControl.IsEnabled = !string.IsNullOrWhiteSpace(_currentExecutablePath);

        ApplyStatusBadgeClasses(manager);

        // Enable/disable card
        EnableSectionTitleText.Text = CoreTools.Translate("Availability");
        EnableSectionDescText.Text = CoreTools.Translate("Enable or disable this package manager. Disabled managers will not load packages or check for updates.");
        _isLoading = true;
        EnabledCheckBoxControl.Content = CoreTools.Translate("Enable {0}", manager.DisplayName);
        EnabledCheckBoxControl.IsChecked = !Settings.GetDictionaryItem<string, bool>(Settings.K.DisabledManagers, manager.Name);
        _isLoading = false;

        // Executable path card
        ExeSectionTitleText.Text = CoreTools.Translate("Executable path");
        ExeSectionDescText.Text = CoreTools.Translate("Select the executable file used to run this manager. Changing this requires reloading the manager.");

        bool customPathsAllowed = SecureSettings.Get(SecureSettings.K.AllowCustomManagerPaths);
        ExeWarningCard.IsVisible = !customPathsAllowed;
        ExeWarningText.Text = CoreTools.Translate("Custom executable paths are disabled. Enable them in the Administrator settings to change this.");
        OpenAdminSettingsButtonControl.Content = CoreTools.Translate("Open administrator settings");
        ExecutableComboBoxControl.IsEnabled = customPathsAllowed;

        _isLoading = true;
        ExecutableComboBoxControl.SelectionChanged -= ExecutableComboBox_OnSelectionChanged;
        ExecutableComboBoxControl.Items.Clear();
        foreach (var path in manager.FindCandidateExecutableFiles())
        {
            ExecutableComboBoxControl.Items.Add(path);
        }
        string currentPath = Settings.GetDictionaryItem<string, string>(Settings.K.ManagerPaths, manager.Name) ?? "";
        if (string.IsNullOrEmpty(currentPath))
        {
            var exe = manager.GetExecutableFile();
            currentPath = exe.Item1 ? exe.Item2 : "";
        }
        ExecutableComboBoxControl.SelectedItem = currentPath;
        ExecutableComboBoxControl.SelectionChanged += ExecutableComboBox_OnSelectionChanged;
        _isLoading = false;

        // Extra manager-specific settings
        BuildExtraSettings(manager);
    }

    private void BuildExtraSettings(IPackageManager manager)
    {
        ExtraSettingsPanel.Children.Clear();

        // Disable notifications toggle (all managers)
        var disableNotifSection = MakeSettingsCard();
        var disableNotifTitle = MakeSectionTitle(CoreTools.Translate("Notifications"));
        var disableNotifDesc = MakeSectionDesc(CoreTools.Translate("Control whether updates from this manager trigger desktop notifications."));
        var disableNotifCheck = new CheckBox
        {
            Content = CoreTools.Translate("Ignore packages from {0} when showing update notifications", manager.DisplayName),
        };
        bool notifDisabled = Settings.GetDictionaryItem<string, bool>(Settings.K.DisabledPackageManagerNotifications, manager.Name);
        disableNotifCheck.IsChecked = notifDisabled;
        disableNotifCheck.Click += (_, _) =>
        {
            if (_isLoading) return;
            Settings.SetDictionaryItem(Settings.K.DisabledPackageManagerNotifications, manager.Name, disableNotifCheck.IsChecked == true);
        };
        var openLogsBtn = new Button
        {
            Content = CoreTools.Translate("View {0} logs", manager.DisplayName),
            Margin = new Thickness(0, 4, 0, 0),
        };
        openLogsBtn.Click += (_, _) =>
        {
            var shell = this.FindAncestorOfType<MainShellView>();
            shell?.OpenManagerLogs(manager);
        };
        ((StackPanel)disableNotifSection.Child!).Children.Add(disableNotifTitle);
        ((StackPanel)disableNotifSection.Child!).Children.Add(disableNotifDesc);
        ((StackPanel)disableNotifSection.Child!).Children.Add(disableNotifCheck);
        ((StackPanel)disableNotifSection.Child!).Children.Add(openLogsBtn);
        ExtraSettingsPanel.Children.Add(disableNotifSection);

        // WinGet-specific
        if (manager.Name == "WinGet")
        {
            var wingetSection = MakeSettingsCard();
            var wingetTitle = MakeSectionTitle(CoreTools.Translate("WinGet options"));

            var forceLocationCheck = new CheckBox
            {
                Content = CoreTools.Translate("Force install location parameter when updating packages with custom locations"),
                IsChecked = Settings.Get(Settings.K.WinGetForceLocationOnUpdate),
                Margin = new Thickness(0, 4, 0, 0),
            };
            forceLocationCheck.Click += (_, _) =>
            {
                if (_isLoading) return;
                Settings.Set(Settings.K.WinGetForceLocationOnUpdate, forceLocationCheck.IsChecked == true);
            };

            var useBundledCheck = new CheckBox
            {
                Content = CoreTools.Translate("Use bundled WinGet instead of system WinGet") + $" ({CoreTools.Translate("This may help if WinGet packages are not shown")})",
                IsChecked = Settings.Get(Settings.K.ForceLegacyBundledWinGet),
                Margin = new Thickness(0, 2, 0, 0),
            };
            useBundledCheck.Click += (_, _) =>
            {
                if (_isLoading) return;
                Settings.Set(Settings.K.ForceLegacyBundledWinGet, useBundledCheck.IsChecked == true);
                _ = ReloadManagerAsync();
            };

            var resetWinGetBtn = new Button
            {
                Content = CoreTools.Translate("Reset WinGet") + $" ({CoreTools.Translate("This may help if WinGet packages are not shown")})",
                Margin = new Thickness(0, 4, 0, 0),
            };
            resetWinGetBtn.Click += async (_, _) =>
            {
                resetWinGetBtn.IsEnabled = false;
                try
                {
                    using var p = new Process
                    {
                        StartInfo = new()
                        {
                            FileName = CoreData.PowerShell5,
                            Arguments =
                                "-ExecutionPolicy Bypass -NoLogo -NoProfile -Command \"& {"
                                + "cmd.exe /C \"rmdir /Q /S `\"%temp%\\WinGet`\"\"; "
                                + "cmd.exe /C \"`\"%localappdata%\\Microsoft\\WindowsApps\\winget.exe`\" source reset --force\"; "
                                + "taskkill /im winget.exe /f; "
                                + "taskkill /im WindowsPackageManagerServer.exe /f; "
                                + "Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force; "
                                + "Install-Module Microsoft.WinGet.Client -Force -AllowClobber; "
                                + "Import-Module Microsoft.WinGet.Client; "
                                + "Repair-WinGetPackageManager -Force -Latest; "
                                + "Get-AppxPackage -Name 'Microsoft.DesktopAppInstaller' | Reset-AppxPackage; "
                                + "}\"",
                            UseShellExecute = true,
                            Verb = "runas",
                        },
                    };
                    p.Start();
                    await p.WaitForExitAsync();
                    _ = UpgradablePackagesLoader.Instance.ReloadPackages();
                    _ = InstalledPackagesLoader.Instance.ReloadPackages();
                }
                finally { resetWinGetBtn.IsEnabled = true; }
            };

            ((StackPanel)wingetSection.Child!).Children.Add(wingetTitle);
            ((StackPanel)wingetSection.Child!).Children.Add(forceLocationCheck);
            ((StackPanel)wingetSection.Child!).Children.Add(useBundledCheck);
            ((StackPanel)wingetSection.Child!).Children.Add(resetWinGetBtn);
            ExtraSettingsPanel.Children.Add(wingetSection);
        }
        // Scoop-specific
        else if (manager.Name == "Scoop")
        {
            var scoopSection = MakeSettingsCard();
            var scoopTitle = MakeSectionTitle(CoreTools.Translate("Scoop options"));

            var installBtn = new Button
            {
                Content = CoreTools.Translate("Install Scoop"),
                Margin = new Thickness(0, 4, 0, 0),
            };
            installBtn.Click += (_, _) =>
                _ = CoreTools.LaunchBatchFile(
                    Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "install_scoop.cmd"),
                    CoreTools.Translate("Scoop Installer - UniGetUI"));

            var uninstallBtn = new Button
            {
                Content = CoreTools.Translate("Uninstall Scoop (and its packages)"),
                Margin = new Thickness(0, 2, 0, 0),
            };
            uninstallBtn.Click += (_, _) =>
                _ = CoreTools.LaunchBatchFile(
                    Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "uninstall_scoop.cmd"),
                    CoreTools.Translate("Scoop Uninstaller - UniGetUI"));

            var cleanupBtn = new Button
            {
                Content = CoreTools.Translate("Run cleanup and clear cache"),
                Margin = new Thickness(0, 2, 0, 0),
            };
            cleanupBtn.Click += (_, _) =>
                _ = CoreTools.LaunchBatchFile(
                    Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "scoop_cleanup.cmd"),
                    CoreTools.Translate("Clearing Scoop cache - UniGetUI"),
                    RunAsAdmin: true);

            var cleanupOnStartCheck = new CheckBox
            {
                Content = CoreTools.Translate("Enable Scoop cleanup on launch"),
                IsChecked = Settings.Get(Settings.K.EnableScoopCleanup),
                Margin = new Thickness(0, 4, 0, 0),
            };
            cleanupOnStartCheck.Click += (_, _) =>
            {
                if (_isLoading) return;
                Settings.Set(Settings.K.EnableScoopCleanup, cleanupOnStartCheck.IsChecked == true);
            };

            ((StackPanel)scoopSection.Child!).Children.Add(scoopTitle);
            ((StackPanel)scoopSection.Child!).Children.Add(installBtn);
            ((StackPanel)scoopSection.Child!).Children.Add(uninstallBtn);
            ((StackPanel)scoopSection.Child!).Children.Add(cleanupBtn);
            ((StackPanel)scoopSection.Child!).Children.Add(cleanupOnStartCheck);
            ExtraSettingsPanel.Children.Add(scoopSection);
        }
        // Chocolatey-specific
        else if (manager.Name == "Chocolatey")
        {
            var chocoSection = MakeSettingsCard();
            var chocoTitle = MakeSectionTitle(CoreTools.Translate("Chocolatey options"));

            var systemChocoCheck = new CheckBox
            {
                Content = CoreTools.Translate("Use system Chocolatey"),
                IsChecked = Settings.Get(Settings.K.UseSystemChocolatey),
                Margin = new Thickness(0, 4, 0, 0),
            };
            systemChocoCheck.Click += (_, _) =>
            {
                if (_isLoading) return;
                Settings.Set(Settings.K.UseSystemChocolatey, systemChocoCheck.IsChecked == true);
                _ = ReloadManagerAsync();
            };

            ((StackPanel)chocoSection.Child!).Children.Add(chocoTitle);
            ((StackPanel)chocoSection.Child!).Children.Add(systemChocoCheck);
            ExtraSettingsPanel.Children.Add(chocoSection);
        }
        // Vcpkg-specific
        else if (manager.Name == "Vcpkg")
        {
            var vcpkgSection = MakeSettingsCard();
            var vcpkgTitle = MakeSectionTitle(CoreTools.Translate("Vcpkg options"));

            // Default triplet
            var tripletLabel = MakeSectionDesc(CoreTools.Translate("Default vcpkg triplet"));
            var tripletCombo = new ComboBox
            {
                MinWidth = 220,
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left,
                Margin = new Thickness(0, 4, 0, 0),
            };

            string currentTriplet = Settings.GetValue(Settings.K.DefaultVcpkgTriplet);
            tripletCombo.Items.Add(CoreTools.Translate("(system default)"));

            // We can't directly call Vcpkg.GetSystemTriplets() since that project is not referenced.
            // Populate with common triplets instead.
            foreach (var t in new[] { "x64-windows", "x86-windows", "arm64-windows", "x64-osx", "x64-linux", "arm64-osx" })
            {
                tripletCombo.Items.Add(t);
            }

            tripletCombo.SelectedItem = string.IsNullOrEmpty(currentTriplet)
                ? CoreTools.Translate("(system default)")
                : currentTriplet;

            tripletCombo.SelectionChanged += (_, _) =>
            {
                if (_isLoading) return;
                var selected = tripletCombo.SelectedItem?.ToString() ?? "";
                if (selected == CoreTools.Translate("(system default)"))
                    Settings.SetValue(Settings.K.DefaultVcpkgTriplet, "");
                else
                    Settings.SetValue(Settings.K.DefaultVcpkgTriplet, selected);
            };

            // Custom vcpkg root
            var vcpkgRootTitle = MakeSectionDesc(CoreTools.Translate("Vcpkg root directory"));
            var vcpkgRootLabel = new TextBlock
            {
                Opacity = 0.82,
                Margin = new Thickness(0, 4, 0, 2),
                Text = Settings.Get(Settings.K.CustomVcpkgRoot)
                    ? Settings.GetValue(Settings.K.CustomVcpkgRoot)
                    : "%VCPKG_ROOT%",
            };
            var vcpkgRootRow = new StackPanel { Orientation = global::Avalonia.Layout.Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 2, 0, 0) };
            var selectRootBtn = new Button { Content = CoreTools.Translate("Select") };
            var resetRootBtn = new Button
            {
                Content = CoreTools.Translate("Reset"),
                IsEnabled = Settings.Get(Settings.K.CustomVcpkgRoot),
            };
            var openRootBtn = new Button
            {
                Content = CoreTools.Translate("Open"),
                IsEnabled = Settings.Get(Settings.K.CustomVcpkgRoot),
            };

            selectRootBtn.Click += async (_, _) =>
            {
                var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
                if (storageProvider is null) return;
                var result = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = CoreTools.Translate("Select vcpkg root directory"),
                    AllowMultiple = false,
                });
                if (result.Count > 0)
                {
                    string? path = result[0].TryGetLocalPath();
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        Settings.SetValue(Settings.K.CustomVcpkgRoot, path);
                        Settings.Set(Settings.K.CustomVcpkgRoot, true);
                        vcpkgRootLabel.Text = path;
                        resetRootBtn.IsEnabled = true;
                        openRootBtn.IsEnabled = true;
                    }
                }
            };
            resetRootBtn.Click += (_, _) =>
            {
                Settings.Set(Settings.K.CustomVcpkgRoot, false);
                vcpkgRootLabel.Text = "%VCPKG_ROOT%";
                resetRootBtn.IsEnabled = false;
                openRootBtn.IsEnabled = false;
            };
            openRootBtn.Click += (_, _) =>
            {
                string dir = Settings.GetValue(Settings.K.CustomVcpkgRoot).Replace("/", "\\");
                if (!string.IsNullOrEmpty(dir))
                    Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
            };

            vcpkgRootRow.Children.Add(selectRootBtn);
            vcpkgRootRow.Children.Add(resetRootBtn);
            vcpkgRootRow.Children.Add(openRootBtn);

            ((StackPanel)vcpkgSection.Child!).Children.Add(vcpkgTitle);
            ((StackPanel)vcpkgSection.Child!).Children.Add(tripletLabel);
            ((StackPanel)vcpkgSection.Child!).Children.Add(tripletCombo);
            ((StackPanel)vcpkgSection.Child!).Children.Add(vcpkgRootTitle);
            ((StackPanel)vcpkgSection.Child!).Children.Add(vcpkgRootLabel);
            ((StackPanel)vcpkgSection.Child!).Children.Add(vcpkgRootRow);
            ExtraSettingsPanel.Children.Add(vcpkgSection);
        }
        // Pip-specific warning
        else if (manager.Name == "Pip")
        {
            var pipSection = MakeSettingsCard();
            var pipTitle = MakeSectionTitle(CoreTools.Translate("Python / Pip note"));
            var pipNote = MakeSectionDesc(
                CoreTools.Translate("If Python cannot be found or is not listing packages but is installed on the system, you may need to disable the \"python.exe\" App Execution Alias in Windows Settings."));
            ((StackPanel)pipSection.Child!).Children.Add(pipTitle);
            ((StackPanel)pipSection.Child!).Children.Add(pipNote);
            ExtraSettingsPanel.Children.Add(pipSection);
        }

        // Source management (for managers that support custom sources, except Vcpkg)
        if (manager.Capabilities.SupportsCustomSources && manager.Name != "Vcpkg")
        {
            var sourcesSection = MakeSettingsCard();
            var sourcesTitle = MakeSectionTitle(CoreTools.Translate("Manage sources"));
            var sourcesDesc = MakeSectionDesc(CoreTools.Translate(
                "Add or remove package sources for {0}.", manager.DisplayName));

            var sourcesListPanel = new StackPanel { Spacing = 4, Margin = new Thickness(0, 6, 0, 2) };

            // Known-sources combo pre-fills name/URL fields when there are predefined sources
            var nameBox = new TextBox
            {
                Watermark = CoreTools.Translate("Source name"),
                MinWidth = 140,
            };
            var urlBox = new TextBox
            {
                Watermark = CoreTools.Translate("Source URL"),
                MinWidth = 220,
            };

            var addInputRow = new StackPanel
            {
                Orientation = global::Avalonia.Layout.Orientation.Horizontal,
                Spacing = 6,
                Margin = new Thickness(0, 8, 0, 0),
            };

            if (manager.Properties.KnownSources.Length > 0)
            {
                var knownCombo = new ComboBox
                {
                    PlaceholderText = CoreTools.Translate("Known sources..."),
                    MinWidth = 160,
                };
                var nameSourceRef = new Dictionary<string, IManagerSource>();
                knownCombo.Items.Add(CoreTools.Translate("(Custom)"));
                foreach (var ks in manager.Properties.KnownSources)
                {
                    knownCombo.Items.Add(ks.Name);
                    nameSourceRef[ks.Name] = ks;
                }
                knownCombo.SelectedIndex = 0;
                knownCombo.SelectionChanged += (_, _) =>
                {
                    var sel = knownCombo.SelectedItem?.ToString();
                    if (sel is null || sel == CoreTools.Translate("(Custom)"))
                    {
                        nameBox.Text = "";
                        urlBox.Text = "";
                        nameBox.IsEnabled = true;
                        urlBox.IsEnabled = true;
                    }
                    else if (nameSourceRef.TryGetValue(sel, out var src))
                    {
                        nameBox.Text = src.Name;
                        urlBox.Text = src.Url.ToString();
                        nameBox.IsEnabled = false;
                        urlBox.IsEnabled = false;
                    }
                };
                addInputRow.Children.Add(knownCombo);
            }

            var addBtn = new Button { Content = CoreTools.Translate("Add source") };
            addBtn.Click += (_, _) =>
            {
                var name = nameBox.Text?.Trim() ?? "";
                var url = urlBox.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url)) return;
                Uri? uri;
                try { uri = new Uri(url); }
                catch { return; }
                var newSource = new ManagerSource(manager, name, uri);
                var op = new AddSourceOperation(newSource);
                AvaloniaOperationRegistry.Add(op);
                op.OperationSucceeded += (_, _) => _ = LoadSourcesAsync(manager, sourcesListPanel);
                nameBox.Text = "";
                urlBox.Text = "";
            };

            addInputRow.Children.Add(nameBox);
            addInputRow.Children.Add(urlBox);
            addInputRow.Children.Add(addBtn);

            var reloadBtn = new Button
            {
                Content = CoreTools.Translate("Reload"),
                Margin = new Thickness(0, 4, 0, 0),
            };
            reloadBtn.Click += (_, _) => _ = LoadSourcesAsync(manager, sourcesListPanel);

            ((StackPanel)sourcesSection.Child!).Children.Add(sourcesTitle);
            ((StackPanel)sourcesSection.Child!).Children.Add(sourcesDesc);
            ((StackPanel)sourcesSection.Child!).Children.Add(sourcesListPanel);
            ((StackPanel)sourcesSection.Child!).Children.Add(addInputRow);
            ((StackPanel)sourcesSection.Child!).Children.Add(reloadBtn);
            ExtraSettingsPanel.Children.Add(sourcesSection);

            _ = LoadSourcesAsync(manager, sourcesListPanel);
        }

        // Default install options (all managers)
        _ = AddDefaultInstallOptionsSectionAsync(manager);
    }

    // ── Default install options ───────────────────────────────────────────

    private async Task AddDefaultInstallOptionsSectionAsync(IPackageManager manager)
    {
        var card = MakeSettingsCard();
        var panel = (StackPanel)card.Child!;

        panel.Children.Add(MakeSectionTitle(CoreTools.Translate("Default install options")));
        panel.Children.Add(MakeSectionDesc(CoreTools.Translate(
            "These options are applied by default to all {0} operations unless overridden per-package.", manager.DisplayName)));

        var loadingBlock = new TextBlock { Text = CoreTools.Translate("Loading..."), Opacity = 0.60 };
        panel.Children.Add(loadingBlock);
        ExtraSettingsPanel.Children.Add(card);

        InstallOptions options;
        try
        {
            options = await InstallOptionsFactory.LoadForManagerAsync(manager);
        }
        catch (Exception ex)
        {
            loadingBlock.Text = CoreTools.Translate("Failed to load: {0}", ex.Message);
            return;
        }

        panel.Children.Remove(loadingBlock);
        var caps = manager.Capabilities;

        var adminCheck = new CheckBox
        {
            Content = CoreTools.Translate("Run as administrator by default"),
            IsChecked = options.RunAsAdministrator,
        };
        panel.Children.Add(adminCheck);

        CheckBox? interactiveCheck = null;
        if (caps.CanRunInteractively)
        {
            interactiveCheck = new CheckBox
            {
                Content = CoreTools.Translate("Interactive installation by default"),
                IsChecked = options.InteractiveInstallation,
                Margin = new Thickness(0, 2, 0, 0),
            };
            panel.Children.Add(interactiveCheck);
        }

        CheckBox? skipHashCheck = null;
        if (caps.CanSkipIntegrityChecks)
        {
            skipHashCheck = new CheckBox
            {
                Content = CoreTools.Translate("Skip integrity checks by default"),
                IsChecked = options.SkipHashCheck,
                Margin = new Thickness(0, 2, 0, 0),
            };
            panel.Children.Add(skipHashCheck);
        }

        CheckBox? preReleaseCheck = null;
        if (caps.SupportsPreRelease)
        {
            preReleaseCheck = new CheckBox
            {
                Content = CoreTools.Translate("Use pre-release versions by default"),
                IsChecked = options.PreRelease,
                Margin = new Thickness(0, 2, 0, 0),
            };
            panel.Children.Add(preReleaseCheck);
        }

        ComboBox? archCombo = null;
        if (caps.SupportsCustomArchitectures)
        {
            panel.Children.Add(MakeSectionDesc(CoreTools.Translate("Default architecture:")));
            archCombo = new ComboBox
            {
                MinWidth = 160,
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left,
                Margin = new Thickness(0, 2, 0, 0),
            };
            archCombo.Items.Add(CoreTools.Translate("Default"));
            archCombo.SelectedIndex = 0;
            foreach (var arch in caps.SupportedCustomArchitectures)
            {
                archCombo.Items.Add(arch);
                if (options.Architecture == arch)
                    archCombo.SelectedItem = arch;
            }
            panel.Children.Add(archCombo);
        }

        ComboBox? scopeCombo = null;
        if (caps.SupportsCustomScopes)
        {
            panel.Children.Add(MakeSectionDesc(CoreTools.Translate("Default installation scope:")));
            scopeCombo = new ComboBox
            {
                MinWidth = 160,
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left,
                Margin = new Thickness(0, 2, 0, 0),
            };
            scopeCombo.Items.Add(CoreTools.Translate("Default"));
            scopeCombo.SelectedIndex = 0;
            scopeCombo.Items.Add(CoreTools.Translate("User / current user"));
            scopeCombo.Items.Add(CoreTools.Translate("Machine / all users"));
            if (options.InstallationScope == PackageScope.User)
                scopeCombo.SelectedItem = CoreTools.Translate("User / current user");
            else if (options.InstallationScope == PackageScope.Machine)
                scopeCombo.SelectedItem = CoreTools.Translate("Machine / all users");
            panel.Children.Add(scopeCombo);
        }

        TextBox? locationBox = null;
        if (caps.SupportsCustomLocations)
        {
            panel.Children.Add(MakeSectionDesc(CoreTools.Translate("Default install location:")));
            var locationRow = new StackPanel
            {
                Orientation = global::Avalonia.Layout.Orientation.Horizontal,
                Spacing = 6,
                Margin = new Thickness(0, 2, 0, 0),
            };
            locationBox = new TextBox
            {
                Watermark = CoreTools.Translate("Leave empty to use the default location"),
                MinWidth = 220,
                Text = options.CustomInstallLocation,
            };
            var browseBtn = new Button { Content = CoreTools.Translate("Browse\u2026") };
            browseBtn.Click += async (_, _) =>
            {
                var sp = TopLevel.GetTopLevel(this)?.StorageProvider;
                if (sp is null) return;
                var result = await sp.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = CoreTools.Translate("Select install location"),
                    AllowMultiple = false,
                });
                if (result.Count > 0)
                {
                    var path = result[0].TryGetLocalPath();
                    if (!string.IsNullOrWhiteSpace(path)) locationBox.Text = path;
                }
            };
            var resetLocationBtn = new Button { Content = CoreTools.Translate("Reset") };
            resetLocationBtn.Click += (_, _) => locationBox.Text = string.Empty;
            locationRow.Children.Add(locationBox);
            locationRow.Children.Add(browseBtn);
            locationRow.Children.Add(resetLocationBtn);
            panel.Children.Add(locationRow);
        }

        // Save / Reset buttons
        var buttonRow = new StackPanel
        {
            Orientation = global::Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 10, 0, 0),
        };

        var saveBtn = new Button { Content = CoreTools.Translate("Save defaults") };
        saveBtn.Click += async (_, _) =>
        {
            saveBtn.IsEnabled = false;
            try
            {
                var newOpts = new InstallOptions();
                newOpts.RunAsAdministrator = adminCheck.IsChecked ?? false;
                if (interactiveCheck is not null) newOpts.InteractiveInstallation = interactiveCheck.IsChecked ?? false;
                if (skipHashCheck is not null) newOpts.SkipHashCheck = skipHashCheck.IsChecked ?? false;
                if (preReleaseCheck is not null) newOpts.PreRelease = preReleaseCheck.IsChecked ?? false;
                if (archCombo is not null)
                {
                    string sel = archCombo.SelectedItem?.ToString() ?? "";
                    newOpts.Architecture = Architecture.ValidValues.Contains(sel) ? sel : string.Empty;
                }
                if (scopeCombo is not null)
                {
                    string sel = scopeCombo.SelectedItem?.ToString() ?? "";
                    if (sel == CoreTools.Translate("User / current user")) newOpts.InstallationScope = PackageScope.User;
                    else if (sel == CoreTools.Translate("Machine / all users")) newOpts.InstallationScope = PackageScope.Machine;
                }
                if (locationBox is not null) newOpts.CustomInstallLocation = locationBox.Text?.Trim() ?? string.Empty;
                await InstallOptionsFactory.SaveForManagerAsync(newOpts, manager);
            }
            finally { saveBtn.IsEnabled = true; }
        };

        var resetBtn = new Button { Content = CoreTools.Translate("Reset to defaults") };
        resetBtn.Click += async (_, _) =>
        {
            resetBtn.IsEnabled = false;
            try
            {
                await InstallOptionsFactory.SaveForManagerAsync(new InstallOptions(), manager);
                adminCheck.IsChecked = false;
                interactiveCheck?.IsChecked = false;
                skipHashCheck?.IsChecked = false;
                preReleaseCheck?.IsChecked = false;
                archCombo?.SelectedIndex = 0;
                scopeCombo?.SelectedIndex = 0;
                locationBox?.Text = string.Empty;
            }
            finally { resetBtn.IsEnabled = true; }
        };

        buttonRow.Children.Add(saveBtn);
        buttonRow.Children.Add(resetBtn);
        panel.Children.Add(buttonRow);
    }

    // ── Event handlers ────────────────────────────────────────────────────

    private async void EnabledCheckBox_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_manager is null || _isLoading) return;
        _isLoading = true;
        EnabledCheckBoxControl.IsEnabled = false;
        try
        {
            bool disabled = EnabledCheckBoxControl.IsChecked != true;
            Settings.SetDictionaryItem(Settings.K.DisabledManagers, _manager.Name, disabled);
            await Task.Run(_manager.Initialize);
        }
        finally
        {
            EnabledCheckBoxControl.IsEnabled = true;
            _isLoading = false;
            ApplyStatusBadgeClasses(_manager);
            StatusText.Text = BuildStatus(_manager);
        }
    }

    private void ExecutableComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_manager is null || _isLoading) return;
        string? selected = ExecutableComboBoxControl.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(selected)) return;
        Settings.SetDictionaryItem(Settings.K.ManagerPaths, _manager.Name, selected);
        _ = ReloadManagerAsync();
    }

    private async Task ReloadManagerAsync()
    {
        if (_manager is null) return;
        _isLoading = true;
        EnabledCheckBoxControl.IsEnabled = false;
        ExecutableComboBoxControl.IsEnabled = false;
        try
        {
            await Task.Run(_manager.Initialize);
        }
        finally
        {
            _isLoading = false;
            EnabledCheckBoxControl.IsEnabled = true;
            ExecutableComboBoxControl.IsEnabled = SecureSettings.Get(SecureSettings.K.AllowCustomManagerPaths);
            ApplyStatusBadgeClasses(_manager);
            StatusText.Text = BuildStatus(_manager);
            VersionText.Text = string.IsNullOrWhiteSpace(_manager.Status.Version)
                ? CoreTools.Translate("Version: unknown")
                : CoreTools.Translate("Version: {0}", _manager.Status.Version);
            _currentExecutablePath = _manager.Status.ExecutablePath ?? string.Empty;
            ExecutablePathText.Text = string.IsNullOrWhiteSpace(_manager.Status.ExecutablePath)
                ? CoreTools.Translate("Executable path: not detected")
                : CoreTools.Translate("Executable path: {0}", _manager.Status.ExecutablePath);
            CopyExecutablePathButtonControl.IsEnabled = !string.IsNullOrWhiteSpace(_currentExecutablePath);
        }
    }

    private void OpenAdminSettingsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var shell = this.FindAncestorOfType<MainShellView>();
        shell?.OpenSettingsSection(SettingsPages.SettingsSectionRoute.Administrator);
    }

    private async void CopyExecutablePathButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentExecutablePath))
            return;

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
            return;

        await clipboard.SetTextAsync(_currentExecutablePath);
        CopyExecutablePathButtonControl.Content = CoreTools.Translate("Copied");
        await Task.Delay(1000);
        CopyExecutablePathButtonControl.Content = CoreTools.Translate("Copy executable path");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void ApplyStatusBadgeClasses(IPackageManager manager)
    {
        StatusBadge.Classes.Set("ready", false);
        StatusBadge.Classes.Set("disabled", false);
        StatusBadge.Classes.Set("missing", false);

        if (!manager.IsEnabled())
            StatusBadge.Classes.Set("disabled", true);
        else if (manager.Status.Found)
            StatusBadge.Classes.Set("ready", true);
        else
            StatusBadge.Classes.Set("missing", true);
    }

    private static string BuildStatus(IPackageManager manager)
    {
        if (!manager.IsEnabled())
            return CoreTools.Translate("Disabled");
        return manager.Status.Found
            ? CoreTools.Translate("Ready")
            : CoreTools.Translate("Not found");
    }

    private static async Task LoadSourcesAsync(IPackageManager manager, StackPanel panel)
    {
        panel.Children.Clear();
        if (!manager.IsReady())
        {
            panel.Children.Add(new TextBlock
            {
                Text = CoreTools.Translate("The manager is not ready. Sources cannot be loaded."),
                Opacity = 0.60,
            });
            return;
        }

        panel.Children.Add(new TextBlock { Text = CoreTools.Translate("Loading..."), Opacity = 0.60 });

        IReadOnlyList<IManagerSource> sources;
        try
        {
            sources = await Task.Run(manager.SourcesHelper.GetSources);
        }
        catch (Exception ex)
        {
            panel.Children.Clear();
            panel.Children.Add(new TextBlock
            {
                Text = CoreTools.Translate("An error occurred while loading sources: {0}", ex.Message),
                Opacity = 0.60,
                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
            });
            return;
        }

        panel.Children.Clear();

        if (sources.Count == 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = CoreTools.Translate("No sources found."),
                Opacity = 0.60,
            });
            return;
        }

        foreach (var source in sources)
        {
            var row = new StackPanel
            {
                Orientation = global::Avalonia.Layout.Orientation.Horizontal,
                Spacing = 10,
                Margin = new Thickness(0, 2, 0, 2),
            };
            var nameText = new TextBlock
            {
                Text = source.Name,
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
                MinWidth = 130,
            };
            var urlText = new TextBlock
            {
                Text = source.Url.ToString(),
                Opacity = 0.70,
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
                MinWidth = 160,
            };
            var removeBtn = new Button { Content = CoreTools.Translate("Remove") };
            var capturedSource = source;
            removeBtn.Click += (_, _) =>
            {
                var op = new RemoveSourceOperation(capturedSource);
                AvaloniaOperationRegistry.Add(op);
                op.OperationSucceeded += (_, _) => _ = LoadSourcesAsync(manager, panel);
            };
            row.Children.Add(nameText);
            row.Children.Add(urlText);
            row.Children.Add(removeBtn);
            panel.Children.Add(row);
        }
    }

    private static Border MakeSettingsCard()
    {
        return new Border
        {
            Padding = new Thickness(18),
            Classes = { "surface-card" },
            Child = new StackPanel { Spacing = 8 },
        };
    }

    private static TextBlock MakeSectionTitle(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
        };
    }

    private static TextBlock MakeSectionDesc(string text)
    {
        return new TextBlock
        {
            Text = text,
            Opacity = 0.78,
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
        };
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

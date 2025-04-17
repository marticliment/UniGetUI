using Windows.Networking.Connectivity;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.System.Power;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Telemetry;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Classes.Packages.Classes;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.Pages.DialogPages;

namespace UniGetUI.Interface.SoftwarePages
{
    public partial class SoftwareUpdatesPage : AbstractPackagesPage
    {
        private BetterMenuItem? MenuAsAdmin;
        private BetterMenuItem? MenuInteractive;
        private BetterMenuItem? MenuskipHash;
        private BetterMenuItem? MenuDownloadInstaller;
        private BetterMenuItem? MenuOpenInstallLocation;

        public SoftwareUpdatesPage()
        : base(new PackagesPageData
        {
            DisableAutomaticPackageLoadOnStart = false,
            DisableFilterOnQueryChange = false,
            MegaQueryBlockEnabled = false,
            ShowLastLoadTime = true,
            PackagesAreCheckedByDefault = true,
            DisableSuggestedResultsRadio = true,
            PageName = "Updates",

            Loader = PEInterface.UpgradablePackagesLoader,
            PageRole = OperationType.Update,

            NoPackages_BackgroundText = CoreTools.Translate("Hooray! No updates were found."),
            NoPackages_SourcesText = CoreTools.Translate("Everything is up to date"),
            NoPackages_SubtitleText_Base = CoreTools.Translate("Everything is up to date"),
            MainSubtitle_StillLoading = CoreTools.Translate("Loading packages"),
            NoMatches_BackgroundText = CoreTools.Translate("No results were found matching the input criteria"),

            PageTitle = CoreTools.Translate("Software Updates"),
            Glyph = "\uE895"
        })
        {
        }

        public override BetterMenu GenerateContextMenu()
        {
            BetterMenu ContextMenu = new();

            BetterMenuItem menuInstall = new()
            {
                Text = CoreTools.AutoTranslated("Update"),
                IconName = IconType.Update,
                KeyboardAcceleratorTextOverride = "Ctrl+Enter"
            };
            menuInstall.Click += MenuInstall_Invoked;

            BetterMenuItem menuInstallSettings = new()
            {
                Text = CoreTools.AutoTranslated("Installation options"),
                IconName = IconType.Options,
                KeyboardAcceleratorTextOverride = "Alt+Enter"
            };
            menuInstallSettings.Click += (_, _) => ShowInstallationOptionsForPackage(SelectedItem);

            MenuOpenInstallLocation = new()
            {
                Text = CoreTools.AutoTranslated("Open install location"),
                IconName = IconType.Launch,
            };
            MenuOpenInstallLocation.Click += (_, _) => OpenPackageInstallLocation(SelectedItem);

            MenuAsAdmin = new BetterMenuItem
            {
                Text = CoreTools.AutoTranslated("Update as administrator"),
                IconName = IconType.UAC,
            };
            MenuAsAdmin.Click += MenuAsAdmin_Invoked;

            MenuInteractive = new BetterMenuItem
            {
                Text = CoreTools.AutoTranslated("Interactive update"),
                IconName = IconType.Interactive,
            };
            MenuInteractive.Click += MenuInteractive_Invoked;

            MenuskipHash = new BetterMenuItem
            {
                Text = CoreTools.AutoTranslated("Skip hash check"),
                IconName = IconType.Checksum,
            };
            MenuskipHash.Click += MenuSkipHash_Invoked;

            MenuDownloadInstaller = new BetterMenuItem
            {
                Text = CoreTools.AutoTranslated("Download installer"),
                IconName = IconType.Download
            };
            MenuDownloadInstaller.Click += (_, _) => _ = MainApp.Operations.AskLocationAndDownload(SelectedItem, TEL_InstallReferral.ALREADY_INSTALLED);

            BetterMenuItem menuUpdateAfterUninstall = new()
            {
                Text = CoreTools.AutoTranslated("Uninstall package, then update it"),
                IconName = IconType.Undelete,
            };
            menuUpdateAfterUninstall.Click += MenuUpdateAfterUninstall_Invoked;

            BetterMenuItem menuUninstall = new()
            {
                Text = CoreTools.AutoTranslated("Uninstall package"),
                IconName = IconType.Delete,
            };
            menuUninstall.Click += MenuUninstall_Invoked;

            BetterMenuItem menuIgnorePackage = new()
            {
                Text = CoreTools.AutoTranslated("Ignore updates for this package"),
                IconName = IconType.Pin,
            };
            menuIgnorePackage.Click += MenuIgnorePackage_Invoked;

            BetterMenuItem menuSkipVersion = new()
            {
                Text = CoreTools.AutoTranslated("Skip this version"),
                IconName = IconType.Skip,
            };
            menuSkipVersion.Click += MenuSkipVersion_Invoked;

            BetterMenuItem menuShare = new()
            {
                Text = CoreTools.AutoTranslated("Share this package"),
                IconName = IconType.Share,
            };
            menuShare.Click += (_, _) => SharePackage(SelectedItem);

            BetterMenuItem menuDetails = new()
            {
                Text = CoreTools.AutoTranslated("Package details"),
                IconName = IconType.Info_Round,
                KeyboardAcceleratorTextOverride = "Enter"
            };
            menuDetails.Click += (_, _) => ShowDetailsForPackage(SelectedItem, TEL_InstallReferral.ALREADY_INSTALLED);

            MenuFlyoutSubItem menuPause = new()
            {
                Text = "Pause updates for",
                Icon = new FontIcon { Glyph = "\uE769" },
            };
            foreach (IgnoredUpdatesDatabase.PauseTime menuTime in new List<IgnoredUpdatesDatabase.PauseTime>{
                new() { Days = 1 }, new() { Days = 3 },
                new() { Weeks = 1 }, new() { Weeks = 2 }, new() { Weeks = 4 },
                new() { Months = 3 }, new() { Months = 6 }, new() { Months = 12 },
            })
            {
                BetterMenuItem menuItem = new()
                {
                    Text = menuTime.StringRepresentation(),
                };
                menuItem.Click += (_, _) => {
                    if (SelectedItem != null)
                    {
                        SelectedItem.AddToIgnoredUpdatesAsync("<" + menuTime.GetDateFromNow());
                        PEInterface.UpgradablePackagesLoader.IgnoredPackages[SelectedItem.Id] = SelectedItem;
                        Loader.Remove(SelectedItem);
                    }
                };
                menuPause.Items.Add(menuItem);
            }

            ContextMenu.Items.Add(menuInstall);
            ContextMenu.Items.Add(new MenuFlyoutSeparator());
            ContextMenu.Items.Add(menuInstallSettings);
            ContextMenu.Items.Add(MenuOpenInstallLocation);
            ContextMenu.Items.Add(new MenuFlyoutSeparator());
            ContextMenu.Items.Add(MenuAsAdmin);
            ContextMenu.Items.Add(MenuInteractive);
            ContextMenu.Items.Add(MenuskipHash);
            ContextMenu.Items.Add(MenuDownloadInstaller);
            ContextMenu.Items.Add(new MenuFlyoutSeparator());
            ContextMenu.Items.Add(menuUpdateAfterUninstall);
            ContextMenu.Items.Add(menuUninstall);
            ContextMenu.Items.Add(new MenuFlyoutSeparator());
            ContextMenu.Items.Add(menuIgnorePackage);
            ContextMenu.Items.Add(menuSkipVersion);
            ContextMenu.Items.Add(menuPause);
            ContextMenu.Items.Add(new MenuFlyoutSeparator());
            ContextMenu.Items.Add(menuShare);
            ContextMenu.Items.Add(menuDetails);

            return ContextMenu;
        }

        protected override void WhenShowingContextMenu(IPackage package)
        {
            if (MenuAsAdmin is null
                || MenuInteractive is null
                || MenuskipHash is null
                || MenuDownloadInstaller is null
                || MenuOpenInstallLocation is null)
            {
                Logger.Error("Menu items are null on SoftwareUpdatesTab");
                return;
            }

            MenuAsAdmin.IsEnabled = package.Manager.Capabilities.CanRunAsAdmin;
            MenuInteractive.IsEnabled = package.Manager.Capabilities.CanRunInteractively;
            MenuskipHash.IsEnabled = package.Manager.Capabilities.CanSkipIntegrityChecks;
            MenuDownloadInstaller.IsEnabled = package.Manager.Capabilities.CanDownloadInstaller;

            MenuOpenInstallLocation.IsEnabled = package.Manager.DetailsHelper.GetInstallLocation(package) is not null;
        }

        public override void GenerateToolBar()
        {
            AppBarButton UpdateSelected = new();
            AppBarButton UpdateAsAdmin = new();
            AppBarButton UpdateSkipHash = new();
            AppBarButton UpdateInteractive = new();

            AppBarButton InstallationSettings = new();

            AppBarButton PackageDetails = new();
            AppBarButton SharePackage = new();

            AppBarButton IgnoreSelected = new();
            AppBarButton ManageIgnored = new();

            AppBarButton HelpButton = new();

            ToolBar.PrimaryCommands.Add(UpdateSelected);
            ToolBar.PrimaryCommands.Add(UpdateAsAdmin);
            ToolBar.PrimaryCommands.Add(UpdateSkipHash);
            ToolBar.PrimaryCommands.Add(UpdateInteractive);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(InstallationSettings);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(PackageDetails);
            ToolBar.PrimaryCommands.Add(SharePackage);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(IgnoreSelected);
            ToolBar.PrimaryCommands.Add(ManageIgnored);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(HelpButton);

            Dictionary<AppBarButton, string> Labels = new()
            { // Entries with a leading space are collapsed
              // Their texts will be used as the tooltip
                { UpdateSelected,       CoreTools.Translate("Update selected packages") },
                { UpdateAsAdmin,        " " + CoreTools.Translate("Update as administrator") },
                { UpdateSkipHash,       " " + CoreTools.Translate("Skip integrity checks") },
                { UpdateInteractive,    " " + CoreTools.Translate("Interactive update") },
                { InstallationSettings, " " + CoreTools.Translate("Installation options") },
                { PackageDetails,       " " + CoreTools.Translate("Package details") },
                { SharePackage,         " " + CoreTools.Translate("Share") },
                { IgnoreSelected,       CoreTools.Translate("Ignore selected packages") },
                { ManageIgnored,        CoreTools.Translate("Manage ignored updates") },
                { HelpButton,           CoreTools.Translate("Help") }
            };

            foreach (AppBarButton toolButton in Labels.Keys)
            {
                toolButton.IsCompact = Labels[toolButton][0] == ' ';
                if (toolButton.IsCompact)
                {
                    toolButton.LabelPosition = CommandBarLabelPosition.Collapsed;
                }

                string text = Labels[toolButton].Trim();
                toolButton.Label = text;
                ToolTipService.SetToolTip(toolButton, text);
            }

            Dictionary<AppBarButton, IconType> Icons = new()
            {
                { UpdateSelected,       IconType.Update },
                { UpdateAsAdmin,        IconType.UAC },
                { UpdateSkipHash,       IconType.Checksum },
                { UpdateInteractive,    IconType.Interactive },
                { InstallationSettings, IconType.Options },
                { PackageDetails,       IconType.Info_Round },
                { SharePackage,         IconType.Share },
                { IgnoreSelected,       IconType.Pin },
                { ManageIgnored,        IconType.ClipboardList },
                { HelpButton,           IconType.Help }
            };

            foreach (AppBarButton toolButton in Icons.Keys)
            {
                toolButton.Icon = new LocalIcon(Icons[toolButton]);
            }

            PackageDetails.Click += (_, _) => ShowDetailsForPackage(SelectedItem, TEL_InstallReferral.ALREADY_INSTALLED);
            HelpButton.Click += (_, _) => MainApp.Instance.MainWindow.NavigationPage.ShowHelp();
            InstallationSettings.Click += (_, _) => ShowInstallationOptionsForPackage(SelectedItem);
            ManageIgnored.Click += async (_, _) => await DialogHelper.ManageIgnoredUpdates();
            IgnoreSelected.Click += async (_, _) =>
            {
                foreach (IPackage package in FilteredPackages.GetCheckedPackages())
                {
                    await package.AddToIgnoredUpdatesAsync();
                    PEInterface.UpgradablePackagesLoader.Remove(package);
                    PEInterface.UpgradablePackagesLoader.IgnoredPackages[package.Id] = package;
                }
            };

            UpdateSelected.Click += (_, _) => MainApp.Operations.Update(FilteredPackages.GetCheckedPackages());
            UpdateAsAdmin.Click += (_, _) => MainApp.Operations.Update(FilteredPackages.GetCheckedPackages(), elevated: true);
            UpdateSkipHash.Click += (_, _) => MainApp.Operations.Update(FilteredPackages.GetCheckedPackages(), no_integrity: true);
            UpdateInteractive.Click += (_, _) => MainApp.Operations.Update(FilteredPackages.GetCheckedPackages(), interactive: true);
            SharePackage.Click += (_, _) => MainApp.Instance.MainWindow.SharePackage(SelectedItem);
        }

        protected override void WhenPackageCountUpdated()
        {
            MainApp.Tooltip.AvailableUpdates = Loader.Count();
        }

        protected override void WhenPackagesLoaded(ReloadReason reason)
        {
            List<IPackage> upgradablePackages = [];
            foreach (IPackage package in Loader.Packages)
            {
                if (package.Tag is not PackageTag.OnQueue and not PackageTag.BeingProcessed)
                    upgradablePackages.Add(package);
            }

            try
            {
                if (upgradablePackages.Count == 0)
                    return;

                bool EnableAutoUpdate = Settings.Get("AutomaticallyUpdatePackages");

                if (EnableAutoUpdate)
                {
                    var connectionCost = NetworkInformation.GetInternetConnectionProfile()?.GetConnectionCost().NetworkCostType;
                    if (connectionCost is NetworkCostType.Fixed or NetworkCostType.Variable && Settings.Get("DisableAUPOnMeteredConnections"))
                    {
                        Logger.Warn("Updates will not be installed automatically because the current internet connection is metered.");
                        EnableAutoUpdate = false;
                    }

                    if (PowerManager.EnergySaverStatus is EnergySaverStatus.On && Settings.Get("DisableAUPOnBatterySaver"))
                    {
                        Logger.Warn("Updates will not be installed automatically because battery saver is enabled.");
                        EnableAutoUpdate = false;
                    }
                }

                if (Environment.GetCommandLineArgs().Contains("--updateapps"))
                {
                    Logger.Warn("Automatic install of updates has been enabled via Command Line (user settings have been overriden)");
                    EnableAutoUpdate = true;
                }


                if(EnableAutoUpdate)
                {
                    MainApp.Operations.UpdateAll();
                }

                if (Settings.AreUpdatesNotificationsDisabled())
                    return;

                _ = AppNotificationManager.Default.RemoveByTagAsync(CoreData.UpdatesAvailableNotificationTag.ToString());

                AppNotification notification;
                if (upgradablePackages.Count == 1)
                {
                    if (EnableAutoUpdate)
                    {
                        AppNotificationBuilder builder = new AppNotificationBuilder()
                            .SetScenario(AppNotificationScenario.Default)
                            .SetTag(CoreData.UpdatesAvailableNotificationTag.ToString())

                            .AddText(CoreTools.Translate("An update was found!"))
                            .AddText(CoreTools.Translate("{0} is being updated to version {1}",
                                upgradablePackages[0].Name, upgradablePackages[0].NewVersionString))
                            .SetAttributionText(CoreTools.Translate("You have currently version {0} installed",
                                upgradablePackages[0].VersionString))

                            .AddArgument("action", NotificationArguments.ShowOnUpdatesTab);
                        notification = builder.BuildNotification();
                    }
                    else
                    {
                        AppNotificationBuilder builder = new AppNotificationBuilder()
                            .SetScenario(AppNotificationScenario.Default)
                            .SetTag(CoreData.UpdatesAvailableNotificationTag.ToString())

                            .AddText(CoreTools.Translate("An update was found!"))
                            .AddText(CoreTools.Translate("{0} can be updated to version {1}",
                                upgradablePackages[0].Name, upgradablePackages[0].NewVersionString))
                            .SetAttributionText(CoreTools.Translate("You have currently version {0} installed",
                                upgradablePackages[0].VersionString))

                            .AddArgument("action", NotificationArguments.ShowOnUpdatesTab)
                            .AddButton(new AppNotificationButton(CoreTools.Translate("View on UniGetUI").Replace("'", "´"))
                                .AddArgument("action", NotificationArguments.ShowOnUpdatesTab)
                            )
                            .AddButton(new AppNotificationButton(CoreTools.Translate("Update"))
                                .AddArgument("action", NotificationArguments.UpdateAllPackages)
                            );
                        notification = builder.BuildNotification();
                    }
                }
                else
                {
                    string attribution = "";
                    foreach (IPackage package in upgradablePackages)
                    {
                        if (!Settings.GetDictionaryItem<string, bool>("DisabledPackageManagerNotifications", package.Manager.Name))
                            attribution += package.Name + ", ";
                    }

                    attribution = attribution.TrimEnd(' ').TrimEnd(',');

                    if (EnableAutoUpdate)
                    {

                        AppNotificationBuilder builder = new AppNotificationBuilder()
                            .SetScenario(AppNotificationScenario.Default)
                            .SetTag(CoreData.UpdatesAvailableNotificationTag.ToString())

                            .AddText(
                                CoreTools.Translate("{0} packages are being updated", upgradablePackages.Count))
                            .SetAttributionText(attribution)
                            .AddText(CoreTools.Translate("Updates found!"))

                            .AddArgument("action", NotificationArguments.ShowOnUpdatesTab);
                        notification = builder.BuildNotification();
                    }
                    else
                    {
                        AppNotificationBuilder builder = new AppNotificationBuilder()
                            .SetScenario(AppNotificationScenario.Default)
                            .SetTag(CoreData.UpdatesAvailableNotificationTag.ToString())

                            .AddText(CoreTools.Translate("Updates found!"))
                            .AddText(CoreTools.Translate("{0} packages can be updated", upgradablePackages.Count))
                            .SetAttributionText(attribution)

                            .AddButton(new AppNotificationButton(CoreTools.Translate("Open UniGetUI").Replace("'", "´"))
                                .AddArgument("action", NotificationArguments.ShowOnUpdatesTab)
                            )
                            .AddButton(new AppNotificationButton(CoreTools.Translate("Update all"))
                                .AddArgument("action", NotificationArguments.UpdateAllPackages)
                            )
                            .AddArgument("action", NotificationArguments.ShowOnUpdatesTab);
                        notification = builder.BuildNotification();
                    }
                }

                bool SendNotification = false;
                foreach (var Package in upgradablePackages)
                {
                    if (!Settings.GetDictionaryItem<string, bool>("DisabledPackageManagerNotifications", Package.Manager.Name))
                    {
                        SendNotification = true;
                        break;
                    }
                }
                if (SendNotification)
                {
                    notification.ExpiresOnReboot = true;
                    AppNotificationManager.Default.Show(notification);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void MenuInstall_Invoked(object sender, RoutedEventArgs e)
            => _ = MainApp.Operations.Update(SelectedItem);

        private  void MenuSkipHash_Invoked(object sender, RoutedEventArgs e)
            => _ = MainApp.Operations.Update(SelectedItem, no_integrity: true);

        private void MenuInteractive_Invoked(object sender, RoutedEventArgs e)
            => _ = MainApp.Operations.Update(SelectedItem, interactive: true);

        private void MenuAsAdmin_Invoked(object sender, RoutedEventArgs e)
            => _ = MainApp.Operations.Update(SelectedItem, elevated: true);

        private async void MenuUpdateAfterUninstall_Invoked(object sender, RoutedEventArgs e)
        {
            var op = await MainApp.Operations.Uninstall(SelectedItem);
            _ = MainApp.Operations.Install(SelectedItem, TEL_InstallReferral.ALREADY_INSTALLED, req: op);
        }

        private void MenuUninstall_Invoked(object sender, RoutedEventArgs e)
            => _ = MainApp.Operations.Uninstall(SelectedItem);

        private void MenuIgnorePackage_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package is null)
            {
                return;
            }

            _ = package.AddToIgnoredUpdatesAsync();
            PEInterface.UpgradablePackagesLoader.Remove(package);
            PEInterface.UpgradablePackagesLoader.IgnoredPackages[package.Id] = package;
        }

        private void MenuSkipVersion_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package is null)
            {
                return;
            }

            _ = package.AddToIgnoredUpdatesAsync(package.NewVersionString);
            PEInterface.UpgradablePackagesLoader.Remove(package);
            PEInterface.UpgradablePackagesLoader.IgnoredPackages[package.Id] = package;
        }


        public void UpdateAll()
        {
        }

        public void UpdatePackageForId(string id)
        {
        }

        public async void UpdateAllPackagesForManager(string manager)
        {
        }
    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Telemetry;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Managers.WingetManager;
using UniGetUI.Pages.DialogPages;

namespace UniGetUI.Interface.SoftwarePages
{
    public partial class InstalledPackagesPage : AbstractPackagesPage
    {
        private static bool HasDoneBackup;

        private BetterMenuItem? MenuAsAdmin;
        private BetterMenuItem? MenuInteractive;
        private BetterMenuItem? MenuRemoveData;
        private BetterMenuItem? MenuInstallationOptions;
        private BetterMenuItem? MenuReinstallPackage;
        private BetterMenuItem? MenuUninstallThenReinstall;
        private BetterMenuItem? MenuIgnoreUpdates;
        private BetterMenuItem? MenuSharePackage;
        private BetterMenuItem? MenuPackageDetails;
        private BetterMenuItem? MenuOpenInstallLocation;
        private BetterMenuItem? MenuDownloadInstaller;

        public InstalledPackagesPage()
        : base(new PackagesPageData
        {
            DisableAutomaticPackageLoadOnStart = false,
            DisableFilterOnQueryChange = false,
            MegaQueryBlockEnabled = false,
            ShowLastLoadTime = false,
            PackagesAreCheckedByDefault = false,
            DisableSuggestedResultsRadio = true,
            PageName = "Installed",

            Loader = PEInterface.InstalledPackagesLoader,
            PageRole = OperationType.Uninstall,

            NoPackages_BackgroundText = CoreTools.Translate("No results were found matching the input criteria"),
            NoPackages_SourcesText = CoreTools.Translate("No packages were found"),
            NoPackages_SubtitleText_Base = CoreTools.Translate("No packages were found"),
            MainSubtitle_StillLoading = CoreTools.Translate("Loading packages"),
            NoMatches_BackgroundText = CoreTools.Translate("No results were found matching the input criteria"),

            PageTitle = CoreTools.Translate("Installed Packages"),
            Glyph = "\uE977"
        })
        {
        }

        public override BetterMenu GenerateContextMenu()
        {
            BetterMenu menu = new();
            BetterMenuItem menuUninstall = new()
            {
                Text = CoreTools.AutoTranslated("Uninstall"),
                IconName = IconType.Delete,
                KeyboardAcceleratorTextOverride = "Ctrl+Enter"
            };
            menuUninstall.Click += MenuUninstall_Invoked;
            menu.Items.Add(menuUninstall);

            menu.Items.Add(new MenuFlyoutSeparator { Height = 5 });

            MenuInstallationOptions = new()
            {
                Text = CoreTools.AutoTranslated("Installation options"),
                IconName = IconType.Options,
                KeyboardAcceleratorTextOverride = "Alt+Enter"
            };
            MenuInstallationOptions.Click += MenuInstallSettings_Invoked;
            menu.Items.Add(MenuInstallationOptions);

            MenuOpenInstallLocation = new()
            {
                Text = CoreTools.AutoTranslated("Open install location"),
                IconName = IconType.Launch,
            };
            MenuOpenInstallLocation.Click += (_, _) => OpenPackageInstallLocation(SelectedItem);;
            menu.Items.Add(MenuOpenInstallLocation);

            menu.Items.Add(new MenuFlyoutSeparator());

            MenuAsAdmin = new BetterMenuItem
            {
                Text = CoreTools.AutoTranslated("Uninstall as administrator"),
                IconName = IconType.UAC
            };
            MenuAsAdmin.Click += MenuAsAdmin_Invoked;
            menu.Items.Add(MenuAsAdmin);

            MenuInteractive = new BetterMenuItem
            {
                Text = CoreTools.AutoTranslated("Interactive uninstall"),
                IconName = IconType.Interactive
            };
            MenuInteractive.Click += MenuInteractive_Invoked;
            menu.Items.Add(MenuInteractive);

            MenuRemoveData = new BetterMenuItem
            {
                Text = CoreTools.AutoTranslated("Uninstall and remove data"),
                IconName = IconType.Close_Round
            };
            MenuRemoveData.Click += MenuRemoveData_Invoked;
            menu.Items.Add(MenuRemoveData);

            menu.Items.Add(new MenuFlyoutSeparator());

            MenuDownloadInstaller = new BetterMenuItem
            {
                Text = CoreTools.AutoTranslated("Download installer"),
                IconName = IconType.Download
            };
            MenuDownloadInstaller.Click += (_, _) => _ = MainApp.Operations.AskLocationAndDownload(SelectedItem, TEL_InstallReferral.ALREADY_INSTALLED);
            menu.Items.Add(MenuDownloadInstaller);

            menu.Items.Add(new MenuFlyoutSeparator());

            MenuReinstallPackage = new()
            {
                Text = CoreTools.AutoTranslated("Reinstall package"),
                IconName = IconType.Download
            };
            MenuReinstallPackage.Click += MenuReinstall_Invoked;
            menu.Items.Add(MenuReinstallPackage);

            MenuUninstallThenReinstall = new()
            {
                Text = CoreTools.AutoTranslated("Uninstall package, then reinstall it"),
                IconName = IconType.Undelete
            };
            MenuUninstallThenReinstall.Click += MenuUninstallThenReinstall_Invoked;
            menu.Items.Add(MenuUninstallThenReinstall);
            menu.Items.Add(new MenuFlyoutSeparator());

            MenuIgnoreUpdates = new()
            {
                Text = CoreTools.AutoTranslated("Ignore updates for this package"),
                IconName = IconType.Pin
            };
            MenuIgnoreUpdates.Click += MenuIgnorePackage_Invoked;
            menu.Items.Add(MenuIgnoreUpdates);

            menu.Items.Add(new MenuFlyoutSeparator());

            MenuSharePackage = new()
            {
                Text = CoreTools.AutoTranslated("Share this package"),
                IconName = IconType.Share
            };
            MenuSharePackage.Click += MenuShare_Invoked;
            menu.Items.Add(MenuSharePackage);

            MenuPackageDetails = new()
            {
                Text = CoreTools.AutoTranslated("Package details"),
                IconName = IconType.Info_Round,
                KeyboardAcceleratorTextOverride = "Enter"
            };
            MenuPackageDetails.Click += MenuDetails_Invoked;
            menu.Items.Add(MenuPackageDetails);

            return menu;
        }

        public override void GenerateToolBar()
        {
            AppBarButton UninstallSelected = new();
            AppBarButton UninstallAsAdmin = new();
            AppBarButton UninstallInteractive = new();
            AppBarButton InstallationSettings = new();

            AppBarButton PackageDetails = new();
            AppBarButton SharePackage = new();

            AppBarButton IgnoreSelected = new();
            AppBarButton ManageIgnored = new();
            AppBarButton ExportSelection = new();

            AppBarButton HelpButton = new();

            ToolBar.PrimaryCommands.Add(UninstallSelected);
            ToolBar.PrimaryCommands.Add(UninstallAsAdmin);
            ToolBar.PrimaryCommands.Add(UninstallInteractive);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(InstallationSettings);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(PackageDetails);
            ToolBar.PrimaryCommands.Add(SharePackage);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(IgnoreSelected);
            ToolBar.PrimaryCommands.Add(ManageIgnored);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(ExportSelection);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(HelpButton);

            Dictionary<AppBarButton, string> Labels = new()
            { // Entries with a trailing space are collapsed
              // Their texts will be used as the tooltip
                { UninstallSelected,    CoreTools.Translate("Uninstall selected packages") },
                { UninstallAsAdmin,     " " + CoreTools.Translate("Uninstall as administrator") },
                { UninstallInteractive, " " + CoreTools.Translate("Interactive uninstall") },
                { InstallationSettings, " " + CoreTools.Translate("Installation options") },
                { PackageDetails,       " " + CoreTools.Translate("Package details") },
                { SharePackage,         " " + CoreTools.Translate("Share") },
                { IgnoreSelected,       CoreTools.Translate("Ignore selected packages") },
                { ManageIgnored,        CoreTools.Translate("Manage ignored updates") },
                { ExportSelection,      CoreTools.Translate("Add selection to bundle") },
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
                { UninstallSelected,      IconType.Delete },
                { UninstallAsAdmin,       IconType.UAC },
                { UninstallInteractive,   IconType.Interactive },
                { InstallationSettings,   IconType.Options },
                { PackageDetails,         IconType.Info_Round },
                { SharePackage,           IconType.Share },
                { IgnoreSelected,         IconType.Pin },
                { ManageIgnored,          IconType.ClipboardList },
                { ExportSelection,        IconType.AddTo },
                { HelpButton,             IconType.Help }
            };

            foreach (AppBarButton toolButton in Icons.Keys)
            {
                toolButton.Icon = new LocalIcon(Icons[toolButton]);
            }

            PackageDetails.Click += (_, _) => ShowDetailsForPackage(SelectedItem, TEL_InstallReferral.ALREADY_INSTALLED);

            ExportSelection.Click += ExportSelection_Click;
            HelpButton.Click += (_, _) => MainApp.Instance.MainWindow.NavigationPage.ShowHelp();
            InstallationSettings.Click += (_, _) => ShowInstallationOptionsForPackage(SelectedItem);
            ManageIgnored.Click += async (_, _) => await DialogHelper.ManageIgnoredUpdates();
            IgnoreSelected.Click += async (_, _) =>
            {
                foreach (IPackage package in FilteredPackages.GetCheckedPackages())
                {
                    if (!package.Source.IsVirtualManager)
                    {
                        PEInterface.UpgradablePackagesLoader.Remove(package);
                        await package.AddToIgnoredUpdatesAsync();
                    }
                }
            };

            UninstallSelected.Click += (_, _) => MainApp.Operations.ConfirmAndUninstall(FilteredPackages.GetCheckedPackages());
            UninstallAsAdmin.Click += (_, _) => MainApp.Operations.ConfirmAndUninstall(FilteredPackages.GetCheckedPackages(), elevated: true);
            UninstallInteractive.Click += (_, _) => MainApp.Operations.ConfirmAndUninstall(FilteredPackages.GetCheckedPackages(), interactive: true);
            SharePackage.Click += (_, _) => MainApp.Instance.MainWindow.SharePackage(SelectedItem);
        }

        protected override void WhenPackageCountUpdated()
        {
            return;
        }

        protected override void WhenPackagesLoaded(ReloadReason reason)
        {
            if (!HasDoneBackup)
            {
                if (Settings.Get("EnablePackageBackup"))
                {
                    _ = BackupPackages();
                }
            }

            if (WinGet.NO_PACKAGES_HAVE_BEEN_LOADED && !Settings.Get("DisableWinGetMalfunctionDetector"))
            {
                var infoBar = MainApp.Instance.MainWindow.WinGetWarningBanner;
                infoBar.IsOpen = true;
                infoBar.Title = CoreTools.Translate("WinGet malfunction detected");
                infoBar.Message = CoreTools.Translate("It looks like WinGet is not working properly. Do you want to attempt to repair WinGet?");
                var button = new Button { Content = CoreTools.Translate("Repair WinGet") };
                infoBar.ActionButton = button;
                button.Click += (_, _) => DialogHelper.HandleBrokenWinGet();
            }
        }

        protected override async void WhenShowingContextMenu(IPackage package)
        {
            if (MenuAsAdmin is null
                || MenuInteractive is null
                || MenuRemoveData is null
                || MenuInstallationOptions is null
                || MenuUninstallThenReinstall is null
                || MenuReinstallPackage is null
                || MenuIgnoreUpdates is null
                || MenuSharePackage is null
                || MenuPackageDetails is null
                || MenuOpenInstallLocation is null
                || MenuDownloadInstaller is null)
            {
                Logger.Error("Menu items are null on InstalledPackagesTab");
                return;
            }

            MenuAsAdmin.IsEnabled = package.Manager.Capabilities.CanRunAsAdmin;
            MenuInteractive.IsEnabled = package.Manager.Capabilities.CanRunInteractively;
            MenuRemoveData.IsEnabled = package.Manager.Capabilities.CanRemoveDataOnUninstall;

            bool IS_LOCAL = package.Source.IsVirtualManager;

            MenuInstallationOptions.IsEnabled = !IS_LOCAL;
            MenuReinstallPackage.IsEnabled = !IS_LOCAL;
            MenuUninstallThenReinstall.IsEnabled = !IS_LOCAL;
            MenuIgnoreUpdates.IsEnabled = false; // Will be set on the lines below;
            MenuSharePackage.IsEnabled = !IS_LOCAL;
            MenuPackageDetails.IsEnabled = !IS_LOCAL;
            MenuDownloadInstaller.IsEnabled = !IS_LOCAL && package.Manager.Capabilities.CanDownloadInstaller;;

            MenuOpenInstallLocation.IsEnabled = package.Manager.DetailsHelper.GetInstallLocation(package) is not null;
            if (!IS_LOCAL)
            {
                if (await package.HasUpdatesIgnoredAsync())
                {
                    MenuIgnoreUpdates.Text = CoreTools.Translate("Do not ignore updates for this package anymore");
                    MenuIgnoreUpdates.Icon = new FontIcon { Glyph = "\uE77A" };
                }
                else
                {
                    MenuIgnoreUpdates.Text = CoreTools.Translate("Ignore updates for this package");
                    MenuIgnoreUpdates.Icon = new FontIcon { Glyph = "\uE718" };
                }
                MenuIgnoreUpdates.IsEnabled = true;
            }
        }

        private async void ExportSelection_Click(object sender, RoutedEventArgs e)
        {
            MainApp.Instance.MainWindow.NavigationPage.NavigateTo(PageType.Bundles);
            DialogHelper.ShowLoadingDialog(CoreTools.Translate("Please wait..."));
            await PEInterface.PackageBundlesLoader.AddPackagesAsync(FilteredPackages.GetCheckedPackages());
            DialogHelper.HideLoadingDialog();

        }

        public static async Task BackupPackages()
        {

            try
            {
                Logger.Debug("Starting package backup");
                List<IPackage> packagesToExport = [];
                foreach (IPackage package in PEInterface.InstalledPackagesLoader.Packages)
                {
                    packagesToExport.Add(package);
                }

                string BackupContents = await PackageBundlesPage.CreateBundle(packagesToExport.ToArray(), BundleFormatType.UBUNDLE);

                string dirName = Settings.GetValue("ChangeBackupOutputDirectory");
                if (dirName == "")
                {
                    dirName = CoreData.UniGetUI_DefaultBackupDirectory;
                }

                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }

                string fileName = Settings.GetValue("ChangeBackupFileName");
                if (fileName == "")
                {
                    fileName = CoreTools.Translate("{pcName} installed packages", new Dictionary<string, object?> { { "pcName", Environment.MachineName } });
                }

                if (Settings.Get("EnableBackupTimestamping"))
                {
                    fileName += " " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
                }

                fileName += ".ubundle";

                string filePath = Path.Combine(dirName, fileName);
                await File.WriteAllTextAsync(filePath, BackupContents);
                HasDoneBackup = true;
                Logger.ImportantInfo("Backup saved to " + filePath);
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred while performing a backup");
                Logger.Error(ex);
            }
        }

        private void MenuUninstall_Invoked(object sender, RoutedEventArgs args)
            => MainApp.Operations.ConfirmAndUninstall(SelectedItem);

        private void MenuAsAdmin_Invoked(object sender, RoutedEventArgs args)
            => MainApp.Operations.ConfirmAndUninstall(SelectedItem, elevated: true);

        private void MenuInteractive_Invoked(object sender, RoutedEventArgs args)
            => MainApp.Operations.ConfirmAndUninstall(SelectedItem, interactive: true);

        private void MenuRemoveData_Invoked(object sender, RoutedEventArgs args)
            => MainApp.Operations.ConfirmAndUninstall(SelectedItem, remove_data: true);

        private void MenuReinstall_Invoked(object sender, RoutedEventArgs args)
            => _ = MainApp.Operations.Install(SelectedItem, TEL_InstallReferral.ALREADY_INSTALLED);

        private async void MenuUninstallThenReinstall_Invoked(object sender, RoutedEventArgs args)
        {
            var op = await MainApp.Operations.Uninstall(SelectedItem, ignoreParallel: true);
            _ = MainApp.Operations.Install(SelectedItem, TEL_InstallReferral.ALREADY_INSTALLED, ignoreParallel: true, req: op);
        }

        private async void MenuIgnorePackage_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package is null) return;

            if (await package.HasUpdatesIgnoredAsync())
            {
                await package.RemoveFromIgnoredUpdatesAsync();
            }
            else
            {
                await package.AddToIgnoredUpdatesAsync();
                PEInterface.UpgradablePackagesLoader.Remove(package);
            }
        }

        private void MenuShare_Invoked(object sender, RoutedEventArgs args)
        {
            if (SelectedItem is null)
                return;

            MainApp.Instance.MainWindow.SharePackage(SelectedItem);
        }

        private void MenuDetails_Invoked(object sender, RoutedEventArgs args)
        {
            ShowDetailsForPackage(SelectedItem, TEL_InstallReferral.ALREADY_INSTALLED);
        }

        private void MenuInstallSettings_Invoked(object sender, RoutedEventArgs e)
        {
            ShowInstallationOptionsForPackage(SelectedItem);
        }
    }
}

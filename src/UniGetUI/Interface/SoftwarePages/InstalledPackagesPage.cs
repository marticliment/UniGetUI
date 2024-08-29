using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.Interface.SoftwarePages
{
    public class InstalledPackagesPage : AbstractPackagesPage
    {
        private bool HasDoneBackup;

        BetterMenuItem? MenuAsAdmin;
        BetterMenuItem? MenuInteractive;
        BetterMenuItem? MenuRemoveData;
        private BetterMenuItem? MenuInstallationOptions;
        private BetterMenuItem? MenuReinstallPackage;
        private BetterMenuItem? MenuUninstallThenReinstall;
        private BetterMenuItem? MenuIgnoreUpdates;
        private BetterMenuItem? MenuSharePackage;
        private BetterMenuItem? MenuPackageDetails;
        private BetterMenuItem? MenuOpenInstallLocation;

        public InstalledPackagesPage()
        : base(new PackagesPageData
        {
            DisableAutomaticPackageLoadOnStart = false,
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
                Text = "Uninstall",
                IconName = IconType.Delete,
                KeyboardAcceleratorTextOverride = "Ctrl+Enter"
            };
            menuUninstall.Click += MenuUninstall_Invoked;
            menu.Items.Add(menuUninstall);

            menu.Items.Add(new MenuFlyoutSeparator { Height = 5 });

            MenuInstallationOptions = new()
            {
                Text = "Installation options",
                IconName = IconType.Options,
                KeyboardAcceleratorTextOverride = "Alt+Enter"
            };
            MenuInstallationOptions.Click += MenuInstallSettings_Invoked;
            menu.Items.Add(MenuInstallationOptions);

            MenuOpenInstallLocation = new()
            {
                Text = "Open install location",
                IconName = IconType.Launch,
            };
            MenuOpenInstallLocation.Click += (s, e) => OpenPackageInstallLocation(SelectedItem);;
            menu.Items.Add(MenuOpenInstallLocation);

            menu.Items.Add(new MenuFlyoutSeparator());

            MenuAsAdmin = new BetterMenuItem
            {
                Text = "Uninstall as administrator",
                IconName = IconType.UAC
            };
            MenuAsAdmin.Click += MenuAsAdmin_Invoked;
            menu.Items.Add(MenuAsAdmin);

            MenuInteractive = new BetterMenuItem
            {
                Text = "Interactive uninstall",
                IconName = IconType.Interactive
            };
            MenuInteractive.Click += MenuInteractive_Invoked;
            menu.Items.Add(MenuInteractive);

            MenuRemoveData = new BetterMenuItem
            {
                Text = "Uninstall and remove data",
                IconName = IconType.Close_Round
            };
            MenuRemoveData.Click += MenuRemoveData_Invoked;
            menu.Items.Add(MenuRemoveData);

            menu.Items.Add(new MenuFlyoutSeparator());

            MenuReinstallPackage = new()
            {
                Text = "Reinstall package",
                IconName = IconType.Download
            };
            MenuReinstallPackage.Click += MenuReinstall_Invoked;
            menu.Items.Add(MenuReinstallPackage);

            MenuUninstallThenReinstall = new()
            {
                Text = "Uninstall package, then reinstall it",
                IconName = IconType.Undelete
            };
            MenuUninstallThenReinstall.Click += MenuUninstallThenReinstall_Invoked;
            menu.Items.Add(MenuUninstallThenReinstall);

            menu.Items.Add(new MenuFlyoutSeparator());

            MenuIgnoreUpdates = new()
            {
                Text = "Ignore updates for this package",
                IconName = IconType.Pin
            };
            MenuIgnoreUpdates.Click += MenuIgnorePackage_Invoked;
            menu.Items.Add(MenuIgnoreUpdates);

            menu.Items.Add(new MenuFlyoutSeparator());

            MenuSharePackage = new()
            {
                Text = "Share this package",
                IconName = IconType.Share
            };
            MenuSharePackage.Click += MenuShare_Invoked;
            menu.Items.Add(MenuSharePackage);

            MenuPackageDetails = new()
            {
                Text = "Package details",
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

                toolButton.Label = Labels[toolButton].Trim();
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

            PackageDetails.Click += (s, e) => ShowDetailsForPackage(SelectedItem);

            ExportSelection.Click += ExportSelection_Click;
            HelpButton.Click += (s, e) => MainApp.Instance.MainWindow.NavigationPage.ShowHelp();
            InstallationSettings.Click += (s, e) => ShowInstallationOptionsForPackage(SelectedItem);
            ManageIgnored.Click += async (s, e) => await MainApp.Instance.MainWindow.NavigationPage.ManageIgnoredUpdatesDialog();
            IgnoreSelected.Click += async (s, e) =>
            {
                foreach (IPackage package in FilteredPackages.GetCheckedPackages())
                {
                    PEInterface.UpgradablePackagesLoader.Remove(package);
                    await package.AddToIgnoredUpdatesAsync();
                }
            };

            UninstallSelected.Click += (s, e) => ConfirmAndUninstall(FilteredPackages.GetCheckedPackages());
            UninstallAsAdmin.Click += (s, e) => ConfirmAndUninstall(FilteredPackages.GetCheckedPackages(), elevated: true);
            UninstallInteractive.Click += (s, e) => ConfirmAndUninstall(FilteredPackages.GetCheckedPackages(), interactive: true);
            SharePackage.Click += (s, e) => MainApp.Instance.MainWindow.SharePackage(SelectedItem);
        }

        protected override void WhenPackageCountUpdated()
        {
            return;
        }

#pragma warning disable
        protected override void WhenPackagesLoaded(ReloadReason reason)
        {
            if (!HasDoneBackup)
            {
                if (Settings.Get("EnablePackageBackup"))
                {
                    _ = BackupPackages();
                }
            }
        }
#pragma warning restore

        protected override void WhenShowingContextMenu(IPackage package)
        {
            if (MenuAsAdmin == null
                || MenuInteractive == null
                || MenuRemoveData == null
                || MenuInstallationOptions is null
                || MenuUninstallThenReinstall is null
                || MenuReinstallPackage is null
                || MenuIgnoreUpdates is null
                || MenuSharePackage is null
                || MenuPackageDetails is null
                || MenuOpenInstallLocation is null)
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
            MenuIgnoreUpdates.IsEnabled = !IS_LOCAL;
            MenuSharePackage.IsEnabled = !IS_LOCAL;
            MenuPackageDetails.IsEnabled = !IS_LOCAL;

            MenuOpenInstallLocation.IsEnabled = package.Manager.GetPackageInstallLocation(package) is not null;
        }

        private async void ExportSelection_Click(object sender, RoutedEventArgs e)
        {
            MainApp.Instance.MainWindow.NavigationPage.BundlesNavButton.ForceClick();
            MainApp.Instance.MainWindow.ShowLoadingDialog(CoreTools.Translate("Please wait..."));
            await PEInterface.PackageBundlesLoader.AddPackagesAsync(FilteredPackages.GetCheckedPackages());
            MainApp.Instance.MainWindow.HideLoadingDialog();

        }

        public async void ConfirmAndUninstall(IPackage package, IInstallationOptions options)
        {
            if (await MainApp.Instance.MainWindow.NavigationPage.ConfirmUninstallation(package))
            {
                MainApp.Instance.AddOperationToList(new UninstallPackageOperation(package, options));
            }
        }

        public async void ConfirmAndUninstall(IEnumerable<IPackage> packages, bool? elevated = null, bool? interactive = null, bool? remove_data = null)
        {
            if (await MainApp.Instance.MainWindow.NavigationPage.ConfirmUninstallation(packages))
            {
                foreach (IPackage package in packages)
                {
                    MainApp.Instance.AddOperationToList(new UninstallPackageOperation(package,
                        await InstallationOptions.FromPackageAsync(package, elevated, interactive, remove_data: remove_data)));
                }
            }
        }

        public async Task BackupPackages()
        {

            try
            {
                Logger.Debug("Starting package backup");
                List<IPackage> packagestoExport = [];
                foreach (Package package in Loader.Packages)
                {
                    packagestoExport.Add(package);
                }

                string BackupContents = await PackageBundlesPage.CreateBundle(packagestoExport.ToArray(), BundleFormatType.JSON);

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

                fileName += ".json";

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

        private async void MenuUninstall_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null)
            {
                return;
            }

            ConfirmAndUninstall(package, await InstallationOptions.FromPackageAsync(package));
        }

        private async void MenuAsAdmin_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null)
            {
                return;
            }

            ConfirmAndUninstall(package, await InstallationOptions.FromPackageAsync(package, elevated: true));
        }

        private async void MenuInteractive_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null)
            {
                return;
            }

            ConfirmAndUninstall(package, await InstallationOptions.FromPackageAsync(package, interactive: true));
        }

        private async void MenuRemoveData_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null)
            {
                return;
            }

            ConfirmAndUninstall(package, await InstallationOptions.FromPackageAsync(package, remove_data: true));
        }

        private void MenuReinstall_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null)
            {
                return;
            }

            MainApp.Instance.AddOperationToList(new InstallPackageOperation(package));
        }

        private void MenuUninstallThenReinstall_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null)
            {
                return;
            }

            MainApp.Instance.AddOperationToList(new UninstallPackageOperation(package, IgnoreParallelInstalls: true));
            MainApp.Instance.AddOperationToList(new InstallPackageOperation(package, IgnoreParallelInstalls: true));

        }
        private void MenuIgnorePackage_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null)
            {
                return;
            }

            _ = package.AddToIgnoredUpdatesAsync();
            PEInterface.UpgradablePackagesLoader.Remove(package);
        }

        private void MenuShare_Invoked(object sender, RoutedEventArgs args)
        {
            if (PackageList.SelectedItem == null)
            {
                return;
            }

            MainApp.Instance.MainWindow.SharePackage(SelectedItem);
        }

        private void MenuDetails_Invoked(object sender, RoutedEventArgs args)
        {
            ShowDetailsForPackage(SelectedItem);
        }

        private async void MenuInstallSettings_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package != null &&
                await MainApp.Instance.MainWindow.NavigationPage.ShowInstallationSettingsAndContinue(package, OperationType.Uninstall))
            {
                ConfirmAndUninstall(package, await InstallationOptions.FromPackageAsync(package));
            }
        }
    }
}

using CommunityToolkit.WinUI.Notifications;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.Interface.SoftwarePages
{
    public class NewSoftwareUpdatesPage : AbstractPackagesPage
    {
        private BetterMenuItem MenuAsAdmin;
        private BetterMenuItem MenuInteractive;
        private BetterMenuItem MenuskipHash;

        public override void GenerateUIText()
        {
            PageRole = OperationType.Update;
            NoPackages_BackgroundText = CoreTools.Translate("Hooray! No updates were found.");
            NoPackages_SourcesText = CoreTools.Translate("Everything is up to date");

            NoMatches_BackgroundText = CoreTools.Translate("No results were found matching the input criteria.");
            NoMatches_SourcesText = CoreTools.Translate("No packages were found");

            MainTitleText = CoreTools.AutoTranslated("Software Updates");
            MainTitleGlyph = "\uE895";

            QuerySimilarResultsRadio.IsEnabled = false;
            QueryOptionsGroup.SelectedIndex = 1;
            QueryOptionsGroup.SelectedIndex = 2;
            QueryOptionsGroup.SelectedItem = QueryBothRadio;
        }

        public override BetterMenu GenerateContextMenu()
        {
            BetterMenu ContextMenu = new BetterMenu();

            var menuInstall = new BetterMenuItem()
            {
                Text = "Update",
                IconName="menu_updates",
                KeyboardAcceleratorTextOverride = "Ctrl+Enter"
            };
            menuInstall.Click += MenuInstall_Invoked;

            var menuInstallSettings = new BetterMenuItem()
            {
                Text = "Installation options",
                IconName="options",
                KeyboardAcceleratorTextOverride = "Alt+Enter"
            };
            menuInstallSettings.Click += (s, e) => { ShowInstallationOptionsForPackage(PackageList.SelectedItem as Package); };
            
            MenuAsAdmin = new BetterMenuItem()
            {
                Text = "Update as administrator",
                IconName="runasadmin",
            };
            MenuAsAdmin.Click += MenuAsAdmin_Invoked;

            MenuInteractive = new BetterMenuItem()
            {
                Text = "Interactive update",
                IconName="interactive",
            };
            MenuInteractive.Click += MenuInteractive_Invoked;

            MenuskipHash = new BetterMenuItem()
            {
                Text = "Skip hash check",
                IconName="checksum",
            };
            MenuskipHash.Click += MenuSkipHash_Invoked;

            var menuUpdateAfterUninstall = new BetterMenuItem()
            {
                Text = "Uninstall package, then update it",
                IconName="undelete",
            };
            menuUpdateAfterUninstall.Click += MenuUpdateAfterUninstall_Invoked;

            var menuUninstall = new BetterMenuItem()
            {
                Text = "Uninstall package",
                IconName="trash",
            };
            menuUninstall.Click += MenuUninstall_Invoked;


            var menuIgnorePackage = new BetterMenuItem()
            {
                Text = "Ignore updates for this package",
                IconName="pin",
            };
            menuIgnorePackage.Click += MenuIgnorePackage_Invoked;

            var menuSkipVersion = new BetterMenuItem()
            {
                Text = "Skip this version",
                IconName="skip",
            };
            menuSkipVersion.Click += MenuSkipVersion_Invoked;

            var menuShare = new BetterMenuItem()
            {
                Text = "Share this package",
                IconName="share",
            };
            menuShare.Click += (o, e) => { SharePackage(PackageList.SelectedItem as Package); };

            var menuDetails = new BetterMenuItem()
            {
                Text = "Package details",
                IconName="info",
                KeyboardAcceleratorTextOverride = "Enter"
            };
            menuDetails.Click += (o, e) => { ShowDetailsForPackage(PackageList.SelectedItem as Package); };

            ContextMenu.Items.Add(menuInstall);
            ContextMenu.Items.Add(new MenuFlyoutSeparator());
            ContextMenu.Items.Add(menuInstallSettings);
            ContextMenu.Items.Add(new MenuFlyoutSeparator());
            ContextMenu.Items.Add(MenuAsAdmin);
            ContextMenu.Items.Add(MenuInteractive);
            ContextMenu.Items.Add(MenuskipHash);
            ContextMenu.Items.Add(new MenuFlyoutSeparator());
            ContextMenu.Items.Add(menuUpdateAfterUninstall);
            ContextMenu.Items.Add(menuUninstall);
            ContextMenu.Items.Add(new MenuFlyoutSeparator());
            ContextMenu.Items.Add(menuIgnorePackage);
            ContextMenu.Items.Add(menuSkipVersion);
            ContextMenu.Items.Add(new MenuFlyoutSeparator());
            ContextMenu.Items.Add(menuShare);
            ContextMenu.Items.Add(menuDetails);

            return ContextMenu;
        }

        protected override void WhenShowingContextMenu(Package package)
        {
            MenuAsAdmin.IsEnabled = package.Manager.Capabilities.CanRunAsAdmin;
            MenuInteractive.IsEnabled = package.Manager.Capabilities.CanRunInteractively;
            MenuskipHash.IsEnabled = package.Manager.Capabilities.CanSkipIntegrityChecks;

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

            AppBarButton SelectAll = new();
            AppBarButton SelectNone = new();

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
            ToolBar.PrimaryCommands.Add(SelectAll);
            ToolBar.PrimaryCommands.Add(SelectNone);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(IgnoreSelected);
            ToolBar.PrimaryCommands.Add(ManageIgnored);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(HelpButton);

            Dictionary<AppBarButton, string> Labels = new()
            { // Entries with a trailing space are collapsed
              // Their texts will be used as the tooltip
                { UpdateSelected,       CoreTools.Translate("Update selected packages") },
                { UpdateAsAdmin,        " " + CoreTools.Translate("Update as administrator") },
                { UpdateSkipHash,       " " + CoreTools.Translate("Skip integrity checks") },
                { UpdateInteractive,    " " + CoreTools.Translate("Interactive update") },
                { InstallationSettings, " " + CoreTools.Translate("Installation options") },
                { PackageDetails,       " " + CoreTools.Translate("Package details") },
                { SharePackage,         " " + CoreTools.Translate("Share") },
                { SelectAll,            " " + CoreTools.Translate("Select all") },
                { SelectNone,           " " + CoreTools.Translate("Clear selection") },
                { IgnoreSelected,       CoreTools.Translate("Ignore selected packages") },
                { ManageIgnored,        CoreTools.Translate("Manage ignored updates") },
                { HelpButton,           CoreTools.Translate("Help") }
            };

            foreach (AppBarButton toolButton in Labels.Keys)
            {
                toolButton.IsCompact = Labels[toolButton][0] == ' ';
                if (toolButton.IsCompact)
                    toolButton.LabelPosition = CommandBarLabelPosition.Collapsed;
                toolButton.Label = Labels[toolButton].Trim();
            }

            Dictionary<AppBarButton, string> Icons = new()
            {
                { UpdateSelected,       "menu_updates" },
                { UpdateAsAdmin,        "runasadmin" },
                { UpdateSkipHash,       "checksum" },
                { UpdateInteractive,    "interactive" },
                { InstallationSettings, "options" },
                { PackageDetails,       "info" },
                { SharePackage,         "share" },
                { SelectAll,            "selectall" },
                { SelectNone,           "selectnone" },
                { IgnoreSelected,       "pin" },
                { ManageIgnored,        "clipboard_list" },
                { HelpButton,           "help" }
            };

            foreach (AppBarButton toolButton in Icons.Keys)
                toolButton.Icon = new LocalIcon(Icons[toolButton]);


            PackageDetails.Click += (s, e) =>
            {
                if (PackageList.SelectedItem != null)
                    ShowDetailsForPackage(PackageList.SelectedItem as Package);
            };

            HelpButton.Click += (s, e) => { MainApp.Instance.MainWindow.NavigationPage.ShowHelp(); };

            InstallationSettings.Click += async (s, e) =>
            {   if (PackageList.SelectedItem != null)
                    ShowInstallationOptionsForPackage(PackageList.SelectedItem as Package);
            };

            ManageIgnored.Click += async (s, e) => { await MainApp.Instance.MainWindow.NavigationPage.ManageIgnoredUpdatesDialog(); };
            IgnoreSelected.Click += async (s, e) =>
            {
                foreach (UpgradablePackage package in FilteredPackages.ToArray()) if (package.IsChecked)
                    {
                        await package.AddToIgnoredUpdatesAsync();
                        MainApp.Instance.MainWindow.NavigationPage.UpdatesPage.RemoveCorrespondingPackages(package);
                    }
            };

            UpdateSelected.Click += (s, e) =>
            {
                foreach (UpgradablePackage package in FilteredPackages.ToArray()) if (package.IsChecked)
                        MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package));
            };
            UpdateAsAdmin.Click += (s, e) =>
            {
                foreach (UpgradablePackage package in FilteredPackages.ToArray()) if (package.IsChecked)
                        MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package,
                            new InstallationOptions(package) { RunAsAdministrator = true }));
            };
            UpdateSkipHash.Click += (s, e) =>
            {
                foreach (UpgradablePackage package in FilteredPackages.ToArray()) if (package.IsChecked)
                        MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package,
                            new InstallationOptions(package) { SkipHashCheck = true }));
            };
            UpdateInteractive.Click += (s, e) =>
            {
                foreach (UpgradablePackage package in FilteredPackages.ToArray()) if (package.IsChecked)
                        MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package,
                            new InstallationOptions(package) { InteractiveInstallation = true }));
            };

            SharePackage.Click += (s, e) =>
            {
                if (PackageList.SelectedItem != null)
                    MainApp.Instance.MainWindow.SharePackage(PackageList.SelectedItem as Package);
            };

            SelectAll.Click += (s, e) => { SelectAllItems(); };
            SelectNone.Click += (s, e) => { ClearItemSelection(); };

        }

        protected override async Task<bool> IsPackageValid(Package package)
        {
            if (await package.HasUpdatesIgnoredAsync(package.NewVersion))
                return false;

            if (package is UpgradablePackage && (package as UpgradablePackage).NewVersionIsInstalled())
                return false;
            
            return true;
        }

        protected override Task<Package[]> LoadPackagesFromManager(PackageManager manager)
        {
            return manager.GetAvailableUpdates();
        }
#pragma warning disable 
        protected override async Task WhenAddingPackage(Package package)
        {
            package.GetAvailablePackage()?.SetTag(PackageTag.IsUpgradable);
            package.GetInstalledPackage()?.SetTag(PackageTag.IsUpgradable);
        }
#pragma warning restore

        protected override void WhenPackageCountUpdated()
        {
            try
            {
                MainApp.Instance.TooltipStatus.AvailableUpdates = Packages.Count();
            }
            catch (Exception)
            { }
        }

        public void UpdateAll()
        {
            foreach (UpgradablePackage package in Packages)
            {
                MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package));
            }
        }

        protected override async Task WhenPackagesLoaded(ReloadReason reason)
        {
            List<UpgradablePackage> upgradablePackages = new();
            foreach (UpgradablePackage package in Packages)
            {
                if (package.Tag != PackageTag.OnQueue && package.Tag != PackageTag.BeingProcessed)
                    upgradablePackages.Add(package);
            }

            if (upgradablePackages.Count > 0)
            {
                string body = "";
                string title = "";
                string attribution = "";
                bool ShowButtons = false;
                if (Settings.Get("AutomaticallyUpdatePackages") || Environment.GetCommandLineArgs().Contains("--updateapps"))
                {
                    if (upgradablePackages.Count == 1)
                    {
                        title = CoreTools.Translate("An update was found!");
                        body = CoreTools.Translate("{0} is being updated to version {1}").Replace("{0}", upgradablePackages[0].Name).Replace("{1}", upgradablePackages[0].NewVersion);
                        attribution = CoreTools.Translate("You have currently version {0} installed").Replace("{0}", upgradablePackages[0].Version);
                    }
                    else
                    {
                        title = CoreTools.Translate("Updates found!");
                        body = CoreTools.Translate("{0} packages are being updated").Replace("{0}", upgradablePackages.Count.ToString()); ;
                        foreach (UpgradablePackage package in upgradablePackages)
                        {
                            attribution += package.Name + ", ";
                        }
                        attribution = attribution.TrimEnd(' ').TrimEnd(',');
                    }
                    UpdateAll();
                }
                else
                {
                    if (upgradablePackages.Count == 1)
                    {
                        title = CoreTools.Translate("An update was found!");
                        body = CoreTools.Translate("{0} can be updated to version {1}").Replace("{0}", upgradablePackages[0].Name).Replace("{1}", upgradablePackages[0].NewVersion);
                        attribution = CoreTools.Translate("You have currently version {0} installed").Replace("{0}", upgradablePackages[0].Version);
                    }
                    else
                    {
                        title = CoreTools.Translate("Updates found!");
                        body = CoreTools.Translate("{0} packages can be updated").Replace("{0}", upgradablePackages.Count.ToString()); ;
                        foreach (UpgradablePackage package in upgradablePackages)
                        {
                            attribution += package.Name + ", ";
                        }
                        attribution = attribution.TrimEnd(' ').TrimEnd(',');
                    }
                    ShowButtons = true;
                }

                if (!Settings.Get("DisableUpdatesNotifications") && !Settings.Get("DisableNotifications"))
                {
                    try
                    {

                        ToastContentBuilder toast = new();
                        toast.AddArgument("action", "openUniGetUIOnUpdatesTab");
                        toast.AddArgument("notificationId", CoreData.UpdatesAvailableNotificationId);
                        toast.AddText(title);
                        toast.AddText(body);
                        toast.AddAttributionText(attribution);
                        if (ShowButtons)
                        {
                            toast.AddButton(new ToastButton()
                                .SetContent(CoreTools.Translate("Open WingetUI"))
                                .AddArgument("action", "openUniGetUIOnUpdatesTab")
                                .SetBackgroundActivation());
                            toast.AddButton(new ToastButton()
                                .SetContent(upgradablePackages.Count == 1 ? CoreTools.Translate("Update") : CoreTools.Translate("Update all"))
                                .AddArgument("action", "updateAll")
                                .SetBackgroundActivation());
                        }
                        toast.Show();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("An error occurred when showing a toast notification regarding available updates");
                        Logger.Warn(ex);
                    }
                }
            }

            if (!Settings.Get("DisableAutoCheckforUpdates") && reason != ReloadReason.Manual && reason != ReloadReason.External)
            {
                long waitTime = 3600;
                try
                {
                    waitTime = long.Parse(Settings.GetValue("UpdatesCheckInterval"));
                    Logger.Debug($"Starting check for updates wait interval with waitTime={waitTime}");
                }
                catch
                {
                    Logger.Debug("Invalid value for UpdatesCheckInterval, using default value of 3600 seconds");
                }
                await Task.Delay(TimeSpan.FromSeconds(waitTime));
                _ = LoadPackages(ReloadReason.Automated);
            }
        }

        private void MenuInstall_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            MainApp.Instance.AddOperationToList(new UpdatePackageOperation(PackageList.SelectedItem as UpgradablePackage));
        }

        private void MenuSkipHash_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            MainApp.Instance.AddOperationToList(new UpdatePackageOperation(PackageList.SelectedItem as UpgradablePackage,
                new InstallationOptions(PackageList.SelectedItem as UpgradablePackage) { SkipHashCheck = true }));
        }

        private void MenuInteractive_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            MainApp.Instance.AddOperationToList(new UpdatePackageOperation(PackageList.SelectedItem as UpgradablePackage,
                new InstallationOptions(PackageList.SelectedItem as UpgradablePackage) { InteractiveInstallation = true }));
        }

        private void MenuAsAdmin_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            MainApp.Instance.AddOperationToList(new UpdatePackageOperation(PackageList.SelectedItem as UpgradablePackage,
                new InstallationOptions(PackageList.SelectedItem as UpgradablePackage) { RunAsAdministrator = true }));
        }

        private void MenuUpdateAfterUninstall_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            MainApp.Instance.AddOperationToList(new UninstallPackageOperation(PackageList.SelectedItem as UpgradablePackage));
            MainApp.Instance.AddOperationToList(new InstallPackageOperation(PackageList.SelectedItem as UpgradablePackage));
        }

        private void MenuUninstall_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            MainApp.Instance.AddOperationToList(new UninstallPackageOperation(PackageList.SelectedItem as Package));
        }

        private void MenuIgnorePackage_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            _ = (PackageList.SelectedItem as Package).AddToIgnoredUpdatesAsync();
            MainApp.Instance.MainWindow.NavigationPage.UpdatesPage.RemoveCorrespondingPackages(PackageList.SelectedItem as Package);
        }

        private void MenuSkipVersion_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            _ = (PackageList.SelectedItem as Package).AddToIgnoredUpdatesAsync((PackageList.SelectedItem as Package).NewVersion);
            MainApp.Instance.MainWindow.NavigationPage.UpdatesPage.RemoveCorrespondingPackages(PackageList.SelectedItem as Package);
        }

        public void UpdatePackageForId(string id)
        {
            foreach (UpgradablePackage package in Packages)
            {
                if (package.Id == id)
                {
                    MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package));
                    Logger.Info($"[WIDGETS] Updating package with id {id}");
                    break;
                }
            }
            Logger.Warn($"[WIDGETS] No package with id={id} was found");
        }

        public void UpdateAllPackages()
        {
            Logger.Info("[WIDGETS] Updating all packages");
            foreach (UpgradablePackage package in Packages)
                if (package.Tag != PackageTag.OnQueue && package.Tag != PackageTag.BeingProcessed)
                    MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package));
        }

        public void UpdateAllPackagesForManager(string manager)
        {
            Logger.Info($"[WIDGETS] Updating all packages for manager {manager}");
            foreach (UpgradablePackage package in Packages)
                if (package.Tag != PackageTag.OnQueue && package.Tag != PackageTag.BeingProcessed)
                    if (package.Manager.Name == manager)
                        MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package));
        }
    }
}

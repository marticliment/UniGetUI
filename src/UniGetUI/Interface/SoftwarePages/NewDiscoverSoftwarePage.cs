using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.Interface.SoftwarePages
{
    public class NewDiscoverSoftwarePage : AbstractPackagesPage
    {
        BetterMenuItem? MenuAsAdmin;
        BetterMenuItem? MenuInteractive;
        BetterMenuItem? MenuSkipHash;

        public override BetterMenu GenerateContextMenu()
        {
            BetterMenu menu = new BetterMenu();

            var menuInstall = new BetterMenuItem
            {
                Text = "Install",
                IconName = "newversion",
                KeyboardAcceleratorTextOverride = "Ctrl+Enter"
            };
            menuInstall.Click += MenuInstall_Invoked;
            menu.Items.Add(menuInstall);

            menu.Items.Add(new MenuFlyoutSeparator { Height = 5 });

            var menuInstallSettings = new BetterMenuItem
            {
                Text = "Installation options",
                IconName = "options",
                KeyboardAcceleratorTextOverride = "Alt+Enter"
            };
            menuInstallSettings.Click += MenuInstallSettings_Invoked;
            menu.Items.Add(menuInstallSettings);

            menu.Items.Add(new MenuFlyoutSeparator { Height = 5 });

            MenuAsAdmin = new BetterMenuItem
            {
                Text = "Install as administrator",
                IconName = "runasadmin"
            };
            MenuAsAdmin.Click += MenuAsAdmin_Invoked;
            menu.Items.Add(MenuAsAdmin);

            MenuInteractive = new BetterMenuItem
            {
                Text = "Interactive installation",
                IconName = "interactive"
            };
            MenuInteractive.Click += MenuInteractive_Invoked;
            menu.Items.Add(MenuInteractive);

            MenuSkipHash = new BetterMenuItem
            {
                Text = "Skip hash check",
                IconName = "checksum"
            };
            MenuSkipHash.Click += MenuSkipHash_Invoked;
            menu.Items.Add(MenuSkipHash);

            menu.Items.Add(new MenuFlyoutSeparator { Height = 5 });

            var menuShare = new BetterMenuItem
            {
                Text = "Share this package",
                IconName = "share"
            };
            menuShare.Click += MenuShare_Invoked;
            menu.Items.Add(menuShare);

            var menuDetails = new BetterMenuItem
            {
                Text = "Package details",
                IconName = "info",
                KeyboardAcceleratorTextOverride = "Enter"
            };
            menuDetails.Click += MenuDetails_Invoked;
            menu.Items.Add(menuDetails);

            return menu;
        }

        public override void GenerateToolBar()
        {
            if (!Initialized)
                return;
            AppBarButton InstallSelected = new();
            AppBarButton InstallAsAdmin = new();
            AppBarButton InstallSkipHash = new();
            AppBarButton InstallInteractive = new();

            AppBarButton InstallationSettings = new();

            AppBarButton PackageDetails = new();
            AppBarButton SharePackage = new();

            AppBarButton SelectAll = new();
            AppBarButton SelectNone = new();

            AppBarButton ExportSelection = new();

            AppBarButton HelpButton = new();

            ToolBar.PrimaryCommands.Add(InstallSelected);
            ToolBar.PrimaryCommands.Add(InstallAsAdmin);
            ToolBar.PrimaryCommands.Add(InstallSkipHash);
            ToolBar.PrimaryCommands.Add(InstallInteractive);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(InstallationSettings);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(PackageDetails);
            ToolBar.PrimaryCommands.Add(SharePackage);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(SelectAll);
            ToolBar.PrimaryCommands.Add(SelectNone);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(ExportSelection);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(HelpButton);

            Dictionary<AppBarButton, string> Labels = new()
            {   // Entries with a trailing space are collapsed
                // Their texts will be used as the tooltip
                { InstallSelected,        CoreTools.Translate("Install selected packages") },
                { InstallAsAdmin,         " " + CoreTools.Translate("Install as administrator") },
                { InstallSkipHash,        " " + CoreTools.Translate("Skip integrity checks") },
                { InstallInteractive,     " " + CoreTools.Translate("Interactive installation") },
                { InstallationSettings,   CoreTools.Translate("Installation options") },
                { PackageDetails,         " " + CoreTools.Translate("Package details") },
                { SharePackage,           " " + CoreTools.Translate("Share") },
                { SelectAll,              " " + CoreTools.Translate("Select all") },
                { SelectNone,             " " + CoreTools.Translate("Clear selection") },
                { ExportSelection,        CoreTools.Translate("Add selection to bundle") },
                { HelpButton,             CoreTools.Translate("Help") }
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
                { InstallSelected,      "install" },
                { InstallAsAdmin,       "runasadmin" },
                { InstallSkipHash,      "checksum" },
                { InstallInteractive,   "interactive" },
                { InstallationSettings, "options" },
                { PackageDetails,       "info" },
                { SharePackage,         "share" },
                { SelectAll,            "selectall" },
                { SelectNone,           "selectnone" },
                { ExportSelection,      "add_to" },
                { HelpButton,           "help" }
            };

            foreach (AppBarButton toolButton in Icons.Keys)
                toolButton.Icon = new LocalIcon(Icons[toolButton]);

            PackageDetails.Click += (s, e) =>
            {
                if (PackageList.SelectedItem != null)
                    ShowDetailsForPackage(PackageList.SelectedItem as Package);
            };

            ExportSelection.Click += ExportSelection_Click;
            HelpButton.Click += (s, e) => { MainApp.Instance.MainWindow.NavigationPage.ShowHelp(); };

            InstallationSettings.Click += (s, e) =>
            {
                ShowInstallationOptionsForPackage(PackageList.SelectedItem as Package);
            };

            InstallSelected.Click += (s, e) =>
            {
                foreach (Package package in FilteredPackages) if (package.IsChecked)
                        MainApp.Instance.AddOperationToList(new InstallPackageOperation(package));
            };

            InstallAsAdmin.Click += (s, e) =>
            {
                foreach (Package package in FilteredPackages) if (package.IsChecked)
                        MainApp.Instance.AddOperationToList(new InstallPackageOperation(package,
                            new InstallationOptions(package) { RunAsAdministrator = true }));
            };

            InstallSkipHash.Click += (s, e) =>
            {
                foreach (Package package in FilteredPackages) if (package.IsChecked)
                        MainApp.Instance.AddOperationToList(new InstallPackageOperation(package,
                            new InstallationOptions(package) { SkipHashCheck = true }));
            };

            InstallInteractive.Click += (s, e) =>
            {
                foreach (Package package in FilteredPackages) if (package.IsChecked)
                        MainApp.Instance.AddOperationToList(new InstallPackageOperation(package,
                            new InstallationOptions(package) { InteractiveInstallation = true }));
            };

            SharePackage.Click += (s, e) =>
            {
                MainApp.Instance.MainWindow.SharePackage(PackageList.SelectedItem as Package);
            };

            SelectAll.Click += (s, e) => { SelectAllItems(); };
            SelectNone.Click += (s, e) => { ClearItemSelection(); };
        }

        public override void GenerateUIText()
        {
            DISABLE_AUTOMATIC_PACKAGE_LOAD_ON_START = true;
            MEGA_QUERY_BOX_ENABLED = true;
            PAGE_NAME = "Discover";

            PageRole = OperationType.Install;
            InstantSearchCheckbox.IsEnabled = false;
            InstantSearchCheckbox.Visibility = Visibility.Collapsed;

            FindButton.Click += (s, e) => { if(QueryBlock.Text.Trim() != "") _ = LoadPackages(ReloadReason.Manual); };
            QueryBlock.KeyUp += (s, e) =>
            {
                if (QueryBlock.Text.Trim() != "" && e.Key == Windows.System.VirtualKey.Enter)
                    _ = LoadPackages(ReloadReason.Manual);
            };

            MegaFindButton.Click += (s, e) => { if (QueryBlock.Text.Trim() != "") _ = LoadPackages(ReloadReason.Manual); };
            MegaQueryBlock.KeyUp += (s, e) => { if (QueryBlock.Text.Trim() != "" && e.Key == Windows.System.VirtualKey.Enter) _ = LoadPackages(ReloadReason.Manual); };


            NoPackages_BackgroundText = CoreTools.Translate("No results were found matching the input criteria");
            NoPackages_SourcesText = CoreTools.Translate("No packages were found");
            NoPackages_SubtitleMainText = NoPackages_SourcesText;

            NoMatches_BackgroundText = NoPackages_BackgroundText;
            NoMatches_SourcesText = NoPackages_SourcesText;

            MainTitleText = CoreTools.AutoTranslated("Discover Packages");
            MainTitleGlyph = "\uF6FA";

            QueryOptionsGroup.SelectedIndex = 1;
            QueryOptionsGroup.SelectedIndex = 2;
            QueryOptionsGroup.SelectedItem = QueryBothRadio;
        }

#pragma warning disable
        protected override async Task<bool> IsPackageValid(Package package)
        {
            return true;
        }
#pragma warning restore

        protected override Task<Package[]> LoadPackagesFromManager(PackageManager manager)
        {
            var text = QueryBlock.Text;
            text = CoreTools.EnsureSafeQueryString(text);
            if (text == string.Empty)
                return new Task<Package[]>(() => { return []; });
            else
                return manager.FindPackages(text);
        }

#pragma warning disable
        protected override async Task WhenAddingPackage(Package package)
        {
            if (package.GetUpgradablePackage() != null)
                package.SetTag(PackageTag.IsUpgradable);
            else if (package.GetInstalledPackage() != null)
                package.SetTag(PackageTag.AlreadyInstalled);
        }
#pragma warning restore

        protected override void WhenPackageCountUpdated()
        {
            return;
        }

#pragma warning disable
        protected override async Task WhenPackagesLoaded(ReloadReason reason)
        {
            return;
        }
#pragma warning restore

        protected override void WhenShowingContextMenu(Package package)
        {
            if (MenuAsAdmin == null || MenuInteractive == null || MenuSkipHash == null)
            {
                Logger.Warn("MenuItems are null on DiscoverPackagesPage");
                return;
            }

            MenuAsAdmin.IsEnabled = package.Manager.Capabilities.CanRunAsAdmin;
            MenuInteractive.IsEnabled = package.Manager.Capabilities.CanRunInteractively;
            MenuSkipHash.IsEnabled = package.Manager.Capabilities.CanSkipIntegrityChecks;
        }

        private async void ExportSelection_Click(object sender, RoutedEventArgs e)
        {
            MainApp.Instance.MainWindow.NavigationPage.BundlesNavButton.ForceClick();
            await MainApp.Instance.MainWindow.NavigationPage.BundlesPage.AddPackages(FilteredPackages.ToArray().Where(x => x.IsChecked));
        }

        private void MenuDetails_Invoked(object sender, RoutedEventArgs e)
        {
            ShowDetailsForPackage(PackageList.SelectedItem as Package);
        }

        private void MenuShare_Invoked(object sender, RoutedEventArgs e)
        {
            if (!Initialized || PackageList.SelectedItem == null)
                return;
            MainApp.Instance.MainWindow.SharePackage(PackageList.SelectedItem as Package);
        }

        private void MenuInstall_Invoked(object sender, RoutedEventArgs e)
        {
            var package = PackageList.SelectedItem as Package;
            if (!Initialized || package == null)
                return;
            MainApp.Instance.AddOperationToList(new InstallPackageOperation(package));
        }

        private void MenuSkipHash_Invoked(object sender, RoutedEventArgs e)
        {
            var package = PackageList.SelectedItem as Package;
            if (!Initialized || package == null)
                return;
            MainApp.Instance.AddOperationToList(new InstallPackageOperation(package,
                new InstallationOptions(package) { SkipHashCheck = true }));
        }

        private void MenuInteractive_Invoked(object sender, RoutedEventArgs e)
        {
            var package = PackageList.SelectedItem as Package;
            if (!Initialized || package == null)
                return;
            MainApp.Instance.AddOperationToList(new InstallPackageOperation(package,
                new InstallationOptions(package) { InteractiveInstallation = true }));
        }

        private void MenuAsAdmin_Invoked(object sender, RoutedEventArgs e)
        {
            var package = PackageList.SelectedItem as Package;
            if (!Initialized || package == null)
                return;
            MainApp.Instance.AddOperationToList(new InstallPackageOperation(package,
                new InstallationOptions(package) { RunAsAdministrator = true }));
        }

        private void MenuInstallSettings_Invoked(object sender, RoutedEventArgs e)
        {
            ShowInstallationOptionsForPackage(PackageList.SelectedItem as Package);
        }

        public void ShowSharedPackage_ThreadSafe(string pId, string pSource)
        {
            MainApp.Instance.MainWindow.DispatcherQueue.TryEnqueue(() => { ShowSharedPackage(pId, pSource); });
        }

        private async void ShowSharedPackage(string pId, string pSource)
        {
            if (!Initialized)
                return;

            Logger.Info($"Showing shared package with pId=${pId} and pSource=${pSource}...");

            MainApp.Instance.MainWindow.Activate();

            MainApp.Instance.MainWindow.ShowLoadingDialog(CoreTools.Translate("Please wait...", pId));
            QueryIdRadio.IsChecked = true;
            QueryBlock.Text = pId;
            await LoadPackages();
            QueryBothRadio.IsChecked = true;
            MainApp.Instance.MainWindow.HideLoadingDialog();
            if (FilteredPackages.Count == 1)
            {
                Logger.Debug("Only one package was found for pId=" + pId + ", showing it.");
                await MainApp.Instance.MainWindow.NavigationPage.ShowPackageDetails(FilteredPackages[0], OperationType.Install);
            }
            else if (FilteredPackages.Count > 1)
            {
                string managerName = pSource.Contains(':') ? pSource.Split(':')[0] : pSource;
                foreach (Package match in FilteredPackages)
                    if (match.Source.Manager.Name == managerName)
                    {
                        Logger.Debug("Equivalent package for pId=" + pId + " and pSource=" + pSource + " found: " + match.ToString());
                        await MainApp.Instance.MainWindow.NavigationPage.ShowPackageDetails(match, OperationType.Install);
                        return;
                    }
                Logger.Debug("No package found with the exact same manager, showing the first one.");
                await MainApp.Instance.MainWindow.NavigationPage.ShowPackageDetails(FilteredPackages[0], OperationType.Install);
            }
            else
            {
                Logger.Error("No packages were found matching the given pId=" + pId);
                ContentDialog c = new();
                c.XamlRoot = XamlRoot;
                c.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
                c.Title = CoreTools.Translate("Package not found");
                c.Content = CoreTools.Translate("The package {0} from {1} was not found.", pId, pSource);
                c.PrimaryButtonText = CoreTools.Translate("OK");
                c.DefaultButton = ContentDialogButton.Primary;
                await MainApp.Instance.MainWindow.ShowDialogAsync(c);
            }
        }

    }
}

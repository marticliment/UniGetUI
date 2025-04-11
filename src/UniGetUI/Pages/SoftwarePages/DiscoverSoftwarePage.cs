using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageLoader;
using Windows.System;
using UniGetUI.Interface.Telemetry;
using UniGetUI.Pages.DialogPages;

namespace UniGetUI.Interface.SoftwarePages
{
    public partial class DiscoverSoftwarePage : AbstractPackagesPage
    {
        private BetterMenuItem? MenuAsAdmin;
        private BetterMenuItem? MenuInteractive;
        private BetterMenuItem? MenuSkipHash;
        private BetterMenuItem? MenuDownloadInstaller;
        public DiscoverSoftwarePage()
        : base(new PackagesPageData
        {
            DisableAutomaticPackageLoadOnStart = true,
            DisableFilterOnQueryChange = true,
            MegaQueryBlockEnabled = true,
            PackagesAreCheckedByDefault = false,
            ShowLastLoadTime = false,
            DisableSuggestedResultsRadio = false,
            PageName = "Discover",

            Loader = PEInterface.DiscoveredPackagesLoader,
            PageRole = OperationType.Install,

            NoPackages_BackgroundText = CoreTools.Translate("No results were found matching the input criteria"),
            NoPackages_SourcesText = CoreTools.Translate("No packages were found"),
            NoPackages_SubtitleText_Base = CoreTools.Translate("No packages were found"),
            MainSubtitle_StillLoading = CoreTools.Translate("Loading packages"),
            NoMatches_BackgroundText = CoreTools.Translate("No results were found matching the input criteria"),

            PageTitle = CoreTools.Translate("Discover Packages"),
            Glyph = "\uF6FA"
        })
        {
            InstantSearchCheckbox.IsEnabled = false;
            InstantSearchCheckbox.Visibility = Visibility.Collapsed;

            FindButton.Click += Event_SearchPackages;
            MegaFindButton.Click += Event_SearchPackages;

            QueryBlock.KeyUp += (s, e) => { if (e.Key == VirtualKey.Enter) { Event_SearchPackages(s, e); } };
            MegaQueryBlock.KeyUp += (s, e) => { if (e.Key == VirtualKey.Enter) { Event_SearchPackages(s, e); } };
        }

        public override BetterMenu GenerateContextMenu()
        {
            BetterMenu menu = new();

            BetterMenuItem menuInstall = new()
            {
                Text = CoreTools.AutoTranslated("Install"),
                IconName = IconType.Download,
                KeyboardAcceleratorTextOverride = "Ctrl+Enter"
            };
            menuInstall.Click += MenuInstall_Invoked;
            menu.Items.Add(menuInstall);

            menu.Items.Add(new MenuFlyoutSeparator { Height = 5 });

            BetterMenuItem menuInstallSettings = new()
            {
                Text = CoreTools.AutoTranslated("Installation options"),
                IconName = IconType.Options,
                KeyboardAcceleratorTextOverride = "Alt+Enter"
            };
            menuInstallSettings.Click += MenuInstallSettings_Invoked;
            menu.Items.Add(menuInstallSettings);

            menu.Items.Add(new MenuFlyoutSeparator { Height = 5 });

            MenuAsAdmin = new BetterMenuItem
            {
                Text = CoreTools.AutoTranslated("Install as administrator"),
                IconName = IconType.UAC
            };
            MenuAsAdmin.Click += MenuAsAdmin_Invoked;
            menu.Items.Add(MenuAsAdmin);

            MenuInteractive = new BetterMenuItem
            {
                Text = CoreTools.AutoTranslated("Interactive installation"),
                IconName = IconType.Interactive
            };
            MenuInteractive.Click += MenuInteractive_Invoked;
            menu.Items.Add(MenuInteractive);

            MenuSkipHash = new BetterMenuItem
            {
                Text = CoreTools.AutoTranslated("Skip hash check"),
                IconName = IconType.Checksum
            };
            MenuSkipHash.Click += MenuSkipHash_Invoked;
            menu.Items.Add(MenuSkipHash);

            MenuDownloadInstaller = new BetterMenuItem
            {
                Text = CoreTools.AutoTranslated("Download installer"),
                IconName = IconType.Download
            };
            MenuDownloadInstaller.Click += (_, _) => _ = MainApp.Operations.AskLocationAndDownload(SelectedItem, TEL_InstallReferral.DIRECT_SEARCH);
            menu.Items.Add(MenuDownloadInstaller);

            menu.Items.Add(new MenuFlyoutSeparator { Height = 5 });

            BetterMenuItem menuShare = new()
            {
                Text = CoreTools.AutoTranslated("Share this package"),
                IconName = IconType.Share
            };
            menuShare.Click += MenuShare_Invoked;
            menu.Items.Add(menuShare);

            BetterMenuItem menuDetails = new()
            {
                Text = CoreTools.AutoTranslated("Package details"),
                IconName = IconType.Info_Round,
                KeyboardAcceleratorTextOverride = "Enter"
            };
            menuDetails.Click += MenuDetails_Invoked;
            menu.Items.Add(menuDetails);

            return menu;
        }

        public override void GenerateToolBar()
        {
            AppBarButton InstallSelected = new();
            AppBarButton InstallAsAdmin = new();
            AppBarButton InstallSkipHash = new();
            AppBarButton InstallInteractive = new();

            AppBarButton InstallationSettings = new();

            AppBarButton PackageDetails = new();
            AppBarButton SharePackage = new();

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
                { ExportSelection,        CoreTools.Translate("Add selection to bundle") },
                { HelpButton,             CoreTools.Translate("Help") }
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
                { InstallSelected,      IconType.Download },
                { InstallAsAdmin,       IconType.UAC },
                { InstallSkipHash,      IconType.Checksum },
                { InstallInteractive,   IconType.Interactive },
                { InstallationSettings, IconType.Options },
                { PackageDetails,       IconType.Info_Round },
                { SharePackage,         IconType.Share },
                { ExportSelection,      IconType.AddTo },
                { HelpButton,           IconType.Help }
            };

            foreach (AppBarButton toolButton in Icons.Keys)
            {
                toolButton.Icon = new LocalIcon(Icons[toolButton]);
            }

            PackageDetails.Click += (_, _) => ShowDetailsForPackage(SelectedItem, TEL_InstallReferral.DIRECT_SEARCH);
            ExportSelection.Click += ExportSelection_Click;
            HelpButton.Click += (_, _) => MainApp.Instance.MainWindow.NavigationPage.ShowHelp();
            InstallationSettings.Click += (_, _) => ShowInstallationOptionsForPackage(SelectedItem);

            InstallSelected.Click += (_, _) => MainApp.Operations.Install(FilteredPackages.GetCheckedPackages(), TEL_InstallReferral.DIRECT_SEARCH);
            InstallAsAdmin.Click += (_, _) => MainApp.Operations.Install(FilteredPackages.GetCheckedPackages(), TEL_InstallReferral.DIRECT_SEARCH, elevated: true);
            InstallSkipHash.Click += (_, _) => MainApp.Operations.Install(FilteredPackages.GetCheckedPackages(), TEL_InstallReferral.DIRECT_SEARCH, no_integrity: true);
            InstallInteractive.Click += (_, _) => MainApp.Operations.Install(FilteredPackages.GetCheckedPackages(), TEL_InstallReferral.DIRECT_SEARCH, interactive: true);

            SharePackage.Click += (_, _) => MainApp.Instance.MainWindow.SharePackage(SelectedItem);
        }

        public override async Task LoadPackages()
        {
            if (QueryBlock.Text.Trim() != "")
            {
                await LoadPackages(ReloadReason.External);
            }
        }

        private void Event_SearchPackages(object sender, RoutedEventArgs e)
        {
            if (QueryBlock.Text.Trim() != "")
            {
                _ = (Loader as DiscoverablePackagesLoader)?.ReloadPackages(QueryBlock.Text.Trim());
            }
            else
            {
                Loader.StopLoading();
            }
        }

        protected override void WhenPackageCountUpdated()
        { }

        protected override void WhenPackagesLoaded(ReloadReason reason)
        { }

        protected override void WhenShowingContextMenu(IPackage package)
        {
            if (MenuAsAdmin is null || MenuInteractive is null || MenuSkipHash is null || MenuDownloadInstaller is null)
            {
                Logger.Warn("MenuItems are null on DiscoverPackagesPage");
                return;
            }

            MenuAsAdmin.IsEnabled = package.Manager.Capabilities.CanRunAsAdmin;
            MenuInteractive.IsEnabled = package.Manager.Capabilities.CanRunInteractively;
            MenuSkipHash.IsEnabled = package.Manager.Capabilities.CanSkipIntegrityChecks;
            MenuDownloadInstaller.IsEnabled = package.Manager.Capabilities.CanDownloadInstaller;
        }

        private async void ExportSelection_Click(object sender, RoutedEventArgs e)
        {
            MainApp.Instance.MainWindow.NavigationPage.NavigateTo(PageType.Bundles);
            DialogHelper.ShowLoadingDialog(CoreTools.Translate("Please wait..."));
            await PEInterface.PackageBundlesLoader.AddPackagesAsync(FilteredPackages.GetCheckedPackages());
            DialogHelper.HideLoadingDialog();
        }

        private void MenuDetails_Invoked(object sender, RoutedEventArgs e)
        {
            ShowDetailsForPackage(SelectedItem, TEL_InstallReferral.DIRECT_SEARCH);
        }

        private void MenuShare_Invoked(object sender, RoutedEventArgs e)
        {
            if (SelectedItem is null)
                return;

            MainApp.Instance.MainWindow.SharePackage(SelectedItem);
        }

        private void MenuInstall_Invoked(object sender, RoutedEventArgs e)
            => _ = MainApp.Operations.Install(SelectedItem, TEL_InstallReferral.DIRECT_SEARCH);

        private void MenuSkipHash_Invoked(object sender, RoutedEventArgs e)
            => _ = MainApp.Operations.Install(SelectedItem, TEL_InstallReferral.DIRECT_SEARCH, no_integrity: true);

        private void MenuInteractive_Invoked(object sender, RoutedEventArgs e)
            => _ = MainApp.Operations.Install(SelectedItem, TEL_InstallReferral.DIRECT_SEARCH, interactive: true);

        private void MenuAsAdmin_Invoked(object sender, RoutedEventArgs e)
            => _ = MainApp.Operations.Install(SelectedItem, TEL_InstallReferral.DIRECT_SEARCH, elevated: true);

        private void MenuInstallSettings_Invoked(object sender, RoutedEventArgs e)
            => ShowInstallationOptionsForPackage(SelectedItem);

    }
}

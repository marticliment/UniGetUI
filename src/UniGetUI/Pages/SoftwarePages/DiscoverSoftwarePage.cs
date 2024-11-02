using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.PackageLoader;
using Windows.System;
using UniGetUI.Pages.DialogPages;

namespace UniGetUI.Interface.SoftwarePages
{
    public class DiscoverSoftwarePage : AbstractPackagesPage
    {
        private BetterMenuItem? MenuAsAdmin;
        private BetterMenuItem? MenuInteractive;
        private BetterMenuItem? MenuSkipHash;
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

                toolButton.Label = Labels[toolButton].Trim();
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

            PackageDetails.Click += (_, _) => ShowDetailsForPackage(SelectedItem);
            ExportSelection.Click += ExportSelection_Click;
            HelpButton.Click += (_, _) => MainApp.Instance.MainWindow.NavigationPage.ShowHelp();
            InstallationSettings.Click += (_, _) => ShowInstallationOptionsForPackage(SelectedItem);

            InstallSelected.Click += (_, _) =>
            {
                foreach (IPackage package in FilteredPackages.GetCheckedPackages())
                {
                    MainApp.Instance.AddOperationToList(new InstallPackageOperation(package));
                }
            };

            InstallAsAdmin.Click += async (_, _) =>
            {
                foreach (IPackage package in FilteredPackages.GetCheckedPackages())
                {
                    InstallationOptions options = await InstallationOptions.FromPackageAsync(package, elevated: true);
                    MainApp.Instance.AddOperationToList(new InstallPackageOperation(package, options));
                }
            };

            InstallSkipHash.Click += async (_, _) =>
            {
                foreach (IPackage package in FilteredPackages.GetCheckedPackages())
                {
                    InstallationOptions options = await InstallationOptions.FromPackageAsync(package, no_integrity: true);
                    MainApp.Instance.AddOperationToList(new InstallPackageOperation(package, options));
                }
            };

            InstallInteractive.Click += async (_, _) =>
            {
                foreach (IPackage package in FilteredPackages.GetCheckedPackages())
                {
                    InstallationOptions options = await InstallationOptions.FromPackageAsync(package, interactive: true);
                    MainApp.Instance.AddOperationToList(new InstallPackageOperation(package, options));
                }
            };

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
            if (MenuAsAdmin is null || MenuInteractive is null || MenuSkipHash is null)
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
            DialogHelper.ShowLoadingDialog(CoreTools.Translate("Please wait..."));
            await PEInterface.PackageBundlesLoader.AddPackagesAsync(FilteredPackages.GetCheckedPackages());
            DialogHelper.HideLoadingDialog();
        }

        private void MenuDetails_Invoked(object sender, RoutedEventArgs e)
        {
            ShowDetailsForPackage(SelectedItem);
        }

        private void MenuShare_Invoked(object sender, RoutedEventArgs e)
        {
            if (PackageList.SelectedItem is null)
            {
                return;
            }

            MainApp.Instance.MainWindow.SharePackage(SelectedItem);
        }

        private void MenuInstall_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package is null)
            {
                return;
            }

            MainApp.Instance.AddOperationToList(new InstallPackageOperation(package));
        }

        private async void MenuSkipHash_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package is null)
            {
                return;
            }

            MainApp.Instance.AddOperationToList(new InstallPackageOperation(package,
                await InstallationOptions.FromPackageAsync(package, no_integrity: true)));
        }

        private async void MenuInteractive_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package is null)
            {
                return;
            }

            MainApp.Instance.AddOperationToList(new InstallPackageOperation(package,
                await InstallationOptions.FromPackageAsync(package, interactive: true)));
        }

        private async void MenuAsAdmin_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package is null)
            {
                return;
            }

            MainApp.Instance.AddOperationToList(new InstallPackageOperation(package,
                await InstallationOptions.FromPackageAsync(package, elevated: true)));
        }

        private void MenuInstallSettings_Invoked(object sender, RoutedEventArgs e)
        {
            ShowInstallationOptionsForPackage(SelectedItem);
        }

        public void ShowSharedPackage_ThreadSafe(string id, string combinedSourceName)
        {
            var contents = combinedSourceName.Split(':');
            string managerName = contents[0];
            string sourceName = "";
            if (contents.Length > 1) sourceName = contents[1];
            ShowSharedPackage_ThreadSafe(id, managerName, sourceName);
        }

        public void ShowSharedPackage_ThreadSafe(string id, string managerName, string sourceName)
        {
            MainApp.Instance.MainWindow.DispatcherQueue.TryEnqueue(async () =>
            {
                IPackage? package = await GetPackageFromIdAndManager(id, managerName, sourceName);
                if (package is not null) ShowDetailsForPackage(package);
            });
        }

        private static async Task<IPackage?> GetPackageFromIdAndManager(string id, string managerName, string sourceName)
        {
            try
            {
                Logger.Info($"Showing shared package with pId={id} and pSource={managerName}: ´{sourceName} ...");
                MainApp.Instance.MainWindow.Activate();
                DialogHelper.ShowLoadingDialog(CoreTools.Translate("Please wait...", id));

                IPackageManager? manager = null;

                foreach (var candidate in PEInterface.Managers)
                {
                    if (candidate.Name == managerName || candidate.DisplayName == managerName)
                    {
                        manager = candidate;
                        break;
                    }
                }

                if (manager is null)
                {
                    throw new ArgumentException(CoreTools.Translate("The package manager \"{0}\" was not found", managerName));
                }

                if(!manager.IsEnabled())
                    throw new ArgumentException(CoreTools.Translate("The package manager \"{0}\" is disabled", manager.DisplayName));

                if(!manager.Status.Found)
                    throw new ArgumentException(CoreTools.Translate("There is an error with the configuration of the package manager \"{0}\"", manager.DisplayName));

                var results = await Task.Run(() => manager.FindPackages(id));
                var candidates = results.Where(p => p.Id == id).ToArray();

                if (candidates.Length == 0)
                {
                    throw new ArgumentException(CoreTools.Translate("The package \"{0}\" was not found on the package manager \"{1}\"", id, manager.DisplayName));
                }

                IPackage package = candidates[0];

                // Get package from best source
                if (candidates.Length >= 1 && manager.Capabilities.SupportsCustomSources)
                    foreach (var candidate in candidates)
                        if (candidate.Source.Name == sourceName)
                            package = candidate;

                Logger.ImportantInfo($"Found package {package.Id} on manager {package.Manager.Name}, showing it...");
                DialogHelper.HideLoadingDialog();
                return package;
            }
            catch (Exception ex)
            {
                Logger.Error($"An error occurred while attempting to show the package with id {id}");
                Logger.Error(ex);
                var warningDialog = new ContentDialog
                {
                    Title = CoreTools.Translate("Package not found"),
                    Content = CoreTools.Translate("An error occurred when attempting to show the package with Id {0}", id) + ":\n" + ex.Message,
                    CloseButtonText = CoreTools.Translate("Ok"),
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = MainApp.Instance.MainWindow.Content.XamlRoot // Ensure the dialog is shown in the correct context
                };

                DialogHelper.HideLoadingDialog();
                await MainApp.Instance.MainWindow.ShowDialogAsync(warningDialog);
                return null;
            }
        }
    }
}

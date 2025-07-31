using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Serialization;
using Windows.UI.Text;
using ExternalLibraries.Pickers;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.SettingsEngine.SecureSettings;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Telemetry;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Classes.Serializable;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.Pages.DialogPages;

namespace UniGetUI.Interface.SoftwarePages
{
    public partial class PackageBundlesPage : AbstractPackagesPage
    {
        private BetterMenuItem? MenuInstallOptions;
        private BetterMenuItem? MenuInstall;
        private BetterMenuItem? MenuShare;
        private BetterMenuItem? MenuDetails;
        private BetterMenuItem? MenuAsAdmin;
        private BetterMenuItem? MenuInteractive;
        private BetterMenuItem? MenuSkipHash;
        private BetterMenuItem? MenuDownloadInstaller;

        public event EventHandler<EventArgs>? UnsavedChangesStateChanged;

        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set
            {
                UnsavedChangesStateChanged?.Invoke(this, EventArgs.Empty);
                _hasUnsavedChanges = value;
            }
        }

        public PackageBundlesPage()
        : base(new PackagesPageData
        {
            DisableAutomaticPackageLoadOnStart = true,
            DisableFilterOnQueryChange = false,
            MegaQueryBlockEnabled = false,
            ShowLastLoadTime = false,
            DisableReload = true,
            PackagesAreCheckedByDefault = false,
            DisableSuggestedResultsRadio = true,
            PageName = "Bundles",

            Loader = PEInterface.PackageBundlesLoader,
            PageRole = OperationType.Install,

            NoPackages_BackgroundText = CoreTools.Translate("Add packages or open an existing package bundle"),
            NoPackages_SourcesText = CoreTools.Translate("Add packages to start"),
            NoPackages_SubtitleText_Base = CoreTools.Translate("The current bundle has no packages. Add some packages to get started"),
            MainSubtitle_StillLoading = CoreTools.Translate("Loading packages"),
            NoMatches_BackgroundText = CoreTools.Translate("No results were found matching the input criteria"),

            PageTitle = CoreTools.Translate("Package Bundles"),
            Glyph = "\uF133"
        })
        {
            Loader.PackagesChanged += (_, _) =>
            {
                HasUnsavedChanges = true;
            };
        }

        public override BetterMenu GenerateContextMenu()
        {
            BetterMenu menu = new();
            MenuInstall = new()
            {
                Text = CoreTools.AutoTranslated("Install"),
                IconName = IconType.Download,
                KeyboardAcceleratorTextOverride = "Ctrl+Enter"
            };
            MenuInstall.Click += MenuInstall_Invoked;
            menu.Items.Add(MenuInstall);

            menu.Items.Add(new MenuFlyoutSeparator { Height = 5 });

            MenuInstallOptions = new()
            {
                Text = CoreTools.AutoTranslated("Install options"),
                IconName = IconType.Options,
                KeyboardAcceleratorTextOverride = "Alt+Enter"
            };
            MenuInstallOptions.Click += MenuInstallSettings_Invoked;
            menu.Items.Add(MenuInstallOptions);

            menu.Items.Add(new MenuFlyoutSeparator());

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
                Text = CoreTools.AutoTranslated("Skip hash checks"),
                IconName = IconType.Checksum
            };
            MenuSkipHash.Click += MenuSkipHash_Invoked;
            menu.Items.Add(MenuSkipHash);

            MenuDownloadInstaller = new BetterMenuItem
            {
                Text = CoreTools.AutoTranslated("Download installer"),
                IconName = IconType.Download
            };
            MenuDownloadInstaller.Click += (_, _) => _ = MainApp.Operations.AskLocationAndDownload(SelectedItem, TEL_InstallReferral.FROM_BUNDLE);
            menu.Items.Add(MenuDownloadInstaller);

            menu.Items.Add(new MenuFlyoutSeparator());

            BetterMenuItem menuRemoveFromList = new()
            {
                Text = CoreTools.AutoTranslated("Remove from list"),
                IconName = IconType.Delete
            };
            menuRemoveFromList.Click += MenuRemoveFromList_Invoked;
            menu.Items.Add(menuRemoveFromList);
            menu.Items.Add(new MenuFlyoutSeparator());

            MenuShare = new()
            {
                Text = CoreTools.AutoTranslated("Share this package"),
                IconName = IconType.Share
            };
            MenuShare.Click += MenuShare_Invoked;
            menu.Items.Add(MenuShare);

            MenuDetails = new()
            {
                Text = CoreTools.AutoTranslated("Package details"),
                IconName = IconType.Info_Round,
                KeyboardAcceleratorTextOverride = "Enter"
            };
            MenuDetails.Click += MenuDetails_Invoked;
            menu.Items.Add(MenuDetails);

            return menu;
        }

        public override void GenerateToolBar()
        {
            AppBarButton OpenBundle = new();
            AppBarButton NewBundle = new();

            BetterMenuItem InstallAsAdmin = new();
            BetterMenuItem InstallSkipHash = new();
            BetterMenuItem InstallInteractive = new();
            BetterMenuItem DownloadInstallers = new();

            MainToolbarButtonDropdown.Flyout = new BetterMenu()
            {
                Items =
                {
                    InstallAsAdmin,
                    InstallSkipHash,
                    InstallInteractive,
                    new MenuFlyoutSeparator(),
                    DownloadInstallers,
                },
                Placement = FlyoutPlacementMode.Bottom
            };
            MainToolbarButtonIcon.Icon = IconType.Download;
            MainToolbarButtonText.Text = CoreTools.Translate("Install selection");

            AppBarButton RemoveSelected = new();
            AppBarButton SaveBundle = new();
            AppBarButton AddPackagesToBundle = new();
            AppBarButton PackageDetails = new();
            AppBarButton SharePackage = new();
            AppBarButton HelpButton = new();

            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(NewBundle);
            ToolBar.PrimaryCommands.Add(OpenBundle);
            ToolBar.PrimaryCommands.Add(SaveBundle);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(AddPackagesToBundle);
            ToolBar.PrimaryCommands.Add(RemoveSelected);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(PackageDetails);
            ToolBar.PrimaryCommands.Add(SharePackage);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(HelpButton);

            Dictionary<DependencyObject, string> Labels = new()
            { // Entries with a trailing space are collapsed
              // Their texts will be used as the tooltip
                { NewBundle,           CoreTools.Translate("New") },
                { InstallAsAdmin,      CoreTools.Translate("Install as administrator") },
                { InstallInteractive,  CoreTools.Translate("Interactive installation") },
                { InstallSkipHash,     CoreTools.Translate("Skip integrity checks") },
                { DownloadInstallers,  CoreTools.Translate("Download selected installers") },
                { OpenBundle,          CoreTools.Translate("Open") },
                { RemoveSelected,      CoreTools.Translate("Remove selection from bundle") },
                { SaveBundle,          CoreTools.Translate("Save as") },
                { AddPackagesToBundle, CoreTools.Translate("Add packages to bundle") },
                { PackageDetails,      " " + CoreTools.Translate("Package details") },
                { SharePackage,        " " + CoreTools.Translate("Share") },
                { HelpButton,          CoreTools.Translate("Help") }
            };

            Dictionary<DependencyObject, IconType> Icons = new()
            {
                { NewBundle,           IconType.AddTo },
                { InstallAsAdmin,      IconType.UAC },
                { InstallInteractive,  IconType.Interactive },
                { InstallSkipHash,     IconType.Checksum },
                { DownloadInstallers,  IconType.Download },
                { OpenBundle,          IconType.OpenFolder },
                { RemoveSelected,      IconType.Delete},
                { SaveBundle,          IconType.SaveAs },
                { AddPackagesToBundle, IconType.AddTo },
                { PackageDetails,      IconType.Info_Round },
                { SharePackage,        IconType.Share },
                { HelpButton,          IconType.Help }
            };

            ApplyTextAndIconsToToolbar(Labels, Icons);

            PackageDetails.Click += (_, _) =>
            {
                if (SelectedItem is null)
                    return;

                if (SelectedItem.Source.IsVirtualManager || SelectedItem is InvalidImportedPackage)
                {
                    DialogHelper.ShowDismissableBalloon(
                        CoreTools.Translate("Something went wrong"),
                        CoreTools.Translate("\"{0}\" is a local package and can't be shared", SelectedItem.Name)
                    );
                    return;
                }

                _ = DialogHelper.ShowPackageDetails(SelectedItem, OperationType.None, TEL_InstallReferral.FROM_BUNDLE);
            };

            HelpButton.Click += (_, _) => { MainApp.Instance.MainWindow.NavigationPage.ShowHelp(); };
            NewBundle.Click += async (s, e) => await AskForNewBundle();

            RemoveSelected.Click += (_, _) =>
            {
                HasUnsavedChanges = true;
                PEInterface.PackageBundlesLoader.RemoveRange(FilteredPackages.GetCheckedPackages());
            };

            IReadOnlyList<IPackage> GetCheckedNonInstalledPackages()
            {
                if(Settings.Get(Settings.K.InstallInstalledPackagesBundlesPage))
                    return FilteredPackages.GetCheckedPackages().ToList();
                else
                    return FilteredPackages.GetCheckedPackages().Where(p => p.Tag is not PackageTag.AlreadyInstalled).ToList();
            }

            MainToolbarButton.Click += async (_, _) => await ImportAndInstallPackage(GetCheckedNonInstalledPackages());
            InstallSkipHash.Click += async (_, _) => await ImportAndInstallPackage(GetCheckedNonInstalledPackages(), skiphash: true);
            InstallInteractive.Click += async (_, _) => await ImportAndInstallPackage(GetCheckedNonInstalledPackages(), interactive: true);
            InstallAsAdmin.Click += async (_, _) => await ImportAndInstallPackage(GetCheckedNonInstalledPackages(), elevated: true);
            DownloadInstallers.Click += (_, _) => _ = MainApp.Operations.Download(FilteredPackages.GetCheckedPackages(), TEL_InstallReferral.FROM_BUNDLE);
            OpenBundle.Click += async (_, _) => await AskOpenFromFile();
            SaveBundle.Click += async (_, _) => await SaveFile();

            SharePackage.Click += (_, _) =>
            {
                IPackage? package = SelectedItem;
                if (package is not null) DialogHelper.SharePackage(package);
            };

            AddPackagesToBundle.Click += (_, _) => _ = DialogHelper.HowToAddPackagesToBundle();
        }

        public async Task<bool> AskForNewBundle()
        {
            if (!Loader.Any() || !HasUnsavedChanges || await DialogHelper.AskLoseChangesAndCreateBundle())
            {
                // Need to call ClearPackages, this method also clears internal caches
                Loader.ClearPackages();
                HasUnsavedChanges = false;
                return true;
            }

            return false;
        }

        public async Task ImportAndInstallPackage(IReadOnlyList<IPackage> packages, bool? elevated = null, bool? interactive = null, bool? skiphash = null)
        {
            int loadingId = DialogHelper.ShowLoadingDialog(CoreTools.Translate("Preparing packages, please wait..."));
            List<Package> packages_to_install = [];
            foreach (IPackage package in packages)
            {
                if (package is ImportedPackage imported)
                {
                    Logger.ImportantInfo($"Registering package {imported.Id} from manager {imported.Source.AsString}");
                    packages_to_install.Add(await imported.RegisterAndGetPackageAsync());
                }
                else
                {
                    Logger.Warn($"Attempted to install an invalid/incompatible package with Id={package.Id}");
                }
            }

            DialogHelper.HideLoadingDialog(loadingId);
            MainApp.Operations.Install(packages_to_install, TEL_InstallReferral.FROM_BUNDLE, elevated, interactive, skiphash);
        }

        protected override void WhenPackageCountUpdated()
        { }

        protected override void WhenPackagesLoaded(ReloadReason reason)
        { }

        protected override void WhenShowingContextMenu(IPackage package)
        {
            if (MenuAsAdmin is null
                || MenuInteractive is null
                || MenuSkipHash is null
                || MenuDetails is null
                || MenuShare is null
                || MenuInstall is null
                || MenuInstallOptions is null
                || MenuDownloadInstaller is null)
            {
                Logger.Error("Menu items are null on InstalledPackagesTab");
                return;
            }

            bool IS_VALID = package as InvalidImportedPackage is null;

            MenuAsAdmin.IsEnabled = IS_VALID && package.Manager.Capabilities.CanRunAsAdmin;
            MenuInteractive.IsEnabled = IS_VALID && package.Manager.Capabilities.CanRunInteractively;
            MenuSkipHash.IsEnabled = IS_VALID && package.Manager.Capabilities.CanSkipIntegrityChecks;
            MenuDetails.IsEnabled = IS_VALID;
            MenuShare.IsEnabled = IS_VALID;
            MenuInstall.IsEnabled = IS_VALID;
            MenuInstallOptions.IsEnabled = IS_VALID;
            MenuDownloadInstaller.IsEnabled = IS_VALID && package.Manager.Capabilities.CanDownloadInstaller;
        }

        private void MenuInstall_Invoked(object sender, RoutedEventArgs args)
        {
            if (SelectedItem is null) return;
            _ = ImportAndInstallPackage([SelectedItem]);
        }

        private void MenuAsAdmin_Invoked(object sender, RoutedEventArgs args)
        {
            if (SelectedItem is null) return;
            _ = ImportAndInstallPackage([SelectedItem], elevated: true);
        }

        private void MenuInteractive_Invoked(object sender, RoutedEventArgs args)
        {
            if (SelectedItem is null) return;
            _ = ImportAndInstallPackage([SelectedItem], interactive: true);
        }

        private void MenuSkipHash_Invoked(object sender, RoutedEventArgs args)
        {
            if (SelectedItem is null) return;
            _ = ImportAndInstallPackage([SelectedItem], skiphash: true);
        }

        private void MenuShare_Invoked(object sender, RoutedEventArgs args)
        {
            if (SelectedItem is null) return;
            DialogHelper.SharePackage(SelectedItem);
        }

        private void MenuDetails_Invoked(object sender, RoutedEventArgs args)
        {
            ShowDetailsForPackage(SelectedItem, TEL_InstallReferral.FROM_BUNDLE);
        }

        private void MenuInstallSettings_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package is ImportedPackage imported)
            {
                HasUnsavedChanges = true;
                _ = DialogHelper.ShowInstallOptions_ImportedPackage(imported);
            }
        }

        private void MenuRemoveFromList_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package is null) return;

            HasUnsavedChanges = true;
            Loader.Remove(package);
        }

        public async Task OpenFromString(string payload, BundleFormatType format, string source, int? loadingId)
        {
            if (await AskForNewBundle() is false)
                return;

            loadingId ??= DialogHelper.ShowLoadingDialog(CoreTools.Translate("Loading packages, please wait..."));

            var (open_version, report) = await AddFromBundle(payload, format);
            TelemetryHandler.ImportBundle(format);
            HasUnsavedChanges = false;

            if ((int)(open_version*10) != (int)(SerializableBundle.ExpectedVersion*10))
            {   // Check only up to first decimal digit, prevent floating point precision error.
                Logger.Warn($"The loaded bundle \"{source}\" is based on schema version {open_version}, " +
                            $"while this UniGetUI build expects version {SerializableBundle.ExpectedVersion}." +
                            $"\nThis should not be a problem if packages show up, but be careful");
            }

            DialogHelper.HideLoadingDialog(loadingId.Value);
            if (!report.IsEmpty)
            {
                await DialogHelper.ShowBundleSecurityReport(report.Contents);
            }
        }

        public async Task OpenFromFile(string file)
        {
            int loadingId = DialogHelper.ShowLoadingDialog(CoreTools.Translate("Loading packages, please wait..."));
            try
            {
                BundleFormatType formatType;
                string EXT = file.Split('.')[^1].ToLower();
                if (EXT == "yaml")
                    formatType = BundleFormatType.YAML;
                else if (EXT == "xml")
                    formatType = BundleFormatType.XML;
                else if (EXT == "json")
                    formatType = BundleFormatType.JSON;
                else if (EXT == "ubundle")
                    formatType = BundleFormatType.UBUNDLE;
                else
                    formatType = BundleFormatType.UBUNDLE;

                string fileContent = await File.ReadAllTextAsync(file);
                await OpenFromString(fileContent, formatType, file, loadingId);
            }
            catch (Exception ex)
            {

                Logger.Error("An error occurred while attempting to open a bundle");
                Logger.Error(ex);
                var warningDialog = new ContentDialog
                {
                    Title = CoreTools.Translate("The package bundle is not valid"),
                    Content = CoreTools.Translate("The bundle you are trying to load appears to be invalid. Please check the file and try again.") + "\n\n" + ex.Message,
                    CloseButtonText = CoreTools.Translate("Ok"),
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = MainApp.Instance.MainWindow.Content.XamlRoot // Ensure the dialog is shown in the correct context
                };

                DialogHelper.HideLoadingDialog(loadingId);
                await DialogHelper.ShowDialogAsync(warningDialog);
            }
        }

        public async Task AskOpenFromFile()
        {
            if (await AskForNewBundle() is false)
                return;

            FileOpenPicker picker = new(MainApp.Instance.MainWindow.GetWindowHandle());
            string file = picker.Show(["*.ubundle", "*.json", "*.yaml", "*.xml"]);
            if (file == String.Empty)
                return;

            await OpenFromFile(file);
        }

        public async Task SaveFile()
        {
            try
            {
                // Get file
                string defaultName = CoreTools.Translate("Package bundle") + ".ubundle";
                string file = (new FileSavePicker(MainApp.Instance.MainWindow.GetWindowHandle())).Show(["*.ubundle", "*.json"], defaultName);
                if (file != String.Empty)
                {
                    // Loading dialog
                    int loadingId = DialogHelper.ShowLoadingDialog(CoreTools.Translate("Saving packages, please wait..."));

                    // Select appropriate format
                    BundleFormatType formatType;
                    string EXT = file.Split('.')[^1].ToLower();
                    if (EXT == "json")
                        formatType = BundleFormatType.JSON;
                    else if (EXT == "ubundle")
                        formatType = BundleFormatType.UBUNDLE;
                    else
                        formatType = BundleFormatType.UBUNDLE;

                    // Save serialized data
                    await File.WriteAllTextAsync(file, await CreateBundle(Loader.Packages));
                    TelemetryHandler.ExportBundle(formatType);

                    DialogHelper.HideLoadingDialog(loadingId);

                    // Launch file
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = @$"/select, ""{file}"""
                    });

                    HasUnsavedChanges = false;

                }
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred when saving packages to a file");
                Logger.Error(ex);

                var warningDialog = new ContentDialog
                {
                    Title = CoreTools.Translate("Could not create bundle"),
                    Content = CoreTools.Translate("The package bundle could not be created due to an error.") + "\n\n" + ex.Message,
                    CloseButtonText = CoreTools.Translate("Ok"),
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = MainApp.Instance.MainWindow.Content.XamlRoot // Ensure the dialog is shown in the correct context
                };

                DialogHelper.HideAllLoadingDialogs();
                await DialogHelper.ShowDialogAsync(warningDialog);
            }
        }

        public static async Task<string> CreateBundle(IReadOnlyList<IPackage> unsorted_packages)
        {
            SerializableBundle exportableData = new();

            List<IPackage> packages = unsorted_packages.ToList();
            packages.Sort(Comparison);

            static int Comparison(IPackage x, IPackage y)
            {
                if (x.Id != y.Id) return String.Compare(x.Id, y.Id, StringComparison.Ordinal);
                if (x.Name != y.Name) return String.Compare(x.Name, y.Name, StringComparison.Ordinal);
                return (x.NormalizedVersion > y.NormalizedVersion) ? -1 : 1;
            }

            foreach (IPackage package in packages)
            {
                if (package is Package && !package.Source.IsVirtualManager)
                    exportableData.packages.Add(await package.AsSerializableAsync());
                else
                    exportableData.incompatible_packages.Add(package.AsSerializable_Incompatible());
            }

            Logger.Debug("Finished loading serializable objects.");
            string exportablePayload = exportableData.AsJsonString();
            Logger.Debug("Serialization finished successfully");
            return exportablePayload;
        }

        public async Task<(double, BundleReport)> AddFromBundle(string content, BundleFormatType format)
        {
            // Deserialize data
            SerializableBundle? DeserializedData;

            if (format is BundleFormatType.YAML)
            {
                // Dynamic convert to JSON
                content = await SerializationHelpers.YAML_to_JSON(content);
                Logger.ImportantInfo("YAML bundle was converted to JSON before deserialization");
            }

            if (format is BundleFormatType.XML)
            {
                // Dynamic convert to JSON
                content = await SerializationHelpers.XML_to_JSON(content);
                Logger.ImportantInfo("XML payload was converted to JSON dynamically before deserialization");
            }

            DeserializedData = await Task.Run(() =>
            {
                return new SerializableBundle(JsonNode.Parse(content) ?? throw new JsonException("Could not parse JSON object"));
            });

            List<IPackage> packages = [];

            var report = new BundleReport();
            report.IsEmpty = true;

            bool AllowCLIParameters =
                SecureSettings.Get(SecureSettings.K.AllowCLIArguments) &&
                SecureSettings.Get(SecureSettings.K.AllowImportingCLIArguments);

            bool AllowPrePostOps =
                SecureSettings.Get(SecureSettings.K.AllowPrePostOpCommand) &&
                SecureSettings.Get(SecureSettings.K.AllowImportPrePostOpCommands);


            foreach (var pkg in DeserializedData.packages)
            {
                var opts = pkg.InstallationOptions;

                if (opts.CustomParameters_Install.Where(x => x.Any()).Any())
                {
                    report.IsEmpty = false;
                    if (!report.Contents.ContainsKey(pkg.Id)) report.Contents[pkg.Id] = new();
                    report.Contents[pkg.Id].Add(new($"Custom install arguments: [{string.Join(", ", opts.CustomParameters_Install)}]", AllowCLIParameters));
                    if(!AllowCLIParameters) opts.CustomParameters_Install.Clear();
                }
                if (opts.CustomParameters_Update.Where(x => x.Any()).Any())
                {
                    report.IsEmpty = false;
                    if (!report.Contents.ContainsKey(pkg.Id)) report.Contents[pkg.Id] = new();
                    report.Contents[pkg.Id].Add(new($"Custom update arguments: [{string.Join(", ", opts.CustomParameters_Update)}]", AllowCLIParameters));
                    if(!AllowCLIParameters) opts.CustomParameters_Update.Clear();
                }
                if (opts.CustomParameters_Uninstall.Where(x => x.Any()).Any())
                {
                    report.IsEmpty = false;
                    if (!report.Contents.ContainsKey(pkg.Id)) report.Contents[pkg.Id] = new();
                    report.Contents[pkg.Id].Add(new($"Custom uninstall arguments: [{string.Join(", ", opts.CustomParameters_Uninstall)}]", AllowCLIParameters));
                    if(!AllowCLIParameters) opts.CustomParameters_Uninstall.Clear();
                }

                if (opts.PreInstallCommand.Any())
                {
                    report.IsEmpty = false;
                    if (!report.Contents.ContainsKey(pkg.Id)) report.Contents[pkg.Id] = new();
                    report.Contents[pkg.Id].Add(new($"Pre-install command: {opts.PreInstallCommand}", AllowPrePostOps));
                    if (!AllowPrePostOps) opts.PreInstallCommand = "";
                }
                if (opts.PostInstallCommand.Any())
                {
                    report.IsEmpty = false;
                    if (!report.Contents.ContainsKey(pkg.Id)) report.Contents[pkg.Id] = new();
                    report.Contents[pkg.Id].Add(new($"Post-install command: {opts.PostInstallCommand}", AllowPrePostOps));
                    if (!AllowPrePostOps) opts.PostInstallCommand = "";
                }
                if (opts.PreUpdateCommand.Any())
                {
                    report.IsEmpty = false;
                    if (!report.Contents.ContainsKey(pkg.Id)) report.Contents[pkg.Id] = new();
                    report.Contents[pkg.Id].Add(new($"Pre-update command: {opts.PreUpdateCommand}", AllowPrePostOps));
                    if (!AllowPrePostOps) opts.PreUpdateCommand = "";
                }
                if (opts.PostUpdateCommand.Any())
                {
                    report.IsEmpty = false;
                    if (!report.Contents.ContainsKey(pkg.Id)) report.Contents[pkg.Id] = new();
                    report.Contents[pkg.Id].Add(new($"Post-update command: {opts.PostUpdateCommand}", AllowPrePostOps));
                    if (!AllowPrePostOps) opts.PostUpdateCommand = "";
                }
                if (opts.PreUninstallCommand.Any())
                {
                    report.IsEmpty = false;
                    if (!report.Contents.ContainsKey(pkg.Id)) report.Contents[pkg.Id] = new();
                    report.Contents[pkg.Id].Add(new($"Pre-uninstall command: {opts.PreUninstallCommand}", AllowPrePostOps));
                    if (!AllowPrePostOps) opts.PreUninstallCommand = "";
                }
                if (opts.PostUninstallCommand.Any())
                {
                    report.IsEmpty = false;
                    if (!report.Contents.ContainsKey(pkg.Id)) report.Contents[pkg.Id] = new();
                    report.Contents[pkg.Id].Add(new($"Post-uninstall command: {opts.PostUninstallCommand}", AllowPrePostOps));
                    if (!AllowPrePostOps) opts.PostUninstallCommand = "";
                }

                pkg.InstallationOptions = opts;
                packages.Add(DeserializePackage(pkg));
            }

            foreach (var pkg in DeserializedData.incompatible_packages)
                packages.Add(DeserializeIncompatiblePackage(pkg, NullSource.Instance));

            await PEInterface.PackageBundlesLoader.AddPackagesAsync(packages);

            return (DeserializedData.export_version, report);
        }

        public static IPackage DeserializePackage(SerializablePackage raw_package)
        {
            IPackageManager? manager = null;
            IManagerSource? source;

            foreach (var possible_manager in PEInterface.Managers)
            {
                if (possible_manager.Name == raw_package.ManagerName || possible_manager.DisplayName == raw_package.ManagerName)
                {
                    manager = possible_manager;
                    break;
                }
            }

            if (manager?.Capabilities.SupportsCustomSources == true)
            {
                if (raw_package.Source.Contains(": ")) // Add compatibility with previons 2.0 bundles
                    // where SourceName is $"{ManagerName}: {SourceName}"
                    raw_package.Source = raw_package.Source.Split(": ")[^1];

                source = manager?.SourcesHelper?.Factory.GetSourceIfExists(raw_package.Source);
            }
            else
                source = manager?.DefaultSource;

            if (manager is null || source is null)
            {
                return DeserializeIncompatiblePackage(raw_package.GetInvalidEquivalent(), NullSource.Instance);
            }

            return new ImportedPackage(raw_package, manager, source);
        }

        public static IPackage DeserializeIncompatiblePackage(SerializableIncompatiblePackage raw_package, IManagerSource source)
        {
            return new InvalidImportedPackage(raw_package, source);
        }
    }
}

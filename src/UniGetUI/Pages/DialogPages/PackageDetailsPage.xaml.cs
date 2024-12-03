using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Documents;
using Windows.UI.Text;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Managers.PowerShellManager;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Widgets;
using UniGetUI.Pages.DialogPages;
using FileSavePicker = Windows.Storage.Pickers.FileSavePicker;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PackageDetailsPage : Page
    {
        public IPackage Package;
        public IPackage? AvailablePackage;
        public IPackage? UpgradablePackage;
        public IPackage? InstalledPackage;


        private readonly InstallOptionsPage InstallOptionsPage;
        public event EventHandler? Close;
        private readonly OperationType OperationRole;

        private bool PackageHasScreenshots;
        public ObservableCollection<TextBlock> ShowableTags = [];

        private enum LayoutMode
        {
            Normal,
            Wide,
            Unset
        }

        private LayoutMode __layout_mode = LayoutMode.Unset;
        public PackageDetailsPage(IPackage package, OperationType role)
        {
            if (role == OperationType.None)
                role = OperationType.Install;

            OperationRole = role;
            Package = package;

            InitializeComponent();
            SizeChanged += PackageDetailsPage_SizeChanged;

            PackageName.Text = package.Name;
            LoadingIndicator.Visibility = Visibility.Visible;
            string LoadingString = CoreTools.Translate("Loading...");

            // Basic details section
            SetTextToItem(DescriptionContent, LoadingString);
            SetTextToItem(HomepageUrl_Label, CoreTools.Translate("Homepage") + ": ");
            SetTextToItem(HomepageUrl_Content, LoadingString);
            SetTextToItem(Author_Label, CoreTools.Translate("Author") + ": ");
            SetTextToItem(Author_Content, LoadingString);
            SetTextToItem(Publisher_Label, CoreTools.Translate("Publisher") + ": ");
            SetTextToItem(Publisher_Content, LoadingString);
            SetTextToItem(License_Label, CoreTools.Translate("License") + ": ");
            SetTextToItem(License_Content_Text, LoadingString);
            SetTextToItem(License_Content_Uri, "");
            SetTextToItem(Source_Label, CoreTools.Translate("Package Manager") + ": ");
            SetTextToItem(Source_Content, Package.Source.AsString_DisplayName);

            // Extended details section
            SetTextToItem(PackageId_Label, CoreTools.Translate("Package ID") + ": ");
            SetTextToItem(PackageId_Content, package.Id);
            SetTextToItem(ManifestUrl_Label, CoreTools.Translate("Manifest") + ": ");
            SetTextToItem(ManifestUrl_Content, LoadingString);

            SetTextToItem(InstallerType_Label, CoreTools.Translate("Installer Type") + ": ");
            SetTextToItem(InstallerType_Content, LoadingString);
            SetTextToItem(InstallerHash_Label, CoreTools.Translate("Installer SHA256") + ": ");
            SetTextToItem(InstallerHash_Content, LoadingString);
            SetTextToItem(InstallerUrl_Label, CoreTools.Translate("Installer URL") + ": ");
            SetTextToItem(InstallerUrl_Content, LoadingString);
            DownloadInstaller_Button.Click += DownloadInstallerButton_Click;
            SetTextToItem(DownloadInstaller_Button, CoreTools.Translate("Download installer"));
            SetTextToItem(UpdateDate_Label, CoreTools.Translate("Last updated:") + " ");
            SetTextToItem(UpdateDate_Content, LoadingString);
            SetTextToItem(ReleaseNotes_Label, CoreTools.Translate("Release notes") + ": ");
            SetTextToItem(ReleaseNotes_Content, LoadingString);
            SetTextToItem(ReleaseNotesUrl_Label, CoreTools.Translate("Release notes URL") + ": ");
            SetTextToItem(ReleaseNotesUrl_Content, LoadingString);

            AvailablePackage = Package.GetAvailablePackage();
            UpgradablePackage = Package.GetUpgradablePackage();
            InstalledPackage = UpgradablePackage?.GetInstalledPackage() ?? Package.GetInstalledPackage();

            var options = InstallationOptions.FromPackage(package).AsSerializable();
            InstallOptionsPage = new InstallOptionsPage(package, OperationRole, options);
            InstallOptionsExpander.Content = InstallOptionsPage;

            if (OperationRole is OperationType.Install)
            {
                MainActionButton.Content = CoreTools.Translate("Install");
                SetTextToItem(Version_Label, CoreTools.Translate("Version"));
                SetTextToItem(Version_Content, AvailablePackage?.Version ?? "NULL");
                SetUpActionButtonAsInstall();
            }
            else if (OperationRole is OperationType.Update)
            {
                MainActionButton.Content = CoreTools.Translate("Update to version {0}", UpgradablePackage?.NewVersion ?? "NULL");
                SetTextToItem(Version_Label, CoreTools.Translate("Installed Version"));
                SetTextToItem(Version_Content, (UpgradablePackage?.Version ?? "NULL")
                                               + $" - {CoreTools.Translate("Update to {0} available", UpgradablePackage?.NewVersion ?? "NULL")}");
                SetUpActionButtonAsUpdate();
            }
            else /* OperationRole is OperationType.Uninstall */
            {
                MainActionButton.Content = CoreTools.Translate("Uninstall");
                SetTextToItem(Version_Label, CoreTools.Translate("Installed Version"));
                SetTextToItem(Version_Content, InstalledPackage?.Version ?? "NULL");
                SetUpActionButtonAsUninstall();
            }
            _ = LoadInformation();
        }

        public void SetUpActionButtonAsInstall()
        {
            var AsAdmin = new BetterMenuItem()
            {
                Text = CoreTools.Translate("Install as administrator"),
                IconName = IconType.UAC,
                IsEnabled = Package.Manager.Capabilities.CanRunAsAdmin
            };
            var Interactive = new BetterMenuItem()
            {
                Text = CoreTools.Translate("Interactive installation"),
                IconName = IconType.Interactive,
                IsEnabled = Package.Manager.Capabilities.CanRunInteractively
            };
            var SkipHash = new BetterMenuItem()
            {
                Text = CoreTools.Translate("Skip hash check"),
                IconName = IconType.Checksum,
                IsEnabled = Package.Manager.Capabilities.CanSkipIntegrityChecks
            };

            AsAdmin.Click += (_, _) => DoAction(Package, OperationType.Install, AsAdmin: true);
            Interactive.Click += (_, _) => DoAction(Package, OperationType.Install, Interactive: true);
            SkipHash.Click += (_, _) => DoAction(Package, OperationType.Install, SkipHash: true);

            ExtendedActionsMenu.Items.Add(AsAdmin);
            ExtendedActionsMenu.Items.Add(Interactive);
            ExtendedActionsMenu.Items.Add(SkipHash);


            if (UpgradablePackage is not null)
            {
                ExtendedActionsMenu.Items.Add(new MenuFlyoutSeparator());
                var Upgrade = new BetterMenuItem()
                {
                    Text = CoreTools.Translate("Update to version {0}", UpgradablePackage.NewVersion),
                    IconName = IconType.Update
                };
                Upgrade.Click += (_, _) => DoAction(UpgradablePackage, OperationType.Update);
                ExtendedActionsMenu.Items.Add(Upgrade);
            }

            if (InstalledPackage is not null)
            {
                ExtendedActionsMenu.Items.Add(new MenuFlyoutSeparator());
                var Uninstall = new BetterMenuItem()
                {
                    Text = CoreTools.Translate("Uninstall"),
                    IconName = IconType.Delete
                };
                Uninstall.Click += (_, _) => DoAction(InstalledPackage, OperationType.Uninstall);
                ExtendedActionsMenu.Items.Add(Uninstall);
            }
        }

        public void SetUpActionButtonAsUpdate()
        {
            var AsAdmin = new BetterMenuItem()
            {
                Text = CoreTools.Translate("Update as administrator"),
                IconName = IconType.UAC,
                IsEnabled = Package.Manager.Capabilities.CanRunAsAdmin
            };
            var Interactive = new BetterMenuItem()
            {
                Text = CoreTools.Translate("Interactive update"),
                IconName = IconType.Interactive,
                IsEnabled = Package.Manager.Capabilities.CanRunInteractively
            };
            var SkipHash = new BetterMenuItem()
            {
                Text = CoreTools.Translate("Skip hash check"),
                IconName = IconType.Checksum,
                IsEnabled = Package.Manager.Capabilities.CanSkipIntegrityChecks
            };

            AsAdmin.Click += (_, _) => DoAction(Package, OperationType.Update, AsAdmin: true);
            Interactive.Click += (_, _) => DoAction(Package, OperationType.Update, Interactive: true);
            SkipHash.Click += (_, _) => DoAction(Package, OperationType.Update, SkipHash: true);

            ExtendedActionsMenu.Items.Add(AsAdmin);
            ExtendedActionsMenu.Items.Add(Interactive);
            ExtendedActionsMenu.Items.Add(SkipHash);

            if (InstalledPackage is not null)
            {
                ExtendedActionsMenu.Items.Add(new MenuFlyoutSeparator());
                var Uninstall = new BetterMenuItem()
                {
                    Text = CoreTools.Translate("Uninstall"), IconName = IconType.Delete
                };
                Uninstall.Click += (_, _) => DoAction(InstalledPackage, OperationType.Uninstall);
                ExtendedActionsMenu.Items.Add(Uninstall);
            }

            ExtendedActionsMenu.Items.Add(new MenuFlyoutSeparator());
            var Reinstall = new BetterMenuItem()
            {
                Text = CoreTools.Translate("Reinstall"),
                IconName = IconType.Download
            };
            Reinstall.Click += (_, _) => DoAction(Package, OperationType.Install);
            ExtendedActionsMenu.Items.Add(Reinstall);
        }

        public void SetUpActionButtonAsUninstall()
        {
            var AsAdmin = new BetterMenuItem()
            {
                Text = CoreTools.Translate("Uninstall as administrator"),
                IconName = IconType.UAC,
                IsEnabled = Package.Manager.Capabilities.CanRunAsAdmin
            };
            var Interactive = new BetterMenuItem()
            {
                Text = CoreTools.Translate("Interactive uninstall"),
                IconName = IconType.Interactive,
                IsEnabled = Package.Manager.Capabilities.CanRunInteractively
            };
            var RemoveData = new BetterMenuItem()
            {
                Text = CoreTools.Translate("Uninstall and remove data"),
                IconName = IconType.Close_Round,
                IsEnabled = Package.Manager.Capabilities.CanRemoveDataOnUninstall
            };

            AsAdmin.Click += (_, _) => DoAction(Package, OperationType.Uninstall, AsAdmin: true);
            Interactive.Click += (_, _) => DoAction(Package, OperationType.Uninstall, Interactive: true);
            RemoveData.Click += (_, _) => DoAction(Package, OperationType.Uninstall, RemoveData: true);

            ExtendedActionsMenu.Items.Add(AsAdmin);
            ExtendedActionsMenu.Items.Add(Interactive);
            ExtendedActionsMenu.Items.Add(RemoveData);

            if (UpgradablePackage is not null)
            {
                ExtendedActionsMenu.Items.Add(new MenuFlyoutSeparator());
                var Upgrade = new BetterMenuItem()
                {
                    Text = CoreTools.Translate("Update to version {0}", UpgradablePackage.NewVersion),
                    IconName = IconType.Update
                };
                Upgrade.Click += (_, _) => DoAction(UpgradablePackage, OperationType.Update);
                ExtendedActionsMenu.Items.Add(Upgrade);
            }

            ExtendedActionsMenu.Items.Add(new MenuFlyoutSeparator());
            var Reinstall = new BetterMenuItem()
            {
                Text = CoreTools.Translate("Reinstall"), IconName = IconType.Download
            };
            Reinstall.Click += (_, _) => DoAction(Package, OperationType.Install);
            ExtendedActionsMenu.Items.Add(Reinstall);
        }

        public async Task LoadInformation()
        {
            LoadingIndicator.Visibility = Visibility.Visible;

            LoadIcon();
            LoadScreenshots();

            IPackageDetails details = Package.Details;
            if (!details.IsPopulated)
            {
                await details.Load();
            }

            CommandTextBlock.Text = Package.Manager.Properties.ExecutableFriendlyName + " " +
                string.Join(' ', Package.Manager.OperationHelper.GetParameters(Package, await InstallationOptions.FromPackageAsync(Package), OperationRole));

            LoadingIndicator.Visibility = Visibility.Collapsed;

            // Basic details section
            SetTextToItem(DescriptionContent, details.Description);
            SetTextToItem(HomepageUrl_Content, details.HomepageUrl);
            SetTextToItem(Author_Content, details.Author);
            SetTextToItem(Publisher_Content, details.Publisher);

            if (details.License is not null && details.LicenseUrl is not null)
            {
                SetTextToItem(License_Content_Text, details.License);
                SetTextToItem(License_Content_Uri, details.LicenseUrl, "(", ")");
            }
            else if (details.License is not null && details.LicenseUrl is null)
            {
                SetTextToItem(License_Content_Text, details.License);
                SetTextToItem(License_Content_Uri, "");
            }
            else if (details.License is null && details.LicenseUrl is not null)
            {
                SetTextToItem(License_Content_Text, "");
                SetTextToItem(License_Content_Uri, details.LicenseUrl);
            }
            else
            {
                SetTextToItem(License_Content_Text, null);
                SetTextToItem(License_Content_Uri, details.LicenseUrl);
            }

            // Extended details section
            SetTextToItem(ManifestUrl_Content, details.ManifestUrl);
            if (Package.Manager == PEInterface.Chocolatey)
            {
                SetTextToItem(InstallerHash_Label, CoreTools.Translate("Installer SHA512") + ": ");
            }
            else
            {
                SetTextToItem(InstallerHash_Label, CoreTools.Translate("Installer SHA256") + ": ");
            }

            SetTextToItem(InstallerHash_Content, details.InstallerHash);
            if (details.InstallerUrl is not null)
            {
                SetTextToItem(InstallerSize_Content, details.InstallerSize > 0 ? $" ({details.InstallerSize} MB)" : $" ({CoreTools.Translate("Unknown size")})");
                SetTextToItem(DownloadInstaller_Button, CoreTools.Translate("Download installer"));
            }
            else
            {
                SetTextToItem(InstallerSize_Content, "");
                SetTextToItem(DownloadInstaller_Button, CoreTools.Translate("Installer not available"));
            }
            SetTextToItem(InstallerUrl_Content, details.InstallerUrl);
            SetTextToItem(InstallerType_Content, details.InstallerType);
            SetTextToItem(UpdateDate_Content, details.UpdateDate);
            SetTextToItem(ReleaseNotes_Content, details.ReleaseNotes);
            SetTextToItem(ReleaseNotesUrl_Content, details.ReleaseNotesUrl);

            ShowableTags.Clear();
            foreach (string tag in details.Tags)
            {
                ShowableTags.Add(new TextBlock
                {
                    Text = tag,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextLineBounds = TextLineBounds.Tight
                });
            }
        }

        public void SetTextToItem(Run r, string? s)
        {
            if (s is null)
            {
                r.Text = CoreTools.Translate("Not available");
                r.Foreground = new SolidColorBrush(color: Color.FromArgb(255, 127, 127, 127));
            }
            else
            {
                r.Text = s;
                r.ClearValue(Run.ForegroundProperty);
            }
        }

        public void SetTextToItem(Hyperlink h, Uri? u, string prefix = "", string suffix = "")
        {
            if (u is null)
            {
                h.Inlines.Clear();
                h.Inlines.Add(new Run()
                {
                    Text = CoreTools.Translate("Not available"),
                    TextDecorations = TextDecorations.None,
                    Foreground = new SolidColorBrush(color: Color.FromArgb(255, 127, 127, 127))
                });
                h.NavigateUri = u;
            }
            else
            {
                h.Inlines.Clear();
                h.Inlines.Add(new Run { Text = prefix + u.ToString() + suffix });
                h.NavigateUri = u;
            }
        }
        public void SetTextToItem(Hyperlink h, string s)
        {
            h.Inlines.Clear();
            h.Inlines.Add(new Run { Text = s });
            h.NavigateUri = new Uri("about:blank");
        }

        public async void LoadIcon()
        {
            PackageIcon.Source = new BitmapImage
            {
                UriSource = await Task.Run(Package.GetIconUrl)
            };
        }

        public async void LoadScreenshots()
        {
            IEnumerable<Uri> screenshots = await Task.Run(Package.GetScreenshots);
            PackageHasScreenshots = screenshots.Any();
            if (PackageHasScreenshots)
            {
                PackageHasScreenshots = true;
                IconsExtraBanner.Visibility = Visibility.Visible;
                ScreenshotsCarroussel.Items.Clear();
                foreach (Uri image in screenshots)
                {
                    ScreenshotsCarroussel.Items.Add(new Image { Source = new BitmapImage(image) });
                }
            }

            __layout_mode = LayoutMode.Unset;
            PackageDetailsPage_SizeChanged();

        }

        public void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            MainApp.Instance.MainWindow.SharePackage(Package);
        }

        public async void DownloadInstallerButton_Click(object sender, RoutedEventArgs e)
        {
            bool running = true;
            try
            {
                if (Package.Details?.InstallerUrl is null)
                {
                    return;
                }

                FileSavePicker savePicker = new();
                MainWindow window = MainApp.Instance.MainWindow;
                IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);
                savePicker.SuggestedStartLocation = PickerLocationId.Downloads;
                string extension = Package.Manager is BaseNuGet
                    ? "nupkg"
                    : Package.Details.InstallerUrl.ToString().Split('.')[^1];
                savePicker.SuggestedFileName = Package.Id + " installer." + extension;

                if (Package.Details.InstallerUrl.ToString().Split('.')[^1] == "nupkg")
                {
                    savePicker.FileTypeChoices.Add("Compressed Manifest File", [".zip"]);
                }

                savePicker.FileTypeChoices.Add("Default", [$".{extension}"]);

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file is not null)
                {
                    Func<Task> loader = async () =>
                    {
                        List<string> texts = [
                            " .   ",
                            " ..  ",
                            " ... ",
                            "  ...",
                            "   ..",
                            "    ."];
                        int i = 0;
                        string baseString = CoreTools.Translate("Downloading installer for {package}", new Dictionary<string, object?> { { "package", Package.Name } });
                        while (running)
                        {
                            DownloadInstaller_Button.Inlines.Clear();
                            DownloadInstaller_Button.Inlines.Add(new Run { Text = baseString + " " + texts[i++ % 6] });
                            await Task.Delay(500);
                        }
                    };
                    _ = loader();

                    Logger.Debug($"Downloading installer ${file.Path}");

                    using HttpClient httpClient = new(CoreData.GenericHttpClientParameters);
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(CoreData.UserAgentString);
                    await using Stream s = await httpClient.GetStreamAsync(Package.Details.InstallerUrl);
                    await using FileStream fs = File.OpenWrite(file.Path);
                    await s.CopyToAsync(fs);
                    DownloadInstaller_Button.Inlines.Clear();
                    DownloadInstaller_Button.Inlines.Add(new Run { Text = CoreTools.Translate("Download installer") });
                    running = false;
                    Logger.ImportantInfo($"Installer for {Package.Id} has been downloaded successfully");
                    DialogHelper.HideLoadingDialog();
                    Process.Start("explorer.exe", "/select," + $"\"{file.Path}\"");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"An error occurred while downloading the installer for the package {Package.Id}");
                Logger.Error(ex);

                DownloadInstaller_Button.Inlines.Clear();
                DownloadInstaller_Button.Inlines.Add(new Run { Text = CoreTools.Translate("An error occurred") + ": " + ex.Message });
            }
        }

        public void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
        }

        public void PackageDetailsPage_SizeChanged(object? sender = null, SizeChangedEventArgs? e = null)
        {
            if (MainApp.Instance.MainWindow.AppWindow.Size.Width < 1050)
            {
                if (__layout_mode != LayoutMode.Normal)
                {
                    __layout_mode = LayoutMode.Normal;

                    MainGrid.ColumnDefinitions.Clear();
                    MainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    Grid.SetColumn(TitlePanel, 0);
                    Grid.SetColumn(BasicInfoPanelText, 0);
                    Grid.SetColumn(ScreenshotsPanel, 0);
                    Grid.SetColumn(ActionsPanel, 0);
                    Grid.SetColumn(InstallOptionsBorder, 0);
                    Grid.SetColumn(DetailsPanelText, 0);

                    MainGrid.RowDefinitions.Clear();
                    MainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    MainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    MainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    MainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    MainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    MainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    MainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    Grid.SetRow(TitlePanel, 0);
                    Grid.SetRow(DescriptionPanel, 1);
                    Grid.SetRow(BasicInfoPanelText, 2);
                    Grid.SetRow(ActionsPanel, 3);
                    Grid.SetRow(InstallOptionsBorder, 4);
                    Grid.SetRow(ScreenshotsPanel, 5);
                    Grid.SetRow(DetailsPanelText, 6);

                    LeftPanel.Children.Clear();
                    RightPanel.Children.Clear();
                    MainGrid.Children.Clear();
                    MainGrid.Children.Add(TitlePanel);
                    MainGrid.Children.Add(DescriptionPanel);
                    MainGrid.Children.Add(BasicInfoPanelText);
                    MainGrid.Children.Add(ScreenshotsPanel);
                    MainGrid.Children.Add(ActionsPanel);
                    MainGrid.Children.Add(InstallOptionsBorder);
                    MainGrid.Children.Add(DetailsPanelText);
                    ScreenshotsCarroussel.Height = PackageHasScreenshots ? 225 : 150;

                    InstallOptionsExpander.IsExpanded = false;

                }
            }
            else
            {
                if (__layout_mode != LayoutMode.Wide)
                {
                    __layout_mode = LayoutMode.Wide;

                    MainGrid.ColumnDefinitions.Clear();
                    MainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 550 });
                    MainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    Grid.SetColumn(LeftPanel, 0);
                    Grid.SetColumn(RightPanel, 1);
                    Grid.SetColumn(TitlePanel, 0);
                    Grid.SetColumnSpan(TitlePanel, 1);

                    MainGrid.RowDefinitions.Clear();
                    MainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    Grid.SetRow(LeftPanel, 0);
                    Grid.SetRow(RightPanel, 0);

                    LeftPanel.Children.Clear();
                    RightPanel.Children.Clear();
                    MainGrid.Children.Clear();

                    Grid.SetRow(TitlePanel, 0);
                    Grid.SetRow(DescriptionPanel, 1);
                    Grid.SetRow(BasicInfoPanelText, 2);
                    Grid.SetRow(ActionsPanel, 3);
                    Grid.SetRow(InstallOptionsBorder, 4);

                    LeftPanel.Children.Add(TitlePanel);
                    LeftPanel.Children.Add(DescriptionPanel);
                    LeftPanel.Children.Add(BasicInfoPanelText);
                    LeftPanel.Children.Add(ActionsPanel);
                    LeftPanel.Children.Add(InstallOptionsBorder);

                    Grid.SetRow(ScreenshotsPanel, 0);
                    Grid.SetRow(DetailsPanelText, 1);

                    RightPanel.Children.Add(ScreenshotsPanel);
                    RightPanel.Children.Add(DetailsPanelText);
                    ScreenshotsCarroussel.Height = PackageHasScreenshots ? 400 : 150;

                    InstallOptionsExpander.IsExpanded = true;

                    MainGrid.Children.Add(LeftPanel);
                    MainGrid.Children.Add(RightPanel);

                }
            }
        }

        public void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            DoAction(Package, OperationRole);
        }

        public async void DoAction(
            IPackage package,
            OperationType action,
            bool? AsAdmin = null,
            bool? Interactive = null,
            bool? SkipHash = null,
            bool? RemoveData = null)
        {
            Close?.Invoke(this, EventArgs.Empty);

            var newOptions = (await InstallationOptions.FromPackageAsync(package));
            newOptions.FromSerializable(await InstallOptionsPage.GetUpdatedOptions());
            newOptions.SaveToDisk();

            if (AsAdmin is not null) newOptions.RunAsAdministrator = (bool)AsAdmin;
            if (Interactive is not null) newOptions.InteractiveInstallation = (bool)Interactive;
            if (SkipHash is not null) newOptions.SkipHashCheck = (bool)SkipHash;
            if (RemoveData is not null) newOptions.RemoveDataOnUninstall = (bool)RemoveData;

            if (action is OperationType.Install)
            {
                MainApp.Instance.AddOperationToList(new InstallPackageOperation(package, newOptions));
            }
            else if (action is OperationType.Uninstall)
            {
                if (await DialogHelper.ConfirmUninstallation(package))
                {
                    MainApp.Instance.AddOperationToList(new UninstallPackageOperation(package, newOptions));
                }
            }
            else if (action is OperationType.Update)
            {
                MainApp.Instance.AddOperationToList(new UpdatePackageOperation(package, newOptions));
            }
            else
            {
                throw new ArgumentException("PackageDetailsPage.DoAction should never be called with action=None");
            }
        }
    }
}

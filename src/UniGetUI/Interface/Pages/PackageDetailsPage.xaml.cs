using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UniGetUI.Core;
using UniGetUI.Core.Data;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.Core.Logging;
using Windows.Storage;
using Windows.Storage.Pickers;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.Core.Tools;
using UniGetUI.Core.IconEngine;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Text;
using Windows.UI.Text;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PackageDetailsPage : Page
    {
        public Package Package;
        private InstallOptionsPage InstallOptionsPage;
        public event EventHandler? Close;
        private PackageDetails? Info;
        OperationType OperationRole;
        bool PackageHasScreenshots = false;
        public ObservableCollection<TextBlock> ShowableTags = new();
        Hyperlink downloadButton;

        private enum LayoutMode
        {
            Normal,
            Wide,
            Unloaded
        }

        private TextStringsStruct TextStrings;
        private struct TextStringsStruct
        {
            public Run Content_Loading { get { return new Run() { Text = CoreTools.Translate("Loading...") }; } }
            public Run Content_NotAvailable { get { return new Run() { Text = CoreTools.Translate("Not available") }; } }

            public Run Label_HomePage { get { return new Run() { Text = CoreTools.Translate("Homepage") + ": ", FontWeight = new FontWeight(700) }; } }
            public Run Label_Publisher { get { return new Run() { Text = CoreTools.Translate("Publisher") + ": ", FontWeight = new FontWeight(700) }; } }
            public Run Label_Author { get { return new Run() { Text = CoreTools.Translate("Author") + ": ", FontWeight = new FontWeight(700) }; } }
            public Run Label_License { get { return new Run() { Text = CoreTools.Translate("License") + ": ", FontWeight = new FontWeight(700) }; } }
            public Run Label_SourceName { get { return new Run() { Text = CoreTools.Translate("Package Manager") + ": ", FontWeight = new FontWeight(700) }; } }
            public Run Label_Id { get { return new Run() { Text = CoreTools.Translate("Package ID") + ": ", FontWeight = new FontWeight(700) }; } }
            public Run Label_Manifest{ get { return new Run() { Text = CoreTools.Translate("Manifest") + ": ", FontWeight = new FontWeight(700) }; } }
            public Run Label_InstalledVersion { get { return new Run() { Text = CoreTools.Translate("Installed Version") + ": ", FontWeight = new FontWeight(700) }; } }
            public Run Label_Version { get { return new Run() { Text = CoreTools.Translate("Version") + ": ", FontWeight = new FontWeight(700) }; } }
            public Run Label_InstallerSha256 { get { return new Run() { Text = CoreTools.Translate("Installer SHA256") + ": ", FontWeight = new FontWeight(700) }; } }
            public Run Label_InstallerSha512 { get { return new Run() { Text = CoreTools.Translate("Installer SHA512") + ": ", FontWeight = new FontWeight(700) }; } }
            public Run Label_InstallerUrl { get { return new Run() { Text = CoreTools.Translate("Installer URL") + ": ", FontWeight = new FontWeight(700) }; } }
            public Run Label_InstallerType { get { return new Run() { Text = CoreTools.Translate("Installer Type") + ": ", FontWeight = new FontWeight(700) }; } }
            public Run Label_LastUpdated { get { return new Run() { Text = CoreTools.Translate("Last updated:") + " ", FontWeight = new FontWeight(700) }; } }
            public Run Label_ReleaseNotes { get { return new Run() { Text = CoreTools.Translate("Release notes") + ": ", FontWeight = new FontWeight(700) }; } }
            public Run Label_ReleaseNotesUrl { get { return new Run() { Text = CoreTools.Translate("Release notes URL") + ": ", FontWeight = new FontWeight(700) }; } }

            public Inline Content_GetUri(Uri? uri)
            {
                if (uri == null) return Content_NotAvailable;
                var h = new Hyperlink() { NavigateUri = uri };
                h.Inlines.Add(Content_GetText(uri.ToString()));
                return h;
            }
            public Inline Content_GetText(string? text)
            {
                if (text == null || text == String.Empty) return Content_NotAvailable;
                return new Run() { Text = text };
            }

            public TextStringsStruct() { }
        }

        private LayoutMode __layout_mode = LayoutMode.Unloaded;
        public PackageDetailsPage(Package package, OperationType operationRole)
        {
            TextStrings = new TextStringsStruct();
            OperationRole = operationRole;
            Package = package;

            InitializeComponent();

            InstallOptionsPage = new InstallOptionsPage(package, operationRole);
            InstallOptionsExpander.Content = InstallOptionsPage;

            SizeChanged += PackageDetailsPage_SizeChanged;

            if (operationRole == OperationType.None)
                operationRole = OperationType.Install;

            switch (operationRole)
            {
                case OperationType.Install:
                    ActionButton.Content = CoreTools.Translate("Install");
                    break;
                case OperationType.Uninstall:
                    ActionButton.Content = CoreTools.Translate("Uninstall");
                    break;
                case OperationType.Update:
                    ActionButton.Content = CoreTools.Translate("Update");
                    break;
            }

            PackageName.Text = package.Name;
            LoadingIndicator.Visibility = Visibility.Visible;
            string LoadingString = CoreTools.Translate("Loading...");
            DescriptionBox.Text = LoadingString;

            BasicInfoPanelText.Blocks.Clear();
            Paragraph paragraph = new Paragraph();
            BasicInfoPanelText.Blocks.Add(paragraph);

            AddToParagraph(paragraph, TextStrings.Label_HomePage, TextStrings.Content_Loading);
            AddToParagraph(paragraph, TextStrings.Label_Publisher, TextStrings.Content_Loading);
            AddToParagraph(paragraph, TextStrings.Label_Author, TextStrings.Content_Loading);
            AddToParagraph(paragraph, TextStrings.Label_License, TextStrings.Content_Loading);
            AddToParagraph(paragraph, TextStrings.Label_SourceName, package.SourceAsString, AddLineBreak: false);

            DetailsPanelText.Blocks.Clear();
            paragraph = new Paragraph();
            DetailsPanelText.Blocks.Add(paragraph);

            AddToParagraph(paragraph, TextStrings.Label_Id, package.Id);
            AddToParagraph(paragraph, TextStrings.Label_Manifest, TextStrings.Content_Loading);
            if (package.IsUpgradable)
                AddToParagraph(
                    paragraph, 
                    operationRole == OperationType.Uninstall? TextStrings.Label_InstalledVersion: TextStrings.Label_Version, 
                    package.Version);
            else
                AddToParagraph(
                    paragraph, 
                    TextStrings.Label_InstalledVersion, 
                    $"{package.Version} - {CoreTools.Translate("Update to {0} available", package.NewVersion)}");

            AddToParagraph(paragraph, TextStrings.Label_InstallerSha256, TextStrings.Content_Loading);
            AddToParagraph(paragraph, TextStrings.Label_InstallerUrl, TextStrings.Content_Loading);
            AddToParagraph(paragraph, TextStrings.Label_InstallerType, TextStrings.Content_Loading);
            AddToParagraph(paragraph, TextStrings.Label_LastUpdated, TextStrings.Content_Loading);
            AddToParagraph(paragraph, TextStrings.Label_ReleaseNotes, TextStrings.Content_Loading);
            AddToParagraph(paragraph, TextStrings.Label_ReleaseNotesUrl, TextStrings.Content_Loading);

            _ = LoadInformation();

        }
        public async Task LoadInformation()
        {
            LoadingIndicator.Visibility = Visibility.Visible;

            LoadIcon();
            LoadScreenshots();

            string NotFound = CoreTools.Translate("Not available");
            Uri InvalidUri = new("about:blank");
            Info = await Package.Manager.GetPackageDetails(Package);
            Logger.Debug("Received info " + Info);

            string command = "";

            switch (OperationRole)
            {
                case OperationType.Install:
                    command = Package.Manager.Properties.ExecutableFriendlyName + " " + String.Join(' ', Package.Manager.GetInstallParameters(Package, await InstallationOptions.FromPackageAsync(Package)));
                    break;

                case OperationType.Uninstall:
                    command = Package.Manager.Properties.ExecutableFriendlyName + " " + String.Join(' ', Package.Manager.GetUninstallParameters(Package, await InstallationOptions.FromPackageAsync(Package)));
                    break;

                case OperationType.Update:
                    command = Package.Manager.Properties.ExecutableFriendlyName + " " + String.Join(' ', Package.Manager.GetUpdateParameters(Package, await InstallationOptions.FromPackageAsync(Package)));
                    break;
            }
            CommandTextBlock.Text = command;

            LoadingIndicator.Visibility = Visibility.Collapsed;

            BasicInfoPanelText.Blocks.Clear();
            Paragraph paragraph = new Paragraph();
            BasicInfoPanelText.Blocks.Add(paragraph);
            AddToParagraph(paragraph, TextStrings.Label_HomePage, Info.HomepageUrl);
            AddToParagraph(paragraph, TextStrings.Label_Publisher, Info.Publisher);
            AddToParagraph(paragraph, TextStrings.Label_Author, Info.Author);

            paragraph.Inlines.Add(TextStrings.Label_License);

            if (Info.License != null && Info.LicenseUrl != null)
            {
                paragraph.Inlines.Add(TextStrings.Content_GetText(Info.License));
                paragraph.Inlines.Add(TextStrings.Content_GetText(" ("));
                paragraph.Inlines.Add(TextStrings.Content_GetUri(Info.LicenseUrl));
                paragraph.Inlines.Add(TextStrings.Content_GetText(")"));
            }
            else if (Info.License != null && Info.LicenseUrl == null)
            {
                paragraph.Inlines.Add(TextStrings.Content_GetText(Info.License));
            }
            else if (Info.License == null && Info.LicenseUrl != null)
            {
                paragraph.Inlines.Add(TextStrings.Content_GetUri(Info.LicenseUrl));
            }
            else
            {
                paragraph.Inlines.Add(TextStrings.Content_NotAvailable);
            }
            paragraph.Inlines.Add(new LineBreak());
            AddToParagraph(paragraph, TextStrings.Label_SourceName, Package.SourceAsString, AddLineBreak: false);

            DescriptionBox.Text = Info.Description;
            DetailsPanelText.Blocks.Clear();
            paragraph = new Paragraph();
            DetailsPanelText.Blocks.Add(paragraph);

            AddToParagraph(paragraph, TextStrings.Label_Id, Package.Id);
            AddToParagraph(paragraph, TextStrings.Label_Manifest, Info.ManifestUrl);
            if (Package.IsUpgradable)
                AddToParagraph(
                    paragraph,
                    OperationRole == OperationType.Uninstall ? TextStrings.Label_InstalledVersion : TextStrings.Label_Version,
                    Package.Version);
            else
                AddToParagraph(
                    paragraph,
                    TextStrings.Label_InstalledVersion,
                $"{Package.Version} - {CoreTools.Translate("Update to {0} available", Package.NewVersion)}");

            if(Package.Manager == MainApp.Choco)
                AddToParagraph(paragraph, TextStrings.Label_InstallerSha512, Info.InstallerHash);
            else
                AddToParagraph(paragraph, TextStrings.Label_InstallerSha256, Info.InstallerHash);
            AddToParagraph(paragraph, TextStrings.Label_InstallerUrl, Info.InstallerUrl);

            if (Info.InstallerUrl != null)
            {
                downloadButton = new Hyperlink();
                downloadButton.Click += (s, e) => { DownloadInstallerButton_Click(s, e); };
                downloadButton.Inlines.Add(new Run() { Text = CoreTools.Translate("Download installer") });
                paragraph.Inlines.Add(downloadButton);
                if (Info.InstallerSize > 0) paragraph.Inlines.Add(new Run() { Text = $" ({Info.InstallerSize} MB)" });
                paragraph.Inlines.Add(new LineBreak());
            }
            AddToParagraph(paragraph, TextStrings.Label_InstallerType, Info.InstallerType);
            AddToParagraph(paragraph, TextStrings.Label_LastUpdated, Info.UpdateDate);
            AddToParagraph(paragraph, TextStrings.Label_ReleaseNotes, Info.ReleaseNotes);
            AddToParagraph(paragraph, TextStrings.Label_ReleaseNotesUrl, Info.ReleaseNotesUrl);

            ShowableTags.Clear();
            foreach (string tag in Info.Tags)
                ShowableTags.Add(new TextBlock() { 
                    Text = tag, 
                    VerticalAlignment = VerticalAlignment.Center,
                    TextLineBounds = TextLineBounds.Tight
                });
        }

        public void AddToParagraph(Paragraph p, Inline i1, Inline i2, bool AddLineBreak = true)
        {
            p.Inlines.Add(i1);
            p.Inlines.Add(i2);
            if(AddLineBreak) p.Inlines.Add(new LineBreak());
        }

        public void AddToParagraph(Paragraph p, Inline i1, string? s2, bool AddLineBreak = true)
        {
            AddToParagraph(p, i1, TextStrings.Content_GetText(s2), AddLineBreak);
        }

        public void AddToParagraph(Paragraph p, Inline i1, Uri? u2, bool AddLineBreak = true)
        {
            AddToParagraph(p, i1, TextStrings.Content_GetUri(u2), AddLineBreak);
        }

        public async void LoadIcon()
        {
            PackageIcon.Source = new BitmapImage { UriSource = (await Package.GetIconUrl()) };
        }

        public async void LoadScreenshots()
        {
            var screenshots = await Package.GetPackageScreenshots();
            PackageHasScreenshots = screenshots.Count() > 0;
            if (PackageHasScreenshots)
            {
                PackageHasScreenshots = true;
                IconsExtraBanner.Visibility = Visibility.Visible;
                ScreenshotsCarroussel.Items.Clear();
                foreach (Uri image in screenshots)
                    ScreenshotsCarroussel.Items.Add(new Image { Source = new BitmapImage(image) });
            }

            __layout_mode = LayoutMode.Unloaded;
            PackageDetailsPage_SizeChanged();

        }

        public void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            Close?.Invoke(this, new EventArgs());
            InstallOptionsPage.SaveToDisk();
            switch (OperationRole)
            {
                case OperationType.Install:
                    MainApp.Instance.AddOperationToList(new InstallPackageOperation(Package, InstallOptionsPage.Options));
                    break;
                case OperationType.Uninstall:
                    MainApp.Instance.MainWindow.NavigationPage.InstalledPage.ConfirmAndUninstall(Package, InstallOptionsPage.Options);
                    break;
                case OperationType.Update:
                    MainApp.Instance.AddOperationToList(new UpdatePackageOperation(Package, InstallOptionsPage.Options));
                    break;
            }
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
                if (Info?.InstallerUrl == null)
                    return;

                
                FileSavePicker savePicker = new();
                MainWindow window = MainApp.Instance.MainWindow;
                IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);
                savePicker.SuggestedStartLocation = PickerLocationId.Downloads;
                savePicker.SuggestedFileName = Package.Id + " installer." + Info.InstallerUrl.ToString().Split('.')[^1];
                
                if (Info.InstallerUrl.ToString().Split('.')[^1] == "nupkg")
                    savePicker.FileTypeChoices.Add("Compressed Manifest File", new System.Collections.Generic.List<string>() { ".zip" });
                savePicker.FileTypeChoices.Add("Default", new System.Collections.Generic.List<string>() { "." + Info.InstallerUrl.ToString().Split('.')[^1] });
                
                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    var loader = async () =>
                    {
                        List<string> texts = [
                            "[≡≡≡≡        ]",
                            "[  ≡≡≡≡      ]",
                            "[    ≡≡≡≡    ]",
                            "[      ≡≡≡≡  ]",
                            "[        ≡≡≡≡]",
                            "[≡≡        ≡≡]"];
                        int i = 0;
                        var baseString = CoreTools.Translate("Downloading installer for {package}", new Dictionary<string, object?> { { "package", Package.Name } });
                        while (running)
                        {
                            downloadButton.Inlines.Clear();
                            downloadButton.Inlines.Add(new Run() { Text = baseString + " " + texts[(i++) % 6] });
                            await Task.Delay(500);
                        }
                    };
                    _ = loader();
                    
                    Logger.Debug($"Downloading installer ${file.Path.ToString()}");
                    
                    using HttpClient httpClient = new();
                    await using Stream s = await httpClient.GetStreamAsync(Info.InstallerUrl);
                    await using FileStream fs = File.OpenWrite(file.Path.ToString());
                    await s.CopyToAsync(fs);
                    fs.Dispose();
                    downloadButton.Inlines.Clear();
                    downloadButton.Inlines.Add(new Run() { Text = CoreTools.Translate("Download installer") });
                    running = false;
                    Logger.ImportantInfo($"Installer for {Package.Id} has been downloaded successfully");
                    MainApp.Instance.MainWindow.HideLoadingDialog();
                    System.Diagnostics.Process.Start("explorer.exe", "/select," + $"\"{file.Path.ToString()}\"");
                }
            }
            catch (Exception ex)
            {
                running = false;
                Logger.Error($"An error occurred while downloading the installer for the package {Package.Id}");
                Logger.Error(ex);

                downloadButton.Inlines.Clear();
                downloadButton.Inlines.Add(new Run() { Text = CoreTools.Translate("An error occurred") + ": " + ex.Message });
            }


        }
        public void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close?.Invoke(this, new EventArgs());
        }

        public void PackageDetailsPage_SizeChanged(object? sender = null, SizeChangedEventArgs? e = null)
        {
            if (MainApp.Instance.MainWindow.AppWindow.Size.Width < 950)
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
                    MainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    Grid.SetRow(LeftPanel, 1);
                    Grid.SetRow(RightPanel, 0);
                    Grid.SetRow(TitlePanel, 0);
                    Grid.SetRowSpan(RightPanel, 2);

                    LeftPanel.Children.Clear();
                    RightPanel.Children.Clear();
                    MainGrid.Children.Clear();
                    LeftPanel.Children.Add(DescriptionPanel);
                    LeftPanel.Children.Add(BasicInfoPanelText);
                    RightPanel.Children.Add(ScreenshotsPanel);
                    LeftPanel.Children.Add(ActionsPanel);
                    LeftPanel.Children.Add(InstallOptionsBorder);
                    RightPanel.Children.Add(DetailsPanelText);
                    ScreenshotsCarroussel.Height = PackageHasScreenshots ? 400 : 150;

                    InstallOptionsExpander.IsExpanded = true;

                    MainGrid.Children.Add(LeftPanel);
                    MainGrid.Children.Add(RightPanel);
                    MainGrid.Children.Add(TitlePanel);

                }
            }
        }
    }
}

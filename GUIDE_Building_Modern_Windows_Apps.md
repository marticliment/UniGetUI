# Building Modern Windows Desktop Apps with WinUI 3

**A Comprehensive Guide Using UniGetUI as Reference**

> This guide shows you how to build polished, modern Windows desktop applications using WinUI 3 and .NET 8, based on patterns and practices from the UniGetUI project.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Project Setup](#project-setup)
3. [Architecture Patterns](#architecture-patterns)
4. [UI/UX Implementation](#uiux-implementation)
5. [Key Features](#key-features)
6. [Step-by-Step Tutorials](#step-by-step-tutorials)
7. [Best Practices](#best-practices)
8. [Resources](#resources)

---

## Prerequisites

### Required Tools

- **Visual Studio 2022** (v17.8+) or **Visual Studio Code** with C# extensions
- **.NET 8.0 SDK** or later
- **Windows App SDK 1.7** or later
- **Windows 10 SDK** (Build 26100 or later)
- **Git** (for version control)

### Recommended Knowledge

- C# programming (intermediate level)
- XAML basics
- Async/await programming
- Basic MVVM pattern understanding

### Verify Installation

```bash
# Check .NET version
dotnet --version

# Check Windows SDK
reg query "HKLM\SOFTWARE\Microsoft\Windows Kits\Installed Roots"
```

---

## Project Setup

### 1. Create a New WinUI 3 Project

#### Option A: Using Visual Studio

1. Open Visual Studio 2022
2. **File → New → Project**
3. Search for "WinUI 3" → Select **"Blank App, Packaged (WinUI 3 in Desktop)"**
4. Name: `MyModernApp`
5. Create

#### Option B: Using .NET CLI

```bash
# Install WinUI 3 templates
dotnet new install Microsoft.WindowsAppSDK.Templates

# Create new project
dotnet new winui3 -n MyModernApp
cd MyModernApp
```

### 2. Configure Directory.Build.props

Create `/src/Directory.Build.props`:

```xml
<Project>
    <PropertyGroup>
        <ImplicitUsings>enable</ImplicitUsings>
        <TargetFramework>net8.0-windows10.0.26100.0</TargetFramework>
        <TargetPlatformMinVersion>10.0.19041.0</TargetPlatformMinVersion>
        <WindowsSdkPackageVersion>10.0.26100.57</WindowsSdkPackageVersion>

        <Authors>Your Name</Authors>
        <PublisherName>Your Company</PublisherName>
        <Nullable>enable</Nullable>
    </PropertyGroup>

    <PropertyGroup>
        <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
        <RuntimeIdentifiers>win-x64;win-arm64</RuntimeIdentifiers>
        <RuntimeIdentifier>win-$(Platform)</RuntimeIdentifier>
        <Platforms>x64</Platforms>
        <PublishSelfContained>true</PublishSelfContained>
    </PropertyGroup>

    <PropertyGroup Condition="'$(Configuration)' == 'Release'">
        <Optimize>true</Optimize>
        <TieredCompilation>true</TieredCompilation>
        <PublishReadyToRun>true</PublishReadyToRun>
    </PropertyGroup>

    <PropertyGroup>
        <LangVersion>latest</LangVersion>
        <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    </PropertyGroup>
</Project>
```

### 3. Add Essential NuGet Packages

Edit your `.csproj` file:

```xml
<ItemGroup>
    <!-- Windows App SDK -->
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.250606001" />
    <PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.26100.4948" />

    <!-- CommunityToolkit for WinUI -->
    <PackageReference Include="CommunityToolkit.WinUI.Animations" Version="8.2.250402" />
    <PackageReference Include="CommunityToolkit.WinUI.Controls.Primitives" Version="8.2.250402" />
    <PackageReference Include="CommunityToolkit.WinUI.Controls.Segmented" Version="8.2.250402" />
    <PackageReference Include="CommunityToolkit.WinUI.Controls.SettingsControls" Version="8.2.250402" />
    <PackageReference Include="CommunityToolkit.WinUI.Converters" Version="8.2.250402" />

    <!-- System Tray Icon -->
    <PackageReference Include="H.NotifyIcon.WinUI" Version="2.3.0" />
</ItemGroup>
```

Install packages:

```bash
dotnet restore
```

---

## Architecture Patterns

### Layered Architecture

UniGetUI uses a clean 4-tier architecture:

```
┌─────────────────────────────────────────┐
│ UI Layer (XAML Pages & Controls)        │  Presentation
├─────────────────────────────────────────┤
│ Interface Layer (Services, Background)  │  Application Services
├─────────────────────────────────────────┤
│ Business Logic (Domain Models)          │  Domain Logic
├─────────────────────────────────────────┤
│ Core Infrastructure (Settings, Logging) │  Infrastructure
└─────────────────────────────────────────┘
```

### Project Structure

Organize your solution like this:

```
MyModernApp/
├── src/
│   ├── MyModernApp/                    # Main UI project
│   │   ├── App.xaml
│   │   ├── App.xaml.cs
│   │   ├── MainWindow.xaml
│   │   ├── MainWindow.xaml.cs
│   │   ├── Pages/
│   │   ├── Controls/
│   │   └── Assets/
│   │
│   ├── MyModernApp.Core/               # Core infrastructure
│   │   ├── Data/
│   │   ├── Settings/
│   │   └── Logging/
│   │
│   ├── MyModernApp.Services/           # Business logic
│   │   └── Interfaces/
│   │
│   └── MyModernApp.UI.Controls/        # Reusable controls
│
└── tests/
    └── MyModernApp.Tests/
```

Create projects:

```bash
# Create solution
dotnet new sln -n MyModernApp

# Create projects
dotnet new classlib -n MyModernApp.Core -f net8.0
dotnet new classlib -n MyModernApp.Services -f net8.0
dotnet new classlib -n MyModernApp.UI.Controls -f net8.0

# Add to solution
dotnet sln add **/*.csproj
```

---

## UI/UX Implementation

### 1. Modern MainWindow with Mica Backdrop

**MainWindow.xaml**:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Window
    x:Class="MyModernApp.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    Title="MyModernApp">

    <!-- Mica Backdrop -->
    <Window.SystemBackdrop>
        <MicaBackdrop Kind="Base" />
    </Window.SystemBackdrop>

    <Grid x:Name="MainGrid">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <!-- Custom Title Bar -->
        <TitleBar
            x:Name="AppTitleBar"
            Title="MyModernApp"
            Grid.Row="0"
            IsBackButtonVisible="False"
            IsPaneToggleButtonVisible="True" />

        <!-- Main Content -->
        <Frame Name="ContentFrame" Grid.Row="1" />
    </Grid>
</Window>
```

**MainWindow.xaml.cs**:

```csharp
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;

namespace MyModernApp;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Configure title bar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        // Set app icon
        AppWindow.SetIcon("Assets/icon.ico");

        // Apply theme
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        // Get system theme preference
        var theme = Application.Current.RequestedTheme;
        MainGrid.RequestedTheme = theme == ApplicationTheme.Dark
            ? ElementTheme.Dark
            : ElementTheme.Light;
    }
}
```

### 2. NavigationView with Pages

**Create MainPage.xaml**:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Page
    x:Class="MyModernApp.MainPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <NavigationView
        x:Name="NavView"
        IsBackButtonVisible="Auto"
        IsSettingsVisible="True"
        CompactModeThresholdWidth="800"
        ExpandedModeThresholdWidth="1200"
        SelectionChanged="NavView_SelectionChanged">

        <NavigationView.MenuItems>
            <NavigationViewItem
                Content="Home"
                Icon="Home"
                Tag="Home" />
            <NavigationViewItem
                Content="Documents"
                Icon="Document"
                Tag="Documents" />
            <NavigationViewItem
                Content="Settings"
                Icon="Setting"
                Tag="Settings" />
        </NavigationView.MenuItems>

        <Frame x:Name="ContentFrame" />
    </NavigationView>
</Page>
```

**MainPage.xaml.cs**:

```csharp
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MyModernApp;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
    }

    private void NavView_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();

            switch (tag)
            {
                case "Home":
                    ContentFrame.Navigate(typeof(HomePage));
                    break;
                case "Documents":
                    ContentFrame.Navigate(typeof(DocumentsPage));
                    break;
                case "Settings":
                    ContentFrame.Navigate(typeof(SettingsPage));
                    break;
            }
        }
    }
}
```

### 3. Beautiful Animations

**Add entry animations** using CommunityToolkit:

```xml
<Page
    xmlns:animations="using:CommunityToolkit.WinUI.Animations">

    <animations:Implicit.ShowAnimations>
        <animations:TranslationAnimation
            From="0,50,0"
            To="0,0,0"
            Duration="0:0:0.3" />
        <animations:OpacityAnimation
            From="0"
            To="1"
            Duration="0:0:0.3" />
    </animations:Implicit.ShowAnimations>

    <StackPanel Spacing="8">
        <TextBlock Text="Welcome!" FontSize="32" />
        <Button Content="Get Started" />
    </StackPanel>
</Page>
```

### 4. Custom Styled Controls

**App.xaml** - Add global styles:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
        </ResourceDictionary.MergedDictionaries>

        <!-- Better Button Style -->
        <Style x:Key="AccentButtonStyle" TargetType="Button">
            <Setter Property="Background" Value="{ThemeResource AccentFillColorDefaultBrush}" />
            <Setter Property="Foreground" Value="{ThemeResource TextOnAccentFillColorPrimaryBrush}" />
            <Setter Property="CornerRadius" Value="8" />
            <Setter Property="Padding" Value="16,8" />
            <Setter Property="MinHeight" Value="40" />
        </Style>

        <!-- Card Style -->
        <Style x:Key="CardStyle" TargetType="Border">
            <Setter Property="Background" Value="{ThemeResource CardBackgroundFillColorDefaultBrush}" />
            <Setter Property="BorderBrush" Value="{ThemeResource CardStrokeColorDefaultBrush}" />
            <Setter Property="BorderThickness" Value="1" />
            <Setter Property="CornerRadius" Value="8" />
            <Setter Property="Padding" Value="16" />
        </Style>
    </ResourceDictionary>
</Application.Resources>
```

Usage:

```xml
<StackPanel Spacing="16" Padding="16">
    <Border Style="{StaticResource CardStyle}">
        <StackPanel Spacing="8">
            <TextBlock Text="Welcome Card" FontWeight="SemiBold" FontSize="18" />
            <TextBlock Text="This is a beautiful card with modern styling" />
            <Button Content="Action" Style="{StaticResource AccentButtonStyle}" />
        </StackPanel>
    </Border>
</StackPanel>
```

---

## Key Features

### 1. Settings Management

**Create SettingsService.cs**:

```csharp
using Windows.Storage;

namespace MyModernApp.Core.Services;

public class SettingsService
{
    private readonly ApplicationDataContainer _localSettings;

    public SettingsService()
    {
        _localSettings = ApplicationData.Current.LocalSettings;
    }

    public T Get<T>(string key, T defaultValue = default!)
    {
        if (_localSettings.Values.ContainsKey(key))
        {
            var value = _localSettings.Values[key];
            return (T)Convert.ChangeType(value, typeof(T));
        }
        return defaultValue;
    }

    public void Set<T>(string key, T value)
    {
        _localSettings.Values[key] = value;
    }

    // Theme management
    public ElementTheme Theme
    {
        get => (ElementTheme)Get("Theme", (int)ElementTheme.Default);
        set => Set("Theme", (int)value);
    }
}
```

### 2. Logging System

**Create Logger.cs**:

```csharp
using System.Diagnostics;

namespace MyModernApp.Core.Logging;

public static class Logger
{
    private static readonly string LogFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MyModernApp",
        "Logs",
        $"app_{DateTime.Now:yyyyMMdd}.log"
    );

    static Logger()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
    }

    public static void Info(string message)
    {
        Log("INFO", message);
    }

    public static void Error(string message)
    {
        Log("ERROR", message);
    }

    public static void Error(Exception ex)
    {
        Log("ERROR", $"{ex.Message}\n{ex.StackTrace}");
    }

    private static void Log(string level, string message)
    {
        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

        // Write to Debug
        Debug.WriteLine(logEntry);

        // Write to file
        try
        {
            File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
        }
        catch { /* Ignore file write errors */ }
    }
}
```

### 3. System Tray Integration

**MainWindow.xaml**:

```xml
<Window>
    <Grid x:Name="MainGrid">
        <!-- Add system tray icon -->
        <tray:TaskbarIcon
            x:Name="TrayIcon"
            Visibility="Visible"
            ToolTipText="MyModernApp"
            LeftClickCommand="{x:Bind ShowWindowCommand}">
            <tray:TaskbarIcon.ContextFlyout>
                <MenuFlyout>
                    <MenuFlyoutItem Text="Show" Click="ShowWindow_Click" />
                    <MenuFlyoutSeparator />
                    <MenuFlyoutItem Text="Exit" Click="Exit_Click" />
                </MenuFlyout>
            </tray:TaskbarIcon.ContextFlyout>
        </tray:TaskbarIcon>
    </Grid>
</Window>
```

**MainWindow.xaml.cs**:

```csharp
private void ShowWindow_Click(object sender, RoutedEventArgs e)
{
    Activate();
}

private void Exit_Click(object sender, RoutedEventArgs e)
{
    Application.Current.Exit();
}
```

---

## Step-by-Step Tutorials

### Tutorial 1: Create a Search Page

**SearchPage.xaml**:

```xml
<Page>
    <Grid Padding="16" RowSpacing="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <!-- Search Box -->
        <AutoSuggestBox
            x:Name="SearchBox"
            PlaceholderText="Search..."
            QueryIcon="Find"
            TextChanged="SearchBox_TextChanged"
            MinWidth="300" />

        <!-- Results List -->
        <ListView
            x:Name="ResultsList"
            Grid.Row="1"
            SelectionMode="Single">
            <ListView.ItemTemplate>
                <DataTemplate x:DataType="local:SearchResult">
                    <Border Style="{StaticResource CardStyle}" Margin="0,4">
                        <StackPanel Spacing="4">
                            <TextBlock Text="{x:Bind Title}" FontWeight="SemiBold" />
                            <TextBlock Text="{x:Bind Description}" Opacity="0.7" />
                        </StackPanel>
                    </Border>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
    </Grid>
</Page>
```

**SearchPage.xaml.cs**:

```csharp
public sealed partial class SearchPage : Page
{
    public ObservableCollection<SearchResult> Results { get; } = new();

    public SearchPage()
    {
        InitializeComponent();
        ResultsList.ItemsSource = Results;
    }

    private async void SearchBox_TextChanged(
        AutoSuggestBox sender,
        AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var query = sender.Text;
            await PerformSearch(query);
        }
    }

    private async Task PerformSearch(string query)
    {
        Results.Clear();

        if (string.IsNullOrWhiteSpace(query))
            return;

        // Simulate search
        await Task.Delay(300);

        // Add results
        for (int i = 0; i < 5; i++)
        {
            Results.Add(new SearchResult
            {
                Title = $"Result {i + 1}",
                Description = $"Matches query: {query}"
            });
        }
    }
}

public class SearchResult
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}
```

### Tutorial 2: Async Data Loading with Progress

**DataPage.xaml**:

```xml
<Page>
    <Grid>
        <!-- Loading Indicator -->
        <ProgressRing
            x:Name="LoadingRing"
            IsActive="True"
            Width="50"
            Height="50" />

        <!-- Content -->
        <ScrollViewer x:Name="ContentScroll" Visibility="Collapsed">
            <ItemsRepeater x:Name="ItemsList" />
        </ScrollViewer>
    </Grid>
</Page>
```

**DataPage.xaml.cs**:

```csharp
public sealed partial class DataPage : Page
{
    public DataPage()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            LoadingRing.IsActive = true;
            ContentScroll.Visibility = Visibility.Collapsed;

            // Simulate data loading
            var data = await FetchDataFromServiceAsync();

            ItemsList.ItemsSource = data;

            LoadingRing.IsActive = false;
            ContentScroll.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            await ShowErrorDialog("Failed to load data");
        }
    }

    private async Task<List<DataItem>> FetchDataFromServiceAsync()
    {
        await Task.Delay(2000); // Simulate network delay

        return Enumerable.Range(1, 20)
            .Select(i => new DataItem { Name = $"Item {i}" })
            .ToList();
    }

    private async Task ShowErrorDialog(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }
}

public class DataItem
{
    public string Name { get; set; } = "";
}
```

### Tutorial 3: Settings Page with Theme Switching

**SettingsPage.xaml**:

```xml
<Page xmlns:settings="using:CommunityToolkit.WinUI.Controls">
    <ScrollViewer>
        <StackPanel Spacing="4" Padding="16">
            <!-- Appearance Section -->
            <settings:SettingsCard Header="Theme" Description="Choose app theme">
                <ComboBox x:Name="ThemeComboBox" SelectionChanged="ThemeComboBox_SelectionChanged">
                    <ComboBoxItem Content="Light" Tag="Light" />
                    <ComboBoxItem Content="Dark" Tag="Dark" />
                    <ComboBoxItem Content="System Default" Tag="Default" IsSelected="True" />
                </ComboBox>
            </settings:SettingsCard>

            <settings:SettingsCard Header="Notifications" Description="Enable notifications">
                <ToggleSwitch x:Name="NotificationsToggle" />
            </settings:SettingsCard>

            <settings:SettingsCard Header="About">
                <StackPanel Spacing="4">
                    <TextBlock Text="MyModernApp" FontWeight="SemiBold" />
                    <TextBlock Text="Version 1.0.0" Opacity="0.7" />
                    <HyperlinkButton Content="GitHub" NavigateUri="https://github.com/yourusername/mymodernapp" />
                </StackPanel>
            </settings:SettingsCard>
        </StackPanel>
    </ScrollViewer>
</Page>
```

**SettingsPage.xaml.cs**:

```csharp
public sealed partial class SettingsPage : Page
{
    private readonly SettingsService _settings = new();

    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        var theme = _settings.Theme;
        ThemeComboBox.SelectedIndex = theme switch
        {
            ElementTheme.Light => 0,
            ElementTheme.Dark => 1,
            _ => 2
        };
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeComboBox.SelectedItem is ComboBoxItem item)
        {
            var theme = item.Tag?.ToString() switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };

            _settings.Theme = theme;
            ApplyTheme(theme);
        }
    }

    private void ApplyTheme(ElementTheme theme)
    {
        if (Window.Current.Content is FrameworkElement root)
        {
            root.RequestedTheme = theme;
        }
    }
}
```

---

## Best Practices

### 1. Async/Await Pattern

✅ **Do**:
```csharp
private async Task LoadDataAsync()
{
    var data = await _service.GetDataAsync();
    UpdateUI(data);
}
```

❌ **Don't**:
```csharp
private void LoadData()
{
    var data = _service.GetDataAsync().Result; // Blocks UI!
    UpdateUI(data);
}
```

### 2. Error Handling

```csharp
public async Task<bool> SaveDataAsync()
{
    try
    {
        await _service.SaveAsync(data);
        return true;
    }
    catch (Exception ex)
    {
        Logger.Error($"Failed to save data: {ex.Message}");
        Logger.Error(ex);

        await ShowErrorDialog("Could not save data. Please try again.");
        return false;
    }
}
```

### 3. Resource Management

```csharp
public class DataService : IDisposable
{
    private readonly HttpClient _httpClient = new();

    public async Task<string> FetchDataAsync()
    {
        return await _httpClient.GetStringAsync("https://api.example.com/data");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// Usage
using var service = new DataService();
var data = await service.FetchDataAsync();
```

### 4. UI Responsiveness

```csharp
// Show immediate feedback
LoadingIndicator.IsActive = true;
StatusText.Text = "Loading...";

// Do work on background thread
var result = await Task.Run(() => ExpensiveOperation());

// Update UI on UI thread
LoadingIndicator.IsActive = false;
StatusText.Text = $"Loaded {result.Count} items";
```

### 5. Keyboard Shortcuts

```csharp
protected override void OnKeyDown(KeyRoutedEventArgs e)
{
    var isCtrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control)
        .HasFlag(CoreVirtualKeyStates.Down);

    if (isCtrl && e.Key == VirtualKey.S)
    {
        _ = SaveAsync();
        e.Handled = true;
    }
    else if (isCtrl && e.Key == VirtualKey.F)
    {
        SearchBox.Focus(FocusState.Programmatic);
        e.Handled = true;
    }

    base.OnKeyDown(e);
}
```

---

## Resources

### Official Documentation

- [WinUI 3 Documentation](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/)
- [Windows App SDK](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/)
- [.NET Documentation](https://learn.microsoft.com/en-us/dotnet/)

### Community Resources

- [CommunityToolkit for WinUI](https://learn.microsoft.com/en-us/windows/communitytoolkit/winui/)
- [UniGetUI Source Code](https://github.com/marticliment/UniGetUI)
- [WinUI 3 Gallery App](https://github.com/microsoft/WinUI-Gallery)

### Design Guidelines

- [Windows 11 Design Principles](https://learn.microsoft.com/en-us/windows/apps/design/)
- [Fluent Design System](https://www.microsoft.com/design/fluent/)
- [Windows UI Library](https://aka.ms/winui)

### Useful Tools

- **XAML Styler** - Format XAML automatically
- **Visual Studio IntelliCode** - AI-assisted coding
- **Windows Community Toolkit Sample App** - Interactive examples

---

## Next Steps

### 1. Build Your First Feature

Start with a simple page:
- Create a `HomePage.xaml`
- Add a welcome message
- Add a button that opens another page

### 2. Add Data Persistence

- Implement `SettingsService` for app settings
- Use `ApplicationData` for local storage
- Consider SQLite for complex data

### 3. Implement MVVM

Create ViewModels for better separation:

```csharp
public class HomeViewModel : INotifyPropertyChanged
{
    private string _welcomeMessage = "Hello!";
    public string WelcomeMessage
    {
        get => _welcomeMessage;
        set
        {
            _welcomeMessage = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
```

### 4. Add Unit Tests

```csharp
[TestClass]
public class SettingsServiceTests
{
    [TestMethod]
    public void CanSaveAndRetrieveSetting()
    {
        var settings = new SettingsService();
        settings.Set("TestKey", "TestValue");

        var value = settings.Get<string>("TestKey");
        Assert.AreEqual("TestValue", value);
    }
}
```

### 5. Publish Your App

- **Microsoft Store**: Reach millions of users
- **MSIX Package**: Modern installation
- **GitHub Releases**: Direct distribution

---

## Conclusion

You now have a solid foundation for building modern Windows desktop apps! This guide covered:

✅ Project setup with WinUI 3 and .NET 8
✅ Layered architecture patterns
✅ Modern UI with Fluent Design
✅ Key features (settings, logging, system tray)
✅ Step-by-step tutorials
✅ Best practices

### Study the UniGetUI Source

Explore these key files in UniGetUI:
- `MainWindow.xaml.cs` - Window management
- `MainView.xaml` - Navigation structure
- `AbstractPackagesPage.xaml` - List/Grid views
- `SettingsBasePage.xaml` - Settings UI
- `PEInterface.cs` - Plugin architecture

**Happy coding! 🚀**

---

**Questions or feedback?** Open an issue on the UniGetUI repository or check the [Discussions](https://github.com/marticliment/UniGetUI/discussions).

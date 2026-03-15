using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using UniGetUI.Avalonia.Infrastructure;
using UniGetUI.Avalonia.Models;
using UniGetUI.Avalonia.Views;
using UniGetUI.Avalonia.Views.Pages;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using System.Net;

namespace UniGetUI.Avalonia;

public partial class MainWindow : Window
{
    /// <summary>Gets the currently active <see cref="MainWindow"/> instance.</summary>
    public static MainWindow? Instance { get; private set; }

    private bool _initialized;
    private TrayIcon? _trayIcon;
    private WindowNotificationManager? _notificationManager;
    private bool _isExplicitQuit;
    private CancellationTokenSource? _geometrySaveCts;

    public MainWindow()
    {
        Instance = this;
        ApplyTheme();
        InitializeComponent();
        InitializeNotificationManager();
        Title = BuildWindowTitle();
        Opened += OnOpened;
        SizeChanged += (_, _) => _ = SaveGeometryDebounced();
        PositionChanged += (_, _) => _ = SaveGeometryDebounced();
    }

    private void InitializeNotificationManager()
    {
        _notificationManager = new WindowNotificationManager(this)
        {
            Position = NotificationPosition.TopRight,
            MaxItems = 4,
        };
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        RestoreGeometry();

        // Handle --daemon: launch straight to tray without showing the window
        if (Environment.GetCommandLineArgs().Contains("--daemon"))
        {
            Hide();
        }

        Content = new LoadingView();

        try
        {
            await AvaloniaBootstrapper.InitializeAsync();
            Content = new MainShellView();
            Closing += OnWindowClosing;
            Closed += OnWindowClosed;
            InitTrayIcon();

            // Background integrity check (non-blocking)
            _ = Task.Run(() => IntegrityTester.CheckIntegrity()).ContinueWith(async t =>
            {
                if (!t.Result.Passed && !Settings.Get(Settings.K.DisableIntegrityChecks))
                    await Dispatcher.UIThread.InvokeAsync(() => ShowIntegrityWarningAsync(this));
            }, TaskScheduler.Default);
        }
        catch (Exception ex)
        {
            Logger.Error("UniGetUI initialization failed");
            Logger.Error(ex);
            Content = new ErrorView(
                CoreTools.Translate("UniGetUI failed to initialize"),
                ex.Message
            );
        }
    }

    private void InitTrayIcon()
    {
        try
        {
            using var iconStream = AssetLoader.Open(new Uri("avares://UniGetUI.Avalonia/Assets/icon.ico"));
            var windowIcon = new WindowIcon(iconStream);

            _trayIcon = new TrayIcon
            {
                Icon = windowIcon,
                ToolTipText = "UniGetUI",
                IsVisible = !Settings.Get(Settings.K.DisableSystemTray),
            };

            var menu = new NativeMenu();

            var showItem = new NativeMenuItem(CoreTools.Translate("Open UniGetUI"));
            showItem.Click += (_, _) => ShowFromTray();
            menu.Items.Add(showItem);

            menu.Items.Add(new NativeMenuItemSeparator());

            var discoverItem = new NativeMenuItem(CoreTools.Translate("Discover packages"));
            discoverItem.Click += (_, _) => { ShowFromTray(); NavigateShell(ShellPageType.Discover); };
            menu.Items.Add(discoverItem);

            var updatesItem = new NativeMenuItem(CoreTools.Translate("Software updates"));
            updatesItem.Click += (_, _) => { ShowFromTray(); NavigateShell(ShellPageType.Updates); };
            menu.Items.Add(updatesItem);

            var installedItem = new NativeMenuItem(CoreTools.Translate("Installed packages"));
            installedItem.Click += (_, _) => { ShowFromTray(); NavigateShell(ShellPageType.Installed); };
            menu.Items.Add(installedItem);

            menu.Items.Add(new NativeMenuItemSeparator());

            var releaseNotesItem = new NativeMenuItem(CoreTools.Translate("Release notes"));
            releaseNotesItem.Click += (_, _) =>
            {
                ShowFromTray();
                var win = new ReleaseNotesWindow();
                _ = win.ShowDialog(this);
            };
            menu.Items.Add(releaseNotesItem);

            var aboutItem = new NativeMenuItem(CoreTools.Translate("About UniGetUI"));
            aboutItem.Click += (_, _) => new AboutPageWindow().Show();
            menu.Items.Add(aboutItem);

            menu.Items.Add(new NativeMenuItemSeparator());

            var quitItem = new NativeMenuItem(CoreTools.Translate("Quit UniGetUI"));
            quitItem.Click += (_, _) => QuitApplication();
            menu.Items.Add(quitItem);

            _trayIcon.Menu = menu;
            _trayIcon.Clicked += (_, _) => ShowFromTray();
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to initialize system tray icon:");
            Logger.Error(ex);
        }
    }

    internal void NavigateShell(ShellPageType pageType)
    {
        if (Content is MainShellView shell)
            shell.OpenPage(pageType);
    }

    private string _lastTrayIconVariant = "";

    public void UpdateSystemTrayStatus()
    {
        if (_trayIcon is null) return;
        try
        {
            bool anyRunning = AvaloniaOperationRegistry.Operations.Any(
                o => o.Status is UniGetUI.PackageEngine.Enums.OperationStatus.Running
                    or UniGetUI.PackageEngine.Enums.OperationStatus.InQueue);

            bool anyFailed = AvaloniaOperationRegistry.Operations.Any(
                o => o.Status == UniGetUI.PackageEngine.Enums.OperationStatus.Failed);

            int updates = UniGetUI.PackageEngine.PackageLoader.UpgradablePackagesLoader.Instance.Count();

            string modifier;
            string tooltip;

            if (anyRunning)
            {
                modifier = "blue";
                tooltip = CoreTools.Translate("Operation in progress") + " — UniGetUI";
            }
            else if (anyFailed)
            {
                modifier = "orange";
                tooltip = CoreTools.Translate("Some operations failed") + " — UniGetUI";
            }
            else if (updates == 1)
            {
                modifier = "green";
                tooltip = CoreTools.Translate("1 update is available") + " — UniGetUI";
            }
            else if (updates > 1)
            {
                modifier = "green";
                tooltip = CoreTools.Translate("{0} updates are available", updates) + " — UniGetUI";
            }
            else
            {
                modifier = "empty";
                tooltip = CoreTools.Translate("Everything is up to date") + " — UniGetUI";
            }

            _trayIcon.ToolTipText = tooltip;

            // Determine light/dark theme from registry to pick black/white icon variant
            string themeSuffix = "white"; // default: dark taskbar → white icon
#pragma warning disable CA1416
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("SystemUsesLightTheme") is int val && val > 0)
                    themeSuffix = "black";
            }
            catch { /* registry unavailable; keep default */ }
#pragma warning restore CA1416

            string variant = $"tray_{modifier}_{themeSuffix}";
            if (variant == _lastTrayIconVariant) return;
            _lastTrayIconVariant = variant;

            using var stream = AssetLoader.Open(new Uri($"avares://UniGetUI.Avalonia/Assets/{variant}.ico"));
            _trayIcon.Icon = new WindowIcon(stream);
        }
        catch (Exception ex)
        {
            Logger.Warn("Failed to update system tray status");
            Logger.Warn(ex);
        }
    }

    public void ApplyTrayIconVisibility()
    {
        if (_trayIcon is null) return;
        _trayIcon.IsVisible = !Settings.Get(Settings.K.DisableSystemTray);
    }

    internal void ShowFromTray()
    {
        Show();
        Activate();
        WindowState = WindowState.Normal;
    }

    public void ShowRuntimeNotification(string title, string message, RuntimeNotificationLevel level)
    {
        if (_notificationManager is null)
        {
            return;
        }

        NotificationType type = level switch
        {
            RuntimeNotificationLevel.Success => NotificationType.Success,
            RuntimeNotificationLevel.Error => NotificationType.Error,
            _ => NotificationType.Information,
        };

        TimeSpan expiration = level == RuntimeNotificationLevel.Error
            ? TimeSpan.FromSeconds(8)
            : TimeSpan.FromSeconds(5);

        _notificationManager.Show(new Notification(title, message, type, expiration));
    }

    public void QuitApplication()
    {
        _isExplicitQuit = true;
        Close();
    }

    private static async Task ShowIntegrityWarningAsync(Window owner)
    {
        try
        {
            var dialog = new Window
            {
                Title = CoreTools.Translate("Integrity violation"),
                Width = 520,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = CoreTools.Translate("UniGetUI or some of its components are missing or corrupt.")
                                   + " " + CoreTools.Translate("It is strongly recommended to reinstall UniGetUI to adress the situation."),
                            TextWrapping = TextWrapping.Wrap,
                            FontWeight = FontWeight.SemiBold,
                        },
                        new TextBlock
                        {
                            Text = " ● " + CoreTools.Translate("Refer to the UniGetUI Logs to get more details regarding the affected file(s)"),
                            TextWrapping = TextWrapping.Wrap,
                        },
                        new TextBlock
                        {
                            Text = " ● " + CoreTools.Translate("Integrity checks can be disabled from the Experimental Settings"),
                            TextWrapping = TextWrapping.Wrap,
                        },
                        new Button
                        {
                            Content = CoreTools.Translate("Close"),
                            HorizontalAlignment = HorizontalAlignment.Right,
                        },
                    },
                },
            };
            // Wire close button
            if (dialog.Content is StackPanel sp
                && sp.Children[^1] is Button btn)
                btn.Click += (_, _) => dialog.Close();

            await dialog.ShowDialog(owner);
        }
        catch (Exception ex)
        {
            Logger.Warn("Failed to show integrity warning dialog");
            Logger.Warn(ex);
        }
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!_isExplicitQuit && !Settings.Get(Settings.K.DisableSystemTray))
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _trayIcon?.Dispose();
        _trayIcon = null;
    }

    private static string BuildWindowTitle()
    {
        var details = new List<string>();

        if (
            Settings.Get(Settings.K.ShowVersionNumberOnTitlebar)
            && !string.IsNullOrWhiteSpace(CoreData.VersionName)
        )
        {
            details.Add(CoreTools.Translate("version {0}", CoreData.VersionName));
        }

        if (CoreTools.IsAdministrator())
        {
            details.Add(CoreTools.Translate("[RAN AS ADMINISTRATOR]"));
        }

        if (CoreData.IsPortable)
        {
            details.Add(CoreTools.Translate("Portable mode"));
        }

#if DEBUG
        details.Add(CoreTools.Translate("DEBUG BUILD"));
#endif

        return details.Count == 0 ? "UniGetUI" : $"UniGetUI - {string.Join(" - ", details)}";
    }

    public void RefreshWindowTitle()
    {
        Title = BuildWindowTitle();
    }

    public static void ApplyProxyVariableToProcess()
    {
        try
        {
            var proxyUri = Settings.GetProxyUrl();
            if (proxyUri is null || !Settings.Get(Settings.K.EnableProxy))
            {
                Environment.SetEnvironmentVariable(
                    "HTTP_PROXY",
                    string.Empty,
                    EnvironmentVariableTarget.Process
                );
                return;
            }

            string content;
            if (!Settings.Get(Settings.K.EnableProxyAuth))
            {
                content = proxyUri.ToString();
            }
            else
            {
                var credentials = Settings.GetProxyCredentials();
                if (credentials is null)
                {
                    content = proxyUri.ToString();
                }
                else
                {
                    content =
                        $"{proxyUri.Scheme}://{Uri.EscapeDataString(credentials.UserName)}"
                        + $":{Uri.EscapeDataString(credentials.Password)}"
                        + $"@{proxyUri.AbsoluteUri.Replace($"{proxyUri.Scheme}://", string.Empty)}";
                }
            }

            Environment.SetEnvironmentVariable(
                "HTTP_PROXY",
                content,
                EnvironmentVariableTarget.Process
            );
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to apply proxy settings:");
            Logger.Error(ex);
        }
    }

    private void ApplyTheme()
    {
        RequestedThemeVariant = Settings.GetValue(Settings.K.PreferredTheme) switch
        {
            "dark" => ThemeVariant.Dark,
            "light" => ThemeVariant.Light,
            _ => ThemeVariant.Default,
        };
    }

    private async Task SaveGeometryDebounced()
    {
        _geometrySaveCts?.Cancel();
        _geometrySaveCts = new CancellationTokenSource();
        var token = _geometrySaveCts.Token;
        try
        {
            await Task.Delay(300, token);
            if (token.IsCancellationRequested) return;
            SaveGeometry();
        }
        catch (TaskCanceledException) { }
    }

    private void SaveGeometry()
    {
        try
        {
            if (WindowState == WindowState.Minimized) return;
            int state = WindowState == WindowState.Maximized ? 1 : 0;
            string geometry = $"v2,{Position.X},{Position.Y},{(int)Width},{(int)Height},{state}";
            Logger.Debug($"Saving window geometry: {geometry}");
            Settings.SetValue(Settings.K.WindowGeometry, geometry);
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to save window geometry:");
            Logger.Error(ex);
        }
    }

    private void RestoreGeometry()
    {
        try
        {
            var geometry = Settings.GetValue(Settings.K.WindowGeometry);
            var parts = geometry.Split(',');
            if (parts.Length == 6 && parts[0] == "v2")
            {
                int x = int.Parse(parts[1]);
                int y = int.Parse(parts[2]);
                int w = int.Parse(parts[3]);
                int h = int.Parse(parts[4]);
                int state = int.Parse(parts[5]);

                if (w > 200 && h > 100)
                {
                    Width = w;
                    Height = h;
                    Position = new PixelPoint(x, y);
                }
                if (state == 1)
                    WindowState = WindowState.Maximized;

                Logger.Debug($"Restored window geometry: {geometry}");
            }
        }
        catch (Exception ex)
        {
            Logger.Warn("Failed to restore window geometry:");
            Logger.Warn(ex);
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public enum RuntimeNotificationLevel
    {
        Progress,
        Success,
        Error,
    }
}

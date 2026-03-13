using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using UniGetUI.Avalonia.Infrastructure;
using UniGetUI.Avalonia.Views;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;

namespace UniGetUI.Avalonia;

public partial class MainWindow : Window
{
    private bool _initialized;

    public MainWindow()
    {
        ApplyTheme();
        InitializeComponent();
        Title = BuildWindowTitle();
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        Content = new LoadingView();

        try
        {
            await AvaloniaBootstrapper.InitializeAsync();
            Content = new MainShellView();
        }
        catch (Exception ex)
        {
            Logger.Error("Avalonia shell initialization failed");
            Logger.Error(ex);
            Content = new ErrorView(
                CoreTools.Translate("The Avalonia shell failed to initialize"),
                ex.Message
            );
        }
    }

    private static string BuildWindowTitle()
    {
        var details = new List<string>();

        if (!string.IsNullOrWhiteSpace(CoreData.VersionName))
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

    private void ApplyTheme()
    {
        RequestedThemeVariant = Settings.GetValue(Settings.K.PreferredTheme) switch
        {
            "dark" => ThemeVariant.Dark,
            "light" => ThemeVariant.Light,
            _ => ThemeVariant.Default,
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
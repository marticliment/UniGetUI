using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Devolutions.AvaloniaTheme.DevExpress;
using Devolutions.AvaloniaTheme.Linux;
using Devolutions.AvaloniaTheme.MacOS;
using System.ComponentModel;

namespace UniGetUI.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        Styles.Insert(0, CreatePlatformTheme());

        Name = "UniGetUI.Avalonia";
    }

    private static Styles CreatePlatformTheme()
    {
        Styles styles = OperatingSystem.IsWindows()
            ? new DevolutionsDevExpressTheme()
            : OperatingSystem.IsMacOS()
                ? new DevolutionsMacOsTheme()
                : OperatingSystem.IsLinux()
                    ? new DevolutionsLinuxYaruTheme()
                    : new FluentTheme();

        if (styles is ISupportInitialize initializable)
        {
            initializable.BeginInit();
            initializable.EndInit();
        }

        return styles;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using UniGetUI.Core.Tools;

namespace UniGetUI.Avalonia.Views.Pages.SettingsPages;

public partial class SettingsHomeView : UserControl, ISettingsSectionView
{
    private StackPanel SectionButtonsHost => GetControl<StackPanel>("SectionButtonsPanel");

    private TextBlock IntroTitleText => GetControl<TextBlock>("IntroTitleBlock");

    private TextBlock IntroDescriptionText => GetControl<TextBlock>("IntroDescriptionBlock");

    private TextBlock SectionsLabelText => GetControl<TextBlock>("SectionsLabelBlock");

    public SettingsHomeView()
    {
        InitializeComponent();
        IntroTitleText.Text = CoreTools.Translate("Configure UniGetUI's preferences below.");
        IntroDescriptionText.Text = CoreTools.Translate("Select a category to adjust behavior, appearance, and integrations.");
        SectionsLabelText.Text = CoreTools.Translate("Sections");

        AddSectionButton(
            SettingsSectionRoute.Interface,
            CoreTools.Translate("User interface preferences"),
            CoreTools.Translate("Theme, startup page, navigation, and system tray settings.")
        );
        AddSectionButton(
            SettingsSectionRoute.General,
            CoreTools.Translate("General preferences"),
            CoreTools.Translate("Language selection and appearance preferences.")
        );
        AddSectionButton(
            SettingsSectionRoute.Updates,
            CoreTools.Translate("Update preferences"),
            CoreTools.Translate("Release checks, auto-update behavior, and related defaults.")
        );
        AddSectionButton(
            SettingsSectionRoute.Notifications,
            CoreTools.Translate("Notification preferences"),
            CoreTools.Translate("Update, success, progress, and error notification routing.")
        );
        AddSectionButton(
            SettingsSectionRoute.Operations,
            CoreTools.Translate("Operation preferences"),
            CoreTools.Translate("Install, update, uninstall, and queue defaults.")
        );
        AddSectionButton(
            SettingsSectionRoute.Internet,
            CoreTools.Translate("Internet preferences"),
            CoreTools.Translate("Proxy configuration and package-source connectivity behavior.")
        );
        AddSectionButton(
            SettingsSectionRoute.Administrator,
            CoreTools.Translate("Administrator preferences"),
            CoreTools.Translate("Administrative execution and elevation behavior.")
        );
        AddSectionButton(
            SettingsSectionRoute.Backup,
            CoreTools.Translate("Backup preferences"),
            CoreTools.Translate("Import, export, and backup storage workflows.")
        );
        AddSectionButton(
            SettingsSectionRoute.Experimental,
            CoreTools.Translate("Experimental preferences"),
            CoreTools.Translate("Feature flags and work-in-progress capabilities.")
        );
    }

    internal event EventHandler<SettingsSectionRoute>? NavigationRequested;

    public string SectionTitle => CoreTools.Translate("Settings");

    public string SectionSubtitle => CoreTools.Translate("Choose a preferences category");

    public string SectionStatus => CoreTools.Translate("9 sections");

    private void AddSectionButton(SettingsSectionRoute route, string title, string description)
    {
        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Tag = route,
        };
        button.Classes.Add("settings-tile");
        button.Click += SectionButton_OnClick;

        button.Content = new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new TextBlock
                {
                    FontSize = 16,
                    FontWeight = FontWeight.SemiBold,
                    Text = title,
                    TextWrapping = TextWrapping.Wrap,
                },
                new TextBlock
                {
                    Opacity = 0.74,
                    Text = description,
                    TextWrapping = TextWrapping.Wrap,
                },
            },
        };

        SectionButtonsHost.Children.Add(button);
    }

    private void SectionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SettingsSectionRoute route })
        {
            NavigationRequested?.Invoke(this, route);
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private T GetControl<T>(string name)
        where T : Control
    {
        return this.FindControl<T>(name)
            ?? throw new InvalidOperationException($"Control '{name}' was not found.");
    }
}
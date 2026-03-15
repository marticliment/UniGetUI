using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using UniGetUI.Core.Data;
using UniGetUI.Core.Tools;

namespace UniGetUI.Avalonia.Views.Pages;

public partial class HelpPageView : UserControl, IShellPage
{
    private TextBlock LeadTitleText => GetControl<TextBlock>("LeadTitleBlock");
    private TextBlock LeadDescriptionText => GetControl<TextBlock>("LeadDescriptionBlock");
    private TextBlock DocumentationTitleText => GetControl<TextBlock>("DocumentationTitleBlock");
    private TextBlock DocumentationDescriptionText => GetControl<TextBlock>("DocumentationDescriptionBlock");
    private TextBlock CommunityTitleText => GetControl<TextBlock>("CommunityTitleBlock");
    private TextBlock CommunityDescriptionText => GetControl<TextBlock>("CommunityDescriptionBlock");
    private TextBlock AboutTitleText => GetControl<TextBlock>("AboutTitleBlock");
    private TextBlock AboutVersionText => GetControl<TextBlock>("AboutVersionBlock");
    private TextBlock AboutLicenseText => GetControl<TextBlock>("AboutLicenseBlock");
    private Button OpenDocumentationButtonControl => GetControl<Button>("OpenDocumentationButton");
    private Button OpenChangelogButtonControl => GetControl<Button>("OpenChangelogButton");
    private Button OpenIssuesButtonControl => GetControl<Button>("OpenIssuesButton");
    private Button OpenDiscussionsButtonControl => GetControl<Button>("OpenDiscussionsButton");
    private Button OpenLicenseButtonControl => GetControl<Button>("OpenLicenseButton");
    private Button OpenContributorsButtonControl => GetControl<Button>("OpenContributorsButton");

    public string Title { get; } = CoreTools.Translate("Help");
    public string Subtitle { get; } = CoreTools.Translate("Documentation, support, and about UniGetUI");
    public bool SupportsSearch => false;
    public string SearchPlaceholder => string.Empty;

    public HelpPageView()
    {
        InitializeComponent();
        ApplyLocalizedText();
    }

    private void ApplyLocalizedText()
    {
        LeadTitleText.Text = CoreTools.Translate("Help and Support");
        LeadDescriptionText.Text = CoreTools.Translate(
            "Find documentation, report issues, and learn more about UniGetUI from the links below."
        );

        DocumentationTitleText.Text = CoreTools.Translate("Documentation");
        DocumentationDescriptionText.Text = CoreTools.Translate(
            "Read the official UniGetUI documentation for guides, configuration references, and troubleshooting tips."
        );
        OpenDocumentationButtonControl.Content = CoreTools.Translate("Open documentation");
        OpenChangelogButtonControl.Content = CoreTools.Translate("View changelog");

        CommunityTitleText.Text = CoreTools.Translate("Community and Support");
        CommunityDescriptionText.Text = CoreTools.Translate(
            "Browse open issues, ask questions, or participate in discussions on GitHub."
        );
        OpenIssuesButtonControl.Content = CoreTools.Translate("Browse GitHub issues");
        OpenDiscussionsButtonControl.Content = CoreTools.Translate("Open GitHub discussions");

        AboutTitleText.Text = CoreTools.Translate("About UniGetUI");
        AboutVersionText.Text = CoreTools.Translate("UniGetUI version {0}", CoreData.VersionName);
        AboutLicenseText.Text = CoreTools.Translate("Licensed under the LGPL-2.1 License");
        OpenLicenseButtonControl.Content = CoreTools.Translate("Open license file");
        OpenContributorsButtonControl.Content = CoreTools.Translate("View contributors");
    }

    public void UpdateSearchQuery(string query) { }

    private void OpenDocumentationButton_OnClick(object? sender, RoutedEventArgs e)
        => OpenDocumentationWindow();

    private async void OpenChangelogButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var win = new ReleaseNotesWindow();
        if (VisualRoot is Window owner)
            await win.ShowDialog(owner);
        else
            win.Show();
    }

    private void OpenIssuesButton_OnClick(object? sender, RoutedEventArgs e)
        => OpenUrl("https://github.com/marticliment/UniGetUI/issues");

    private void OpenDiscussionsButton_OnClick(object? sender, RoutedEventArgs e)
        => OpenUrl("https://github.com/marticliment/UniGetUI/discussions");

    private void OpenLicenseButton_OnClick(object? sender, RoutedEventArgs e)
        => OpenUrl("https://github.com/marticliment/UniGetUI/blob/main/LICENSE");

    private void OpenContributorsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var win = new AboutPageWindow();
        if (VisualRoot is Window owner)
            win.ShowDialog(owner);
        else
            win.Show();
    }

    private async void OpenDocumentationWindow()
    {
        var win = new DocumentationBrowserWindow();
        if (VisualRoot is Window owner)
            await win.ShowDialog(owner);
        else
            win.Show();
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch
        {
            // ignore — best effort
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

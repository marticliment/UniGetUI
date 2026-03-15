using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using UniGetUI.Core.Data;
using UniGetUI.Core.Tools;

namespace UniGetUI.Avalonia.Views.Pages;

public partial class ReleaseNotesWindow : Window
{
    private readonly string _releaseNotesUrl =
        $"https://github.com/Devolutions/UniGetUI/releases/tag/{CoreData.VersionName}";

    private TextBlock TitleBlockControl => GetControl<TextBlock>("TitleBlock");
    private TextBlock VersionBlockControl => GetControl<TextBlock>("VersionBlock");
    private TextBlock UrlLabelBlockControl => GetControl<TextBlock>("UrlLabelBlock");
    private TextBlock HintBlockControl => GetControl<TextBlock>("HintBlock");
    private TextBox UrlTextBoxControl => GetControl<TextBox>("UrlTextBox");
    private Button OpenButtonControl => GetControl<Button>("OpenButton");
    private Button CloseButtonControl => GetControl<Button>("CloseButton");

    public ReleaseNotesWindow()
    {
        InitializeComponent();

        Title = CoreTools.Translate("Release notes");
        TitleBlockControl.Text = CoreTools.Translate("Release notes");
        VersionBlockControl.Text = CoreTools.Translate("version {0}", CoreData.VersionName);
        UrlLabelBlockControl.Text = CoreTools.Translate("Release notes URL:");
        UrlTextBoxControl.Text = _releaseNotesUrl;
        HintBlockControl.Text = CoreTools.Translate("Click here for more details");
        OpenButtonControl.Content = CoreTools.Translate("Open release notes");
        CloseButtonControl.Content = CoreTools.Translate("Close");
    }

    private void OpenButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = _releaseNotesUrl, UseShellExecute = true });
        }
        catch
        {
            // best effort
        }
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
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

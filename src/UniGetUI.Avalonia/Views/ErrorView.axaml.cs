using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace UniGetUI.Avalonia.Views;

public partial class ErrorView : UserControl
{
    private TextBlock ErrorTitleText => GetControl<TextBlock>("ErrorTitleBlock");

    private TextBlock ErrorDescriptionText => GetControl<TextBlock>("ErrorDescriptionBlock");

    public ErrorView()
        : this("Shell Error", string.Empty)
    {
    }

    public ErrorView(string title, string description)
    {
        InitializeComponent();
        ErrorTitleText.Text = title;
        ErrorDescriptionText.Text = description;
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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Controls.OperationWidgets;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageOperations;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Pages.DialogPages;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class OperationFailedDialog : Page
{
    public event EventHandler<EventArgs>? Close;
    Paragraph par;

    private static SolidColorBrush errorColor = null!;
    private static SolidColorBrush debugColor = null!;

    public OperationFailedDialog(AbstractOperation operation, OperationControl opControl)
    {
        this.InitializeComponent();

        errorColor ??= (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"];
        debugColor ??= (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBrush"];

        headerContent.Text = $"{operation.Metadata.FailureMessage}.\n"
           + CoreTools.Translate("Please see the Command-line Output or refer to the Operation History for further information about the issue.");

        par = new Paragraph();
        foreach (var line in operation.GetOutput())
        {
            if (line.Item2 is AbstractOperation.LineType.Information)
            {
                par.Inlines.Add(new Run { Text = line.Item1 + "\x0a" });
            }
            else if (line.Item2 is AbstractOperation.LineType.VerboseDetails)
            {
                par.Inlines.Add(new Run { Text = line.Item1 + "\x0a", Foreground = debugColor });
            }
            else
            {
                par.Inlines.Add(new Run { Text = line.Item1 + "\x0a", Foreground = errorColor });
            }
        }

        CommandLineOutput.Blocks.Add(par);

        var CloseButton = new Button
        {
            Content = CoreTools.Translate("Close"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 30,
        };
        CloseButton.Click += (_, _) => Close?.Invoke(this, EventArgs.Empty);

        Control _retryButton;

        var retryOptions = opControl.GetRetryOptions(() => Close?.Invoke(this, EventArgs.Empty));
        if (retryOptions.Count != 0)
        {
            SplitButton RetryButton = new SplitButton
            {
                Content = CoreTools.Translate("Retry"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = 30,
            };
            RetryButton.Click += (_, _) =>
            {
                operation.Retry(AbstractOperation.RetryMode.Retry);
                Close?.Invoke(this, EventArgs.Empty);
            };
            BetterMenu menu = new();
            RetryButton.Flyout = menu;
            foreach (var opt in retryOptions)
            {
                menu.Items.Add(opt);
            }

            _retryButton = RetryButton;
        }
        else
        {
            var RetryButton = new Button
            {
                Content = CoreTools.Translate("Retry"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = 30,
            };
            RetryButton.Click += (_, _) =>
            {
                operation.Retry(AbstractOperation.RetryMode.Retry);
                Close?.Invoke(this, EventArgs.Empty);
            };
            _retryButton = RetryButton;
        }

        ButtonsLayout.Children.Add(CloseButton);
        ButtonsLayout.Children.Add(_retryButton);
        Grid.SetColumn(CloseButton, 1);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close?.Invoke(this, EventArgs.Empty);
    }
}

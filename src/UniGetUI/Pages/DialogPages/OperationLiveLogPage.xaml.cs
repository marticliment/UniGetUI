using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using UniGetUI.PackageOperations;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Pages.DialogPages;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class OperationLiveLogPage : Page
{
    public event EventHandler<EventArgs>? Close;
    private Paragraph par;
    private static SolidColorBrush errorColor = null!;
    private static SolidColorBrush debugColor = null!;
    private bool LastLineWasProgress;

    public OperationLiveLogPage(AbstractOperation operation)
    {
        this.InitializeComponent();
        errorColor ??= (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"];
        debugColor ??= (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBrush"];
        par = new Paragraph() { LineHeight = 4.8 };
        TextBlock.Blocks.Clear();
        TextBlock.Blocks.Add(par);

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
    }

    public void AddLine_ThreadSafe(object? sender, (string, AbstractOperation.LineType) line) =>
        MainApp.Dispatcher.TryEnqueue(() =>
    {
        if (LastLineWasProgress) par.Inlines.RemoveAt(par.Inlines.Count - 1);

        LastLineWasProgress = false;
        if (line.Item2 is AbstractOperation.LineType.Information)
        {
            par.Inlines.Add(new Run { Text = line.Item1 + "\x0a" });
        }
        else if (line.Item2 is AbstractOperation.LineType.VerboseDetails)
        {
            par.Inlines.Add(new Run { Text = line.Item1 + "\x0a", Foreground = debugColor });
        }
        else if (line.Item2 is AbstractOperation.LineType.Error)
        {
            par.Inlines.Add(new Run { Text = line.Item1 + "\x0a", Foreground = errorColor });
        }
        else
        {
            LastLineWasProgress = true;
            par.Inlines.Add(new Run { Text = line.Item1 + "\x0a" });
        }

        this.TextBlock.Blocks.Clear();
        this.TextBlock.Blocks.Add(par);
        this.ScrollBar.ScrollToVerticalOffset(this.ScrollBar.ScrollableHeight);

    });

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close?.Invoke(this, EventArgs.Empty);
    }
}


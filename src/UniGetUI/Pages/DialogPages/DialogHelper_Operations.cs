using ExternalLibraries.Clipboard;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageOperations;

namespace UniGetUI.Pages.DialogPages;

public static partial class DialogHelper
{
    public static async Task ShowOperationFailedDialog(AbstractOperation operation)
    {
        ContentDialog dialog = new()
        {
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style, XamlRoot = Window.XamlRoot
        };
        dialog.Resources["ContentDialogMaxWidth"] = 850;
        dialog.Resources["ContentDialogMaxHeight"] = 800;
        dialog.Title = operation.Metadata.FailureTitle;

        Grid grid = new()
        {
            RowSpacing = 16,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        TextBlock headerContent = new()
        {
            TextWrapping = TextWrapping.WrapWholeWords,
            Text = $"{operation.Metadata.FailureMessage}.\n"
                   + CoreTools.Translate("Please see the Command-line Output or refer to the Operation History for further information about the issue.")
        };

        StackPanel HeaderPanel = new() { Orientation = Orientation.Horizontal, Spacing = 8 };

        HeaderPanel.Children.Add(new LocalIcon(IconType.Console)
        {
            VerticalAlignment = VerticalAlignment.Center,
            Height = 24,
            Width = 24,
            HorizontalAlignment = HorizontalAlignment.Left
        });

        HeaderPanel.Children.Add(new TextBlock
        {
            Text = CoreTools.Translate("Command-line Output"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });

        RichTextBlock CommandLineOutput = new()
        {
            FontFamily = new FontFamily("Consolas"),
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        ScrollViewer ScrollView = new()
        {
            BorderBrush = new SolidColorBrush(),
            Content = CommandLineOutput,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        Grid OutputGrid = new();
        OutputGrid.Children.Add(ScrollView);
        OutputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        OutputGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(ScrollView, 0);
        Grid.SetRow(ScrollView, 0);

        Expander expander = new()
        {
            Header = HeaderPanel,
            Content = OutputGrid,
            CornerRadius = new CornerRadius(8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        Paragraph par = new();

        SolidColorBrush errorColor = (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"];
        SolidColorBrush debugColor = (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBrush"];

        foreach (var line in operation.GetOutput())
        {
            if (line.Item2 is AbstractOperation.LineType.StdOUT)
            {
                par.Inlines.Add(new Run { Text = line.Item1 + "\x0a" });
            }
            else if (line.Item2 is AbstractOperation.LineType.OperationInfo)
            {
                par.Inlines.Add(new Run { Text = line.Item1 + "\x0a", Foreground = debugColor });
            }
            else
            {
                par.Inlines.Add(new Run { Text = line.Item1 + "\x0a", Foreground = errorColor });
            }
        }

        CommandLineOutput.Blocks.Add(par);

        grid.Children.Add(headerContent);
        grid.Children.Add(expander);
        Grid.SetRow(headerContent, 0);
        Grid.SetRow(expander, 1);

        dialog.Content = grid;
        dialog.PrimaryButtonText = CoreTools.Translate("Retry");
        dialog.CloseButtonText = CoreTools.Translate("Close");
        dialog.DefaultButton = ContentDialogButton.Primary;

        var result = await Window.ShowDialogAsync(dialog);
        if (result is ContentDialogResult.Primary) operation.Retry();
    }

    public static async Task ShowLiveLogDialog(AbstractOperation operation)
    {
        bool LastLineWasProgress = false;

        ContentDialog OutputDialog = new ContentDialog
        {
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
            XamlRoot = Window.XamlRoot
        };
        OutputDialog.Resources["ContentDialogMaxWidth"] = 1200;
        OutputDialog.Resources["ContentDialogMaxHeight"] = 1000;

        var LiveOutputTextBlock = new RichTextBlock
        {
            Margin = new Thickness(8),
            FontFamily = new FontFamily("Consolas")
        };

        var LiveOutputScrollBar = new ScrollViewer
        {
            CornerRadius = new CornerRadius(6),
            Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"],
            Height = 400,
            Width = 600,
            Content = LiveOutputTextBlock
        };

        OutputDialog.Title = CoreTools.Translate("Live output");
        OutputDialog.CloseButtonText = CoreTools.Translate("Close");

        OutputDialog.SizeChanged += (_, _) =>
        {
            LiveOutputScrollBar.MinWidth = MainApp.Instance.MainWindow.NavigationPage.ActualWidth - 400;
            LiveOutputScrollBar.MinHeight = MainApp.Instance.MainWindow.NavigationPage.ActualHeight - 200;
        };

        OutputDialog.Content = LiveOutputScrollBar;
        LiveOutputTextBlock.Blocks.Clear();
        Paragraph par = new()
        {
            LineHeight = 4.8
        };
        SolidColorBrush errorColor = (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"];
        SolidColorBrush debugColor = (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBrush"];

        foreach (var line in operation.GetOutput())
        {
            if (line.Item2 is AbstractOperation.LineType.StdOUT)
            {
                par.Inlines.Add(new Run { Text = line.Item1 + "\x0a" });
            }
            else if (line.Item2 is AbstractOperation.LineType.OperationInfo)
            {
                par.Inlines.Add(new Run { Text = line.Item1 + "\x0a", Foreground = debugColor });
            }
            else
            {
                par.Inlines.Add(new Run { Text = line.Item1 + "\x0a", Foreground = errorColor });
            }
        }

        EventHandler<(string, AbstractOperation.LineType)> AddLineLambda = (_, line) => MainApp.Dispatcher.TryEnqueue(() =>
        {
            if(LastLineWasProgress) par.Inlines.RemoveAt(par.Inlines.Count-1);

            LastLineWasProgress = false;
            if (line.Item2 is AbstractOperation.LineType.StdOUT)
            {
                par.Inlines.Add(new Run { Text = line.Item1 + "\x0a" });
            }
            else if (line.Item2 is AbstractOperation.LineType.OperationInfo)
            {
                par.Inlines.Add(new Run { Text = line.Item1 + "\x0a", Foreground = debugColor });
            }
            else if (line.Item2 is AbstractOperation.LineType.StdERR)
            {
                par.Inlines.Add(new Run { Text = line.Item1 + "\x0a", Foreground = errorColor });
            }
            else
            {
                LastLineWasProgress = true;
                par.Inlines.Add(new Run { Text = line.Item1 + "\x0a" });
            }

            LiveOutputTextBlock.Blocks.Clear();
            LiveOutputTextBlock.Blocks.Add(par);
            LiveOutputScrollBar.ScrollToVerticalOffset(LiveOutputScrollBar.ScrollableHeight);
        });

        LiveOutputTextBlock.Blocks.Add(par);

        operation.LogLineAdded += AddLineLambda;
        if (await MainApp.Instance.MainWindow.ShowDialogAsync(OutputDialog) == ContentDialogResult.Secondary)
        {
            LiveOutputScrollBar.ScrollToVerticalOffset(LiveOutputScrollBar.ScrollableHeight);
            string text = "";
            foreach (var line in par.Inlines)
            {
                if (line is Run run)
                    text += run + "\n";
            }
            WindowsClipboard.SetText(text);
        }
        operation.LogLineAdded -= AddLineLambda;
    }
}


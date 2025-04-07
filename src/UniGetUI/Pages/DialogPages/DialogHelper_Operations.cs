using ExternalLibraries.Clipboard;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Controls.OperationWidgets;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageOperations;

namespace UniGetUI.Pages.DialogPages;

public static partial class DialogHelper
{
    public static async Task ShowOperationFailedDialog(AbstractOperation operation, OperationControl opControl)
    {
        ContentDialog dialog = DialogFactory.Create(850, 800);
        dialog.Title = operation.Metadata.FailureTitle;

        Grid grid = new()
        {
            RowSpacing = 16,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

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
            Padding = new Thickness(6),
            FontFamily = new FontFamily("Consolas"),
            TextWrapping = TextWrapping.Wrap,
        };

        ScrollViewer ScrollView = new()
        {
            MaxHeight = MainApp.Instance.MainWindow.NavigationPage.ActualHeight > 800 ? 500 : 300,
            MaxWidth = 800,
            CornerRadius = new CornerRadius(6),
            Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"],
            Content = CommandLineOutput,
            VerticalScrollMode = ScrollMode.Enabled,
            HorizontalScrollMode = ScrollMode.Disabled,
        };

        Paragraph par = new();

        SolidColorBrush errorColor = (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"];
        SolidColorBrush debugColor = (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBrush"];

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

        grid.Children.Add(headerContent);
        grid.Children.Add(ScrollView);
        Grid.SetRow(headerContent, 0);
        Grid.SetRow(ScrollView, 1);

        var CloseButton = new Button
        {
            Content = CoreTools.Translate("Close"), HorizontalAlignment = HorizontalAlignment.Stretch, Height = 30,
        };
        CloseButton.Click += (_, _) =>
        {
            dialog.Hide();
        };
        Control _retryButton;

        var retryOptions = opControl.GetRetryOptions(dialog.Hide);
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
                dialog.Hide();
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
                dialog.Hide();
            };
            _retryButton = RetryButton;
        }

        Grid sp = new()
        {
            Margin = new Thickness(-25, 0, -25, -25),
            ColumnSpacing = 8,
            Padding = new Thickness(30),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"]
        };
        sp.ColumnDefinitions.Add(new ColumnDefinition {Width = new(1, GridUnitType.Star)});
        sp.ColumnDefinitions.Add(new ColumnDefinition {Width = new(1, GridUnitType.Star)});
        sp.Children.Add(_retryButton);
        Grid.SetColumn(CloseButton, 1);
        sp.Children.Add(CloseButton);
        Grid.SetRow(sp, 3);
        Grid.SetColumn(sp, 0);
        grid.Children.Add(sp);
        dialog.Content = grid;

        await Window.ShowDialogAsync(dialog);
    }

    public static async Task ShowLiveLogDialog(AbstractOperation operation)
    {
        ContentDialog OutputDialog = DialogFactory.Create_AsWindow(hasTitle: true);
        var viewer = new OperationLiveLogPage(operation);
        viewer.Close += (_, _) => OutputDialog.Hide();
        OutputDialog.Title = CoreTools.Translate("Live output");
        OutputDialog.Content = viewer;

        operation.LogLineAdded += viewer.AddLine_ThreadSafe;
        await MainApp.Instance.MainWindow.ShowDialogAsync(OutputDialog);
        operation.LogLineAdded -= viewer.AddLine_ThreadSafe;
    }
}

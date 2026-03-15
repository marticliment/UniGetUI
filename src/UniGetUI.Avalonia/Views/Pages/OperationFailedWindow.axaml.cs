using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageOperations;

namespace UniGetUI.Avalonia.Views.Pages;

/// <summary>
/// Dialog shown when an operation finishes with <see cref="OperationStatus.Failed"/> or
/// <see cref="OperationStatus.Canceled"/>.  Replays color-coded output and offers
/// basic retry options derived from the operation's manager capabilities.
/// </summary>
public partial class OperationFailedWindow : Window
{
    private readonly AbstractOperation _operation;

    private static readonly FontFamily MonoFont =
        new("Cascadia Mono,Cascadia Code,Consolas,Courier New,monospace");

    public OperationFailedWindow(AbstractOperation operation)
    {
        _operation = operation;
        Title = CoreTools.Translate("{0} — failed", operation.Metadata.Title);

        InitializeComponent();

        var failureMsg = operation.Metadata.FailureMessage.Length > 0
            ? operation.Metadata.FailureMessage
            : CoreTools.Translate("Operation failed");

        FailureHeaderBlock.Text = failureMsg + ".\n"
            + CoreTools.Translate(
                "Please see the Command-line Output or refer to the Operation History for further information about the issue.");

        StatusHintBlock.Text = CoreTools.Translate("Command-line output:");

        foreach (var (text, type) in operation.GetOutput())
            AddLine(text, type);

        BuildRetryFlyout();

        CloseBtn.Content = CoreTools.Translate("Close");
        RetryBtn.Content = CoreTools.Translate("Retry");
    }

    // ── Log rendering ────────────────────────────────────────────────────────

    private void AddLine(string text, AbstractOperation.LineType type)
    {
        var tb = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = MonoFont,
            FontSize = 12,
        };

        switch (type)
        {
            case AbstractOperation.LineType.Error:
                tb.Foreground = new SolidColorBrush(Color.FromRgb(220, 60, 60));
                break;
            case AbstractOperation.LineType.VerboseDetails:
                tb.Opacity = 0.52;
                break;
        }

        LogLinesPanel.Children.Add(tb);
    }

    // ── Retry flyout ─────────────────────────────────────────────────────────

    private void BuildRetryFlyout()
    {
        var items = new List<(string label, string mode)>();

        if (_operation is PackageOperation pkgOp)
        {
            var caps = pkgOp.Package.Manager.Capabilities;

            if (!pkgOp.Options.RunAsAdministrator && caps.CanRunAsAdmin)
                items.Add((
                    CoreTools.Translate("Retry as administrator"),
                    AbstractOperation.RetryMode.Retry_AsAdmin));

            if (!pkgOp.Options.InteractiveInstallation && caps.CanRunInteractively)
                items.Add((
                    CoreTools.Translate("Retry interactively"),
                    AbstractOperation.RetryMode.Retry_Interactive));

            if (!pkgOp.Options.SkipHashCheck && caps.CanSkipIntegrityChecks)
                items.Add((
                    CoreTools.Translate("Retry skipping integrity checks"),
                    AbstractOperation.RetryMode.Retry_SkipIntegrity));
        }
        else if (_operation is SourceOperation srcOp && !srcOp.ForceAsAdministrator)
        {
            items.Add((
                CoreTools.Translate("Retry as administrator"),
                AbstractOperation.RetryMode.Retry_AsAdmin));
        }

        if (items.Count == 0) return;

        var flyout = new MenuFlyout();

        // Plain retry is always the first option in the flyout
        var plainRetry = new MenuItem { Header = CoreTools.Translate("Retry") };
        plainRetry.Click += (_, _) => { _operation.Retry(AbstractOperation.RetryMode.Retry); Close(); };
        flyout.Items.Add(plainRetry);
        flyout.Items.Add(new Separator());

        foreach (var (label, mode) in items)
        {
            var capturedMode = mode;
            var item = new MenuItem { Header = label };
            item.Click += (_, _) =>
            {
                _operation.Retry(capturedMode);
                Close();
            };
            flyout.Items.Add(item);
        }

        RetryBtn.Flyout = flyout;
        RetryBtn.Content = CoreTools.Translate("Retry ▾");
    }

    // ── Button handlers ──────────────────────────────────────────────────────

    private void CloseBtn_OnClick(object? sender, RoutedEventArgs e) => Close();

    private void RetryBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        // Only invoked when there is no flyout (no extra options).
        _operation.Retry(AbstractOperation.RetryMode.Retry);
        Close();
    }
}

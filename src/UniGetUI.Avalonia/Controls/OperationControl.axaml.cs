using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageOperations;

namespace UniGetUI.Avalonia.Controls;

public partial class OperationControl : UserControl
{
    private readonly AbstractOperation _operation;
    private readonly ObservableCollection<OperationControl> _parentCollection;
    private readonly DetailDialog _detailDialog;
    private string _outputLog = "";

    public OperationControl(
        AbstractOperation operation,
        ObservableCollection<OperationControl> parentCollection,
        DetailDialog detailDialog)
    {
        InitializeComponent();
        _operation = operation;
        _parentCollection = parentCollection;
        _detailDialog = detailDialog;

        TitleText.Text = _operation.Metadata.Title;
        OutputText.Text = _operation.Metadata.Status;

        // Details/Logs button always visible
        DetailsButton.IsVisible = true;

        _operation.StatusChanged += OnStatusChanged;
        _operation.OperationFinished += OnOperationFinished;

        UpdateStatusVisuals(_operation.Status);

        if (!_operation.Started)
            _ = _operation.MainThread();
    }

    private void OnStatusChanged(object? sender, OperationStatus status)
    {
        Dispatcher.UIThread.Post(() =>
        {
            UpdateStatusVisuals(status);
            OutputText.Text = status switch
            {
                OperationStatus.InQueue => "Waiting in queue...",
                OperationStatus.Running => _operation.Metadata.Status,
                _ => OutputText.Text // Keep current text
            };
        });
    }

    private void OnOperationFinished(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ProgressBar.IsVisible = false;
            ActionButtonText.Text = "Dismiss";
            UpdateStatusVisuals(_operation.Status);
            CollectOutput();

            switch (_operation.Status)
            {
                case OperationStatus.Succeeded:
                    OutputText.Text = _operation.Metadata.SuccessTitle;
                    DetailsButton.IsVisible = true;
                    break;
                case OperationStatus.Failed:
                    OutputText.Text = _operation.Metadata.FailureTitle + " — Right-click for options";
                    DetailsButton.IsVisible = true;
                    break;
                case OperationStatus.Canceled:
                    OutputText.Text = "Operation canceled";
                    break;
            }
        });
    }

    private void CollectOutput()
    {
        try
        {
            var output = _operation.GetOutput();
            var sb = new System.Text.StringBuilder();
            foreach (var entry in output)
                sb.AppendLine(entry.Item1);
            _outputLog = sb.ToString();
        }
        catch
        {
            _outputLog = "Could not retrieve operation output.";
        }
    }

    private void UpdateStatusVisuals(OperationStatus status)
    {
        StatusDot.Fill = status switch
        {
            OperationStatus.InQueue => new SolidColorBrush(Color.Parse("#888888")),
            OperationStatus.Running => new SolidColorBrush(Color.Parse("#F59E0B")),
            OperationStatus.Succeeded => new SolidColorBrush(Color.Parse("#22C55E")),
            OperationStatus.Failed => new SolidColorBrush(Color.Parse("#EF4444")),
            OperationStatus.Canceled => new SolidColorBrush(Color.Parse("#A855F7")),
            _ => new SolidColorBrush(Color.Parse("#888888")),
        };

        RootBorder.BorderBrush = status switch
        {
            OperationStatus.Failed => new SolidColorBrush(Color.Parse("#55EF4444")),
            OperationStatus.Succeeded => new SolidColorBrush(Color.Parse("#5522C55E")),
            _ => new SolidColorBrush(Color.Parse("#33808080")),
        };

        ProgressBar.IsVisible = status is OperationStatus.Running;

        bool isActive = status is OperationStatus.Running or OperationStatus.InQueue;
        ActionButtonText.Text = isActive ? "Cancel" : "Dismiss";
        CtxCancel.IsVisible = isActive;
        CtxDismiss.IsVisible = !isActive;
    }

    // ─── Button handlers ────────────────────────────────────────────────

    private void DetailsButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_operation.Status is OperationStatus.Running or OperationStatus.InQueue)
        {
            // Live mode — stream logs in real time
            _detailDialog.ShowLive(_operation);
        }
        else
        {
            // Post-operation mode — show collected output
            CollectOutput();
            if (_operation.Status == OperationStatus.Failed)
                _detailDialog.ShowError(_operation.Metadata.FailureTitle, _operation.Metadata.Title, _outputLog);
            else
                _detailDialog.ShowSuccess(_operation.Metadata.SuccessTitle, _operation.Metadata.Title, _outputLog);
        }
    }

    private void ActionButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_operation.Status is OperationStatus.Running or OperationStatus.InQueue)
            _operation.Cancel();
        else
            Dismiss();
    }

    // ─── Context menu handlers ──────────────────────────────────────────

    private void Retry_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _operation.Retry("Retry");
        ResetForRetry();
    }

    private void RetrySudo_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _operation.Retry("RetryAsAdmin");
        ResetForRetry();
    }

    private void RetryInteractive_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _operation.Retry("RetryInteractive");
        ResetForRetry();
    }

    private void RetrySkipIntegrity_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _operation.Retry("RetryNoHashCheck");
        ResetForRetry();
    }

    private void CtxCancel_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _operation.Cancel();
    }

    private void CtxDismiss_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        Dismiss();
    }

    // ─── Helpers ────────────────────────────────────────────────────────

    private void ResetForRetry()
    {
        DetailsButton.IsVisible = false;
        OutputText.Text = "Retrying...";
    }

    private void Dismiss()
    {
        Detach();
        _parentCollection.Remove(this);
    }

    public bool IsFinished => _operation.Status is OperationStatus.Succeeded
        or OperationStatus.Failed or OperationStatus.Canceled;

    public void RetryIfFailed()
    {
        if (_operation.Status == OperationStatus.Failed)
        {
            _operation.Retry("Retry");
            ResetForRetry();
        }
    }

    public void Detach()
    {
        _operation.StatusChanged -= OnStatusChanged;
        _operation.OperationFinished -= OnOperationFinished;
    }
}

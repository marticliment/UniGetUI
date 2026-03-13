using System.Text;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageOperations;

namespace UniGetUI.Avalonia.Controls;

public partial class DetailDialog : UserControl
{
    private AbstractOperation? _liveOperation;
    private readonly StringBuilder _liveLog = new();
    private int _lineCount;

    public DetailDialog()
    {
        InitializeComponent();
    }

    // ─── Static content modes (post-operation) ──────────────────────────

    public void ShowError(string title, string subtitle, string content)
    {
        DetachLive();
        DialogTitle.Text = title;
        DialogSubtitle.Text = subtitle;
        DialogContent.Text = content;
        HeaderDot.Fill = new SolidColorBrush(Color.Parse("#EF4444"));
        LiveIndicator.IsVisible = false;
        _lineCount = content.Split('\n').Length;
        LineCountText.Text = $"{_lineCount} lines";
        Overlay.IsVisible = true;
    }

    public void ShowInfo(string title, string subtitle, string content)
    {
        DetachLive();
        DialogTitle.Text = title;
        DialogSubtitle.Text = subtitle;
        DialogContent.Text = content;
        HeaderDot.Fill = new SolidColorBrush(Color.Parse("#3B82F6"));
        LiveIndicator.IsVisible = false;
        _lineCount = content.Split('\n').Length;
        LineCountText.Text = $"{_lineCount} lines";
        Overlay.IsVisible = true;
    }

    public void ShowSuccess(string title, string subtitle, string content)
    {
        DetachLive();
        DialogTitle.Text = title;
        DialogSubtitle.Text = subtitle;
        DialogContent.Text = content;
        HeaderDot.Fill = new SolidColorBrush(Color.Parse("#22C55E"));
        LiveIndicator.IsVisible = false;
        _lineCount = content.Split('\n').Length;
        LineCountText.Text = $"{_lineCount} lines";
        Overlay.IsVisible = true;
    }

    // ─── Live mode (during operation) ───────────────────────────────────

    public void ShowLive(AbstractOperation operation)
    {
        DetachLive();
        _liveOperation = operation;
        _liveLog.Clear();
        _lineCount = 0;

        DialogTitle.Text = operation.Metadata.Title;
        DialogSubtitle.Text = "Live output";
        HeaderDot.Fill = new SolidColorBrush(Color.Parse("#F59E0B")); // Amber = running
        LiveIndicator.IsVisible = true;

        // Load existing output
        try
        {
            foreach (var entry in operation.GetOutput())
            {
                _liveLog.AppendLine(entry.Item1);
                _lineCount++;
            }
        }
        catch { /* may not have output yet */ }

        DialogContent.Text = _liveLog.ToString();
        LineCountText.Text = $"{_lineCount} lines — streaming...";

        // Subscribe to new lines
        operation.LogLineAdded += OnLiveLogLine;
        operation.OperationFinished += OnLiveOperationFinished;
        operation.StatusChanged += OnLiveStatusChanged;

        Overlay.IsVisible = true;
        ScrollToBottom();
    }

    private void OnLiveLogLine(object? sender, (string Line, AbstractOperation.LineType Type) entry)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _liveLog.AppendLine(entry.Line);
            _lineCount++;
            DialogContent.Text = _liveLog.ToString();
            LineCountText.Text = $"{_lineCount} lines — streaming...";
            ScrollToBottom();
        });
    }

    private void OnLiveStatusChanged(object? sender, OperationStatus status)
    {
        Dispatcher.UIThread.Post(() =>
        {
            HeaderDot.Fill = status switch
            {
                OperationStatus.Running => new SolidColorBrush(Color.Parse("#F59E0B")),
                OperationStatus.Succeeded => new SolidColorBrush(Color.Parse("#22C55E")),
                OperationStatus.Failed => new SolidColorBrush(Color.Parse("#EF4444")),
                OperationStatus.Canceled => new SolidColorBrush(Color.Parse("#A855F7")),
                _ => new SolidColorBrush(Color.Parse("#888888")),
            };
        });
    }

    private void OnLiveOperationFinished(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            LiveIndicator.IsVisible = false;
            DialogSubtitle.Text = _liveOperation?.Status switch
            {
                OperationStatus.Succeeded => "Completed successfully",
                OperationStatus.Failed => "Operation failed",
                OperationStatus.Canceled => "Operation canceled",
                _ => "Finished",
            };
            LineCountText.Text = $"{_lineCount} lines";
        });
    }

    private void DetachLive()
    {
        if (_liveOperation is not null)
        {
            _liveOperation.LogLineAdded -= OnLiveLogLine;
            _liveOperation.OperationFinished -= OnLiveOperationFinished;
            _liveOperation.StatusChanged -= OnLiveStatusChanged;
            _liveOperation = null;
        }
    }

    private void ScrollToBottom()
    {
        ContentScroller.ScrollToEnd();
    }

    // ─── UI handlers ────────────────────────────────────────────────────

    public void Close()
    {
        DetachLive();
        Overlay.IsVisible = false;
    }

    private void CloseButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void Background_Click(object? sender, global::Avalonia.Input.PointerPressedEventArgs e)
    {
        Close();
    }

    private async void CopyButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
            await clipboard.SetTextAsync(DialogContent.Text ?? "");
    }
}

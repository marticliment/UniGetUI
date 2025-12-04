using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Controls.OperationWidgets;
using UniGetUI.Core.Data;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageOperations;

namespace UniGetUI.Pages.DialogPages;

/// <summary>
/// Dialog shown when a package operation fails.
/// Supports auto-dismiss functionality with configurable timeout and pause-on-hover.
/// </summary>
public sealed partial class OperationFailedDialog : Page, IDisposable
{
    /// <summary>
    /// Event raised when the dialog should be closed.
    /// </summary>
    public event EventHandler<EventArgs>? Close;
    
    private readonly Paragraph _outputParagraph;
    private DispatcherTimer? _autoDismissTimer;
    private int _remainingSeconds;
    private int _lastAnnouncedAutoDismissSeconds = -1;

    private static SolidColorBrush? _errorColor;
    private static SolidColorBrush? _debugColor;

    // Configuration constants
    private const int DEFAULT_AUTO_DISMISS_SECONDS = 10;
    private const int MIN_AUTO_DISMISS_SECONDS = 3;
    private const int MAX_AUTO_DISMISS_SECONDS = 60;
    private const string AUTO_DISMISS_ENABLED_SETTING = "AutoDismissFailureDialogs";
    private const string AUTO_DISMISS_TIMEOUT_SETTING = "AutoDismissFailureDialogsTimeout";
    
    // UI constants
    private const int BUTTON_HEIGHT = 30;

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationFailedDialog"/> class.
    /// </summary>
    /// <param name="operation">The failed operation.</param>
    /// <param name="opControl">The operation control widget.</param>
    public OperationFailedDialog(AbstractOperation operation, OperationControl opControl)
    {
        InitializeComponent();

        InitializeColors();
        SetupHeader(operation);
        SetupOutput(operation);
        SetupButtons(operation, opControl);
        InitializeAutoDismiss();
    }

    private void InitializeColors()
    {
        try
        {
            _errorColor ??= (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"];
            _debugColor ??= (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBrush"];
        }
        catch
        {
            // Fallback brushes to avoid throwing if resources aren't present or are of an unexpected type.
            _errorColor ??= new SolidColorBrush(Microsoft.UI.Colors.Red);
            _debugColor ??= new SolidColorBrush(Microsoft.UI.Colors.Gray);
        }
    }

    private void SetupHeader(AbstractOperation operation)
    {
        var failureMessage = operation.Metadata?.FailureMessage ?? CoreTools.Translate("An unknown error occurred");
        headerContent.Text = $"{failureMessage}.\n"
           + CoreTools.Translate("Please see the Command-line Output or refer to the Operation History for further information about the issue.");
    }

    private void SetupOutput(AbstractOperation operation)
    {
        _outputParagraph = new Paragraph();
        PopulateOutput(operation);
        CommandLineOutput.Blocks.Add(_outputParagraph);
    }

    private void PopulateOutput(AbstractOperation operation)
    {
        foreach (var line in operation.GetOutput())
        {
            var run = new Run { Text = line.Item1 + "\x0a" };
            
            run.Foreground = line.Item2 switch
            {
                AbstractOperation.LineType.VerboseDetails => _debugColor,
                AbstractOperation.LineType.Error => _errorColor,
                _ => run.Foreground
            };
            
            _outputParagraph.Inlines.Add(run);
        }
    }

    private void SetupButtons(AbstractOperation operation, OperationControl opControl)
    {
        var closeButton = CreateButton(
            CoreTools.Translate("Close"),
            () => CloseDialog()
        );

        var retryOptions = opControl.GetRetryOptions(() => CloseDialog());
        var retryButton = CreateRetryButton(operation, retryOptions);

        ButtonsLayout.Children.Add(closeButton);
        ButtonsLayout.Children.Add(retryButton);
        Grid.SetColumn(closeButton, 1);
    }

    private Button CreateButton(string content, Action clickHandler)
    {
        var button = new Button
        {
            Content = content,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = BUTTON_HEIGHT,
        };
        button.Click += (_, _) => clickHandler();
        return button;
    }

    private Control CreateRetryButton(AbstractOperation operation, System.Collections.Generic.List<MenuFlyoutItemBase> retryOptions)
    {
        if (retryOptions.Count != 0)
        {
            var splitButton = new SplitButton
            {
                Content = CoreTools.Translate("Retry"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = BUTTON_HEIGHT,
            };
            splitButton.Click += (_, _) =>
            {
                operation.Retry(AbstractOperation.RetryMode.Retry);
                CloseDialog();
            };
            
            var menu = new BetterMenu();
            foreach (var opt in retryOptions)
            {
                menu.Items.Add(opt);
            }
            splitButton.Flyout = menu;
            
            return splitButton;
        }
        
        return CreateButton(
            CoreTools.Translate("Retry"),
            () =>
            {
                operation.Retry(AbstractOperation.RetryMode.Retry);
                CloseDialog();
            }
        );
    }

    #region Auto-Dismiss Logic

    private void InitializeAutoDismiss()
    {
        var timeout = GetAutoDismissTimeout();
        if (timeout is null)
            return;

        try
        {
            _remainingSeconds = timeout.Value;
            _autoDismissTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _autoDismissTimer.Tick += AutoDismissTimer_Tick;
            
            UpdateAutoDismissUi(open: true);
            _autoDismissTimer.Start();
        }
        catch (Exception ex)
        {
            // If auto-dismiss setup fails, dialog remains open indefinitely
            // This is safer than crashing the dialog
            System.Diagnostics.Debug.WriteLine($"Auto-dismiss initialization failed: {ex.Message}");
            StopAutoDismiss();
        }
    }

    /// <summary>
    /// Gets the auto-dismiss timeout in seconds, or null if auto-dismiss is disabled.
    /// </summary>
    /// <returns>Timeout in seconds (3-60), or null if disabled.</returns>
    private int? GetAutoDismissTimeout()
    {
        if (!Settings.Get(AUTO_DISMISS_ENABLED_SETTING, true))
            return null;

        var timeout = Settings.Get(AUTO_DISMISS_TIMEOUT_SETTING, DEFAULT_AUTO_DISMISS_SECONDS);
        return Math.Clamp(timeout, MIN_AUTO_DISMISS_SECONDS, MAX_AUTO_DISMISS_SECONDS);
    }

    private void AutoDismissTimer_Tick(object? sender, object e)
    {
        if (_autoDismissTimer is null)
            return; // Already stopped

        _remainingSeconds--;

        if (_remainingSeconds <= 0)
        {
            StopAutoDismiss();
            CloseDialog();
        }
        else
        {
            UpdateAutoDismissUi(open: true);
        }
    }

    /// <summary>
    /// Updates the auto-dismiss InfoBar visibility and message.
    /// Throttles accessibility announcements to avoid noisy per-second updates.
    /// </summary>
    /// <param name="open">Whether the InfoBar should be open.</param>
    private void UpdateAutoDismissUi(bool open)
    {
        if (AutoDismissInfoBar is null)
            return;

        AutoDismissInfoBar.IsOpen = open;
        if (!open)
            return;

        var message = _remainingSeconds == 1
            ? CoreTools.Translate("This dialog will close in 1 second")
            : string.Format(CoreTools.Translate("This dialog will close in {0} seconds"), _remainingSeconds);

        // Always update the visible message so the countdown is discoverable
        AutoDismissInfoBar.Message = message;

        // Throttle accessibility announcements to avoid noisy per-second updates.
        // Only announce at key intervals (every 5 seconds) and during the last 5 seconds.
        var shouldAnnounce =
            _remainingSeconds <= 5 ||
            _remainingSeconds % 5 == 0;

        if (shouldAnnounce && _lastAnnouncedAutoDismissSeconds != _remainingSeconds)
        {
            AutoDismissInfoBar.SetValue(
                Microsoft.UI.Xaml.Automation.AutomationProperties.NameProperty,
                message
            );
            _lastAnnouncedAutoDismissSeconds = _remainingSeconds;
        }

        // Set live setting only once (on first update)
        if (_lastAnnouncedAutoDismissSeconds == -1)
        {
            AutoDismissInfoBar.SetValue(
                Microsoft.UI.Xaml.Automation.AutomationProperties.LiveSettingProperty,
                Microsoft.UI.Xaml.Automation.Peers.AutomationLiveSetting.Polite
            );
        }
    }

    private void KeepOpenButton_Click(object sender, RoutedEventArgs e)
    {
        StopAutoDismiss();
        UpdateAutoDismissUi(open: false);
    }

    private void Page_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _autoDismissTimer?.Stop();
    }

    private void Page_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _autoDismissTimer?.Start();
    }

    /// <summary>
    /// Stops the auto-dismiss timer and cleans up resources.
    /// This is the single place where the timer is torn down.
    /// Idempotent - safe to call multiple times.
    /// </summary>
    private void StopAutoDismiss()
    {
        if (_autoDismissTimer is null)
            return;

        _autoDismissTimer.Stop();
        _autoDismissTimer.Tick -= AutoDismissTimer_Tick;
        _autoDismissTimer = null;
    }

    #endregion

    private void CloseDialog()
    {
        StopAutoDismiss();
        Close?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting resources.
    /// </summary>
    public void Dispose()
    {
        StopAutoDismiss();
    }
}
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using Windows.Devices.Sensors;
using Windows.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Packages.Classes;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageOperations;
using UniGetUI.Pages.DialogPages;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine.Operations;

namespace UniGetUI.Controls.OperationWidgets;

public class OperationControl: INotifyPropertyChanged
{
    public AbstractOperation Operation;
    private bool ErrorTooltipShown;
    public BetterMenu OpMenu;
    public OperationStatus? MenuStateOnLoaded;

    public OperationControl(AbstractOperation operation)
    {
        OpMenu = new BetterMenu();
        Operation = operation;
        Operation.LogLineAdded += (_, values) => LiveLine = values.Item1;
        Operation.StatusChanged += OnOperationStatusChanged;
        Operation.OperationStarting += OnOperationStarting;
        Operation.OperationFinished += OnOperationFinished;
        Operation.OperationFailed += OnOperationFailed;
        Operation.OperationSucceeded += OnOperationSucceeded;

        _title = Operation.Metadata.Title;
        _liveLine = operation.GetOutput().Any()? operation.GetOutput().Last().Item1 : CoreTools.Translate("Please wait...");
        _buttonText = "";
        OnOperationStatusChanged(this, operation.Status);
        LoadIcon();
        if (!operation.Started)
            _ = operation.MainThread();
    }

    private void OnOperationStarting(object? sender, EventArgs e)
    {
        ShowProgressToast();
        MainApp.Tooltip.OperationsInProgress++;
        MainApp.Instance.MainWindow.NavigationPage.OperationList.SmoothScrollIntoViewWithItemAsync(this);
    }

    private async void OnOperationSucceeded(object? sender, EventArgs e)
    {
        // Success notification
        ShowSuccessToast();

        // Handle UAC for batches
        if (Settings.Get("DoCacheAdminRightsForBatches"))
        {
            bool isOpRunning = false;
            foreach (var op in MainApp.Operations._operationList)
            {
                if (op.Operation.Status is OperationStatus.Running or OperationStatus.InQueue)
                {
                    isOpRunning = true;
                    break;
                }
            }
            if(!isOpRunning) await CoreTools.ResetUACForCurrentProcess();
        }

        // Clean succesful operation from list
        if(!Settings.Get("MaintainSuccessfulInstalls"))
            await TimeoutAndClose();
    }

    private void OnOperationFailed(object? sender, EventArgs e)
    {
        ShowErrorToast();
    }

    private void OnOperationFinished(object? sender, EventArgs e)
    {
        // Remove progress notification (if any)
        AppNotificationManager.Default.RemoveByTagAsync(Operation.Metadata.Identifier + "progress");

        MainApp.Tooltip.OperationsInProgress--;

        // Generate process output
        List<string> rawOutput = new();
        rawOutput.Add("                           ");
        rawOutput.Add("▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄");
        foreach (var line in Operation.GetOutput())
        {
            rawOutput.Add(line.Item1);
        }

        string[] oldHistory = Settings.GetValue("OperationHistory").Split("\n");
        if (oldHistory.Length > 300) oldHistory = oldHistory.Take(300).ToArray();

        List<string> newHistory = [.. rawOutput, .. oldHistory];
        Settings.SetValue("OperationHistory", string.Join('\n', newHistory));
        rawOutput.Add("");
        rawOutput.Add("");
        rawOutput.Add("");
    }

    private async void LoadIcon()
    {
        Icon = await Operation.GetOperationIcon();
    }

    private void OnOperationStatusChanged(object? sender, OperationStatus newStatus)
    {
        switch (newStatus)
        {
            case OperationStatus.InQueue:
                ProgressIndeterminate = false;
                ProgressValue = 0;
                ProgressForeground = (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBrush"];
                Background = (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBackgroundBrush"];
                ButtonText = CoreTools.Translate("Cancel");
                break;
            case OperationStatus.Running:
                ProgressIndeterminate = true;
                ButtonText = CoreTools.Translate("Cancel");
                ProgressForeground = (SolidColorBrush)Application.Current.Resources["SystemFillColorAttentionBrush"];
                Background = (SolidColorBrush)Application.Current.Resources["SystemFillColorAttentionBackgroundBrush"];
                break;
            case OperationStatus.Succeeded:
                ProgressIndeterminate = false;
                ProgressValue = 100;
                ProgressForeground = (SolidColorBrush)Application.Current.Resources["SystemFillColorSuccessBrush"];
                Background = (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBackgroundBrush"];
                ButtonText = CoreTools.Translate("Close");
                break;
            case OperationStatus.Failed:
                ProgressIndeterminate = false;
                ProgressValue = 100;
                ProgressForeground = (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                Background = (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBackgroundBrush"];
                ButtonText = CoreTools.Translate("Close");
                break;
            case OperationStatus.Canceled:
                ProgressIndeterminate = false;
                ProgressValue = 100;
                ProgressForeground = (SolidColorBrush)Application.Current.Resources["SystemFillColorCautionBrush"];
                Background = (SolidColorBrush)Application.Current.Resources["SystemFillColorNeutralBackgroundBrush"];
                ButtonText = CoreTools.Translate("Close");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newStatus), newStatus, null);
        }

        // Handle error tooltip counter
        if (!ErrorTooltipShown && newStatus is OperationStatus.Failed)
        {
            MainApp.Tooltip.ErrorsOccurred++;
            ErrorTooltipShown = true;
        }
        else if (ErrorTooltipShown && newStatus is not OperationStatus.Failed)
        {
            MainApp.Tooltip.ErrorsOccurred--;
            ErrorTooltipShown = false;
        }
    }

    public async Task LiveLineClick()
    {
        if (Operation.Status is OperationStatus.Failed or OperationStatus.Canceled)
        {
            await DialogHelper.ShowOperationFailedDialog(Operation, this);
        }
        else
        {
            await DialogHelper.ShowLiveLogDialog(Operation);
        }
    }

    public void ButtonClick()
    {
        if (Operation.Status is OperationStatus.Running or OperationStatus.InQueue)
        {
            Operation.Cancel();
        }
        else
        {
            Close();
        }
    }

    public void LoadMenu()
    {
        if (MenuStateOnLoaded == Operation.Status) return;
        MenuStateOnLoaded = Operation.Status;

        // Reset menu
        OpMenu.Items.Clear();

        // Load operation-specific entries
        var normalOptions = GetOperationOptions();
        if (normalOptions.Any())
        {
            foreach(var item in normalOptions)
                OpMenu.Items.Add(item);

            OpMenu.Items.Add(new MenuFlyoutSeparator());
        }

        // Create Cancel/Retry buttons
        if (Operation.Status is OperationStatus.InQueue or OperationStatus.Running)
        {
            var cancel = new BetterMenuItem() { Text = CoreTools.Translate("Cancel"), IconName = IconType.Cross, };
            cancel.Click += (_, _) => Operation.Cancel();
            OpMenu.Items.Add(cancel);
        }
        else
        {
            var retry = new BetterMenuItem() { Text = CoreTools.Translate("Retry"), IconName = IconType.Reload, };
            retry.Click += (_, _) => Operation.Retry(AbstractOperation.RetryMode.Retry);
            OpMenu.Items.Add(retry);

            // Add extra retry options, if applicable
            var extraRetry = GetRetryOptions(() => { });
            if (extraRetry.Any())
            {
                OpMenu.Items.Add(new MenuFlyoutSeparator());

                foreach(var item in extraRetry)
                    OpMenu.Items.Add(item);
            }
        }
    }

    private async Task TimeoutAndClose()
    {
        var oldStatus = Operation.Status;
        await Task.Delay(5000);

        if (Operation.Status == oldStatus)
            Close();
    }

    public void Close()
    {
        MainApp.Operations._operationList.Remove(this);
        while(AbstractOperation.OperationQueue.Remove(Operation));

        if (MainApp.Operations._operationList.Count == 0
            && DesktopShortcutsDatabase.GetUnknownShortcuts().Any()
            && Settings.Get("AskToDeleteNewDesktopShortcuts"))
        {
            _ = DialogHelper.HandleNewDesktopShortcuts();
        }
    }

    private string _buttonText;
    public string ButtonText
    {
        get => _buttonText;
        set { _buttonText = value; OnPropertyChanged(); }
    }

    private string _liveLine;
    public string LiveLine
    {
        get => _liveLine;
        set { _liveLine = value; OnPropertyChanged(); }
    }

    private string _title;
    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); }
    }

    private bool _progressIndeterminate;
    public bool ProgressIndeterminate
    {
        get => _progressIndeterminate;
        set { _progressIndeterminate = value; OnPropertyChanged(); }
    }

    private int _progressValue;
    public int ProgressValue
    {
        get => _progressValue;
        set { _progressValue = value; OnPropertyChanged(); }
    }

    private Uri _icon = new("ms-appx:///Assets/images/package_color.png");
    public Uri Icon
    {
        get => _icon;
        set { _icon = value; OnPropertyChanged(); }
    }

    private SolidColorBrush _background = new(Color.FromArgb(0, 0, 0, 0));
    public SolidColorBrush Background
    {
        get => _background;
        set { _background = value; OnPropertyChanged(); }
    }

    private SolidColorBrush _progressForeground = new(Color.FromArgb(0, 0, 0, 0));
    public SolidColorBrush ProgressForeground
    {
        get => _progressForeground;
        set { _progressForeground = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        MainApp.Dispatcher.TryEnqueue(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
    }


    private void ShowProgressToast()
    {
        if (Settings.AreProgressNotificationsDisabled())
            return;

        try
        {
            AppNotificationManager.Default.RemoveByTagAsync(Operation.Metadata.Identifier + "progress");
            AppNotificationBuilder builder = new AppNotificationBuilder()
                .SetScenario(AppNotificationScenario.Default)
                .SetTag(Operation.Metadata.Identifier + "progress")
                .AddProgressBar(new AppNotificationProgressBar()
                    .SetStatus(CoreTools.Translate("Please wait..."))
                    .SetValueStringOverride("\u2003")
                    .SetTitle(Operation.Metadata.Status)
                    .SetValue(1.0))
                .AddArgument("action", NotificationArguments.Show);
            AppNotification notification = builder.BuildNotification();
            notification.ExpiresOnReboot = true;
            notification.SuppressDisplay = true;
            AppNotificationManager.Default.Show(notification);
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to show toast notification");
            Logger.Error(ex);
        }
    }

    private void ShowSuccessToast()
    {
        if (Settings.AreSuccessNotificationsDisabled())
            return;

        try
        {
            AppNotificationManager.Default.RemoveByTagAsync(Operation.Metadata.Identifier);
            AppNotificationBuilder builder = new AppNotificationBuilder()
                .SetScenario(AppNotificationScenario.Default)
                .SetTag(Operation.Metadata.Identifier)
                .AddText(Operation.Metadata.SuccessTitle)
                .AddText(Operation.Metadata.SuccessMessage)
                .AddArgument("action", NotificationArguments.Show);
            AppNotification notification = builder.BuildNotification();
            notification.ExpiresOnReboot = true;
            AppNotificationManager.Default.Show(notification);
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to show toast notification");
            Logger.Error(ex);
        }
    }

    private void ShowErrorToast()
    {
        if (Settings.AreErrorNotificationsDisabled())
            return;

        try
        {
            AppNotificationManager.Default.RemoveByTagAsync(Operation.Metadata.Identifier);
            AppNotificationBuilder builder = new AppNotificationBuilder()
                .SetScenario(AppNotificationScenario.Urgent)
                .SetTag(Operation.Metadata.Identifier)
                .AddText(Operation.Metadata.FailureTitle)
                .AddText(Operation.Metadata.FailureMessage)
                .AddArgument("action", NotificationArguments.Show);
            AppNotification notification = builder.BuildNotification();
            notification.ExpiresOnReboot = true;
            AppNotificationManager.Default.Show(notification);
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to show toast notification");
            Logger.Error(ex);
        }
    }

    public List<BetterMenuItem> GetRetryOptions(Action callback)
    {
        var retryOptionsMenu = new List<BetterMenuItem>();

        if (Operation is SourceOperation sourceOp && !sourceOp.ForceAsAdministrator)
        {
            var adminButton = new BetterMenuItem() { Text = CoreTools.Translate("Retry as administrator") };
            adminButton.IconName = IconType.UAC;
            adminButton.Click += (_, _) =>
            {
                callback();
                Operation.Retry(AbstractOperation.RetryMode.Retry_AsAdmin);
            };
            retryOptionsMenu.Add(adminButton);
        }
        else if (Operation is PackageOperation packageOp)
        {
            if (!packageOp.Options.RunAsAdministrator && packageOp.Package.Manager.Capabilities.CanRunAsAdmin)
            {
                var adminButton = new BetterMenuItem() { Text = CoreTools.Translate("Retry as administrator") };
                adminButton.IconName = IconType.UAC;
                adminButton.Click += (_, _) =>
                {
                    callback();
                    Operation.Retry(AbstractOperation.RetryMode.Retry_AsAdmin);
                };
                retryOptionsMenu.Add(adminButton);
            }

            if (!packageOp.Options.InteractiveInstallation &&
                packageOp.Package.Manager.Capabilities.CanRunInteractively)
            {
                var interactiveButton = new BetterMenuItem() { Text = CoreTools.Translate("Retry interactively") };
                interactiveButton.IconName = IconType.Interactive;
                interactiveButton.Click += (_, _) =>
                {
                    callback();
                    Operation.Retry(AbstractOperation.RetryMode.Retry_Interactive);
                };
                retryOptionsMenu.Add(interactiveButton);
            }

            if (!packageOp.Options.SkipHashCheck && packageOp.Package.Manager.Capabilities.CanSkipIntegrityChecks)
            {
                var skiphashButton =
                    new BetterMenuItem() { Text = CoreTools.Translate("Retry skipping integrity checks") };
                skiphashButton.IconName = IconType.Checksum;
                skiphashButton.Click += (_, _) =>
                {
                    callback();
                    Operation.Retry(AbstractOperation.RetryMode.Retry_SkipIntegrity);
                };
                retryOptionsMenu.Add(skiphashButton);
            }
        }

        return retryOptionsMenu;
    }

    public List<BetterMenuItem> GetOperationOptions()
    {
        var optionsMenu = new List<BetterMenuItem>();
        if (Operation is PackageOperation packageOp)
        {
            var details = new BetterMenuItem() { Text = CoreTools.Translate("Package details") };
            details.IconName = IconType.Info_Round;
            details.Click += (_, _) =>
            {
                DialogHelper.ShowPackageDetails(packageOp.Package, OperationType.None);
            };
            optionsMenu.Add(details);


            var installationSettings = new BetterMenuItem() { Text = CoreTools.Translate("Installation options") };
            installationSettings.IconName = IconType.Options;
            installationSettings.Click += (_, _) =>
            {
                _ = DialogHelper.ShowInstallatOptions_Continue(packageOp.Package, OperationType.None);
            };
            optionsMenu.Add(installationSettings);

            string? location = packageOp.Package.Manager.DetailsHelper.GetInstallLocation(packageOp.Package);
            if (location is not null && Directory.Exists(location))
            {
                var openLocation = new BetterMenuItem() { Text = CoreTools.Translate("Open install location") };
                openLocation.IconName = IconType.OpenFolder;
                openLocation.Click += (_, _) =>
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
                        FileName = location,
                        UseShellExecute = true,
                        Verb = "open"
                    });

                };
                optionsMenu.Add(openLocation);
            }

        }

        return optionsMenu;
    }
}

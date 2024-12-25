using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Windows.Devices.Sensors;
using Windows.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageOperations;
using UniGetUI.Pages.DialogPages;

namespace UniGetUI.Controls.OperationWidgets;

public class OperationControl: INotifyPropertyChanged
{
    public AbstractOperation Operation;

    public OperationControl(AbstractOperation operation)
    {
        Operation = operation;
        Operation.LogLineAdded += (_, values) => LiveLine = values.Item1;
        Operation.StatusChanged += OperationOnStatusChanged;
        _title = Operation.Metadata.Title;
        _liveLine = operation.GetOutput().Any()? operation.GetOutput().Last().Item1 : CoreTools.Translate("Please wait...");
        _buttonText = "";
        OperationOnStatusChanged(this, operation.Status);
        LoadIcon();
        if (!operation.Started)
            _ = operation.MainThread();
    }

    private async void LoadIcon()
    {
        Icon = await Operation.GetOperationIcon();
    }

    private void OperationOnStatusChanged(object? sender, OperationStatus newStatus)
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
    }

    public void LiveLineClick()
    {
        if (Operation.Status == OperationStatus.Failed || true)
        {
            DialogHelper.ShowOperationFailed(Operation.GetOutput(), "", "");
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

    public void ShowMenu()
    {
        // throw new NotImplementedException();
    }

    public void Close()
    {
        MainApp.Operations._operationList.Remove(this);
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
}

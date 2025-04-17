using Microsoft.UI.Xaml.Controls;
using UniGetUI.Controls.OperationWidgets;
using UniGetUI.Core.Tools;
using UniGetUI.PackageOperations;

namespace UniGetUI.Pages.DialogPages;

public static partial class DialogHelper
{
    public static async Task ShowOperationFailedDialog(AbstractOperation operation, OperationControl opControl)
    {
        ContentDialog dialog = DialogFactory.Create_AsWindow(true, true);
        dialog.Title = operation.Metadata.FailureTitle;
        var contents = new OperationFailedDialog(operation, opControl);

        dialog.Content = contents;
        contents.Close += (_, _) => dialog.Hide();

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

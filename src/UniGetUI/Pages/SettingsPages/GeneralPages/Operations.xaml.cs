using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using UniGetUI.Core.Tools;
using UniGetUI.Pages.DialogPages;
using UniGetUI.PackageOperations;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Pages.SettingsPages.GeneralPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Operations : Page, ISettingsPage
    {
        public Operations()
        {
            this.InitializeComponent();
        }

        public void ShowRestartBanner(object sender, EventArgs e)
            => RestartRequired?.Invoke(this, e);
        private void ManageDesktopShortcutsButton_Click(object sender, RoutedEventArgs e)
            => _ = DialogHelper.ManageDesktopShortcuts();

        public bool CanGoBack => true;
        public string ShortTitle => CoreTools.Translate("Operation preferences");

        public event EventHandler? RestartRequired;
        public event EventHandler<Type>? NavigationRequested;

        private void ParallelOperationCount_OnValueChanged(object sender, EventArgs e)
        {
            if (int.TryParse(ParallelOperationCount.SelectedValue(), out int value))
            {
                AbstractOperation.MAX_OPERATIONS = value;
            }
        }
    }
}

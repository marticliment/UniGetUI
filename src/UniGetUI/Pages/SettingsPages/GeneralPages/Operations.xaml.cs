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

            Dictionary<string, string> updates_dict = new()
                {
                    { CoreTools.Translate("{0} minutes", 10), "600" },
                    { CoreTools.Translate("{0} minutes", 30), "1800" },
                    { CoreTools.Translate("1 hour"), "3600" },
                    { CoreTools.Translate("{0} hours", 2), "7200" },
                    { CoreTools.Translate("{0} hours", 4), "14400" },
                    { CoreTools.Translate("{0} hours", 8), "28800" },
                    { CoreTools.Translate("{0} hours", 12), "43200" },
                    { CoreTools.Translate("1 day"), "86400" },
                    { CoreTools.Translate("{0} days", 2), "172800" },
                    { CoreTools.Translate("{0} days", 3), "259200" },
                    { CoreTools.Translate("1 week"), "604800" }
                };

            foreach (KeyValuePair<string, string> entry in updates_dict)
            {
                UpdatesCheckIntervalSelector.AddItem(entry.Key, entry.Value, false);
            }

            UpdatesCheckIntervalSelector.ShowAddedItems();

            for (int i = 1; i <= 10; i++)
            {
                ParallelOperationCount.AddItem(i.ToString(), i.ToString(), false);
            }

            ParallelOperationCount.AddItem("15", "15", false);
            ParallelOperationCount.AddItem("20", "20", false);
            ParallelOperationCount.AddItem("30", "30", false);
            ParallelOperationCount.AddItem("50", "50", false);
            ParallelOperationCount.AddItem("75", "75", false);
            ParallelOperationCount.AddItem("100", "100", false);
            ParallelOperationCount.ShowAddedItems();
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

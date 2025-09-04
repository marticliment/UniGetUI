using Windows.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Widgets;
using UniGetUI.Pages.SettingsPages.GeneralPages;
using UniGetUI.PackageEngine;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Pages.DialogPages;
using CommunityToolkit.WinUI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Pages.SettingsPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ManagersHomepage : Page, ISettingsPage
    {
        public bool CanGoBack => false;
        public string ShortTitle => CoreTools.Translate("Package manager preferences");

        public event EventHandler? RestartRequired;

        public event EventHandler<Type>? NavigationRequested;

        private List<SettingsPageButton> managerControls = new();

        private bool _isLoadingToggles = false;
        public ManagersHomepage()
        {
            this.InitializeComponent();

            bool first = true;
            foreach(var manager in PEInterface.Managers)
            {
                var button = new SettingsPageButton()
                {
                    Text = manager.DisplayName,
                    Description = manager.Properties.Description.Split("<br>")[0],
                    HeaderIcon = new LocalIcon(manager.Properties.IconId),
                    Padding = new Thickness(16, 2, 16, 2)
                };
                button.CornerRadius = first ? new CornerRadius(8, 8, 0, 0) : new CornerRadius(0);
                button.BorderThickness = first ? new Thickness(1) : new Thickness(1,0,1,1);
                button.Click += (_, _) => NavigationRequested?.Invoke(this, manager.GetType());

                var statusIcon = new FontIcon() { FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
                var statusText = new TextBlock() { FontSize = 12, FontWeight = new FontWeight(600), VerticalAlignment = VerticalAlignment.Center };
                var statusBorder = new Border()
                {
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 3, 6, 3)
                };

                void loadStatusBadge()
                {
                    if (!manager.IsEnabled())
                    {
                        statusText.Text = CoreTools.Translate("Disabled");
                        statusIcon.Glyph = "\uE814";
                        statusIcon.Foreground = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
                        statusBorder.Background = (Brush)Application.Current.Resources["SystemFillColorCautionBackgroundBrush"];
                    }
                    else if (manager.Status.Found)
                    {
                        statusText.Text = CoreTools.Translate("Ready");
                        statusIcon.Glyph = "\uEC61";
                        statusIcon.Foreground = (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
                        statusBorder.Background = (Brush)Application.Current.Resources["SystemFillColorSuccessBackgroundBrush"];
                    }
                    else
                    {
                        statusText.Text = CoreTools.Translate("Not found");
                        statusIcon.Glyph = "\uEB90";
                        statusIcon.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                        statusBorder.Background = (Brush)Application.Current.Resources["SystemFillColorCriticalBackgroundBrush"];
                    }
                }

                loadStatusBadge();
                var toggle = new ToggleSwitch()
                {
                    Height = 22,
                    OnContent = "",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    OffContent = "",
                    Margin = new Thickness(-10, 0, 0, 0),
                };
                toggle.Toggled += async (_, _) =>
                {
                    if (_isLoadingToggles) return;

                    bool disabled = !toggle.IsOn;
                    int loadingId = DialogHelper.ShowLoadingDialog(CoreTools.Translate("Please wait..."));
                    Settings.SetDictionaryItem(Settings.K.DisabledManagers, manager.Name, disabled);
                    await Task.Run(manager.Initialize);
                    loadStatusBadge();
                    DialogHelper.HideLoadingDialog(loadingId);
                };

                var status = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center,
                    Children = { statusIcon, statusText }
                };
                statusBorder.Child = status;
                button.Content = new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 4,
                    Children = { toggle, statusBorder }
                };

                first = false;
                SettingsEntries.Children.Add(button);
                managerControls.Add(button);
            }
            var last = (SettingsPageButton)SettingsEntries.Children[^1];
            last.CornerRadius = new CornerRadius(0, 0, 8, 8);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _isLoadingToggles = true;
            for(int i = 0; i < managerControls.Count; i++)
            {
                var toggle = (ToggleSwitch)((StackPanel)managerControls[i].Content).Children.First();
                toggle.IsOn = !Settings.GetDictionaryItem<string, bool>(Settings.K.DisabledManagers, PEInterface.Managers[i].Name);
            }
            _isLoadingToggles = false;
            base.OnNavigatedTo(e);
        }

        public void Administrator(object s, RoutedEventArgs e) => NavigationRequested?.Invoke(this, typeof(Administrator));
        public void Backup(object s, RoutedEventArgs e) => NavigationRequested?.Invoke(this, typeof(Backup));
        public void Experimental(object s, RoutedEventArgs e) => NavigationRequested?.Invoke(this, typeof(Experimental));
        public void General(object s, RoutedEventArgs e) => NavigationRequested?.Invoke(this, typeof(General));
        public void Interface(object s, RoutedEventArgs e) => NavigationRequested?.Invoke(this, typeof(Interface_P));
        public void Notifications(object s, RoutedEventArgs e) => NavigationRequested?.Invoke(this, typeof(Notifications));
        public void Operations(object s, RoutedEventArgs e) => NavigationRequested?.Invoke(this, typeof(Operations));
        public void Startup(object s, RoutedEventArgs e) => NavigationRequested?.Invoke(this, typeof(Updates));

    }
}

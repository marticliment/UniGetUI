using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Widgets;
using UniGetUI.Pages.SettingsPages.GeneralPages;
using UniGetUI.PackageEngine;
using UniGetUI.Core.SettingsEngine;

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
                };
                button.CornerRadius = first ? new CornerRadius(8, 8, 0, 0) : new CornerRadius(0);
                button.BorderThickness = first ? new Thickness(1) : new Thickness(1,0,1,1);
                button.Click += (_, _) => NavigationRequested?.Invoke(this, manager.GetType());

                var toggle = new ToggleSwitch();
                toggle.Toggled += (_, _) => Settings.SetDictionaryItem("DisabledManagers", manager.Name, !toggle.IsOn);
                button.Content = toggle;

                first = false;
                SettingsEntries.Children.Add(button);
                managerControls.Add(button);
            }
            var last = (SettingsPageButton)SettingsEntries.Children[^1];
            last.CornerRadius = new CornerRadius(0, 0, 8, 8);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            for(int i = 0; i < managerControls.Count; i++)
            {
                var toggle = (ToggleSwitch)managerControls[i].Content;
                toggle.IsOn = !Settings.GetDictionaryItem<string, bool>("DisabledManagers", PEInterface.Managers[i].Name);
            }

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

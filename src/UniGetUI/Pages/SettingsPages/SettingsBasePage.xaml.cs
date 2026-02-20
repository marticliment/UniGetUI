using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using UniGetUI.Core.Tools;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Input;
using CommunityToolkit.WinUI.Controls;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.Pages.SettingsPages.GeneralPages;
using UniGetUI.Interface.Pages;
using UniGetUI.PackageEngine.Interfaces;
using Windows.System;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Pages.SettingsPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsBasePage : Page, IEnterLeaveListener, IInnerNavigationPage
    {
        bool IsManagers;
        public string BackAutomationName => CoreTools.Translate("Back");

        public SettingsBasePage(bool isManagers)
        {
            IsManagers = isManagers;
            this.InitializeComponent();
            MainNavigationFrame.IsTabStop = false;
            MainNavigationFrame.TabFocusNavigation = KeyboardNavigationMode.Local;
            BackButton.PreviewKeyDown += BackButton_PreviewKeyDown;
            BackButton.Click += (_, _) =>
            {
                if (MainNavigationFrame.Content is ManagersHomepage or SettingsHomepage) MainApp.Instance.MainWindow.GoBack();
                else if (MainNavigationFrame.CanGoBack) MainNavigationFrame.GoBack();
                else MainNavigationFrame.Navigate(isManagers? typeof(ManagersHomepage): typeof(SettingsHomepage), null, new DrillInNavigationTransitionInfo());
            };
            MainNavigationFrame.Navigated += MainNavigationFrame_Navigated;
            MainNavigationFrame.Navigating += MainNavigationFrame_Navigating;
            MainNavigationFrame.Navigate(isManagers ? typeof(ManagersHomepage) : typeof(SettingsHomepage), null, new DrillInNavigationTransitionInfo());

            RestartRequired.Message = CoreTools.Translate("Restart WingetUI to fully apply changes");
            var RestartButton = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Content = CoreTools.Translate("Restart WingetUI"),
            };
            RestartButton.Click += (_, _) => MainApp.Instance.KillAndRestart();
            RestartRequired.ActionButton = RestartButton;

        }

        private void MainNavigationFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (MainNavigationFrame.Content is null) return;
            var page = MainNavigationFrame.Content as ISettingsPage;
            if (page is null) throw new InvalidCastException("Settings page does not inherit from ISettingsPage");

            page.NavigationRequested -= Page_NavigationRequested;
            page.RestartRequired -= Page_RestartRequired;
            if (page is PackageManagerPage pmpage) pmpage.ReapplyProperties -= SettingsBasePage_ReapplyProperties;
        }

        private void MainNavigationFrame_Navigated(object sender, NavigationEventArgs e)
        {
            var page = e.Content as ISettingsPage;
            if (page is null) throw new InvalidCastException("Settings page does not inherit from ISettingsPage");

            BackButton.Visibility = Visibility.Visible;
            SettingsTitle.Text = page.ShortTitle;
            page.NavigationRequested += Page_NavigationRequested;
            page.RestartRequired += Page_RestartRequired;
            if (page is PackageManagerPage pmpage) pmpage.ReapplyProperties += SettingsBasePage_ReapplyProperties;

            if (e.Content is FrameworkElement root)
            {
                DispatcherQueue.TryEnqueue(() => ApplyAccessibilityMetadata(root));
            }
        }

        private static void ApplyAccessibilityMetadata(FrameworkElement root)
        {
            root.TabFocusNavigation = KeyboardNavigationMode.Local;
            ApplyAccessibilityMetadataToNode(root);
        }

        private static void ApplyAccessibilityMetadataToNode(DependencyObject node)
        {
            if (node is ScrollViewer scroller)
            {
                EnsureScrollViewerAccessibility(scroller);
            }

            if (node is SettingsCard card)
            {
                EnsureSettingsCardAccessibility(card);
            }

            if (node is ToggleSwitch toggle)
            {
                EnsureToggleSwitchAccessibility(toggle);
            }

            if (node is ButtonBase button)
            {
                EnsureButtonAccessibility(button);
            }

            int childCount = VisualTreeHelper.GetChildrenCount(node);
            for (int i = 0; i < childCount; i++)
            {
                ApplyAccessibilityMetadataToNode(VisualTreeHelper.GetChild(node, i));
            }
        }

        private static void EnsureScrollViewerAccessibility(ScrollViewer scroller)
        {
            scroller.IsTabStop = false;
            scroller.TabFocusNavigation = KeyboardNavigationMode.Local;
        }

        private void BackButton_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Tab)
            {
                return;
            }

            bool isShiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            if (isShiftPressed)
            {
                return;
            }

            if (TryFocusMainContent())
            {
                e.Handled = true;
            }
        }

        private bool TryFocusMainContent()
        {
            if (MainNavigationFrame.Content is not FrameworkElement root)
            {
                return false;
            }

            FrameworkElement? target = FindFirstFocusableMainContentElement(root, allowHyperlinks: false)
                ?? FindFirstFocusableMainContentElement(root, allowHyperlinks: true);
            return target?.Focus(FocusState.Programmatic) ?? false;
        }

        private static FrameworkElement? FindFirstFocusableMainContentElement(DependencyObject node, bool allowHyperlinks)
        {
            if (node is SettingsCard card && card.IsClickEnabled && IsFocusable(card, allowHyperlinks))
            {
                return card;
            }

            if (node is FrameworkElement element && IsFocusable(element, allowHyperlinks))
            {
                return element;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(node);
            for (int i = 0; i < childCount; i++)
            {
                FrameworkElement? candidate = FindFirstFocusableMainContentElement(VisualTreeHelper.GetChild(node, i), allowHyperlinks);
                if (candidate is not null)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool IsFocusable(FrameworkElement element, bool allowHyperlinks)
        {
            if (element is not (SettingsCard or ButtonBase or ToggleSwitch or ComboBox or TextBox or PasswordBox or CheckBox or RadioButton))
            {
                return false;
            }

            if (!allowHyperlinks && element is HyperlinkButton)
            {
                return false;
            }

            if (element is not Control control)
            {
                return false;
            }

            return control.IsEnabled && control.IsTabStop && control.Visibility == Visibility.Visible;
        }

        private static void EnsureSettingsCardAccessibility(SettingsCard card)
        {
            string headerText = ExtractTextFromObject(card.Header);
            string descriptionText = ExtractTextFromObject(card.Description);
            string cardName = BuildCombinedText(headerText, descriptionText);
            if (!string.IsNullOrWhiteSpace(cardName) && string.IsNullOrWhiteSpace(AutomationProperties.GetName(card)))
            {
                AutomationProperties.SetName(card, cardName);
            }

            if (card.IsClickEnabled)
            {
                card.IsTabStop = true;
                card.UseSystemFocusVisuals = true;
            }
        }

        private static void EnsureToggleSwitchAccessibility(ToggleSwitch toggle)
        {
            string currentName = (AutomationProperties.GetName(toggle) ?? string.Empty).Trim();
            if (!NeedsContextualName(currentName, toggle.OnContent, toggle.OffContent))
            {
                return;
            }

            string fallbackName = FindAncestorSettingsCardName(toggle);
            if (!string.IsNullOrWhiteSpace(fallbackName))
            {
                AutomationProperties.SetName(toggle, fallbackName);
            }
        }

        private static void EnsureButtonAccessibility(ButtonBase button)
        {
            string currentName = (AutomationProperties.GetName(button) ?? string.Empty).Trim();
            if (!NeedsContextualName(currentName))
            {
                return;
            }

            string name = ExtractTextFromObject(button.Content);
            if (string.IsNullOrWhiteSpace(name))
            {
                name = ExtractTextFromObject(ToolTipService.GetToolTip(button));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = FindAncestorSettingsCardName(button);
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                AutomationProperties.SetName(button, name);
            }
        }

        private static bool NeedsContextualName(string currentName, params object?[] alternatives)
        {
            if (string.IsNullOrWhiteSpace(currentName))
            {
                return true;
            }

            foreach (object? alternative in alternatives)
            {
                string alternativeText = ExtractTextFromObject(alternative);
                if (!string.IsNullOrWhiteSpace(alternativeText)
                    && string.Equals(currentName, alternativeText, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            if (currentName.StartsWith("ms-resource://", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string translatedEnabled = CoreTools.Translate("Enabled");
            string translatedDisabled = CoreTools.Translate("Disabled");
            string[] genericNames =
            [
                "button",
                "on",
                "off",
                "enabled",
                "disabled",
                translatedEnabled,
                translatedDisabled
            ];

            return genericNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Any(name => string.Equals(currentName, name.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static string FindAncestorSettingsCardName(DependencyObject node)
        {
            for (DependencyObject? current = VisualTreeHelper.GetParent(node); current is not null; current = VisualTreeHelper.GetParent(current))
            {
                if (current is not SettingsCard card)
                {
                    continue;
                }

                EnsureSettingsCardAccessibility(card);
                string name = AutomationProperties.GetName(card);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }
            }

            return string.Empty;
        }

        private static string ExtractTextFromObject(object? value)
        {
            return value switch
            {
                string s => s.Trim(),
                TextBlock textBlock => textBlock.Text.Trim(),
                Run run => run.Text.Trim(),
                FrameworkElement element => ExtractTextFromElement(element),
                _ => string.Empty
            };
        }

        private static string ExtractTextFromElement(FrameworkElement element)
        {
            List<string> parts = [];
            CollectTextParts(element, parts);
            return BuildCombinedText(parts.ToArray());
        }

        private static void CollectTextParts(DependencyObject node, List<string> parts)
        {
            switch (node)
            {
                case TextBlock textBlock when !string.IsNullOrWhiteSpace(textBlock.Text):
                    parts.Add(textBlock.Text.Trim());
                    break;
                case Run run when !string.IsNullOrWhiteSpace(run.Text):
                    parts.Add(run.Text.Trim());
                    break;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(node);
            for (int i = 0; i < childCount; i++)
            {
                CollectTextParts(VisualTreeHelper.GetChild(node, i), parts);
            }
        }

        private static string BuildCombinedText(params string[] parts)
        {
            return string.Join(". ", parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).Distinct());
        }

        private void SettingsBasePage_ReapplyProperties(object? sender, EventArgs e)
        {
            BackButton.Visibility = ((MainNavigationFrame.Content as ISettingsPage)?.CanGoBack ?? true) ? Visibility.Visible : Visibility.Collapsed;
            SettingsTitle.Text = (MainNavigationFrame.Content as ISettingsPage)?.ShortTitle ?? "INVALID CONTENT PAGE!";
        }

        private void Page_RestartRequired(object? sender, EventArgs e)
        {
            RestartRequired.IsOpen = true;
        }

        private void Page_NavigationRequested(object? sender, Type e)
        {
            if(e == typeof(ManagersHomepage))
            {
                MainApp.Instance.MainWindow.NavigationPage.NavigateTo(Interface.PageType.Managers);
            }
            if(e.IsSubclassOf(typeof(PackageManager)))
            {
                MainNavigationFrame.Navigate(typeof(PackageManagerPage), e, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight} );
            }
            else
            {
                MainNavigationFrame.Navigate(e, null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight} );
            }
        }

        public void NavigateTo(IPackageManager manager)
        {
            Page_NavigationRequested(this, manager.GetType());
        }

        public void NavigateTo(Type e)
        {
            MainNavigationFrame.Navigate(e, null, new DrillInNavigationTransitionInfo());
        }

        public void OnEnter()
            => MainNavigationFrame.Navigate(IsManagers ? typeof(ManagersHomepage) : typeof(SettingsHomepage), null, new DrillInNavigationTransitionInfo());

        public void OnLeave() { }

        public bool CanGoBack()
            => MainNavigationFrame.CanGoBack && MainNavigationFrame.Content is not SettingsHomepage && MainNavigationFrame.Content is not ManagersHomepage;

        public void GoBack()
        {
            if (CanGoBack()) MainNavigationFrame.GoBack();
            else MainApp.Instance.MainWindow.GoBack();
        }
    }
}

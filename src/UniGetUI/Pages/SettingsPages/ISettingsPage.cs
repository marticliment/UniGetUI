namespace UniGetUI.Pages.SettingsPages
{
    interface ISettingsPage
    {
        public bool CanGoBack { get; }
        public string ShortTitle { get; }

        public event EventHandler? RestartRequired;

        public event EventHandler<Type>? NavigationRequested;
    }
}

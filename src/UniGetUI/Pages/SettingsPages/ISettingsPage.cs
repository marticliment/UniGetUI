using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.Interface.Widgets;

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.VoiceCommands;

namespace UniGetUI.Interface.Pages
{
    interface IPageWithKeyboardShortcuts
    {
        public void SearchTriggered();

        public void ReloadTriggered();

        public void SelectAllTriggered();
    }
}

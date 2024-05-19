using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using UniGetUI.Core;
using UniGetUI.Core.Language;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Classes;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Pages.AboutPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Translators : Page
    {
        public ObservableCollection<Person> TranslatorList = new();
        public Translators()
        {
            InitializeComponent();
            foreach(Person person in LanguageData.TranslatorsList)
                TranslatorList.Add(person);
        }
    }
}

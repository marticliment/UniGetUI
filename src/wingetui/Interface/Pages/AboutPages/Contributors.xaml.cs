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
using ModernWindow.Core.Data;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface.Pages.AboutPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public class Person
    {
        public string Name { get; set; }
        public Uri? ProfilePicture;
        public Uri? GitHubUrl;
        public bool HasPicture = false;
        public bool HasGithubProfile = false;
        public string Language = "";
    }

    public sealed partial class Contributors : Page
    {
        public ObservableCollection<Person> ContributorList = new();
        public Contributors()
        {
            this.InitializeComponent();
            foreach (string contributor in ContributorsData.Contributors)
            {
                Person person = new()
                {
                    Name = "@" + contributor,
                    ProfilePicture = new Uri("https://github.com/" + contributor + ".png"),
                    GitHubUrl = new Uri("https://github.com/" + contributor),
                    HasPicture = true,
                    HasGithubProfile = true,
                };
                ContributorList.Add(person);
            }
        }
    }
}

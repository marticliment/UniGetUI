using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using ModernWindow.Data;
using ModernWindow.Structures;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public class LibraryLicense
    {
        public string Name { get; set; }
        public string License { get; set; }
        public Uri LicenseURL { get; set; }
    }
    public class Person
    {
        public string Name { get; set; }
        public Uri? ProfilePicture;
        public Uri? GitHubUrl;
        public bool HasPicture = false;
        public bool HasGithubProfile = false;
    }

    public sealed partial class AboutWingetUI : Page
    {

        AppTools bindings = AppTools.Instance;
        public ObservableCollection<LibraryLicense> Licenses = new ObservableCollection<LibraryLicense>();
        public ObservableCollection<Person> Contributors = new();
        public ObservableCollection<Person> Translators = new();
        public AboutWingetUI()
        {
            this.InitializeComponent();
            foreach(var license in LicenseData.LicenseNames.Keys)
            {
                Licenses.Add(new LibraryLicense()
                {
                    Name = license,
                    License = LicenseData.LicenseNames[license],
                    LicenseURL = LicenseData.LicenseURLs[license]
                });
            }

            foreach(var contributor in ContributorsData.Contributors)
            {
                Person person = new Person()
                {
                    Name = "@"+contributor,
                    ProfilePicture = new Uri("https://github.com/" + contributor + ".png"),
                    GitHubUrl = new Uri("https://github.com/" + contributor),
                    HasPicture = true,
                    HasGithubProfile = true,
                };
                Contributors.Add(person);
            }
        }
    }
}

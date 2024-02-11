using Microsoft.UI.Xaml.Controls;
using ModernWindow.Data;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Nodes;

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
        public string Language = "";
    }

    public sealed partial class AboutWingetUI : Page
    {

        AppTools bindings = AppTools.Instance;
        public ObservableCollection<LibraryLicense> Licenses = new();
        public ObservableCollection<Person> Contributors = new();
        public ObservableCollection<Person> Translators = new();
        public AboutWingetUI()
        {
            InitializeComponent();
            foreach (string license in LicenseData.LicenseNames.Keys)
            {
                Licenses.Add(new LibraryLicense()
                {
                    Name = license,
                    License = LicenseData.LicenseNames[license],
                    LicenseURL = LicenseData.LicenseURLs[license]
                });
            }

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
                Contributors.Add(person);
            }

            JsonObject TranslatorsInfo = JsonNode.Parse(LanguageData.TranslatorsJSON).AsObject();

            VersionText.Text = bindings.Translate("You have installed WingetUI Version {0}").Replace("{0}", CoreData.VersionName);

            foreach (KeyValuePair<string, JsonNode> langKey in TranslatorsInfo)
            {
                JsonArray TranslatorsForLang = langKey.Value.AsArray();
                bool LangShown = false;
                foreach (JsonNode translator in TranslatorsForLang)
                {
                    Uri? url = null;
                    if (translator["link"].ToString() != "")
                        url = new Uri(translator["link"].ToString());
                    Person person = new()
                    {
                        Name = (url != null ? "@" : "") + translator["name"].ToString(),
                        HasPicture = url != null,
                        HasGithubProfile = url != null,
                        GitHubUrl = url != null ? url : new Uri("https://github.com/"),
                        ProfilePicture = url != null ? new Uri(url.ToString() + ".png") : new Uri("https://github.com/"),
                        Language = !LangShown ? LanguageData.LanguageList[langKey.Key] : "",
                    };
                    LangShown = true;
                    Translators.Add(person);
                }
            }
        }
    }
}

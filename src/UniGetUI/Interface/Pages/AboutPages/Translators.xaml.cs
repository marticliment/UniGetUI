using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Data;
using UniGetUI.Core;
using System.Text.Json.Nodes;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Pages.AboutPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Translators : Page
    {
        private ILogger AppLogger => Core.AppLogger.Instance;

        public ObservableCollection<Person> TranslatorList = new();
        public Translators()
        {
            this.InitializeComponent();
            JsonObject TranslatorsInfo = JsonNode.Parse(LanguageData.TranslatorsJSON).AsObject();


            foreach (KeyValuePair<string, JsonNode> langKey in TranslatorsInfo)
            {
                if (!LanguageData.LanguageList.ContainsKey(langKey.Key))
                {
                    AppLogger.Log($"Language {langKey.Key} not in list, maybe has not been added yet?");
                    continue;
                }
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
                        HasGitHubProfile = url != null,
                        GitHubUrl = url != null ? url : new Uri("https://github.com/"),
                        ProfilePicture = url != null ? new Uri(url.ToString() + ".png") : new Uri("https://github.com/"),
                        Language = !LangShown ? LanguageData.LanguageList[langKey.Key] : "",
                    };
                    LangShown = true;
                    TranslatorList.Add(person);
                }
            }
        }
    }
}

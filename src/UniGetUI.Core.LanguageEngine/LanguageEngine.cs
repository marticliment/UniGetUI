using System.Text.Json.Nodes;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.PackageEngine.Enums;

namespace UniGetUI.Core.Language
{
    public class LanguageEngine
    {
        public static Dictionary<string, string> MainLangDict = new();

        public LanguageEngine(string ForceLanguage = "")
        {
            string LangName = Settings.GetValue("PreferredLanguage");
            if (LangName == "default" || LangName == "")
            {
                LangName = System.Globalization.CultureInfo.CurrentCulture.ToString().Replace("-", "_");
            }
            LoadLanguage((ForceLanguage != "")? ForceLanguage: LangName);
        }

        /// <summary>
        /// Loads the specified language into the current instance
        /// </summary>
        /// <param name="lang">the language code</param>
        public void LoadLanguage(string lang)
        {
            if (LanguageData.LanguageReference.ContainsKey(lang))
            {
                MainLangDict = LoadLanguageFile(lang);
                MainLangDict.TryAdd("locale", lang);
            }
            else if (LanguageData.LanguageReference.ContainsKey(lang[0..2]))
            {
                MainLangDict = LoadLanguageFile(lang[0..2]);
                MainLangDict.TryAdd("locale", lang[0..2]);
            }
            else
            {
                MainLangDict = LoadLanguageFile("en");
                MainLangDict.TryAdd("locale", "en");
            }
            LoadStaticTranslation();
            Logger.Info("Loaded language locale: " + MainLangDict["locale"]);
        }

        public Dictionary<string, string> LoadLanguageFile(string LangKey, bool ForceBundled = false)
        {
            try
            {
                Dictionary<string, string> LangDict = new();
                string LangFileToLoad = Path.Join(CoreData.UniGetUICacheDirectory_Lang, "lang_" + LangKey + ".json");

                if (!File.Exists(LangFileToLoad) || Settings.Get("DisableLangAutoUpdater"))
                {
                    ForceBundled = true;
                }

                if (ForceBundled)
                {
                    LangFileToLoad = Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Languages", "lang_" + LangKey + ".json");
                }

                LangDict = (JsonNode.Parse(File.ReadAllText(LangFileToLoad)) as JsonObject).ToDictionary(x => x.Key, x => x.Value != null ? x.Value.ToString() : "");

                if (!Settings.Get("DisableLangAutoUpdater"))
                    _ = DownloadUpdatedLanguageFile(LangKey);

                return LangDict;
            }
            catch (Exception e)
            {
                Logger.Error($"LoadLanguageFile Failed for LangKey={LangKey}, ForceBundled={ForceBundled}");
                Logger.Error(e);
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Downloads and saves an updated version of the translations for the specified language.
        /// </summary>
        /// <param name="LangKey">The Id of the language to download</param>
        /// <param name="UseOldUrl">Use the new or the old Url (should not be used manually)</param>
        /// <returns></returns>
        public async Task DownloadUpdatedLanguageFile(string LangKey, bool UseOldUrl = false)
        {
            try
            {
                Uri NewFile = new("https://raw.githubusercontent.com/marticliment/WingetUI/main/src/" + (UseOldUrl ? "wingetui" : "UniGetUI") + "/Assets/Languages/lang_" + LangKey + ".json");

                HttpClient client = new();
                string fileContents = await client.GetStringAsync(NewFile);

                if (!Directory.Exists(CoreData.UniGetUICacheDirectory_Lang))
                    Directory.CreateDirectory(CoreData.UniGetUICacheDirectory_Lang);

                File.WriteAllText(Path.Join(CoreData.UniGetUICacheDirectory_Lang, "lang_" + LangKey + ".json"), fileContents);

                Logger.ImportantInfo("Lang files were updated successfully from GitHub");
            }
            catch (Exception e)
            {
                if (e is HttpRequestException && !UseOldUrl)
                    await DownloadUpdatedLanguageFile(LangKey, true);
                else
                {
                    Logger.Warn("Could not download updated translations from GitHub");
                    Logger.Warn(e);
                }
            }
        }

        public void LoadStaticTranslation()
        {
            CommonTranslations.ScopeNames[PackageScope.Local] = Translate("User | Local");
            CommonTranslations.ScopeNames[PackageScope.Global] = Translate("Machine | Global");

            CommonTranslations.InvertedScopeNames.Clear();
            CommonTranslations.InvertedScopeNames.Add(Translate("Machine | Global"), PackageScope.Global);
            CommonTranslations.InvertedScopeNames.Add(Translate("User | Local"), PackageScope.Local);
        }

        public string Translate(string key)
        {
            if (key == "WingetUI")
            {
                if (MainLangDict.ContainsKey("formerly WingetUI") && MainLangDict["formerly WingetUI"] != "")
                    return "UniGetUI (" + MainLangDict["formerly WingetUI"] + ")";
                return "UniGetUI (formerly WingetUI)";
            }
            else if (key == "Formerly known as WingetUI")
            {
                if (MainLangDict.ContainsKey(key))
                    return MainLangDict[key];
                return key;
            }

            if (key == null || key == "")
                return "";
            else if (MainLangDict.ContainsKey(key) && MainLangDict[key] != "")
                return MainLangDict[key].Replace("WingetUI", "UniGetUI");
            else
                return key.Replace("WingetUI", "UniGetUI");
        }
    }
}

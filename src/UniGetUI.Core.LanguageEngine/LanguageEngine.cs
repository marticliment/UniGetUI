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

        public LanguageEngine()
        {
            string LangName = Settings.GetValue("PreferredLanguage");
            if (LangName == "default" || LangName == "")
            {
                LangName = System.Globalization.CultureInfo.CurrentCulture.ToString().Replace("-", "_");
            }

            if (LanguageData.LanguageList.ContainsKey(LangName))
            {
                MainLangDict = LoadLanguageFile(LangName);
                MainLangDict.TryAdd("locale", LangName);
            }
            else if (LanguageData.LanguageList.ContainsKey(LangName[0..2]))
            {
                MainLangDict = LoadLanguageFile(LangName[0..2]);
                MainLangDict.TryAdd("locale", LangName[0..2]);
            }
            else
            {
                MainLangDict = LoadLanguageFile("en");
                MainLangDict.TryAdd("locale", "en");
            }
            LoadStaticTranslation();
            Logger.Log("Loaded language locale: " + MainLangDict["locale"]);
        }

        public Dictionary<string, string> LoadLanguageFile(string LangKey, bool ForceBundled = false)
        {
            try
            {
                Dictionary<string, string> LangDict = new();
                string LangFileToLoad = Path.Join(CoreData.UniGetUICacheDirectory_Lang, "lang_" + LangKey + ".json");
                Logger.Log(LangFileToLoad);

                if (!File.Exists(LangFileToLoad) || Settings.Get("DisableLangAutoUpdater"))
                {
                    ForceBundled = true;
                }

                if (ForceBundled)
                {
                    LangFileToLoad = Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Languages", "lang_" + LangKey + ".json");
                    Logger.Log(LangFileToLoad);
                }

                LangDict = (JsonNode.Parse(File.ReadAllText(LangFileToLoad)) as JsonObject).ToDictionary(x => x.Key, x => x.Value != null ? x.Value.ToString() : "");

                if (!Settings.Get("DisableLangAutoUpdater"))
                    _ = UpdateLanguageFile(LangKey);

                return LangDict;
            }
            catch (Exception e)
            {
                Logger.Log($"LoadLanguageFile Failed for LangKey={LangKey}, ForceBundled={ForceBundled}");
                Logger.Log(e);
                return new Dictionary<string, string>();
            }
        }

        public async Task UpdateLanguageFile(string LangKey, bool UseOldUrl = false)
        {
            try
            {
                Uri NewFile = new("https://raw.githubusercontent.com/marticliment/WingetUI/main/src/" + (UseOldUrl ? "wingetui" : "UniGetUI") + "/Assets/Languages/lang_" + LangKey + ".json");

                HttpClient client = new();
                string fileContents = await client.GetStringAsync(NewFile);

                if (!Directory.Exists(CoreData.UniGetUICacheDirectory_Lang))
                    Directory.CreateDirectory(CoreData.UniGetUICacheDirectory_Lang);

                File.WriteAllText(Path.Join(CoreData.UniGetUICacheDirectory_Lang, "lang_" + LangKey + ".json"), fileContents);

                Logger.Log("Lang files were updated successfully");
            }
            catch (Exception e)
            {
                if (e is HttpRequestException && !UseOldUrl)
                    await UpdateLanguageFile(LangKey, true);
                else
                    Logger.Log(e);
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

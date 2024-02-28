using ModernWindow.PackageEngine.Classes;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Windows.Media.Streaming.Adaptive;

namespace ModernWindow.Core.Data
{
    public static class LanguageData
    {
        public static string TranslatorsJSON = File.ReadAllText(Path.Join(CoreData.WingetUIExecutableDirectory, "Assets", "Languages", "Translators.json"));

        public static Dictionary<string, string> LanguageList = new()
        {
            {"default", "System language" },
            {"ar",     "Arabic - عربي‎"},
            {"bg",     "Bulgarian - български"},
            {"bn",     "Bangla - বাংলা"},
            {"ca",     "Catalan - Català"},
            {"cs",     "Czech - Čeština"},
            {"da",     "Danish - Dansk"},
            {"de",     "German - Deutsch"},
            {"el",     "Greek - Ελληνικά"},
            {"en",     "English - English"},
            {"es",     "Spanish - Castellano"},
            {"fa",     "Persian - فارسی‎"},
            {"fr",     "French - Français"},
            {"hi",     "Hindi - हिंदी"},
            {"hr",     "Croatian - Hrvatski"},
            {"he",     "Hebrew - עִבְרִית‎"},
            {"hu",     "Hungarian - Magyar"},
            {"it",     "Italian - Italiano"},
            {"id",     "Indonesian - Bahasa Indonesia"},
            {"ja",     "Japanese - 日本語"},
            {"ko",     "Korean - 한국어"},
            {"mk",     "Macedonian - Македонски"},
            {"nb",     "Norwegian (bokmål)"},
            {"nl",     "Dutch - Nederlands"},
            {"pl",     "Polish - Polski"},
            {"pt_BR",  "Portuguese (Brazil)"},
            {"pt_PT",  "Portuguese (Portugal)"},
            {"ro",     "Romanian - Română"},
            {"ru",     "Russian - Русский"},
            {"sr",     "Serbian - Srpski"},
            {"si",     "Sinhala - සිංහල"},
            {"sl",     "Slovene - Slovenščina"},
            {"tg",     "Tagalog - Tagalog"},
            {"th",     "Thai - ภาษาไทย"},
            {"tr",     "Turkish - Türkçe"},
            {"ua",     "Ukranian - Yкраї́нська"},
            {"vi",     "Vietnamese - Tiếng Việt"},
            {"zh_CN",  "Simplified Chinese (China)"},
            {"zh_TW",  "Traditional Chinese (Taiwan)"},
        };


        public static Dictionary<string, string> TranslatedPercentages = new()
        {
          {"ar", "87%"},
          {"bg", "83%"},
          {"bn", "45%"},
          {"cs", "92%"},
          {"da", "29%"},
          {"de", "99%"},
          {"el", "72%"},
          {"fa", "71%"},
          {"he", "72%"},
          {"hi", "81%"},
          {"hr", "86%"},
          {"id", "88%"},
          {"ja", "86%"},
          {"ko", "96%"},
          {"mk", "95%"},
          {"nb", "84%"},
          {"pt_BR", "96%"},
          {"pt_PT", "96%"},
          {"ru", "89%"},
          {"si", "9%"},
          {"sl", "88%"},
          {"sr", "96%"},
          {"tg", "21%"},
          {"th", "92%"},
          {"tr", "85%"},
          {"ua", "84%"},
          {"vi", "96%"},
          { "zh_CN", "92%" }
        };

    }

    public class LanguageEngine
    {
        public static Dictionary<string, string> MainLangDict = new();

        public LanguageEngine()
        {
            string LangName = AppTools.GetSettingsValue_Static("PreferredLanguage");
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
            AppTools.Log("Loaded language locale: " + MainLangDict["locale"]);
        }

        public Dictionary<string, string> LoadLanguageFile(string LangKey, bool ForceBundled = false)
        {
            Dictionary<string, string> LangDict = new();
            string LangFileToLoad = Path.Join(CoreData.WingetUICacheDirectory_Lang, "lang_" + LangKey + ".json");
            AppTools.Log(LangFileToLoad);

            if (!File.Exists(LangFileToLoad) || AppTools.GetSettings_Static("DisableLangAutoUpdater"))
                ForceBundled = true;

            if (ForceBundled)
            {
                LangFileToLoad = Path.Join(CoreData.WingetUIExecutableDirectory, "Assets", "Languages", "lang_" + LangKey + ".json");
                AppTools.Log(LangFileToLoad);
            }

            LangDict = (JsonNode.Parse(File.ReadAllText(LangFileToLoad)) as JsonObject).ToDictionary(x => x.Key, x => x.Value != null ? x.Value.ToString() : "");

            if (!AppTools.GetSettings_Static("DisableLangAutoUpdater"))
                _ = UpdateLanguageFile(LangKey);

            return LangDict;
        }

        public async Task UpdateLanguageFile(string LangKey)
        {
            try
            {
                Uri NewFile = new("https://raw.githubusercontent.com/marticliment/WingetUI/main/wingetui/Core/Languages/" + "lang_" + LangKey + ".json");
                using (WebClient client = new())
                {
                    string fileContents = await client.DownloadStringTaskAsync(NewFile);

                    if (!Directory.Exists(CoreData.WingetUICacheDirectory_Lang))
                        Directory.CreateDirectory(CoreData.WingetUICacheDirectory_Lang);

                    File.WriteAllText(Path.Join(CoreData.WingetUICacheDirectory_Lang, "lang_" + LangKey + ".json"), fileContents);
                }
                AppTools.Log("Lang files were updated successfully");
            }
            catch (Exception e)
            {
                AppTools.Log(e);
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
            if (key == null || key == "")
                return "";
            else if (MainLangDict.ContainsKey(key) && MainLangDict[key] != "")
                return MainLangDict[key];
            else
                return key;
        }
    }

    public static class CommonTranslations
    {
        public static Dictionary<Architecture, string> ArchNames = new()
        {
            { Architecture.X64, "x64" },
            { Architecture.X86, "x86" },
            { Architecture.Arm64, "arm64" },
            { Architecture.Arm, "arm32" },
        };

        public static Dictionary<string, Architecture> InvertedArchNames = new()
        {
            { "x64", Architecture.X64 },
            { "x86", Architecture.X86 },
            { "arm64", Architecture.Arm64 },
            { "arm32", Architecture.Arm },
        };

        public static Dictionary<PackageScope, string> ScopeNames = new()
        {
            { PackageScope.Global, "Machine | Global" },
            { PackageScope.Local, "User | Local" },
        };

        public static Dictionary<string, PackageScope> InvertedScopeNames = new()
        {
            { "Machine | Global", PackageScope.Global },
            { "User | Local", PackageScope.Local },
        };

        public static Dictionary<PackageScope, string> ScopeNames_NonLang = new()
        {
            { PackageScope.Global, "machine" },
            { PackageScope.Local, "user" },
        };

        public static Dictionary<string, PackageScope> InvertedScopeNames_NonLang = new()
        {
            { "machine", PackageScope.Global },
            { "user", PackageScope.Local },
        };
    }
}


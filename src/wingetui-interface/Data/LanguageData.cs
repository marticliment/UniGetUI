using ModernWindow.PackageEngine;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ModernWindow.Data
{
    public static class LanguageData
    {
        public static string TranslatorsJSON =
            @"
                {
                  ""ar"": [
                    {
                      ""name"": ""Abdu11ahAS"",
                      ""link"": ""https://github.com/Abdu11ahAS""
                    },
                    {
                      ""name"": ""FancyCookin"",
                      ""link"": ""https://github.com/FancyCookin""
                    },
                    {
                      ""name"": ""mo9a7i"",
                      ""link"": ""https://github.com/mo9a7i""
                    }
                  ],
                  ""bg"": [
                    {
                      ""name"": ""Vasil Kolev"",
                      ""link"": """"
                    }
                  ],
                  ""bn"": [
                    {
                      ""name"": ""fluentmoheshwar"",
                      ""link"": ""https://github.com/fluentmoheshwar""
                    },
                    {
                      ""name"": ""Mushfiq Iqbal Rayon"",
                      ""link"": """"
                    },
                    {
                      ""name"": ""Nilavra Bhattacharya"",
                      ""link"": """"
                    }
                  ],
                  ""ca"": [
                    {
                      ""name"": ""marticliment"",
                      ""link"": ""https://github.com/marticliment""
                    }
                  ],
                  ""cs"": [
                    {
                      ""name"": ""panther7"",
                      ""link"": ""https://github.com/panther7""
                    }
                  ],
                  ""da"": [
                    {
                      ""name"": ""mikkolukas"",
                      ""link"": ""https://github.com/mikkolukas""
                    }
                  ],
                  ""de"": [
                    {
                      ""name"": ""CanePlayz"",
                      ""link"": ""https://github.com/CanePlayz""
                    },
                    {
                      ""name"": ""Datacra5H"",
                      ""link"": ""https://github.com/Datacra5H""
                    },
                    {
                      ""name"": ""ebnater"",
                      ""link"": ""https://github.com/ebnater""
                    },
                    {
                      ""name"": ""michaelmairegger"",
                      ""link"": ""https://github.com/michaelmairegger""
                    },
                    {
                      ""name"": ""Seeloewen"",
                      ""link"": ""https://github.com/Seeloewen""
                    }
                  ],
                  ""el"": [
                    {
                      ""name"": ""antwnhsx  @wobblerrrgg"",
                      ""link"": ""https://github.com/antwnhsx  @wobblerrrgg""
                    }
                  ],
                  ""en"": [
                    {
                      ""name"": ""marticliment"",
                      ""link"": ""https://github.com/marticliment""
                    },
                    {
                      ""name"": ""ppvnf"",
                      ""link"": ""https://github.com/ppvnf""
                    }
                  ],
                  ""es"": [
                    {
                      ""name"": ""apazga"",
                      ""link"": ""https://github.com/apazga""
                    },
                    {
                      ""name"": ""dalbitresb12"",
                      ""link"": ""https://github.com/dalbitresb12""
                    },
                    {
                      ""name"": ""evaneliasyoung"",
                      ""link"": ""https://github.com/evaneliasyoung""
                    },
                    {
                      ""name"": ""guplem"",
                      ""link"": ""https://github.com/guplem""
                    },
                    {
                      ""name"": ""JMoreno97"",
                      ""link"": ""https://github.com/JMoreno97""
                    },
                    {
                      ""name"": ""marticliment"",
                      ""link"": ""https://github.com/marticliment""
                    },
                    {
                      ""name"": ""rubnium"",
                      ""link"": ""https://github.com/rubnium""
                    },
                    {
                      ""name"": ""uKER"",
                      ""link"": ""https://github.com/uKER""
                    }
                  ],
                  ""fa"": [
                    {
                      ""name"": ""itsarian"",
                      ""link"": ""https://github.com/itsarian""
                    },
                    {
                      ""name"": ""Mahdi-Hazrati"",
                      ""link"": ""https://github.com/Mahdi-Hazrati""
                    },
                    {
                      ""name"": ""smsi2001"",
                      ""link"": ""https://github.com/smsi2001""
                    }
                  ],
                  ""fr"": [
                    {
                      ""name"": ""Evans Costa"",
                      ""link"": """"
                    },
                    {
                      ""name"": ""Rémi Guerrero"",
                      ""link"": """"
                    },
                    {
                      ""name"": ""W1L7dev"",
                      ""link"": ""https://github.com/W1L7dev""
                    }
                  ],
                  ""he"": [
                    {
                      ""name"": ""Oryan"",
                      ""link"": """"
                    }
                  ],
                  ""hi"": [
                    {
                      ""name"": ""atharva_xoxo"",
                      ""link"": ""https://github.com/atharva_xoxo""
                    },
                    {
                      ""name"": ""satanarious"",
                      ""link"": ""https://github.com/satanarious""
                    }
                  ],
                  ""hr"": [
                    {
                      ""name"": ""Stjepan Treger"",
                      ""link"": """"
                    }
                  ],
                  ""hu"": [
                    {
                      ""name"": ""gidano"",
                      ""link"": ""https://github.com/gidano""
                    }
                  ],
                  ""id"": [
                    {
                      ""name"": ""arthackrc"",
                      ""link"": ""https://github.com/arthackrc""
                    },
                    {
                      ""name"": ""joenior"",
                      ""link"": ""https://github.com/joenior""
                    }
                  ],
                  ""it"": [
                    {
                      ""name"": ""David Senoner"",
                      ""link"": """"
                    },
                    {
                      ""name"": ""giacobot"",
                      ""link"": ""https://github.com/giacobot""
                    },
                    {
                      ""name"": ""maicol07"",
                      ""link"": ""https://github.com/maicol07""
                    },
                    {
                      ""name"": ""mapi68"",
                      ""link"": ""https://github.com/mapi68""
                    },
                    {
                      ""name"": ""mrfranza"",
                      ""link"": ""https://github.com/mrfranza""
                    },
                    {
                      ""name"": ""Rosario Di Mauro"",
                      ""link"": """"
                    }
                  ],
                  ""ja"": [
                    {
                      ""name"": ""nob-swik"",
                      ""link"": ""https://github.com/nob-swik""
                    },
                    {
                      ""name"": ""sho9029"",
                      ""link"": """"
                    },
                    {
                      ""name"": ""Yuki Takase"",
                      ""link"": """"
                    }
                  ],
                  ""ko"": [
                    {
                      ""name"": ""minbert"",
                      ""link"": ""https://github.com/minbert""
                    },
                    {
                      ""name"": ""shblue21"",
                      ""link"": ""https://github.com/shblue21""
                    }
                  ],
                  ""mk"": [
                    {
                      ""name"": ""LordDeatHunter"",
                      ""link"": """"
                    }
                  ],
                  ""nb"": [
                    {
                      ""name"": ""jomaskm"",
                      ""link"": ""https://github.com/jomaskm""
                    }
                  ],
                  ""nl"": [
                    {
                      ""name"": ""abbydiode"",
                      ""link"": ""https://github.com/abbydiode""
                    },
                    {
                      ""name"": ""Stephan-P"",
                      ""link"": ""https://github.com/Stephan-P""
                    }
                  ],
                  ""pl"": [
                    {
                      ""name"": ""KamilZielinski"",
                      ""link"": ""https://github.com/KamilZielinski""
                    },
                    {
                      ""name"": ""kwiateusz"",
                      ""link"": ""https://github.com/kwiateusz""
                    },
                    {
                      ""name"": ""RegularGvy13"",
                      ""link"": ""https://github.com/RegularGvy13""
                    }
                  ],
                  ""pt_BR"": [
                    {
                      ""name"": ""maisondasilva"",
                      ""link"": ""https://github.com/maisondasilva""
                    },
                    {
                      ""name"": ""ppvnf"",
                      ""link"": ""https://github.com/ppvnf""
                    },
                    {
                      ""name"": ""wanderleihuttel"",
                      ""link"": ""https://github.com/wanderleihuttel""
                    }
                  ],
                  ""pt_PT"": [
                    {
                      ""name"": ""PoetaGA"",
                      ""link"": ""https://github.com/PoetaGA""
                    },
                    {
                      ""name"": ""ppvnf"",
                      ""link"": ""https://github.com/ppvnf""
                    }
                  ],
                  ""ro"": [
                    {
                      ""name"": ""Mihai Vasiliu"",
                      ""link"": """"
                    },
                    {
                      ""name"": ""TZACANEL"",
                      ""link"": """"
                    }
                  ],
                  ""ru"": [
                    {
                      ""name"": ""bropines"",
                      ""link"": ""https://github.com/bropines""
                    },
                    {
                      ""name"": ""flatron4eg"",
                      ""link"": ""https://github.com/flatron4eg""
                    },
                    {
                      ""name"": ""katrovsky"",
                      ""link"": ""https://github.com/katrovsky""
                    },
                    {
                      ""name"": ""Sergey"",
                      ""link"": """"
                    },
                    {
                      ""name"": ""sklart"",
                      ""link"": ""https://github.com/sklart""
                    }
                  ],
                  ""si"": [
                    {
                      ""name"": ""SashikaSandeepa"",
                      ""link"": ""https://github.com/SashikaSandeepa""
                    }
                  ],
                  ""sl"": [
                    {
                      ""name"": ""rumplin"",
                      ""link"": ""https://github.com/rumplin""
                    }
                  ],
                  ""sr"": [
                    {
                      ""name"": ""daVinci13"",
                      ""link"": ""https://github.com/daVinci13""
                    },
                    {
                      ""name"": ""Nemanja Djurcic"",
                      ""link"": """"
                    }
                  ],
                  ""tg"": [
                    {
                      ""name"": ""lasersPew"",
                      ""link"": """"
                    }
                  ],
                  ""th"": [
                    {
                      ""name"": ""apaeisara"",
                      ""link"": ""https://github.com/apaeisara""
                    },
                    {
                      ""name"": ""dulapahv"",
                      ""link"": ""https://github.com/dulapahv""
                    }
                  ],
                  ""tr"": [
                    {
                      ""name"": ""ahmetozmtn"",
                      ""link"": ""https://github.com/ahmetozmtn""
                    },
                    {
                      ""name"": ""gokberkgs"",
                      ""link"": ""https://github.com/gokberkgs""
                    }
                  ],
                  ""ua"": [
                    {
                      ""name"": ""Artem Moldovanenko"",
                      ""link"": """"
                    },
                    {
                      ""name"": ""Operator404"",
                      ""link"": """"
                    }
                  ],
                  ""vi"": [
                    {
                      ""name"": ""legendsjoon"",
                      ""link"": ""https://github.com/legendsjoon""
                    },
                    {
                      ""name"": ""txavlog"",
                      ""link"": ""https://github.com/txavlog""
                    }
                  ],
                  ""zh_CN"": [
                    {
                      ""name"": ""Aaron Liu"",
                      ""link"": """"
                    },
                    {
                      ""name"": ""adfnekc"",
                      ""link"": ""https://github.com/adfnekc""
                    },
                    {
                      ""name"": ""BUGP Association"",
                      ""link"": """"
                    },
                    {
                      ""name"": ""ciaran"",
                      ""link"": """"
                    },
                    {
                      ""name"": ""CnYeSheng"",
                      ""link"": """"
                    },
                    {
                      ""name"": ""Cololi"",
                      ""link"": """"
                    }
                  ],
                  ""zh_TW"": [
                    {
                      ""name"": ""Aaron Liu"",
                      ""link"": """"
                    },
                    {
                      ""name"": ""CnYeSheng"",
                      ""link"": """"
                    },
                    {
                      ""name"": ""Cololi"",
                      ""link"": """"
                    },
                    {
                      ""name"": ""yrctw"",
                      ""link"": ""https://github.com/yrctw""
                    }
                  ]
                }
                ";

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
                LangName = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];
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


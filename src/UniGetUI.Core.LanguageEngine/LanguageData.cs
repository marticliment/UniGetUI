using System;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.Core.Classes;
using System.Collections.ObjectModel;

namespace UniGetUI.Core.Language
{
    public static class LanguageData
    {
        /// <summary>
        /// Returns 
        /// </summary>
        private static Person[]? __translators_list;
        
        public static Person[] TranslatorsList
        {
            get
            {
                if (__translators_list == null)
                    __translators_list = LoadLanguageTranslatorList();

                return __translators_list;
            }
        }

        private static ReadOnlyDictionary<string, string>? __language_reference;
        public static ReadOnlyDictionary<string, string> LanguageReference
        {
            get {
                if (__language_reference == null)
                    __language_reference = LoadLanguageReference();
                return __language_reference;
            }
        }

        private static ReadOnlyDictionary<string, string>? __translation_percentages;
        public static ReadOnlyDictionary<string, string> TranslationPercentages
        {
            get
            {
                if (__translation_percentages == null)
                    __translation_percentages = LoadTranslationPercentages();
                return __translation_percentages;
            }
        }

        private static ReadOnlyDictionary<string, string> LoadTranslationPercentages()
        {
            JsonObject? val = JsonObject.Parse(File.ReadAllText(Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Data", "TranslatedPercentages.json"))) as JsonObject;
            if (val != null)
                return new(val.ToDictionary(x => x.Key, x => (x.Value ?? ("404%" + x.Key)).ToString()));
            else
                return new(new Dictionary<string, string>());
        }

        private static ReadOnlyDictionary<string, string> LoadLanguageReference()
        {
            JsonObject? val = JsonObject.Parse(File.ReadAllText(Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Data", "LanguagesReference.json"))) as JsonObject;
            if (val != null)
                return new(val.ToDictionary(x => x.Key, x => (x.Value ?? ("NoNameLang_" + x.Key)).ToString()));
            else
                return new(new Dictionary<string, string>());
        }

        private static Person[] LoadLanguageTranslatorList()
        {
            var JsonContents = File.ReadAllText(Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Data", "Translators.json"));
            JsonObject? TranslatorsInfo = JsonNode.Parse(JsonContents) as JsonObject;

            if (TranslatorsInfo == null) return [];

            List<Person> result = new();
            foreach (var langKey in TranslatorsInfo)
            {
                if (!LanguageReference.ContainsKey(langKey.Key))
                {
                    Logger.Log($"Language {langKey.Key} not in list, maybe has not been added yet?");
                    continue;
                }

                JsonArray TranslatorsForLang = (langKey.Value ?? new JsonArray()).AsArray();
                bool LangShown = false;
                foreach (JsonNode? translator in TranslatorsForLang)
                {
                    if (translator is null) continue;

                    Uri? url = null;
                    if (translator["link"] != null && translator["link"]?.ToString() != "")
                        url = new Uri((translator["link"] ?? "").ToString());

                    Person person = new(
                        Name: (url != null ? "@" : "") + (translator["name"] ?? "").ToString(),
                        ProfilePicture: url != null ? new Uri(url.ToString() + ".png") : null,
                        GitHubUrl: url,
                        Language: !LangShown ? LanguageData.LanguageReference[langKey.Key] : ""
                    );
                    LangShown = true;
                    result.Add(person);
                }
            }

            return result.ToArray();
        }
    }

    public static class CommonTranslations
    {
        public static readonly Dictionary<Architecture, string> ArchNames = new()
        {
            { Architecture.X64, "x64" },
            { Architecture.X86, "x86" },
            { Architecture.Arm64, "arm64" },
            { Architecture.Arm, "arm32" },
        };

        public static readonly Dictionary<string, Architecture> InvertedArchNames = new()
        {
            { "x64", Architecture.X64 },
            { "x86", Architecture.X86 },
            { "arm64", Architecture.Arm64 },
            { "arm32", Architecture.Arm },
        };

        public static readonly Dictionary<PackageScope, string> ScopeNames = new()
        {
            { PackageScope.Global, "Machine | Global" },
            { PackageScope.Local, "User | Local" },
        };

        public static readonly Dictionary<string, PackageScope> InvertedScopeNames = new()
        {
            { "Machine | Global", PackageScope.Global },
            { "User | Local", PackageScope.Local },
        };

        public static readonly Dictionary<PackageScope, string> ScopeNames_NonLang = new()
        {
            { PackageScope.Global, "mac" +
                "hine" },
            { PackageScope.Local, "user" },
        };

        public static readonly Dictionary<string, PackageScope> InvertedScopeNames_NonLang = new()
        {
            { "machine", PackageScope.Global },
            { "user", PackageScope.Local },
        };
    }
}


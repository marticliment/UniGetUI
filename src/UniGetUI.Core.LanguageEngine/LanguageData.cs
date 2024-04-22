using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using UniGetUI.Core.Data;
using UniGetUI.PackageEngine.Enums;

namespace UniGetUI.Core.Language
{
    public static class LanguageData
    {
        public static readonly string TranslatorsJSON = File.ReadAllText(
            Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Data", "Translators.json")
        );

        public static readonly Dictionary<string, string> LanguageList = (JsonObject.Parse(
            File.ReadAllText(Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Data", "LanguagesReference.json"))
        ) as JsonObject).ToDictionary(x => x.Key, x => x.Value.ToString());

        public static readonly Dictionary<string, string> TranslatedPercentages = (JsonObject.Parse(
            File.ReadAllText(Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Data", "TranslatedPercentages.json"))
        ) as JsonObject).ToDictionary(x => x.Key, x => x.Value.ToString());
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
            { PackageScope.Global, "machine" },
            { PackageScope.Local, "user" },
        };

        public static readonly Dictionary<string, PackageScope> InvertedScopeNames_NonLang = new()
        {
            { "machine", PackageScope.Global },
            { "user", PackageScope.Local },
        };
    }
}


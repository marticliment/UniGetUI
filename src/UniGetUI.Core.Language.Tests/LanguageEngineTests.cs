using UniGetUI.PackageEngine.Enums;

namespace UniGetUI.Core.Language.Tests
{
    public class LanguageEngineTests
    {
        [Theory]
        [InlineData("ca", "Fes una còpia de seguretat dels paquets instal·lats")]
        [InlineData("es", "Respaldar paquetes instalados")]
        public void TestLoadingLanguage(string language, string translation)
        {
            LanguageEngine engine = new();

            engine.LoadLanguage(language);
            Assert.Equal(translation, engine.Translate("Backup installed packages"));
        }

        [Fact]
        public void TestLoadingLanguageForNonExistentKey()
        {
            //arrange
            LanguageEngine engine = new();
            engine.LoadLanguage("es");
            //act
            string NONEXISTENT_KEY = "This is a nonexistent key thay should be returned as-is";
            //assert
            Assert.Equal(NONEXISTENT_KEY, engine.Translate(NONEXISTENT_KEY));
        }

        [Theory]
        [InlineData("en", "UniGetUI Log", "UniGetUI (formerly WingetUI)")]
        [InlineData("ca", "Registre de l'UniGetUI", "UniGetUI (abans WingetUI)")]
        public void TestUniGetUIRefactoring(string language, string uniGetUILogTranslation, string uniGetUITranslation)
        {
            LanguageEngine engine = new();

            engine.LoadLanguage(language);
            Assert.Equal(uniGetUILogTranslation, engine.Translate("WingetUI Log"));
            Assert.Equal(uniGetUITranslation, engine.Translate("WingetUI"));
        }

        [Fact]
        public void LocalFallbackTest()
        {
            LanguageEngine engine = new();
            engine.LoadLanguage("random-nonexistent-language");
            Assert.Equal("en", engine.Locale);
        }

        [Fact]
        public void TestStaticallyLoadedLanguages()
        {
            LanguageEngine engine = new();
            engine.LoadLanguage("ca");
            engine.LoadStaticTranslation();
            Assert.Equal("Usuari | Local", CommonTranslations.ScopeNames[PackageScope.Local]);
            Assert.Equal("Màquina | Global", CommonTranslations.ScopeNames[PackageScope.Global]);

            Assert.Equal(PackageScope.Global, CommonTranslations.InvertedScopeNames["Màquina | Global"]);
            Assert.Equal(PackageScope.Local, CommonTranslations.InvertedScopeNames["Usuari | Local"]);
        }

        /*
        [Fact]
        public async Task TestDownloadUpdatedTranslationsAsync()
        {
            string expected_file = Path.Join(CoreData.UniGetUICacheDirectory_Lang, "lang_ca.json");
            if (File.Exists(expected_file))
                File.Delete(expected_file);

            LanguageEngine engine = new();
            engine.LoadLanguage("ca");
            await engine.DownloadUpdatedLanguageFile("ca");

            Assert.True(File.Exists(expected_file), "The updated file was not created");
            File.Delete(expected_file);
        }
        */
    }
}

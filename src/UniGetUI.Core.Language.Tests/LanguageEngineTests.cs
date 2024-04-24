using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.Core.Data;
using UniGetUI.PackageEngine.Enums;

namespace UniGetUI.Core.Language.Tests
{
    [TestClass]
    public class LanguageEngineTests
    {
        [TestMethod]
        public void TestLoadingLanguage()
        {
            var engine = new LanguageEngine();
            
            engine.LoadLanguage("ca");
            Assert.AreEqual("Fes una còpia de seguretat dels paquets instal·lats", engine.Translate("Backup installed packages"), "The translation for the first loaded language does not match the expected value");

            engine.LoadLanguage("es");
            Assert.AreEqual("Respaldar paquetes instalados", engine.Translate("Backup installed packages"), "The translation for the second loaded language does not match the expected value");

            var NONEXISTENT_KEY = "This is a nonexistent key thay should be returned as-is";
            Assert.AreEqual(NONEXISTENT_KEY, engine.Translate(NONEXISTENT_KEY), "The nonexistent key got modified when it should have not!");
        }

        [TestMethod]
        public void TestUniGetUIRefactoring()
        {
            var engine = new LanguageEngine();

            engine.LoadLanguage("en");
            Assert.AreEqual("UniGetUI Log", engine.Translate("WingetUI Log"), "The WingetUI language was not properly translated on english");
            Assert.AreEqual("UniGetUI (formerly WingetUI)", engine.Translate("WingetUI"), "The WingetUI language was not properly translated on english");

            engine.LoadLanguage("ca");
            Assert.AreEqual("Registre del UniGetUI", engine.Translate("WingetUI Log"), "The WingetUI language was not properly translated on non-english");
            Assert.AreEqual("UniGetUI (abans WingetUI)", engine.Translate("WingetUI"), "The WingetUI language was not properly translated on non-english");

            engine.LoadLanguage("random-nonexistent-language");
            Assert.AreEqual("en", engine.Translate("locale"), "Fallback locale not loaded");
        }

        [TestMethod]
        public void TestStaticallyLoadedLanguages()
        {
            var engine = new LanguageEngine();
            engine.LoadLanguage("ca");
            engine.LoadStaticTranslation();
            Assert.AreEqual("Usuari | Local", CommonTranslations.ScopeNames[PackageScope.Local], "Invalid common translation");
            Assert.AreEqual("Màquina | Global", CommonTranslations.ScopeNames[PackageScope.Global], "Invalid common translation");

            Assert.AreEqual(PackageScope.Global, CommonTranslations.InvertedScopeNames["Màquina | Global"], "Invalid common tranalation");
            Assert.AreEqual(PackageScope.Local, CommonTranslations.InvertedScopeNames["Usuari | Local"], "Invalid common tranalation");
        }

        [TestMethod]
        public void TestDownloadUpdatedTranslations()
        {
            var expected_file = Path.Join(CoreData.UniGetUICacheDirectory_Lang, "lang_ca.json");
            if (File.Exists(expected_file))
                File.Delete(expected_file);

            var engine = new LanguageEngine();
            engine.LoadLanguage("ca");
            engine.DownloadUpdatedLanguageFile("ca").Wait();

            Assert.IsTrue(File.Exists(expected_file), "The updated file was not created");
            File.Delete(expected_file);
        }
    }
}

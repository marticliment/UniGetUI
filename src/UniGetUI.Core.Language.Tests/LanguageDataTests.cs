using System.Text.Json.Nodes;

namespace UniGetUI.Core.Language.Tests
{
    [TestClass]
    public class LanguageDataTests
    {
        [TestMethod]
        public void TranslatorsListTest()
        {
            var Translators = LanguageData.TranslatorsList;
            Assert.AreNotEqual(0, Translators.Length, "Translator list is empty");
            foreach (var translator in Translators)
            {
                Assert.AreNotEqual("", translator.Name, "Translator name cannot be null");
            }
        }

        [TestMethod]
        public void LanguageReferenceTest() {
            Assert.AreNotEqual(0, LanguageData.LanguageReference.Count, "The LanguageReference cannot be empty");
            foreach (var language in LanguageData.LanguageReference)
                Assert.IsFalse(language.Value.Contains("NoNameLang_"), $"The language with key {language.Key} has no assigned name");
        }

        [TestMethod]
        public void TranslatedPercentageTests()
        { 
            var TranslatedPercent = LanguageData.TranslationPercentages;
            foreach (var key in TranslatedPercent.Keys)
            {
                Assert.IsTrue(LanguageData.LanguageReference.ContainsKey(key), $"The language key {key} was not found on LanguageReference");
                Assert.IsFalse(LanguageData.TranslationPercentages[key].Contains("404%"), $"Somehow the key {key} has no value");
            }
        }
    }
}
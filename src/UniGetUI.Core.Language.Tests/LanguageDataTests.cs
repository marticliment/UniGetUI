using UniGetUI.Core.Classes;

namespace UniGetUI.Core.Language.Tests
{
    public class LanguageDataTests
    {
        public static object[][] Translators =>
            LanguageData.TranslatorsList.Select(x => new object[] { x }).ToArray();

        public static object[][] LanguageReferences =>
            LanguageData.LanguageReference.Select(x => new object[] { x.Key, x.Value }).ToArray();

        [Fact]
        public void TranslatorsListNotEmptyTest()
            => Assert.NotEmpty(LanguageData.TranslatorsList);

        [Theory]
        [MemberData(nameof(Translators))]
        public void TranslatorsListTest(Person translator)
            => Assert.NotEmpty(translator.Name);

        [Fact]
        public void LanguageReferenceNotEmptyTest()
        {
            Assert.NotEmpty(LanguageData.LanguageReference);
        }

        [Theory]
        [MemberData(nameof(LanguageReferences))]
        public void LanguageReferenceTest(string key, string value)
        {
            Assert.False(value.Contains("NoNameLang_"), $"The language with key {key} has no assigned name");
        }

        [Fact]
        public void TranslatedPercentageNotEmptyTests()
        {
            System.Collections.ObjectModel.ReadOnlyDictionary<string, string> TranslatedPercent = LanguageData.TranslationPercentages;
            foreach (string key in TranslatedPercent.Keys)
            {
                Assert.True(LanguageData.LanguageReference.ContainsKey(key), $"The language key {key} was not found on LanguageReference");
                Assert.False(LanguageData.TranslationPercentages[key].Contains("404%"), $"Somehow the key {key} has no value");
            }
        }
    }
}

using System.Diagnostics;
using UniGetUI.Core.Language;

namespace UniGetUI.Core.Tools.Tests
{
    public class ToolsTests
    {
        [Theory]
        [InlineData("NonExistentString", false)]
        [InlineData(" ", false)]
        [InlineData("", false)]
        [InlineData("@@", false)]
        [InlineData("0 packages found", true)]
        [InlineData("Add packages or open an existing bundle", true)]
        public void TranslateFunctionTester(string textEntry, bool TranslationExists)
        {
            var langEngine = new LanguageEngine("fr");
            CoreTools.ReloadLanguageEngineInstance("fr");

            Assert.Equal(CoreTools.Translate(textEntry), langEngine.Translate(textEntry));

            if (TranslationExists)
                Assert.NotEqual(CoreTools.Translate(textEntry), textEntry);
            else
                Assert.Equal(CoreTools.Translate(textEntry), textEntry);

            Assert.Equal(CoreTools.AutoTranslated(textEntry), textEntry);
        }

        [Fact]
        public async Task TestWhichFunctionForExistingFile()
        {
            var result = await CoreTools.Which("cmd.exe");
            Assert.True(result.Item1);
            Assert.True(File.Exists(result.Item2));
        }

        [Fact]
        public async Task TestWhichFunctionForNonExistingFile()
        {
            var result = await CoreTools.Which("nonexistentfile.exe");
            Assert.False(result.Item1);
            Assert.Equal("", result.Item2);
        }

        [Theory]
        [InlineData("7zip19.00-helpEr", "7zip19.00 HelpEr")]
        [InlineData("packagename", "Packagename")]
        [InlineData("Packagename", "Packagename")]
        [InlineData("PackageName", "PackageName")]
        [InlineData("pacKagENaMe", "PacKagENaMe")]
        [InlineData("PACKAGENAME", "PACKAGENAME")]
        [InlineData("package-Name", "Package Name")]
        [InlineData("pub-name/pkg-version_2.00.portable", "Pkg Version 2.00")]
        [InlineData("!helloWorld", "!helloWorld")]
        [InlineData("@stylistic/eslint-plugin", "Eslint Plugin")]
        [InlineData("Flask-RESTful", "Flask RESTful")]
        public void TestFormatAsName(string id, string name)
        {
            Assert.Equal(name, CoreTools.FormatAsName(id));
        }

        [Theory]
        [InlineData(89)]
        [InlineData(33)]
        [InlineData(558)]
        [InlineData(12)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(64)]
        public void TestRandomStringGenerator(int length)
        {
            var string1 = CoreTools.RandomString(length);
            var string2 = CoreTools.RandomString(length);
            var string3 = CoreTools.RandomString(length);
            Assert.Equal(string1.Length, length);
            Assert.Equal(string2.Length, length);
            Assert.Equal(string3.Length, length);

            // Zero-lenghted strings are always equal
            // One-lenghted strings are likely to be equal
            if (length > 1)
            {
                Assert.NotEqual(string1, string2);
                Assert.NotEqual(string2, string3);
                Assert.NotEqual(string1, string3);
            }

            foreach (string s in new string[] { string1, string2, string3})
                foreach (char c in s)
                   Assert.True("abcdefghijklmnopqrstuvwxyz0123456789".Contains(c));
        }

        [Theory]
        [InlineData("", 0)]
        [InlineData("https://invalid.url.com/this/is/an/invalid.php?file=to_test&if=the&code_returns=zero", 0)]
        [InlineData("https://www.marticliment.com/wingetui/wingetui_size_test.txt", 460)]
        public async Task TestFileSizeLoader(string uri, long expectedSize)
        {
            var size = await CoreTools.GetFileSizeAsync(uri != ""? new Uri(uri): null);
            Debug.WriteLine(size);
            Assert.Equal(expectedSize / 1048576, size);
        }
    }
}